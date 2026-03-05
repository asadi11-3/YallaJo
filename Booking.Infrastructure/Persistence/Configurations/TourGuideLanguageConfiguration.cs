using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class TourGuideLanguageConfiguration : IEntityTypeConfiguration<TourGuideLanguage>
{
    public void Configure(EntityTypeBuilder<TourGuideLanguage> builder)
    {
        builder.ToTable("TourGuideLanguages", "booking");

        builder.HasKey(x => new { x.TourGuideId, x.LanguageId });

        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.LanguageId).IsRequired();
        builder.Property(x => x.ProficiencyLevel).IsRequired().HasDefaultValue((byte)0);

        builder.HasOne(x => x.TourGuide)
            .WithMany(x => x.TourGuideLanguages)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
