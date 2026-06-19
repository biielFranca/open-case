using System.Collections.Concurrent;

namespace OpenCase.Web.Services;

public sealed class GameConnectionRegistry
{
    public sealed record ConnectionInfo(Guid RoomId, Guid PlayerId);

    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<Guid, string> _connectionsByPlayer = new();

    public void Register(string connectionId, Guid roomId, Guid playerId)
    {
        _connections[connectionId] = new ConnectionInfo(roomId, playerId);
        _connectionsByPlayer[playerId] = connectionId;
    }

    public bool TryGetConnection(string connectionId, out ConnectionInfo info) =>
        _connections.TryGetValue(connectionId, out info!);

    public bool TryGetConnectionId(Guid playerId, out string connectionId) =>
        _connectionsByPlayer.TryGetValue(playerId, out connectionId!);

    public bool Remove(string connectionId, out ConnectionInfo info)
    {
        if (!_connections.TryRemove(connectionId, out info!))
        {
            return false;
        }

        _connectionsByPlayer.TryRemove(info.PlayerId, out _);
        return true;
    }

    public List<Guid> ConnectedPlayerIdsForRoom(Guid roomId) =>
        [.. _connections.Values
            .Where(connection => connection.RoomId == roomId)
            .Select(connection => connection.PlayerId)
            .Distinct()];
}
