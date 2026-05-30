using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

public sealed class AgencyAffiliationConfiguration : IEntityTypeConfiguration<AgencyAffiliation>
{
    public void Configure(EntityTypeBuilder<AgencyAffiliation> builder)
    {
        builder.ToTable("AgencyAffiliations", "accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.AgencyUserId).IsRequired();
        builder.Property(a => a.GuideUserId).IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.CommissionPercentage)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(a => a.TerminatedAt).IsRequired(false);
        builder.Property(a => a.TerminatedByUserId).IsRequired(false);

        builder.Property(a => a.TerminationReason)
            .IsRequired(false)
            .HasMaxLength(500);

        // Auditable fields
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired(false);
        builder.Property(a => a.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(a => a.DeletedAt).IsRequired(false);
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasQueryFilter(a => !a.IsDeleted);

        // Indexes
        builder.HasIndex(a => a.AgencyUserId)
            .HasDatabaseName("IX_AgencyAffiliations_AgencyUserId");

        builder.HasIndex(a => a.GuideUserId)
            .HasDatabaseName("IX_AgencyAffiliations_GuideUserId");

        builder.HasIndex(a => new { a.GuideUserId, a.Status })
            .HasDatabaseName("IX_AgencyAffiliations_GuideUserId_Status");
    }
}
