using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ResendInvite;

/// <summary>
/// Admin-initiated resend of the invite link. Refuses to resend for accounts
/// that have already completed onboarding (active + email verified). Marks
/// any prior outstanding invite tokens as used and issues a new one.
/// </summary>
public sealed class ResendInviteCommandHandler(
    IUserRegistrationService userRegistrationService,
    IOtpRepository otpRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    IInviteLinkBuilder inviteLinkBuilder,
    IEmailService emailService)
    : ICommandHandler<ResendInviteCommand, ResendInviteResult>
{
    private const string GenericMessage =
        "If an invited account exists for this email, a new invite has been sent.";

    public async Task<Result<ResendInviteResult>> Handle(
        ResendInviteCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var status = await userRegistrationService.GetInviteAccountStatusAsync(normalizedEmail, ct);
        if (status is null)
        {
            // Do not leak whether the account exists.
            return Result<ResendInviteResult>.Success(new ResendInviteResult(GenericMessage));
        }

        if (status.IsActive || status.IsEmailVerified)
        {
            return Result<ResendInviteResult>.Failure(
                Error.Conflict("Invite.AlreadyCompleted",
                    "This account has already completed onboarding. A new invite cannot be sent."),
                Outcome.Conflict);
        }

        // Invalidate prior outstanding invite tokens for this user.
        var oldInvites = await otpRepository.GetAllAsync(
            filter: o => o.UserId == status.UserId
                      && o.Purpose == InviteConstants.Purpose
                      && !o.IsUsed,
            asNoTracking: false,
            ct: ct);

        foreach (var old in oldInvites)
            old.MarkUsed();

        // Issue a fresh invite token.
        var plainToken = inviteTokenService.Generate();
        var tokenHash  = inviteTokenService.Hash(plainToken);

        var invite = Otp.Create(
            userId:          status.UserId,
            purpose:         InviteConstants.Purpose,
            codeHash:        tokenHash,
            deliveryChannel: "Email",
            deliveryAddress: normalizedEmail,
            expiryMinutes:   InviteConstants.ExpiryMinutes);

        await otpRepository.AddAsync(invite, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var link = inviteLinkBuilder.Build(normalizedEmail, plainToken);

        await emailService.SendAsync(
            normalizedEmail,
            "YallaJo — Your invite link",
            $"Your YallaJo invite has been refreshed.\n" +
            $"Click the link below to set your password and activate your account:\n\n{link}\n\n" +
            $"This link expires in {InviteConstants.ExpiryMinutes / 60 / 24} days.",
            ct);

        return Result<ResendInviteResult>.Success(new ResendInviteResult(GenericMessage));
    }
}
