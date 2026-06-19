using Bunit;
using OpenCase.Shared.Dtos;
using OpenCase.Web.Components.Game;

namespace OpenCase.Application.Tests;

public class NotesGridComponentTests : BunitContext
{
    private static readonly CardDto Suspect = new(Guid.NewGuid(), "Suspect", "Detetive Arthur Vale");
    private static readonly CardDto Location = new(Guid.NewGuid(), "Location", "Biblioteca");
    private static readonly CardDto Weapon = new(Guid.NewGuid(), "Weapon", "Castiçal");

    private IRenderedComponent<NotesGrid> RenderGrid(
        Dictionary<Guid, bool>? marked = null,
        Action<Guid>? onToggle = null)
    {
        return Render<NotesGrid>(parameters => parameters
            .Add(p => p.Suspects, [Suspect])
            .Add(p => p.Locations, [Location])
            .Add(p => p.Weapons, [Weapon])
            .Add(p => p.Marked, marked ?? [])
            .Add(p => p.OnToggle, onToggle ?? (_ => { })));
    }

    [Fact]
    public void RendersSeparateCategoryTabs()
    {
        var cut = RenderGrid();

        Assert.Contains("Detetive Arthur Vale", cut.Markup);
        Assert.DoesNotContain("Biblioteca", cut.Find(".notes-section").TextContent);
        Assert.DoesNotContain(Weapon.Name, cut.Find(".notes-section").TextContent);
        Assert.Equal(3, cut.FindAll(".notes-category").Count);
        Assert.Single(cut.FindAll(".notes-section"));
        Assert.Single(cut.FindAll(".notes-card-thumb img"));
        Assert.Contains("images/characters/arthur-vale.jpg", cut.Find(".notes-card-thumb img").GetAttribute("src"));
        Assert.Contains("active", cut.Find("[data-notes-category='suspects']").ClassList);
    }

    [Fact]
    public void ClickingCategoryShowsOnlyItsCards()
    {
        var cut = RenderGrid();

        cut.Find("[data-notes-category='locations']").Click();

        Assert.Contains("Biblioteca", cut.Find(".notes-section").TextContent);
        Assert.DoesNotContain("Detetive Arthur Vale", cut.Find(".notes-section").TextContent);
        Assert.Contains("images/cards/locations/biblioteca.png", cut.Find(".notes-card-thumb img").GetAttribute("src"));
        Assert.Contains("active", cut.Find("[data-notes-category='locations']").ClassList);
    }

    [Fact]
    public void MarkedCardShowsXAndUpdatesCategoryCount()
    {
        var cut = RenderGrid(marked: new Dictionary<Guid, bool> { [Suspect.Id] = true });

        var row = cut.Find($"[data-card-id='{Suspect.Id}']");
        Assert.Contains("X", row.TextContent);
        Assert.Contains("marked", row.ClassList);
        Assert.Contains("1/1", cut.Find("[data-notes-category='suspects']").TextContent);
    }

    [Fact]
    public void ClickingRowTogglesCard()
    {
        Guid? toggled = null;
        var cut = RenderGrid(onToggle: id => toggled = id);

        cut.Find("[data-notes-category='weapons']").Click();
        cut.Find($"[data-card-id='{Weapon.Id}']").Click();

        Assert.Contains("images/cards/weapons/candlestick.png", cut.Find(".notes-card-thumb img").GetAttribute("src"));
        Assert.Equal(Weapon.Id, toggled);
    }
}
