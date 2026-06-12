using System.Collections.Concurrent;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Gerencia salas/lobby em memória: criação, entrada, personagens, pronto e início.
/// </summary>
public class RoomService(Random random)
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;

    public static readonly IReadOnlyList<Pawn> AvailablePawns =
    [
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Detetive Arthur Vale", Color = "#287D78" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "Condessa Helena Vesper", Color = "#6E2638" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Professor Otávio Lacerda", Color = "#67408B" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "Doutora Cecília Marinho", Color = "#A9CF1D" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = "Capitão Raul Ferraz", Color = "#405A72" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = "Madame Amélia Bellini", Color = "#A64078" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = "Jornalista Clara Vidal", Color = "#C96F32" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = "Juiz Afonso Brandão", Color = "#D8C7A1" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = "Mordomo Sebastião Leme", Color = "#6B4A32" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000010"), Name = "Cantora Íris Montenegro", Color = "#3157A4" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000011"), Name = "Empresário Vicente Dourado", Color = "#477A45" },
        new() { Id = Guid.Parse("00000000-0000-0000-0000-000000000012"), Name = "Jardineira Elisa Campos", Color = "#B65A32" },
    ];

    private readonly ConcurrentDictionary<Guid, Room> _rooms = new();

    public RoomService() : this(Random.Shared) { }

    public Room CreateRoom(string hostName)
    {
        var host = new Player { Name = hostName, IsHost = true };
        var room = new Room { Code = GenerateUniqueCode(), HostPlayerId = host.Id };
        host.RoomId = room.Id;
        room.Players.Add(host);
        _rooms[room.Id] = room;
        return room;
    }

    public Player JoinRoom(string roomCode, string playerName)
    {
        var room = _rooms.Values.FirstOrDefault(r => r.Code == roomCode)
            ?? throw new InvalidOperationException($"Sala com código {roomCode} não encontrada.");

        if (room.Status != RoomStatus.Lobby)
        {
            throw new InvalidOperationException("A sala não está mais aceitando jogadores.");
        }

        if (room.Players.Count >= Room.MaxPlayers)
        {
            throw new InvalidOperationException($"A sala já atingiu o máximo de {Room.MaxPlayers} jogadores.");
        }

        var player = new Player { Name = playerName, RoomId = room.Id };
        room.Players.Add(player);
        return player;
    }

    public void LeaveRoom(Guid roomId, Guid playerId)
    {
        var room = GetRoom(roomId);
        var player = GetPlayer(room, playerId);
        room.Players.Remove(player);

        if (room.Players.Count == 0)
        {
            room.Status = RoomStatus.Closed;
            _rooms.TryRemove(room.Id, out _);
            return;
        }

        if (player.IsHost)
        {
            var newHost = room.Players.OrderBy(p => p.JoinedAt).First();
            newHost.IsHost = true;
            room.HostPlayerId = newHost.Id;
        }
    }

    public void ChoosePawn(Guid roomId, Guid playerId, Guid pawnId)
    {
        var room = GetRoom(roomId);
        var player = GetPlayer(room, playerId);

        if (AvailablePawns.All(p => p.Id != pawnId))
        {
            throw new InvalidOperationException("Personagem inexistente.");
        }

        if (room.Players.Any(p => p.Id != playerId && p.PawnId == pawnId))
        {
            throw new InvalidOperationException("Este personagem já foi escolhido por outro jogador.");
        }

        player.PawnId = pawnId;
    }

    public void SetReady(Guid roomId, Guid playerId, bool isReady)
    {
        var room = GetRoom(roomId);
        GetPlayer(room, playerId).IsReady = isReady;
    }

    public bool CanStartGame(Guid roomId)
    {
        var room = GetRoom(roomId);
        return room.Status == RoomStatus.Lobby
            && room.Players.Count >= Room.MinPlayers
            && room.Players.All(p => p.PawnId is not null)
            && room.Players.All(p => p.IsReady);
    }

    public Room StartGame(Guid roomId, Guid hostPlayerId)
    {
        var room = GetRoom(roomId);

        if (room.HostPlayerId != hostPlayerId)
        {
            throw new InvalidOperationException("Apenas o host pode iniciar a partida.");
        }

        if (!CanStartGame(roomId))
        {
            throw new InvalidOperationException(
                "A partida exige ao menos 3 jogadores, todos com personagem escolhido e prontos.");
        }

        room.Status = RoomStatus.InGame;
        return room;
    }

    public Room GetRoom(Guid roomId) =>
        _rooms.TryGetValue(roomId, out var room)
            ? room
            : throw new InvalidOperationException("Sala não encontrada.");

    private static Player GetPlayer(Room room, Guid playerId) =>
        room.Players.FirstOrDefault(p => p.Id == playerId)
            ?? throw new InvalidOperationException("Jogador não está nesta sala.");

    private string GenerateUniqueCode()
    {
        while (true)
        {
            var code = string.Concat(
                Enumerable.Range(0, CodeLength).Select(_ => CodeAlphabet[random.Next(CodeAlphabet.Length)]));

            if (_rooms.Values.All(r => r.Code != code))
            {
                return code;
            }
        }
    }
}
