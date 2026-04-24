using Accounts.Contracts.Abstractions;
using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AdminReassignAccount;

/// <summary>
/// Phase 3C — admin-initiated account reassignment. Orchestrates the
/// Security-side mutation (email retarget + password invalidation +
/// lifecycle transition) with the Auth-side credential invalidation
/// (sessions, refresh tokens, activation/reset tokens, external
/// providers) and issues a fresh activation email via the existing
/// outbox pipeline.
/// <para>
/// Cross-module atomicity: both modules commit inside a single
/// <see cref="ITransactionalExecutor"/> scope so that either everything
/// lands (Security mutation + Auth credential teardown + new activation
/// token + outbox row) or nothing does. Same shape as the Phase 2C
/// <c>ResetPasswordCommandHandler</c> / <c>VerifyEmailCommandHandler</c>
/// cross-module flows.
/// </para>
/// <para>
/// Flow:
/// </para>
/// <list type="number">
///   <item><description>Guard: actor must be authenticated.</description></item>
///   <item><description>Inside the transactional executor:
///     <list type="bullet">
///       <item><description>Call <see cref="ISecurityService.ReassignUserByAdminAsync"/> — hierarchy/self check, lifecycle gate, email uniqueness gate, domain mutation, Security save.</description></item>
///       <item><description>Revoke all active sessions + refresh tokens via <see cref="ISessionRevocationService"/> with <see cref="SessionRevocationReason.AccountReassigned"/>.</description></item>
///       <item><description>Supersede every non-terminal <see cref="ActivationToken"/> for the user.</description></item>
///       <item><description>Supersede every non-terminal <see cref="PasswordResetToken"/> for the user.</description></item>
///       <item><description>Deactivate every active <see cref="ExternalProvider"/> link for the user — prevents the old owner from signing in via Google/Facebook.</description></item>
///       <item><description>Issue a fresh <see cref="ActivationToken"/> for the NEW email, attach <see cref="ActivationTokenIssuedEvent"/>, persist. The outbox pipeline dispatches the activation email.</description></item>
///       <item><description>Phase 3D — call <see cref="IProfileReassignmentService.ResetForReassignmentAsync"/> to scrub the Accounts profile (FirstName/LastName placeholders, DisplayName = new email local-part, all optional PII cleared). A failure here rolls back Security + Auth reassignment atomically.</description></item>
///       <item><description>Single Auth <see cref="IAuthUnitOfWork.SaveChangesAsync"/> commits everything.</description></item>
///     </list>
///   </description></item>
///   <item><description>Audit log — actor id, target id, old email, new email, reason. Plain token is NEVER logged.</description></item>
/// </list>
/// <para>
/// Lockout proof (old owner cannot log in): password hash replaced
/// with an unusable placeholder; lifecycle is <c>PendingActivation</c>
/// which the login gate rejects; sessions + refresh tokens revoked;
/// prior activation/reset tokens superseded; external provider links
/// deactivated. Only the new owner can complete activation via the
/// fresh activation link on the NEW email.
/// </para>
/// </summary>
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
    ICurrentUser currentUser,
    ILogger<AdminReassignAccountCommandHandler> logger)
    : ICommandHandler<AdminReassignAccountCommand, AdminReassignAccountResult>
{
    private const string SuccessMessage = "Account reassigned. Activation email queued for delivery.";

    public async Task<Result<AdminReassignAccountResult>> Handle(
        AdminReassignAccountCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Actor must be authenticated. Endpoint authorization should
        //    already enforce this; the defensive check surfaces a clean
        //    Unauthorized for non-endpoint call sites.
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<AdminReassignAccountResult>.Failure(
                Error.Failure("Auth.Unauthenticated", "Admin actor is not authenticated."),
                Outcome.Unauthorized);
        }

        var actorId = currentUser.UserId.Value;
        var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();

        // 2. Cross-module atomic unit. On any exception the ambient
        //    TransactionScope is disposed without completion and both
        //    Security + Auth writes roll back. Transient SQL failures
        //    are retried by the execution strategy; the delegate is
        //    idempotent because each retry starts from the
        //    pre-reassignment Security state.
        var outcome = await txExecutor.ExecuteAsync<ReassignOutcome>(
            async innerCt =>
            {
                // 2a. Security-side mutation. Performs hierarchy/self
                //     check, lifecycle gate, email uniqueness gate,
                //     and the domain reassignment via
                //     User.ReassignToPendingActivation. Security's UoW
                //     commits inside this call (enlisted in the
                //     ambient scope).
                var securityResult = await securityService.ReassignUserByAdminAsync(
                    request.TargetUserId,
                    actorId,
                    normalizedNewEmail,
                    innerCt);

                if (securityResult.IsFailure)
                {
                    return ReassignOutcome.Failed(
                        securityResult.Outcome,
                        securityResult.Messages.FirstOrDefault() ?? string.Empty,
                        securityResult.Errors.ToArray());
                }

                var completed = securityResult.Value!;

                // 2b. Revoke all active sessions + refresh tokens. The
                //     service stages the tracked mutations; the single
                //     Auth SaveChangesAsync at the end of the delegate
                //     flushes them.
                await sessionRevocation.RevokeAllForUserAsync(
                    request.TargetUserId,
                    SessionRevocationReason.AccountReassigned,
                    innerCt);

                // 2c. Supersede every non-terminal ActivationToken for
                //     the user. This is the per-user "at most one
                //     active" invariant — a fresh token is issued
                //     below; prior ones must die.
                var activeActivationTokens = await activationTokenRepository.GetActiveForUserAsync(
                    request.TargetUserId, innerCt);

                foreach (var prior in activeActivationTokens)
                    prior.Supersede();

                // 2d. Supersede every non-terminal PasswordResetToken
                //     for the user. Any outstanding reset code issued
                //     to the OLD email must not be redeemable after
                //     reassignment.
                var activeResetTokens = await passwordResetTokenRepository.GetActiveForUserAsync(
                    request.TargetUserId, innerCt);

                foreach (var prior in activeResetTokens)
                    prior.Supersede();

                // 2e. Deactivate every active ExternalProvider link.
                //     Without this, the old owner could sign in via
                //     Google/Facebook despite the password + session
                //     revocation. IsActive=false also releases the
                //     filtered unique index so the link can be re-used
                //     later if needed.
                var activeProviderLinks = await externalProviderRepository.GetAllAsync(
                    filter: ep => ep.UserId == request.TargetUserId && ep.IsActive,
                    asNoTracking: false,
                    ct: innerCt);

                foreach (var providerLink in activeProviderLinks)
                    providerLink.Deactivate();

                // 2f. Issue a fresh ActivationToken for the NEW email.
                //     Same shape as SendActivationEmailCommandHandler:
                //     generate plain token + hash + activation link,
                //     create the aggregate in Issued/Pending, attach
                //     ActivationTokenIssuedEvent carrying the plain
                //     token + link. The
                //     ActivationTokenIssuedDomainEventHandler
                //     translates the event into an OutboxMessage in
                //     the same AuthDbContext, so token + supersede
                //     sweep + outbox row commit atomically on the
                //     single SaveChanges below.
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

                // 2g. Phase 3D — scrub the Accounts profile. Runs
                //     inside the same ambient TransactionScope: a
                //     failure returned here (or an exception thrown
                //     by the service) rolls back Security + Auth
                //     reassignment atomically. Missing profile is a
                //     logged no-op (idempotent) per Phase 3D
                //     contract.
                var profileReset = await profileReassignmentService.ResetForReassignmentAsync(
                    new ProfileReassignmentRequest(
                        UserId:   request.TargetUserId,
                        NewEmail: completed.NewEmail),
                    innerCt);

                if (profileReset.IsFailure)
                {
                    return ReassignOutcome.Failed(
                        profileReset.Outcome,
                        profileReset.Messages.FirstOrDefault() ?? string.Empty,
                        profileReset.Errors.ToArray());
                }

                // 2h. Single Auth SaveChanges commits: session/refresh
                //     revocations, activation token supersede sweep,
                //     password-reset token supersede sweep, external
                //     provider deactivations, new activation token,
                //     and the outbox row emitted by the domain-event
                //     dispatcher. The Accounts UoW SaveChanges above
                //     already flushed inside the same ambient scope.
                await unitOfWork.SaveChangesAsync(innerCt);

                return ReassignOutcome.Ok(
                    activationToken.Id,
                    activeActivationTokens.Count,
                    activeResetTokens.Count,
                    activeProviderLinks.Count,
                    completed.OldEmail,
                    completed.NewEmail);
            },
            cancellationToken);

        if (!outcome.Success)
        {
            return Result<AdminReassignAccountResult>.Fail(
                outcome.FailureOutcome,
                outcome.FailureMessage,
                outcome.FailureErrors);
        }

        // 3. Structured audit log. NEVER logs the plain activation
        //    token — the event payload already carries it to the
        //    outbox dispatcher; the audit trail only records what
        //    happened and to whom.
        logger.LogInformation(
            "Auth: Admin {AdminActorId} reassigned user {TargetUserId} from {OldEmail} to {NewEmail}. " +
            "Activation token {TokenId} issued; {ActivationsSuperseded} activation token(s), " +
            "{ResetsSuperseded} reset token(s), {ProvidersDeactivated} external provider link(s) invalidated. " +
            "Accounts profile scrubbed (ProfileReset=true). Reason={Reason}",
            actorId,
            request.TargetUserId,
            outcome.OldEmail,
            outcome.NewEmail,
            outcome.ActivationTokenId,
            outcome.ActivationsSuperseded,
            outcome.ResetsSuperseded,
            outcome.ProvidersDeactivated,
            string.IsNullOrWhiteSpace(request.Reason) ? "(none provided)" : request.Reason);

        return Result<AdminReassignAccountResult>.Success(
            new AdminReassignAccountResult(SuccessMessage));
    }

    /// <summary>
    /// Local outcome record so the transactional delegate can carry
    /// either success metadata (for audit) or a typed failure payload
    /// out of the executor without throwing for expected business
    /// refusals (which would otherwise roll back the scope and obscure
    /// the original Outcome).
    /// </summary>
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
        string NewEmail)
    {
        public static ReassignOutcome Ok(
            Guid activationTokenId,
            int activationsSuperseded,
            int resetsSuperseded,
            int providersDeactivated,
            string oldEmail,
            string newEmail) =>
            new(true, Outcome.Ok, string.Empty, Array.Empty<Error>(),
                activationTokenId, activationsSuperseded, resetsSuperseded, providersDeactivated,
                oldEmail, newEmail);

        public static ReassignOutcome Failed(Outcome outcome, string message, Error[] errors) =>
            new(false, outcome, message, errors,
                Guid.Empty, 0, 0, 0, string.Empty, string.Empty);
    }
}
