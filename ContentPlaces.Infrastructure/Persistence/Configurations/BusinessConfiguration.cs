using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses", "content_places");

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

        builder.Property(x => x.BusinessType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.PlaceId).IsRequired(false);

        builder.OwnsOne(e => e.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
            loc.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);
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

        builder.Property(x => x.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsFeatured)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.OwnerId).IsRequired();

        builder.Property(x => x.MetaTitle)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(200);

        builder.Property(x => x.MetaDescription)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(500);

        builder.Property(x => x.LicenseNumber)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.TaxId)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.RejectionReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.SubscriptionTier).IsRequired(false).HasConversion<int?>();
        builder.HasIndex(x => x.Status).HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.BusinessTranslations)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BusinessHours)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ServiceItems)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Staff)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Amenities)
            .WithOne(x => x.Business)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.PlaceId);
        builder.HasIndex(x => x.OwnerId);
    }
}
