using System.Reflection;
using OpenCase.Application.Mapping;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;
using OpenCase.Shared.Dtos;

namespace OpenCase.Application.Tests;

public class DtoLeakTests
{
    [Fact]
    public void PublicGameStateDto_DoesNotExposeSolutionOrPrivateData()
    {
        var forbidden = new[] { "solution", "solucao", "hands", "shown", "notes" };

        var propertyNames = CollectPropertyNamesRecursively(typeof(PublicGameStateDto));

        foreach (var name in propertyNames)
        {
            Assert.All(forbidden, f => Assert.DoesNotContain(f, name.ToLowerInvariant()));
        }
    }

    [Fact]
    public void PublicGameStateDto_DoesNotReferenceDomainSolutionType()
    {
        var types = CollectTypesRecursively(typeof(PublicGameStateDto));

        Assert.DoesNotContain(typeof(GameSolution), types);
    }

    [Fact]
    public void GameStateMapper_BuildsPublicStateWithoutCardsOfPlayers()
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
            Hands = [new PlayerHand { PlayerId = playerId, Cards = [new Card { Name = "Segredo", Type = CardType.Weapon }] }],
            PawnPositions = { [playerId] = new BoardPosition(4, 5) },
        };
        game.TurnState.TurnOrder = [playerId];
        game.TurnState.CurrentPlayerId = playerId;

        var dto = GameStateMapper.ToPublicState(game);

        Assert.Equal(game.Id, dto.GameId);
        Assert.Equal(playerId, dto.Turn.CurrentPlayerId);
        Assert.Equal(4, dto.PawnPositions[playerId].X);

        // A contagem de cartas é pública; o conteúdo das mãos não.
        Assert.Equal(1, dto.CardCounts[playerId]);
    }

    [Fact]
    public void GameStateMapper_BuildsPrivateHandOnlyForRequestedPlayer()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var game = new Game
        {
            Hands =
            [
                new PlayerHand { PlayerId = playerA, Cards = [new Card { Name = "Carta A", Type = CardType.Suspect }] },
                new PlayerHand { PlayerId = playerB, Cards = [new Card { Name = "Carta B", Type = CardType.Weapon }] },
            ],
        };

        var hand = GameStateMapper.ToPrivateHand(game, playerA);

        Assert.Equal(playerA, hand.PlayerId);
        var card = Assert.Single(hand.Cards);
        Assert.Equal("Carta A", card.Name);
    }

    private static HashSet<string> CollectPropertyNamesRecursively(Type root)
    {
        var names = new HashSet<string>();
        foreach (var type in CollectTypesRecursively(root))
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                names.Add(property.Name);
            }
        }

        return names;
    }

    private static HashSet<Type> CollectTypesRecursively(Type root)
    {
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    queue.Enqueue(arg);
                }

                continue;
            }

            if (!visited.Add(type) || type.Namespace?.StartsWith("System") != false)
            {
                continue;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                queue.Enqueue(property.PropertyType);
            }
        }

        return visited;
    }
}
