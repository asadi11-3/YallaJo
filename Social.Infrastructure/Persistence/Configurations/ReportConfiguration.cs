using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Social.Domain.Entities;

namespace Social.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports", "social");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReporterUserId).IsRequired();
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasConversion<int>();
        builder.Property(x => x.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).IsRequired().HasConversion<int>().HasDefaultValue(Social.Domain.Enums.ReportStatus.Pending);
        builder.Property(x => x.ResolvedAt).IsRequired(false);
        builder.Property(x => x.ResolvedByUserId).IsRequired(false);
        builder.Property(x => x.ResolutionNotes).IsRequired(false).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.Status });
    }
}
