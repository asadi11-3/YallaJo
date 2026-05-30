using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public sealed class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims", "security");

        builder.HasKey(uc => uc.Id);
        builder.Property(uc => uc.Id).ValueGeneratedNever();

        builder.Property(uc => uc.UserId).IsRequired();

        builder.Property(uc => uc.ClaimType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(uc => uc.ClaimValue)
            .IsRequired()
            .HasMaxLength(1024);

        // Auditable fields
        builder.Property(uc => uc.CreatedAt).IsRequired();
        builder.Property(uc => uc.UpdatedAt).IsRequired(false);
        builder.Property(uc => uc.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(uc => uc.DeletedAt).IsRequired(false);
        builder.Property(uc => uc.RowVersion).IsRowVersion();

        builder.HasQueryFilter(uc => !uc.IsDeleted);

        // Index — prevent duplicate claim assignments per user
        builder.HasIndex(uc => new { uc.UserId, uc.ClaimType, uc.ClaimValue })
            .IsUnique()
            .HasDatabaseName("IX_UserClaims_UserId_ClaimType_ClaimValue_Unique");
    }
}
