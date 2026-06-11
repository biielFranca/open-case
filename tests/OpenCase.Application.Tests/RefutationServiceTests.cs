using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class RefutationServiceTests
{
    private readonly RefutationService _service = new();
    private readonly Game _game;
    private readonly Suggestion _suggestion;
    private readonly Guid _playerA = Guid.NewGuid();
    private readonly Guid _playerB = Guid.NewGuid(); // acusador
    private readonly Guid _playerC = Guid.NewGuid();
    private readonly Guid _playerD = Guid.NewGuid();
    private readonly Card _suspect = new() { Type = CardType.Suspect, Name = "Suspeito 1" };
    private readonly Card _location = new() { Type = CardType.Location, Name = "Local 1" };
    private readonly Card _weapon = new() { Type = CardType.Weapon, Name = "Arma 1" };
    private readonly Card _unrelated = new() { Type = CardType.Weapon, Name = "Arma 2" };

    public RefutationServiceTests()
    {
        _game = new Game { Status = GameStatus.InProgress };
        _game.TurnState.TurnOrder = [_playerA, _playerB, _playerC, _playerD];
        _game.TurnState.CurrentPlayerId = _playerB;
        _game.AllCards = [_suspect, _location, _weapon, _unrelated];
        _game.Hands =
        [
            new PlayerHand { PlayerId = _playerA, Cards = [_unrelated] },
            new PlayerHand { PlayerId = _playerB, Cards = [] },
            new PlayerHand { PlayerId = _playerC, Cards = [_weapon, _location] },
            new PlayerHand { PlayerId = _playerD, Cards = [] },
        ];

        _suggestion = new Suggestion
        {
            GameId = _game.Id,
            SuggestingPlayerId = _playerB,
            SuspectCardId = _suspect.Id,
            LocationCardId = _location.Id,
            WeaponCardId = _weapon.Id,
        };
        _game.ActiveSuggestion = _suggestion;
    }

    [Fact]
    public void CreateRefutationState_StartsAtPreviousPlayerAndFollowsReverseOrder()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);

        // Acusador é B; anterior é A; ordem reversa: A, D, C.
        Assert.Equal([_playerA, _playerD, _playerC], state.RefutationOrder);
        Assert.Equal(_playerA, _service.GetCurrentRefutingPlayer(state));
        Assert.DoesNotContain(_playerB, state.RefutationOrder);
    }

    [Fact]
    public void GetCardsAvailableToShow_ReturnsOnlyCardsMatchingTheSuggestion()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);

        Assert.Empty(_service.GetCardsAvailableToShow(_game, state, _playerA));
        Assert.Equal(
            new[] { _weapon.Id, _location.Id }.OrderBy(id => id),
            _service.GetCardsAvailableToShow(_game, state, _playerC).Select(c => c.Id).OrderBy(id => id));
    }

    [Fact]
    public void PassRefutation_OnlyCurrentPlayerCanPass()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);

        Assert.Throws<InvalidOperationException>(() => _service.PassRefutation(_game, state, _playerC));
    }

    [Fact]
    public void PassRefutation_PlayerWithMatchingCardCannotPass()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);
        _service.PassRefutation(_game, state, _playerA); // A não tem carta, passa
        _service.PassRefutation(_game, state, _playerD); // D não tem carta, passa

        Assert.Throws<InvalidOperationException>(() => _service.PassRefutation(_game, state, _playerC));
    }

    [Fact]
    public void PassRefutation_WhenEveryonePassesFinalAccusationIsUnlocked()
    {
        _game.Hands.First(h => h.PlayerId == _playerC).Cards.Clear();
        var state = _service.CreateRefutationState(_game, _suggestion);

        _service.PassRefutation(_game, state, _playerA);
        _service.PassRefutation(_game, state, _playerD);
        _service.PassRefutation(_game, state, _playerC);

        Assert.True(state.AllPlayersPassed);
        Assert.True(state.IsResolved);
        Assert.False(_suggestion.WasRefuted);
        Assert.Same(_suggestion, _game.LastUnrefutedSuggestion);
        Assert.Equal(TurnPhase.FinalAccusation, _game.TurnState.Phase);
    }

    [Fact]
    public void ShowCard_OnlyCurrentPlayerWithMatchingCardCanShow()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);

        Assert.Throws<InvalidOperationException>(() => _service.ShowCard(_game, state, _playerC, _weapon.Id));
        Assert.Throws<InvalidOperationException>(() => _service.ShowCard(_game, state, _playerA, _unrelated.Id));
    }

    [Fact]
    public void ShowCard_ResolvesRefutationAndReturnsPrivateResultForAccuserOnly()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);
        _service.PassRefutation(_game, state, _playerA);
        _service.PassRefutation(_game, state, _playerD);

        var shown = _service.ShowCard(_game, state, _playerC, _weapon.Id);

        Assert.True(state.IsResolved);
        Assert.False(state.AllPlayersPassed);
        Assert.True(_suggestion.WasRefuted);
        Assert.Null(_game.LastUnrefutedSuggestion);
        Assert.Equal(_playerB, shown.AccuserPlayerId);
        Assert.Equal(_playerC, shown.ShownByPlayerId);
        Assert.Equal(_weapon.Id, shown.CardId);
    }

    [Fact]
    public void ShowCard_AfterResolutionNoFurtherActionsAreAllowed()
    {
        var state = _service.CreateRefutationState(_game, _suggestion);
        _service.PassRefutation(_game, state, _playerA);
        _service.PassRefutation(_game, state, _playerD);
        _service.ShowCard(_game, state, _playerC, _weapon.Id);

        Assert.Throws<InvalidOperationException>(() => _service.PassRefutation(_game, state, _playerC));
        Assert.Null(_service.GetCurrentRefutingPlayer(state));
    }
}
