using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class SuggestionBatchConfiguration : IEntityTypeConfiguration<SuggestionBatch>
{
    public void Configure(EntityTypeBuilder<SuggestionBatch> builder)
    {
        builder.ToTable("SuggestionBatches", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceKind).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Context).HasConversion<byte>().IsRequired();
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsStale).HasDefaultValue(false);

        builder.HasIndex(x => new { x.SourceKind, x.SourceId, x.Context }).IsUnique();
        builder.HasIndex(x => x.IsStale);
        builder.HasIndex(x => x.ComputedAt);
    }
}
