using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class TourBookingConfiguration : IEntityTypeConfiguration<TourBooking>
{
    public void Configure(EntityTypeBuilder<TourBooking> builder)
    {
        builder.ToTable("TourBookings", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired(false);
        builder.Property(x => x.ScheduledDate).IsRequired();
        builder.Property(x => x.StartTime).IsRequired(false);
        builder.Property(x => x.ParticipantCount).IsRequired().HasDefaultValue(1);
        builder.OwnsOne(e => e.TotalPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalPrice").HasPrecision(19, 4);
            money.Property(m => m.Currency).HasColumnName("TotalPriceCurrency").HasMaxLength(3).HasDefaultValue("JOD");
        });
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(0);
        builder.Property(x => x.SpecialRequests).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.CancellationReason).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.CancelledAt).IsRequired(false);
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.Property(x => x.ConfirmedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.JoinRequests)
            .WithOne(x => x.TourBooking)
            .HasForeignKey(x => x.TourBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.UserId, x.ScheduledDate });
        builder.HasIndex(x => x.Status);
    }
}
