using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracking.Domain.Entities;
using Tracking.Domain.Enums;

namespace Tracking.Infrastructure.Persistence.Configurations;

public class TourCheckpointConfiguration : IEntityTypeConfiguration<TourCheckpoint>
{
    public void Configure(EntityTypeBuilder<TourCheckpoint> builder)
    {
        builder.ToTable("TourCheckpoints", "tracking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SessionId).IsRequired();
        builder.Property(x => x.WaypointId).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(CheckpointStatus.NotReached);

        builder.Property(x => x.ReachedAt).IsRequired(false);

        builder.Property(x => x.Notes)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.LiveTrackingSession)
            .WithMany(x => x.TourCheckpoints)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.WaypointId }).IsUnique();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
