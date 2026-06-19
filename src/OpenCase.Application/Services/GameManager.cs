using System.Collections.Concurrent;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Mantém as partidas ativas em memória e orquestra a criação a partir de uma sala.
/// </summary>
public class GameManager(
    BoardService boardService,
    GameSetupService gameSetupService,
    TurnService turnService,
    GameEventLogger? eventLogger = null)
{
    public static readonly IReadOnlyDictionary<Guid, string> StartingLocationByPawnId =
        new Dictionary<Guid, string>
        {
            [Guid.Parse("00000000-0000-0000-0000-000000000001")] = "Escritório",
            [Guid.Parse("00000000-0000-0000-0000-000000000002")] = "Salão de Festas",
            [Guid.Parse("00000000-0000-0000-0000-000000000003")] = "Observatório",
            [Guid.Parse("00000000-0000-0000-0000-000000000004")] = "Cozinha",
            [Guid.Parse("00000000-0000-0000-0000-000000000005")] = "Hall",
            [Guid.Parse("00000000-0000-0000-0000-000000000006")] = "Quarto de Hóspedes",
            [Guid.Parse("00000000-0000-0000-0000-000000000007")] = "Biblioteca",
            [Guid.Parse("00000000-0000-0000-0000-000000000008")] = "Sala de Jantar",
            [Guid.Parse("00000000-0000-0000-0000-000000000009")] = "Porão",
            [Guid.Parse("00000000-0000-0000-0000-000000000010")] = "Salão de Jogos",
            [Guid.Parse("00000000-0000-0000-0000-000000000011")] = "Estufa",
            [Guid.Parse("00000000-0000-0000-0000-000000000012")] = "Jardim de Inverno",
        };

    private readonly ConcurrentDictionary<Guid, Game> _gamesByRoom = new();
    private readonly GameEventLogger _eventLogger = eventLogger ?? new GameEventLogger();

    public Game StartGame(Room room)
    {
        if (room.Status != RoomStatus.InGame)
        {
            throw new InvalidOperationException("A sala ainda não iniciou a partida.");
        }

        var board = boardService.CreateDefaultBoard();
        var deck = BuildDeck(board);
        var players = room.Players.ToList();
        var playerIds = players.Select(p => p.Id).ToList();

        var game = gameSetupService.SetupGame(room.Id, playerIds, deck);
        game.Board = board;

        var suspectCards = deck.Where(c => c.Type == CardType.Suspect).ToList();
        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            game.PlayerSuspects[player.Id] = suspectCards[i].Id;
            game.PawnPositions[player.Id] = StartingPositionFor(board, player);
        }

        _eventLogger.Log(game, GameEventTypes.GameStarted, new { room.Id, Players = playerIds.Count });
        _eventLogger.Log(game, GameEventTypes.SolutionCreated);
        _eventLogger.Log(game, GameEventTypes.CardsDealt, new { Cards = deck.Count - 3 });

        _gamesByRoom[room.Id] = game;
        return game;
    }

    public Game GetGameByRoom(Guid roomId) =>
        _gamesByRoom.TryGetValue(roomId, out var game)
            ? game
            : throw new InvalidOperationException("Não há partida ativa para esta sala.");

    public Dictionary<Guid, int> RollInitialOrder(Game game)
    {
        var playerIds = game.Hands.Select(h => h.PlayerId).ToList();
        Dictionary<Guid, int> rolls;
        InitialOrderResult result;

        do
        {
            rolls = turnService.RollInitialDice(playerIds);
            result = turnService.ResolveInitialOrder(rolls);
        } while (result.HasTieAtTop);

        game.TurnState.TurnOrder = result.TurnOrder;
        game.TurnState.CurrentPlayerId = result.TurnOrder[0];
        game.TurnState.Phase = TurnPhase.RollDice;
        return rolls;
    }

    private static List<Card> BuildDeck(Board board) =>
    [
        .. CardCatalog.SuspectNames.Select(n => new Card { Type = CardType.Suspect, Name = n }),
        .. board.Locations.Select(l => new Card { Type = CardType.Location, Name = l.Name }),
        .. CardCatalog.WeaponNames.Select(n => new Card { Type = CardType.Weapon, Name = n }),
    ];

    private static BoardPosition StartingPositionFor(Board board, Player player)
    {
        var pawnId = player.PawnId
            ?? throw new InvalidOperationException($"O jogador {player.Name} não escolheu um personagem.");

        var locationName = StartingLocationByPawnId.GetValueOrDefault(pawnId)
            ?? throw new InvalidOperationException("O personagem escolhido não possui um local inicial.");

        var location = board.Locations.FirstOrDefault(candidate => candidate.Name == locationName)
            ?? throw new InvalidOperationException($"O local inicial {locationName} não existe na mansão.");

        var entrance = location.EntranceCells[0];
        return new BoardPosition(entrance.X, entrance.Y);
    }
}
