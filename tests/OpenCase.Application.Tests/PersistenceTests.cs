using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;
using OpenCase.Infrastructure.Persistence;

namespace OpenCase.Application.Tests;

public class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OpenCaseDbContext _context;

    public PersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<OpenCaseDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new OpenCaseDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void Model_MapsCoreEntities()
    {
        var entityNames = _context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

        Assert.Contains(nameof(Card), entityNames);
        Assert.Contains(nameof(Pawn), entityNames);
        Assert.Contains(nameof(Room), entityNames);
        Assert.Contains(nameof(Player), entityNames);
        Assert.Contains(nameof(Game), entityNames);
        Assert.Contains(nameof(GameEvent), entityNames);
        Assert.Contains(nameof(Hint), entityNames);
    }

    [Fact]
    public async Task Seeder_CreatesInitialCardsPawnsAndBoardTemplate()
    {
        await DataSeeder.SeedAsync(_context);

        Assert.Equal(8, await _context.Cards.CountAsync(c => c.Type == CardType.Suspect));
        Assert.Equal(12, await _context.Cards.CountAsync(c => c.Type == CardType.Location));
        Assert.Equal(8, await _context.Cards.CountAsync(c => c.Type == CardType.Weapon));
        Assert.Equal(12, await _context.Pawns.CountAsync());

        var template = await _context.BoardTemplates.SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(template.Json));
    }

    [Fact]
    public async Task Seeder_IsIdempotent()
    {
        await DataSeeder.SeedAsync(_context);
        await DataSeeder.SeedAsync(_context);

        Assert.Equal(28, await _context.Cards.CountAsync());
        Assert.Equal(12, await _context.Pawns.CountAsync());
        Assert.Equal(1, await _context.BoardTemplates.CountAsync());
    }

    [Fact]
    public async Task Game_RoundTripsWithSolutionStateAndEvents()
    {
        var playerId = Guid.NewGuid();
        var game = new Game
        {
            Status = GameStatus.InProgress,
            Solution = new GameSolution
            {
                SuspectCardId = Guid.NewGuid(),
                LocationCardId = Guid.NewGuid(),
                WeaponCardId = Guid.NewGuid(),
            },
            Hands = [new PlayerHand { PlayerId = playerId, Cards = [new Card { Type = CardType.Weapon, Name = "Arma 1" }] }],
            PawnPositions = { [playerId] = new BoardPosition(4, 5) },
            Events = [new GameEvent { Type = "GameStarted", Data = "{}" }],
        };
        game.TurnState.TurnOrder = [playerId];

        _context.Games.Add(game);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Games.Include(g => g.Events).SingleAsync(g => g.Id == game.Id);

        Assert.Equal(game.Solution.SuspectCardId, loaded.Solution!.SuspectCardId);
        Assert.Equal(new BoardPosition(4, 5), loaded.PawnPositions[playerId]);
        Assert.Single(loaded.Hands);
        Assert.Equal([playerId], loaded.TurnState.TurnOrder);
        Assert.Single(loaded.Events);
    }

    [Fact]
    public async Task Room_RoundTripsWithPlayers()
    {
        var room = new Room { Code = "ABC123" };
        room.Players.Add(new Player { Name = "Biel", RoomId = room.Id, IsHost = true });

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Rooms.Include(r => r.Players).SingleAsync(r => r.Id == room.Id);

        Assert.Single(loaded.Players);
        Assert.Equal("Biel", loaded.Players[0].Name);
    }
}
