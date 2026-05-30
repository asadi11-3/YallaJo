using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class SeasonalityRuleConfiguration : IEntityTypeConfiguration<SeasonalityRule>
{
    public void Configure(EntityTypeBuilder<SeasonalityRule> builder)
    {
        builder.ToTable("SeasonalityRules", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Multiplier).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200);

        builder.HasIndex(x => new { x.PlaceId, x.IsActive });
    }
}
