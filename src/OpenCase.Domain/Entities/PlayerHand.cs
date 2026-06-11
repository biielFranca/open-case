namespace OpenCase.Domain.Entities;

public class PlayerHand
{
    public Guid PlayerId { get; set; }
    public List<Card> Cards { get; set; } = [];
}
