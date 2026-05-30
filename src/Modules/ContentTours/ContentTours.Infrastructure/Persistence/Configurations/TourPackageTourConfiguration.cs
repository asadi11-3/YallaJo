using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

/// <summary>
/// Junction between <see cref="TourPackage"/> and <see cref="Tour"/>.
/// Composite PK = (TourPackageId, TourId).
/// </summary>
public class TourPackageTourConfiguration : IEntityTypeConfiguration<TourPackageTour>
{
    public void Configure(EntityTypeBuilder<TourPackageTour> builder)
    {
        builder.ToTable("TourPackageTours", "content_tours");

        // Composite key — natural uniqueness for the bundle membership.
        builder.HasKey(x => new { x.TourPackageId, x.TourId });

        builder.Property(x => x.TourPackageId).IsRequired();
        builder.Property(x => x.TourId).IsRequired();

        builder.HasOne(x => x.TourPackage)
            .WithMany(p => p.IncludedTours)
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tour)
            .WithMany()
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TourId);

        builder.HasQueryFilter(x => !x.Tour.IsDeleted && !x.TourPackage.IsDeleted);
    }
}
