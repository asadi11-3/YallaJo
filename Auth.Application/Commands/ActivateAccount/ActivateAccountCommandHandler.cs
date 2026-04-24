using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ActivateAccount;

/// <summary>
/// Phase 2C-5 — final cutover. The legacy
/// <c>Otp(Purpose="UserInvite")</c> fallback path is removed;
/// activation now validates ActivationToken rows exclusively.
/// <para>
/// Any in-flight activation link issued by the Phase 2B / 2C-0
/// inline-SMTP pipeline would, after this cutover, fail to redeem. Per
/// the operator's decision (no real users have outstanding legacy
/// links), that exposure is accepted.
/// </para>
/// <para>
/// Flow:
/// </para>
/// <list type="number">
///   <item><description>Resolve the account snapshot; refuse if missing or already onboarded.</description></item>
///   <item><description>Load the most recent non-terminal <see cref="ActivationToken"/>. Absence is a hard <c>NotFound</c> — no silent fallback.</description></item>
///   <item><description>Check exhaustion and expiry BEFORE incrementing the attempt counter.</description></item>
///   <item><description>Increment attempt; compare token hash; persist attempt even on mismatch so the rate-limit counter stays honest.</description></item>
///   <item><description>On hash match: finalize in Security via <see cref="IUserRegistrationService.CompleteActivationAsync"/> (sets password, verifies email, PendingActivation → Active), <see cref="ActivationToken.Consume"/> the redeemed token, defensively supersede any sibling Issued/Delivered tokens.</description></item>
///   <item><description>Revoke any residual sessions via <see cref="ISessionRevocationService"/> with <see cref="SessionRevocationReason.AccountActivated"/> — invariant uniform across every credential-establishing flow.</description></item>
/// </list>
/// </summary>
public sealed class ActivateAccountCommandHandler(
    IUserRegistrationService userRegistrationService,
    IActivationTokenRepository activationTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    ISessionRevocationService sessionRevocation)
    : ICommandHandler<ActivateAccountCommand, ActivateAccountResult>
{
    public async Task<Result<ActivateAccountResult>> Handle(
        ActivateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Account snapshot.
        var status = await userRegistrationService.GetInviteAccountStatusAsync(
            normalizedEmail, cancellationToken);

        if (status is null)
        {
            return Result<ActivateAccountResult>.Failure(
                Error.NotFound("Invite.NotFound", "This invite is invalid or has expired."),
                Outcome.NotFound);
        }

        if (status.IsActive || status.IsEmailVerified)
        {
            return Result<ActivateAccountResult>.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This invite has already been accepted. Please sign in instead."),
                Outcome.Conflict);
        }

        // 2. Load the latest non-terminal ActivationToken. No legacy
        //    Otp fallback — absence is a hard NotFound.
        var token = await activationTokenRepository.GetLatestActiveForUserAsync(
            status.UserId, cancellationToken);

        if (token is null)
        {
            return Result<ActivateAccountResult>.Failure(
                Error.NotFound("Invite.NotFound", "This invite is invalid or has expired."),
                Outcome.NotFound);
        }

        // 3. Attempt-budget and expiry checks BEFORE the increment so a
        //    bad-faith probe of an already-exhausted token cannot leak
        //    one more attempt.
        if (token.IsExhausted)
        {
            return Result<ActivateAccountResult>.Fail(
                Outcome.TooManyRequests,
                "Too many invalid attempts. Please request a new invite.");
        }

        if (token.IsExpired())
        {
            return Result<ActivateAccountResult>.Failure(
                Error.Validation(
                    "Invite.Expired",
                    "This invite has expired. Please ask an administrator to resend it."),
                Outcome.Invalid);
        }

        token.IncrementAttempt();

        if (!inviteTokenService.Verify(request.Token, token.TokenHash))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // persist the attempt
            return Result<ActivateAccountResult>.Failure(
                Error.Validation("Invite.Invalid", "Invalid invite token."),
                Outcome.Invalid);
        }

        // 4. Finalize in Security (sets password, verifies email,
        //    performs the explicit PendingActivation -> Active transition).
        var completion = await userRegistrationService.CompleteActivationAsync(
            status.UserId, normalizedEmail, request.Password, cancellationToken);

        if (completion.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // keep the attempt counter honest
            return Result<ActivateAccountResult>.Fail(
                completion.Outcome,
                completion.Messages.FirstOrDefault() ?? "Could not activate account.",
                completion.Errors.ToArray());
        }

        // 5. Consume the redeemed token + defensively supersede any
        //    siblings. The send-activation sweep should prevent more
        //    than one active at a time, but if a race slipped through
        //    we clean up here before the flush.
        token.Consume();

        var siblings = await activationTokenRepository.GetActiveForUserAsync(status.UserId, cancellationToken);
        foreach (var sibling in siblings.Where(s => s.Id != token.Id))
            sibling.Supersede();

        // 6. Credential-event invariant — revoke any existing sessions.
        await sessionRevocation.RevokeAllForUserAsync(
            status.UserId,
            SessionRevocationReason.AccountActivated,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ActivateAccountResult>.Success(new ActivateAccountResult(status.UserId));
    }
}
