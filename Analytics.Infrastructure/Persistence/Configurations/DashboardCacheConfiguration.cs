using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class DashboardCacheConfiguration : IEntityTypeConfiguration<DashboardCache>
{
    public void Configure(EntityTypeBuilder<DashboardCache> builder)
    {
        builder.ToTable("DashboardCaches", "analytics");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(500);
        builder.Property(x => x.ValueJson).HasColumnType("nvarchar(max)");
    }
}
