using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class Hint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public HintType Type { get; set; }
    public Guid? TargetPlayerId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
