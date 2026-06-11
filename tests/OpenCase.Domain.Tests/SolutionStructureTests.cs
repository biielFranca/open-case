namespace OpenCase.Domain.Tests;

public class SolutionStructureTests
{
    [Fact]
    public void DomainAssembly_CanBeLoaded()
    {
        var assembly = typeof(OpenCase.Domain.AssemblyMarker).Assembly;

        Assert.Equal("OpenCase.Domain", assembly.GetName().Name);
    }
}
