using Accounts.Contracts.Abstractions;
using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.InviteUser;

/// <summary>
/// Admin-initiated user-invite orchestration.
/// <para>
/// Creates the Security identity in an invited/pending state (no password,
/// email unverified, inactive), creates the Accounts profile, generates a
/// long-lived invite token, and sends the invite link by email.
/// </para>
/// <para>
/// Writes only to <c>Auth.Otp</c> directly; Security &amp; Accounts are touched
/// strictly through their respective contract services.
/// </para>
/// </summary>
public sealed class InviteUserCommandHandler(
    IUserRegistrationService userRegistrationService,
    IProfileCreationService profileCreationService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    IInviteLinkBuilder inviteLinkBuilder,
    IEmailService emailService,
    ILogger<InviteUserCommandHandler> logger)
    : ICommandHandler<InviteUserCommand, InviteUserResult>
{
    public async Task<Result<InviteUserResult>> Handle(
        InviteUserCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Create the Security identity in invited state (no password,
        //    unverified email, inactive).
        var registrationResult = await userRegistrationService.RegisterInvitedAsync(
            new InvitedUserRegistrationRequest(
                request.FirstName,
                request.LastName,
                normalizedEmail,
                request.InitialRoleIds),
            ct);

        if (registrationResult.IsFailure)
        {
            return Result<InviteUserResult>.Fail(
                registrationResult.Outcome,
                registrationResult.Messages.FirstOrDefault() ?? string.Empty,
                registrationResult.Errors.ToArray());
        }

        var userId = registrationResult.Value;

        // 2. Create the Accounts profile linked to the new identity.
        var profileResult = await profileCreationService.CreateForInvitedUserAsync(
            new InvitedProfileCreationRequest(
                UserId:      userId,
                FirstName:   request.FirstName,
                LastName:    request.LastName,
                DisplayName: string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
                AvatarUrl:   string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim()),
            ct);

        if (profileResult.IsFailure)
        {
            return Result<InviteUserResult>.Fail(
                profileResult.Outcome,
                profileResult.Messages.FirstOrDefault() ?? string.Empty,
                profileResult.Errors.ToArray());
        }

        // 3. Generate + store invite token (hash only), issue link, send email.
        var plainToken = inviteTokenService.Generate();
        var tokenHash  = inviteTokenService.Hash(plainToken);

        var invite = Otp.Create(
            userId:          userId,
            purpose:         InviteConstants.Purpose,
            codeHash:        tokenHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail,
            expiryMinutes:   InviteConstants.ExpiryMinutes);

        await otpRepository.AddAsync(invite, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var link = inviteLinkBuilder.Build(normalizedEmail, plainToken);

        try
        {
            await emailService.SendAsync(
                normalizedEmail,
                "YallaJo — You're invited",
                $"Hello {request.FirstName},\n\nYou have been invited to join YallaJo.\n" +
                $"Click the link below to set your password and activate your account:\n\n{link}\n\n" +
                $"This link expires in {InviteConstants.ExpiryMinutes / 60 / 24} days.",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            invite.MarkUsed();
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogError(ex,
                "Auth: Failed to send invite email to {Email}. Invite token invalidated — use ResendInvite.",
                normalizedEmail);

            return Result<InviteUserResult>.Failure(
                Error.Failure("Invite.EmailDeliveryFailed",
                    "Invite created but we couldn't send the email. Please use Resend Invite."),
                Outcome.ServerError);
        }

        return Result<InviteUserResult>.Created(new InviteUserResult(userId, profileResult.Value));
    }
}
