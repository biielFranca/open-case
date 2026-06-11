using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class GameSetupServiceTests
{
    private static List<Card> CreateDeck() =>
    [
        .. Enumerable.Range(1, 8).Select(i => new Card { Type = CardType.Suspect, Name = $"Suspeito {i}" }),
        .. Enumerable.Range(1, 12).Select(i => new Card { Type = CardType.Location, Name = $"Local {i}" }),
        .. Enumerable.Range(1, 8).Select(i => new Card { Type = CardType.Weapon, Name = $"Arma {i}" }),
    ];

    private static List<Guid> CreatePlayers(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => Guid.NewGuid())];

    private static GameSetupService CreateService(int seed = 42) => new(new Random(seed));

    [Fact]
    public void CreateSolution_HasExactlyThreeCards()
    {
        var deck = CreateDeck();
        var solution = CreateService().CreateSolution(deck);

        var ids = new[] { solution.SuspectCardId, solution.LocationCardId, solution.WeaponCardId };
        Assert.Equal(3, ids.Distinct().Count());
        Assert.All(ids, id => Assert.Contains(deck, c => c.Id == id));
    }

    [Fact]
    public void CreateSolution_HasOneCardOfEachType()
    {
        var deck = CreateDeck();
        var solution = CreateService().CreateSolution(deck);

        Assert.Equal(CardType.Suspect, deck.Single(c => c.Id == solution.SuspectCardId).Type);
        Assert.Equal(CardType.Location, deck.Single(c => c.Id == solution.LocationCardId).Type);
        Assert.Equal(CardType.Weapon, deck.Single(c => c.Id == solution.WeaponCardId).Type);
    }

    [Fact]
    public void SetupGame_SolutionCardsAreNotDealt()
    {
        var deck = CreateDeck();
        var game = CreateService().SetupGame(Guid.NewGuid(), CreatePlayers(4), deck);

        var solutionIds = new[]
        {
            game.Solution!.SuspectCardId,
            game.Solution.LocationCardId,
            game.Solution.WeaponCardId,
        };
        var dealtIds = game.Hands.SelectMany(h => h.Cards).Select(c => c.Id).ToList();

        Assert.Empty(dealtIds.Intersect(solutionIds));
    }

    [Fact]
    public void SetupGame_AllRemainingCardsAreDealt()
    {
        var deck = CreateDeck();
        var game = CreateService().SetupGame(Guid.NewGuid(), CreatePlayers(5), deck);

        var dealtIds = game.Hands.SelectMany(h => h.Cards).Select(c => c.Id).ToList();

        Assert.Equal(deck.Count - 3, dealtIds.Count);
        Assert.Equal(dealtIds.Count, dealtIds.Distinct().Count());
    }

    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    public void SetupGame_WorksWithMinAndMaxPlayers(int playerCount)
    {
        var players = CreatePlayers(playerCount);
        var game = CreateService().SetupGame(Guid.NewGuid(), players, CreateDeck());

        Assert.Equal(playerCount, game.Hands.Count);
        Assert.All(game.Hands, h => Assert.NotEmpty(h.Cards));
        Assert.All(players, p => Assert.Contains(game.Hands, h => h.PlayerId == p));
    }

    [Fact]
    public void SetupGame_UnevenDistributionIsAllowed()
    {
        // 28 cartas - 3 da solução = 25, que não divide por 3: mãos de 9, 8, 8.
        var game = CreateService().SetupGame(Guid.NewGuid(), CreatePlayers(3), CreateDeck());

        var handSizes = game.Hands.Select(h => h.Cards.Count).OrderByDescending(n => n).ToList();

        Assert.Equal(25, handSizes.Sum());
        Assert.Equal(1, handSizes.Max() - handSizes.Min());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(9)]
    public void SetupGame_RejectsInvalidPlayerCount(int playerCount)
    {
        Assert.Throws<ArgumentException>(() =>
            CreateService().SetupGame(Guid.NewGuid(), CreatePlayers(playerCount), CreateDeck()));
    }

    [Fact]
    public void ShuffleCards_IsDeterministicForSameSeed()
    {
        var deck = CreateDeck();

        var first = new GameSetupService(new Random(7)).ShuffleCards(deck).Select(c => c.Id);
        var second = new GameSetupService(new Random(7)).ShuffleCards(deck).Select(c => c.Id);

        Assert.Equal(first, second);
        Assert.Equal(deck.Count, first.Count());
    }
}
