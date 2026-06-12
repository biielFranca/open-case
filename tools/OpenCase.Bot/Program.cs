// Bot de teste manual: entra numa sala, escolhe peão, fica pronto e
// responde refutações automaticamente. Uso:
//   dotnet run -- <url> <codigoSala> <nomeBot>
using Microsoft.AspNetCore.SignalR.Client;
using OpenCase.Shared.Dtos;

var url = args.Length > 0 ? args[0] : "http://localhost:5800/gamehub";
var roomCode = args.Length > 1 ? args[1] : throw new ArgumentException("Informe o código da sala.");
var botName = args.Length > 2 ? args[2] : $"Bot{Random.Shared.Next(100, 999)}";

Guid myId = Guid.Empty;
var myCards = new List<CardDto>();
SuggestionDto? activeSuggestion = null;
var pawnChosen = false;
var actedRefutations = new HashSet<string>();

var hub = new HubConnectionBuilder().WithUrl(url).WithAutomaticReconnect().Build();

hub.On<RoomDto>("RoomUpdated", async room =>
{
    var me = room.Players.FirstOrDefault(p => p.Name == botName);
    if (me is null)
    {
        return;
    }

    myId = me.Id;
    if (!pawnChosen && me.PawnId is null)
    {
        pawnChosen = true;
        var takenPawns = room.Players.Where(p => p.PawnId is not null).Select(p => p.PawnId!.Value).ToHashSet();
        // Peões padrão usam GUIDs sequenciais ...0001 a ...0008.
        for (var i = 1; i <= 8; i++)
        {
            var pawnId = Guid.Parse($"00000000-0000-0000-0000-00000000000{i}");
            if (!takenPawns.Contains(pawnId))
            {
                await hub.InvokeAsync("ChoosePawn", pawnId);
                await hub.InvokeAsync("SetReady", true);
                Console.WriteLine($"[{botName}] peão {i} escolhido, pronto.");
                break;
            }
        }
    }
});

hub.On<PlayerHandDto>("PrivateHand", hand =>
{
    myCards.Clear();
    myCards.AddRange(hand.Cards);
    Console.WriteLine($"[{botName}] recebi {hand.Cards.Count} cartas: " +
        string.Join(", ", hand.Cards.Select(c => c.Name)));
});

hub.On<SuggestionDto>("SuggestionCreated", suggestion =>
{
    activeSuggestion = suggestion;
    Console.WriteLine($"[{botName}] palpite criado por {suggestion.SuggestingPlayerId}.");
});

async Task HandleRefutation(PublicRefutationDto refutation)
{
    if (refutation.IsResolved || refutation.CurrentRefutingPlayerId != myId || activeSuggestion is null)
    {
        return;
    }

    var key = $"{refutation.SuggestionId}:{myId}";
    if (!actedRefutations.Add(key))
    {
        return;
    }

    Guid[] cited = [activeSuggestion.SuspectCardId, activeSuggestion.LocationCardId, activeSuggestion.WeaponCardId];
    var match = myCards.FirstOrDefault(c => cited.Contains(c.Id));
    if (match is not null)
    {
        Console.WriteLine($"[{botName}] refutando com {match.Name}.");
        await hub.InvokeAsync("ShowRefutationCard", match.Id);
    }
    else
    {
        Console.WriteLine($"[{botName}] sem carta compatível, passando.");
        await hub.InvokeAsync("PassRefutation");
    }
}

hub.On<PublicRefutationDto>("RefutationRequested", HandleRefutation);

var lastRolledTurn = -1;
async Task PlayMyTurn(PublicGameStateDto state)
{
    if (state.Turn.CurrentPlayerId != myId || state.Status != "InProgress")
    {
        return;
    }

    if (state.Turn.Phase == "RollDice" && lastRolledTurn != state.Turn.TurnNumber)
    {
        lastRolledTurn = state.Turn.TurnNumber;
        await Task.Delay(600);
        Console.WriteLine($"[{botName}] minha vez, rolando o dado.");
        await hub.InvokeAsync("RollTurnDice");
        return;
    }

    if (state.Turn.Phase == "Move" && state.Turn.DiceValue is int dice
        && state.PawnPositions.TryGetValue(myId, out var pos))
    {
        // Estratégia simples: andar pelo corredor inferior (y=19), que é todo caminhável.
        if (pos.Y != 19)
        {
            Console.WriteLine($"[{botName}] estou fora do corredor ({pos.X},{pos.Y}); não sei mover. Pulando ação.");
            return;
        }

        var direction = pos.X + dice <= 19 ? 1 : -1;
        var path = Enumerable.Range(1, dice)
            .Select(i => new PositionDto(pos.X + i * direction, 19))
            .ToList();
        await Task.Delay(600);
        Console.WriteLine($"[{botName}] movendo {dice} casas para {(direction > 0 ? "direita" : "esquerda")}.");
        await hub.InvokeAsync("MovePawn", path);
    }
}

hub.On<PublicGameStateDto>("TurnUpdated", async state =>
{
    if (state.ActiveRefutation is not null)
    {
        await HandleRefutation(state.ActiveRefutation);
    }

    await PlayMyTurn(state);
});

hub.On<string>("ErrorMessage", message => Console.WriteLine($"[{botName}] ERRO: {message}"));
hub.On<object>("GameFinished", result => Console.WriteLine($"[{botName}] fim de jogo: {result}"));

await hub.StartAsync();
await hub.InvokeAsync("JoinRoom", roomCode, botName);
Console.WriteLine($"[{botName}] entrei na sala {roomCode}.");

await Task.Delay(Timeout.Infinite);
