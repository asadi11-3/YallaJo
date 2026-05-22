using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class TourSnapshotConfiguration : IEntityTypeConfiguration<TourSnapshot>
{
    public void Configure(EntityTypeBuilder<TourSnapshot> builder)
    {
        builder.ToTable("TourSnapshots", "social");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TourId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.Property(x => x.BusinessId).IsRequired(false);
        builder.Property(x => x.OwnerProviderId).IsRequired();
        builder.Property(x => x.PlaceId).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.HasIndex(x => x.TourId).IsUnique().HasDatabaseName("IX_TourSnapshots_TourId");
        builder.HasIndex(x => x.BusinessId).HasDatabaseName("IX_TourSnapshots_BusinessId");
        builder.HasIndex(x => x.OwnerProviderId).HasDatabaseName("IX_TourSnapshots_OwnerProviderId");
        builder.HasIndex(x => x.PlaceId).HasDatabaseName("IX_TourSnapshots_PlaceId");
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_TourSnapshots_IsDeleted");
    }
}
