using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class HintServiceTests
{
    private readonly HintService _service;
    private readonly Game _game;
    private readonly List<string> _solutionNames;

    public HintServiceTests()
    {
        _service = new HintService(new Random(42));
        _game = new Game { Status = GameStatus.InProgress };

        var suspects = Enumerable.Range(1, 4).Select(i => new Card { Type = CardType.Suspect, Name = $"Suspeito {i}" }).ToList();
        var locations = Enumerable.Range(1, 4).Select(i => new Card { Type = CardType.Location, Name = $"Local {i}" }).ToList();
        var weapons = Enumerable.Range(1, 4).Select(i => new Card { Type = CardType.Weapon, Name = $"Arma {i}" }).ToList();
        _game.AllCards = [.. suspects, .. locations, .. weapons];

        _game.Solution = new GameSolution
        {
            SuspectCardId = suspects[0].Id,
            LocationCardId = locations[0].Id,
            WeaponCardId = weapons[0].Id,
        };
        _solutionNames = [suspects[0].Name, locations[0].Name, weapons[0].Name];

        var players = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList();
        _game.TurnState.TurnOrder = players;
        _game.Hands = [.. players.Select(p => new PlayerHand { PlayerId = p })];
    }

    [Fact]
    public void GeneratePrivateHint_TargetsARandomPlayerOfTheGame()
    {
        var hint = _service.GeneratePrivateHint(_game);

        Assert.Equal(HintType.Private, hint.Type);
        Assert.NotNull(hint.TargetPlayerId);
        Assert.Contains(hint.TargetPlayerId!.Value, _game.TurnState.TurnOrder);
        Assert.False(string.IsNullOrWhiteSpace(hint.Text));
    }

    [Fact]
    public void GeneratePublicHint_HasNoTargetPlayer()
    {
        var hint = _service.GeneratePublicHint(_game);

        Assert.Equal(HintType.Public, hint.Type);
        Assert.Null(hint.TargetPlayerId);
    }

    [Fact]
    public void GenerateNoiseHint_HasNoTargetAndNeverMentionsAnyCard()
    {
        for (var i = 0; i < 100; i++)
        {
            var hint = _service.GenerateNoiseHint(_game);

            Assert.Equal(HintType.Noise, hint.Type);
            Assert.Null(hint.TargetPlayerId);
            Assert.All(_game.AllCards, card => Assert.DoesNotContain(card.Name, hint.Text));
        }
    }

    [Fact]
    public void PrivateAndPublicHints_NeverMentionSolutionCards()
    {
        for (var i = 0; i < 200; i++)
        {
            var hints = new[] { _service.GeneratePrivateHint(_game), _service.GeneratePublicHint(_game) };

            foreach (var hint in hints)
            {
                Assert.All(_solutionNames, name => Assert.DoesNotContain(name, hint.Text));
            }
        }
    }

    [Fact]
    public void GenerateRandomHint_ProducesAllTypesOverManyDraws()
    {
        var seen = new HashSet<HintType>();

        for (var i = 0; i < 200; i++)
        {
            seen.Add(_service.GenerateRandomHint(_game).Type);
        }

        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void Hints_CanRepeatWithoutLimit()
    {
        var hints = Enumerable.Range(0, 50).Select(_ => _service.GenerateRandomHint(_game)).ToList();

        Assert.Equal(50, hints.Count);
        Assert.All(hints, h => Assert.Equal(_game.Id, h.GameId));
    }

    [Fact]
    public void PickRandomTargetPlayer_ReturnsPlayerFromTheGame()
    {
        for (var i = 0; i < 50; i++)
        {
            Assert.Contains(_service.PickRandomTargetPlayer(_game), _game.TurnState.TurnOrder);
        }
    }
}
