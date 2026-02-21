using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Accounts.Infrastructure.Persistence.Configurations
{
    public class UserEmailConfiguration : IEntityTypeConfiguration<UserEmail>
    {
        public void Configure(EntityTypeBuilder<UserEmail> builder)
        {
            builder.ToTable("UserEmails", "accounts");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .ValueGeneratedNever();

            builder.Property(e => e.UserId)
                .IsRequired();

            // Value Object: EmailAddress stored as owned type
            builder.OwnsOne(e => e.Address, email =>
            {
                email.Property(a => a.Value)
                    .HasColumnName("Email")
                    .IsRequired()
                    .HasMaxLength(255);

                // Unique constraint: Email must be globally unique
                email.HasIndex(a => a.Value)
                    .IsUnique()
                    .HasDatabaseName("IX_UserEmails_Email_Unique");
            });

            builder.Property(e => e.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.VerifiedAt)
                .IsRequired(false);

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .IsRequired(false);

            // Composite index for finding primary email per user
            builder.HasIndex(e => new { e.UserId, e.IsPrimary });

            // Index for verification queries
            builder.HasIndex(e => e.IsVerified);
        }
    }
}
