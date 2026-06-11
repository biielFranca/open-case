using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class RoomServiceTests
{
    private static RoomService CreateService() => new(new Random(42));

    private static (RoomService Service, Room Room) CreateRoomWithPlayers(int totalPlayers, bool allReadyWithPawns = false)
    {
        var service = CreateService();
        var room = service.CreateRoom("Host");
        for (var i = 1; i < totalPlayers; i++)
        {
            service.JoinRoom(room.Code, $"Jogador {i}");
        }

        if (allReadyWithPawns)
        {
            var pawnId = 0;
            foreach (var player in room.Players.ToList())
            {
                service.ChoosePawn(room.Id, player.Id, RoomService.AvailablePawns[pawnId++].Id);
                service.SetReady(room.Id, player.Id, true);
            }
        }

        return (service, room);
    }

    [Fact]
    public void CreateRoom_FirstPlayerIsHostAndRoomHasUniqueCode()
    {
        var service = CreateService();
        var roomA = service.CreateRoom("Ana");
        var roomB = service.CreateRoom("Bia");

        Assert.NotEqual(roomA.Code, roomB.Code);
        Assert.NotEmpty(roomA.Code);
        var host = Assert.Single(roomA.Players);
        Assert.True(host.IsHost);
        Assert.Equal(host.Id, roomA.HostPlayerId);
        Assert.Equal(RoomStatus.Lobby, roomA.Status);
    }

    [Fact]
    public void JoinRoom_AddsPlayerByCode()
    {
        var service = CreateService();
        var room = service.CreateRoom("Host");

        var player = service.JoinRoom(room.Code, "Convidado");

        Assert.Equal(2, room.Players.Count);
        Assert.False(player.IsHost);
        Assert.Contains(room.Players, p => p.Id == player.Id);
    }

    [Fact]
    public void JoinRoom_UnknownCodeFails()
    {
        Assert.Throws<InvalidOperationException>(() => CreateService().JoinRoom("XXXXXX", "Alguém"));
    }

    [Fact]
    public void JoinRoom_RejectsNinthPlayer()
    {
        var (service, room) = CreateRoomWithPlayers(8);

        Assert.Throws<InvalidOperationException>(() => service.JoinRoom(room.Code, "Nono"));
    }

    [Fact]
    public void LeaveRoom_RemovesPlayerAndTransfersHost()
    {
        var (service, room) = CreateRoomWithPlayers(3);
        var originalHostId = room.HostPlayerId;

        service.LeaveRoom(room.Id, originalHostId);

        Assert.Equal(2, room.Players.Count);
        Assert.NotEqual(originalHostId, room.HostPlayerId);
        Assert.True(room.Players.Single(p => p.Id == room.HostPlayerId).IsHost);
    }

    [Fact]
    public void ChoosePawn_TakenPawnCannotBeChosenByAnotherPlayer()
    {
        var (service, room) = CreateRoomWithPlayers(2);
        var pawn = RoomService.AvailablePawns[0];
        var (first, second) = (room.Players[0], room.Players[1]);

        service.ChoosePawn(room.Id, first.Id, pawn.Id);

        Assert.Throws<InvalidOperationException>(() => service.ChoosePawn(room.Id, second.Id, pawn.Id));
        Assert.Equal(pawn.Id, first.PawnId);
    }

    [Fact]
    public void SetReady_MarksPlayer()
    {
        var (service, room) = CreateRoomWithPlayers(3);
        var player = room.Players[1];

        service.SetReady(room.Id, player.Id, true);

        Assert.True(player.IsReady);
    }

    [Fact]
    public void CanStartGame_RequiresMinimumThreePlayers()
    {
        var (service, room) = CreateRoomWithPlayers(2, allReadyWithPawns: true);

        Assert.False(service.CanStartGame(room.Id));
    }

    [Fact]
    public void CanStartGame_RequiresAllPawnsChosenAndAllReady()
    {
        var (service, room) = CreateRoomWithPlayers(3);

        Assert.False(service.CanStartGame(room.Id));

        var pawnId = 0;
        foreach (var player in room.Players)
        {
            service.ChoosePawn(room.Id, player.Id, RoomService.AvailablePawns[pawnId++].Id);
        }

        Assert.False(service.CanStartGame(room.Id));

        foreach (var player in room.Players)
        {
            service.SetReady(room.Id, player.Id, true);
        }

        Assert.True(service.CanStartGame(room.Id));
    }

    [Fact]
    public void StartGame_OnlyHostCanStart()
    {
        var (service, room) = CreateRoomWithPlayers(3, allReadyWithPawns: true);
        var nonHost = room.Players.First(p => !p.IsHost);

        Assert.Throws<InvalidOperationException>(() => service.StartGame(room.Id, nonHost.Id));
    }

    [Fact]
    public void StartGame_ByHostMovesRoomToInGame()
    {
        var (service, room) = CreateRoomWithPlayers(4, allReadyWithPawns: true);

        service.StartGame(room.Id, room.HostPlayerId);

        Assert.Equal(RoomStatus.InGame, room.Status);
    }

    [Fact]
    public void StartGame_FailsWhenRequirementsNotMet()
    {
        var (service, room) = CreateRoomWithPlayers(3);

        Assert.Throws<InvalidOperationException>(() => service.StartGame(room.Id, room.HostPlayerId));
    }
}
