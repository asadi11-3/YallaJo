using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracking.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Tracking.Infrastructure.Persistence.Configurations;

public class LocationSnapshotConfiguration : IEntityTypeConfiguration<LocationSnapshot>
{
    public void Configure(EntityTypeBuilder<LocationSnapshot> builder)
    {
        builder.ToTable("LocationSnapshots", "tracking");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SessionId).IsRequired();

        builder.OwnsOne(e => e.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
            loc.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);
        });

        builder.Property(x => x.Accuracy)
            .IsRequired()
            .HasColumnType("float");

        builder.Property(x => x.Speed)
            .IsRequired(false)
            .HasColumnType("float");

        builder.Property(x => x.Heading)
            .IsRequired(false)
            .HasColumnType("float");

        builder.Property(x => x.Altitude)
            .IsRequired(false)
            .HasColumnType("float");

        builder.Property(x => x.CapturedAt).IsRequired();

        builder.HasOne(x => x.LiveTrackingSession)
            .WithMany(x => x.LocationSnapshots)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SessionId, x.CapturedAt });
    }
}
