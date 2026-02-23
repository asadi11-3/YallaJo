using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices", "auth");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.UserId).IsRequired();

        builder.Property(d => d.DeviceToken)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.UserAgent)
            .HasMaxLength(512)
            .IsRequired(false);

        builder.Property(d => d.DeviceName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(d => d.IsTrusted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.TrustedAt)
            .IsRequired(false);

        builder.Property(d => d.LastSeenAt)
            .IsRequired();

        // Auditable fields
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired(false);
        builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).IsRequired(false);
        builder.Property(d => d.RowVersion).IsRowVersion();

        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.DeviceToken).IsUnique();
        builder.HasIndex(d => d.IsTrusted);
    }
}
