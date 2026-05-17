using YallaJo.SharedKernel.Application.Authorization;

namespace Messaging.Contracts.Authorization;

public sealed class MessagingPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Messaging";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // Notification (own)
        new(MessagingFeatures.Notification, AppAction.ReadOwn,  PermissionGroup.SupportOperations, "View own notifications"),
        new(MessagingFeatures.Notification, AppAction.UpdateSelf, PermissionGroup.SupportOperations, "Mark own notifications as read"),
        new(MessagingFeatures.Notification, AppAction.DeleteAny, PermissionGroup.ModerationTools, "Delete any notification (admin)"),

        // NotificationPreference (own)
        new(MessagingFeatures.NotificationPreference, AppAction.ReadOwn,    PermissionGroup.SupportOperations, "View own notification preferences"),
        new(MessagingFeatures.NotificationPreference, AppAction.UpdateSelf, PermissionGroup.SupportOperations, "Update own notification preferences"),

        // NotificationTemplate (admin CRUD)
        new(MessagingFeatures.NotificationTemplate, AppAction.Read,   PermissionGroup.SupportOperations, "View notification templates"),
        new(MessagingFeatures.NotificationTemplate, AppAction.Create, PermissionGroup.SupportOperations, "Create notification template"),
        new(MessagingFeatures.NotificationTemplate, AppAction.Update, PermissionGroup.SupportOperations, "Update notification template"),
        new(MessagingFeatures.NotificationTemplate, AppAction.Delete, PermissionGroup.SupportOperations, "Delete notification template"),

        // DeviceToken (own)
        new(MessagingFeatures.DeviceToken, AppAction.Create,     PermissionGroup.SupportOperations, "Register device token"),
        new(MessagingFeatures.DeviceToken, AppAction.UpdateSelf, PermissionGroup.SupportOperations, "Update own device token"),
        new(MessagingFeatures.DeviceToken, AppAction.Delete,     PermissionGroup.SupportOperations, "Revoke own device token"),

        // SupportTicket
        new(MessagingFeatures.SupportTicket, AppAction.ReadOwn,  PermissionGroup.SupportOperations, "View own support tickets"),
        new(MessagingFeatures.SupportTicket, AppAction.ReadAny,  PermissionGroup.SupportOperations, "View any support ticket (staff)"),
        new(MessagingFeatures.SupportTicket, AppAction.Create,   PermissionGroup.SupportOperations, "Open support ticket"),
        new(MessagingFeatures.SupportTicket, AppAction.Assign,   PermissionGroup.SupportOperations, "Assign ticket to staff"),
        new(MessagingFeatures.SupportTicket, AppAction.Resolve,  PermissionGroup.SupportOperations, "Resolve ticket"),
        new(MessagingFeatures.SupportTicket, AppAction.Close,    PermissionGroup.SupportOperations, "Close ticket"),
    ];
}
