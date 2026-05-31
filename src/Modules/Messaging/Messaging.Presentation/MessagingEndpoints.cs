using Messaging.Presentation.Endpoints.Device;
using Messaging.Presentation.Endpoints.Notification;
using Messaging.Presentation.Endpoints.NotificationTemplate;
using Messaging.Presentation.Endpoints.SupportTicket;
using Messaging.Presentation.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Messaging.Presentation;

public static class MessagingEndpoints
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // REST endpoints
        var notifications = endpoints.MapGroup("/api/v1/notifications");
        NotificationEndpoints.MapNotificationEndpoints(notifications);

        var devices = endpoints.MapGroup("/api/v1/devices");
        DeviceEndpoints.MapDeviceEndpoints(devices);

        var support = endpoints.MapGroup("/api/v1/support");
        SupportTicketEndpoints.MapSupportTicketEndpoints(support);

        var notificationTemplates = endpoints.MapGroup("/api/v1/admin/notification-templates");
        NotificationTemplateEndpoints.MapNotificationTemplateEndpoints(notificationTemplates);

        // SignalR hub (T2)
        endpoints.MapHub<NotificationHub>("/hubs/notifications");

        return endpoints;
    }
}
