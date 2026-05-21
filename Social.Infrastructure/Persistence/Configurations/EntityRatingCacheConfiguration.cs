using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class EntityRatingCacheConfiguration : IEntityTypeConfiguration<EntityRatingCache>
{
    public void Configure(EntityTypeBuilder<EntityRatingCache> builder)
    {
        builder.ToTable("EntityRatingCaches", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TargetType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TargetId).IsRequired();

        builder.Property(x => x.AverageRating).IsRequired().HasPrecision(3, 2);
        builder.Property(x => x.ReviewCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.BayesianScore).IsRequired().HasPrecision(5, 4);
        builder.Property(x => x.LastRecalculatedAt).IsRequired();

        // Audit columns (from AuditableEntity)
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // One cache row per (TargetType, TargetId)
        builder.HasIndex(x => new { x.TargetType, x.TargetId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_EntityRatingCaches_TargetType_TargetId_Unique");

        builder.HasIndex(x => x.LastRecalculatedAt)
            .HasDatabaseName("IX_EntityRatingCaches_LastRecalculatedAt");
    }
}
