using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourChildFacilityConfiguration : IEntityTypeConfiguration<TourChildFacility>
{
    public void Configure(EntityTypeBuilder<TourChildFacility> builder)
    {
        builder.ToTable("TourChildFacilities", "content_tours");

        // Composite key — no surrogate Id. Replace-set semantics are owned by Tour.UpdateChildrenInfo.
        builder.HasKey(x => new { x.TourId, x.Facility });

        builder.Property(x => x.TourId)
            .IsRequired();

        builder.Property(x => x.Facility)
            .IsRequired()
            .HasConversion<byte>();

        builder.HasOne(x => x.Tour)
            .WithMany(x => x.ChildFacilities)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        // Inherit Tour's soft-delete query filter so child rows disappear when the parent tour is soft-deleted.
        builder.HasQueryFilter(x => !x.Tour.IsDeleted);
    }
}
