using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CardType Type { get; set; }
    public string Name { get; set; } = string.Empty;
}
