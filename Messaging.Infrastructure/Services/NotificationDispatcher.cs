using Messaging.Application.Interfaces;
using Messaging.Contracts.Services;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Services;

/// <summary>Routes notification to InApp channel first, then fans out to Email/Push based on user preferences.</summary>
internal sealed class NotificationDispatcher(
    IEnumerable<INotificationChannelStrategy> strategies,
    INotificationRepository notificationRepository,
    INotificationPreferenceRepository preferenceRepository,
    INotificationDeliveryAttemptRepository deliveryAttemptRepository,
    ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    private readonly Dictionary<NotificationChannel, INotificationChannelStrategy> _strategies
        = strategies.ToDictionary(s => s.Channel);

    public async Task DispatchAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId, ct);
        if (notification is null)
        {
            logger.LogWarning("Notification {Id} not found; dispatch skipped", notificationId);
            return;
        }

        // M-R2: InApp always first
        await DeliverChannelAsync(notification, NotificationChannel.InApp, ct);

        // Fan out to Email and Push based on preferences
        foreach (var channel in new[] { NotificationChannel.Email, NotificationChannel.Push })
        {
            var enabled = await preferenceRepository.IsEnabledForUserAsync(
                userId, notification.Type, channel, ct);
            if (!enabled) continue;
            await DeliverChannelAsync(notification, channel, ct);
        }
    }

    private async Task DeliverChannelAsync(Notification notification, NotificationChannel channel, CancellationToken ct)
    {
        if (!_strategies.TryGetValue(channel, out var strategy))
        {
            logger.LogWarning("No strategy registered for channel {Channel}", channel);
            return;
        }

        var attempt = NotificationDeliveryAttempt.Create(notification.Id, channel, 1);
        try
        {
            var result = await strategy.DeliverAsync(notification, ct);
            if (result.Success) attempt.MarkSucceeded(result.ExternalRef);
            else attempt.MarkFailed(result.FailureReason ?? "Unknown failure");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Channel {Channel} delivery failed for notification {Id}", channel, notification.Id);
            attempt.MarkFailed(ex.Message);
        }
        await deliveryAttemptRepository.AddAsync(attempt, ct);
    }
}
