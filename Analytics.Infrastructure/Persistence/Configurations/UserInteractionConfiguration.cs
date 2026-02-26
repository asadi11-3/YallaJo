using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analytics.Infrastructure.Persistence.Configurations;

public class UserInteractionConfiguration : IEntityTypeConfiguration<UserInteraction>
{
    public void Configure(EntityTypeBuilder<UserInteraction> builder)
    {
        builder.ToTable("UserInteractions", "analytics");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.InteractionType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId).IsRequired();

        builder.Property(x => x.Latitude)
            .IsRequired(false)
            .HasPrecision(10, 8);

        builder.Property(x => x.Longitude)
            .IsRequired(false)
            .HasPrecision(11, 8);

        builder.Property(x => x.SessionId)
            .IsRequired(false)
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.DeviceType)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(x => x.DurationSeconds).IsRequired(false);
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.OccurredAt);
    }
}
