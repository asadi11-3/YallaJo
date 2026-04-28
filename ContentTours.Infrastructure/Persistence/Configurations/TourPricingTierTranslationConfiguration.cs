using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class TourPricingTierTranslationConfiguration : IEntityTypeConfiguration<TourPricingTierTranslation>
{
    public void Configure(EntityTypeBuilder<TourPricingTierTranslation> builder)
    {
        builder.ToTable("TourPricingTierTranslations", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourPricingTierId).IsRequired();

        builder.Property(x => x.LanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .IsUnicode(false);

        builder.HasIndex(x => new { x.TourPricingTierId, x.LanguageCode })
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.TourPricingTier)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.TourPricingTierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.TourPricingTier.Tour.IsDeleted);
    }
}
