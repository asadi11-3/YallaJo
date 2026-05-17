# TASK 2 — NotificationHub (SignalR)

> **Owner:** Mohammad (Lead) — **Hours:** 30h — **Hard deadline:** Sun **2027-01-03 17:00**
> **Earliest start:** Wed 2026-12-02 09:00 (no T1 dependency for hub scaffolding)
> **Endpoints:** 0 HTTP — **1 SignalR Hub at `/hubs/notifications`**
> **Depends on:** PW-2, PW-6 (INotificationDispatcher), T1 (provides notification domain events that hub broadcasts)

---

## Hub Class

`Messaging.Infrastructure/Hubs/NotificationHub.cs`:

```csharp
[Authorize]  // JWT Bearer required — connection rejected if missing/invalid
public sealed class NotificationHub(
    ICurrentUser currentUser,
    IProviderResolverService providerResolver,
    ILogger<NotificationHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            logger.LogWarning("NotificationHub connection rejected — no user ID. ConnectionId={Cid}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        if (currentUser.IsInRole("Admin"))
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");

        if (currentUser.IsInRole("Provider"))
        {
            var providerIds = await providerResolver.GetProviderIdsForUserAsync(userId.Value, Context.ConnectionAborted);
            foreach (var pid in providerIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"provider:{pid}");
        }

        logger.LogInformation("SignalR connected: User={UserId} Cid={Cid}", userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("SignalR disconnected: User={UserId} Cid={Cid} Ex={Ex}", currentUser.UserId, Context.ConnectionId, exception?.Message);
        // Groups auto-cleaned by SignalR on disconnect; no explicit removal needed.
        await base.OnDisconnectedAsync(exception);
    }

    // Client → Server
    public async Task MarkAsRead(Guid notificationId)
    {
        if (currentUser.UserId is null) throw new HubException("Unauthorized");

        // Delegate to MediatR command — hub stays thin
        var mediator = Context.GetHttpContext()!.RequestServices.GetRequiredService<IMediator>();
        var result = await mediator.Send(new MarkNotificationReadCommand(notificationId), Context.ConnectionAborted);

        if (result.IsFailure)
            throw new HubException($"{result.Error?.Code}: {result.Error?.Description}");

        // Broadcast confirmation back to same user (in case they have multiple tabs)
        await Clients.Group($"user:{currentUser.UserId}").SendAsync("NotificationRead", notificationId);
    }

    public async Task MarkAllAsRead()
    {
        if (currentUser.UserId is null) throw new HubException("Unauthorized");

        var mediator = Context.GetHttpContext()!.RequestServices.GetRequiredService<IMediator>();
        await mediator.Send(new MarkAllNotificationsReadCommand(), Context.ConnectionAborted);

        await Clients.Group($"user:{currentUser.UserId}").SendAsync("AllNotificationsRead");
    }
}
```

---

## Server → Client Methods (TypeScript client contract)

| Method | Payload | Trigger |
|---|---|---|
| `ReceiveNotification` | `{id, type, title, body, createdAt, priority, data?, entityType?, entityId?}` | New notification created (in-process domain event handler) |
| `NotificationRead` | `{notificationId}` | Confirmation broadcast after MarkAsRead |
| `AllNotificationsRead` | `{}` | Confirmation broadcast after MarkAllAsRead |
| `UnreadCountUpdated` | `{unreadCount}` | Broadcast after any read/delete/new |
| `NewSupportTicket` | `{ticketId, category, priority, createdByName}` | Admin group only — fires on TicketCreated |
| `SupportTicketAssigned` | `{ticketId, assignedToUserId}` | Admin group + assignee user group |
| `TourLiveTrackingUpdate` | (Phase 4 — placeholder) | OUT OF SCOPE |

---

## Client → Server Methods

| Method | Args | Behavior |
|---|---|---|
| `MarkAsRead(notificationId: Guid)` | one notification ID | Updates DB + broadcasts NotificationRead to same user's other tabs |
| `MarkAllAsRead()` | – | Bulk update + broadcast AllNotificationsRead |

---

## Hub-Side Dispatch (In-Process Domain Event Handler)

`Messaging.Infrastructure/EventHandlers/NotificationCreatedSignalRBroadcastHandler.cs`:

```csharp
public sealed class NotificationCreatedSignalRBroadcastHandler(
    IHubContext<NotificationHub> hub,
    INotificationRepository repo,
    ILogger<NotificationCreatedSignalRBroadcastHandler> logger)
    : INotificationHandler<NotificationCreatedDomainEvent>
{
    public async Task Handle(NotificationCreatedDomainEvent evt, CancellationToken ct)
    {
        // Look up full notification for payload
        var n = await repo.GetByIdAsync(evt.NotificationId, ct);
        if (n is null)
        {
            logger.LogWarning("Notification {Id} not found for SignalR broadcast (race)", evt.NotificationId);
            return;
        }

        var payload = new
        {
            id = n.Id, type = n.Type.ToString(), title = n.Title, body = n.Body,
            createdAt = n.CreatedAt, priority = n.Priority.ToString(),
            data = n.Data, entityType = n.EntityType, entityId = n.EntityId
        };

        await hub.Clients.Group($"user:{evt.UserId}").SendAsync("ReceiveNotification", payload, ct);

        // Refresh unread count
        var unreadCount = await repo.GetUnreadCountByUserAsync(evt.UserId, ct);
        await hub.Clients.Group($"user:{evt.UserId}").SendAsync("UnreadCountUpdated", new { unreadCount }, ct);
    }
}
```

