using Accounts.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.Authorization;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Assigns the Provider or TourGuide role when a provider application is approved,
/// and issues the server-generated <c>provider_id</c> identity claim
/// (value = approved <c>ProviderApplication.Id</c>) so provider-scoped Finance
/// endpoints can resolve the caller's provider from the JWT.
/// IndependentGuide → TourGuide role. All other ProviderTypes → Provider role.
/// Idempotent: skips role/claim assignment when already present.
/// </summary>
public sealed class ProviderApprovedAssignRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserClaimRepository userClaimRepository,
    ISecurityUnitOfWork unitOfWork,
    ISecurityInboxStore inboxStore,
    ILogger<ProviderApprovedAssignRoleHandler> logger)
    : INotificationHandler<IntegrationEventNotification<ProviderApprovedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<ProviderApprovedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (ProviderApproved role-assign for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var roleName = ev.ProviderType == "IndependentGuide"
            ? AppRoles.TourGuide
            : AppRoles.Provider;

        var roles = await roleRepository.GetRolesByNamesAsync([roleName], cancellationToken);
        var role = roles.FirstOrDefault();
        if (role is null)
        {
            logger.LogError(
                "Security: Role {Role} not found in database — cannot assign.",
                roleName);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var existing = await userRepository.GetUserRoleAsync(ev.UserId, role.Id, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "Security: User {UserId} already has role {Role} — skipping role assignment.",
                ev.UserId, roleName);

            // Still ensure the provider_id claim exists — the role may have been
            // granted before claim-issuing logic existed.
            await EnsureProviderIdClaimAsync(ev.UserId, ev.ApplicationId, cancellationToken);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = await userRepository.GetByIdAsync(ev.UserId, cancellationToken, asNoTracking: false);
        if (user is null)
        {
            logger.LogError(
                "Security: Cannot assign {Role} — user {UserId} not found.",
                roleName, ev.UserId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        user.AssignRole(role);
        await EnsureProviderIdClaimAsync(ev.UserId, ev.ApplicationId, cancellationToken);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: Assigned role {Role} to user {UserId} (provider type: {ProviderType}).",
            roleName, ev.UserId, ev.ProviderType);
    }

    /// <summary>
    /// Idempotently persists the <c>provider_id</c> UserClaim for the approved
    /// provider. The value is the server-side <c>ProviderApplication.Id</c> carried
    /// on the approval event — never sourced from a client. Does not call SaveChanges;
    /// the caller commits within its unit of work.
    /// </summary>
    private async Task EnsureProviderIdClaimAsync(
        Guid userId,
        Guid providerId,
        CancellationToken cancellationToken)
    {
        var claimValue = providerId.ToString();

        var alreadyIssued = await userClaimRepository.AnyAsync(
            c => c.UserId == userId
                 && c.ClaimType == ProviderClaimTypes.ProviderId
                 && c.ClaimValue == claimValue,
            cancellationToken);

        if (alreadyIssued)
        {
            logger.LogInformation(
                "Security: provider_id claim already present for user {UserId} (provider {ProviderId}) — skipping.",
                userId, providerId);
            return;
        }

        await userClaimRepository.AddAsync(
            UserClaim.Create(userId, ProviderClaimTypes.ProviderId, claimValue),
            cancellationToken);

        logger.LogInformation(
            "Security: Issued provider_id claim for user {UserId} (provider {ProviderId}).",
            userId, providerId);
    }
}
