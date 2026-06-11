namespace OpenCase.Shared.Dtos;

// ─── DTOs públicos: visíveis a todos os jogadores da sala ───
// Regra de ouro: nada aqui pode conter a solução secreta, cartas de outros
// jogadores, carta mostrada em refutação ou notas privadas.

public record PawnDto(Guid Id, string Name, string Color);

public record PlayerDto(
    Guid Id,
    string Name,
    Guid? PawnId,
    bool IsHost,
    bool IsReady,
    string ConnectionStatus);

public record RoomDto(
    Guid Id,
    string Code,
    Guid HostPlayerId,
    string Status,
    List<PlayerDto> Players);

public record CardDto(Guid Id, string Type, string Name);

public record BoardCellDto(int X, int Y, string Type, Guid? LocationId);

public record BoardLocationDto(Guid Id, string Name, List<PositionDto> Entrances, bool HasSecretPassage);

public record PositionDto(int X, int Y);

public record BoardDto(int Width, int Height, List<BoardCellDto> Cells, List<BoardLocationDto> Locations);

public record TurnDto(
    Guid CurrentPlayerId,
    string Phase,
    int TurnNumber,
    int? DiceValue,
    List<Guid> TurnOrder);

public record SuggestionDto(
    Guid Id,
    Guid SuggestingPlayerId,
    Guid SuspectCardId,
    Guid LocationCardId,
    Guid WeaponCardId);

public record PublicRefutationDto(
    Guid SuggestionId,
    Guid? CurrentRefutingPlayerId,
    bool IsResolved,
    bool AllPlayersPassed,
    Guid? RefutedByPlayerId);

public record PublicHintDto(Guid Id, string Type, string Text);

public record PublicGameStateDto(
    Guid GameId,
    Guid RoomId,
    string Status,
    TurnDto Turn,
    Dictionary<Guid, PositionDto> PawnPositions,
    Dictionary<Guid, int> CardCounts,
    SuggestionDto? ActiveSuggestion,
    PublicRefutationDto? ActiveRefutation,
    Guid? WinnerPlayerId);
