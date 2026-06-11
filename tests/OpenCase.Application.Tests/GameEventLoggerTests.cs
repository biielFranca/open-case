using OpenCase.Application.Services;
using OpenCase.Domain.Entities;

namespace OpenCase.Application.Tests;

public class GameEventLoggerTests
{
    private readonly GameEventLogger _logger = new();
    private readonly Game _game = new();

    [Fact]
    public void Log_AppendsEventWithTypeDataAndTimestamp()
    {
        var before = DateTimeOffset.UtcNow;

        _logger.Log(_game, GameEventTypes.DiceRolled, new { PlayerId = Guid.NewGuid(), Value = 7 });

        var entry = Assert.Single(_game.Events);
        Assert.Equal(GameEventTypes.DiceRolled, entry.Type);
        Assert.Equal(_game.Id, entry.GameId);
        Assert.Contains("\"Value\":7", entry.Data);
        Assert.InRange(entry.CreatedAt, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Log_KeepsChronologicalOrder()
    {
        _logger.Log(_game, GameEventTypes.RoomCreated);
        _logger.Log(_game, GameEventTypes.PlayerJoined);
        _logger.Log(_game, GameEventTypes.GameStarted);

        Assert.Equal(
            [GameEventTypes.RoomCreated, GameEventTypes.PlayerJoined, GameEventTypes.GameStarted],
            _game.Events.Select(e => e.Type));
    }

    [Fact]
    public void GameEventTypes_CoversAllRequiredEvents()
    {
        string[] required =
        [
            GameEventTypes.RoomCreated,
            GameEventTypes.PlayerJoined,
            GameEventTypes.PlayerLeft,
            GameEventTypes.GameStarted,
            GameEventTypes.SolutionCreated,
            GameEventTypes.CardsDealt,
            GameEventTypes.DiceRolled,
            GameEventTypes.PawnMoved,
            GameEventTypes.SuggestionMade,
            GameEventTypes.RefutationResolved,
            GameEventTypes.FinalAccusationMade,
            GameEventTypes.HintGenerated,
            GameEventTypes.GameFinished,
        ];

        Assert.Equal(13, required.Distinct().Count());
        Assert.All(required, t => Assert.False(string.IsNullOrWhiteSpace(t)));
    }

    [Fact]
    public void Log_WithoutPayloadStoresEmptyJsonObject()
    {
        _logger.Log(_game, GameEventTypes.GameFinished);

        Assert.Equal("{}", _game.Events[0].Data);
    }
}
