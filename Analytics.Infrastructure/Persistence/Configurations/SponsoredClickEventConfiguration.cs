using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class SponsoredClickEventConfiguration : IEntityTypeConfiguration<SponsoredClickEvent>
{
    public void Configure(EntityTypeBuilder<SponsoredClickEvent> builder)
    {
        builder.ToTable("SponsoredClickEvents", "analytics");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ChargedAmount).HasPrecision(18, 4);
        builder.Property(e => e.SessionId).HasMaxLength(200);
        builder.HasIndex(e => new { e.BidId, e.ClickedAt });
        builder.HasIndex(e => new { e.UserId, e.BidId, e.ClickedAt });
    }
}
