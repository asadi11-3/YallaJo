using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public class AccessibilityReviewConfiguration : IEntityTypeConfiguration<AccessibilityReview>
{
    public void Configure(EntityTypeBuilder<AccessibilityReview> builder)
    {
        builder.ToTable("AccessibilityReviews", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.WheelchairAccessible).IsRequired(false);
        builder.Property(x => x.VisualAidAvailable).IsRequired(false);
        builder.Property(x => x.HearingAidAvailable).IsRequired(false);
        builder.Property(x => x.AccessibilityRating).IsRequired(false).HasPrecision(3, 2);
        builder.Property(x => x.Comments).IsRequired(false).HasColumnType("nvarchar(max)");
        builder.Property(x => x.VisitDate).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
