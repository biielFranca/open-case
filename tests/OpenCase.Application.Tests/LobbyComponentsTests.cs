using Bunit;
using OpenCase.Shared.Dtos;
using OpenCase.Web.Components.Lobby;

namespace OpenCase.Application.Tests;

public class LobbyComponentsTests : BunitContext
{
    private static PlayerDto CreatePlayer(bool isHost = false, bool isReady = false, Guid? pawnId = null) =>
        new(Guid.NewGuid(), "Biel", pawnId, isHost, isReady, "Connected");

    [Fact]
    public void PlayerLobbyCard_ShowsNameHostBadgeAndReadyState()
    {
        var player = CreatePlayer(isHost: true, isReady: true);
        var pawn = new PawnDto(Guid.NewGuid(), "Detetive Arthur Vale", "#287D78");

        var cut = Render<PlayerLobbyCard>(p => p
            .Add(c => c.Player, player)
            .Add(c => c.Pawn, pawn));

        Assert.Contains("Biel", cut.Markup);
        Assert.Contains("Arthur Vale", cut.Markup);
        Assert.Contains("images/characters/arthur-vale.jpg", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".host-badge"));
        Assert.NotEmpty(cut.FindAll(".ready-badge"));
    }

    [Fact]
    public void PlayerLobbyCard_WithoutReadyShowsWaitingState()
    {
        var cut = Render<PlayerLobbyCard>(p => p.Add(c => c.Player, CreatePlayer()));

        Assert.Empty(cut.FindAll(".ready-badge"));
        Assert.NotEmpty(cut.FindAll(".waiting-badge"));
    }

    [Fact]
    public void EmptyLobbyCard_ShowsPlaceholder()
    {
        var cut = Render<EmptyLobbyCard>();

        Assert.NotEmpty(cut.FindAll(".empty-card"));
        Assert.Contains("Aguardando", cut.Markup);
    }

    [Fact]
    public void RoomCodeBox_ShowsCodeAndCopiesOnClick()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<RoomCodeBox>(p => p.Add(c => c.Code, "ABC123"));
        cut.Find(".copy-button").Click();

        Assert.Contains("ABC123", cut.Markup);
        var invocation = JSInterop.Invocations.Single(i => i.Identifier == "navigator.clipboard.writeText");
        Assert.Equal("ABC123", invocation.Arguments[0]);
    }

    [Fact]
    public void RoomSettingsPanel_StartButtonOnlyEnabledForHostWhenAllReady()
    {
        var pawns = new List<PawnDto> { new(Guid.NewGuid(), "Detetive Arthur Vale", "#287D78") };

        var cut = Render<RoomSettingsPanel>(p => p
            .Add(c => c.AvailablePawns, pawns)
            .Add(c => c.TakenPawnIds, [])
            .Add(c => c.IsHost, false)
            .Add(c => c.CanStart, false));

        Assert.True(cut.Find(".start-button").HasAttribute("disabled"));

        cut = Render<RoomSettingsPanel>(p => p
            .Add(c => c.AvailablePawns, pawns)
            .Add(c => c.TakenPawnIds, [])
            .Add(c => c.IsHost, true)
            .Add(c => c.CanStart, true));

        Assert.False(cut.Find(".start-button").HasAttribute("disabled"));
    }

    [Fact]
    public void RoomSettingsPanel_TakenPawnIsDisabled()
    {
        var taken = new PawnDto(Guid.NewGuid(), "Detetive Arthur Vale", "#287D78");
        var free = new PawnDto(Guid.NewGuid(), "Condessa Helena Vesper", "#6E2638");

        var cut = Render<RoomSettingsPanel>(p => p
            .Add(c => c.AvailablePawns, [taken, free])
            .Add(c => c.TakenPawnIds, [taken.Id])
            .Add(c => c.IsHost, true)
            .Add(c => c.CanStart, false));

        Assert.True(cut.Find($"[data-pawn-id='{taken.Id}']").HasAttribute("disabled"));
        Assert.False(cut.Find($"[data-pawn-id='{free.Id}']").HasAttribute("disabled"));
        Assert.Equal(2, cut.FindAll(".pawn-option-portrait img").Count);
    }
}
