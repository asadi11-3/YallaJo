using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourWaypointConfiguration : IEntityTypeConfiguration<TourWaypoint>
{
    public void Configure(EntityTypeBuilder<TourWaypoint> builder)
    {
        builder.ToTable("TourWaypoints", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.OwnsOne(e => e.Location, loc =>
        {
            loc.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
            loc.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);
        });

        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired(false);
        builder.Property(x => x.WaypointType).IsRequired().HasDefaultValue((byte)0);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Tour)
            .WithMany(x => x.TourWaypoints)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
