using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class GuideScheduleConfiguration : IEntityTypeConfiguration<GuideSchedule>
{
    public void Configure(EntityTypeBuilder<GuideSchedule> builder)
    {
        builder.ToTable("GuideSchedules", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.GuideTourOfferingId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.DayOfWeek).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.GuideTourOfferingId);
        builder.HasIndex(x => new { x.TourGuideId, x.TourId });
    }
}
