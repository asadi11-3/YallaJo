using Messaging.Contracts.Services;
using Messaging.Domain.Entities;
using Messaging.Domain.Enums;

namespace Messaging.Application.Interfaces;

/// <summary>Strategy for delivering a notification over a specific channel (InApp, Email, Push).</summary>
public interface INotificationChannelStrategy
{
    NotificationChannel Channel { get; }
    Task<ChannelDispatchResult> DeliverAsync(Notification notification, CancellationToken ct = default);
}
