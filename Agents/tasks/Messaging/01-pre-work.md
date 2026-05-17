# Messaging — Pre-Work (PW-1..PW-8)

> **Tech Lead drives. ALL pre-work merges into `main` BEFORE Day-0 kickoff Sun 2026-11-29 09:30. Hard deadline: Tue 2026-12-01 17:00 AST.** Task owners are blocked from starting until PW lands; the workaround is to use feature branches off of the PW branch but rebase once main updates.

---

## PW-1 — MessagingUnitOfWork delegates to SharedKernel

**Current bug** (same shape as ContentBlogs/SEO and Booking and Finance hit in their respective sprints — see error-log.md):

```csharp
// Messaging.Infrastructure/Persistence/MessagingUnitOfWork.cs (likely WRONG today)
public sealed class MessagingUnitOfWork(MessagingDbContext ctx) : IMessagingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => ctx.SaveChangesAsync(ct);   // ❌ bypasses MediatR Publish for aggregate roots
}
```

**Required fix:**

```csharp
public sealed class MessagingUnitOfWork(IUnitOfWork<MessagingDbContext> inner) : IMessagingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);  // ✅ delegates to SharedKernel which dispatches domain events BEFORE SaveChanges
}
```

**Acceptance gate:**
1. `MessagingUnitOfWorkDispatchesEventsTests` (xunit, NSubstitute IPublisher) — POSTs a fake aggregate raising 1 domain event, calls SaveChangesAsync → asserts `IPublisher.Publish` called 1× **before** the SaveChanges call returns. Test must run **green** in CI.
2. `dotnet build Messaging.Infrastructure` clean (zero warnings).
3. Code-review: no other call to `ctx.SaveChangesAsync` directly anywhere in `Messaging.Infrastructure/Persistence/` or `Messaging.Infrastructure/EventHandlers/`. Tech Lead greps for this.

---

## PW-2 — IAggregateRoot markers + AuditableEntity upgrades

**Aggregates to promote** (5 total — verify with `find . -name '*.cs' -exec grep -l 'class Notification' {} \;` etc.):

| Entity | Current base | New base | Migration touch | Notes |
|---|---|---|---|---|
| `Notification` | AuditableEntity ✅ (already) | + IAggregateRoot | none if RowVersion already on AuditableEntity | 71L — `Create` + `MarkSent` + `MarkRead` already present; verify they raise domain events (PW-3) |
| `NotificationPreference` | AuditableEntity | + IAggregateRoot | none | Per-user-per-type-per-channel triple key — see EF config in 03-entities-matrix.md |
| `NotificationTemplate` | AuditableEntity | + IAggregateRoot | none | Admin-only CRUD |
| `DeviceToken` | AuditableEntity | + IAggregateRoot | none | Per-user-per-deviceId UNIQUE |
| `SupportTicket` | AuditableEntity | + IAggregateRoot | none | Owns TicketMessage collection (BaseEntity, NOT promoted) |
| `TicketMessage` | BaseEntity | **stays BaseEntity** | none | Owned child; SupportTicket raises `TicketMessageAdded` |
| `ChatBotConversation` | BaseEntity | **stays BaseEntity** | none | OUT OF SCOPE Phase 4 |
| `ChatBotMessage` | BaseEntity | **stays BaseEntity** | none | OUT OF SCOPE Phase 4 |

**Migration:** `MessagingAddAggregateRootAndAuditMembers` — likely **NO COLUMN CHANGES** since all 5 are already AuditableEntity (RowVersion + IsDeleted + DeletedAt already present from existing infrastructure). If migration is empty, **commit empty migration anyway** so the migration sequence stays clean.

**Acceptance gate:**
1. All 5 entities implement `IAggregateRoot` (marker interface).
2. `EfRepository<T>` generic constraint compiles for all 5 (repository interface inheritance test in `tests/Messaging.Tests.Unit/Domain/AggregateRootMarkerTests.cs`).
3. `dotnet ef migrations add MessagingAddAggregateRootAndAuditMembers` produces a (possibly empty) migration. **Empty migration committed with a comment explaining why.**
4. `MessagingDbContext.OnModelCreating` — verify entity configurations live in separate `*Configuration.cs` files (NOT inline fluent API in OnModelCreating).

