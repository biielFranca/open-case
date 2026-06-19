using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenCase.Domain.Entities;

namespace OpenCase.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);

        // A solução fica em colunas próprias e NUNCA deve ser projetada em DTOs públicos.
        builder.OwnsOne(g => g.Solution);

        ConfigureJson(builder, g => g.AllCards);
        ConfigureJson(builder, g => g.Hands);
        ConfigureJson(builder, g => g.TurnState);
        ConfigureJson(builder, g => g.Board);
        ConfigureJson(builder, g => g.PawnPositions);
        ConfigureJson(builder, g => g.PlayerSuspects);
        ConfigureJsonNullable(builder, g => g.ActiveSuggestion);
        ConfigureJsonNullable(builder, g => g.ActiveRefutation);
        ConfigureJsonNullable(builder, g => g.LastUnrefutedSuggestion);

        builder.HasMany(g => g.Events)
            .WithOne()
            .HasForeignKey(e => e.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureJson<T>(
        EntityTypeBuilder<Game> builder,
        System.Linq.Expressions.Expression<Func<Game, T>> property) where T : new()
    {
        builder.Property(property)
            .HasConversion(OpenCaseDbContext.JsonConverter<T>(), OpenCaseDbContext.JsonComparer<T>());
    }

    private static void ConfigureJsonNullable<T>(
        EntityTypeBuilder<Game> builder,
        System.Linq.Expressions.Expression<Func<Game, T?>> property) where T : class, new()
    {
        builder.Property(property)
            .HasConversion(OpenCaseDbContext.JsonConverter<T>()!, OpenCaseDbContext.JsonComparer<T>());
    }
}

public class GameEventConfiguration : IEntityTypeConfiguration<GameEvent>
{
    public void Configure(EntityTypeBuilder<GameEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasMaxLength(60).IsRequired();
    }
}

public class HintConfiguration : IEntityTypeConfiguration<Hint>
{
    public void Configure(EntityTypeBuilder<Hint> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Delivery).HasMaxLength(20);
        builder.Property(h => h.Text).HasMaxLength(500);
    }
}
