using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class AccessibilityReviewConfiguration : IEntityTypeConfiguration<AccessibilityReview>
{
    public void Configure(EntityTypeBuilder<AccessibilityReview> builder)
    {
        builder.ToTable("AccessibilityReviews", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Ownership
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TargetType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.TargetId).IsRequired();

        // Content
        builder.Property(x => x.Rating).IsRequired().HasPrecision(2, 1);
        builder.Property(x => x.Title).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.Content).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.VisitDate).IsRequired(false);
        builder.Property(x => x.FeatureTypesCsv).IsRequired().HasMaxLength(500).HasDefaultValue(string.Empty);

        // Status / audit
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(AccessibilityReviewStatus.Published);
        builder.Property(x => x.LastEditedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // S-AR1: one accessibility review per (UserId, TargetType, TargetId) — unique filtered
        builder.HasIndex(x => new { x.UserId, x.TargetType, x.TargetId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_AccessibilityReviews_User_Target_Unique");

        builder.HasIndex(x => new { x.TargetType, x.TargetId })
            .HasDatabaseName("IX_AccessibilityReviews_TargetType_TargetId");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_AccessibilityReviews_UserId");
    }
}
