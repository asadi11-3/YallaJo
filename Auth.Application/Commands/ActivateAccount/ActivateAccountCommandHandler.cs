using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ActivateAccount;

/// <summary>
/// Phase 2C-1 — redeems an activation token using the new
/// <see cref="ActivationToken"/> aggregate, with a fallback to the legacy
/// <c>Otp(Purpose="UserInvite")</c> row for activation links that were
/// issued by Phase 2B <c>SendActivationEmail</c> before the 2C-1 deploy.
/// <para>
/// Validation order (ActivationToken path):
/// </para>
/// <list type="number">
///   <item><description>Resolve the account snapshot via <see cref="IUserRegistrationService.GetInviteAccountStatusAsync"/>. Refuse if missing or already onboarded.</description></item>
///   <item><description>Load the most recent non-terminal <see cref="ActivationToken"/>. If none exists, fall through to the legacy Otp path.</description></item>
///   <item><description>Check exhaustion and expiry BEFORE incrementing the attempt counter (attempt-leak invariant).</description></item>
///   <item><description>Increment the attempt counter, compare hashes.</description></item>
///   <item><description>On hash match: delegate to <see cref="IUserRegistrationService.CompleteActivationAsync"/> (password set, email verified, <c>PendingActivation → Active</c>). On success, <see cref="ActivationToken.Consume"/> the token and any outstanding siblings get <see cref="ActivationToken.Supersede"/>d defensively (there should be none because of the supersede step in send).</description></item>
///   <item><description>Revoke sessions via <see cref="ISessionRevocationService"/> (<c>AccountActivated</c> reason — the invariant is uniform across activation, self-service reset, and future admin verbs).</description></item>
/// </list>
/// <para>
/// Legacy Otp fallback: identical logic against the Otp row, preserved
/// verbatim from Phase 2B so existing activation links that were
/// persisted as Otp(UserInvite) continue to work until the 7-day window
/// elapses. A follow-up phase (2C-2) can drop this path.
/// </para>
/// </summary>
public sealed class ActivateAccountCommandHandler(
    IUserRegistrationService userRegistrationService,
    IActivationTokenRepository activationTokenRepository,
    IOtpRepository otpRepository,
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

        // 2. Try the ActivationToken aggregate first.
        var token = await activationTokenRepository.GetLatestActiveForUserAsync(
            status.UserId, cancellationToken);

        if (token is not null)
        {
            return await ActivateViaActivationTokenAsync(
                token, status.UserId, normalizedEmail, request, cancellationToken);
        }

        // 3. Fallback: legacy Otp(UserInvite) row from Phase 2B.
        return await ActivateViaLegacyOtpAsync(
            status.UserId, normalizedEmail, request, cancellationToken);
    }

    // ── ActivationToken path (Phase 2C-1) ────────────────────────────────────

    private async Task<Result<ActivateAccountResult>> ActivateViaActivationTokenAsync(
        ActivationToken token,
        Guid userId,
        string normalizedEmail,
        ActivateAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Domain invariants — checked BEFORE the attempt counter is
        // incremented so the budget cannot be leaked by a bad-faith
        // probe of an already-exhausted token.
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
            // Persist the attempt so the rate-limit counter reflects the try.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ActivateAccountResult>.Failure(
                Error.Validation("Invite.Invalid", "Invalid invite token."),
                Outcome.Invalid);
        }

        // Finalize activation in Security (sets password, verifies email,
        // performs the explicit PendingActivation -> Active transition).
        var completion = await userRegistrationService.CompleteActivationAsync(
            userId, normalizedEmail, request.Password, cancellationToken);

        if (completion.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // keep the attempt counter honest
            return Result<ActivateAccountResult>.Fail(
                completion.Outcome,
                completion.Messages.FirstOrDefault() ?? "Could not activate account.",
                completion.Errors.ToArray());
        }

        token.Consume();

        // Defensive: supersede any sibling Issued/Delivered tokens. The
        // send-activation sweep should have prevented more than one, but
        // if a race slipped through we clean up here before the flush.
        var siblings = await activationTokenRepository.GetActiveForUserAsync(userId, cancellationToken);
        foreach (var sibling in siblings.Where(s => s.Id != token.Id))
            sibling.Supersede();

        // Defensive: if any legacy Otp(UserInvite) rows exist alongside,
        // mark them used. Prevents an in-flight legacy link from being
        // redeemable after activation.
        var legacy = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId
                      && o.Purpose == InviteConstants.Purpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: cancellationToken);
        foreach (var old in legacy)
            old.MarkUsed();

        // Credential-event invariant — revoke any existing sessions.
        await sessionRevocation.RevokeAllForUserAsync(
            userId, SessionRevocationReason.AccountActivated, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ActivateAccountResult>.Success(new ActivateAccountResult(userId));
    }

    // ── Legacy Otp(UserInvite) fallback (to be dropped in Phase 2C-2) ────────

    private async Task<Result<ActivateAccountResult>> ActivateViaLegacyOtpAsync(
        Guid userId,
        string normalizedEmail,
        ActivateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var invite = await otpRepository.FirstOrDefaultAsync(
            filter:  o => o.UserId == userId
                       && o.Purpose == InviteConstants.Purpose
                       && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false,
            ct: cancellationToken);

        if (invite is null)
        {
            return Result<ActivateAccountResult>.Failure(
                Error.NotFound("Invite.NotFound", "This invite is invalid or has expired."),
                Outcome.NotFound);
        }

        if (invite.IsExhausted)
        {
            return Result<ActivateAccountResult>.Fail(
                Outcome.TooManyRequests,
                "Too many invalid attempts. Please request a new invite.");
        }

        if (invite.IsExpired())
        {
            return Result<ActivateAccountResult>.Failure(
                Error.Validation(
                    "Invite.Expired",
                    "This invite has expired. Please ask an administrator to resend it."),
                Outcome.Invalid);
        }

        invite.IncrementAttempt();

        if (!inviteTokenService.Verify(request.Token, invite.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ActivateAccountResult>.Failure(
                Error.Validation("Invite.Invalid", "Invalid invite token."),
                Outcome.Invalid);
        }

        var completion = await userRegistrationService.CompleteActivationAsync(
            userId, normalizedEmail, request.Password, cancellationToken);

        if (completion.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<ActivateAccountResult>.Fail(
                completion.Outcome,
                completion.Messages.FirstOrDefault() ?? "Could not activate account.",
                completion.Errors.ToArray());
        }

        invite.MarkUsed();

        var otherInvites = await otpRepository.GetAllAsync(
            filter: o => o.UserId == userId
                      && o.Purpose == InviteConstants.Purpose
                      && !o.IsUsed
                      && o.Id != invite.Id,
            asNoTracking: false,
            ct: cancellationToken);
        foreach (var other in otherInvites)
            other.MarkUsed();

        await sessionRevocation.RevokeAllForUserAsync(
            userId, SessionRevocationReason.AccountActivated, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ActivateAccountResult>.Success(new ActivateAccountResult(userId));
    }
}
