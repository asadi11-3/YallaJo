using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Messaging.Presentation.Hubs;

/// <summary>
/// SignalR hub for real-time notification delivery (M-R6).
/// Groups: user:{userId}, provider:{providerId}, admin.
/// JWT via query string ?access_token=... for WebSocket connections.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier
            ?? Context.User?.FindFirst("sub")?.Value
            ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // Add to provider group if provider_id claim exists
        var providerId = Context.User?.FindFirst("provider_id")?.Value;
        if (!string.IsNullOrWhiteSpace(providerId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"provider:{providerId}");

        // Add to admin group if user has admin permission claim
        var hasAdminPermission = Context.User?.HasClaim("permission", "Permission.AdminSupportQueue.Read") ?? false;
        if (hasAdminPermission)
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Groups are automatically cleaned up on disconnect
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Client can call this to mark a notification as read in real-time.</summary>
    public Task MarkAsRead(Guid notificationId)
    {
        // Delegate to clients via return message; actual state change handled by REST endpoint
        return Task.CompletedTask;
    }
}
