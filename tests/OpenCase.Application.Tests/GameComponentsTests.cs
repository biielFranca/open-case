using Bunit;
using OpenCase.Application.Services;
using OpenCase.Shared.Dtos;
using OpenCase.Web.Components.Game;

namespace OpenCase.Application.Tests;

public class GameComponentsTests : BunitContext
{
    private static BoardDto CreateSmallBoard()
    {
        // Tabuleiro reduzido só para o teste do componente: 2x2.
        var locationId = Guid.NewGuid();
        return new BoardDto(2, 2,
            [
                new BoardCellDto(0, 0, "Path", null),
                new BoardCellDto(0, 1, "Wall", locationId),
                new BoardCellDto(1, 0, "Entrance", locationId),
                new BoardCellDto(1, 1, "Location", locationId),
            ],
            [new BoardLocationDto(locationId, "Biblioteca", [new PositionDto(1, 0)], true)]);
    }

    [Fact]
    public void BoardView_RendersOneCellPerCoordinateAndPawns()
    {
        var playerId = Guid.NewGuid();
        var cut = Render<BoardView>(p => p
            .Add(c => c.Board, CreateSmallBoard())
            .Add(c => c.PawnPositions, new Dictionary<Guid, PositionDto> { [playerId] = new(0, 0) })
            .Add(c => c.PawnColors, new Dictionary<Guid, string> { [playerId] = "#c9a227" })
            .Add(c => c.PawnCharacters, new Dictionary<Guid, PawnDto>
            {
                [playerId] = new(RoomService.AvailablePawns[0].Id, "Detetive Arthur Vale", "#c9a227"),
            }));

        Assert.Equal(4, cut.FindAll(".board-cell").Count);
        Assert.Single(cut.FindAll(".board-character"));
        Assert.Contains("character-3d", cut.Find(".board-character").ClassList);
        Assert.Contains("pawn-arthur", cut.Find(".board-character").ClassList);
        Assert.Single(cut.FindAll(".board-character > .character-shadow"));
        Assert.Single(cut.FindAll(".board-character > .character-figure"));
        Assert.Single(cut.FindAll(".board-character > .character-base"));
        Assert.Single(cut.FindAll(".character-figure > .character-portrait"));
    }

    [Fact]
    public void BoardView_RendersDistinctThreeDimensionalPawnForEveryCharacter()
    {
        var players = RoomService.AvailablePawns.Select(_ => Guid.NewGuid()).ToList();
        var positions = players.ToDictionary(playerId => playerId, _ => new PositionDto(0, 0));
        var colors = players.Zip(RoomService.AvailablePawns)
            .ToDictionary(pair => pair.First, pair => pair.Second.Color);
        var characters = players.Zip(RoomService.AvailablePawns)
            .ToDictionary(pair => pair.First, pair =>
                new PawnDto(pair.Second.Id, pair.Second.Name, pair.Second.Color));

        var cut = Render<BoardView>(parameters => parameters
            .Add(component => component.Board, CreateSmallBoard())
            .Add(component => component.PawnPositions, positions)
            .Add(component => component.PawnColors, colors)
            .Add(component => component.PawnCharacters, characters));

        Assert.Equal(12, cut.FindAll(".character-3d").Count);
        Assert.Equal(12, cut.FindAll(".character-3d").Select(pawn =>
            pawn.ClassList.Single(className => className.StartsWith("pawn-"))).Distinct().Count());
    }

    [Fact]
    public void BoardView_ClickingWalkableCellRaisesCallback()
    {
        PositionDto? clicked = null;
        var reachable = new PositionDto(0, 0);
        var cut = Render<BoardView>(p => p
            .Add(c => c.Board, CreateSmallBoard())
            .Add(c => c.ReachableDestinations, [reachable])
            .Add(c => c.OnCellClick, (PositionDto pos) => clicked = pos));

        cut.Find("[data-cell='0-0']").Click();

        Assert.Equal(reachable, clicked);
        Assert.Contains("reachable", cut.Find("[data-cell='0-0']").ClassList);
    }

    [Fact]
    public void BoardView_ClickingUnavailableCellDoesNotRaiseCallback()
    {
        PositionDto? clicked = null;
        var cut = Render<BoardView>(p => p
            .Add(c => c.Board, CreateSmallBoard())
            .Add(c => c.OnCellClick, (PositionDto pos) => clicked = pos));

        cut.Find("[data-cell='0-0']").Click();

        Assert.Null(clicked);
        Assert.DoesNotContain("clickable", cut.Find("[data-cell='0-0']").ClassList);
    }

    [Fact]
    public void BoardView_RendersLocationAsOneReachableSpace()
    {
        PositionDto? clicked = null;
        var board = CreateSmallBoard();
        var entrance = board.Locations[0].Entrances[0];
        var playerId = Guid.NewGuid();
        var cut = Render<BoardView>(p => p
            .Add(c => c.Board, board)
            .Add(c => c.ReachableDestinations, [entrance])
            .Add(c => c.PawnPositions, new Dictionary<Guid, PositionDto> { [playerId] = entrance })
            .Add(c => c.OnCellClick, (PositionDto position) => clicked = position));

        var room = cut.Find(".board-room-layer");
        room.Click();

        Assert.Contains("reachable", room.ClassList);
        Assert.Single(room.QuerySelectorAll(".board-room-character"));
        Assert.Empty(cut.Find("[data-cell='1-0']").QuerySelectorAll(".board-character"));
        Assert.DoesNotContain("reachable", cut.Find("[data-cell='1-0']").ClassList);
        Assert.Equal(entrance, clicked);
    }