**Important**: this is a NORMAL `INotificationHandler<TDomainEvent>` — but it does NOT call `SaveChanges` (M-R3). It only sends transient SignalR messages — no DB writes.

---

## Auth & Security

**JWT Bearer required** at connection — same `JwtBearerOptions` as REST endpoints. Configure in `Program.cs`:

```csharp
app.MapHub<NotificationHub>("/hubs/notifications");
```

SignalR auto-picks up `JwtBearerHandler`. For token in query string (browsers can't set headers on WS connect), add `JwtBearerEvents.OnMessageReceived`:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = ctx =>
    {
        var accessToken = ctx.Request.Query["access_token"];
        var path = ctx.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            ctx.Token = accessToken;
        return Task.CompletedTask;
    }
};
```

**Group membership** is server-stamped at connection. Clients CANNOT subscribe to arbitrary groups — eliminates impersonation risk.

---

## Connection Limits & Scaling

**v1 single-instance:** in-memory SignalR backplane. Limit ~10K concurrent connections per instance (per Microsoft docs). YallaJo current scale comfortably fits.

**v2 multi-instance:** add **Azure SignalR Service** or **Redis backplane**. Add NuGet `Microsoft.Azure.SignalR` and:

```csharp
services.AddSignalR().AddAzureSignalR(cfg.GetConnectionString("AzureSignalR"));
```

When this happens, document in `Agents/decisions/ADR-XXX-signalr-scaleout.md`. Acceptance for THIS sprint = single-instance assumption holds; no Redis/Azure needed.

---

## Reconnect Strategy (Client-Side Contract for Frontend Team)

Frontend TS client should:
1. Build HubConnection with `withAutomaticReconnect([0, 2000, 10000, 30000])` (4 attempts).
2. On reconnect, **re-fetch unread count via REST** `GET /notifications/unread-count` to ensure consistency (SignalR push during disconnect lost).
3. Show "Reconnecting…" UI banner during retries; "Connection lost" if all 4 fail (user can manual-refresh).

---

## DI Registration

In `Messaging.Infrastructure/DependencyInjection.cs` (BEFORE `services.AddHostedService<...>` calls):

```csharp
services.AddSignalR(opts =>
{
    var cfg = configuration.GetSection("Messaging:SignalR");
    opts.MaximumParallelInvocationsPerClient = cfg.GetValue<int>("MaxParallelInvocationsPerClient", 1);
    opts.ClientTimeoutInterval = TimeSpan.FromSeconds(cfg.GetValue<int>("ClientTimeoutSeconds", 30));
    opts.KeepAliveInterval = TimeSpan.FromSeconds(cfg.GetValue<int>("KeepAliveIntervalSeconds", 15));
    opts.EnableDetailedErrors = cfg.GetValue<bool>("EnableDetailedErrors", false);
});
```

In `YallaJo.Api/Program.cs` AFTER `app.UseAuthorization()` BEFORE module endpoint registration:

```csharp
app.MapHub<NotificationHub>("/hubs/notifications");
```

---

## WBS (30h)

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Hub class skeleton + OnConnectedAsync/OnDisconnectedAsync | 4 | 2026-12-04 |
| 2 | Group routing (user/admin/provider) + IProviderResolverService stub | 4 | 2026-12-08 |
| 3 | JWT query-token support in JwtBearerEvents.OnMessageReceived | 2 | 2026-12-10 |
| 4 | Client → Server methods (MarkAsRead/MarkAllAsRead) with MediatR delegation | 3 | 2026-12-13 |
| 5 | NotificationCreatedSignalRBroadcastHandler + unit test | 4 | 2026-12-17 |
| 6 | DI registration + Program.cs wiring | 1 | 2026-12-19 |
| 7 | Integration test using SignalR `HubConnection` client + TestServer | 8 | 2026-12-26 |
| 8 | Manual smoke test with browser dev tools (connect, receive, mark, disconnect) | 2 | 2026-12-30 |
| 9 | PR review fixes | 2 | 2027-01-03 |
| **Total** | | **30h** | **Sun 2027-01-03** |

---

## Acceptance Tests (Integration — `tests/Messaging.IntegrationTests/Hubs/`)

1. Connect with valid JWT → OnConnectedAsync runs, user added to `user:{userId}` group, no exception.
2. Connect without JWT → `Context.Abort()`, connection rejected.
3. Connect with expired JWT → `HubException("Unauthorized")` or transport-level 401.
4. User in Admin role → added to `admin` group at connection time.
5. User in Provider role for Provider X → added to `provider:{X}` group.
6. Server fires `NotificationCreatedSignalRBroadcastHandler` → connected client receives `ReceiveNotification` payload within 500ms.
7. Client calls `MarkAsRead(notId)` → DB updated AND `NotificationRead` event broadcasts back to same user's group.
8. Client calls `MarkAsRead(notId)` for someone else's notification → `HubException("Notification.OwnerMismatch: ...")`.
9. User has 2 connected tabs → notification arrives → both tabs receive `ReceiveNotification`.
10. Tab A calls MarkAsRead → Tab B receives `NotificationRead` event (cross-tab sync).
11. Reconnect after 5s disconnect → user re-added to all 3 group types automatically.
12. 100 concurrent connections from same user → all receive same notification (no de-dupe in v1; client handles).
