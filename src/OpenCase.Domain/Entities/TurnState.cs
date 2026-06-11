using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class TurnState
{
    public TurnPhase Phase { get; set; } = TurnPhase.RollDice;
    public Guid CurrentPlayerId { get; set; }
    public int TurnNumber { get; set; }
    public int? DiceValue { get; set; }
    public List<Guid> TurnOrder { get; set; } = [];

    // Jogadores que perderão o próximo turno (penalidade de acusação errada).
    public HashSet<Guid> PlayersToSkip { get; set; } = [];
}
