using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.BusinessId).IsRequired();
        builder.Property(x => x.AvailabilitySlotId).IsRequired(false);
        builder.Property(x => x.ServiceItemId).IsRequired(false);
        builder.Property(x => x.ReservationDate).IsRequired();
        builder.Property(x => x.ReservationTime).IsRequired();
        builder.Property(x => x.PartySize).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.TotalPrice).HasPrecision(19, 4);
        builder.Property(x => x.TotalPriceCurrency).HasMaxLength(3);
        builder.Property(x => x.Currency).HasMaxLength(3).IsUnicode(false);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.SpecialRequests).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.ConfirmedAt).IsRequired(false);
        builder.Property(x => x.CancellationReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.CancelledAt).IsRequired(false);
        builder.Property(x => x.CompletedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.BusinessId, x.ReservationDate, x.ReservationTime });
        builder.HasIndex(x => x.Status);
    }
}
