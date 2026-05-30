using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF model configuration for the Phase 2C-1 <see cref="ActivationToken"/>
/// aggregate. Mirrors the OTP-era sizing conventions for string columns
/// (320 for email addresses, 512 for hashes) and adds indexes on the
/// lookups the handlers use.
/// </summary>
public sealed class ActivationTokenConfiguration : IEntityTypeConfiguration<ActivationToken>
{
    public void Configure(EntityTypeBuilder<ActivationToken> builder)
    {
        builder.ToTable("ActivationTokens", "auth");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.UserId).IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(t => t.IssuedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.ConsumedAt).IsRequired(false);
        builder.Property(t => t.RevokedAt).IsRequired(false);
        builder.Property(t => t.LastSentAt).IsRequired(false);

        builder.Property(t => t.State)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ActivationTokenState.Issued);

        builder.Property(t => t.DeliveryStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ActivationTokenDeliveryStatus.Pending);

        builder.Property(t => t.RevokedReason)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ActivationTokenRevokedReason.None);

        builder.Property(t => t.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Auditable fields — same conventions as Otp / Session.
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired(false);
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).IsRequired(false);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        // Canonical lookup used by both the resend-supersede sweep and the
        // activate-account latest-active query:
        //   WHERE UserId = @u AND State IN (Issued, Delivered)
        //   ORDER BY CreatedAt DESC
        // The covering index narrows to the two non-terminal states via a
        // filtered index so Consumed/Revoked rows never appear in the scan.
        builder.HasIndex(t => new { t.UserId, t.State })
            .HasDatabaseName("IX_ActivationTokens_UserId_State");

        // Supports the background sweep that will materialize expired rows
        // in Phase 2C-2+.
        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("IX_ActivationTokens_ExpiresAt");
    }
}
