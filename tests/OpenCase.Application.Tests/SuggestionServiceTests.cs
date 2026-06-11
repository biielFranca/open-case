using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Application.Tests;

public class SuggestionServiceTests
{
    private readonly BoardService _boardService = new();
    private readonly SuggestionService _service;
    private readonly Game _game;
    private readonly Guid _accuser = Guid.NewGuid();
    private readonly Guid _cited = Guid.NewGuid();
    private readonly Card _suspectOfCited;
    private readonly Card _suspectOfAccuser;
    private readonly Card _weapon;
    private readonly BoardLocation _location;

    public SuggestionServiceTests()
    {
        _service = new SuggestionService(_boardService);
        var board = _boardService.CreateDefaultBoard();
        _game = new Game { Board = board, Status = GameStatus.InProgress };

        _game.AllCards =
        [
            .. board.Locations.Select(l => new Card { Type = CardType.Location, Name = l.Name }),
            _suspectOfCited = new Card { Type = CardType.Suspect, Name = "Suspeito 1" },
            _suspectOfAccuser = new Card { Type = CardType.Suspect, Name = "Suspeito 2" },
            _weapon = new Card { Type = CardType.Weapon, Name = "Arma 1" },
        ];

        _game.PlayerSuspects[_cited] = _suspectOfCited.Id;
        _game.PlayerSuspects[_accuser] = _suspectOfAccuser.Id;

        _location = board.Locations[0];
        var entrance = _location.EntranceCells[0];
        _game.PawnPositions[_accuser] = new BoardPosition(entrance.X, entrance.Y);
        _game.PawnPositions[_cited] = new BoardPosition(4, 5); // corredor
        _game.TurnState.CurrentPlayerId = _accuser;
        _game.TurnState.TurnOrder = [_accuser, _cited];
    }

    [Fact]
    public void CreateSuggestion_FailsWhenPlayerIsOutsideLocation()
    {
        _game.PawnPositions[_accuser] = new BoardPosition(4, 5);

        Assert.Throws<InvalidOperationException>(() =>
            _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _weapon.Id));
    }

    [Fact]
    public void CreateSuggestion_LocationIsDefinedByBackendFromPlayerPosition()
    {
        var suggestion = _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _weapon.Id);

        var locationCard = _game.AllCards.Single(c => c.Id == suggestion.LocationCardId);
        Assert.Equal(CardType.Location, locationCard.Type);
        Assert.Equal(_location.Name, locationCard.Name);
    }

    [Fact]
    public void CreateSuggestion_MovesCitedPawnToAccuserPosition()
    {
        _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _weapon.Id);

        Assert.Equal(_game.PawnPositions[_accuser], _game.PawnPositions[_cited]);
    }

    [Fact]
    public void CreateSuggestion_CanCiteSelf()
    {
        var suggestion = _service.CreateSuggestion(_game, _accuser, _suspectOfAccuser.Id, _weapon.Id);

        Assert.Equal(_suspectOfAccuser.Id, suggestion.SuspectCardId);
    }

    [Fact]
    public void CreateSuggestion_CanBeRepeated()
    {
        _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _weapon.Id);
        _game.TurnState.Phase = TurnPhase.Suggestion;

        var repeated = _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _weapon.Id);

        Assert.NotNull(repeated);
    }

    [Fact]
    public void CreateSuggestion_StartsRefutationPhaseAndStoresActiveSuggestion()
    {
        var suggestion = _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _weapon.Id);

        Assert.Equal(TurnPhase.Refutation, _game.TurnState.Phase);
        Assert.Same(suggestion, _game.ActiveSuggestion);
        Assert.Equal(_accuser, suggestion.SuggestingPlayerId);
    }

    [Fact]
    public void CreateSuggestion_RejectsWrongCardTypes()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _service.CreateSuggestion(_game, _accuser, _weapon.Id, _weapon.Id));
        Assert.Throws<InvalidOperationException>(() =>
            _service.CreateSuggestion(_game, _accuser, _suspectOfCited.Id, _suspectOfAccuser.Id));
    }
}
