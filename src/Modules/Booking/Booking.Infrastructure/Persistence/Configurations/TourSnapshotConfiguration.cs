using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

internal sealed class TourSnapshotConfiguration : IEntityTypeConfiguration<TourSnapshot>
{
    public void Configure(EntityTypeBuilder<TourSnapshot> builder)
    {
        builder.ToTable("TourSnapshots", "booking");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.BasePrice).IsRequired().HasPrecision(18, 4);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsApproved).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsInstantBooking).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RefundPolicyId).IsRequired(false);
        builder.Property(x => x.RefundPolicySnapshotJson).IsRequired(false).HasMaxLength(4000);
        builder.Property(x => x.LastUpdatedAt).IsRequired();

        builder.HasIndex(x => x.TourId).IsUnique().HasDatabaseName("IX_TourSnapshots_TourId");
        builder.HasIndex(x => x.ProviderId).HasDatabaseName("IX_TourSnapshots_ProviderId");
    }
}
