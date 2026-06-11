using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Tests;

public class DomainModelsTests
{
    [Fact]
    public void Card_HoldsIdTypeAndName()
    {
        var card = new Card { Id = Guid.NewGuid(), Type = CardType.Suspect, Name = "Coronel Mostarda" };

        Assert.NotEqual(Guid.Empty, card.Id);
        Assert.Equal(CardType.Suspect, card.Type);
        Assert.Equal("Coronel Mostarda", card.Name);
    }

    [Fact]
    public void Room_StartsInLobbyWithEmptyPlayerListAndLimits()
    {
        var room = new Room { Id = Guid.NewGuid(), Code = "ABC123" };

        Assert.Equal(RoomStatus.Lobby, room.Status);
        Assert.Empty(room.Players);
        Assert.Equal(3, Room.MinPlayers);
        Assert.Equal(8, Room.MaxPlayers);
    }

    [Fact]
    public void Player_StartsNotReadyAndConnected()
    {
        var player = new Player { Id = Guid.NewGuid(), Name = "Biel" };

        Assert.False(player.IsReady);
        Assert.False(player.IsHost);
        Assert.Null(player.PawnId);
        Assert.Equal(PlayerConnectionStatus.Connected, player.ConnectionStatus);
    }

    [Fact]
    public void GameSolution_HoldsExactlyOneCardOfEachType()
    {
        var solution = new GameSolution
        {
            SuspectCardId = Guid.NewGuid(),
            LocationCardId = Guid.NewGuid(),
            WeaponCardId = Guid.NewGuid(),
        };

        Assert.NotEqual(Guid.Empty, solution.SuspectCardId);
        Assert.NotEqual(Guid.Empty, solution.LocationCardId);
        Assert.NotEqual(Guid.Empty, solution.WeaponCardId);
    }

    [Fact]
    public void Game_StartsWaitingWithEmptyHandsAndCreationTimestamp()
    {
        var before = DateTimeOffset.UtcNow;
        var game = new Game { Id = Guid.NewGuid(), RoomId = Guid.NewGuid() };

        Assert.Equal(GameStatus.WaitingToStart, game.Status);
        Assert.Empty(game.Hands);
        Assert.Empty(game.Events);
        Assert.InRange(game.CreatedAt, before, DateTimeOffset.UtcNow);
        Assert.Null(game.FinishedAt);
    }

    [Fact]
    public void PlayerHand_StartsEmpty()
    {
        var hand = new PlayerHand { PlayerId = Guid.NewGuid() };

        Assert.Empty(hand.Cards);
    }

    [Fact]
    public void TurnState_StartsAtRollPhaseWithoutDiceValue()
    {
        var turn = new TurnState();

        Assert.Equal(TurnPhase.RollDice, turn.Phase);
        Assert.Null(turn.DiceValue);
        Assert.Empty(turn.TurnOrder);
        Assert.Empty(turn.PlayersToSkip);
    }

    [Fact]
    public void Board_Holds20x20GridAndLocations()
    {
        var board = new Board();

        Assert.Equal(20, Board.Width);
        Assert.Equal(20, Board.Height);
        Assert.Empty(board.Cells);
        Assert.Empty(board.Locations);
    }

    [Fact]
    public void BoardCell_HoldsCoordinatesTypeAndOptionalLocation()
    {
        var cell = new BoardCell { X = 3, Y = 7, Type = CellType.Entrance, LocationId = Guid.NewGuid() };

        Assert.Equal(3, cell.X);
        Assert.Equal(7, cell.Y);
        Assert.Equal(CellType.Entrance, cell.Type);
        Assert.NotNull(cell.LocationId);
    }

    [Fact]
    public void BoardLocation_MayHaveSecretPassage()
    {
        var location = new BoardLocation { Id = Guid.NewGuid(), Name = "Biblioteca" };

        Assert.Null(location.SecretPassageToLocationId);
        Assert.Empty(location.EntranceCells);
    }

    [Fact]
    public void Suggestion_HoldsSuspectLocationWeaponAndTimestamp()
    {
        var suggestion = new Suggestion
        {
            Id = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SuggestingPlayerId = Guid.NewGuid(),
            SuspectCardId = Guid.NewGuid(),
            LocationCardId = Guid.NewGuid(),
            WeaponCardId = Guid.NewGuid(),
        };

        Assert.False(suggestion.WasRefuted);
        Assert.NotEqual(default, suggestion.CreatedAt);
    }

    [Fact]
    public void RefutationState_StartsUnresolved()
    {
        var refutation = new RefutationState { SuggestionId = Guid.NewGuid() };

        Assert.False(refutation.AllPlayersPassed);
        Assert.Null(refutation.ShownCardId);
        Assert.Null(refutation.ShownByPlayerId);
        Assert.Empty(refutation.RefutationOrder);
    }

    [Fact]
    public void FinalAccusation_HoldsResult()
    {
        var accusation = new FinalAccusation
        {
            Id = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            AccusingPlayerId = Guid.NewGuid(),
            SuspectCardId = Guid.NewGuid(),
            LocationCardId = Guid.NewGuid(),
            WeaponCardId = Guid.NewGuid(),
            IsCorrect = true,
        };

        Assert.True(accusation.IsCorrect);
        Assert.NotEqual(default, accusation.CreatedAt);
    }

    [Fact]
    public void Hint_HoldsTypeAndOptionalTarget()
    {
        var hint = new Hint { Id = Guid.NewGuid(), GameId = Guid.NewGuid(), Type = HintType.Private, TargetPlayerId = Guid.NewGuid(), Text = "Alguém viu algo na biblioteca..." };

        Assert.Equal(HintType.Private, hint.Type);
        Assert.NotNull(hint.TargetPlayerId);
    }

    [Fact]
    public void GameEvent_HoldsTypeDataAndTimestamp()
    {
        var gameEvent = new GameEvent { Id = Guid.NewGuid(), GameId = Guid.NewGuid(), Type = "RoomCreated", Data = "{}" };

        Assert.Equal("RoomCreated", gameEvent.Type);
        Assert.NotEqual(default, gameEvent.CreatedAt);
    }

    [Fact]
    public void Enums_HaveExpectedMembers()
    {
        Assert.Equal(3, Enum.GetValues<CardType>().Length);
        Assert.Contains(HintType.Noise, Enum.GetValues<HintType>());
        Assert.Contains(CellType.SecretPassage, Enum.GetValues<CellType>());
        Assert.Contains(TurnPhase.FinalAccusation, Enum.GetValues<TurnPhase>());
        Assert.Contains(GameStatus.Finished, Enum.GetValues<GameStatus>());
        Assert.Contains(RoomStatus.InGame, Enum.GetValues<RoomStatus>());
        Assert.Contains(PlayerConnectionStatus.Disconnected, Enum.GetValues<PlayerConnectionStatus>());
    }
}
