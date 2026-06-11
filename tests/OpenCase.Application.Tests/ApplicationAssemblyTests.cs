namespace OpenCase.Application.Tests;

public class ApplicationAssemblyTests
{
    [Fact]
    public void ApplicationAssembly_ReferencesDomain()
    {
        var referenced = typeof(OpenCase.Application.AssemblyMarker).Assembly
            .GetReferencedAssemblies();

        Assert.Contains(referenced, a => a.Name == "OpenCase.Domain");
    }
}
