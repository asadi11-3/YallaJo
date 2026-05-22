using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class ExperimentAssignmentConfiguration : IEntityTypeConfiguration<ExperimentAssignment>
{
    public void Configure(EntityTypeBuilder<ExperimentAssignment> builder)
    {
        builder.ToTable("ExperimentAssignments", "analytics");
        builder.HasKey(e => new { e.UserId, e.ExperimentId });
        builder.Property(e => e.VariantName).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.ExperimentId);
    }
}
