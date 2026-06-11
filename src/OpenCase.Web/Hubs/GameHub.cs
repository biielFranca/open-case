using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenCase.Application.Mapping;
using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Shared.Dtos;

namespace OpenCase.Web.Hubs;

/// <summary>
/// Hub da partida. Estado público vai para o grupo da sala; cartas e dicas
/// privadas vão apenas para a conexão do jogador. A solução nunca é emitida
/// antes do fim da partida.
/// </summary>
public class GameHub(
    RoomService roomService,
    GameManager gameManager,
    TurnService turnService,
    MovementService movementService,
    SuggestionService suggestionService,
    RefutationService refutationService,
    FinalAccusationService finalAccusationService,
    BoardService boardService) : Hub
{
    private record ConnectionInfo(Guid RoomId, Guid PlayerId);

    private static readonly ConcurrentDictionary<string, ConnectionInfo> Connections = new();
    private static readonly ConcurrentDictionary<Guid, string> ConnectionsByPlayer = new();

    // ─── Lobby ───

    public async Task CreateRoom(string hostName)
    {
        var room = roomService.CreateRoom(hostName);
        await RegisterConnection(room.Id, room.HostPlayerId);
        await Clients.Caller.SendAsync("RoomUpdated", GameStateMapper.ToRoomDto(room));
    }

    public async Task JoinRoom(string roomCode, string playerName)
    {
        try
        {
            var player = roomService.JoinRoom(roomCode, playerName);
            await RegisterConnection(player.RoomId, player.Id);
            await BroadcastRoom(player.RoomId);
        }
        catch (InvalidOperationException ex)
        {
            await SendError(ex.Message);
        }
    }

    public async Task LeaveRoom()
    {
        if (!TryGetConnection(out var info))
        {
            return;
        }

        roomService.LeaveRoom(info.RoomId, info.PlayerId);
        Connections.TryRemove(Context.ConnectionId, out _);
        ConnectionsByPlayer.TryRemove(info.PlayerId, out _);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(info.RoomId));
        await BroadcastRoom(info.RoomId);
    }

    public async Task ChoosePawn(Guid pawnId)
    {
        await WithRoom(async info =>
        {
            roomService.ChoosePawn(info.RoomId, info.PlayerId, pawnId);
            await BroadcastRoom(info.RoomId);
        });
    }

    public async Task SetReady(bool isReady)
    {
        await WithRoom(async info =>
        {
            roomService.SetReady(info.RoomId, info.PlayerId, isReady);
            await BroadcastRoom(info.RoomId);
        });
    }

    public async Task StartGame()
    {
        await WithRoom(async info =>
        {
            var room = roomService.StartGame(info.RoomId, info.PlayerId);
            var game = gameManager.StartGame(room);

            await Clients.Group(GroupName(info.RoomId)).SendAsync(
                "GameStarted",
                GameStateMapper.ToPublicState(game),
                GameStateMapper.ToBoardDto(game.Board));

            // Cada jogador recebe a própria mão em privado.
            foreach (var hand in game.Hands)
            {
                await SendToPlayer(hand.PlayerId, "PrivateHand", GameStateMapper.ToPrivateHand(game, hand.PlayerId));
            }
        });
    }

    // ─── Turnos e movimento ───

    public async Task RollInitialDice()
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            var rolls = gameManager.RollInitialOrder(game);

            await Clients.Group(GroupName(info.RoomId)).SendAsync("DiceRolled", new { Initial = true, Rolls = rolls });
            await BroadcastTurn(info.RoomId, game);
        });
    }

    public async Task RollTurnDice()
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            EnsureCurrentPlayer(game, info.PlayerId);

            game.TurnState.DiceValue = turnService.RollTurnDice();
            game.TurnState.Phase = Domain.Enums.TurnPhase.Move;

            await Clients.Group(GroupName(info.RoomId)).SendAsync(
                "DiceRolled", new { Initial = false, PlayerId = info.PlayerId, Value = game.TurnState.DiceValue });
            await BroadcastTurn(info.RoomId, game);
        });
    }

    public async Task MovePawn(List<PositionDto> path)
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            EnsureCurrentPlayer(game, info.PlayerId);

            var dice = game.TurnState.DiceValue
                ?? throw new InvalidOperationException("Role o dado antes de mover.");

            var domainPath = path.Select(p => new BoardPosition(p.X, p.Y)).ToList();
            var result = movementService.ApplyMove(game, info.PlayerId, domainPath, dice);

            if (!result.IsValid)
            {
                await SendError(string.Join(" ", result.Errors));
                return;
            }

            game.TurnState.Phase = result.EnteredLocation
                ? Domain.Enums.TurnPhase.Suggestion
                : Domain.Enums.TurnPhase.EndOfTurn;

            await Clients.Group(GroupName(info.RoomId)).SendAsync("PawnMoved", new
            {
                PlayerId = info.PlayerId,
                Position = new PositionDto(result.NewPosition.X, result.NewPosition.Y),
                result.EnteredLocation,
                result.LocationId,
            });

            if (!result.EnteredLocation)
            {
                turnService.AdvanceTurn(game.TurnState);
            }

            await BroadcastTurn(info.RoomId, game);
        });
    }

    // ─── Palpite, refutação e acusação ───

    public async Task MakeSuggestion(Guid suspectCardId, Guid weaponCardId)
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            EnsureCurrentPlayer(game, info.PlayerId);

            var suggestion = suggestionService.CreateSuggestion(game, info.PlayerId, suspectCardId, weaponCardId);
            var state = refutationService.CreateRefutationState(game, suggestion);

            await Clients.Group(GroupName(info.RoomId)).SendAsync(
                "SuggestionCreated", GameStateMapper.ToSuggestionDto(suggestion));
            await Clients.Group(GroupName(info.RoomId)).SendAsync(
                "RefutationRequested", GameStateMapper.ToPublicRefutationDto(state));
        });
    }

    public async Task PassRefutation()
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            var state = game.ActiveRefutation
                ?? throw new InvalidOperationException("Não há refutação em andamento.");

            refutationService.PassRefutation(game, state, info.PlayerId);

            await Clients.Group(GroupName(info.RoomId)).SendAsync(
                "RefutationPassed", new { PlayerId = info.PlayerId });

            if (state.IsResolved)
            {
                await Clients.Group(GroupName(info.RoomId)).SendAsync(
                    "RefutationResolved", GameStateMapper.ToPublicRefutationDto(state));
            }

            await BroadcastTurn(info.RoomId, game);
        });
    }

    public async Task ShowRefutationCard(Guid cardId)
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            var state = game.ActiveRefutation
                ?? throw new InvalidOperationException("Não há refutação em andamento.");

            var shown = refutationService.ShowCard(game, state, info.PlayerId, cardId);
            var card = game.AllCards.First(c => c.Id == shown.CardId);

            // Evento público sem a carta; a carta vai somente ao acusador.
            await Clients.Group(GroupName(info.RoomId)).SendAsync(
                "RefutationResolved", GameStateMapper.ToPublicRefutationDto(state));
            await SendToPlayer(shown.AccuserPlayerId, "PrivateCardShown",
                new PrivateCardShownDto(shown.ShownByPlayerId, GameStateMapper.ToCardDto(card)));

            await BroadcastTurn(info.RoomId, game);
        });
    }

    public async Task MakeFinalAccusation()
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            var result = finalAccusationService.MakeFinalAccusation(game, info.PlayerId);

            await Clients.Group(GroupName(info.RoomId)).SendAsync("FinalAccusationResult", new
            {
                PlayerId = info.PlayerId,
                result.IsCorrect,
            });

            if (result.IsCorrect)
            {
                // Só agora a solução pode ser revelada.
                await Clients.Group(GroupName(info.RoomId)).SendAsync("GameFinished", new
                {
                    WinnerPlayerId = game.WinnerPlayerId,
                    Solution = result.RevealedSolution,
                });
            }
            else
            {
                turnService.AdvanceTurn(game.TurnState);
                await BroadcastTurn(info.RoomId, game);
            }
        });
    }

    public async Task UseSecretPassage()
    {
        await WithRoom(async info =>
        {
            var game = gameManager.GetGameByRoom(info.RoomId);
            EnsureCurrentPlayer(game, info.PlayerId);

            var position = game.PawnPositions[info.PlayerId];
            var location = boardService.GetLocationByCell(game.Board, position.X, position.Y)
                ?? throw new InvalidOperationException("É preciso estar dentro de um local.");
            var target = boardService.GetSecretPassageTarget(game.Board, location.Id)
                ?? throw new InvalidOperationException("Este local não possui passagem secreta.");

            var entrance = target.EntranceCells[0];
            game.PawnPositions[info.PlayerId] = new BoardPosition(entrance.X, entrance.Y);
            game.TurnState.Phase = Domain.Enums.TurnPhase.Suggestion;

            await Clients.Group(GroupName(info.RoomId)).SendAsync("PawnMoved", new
            {
                PlayerId = info.PlayerId,
                Position = new PositionDto(entrance.X, entrance.Y),
                EnteredLocation = true,
                LocationId = (Guid?)target.Id,
            });
            await BroadcastTurn(info.RoomId, game);
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveRoom();
        await base.OnDisconnectedAsync(exception);
    }

    // ─── Auxiliares ───

    private static string GroupName(Guid roomId) => $"room:{roomId}";

    private async Task RegisterConnection(Guid roomId, Guid playerId)
    {
        Connections[Context.ConnectionId] = new ConnectionInfo(roomId, playerId);
        ConnectionsByPlayer[playerId] = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
    }

    private bool TryGetConnection(out ConnectionInfo info) =>
        Connections.TryGetValue(Context.ConnectionId, out info!);

    private async Task WithRoom(Func<ConnectionInfo, Task> action)
    {
        if (!TryGetConnection(out var info))
        {
            await SendError("Conexão não está associada a uma sala.");
            return;
        }

        try
        {
            await action(info);
        }
        catch (InvalidOperationException ex)
        {
            await SendError(ex.Message);
        }
    }

    private static void EnsureCurrentPlayer(Game game, Guid playerId)
    {
        if (game.TurnState.CurrentPlayerId != playerId)
        {
            throw new InvalidOperationException("Não é o seu turno.");
        }
    }

    private async Task BroadcastRoom(Guid roomId)
    {
        var room = roomService.GetRoom(roomId);
        await Clients.Group(GroupName(roomId)).SendAsync("RoomUpdated", GameStateMapper.ToRoomDto(room));
    }

    private async Task BroadcastTurn(Guid roomId, Game game)
    {
        await Clients.Group(GroupName(roomId)).SendAsync("TurnUpdated", GameStateMapper.ToPublicState(game));
    }

    private async Task SendToPlayer(Guid playerId, string eventName, object payload)
    {
        if (ConnectionsByPlayer.TryGetValue(playerId, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync(eventName, payload);
        }
    }

    private async Task SendError(string message) =>
        await Clients.Caller.SendAsync("ErrorMessage", message);
}
