using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class TourGuideConfiguration : IEntityTypeConfiguration<TourGuide>
{
    public void Configure(EntityTypeBuilder<TourGuide> builder)
    {
        builder.ToTable("TourGuides", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Bio)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.YearsOfExperience).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AverageRating).IsRequired().HasPrecision(3, 2).HasDefaultValue(0m);
        builder.Property(x => x.ReviewCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CompletedTourCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IsVerified).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.HourlyRate).HasPrecision(19, 4).IsRequired(false);
        builder.Property(x => x.Currency).IsRequired(false).HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.ResponseTimeMinutes).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.TourGuideLanguages)
            .WithOne(x => x.TourGuide)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TourGuideSpecializations)
            .WithOne(x => x.TourGuide)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AvailabilitySlots)
            .WithOne(x => x.TourGuide)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProviderDocuments)
            .WithOne(x => x.TourGuide)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
