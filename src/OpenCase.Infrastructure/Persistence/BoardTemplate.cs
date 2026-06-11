namespace OpenCase.Infrastructure.Persistence;

/// <summary>
/// Mapa serializado usado como base para novas partidas.
/// </summary>
public class BoardTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}
