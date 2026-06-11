using System.Collections.Concurrent;
using OpenCase.Shared.Dtos;

namespace OpenCase.Application.Services;

/// <summary>
/// Notas privadas de investigação. Nunca são sincronizadas entre jogadores.
/// </summary>
public class NotesService
{
    private readonly ConcurrentDictionary<(Guid GameId, Guid PlayerId), Dictionary<Guid, bool>> _notes = new();

    public NotesDto GetNotes(Guid gameId, Guid playerId)
    {
        var marks = _notes.GetOrAdd((gameId, playerId), _ => []);
        return new NotesDto(playerId, new Dictionary<Guid, bool>(marks));
    }

    public NotesDto ToggleMark(Guid gameId, Guid playerId, Guid cardId)
    {
        var marks = _notes.GetOrAdd((gameId, playerId), _ => []);
        marks[cardId] = !marks.GetValueOrDefault(cardId);
        return GetNotes(gameId, playerId);
    }
}
