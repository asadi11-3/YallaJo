using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("Places", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(300);

        builder.Property(x => x.Slug)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(300);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.PlaceType)
            .IsRequired()
            .HasConversion<int>();

        builder.OwnsOne(e => e.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
            loc.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);

            // Bounding-box pre-filter support for GetNearbyAsync.
            // Owned-navigation indexes must be configured from the owned builder.
            loc.HasIndex(l => new { l.Latitude, l.Longitude })
                .HasDatabaseName("IX_Places_Latitude_Longitude");
        });

        builder.Property(x => x.Address)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.City)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.Country)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.PostalCode)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(20);

        builder.Property(x => x.Phone)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.Website)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.AverageRating)
            .IsRequired()
            .HasPrecision(3, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.ReviewCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsWheelchairAccessible).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasAudioGuide).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.HasBrailleSignage).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.MetaTitle)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.MetaDescription)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.CategoryId)
            .IsRequired(false);

        builder.Property(x => x.TourCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedByUserId).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.PlaceTranslations)
            .WithOne(x => x.Place)
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.IsFeatured);
        builder.HasIndex(x => x.IsVerified);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.TourCount);

        // Composite owned-type index for geo bounding-box pre-filter used by GetNearbyAsync.
        // Latitude + Longitude let SQL Server range-scan the bounding box
        // before evaluating the expensive Haversine trig functions.
        // TODO: Replace with a SPATIAL INDEX on a geography computed column when
        // dataset grows beyond ~50k places (requires NetTopologySuite migration).
    }
}
