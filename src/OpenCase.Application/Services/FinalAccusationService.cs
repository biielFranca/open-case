using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// A solução só é revelada quando a acusação está correta; em erro ela permanece secreta.
/// </summary>
public record FinalAccusationResult(bool IsCorrect, FinalAccusation Accusation, GameSolution? RevealedSolution);

/// <summary>
/// Acusação final: nunca recebe combinação do frontend — usa o último palpite não refutado.
/// </summary>
public class FinalAccusationService
{
    public FinalAccusationResult MakeFinalAccusation(Game game, Guid playerId)
    {
        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidOperationException("A partida não está em andamento.");
        }

        var suggestion = game.LastUnrefutedSuggestion
            ?? throw new InvalidOperationException(
                "A acusação final só é liberada após um palpite que ninguém refutou.");

        if (suggestion.SuggestingPlayerId != playerId)
        {
            throw new InvalidOperationException("Apenas o autor do palpite não refutado pode acusar.");
        }

        var solution = game.Solution
            ?? throw new InvalidOperationException("A partida não possui solução definida.");

        var accusation = new FinalAccusation
        {
            GameId = game.Id,
            AccusingPlayerId = playerId,
            SuspectCardId = suggestion.SuspectCardId,
            LocationCardId = suggestion.LocationCardId,
            WeaponCardId = suggestion.WeaponCardId,
        };

        accusation.IsCorrect =
            accusation.SuspectCardId == solution.SuspectCardId
            && accusation.LocationCardId == solution.LocationCardId
            && accusation.WeaponCardId == solution.WeaponCardId;

        // O palpite é consumido: errar não dá segunda chance com o mesmo palpite.
        game.LastUnrefutedSuggestion = null;

        if (accusation.IsCorrect)
        {
            game.Status = GameStatus.Finished;
            game.WinnerPlayerId = playerId;
            game.FinishedAt = DateTimeOffset.UtcNow;
            return new FinalAccusationResult(true, accusation, solution);
        }

        game.TurnState.PlayersToSkip.Add(playerId);
        game.TurnState.Phase = TurnPhase.EndOfTurn;
        return new FinalAccusationResult(false, accusation, null);
    }
}
