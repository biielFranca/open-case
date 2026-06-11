namespace OpenCase.Domain.Entities;

public class Suggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public Guid SuggestingPlayerId { get; set; }
    public Guid SuspectCardId { get; set; }
    public Guid LocationCardId { get; set; }
    public Guid WeaponCardId { get; set; }
    public bool WasRefuted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
