# Messaging — Critical Rules (M-R1..M-R12)

> These rules are **additive** to the master `Phase1-Phase2-Completion-INDEX.md §4` (16 universal rules). Every Messaging PR review enforces these on top.

---

## M-R1 — Critical notifications cannot be disabled

PDF 2 §12 — OTP, payment, refund, security notifications are **always delivered** regardless of NotificationPreference settings.

**Critical notification types** (hardcoded enum check):
- `OtpDelivery`
- `PaymentCompleted`
- `PaymentFailed`
- `RefundInitiated`
- `RefundCompleted`
- `SecurityAlert`
- `PasswordChanged`
- `LoginFromNewDevice`

**Implementation:**

```csharp
public static class NotificationTypeExtensions
{
    public static bool IsCritical(this NotificationType type) => type switch
    {
        NotificationType.OtpDelivery
            or NotificationType.PaymentCompleted
            or NotificationType.PaymentFailed
            or NotificationType.RefundInitiated
            or NotificationType.RefundCompleted
            or NotificationType.SecurityAlert
            or NotificationType.PasswordChanged
            or NotificationType.LoginFromNewDevice => true,
        _ => false
    };
}
```

`INotificationDispatcher.DispatchAsync` ignores `NotificationPreference.IsEnabled = false` when `type.IsCritical()`. `PUT /notifications/preferences` validator REJECTS attempts to disable critical types with `Error("NotificationPreference.CannotDisableCritical", ...)` Outcome.UnprocessableEntity.

---

## M-R2 — Notification channels

| Channel | v1 status | Implementation | Notes |
|---|---|---|---|
| InApp | ✅ Full | `Notification` table + SignalR push to `user:{userId}` via NotificationHub (T2) | Persisted; visible in bell icon |
| Email | ✅ Full | EmailSender BG (T5) picks Pending NotificationDeliveryAttempts and sends via SMTP | Templated; retried 3x; failures alert admin |
| Push | 🟡 Stub | `PushNotificationStrategy` returns `Success=false, FailureReason="PushNotImplementedYet_v3"` | Device tokens REGISTERED in this sprint but not consumed until v3 (Phase 3+) |
| SMS | ⬜ Out of scope | (no strategy) | Phase 2.5 critical-only — DEFERRED |

`NotificationChannel` enum: `InApp = 0, Email = 1, Push = 2, Sms = 3`. **All notification creates write to InApp first** (Notification row); then the dispatcher fans out to additional channels based on preferences + type defaults.

---

## M-R3 — Notification creation rule

Every `Notification.Create` invocation MUST go through this chain:

1. **Resolve template** via `INotificationTemplateRenderer.RenderAsync(type, channel, languageCode, placeholders)`.
2. **Create the Notification aggregate** (`Notification.Create(userId, type, channel, title, body, ...)`).
3. **Persist** in same UoW that triggered the create (NEVER call `SaveChanges` from within domain event handler — INDEX §4 R15).
4. **Outbox: NO** — notifications are NOT emitted as integration events (they ARE the integration). The 23 inbox handlers (00-README §3.2) emit `NotificationCreatedDomainEvent` which the `INotificationDispatcher` listens to in-process for real-time SignalR push.
5. **Dispatcher fans out** to channels via DI keyed services (PW-6).

**Anti-pattern (will fail PR review):**
```csharp
// ❌ DON'T do this in inbox handlers
var notification = Notification.Create(...);
db.Notifications.Add(notification);
await db.SaveChangesAsync(ct);   // bypasses UoW dispatch + steals from outer handler's transaction
```

**Correct pattern:**
```csharp
// ✅ Inbox handler MUST call MarkAsProcessed then return; UoW commits at outer SaveChanges boundary
public async Task Handle(BookingTourBookingConfirmedIntegrationEvent evt, CancellationToken ct)
{
    if (await _inbox.HasBeenProcessedAsync(evt.Id, ct)) return;
    var notification = Notification.Create(evt.UserId, NotificationType.BookingConfirmed, NotificationChannel.InApp, ...);
    await _notificationRepo.AddAsync(notification, ct);
    _inbox.MarkAsProcessed(evt.Id);
    await _uow.SaveChangesAsync(ct);   // single SaveChanges per integration event consumption
}
```

---

## M-R4 — Template placeholder syntax

Templates use **Mustache-style `{{Placeholder}}`** rendered by `Stubble.Core` (PW-6). Standard placeholders:

| Placeholder | Source |
|---|---|
| `{{UserName}}` | Notification.UserId → Accounts.Profile.DisplayName (resolved via inbox snapshot in PW for User snapshot table, OR via INotificationContextResolver service) |
| `{{TourName}}` | from event payload |
| `{{BookingReference}}` | from event payload, e.g. `YJ-20270215-A7X3` |
| `{{Date}}` | formatted per user's locale + tour's timezone |
| `{{Amount}}` + `{{Currency}}` | from event payload — formatted `IFormatProvider`-aware |
| `{{ConfirmationCode}}` | for OTP / booking notifications |
| `{{ProviderName}}` | for support/dispute notifications |
| `{{Url}}` | deep-link to app screen — built by `INotificationLinkBuilder` (PW-6 sibling) |

