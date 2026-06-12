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
    private static readonly string[] SuspectNames = CardCatalog.SuspectNames;
    private static readonly string[] WeaponNames = CardCatalog.WeaponNames;

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

        var storedPawns = await context.Pawns.ToDictionaryAsync(p => p.Id, cancellationToken);
        foreach (var pawn in RoomService.AvailablePawns)
        {
            if (storedPawns.TryGetValue(pawn.Id, out var stored))
            {
                stored.Name = pawn.Name;
                stored.Color = pawn.Color;
            }
            else
            {
                context.Pawns.Add(new Pawn
                {
                    Id = pawn.Id,
                    Name = pawn.Name,
                    Color = pawn.Color,
                });
            }
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
