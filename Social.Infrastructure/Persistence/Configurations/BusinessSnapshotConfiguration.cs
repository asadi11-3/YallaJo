using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class BusinessSnapshotConfiguration : IEntityTypeConfiguration<BusinessSnapshot>
{
    public void Configure(EntityTypeBuilder<BusinessSnapshot> builder)
    {
        builder.ToTable("BusinessSnapshots", "social");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.BusinessId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.PlaceId).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.HasIndex(x => x.BusinessId).IsUnique().HasDatabaseName("IX_BusinessSnapshots_BusinessId");
        builder.HasIndex(x => x.OwnerId).HasDatabaseName("IX_BusinessSnapshots_OwnerId");
        builder.HasIndex(x => x.PlaceId).HasDatabaseName("IX_BusinessSnapshots_PlaceId");
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_BusinessSnapshots_IsDeleted");
    }
}
