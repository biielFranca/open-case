using System.Collections.Concurrent;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Services;

/// <summary>
/// Mantém as partidas ativas em memória e orquestra a criação a partir de uma sala.
/// </summary>
public class GameManager(BoardService boardService, GameSetupService gameSetupService, TurnService turnService)
{
    private readonly ConcurrentDictionary<Guid, Game> _gamesByRoom = new();

    public Game StartGame(Room room)
    {
        if (room.Status != RoomStatus.InGame)
        {
            throw new InvalidOperationException("A sala ainda não iniciou a partida.");
        }

        var board = boardService.CreateDefaultBoard();
        var deck = BuildDeck(board);
        var playerIds = room.Players.Select(p => p.Id).ToList();

        var game = gameSetupService.SetupGame(room.Id, playerIds, deck);
        game.Board = board;

        var suspectCards = deck.Where(c => c.Type == CardType.Suspect).ToList();
        for (var i = 0; i < playerIds.Count; i++)
        {
            game.PlayerSuspects[playerIds[i]] = suspectCards[i].Id;
            // Peões começam espalhados no corredor inferior (y=19 é linha de corredor).
            game.PawnPositions[playerIds[i]] = new BoardPosition(2 + i * 2, Board.Height - 1);
        }

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
}
