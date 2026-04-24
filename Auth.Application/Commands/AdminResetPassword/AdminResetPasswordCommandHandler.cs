using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AdminResetPassword;

/// <summary>
/// Phase 3A — admin-initiated password reset. Reuses the Phase 2C-2
/// <see cref="PasswordResetToken"/> aggregate and the Phase 2C-3 outbox
/// email dispatch pipeline. Does NOT introduce new email or
/// integration-event infrastructure — the existing
/// <c>PasswordResetTokenIssuedEvent</c> carries the new
/// <c>AdminInitiated</c> origin, and
/// <c>PasswordResetEmailDispatchHandler</c> branches the wording by
/// origin.
/// <para>
/// Flow:
/// </para>
/// <list type="number">
///   <item><description>Resolve the admin actor from <see cref="ICurrentUser"/>. Unauthenticated callers are rejected (endpoint authorization should prevent this from being reachable, but the handler guards defensively).</description></item>
///   <item><description>Call <see cref="ISecurityService.GetAdminResetEligibilityAsync"/> — returns the target user's primary email + lifecycle, or a failure if the hierarchy check denies (including self-management denial) or the user is missing.</description></item>
///   <item><description>Gate on <see cref="AccountLifecycleSnapshot.Active"/> or <see cref="AccountLifecycleSnapshot.PendingPasswordReset"/> — reject <c>Provisioned</c> / <c>PendingActivation</c> (wrong verb — admin should use <c>SendActivationEmail</c>), <c>Suspended</c> (admin must <c>Reactivate</c> first), and <c>Archived</c> (terminal).</description></item>
///   <item><description>Gate on <c>IsPrimaryEmailVerified</c> — cannot send a reset to an unverified address.</description></item>
///   <item><description>Supersede every non-terminal <see cref="PasswordResetToken"/> for the target user.</description></item>
///   <item><description>Issue a new <see cref="PasswordResetToken"/> with <see cref="PasswordResetOrigin.AdminInitiated"/>, attach a <see cref="PasswordResetTokenIssuedEvent"/> carrying the plain code + origin, and persist the token.</description></item>
///   <item><description>Transition the target user to <see cref="AccountLifecycleSnapshot.PendingPasswordReset"/> via <see cref="IUserRegistrationService.MarkPendingPasswordResetAsync"/> (idempotent). This blocks login until the user completes the reset.</description></item>
///   <item><description>Stage revocation of all active sessions + refresh tokens via <see cref="ISessionRevocationService"/> with <see cref="SessionRevocationReason.PasswordResetByAdmin"/>.</description></item>
///   <item><description>Single <see cref="IAuthUnitOfWork.SaveChangesAsync"/> commits token + supersede sweep + outbox row + session revocations atomically.</description></item>
///   <item><description>Structured log: <c>AdminActorId</c>, <c>TargetUserId</c>, <c>Reason</c>, <c>TokenId</c>. <b>Never</b> logs the plain code.</description></item>
/// </list>
/// <para>
/// The handler does NOT call <see cref="IEmailService"/>; the existing
/// <c>PasswordResetEmailDispatchHandler</c> dispatches the email out of
/// band when the outbox message is processed.
/// </para>
/// <para>
/// While the target is in <c>PendingPasswordReset</c>, self-service
/// <c>ForgotPassword</c> is gated out (lifecycle check rejects
/// non-Active accounts). This is the deliberate enforcement story: if
/// the user loses the admin-emailed code, admin must reissue via this
/// command. Completion of the reset via <c>ResetPasswordCommand</c>
/// automatically transitions the user back to <c>Active</c> inside
/// <see cref="ISecurityService.ReplacePasswordBySelfAsync"/>.
/// </para>
/// </summary>
public sealed class AdminResetPasswordCommandHandler(
    ISecurityService securityService,
    IUserRegistrationService userRegistrationService,
    IPasswordResetTokenRepository resetTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IOtpService otpService,
    ISessionRevocationService sessionRevocation,
    IAdminAuditWriter adminAuditWriter,
    IRequestContext requestContext,
    ICurrentUser currentUser,
    ILogger<AdminResetPasswordCommandHandler> logger)
    : ICommandHandler<AdminResetPasswordCommand, AdminResetPasswordResult>
{
    private const string SuccessMessage = "Password reset email queued.";
    private const int    ExpiryMinutes   = 10;

    public async Task<Result<AdminResetPasswordResult>> Handle(
        AdminResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Actor must be authenticated. Endpoint authorization should
        //    already enforce this; the defensive check here keeps the
        //    handler safe to invoke from non-endpoint contexts (tests,
        //    future admin CLIs, etc.) and surfaces Unauthorized cleanly.
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<AdminResetPasswordResult>.Failure(
                Error.Failure("Auth.Unauthenticated", "Admin actor is not authenticated."),
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;

        // 2. Cross-module eligibility probe. The Security service runs the
        //    IRoleHierarchyService check (which covers self-management
        //    denial) and returns the target's primary email + lifecycle
        //    snapshot, or a typed failure.
        var eligibility = await securityService.GetAdminResetEligibilityAsync(
            request.TargetUserId, actorId, cancellationToken);

        if (eligibility.IsFailure)
        {
            return Result<AdminResetPasswordResult>.Fail(
                eligibility.Outcome,
                eligibility.Messages.FirstOrDefault() ?? string.Empty,
                eligibility.Errors.ToArray());
        }

        var snapshot = eligibility.Value!;

        // 3. Lifecycle gate — admin reset only for accounts that have
        //    actually established a password and can receive reset mail.
        if (snapshot.Lifecycle != AccountLifecycleSnapshot.Active
         && snapshot.Lifecycle != AccountLifecycleSnapshot.PendingPasswordReset)
        {
            return Result<AdminResetPasswordResult>.Failure(
                Error.Conflict(
                    "User.IneligibleForPasswordReset",
                    $"Account is in state '{snapshot.Lifecycle}' and is not eligible for admin-initiated password reset. " +
                    "Use SendActivationEmail for Provisioned/PendingActivation accounts; Reactivate a Suspended account before resetting."),
                Outcome.Conflict);
        }

        // 4. Email-verified gate. An account could theoretically reach
        //    Active without a verified primary email via the legacy
        //    provisioning shortcut; refuse rather than silently drop the
        //    email into the void.
        if (!snapshot.IsPrimaryEmailVerified || string.IsNullOrWhiteSpace(snapshot.PrimaryEmail))
        {
            return Result<AdminResetPasswordResult>.Failure(
                Error.Conflict(
                    "User.EmailUnverified",
                    "Target account does not have a verified primary email — cannot send a password reset."),
                Outcome.Conflict);
        }

        var normalizedEmail = snapshot.PrimaryEmail.Trim().ToLowerInvariant();

        // 5. Supersede every non-terminal PasswordResetToken for this
        //    user. Admin-initiated and self-service tokens share the
        //    same aggregate; a fresh admin reset invalidates any prior
        //    outstanding code regardless of origin.
        var active = await resetTokenRepository.GetActiveForUserAsync(
            request.TargetUserId, cancellationToken);

        foreach (var prior in active)
            prior.Supersede();

        // 6. Generate the plain code up-front. Only the hash is
        //    persisted on the aggregate; the plain code travels through
        //    the domain event → outbox payload → email dispatcher.
        var plainCode = otpService.Generate();
        var codeHash  = otpService.Hash(plainCode);

        // 7. Create the token + attach the domain event. The
        //    PasswordResetTokenIssuedDomainEventHandler translates the
        //    event into an OutboxMessage in the same DbContext, so
        //    token + supersede + outbox row commit atomically on the
        //    single SaveChanges below.
        var token = PasswordResetToken.Issue(
            userId:          request.TargetUserId,
            tokenHash:       codeHash,
            deliveryAddress: normalizedEmail,
            expiryMinutes:   ExpiryMinutes,
            origin:          PasswordResetOrigin.AdminInitiated);

        token.AddDomainEvent(new PasswordResetTokenIssuedEvent(
            TokenId:         token.Id,
            UserId:          token.UserId,
            DeliveryAddress: token.DeliveryAddress,
            PlainCode:       plainCode,
            ExpiresAt:       token.ExpiresAt,
            Origin:          token.ResetOrigin));

        await resetTokenRepository.AddAsync(token, cancellationToken);

        // 8. Lifecycle transition — Active → PendingPasswordReset, or
        //    idempotent no-op if already PendingPasswordReset. This
        //    blocks login immediately; the session-revocation below
        //    tears down any live sessions in the same commit.
        var transition = await userRegistrationService.MarkPendingPasswordResetAsync(
            request.TargetUserId, cancellationToken);

        if (transition.IsFailure)
        {
            // Shouldn't happen — we already gated the lifecycle via
            // GetAdminResetEligibilityAsync. Propagate faithfully so
            // telemetry catches any drift.
            return Result<AdminResetPasswordResult>.Fail(
                transition.Outcome,
                transition.Messages.FirstOrDefault() ?? "Failed to update account lifecycle state.",
                transition.Errors.ToArray());
        }

        // 9. Stage revocation of all active sessions + refresh tokens.
        //    No ambient TransactionScope — MarkPendingPasswordResetAsync
        //    saves on the Security UoW separately (minor two-step
        //    commit; accepted because the failure mode is benign:
        //    Security says "pending reset" and Auth still has live
        //    sessions, but those would be revoked on the next admin
        //    re-issue).
        await sessionRevocation.RevokeAllForUserAsync(
            request.TargetUserId,
            SessionRevocationReason.PasswordResetByAdmin,
            cancellationToken);

        // 10. Single Auth SaveChanges commits the new token row, the
        //     supersede sweep, the outbox message (via the domain-event
        //     dispatcher), and the session + refresh-token revocations
        //     atomically.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 11. Phase 4 — append the admin audit timeline row. Reached
        //     only after the lifecycle transition + token issue +
        //     session revocation succeeded. The plain code is NEVER
        //     persisted in metadata; only TokenId and origin are.
        var metadata = BuildMetadata(token.Id);
        await adminAuditWriter.RecordAsync(
            new AdminAuditEntry(
                ActorUserId:  actorId,
                TargetUserId: request.TargetUserId,
                Action:       AuditActions.AdminResetPasswordInitiated,
                Reason:       request.Reason,
                Metadata:     metadata,
                IpAddress:    requestContext.IpAddress),
            cancellationToken);

        // 12. Structured log. NEVER logs the plain code.
        logger.LogInformation(
            "Auth: Admin {AdminActorId} initiated password reset for user {TargetUserId}. Token {TokenId} issued (origin=AdminInitiated); email queued on outbox. Reason={Reason}",
            actorId,
            request.TargetUserId,
            token.Id,
            string.IsNullOrWhiteSpace(request.Reason) ? "(none provided)" : request.Reason);

        return Result<AdminResetPasswordResult>.Success(
            new AdminResetPasswordResult(SuccessMessage));
    }

    /// <summary>
    /// Phase 4 — builds the compact metadata JSON embedded in the
    /// <c>ADMIN_RESET_PASSWORD_INITIATED</c> audit row. Records the
    /// issued token id (so analytics can correlate to outbox dispatch
    /// rows) and the origin marker. The plain reset code is never
    /// included.
    /// </summary>
    private static string BuildMetadata(Guid tokenId)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            tokenId = tokenId,
            origin = "AdminInitiated"
        });
    }
}
