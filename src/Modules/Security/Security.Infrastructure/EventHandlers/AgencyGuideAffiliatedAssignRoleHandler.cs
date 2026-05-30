using Accounts.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.Authorization;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Assigns the TourGuide role when a guide is affiliated with an agency.
/// Forward-declared: the AgencyGuideAffiliatedIntegrationEvent will be published
/// once the Platform-Onboarding plan is implemented.
/// Idempotent: skips if user already has the TourGuide role.
/// </summary>
public sealed class AgencyGuideAffiliatedAssignRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    ISecurityInboxStore inboxStore,
    ILogger<AgencyGuideAffiliatedAssignRoleHandler> logger)
    : INotificationHandler<IntegrationEventNotification<AgencyGuideAffiliatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<AgencyGuideAffiliatedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (AgencyGuideAffiliated role-assign for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var roles = await roleRepository.GetRolesByNamesAsync([AppRoles.TourGuide], cancellationToken);
        var role = roles.FirstOrDefault();
        if (role is null)
        {
            logger.LogError("Security: Role {Role} not found in database — cannot assign.", AppRoles.TourGuide);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var existing = await userRepository.GetUserRoleAsync(ev.UserId, role.Id, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation(
                "Security: User {UserId} already has role {Role} — skipping.",
                ev.UserId, AppRoles.TourGuide);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = await userRepository.GetByIdAsync(ev.UserId, cancellationToken, asNoTracking: false);
        if (user is null)
        {
            logger.LogError(
                "Security: Cannot assign {Role} — user {UserId} not found.",
                AppRoles.TourGuide, ev.UserId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        user.AssignRole(role);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: Assigned role {Role} to user {UserId} (agency: {AgencyId}).",
            AppRoles.TourGuide, ev.UserId, ev.AgencyId);
    }
}
