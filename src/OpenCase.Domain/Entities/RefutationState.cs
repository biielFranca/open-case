namespace OpenCase.Domain.Entities;

public class RefutationState
{
    public Guid SuggestionId { get; set; }
    public List<Guid> RefutationOrder { get; set; } = [];
    public int CurrentIndex { get; set; }
    public bool AllPlayersPassed { get; set; }
    public bool IsResolved { get; set; }
    public Guid? ShownCardId { get; set; }
    public Guid? ShownByPlayerId { get; set; }
}
