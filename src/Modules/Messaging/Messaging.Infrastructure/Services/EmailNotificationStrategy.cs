using Messaging.Application.Interfaces;
using Messaging.Contracts.Services;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Services;

/// <summary>Enqueues email delivery attempt. Actual sending happens in T5 EmailNotificationSenderService.</summary>
internal sealed class EmailNotificationStrategy(
    INotificationDeliveryAttemptRepository deliveryAttemptRepository,
    TimeProvider timeProvider,
    ILogger<EmailNotificationStrategy> logger)
    : INotificationChannelStrategy
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<ChannelDispatchResult> DeliverAsync(Notification notification, CancellationToken ct = default)
    {
        // Enqueue — actual SMTP send happens async via EmailNotificationSenderService (T5)
        var attempt = NotificationDeliveryAttempt.Create(
            notification.Id, NotificationChannel.Email, timeProvider, attemptNumber: 1);
        await deliveryAttemptRepository.AddAsync(attempt, ct);
        logger.LogDebug("Email delivery attempt queued for notification {Id}", notification.Id);
        return new ChannelDispatchResult(Success: true, ExternalRef: attempt.Id.ToString(), FailureReason: null);
    }
}
