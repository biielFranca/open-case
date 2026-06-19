using OpenCase.Domain.Entities;
using OpenCase.Shared.Dtos;

namespace OpenCase.Application.Mapping;

/// <summary>
/// Converte o estado do domínio em DTOs. O estado público nunca inclui a solução
/// nem o conteúdo das mãos — apenas contagens.
/// </summary>
public static class GameStateMapper
{
    public static PublicGameStateDto ToPublicState(Game game) => new(
        game.Id,
        game.RoomId,
        game.Status.ToString(),
        ToTurnDto(game.TurnState),
        game.PawnPositions.ToDictionary(p => p.Key, p => new PositionDto(p.Value.X, p.Value.Y)),
        game.Hands.ToDictionary(h => h.PlayerId, h => h.Cards.Count),
        game.ActiveSuggestion is null ? null : ToSuggestionDto(game.ActiveSuggestion),
        game.ActiveRefutation is null ? null : ToPublicRefutationDto(game.ActiveRefutation),
        game.WinnerPlayerId);

    public static PlayerHandDto ToPrivateHand(Game game, Guid playerId)
    {
        var hand = game.Hands.FirstOrDefault(h => h.PlayerId == playerId)
            ?? throw new InvalidOperationException("Jogador não possui mão nesta partida.");

        return new PlayerHandDto(playerId, [.. hand.Cards.Select(ToCardDto)]);
    }

    public static RoomDto ToRoomDto(Room room) => new(
        room.Id,
        room.Code,
        room.HostPlayerId,
        room.Status.ToString(),
        [.. room.Players.Select(p => new PlayerDto(
            p.Id, p.Name, p.PawnId, p.IsHost, p.IsReady, p.ConnectionStatus.ToString()))]);

    public static BoardDto ToBoardDto(Board board) => new(
        Board.Width,
        Board.Height,
        [.. board.Cells.Select(c => new BoardCellDto(c.X, c.Y, c.Type.ToString(), c.LocationId))],
        [.. board.Locations.Select(l => new BoardLocationDto(
            l.Id,
            l.Name,
            [.. l.EntranceCells.Select(e => new PositionDto(e.X, e.Y))],
            l.SecretPassageToLocationId is not null))]);

    public static TurnDto ToTurnDto(TurnState turn) => new(
        turn.CurrentPlayerId,
        turn.Phase.ToString(),
        turn.TurnNumber,
        turn.DiceValue,
        [.. turn.TurnOrder]);

    public static SuggestionDto ToSuggestionDto(Suggestion suggestion) => new(
        suggestion.Id,
        suggestion.SuggestingPlayerId,
        suggestion.SuspectCardId,
        suggestion.LocationCardId,
        suggestion.WeaponCardId);

    public static PublicRefutationDto ToPublicRefutationDto(RefutationState state) => new(
        state.SuggestionId,
        state.IsResolved || state.CurrentIndex >= state.RefutationOrder.Count
            ? null
            : state.RefutationOrder[state.CurrentIndex],
        state.IsResolved,
        state.AllPlayersPassed,
        state.ShownByPlayerId);

    public static CardDto ToCardDto(Card card) => new(card.Id, card.Type.ToString(), card.Name);

    public static PublicHintDto ToPublicHintDto(Hint hint) =>
        new(hint.Id, hint.Type.ToString(), hint.Delivery, hint.Text, hint.TargetPlayerId);

    public static PrivateHintDto ToPrivateHintDto(Hint hint) => new(hint.Id, hint.Text);
}
