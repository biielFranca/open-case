using Bunit;
using OpenCase.Shared.Dtos;
using OpenCase.Web.Components.Game;

namespace OpenCase.Application.Tests;

public class NotesGridComponentTests : BunitContext
{
    private static readonly CardDto Suspect = new(Guid.NewGuid(), "Suspect", "Coronel Mostarda");
    private static readonly CardDto Location = new(Guid.NewGuid(), "Location", "Biblioteca");
    private static readonly CardDto Weapon = new(Guid.NewGuid(), "Weapon", "Castiçal");

    private IRenderedComponent<NotesGrid> RenderGrid(Dictionary<Guid, bool>? marked = null, Action<Guid>? onToggle = null)
    {
        return Render<NotesGrid>(parameters => parameters
            .Add(p => p.Suspects, [Suspect])
            .Add(p => p.Locations, [Location])
            .Add(p => p.Weapons, [Weapon])
            .Add(p => p.Marked, marked ?? [])
            .Add(p => p.OnToggle, onToggle ?? (_ => { })));
    }

    [Fact]
    public void RendersSectionsForSuspectsLocationsAndWeapons()
    {
        var cut = RenderGrid();

        Assert.Contains("Coronel Mostarda", cut.Markup);
        Assert.Contains("Biblioteca", cut.Markup);
        Assert.Contains("Castiçal", cut.Markup);
        Assert.Equal(3, cut.FindAll(".notes-section").Count);
    }

    [Fact]
    public void MarkedCardShowsX()
    {
        var cut = RenderGrid(marked: new Dictionary<Guid, bool> { [Suspect.Id] = true });

        var row = cut.Find($"[data-card-id='{Suspect.Id}']");
        Assert.Contains("X", row.TextContent);
        Assert.Contains("marked", row.ClassList);
    }

    [Fact]
    public void ClickingRowTogglesCard()
    {
        Guid? toggled = null;
        var cut = RenderGrid(onToggle: id => toggled = id);

        cut.Find($"[data-card-id='{Weapon.Id}']").Click();

        Assert.Equal(Weapon.Id, toggled);
    }
}
