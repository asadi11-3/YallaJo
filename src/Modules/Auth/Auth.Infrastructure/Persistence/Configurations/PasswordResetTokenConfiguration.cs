using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF model configuration for the Phase 2C-2 <see cref="PasswordResetToken"/>
/// aggregate. Mirrors the sizing conventions used by
/// <c>ActivationTokenConfiguration</c> (320 for email addresses, 512
/// for hashes) and adds indexes tuned to the handler lookups.
/// </summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens", "auth");

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
            .HasDefaultValue(PasswordResetTokenState.Issued);

        builder.Property(t => t.DeliveryStatus)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PasswordResetTokenDeliveryStatus.Pending);

        builder.Property(t => t.RevokedReason)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PasswordResetTokenRevokedReason.None);

        builder.Property(t => t.ResetOrigin)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(PasswordResetOrigin.SelfService);

        builder.Property(t => t.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Auditable fields — same conventions as Otp / Session / ActivationToken.
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired(false);
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAt).IsRequired(false);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        // Canonical lookup used by ForgotPassword's supersede sweep and
        // ResetPassword's latest-active query:
        //   WHERE UserId = @u AND State IN (Issued, Delivered)
        //   ORDER BY CreatedAt DESC
        builder.HasIndex(t => new { t.UserId, t.State })
            .HasDatabaseName("IX_PasswordResetTokens_UserId_State");

        // Supports the future background sweep that will materialize
        // expired rows.
        builder.HasIndex(t => t.ExpiresAt)
            .HasDatabaseName("IX_PasswordResetTokens_ExpiresAt");
    }
}
