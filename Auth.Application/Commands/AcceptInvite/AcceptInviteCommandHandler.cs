using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.AcceptInvite;

/// <summary>
/// Invitee-facing invite-acceptance orchestration.
/// <para>
/// Validates the invite token (presence, not-used, not-expired, attempts not
/// exhausted, hash match), then delegates to
/// <see cref="IUserRegistrationService.CompleteInviteAsync"/> in Security to:
/// set the password, mark the primary email verified, and activate the
/// account — atomically inside Security.
/// </para>
/// </summary>
public sealed class AcceptInviteCommandHandler(
    IUserRegistrationService userRegistrationService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService)
    : ICommandHandler<AcceptInviteCommand, AcceptInviteResult>
{
    public async Task<Result<AcceptInviteResult>> Handle(
        AcceptInviteCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Look up the account status via the Security contract.
        var status = await userRegistrationService.GetInviteAccountStatusAsync(normalizedEmail, ct);
        if (status is null)
        {
            return Result<AcceptInviteResult>.Failure(
                Error.NotFound("Invite.NotFound", "This invite is invalid or has expired."),
                Outcome.NotFound);
        }

        if (status.IsActive || status.IsEmailVerified)
        {
            return Result<AcceptInviteResult>.Failure(
                Error.Conflict("Invite.AlreadyCompleted",
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
            ct: ct);

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
                Error.Validation("Invite.Expired",
                    "This invite has expired. Please ask an administrator to resend it."),
                Outcome.Invalid);
        }

        invite.IncrementAttempt();

        if (!inviteTokenService.Verify(request.Token, invite.CodeHash))
        {
            await unitOfWork.SaveChangesAsync(ct); // persist the attempt
            return Result<AcceptInviteResult>.Failure(
                Error.Validation("Invite.Invalid", "Invalid invite token."),
                Outcome.Invalid);
        }

        // 4. Finalize in Security (password + email verified + active) atomically.
        var completion = await userRegistrationService.CompleteInviteAsync(
            status.UserId, normalizedEmail, request.Password, ct);

        if (completion.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(ct); // still persist attempt counter
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
            ct: ct);

        foreach (var other in otherInvites)
            other.MarkUsed();

        await unitOfWork.SaveChangesAsync(ct);

        return Result<AcceptInviteResult>.Success(new AcceptInviteResult(status.UserId));
    }
}
