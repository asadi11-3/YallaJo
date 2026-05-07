using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Errors;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.SendActivationEmail;

public sealed class SendActivationEmailCommandHandler(
    IUserRegistrationService userRegistrationService,
    IActivationTokenRepository activationTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    IInviteLinkBuilder inviteLinkBuilder,
    ILogger<SendActivationEmailCommandHandler> logger)
    : ICommandHandler<SendActivationEmailCommand, SendActivationEmailResult>
{
    private const string SuccessMessage = "Activation email queued for delivery.";

    public async Task<Result<SendActivationEmailResult>> Handle(
        SendActivationEmailCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var status = await userRegistrationService.GetInviteAccountStatusAsync(
            normalizedEmail, ct);

        if (status is null)
        {
            return Result<SendActivationEmailResult>.Fail(
                Outcome.NotFound,
                "No account found for this email.",
                InviteErrors.NotFound);
        }

        if (status.Lifecycle != AccountLifecycleSnapshot.Provisioned
         && status.Lifecycle != AccountLifecycleSnapshot.PendingActivation)
        {
            return Result<SendActivationEmailResult>.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This account has already completed onboarding. A new activation email cannot be sent."),
                Outcome.Conflict);
        }

        var existing = await activationTokenRepository.GetActiveForUserAsync(
            status.UserId, ct);

        foreach (var prior in existing)
            prior.Supersede();

        var plainToken = inviteTokenService.Generate();
        var tokenHash  = inviteTokenService.Hash(plainToken);
        var link       = inviteLinkBuilder.Build(normalizedEmail, plainToken);

        var token = ActivationToken.Issue(
            userId:          status.UserId,
            tokenHash:       tokenHash,
            deliveryAddress: normalizedEmail,
            expiryMinutes:   InviteConstants.ExpiryMinutes);

        token.AddDomainEvent(new ActivationTokenIssuedEvent(
            TokenId:         token.Id,
            UserId:          token.UserId,
            DeliveryAddress: token.DeliveryAddress,
            PlainToken:      plainToken,
            ActivationLink:  link,
            ExpiresAt:       token.ExpiresAt));

        await activationTokenRepository.AddAsync(token, ct);

        var transition = await userRegistrationService.MarkPendingActivationAsync(
            status.UserId, ct);

        if (transition.IsFailure)
        {
            logger.LogError(
                "Auth: MarkPendingActivationAsync failed for user {UserId} during activation send. Token {TokenId} will still be persisted and the outbox will still dispatch; admin should inspect the lifecycle.",
                status.UserId, token.Id);

            return Result<SendActivationEmailResult>.Fail(
                transition.Outcome,
                transition.Messages.FirstOrDefault() ?? "Failed to update account lifecycle state.",
                transition.Errors.ToArray());
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Activation token {TokenId} issued for user {UserId}; email queued on outbox.",
            token.Id, status.UserId);

        return Result<SendActivationEmailResult>.Success(
            new SendActivationEmailResult(SuccessMessage));
    }
}
