using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class GdprDeletionRequestConfiguration : IEntityTypeConfiguration<GdprDeletionRequest>
{
    public void Configure(EntityTypeBuilder<GdprDeletionRequest> builder)
    {
        builder.ToTable("GdprDeletionRequests", "analytics");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.UserId, e.IsCancelled, e.IsExecuted });
        builder.HasIndex(e => e.ScheduledHardDeleteAt);
    }
}
