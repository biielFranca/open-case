using System.Reflection;
using Bunit;
using OpenCase.Shared.Dtos;
using OpenCase.Web.Components.Lobby;
using OpenCase.Web.Hubs;

namespace OpenCase.Application.Tests;

public class AddBotButtonTests : BunitContext
{
    private static readonly List<PawnDto> Pawns =
        [new(Guid.NewGuid(), "Detetive Arthur Vale", "#287D78")];

    private IRenderedComponent<RoomSettingsPanel> RenderPanel(bool isHost, bool canAddBot = true, Action? onAddBot = null)
    {
        return Render<RoomSettingsPanel>(p => p
            .Add(c => c.AvailablePawns, Pawns)
            .Add(c => c.TakenPawnIds, [])
            .Add(c => c.IsHost, isHost)
            .Add(c => c.CanStart, false)
            .Add(c => c.CanAddBot, canAddBot)
            .Add(c => c.OnAddBot, onAddBot ?? (() => { })));
    }

    [Fact]
    public void AddBotButton_VisibleOnlyForHost()
    {
        Assert.NotEmpty(RenderPanel(isHost: true).FindAll(".add-bot-button"));
        Assert.Empty(RenderPanel(isHost: false).FindAll(".add-bot-button"));
    }

    [Fact]
    public void AddBotButton_DisabledWhenRoomCannotReceiveBots()
    {
        var cut = RenderPanel(isHost: true, canAddBot: false);

        Assert.True(cut.Find(".add-bot-button").HasAttribute("disabled"));
    }

    [Fact]
    public void AddBotButton_ClickRaisesCallback()
    {
        var clicked = false;
        var cut = RenderPanel(isHost: true, onAddBot: () => clicked = true);

        cut.Find(".add-bot-button").Click();

        Assert.True(clicked);
    }

    [Fact]
    public void GameHub_ExposesAddBotMethod()
    {
        var method = typeof(GameHub).GetMethod("AddBot", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);
    }
}
