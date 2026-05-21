using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Persistence.Configurations;

internal sealed class NotificationDeliveryAttemptConfiguration
    : IEntityTypeConfiguration<NotificationDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryAttempt> builder)
    {
        builder.ToTable("NotificationDeliveryAttempts", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.NotificationId).IsRequired();
        builder.Property(x => x.Channel).IsRequired().HasConversion<byte>();
        builder.Property(x => x.Status).IsRequired().HasConversion<byte>()
            .HasDefaultValue(NotificationDeliveryStatus.Pending);
        builder.Property(x => x.AttemptNumber).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.AttemptedAt).IsRequired();
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.Property(x => x.FailureReason).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.ExternalRef).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.NotificationId, x.Channel });
        builder.HasIndex(x => new { x.Status, x.AttemptedAt }); // for retry picker
    }
}
