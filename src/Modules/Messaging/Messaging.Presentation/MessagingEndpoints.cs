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

        // SignalR hub (T2) — authenticated notifications (user/provider/admin groups)
        endpoints.MapHub<NotificationHub>("/hubs/notifications");

        // SignalR hub — PUBLIC anonymous live tour-slot capacity (UI-PERF S2 / CAL3 / RT1).
        // tour:{tourId} group only; WebSockets + LongPolling (skip SSE per S1).
        endpoints.MapHub<TourSlotsHub>(
            "/hubs/tour",
            options => options.Transports =
                Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                | Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling);

        return endpoints;
    }
}
