namespace OpenCase.Application.Tests;

public class ReadmeTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !File.Exists(Path.Combine(dir.FullName, "OpenCase.sln"))
               && !File.Exists(Path.Combine(dir.FullName, "OpenCase.slnx")))
        {
            dir = dir.Parent!;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }

    [Fact]
    public void Readme_ExistsAndDeclaresRealStatus()
    {
        var readme = File.ReadAllText(Path.Combine(FindRepoRoot(), "README.md"));

        Assert.Contains("Status", readme);
        Assert.Contains("em desenvolvimento", readme);
        // Não pode alegar que está completo.
        Assert.DoesNotContain("Status: completo", readme, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("## Regras")]
    [InlineData("## Stack")]
    [InlineData("## Arquitetura")]
    [InlineData("## Como rodar")]
    [InlineData("## Como testar")]
    [InlineData("## Roadmap")]
    [InlineData("## Licença")]
    [InlineData("## Originalidade")]
    public void Readme_HasRequiredSection(string section)
    {
        var readme = File.ReadAllText(Path.Combine(FindRepoRoot(), "README.md"));

        Assert.Contains(section, readme);
    }

    [Fact]
    public void License_FileExists()
    {
        Assert.True(File.Exists(Path.Combine(FindRepoRoot(), "LICENSE")));
    }
}
