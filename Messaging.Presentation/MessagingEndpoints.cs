using Messaging.Presentation.Endpoints;
using Messaging.Presentation.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Messaging.Presentation;

public static class MessagingEndpoints
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // REST endpoints
        endpoints.MapGroup("/api/v1/notifications").MapNotificationEndpoints();
        endpoints.MapGroup("/api/v1/devices").MapDeviceEndpoints();
        endpoints.MapGroup("/api/v1/support").MapSupportTicketEndpoints();
        endpoints.MapGroup("/api/v1/admin/notification-templates").MapNotificationTemplateEndpoints();

        // SignalR hub (T2)
        endpoints.MapHub<NotificationHub>("/hubs/notifications");

        return endpoints;
    }
}
