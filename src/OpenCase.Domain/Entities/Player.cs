using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? PawnId { get; set; }
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public PlayerConnectionStatus ConnectionStatus { get; set; } = PlayerConnectionStatus.Connected;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
