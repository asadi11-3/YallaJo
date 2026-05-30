using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class AgencyApplicationConfiguration : IEntityTypeConfiguration<AgencyApplication>
{
    public void Configure(EntityTypeBuilder<AgencyApplication> builder)
    {
        builder.ToTable("AgencyApplications", "accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.GuideUserId).IsRequired();
        builder.Property(a => a.AgencyUserId).IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.Message)
            .IsRequired(false)
            .HasMaxLength(1000);

        builder.Property(a => a.RejectionReason)
            .IsRequired(false)
            .HasMaxLength(500);

        builder.Property(a => a.RespondedAt).IsRequired(false);

        // Auditable fields
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired(false);
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).IsRequired(false);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        // Indexes
        builder.HasIndex(a => a.GuideUserId)
            .HasDatabaseName("IX_AgencyApplications_GuideUserId");

        builder.HasIndex(a => new { a.AgencyUserId, a.Status })
            .HasDatabaseName("IX_AgencyApplications_AgencyUserId_Status");
    }
}
