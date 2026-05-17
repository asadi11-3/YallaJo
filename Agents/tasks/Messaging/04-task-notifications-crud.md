# TASK 1 — Notifications CRUD + Preferences + Inbox Handlers

> **Owner:** Mahmoud (Intermediate) — **Hours:** 40h — **Hard deadline:** Sun **2026-12-27 17:00**
> **Earliest start:** Wed 2026-12-02 09:00
> **Endpoints:** 8 HTTP + ~23 inbox handlers (PW-4 list)
> **Depends on:** PW-1..PW-7

---

## Endpoint List

| # | Method | Path | Permission |
|---|---|---|---|
| 1 | GET | `/api/v1/notifications` | `MustHavePermission(Notification, Read)` + ICurrentUser self-filter |
| 2 | GET | `/api/v1/notifications/unread-count` | same |
| 3 | GET | `/api/v1/notifications/preferences` | `MustHavePermission(NotificationPreference, Read)` + ICurrentUser |
| 4 | PUT | `/api/v1/notifications/preferences` | `MustHavePermission(NotificationPreference, Update)` + ICurrentUser |
| 5 | POST | `/api/v1/notifications/{id}/read` | `MustHavePermission(Notification, Read)` + ICurrentUser ownership |
| 6 | POST | `/api/v1/notifications/read-all` | `MustHavePermission(Notification, Read)` + ICurrentUser |
| 7 | DELETE | `/api/v1/notifications/{id}` | `MustHavePermission(Notification, Delete)` + ICurrentUser ownership |
| 8 | GET | `/api/v1/notifications/{id}` | `MustHavePermission(Notification, Read)` + ICurrentUser ownership |

All filter to current user. Admin variant out of scope for this sprint.

---

## Inbox Handlers (≈23-29)

Per 00-README §3.2 and 03-entities-matrix.md §5. Each handler is small (≤40 LOC). Skeleton:

```csharp
public sealed class BookingTourBookingConfirmedHandler(
    IMessagingInboxStore inbox,
    INotificationRepository repo,
    INotificationTemplateRenderer renderer,
    IMessagingUnitOfWork uow,
    ILogger<BookingTourBookingConfirmedHandler> logger)
    : INotificationHandler<BookingTourBookingConfirmedIntegrationEvent>
{
    public async Task Handle(BookingTourBookingConfirmedIntegrationEvent evt, CancellationToken ct)
    {
        if (await inbox.HasBeenProcessedAsync(evt.Id, ct)) return;

        var rendered = await renderer.RenderAsync(
            NotificationType.BookingConfirmed,
            NotificationChannel.InApp,
            languageCode: "en",   // resolve via UserSnapshot.LanguageCode in v2
            placeholders: new Dictionary<string, string> {
                ["TourName"] = evt.TourTitle,
                ["BookingReference"] = evt.BookingReference,
                ["Date"] = evt.SlotDate.ToString("D"),
                ["ConfirmationCode"] = evt.BookingReference
            },
            ct);

        var notification = Notification.Create(
            userId: evt.UserId,
            type: NotificationType.BookingConfirmed,
            channel: NotificationChannel.InApp,
            title: rendered.Title,
            body: rendered.Body,
            priority: NotificationPriority.Medium,
            data: $"{{\"bookingId\":\"{evt.BookingId}\"}}",
            entityType: "TourBooking",
            entityId: evt.BookingId);

        await repo.AddAsync(notification, ct);
        inbox.MarkAsProcessed(evt.Id);
        await uow.SaveChangesAsync(ct);
    }
}
```

The `INotificationDispatcher` is invoked **in-process** by `NotificationCreatedDomainEvent` handler (registered via UoW dispatch) — it fans out to SignalR (InApp), enqueues Email if user has Email channel enabled, and stub-logs Push.

---

## Domain Methods (Notification aggregate)

| Method | Raises | Notes |
|---|---|---|
| `Create(userId, type, channel, title, body, ...)` (factory) | `NotificationCreatedDomainEvent` | Verify EXISTING — may need to add fields per PW-3 |
| `MarkRead()` | `NotificationReadDomainEvent` | Idempotent — guard `if (IsRead) return Result.Success();` |
| `MarkSent(externalRef?)` | `NotificationDeliveredDomainEvent` | Called by EmailSender BG |
| `MarkFailed(reason)` | `NotificationFailedDomainEvent` | Called by EmailSender after final retry |
| `SoftDelete()` | (none — handled via auditable) | User-initiated |

---

## Cache Tags

| Query | Tag | TTL |
|---|---|---|
| `GET /notifications?...` | `notifications:user:{userId}` | 30s (high churn) |
| `GET /notifications/unread-count` | `notifications:user:{userId}:unread` | 15s |
| `GET /notifications/{id}` | `notification:{id}` | 5min |
| `GET /notifications/preferences` | `notification-preferences:user:{userId}` | 10min |

**Cache invalidation:**
- POST read/delete + inbox-handler create → `RemoveByTagAsync($"notifications:user:{userId}")` AND `RemoveByTagAsync($"notifications:user:{userId}:unread")`
- PUT preferences → `RemoveByTagAsync($"notification-preferences:user:{userId}")`

---

## Preference Validation

`UpdatePreferencesCommandValidator`:
- For each (type, channel) entry in payload:
  - `type` must be valid enum value
  - `channel` must be valid enum value
  - If `IsEnabled = false` AND `type.IsCritical()` (M-R1) → error `NotificationPreference.CannotDisableCritical`
  - Default Enabled = true for InApp + Email on critical; false for non-critical Email; always false for Push (until v3)

---

## WBS (40h)

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Notification aggregate domain methods update + tests | 4 | 2026-12-04 |
| 2 | NotificationPreference aggregate + Update + critical guard + tests | 4 | 2026-12-08 |
| 3 | 8 endpoints + commands/queries/validators/handlers + DTOs | 12 | 2026-12-13 |
| 4 | 27+ inbox handlers (lean — generate via T4 base class for parallel boilerplate) | 8 | 2026-12-17 |
| 5 | Cache wiring (tags + invalidation) | 2 | 2026-12-19 |
| 6 | Permission seeder integration test | 1 | 2026-12-19 |
| 7 | Integration tests (inbox round-trip + endpoint smoke) | 6 | 2026-12-23 |
| 8 | Outbox parity test for any messaging.* events emitted from inbox-resultant downstream side-effects (none in T1 directly) | 1 | 2026-12-23 |
| 9 | PR review fixes | 2 | 2026-12-27 |
| **Total** | | **40h** | **Sun 2026-12-27** |

---

## Acceptance Tests

1. POST /notifications/{id}/read on unread → 200 + IsRead=true + ReadAt stamped.
2. POST /notifications/{id}/read on already-read → 200 (idempotent), ReadAt unchanged.
3. POST /notifications/{id}/read on someone else's notification → 403.
4. PUT preferences disabling OtpDelivery on Email → 422 `NotificationPreference.CannotDisableCritical`.
5. Inbox handler `BookingTourBookingConfirmedHandler` processes event → creates Notification row + SignalR push fires (via in-process domain event handler).
6. Re-deliver same integration event (same ID) → inbox skip, no duplicate Notification row.
7. `GET /notifications?type=BookingConfirmed&read=false&pageSize=10` → returns cursor, items count ≤ 10, all match filter.
8. `GET /notifications/unread-count` returns scalar count, decrements after read.
9. DELETE /notifications/{id} on own → 204 + soft-delete (IsDeleted=true).
10. Cache invalidation: after POST read → next GET /notifications/unread-count returns fresh decrement (not cached stale).
