using Microsoft.AspNetCore.SignalR.Client;
using OpenCase.Application.Services;
using OpenCase.Shared.Dtos;

namespace OpenCase.Web.Services;

/// <summary>
/// Jogador automático: conecta ao GameHub como um cliente comum, escolhe
/// personagem, fica pronto e joga turnos sozinho — anda pelo tabuleiro,
/// entra em salas para palpitar, refuta quando obrigado e acusa na fase final.
/// </summary>
public sealed class BotClient : IAsyncDisposable
{
    private readonly HubConnection _hub;
    private readonly string _name;
    private readonly ILogger _logger;

    private Guid _myId;
    private bool _pawnChosen;
    private string? _lastActionKey;

    private readonly List<CardDto> _myCards = [];
    private readonly List<CardDto> _suspects = [];
    private readonly List<CardDto> _weapons = [];
    private Dictionary<(int X, int Y), string> _cellTypes = [];
    private SuggestionDto? _activeSuggestion;
    private readonly HashSet<string> _actedRefutations = [];

    public string Name => _name;

    private BotClient(string url, string name, ILogger logger)
    {
        _name = name;
        _logger = logger;
        _hub = new HubConnectionBuilder().WithUrl(url).WithAutomaticReconnect().Build();
        RegisterHandlers();
    }

    public static async Task<BotClient> JoinAsync(string url, string roomCode, string name, ILogger logger)
    {
        var bot = new BotClient(url, name, logger);
        await bot._hub.StartAsync();
        await bot._hub.InvokeAsync("JoinRoom", roomCode, name);
        return bot;
    }

    private void RegisterHandlers()
    {
        _hub.On<RoomDto>("RoomUpdated", async room =>
        {
            var me = room.Players.FirstOrDefault(p => p.Name == _name);
            if (me is null)
            {
                return;
            }

            _myId = me.Id;
            if (!_pawnChosen && me.PawnId is null)
            {
                _pawnChosen = true;
                var taken = room.Players.Where(p => p.PawnId is not null).Select(p => p.PawnId!.Value).ToHashSet();
                var pawn = RoomService.AvailablePawns.FirstOrDefault(p => !taken.Contains(p.Id));
                if (pawn is not null)
                {
                    await _hub.InvokeAsync("ChoosePawn", pawn.Id);
                    await _hub.InvokeAsync("SetReady", true);
                }
            }
        });

        _hub.On<PublicGameStateDto, BoardDto>("GameStarted", (state, board) =>
        {
            CaptureBoard(board);
        });

        _hub.On<List<CardDto>>("CardCatalog", cards =>
        {
            _suspects.Clear();
            _suspects.AddRange(cards.Where(c => c.Type == "Suspect"));
            _weapons.Clear();
            _weapons.AddRange(cards.Where(c => c.Type == "Weapon"));
        });

        _hub.On<PlayerHandDto>("PrivateHand", hand =>
        {
            _myCards.Clear();
            _myCards.AddRange(hand.Cards);
        });

        _hub.On<SuggestionDto>("SuggestionCreated", suggestion => _activeSuggestion = suggestion);

        _hub.On<PublicRefutationDto>("RefutationRequested", HandleRefutationAsync);

        _hub.On<PublicGameStateDto>("TurnUpdated", async state =>
        {
            if (state.ActiveRefutation is not null)
            {
                await HandleRefutationAsync(state.ActiveRefutation);
            }

            await PlayMyTurnAsync(state);
        });

        _hub.On<string>("ErrorMessage", message =>
            _logger.LogWarning("Bot {Bot}: erro do servidor: {Message}", _name, message));
    }

    private void CaptureBoard(BoardDto board) =>
        _cellTypes = board.Cells.ToDictionary(c => (c.X, c.Y), c => c.Type);

    private async Task HandleRefutationAsync(PublicRefutationDto refutation)
    {
        if (refutation.IsResolved || refutation.CurrentRefutingPlayerId != _myId || _activeSuggestion is null)
        {
            return;
        }

        if (!_actedRefutations.Add($"{refutation.SuggestionId}:{_myId}"))
        {
            return;
        }

        Guid[] cited =
        [
            _activeSuggestion.SuspectCardId,
            _activeSuggestion.LocationCardId,
            _activeSuggestion.WeaponCardId,
        ];

        await Task.Delay(700);
        var match = _myCards.FirstOrDefault(c => cited.Contains(c.Id));
        if (match is not null)
        {
            await _hub.InvokeAsync("ShowRefutationCard", match.Id);
        }
        else
        {
            await _hub.InvokeAsync("PassRefutation");
        }
    }

