using ContentPlaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentPlaces.Infrastructure.Persistence.Configurations;

public class PlaceBusinessConfiguration : IEntityTypeConfiguration<PlaceBusiness>
{
    public void Configure(EntityTypeBuilder<PlaceBusiness> builder)
    {
        builder.ToTable("PlaceBusinesses", "content_places");

        builder.HasKey(x => new { x.PlaceId, x.BusinessId });

        builder.Property(x => x.PlaceId).IsRequired();
        builder.Property(x => x.BusinessId).IsRequired();

        builder.HasIndex(x => x.BusinessId);
    }
}
