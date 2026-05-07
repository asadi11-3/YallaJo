using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Security.Domain.Entities;

namespace Security.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "security");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.IsActive).IsRequired();

        // Phase 2A: explicit lifecycle state. Persisted as INT (enum value)
        // alongside the legacy IsActive boolean. Both are kept until Phase 2B
        // can safely drop IsActive — the duplication is intentional for
        // rollback safety and to leave existing LINQ projections (e.g.
        // queries filtering on Where(u => u.IsActive)) untouched until they
        // are migrated one by one.
        builder.Property(u => u.LifecycleState)
            .HasConversion<int>()
            .HasColumnName("LifecycleState")
            .HasDefaultValue(AccountLifecycleState.Provisioned)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        // Auditable fields
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired(false);
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.DeletedAt).IsRequired(false);
        builder.Property(u => u.RowVersion).IsRowVersion();

        // Relationships
        builder.HasMany(u => u.Emails)
            .WithOne(e => e.User)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Phones)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UserClaims)
            .WithOne(uc => uc.User)
            .HasForeignKey(uc => uc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query Filter for Soft Delete
        builder.HasQueryFilter(u => !u.IsDeleted);

        // Indexes
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.LifecycleState);
        builder.HasIndex(u => u.IsDeleted);
    }
}
