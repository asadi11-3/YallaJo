# TASK 5 + TASK 6 — Background Services (EmailSender + ReadCleanup)

> **TASK 5 — EmailNotificationSenderService:** Owner Mohammad, 20h, deadline Sun **2027-01-10 17:00**
> **TASK 6 — ReadNotificationCleanupService:** Owner Fadwa, 14h, deadline Sun **2027-01-10 17:00**
> **Endpoints:** 0 HTTP — **2 BackgroundService implementations.**
> **Depends on:** PW-1 (UoW dispatch), PW-6 (INotificationDispatcher + IEmailSender), PW-9 (NotificationDeliveryAttempts table), T1 (notification create + read flow), T3 (DeviceToken cleanup share)

This task wires the two cron-style hosted services for Messaging. Follows the **shared pattern from `Booking/10-task-background-services.md §1`** — refer there for the boilerplate `PeriodicTimer` + scoped service provider + try/catch policy + OTEL counters.

---

## 0. Service Catalog

| # | Service | Cadence | Project | Trigger event(s) emitted |
|---|---|---|---|---|
| 1 | `EmailNotificationSenderService` | every **30 sec** | Messaging.Infrastructure | `NotificationDeliveredDomainEvent` / `NotificationFailedDomainEvent` → integration `messaging.notification.delivered.v1` / `.failed.v1` |
| 2 | `ReadNotificationCleanupService` | **weekly Sun 02:00 UTC** | Messaging.Infrastructure | (none — purely housekeeping) |

DI registration LIFO in `Messaging.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddHostedService<ReadNotificationCleanupService>();   // stops first on shutdown
services.AddHostedService<EmailNotificationSenderService>();   // stops last
```

---

## TASK 5 — EmailNotificationSenderService

**Goal:** drain pending `NotificationDeliveryAttempts` where `Channel=Email AND Status=Pending`, send via `IEmailSender`, mark Succeeded/Failed per M-R7 exponential backoff (1+5+15 min).

### Algorithm

```csharp
private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
{
    var attemptRepo = scoped.GetRequiredService<INotificationDeliveryAttemptRepository>();
    var notRepo = scoped.GetRequiredService<INotificationRepository>();
    var emailSender = scoped.GetRequiredService<IEmailSender>();
    var uow = scoped.GetRequiredService<IMessagingUnitOfWork>();
    var now = timeProvider.GetUtcNow().UtcDateTime;

    var due = await attemptRepo.GetPendingDueAsync(
        channel: NotificationChannel.Email,
        now: now,
        backoffMinutes: options.Value.BackoffMinutes,   // [1, 5, 15]
        batchSize: options.Value.BatchSize,             // 50 default
        ct);

    if (due.Count == 0) return;

    foreach (var attempt in due)
    {
        var notification = await notRepo.GetByIdAsync(attempt.NotificationId, ct);
        if (notification is null)
        {
            // Stale attempt — notification deleted; mark Failed permanently
            attempt.MarkFailed("Notification deleted", isPermanent: true);
            continue;
        }

        var userEmail = await ResolveUserEmailAsync(notification.UserId, scoped, ct);
        if (userEmail is null)
        {
            attempt.MarkFailed("User email unknown", isPermanent: true);
            continue;
        }

        var msg = new EmailMessage(
            ToEmail: userEmail.Address,
            ToName: userEmail.Name,
            Subject: notification.Title,
            PlainBody: notification.Body,
            HtmlBody: null);  // T7 templates can produce HTML later

        attempt.MarkAttempted();
        EmailSendResult result;
        try
        {
            result = await emailSender.SendAsync(msg, ct);
        }
        catch (Exception ex)
        {
            // Per INDEX §4 R12 — Infrastructure may catch general Exception around external service
            result = new(false, null, ex.Message);
            logger.LogWarning(ex, "Email send threw exception for attempt {AttemptId}", attempt.Id);
        }

        if (result.Success)
        {
            attempt.MarkSucceeded(result.ExternalRef);
            notification.MarkSent(result.ExternalRef);
            BookingDiagnostics.NotificationsDelivered.Add(1, new("channel", "Email"));
        }
        else
        {
            attempt.MarkFailed(result.FailureReason ?? "Unknown");
            if (attempt.AttemptNumber >= options.Value.MaxRetries)
            {
                notification.MarkFailed($"Email send failed after {attempt.AttemptNumber} attempts: {result.FailureReason}");
                BookingDiagnostics.NotificationsFinalFailed.Add(1, new("channel", "Email"));
            }
        }
    }

    await uow.SaveChangesAsync(ct);
    BookingDiagnostics.NotificationsProcessed.Add(due.Count, new("service", nameof(EmailNotificationSenderService)));
}
```