**Missing placeholder behavior:** Stubble defaults to **empty string** (silent). Required by spec PDF 2 §12 "missing placeholder = empty string". **Acceptance test asserts this.**

**Template fallback chain** (template renderer logic):

1. Look up `(NotificationType, NotificationChannel, LanguageCode)` triple
2. Fallback to `(NotificationType, NotificationChannel, "en")` if missing
3. Fallback to `(NotificationType, NotificationChannel.InApp, "en")` if still missing
4. Fallback to last-resort **inline string** in code: `"You have a new {0} notification."` with type name

Log warning whenever fallback 3 or 4 hits — indicates missing template that admin should add via T7 CRUD.

---

## M-R5 — Notification capacity per user

PDF 2 §12 lifecycle: **MAX 500 per user**, oldest READ purged first. ReadNotificationCleanupService (T6) handles the bulk purge; per-write purge is too expensive.

**Read notifications** auto-deleted after **30 days** unread never (also handled by T6 BG service).

**Implementation:** ReadNotificationCleanupService runs **weekly Sun 02:00 UTC** and:
1. Per user, `DELETE FROM Notifications WHERE UserId = @uid AND IsRead = 1 AND ReadAt < DATEADD(DAY, -30, SYSUTCDATETIME())`.
2. After the time-based purge, if a user still has > 500 notifications, deletes oldest READ first then oldest UNREAD if read-pool exhausted.
3. Critical types (M-R1) are **NEVER deleted by the cleanup job** regardless of age. Audit trail value.

Cursor pagination + `GET /notifications/unread-count` are NOT affected by purges except naturally.

---

## M-R6 — SignalR connection & group rules

NotificationHub (T2) groups:

| Group | Member set | Purpose |
|---|---|---|
| `user:{userId}` | One user | Personal notifications |
| `provider:{providerId}` | All members with `Provider` role for that provider | Provider dashboards (new booking arrived, payout cleared) |
| `admin` | All users with `Admin` role | Moderation queue, large payout, dispute filed |

**Connection handshake:** JWT Bearer required (skip authentication = `HubException("Unauthorized")` immediate disconnect). On `OnConnectedAsync`:
1. Read `Context.User.GetUserId()`.
2. Add to `user:{userId}` group.
3. Read roles → if Provider, add to `provider:{providerId}` for each provider record they're attached to (resolved via `IProviderResolverService` snapshot in Messaging DbContext).
4. If Admin, add to `admin` group.

**Group leave on disconnect** is automatic in SignalR; no cleanup code needed.

**Send patterns:**
- `await hub.Clients.Group($"user:{userId}").SendAsync("ReceiveNotification", payload, ct)`
- `await hub.Clients.Group("admin").SendAsync("NewSupportTicket", payload, ct)`

**Scaling note:** v1 uses **in-memory SignalR backplane** (single-instance API). When YallaJo scales to multi-instance, add **Azure SignalR Service** or **Redis backplane** — see 10-cross-cutting.md §SignalR Scaling.

---

## M-R7 — Email retry & exponential backoff

PDF 2 §12: **3 retry attempts with exponential backoff 1+5+15 min**.

**EmailNotificationSenderService (T5) algorithm:**

```text
foreach pending attempt where attempt.AttemptNumber < MaxRetries (default 3):
    compute due time = attempt.AttemptedAt + BackoffMinutes[attempt.AttemptNumber - 1]
    if due time > now: skip (not yet due)
    call IEmailSender.SendAsync(emailMessage, ct)
    if success: MarkSucceeded + raise NotificationDeliveredDomainEvent
    if failure:
        IncrementAttempt
        if AttemptNumber >= MaxRetries: MarkFailed + raise NotificationFailedDomainEvent + emit messaging.notification.failed.v1 → admin alert
        else: stays Pending for next tick
```

`BackoffMinutes` (default `[1, 5, 15]`) from `appsettings.json` per PW-10.

**Idempotency:** if SMTP server times out mid-send but actually delivered, the next retry may double-send. **Acceptable trade-off** for v1 — emails are receiver-side de-duplicated by message-id if SMTP server honors it. Document in error-log.md.

---

## M-R8 — Support Ticket SLA & priority assignment

PDF 2 §12 + agent-context spec:

| Category | Priority | SLA |
|---|---|---|
| `PaymentProblem` | High | 4 hours |
| `BookingIssue` | Medium | 12 hours |
| `ProviderComplaint` | Medium | 12 hours |
| `AccountHelp` | Low | 24 hours |
| `BugReport` | Low | 24 hours |
| `Other` | Low | 24 hours |

**Implementation:** `SupportTicket.Create` factory takes category, auto-derives priority via `TicketCategoryExtensions.GetDefaultPriority(category)`. SLA breach time = `CreatedAt + slaHours`.

