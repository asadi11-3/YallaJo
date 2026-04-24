using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ForgotPassword;

/// <summary>
/// Phase 2C-3 — self-service password recovery, now event-driven.
/// Preserves every pre-existing enumeration-safety invariant:
/// <list type="bullet">
///   <item><description>Non-existent email → generic success (no DB write, no event).</description></item>
///   <item><description>Account not Active or email unverified → generic success (no DB write, no event).</description></item>
///   <item><description>Recent reset token within 60 s → generic success (no DB write, no event).</description></item>
///   <item><description>Prior active reset tokens → <see cref="PasswordResetToken.Supersede"/>d before the new one is issued.</description></item>
/// </list>
/// <para>
/// On the happy path the handler generates a plain reset code + hash,
/// creates a new <see cref="PasswordResetToken"/> in
/// <see cref="PasswordResetTokenState.Issued"/> /
/// <see cref="PasswordResetTokenDeliveryStatus.Pending"/>, and raises a
/// <see cref="PasswordResetTokenIssuedEvent"/> that carries the plain
/// code. The unit-of-work domain-event dispatcher routes it through the
/// <c>PasswordResetTokenIssuedDomainEventHandler</c> which writes an
/// outbox message in the same DbContext. A single
/// <c>SaveChangesAsync</c> commits the token row, the supersede sweep,
/// and the outbox row atomically; the
/// <c>PasswordResetEmailDispatchHandler</c> sends the email out of band.
/// </para>
/// <para>
/// SMTP is no longer invoked inside this handler. Enumeration safety is
/// actually reinforced — command latency no longer varies with SMTP
/// latency, so timing side-channels are narrowed.
/// </para>
/// </summary>
public sealed class ForgotPasswordCommandHandler(
    ISecurityService securityService,
    IPasswordResetTokenRepository resetTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private const string GenericMessage  = "If this email exists, a reset code was sent.";
    private const int    ThrottleSeconds = 60;
    private const int    ExpiryMinutes   = 10;

    public async Task<Result<ForgotPasswordResult>> Handle(
        ForgotPasswordCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Lifecycle gate: only fully-onboarded, active accounts may recover.
        var status = await securityService.GetAccountStatusByEmailAsync(normalizedEmail, ct);
        if (status is null
         || status.Lifecycle != AccountLifecycleSnapshot.Active
         || !status.IsEmailVerified)
        {
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        var userId = status.UserId;

        // Per-user DB throttle (defense-in-depth beyond IP/email rate
        // limiter). Readonly lookup so the check doesn't interfere with
        // the tracked supersede sweep below.
        var recent = await resetTokenRepository.GetLatestActiveForUserReadOnlyAsync(userId, ct);
        if (recent is not null
            && recent.CreatedAt > DateTime.UtcNow.AddSeconds(-ThrottleSeconds))
        {
            return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
        }

        // Generate the plain code up-front. Only the hash is persisted
        // on the aggregate; the plain code travels through the domain
        // event → outbox payload → email dispatcher. See the integration
        // event XML-doc for the security tradeoff.
        var plainCode = otpService.Generate();
        var codeHash  = otpService.Hash(plainCode);

        // Supersede every non-terminal reset token for this user.
        var active = await resetTokenRepository.GetActiveForUserAsync(userId, ct);
        foreach (var prior in active)
            prior.Supersede();

        // Create the token in Issued/Pending and raise the domain event.
        // The PasswordResetTokenIssuedDomainEventHandler turns the domain
        // event into an OutboxMessage in the same DbContext, so token
        // + supersede + outbox commit atomically on the single
        // SaveChanges below.
        var token = PasswordResetToken.Issue(
            userId:          userId,
            tokenHash:       codeHash,
            deliveryAddress: normalizedEmail,
            expiryMinutes:   ExpiryMinutes,
            origin:          PasswordResetOrigin.SelfService);

        token.AddDomainEvent(new PasswordResetTokenIssuedEvent(
            TokenId:         token.Id,
            UserId:          token.UserId,
            DeliveryAddress: token.DeliveryAddress,
            PlainCode:       plainCode,
            ExpiresAt:       token.ExpiresAt,
            Origin:          token.ResetOrigin));

        await resetTokenRepository.AddAsync(token, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: PasswordResetToken {TokenId} issued for user {UserId}; email queued on outbox.",
            token.Id, userId);

        return Result<ForgotPasswordResult>.Success(new ForgotPasswordResult(GenericMessage));
    }
}
