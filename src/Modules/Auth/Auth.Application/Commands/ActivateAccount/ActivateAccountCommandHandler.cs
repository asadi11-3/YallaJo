using Auth.Application.Caching;
using Auth.Application.Interfaces;
using Auth.Application.Interfaces.SessionRevocation;
using Auth.Domain.Entities;
using Auth.Domain.Errors;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ActivateAccount;

public sealed class ActivateAccountCommandHandler(
    IUserRegistrationService userRegistrationService,
    IActivationTokenRepository activationTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    ISessionRevocationService sessionRevocation,
    HybridCache cache)
    : ICommandHandler<ActivateAccountCommand, ActivateAccountResult>
{
    public async Task<Result<ActivateAccountResult>> Handle(
        ActivateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var status = await userRegistrationService.GetInviteAccountStatusAsync(
            normalizedEmail, cancellationToken);

        if (status is null)
        {
            return Result<ActivateAccountResult>.Fail(
                Outcome.NotFound,
                "This invite is invalid or has expired.",
                InviteErrors.NotFound);
        }

        if (status.IsActive || status.IsEmailVerified)
        {
            return Result<ActivateAccountResult>.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This invite has already been accepted. Please sign in instead."),
                Outcome.Conflict);
        }

        var token = await activationTokenRepository.GetLatestActiveForUserAsync(
            status.UserId, cancellationToken);

        if (token is null)
        {
            return Result<ActivateAccountResult>.Fail(
                Outcome.NotFound,
                "This invite is invalid or has expired.",
                InviteErrors.NotFound);
        }

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

        token.Consume();

        var siblings = await activationTokenRepository.GetActiveForUserAsync(status.UserId, cancellationToken);
        foreach (var sibling in siblings.Where(s => s.Id != token.Id))
            sibling.Supersede();

        await sessionRevocation.RevokeAllForUserAsync(
            status.UserId,
            SessionRevocationReason.AccountActivated,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(
            AuthCacheKeys.UserSessionsTag(status.UserId), cancellationToken);

        return Result<ActivateAccountResult>.Success(new ActivateAccountResult(status.UserId));
    }
}
