using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class HolidayCalendarConfiguration : IEntityTypeConfiguration<HolidayCalendar>
{
    public void Configure(EntityTypeBuilder<HolidayCalendar> builder)
    {
        builder.ToTable("HolidayCalendar", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HolidayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BoostRulesJson).HasMaxLength(2000);

        builder.HasIndex(x => new { x.Year, x.IsActive });
        builder.HasIndex(x => new { x.StartDate, x.EndDate });
    }
}
