using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class EntityPopularitySnapshotConfiguration : IEntityTypeConfiguration<EntityPopularitySnapshot>
{
    public void Configure(EntityTypeBuilder<EntityPopularitySnapshot> builder)
    {
        builder.ToTable("EntityPopularitySnapshots", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Score).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.TakenAt });
    }
}
