using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class MovementServiceTests
{
    private readonly BoardService _boardService = new();
    private readonly MovementService _service;
    private readonly Game _game;
    private readonly Guid _playerId = Guid.NewGuid();

    public MovementServiceTests()
    {
        _service = new MovementService(_boardService);
        _game = new Game { Board = _boardService.CreateDefaultBoard() };
        // A coluna x=7 é um corredor vertical que liga a entrada principal à ala oeste.
        _game.PawnPositions[_playerId] = new BoardPosition(7, 29);
    }

    private static List<BoardPosition> Path(params (int X, int Y)[] steps) =>
        [.. steps.Select(s => new BoardPosition(s.X, s.Y))];

    [Fact]
    public void ValidateMove_AcceptsOrthogonalPathUsingFullDice()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((7, 28), (7, 27), (7, 26)), 3);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(new BoardPosition(7, 26), result.NewPosition);
        Assert.False(result.EnteredLocation);
    }

    [Fact]
    public void ValidateMove_RejectsDiagonalStep()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((8, 28)), 1);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidateMove_RejectsPathThroughWall()
    {
        // (8,29) é corredor, mas (8,28) é a parede inferior do porão.
        var result = _service.ValidateMove(_game, _playerId, Path((8, 29), (8, 28)), 2);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsPathShorterThanDiceWhenNotEnteringLocation()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((7, 28), (7, 27)), 3);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsPathLongerThanDice()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((7, 28), (7, 27), (7, 26), (7, 25)), 3);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_EnteringLocationBeforeUsingFullDiceIsValidAndEndsMove()
    {
        // A porta leste da estufa fica em (6,24), adjacente ao corredor x=7.
        var result = _service.ValidateMove(_game, _playerId, Path((7, 28), (7, 27), (7, 26), (7, 25), (7, 24), (6, 24)), 12);

        Assert.True(result.IsValid);
        Assert.True(result.EnteredLocation);
        Assert.NotNull(result.LocationId);
        Assert.Equal(new BoardPosition(6, 24), result.NewPosition);
    }

    [Fact]
    public void ValidateMove_RejectsEntranceInMiddleOfPath()
    {
        // Passar pela porta da estufa e continuar andando é inválido: entrar encerra o movimento.
        var result = _service.ValidateMove(_game, _playerId, Path((7, 28), (7, 27), (7, 26), (7, 25), (7, 24), (6, 24), (7, 24)), 7);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsFirstStepNotAdjacentToCurrentPosition()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((7, 26)), 1);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsEmptyPathAndUnknownPlayer()
    {
        Assert.False(_service.ValidateMove(_game, _playerId, [], 3).IsValid);
        Assert.False(_service.ValidateMove(_game, Guid.NewGuid(), Path((7, 28)), 1).IsValid);
    }

    [Fact]
    public void ValidateMove_AllowsSharingCellWithAnotherPawn()
    {
        var otherPlayer = Guid.NewGuid();
        _game.PawnPositions[otherPlayer] = new BoardPosition(7, 28);

        var result = _service.ValidateMove(_game, _playerId, Path((7, 28)), 1);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ApplyMove_UpdatesPawnPositionAndReportsLocation()
    {
        var result = _service.ApplyMove(_game, _playerId, Path((7, 28), (7, 27), (7, 26), (7, 25), (7, 24), (6, 24)), 12);

        Assert.True(result.IsValid);
        Assert.Equal(new BoardPosition(6, 24), _game.PawnPositions[_playerId]);
        Assert.True(result.EnteredLocation);
    }

    [Fact]
    public void ApplyMove_InvalidMoveDoesNotChangePosition()
    {
        var before = _game.PawnPositions[_playerId];

        var result = _service.ApplyMove(_game, _playerId, Path((8, 28)), 1);

        Assert.False(result.IsValid);
        Assert.Equal(before, _game.PawnPositions[_playerId]);
    }
}
