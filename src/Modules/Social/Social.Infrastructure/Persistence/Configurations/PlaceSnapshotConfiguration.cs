using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class PlaceSnapshotConfiguration : IEntityTypeConfiguration<PlaceSnapshot>
{
    public void Configure(EntityTypeBuilder<PlaceSnapshot> builder)
    {
        builder.ToTable("PlaceSnapshots", "social");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PlaceId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.HasIndex(x => x.PlaceId).IsUnique().HasDatabaseName("IX_PlaceSnapshots_PlaceId");
        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_PlaceSnapshots_IsDeleted");
    }
}