**`ResolveUserEmailAsync`** — needs cross-module read of `Accounts.UserProfile.Email`. Options:
1. Read direct from `accounts.UserProfiles` table (cross-DbContext via shared connection string — acceptable read-only).
2. Add `UserSnapshot` table in Messaging populated by inbox handler `AuthUserRegisteredHandler` (already wired in PW) — stores `UserId, Email, FullName, LanguageCode, NotificationsEnabled`.

**Pick option 2** — keeps modules decoupled and survives Accounts DB split. Add `UserSnapshot` table in PW-9 if not already there.

### Options Class

```csharp
public sealed class EmailNotificationSenderOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);
    public int BatchSize { get; set; } = 50;
    public int MaxRetries { get; set; } = 3;
    public int[] BackoffMinutes { get; set; } = [1, 5, 15];
    public bool Enabled { get; set; } = true;
}
```

### WBS (20h)

| # | Step | Hours |
|---|---|---|
| 1 | Options + appsettings + dev overrides | 2 |
| 2 | UserSnapshot table + inbox handler `AuthUserRegisteredHandler` (extends prior sprint scope) | 4 |
| 3 | Service class + ProcessBatchAsync + idempotent retry | 6 |
| 4 | `ResolveUserEmailAsync` via UserSnapshot lookup | 1 |
| 5 | DI registration | 1 |
| 6 | OpenTelemetry counters | 1 |
| 7 | Integration test (Pending row → send → Succeeded) | 3 |
| 8 | Failure-path tests (3 retries then permanent fail + outbox event) | 1 |
| 9 | PR fixes | 1 |
| **Total** | | **20h** |

### Acceptance

1. Insert `NotificationDeliveryAttempt(Channel=Email, Status=Pending, AttemptedAt=null)` → after 30s tick → Status=Succeeded, ExternalRef populated.
2. Fake `IEmailSender` returns failure → Status=Pending, AttemptNumber=1; wait 1min → 2nd retry; wait 5min → 3rd retry; wait 15min → Status=Failed + `messaging.notification.failed.v1` in outbox.
3. Notification with deleted user → MarkFailed("Notification deleted") immediately.
4. SMTP throws exception → caught, MarkFailed with exception message, retry next tick.
5. OTEL `messaging_notifications_delivered_total{channel=Email}` increments on success.

---

## TASK 6 — ReadNotificationCleanupService

**Goal:** weekly Sun 02:00 UTC. Two responsibilities:
1. **Notification purge** (M-R5): delete READ notifications older than 30 days. Cap each user at 500 notifications.
2. **Device token cleanup** (M-R9): delete device tokens not seen in 30 days.

### Algorithm