---

## PW-3 — Domain event record stubs

Seed **14 domain event records** under `Messaging.Domain/Events/`. Each record is a 1-line file:

```csharp
public sealed record NotificationCreatedDomainEvent(Guid NotificationId, Guid UserId, NotificationType Type, NotificationChannel Channel) : IDomainEvent;
```

Full list (handlers wired in respective tasks; events raised by aggregate methods on their respective state transitions):

1. `NotificationCreatedDomainEvent` — raised by `Notification.Create` (verify existing method — likely needs update)
2. `NotificationReadDomainEvent` — raised by `Notification.MarkRead`
3. `NotificationDeliveredDomainEvent` — raised by `Notification.MarkSent`
4. `NotificationFailedDomainEvent` — raised by `Notification.MarkFailed` (NEW method)
5. `NotificationPreferenceUpdatedDomainEvent` — raised by `NotificationPreference.Update`
6. `NotificationTemplateCreatedDomainEvent` — raised by `NotificationTemplate.Create`
7. `NotificationTemplateUpdatedDomainEvent` — raised by `NotificationTemplate.Update`
8. `NotificationTemplateDeletedDomainEvent` — raised by `NotificationTemplate.Delete`
9. `DeviceTokenRegisteredDomainEvent` — raised by `DeviceToken.Register` (UPSERT pattern)
10. `DeviceTokenDeletedDomainEvent` — raised by `DeviceToken.Delete`
11. `TicketCreatedDomainEvent` — raised by `SupportTicket.Create`
12. `TicketAssignedDomainEvent` — raised by `SupportTicket.AssignTo`
13. `TicketResolvedDomainEvent` — raised by `SupportTicket.Resolve`
14. `TicketClosedDomainEvent` — raised by `SupportTicket.Close`

**NB**: prior sprint left `Notification.Create` already raising events but check exact payload — likely needs more fields (Channel, EntityType, EntityId). Update factory if mismatch.

**Acceptance gate:**
1. All 14 records compile under `Messaging.Domain/Events/`.
2. Each event payload contains enough info for downstream consumers (no need to query DB to react).
3. Aggregate methods that should raise events DO raise them — covered by unit tests in T1/T2/T3/T4/T7.
4. No domain event record is unused (every one has at least 1 in-process handler **or** 1 integration event mapping per PW-4).

---

## PW-4 — Integration event records + IntegrationEventTypeRegistry

