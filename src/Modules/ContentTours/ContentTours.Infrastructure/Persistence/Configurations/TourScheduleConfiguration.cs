using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourScheduleConfiguration : IEntityTypeConfiguration<TourSchedule>
{
    public void Configure(EntityTypeBuilder<TourSchedule> builder)
    {
        builder.ToTable("TourSchedules", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourId).IsRequired();

        builder.Property(x => x.DayOfWeek)
            .IsRequired()
            .HasColumnType("tinyint");

        // TimeOnly stored as time(7) in SQL Server via TimeSpan conversion.
        builder.Property(x => x.StartTime)
            .IsRequired()
            .HasConversion(
                v => v.ToTimeSpan(),
                v => TimeOnly.FromTimeSpan(v))
            .HasColumnType("time(7)");

        builder.Property(x => x.EndTime)
            .IsRequired(false)
            .HasConversion(
                v => v.HasValue ? (TimeSpan?)v.Value.ToTimeSpan() : null,
                v => v.HasValue ? TimeOnly.FromTimeSpan(v.Value) : (TimeOnly?)null)
            .HasColumnType("time(7)");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Tour)
            .WithMany(x => x.TourSchedules)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index supports idempotency check and overlap queries.
        builder.HasIndex(x => new { x.TourId, x.DayOfWeek, x.StartTime })
            .HasDatabaseName("IX_TourSchedules_TourId_DayOfWeek_StartTime");

        builder.HasQueryFilter(x => !x.Tour.IsDeleted);
    }
}
