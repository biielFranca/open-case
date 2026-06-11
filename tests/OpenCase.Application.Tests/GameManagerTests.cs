using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class GameManagerTests
{
    private readonly GameManager _manager;
    private readonly RoomService _roomService;

    public GameManagerTests()
    {
        var random = new Random(42);
        _roomService = new RoomService(random);
        _manager = new GameManager(
            new BoardService(),
            new GameSetupService(random),
            new TurnService(random));
    }

    private Room CreateStartedRoom(int players = 4)
    {
        var room = _roomService.CreateRoom("Host");
        for (var i = 1; i < players; i++)
        {
            _roomService.JoinRoom(room.Code, $"Jogador {i}");
        }

        var pawnIndex = 0;
        foreach (var player in room.Players)
        {
            _roomService.ChoosePawn(room.Id, player.Id, RoomService.AvailablePawns[pawnIndex++].Id);
            _roomService.SetReady(room.Id, player.Id, true);
        }

        _roomService.StartGame(room.Id, room.HostPlayerId);
        return room;
    }

    [Fact]
    public void StartGame_CreatesGameWithFullDeckHandsPositionsAndSuspects()
    {
        var room = CreateStartedRoom();

        var game = _manager.StartGame(room);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(28, game.AllCards.Count); // 8 suspeitos + 12 locais + 8 armas
        Assert.Equal(room.Players.Count, game.Hands.Count);
        Assert.Equal(room.Players.Count, game.PawnPositions.Count);
        Assert.Equal(room.Players.Count, game.PlayerSuspects.Count);
        Assert.NotNull(game.Solution);

        // Todos os peões começam em células caminháveis.
        var boardService = new BoardService();
        Assert.All(game.PawnPositions.Values, p =>
            Assert.True(boardService.IsWalkable(game.Board, p.X, p.Y)));
    }

    [Fact]
    public void StartGame_FailsIfRoomIsNotInGame()
    {
        var room = _roomService.CreateRoom("Host");

        Assert.Throws<InvalidOperationException>(() => _manager.StartGame(room));
    }

    [Fact]
    public void GetGameByRoom_ReturnsStartedGame()
    {
        var room = CreateStartedRoom();
        var game = _manager.StartGame(room);

        Assert.Same(game, _manager.GetGameByRoom(room.Id));
    }

    [Fact]
    public void RollInitialOrder_DefinesTurnOrderWithAllPlayers()
    {
        var room = CreateStartedRoom();
        var game = _manager.StartGame(room);

        var rolls = _manager.RollInitialOrder(game);

        Assert.Equal(room.Players.Count, rolls.Count);
        Assert.Equal(room.Players.Count, game.TurnState.TurnOrder.Count);
        Assert.All(room.Players, p => Assert.Contains(p.Id, game.TurnState.TurnOrder));
        Assert.Equal(game.TurnState.TurnOrder[0], game.TurnState.CurrentPlayerId);
        Assert.Equal(TurnPhase.RollDice, game.TurnState.Phase);
    }
}
