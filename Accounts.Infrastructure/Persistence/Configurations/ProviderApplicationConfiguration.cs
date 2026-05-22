using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class ProviderApplicationConfiguration : IEntityTypeConfiguration<ProviderApplication>
{
    public void Configure(EntityTypeBuilder<ProviderApplication> builder)
    {
        builder.ToTable("ProviderApplications", "accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.UserId).IsRequired();

        builder.Property(a => a.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(a => a.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.ContactPhone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(a => a.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(a => a.SubmittedAt).IsRequired(false);
        builder.Property(a => a.ReviewedAt).IsRequired(false);
        builder.Property(a => a.ReviewedByUserId).IsRequired(false);

        builder.Property(a => a.RejectionReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(a => a.SuspensionReason)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(a => a.ReapplicationCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.CoolingPeriodEndsAt).IsRequired(false);

        builder.Property(a => a.TypeSpecificDataJson)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        // Auditable fields
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired(false);
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).IsRequired(false);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        // Navigation — documents
        builder.HasMany(a => a.Documents)
            .WithOne(d => d.Application)
            .HasForeignKey(d => d.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_ProviderApplications_UserId");

        builder.HasIndex(a => new { a.Status, a.ReviewedAt })
            .HasDatabaseName("IX_ProviderApplications_Status_ReviewedAt");

        builder.HasIndex(a => new { a.Type, a.Status })
            .HasDatabaseName("IX_ProviderApplications_Type_Status");

        // Unique filtered index (one approved per user) — created manually in migration
        // CREATE UNIQUE INDEX UX_ProviderApplications_UserId_Approved
        //     ON accounts.ProviderApplications (UserId) WHERE Status = 'Approved'
    }
}
