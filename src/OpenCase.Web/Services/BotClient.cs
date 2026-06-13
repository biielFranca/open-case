using Microsoft.AspNetCore.SignalR.Client;
using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Shared.Dtos;

namespace OpenCase.Web.Services;

/// <summary>
/// Jogador automático: conecta ao GameHub como um cliente comum, escolhe
/// personagem, fica pronto, joga turnos simples e refuta quando obrigado.
/// </summary>
public sealed class BotClient : IAsyncDisposable
{
    private readonly HubConnection _hub;
    private readonly string _name;
    private readonly ILogger _logger;

    private Guid _myId;
    private bool _pawnChosen;
    private int _lastRolledTurn = -1;
    private readonly List<CardDto> _myCards = [];
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

        await Task.Delay(800);
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

        if (state.Turn.Phase == "RollDice" && _lastRolledTurn != state.Turn.TurnNumber)
        {
            _lastRolledTurn = state.Turn.TurnNumber;
            await Task.Delay(800);
            await _hub.InvokeAsync("RollTurnDice");
            return;
        }

        if (state.Turn.Phase == "Move" && state.Turn.DiceValue is int dice
            && state.PawnPositions.TryGetValue(_myId, out var pos))
        {
            var corridorRow = Board.Height - 1;
            if (pos.Y != corridorRow)
            {
                // Fora do corredor inferior (ex.: puxado por um palpite): sem estratégia, não age.
                _logger.LogInformation("Bot {Bot} fora do corredor em ({X},{Y}); turno parado.", _name, pos.X, pos.Y);
                return;
            }

            var direction = pos.X + dice <= Board.Width - 1 ? 1 : -1;
            var path = Enumerable.Range(1, dice)
                .Select(i => new PositionDto(pos.X + i * direction, corridorRow))
                .ToList();

            await Task.Delay(800);
            await _hub.InvokeAsync("MovePawn", path);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
