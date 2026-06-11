namespace OpenCase.Shared.Dtos;

// ─── DTOs privados: enviados apenas ao jogador dono da informação ───

public record PlayerHandDto(Guid PlayerId, List<CardDto> Cards);

public record PrivateCardShownDto(Guid ShownByPlayerId, CardDto Card);

public record PrivateHintDto(Guid Id, string Text);

public record NotesDto(Guid PlayerId, Dictionary<Guid, bool> MarkedCards);
