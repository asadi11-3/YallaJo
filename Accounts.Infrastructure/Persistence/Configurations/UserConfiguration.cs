using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "accounts");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedNever(); 

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.IsActive)
                .IsRequired();

            // Auditable fields
            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired(false);

            builder.Property(u => u.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(u => u.DeletedAt)
                .IsRequired(false);

            builder.Property(u => u.RowVersion)
                .IsRowVersion();

            // Relationships
            builder.HasMany(u => u.Emails)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Phones)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Query Filter for Soft Delete
            builder.HasQueryFilter(u => !u.IsDeleted);

            // Indexes
            builder.HasIndex(u => u.IsActive);
            builder.HasIndex(u => u.IsDeleted);
        }
    }
}
