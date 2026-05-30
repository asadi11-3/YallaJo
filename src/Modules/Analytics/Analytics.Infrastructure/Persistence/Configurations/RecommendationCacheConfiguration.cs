using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class RecommendationCacheConfiguration : IEntityTypeConfiguration<RecommendationCache>
{
    public void Configure(EntityTypeBuilder<RecommendationCache> builder)
    {
        builder.ToTable("RecommendationCaches", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired(false);
        builder.Property(x => x.BatchId).IsRequired();
        builder.Property(x => x.EntityKind).HasConversion<byte>().IsRequired();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Score).HasPrecision(10, 4).IsRequired();
        builder.Property(x => x.Position).IsRequired();
        builder.Property(x => x.SignalsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.GeneratedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasIndex(x => x.BatchId);
        builder.HasIndex(x => new { x.UserId, x.EntityKind, x.Score })
            .IsDescending(false, false, true);
        builder.HasIndex(x => new { x.BatchId, x.Position }).IsUnique();
    }
}
