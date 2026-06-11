using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class SecretPassageServiceTests
{
    private readonly BoardService _boardService = new();
    private readonly SecretPassageService _service;
    private readonly Game _game;
    private readonly Guid _playerId = Guid.NewGuid();

    public SecretPassageServiceTests()
    {
        _service = new SecretPassageService(_boardService);
        _game = new Game { Board = _boardService.CreateDefaultBoard(), Status = GameStatus.InProgress };
        _game.TurnState.TurnOrder = [_playerId];
        _game.TurnState.CurrentPlayerId = _playerId;
        _game.TurnState.Phase = TurnPhase.Move;
    }

    private BoardLocation LocationWithPassage() =>
        _game.Board.Locations.First(l => l.SecretPassageToLocationId is not null);

    private BoardLocation LocationWithoutPassage() =>
        _game.Board.Locations.First(l => l.SecretPassageToLocationId is null);

    private void PlacePlayerInside(BoardLocation location)
    {
        var entrance = location.EntranceCells[0];
        _game.PawnPositions[_playerId] = new BoardPosition(entrance.X, entrance.Y);
    }

    [Fact]
    public void CanUseSecretPassage_FalseWhenOutsideAnyLocation()
    {
        _game.PawnPositions[_playerId] = new BoardPosition(4, 5); // corredor

        Assert.False(_service.CanUseSecretPassage(_game, _playerId));
    }

    [Fact]
    public void CanUseSecretPassage_FalseWhenLocationHasNoPassage()
    {
        PlacePlayerInside(LocationWithoutPassage());

        Assert.False(_service.CanUseSecretPassage(_game, _playerId));
    }

    [Fact]
    public void CanUseSecretPassage_TrueWhenInsideLocationWithPassage()
    {
        PlacePlayerInside(LocationWithPassage());

        Assert.True(_service.CanUseSecretPassage(_game, _playerId));
    }

    [Fact]
    public void UseSecretPassage_MovesPawnToTargetLocation()
    {
        var origin = LocationWithPassage();
        PlacePlayerInside(origin);

        var target = _service.UseSecretPassage(_game, _playerId);

        Assert.Equal(origin.SecretPassageToLocationId, target.Id);
        var position = _game.PawnPositions[_playerId];
        var currentLocation = _boardService.GetLocationByCell(_game.Board, position.X, position.Y);
        Assert.Equal(target.Id, currentLocation!.Id);
    }

    [Fact]
    public void UseSecretPassage_CountsAsTurnActionAndAllowsSuggestion()
    {
        PlacePlayerInside(LocationWithPassage());

        _service.UseSecretPassage(_game, _playerId);

        Assert.Equal(TurnPhase.Suggestion, _game.TurnState.Phase);
    }

    [Fact]
    public void UseSecretPassage_ThrowsWhenNotAllowed()
    {
        _game.PawnPositions[_playerId] = new BoardPosition(4, 5);

        Assert.Throws<InvalidOperationException>(() => _service.UseSecretPassage(_game, _playerId));

        PlacePlayerInside(LocationWithoutPassage());

        Assert.Throws<InvalidOperationException>(() => _service.UseSecretPassage(_game, _playerId));
    }
}
