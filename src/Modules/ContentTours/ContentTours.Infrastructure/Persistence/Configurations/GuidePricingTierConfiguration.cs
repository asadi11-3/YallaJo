using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class GuidePricingTierConfiguration : IEntityTypeConfiguration<GuidePricingTier>
{
    public void Configure(EntityTypeBuilder<GuidePricingTier> builder)
    {
        builder.ToTable("GuidePricingTiers", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.GuideTourOfferingId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(512);
        builder.Property(x => x.MinParticipants).IsRequired();
        builder.Property(x => x.MaxParticipants).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        // Price is a Money value object — store as columns
        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("PriceAmount").HasPrecision(19, 4).IsRequired();
            price.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(x => x.GuideTourOfferingId);
        builder.HasIndex(x => new { x.TourGuideId, x.TourId });
    }
}
