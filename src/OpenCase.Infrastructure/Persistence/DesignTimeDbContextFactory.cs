using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenCase.Infrastructure.Persistence;

/// <summary>
/// Usado pelo dotnet-ef para gerar migrations sem subir a aplicação.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OpenCaseDbContext>
{
    public OpenCaseDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5432;Database=opencase;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<OpenCaseDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new OpenCaseDbContext(options);
    }
}
