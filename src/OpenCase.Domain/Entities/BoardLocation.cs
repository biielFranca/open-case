namespace OpenCase.Domain.Entities;

public class BoardLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<BoardPosition> EntranceCells { get; set; } = [];
    public Guid? SecretPassageToLocationId { get; set; }
}
