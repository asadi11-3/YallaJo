using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.AddedAt).IsRequired();

        // Audit columns (from AuditableEntity)
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        // S-R7: unique active favorite per (UserId, EntityType, EntityId)
        builder.HasIndex(x => new { x.UserId, x.EntityType, x.EntityId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Favorites_User_Entity_Unique");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_Favorites_UserId");
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_Favorites_EntityType_EntityId");
    }
}
