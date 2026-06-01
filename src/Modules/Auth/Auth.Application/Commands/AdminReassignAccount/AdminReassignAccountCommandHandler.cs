using Accounts.Contracts.Abstractions;
using Auth.Application.Caching;
using Auth.Application.Errors;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Application.Invitations;
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

namespace Auth.Application.Commands.AdminReassignAccount;

public sealed class AdminReassignAccountCommandHandler(
    ISecurityService securityService,
    IActivationTokenRepository activationTokenRepository,
    IPasswordResetTokenRepository passwordResetTokenRepository,
    IExternalProviderRepository externalProviderRepository,
    ISessionRevocationService sessionRevocation,
    IAuthUnitOfWork unitOfWork,
    ITransactionalExecutor txExecutor,
    IInviteTokenService inviteTokenService,
    IInviteLinkBuilder inviteLinkBuilder,
    IProfileReassignmentService profileReassignmentService,
    IAdminAuditWriter adminAuditWriter,
    IRequestContext requestContext,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<AdminReassignAccountCommandHandler> logger)
    : ICommandHandler<AdminReassignAccountCommand, AdminReassignAccountResult>
{
    private const string SuccessMessage = "Account reassigned. Activation email queued for delivery.";

    public async Task<Result<AdminReassignAccountResult>> Handle(
        AdminReassignAccountCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<AdminReassignAccountResult>.Failure(
                AuthErrors.AdminUnauthenticated,
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;
        var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();

        ReassignOutcome outcome;
        try
        {
            outcome = await txExecutor.ExecuteAsync<ReassignOutcome>(
                async innerCt =>
                {
                    var securityResult = await securityService.ReassignUserByAdminAsync(
                        request.TargetUserId,
                        actorId,
                        normalizedNewEmail,
                        innerCt);

                    if (securityResult.IsFailure)
                    {
                        return ReassignOutcome.Failed(
                            securityResult.Outcome,
                            securityResult.Messages.Count > 0 ? securityResult.Messages[0] : string.Empty,
                            securityResult.Errors.ToArray());
                    }

                    var completed = securityResult.Value!;
                    await sessionRevocation.RevokeAllForUserAsync(
                        request.TargetUserId,
                        SessionRevocationReason.AccountReassigned,
                        innerCt);

                    var activeActivationTokens = await activationTokenRepository.GetActiveForUserAsync(
                        request.TargetUserId, innerCt);

                    foreach (var prior in activeActivationTokens)
                        prior.Supersede();

                    var activeResetTokens = await passwordResetTokenRepository.GetActiveForUserAsync(
                        request.TargetUserId, innerCt);

                    foreach (var prior in activeResetTokens)
                        prior.Supersede();

                    var activeProviderLinks = await externalProviderRepository.GetAllAsync(
                        filter: ep => ep.UserId == request.TargetUserId && ep.IsActive,
                        asNoTracking: false,
                        ct: innerCt);

                    foreach (var providerLink in activeProviderLinks)
                        providerLink.Deactivate();

                    var plainToken = inviteTokenService.Generate();
                    var tokenHash  = inviteTokenService.Hash(plainToken);
                    var link       = inviteLinkBuilder.Build(completed.NewEmail, plainToken);

                    var activationToken = ActivationToken.Issue(
                        userId:          request.TargetUserId,
                        tokenHash:       tokenHash,
                        deliveryAddress: completed.NewEmail,
                        expiryMinutes:   InviteConstants.ExpiryMinutes);

                    activationToken.AddDomainEvent(new ActivationTokenIssuedEvent(
                        TokenId:         activationToken.Id,
                        UserId:          activationToken.UserId,
                        DeliveryAddress: activationToken.DeliveryAddress,
                        PlainToken:      plainToken,
                        ActivationLink:  link,
                        ExpiresAt:       activationToken.ExpiresAt));

                    await activationTokenRepository.AddAsync(activationToken, innerCt);

                    var profileReset = await profileReassignmentService.ResetForReassignmentAsync(
                        new ProfileReassignmentRequest(
                            UserId:   request.TargetUserId,
                            NewEmail: completed.NewEmail),
                        innerCt);

                    if (profileReset.IsFailure)
                    {
                        return ReassignOutcome.Failed(
                            profileReset.Outcome,
                            profileReset.Messages.Count > 0 ? profileReset.Messages[0] : string.Empty,
                            profileReset.Errors.ToArray());
                    }

                    var profileScrubbed = profileReset.Value!.Scrubbed;

                    var metadata = BuildReassignMetadata(
                        oldEmail:              completed.OldEmail,
                        newEmail:              completed.NewEmail,
                        lifecycleFrom:         "Active|Suspended|PendingPasswordReset",
                        lifecycleTo:           completed.Lifecycle.ToString(),
                        activationsSuperseded: activeActivationTokens.Count,
                        resetsSuperseded:      activeResetTokens.Count,
                        providersDeactivated:  activeProviderLinks.Count,
                        profileScrubbed:       profileScrubbed);

                    await adminAuditWriter.RecordAsync(
                        new AdminAuditEntry(
                            ActorUserId:  actorId,
                            TargetUserId: request.TargetUserId,
                            Action:       AuditActions.AdminReassignAccount,
                            Reason:       request.Reason,
                            Metadata:     metadata,
                            IpAddress:    requestContext.IpAddress),
                        innerCt);

                    await unitOfWork.SaveChangesAsync(innerCt);

                    return ReassignOutcome.Ok(
                        activationToken.Id,
                        activeActivationTokens.Count,
                        activeResetTokens.Count,
                        activeProviderLinks.Count,
                        completed.OldEmail,
                        completed.NewEmail,
                        profileScrubbed);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<AdminReassignAccountResult>.Failure(
                new Error("ActivationToken.ConcurrencyConflict", "Activation state was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        if (!outcome.Success)
        {
            return Result<AdminReassignAccountResult>.Fail(
                outcome.FailureOutcome,
                outcome.FailureMessage,
                outcome.FailureErrors);
        }

        await cache.RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(request.TargetUserId), cancellationToken);

        logger.LogInformation(
            "Auth: Admin {AdminActorId} reassigned user {TargetUserId} from {OldEmail} to {NewEmail}. " +
            "Activation token {TokenId} issued; {ActivationsSuperseded} activation token(s), " +
            "{ResetsSuperseded} reset token(s), {ProvidersDeactivated} external provider link(s) invalidated. " +
            "Accounts profile scrubbed (ProfileReset={ProfileScrubbed}). Reason={Reason}",
            actorId,
            request.TargetUserId,
            outcome.OldEmail,
            outcome.NewEmail,
            outcome.ActivationTokenId,
            outcome.ActivationsSuperseded,
            outcome.ResetsSuperseded,
            outcome.ProvidersDeactivated,
            outcome.ProfileScrubbed,
            string.IsNullOrWhiteSpace(request.Reason) ? "(none provided)" : request.Reason);

        return Result<AdminReassignAccountResult>.Success(
            new AdminReassignAccountResult(SuccessMessage));
    }

    private static string BuildReassignMetadata(
        string oldEmail,
        string newEmail,
        string lifecycleFrom,
        string lifecycleTo,
        int activationsSuperseded,
        int resetsSuperseded,
        int providersDeactivated,
        bool profileScrubbed)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            oldEmail              = oldEmail,
            newEmail              = newEmail,
            lifecycleFrom         = lifecycleFrom,
            lifecycleTo           = lifecycleTo,
            activationsSuperseded = activationsSuperseded,
            resetsSuperseded      = resetsSuperseded,
            providersDeactivated  = providersDeactivated,
            profileScrubbed       = profileScrubbed
        });
    }

    private readonly record struct ReassignOutcome(
        bool Success,
        Outcome FailureOutcome,
        string FailureMessage,
        Error[] FailureErrors,
        Guid ActivationTokenId,
        int ActivationsSuperseded,
        int ResetsSuperseded,
        int ProvidersDeactivated,
        string OldEmail,
        string NewEmail,
        bool ProfileScrubbed)
    {
        public static ReassignOutcome Ok(
            Guid activationTokenId,
            int activationsSuperseded,
            int resetsSuperseded,
            int providersDeactivated,
            string oldEmail,
            string newEmail,
            bool profileScrubbed) =>
            new(true, Outcome.Ok, string.Empty, Array.Empty<Error>(),
                activationTokenId, activationsSuperseded, resetsSuperseded, providersDeactivated,
                oldEmail, newEmail, profileScrubbed);

        public static ReassignOutcome Failed(Outcome outcome, string message, Error[] errors) =>
            new(false, outcome, message, errors,
                Guid.Empty, 0, 0, 0, string.Empty, string.Empty, false);
    }
}