    private async Task PlayMyTurnAsync(PublicGameStateDto state)
    {
        if (state.Turn.CurrentPlayerId != _myId || state.Status != "InProgress")
        {
            return;
        }

        // Uma ação por (turno, fase) para não reagir duas vezes ao mesmo broadcast.
        var actionKey = $"{state.Turn.TurnNumber}:{state.Turn.Phase}";
        if (_lastActionKey == actionKey)
        {
            return;
        }

        _lastActionKey = actionKey;

        try
        {
            switch (state.Turn.Phase)
            {
                case "RollDice":
                    await Task.Delay(700);
                    await _hub.InvokeAsync("RollTurnDice");
                    break;

                case "Move" when state.Turn.DiceValue is int dice
                    && state.PawnPositions.TryGetValue(_myId, out var pos):
                    await MoveAsync(pos, dice);
                    break;

                case "Suggestion":
                    await SuggestAsync();
                    break;

                case "FinalAccusation":
                    // Para não travar o turno, o bot acusa (provavelmente erra e só perde a vez).
                    await Task.Delay(700);
                    await _hub.InvokeAsync("MakeFinalAccusation");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bot {Bot} falhou na fase {Phase}.", _name, state.Turn.Phase);
        }
    }

    private async Task MoveAsync(PositionDto start, int dice)
    {
        var path = BuildPathToEntrance(start, dice) ?? BuildRandomWalk(start, dice);
        if (path is null || path.Count == 0)
        {
            _logger.LogInformation("Bot {Bot} sem movimento legal em ({X},{Y}).", _name, start.X, start.Y);
            return;
        }

        await Task.Delay(700);
        await _hub.InvokeAsync("MovePawn", path);
    }

    private async Task SuggestAsync()
    {
        if (_suspects.Count == 0 || _weapons.Count == 0)
        {
            _logger.LogWarning("Bot {Bot} não pode palpitar: catálogo vazio.", _name);
            return;
        }

        var suspect = _suspects[Random.Shared.Next(_suspects.Count)];
        var weapon = _weapons[Random.Shared.Next(_weapons.Count)];

        await Task.Delay(700);
        await _hub.InvokeAsync("MakeSuggestion", suspect.Id, weapon.Id);
    }

    // ─── Navegação no tabuleiro ───

    private string? TypeAt((int X, int Y) cell) => _cellTypes.GetValueOrDefault(cell);

    private static IEnumerable<(int X, int Y)> Neighbors((int X, int Y) cell)
    {
        yield return (cell.X + 1, cell.Y);
        yield return (cell.X - 1, cell.Y);
        yield return (cell.X, cell.Y + 1);
        yield return (cell.X, cell.Y - 1);
    }

    /// <summary>BFS até a entrada de sala mais próxima alcançável dentro do dado.</summary>
    private List<PositionDto>? BuildPathToEntrance(PositionDto start, int dice)
    {
        if (_cellTypes.Count == 0)
        {
            return null;
        }

        var origin = (start.X, start.Y);
        var dist = new Dictionary<(int X, int Y), int> { [origin] = 0 };
        var prev = new Dictionary<(int X, int Y), (int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            var d = dist[cur];
            if (d >= dice)
            {
                continue;
            }

            foreach (var n in Neighbors(cur))
            {
                var type = TypeAt(n);
                if (type == "Entrance" && d + 1 <= dice)
                {
                    var path = Reconstruct(prev, origin, cur);
                    path.Add(new PositionDto(n.X, n.Y));
                    return path;
                }

                if (type == "Path" && !dist.ContainsKey(n))
                {
                    dist[n] = d + 1;
                    prev[n] = cur;
                    queue.Enqueue(n);
                }
            }
        }

        return null;
    }

    /// <summary>Passeio de exatamente <paramref name="dice"/> passos pelos corredores.</summary>
    private List<PositionDto>? BuildRandomWalk(PositionDto start, int dice)
    {
        var path = new List<PositionDto>();
        var cur = (start.X, start.Y);

        for (var i = 0; i < dice; i++)
        {
            var options = Neighbors(cur).Where(n => TypeAt(n) == "Path").ToList();
            if (options.Count == 0)
            {
                return null;
            }

            var previous = i > 0 ? (path[i - 1].X, path[i - 1].Y) : (start.X, start.Y);
            var fresh = options.Where(o => o != previous).ToList();
            var pool = fresh.Count > 0 ? fresh : options;
            var pick = pool[Random.Shared.Next(pool.Count)];

            path.Add(new PositionDto(pick.X, pick.Y));
            cur = pick;
        }

        return path;
    }

    private static List<PositionDto> Reconstruct(
        Dictionary<(int X, int Y), (int X, int Y)> prev, (int X, int Y) origin, (int X, int Y) cur)
    {
        var steps = new List<PositionDto>();
        var c = cur;
        while (c != origin)
        {
            steps.Add(new PositionDto(c.X, c.Y));
            c = prev[c];
        }

        steps.Reverse();
        return steps;
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
