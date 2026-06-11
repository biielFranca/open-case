using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class TurnServiceTests
{
    private static readonly Guid PlayerA = Guid.NewGuid();
    private static readonly Guid PlayerB = Guid.NewGuid();
    private static readonly Guid PlayerC = Guid.NewGuid();

    private static TurnService CreateService(int seed = 42) => new(new Random(seed));

    private static TurnState CreateTurnState() => new()
    {
        TurnOrder = [PlayerA, PlayerB, PlayerC],
        CurrentPlayerId = PlayerA,
        Phase = TurnPhase.EndOfTurn,
    };

    [Fact]
    public void RollInitialDice_GivesOneRollPerPlayerBetween1And6()
    {
        var rolls = CreateService().RollInitialDice([PlayerA, PlayerB, PlayerC]);

        Assert.Equal(3, rolls.Count);
        Assert.All(rolls.Values, v => Assert.InRange(v, 1, 6));
    }

    [Fact]
    public void ResolveInitialOrder_OrdersFromHighestToLowest()
    {
        var rolls = new Dictionary<Guid, int> { [PlayerA] = 2, [PlayerB] = 6, [PlayerC] = 4 };

        var result = CreateService().ResolveInitialOrder(rolls);

        Assert.False(result.HasTieAtTop);
        Assert.Equal([PlayerB, PlayerC, PlayerA], result.TurnOrder);
    }

    [Fact]
    public void ResolveInitialOrder_DetectsTieAtTop()
    {
        var rolls = new Dictionary<Guid, int> { [PlayerA] = 6, [PlayerB] = 6, [PlayerC] = 3 };

        var result = CreateService().ResolveInitialOrder(rolls);

        Assert.True(result.HasTieAtTop);
        Assert.Equal(2, result.TiedPlayerIds.Count);
        Assert.Contains(PlayerA, result.TiedPlayerIds);
        Assert.Contains(PlayerB, result.TiedPlayerIds);
        Assert.Empty(result.TurnOrder);
    }

    [Fact]
    public void ResolveInitialOrder_TieBelowTopIsNotATie()
    {
        var rolls = new Dictionary<Guid, int> { [PlayerA] = 6, [PlayerB] = 3, [PlayerC] = 3 };

        var result = CreateService().ResolveInitialOrder(rolls);

        Assert.False(result.HasTieAtTop);
        Assert.Equal(PlayerA, result.TurnOrder[0]);
    }

    [Fact]
    public void AdvanceTurn_MovesToNextPlayerInOrderAndWrapsAround()
    {
        var service = CreateService();
        var turn = CreateTurnState();

        service.AdvanceTurn(turn);
        Assert.Equal(PlayerB, turn.CurrentPlayerId);

        service.AdvanceTurn(turn);
        Assert.Equal(PlayerC, turn.CurrentPlayerId);

        service.AdvanceTurn(turn);
        Assert.Equal(PlayerA, turn.CurrentPlayerId);
        Assert.Equal(TurnPhase.RollDice, turn.Phase);
    }

    [Fact]
    public void AdvanceTurn_SkipsPenalizedPlayer()
    {
        var service = CreateService();
        var turn = CreateTurnState();
        service.ApplySkipNextTurn(turn, PlayerB);

        service.AdvanceTurn(turn);

        Assert.Equal(PlayerC, turn.CurrentPlayerId);
    }

    [Fact]
    public void AdvanceTurn_PenaltyIsRemovedAfterSkippedTurn()
    {
        var service = CreateService();
        var turn = CreateTurnState();
        service.ApplySkipNextTurn(turn, PlayerB);

        service.AdvanceTurn(turn); // B é pulado, vai para C
        service.AdvanceTurn(turn); // volta para A
        service.AdvanceTurn(turn); // B joga normalmente

        Assert.Equal(PlayerB, turn.CurrentPlayerId);
        Assert.Empty(turn.PlayersToSkip);
    }

    [Fact]
    public void RollTurnDice_IsAlwaysBetween1And12()
    {
        var service = CreateService();

        for (var i = 0; i < 500; i++)
        {
            Assert.InRange(service.RollTurnDice(), 1, 12);
        }
    }

    [Fact]
    public void RollTurnDice_CanProduceFullRange()
    {
        var service = CreateService();
        var seen = new HashSet<int>();

        for (var i = 0; i < 5000; i++)
        {
            seen.Add(service.RollTurnDice());
        }

        Assert.Contains(1, seen);
        Assert.Contains(12, seen);
    }
}
