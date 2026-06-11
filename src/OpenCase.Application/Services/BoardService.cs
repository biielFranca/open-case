using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Tabuleiro 20x20 do MVP: 12 locais em grade 4x3, corredores entre eles,
/// paredes no perímetro de cada local e entradas voltadas para os corredores.
/// </summary>
public class BoardService
{
    private static readonly string[] LocationNames =
    [
        "Biblioteca", "Salão de Festas", "Cozinha", "Escritório",
        "Jardim de Inverno", "Sala de Jantar", "Salão de Jogos", "Hall",
        "Estufa", "Porão", "Observatório", "Quarto de Hóspedes",
    ];

    // Retângulos dos 12 locais: 4 colunas x 3 linhas, salas de 4x5 células.
    private static readonly int[] ColumnStarts = [0, 5, 11, 16];
    private static readonly int[] RowStarts = [0, 7, 14];
    private const int RoomWidth = 4;
    private const int RoomHeight = 5;

    public Board CreateDefaultBoard()
    {
        var board = new Board();
        var rects = new List<(BoardLocation Location, int X, int Y)>();

        var nameIndex = 0;
        foreach (var rowY in RowStarts)
        {
            foreach (var colX in ColumnStarts)
            {
                var location = new BoardLocation { Name = LocationNames[nameIndex++] };
                board.Locations.Add(location);
                rects.Add((location, colX, rowY));
            }
        }

        // Passagens secretas ligando os cantos opostos (como no jogo clássico).
        LinkSecretPassage(rects[0].Location, rects[11].Location);
        LinkSecretPassage(rects[3].Location, rects[8].Location);

        for (var x = 0; x < Board.Width; x++)
        {
            for (var y = 0; y < Board.Height; y++)
            {
                board.Cells.Add(CreateCell(rects, x, y));
            }
        }

        foreach (var (location, colX, rowY) in rects)
        {
            foreach (var entrance in EntrancesFor(colX, rowY))
            {
                location.EntranceCells.Add(entrance);
                var cell = GetCell(board, entrance.X, entrance.Y)!;
                cell.Type = CellType.Entrance;
                cell.LocationId = location.Id;
            }
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

    private static void LinkSecretPassage(BoardLocation a, BoardLocation b)
    {
        a.SecretPassageToLocationId = b.Id;
        b.SecretPassageToLocationId = a.Id;
    }

    private static BoardCell CreateCell(
        List<(BoardLocation Location, int X, int Y)> rects, int x, int y)
    {
        foreach (var (location, rx, ry) in rects)
        {
            if (x < rx || x >= rx + RoomWidth || y < ry || y >= ry + RoomHeight)
            {
                continue;
            }

            var isBorder = x == rx || x == rx + RoomWidth - 1 || y == ry || y == ry + RoomHeight - 1;
            var type = isBorder
                ? CellType.Wall
                : location.SecretPassageToLocationId is not null && x == rx + 1 && y == ry + 1
                    ? CellType.SecretPassage
                    : CellType.Location;

            return new BoardCell { X = x, Y = y, Type = type, LocationId = location.Id };
        }

        return new BoardCell { X = x, Y = y, Type = CellType.Path };
    }

    private static IEnumerable<BoardPosition> EntrancesFor(int colX, int rowY)
    {
        if (rowY == RowStarts[0])
        {
            // Salas do topo: 1 entrada na borda inferior, voltada ao corredor y=5.
            yield return new BoardPosition(colX + 1, rowY + RoomHeight - 1);
        }
        else if (rowY == RowStarts[1])
        {
            // Salas do meio: 2 entradas (borda superior e inferior).
            yield return new BoardPosition(colX + 1, rowY);
            yield return new BoardPosition(colX + 2, rowY + RoomHeight - 1);
        }
        else
        {
            // Salas de baixo: 1 entrada na borda superior, voltada ao corredor y=13.
            yield return new BoardPosition(colX + 1, rowY);
        }
    }
}
