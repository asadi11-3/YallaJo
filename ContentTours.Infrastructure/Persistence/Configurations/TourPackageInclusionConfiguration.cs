using ContentTours.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentTours.Infrastructure.Persistence.Configurations;

public class TourPackageInclusionConfiguration : IEntityTypeConfiguration<TourPackageInclusion>
{
    public void Configure(EntityTypeBuilder<TourPackageInclusion> builder)
    {
        builder.ToTable("TourPackageInclusions", "content_tours");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TourPackageId).IsRequired();

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.SortOrder)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.TourPackage)
            .WithMany(x => x.Inclusions)
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TourPackageId, x.Description })
            .IsUnique()
            .HasDatabaseName("UX_TourPackageInclusions_PackageId_Description");

        builder.HasQueryFilter(x => !x.TourPackage.IsDeleted);
    }
}
