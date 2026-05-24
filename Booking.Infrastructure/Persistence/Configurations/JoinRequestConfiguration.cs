using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class JoinRequestConfiguration : IEntityTypeConfiguration<JoinRequest>
{
    public void Configure(EntityTypeBuilder<JoinRequest> builder)
    {
        builder.ToTable("JoinRequests", "booking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourBookingId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.Message).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.ParticipantCount).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.RespondedAt).IsRequired(false);
        builder.Property(x => x.ResponseMessage).IsRequired(false).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.TourBooking)
            .WithMany(x => x.JoinRequests)
            .HasForeignKey(x => x.TourBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.TourBookingId)
            .HasDatabaseName("IX_JoinRequests_TourBookingId");

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.TourBookingId, x.UserId })
            .IsUnique()
            .HasFilter("[Status] = 0 AND [IsDeleted] = 0")
            .HasDatabaseName("UX_JoinRequests_Pending_TourBookingId_UserId");
    }
}
