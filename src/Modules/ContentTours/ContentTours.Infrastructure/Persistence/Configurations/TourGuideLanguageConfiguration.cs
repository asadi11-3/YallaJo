using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class TourGuideLanguageConfiguration : IEntityTypeConfiguration<TourGuideLanguage>
{
    public void Configure(EntityTypeBuilder<TourGuideLanguage> builder)
    {
        builder.ToTable("TourGuideLanguages", "content_tours");

        builder.HasKey(language => new { language.TourGuideId, language.LanguageId });

        builder.Property(language => language.TourGuideId).IsRequired();
        builder.Property(language => language.LanguageId).IsRequired();
        builder.Property(language => language.Proficiency).IsRequired().HasMaxLength(32);

        builder.HasOne(language => language.TourGuide)
            .WithMany(guide => guide.Languages)
            .HasForeignKey(language => language.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(language => !language.TourGuide.IsDeleted);
    }
}
