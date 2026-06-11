using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenCase.Domain.Entities;

namespace OpenCase.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(r => r.Code).IsUnique();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasMany(r => r.Players)
            .WithOne()
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ConnectionStatus).HasConversion<string>().HasMaxLength(20);
    }
}
