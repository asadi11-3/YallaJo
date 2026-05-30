using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class EntityAttributeSnapshotConfiguration : IEntityTypeConfiguration<EntityAttributeSnapshot>
{
    public void Configure(EntityTypeBuilder<EntityAttributeSnapshot> builder)
    {
        builder.ToTable("EntityAttributeSnapshots", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityKind).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(300);
        builder.Property(x => x.BasePriceAmount).HasPrecision(19, 4);
        builder.Property(x => x.BasePriceCurrency).HasMaxLength(3);
        builder.Property(x => x.SalePrice).HasPrecision(19, 4);
        builder.Property(x => x.AverageRating).HasPrecision(3, 2);
        builder.Property(x => x.LocationLatitude).HasPrecision(9, 6);
        builder.Property(x => x.LocationLongitude).HasPrecision(9, 6);
        builder.Property(x => x.BusinessType).HasMaxLength(100);
        builder.Property(x => x.Difficulty).HasMaxLength(50);
        builder.Property(x => x.IsHalal).IsRequired(false);
        builder.Property(x => x.HasVegetarianOptions).IsRequired(false);
        builder.Property(x => x.HasAlcoholFreeArea).IsRequired(false);
        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.CategoryIdsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.UpcomingCapacity).IsRequired(false);
        builder.Property(x => x.UpcomingBookings).IsRequired(false);
        builder.Property(x => x.IsPhotogenicHotspot).HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.EntityKind, x.EntityId }).IsUnique();
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.PlaceId);
        builder.HasIndex(x => new { x.LocationLatitude, x.LocationLongitude });
    }
}
