using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace Auth.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for the Auth module's outbox table.
/// <para>
/// <b>⚠ Phase 2C-3 security note.</b> For activation and password-
/// reset email dispatch, the plain activation token + pre-built
/// activation link (<c>ActivationTokenIssuedIntegrationEvent</c>) and
/// the plain password-reset code
/// (<c>PasswordResetTokenIssuedIntegrationEvent</c>) are embedded in
/// the <see cref="OutboxMessage.Content"/> JSON so the downstream
/// <c>ActivationEmailDispatchHandler</c> and
/// <c>PasswordResetEmailDispatchHandler</c> can construct the outgoing
/// email body. Implications:
/// </para>
/// <list type="bullet">
///   <item><description>Anyone with SELECT access to <c>auth.OutboxMessages</c> can, until the token's expiry, redeem an unprocessed row.</description></item>
///   <item><description>Processed rows retain the payload until the Phase 2C-4 retention worker purges them (<c>Auth.Infrastructure.BackgroundJobs.AuthRetentionWorker</c>, default 24h via <c>Auth:Retention:ProcessedOutboxRetentionHours</c>).</description></item>
///   <item><description>Dead-lettered messages (RetryCount exhausted, still unprocessed) are NOT auto-purged; they remain for ops investigation.</description></item>
/// </list>
/// <para>
/// Mitigations wired today:
/// </para>
/// <list type="number">
///   <item><description>Aggressive 24-hour purge of processed rows (Phase 2C-4).</description></item>
///   <item><description>Reset codes expire after 10 minutes; activation tokens after 7 days — both narrow the usable-token window independently of outbox retention.</description></item>
///   <item><description>Handlers short-circuit on terminal / expired / already-delivered tokens so a replay attack against an unprocessed row would be capped by the aggregate's own state machine.</description></item>
/// </list>
/// <para>
/// Future hardening (deferred): symmetric payload encryption with a
/// KMS-provided key, or carry only tokenId in the event and stage the
/// plain token in a short-TTL distributed cache.
/// </para>
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "auth");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(o => o.OccurredOnUtc).IsRequired();
        builder.Property(o => o.ProcessedOnUtc).IsRequired(false);

        builder.Property(o => o.Error)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.Property(o => o.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.TraceContext)
                    .IsRequired(false)
                    .HasMaxLength(500);
        
                builder.Property(o => o.Status)
                            .IsRequired()
                            .HasDefaultValue(OutboxMessageStatus.Pending)
                            .HasConversion<int>();
        
                builder.HasIndex(o => new { o.ProcessedOnUtc, o.RetryCount, o.OccurredOnUtc })
                    .HasDatabaseName("IX_OutboxMessages_Unprocessed");
    }
}
