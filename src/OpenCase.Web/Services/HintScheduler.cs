using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenCase.Application.Mapping;
using OpenCase.Application.Services;
using OpenCase.Domain.Enums;
using OpenCase.Web.Hubs;

namespace OpenCase.Web.Services;

public sealed class HintScheduler(
    IHubContext<GameHub> hubContext,
    RoomService roomService,
    GameManager gameManager,
    HintService hintService,
    GameConnectionRegistry connectionRegistry,
    ILogger<HintScheduler> logger) : IAsyncDisposable
{
    private static readonly TimeSpan FirstHintMinDelay = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan FirstHintMaxDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan HintMinInterval = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan HintMaxInterval = TimeSpan.FromSeconds(75);

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _schedules = new();

    public void Start(Guid roomId)
    {
        var cts = new CancellationTokenSource();
        if (!_schedules.TryAdd(roomId, cts))
        {
            cts.Dispose();
            return;
        }

        _ = RunScheduleAsync(roomId, cts);
    }

    public void Stop(Guid roomId)
    {
        if (_schedules.TryGetValue(roomId, out var cts))
        {
            cts.Cancel();
        }
    }

    private async Task RunScheduleAsync(Guid roomId, CancellationTokenSource cts)
    {
        try
        {
            await DelayRandomAsync(FirstHintMinDelay, FirstHintMaxDelay, cts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                await DeliverHintAsync(roomId);
                await DelayRandomAsync(HintMinInterval, HintMaxInterval, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Timer encerrado pela sala ou pelo fim da partida.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Agendador de dicas parou para a sala {RoomId}.", roomId);
        }
        finally
        {
            _schedules.TryRemove(roomId, out _);
            cts.Dispose();
        }
    }

    private async Task DeliverHintAsync(Guid roomId)
    {
        var game = gameManager.GetGameByRoom(roomId);
        if (game.Status != GameStatus.InProgress)
        {
            Stop(roomId);
            return;
        }

        if (game.ActiveRefutation is { IsResolved: false })
        {
            return;
        }

        var eligiblePlayerIds = EligiblePlayerIds(roomId);
        if (eligiblePlayerIds.Count == 0)
        {
            return;
        }

        var hint = hintService.GenerateDistributedHint(game, eligiblePlayerIds);
        var payload = GameStateMapper.ToPublicHintDto(hint);

        if (hint.TargetPlayerId is Guid targetPlayerId
            && connectionRegistry.TryGetConnectionId(targetPlayerId, out var connectionId))
        {
            await hubContext.Clients.Client(connectionId).SendAsync("HintReceived", payload);
        }
    }

    private List<Guid> EligiblePlayerIds(Guid roomId)
    {
        var connected = connectionRegistry.ConnectedPlayerIdsForRoom(roomId).ToHashSet();
        if (connected.Count == 0)
        {
            return [];
        }

        var room = roomService.GetRoom(roomId);
        var humanPlayers = room.Players
            .Where(player => connected.Contains(player.Id))
            .Where(player => !player.Name.Contains("(bot)", StringComparison.OrdinalIgnoreCase))
            .Select(player => player.Id)
            .ToList();

        return humanPlayers.Count > 0 ? humanPlayers : [.. connected];
    }

    private static Task DelayRandomAsync(TimeSpan min, TimeSpan max, CancellationToken token) =>
        Task.Delay(RandomBetween(min, max), token);

    private static TimeSpan RandomBetween(TimeSpan min, TimeSpan max)
    {
        var rangeMilliseconds = Math.Max(0, (int)(max - min).TotalMilliseconds);
        return min + TimeSpan.FromMilliseconds(Random.Shared.Next(rangeMilliseconds + 1));
    }

    public ValueTask DisposeAsync()
    {
        foreach (var schedule in _schedules.Values)
        {
            schedule.Cancel();
        }

        return ValueTask.CompletedTask;
    }
}
