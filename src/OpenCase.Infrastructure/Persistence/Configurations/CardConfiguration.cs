using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenCase.Domain.Entities;

namespace OpenCase.Infrastructure.Persistence.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
    }
}

public class PawnConfiguration : IEntityTypeConfiguration<Pawn>
{
    public void Configure(EntityTypeBuilder<Pawn> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Color).HasMaxLength(20);
    }
}

public class BoardTemplateConfiguration : IEntityTypeConfiguration<BoardTemplate>
{
    public void Configure(EntityTypeBuilder<BoardTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Json).IsRequired();
    }
}
