using YallaJo.SharedKernel.Application.Authorization;

namespace Messaging.Contracts.Authorization;

/// <summary>
/// 18-permission catalog for the Messaging module.
/// Expected boot log: "PermissionSeeder inserted/verified 18 Messaging permissions".
/// </summary>
public sealed class MessagingPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Messaging";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Notification (3) ──────────────────────────────────────────────────
        new(MessagingFeatures.Notification, AppAction.Read,   PermissionGroup.SupportOperations, "View own notifications"),
        new(MessagingFeatures.Notification, AppAction.Update, PermissionGroup.SupportOperations, "Mark own notifications as read or delete them"),
        new(MessagingFeatures.Notification, AppAction.Delete, PermissionGroup.SupportOperations, "Delete own notification"),

        // ── NotificationPreference (2) ────────────────────────────────────────
        new(MessagingFeatures.NotificationPreference, AppAction.Read,   PermissionGroup.SupportOperations, "View own notification preferences"),
        new(MessagingFeatures.NotificationPreference, AppAction.Update, PermissionGroup.SupportOperations, "Update own notification channel preferences"),

        // ── NotificationTemplate (4) ──────────────────────────────────────────
        new(MessagingFeatures.NotificationTemplate, AppAction.Read,   PermissionGroup.SupportOperations, "View notification templates (admin)"),
        new(MessagingFeatures.NotificationTemplate, AppAction.Create, PermissionGroup.SupportOperations, "Create notification template (admin)"),
        new(MessagingFeatures.NotificationTemplate, AppAction.Update, PermissionGroup.SupportOperations, "Update notification template (admin)"),
        new(MessagingFeatures.NotificationTemplate, AppAction.Delete, PermissionGroup.SupportOperations, "Delete notification template (admin)"),

        // ── DeviceToken (3) ───────────────────────────────────────────────────
        new(MessagingFeatures.DeviceToken, AppAction.Create, PermissionGroup.SupportOperations, "Register push-notification device token"),
        new(MessagingFeatures.DeviceToken, AppAction.Read,   PermissionGroup.SupportOperations, "List own device tokens"),
        new(MessagingFeatures.DeviceToken, AppAction.Delete, PermissionGroup.SupportOperations, "Revoke own device token"),

        // ── SupportTicket (3) ─────────────────────────────────────────────────
        new(MessagingFeatures.SupportTicket, AppAction.Create, PermissionGroup.SupportOperations, "Open a support ticket"),
        new(MessagingFeatures.SupportTicket, AppAction.Read,   PermissionGroup.SupportOperations, "View own support tickets"),
        new(MessagingFeatures.SupportTicket, AppAction.Close,  PermissionGroup.SupportOperations, "Close own support ticket"),

        // ── AdminSupportQueue (3) ─────────────────────────────────────────────
        new(MessagingFeatures.AdminSupportQueue, AppAction.Read,    PermissionGroup.SupportOperations, "View all support tickets (staff)"),
        new(MessagingFeatures.AdminSupportQueue, AppAction.Assign,  PermissionGroup.SupportOperations, "Assign support ticket to staff member"),
        new(MessagingFeatures.AdminSupportQueue, AppAction.Resolve, PermissionGroup.SupportOperations, "Resolve support ticket (staff)"),
    ];
}
