using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class FinalAccusationServiceTests
{
    private readonly FinalAccusationService _service = new();
    private readonly Game _game;
    private readonly Guid _accuser = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly GameSolution _solution;

    public FinalAccusationServiceTests()
    {
        _solution = new GameSolution
        {
            SuspectCardId = Guid.NewGuid(),
            LocationCardId = Guid.NewGuid(),
            WeaponCardId = Guid.NewGuid(),
        };
        _game = new Game { Status = GameStatus.InProgress, Solution = _solution };
        _game.TurnState.TurnOrder = [_accuser, _other];
        _game.TurnState.CurrentPlayerId = _accuser;
    }

    private Suggestion SetUnrefutedSuggestion(bool matchesSolution)
    {
        var suggestion = new Suggestion
        {
            GameId = _game.Id,
            SuggestingPlayerId = _accuser,
            SuspectCardId = matchesSolution ? _solution.SuspectCardId : Guid.NewGuid(),
            LocationCardId = matchesSolution ? _solution.LocationCardId : Guid.NewGuid(),
            WeaponCardId = matchesSolution ? _solution.WeaponCardId : Guid.NewGuid(),
            WasRefuted = false,
        };
        _game.LastUnrefutedSuggestion = suggestion;
        _game.TurnState.Phase = TurnPhase.FinalAccusation;
        return suggestion;
    }

    [Fact]
    public void MakeFinalAccusation_FailsWithoutUnrefutedSuggestion()
    {
        Assert.Throws<InvalidOperationException>(() => _service.MakeFinalAccusation(_game, _accuser));
    }

    [Fact]
    public void MakeFinalAccusation_OnlyTheSuggestingPlayerCanAccuse()
    {
        SetUnrefutedSuggestion(matchesSolution: true);

        Assert.Throws<InvalidOperationException>(() => _service.MakeFinalAccusation(_game, _other));
    }

    [Fact]
    public void MakeFinalAccusation_UsesTheUnrefutedSuggestionNotFrontendInput()
    {
        var suggestion = SetUnrefutedSuggestion(matchesSolution: false);

        var result = _service.MakeFinalAccusation(_game, _accuser);

        Assert.Equal(suggestion.SuspectCardId, result.Accusation.SuspectCardId);
        Assert.Equal(suggestion.LocationCardId, result.Accusation.LocationCardId);
        Assert.Equal(suggestion.WeaponCardId, result.Accusation.WeaponCardId);
    }

    [Fact]
    public void MakeFinalAccusation_CorrectAccusationEndsGameWithWinner()
    {
        SetUnrefutedSuggestion(matchesSolution: true);

        var result = _service.MakeFinalAccusation(_game, _accuser);

        Assert.True(result.IsCorrect);
        Assert.Equal(GameStatus.Finished, _game.Status);
        Assert.Equal(_accuser, _game.WinnerPlayerId);
        Assert.NotNull(_game.FinishedAt);
    }

    [Fact]
    public void MakeFinalAccusation_WrongAccusationPenalizesAndKeepsGameRunning()
    {
        SetUnrefutedSuggestion(matchesSolution: false);

        var result = _service.MakeFinalAccusation(_game, _accuser);

        Assert.False(result.IsCorrect);
        Assert.Equal(GameStatus.InProgress, _game.Status);
        Assert.Null(_game.WinnerPlayerId);
        Assert.Contains(_accuser, _game.TurnState.PlayersToSkip);
    }

    [Fact]
    public void MakeFinalAccusation_WrongAccusationDoesNotRevealSolution()
    {
        SetUnrefutedSuggestion(matchesSolution: false);

        var result = _service.MakeFinalAccusation(_game, _accuser);

        Assert.Null(result.RevealedSolution);
    }

    [Fact]
    public void MakeFinalAccusation_CorrectAccusationRevealsSolution()
    {
        SetUnrefutedSuggestion(matchesSolution: true);

        var result = _service.MakeFinalAccusation(_game, _accuser);

        Assert.Same(_solution, result.RevealedSolution);
    }

    [Fact]
    public void MakeFinalAccusation_CannotAccuseTwiceWithSameSuggestion()
    {
        SetUnrefutedSuggestion(matchesSolution: false);
        _service.MakeFinalAccusation(_game, _accuser);

        Assert.Throws<InvalidOperationException>(() => _service.MakeFinalAccusation(_game, _accuser));
    }
}
