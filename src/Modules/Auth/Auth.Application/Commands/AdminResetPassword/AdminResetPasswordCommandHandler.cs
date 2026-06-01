using Auth.Application.Caching;
using Auth.Application.Errors;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AdminResetPassword;

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
    HybridCache cache,
    ILogger<AdminResetPasswordCommandHandler> logger)
    : ICommandHandler<AdminResetPasswordCommand, AdminResetPasswordResult>
{
    private const string SuccessMessage = "Password reset email queued.";
    private const int    ExpiryMinutes   = 10;

    public async Task<Result<AdminResetPasswordResult>> Handle(
        AdminResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<AdminResetPasswordResult>.Failure(
                AuthErrors.AdminUnauthenticated,
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;

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

        if (!snapshot.IsPrimaryEmailVerified || string.IsNullOrWhiteSpace(snapshot.PrimaryEmail))
        {
            return Result<AdminResetPasswordResult>.Failure(
                Error.Conflict(
                    "User.EmailUnverified",
                    "Target account does not have a verified primary email — cannot send a password reset."),
                Outcome.Conflict);
        }

        var normalizedEmail = snapshot.PrimaryEmail.Trim().ToLowerInvariant();

        var active = await resetTokenRepository.GetActiveForUserAsync(
            request.TargetUserId, cancellationToken);

        foreach (var prior in active)
            prior.Supersede();

        var plainCode = otpService.Generate();
        var codeHash  = otpService.Hash(plainCode);

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

        var transition = await userRegistrationService.MarkPendingPasswordResetAsync(
            request.TargetUserId, cancellationToken);

        if (transition.IsFailure)
        {
            return Result<AdminResetPasswordResult>.Fail(
                transition.Outcome,
                transition.Messages.FirstOrDefault() ?? "Failed to update account lifecycle state.",
                transition.Errors.ToArray());
        }

        await sessionRevocation.RevokeAllForUserAsync(
            request.TargetUserId,
            SessionRevocationReason.PasswordResetByAdmin,
            cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<AdminResetPasswordResult>.Failure(
                new Error("PasswordResetToken.ConcurrencyConflict", "Password reset state was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(request.TargetUserId), cancellationToken);

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

        logger.LogInformation(
            "Auth: Admin {AdminActorId} initiated password reset for user {TargetUserId}. Token {TokenId} issued (origin=AdminInitiated); email queued on outbox. Reason={Reason}",
            actorId,
            request.TargetUserId,
            token.Id,
            string.IsNullOrWhiteSpace(request.Reason) ? "(none provided)" : request.Reason);

        return Result<AdminResetPasswordResult>.Success(
            new AdminResetPasswordResult(SuccessMessage));
    }

    private static string BuildMetadata(Guid tokenId)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            tokenId = tokenId,
            origin = "AdminInitiated"
        });
    }
}
