using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("Tours", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ShortDescription)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(x => x.Difficulty)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.MaxGroupSize).IsRequired();
        builder.Property(x => x.MinAge).IsRequired(false);

        builder.OwnsOne(e => e.BasePrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("BasePrice").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("BasePriceCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsUnicode(false);

        builder.OwnsOne(e => e.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
            loc.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);
        });

        builder.OwnsOne(e => e.MeetingPoint, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("MeetingPointLatitude").HasPrecision(10, 8);
            loc.Property(l => l.Longitude).HasColumnName("MeetingPointLongitude").HasPrecision(11, 8);
        });

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue((int)TourStatus.Draft);

        builder.Property(x => x.AverageRating)
            .IsRequired()
            .HasPrecision(3, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.ReviewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.BookingCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsInstantBooking)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CancellationPolicyHours)
            .IsRequired()
            .HasDefaultValue(24);

        builder.Property(x => x.MetaTitle)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.MetaDescription)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedByUserId).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.TourTranslations)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TourSchedules)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TourWaypoints)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TourPricingTiers)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TourPackages)
            .WithOne(x => x.Tour)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
