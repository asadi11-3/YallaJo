using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "security");

        builder.HasKey(ur => ur.Id);
        builder.Property(ur => ur.Id).ValueGeneratedNever();

        builder.Property(ur => ur.UserId).IsRequired();
        builder.Property(ur => ur.RoleId).IsRequired();

        // Auditable fields
        builder.Property(ur => ur.CreatedAt).IsRequired();
        builder.Property(ur => ur.UpdatedAt).IsRequired(false);
        builder.Property(ur => ur.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(ur => ur.DeletedAt).IsRequired(false);
        builder.Property(ur => ur.RowVersion).IsRowVersion();

        builder.HasQueryFilter(ur => !ur.IsDeleted);

        // Indexes — prevent duplicate role assignments
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_UserId_RoleId_Unique");

        // FK index on RoleId for efficient role-based user queries (e.g., AnyWithRoleAsync)
        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");
    }
}
