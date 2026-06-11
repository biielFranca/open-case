using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Cria palpites. O local nunca vem do frontend: é derivado da posição atual do jogador.
/// </summary>
public class SuggestionService(BoardService boardService)
{
    public Suggestion CreateSuggestion(Game game, Guid playerId, Guid suspectCardId, Guid weaponCardId)
    {
        if (!game.PawnPositions.TryGetValue(playerId, out var position))
        {
            throw new InvalidOperationException("Jogador não possui posição neste jogo.");
        }

        var location = boardService.GetLocationByCell(game.Board, position.X, position.Y)
            ?? throw new InvalidOperationException("Só é possível dar palpite dentro de um local.");

        var suspectCard = GetCardOfType(game, suspectCardId, CardType.Suspect);
        var weaponCard = GetCardOfType(game, weaponCardId, CardType.Weapon);
        var locationCard = game.AllCards.FirstOrDefault(c => c.Type == CardType.Location && c.Name == location.Name)
            ?? throw new InvalidOperationException($"Não existe carta para o local {location.Name}.");

        var suggestion = new Suggestion
        {
            GameId = game.Id,
            SuggestingPlayerId = playerId,
            SuspectCardId = suspectCard.Id,
            LocationCardId = locationCard.Id,
            WeaponCardId = weaponCard.Id,
        };

        MoveCitedPawn(game, suspectCard.Id, position);

        game.ActiveSuggestion = suggestion;
        game.TurnState.Phase = TurnPhase.Refutation;
        return suggestion;
    }

    private static Card GetCardOfType(Game game, Guid cardId, CardType expectedType)
    {
        var card = game.AllCards.FirstOrDefault(c => c.Id == cardId)
            ?? throw new InvalidOperationException("Carta não existe nesta partida.");

        return card.Type == expectedType
            ? card
            : throw new InvalidOperationException($"A carta {card.Name} não é do tipo {expectedType}.");
    }

    private static void MoveCitedPawn(Game game, Guid suspectCardId, BoardPosition accuserPosition)
    {
        var citedPlayer = game.PlayerSuspects
            .Where(ps => ps.Value == suspectCardId)
            .Select(ps => (Guid?)ps.Key)
            .FirstOrDefault();

        if (citedPlayer is not null)
        {
            game.PawnPositions[citedPlayer.Value] = accuserPosition;
        }
    }
}
