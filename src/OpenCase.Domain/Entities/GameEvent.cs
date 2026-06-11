namespace OpenCase.Domain.Entities;

public class GameEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
