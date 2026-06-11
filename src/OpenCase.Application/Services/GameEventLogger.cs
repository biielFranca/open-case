using System.Text.Json;
using OpenCase.Domain.Entities;

namespace OpenCase.Application.Services;

/// <summary>
/// Tipos de eventos técnicos registrados para debug e auditoria.
/// Não alimentam histórico público na UI.
/// </summary>
public static class GameEventTypes
{
    public const string RoomCreated = "RoomCreated";
    public const string PlayerJoined = "PlayerJoined";
    public const string PlayerLeft = "PlayerLeft";
    public const string GameStarted = "GameStarted";
    public const string SolutionCreated = "SolutionCreated";
    public const string CardsDealt = "CardsDealt";
    public const string DiceRolled = "DiceRolled";
    public const string PawnMoved = "PawnMoved";
    public const string SuggestionMade = "SuggestionMade";
    public const string RefutationResolved = "RefutationResolved";
    public const string FinalAccusationMade = "FinalAccusationMade";
    public const string HintGenerated = "HintGenerated";
    public const string GameFinished = "GameFinished";
}

public class GameEventLogger
{
    public GameEvent Log(Game game, string type, object? payload = null)
    {
        var entry = new GameEvent
        {
            GameId = game.Id,
            Type = type,
            Data = payload is null ? "{}" : JsonSerializer.Serialize(payload),
        };

        game.Events.Add(entry);
        return entry;
    }
}
