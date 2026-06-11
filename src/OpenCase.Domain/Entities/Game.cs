using OpenCase.Domain.Enums;

namespace OpenCase.Domain.Entities;

public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public GameStatus Status { get; set; } = GameStatus.WaitingToStart;
    public GameSolution? Solution { get; set; }
    public List<Card> AllCards { get; set; } = [];
    public List<PlayerHand> Hands { get; set; } = [];
    public TurnState TurnState { get; set; } = new();
    public Board Board { get; set; } = new();
    public Dictionary<Guid, BoardPosition> PawnPositions { get; set; } = [];
    public Suggestion? ActiveSuggestion { get; set; }
    public RefutationState? ActiveRefutation { get; set; }

    // Último palpite que ninguém refutou — base da acusação final.
    public Suggestion? LastUnrefutedSuggestion { get; set; }
    public Guid? WinnerPlayerId { get; set; }
    public List<GameEvent> Events { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
}
