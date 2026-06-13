using Bunit;
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
            .Add(c => c.PawnColors, new Dictionary<Guid, string> { [playerId] = "#c9a227" }));

        Assert.Equal(4, cut.FindAll(".board-cell").Count);
        Assert.Single(cut.FindAll(".board-pawn"));
    }

    [Fact]
    public void BoardView_ClickingWalkableCellRaisesCallback()
    {
        PositionDto? clicked = null;
        var cut = Render<BoardView>(p => p
            .Add(c => c.Board, CreateSmallBoard())
            .Add(c => c.OnCellClick, (PositionDto pos) => clicked = pos));

        cut.Find("[data-cell='0-0']").Click();

        Assert.Equal(new PositionDto(0, 0), clicked);
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
