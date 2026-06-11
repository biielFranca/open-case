using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class Room
{
    public const int MinPlayers = 3;
    public const int MaxPlayers = 8;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public Guid HostPlayerId { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Lobby;
    public List<Player> Players { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
