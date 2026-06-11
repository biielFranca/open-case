using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class BoardCell
{
    public int X { get; set; }
    public int Y { get; set; }
    public CellType Type { get; set; } = CellType.Path;
    public Guid? LocationId { get; set; }
}
