using Accounts.Contracts.Abstractions;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ProvisionAccount;

/// <summary>
/// Phase 2B — admin-provisioning handler. Creates the Security identity in
/// <c>Provisioned</c> lifecycle state and the Accounts profile shell that
/// tracks non-security user data. Does NOT issue activation tokens and does
/// NOT send an activation email — those are the responsibility of the
/// <c>SendActivationEmailCommand</c> handler, invoked separately by the
/// admin (or by the legacy <c>InviteUserCommand</c> façade).
/// <para>
/// Ordering discipline: Security first, Accounts second. If profile
/// creation fails, the Security user already exists. This matches the
/// pre-Phase-2B behaviour of <c>InviteUserCommandHandler</c> and is
/// acceptable in the target business model because a provisioned account
/// without a profile shell can be reconciled by an admin retry — nothing
/// downstream attempts to log in or send email against the half-created
/// record. Full two-phase atomicity across modules is deferred to a later
/// phase that introduces an outbox-coordinated provisioning saga.
/// </para>
/// </summary>
public sealed class ProvisionAccountCommandHandler(
    IUserRegistrationService userRegistrationService,
    IProfileCreationService profileCreationService)
    : ICommandHandler<ProvisionAccountCommand, ProvisionAccountResult>
{
    public async Task<Result<ProvisionAccountResult>> Handle(
        ProvisionAccountCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Create the Security identity in Provisioned state. No password,
        //    unverified email, no lifecycle advance to PendingActivation
        //    (that happens later, after an activation email is actually sent).
        var registration = await userRegistrationService.RegisterProvisionedAsync(
            new InvitedUserRegistrationRequest(
                request.FirstName,
                request.LastName,
                normalizedEmail,
                request.InitialRoleIds),
            cancellationToken);

        if (registration.IsFailure)
        {
            return Result<ProvisionAccountResult>.Fail(
                registration.Outcome,
                registration.Messages.FirstOrDefault() ?? string.Empty,
                registration.Errors.ToArray());
        }

        var userId = registration.Value;

        // 2. Create the Accounts profile shell linked to the new identity.
        var profile = await profileCreationService.CreateForInvitedUserAsync(
            new InvitedProfileCreationRequest(
                UserId:      userId,
                FirstName:   request.FirstName,
                LastName:    request.LastName,
                DisplayName: string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
                AvatarUrl:   string.IsNullOrWhiteSpace(request.AvatarUrl)   ? null : request.AvatarUrl.Trim()),
            cancellationToken);

        if (profile.IsFailure)
        {
            return Result<ProvisionAccountResult>.Fail(
                profile.Outcome,
                profile.Messages.FirstOrDefault() ?? string.Empty,
                profile.Errors.ToArray());
        }

        return Result<ProvisionAccountResult>.Created(
            new ProvisionAccountResult(userId, profile.Value));
    }
}
