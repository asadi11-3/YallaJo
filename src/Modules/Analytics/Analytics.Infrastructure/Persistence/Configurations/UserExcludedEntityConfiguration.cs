using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class UserExcludedEntityConfiguration : IEntityTypeConfiguration<UserExcludedEntity>
{
    public void Configure(EntityTypeBuilder<UserExcludedEntity> builder)
    {
        builder.ToTable("UserExcludedEntities", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.EntityKind).IsRequired()
            .HasConversion(v => (byte)v, v => (EntityType)v);
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.EntityKind, x.EntityId }).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}
