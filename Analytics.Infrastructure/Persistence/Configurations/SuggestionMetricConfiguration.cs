using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class SuggestionMetricConfiguration : IEntityTypeConfiguration<SuggestionMetric>
{
    public void Configure(EntityTypeBuilder<SuggestionMetric> builder)
    {
        builder.ToTable("SuggestionMetrics", "analytics");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Stage).HasMaxLength(20).IsRequired();
        builder.Property(e => e.SessionId).HasMaxLength(200);
        builder.Property(e => e.ExperimentVariant).HasMaxLength(100);
        builder.HasIndex(e => new { e.BatchId, e.Stage, e.OccurredAt });
        builder.HasIndex(e => new { e.UserId, e.OccurredAt });
        builder.HasIndex(e => e.OccurredAt);
    }
}
