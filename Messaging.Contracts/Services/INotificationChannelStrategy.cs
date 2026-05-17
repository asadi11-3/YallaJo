namespace Messaging.Contracts.Services;

/// <summary>
/// Strategy for delivering a notification over a specific channel (Push, Email, InApp, Sms).
/// Resolved via keyed-service registration using the channel name as the key.
/// </summary>
public interface INotificationChannelStrategy
{
    Task<NotificationChannelResult> SendAsync(NotificationDeliveryRequest request, CancellationToken ct = default);
}

public sealed record NotificationDeliveryRequest(
    Guid NotificationId,
    Guid UserId,
    string Title,
    string Body,
    string? Data);

public sealed record NotificationChannelResult(
    bool Success,
    string? ProviderMessageId,
    string? Error);
