using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class IngestDebounceMarkerConfiguration : IEntityTypeConfiguration<IngestDebounceMarker>
{
    public void Configure(EntityTypeBuilder<IngestDebounceMarker> builder)
    {
        builder.ToTable("IngestDebounceMarkers", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasConversion<byte>().IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();
    }
}
