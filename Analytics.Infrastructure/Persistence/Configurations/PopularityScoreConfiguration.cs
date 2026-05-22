using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class PopularityScoreConfiguration : IEntityTypeConfiguration<PopularityScore>
{
    public void Configure(EntityTypeBuilder<PopularityScore> builder)
    {
        builder.ToTable("PopularityScores", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Score).HasPrecision(18, 4);
        builder.Property(x => x.AverageRatingSnapshot).HasPrecision(3, 2);
        builder.Property(x => x.CategoryRankPercentile).HasPrecision(5, 4);
        builder.Property(x => x.IsStale).HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.EntityType, x.TrendingRank });
        builder.HasIndex(x => x.IsStale);
    }
}
