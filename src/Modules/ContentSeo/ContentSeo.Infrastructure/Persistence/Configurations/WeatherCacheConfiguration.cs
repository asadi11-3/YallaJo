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

        // PDF §11: PlaceId is now optional (cache is keyed by coordinates, not PlaceId).
        builder.Property(x => x.PlaceId).IsRequired(false);

        // PDF §11: composite cache key = (RoundedLatitude, RoundedLongitude, ForecastDate).
        builder.Property(x => x.RoundedLatitude)
            .IsRequired()
            .HasPrecision(7, 2);

        builder.Property(x => x.RoundedLongitude)
            .IsRequired()
            .HasPrecision(8, 2);

        builder.Property(x => x.ForecastDate).IsRequired();

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

        // Renamed from Forecast → ForecastJson; nvarchar(max) for 7-day JSON array.
        builder.Property(x => x.ForecastJson)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.FetchedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // PDF §11: unique composite key (lat, lng, date) — nearby tours share cache.
        builder.HasIndex(x => new { x.RoundedLatitude, x.RoundedLongitude, x.ForecastDate })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_WeatherCache_Lat_Lng_Date");

        builder.HasIndex(x => x.PlaceId)
            .HasFilter("[PlaceId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_WeatherCache_PlaceId");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_WeatherCache_ExpiresAt");
    }
}
