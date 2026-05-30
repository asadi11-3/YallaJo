namespace Messaging.Contracts.Services;

/// <summary>Dispatches a notification via all applicable channel strategies based on user preferences.</summary>
public interface INotificationDispatcher
{
    /// <summary>Dispatch notification to all applicable channels. M-R2: InApp always first.</summary>
    Task DispatchAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
}

/// <summary>Result of a single channel dispatch attempt.</summary>
public sealed record ChannelDispatchResult(bool Success, string? ExternalRef, string? FailureReason);
