using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public class WeatherDailyBudgetConfiguration : IEntityTypeConfiguration<WeatherDailyBudget>
{
    public void Configure(EntityTypeBuilder<WeatherDailyBudget> builder)
    {
        builder.ToTable("WeatherDailyBudget", "content_seo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.CallsUsed).IsRequired();
        builder.Property(x => x.DailyLimit).IsRequired();
        builder.Property(x => x.AlertSentAt).IsRequired(false);
        builder.HasIndex(x => x.Date).IsUnique().HasDatabaseName("UX_WeatherDailyBudget_Date");
    }
}
