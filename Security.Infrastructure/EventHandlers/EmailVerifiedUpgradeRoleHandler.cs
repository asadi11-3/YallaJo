using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Interfaces;
using Security.Contracts.Authorization;
using Security.Contracts.IntegrationEvents;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Security.Infrastructure.EventHandlers;

/// <summary>
/// Upgrades a user from Guest to User role when their email is verified.
/// Removes the Guest role and assigns the User role.
/// Idempotent: skips if user already has the User role.
/// </summary>
public sealed class EmailVerifiedUpgradeRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISecurityUnitOfWork unitOfWork,
    ISecurityInboxStore inboxStore,
    ILogger<EmailVerifiedUpgradeRoleHandler> logger)
    : INotificationHandler<IntegrationEventNotification<EmailVerifiedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<EmailVerifiedIntegrationEvent> notification,
        CancellationToken cancellationToken)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, cancellationToken))
        {
            logger.LogWarning(
                "Security: Message {MessageId} (EmailVerified role-upgrade for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var ev = notification.Event;

        var roles = await roleRepository.GetRolesByNamesAsync(
            [AppRoles.User, AppRoles.Guest], cancellationToken);

        var userRole = roles.FirstOrDefault(r => r.Name == AppRoles.User);
        var guestRole = roles.FirstOrDefault(r => r.Name == AppRoles.Guest);

        if (userRole is null)
        {
            logger.LogError("Security: Role {Role} not found in database — cannot upgrade.", AppRoles.User);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        // Already has User role — idempotent skip
        var existingUserRole = await userRepository.GetUserRoleAsync(ev.UserId, userRole.Id, cancellationToken);
        if (existingUserRole is not null)
        {
            logger.LogInformation(
                "Security: User {UserId} already has role {Role} — skipping upgrade.",
                ev.UserId, AppRoles.User);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var user = await userRepository.GetByIdAsync(ev.UserId, cancellationToken, asNoTracking: false);
        if (user is null)
        {
            logger.LogError(
                "Security: Cannot upgrade role — user {UserId} not found.",
                ev.UserId);
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        // Remove Guest role if present
        if (guestRole is not null)
        {
            var existingGuestRole = await userRepository.GetUserRoleAsync(ev.UserId, guestRole.Id, cancellationToken);
            if (existingGuestRole is not null)
            {
                userRepository.RemoveUserRole(existingGuestRole);
            }
        }

        // Assign User role
        user.AssignRole(userRole);
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Security: Upgraded user {UserId} from Guest to User role (email verified: {Email}).",
            ev.UserId, ev.EmailAddress);
    }
}
