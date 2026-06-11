using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

public record InitialOrderResult(bool HasTieAtTop, IReadOnlyList<Guid> TiedPlayerIds, List<Guid> TurnOrder);

/// <summary>
/// Controla a ordem inicial, o avanço de turnos e os dados (inicial 1-6, turno 1-12).
/// </summary>
public class TurnService(Random random)
{
    public TurnService() : this(Random.Shared) { }

    public Dictionary<Guid, int> RollInitialDice(IReadOnlyList<Guid> playerIds) =>
        playerIds.ToDictionary(id => id, _ => random.Next(1, 7));

    public InitialOrderResult ResolveInitialOrder(IReadOnlyDictionary<Guid, int> rolls)
    {
        var highest = rolls.Values.Max();
        var tied = rolls.Where(r => r.Value == highest).Select(r => r.Key).ToList();

        if (tied.Count > 1)
        {
            // Empate no maior: quem empatou precisa rolar de novo.
            return new InitialOrderResult(true, tied, []);
        }

        var order = rolls.OrderByDescending(r => r.Value).Select(r => r.Key).ToList();
        return new InitialOrderResult(false, [], order);
    }

    public void AdvanceTurn(TurnState turn)
    {
        if (turn.TurnOrder.Count == 0)
        {
            throw new InvalidOperationException("A ordem de turnos ainda não foi definida.");
        }

        var next = NextInOrder(turn, turn.CurrentPlayerId);
        while (turn.PlayersToSkip.Remove(next))
        {
            next = NextInOrder(turn, next);
        }

        turn.CurrentPlayerId = next;
        turn.TurnNumber++;
        turn.Phase = TurnPhase.RollDice;
        turn.DiceValue = null;
    }

    public void ApplySkipNextTurn(TurnState turn, Guid playerId) =>
        turn.PlayersToSkip.Add(playerId);

    public int RollTurnDice() => random.Next(1, 13);

    private static Guid NextInOrder(TurnState turn, Guid current)
    {
        var index = turn.TurnOrder.IndexOf(current);
        return turn.TurnOrder[(index + 1) % turn.TurnOrder.Count];
    }
}
