using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracking.Domain.Entities;
using Tracking.Domain.Enums;

namespace Tracking.Infrastructure.Persistence.Configurations;

public class LiveTrackingSessionConfiguration : IEntityTypeConfiguration<LiveTrackingSession>
{
    public void Configure(EntityTypeBuilder<LiveTrackingSession> builder)
    {
        builder.ToTable("LiveTrackingSessions", "tracking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourBookingId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(SessionStatus.Active);

        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.EndedAt).IsRequired(false);
        builder.Property(x => x.LastLocationUpdate).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasMany(x => x.LocationSnapshots)
            .WithOne(x => x.LiveTrackingSession)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TourCheckpoints)
            .WithOne(x => x.LiveTrackingSession)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TourBookingId);
        builder.HasIndex(x => new { x.TourGuideId, x.Status });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
