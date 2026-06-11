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
        // (4,5) é corredor: x=4 é coluna de corredor e y=5 é linha de corredor.
        _game.PawnPositions[_playerId] = new BoardPosition(4, 5);
    }

    private static List<BoardPosition> Path(params (int X, int Y)[] steps) =>
        [.. steps.Select(s => new BoardPosition(s.X, s.Y))];

    [Fact]
    public void ValidateMove_AcceptsOrthogonalPathUsingFullDice()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((4, 6), (4, 7), (4, 8)), 3);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(new BoardPosition(4, 8), result.NewPosition);
        Assert.False(result.EnteredLocation);
    }

    [Fact]
    public void ValidateMove_RejectsDiagonalStep()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((5, 6)), 1);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidateMove_RejectsPathThroughWall()
    {
        // (3,5) é corredor; (3,4)... (1,4) é entrada; (3,4) é parede da sala do topo.
        var result = _service.ValidateMove(_game, _playerId, Path((3, 5), (3, 4)), 2);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsPathShorterThanDiceWhenNotEnteringLocation()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((4, 6), (4, 7)), 3);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsPathLongerThanDice()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((4, 6), (4, 7), (4, 8), (4, 9)), 3);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_EnteringLocationBeforeUsingFullDiceIsValidAndEndsMove()
    {
        // Entrada da sala do topo-esquerda fica em (1,4), adjacente ao corredor (1,5).
        var result = _service.ValidateMove(_game, _playerId, Path((3, 5), (2, 5), (1, 5), (1, 4)), 12);

        Assert.True(result.IsValid);
        Assert.True(result.EnteredLocation);
        Assert.NotNull(result.LocationId);
        Assert.Equal(new BoardPosition(1, 4), result.NewPosition);
    }

    [Fact]
    public void ValidateMove_RejectsEntranceInMiddleOfPath()
    {
        // Passar pela entrada (1,4) e continuar andando é inválido: entrar encerra o movimento.
        var result = _service.ValidateMove(_game, _playerId, Path((3, 5), (2, 5), (1, 5), (1, 4), (1, 5)), 5);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsFirstStepNotAdjacentToCurrentPosition()
    {
        var result = _service.ValidateMove(_game, _playerId, Path((4, 8)), 1);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateMove_RejectsEmptyPathAndUnknownPlayer()
    {
        Assert.False(_service.ValidateMove(_game, _playerId, [], 3).IsValid);
        Assert.False(_service.ValidateMove(_game, Guid.NewGuid(), Path((4, 6)), 1).IsValid);
    }

    [Fact]
    public void ValidateMove_AllowsSharingCellWithAnotherPawn()
    {
        var otherPlayer = Guid.NewGuid();
        _game.PawnPositions[otherPlayer] = new BoardPosition(4, 6);

        var result = _service.ValidateMove(_game, _playerId, Path((4, 6)), 1);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ApplyMove_UpdatesPawnPositionAndReportsLocation()
    {
        var result = _service.ApplyMove(_game, _playerId, Path((3, 5), (2, 5), (1, 5), (1, 4)), 12);

        Assert.True(result.IsValid);
        Assert.Equal(new BoardPosition(1, 4), _game.PawnPositions[_playerId]);
        Assert.True(result.EnteredLocation);
    }

    [Fact]
    public void ApplyMove_InvalidMoveDoesNotChangePosition()
    {
        var before = _game.PawnPositions[_playerId];

        var result = _service.ApplyMove(_game, _playerId, Path((5, 6)), 1);

        Assert.False(result.IsValid);
        Assert.Equal(before, _game.PawnPositions[_playerId]);
    }
}