    [Fact]
    public void BoardMovementPaths_ReturnsEarlyEntrances()
    {
        var board = CreateSmallBoard();

        var paths = BoardMovementPaths.FindLegalPaths(board, new PositionDto(0, 0), 2);

        Assert.True(paths.TryGetValue(new PositionDto(1, 0), out var entrancePath));
        Assert.Single(entrancePath);
        Assert.False(paths.ContainsKey(new PositionDto(0, 0)));
    }

    [Fact]
    public void BoardMovementPaths_ReturnsOnlyPathCellsReachedWithExactDiceValue()
    {
        var locationId = Guid.NewGuid();
        var board = new BoardDto(4, 1,
            [
                new BoardCellDto(0, 0, "Path", null),
                new BoardCellDto(1, 0, "Path", null),
                new BoardCellDto(2, 0, "Path", null),
                new BoardCellDto(3, 0, "Entrance", locationId),
            ],
            [new BoardLocationDto(locationId, "Biblioteca", [new PositionDto(3, 0)], false)]);

        var paths = BoardMovementPaths.FindLegalPaths(board, new PositionDto(0, 0), 2);

        Assert.Equal(2, paths[new PositionDto(2, 0)].Count);
        Assert.False(paths.ContainsKey(new PositionDto(1, 0)));
        Assert.False(paths.ContainsKey(new PositionDto(3, 0)));
    }

    [Fact]
    public void PlayerHand_RendersOneCardPerDto()
    {
        var cards = new List<CardDto>
        {
            new(Guid.NewGuid(), "Suspect", "Coronel Mostarda"),
            new(Guid.NewGuid(), "Weapon", "Castiçal"),
        };

        var cut = Render<PlayerHand>(p => p.Add(c => c.Cards, cards));

        Assert.Equal(2, cut.FindAll(".hand-card").Count);
        Assert.Equal(2, cut.FindAll(".hand-card-image").Count);
        Assert.Contains("Coronel Mostarda", cut.Markup);
        Assert.Contains("images/characters/raul-ferraz.jpg", cut.Markup);
        Assert.Contains("images/cards/weapons/candlestick.png", cut.Markup);
    }

    [Fact]
    public void GamePlayerSeat_ShowsCharacterPortraitAndTurnState()
    {
        var player = new PlayerDto(Guid.NewGuid(), "Biel", Guid.NewGuid(), true, true, "Connected");
        var pawn = new PawnDto(player.PawnId!.Value, "Detetive Arthur Vale", "#287D78");

        var cut = Render<GamePlayerSeat>(p => p
            .Add(c => c.Player, player)
            .Add(c => c.Pawn, pawn)
            .Add(c => c.SeatNumber, 3)
            .Add(c => c.IsCurrentTurn, true));

        Assert.Contains("Biel", cut.Markup);
        Assert.Contains("Arthur Vale", cut.Markup);
        Assert.Contains("images/characters/arthur-vale.jpg", cut.Markup);
        Assert.Contains("current-turn", cut.Find(".game-player-seat").ClassList);
        Assert.Contains("seat-3", cut.Find(".game-player-seat").ClassList);
    }

    [Fact]
    public void TurnPanel_ShowsCurrentPlayerAndPhase()
    {
        var playerId = Guid.NewGuid();
        var turn = new TurnDto(playerId, "Move", 3, 7, [playerId]);

        var cut = Render<TurnPanel>(p => p
            .Add(c => c.Turn, turn)
            .Add(c => c.PlayerNames, new Dictionary<Guid, string> { [playerId] = "Biel" })
            .Add(c => c.MyPlayerId, playerId));

        Assert.Contains("Biel", cut.Markup);
        Assert.Contains("sua vez", cut.Markup.ToLowerInvariant());
    }

    [Fact]
    public void DicePanel_RollButtonDisabledWhenNotMyTurn()
    {
        var cut = Render<DicePanel>(p => p
            .Add(c => c.CanRoll, false)
            .Add(c => c.DiceValue, 9));

        Assert.True(cut.Find(".roll-button").HasAttribute("disabled"));
        Assert.Contains("9", cut.Find(".dice-value").TextContent);
    }

    [Fact]
    public void SuggestionModal_RequiresSuspectAndWeaponBeforeConfirm()
    {
        var suspect = new CardDto(Guid.NewGuid(), "Suspect", "Coronel Mostarda");
        var weapon = new CardDto(Guid.NewGuid(), "Weapon", "Castiçal");
        (Guid, Guid)? confirmed = null;

        var cut = Render<SuggestionModal>(p => p
            .Add(c => c.Suspects, [suspect])
            .Add(c => c.Weapons, [weapon])
            .Add(c => c.LocationName, "Biblioteca")
            .Add(c => c.OnConfirm, ((Guid s, Guid w) pick) => confirmed = pick));

        Assert.True(cut.Find(".confirm-button").HasAttribute("disabled"));

        cut.Find($"[data-card-id='{suspect.Id}']").Click();
        cut.Find($"[data-card-id='{weapon.Id}']").Click();
        cut.Find(".confirm-button").Click();

        Assert.Equal((suspect.Id, weapon.Id), confirmed);
    }

    [Fact]
    public void HintPopup_ShowsTextAndType()
    {
        var cut = Render<HintPopup>(p => p
            .Add(c => c.Hint, new PublicHintDto(Guid.NewGuid(), "Noise", "Passos no corredor...")));

        Assert.Contains("Passos no corredor", cut.Markup);
        Assert.Contains("hint-noise", cut.Find(".hint-popup").ClassList);
    }
}
