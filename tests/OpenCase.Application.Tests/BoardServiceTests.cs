using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class BoardServiceTests
{
    private readonly BoardService _service = new();

    [Fact]
    public void CreateDefaultBoard_Is30x30WithACellForEveryCoordinate()
    {
        var board = _service.CreateDefaultBoard();

        Assert.Equal(900, board.Cells.Count);
        for (var x = 0; x < Board.Width; x++)
        {
            for (var y = 0; y < Board.Height; y++)
            {
                Assert.NotNull(_service.GetCell(board, x, y));
            }
        }
    }

    [Fact]
    public void CreateDefaultBoard_HasTwelveNamedLocations()
    {
        var board = _service.CreateDefaultBoard();

        Assert.Equal(12, board.Locations.Count);
        Assert.All(board.Locations, l => Assert.False(string.IsNullOrWhiteSpace(l.Name)));
        Assert.Equal(12, board.Locations.Select(l => l.Id).Distinct().Count());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(30, 0)]
    [InlineData(0, 30)]
    public void GetCell_OutsideGridReturnsNull(int x, int y)
    {
        var board = _service.CreateDefaultBoard();

        Assert.Null(_service.GetCell(board, x, y));
        Assert.False(_service.IsWalkable(board, x, y));
    }

    [Fact]
    public void EveryLocationHasAnEntranceAndSomeHaveMultipleDoors()
    {
        var board = _service.CreateDefaultBoard();

        Assert.All(board.Locations, location =>
            Assert.NotEmpty(location.EntranceCells));
        Assert.Contains(board.Locations, location => location.EntranceCells.Count >= 3);
    }

    [Fact]
    public void EntranceCellsAreMarkedOnTheGridAndBelongToTheLocation()
    {
        var board = _service.CreateDefaultBoard();

        foreach (var location in board.Locations)
        {
            foreach (var entrance in location.EntranceCells)
            {
                var cell = _service.GetCell(board, entrance.X, entrance.Y);
                Assert.NotNull(cell);
                Assert.Equal(CellType.Entrance, cell!.Type);
                Assert.Equal(location.Id, cell.LocationId);
            }
        }
    }

    [Fact]
    public void SecretPassagesPointToValidOtherLocations()
    {
        var board = _service.CreateDefaultBoard();
        var locationsWithPassage = board.Locations.Where(l => l.SecretPassageToLocationId is not null).ToList();

        Assert.Equal(4, locationsWithPassage.Count);
        foreach (var location in locationsWithPassage)
        {
            var target = _service.GetSecretPassageTarget(board, location.Id);
            Assert.NotNull(target);
            Assert.NotEqual(location.Id, target!.Id);
            Assert.Contains(board.Locations, l => l.Id == target.Id);
            Assert.Equal(location.Id, target.SecretPassageToLocationId);
        }
    }

    [Fact]
    public void GetSecretPassageTarget_ReturnsNullForLocationWithoutPassage()
    {
        var board = _service.CreateDefaultBoard();
        var withoutPassage = board.Locations.First(l => l.SecretPassageToLocationId is null);

        Assert.Null(_service.GetSecretPassageTarget(board, withoutPassage.Id));
    }

    [Fact]
    public void IsWalkable_PathAndEntranceAreWalkableWallsAndRoomInteriorAreNot()
    {
        var board = _service.CreateDefaultBoard();
        var path = board.Cells.First(c => c.Type == CellType.Path);
        var wall = board.Cells.First(c => c.Type == CellType.Wall);
        var entrance = board.Cells.First(c => c.Type == CellType.Entrance);
        var interior = board.Cells.First(c => c.Type == CellType.Location);

        Assert.True(_service.IsWalkable(board, path.X, path.Y));
        Assert.True(_service.IsWalkable(board, entrance.X, entrance.Y));
        Assert.False(_service.IsWalkable(board, wall.X, wall.Y));
        Assert.False(_service.IsWalkable(board, interior.X, interior.Y));
    }

    [Fact]
    public void IsInsideLocation_And_GetLocationByCell_AgreeForRoomCells()
    {
        var board = _service.CreateDefaultBoard();
        var interior = board.Cells.First(c => c.Type == CellType.Location);
        var corridor = board.Cells.First(c => c.Type == CellType.Path);

        Assert.True(_service.IsInsideLocation(board, interior.X, interior.Y));
        Assert.NotNull(_service.GetLocationByCell(board, interior.X, interior.Y));
        Assert.False(_service.IsInsideLocation(board, corridor.X, corridor.Y));
        Assert.Null(_service.GetLocationByCell(board, corridor.X, corridor.Y));
    }

    [Fact]
    public void EveryEntranceIsAdjacentToAWalkableCorridorCell()
    {
        var board = _service.CreateDefaultBoard();

        foreach (var location in board.Locations)
        {
            foreach (var entrance in location.EntranceCells)
            {
                var neighbors = new (int X, int Y)[]
                {
                    (entrance.X + 1, entrance.Y),
                    (entrance.X - 1, entrance.Y),
                    (entrance.X, entrance.Y + 1),
                    (entrance.X, entrance.Y - 1),
                };

                Assert.Contains(neighbors, n =>
                    _service.GetCell(board, n.X, n.Y)?.Type == CellType.Path);
            }
        }
    }
}