**Round-robin assignment** logic: maintain `support.AdminAssignmentRoster` row per admin user with `LastAssignedAt`. Pick admin with oldest `LastAssignedAt` (or random among ties). Skip admins marked `IsOnLeave = true`. Future: replace with workload-aware (count of OPEN tickets per admin).

**SLA breach detection:** Phase 3+ scheduled job. For v1, just store `SlaBreachAt` on ticket and `GET /support/admin/tickets` returns sorted by `SlaBreachAt ASC` so admins naturally pick from most-overdue first.

---

## M-R9 — Device token uniqueness & cleanup

**Unique constraint:** `IX_DeviceTokens_UserId_DeviceId WHERE IsDeleted = 0`. UPSERT on POST `/devices/token`:
- if `(UserId, DeviceId)` exists → update Token + LastSeenAt
- if not → insert new row

**Stale cleanup:** rows with `LastSeenAt < now - 30 days` deleted by `ReadNotificationCleanupService` (T6) as a second pass after the read cleanup. Re-uses the same BG service; runs in same SaveChanges.

**Platform enum:** `DevicePlatform { Android = 0, Ios = 1, Web = 2 }`. Web tokens are FCM web push (browser endpoint). Android = FCM. iOS = APNs.

**Token rotation note:** when client re-registers with a new token for same device (token expired), POST `/devices/token` UPSERTs replacing the old token. Old token is NOT pushed to anymore.

---

## M-R10 — Cursor pagination

Same shape as INDEX §4 R9, Booking, Finance, Social. Cursor format: base64 of `{Id, CreatedAt}`. Default pageSize 20, max 50. `GET /notifications` supports filters `?type=BookingConfirmed,PaymentReceived&read=false&from=2026-12-01&to=2027-01-31`.

**Sort order:** ALWAYS `CreatedAt DESC, Id DESC` (newest first). Cursor reverses on `?direction=oldest-first` (rarely used; supported for archive UI).

---

## M-R11 — ICurrentUser audit list

12 handlers ALLOWED to inject `ICurrentUser`:

| Handler | Reason |
|---|---|
| `GetMyNotificationsQueryHandler` | User-scoped list |
| `GetUnreadCountQueryHandler` | User-scoped count |
| `MarkNotificationReadCommandHandler` | Ownership guard (cannot mark someone else's notification as read) |
| `MarkAllAsReadCommandHandler` | User-scoped bulk |
| `DeleteNotificationCommandHandler` | Ownership guard |
| `GetMyPreferencesQueryHandler` | User-scoped |
| `UpdatePreferencesCommandHandler` | User-scoped self-edit |
| `RegisterDeviceTokenCommandHandler` | Stamps UserId on the token |
| `DeleteDeviceTokenCommandHandler` | Ownership guard |
| `GetMyDeviceTokensQueryHandler` | User-scoped list |
| `CreateSupportTicketCommandHandler` | Stamps CreatedByUserId |
| `GetMyTicketsQueryHandler` | User-scoped list |
| `PostTicketMessageCommandHandler` | Stamps AuthorUserId + ownership guard on the ticket |
| `CloseTicketCommandHandler` | Ownership guard (only ticket creator OR assigned admin) |
| (Admin handlers use `MustHavePermission(AdminSupportQueue, ...)` — no ICurrentUser needed) | |

**All other handlers FORBIDDEN to inject `ICurrentUser`.** Permission-based auth is sufficient.

---

## M-R12 — Error registry

| Code | HTTP | Outcome |
|---|---|---|
| `Notification.NotFound` | 404 | NotFound |
| `Notification.OwnerMismatch` | 403 | Forbidden |
| `Notification.AlreadyRead` | (no — idempotent) | (treated as success) |
| `NotificationPreference.CannotDisableCritical` | 422 | UnprocessableEntity |
| `NotificationPreference.InvalidType` | 400 | Validation |
| `NotificationTemplate.NotFound` | 404 | NotFound |
| `NotificationTemplate.DuplicateKey` | 409 | Conflict |
| `NotificationTemplate.InvalidPlaceholderSyntax` | 400 | Validation |
| `DeviceToken.NotFound` | 404 | NotFound |
| `DeviceToken.OwnerMismatch` | 403 | Forbidden |
| `DeviceToken.InvalidPlatform` | 400 | Validation |
| `SupportTicket.NotFound` | 404 | NotFound |
| `SupportTicket.OwnerMismatch` | 403 | Forbidden |
| `SupportTicket.AlreadyClosed` | 422 | UnprocessableEntity |
| `SupportTicket.MessageRequired` | 400 | Validation |
| `SupportTicket.NoAdminAvailable` | 503 | ServiceUnavailable (admin roster empty — fall back to admin@yallajo.com) |
| `SupportTicket.AssignmentRoleMismatch` | 403 | Forbidden (target user not an admin) |
| `Hub.Unauthorized` | (HubException — disconnects) | — |

All codes follow `{Entity}.{Reason}` PascalCase per INDEX §4 R13.
