using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.SendActivationEmail;

/// <summary>
/// Phase 2C-1 — now backed by the dedicated <see cref="ActivationToken"/>
/// aggregate instead of the overloaded <c>Otp(Purpose="UserInvite")</c>
/// representation. The handler still returns the same outcomes and the
/// legacy <c>InviteUserCommand</c> / <c>ResendInviteCommand</c> façades
/// are unaffected.
/// <para>
/// Flow:
/// </para>
/// <list type="number">
///   <item><description>Look up the account snapshot. <c>NotFound</c> for unknown emails.</description></item>
///   <item><description>Lifecycle gate — <c>Provisioned</c> or <c>PendingActivation</c> only. Any later state returns <c>Conflict</c>.</description></item>
///   <item><description>Supersede every non-terminal <see cref="ActivationToken"/> for the user (state <c>Issued</c> or <c>Delivered</c>) by calling <see cref="ActivationToken.Supersede"/>. Enforces the "at most one active token per user" invariant in code.</description></item>
///   <item><description>Create the new token in <c>Issued</c> / <c>DeliveryStatus=Pending</c> and persist it in the same unit of work as the supersede sweep. This is a deliberate change from the Phase 2B Otp flow, which deferred persistence until after the email: the activation-token aggregate has a real state machine that can represent "persisted but email failed" as <c>Revoked(EmailFailed)</c>, so the DB stays honest without needing to hide rows from failed sends.</description></item>
///   <item><description>Send the activation email. On success: <see cref="ActivationToken.MarkDelivered"/> → <c>Delivered</c> + <c>Sent</c>. On SMTP failure: <see cref="ActivationToken.RevokeOnEmailFailure"/> → <c>Revoked</c> + <c>Failed</c>.</description></item>
///   <item><description>Flush the delivery-status update.</description></item>
///   <item><description>On email success, advance the user lifecycle via <see cref="IUserRegistrationService.MarkPendingActivationAsync"/>. On email failure, the lifecycle is NOT advanced — the account stays in its current state (Provisioned / PendingActivation) and the admin can retry.</description></item>
/// </list>
/// <para>
/// Failure windows:
/// </para>
/// <list type="bullet">
///   <item><description>Crash between steps 4 and 5: a <c>Issued</c>/<c>Pending</c> row is left in the DB. The next send supersedes it and issues a fresh token. No user-visible impact.</description></item>
///   <item><description>Crash between steps 6 and 7 (email sent, lifecycle transition not committed): the user is still <c>Provisioned</c>; the next send finds the active Delivered token on their record and treats it as a supersede target. The original activation link is therefore invalidated by the next send, which is strictly safer than letting two live links exist.</description></item>
/// </list>
/// </summary>
public sealed class SendActivationEmailCommandHandler(
    IUserRegistrationService userRegistrationService,
    IActivationTokenRepository activationTokenRepository,
    IAuthUnitOfWork unitOfWork,
    IInviteTokenService inviteTokenService,
    IInviteLinkBuilder inviteLinkBuilder,
    IEmailService emailService,
    ILogger<SendActivationEmailCommandHandler> logger)
    : ICommandHandler<SendActivationEmailCommand, SendActivationEmailResult>
{
    private const string SuccessMessage = "Activation email sent.";

    public async Task<Result<SendActivationEmailResult>> Handle(
        SendActivationEmailCommand request,
        CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Resolve account + lifecycle snapshot.
        var status = await userRegistrationService.GetInviteAccountStatusAsync(
            normalizedEmail, ct);

        if (status is null)
        {
            return Result<SendActivationEmailResult>.Failure(
                Error.NotFound("Invite.NotFound", "No account found for this email."),
                Outcome.NotFound);
        }

        // 2. Lifecycle gate.
        if (status.Lifecycle != AccountLifecycleSnapshot.Provisioned
         && status.Lifecycle != AccountLifecycleSnapshot.PendingActivation)
        {
            return Result<SendActivationEmailResult>.Failure(
                Error.Conflict(
                    "Invite.AlreadyCompleted",
                    "This account has already completed onboarding. A new activation email cannot be sent."),
                Outcome.Conflict);
        }

        // 3. Supersede every non-terminal activation token for this user.
        //    Tracked load — transitions raise no domain events today but
        //    persist RevokedAt/RevokedReason for the audit trail.
        var existing = await activationTokenRepository.GetActiveForUserAsync(
            status.UserId, ct);

        foreach (var prior in existing)
            prior.Supersede();

        // 4. Create the new token, hash the plaintext once, persist.
        var plainToken = inviteTokenService.Generate();
        var tokenHash  = inviteTokenService.Hash(plainToken);
        var link       = inviteLinkBuilder.Build(normalizedEmail, plainToken);

        var token = ActivationToken.Issue(
            userId:          status.UserId,
            tokenHash:       tokenHash,
            deliveryAddress: normalizedEmail,
            expiryMinutes:   InviteConstants.ExpiryMinutes);

        await activationTokenRepository.AddAsync(token, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // 5. Send the activation email. On success → MarkDelivered. On
        //    failure → RevokeOnEmailFailure. Either way we commit the
        //    delivery-status update before returning so the audit trail
        //    reflects what happened.
        bool emailSent;
        try
        {
            await emailService.SendAsync(
                normalizedEmail,
                "YallaJo — Activate your account",
                $"Click the link below to set your password and activate your account:\n\n{link}\n\n" +
                $"This link expires in {InviteConstants.ExpiryMinutes / 60 / 24} days.",
                ct);

            token.MarkDelivered();
            emailSent = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogError(ex,
                "Auth: Activation email delivery failed for {Email} / token {TokenId}. Token revoked with reason EmailFailed.",
                normalizedEmail, token.Id);

            token.RevokeOnEmailFailure();
            emailSent = false;
        }

        // 6. Flush the delivery-status (and any supersede mutations).
        await unitOfWork.SaveChangesAsync(ct);

        if (!emailSent)
        {
            return Result<SendActivationEmailResult>.Failure(
                Error.Failure(
                    "Invite.EmailDeliveryFailed",
                    "Activation email could not be sent. Please try again."),
                Outcome.ServerError);
        }

        // 7. Advance the user lifecycle. Idempotent for PendingActivation.
        var transition = await userRegistrationService.MarkPendingActivationAsync(
            status.UserId, ct);

        if (transition.IsFailure)
        {
            logger.LogError(
                "Auth: Activation email was sent and token {TokenId} delivered for user {UserId}, but the lifecycle transition to PendingActivation failed: {Outcome} / {Error}.",
                token.Id, status.UserId, transition.Outcome,
                transition.Errors.FirstOrDefault()?.Message);

            return Result<SendActivationEmailResult>.Fail(
                transition.Outcome,
                transition.Messages.FirstOrDefault() ?? "Failed to update account lifecycle state.",
                transition.Errors.ToArray());
        }

        return Result<SendActivationEmailResult>.Success(
            new SendActivationEmailResult(SuccessMessage));
    }
}
