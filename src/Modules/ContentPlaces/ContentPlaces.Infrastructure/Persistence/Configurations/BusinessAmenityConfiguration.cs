using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class BusinessAmenityConfiguration : IEntityTypeConfiguration<BusinessAmenity>
{
    public void Configure(EntityTypeBuilder<BusinessAmenity> builder)
    {
        builder.ToTable("BusinessAmenities", "content_places");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.BusinessId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Icon)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.SortOrder).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasOne(x => x.Business)
            .WithMany(x => x.Amenities)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.Business.IsDeleted);

        builder.HasIndex(x => x.BusinessId);
    }
}
