using ContentSeo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentSeo.Infrastructure.Persistence.Configurations;

public class WeatherCacheConfiguration : IEntityTypeConfiguration<WeatherCache>
{
    public void Configure(EntityTypeBuilder<WeatherCache> builder)
    {
        builder.ToTable("WeatherCache", "content_seo");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PlaceId).IsRequired();

        builder.Property(x => x.Temperature)
            .IsRequired(false)
            .HasPrecision(5, 2);

        builder.Property(x => x.FeelsLike)
            .IsRequired(false)
            .HasPrecision(5, 2);

        builder.Property(x => x.Humidity).IsRequired(false);

        builder.Property(x => x.WindSpeed)
            .IsRequired(false)
            .HasPrecision(5, 2);

        builder.Property(x => x.WindDirection).IsRequired(false);

        builder.Property(x => x.Condition)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.Icon)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.UvIndex)
            .IsRequired(false)
            .HasPrecision(4, 2);

        builder.Property(x => x.Forecast)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.FetchedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => new { x.PlaceId, x.FetchedAt });
    }
}
