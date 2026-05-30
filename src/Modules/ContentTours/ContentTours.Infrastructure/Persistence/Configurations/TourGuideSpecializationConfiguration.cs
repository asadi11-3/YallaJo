using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public sealed class TourGuideSpecializationConfiguration : IEntityTypeConfiguration<TourGuideSpecialization>
{
    public void Configure(EntityTypeBuilder<TourGuideSpecialization> builder)
    {
        builder.ToTable("TourGuideSpecializations", "content_tours");

        builder.HasKey(specialization => new { specialization.TourGuideId, specialization.SpecializationId });

        builder.Property(specialization => specialization.TourGuideId).IsRequired();
        builder.Property(specialization => specialization.SpecializationId).IsRequired();

        builder.HasOne(specialization => specialization.TourGuide)
            .WithMany(guide => guide.Specializations)
            .HasForeignKey(specialization => specialization.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(specialization => !specialization.TourGuide.IsDeleted);
    }
}
