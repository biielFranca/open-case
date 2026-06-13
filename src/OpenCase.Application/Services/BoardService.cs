using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Planta principal da mansão: cômodos de tamanhos variados, corredores amplos
/// e portas distribuídas de acordo com a arquitetura de cada ambiente.
/// </summary>
public class BoardService
{
    private static readonly RoomSpec[] RoomSpecs =
    [
        new("Biblioteca", 0, 0, 7, 7, [(3, 6), (6, 3)]),
        new("Salão de Festas", 9, 0, 9, 8, [(12, 7), (15, 7)]),
        new("Cozinha", 23, 0, 7, 7, [(23, 4), (26, 6)]),

        new("Jardim de Inverno", 0, 10, 7, 8, [(6, 13), (3, 17)]),
        new("Sala de Jantar", 9, 10, 8, 8, [(12, 10), (16, 14)]),
        new("Hall", 19, 9, 5, 5, [(21, 9), (21, 13), (23, 11)]),
        new("Escritório", 25, 9, 5, 9, [(25, 12), (27, 17)]),
        new("Salão de Jogos", 18, 15, 7, 5, [(20, 15), (24, 18), (22, 19)]),

        new("Estufa", 0, 21, 7, 7, [(6, 24)]),
        new("Porão", 8, 22, 6, 7, [(10, 22), (13, 25)]),
        new("Observatório", 16, 21, 7, 8, [(19, 21), (22, 25)]),
        new("Quarto de Hóspedes", 24, 21, 6, 7, [(26, 21), (24, 25)]),
    ];

    public Board CreateDefaultBoard()
    {
        var board = new Board();
        var rooms = RoomSpecs.Select(spec => (Spec: spec, Location: new BoardLocation { Name = spec.Name })).ToList();

        board.Locations.AddRange(rooms.Select(room => room.Location));
        LinkSecretPassage(rooms[0].Location, rooms[11].Location);
        LinkSecretPassage(rooms[2].Location, rooms[8].Location);

        for (var x = 0; x < Board.Width; x++)
        {
            for (var y = 0; y < Board.Height; y++)
            {
                board.Cells.Add(new BoardCell { X = x, Y = y, Type = CellType.Path });
            }
        }

        foreach (var room in rooms)
        {
            PaintRoom(board, room.Spec, room.Location);
        }

        return board;
    }

    public BoardCell? GetCell(Board board, int x, int y) =>
        x is < 0 or >= Board.Width || y is < 0 or >= Board.Height
            ? null
            : board.Cells[x * Board.Height + y];

    public bool IsWalkable(Board board, int x, int y) =>
        GetCell(board, x, y)?.Type is CellType.Path or CellType.Entrance;

    public bool IsInsideLocation(Board board, int x, int y) =>
        GetCell(board, x, y)?.Type is CellType.Location or CellType.SecretPassage or CellType.Entrance;

    public BoardLocation? GetLocationByCell(Board board, int x, int y)
    {
        var locationId = GetCell(board, x, y)?.LocationId;
        return locationId is null ? null : board.Locations.FirstOrDefault(l => l.Id == locationId);
    }

    public BoardLocation? GetSecretPassageTarget(Board board, Guid locationId)
    {
        var location = board.Locations.FirstOrDefault(l => l.Id == locationId)
            ?? throw new InvalidOperationException("Local não existe neste tabuleiro.");

        return location.SecretPassageToLocationId is null
            ? null
            : board.Locations.FirstOrDefault(l => l.Id == location.SecretPassageToLocationId);
    }

    private static void PaintRoom(Board board, RoomSpec spec, BoardLocation location)
    {
        for (var x = spec.X; x < spec.X + spec.Width; x++)
        {
            for (var y = spec.Y; y < spec.Y + spec.Height; y++)
            {
                var isBorder = x == spec.X
                    || x == spec.X + spec.Width - 1
                    || y == spec.Y
                    || y == spec.Y + spec.Height - 1;

                var cell = board.Cells[x * Board.Height + y];
                cell.Type = isBorder ? CellType.Wall : CellType.Location;
                cell.LocationId = location.Id;
            }
        }

        if (location.SecretPassageToLocationId is not null)
        {
            var passage = board.Cells[(spec.X + 1) * Board.Height + spec.Y + 1];
            passage.Type = CellType.SecretPassage;
        }

        foreach (var (x, y) in spec.Entrances)
        {
            var entrance = new BoardPosition(x, y);
            location.EntranceCells.Add(entrance);

            var cell = board.Cells[x * Board.Height + y];
            cell.Type = CellType.Entrance;
            cell.LocationId = location.Id;
        }
    }

    private static void LinkSecretPassage(BoardLocation a, BoardLocation b)
    {
        a.SecretPassageToLocationId = b.Id;
        b.SecretPassageToLocationId = a.Id;
    }

    private sealed record RoomSpec(
        string Name,
        int X,
        int Y,
        int Width,
        int Height,
        IReadOnlyList<(int X, int Y)> Entrances);
}
