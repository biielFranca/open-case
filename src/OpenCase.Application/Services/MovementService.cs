using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

public record MoveResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    BoardPosition NewPosition,
    bool EnteredLocation,
    Guid? LocationId)
{
    public static MoveResult Invalid(BoardPosition current, params string[] errors) =>
        new(false, errors, current, false, null);
}

/// <summary>
/// Valida e aplica o caminho escolhido pelo jogador (lista de coordenadas).
/// </summary>
public class MovementService(BoardService boardService)
{
    public MoveResult ValidateMove(Game game, Guid playerId, IReadOnlyList<BoardPosition> path, int diceValue)
    {
        if (!game.PawnPositions.TryGetValue(playerId, out var current))
        {
            return MoveResult.Invalid(default, "Jogador não possui posição neste jogo.");
        }

        if (path.Count == 0)
        {
            return MoveResult.Invalid(current, "O caminho não pode ser vazio.");
        }

        if (path.Count > diceValue)
        {
            return MoveResult.Invalid(current, "O caminho é maior que o valor do dado.");
        }

        var board = game.Board;
        var previous = current;

        for (var i = 0; i < path.Count; i++)
        {
            var step = path[i];
            var isLast = i == path.Count - 1;

            var dx = Math.Abs(step.X - previous.X);
            var dy = Math.Abs(step.Y - previous.Y);
            if (dx + dy != 1)
            {
                return MoveResult.Invalid(current,
                    $"Passo ({step.X},{step.Y}) não é adjacente na horizontal/vertical.");
            }

            var cell = boardService.GetCell(board, step.X, step.Y);
            if (cell is null)
            {
                return MoveResult.Invalid(current, $"Passo ({step.X},{step.Y}) está fora do tabuleiro.");
            }

            if (cell.Type == CellType.Entrance)
            {
                if (!isLast)
                {
                    return MoveResult.Invalid(current,
                        "Entrar em um local encerra o movimento; a entrada deve ser o último passo.");
                }

                // Entrar no local é válido mesmo sem gastar todo o dado.
                return new MoveResult(true, [], step, true, cell.LocationId);
            }

            if (cell.Type != CellType.Path)
            {
                return MoveResult.Invalid(current,
                    $"Passo ({step.X},{step.Y}) não é uma célula caminhável.");
            }

            previous = step;
        }

        if (path.Count < diceValue)
        {
            return MoveResult.Invalid(current,
                "É preciso usar todo o valor do dado, exceto ao entrar em um local.");
        }

        return new MoveResult(true, [], previous, false, null);
    }

    public MoveResult ApplyMove(Game game, Guid playerId, IReadOnlyList<BoardPosition> path, int diceValue)
    {
        var result = ValidateMove(game, playerId, path, diceValue);
        if (result.IsValid)
        {
            game.PawnPositions[playerId] = result.NewPosition;
        }

        return result;
    }
}
