namespace OpenCase.Domain.Entities;

public class Board
{
    public const int Width = 20;
    public const int Height = 20;

    public List<BoardCell> Cells { get; set; } = [];
    public List<BoardLocation> Locations { get; set; } = [];
}
