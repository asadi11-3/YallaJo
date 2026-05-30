using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class EditorialPinConfiguration : IEntityTypeConfiguration<EditorialPin>
{
    public void Configure(EntityTypeBuilder<EditorialPin> builder)
    {
        builder.ToTable("EditorialPins", "analytics");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityKind).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Context).HasConversion<int>().IsRequired();
        builder.Property(x => x.BadgeText).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.Context, x.IsActive });
        builder.HasIndex(x => new { x.EntityKind, x.EntityId });
    }
}
