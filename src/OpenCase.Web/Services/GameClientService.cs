using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using OpenCase.Shared.Dtos;

namespace OpenCase.Web.Services;

/// <summary>
/// Estado do cliente por circuito Blazor: uma única conexão SignalR compartilhada
/// entre o lobby e a tela de partida.
/// </summary>
public class GameClientService(NavigationManager navigation) : IAsyncDisposable
{
    private HubConnection? _hub;

    public RoomDto? Room { get; private set; }
    public PublicGameStateDto? GameState { get; private set; }
    public BoardDto? Board { get; private set; }
    public PlayerHandDto? Hand { get; private set; }
    public List<CardDto> AllCards { get; private set; } = [];
    public PublicHintDto? LastHint { get; private set; }
    public PrivateCardShownDto? LastPrivateCard { get; private set; }
    public string? LastError { get; private set; }
    public string PlayerName { get; set; } = string.Empty;

    public Guid MyPlayerId =>
        Room?.Players.FirstOrDefault(p => p.Name == PlayerName)?.Id ?? Guid.Empty;

    public event Action? OnChange;
    public event Action? OnGameStarted;

    public async Task EnsureConnectedAsync()
    {
        if (_hub is not null)
        {
            return;
        }

        _hub = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<RoomDto>("RoomUpdated", room => Update(() => { Room = room; LastError = null; }));
        _hub.On<PublicGameStateDto, BoardDto>("GameStarted", (state, board) =>
        {
            Update(() => { GameState = state; Board = board; });
            OnGameStarted?.Invoke();
        });
        _hub.On<PlayerHandDto>("PrivateHand", hand => Update(() => Hand = hand));
        _hub.On<List<CardDto>>("CardCatalog", cards => Update(() => AllCards = cards));
        _hub.On<PublicGameStateDto>("TurnUpdated", state => Update(() => GameState = state));
        _hub.On<PublicHintDto>("HintReceived", hint => Update(() => LastHint = hint));
        _hub.On<PrivateCardShownDto>("PrivateCardShown", shown => Update(() => LastPrivateCard = shown));
        _hub.On<string>("ErrorMessage", message => Update(() => LastError = message));

        await _hub.StartAsync();
    }

    public Task CreateRoomAsync() => Invoke("CreateRoom", PlayerName);
    public Task JoinRoomAsync(string code) => Invoke("JoinRoom", code, PlayerName);
    public Task ChoosePawnAsync(Guid pawnId) => Invoke("ChoosePawn", pawnId);
    public Task SetReadyAsync(bool ready) => Invoke("SetReady", ready);
    public Task StartGameAsync() => Invoke("StartGame");
    public Task AddBotAsync() => Invoke("AddBot");
    public Task RollInitialDiceAsync() => Invoke("RollInitialDice");
    public Task RollTurnDiceAsync() => Invoke("RollTurnDice");
    public Task MovePawnAsync(List<PositionDto> path) => Invoke("MovePawn", path);
    public Task MakeSuggestionAsync(Guid suspectId, Guid weaponId) => Invoke("MakeSuggestion", suspectId, weaponId);
    public Task PassRefutationAsync() => Invoke("PassRefutation");
    public Task ShowRefutationCardAsync(Guid cardId) => Invoke("ShowRefutationCard", cardId);
    public Task MakeFinalAccusationAsync() => Invoke("MakeFinalAccusation");
    public Task UseSecretPassageAsync() => Invoke("UseSecretPassage");

    public void ClearHint() => Update(() => LastHint = null);
    public void ClearPrivateCard() => Update(() => LastPrivateCard = null);
    public void ClearError() => Update(() => LastError = null);

    private async Task Invoke(string method, params object?[] args)
    {
        await EnsureConnectedAsync();
        await _hub!.InvokeCoreAsync(method, args);
    }

    private void Update(Action mutate)
    {
        mutate();
        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
        }
    }
}
