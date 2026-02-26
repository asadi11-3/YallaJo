using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class PackageBookingConfiguration : IEntityTypeConfiguration<PackageBooking>
{
    public void Configure(EntityTypeBuilder<PackageBooking> builder)
    {
        builder.ToTable("PackageBookings", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TourPackageId).IsRequired();
        builder.Property(x => x.BookingDate).IsRequired();
        builder.Property(x => x.ParticipantCount).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TotalPrice).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(0);
        builder.Property(x => x.SpecialRequests).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.CancellationReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.CancelledAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.UserId, x.BookingDate });
        builder.HasIndex(x => x.Status);
    }
}
