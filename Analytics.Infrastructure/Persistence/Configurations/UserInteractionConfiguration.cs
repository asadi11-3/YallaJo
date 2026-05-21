using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public sealed class UserInteractionConfiguration : IEntityTypeConfiguration<UserInteraction>
{
    public void Configure(EntityTypeBuilder<UserInteraction> builder)
    {
        builder.ToTable("UserInteractions", "analytics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.InteractionType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.ClientIpHash).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.SessionId).HasMaxLength(128);
        builder.HasIndex(x => new { x.OccurredAt, x.Id }).HasDatabaseName("IX_UserInteractions_OccurredAt_Id").IsDescending(true, false);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.InteractionType);
    }
}
