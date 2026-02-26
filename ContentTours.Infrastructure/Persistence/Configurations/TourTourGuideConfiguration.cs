using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourTourGuideConfiguration : IEntityTypeConfiguration<TourTourGuide>
{
    public void Configure(EntityTypeBuilder<TourTourGuide> builder)
    {
        builder.ToTable("TourTourGuides", "content_tours");

        builder.HasKey(x => new { x.TourId, x.TourGuideId });

        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.TourGuideId).IsRequired();
        builder.Property(x => x.IsPrimary).IsRequired().HasDefaultValue(false);

        builder.HasOne<Tour>()
            .WithMany()
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
