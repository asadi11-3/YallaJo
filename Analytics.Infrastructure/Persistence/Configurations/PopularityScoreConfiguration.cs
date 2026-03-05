using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public class PopularityScoreConfiguration : IEntityTypeConfiguration<PopularityScore>
{
    public void Configure(EntityTypeBuilder<PopularityScore> builder)
    {
        builder.ToTable("PopularityScores", "analytics");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.TrendingScore)
            .IsRequired()
            .HasPrecision(10, 4)
            .HasDefaultValue(0m);

        builder.Property(x => x.ViewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.BookmarkCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ShareCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.BookingCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReviewScore)
            .IsRequired()
            .HasPrecision(3, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.LastCalculatedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();
    }
}