```csharp
private async Task ProcessBatchAsync(IServiceProvider scoped, CancellationToken ct)
{
    var notRepo = scoped.GetRequiredService<INotificationRepository>();
    var deviceRepo = scoped.GetRequiredService<IDeviceTokenRepository>();
    var uow = scoped.GetRequiredService<IMessagingUnitOfWork>();
    var now = timeProvider.GetUtcNow().UtcDateTime;
    var olderThan = now.AddDays(-options.Value.OlderThanDays); // 30

    // 1. Time-based purge — bulk DELETE non-critical READ notifications older than 30d
    var purgedTime = await notRepo.DeleteOldReadNonCriticalAsync(
        olderThan: olderThan,
        batchSize: options.Value.BatchSize,
        ct);

    // 2. Per-user cap (500 max) — find users over the cap, oldest READ first
    var purgedCap = await notRepo.EnforcePerUserCapAsync(
        maxPerUser: 500,
        batchSize: options.Value.BatchSize,
        ct);

    // 3. Device token cleanup
    var purgedTokens = await deviceRepo.DeleteStaleAsync(
        olderThan: olderThan,
        ct);

    await uow.SaveChangesAsync(ct);

    logger.LogInformation(
        "ReadNotificationCleanup: purged {Time} by time, {Cap} by cap, {Tokens} stale tokens",
        purgedTime, purgedCap, purgedTokens);
}
```

**Bulk SQL for `DeleteOldReadNonCriticalAsync`:**

```sql
DELETE TOP (@batch) FROM messaging.Notifications
WHERE IsRead = 1
  AND ReadAt < @olderThan
  AND Type NOT IN (1, 10, 11, 12, 13, 30, 31, 32) -- critical types (M-R1)
  AND IsDeleted = 0
```

**Per-user cap** uses CTE:

```sql
WITH OverCap AS (
  SELECT Id,
         ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY IsRead DESC, CreatedAt ASC) AS rn,
         COUNT(*) OVER (PARTITION BY UserId) AS userTotal
  FROM messaging.Notifications
  WHERE IsDeleted = 0
)
DELETE FROM messaging.Notifications
WHERE Id IN (
  SELECT Id FROM OverCap
  WHERE userTotal > 500 AND rn > 500
);
```

(NB — for very large users with >500 notifications this hits hot paths; consider per-user iteration in v2.)

### Cron-style scheduling

Use the `NextOccurrenceWeekly(now, DayOfWeek.Sunday, TimeOnly(02,00))` helper from `SharedKernel.Infrastructure/Time/`. Compute next Sunday 02:00 UTC, `Task.Delay` to it, then `PeriodicTimer(TimeSpan.FromDays(7))` thereafter.

### Options

```csharp
public sealed class ReadNotificationCleanupOptions
{
    public DayOfWeek TargetUtcDay { get; set; } = DayOfWeek.Sunday;
    public TimeOnly TargetUtcTime { get; set; } = new(02, 00);
    public int OlderThanDays { get; set; } = 30;
    public int BatchSize { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
}
```

### WBS (14h)

| # | Step | Hours |
|---|---|---|
| 1 | Options + appsettings | 1 |
| 2 | Bulk SQL repository methods (DeleteOldReadNonCriticalAsync, EnforcePerUserCapAsync, DeleteStaleAsync) | 4 |
| 3 | Service class + NextOccurrenceWeekly helper | 3 |
| 4 | DI registration | 1 |
| 5 | Integration test (seed 600 notifications, run service, expect 100 deleted) | 3 |
| 6 | Integration test (deletes stale tokens) | 1 |
| 7 | PR fixes | 1 |
| **Total** | | **14h** |

### Acceptance

1. Seed 700 notifications for 1 user (500 read >30d, 200 unread) → service deletes the 500 read+expired → user now has 200 unread (within cap).
2. Critical type notifications never deleted (seed OtpDelivery from 60 days ago → stays).
3. User with 600 notifications (any state) → cap enforces 500, oldest READ first, then oldest UNREAD if read exhausted.
4. Device tokens with LastSeenAt > 30 days deleted.
5. Service runs on Sunday 02:00 UTC ONLY (verify timer fires exactly once per week).
6. Service can be disabled via `Enabled=false` config → boot log "service disabled, skipping registration".
