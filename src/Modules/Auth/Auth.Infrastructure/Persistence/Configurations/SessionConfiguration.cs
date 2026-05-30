using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions", "auth");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.DeviceId).IsRequired();

        builder.Property(s => s.ExpiresAt).IsRequired();

        builder.Property(s => s.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.RevokedAt).IsRequired(false);

        builder.Property(s => s.IpAddress)
            .HasMaxLength(64)
            .IsRequired(false);

        // Auditable fields
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired(false);
        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(s => s.DeletedAt).IsRequired(false);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasOne<Device>()
            .WithMany()
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.DeviceId);
        builder.HasIndex(s => s.IsRevoked);
        builder.HasIndex(s => s.ExpiresAt);
    }
}
