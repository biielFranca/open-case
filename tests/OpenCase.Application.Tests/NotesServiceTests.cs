using OpenCase.Application.Services;

namespace OpenCase.Application.Tests;

public class NotesServiceTests
{
    private readonly NotesService _service = new();
    private readonly Guid _gameId = Guid.NewGuid();
    private readonly Guid _playerA = Guid.NewGuid();
    private readonly Guid _playerB = Guid.NewGuid();
    private readonly Guid _cardId = Guid.NewGuid();

    [Fact]
    public void GetNotes_StartsEmpty()
    {
        var notes = _service.GetNotes(_gameId, _playerA);

        Assert.Equal(_playerA, notes.PlayerId);
        Assert.Empty(notes.MarkedCards);
    }

    [Fact]
    public void ToggleMark_MarksAndUnmarksCard()
    {
        _service.ToggleMark(_gameId, _playerA, _cardId);
        Assert.True(_service.GetNotes(_gameId, _playerA).MarkedCards[_cardId]);

        _service.ToggleMark(_gameId, _playerA, _cardId);
        Assert.False(_service.GetNotes(_gameId, _playerA).MarkedCards[_cardId]);
    }

    [Fact]
    public void Notes_ArePrivatePerPlayer()
    {
        _service.ToggleMark(_gameId, _playerA, _cardId);

        Assert.Empty(_service.GetNotes(_gameId, _playerB).MarkedCards);
    }

    [Fact]
    public void Notes_AreSeparatedPerGame()
    {
        _service.ToggleMark(_gameId, _playerA, _cardId);

        Assert.Empty(_service.GetNotes(Guid.NewGuid(), _playerA).MarkedCards);
    }

    [Fact]
    public void Notes_PersistAcrossCalls()
    {
        var otherCard = Guid.NewGuid();
        _service.ToggleMark(_gameId, _playerA, _cardId);
        _service.ToggleMark(_gameId, _playerA, otherCard);

        var notes = _service.GetNotes(_gameId, _playerA);

        Assert.True(notes.MarkedCards[_cardId]);
        Assert.True(notes.MarkedCards[otherCard]);
    }
}
