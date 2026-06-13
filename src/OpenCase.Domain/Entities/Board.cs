namespace OpenCase.Domain.Entities;

public class Board
{
    public const int Width = 30;
    public const int Height = 30;

    public List<BoardCell> Cells { get; set; } = [];
    public List<BoardLocation> Locations { get; set; } = [];
}
