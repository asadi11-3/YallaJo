using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public sealed class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.ToTable("RoleClaims", "security");

        builder.HasKey(rc => rc.Id);
        builder.Property(rc => rc.Id).ValueGeneratedNever();

        builder.Property(rc => rc.RoleId).IsRequired();

        builder.Property(rc => rc.ClaimType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(rc => rc.ClaimValue)
            .IsRequired()
            .HasMaxLength(1024);

        // Auditable fields
        builder.Property(rc => rc.CreatedAt).IsRequired();
        builder.Property(rc => rc.UpdatedAt).IsRequired(false);
        builder.Property(rc => rc.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(rc => rc.DeletedAt).IsRequired(false);
        builder.Property(rc => rc.RowVersion).IsRowVersion();

        builder.HasQueryFilter(rc => !rc.IsDeleted);

        // Index — prevent duplicate claim assignments per role
        builder.HasIndex(rc => new { rc.RoleId, rc.ClaimType, rc.ClaimValue })
            .IsUnique()
            .HasDatabaseName("IX_RoleClaims_RoleId_ClaimType_ClaimValue_Unique");
    }
}
