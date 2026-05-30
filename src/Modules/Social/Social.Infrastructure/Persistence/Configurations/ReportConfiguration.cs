using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;
using Social.Domain.Enums;

namespace Social.Infrastructure.Persistence.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReporterUserId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired().HasConversion<byte>();
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasConversion<byte>();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>().HasDefaultValue(ReportStatus.Open);
        builder.Property(x => x.SubmittedAt).IsRequired();
        builder.Property(x => x.ResolvedByUserId).IsRequired(false);
        builder.Property(x => x.ResolvedAt).IsRequired(false);
        builder.Property(x => x.ResolutionAction).IsRequired(false).HasConversion<byte?>();
        builder.Property(x => x.ResolutionNotes).IsRequired(false).HasMaxLength(1000);

        // Audit columns (from AuditableEntity)
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.Status })
            .HasDatabaseName("IX_Reports_EntityType_EntityId_Status");

        builder.HasIndex(x => x.ReporterUserId).HasDatabaseName("IX_Reports_ReporterUserId");

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_Reports_Status_CreatedAt");
    }
}
