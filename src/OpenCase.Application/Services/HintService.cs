using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Gera pistas verdadeiras sem revelar diretamente a solucao. Uma pista real pode
/// apontar para uma carta da solucao por aparencia ou descartar uma carta inocente.
/// Rumores apenas criam clima e nunca mencionam cartas.
/// </summary>
public class HintService(Random random)
{
    private static readonly string[] NoiseTexts =
    [
        "Passos foram ouvidos no corredor durante a madrugada...",
        "Alguem apagou as luzes do salao mais cedo do que de costume.",
        "Um bilhete amassado foi encontrado, mas a tinta borrou na chuva.",
        "Os empregados juram ter ouvido uma discussao abafada.",
        "Uma janela ficou aberta a noite inteira. Coincidencia?",
        "O relogio do hall parou exatamente a meia-noite.",
    ];

    private static readonly string[] DeliveryModes = ["Text", "Audio"];

    public HintService() : this(Random.Shared) { }

    public Hint GenerateRandomHint(Game game) =>
        random.Next(3) switch
        {
            0 => GeneratePrivateHint(game),
            1 => GeneratePublicHint(game),
            _ => GenerateNoiseHint(game),
        };

    public Hint GenerateDistributedHint(Game game, IReadOnlyList<Guid> eligiblePlayerIds)
    {
        if (eligiblePlayerIds.Count == 0)
        {
            throw new InvalidOperationException("Nao ha jogadores disponiveis para receber a pista.");
        }

        var targetPlayerId = eligiblePlayerIds[random.Next(eligiblePlayerIds.Count)];
        return random.Next(3) == 0
            ? GenerateNoiseHint(game, targetPlayerId)
            : GeneratePrivateHint(game, targetPlayerId);
    }

    public Hint GeneratePrivateHint(Game game) =>
        GeneratePrivateHint(game, PickRandomTargetPlayer(game));

    public Hint GeneratePrivateHint(Game game, Guid targetPlayerId)
    {
        EnsurePlayerCanReceiveHint(game, targetPlayerId);

        return new()
        {
            GameId = game.Id,
            Type = HintType.Private,
            TargetPlayerId = targetPlayerId,
            Delivery = PickDelivery(),
            Text = BuildTrueHintText(game),
        };
    }

    public Hint GeneratePublicHint(Game game) => new()
    {
        GameId = game.Id,
        Type = HintType.Public,
        Delivery = PickDelivery(),
        Text = BuildTrueHintText(game),
    };

    public Hint GenerateNoiseHint(Game game) =>
        GenerateNoiseHint(game, null);

    public Hint GenerateNoiseHint(Game game, Guid? targetPlayerId) => new()
    {
        GameId = game.Id,
        Type = HintType.Noise,
        TargetPlayerId = targetPlayerId,
        Delivery = PickDelivery(),
        Text = NoiseTexts[random.Next(NoiseTexts.Length)],
    };

    public Guid PickRandomTargetPlayer(Game game)
    {
        var players = game.TurnState.TurnOrder;
        if (players.Count == 0)
        {
            throw new InvalidOperationException("A partida nao possui jogadores.");
        }

        return players[random.Next(players.Count)];
    }

    private string PickDelivery() =>
        DeliveryModes[random.Next(DeliveryModes.Length)];

    private string BuildTrueHintText(Game game)
    {
        var solution = game.Solution
            ?? throw new InvalidOperationException("A partida nao possui solucao definida.");

        var solutionIds = new HashSet<Guid>
        {
            solution.SuspectCardId,
            solution.LocationCardId,
            solution.WeaponCardId,
        };

        var solutionCards = game.AllCards.Where(c => solutionIds.Contains(c.Id)).ToList();
        if (solutionCards.Count == 0)
        {
            throw new InvalidOperationException("A solucao nao corresponde ao baralho da partida.");
        }

        var innocents = game.AllCards.Where(c => !solutionIds.Contains(c.Id)).ToList();
        var shouldPointToSolution = innocents.Count == 0 || random.Next(2) == 0;
        if (shouldPointToSolution)
        {
            var solutionCard = solutionCards[random.Next(solutionCards.Count)];
            return HintCatalog.PickSuspicion(solutionCard, random)
                ?? BuildFallbackSuspicionText(solutionCard);
        }

        var innocentCard = innocents[random.Next(innocents.Count)];
        return HintCatalog.PickInnocence(innocentCard, random)
            ?? BuildFallbackInnocenceText(innocentCard);
    }

    private static void EnsurePlayerCanReceiveHint(Game game, Guid playerId)
    {
        if (game.Hands.Any(hand => hand.PlayerId == playerId)
            || game.TurnState.TurnOrder.Contains(playerId))
        {
            return;
        }

        throw new InvalidOperationException("Jogador nao encontrado nesta partida.");
    }

    private static string BuildFallbackSuspicionText(Card card) => card.Type switch
    {
        CardType.Suspect => "A silhueta combinava com detalhes marcantes de um dos suspeitos.",
        CardType.Location => "A cena tinha caracteristicas fortes de um dos ambientes da mansao.",
        CardType.Weapon => "A marca deixada aponta para um objeto de formato muito especifico.",
        _ => "A pista combina com uma das cartas centrais do caso.",
    };

    private static string BuildFallbackInnocenceText(Card card) => card.Type switch
    {
        CardType.Suspect => "Uma testemunha descreveu alguem incompativel com um dos suspeitos anotados.",
        CardType.Location => "Os vestigios nao combinam com um dos ambientes investigados.",
        CardType.Weapon => "A marca encontrada descarta um dos objetos do arquivo.",
        _ => "A pista elimina uma possibilidade do caso.",
    };
}