Seed **6 integration event records** in `Messaging.Contracts/IntegrationEvents/` (the public surface). NOT all domain events map to integration events — many notification events stay intra-module (read/created don't need to leave).

**External integration events emitted by Messaging:**

| # | Logical name | Record | Trigger | Consumer |
|---|---|---|---|---|
| 1 | `messaging.notification.delivered.v1` | `NotificationDeliveredIntegrationEvent` | EmailSender BG marks email sent | Analytics (delivery success rate metric) |
| 2 | `messaging.notification.failed.v1` | `NotificationFailedIntegrationEvent` | EmailSender BG exhausts retries | Analytics + Admin Slack |
| 3 | `messaging.ticket.created.v1` | `TicketCreatedIntegrationEvent` | SupportTicket.Create | Admin dashboard real-time counter |
| 4 | `messaging.ticket.assigned.v1` | `TicketAssignedIntegrationEvent` | SupportTicket.AssignTo | Audit log |
| 5 | `messaging.ticket.resolved.v1` | `TicketResolvedIntegrationEvent` | SupportTicket.Resolve | Analytics (SLA breach metric, CSAT prompt trigger) |
| 6 | `messaging.support-sla-breached.v1` | `SupportSlaBreachedIntegrationEvent` | (deferred — Phase 3 alerting system) | Stub registered, no consumer yet |

**Register all 6 in `IntegrationEventTypeRegistry.cs` under `messaging.` namespace prefix.**

**Acceptance gate:**
1. `IntegrationEventTypeRegistryParityTests.cs` (mirrors Booking/Finance/Social pattern) — every declared `…IntegrationEvent` is registered, every registered `messaging.*` logical name has a declared record. Zero drift.
2. Records are immutable (`sealed record`), implement `IIntegrationEvent`, and contain ONLY primitive types + `Guid` + `string` + DateTime (no entity references, no DTOs).

---

## PW-5 — Repository interface stubs

Seed **6 application-level repository interfaces** under `Messaging.Application/Interfaces/`. Implementations live in `Messaging.Infrastructure/Repositories/` (EF Core primary-constructor pattern from ContentSeo prior sprint).

| Interface | Custom finders required |
|---|---|
| `INotificationRepository` | `GetByUserPagedAsync(userId, cursor, pageSize, typeFilter, readFilter, ct)` cursor-paginated; `GetUnreadCountByUserAsync(userId, ct)`; `MarkAllAsReadByUserAsync(userId, ct)` (bulk SQL); `DeleteOldReadAsync(olderThan, batchSize, ct)` for the BG cleanup; `GetPendingDispatchAsync(channel, batchSize, ct)` for EmailSender BG |
| `INotificationPreferenceRepository` | `GetByUserAsync(userId, ct)` + `UpsertAsync(prefs, ct)` (uses MERGE pattern); `IsEnabledForUserAsync(userId, type, channel, ct)` for dispatch decision |
| `INotificationTemplateRepository` | `GetByKeyAndLanguageAsync(key, lang, ct)` (key = NotificationType + Channel + Language tuple); `ListAllAsync(filter, ct)` for admin CRUD |
| `IDeviceTokenRepository` | `GetActiveByUserAsync(userId, ct)`; `GetByTokenAsync(token, ct)`; `DeleteStaleAsync(olderThan, ct)` (30-day stale per agent-context §11.1 spec) |
| `ISupportTicketRepository` | `GetByUserPagedAsync` + `GetByAdminPagedAsync(filters)` + `GetByIdWithMessagesAsync(id)` (Include) + `GetUnassignedOldestAsync(ct)` for round-robin |
| `INotificationDeliveryAttemptRepository` | (NEW BaseEntity in PW-9 — see below) `GetPendingForRetryAsync(now, ct)` + `MarkAttempted/MarkSucceeded/MarkFailed` |

**Acceptance gate:**
1. All 6 interfaces compile.
2. 6 EF implementations compile (primary-constructor `public sealed class EfXxxRepository(MessagingDbContext db) : IXxxRepository`).
3. All custom finders have at least 1 unit test in `tests/Messaging.Tests.Unit/Repositories/`.
4. No leaky abstractions — interfaces return `IReadOnlyList<T>` or `T?`, never `IQueryable<T>` and never `DbContext`.

---

## PW-6 — Notification dispatch abstractions

Seed in `SharedKernel.Application/Abstractions/Messaging/`:

```csharp
public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, CancellationToken ct);
}

public interface INotificationChannelStrategy
{
    NotificationChannel Channel { get; }
    Task<NotificationDispatchResult> SendAsync(Notification notification, RenderedTemplate rendered, CancellationToken ct);
}

public sealed record NotificationDispatchResult(bool Success, string? FailureReason, string? ExternalRef);

public interface INotificationTemplateRenderer
{
    Task<RenderedTemplate> RenderAsync(NotificationType type, NotificationChannel channel, string languageCode, IReadOnlyDictionary<string, string> placeholders, CancellationToken ct);
}

public sealed record RenderedTemplate(string Title, string Body, string? Html);

public interface IEmailSender   // wraps SMTP / SES / SendGrid impl
{
    Task<EmailSendResult> SendAsync(EmailMessage msg, CancellationToken ct);
}

public sealed record EmailMessage(string ToEmail, string ToName, string Subject, string PlainBody, string? HtmlBody);
public sealed record EmailSendResult(bool Success, string? ExternalRef, string? FailureReason);
```

**Implementations** (in `Messaging.Infrastructure/Dispatch/`):

- `NotificationDispatcher` — resolves the right `INotificationChannelStrategy` via DI keyed by `NotificationChannel`, calls SendAsync, persists the attempt to `NotificationDeliveryAttempts` table (see PW-9).
- `InAppNotificationStrategy` — pushes via SignalR `INotificationHubContext<NotificationHub>` to `user:{userId}` group (T2 wires hub itself).
- `EmailNotificationStrategy` — enqueues `NotificationDeliveryAttempt` row Channel=Email Status=Pending, EmailSender BG picks up.
- `PushNotificationStrategy` — STUB returns Success=false with reason `"PushNotImplementedYet_v3"` and logs warning. Phase 3+ work.
- `SmtpEmailSender` impl uses `MailKit` (already in solution per ContentBlogs sprint Welcome email) reading `Messaging:Email:Smtp` config section.
- `BlocklistEmailSender` test-double for unit tests.
- `MustacheNotificationTemplateRenderer` — uses **`Stubble.Core`** NuGet (Mustache-compatible C# renderer) for `{{Placeholder}}` substitution.

**Acceptance gate:**
1. All abstractions compile in SharedKernel.
2. DI registration in `Messaging.Infrastructure/DependencyInjection.cs` uses **keyed services** (.NET 8+ feature): `services.AddKeyedScoped<INotificationChannelStrategy, InAppNotificationStrategy>(NotificationChannel.InApp);` and so on.
3. Unit tests in `tests/Messaging.Tests.Unit/Dispatch/`: verify dispatcher picks right strategy per channel, verifies failure path persists attempt row with FailureReason.

---

## PW-7 — Permission catalog seeding

Seed in `Messaging.Contracts/Authorization/`:

```csharp
public static class MessagingFeatures
{
    public const string Notification = nameof(Notification);
    public const string NotificationPreference = nameof(NotificationPreference);
    public const string NotificationTemplate = nameof(NotificationTemplate);
    public const string DeviceToken = nameof(DeviceToken);
    public const string SupportTicket = nameof(SupportTicket);
    public const string AdminSupportQueue = nameof(AdminSupportQueue);
}

public sealed class MessagingPermissionCatalog : IPermissionCatalog
{
    public string Module => "Messaging";
    public IReadOnlyCollection<PermissionDescriptor> Permissions { get; } = [
        new(MessagingFeatures.Notification, AppAction.Read),
        new(MessagingFeatures.Notification, AppAction.Delete),
        new(MessagingFeatures.NotificationPreference, AppAction.Read),
        new(MessagingFeatures.NotificationPreference, AppAction.Update),
        new(MessagingFeatures.NotificationTemplate, AppAction.Create),
        new(MessagingFeatures.NotificationTemplate, AppAction.Read),
        new(MessagingFeatures.NotificationTemplate, AppAction.Update),
        new(MessagingFeatures.NotificationTemplate, AppAction.Delete),
        new(MessagingFeatures.DeviceToken, AppAction.Create),
        new(MessagingFeatures.DeviceToken, AppAction.Read),
        new(MessagingFeatures.DeviceToken, AppAction.Delete),
        new(MessagingFeatures.SupportTicket, AppAction.Create),
        new(MessagingFeatures.SupportTicket, AppAction.Read),
        new(MessagingFeatures.SupportTicket, AppAction.Update),
        new(MessagingFeatures.SupportTicket, AppAction.Close),    // NEW AppAction
        new(MessagingFeatures.AdminSupportQueue, AppAction.Read),
        new(MessagingFeatures.AdminSupportQueue, AppAction.Assign),  // NEW AppAction
        new(MessagingFeatures.AdminSupportQueue, AppAction.Resolve)   // NEW AppAction (or reuse from Social if same enum value)
    ];
}
```

**NEW AppActions required:** `Close`, `Assign`, `Resolve` — verify against existing enum, ADD if missing (likely missing). Coordinate with cross-cutting AppAction set across modules.

**Expected boot log:**

```
PermissionSeeder discovered 10 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social, Messaging
PermissionSeeder inserted/verified 18 Messaging permissions
```

**Acceptance gate:**
1. Catalog discovered automatically via DI scan (registration in `Messaging.Infrastructure/DependencyInjection.cs` line `services.AddSingleton<IPermissionCatalog, MessagingPermissionCatalog>();`).
2. `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Messaging.%'` = **18** after first boot.

---

## PW-8 — Test projects scaffolded

Create:

```
tests/Messaging.Tests.Unit/
  Messaging.Tests.Unit.csproj      (xunit 2.9.3 + NSubstitute 5.3.0 + FluentAssertions 7.0.0 + EF InMemory 9.0.15)
  Domain/AggregateRootMarkerTests.cs
  Persistence/MessagingUnitOfWorkDispatchesEventsTests.cs
  Repositories/* (6 stub test classes per PW-5 finder)
  Dispatch/NotificationDispatcherTests.cs

tests/Messaging.IntegrationTests/
  Messaging.IntegrationTests.csproj (xunit + Mvc.Testing 9.0.15 + EF InMemory)
  IntegrationEventTypeRegistryParityTests.cs
  Outbox/EventDispatchSanityTests.cs
  Hubs/NotificationHubE2ETests.cs  (T2 — uses TestServer + SignalR client)
```

**`InternalsVisibleTo`** in csproj:
- `Messaging.Application` exposes internals to `Messaging.Tests.Unit` + `Messaging.IntegrationTests`
- `Messaging.Infrastructure` exposes internals to `Messaging.Tests.Unit` + `Messaging.IntegrationTests`

**Acceptance gate:**
1. Both test projects compile and execute (even with 0 tests).
2. `dotnet test tests/Messaging.Tests.Unit` green.
3. `dotnet test tests/Messaging.IntegrationTests` green.

---

## PW-9 — NotificationDeliveryAttempts table

EmailSender BG and any retry-logic needs an audit trail of every send attempt per notification per channel. Schema:

```csharp
public sealed class NotificationDeliveryAttempt : BaseEntity
{
    public Guid NotificationId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DateTime AttemptedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }  // Pending, Succeeded, Failed
    public int AttemptNumber { get; private set; }     // 1, 2, 3
    public string? FailureReason { get; private set; }
    public string? ExternalRef { get; private set; }   // e.g. SES Message ID
}
```

Migration `MessagingAddNotificationDeliveryAttempts`:
- Indexes: `IX_NotificationDeliveryAttempts_Status_AttemptedAt WHERE Status='Pending'` for BG service query
- `IX_NotificationDeliveryAttempts_NotificationId` for audit lookup

**Acceptance gate:**
1. Migration generated + applied in dev.
2. Schema seeded by `MessagingDbInitializer` if dev environment.
3. Repository `INotificationDeliveryAttemptRepository` per PW-5 list.

---

## PW-10 — appsettings template

Block to add to `appsettings.json`:

```json
{
  "Messaging": {
    "Email": {
      "Smtp": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "UseStartTls": true,
        "Username": "",   // env var only — never commit
        "Password": ""    // env var only — never commit; STRIP SPACES from Gmail app pwd per Gotcha #18 from agent-context §9.1
      },
      "FromAddress": "no-reply@yallajo.com",
      "FromName": "YallaJo"
    },
    "BackgroundServices": {
      "EmailNotificationSender": {
        "Interval": "00:00:30",
        "BatchSize": 50,
        "MaxRetries": 3,
        "InitialDelay": "00:00:30",
        "BackoffMinutes": [1, 5, 15],
        "Enabled": true
      },
      "ReadNotificationCleanup": {
        "TargetUtcDay": "Sunday",
        "TargetUtcTime": "02:00:00",
        "OlderThanDays": 30,
        "BatchSize": 1000,
        "Enabled": true
      }
    },
    "SignalR": {
      "MaxParallelInvocationsPerClient": 1,
      "ClientTimeoutSeconds": 30,
      "KeepAliveIntervalSeconds": 15,
      "EnableDetailedErrors": false  // true in dev only
    },
    "Tickets": {
      "Sla": {
        "HighHours": 4,
        "MediumHours": 12,
        "LowHours": 24
      }
    }
  }
}
```

**Acceptance gate:** appsettings.Development.json overrides with smaller intervals (5s EmailSender) + `EnableDetailedErrors: true`.

---

## PW Total Hours

**~10 hours Tech Lead time over 3 days** (PW-1 1h, PW-2 1h, PW-3 1h, PW-4 1h, PW-5 2h, PW-6 2h, PW-7 0.5h, PW-8 1h, PW-9 0.5h, PW-10 — already in this file). Done.
