using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Passagens secretas: só dentro de um local com passagem, e usar conta como a ação do turno.
/// </summary>
public class SecretPassageService(BoardService boardService)
{
    public bool CanUseSecretPassage(Game game, Guid playerId)
    {
        var location = GetCurrentLocation(game, playerId);
        return location?.SecretPassageToLocationId is not null;
    }

    public BoardLocation UseSecretPassage(Game game, Guid playerId)
    {
        var location = GetCurrentLocation(game, playerId)
            ?? throw new InvalidOperationException("É preciso estar dentro de um local para usar a passagem secreta.");

        var target = boardService.GetSecretPassageTarget(game.Board, location.Id)
            ?? throw new InvalidOperationException("Este local não possui passagem secreta.");

        var entrance = target.EntranceCells[0];
        game.PawnPositions[playerId] = new BoardPosition(entrance.X, entrance.Y);

        // Usar a passagem é a ação do turno; em seguida o jogador pode dar palpite no destino.
        game.TurnState.Phase = TurnPhase.Suggestion;
        return target;
    }

    private BoardLocation? GetCurrentLocation(Game game, Guid playerId) =>
        game.PawnPositions.TryGetValue(playerId, out var position)
            ? boardService.GetLocationByCell(game.Board, position.X, position.Y)
            : null;
}
