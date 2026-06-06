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

        // Identity / relationships
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.GuideId).IsRequired();
        builder.Property(x => x.AvailabilitySlotId).IsRequired();
        builder.Property(x => x.ParticipantCount).IsRequired().HasDefaultValue(1);

        // Guide / private tour
        builder.Property(x => x.IsPrivate).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.JoinedFromBookingId).IsRequired(false);

        // Business reference key
        builder.Property(x => x.Reference)
            .IsRequired()
            .HasMaxLength(32)
            .IsUnicode(false);

        // Pricing (decimal precision 19,4 per B-R1)
        builder.Property(x => x.Subtotal).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.DiscountAmount).IsRequired().HasPrecision(19, 4).HasDefaultValue(0m);
        builder.Property(x => x.LoyaltyAmount).IsRequired().HasPrecision(19, 4).HasDefaultValue(0m);
        builder.Property(x => x.TotalAmount).IsRequired().HasPrecision(19, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).IsUnicode(false);

        // Commission (provider-facing; stamped at creation)
        builder.Property(x => x.CommissionRate).IsRequired().HasPrecision(9, 4);
        builder.Property(x => x.CommissionAmount).IsRequired().HasPrecision(19, 4);

        // Snapshots (JSON columns, NVARCHAR(MAX))
        builder.Property(x => x.LineItemsJson).IsRequired();
        builder.Property(x => x.RefundPolicySnapshot).IsRequired();

        // Booking mode
        builder.Property(x => x.IsInstantBooking).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.PaymentExpiresAt).IsRequired();

        // State machine
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();

        // Confirmation lifecycle
        builder.Property(x => x.ConfirmedAt).IsRequired(false);
        builder.Property(x => x.ConfirmationSource).IsRequired(false).HasConversion<int?>();

        // Rejection lifecycle (TASK 5)
        builder.Property(x => x.RejectedAt).IsRequired(false);
        builder.Property(x => x.RejectionReason).IsRequired(false).HasMaxLength(500);

        // Cancellation lifecycle (TASK 5)
        builder.Property(x => x.CancelledAt).IsRequired(false);
        builder.Property(x => x.CancellationSource).IsRequired(false).HasConversion<int?>();
        builder.Property(x => x.CancellationReason).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.RefundAmount).IsRequired(false).HasPrecision(19, 4);

        // Completion lifecycle (TASK 5)
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.Property(x => x.CompletedByUserId).IsRequired(false);

        // Phase 3 (G4a): Dispute lifecycle
        builder.Property(x => x.DisputedAt).IsRequired(false);
        builder.Property(x => x.DisputeOpenedByUserId).IsRequired(false);
        builder.Property(x => x.DisputeReason).IsRequired(false).HasMaxLength(2000);
        builder.Property(x => x.ResolvedAt).IsRequired(false);
        builder.Property(x => x.ResolvedByAdminId).IsRequired(false);
        builder.Property(x => x.ResolutionNotes).IsRequired(false).HasMaxLength(2000);

        // Optional user input
        builder.Property(x => x.SpecialRequests).IsRequired(false).HasMaxLength(2000);

        // Audit
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Navigation
        builder.HasMany(x => x.JoinRequests)
            .WithOne(x => x.TourBooking)
            .HasForeignKey(x => x.TourBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Filters + indexes
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Reference).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.TourId, x.Status });
        builder.HasIndex(x => new { x.ProviderId, x.Status });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Status, x.UpdatedAt })
            .HasDatabaseName("IX_TourBookings_Status_UpdatedAt");
        builder.HasIndex(x => x.AvailabilitySlotId);
    }
}
