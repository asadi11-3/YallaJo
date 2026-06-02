using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "messaging");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Channel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(NotificationPriority.Low);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Body)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Data)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ReadAt).IsRequired(false);
        builder.Property(x => x.SentAt).IsRequired(false);

        builder.Property(x => x.EntityType)
            .IsRequired(false)
            .HasMaxLength(200);

        builder.Property(x => x.EntityId).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .IsDescending(false, false, true);
    }
}
