using Accounts.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.Authorization;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Assigns the Provider or TourGuide role when a provider application is approved.
/// IndependentGuide → TourGuide role. All other ProviderTypes → Provider role.
/// Idempotent: skips if user already has the target role.
/// </summary>
public sealed class ProviderApprovedAssignRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
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
                "Security: User {UserId} already has role {Role} — skipping.",
                ev.UserId, roleName);
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
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: Assigned role {Role} to user {UserId} (provider type: {ProviderType}).",
            roleName, ev.UserId, ev.ProviderType);
    }
}
