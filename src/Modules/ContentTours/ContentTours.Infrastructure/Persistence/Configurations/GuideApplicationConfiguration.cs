using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class GuideApplicationConfiguration : IEntityTypeConfiguration<GuideApplication>
{
    public void Configure(EntityTypeBuilder<GuideApplication> builder)
    {
        builder.ToTable("GuideApplications", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.GuideUserId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.ProposedScheduleJson).IsRequired(false).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ProposedBasePrice).IsRequired(false).HasPrecision(19, 4);
        builder.Property(x => x.RelevantExperience).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.ResubmissionCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ReviewedByAdminId).IsRequired(false);
        builder.Property(x => x.ReviewedAt).IsRequired(false);
        builder.Property(x => x.RejectionReason).IsRequired(false).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.TourId, x.TourGuideId });
        builder.HasIndex(x => x.GuideUserId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
