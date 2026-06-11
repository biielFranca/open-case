using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenCase.Application.Services;
using OpenCase.Domain.Entities;
using OpenCase.Domain.Enums;

namespace OpenCase.Infrastructure.Persistence;

/// <summary>
/// Seed inicial: 8 suspeitos, 12 locais, 8 armas, 8 peões e o mapa 20x20 padrão.
/// </summary>
public static class DataSeeder
{
    private static readonly string[] SuspectNames =
    [
        "Coronel Mostarda", "Dona Violeta", "Professor Black", "Senhorita Rosa",
        "Doutor Marinho", "Madame Café", "Capitão Cinza", "Jovem Verde",
    ];

    private static readonly string[] WeaponNames =
    [
        "Castiçal", "Adaga", "Corda", "Revólver",
        "Cano de Chumbo", "Chave Inglesa", "Veneno", "Abridor de Cartas",
    ];

    public static async Task SeedAsync(OpenCaseDbContext context, CancellationToken cancellationToken = default)
    {
        var boardService = new BoardService();
        var board = boardService.CreateDefaultBoard();

        if (!await context.Cards.AnyAsync(cancellationToken))
        {
            context.Cards.AddRange(SuspectNames.Select(n => new Card { Type = CardType.Suspect, Name = n }));
            context.Cards.AddRange(board.Locations.Select(l => new Card { Type = CardType.Location, Name = l.Name }));
            context.Cards.AddRange(WeaponNames.Select(n => new Card { Type = CardType.Weapon, Name = n }));
        }

        if (!await context.Pawns.AnyAsync(cancellationToken))
        {
            context.Pawns.AddRange(RoomService.AvailablePawns.Select(p => new Pawn
            {
                Id = p.Id,
                Name = p.Name,
                Color = p.Color,
            }));
        }

        if (!await context.BoardTemplates.AnyAsync(cancellationToken))
        {
            context.BoardTemplates.Add(new BoardTemplate
            {
                Name = "Mansão Open Case (20x20)",
                Json = JsonSerializer.Serialize(board),
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
