using Auth.Application.Interfaces;
using Auth.Application.Invitations;
using Auth.Domain.Entities;
using Auth.Domain.Events;
using Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.SendActivationEmail;

/// <summary>
/// Phase 2C-3 — the email dispatch leg of activation is now event-driven.
/// The handler:
/// </summary>
/// <list type="number">
///   <item><description>Resolves the account snapshot and applies the Phase 2B lifecycle gate (<c>Provisioned</c> or <c>PendingActivation</c> only).</description></item>
///   <item><description>Supersedes every non-terminal <see cref="ActivationToken"/> for the user (the per-user at-most-one-active invariant).</description></item>
///   <item><description>Generates the plain token + hash + activation link in-memory.</description></item>
///   <item><description>Creates a new <see cref="ActivationToken"/> in <see cref="ActivationTokenState.Issued"/> / <see cref="ActivationTokenDeliveryStatus.Pending"/>, attaches an <see cref="ActivationTokenIssuedEvent"/> carrying the plain token + link, and persists the token.</description></item>
///   <item><description>Advances the user lifecycle via <see cref="IUserRegistrationService.MarkPendingActivationAsync"/>. Phase 2C-3 intentionally moves this transition BEFORE the email is dispatched — <c>PendingActivation</c> now means "activation initiated and a valid token exists"; email outcome is recorded separately on <c>DeliveryStatus</c>.</description></item>
///   <item><description>Calls <c>SaveChangesAsync</c> once. The unit-of-work domain-event dispatcher turns the <see cref="ActivationTokenIssuedEvent"/> into an outbox message in the same DbContext, so token row + supersede sweep + outbox row commit atomically. The <c>CompositeOutboxProcessor</c> delivers the email asynchronously via <c>ActivationEmailDispatchHandler</c>.</description></item>
/// </list>
/// <para>
/// SMTP is no longer invoked inside this handler; <c>IEmailService</c> is
/// not a dependency here any more. Command latency decouples from SMTP
/// latency and delivery retries happen in the outbox processor rather
/// than blocking the admin UX.
/// </para>
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
        var existing = await activationTokenRepository.GetActiveForUserAsync(
            status.UserId, ct);

        foreach (var prior in existing)
            prior.Supersede();

        // 4. Generate token + hash + link in-memory. The link is built
        //    HERE so the email dispatch handler doesn't need URL-building
        //    concerns — it just sends whatever is in the event payload.
        var plainToken = inviteTokenService.Generate();
        var tokenHash  = inviteTokenService.Hash(plainToken);
        var link       = inviteLinkBuilder.Build(normalizedEmail, plainToken);

        // 5. Create the token in Issued/Pending and raise the domain
        //    event carrying the plain token + link. The
        //    ActivationTokenIssuedDomainEventHandler in Auth.Infrastructure
        //    converts the domain event into an OutboxMessage in the same
        //    DbContext; the single SaveChanges below commits both.
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

        // 6. Advance the user lifecycle BEFORE the SaveChanges so the
        //    transition commits in the SAME unit of work as the token +
        //    outbox row. MarkPendingActivationAsync saves on the Security
        //    UoW separately — the two UoWs straddle module boundaries
        //    the same way they did in 2C-1; ambient TransactionScope
        //    is not used here because the failure mode ("activation
        //    initiated but user still Provisioned") is benign: the next
        //    send will supersede the orphan token and retry.
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

        // 7. Single SaveChanges commits the token row, the supersede
        //    mutations, and (via the UnitOfWork's domain-event dispatch)
        //    the outbox row atomically.
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Auth: Activation token {TokenId} issued for user {UserId}; email queued on outbox.",
            token.Id, status.UserId);

        return Result<SendActivationEmailResult>.Success(
            new SendActivationEmailResult(SuccessMessage));
    }
}
