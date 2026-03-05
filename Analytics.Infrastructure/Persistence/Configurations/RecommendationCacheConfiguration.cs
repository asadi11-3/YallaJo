using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public class RecommendationCacheConfiguration : IEntityTypeConfiguration<RecommendationCache>
{
    public void Configure(EntityTypeBuilder<RecommendationCache> builder)
    {
        builder.ToTable("RecommendationCaches", "analytics");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.Score)
            .IsRequired()
            .HasPrecision(10, 4);

        builder.Property(x => x.Reason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.GeneratedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.EntityType, x.Score })
            .IsDescending(false, false, true);
    }
}
