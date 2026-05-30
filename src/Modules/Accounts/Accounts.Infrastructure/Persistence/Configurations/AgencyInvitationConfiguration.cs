using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class AgencyInvitationConfiguration : IEntityTypeConfiguration<AgencyInvitation>
{
    public void Configure(EntityTypeBuilder<AgencyInvitation> builder)
    {
        builder.ToTable("AgencyInvitations", "accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.AgencyUserId).IsRequired();
        builder.Property(a => a.GuideUserId).IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Message)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(a => a.ProposedCommissionPercentage)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(a => a.ExpiresAt).IsRequired();
        builder.Property(a => a.RespondedAt).IsRequired(false);

        // Auditable fields
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired(false);
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).IsRequired(false);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        // Indexes
        builder.HasIndex(a => a.AgencyUserId)
            .HasDatabaseName("IX_AgencyInvitations_AgencyUserId");

        builder.HasIndex(a => new { a.GuideUserId, a.Status })
            .HasDatabaseName("IX_AgencyInvitations_GuideUserId_Status");

        builder.HasIndex(a => a.ExpiresAt)
            .HasDatabaseName("IX_AgencyInvitations_ExpiresAt");
    }
}
