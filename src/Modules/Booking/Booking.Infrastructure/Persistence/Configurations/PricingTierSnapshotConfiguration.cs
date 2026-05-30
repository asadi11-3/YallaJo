using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

internal sealed class PricingTierSnapshotConfiguration : IEntityTypeConfiguration<PricingTierSnapshot>
{
    public void Configure(EntityTypeBuilder<PricingTierSnapshot> builder)
    {
        builder.ToTable("PricingTierSnapshots", "booking");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.TierType).IsRequired();
        builder.Property(x => x.Price).IsRequired().HasPrecision(18, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.HasIndex(x => new { x.TourId, x.TierType })
            .IsUnique()
            .HasDatabaseName("IX_PricingTierSnapshots_TourId_TierType");
    }
}
