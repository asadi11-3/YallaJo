namespace Messaging.Contracts.Authorization;

/// <summary>Feature keys for the Messaging module permission catalog.</summary>
public static class MessagingFeatures
{
    /// <summary>In-app notification reading and management.</summary>
    public const string Notification = nameof(Notification);

    /// <summary>User-controlled notification channel preferences.</summary>
    public const string NotificationPreference = nameof(NotificationPreference);

    /// <summary>Admin-managed Mustache templates for notification rendering.</summary>
    public const string NotificationTemplate = nameof(NotificationTemplate);

    /// <summary>Push-notification device token registration and management.</summary>
    public const string DeviceToken = nameof(DeviceToken);

    /// <summary>User-facing support ticket lifecycle.</summary>
    public const string SupportTicket = nameof(SupportTicket);

    /// <summary>Staff-facing support ticket queue and assignment.</summary>
    public const string AdminSupportQueue = nameof(AdminSupportQueue);
}
