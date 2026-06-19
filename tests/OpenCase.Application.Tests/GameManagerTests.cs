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
        Assert.Equal(36, game.AllCards.Count); // 12 suspeitos + 12 locais + 12 armas
        Assert.Equal(room.Players.Count, game.Hands.Count);
        Assert.Equal(room.Players.Count, game.PawnPositions.Count);
        Assert.Equal(room.Players.Count, game.PlayerSuspects.Count);
        Assert.NotNull(game.Solution);

        // Cada personagem começa dentro de seu cômodo temático.
        var boardService = new BoardService();
        foreach (var player in room.Players)
        {
            var position = game.PawnPositions[player.Id];
            var location = boardService.GetLocationByCell(game.Board, position.X, position.Y);

            Assert.NotNull(location);
            Assert.Equal(GameManager.StartingLocationByPawnId[player.PawnId!.Value], location!.Name);
        }
    }

    [Fact]
    public void CharacterStartingLocations_CoverAllCharactersAndRoomsWithoutRepeating()
    {
        var board = new BoardService().CreateDefaultBoard();

        Assert.Equal(RoomService.AvailablePawns.Count, GameManager.StartingLocationByPawnId.Count);
        Assert.Equal(board.Locations.Count, GameManager.StartingLocationByPawnId.Values.Distinct().Count());
        Assert.All(RoomService.AvailablePawns, pawn =>
            Assert.True(GameManager.StartingLocationByPawnId.ContainsKey(pawn.Id)));
        Assert.All(GameManager.StartingLocationByPawnId.Values, locationName =>
            Assert.Contains(board.Locations, location => location.Name == locationName));
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
