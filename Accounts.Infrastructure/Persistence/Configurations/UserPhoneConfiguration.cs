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
    public class UserPhoneConfiguration : IEntityTypeConfiguration<UserPhone>
    {
        public void Configure(EntityTypeBuilder<UserPhone> builder)
        {
            builder.ToTable("UserPhones", "accounts");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedNever();

            builder.Property(p => p.UserId)
                .IsRequired();

            // Value Object: PhoneNumber stored as owned type
            builder.OwnsOne(p => p.Number, phone =>
            {
                phone.Property(n => n.Value)
                    .HasColumnName("PhoneNumber")
                    .IsRequired()
                    .HasMaxLength(20);
            });

            builder.Property(p => p.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.VerifiedAt)
                .IsRequired(false);

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired(false);

            // Unique constraint: Phone must be globally unique when verified
            // Note: SQL Server doesn't support conditional unique indexes directly in EF,
            // so we'll add this via raw SQL migration
            builder.HasIndex(p => p.Number.Value)
                .HasDatabaseName("IX_UserPhones_PhoneNumber")
                .HasFilter("[IsVerified] = 1"); // Only verified phones must be unique

            // Composite index
            builder.HasIndex(p => new { p.UserId, p.IsPrimary });

            builder.HasIndex(p => p.IsVerified);
        }
    }
}
