using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class TourGuideConfiguration : IEntityTypeConfiguration<TourGuide>
{
    public void Configure(EntityTypeBuilder<TourGuide> builder)
    {
        builder.ToTable("TourGuides", "content_tours");

        builder.HasKey(guide => guide.Id);
        builder.Property(guide => guide.Id).ValueGeneratedNever();

        builder.Property(guide => guide.UserId).IsRequired();
        builder.Property(guide => guide.Slug).IsRequired().HasMaxLength(128);
        builder.Property(guide => guide.DisplayName).IsRequired().HasMaxLength(256);
        builder.Property(guide => guide.Bio).IsRequired().HasMaxLength(2000);
        builder.Property(guide => guide.AvatarUrl).IsRequired(false).HasMaxLength(500);
        builder.Property(guide => guide.CoverImageUrl).IsRequired(false).HasMaxLength(500);
        builder.Property(guide => guide.ApplicationId).IsRequired(false);
        builder.Property(guide => guide.LinkedProviderId).IsRequired(false);
        builder.Property(guide => guide.TrustTier).IsRequired().HasConversion<int>();
        builder.Property(guide => guide.Status).IsRequired().HasConversion<int>();
        builder.Property(guide => guide.SuspensionReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(guide => guide.SuspendedByAdminId).IsRequired(false);
        builder.Property(guide => guide.SuspendedAt).IsRequired(false);
        builder.Property(guide => guide.YearsOfExperience).IsRequired().HasDefaultValue(0);
        builder.Property(guide => guide.HasFirstAid).IsRequired().HasDefaultValue(false);
        builder.Property(guide => guide.MoTALicenseNumber).HasMaxLength(128).IsRequired(false);
        builder.Property(guide => guide.AverageRating).IsRequired().HasPrecision(3, 2).HasDefaultValue(0m);
        builder.Property(guide => guide.ReviewCount).IsRequired().HasDefaultValue(0);
        builder.Property(guide => guide.CompletedTourCount).IsRequired().HasDefaultValue(0);
        builder.Property(guide => guide.ReportCount).IsRequired().HasDefaultValue(0);
        builder.Property(guide => guide.CommissionRate).IsRequired(false).HasPrecision(5, 4);

        // IsActive removed - use Status instead
        builder.Ignore("IsActive");

        builder.Property(guide => guide.CreatedAt).IsRequired();
        builder.Property(guide => guide.UpdatedAt).IsRequired(false);
        builder.Property(guide => guide.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(guide => guide.DeletedAt).IsRequired(false);
        builder.Property(guide => guide.RowVersion).IsRowVersion();

        builder.HasMany(guide => guide.Languages)
            .WithOne(language => language.TourGuide)
            .HasForeignKey(language => language.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(guide => guide.Specializations)
            .WithOne(specialization => specialization.TourGuide)
            .HasForeignKey(specialization => specialization.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(guide => !guide.IsDeleted);
        builder.HasIndex(guide => guide.UserId).IsUnique();
        builder.HasIndex(guide => guide.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(guide => guide.Status);
    }
}
