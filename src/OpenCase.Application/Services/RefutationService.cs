using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Resultado privado da refutação: a carta mostrada só pode ser enviada ao acusador.
/// </summary>
public record PrivateCardShownResult(Guid AccuserPlayerId, Guid ShownByPlayerId, Guid CardId);

/// <summary>
/// Conduz a refutação: começa no jogador anterior ao acusador e segue em ordem reversa.
/// </summary>
public class RefutationService
{
    public RefutationState CreateRefutationState(Game game, Suggestion suggestion)
    {
        var order = game.TurnState.TurnOrder;
        var accuserIndex = order.IndexOf(suggestion.SuggestingPlayerId);
        if (accuserIndex < 0)
        {
            throw new InvalidOperationException("Acusador não está na ordem de turnos.");
        }

        var refutationOrder = new List<Guid>();
        for (var step = 1; step < order.Count; step++)
        {
            refutationOrder.Add(order[(accuserIndex - step + order.Count * step) % order.Count]);
        }

        var state = new RefutationState
        {
            SuggestionId = suggestion.Id,
            RefutationOrder = refutationOrder,
        };

        game.ActiveRefutation = state;
        return state;
    }

    public Guid? GetCurrentRefutingPlayer(RefutationState state) =>
        state.IsResolved || state.CurrentIndex >= state.RefutationOrder.Count
            ? null
            : state.RefutationOrder[state.CurrentIndex];

    public List<Card> GetCardsAvailableToShow(Game game, RefutationState state, Guid playerId)
    {
        var suggestion = GetSuggestion(game, state);
        var suggestedIds = new HashSet<Guid>
        {
            suggestion.SuspectCardId,
            suggestion.LocationCardId,
            suggestion.WeaponCardId,
        };

        var hand = game.Hands.FirstOrDefault(h => h.PlayerId == playerId)
            ?? throw new InvalidOperationException("Jogador não possui mão nesta partida.");

        return [.. hand.Cards.Where(c => suggestedIds.Contains(c.Id))];
    }

    public void PassRefutation(Game game, RefutationState state, Guid playerId)
    {
        EnsureCurrentPlayer(state, playerId);

        if (GetCardsAvailableToShow(game, state, playerId).Count > 0)
        {
            throw new InvalidOperationException(
                "Jogador possui carta compatível e é obrigado a mostrá-la.");
        }

        state.CurrentIndex++;
        if (state.CurrentIndex >= state.RefutationOrder.Count)
        {
            // Ninguém refutou: o palpite fica disponível para a acusação final.
            var suggestion = GetSuggestion(game, state);
            state.AllPlayersPassed = true;
            state.IsResolved = true;
            suggestion.WasRefuted = false;
            game.LastUnrefutedSuggestion = suggestion;
            game.TurnState.Phase = TurnPhase.FinalAccusation;
        }
    }

    public PrivateCardShownResult ShowCard(Game game, RefutationState state, Guid playerId, Guid cardId)
    {
        EnsureCurrentPlayer(state, playerId);

        if (GetCardsAvailableToShow(game, state, playerId).All(c => c.Id != cardId))
        {
            throw new InvalidOperationException("Esta carta não pode ser usada para refutar o palpite.");
        }

        var suggestion = GetSuggestion(game, state);
        state.ShownCardId = cardId;
        state.ShownByPlayerId = playerId;
        state.IsResolved = true;
        suggestion.WasRefuted = true;
        game.TurnState.Phase = TurnPhase.EndOfTurn;

        return new PrivateCardShownResult(suggestion.SuggestingPlayerId, playerId, cardId);
    }

    private static void EnsureCurrentPlayer(RefutationState state, Guid playerId)
    {
        if (state.IsResolved)
        {
            throw new InvalidOperationException("A refutação já foi resolvida.");
        }

        if (state.CurrentIndex >= state.RefutationOrder.Count
            || state.RefutationOrder[state.CurrentIndex] != playerId)
        {
            throw new InvalidOperationException("Não é a vez deste jogador na refutação.");
        }
    }

    private static Suggestion GetSuggestion(Game game, RefutationState state) =>
        game.ActiveSuggestion?.Id == state.SuggestionId
            ? game.ActiveSuggestion
            : throw new InvalidOperationException("O palpite desta refutação não está mais ativo.");
}
