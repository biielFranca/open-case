namespace OpenCase.Domain.Entities;

public class Pawn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
