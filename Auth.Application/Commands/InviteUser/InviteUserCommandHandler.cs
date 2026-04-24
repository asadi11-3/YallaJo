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

        // 3. Generate token + link in memory. We do NOT persist the OTP row
        //    yet — see step 4 for the ordering rationale.
        var plainToken = inviteTokenService.Generate();
        var tokenHash  = inviteTokenService.Hash(plainToken);
        var link       = inviteLinkBuilder.Build(normalizedEmail, plainToken);

        // 4. Send the email FIRST. If delivery fails we return a clear
        //    "email delivery failed" error without ever persisting a token
        //    row. The previous ordering persisted the token, then (on
        //    failure) MarkedUsed() the row — stamping UsedAt on a token that
        //    was never consumed. That conflated three different terminal
        //    states (consumed / revoked / failed-delivery) under one flag
        //    and wrote a lie into the audit trail.
        //
        //    The identity + profile are intentionally left in place on email
        //    failure: provisioning-without-activation IS a valid state in
        //    the target business model (an admin may provision an account
        //    weeks before the real person is ready), and the admin can
        //    subsequently use Resend Invite to issue + send a fresh link.
        //    Phase 2 will promote this into an explicit SendActivationEmail
        //    use case with delivery-status tracking on a dedicated
        //    ActivationToken aggregate.
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
            logger.LogError(ex,
                "Auth: Invite email delivery failed for {Email}. Identity + profile were provisioned; no invite token was persisted. Admin can issue a fresh invite via Resend Invite.",
                normalizedEmail);

            return Result<InviteUserResult>.Failure(
                Error.Failure(
                    "Invite.EmailDeliveryFailed",
                    "Account was provisioned but we couldn't send the invite email. Please use Resend Invite."),
                Outcome.ServerError);
        }

        // 5. Email delivered — now it is safe to persist the invite token.
        var invite = Otp.Create(
            userId:          userId,
            purpose:         InviteConstants.Purpose,
            codeHash:        tokenHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail,
            expiryMinutes:   InviteConstants.ExpiryMinutes);

        await otpRepository.AddAsync(invite, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<InviteUserResult>.Created(new InviteUserResult(userId, profileResult.Value));
    }
}
