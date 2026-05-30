using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class TourGuideSpecializationConfiguration : IEntityTypeConfiguration<TourGuideSpecialization>
{
    public void Configure(EntityTypeBuilder<TourGuideSpecialization> builder)
    {
        builder.ToTable("TourGuideSpecializations", "booking");

        builder.HasKey(x => new { x.TourGuideId, x.SpecializationId });

        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.SpecializationId).IsRequired();

        builder.HasOne(x => x.TourGuide)
            .WithMany(x => x.TourGuideSpecializations)
            .HasForeignKey(x => x.TourGuideId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.TourGuide.IsDeleted);
    }
}
