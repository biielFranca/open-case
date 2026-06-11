using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Monta o estado inicial da partida: solução secreta e distribuição de cartas.
/// O <see cref="Random"/> é injetado para que os testes possam fixar a semente.
/// </summary>
public class GameSetupService(Random random)
{
    public GameSetupService() : this(Random.Shared) { }

    public GameSolution CreateSolution(IReadOnlyList<Card> allCards)
    {
        return new GameSolution
        {
            SuspectCardId = PickRandom(allCards, CardType.Suspect).Id,
            LocationCardId = PickRandom(allCards, CardType.Location).Id,
            WeaponCardId = PickRandom(allCards, CardType.Weapon).Id,
        };
    }

    public List<Card> ShuffleCards(IReadOnlyList<Card> cards)
    {
        var shuffled = cards.ToList();
        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = random.Next(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }

    public List<PlayerHand> DealCards(IReadOnlyList<Card> cards, IReadOnlyList<Guid> playerIds)
    {
        var hands = playerIds.Select(id => new PlayerHand { PlayerId = id }).ToList();
        for (var i = 0; i < cards.Count; i++)
        {
            hands[i % hands.Count].Cards.Add(cards[i]);
        }

        return hands;
    }

    public Game SetupGame(Guid roomId, IReadOnlyList<Guid> playerIds, IReadOnlyList<Card> allCards)
    {
        if (playerIds.Count is < Room.MinPlayers or > Room.MaxPlayers)
        {
            throw new ArgumentException(
                $"A partida exige entre {Room.MinPlayers} e {Room.MaxPlayers} jogadores.", nameof(playerIds));
        }

        var solution = CreateSolution(allCards);
        var solutionIds = new HashSet<Guid>
        {
            solution.SuspectCardId,
            solution.LocationCardId,
            solution.WeaponCardId,
        };

        var remaining = allCards.Where(c => !solutionIds.Contains(c.Id)).ToList();
        var hands = DealCards(ShuffleCards(remaining), playerIds);

        return new Game
        {
            RoomId = roomId,
            Status = GameStatus.InProgress,
            Solution = solution,
            AllCards = [.. allCards],
            Hands = hands,
        };
    }

    private Card PickRandom(IReadOnlyList<Card> cards, CardType type)
    {
        var ofType = cards.Where(c => c.Type == type).ToList();
        if (ofType.Count == 0)
        {
            throw new ArgumentException($"O baralho não contém cartas do tipo {type}.", nameof(cards));
        }

        return ofType[random.Next(0, ofType.Count)];
    }
}
