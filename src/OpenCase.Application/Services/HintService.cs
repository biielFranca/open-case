using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Gera dicas. Dicas reais falam apenas de cartas inocentes (fora da solução);
/// ruído usa frases vagas que não afirmam nada sobre cartas — nunca mente diretamente.
/// </summary>
public class HintService(Random random)
{
    private static readonly string[] NoiseTexts =
    [
        "Passos foram ouvidos no corredor durante a madrugada...",
        "Alguém apagou as luzes do salão mais cedo do que de costume.",
        "Um bilhete amassado foi encontrado, mas a tinta borrou na chuva.",
        "Os empregados juram ter ouvido uma discussão abafada.",
        "Uma janela ficou aberta a noite inteira. Coincidência?",
        "O relógio do hall parou exatamente à meia-noite.",
    ];

    private static readonly string[] InnocentTemplates =
    [
        "Fontes confiáveis garantem: {0} não tem relação com o caso.",
        "A investigação descartou {0}.",
        "Pode riscar da lista: {0} está fora do caso.",
    ];

    public HintService() : this(Random.Shared) { }

    public Hint GenerateRandomHint(Game game) =>
        random.Next(3) switch
        {
            0 => GeneratePrivateHint(game),
            1 => GeneratePublicHint(game),
            _ => GenerateNoiseHint(game),
        };

    public Hint GeneratePrivateHint(Game game) => new()
    {
        GameId = game.Id,
        Type = HintType.Private,
        TargetPlayerId = PickRandomTargetPlayer(game),
        Text = BuildInnocentText(game),
    };

    public Hint GeneratePublicHint(Game game) => new()
    {
        GameId = game.Id,
        Type = HintType.Public,
        Text = BuildInnocentText(game),
    };

    public Hint GenerateNoiseHint(Game game) => new()
    {
        GameId = game.Id,
        Type = HintType.Noise,
        Text = NoiseTexts[random.Next(NoiseTexts.Length)],
    };

    public Guid PickRandomTargetPlayer(Game game)
    {
        var players = game.TurnState.TurnOrder;
        if (players.Count == 0)
        {
            throw new InvalidOperationException("A partida não possui jogadores.");
        }

        return players[random.Next(players.Count)];
    }

    private string BuildInnocentText(Game game)
    {
        var solution = game.Solution
            ?? throw new InvalidOperationException("A partida não possui solução definida.");

        var solutionIds = new HashSet<Guid>
        {
            solution.SuspectCardId,
            solution.LocationCardId,
            solution.WeaponCardId,
        };

        var innocents = game.AllCards.Where(c => !solutionIds.Contains(c.Id)).ToList();
        if (innocents.Count == 0)
        {
            throw new InvalidOperationException("Não há cartas inocentes para gerar dicas.");
        }

        var card = innocents[random.Next(innocents.Count)];
        var template = InnocentTemplates[random.Next(InnocentTemplates.Length)];
        return string.Format(template, card.Name);
    }
}
