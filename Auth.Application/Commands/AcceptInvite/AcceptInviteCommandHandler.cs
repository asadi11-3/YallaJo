using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AcceptInvite;

public sealed class AcceptInviteCommandHandler(
    IUserRegistrationService userRegistrationService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    ISessionRevocationService sessionRevocation)
    : ICommandHandler<AcceptInviteCommand, AcceptInviteResult>
{
    public async Task<Result<AcceptInviteResult>> Handle(
        AcceptInviteCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Look up the account status via the Security contract.
        var status = await userRegistrationService.GetInviteAccountStatusAsync(normalizedEmail, cancellationToken);
        if (status is null)
        {
            return Result<AcceptInviteResult>.Failure(
                Error.NotFound("Invite.NotFound", "This invite is invalid or has expired."),
                Outcome.NotFound);
        }

        if (status.IsActive || status.IsEmailVerified)
        {
            return Result<AcceptInviteResult>.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This invite has already been accepted. Please sign in instead."),
                Outcome.Conflict);
        }

        // 2. Locate the latest unused invite token for this user.
        var invite = await otpRepository.FirstOrDefaultAsync(
            filter:  o => o.UserId == status.UserId
                       && o.Purpose == InviteConstants.Purpose
                       && !o.IsUsed,
            orderBy: q => q.OrderByDescending(o => o.CreatedAt),
            asNoTracking: false,
            ct: cancellationToken);

        if (invite is null)
        {
            return Result<AcceptInviteResult>.Failure(
                Error.NotFound("Invite.NotFound", "This invite is invalid or has expired."),
                Outcome.NotFound);
        }

        // 3. Domain invariants on the token.
        if (invite.IsExhausted)
        {
            return Result<AcceptInviteResult>.Fail(
                Outcome.TooManyRequests,
                "Too many invalid attempts. Please request a new invite.");
        }

        if (invite.IsExpired())
        {
            return Result<AcceptInviteResult>.Failure(
                Error.Validation(
                    "Invite.Expired",
                    "This invite has expired. Please ask an administrator to resend it."),
                Outcome.Invalid);
        }

        invite.IncrementAttempt();

        if (!inviteTokenService.Verify(request.Token, invite.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // persist the attempt
            return Result<AcceptInviteResult>.Failure(
                Error.Validation("Invite.Invalid", "Invalid invite token."),
                Outcome.Invalid);
        }

        // 4. Finalize in Security (password + email verified + active) atomically.
        var completion = await userRegistrationService.CompleteInviteAsync(
            status.UserId, normalizedEmail, request.Password, cancellationToken);

        if (completion.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); // still persist attempt counter
            return Result<AcceptInviteResult>.Fail(
                completion.Outcome,
                completion.Messages.FirstOrDefault() ?? "Could not accept invite.",
                completion.Errors.ToArray());
        }

        // 5. Consume the invite token + any other outstanding tokens for this user.
        invite.MarkUsed();

        var otherInvites = await otpRepository.GetAllAsync(
            filter: o => o.UserId == status.UserId
                      && o.Purpose == InviteConstants.Purpose
                      && !o.IsUsed
                      && o.Id != invite.Id,
            asNoTracking: false,
            ct: cancellationToken);

        foreach (var other in otherInvites)
            other.MarkUsed();

        // 6. Defensive credential-event invariant: ANY flow that establishes
        //    or mutates a user's password MUST tear down any session that
        //    might already be attached to that user — otherwise a stale
        //    session could outlive the credential event. For activation this
        //    is effectively a no-op today (the user has never logged in),
        //    but wiring it here makes the invariant uniform across
        //    activation, self-service reset, and (Phase 2+) admin reset /
        //    reassignment. Cheap to call, costly to forget.
        await sessionRevocation.RevokeAllForUserAsync(
            status.UserId,
            SessionRevocationReason.AccountActivated,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcceptInviteResult>.Success(new AcceptInviteResult(status.UserId));
    }
}
