namespace Messaging.Application.Caching;

public static class MessagingCacheKeys
{
    public static string NotificationsTag(Guid userId) => $"notifications:user:{userId}";
    public static string Notification(Guid notificationId, Guid userId) => $"msg:notification:{notificationId}:user:{userId}";

    public static string Notifications(Guid userId, string? type, bool? isRead, DateTime? from, DateTime? to, Guid? cursor, int pageSize)
        => $"msg:notifications:user:{userId}:type:{type ?? "all"}:read:{isRead?.ToString() ?? "all"}:from:{FormatDate(from)}:to:{FormatDate(to)}:cursor:{cursor?.ToString() ?? "first"}:size:{pageSize}";

    public static string UnreadCount(Guid userId) => $"msg:notifications:unread:user:{userId}";

    public static string NotificationPreferences(Guid userId) => $"msg:notification-preferences:user:{userId}";
    public static string NotificationPreferencesTag(Guid userId) => $"notification-preferences:user:{userId}";

    public static string SupportTicketsTag(Guid userId) => $"support-tickets:user:{userId}";
    public static string SupportTicketsAdminTag => "support-tickets:admin";
    public static string SupportTicketTag(Guid ticketId) => $"support-ticket:{ticketId}";
    public static string TicketMessagesTag(Guid ticketId) => $"support-ticket-messages:{ticketId}";

    public static string SupportTickets(Guid? userId, bool isAdmin, string? status, string? category, Guid? cursor, int pageSize)
        => $"msg:support-tickets:{(isAdmin ? "admin" : $"user:{userId}")}:status:{status ?? "all"}:category:{category ?? "all"}:cursor:{cursor?.ToString() ?? "first"}:size:{pageSize}";

    public static string SupportTicket(Guid ticketId, Guid callerUserId, bool isAdmin)
        => $"msg:support-ticket:{ticketId}:caller:{callerUserId}:admin:{isAdmin}";

    public static string NotificationTemplates => "msg:notification-templates";
    public static string NotificationTemplatesTag => "notification-templates";

    public static string DeviceTokens(Guid userId) => $"msg:device-tokens:user:{userId}";
    public static string DeviceTokensTag(Guid userId) => $"device-tokens:user:{userId}";

    private static string FormatDate(DateTime? value) => value?.ToUniversalTime().ToString("O") ?? "none";
}
