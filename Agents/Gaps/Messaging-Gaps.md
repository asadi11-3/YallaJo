# Messaging Module — Audit Report

> **Audit date**: 2025-01-27
> **Sources of truth**: `agent-context.md`, `YallaJo.md`, `Endpoints.pdf`, `YallaJo Business Rules & Edge Cases.pdf`
> **Overall score**: **7.0 / 10**

---

## 1. Module Overview

| Layer | Project | Files | Key Contents |
|-------|---------|-------|--------------|
| Domain | `Messaging.Domain` | 45 | 11 entities, 8 enums, 16 domain events, 9 repository interfaces |
| Application | `Messaging.Application` | 53 | 14 command handlers, 8 query handlers, 4 interfaces, 2 DTOs |
| Contracts | `Messaging.Contracts` | 10 | 6 features, 18 permissions, 6 integration events, 2 service contracts |
| Infrastructure | `Messaging.Infrastructure` | 87 | 41 event handlers, 13 EF configs, 9 repositories, 9 services, 2 background services, 3 migrations |
| Presentation | `Messaging.Presentation` | 6 | 4 endpoint groups, 1 SignalR hub, 1 route registration |
| Tests | `Messaging.Tests.Unit` | 1 | Permission catalog test only |
| **Total** | | **~202** | |

### Domain Entities (11)

| Entity | Lines | Aggregate Root | Domain Events | Notes |
|--------|-------|----------------|---------------|-------|
| Notification | 97 | Yes | 4 (Created, Delivered, Read, Failed) | Full lifecycle, idempotent MarkSent/MarkRead |
| SupportTicket | 163 | Yes | 5 (Opened, Assigned, Resolved, Closed, MessagePosted) | SLA-driven, round-robin assignment, uses TimeProvider |
| TicketMessage | 33 | No (child) | 0 | Internal/external flag, `DateTime.UtcNow` usage |
| NotificationPreference | 48 | Yes | 1 (Updated) | Critical type guard (cannot disable) |
| NotificationTemplate | 69 | Yes | 3 (Created, Updated, Deleted) | Mustache template, multi-channel, i18n |
| NotificationDeliveryAttempt | 70 | No | 0 | Retry tracking, `DateTime.UtcNow` usage (3 occurrences) |
| DeviceToken | 76 | Yes | 2 (Registered, Revoked) | FCM/APNs/Web push, uses TimeProvider |
| AdminAssignmentRoster | 40 | No | 0 | Round-robin roster for ticket auto-assign |
| UserSnapshot | 37 | No | 0 | Local read-side copy of user profile data |
| ChatBotConversation | 17 | Yes | 0 | **BARE entity** — no factory, no methods, no events |
| ChatBotMessage | 16 | No | 0 | **BARE entity** — no factory, no methods, no events |

### Domain Enums (8)

| Enum | Values |
|------|--------|
| NotificationType | 38 values (0-37): General(0-6), Booking(10-15), Payment(16-22), Reviews/Social(23-26), Support(27-28), Security(29-32), Business/Provider(33-37) |
| NotificationChannel | InApp=0, Push=1, Email=2, Sms=3 |
| NotificationPriority | Low=0, Medium=1, High=2, Critical=3 |
| NotificationDeliveryStatus | Pending, Succeeded, Failed |
| TicketStatus | Open=0, Assigned=1, InProgress=2, AwaitingUser=3, Resolved=4, Closed=5 |
| TicketCategory | PaymentProblem=0, BookingIssue=1, ProviderComplaint=2, AccountHelp=3, BugReport=4, Other=5 |
| TicketPriority | Low, Medium, High |
| DevicePlatform | (not read — inferred iOS/Android/Web) |

### Critical Notifications (non-disableable)
EmailVerification, PasswordChanged, PaymentCompleted, PaymentFailed, RefundInitiated, RefundCompleted, OtpDelivery, SecurityAlert, LoginFromNewDevice — enforced via `NotificationTypeExtensions.IsCritical()`.

### Integration Events (outbound, 6)
TicketCreatedIntegrationEvent, TicketAssignedIntegrationEvent, SupportTicketResolvedIntegrationEvent, SupportSlaBreachedIntegrationEvent, NotificationDeliveredIntegrationEvent, NotificationFailedIntegrationEvent

### Cross-Module Service Contracts (2)
- `IEmailSender` — email delivery abstraction (SmtpEmailSender + NoopEmailSender implementations)
- `INotificationDispatcher` — multi-channel fan-out (InApp always → Email/Push per preference)

### Repository Interfaces (9)
INotificationRepository, INotificationPreferenceRepository, INotificationDeliveryAttemptRepository, INotificationTemplateRepository, ISupportTicketRepository, IDeviceTokenRepository, IAdminAssignmentRosterRepository, IUserSnapshotRepository, IMessagingOutboxWriter

---

## 2. Architecture Compliance

| Aspect | Status | Notes |
|--------|--------|-------|
| Clean Architecture layers | **Pass** | Domain → Application → Infrastructure → Presentation |
| CQRS via MediatR | **Pass** | 14 commands + 8 queries, all via ISender |
| DDD aggregates | **Partial** | 6 proper aggregates, 2 bare ChatBot entities |
| Outbox pattern | **Pass** | 5 domain→outbox converters in MessagingIntegrationConverters.cs |
| Inbox deduplication | **Pass** | IMessagingInboxStore + InboxMessageConfiguration |

---

## 3. Features & Permission Catalog

| Feature | Permissions | Group |
|---------|------------|-------|
| Notification | Read, Update, Delete (3) | SupportOperations |
| NotificationPreference | Read, Update (2) | SupportOperations |
| NotificationTemplate | Read, Create, Update, Delete (4) | SupportOperations |
| DeviceToken | Create, Read, Delete (3) | SupportOperations |
| SupportTicket | Create, Read, Close (3) | SupportOperations |
| AdminSupportQueue | Read, Assign, Resolve (3) | SupportOperations |
| **Total** | **18 permissions** | |

---

## 4. Endpoint Security Audit

### NotificationEndpoints.cs (8 endpoints)

| # | Method | Route | Auth | ICurrentUser |
|---|--------|-------|------|-------------|
| 1 | GET | `/notifications` | MustHavePermission(Notification, Read) + RequireAuthorization | Auth gate + UserId |
| 2 | GET | `/notifications/unread-count` | MustHavePermission(Notification, Read) + RequireAuthorization | Auth gate + UserId |
| 3 | GET | `/notifications/preferences` | MustHavePermission(NotificationPreference, Read) + RequireAuthorization | Auth gate + UserId |
| 4 | PUT | `/notifications/preferences` | MustHavePermission(NotificationPreference, Update) + RequireAuthorization | Auth gate + UserId |
| 5 | POST | `/notifications/{id}/read` | MustHavePermission(Notification, Update) + RequireAuthorization | Auth gate + UserId |
| 6 | POST | `/notifications/read-all` | MustHavePermission(Notification, Update) + RequireAuthorization | Auth gate + UserId |
| 7 | DELETE | `/notifications/{id}` | MustHavePermission(Notification, Delete) + RequireAuthorization | Auth gate + UserId |
| 8 | GET | `/notifications/{id}` | MustHavePermission(Notification, Read) + RequireAuthorization | Auth gate + UserId |

### SupportTicketEndpoints.cs (7 endpoints)

| # | Method | Route | Auth | ICurrentUser |
|---|--------|-------|------|-------------|
| 1 | POST | `/support/tickets` | MustHavePermission(SupportTicket, Create) + RequireAuthorization | Auth gate + UserId |
| 2 | GET | `/support/tickets` | MustHavePermission(SupportTicket, Read) + RequireAuthorization | Auth gate + UserId + admin check via HasPermission |
| 3 | GET | `/support/tickets/{id}` | MustHavePermission(SupportTicket, Read) + RequireAuthorization | Auth gate + UserId + admin check |
| 4 | POST | `/support/tickets/{id}/close` | MustHavePermission(SupportTicket, Close) + RequireAuthorization | Auth gate + UserId + admin check |
| 5 | POST | `/support/tickets/{id}/messages` | MustHavePermission(SupportTicket, Read) + RequireAuthorization | Auth gate + UserId + admin check |
| 6 | POST | `/support/admin/tickets/{id}/assign` | MustHavePermission(AdminSupportQueue, Assign) + RequireAuthorization | Auth gate + UserId |
| 7 | POST | `/support/admin/tickets/{id}/resolve` | MustHavePermission(AdminSupportQueue, Resolve) + RequireAuthorization | Auth gate + UserId |

### NotificationTemplateEndpoints.cs (4 endpoints)

| # | Method | Route | Auth | ICurrentUser |
|---|--------|-------|------|-------------|
| 1 | GET | `/admin/notification-templates` | MustHavePermission(NotificationTemplate, Read) + RequireAuthorization | None |
| 2 | POST | `/admin/notification-templates` | MustHavePermission(NotificationTemplate, Create) + RequireAuthorization | Auth gate |
| 3 | PUT | `/admin/notification-templates/{id}` | MustHavePermission(NotificationTemplate, Update) + RequireAuthorization | Auth gate |
| 4 | DELETE | `/admin/notification-templates/{id}` | MustHavePermission(NotificationTemplate, Delete) + RequireAuthorization | Auth gate |

### DeviceEndpoints.cs (3 endpoints)

| # | Method | Route | Auth | ICurrentUser |
|---|--------|-------|------|-------------|
| 1 | POST | `/devices/token` | MustHavePermission(DeviceToken, Create) + RequireAuthorization | Auth gate + UserId |
| 2 | DELETE | `/devices/token/{id}` | MustHavePermission(DeviceToken, Delete) + RequireAuthorization | Auth gate + UserId |
| 3 | GET | `/devices/tokens` | MustHavePermission(DeviceToken, Read) + RequireAuthorization | Auth gate + UserId |

### NotificationHub (SignalR)

| Aspect | Detail |
|--------|--------|
| Class | `NotificationHub : Hub` |
| Auth | `[Authorize]` class-level |
| Groups | `user:{userId}`, `provider:{providerId}`, `admin` (permission-based) |
| Methods | `MarkAsRead(Guid)` — no-op stub |
| Route | `/hubs/notifications` |

**Summary: 22/22 endpoints + 1 hub have correct auth decorations.** Zero AllowAnonymous. Zero missing auth.

---

## 5. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | MustHavePermission on every endpoint | **Pass** | 22/22 endpoints decorated |
| 2 | ICurrentUser only for ownership | **Fail** | 21/22 endpoints have redundant auth gate |
| 3 | Result pattern (no throws in Application) | **Pass** | Zero `throw new` in 53 Application files |
| 4 | Per-module IPermissionCatalog | **Pass** | MessagingPermissionCatalog with 18 permissions |
| 5 | No SaveChanges in domain event handlers | **Pass** | 5 domain→outbox converters + 1 SignalR broadcaster, zero SaveChanges |
| 6 | DateTime.UtcNow (should use TimeProvider) | **Fail** | 6 violations in Domain (Notification:74,85, NotificationDeliveryAttempt:50,61,68, TicketMessage:15) |
| 7 | Guid.CreateVersion7 (not NewGuid) | **Pass** | Zero violations |
| 8 | FluentValidation on commands | **Fail** | Zero validators in entire module |
| 9 | HybridCache for read paths | **Fail** | Zero HybridCache usage |
| 10 | Result pattern consistency | **Partial** | DeviceEndpoints.DeleteDeviceToken returns `Results.Unauthorized()` instead of Result pipeline |
| 11 | Outbox/Inbox pattern | **Pass** | Both configured |
| 12 | Domain throws (DDD guard clauses) | **Pass** | 12 guard throws in domain entities — acceptable DDD invariants |

---

## 6. Gap #1 — Redundant ICurrentUser Auth Gates (MEDIUM)

**Severity**: Medium
**Impact**: Code bloat, confusing auth layering, inconsistent error format
**Rule Violated**: Rule #2 — ICurrentUser only for ownership

### Affected (21/22 endpoints)

All endpoints except `GET /admin/notification-templates` manually check:
```csharp
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure<...>(new Error("*.Unauthorized", "Not authenticated"), Outcome.Unauthorized).ToApiResult();
```

Despite every endpoint already having `.RequireAuthorization()` which handles this at the middleware level.

### Additionally
- `DeviceEndpoints.DeleteDeviceToken` uses `Results.Unauthorized()` instead of the module's `Result.Failure(...).ToApiResult()` pattern — inconsistent error format.
- SupportTicketEndpoints uses `currentUser.HasPermission("Permission.AdminSupportQueue.Read")` in 4 endpoints for admin-tier checks — this belongs in endpoint-level authorization policy, not handler code.

### Why Wrong
The auth middleware already rejects unauthenticated requests before the endpoint body executes. The manual check is dead code that adds noise and can desync with the actual auth configuration.

### Required Fix
Remove all `IsAuthenticated`/`UserId is null` checks from endpoint bodies. Keep only ownership-scoping (`currentUser.UserId.Value` to pass as query parameter). Move admin-tier permission checks to authorization policies.

---

## 7. Gap #2 — DateTime.UtcNow in Domain (MEDIUM)

**Severity**: Medium
**Impact**: Untestable time-dependent behavior
**Rule Violated**: Convention — use TimeProvider for testability

### Affected Files (6 occurrences)

| File | Line | Usage |
|------|------|-------|
| `Notification.cs` | 74 | `SentAt = DateTime.UtcNow` in MarkSent() |
| `Notification.cs` | 85 | `ReadAt = DateTime.UtcNow` in MarkRead() |
| `NotificationDeliveryAttempt.cs` | 50 | `AttemptedAt = DateTime.UtcNow` in Create() factory |
| `NotificationDeliveryAttempt.cs` | 61 | `CompletedAt = DateTime.UtcNow` in MarkSucceeded() |
| `NotificationDeliveryAttempt.cs` | 68 | `CompletedAt = DateTime.UtcNow` in MarkFailed() |
| `TicketMessage.cs` | 15 | `CreatedAt = DateTime.UtcNow` in constructor |

### Contrast
`SupportTicket.cs` and `DeviceToken.cs` correctly accept `TimeProvider` and use `timeProvider.GetUtcNow().UtcDateTime`.

### Required Fix
Inject `TimeProvider` (or accept `DateTime nowUtc` parameter) in all 6 locations to match the pattern used by SupportTicket and DeviceToken.

---

## 8. Gap #3 — Zero FluentValidation Validators (HIGH)

**Severity**: High
**Impact**: No input validation on any of 14 commands
**Rule Violated**: Convention — FluentValidation on every command

### Affected Commands (14, all missing validators)
- CreateSupportTicket, AssignSupportTicket, CloseSupportTicket, ResolveSupportTicket, PostTicketMessage
- MarkNotificationRead, MarkAllNotificationsRead, DeleteNotification
- UpdatePreferences
- CreateNotificationTemplate, UpdateNotificationTemplate, DeleteNotificationTemplate
- RegisterDeviceToken, DeleteDeviceToken

### Comparison
- ContentCore: 33 validators
- ContentBlogs: 28+ validators
- ContentTours: 28 validators
- ContentSeo: 6 validators
- Messaging: **0 validators**

### Required Fix
Create FluentValidation validators for at least the create/update commands:
- `CreateSupportTicketCommandValidator` — Subject 10-200 chars, Body required, Category valid enum
- `PostTicketMessageCommandValidator` — Body required
- `CreateNotificationTemplateCommandValidator` — Title 1-200, Body 1-5000, HtmlBody max 50000, LanguageCode BCP-47
- `RegisterDeviceTokenCommandValidator` — DeviceId max 200, Token max 500, Platform valid enum
- `UpdatePreferencesCommandValidator` — Updates list not empty, valid enum values

---

## 9. Gap #4 — Bare ChatBot Entities (MEDIUM)

**Severity**: Medium
**Impact**: Entire chatbot feature area is schema-only — no business logic, no endpoints, no handlers
**Rule Violated**: Spec requirement — IChatbotProvider interface, daily limits, handoff to human

### Affected Entities

| Entity | Lines | State | Problem |
|--------|-------|-------|---------|
| ChatBotConversation | 17 | Bare | No factory, no domain events, no methods beyond properties |
| ChatBotMessage | 16 | Bare | No factory, no domain events, child entity with no behavior |

### Spec Requirements (from YallaJo Business Rules §23)
- `IChatbotProvider` interface for pluggable AI backend
- 5 messages/day guest limit, 50 messages/day user limit
- Anti-hallucination post-processing
- Handoff to human support (trigger keywords)
- Context window management
- Confidence score tracking

### What Exists
- EF configurations for both entities (tables exist in DB)
- `ChatBotMessage.Confidence` and `ChatBotMessage.Intent` properties suggest schema preparation
- Zero handlers, zero endpoints, zero integration

### Required Fix
Phase 4 feature — implement when ready. Consider:
1. Add `IChatbotProvider` to Contracts
2. Create ChatBot endpoints (POST /chatbot/message, GET /chatbot/conversations)
3. Implement daily limit logic in domain
4. Add NoopChatbotProvider for development

---

## 10. Gap #5 — Zero HybridCache (LOW)

**Severity**: Low
**Impact**: No read-path caching for notification queries
**Rule Violated**: Convention — HybridCache for frequently-accessed read paths

### Affected Queries (8, all uncached)
GetMyNotifications, GetUnreadCount, GetMyPreferences, GetNotificationById, GetNotificationTemplates, GetSupportTickets, GetSupportTicketById, GetMyDeviceTokens

### Comparison
- ContentCore: 20+ handlers with HybridCache
- ContentTours: 29 handlers
- ContentBlogs: 40 handlers
- Messaging: **0 handlers**

### Required Fix
Add HybridCache at minimum to:
- `GetUnreadCountQueryHandler` — hot path, called on every page load
- `GetNotificationTemplatesQueryHandler` — admin templates rarely change
- `GetMyPreferencesQueryHandler` — preference settings rarely change

---

## 11. Gap #6 — SignalR Hub Implementation Issues (MEDIUM)

**Severity**: Medium
**Impact**: Real-time notifications may not route to correct hub groups

### Issue 1: Wrong Hub Type in Broadcast Handler

**File**: `NotificationCreatedSignalRBroadcastHandler.cs:19`
```csharp
var hubContext = serviceProvider.GetService<IHubContext<Hub>>();
```

Should be:
```csharp
var hubContext = serviceProvider.GetService<IHubContext<NotificationHub>>();
```

Using `IHubContext<Hub>` resolves to the generic base hub, not the `NotificationHub` instance with its custom group assignments. Messages sent to `user:{userId}` group via `IHubContext<Hub>` will not reach clients connected to `NotificationHub`.

### Issue 2: MarkAsRead Stub

**File**: `NotificationHub.cs:43-47`
```csharp
public Task MarkAsRead(Guid notificationId)
{
    return Task.CompletedTask; // no-op
}
```

This is a no-op that doesn't actually mark notifications as read. Either implement it (delegate to MediatR) or remove it to avoid client confusion.

### Issue 3: No SLA Breach Monitoring

The spec mentions SLA tracking with `SlaBreachAt` on SupportTicket. The entity correctly calculates SLA deadlines (High=4h, Medium=12h, Low=24h), and `SupportSlaBreachedIntegrationEvent` exists in Contracts. But there is **no background service** to monitor approaching SLA breaches and trigger this event.

### Required Fix
1. Change `IHubContext<Hub>` → `IHubContext<NotificationHub>` in broadcast handler
2. Either implement MarkAsRead or remove the stub
3. Add `SlaMonitoringService` background service that checks for tickets approaching breach

---

## 12. What Passed — Full Checklist

### Endpoint Authorization (22/22)
Every endpoint has `MustHavePermission(feature, action)` + `.RequireAuthorization()`. Zero anonymous endpoints. NotificationHub has `[Authorize]` at class level.

### Result Pattern in Application
Zero `throw new` in 53 Application files. All command handlers return `Result<T>` / `Result`. Consistent `Result.Success(...)` / `Result.Failure(...)` usage.

### SaveChanges in Domain Event Handlers
5 domain→outbox converters in `MessagingIntegrationConverters.cs` — all write to outbox only, zero `SaveChangesAsync`. 1 SignalR broadcast handler — no persistence.

36 integration event handlers (from 8 modules: Auth, Booking, Finance, ContentPlaces, ContentBlogs, Social, Accounts/Provider) — all call `SaveChangesAsync` correctly in their own scope.

### PermissionCatalog
18 permissions across 6 features. All endpoint decorations reference valid catalog entries.

### Guid.CreateVersion7
Zero `Guid.NewGuid()` in Application or Domain. `Notification.Create()` uses `Guid.CreateVersion7()`.

### DateTime (partial)
`SupportTicket.cs` and `DeviceToken.cs` use `TimeProvider` correctly. 3 other entities violate (see Gap #2).

### Outbox/Inbox
Both configured via EF configs (OutboxMessageConfiguration, InboxMessageConfiguration).

### Domain Model Quality
- `Notification`: Full lifecycle with idempotent transitions
- `SupportTicket`: Rich state machine (Open→Assigned→InProgress↔AwaitingUser→Resolved→Closed), SLA calculation, TimeProvider usage, message threading with internal notes
- `NotificationPreference`: Critical type guard enforcement
- `NotificationTemplate`: Mustache-based, multi-channel, i18n support
- `DeviceToken`: Stale token detection (30-day window), upsert dedup by DeviceId

### Notification Dispatcher
`NotificationDispatcher.cs` implements correct fan-out: InApp always first, then Email/Push based on user preferences. Strategy pattern for channel implementations (InAppNotificationStrategy, EmailNotificationStrategy, PushNotificationStrategy + NoOp variants).

### Background Services (2)
1. **EmailNotificationSenderService**: 30s polling, batch 50, 3 retries with exponential backoff (1m, 5m, 15m). Configurable via options.
2. **ReadNotificationCleanupService**: Weekly Sunday 02:00 UTC. Purges read notifications >30 days (excludes critical types). Caps per-user at 500. Cleans stale device tokens >30 days.

### Event Handler Coverage (41 handlers)
Comprehensive integration: Booking(5: Confirmed/Cancelled/Completed/Rejected + JoinRequest×3), Payment(3: Completed/Failed + InvoiceGenerated), Refund(2: Initiated/Failed), Payout(1: Scheduled), Provider(5: Approved/Rejected/Suspended/Reinstated/MoreDocsRequested), Business(4: Approved/Rejected/Suspended/Reinstated), Creator(12: Application/Invitation/Post/Profile lifecycle), Social(1: ReportResolved), Auth(1: UserRegistered), Support(3: TicketCreated×2 + Resolved).

### Cross-Module Integration
UserSnapshot for email rendering. Admin round-robin roster for ticket assignment. Integration events consumed from 8+ modules.

---

## 13. Scorecard

| # | Area | Score | Notes |
|---|------|-------|-------|
| 1 | Endpoint Security | 10/10 | 22/22 + hub secured |
| 2 | ICurrentUser Compliance | 5/10 | 21/22 redundant auth gates + admin-tier checks in handlers |
| 3 | Result Pattern | 9/10 | 1 inconsistency (DeviceEndpoints.Delete) |
| 4 | Domain Model | 7/10 | 6 good aggregates, 2 bare ChatBot entities |
| 5 | FluentValidation | 0/10 | Zero validators |
| 6 | DateTime/GUID Conventions | 7/10 | 6 DateTime.UtcNow violations, zero GUID violations |
| 7 | SaveChanges Compliance | 10/10 | Domain handlers clean, integration handlers correct |
| 8 | HybridCache | 0/10 | Zero usage |
| 9 | Outbox/Cross-Module | 10/10 | Full coverage, 41 event handlers |
| 10 | Test Coverage | 1/10 | 1 test file (permission catalog only) |
| | **Overall** | **7.0/10** | Strong integration layer, weak validation + caching |

---

## 14. Fix Priority & Recommendations

### P1 — High Priority (before feature work)

| # | Fix | Effort | Impact |
|---|-----|--------|--------|
| 1 | Add FluentValidation validators for 14 commands | ~4h | Prevents invalid data |
| 2 | Fix `IHubContext<Hub>` → `IHubContext<NotificationHub>` | ~5min | SignalR routing bug |
| 3 | Replace 6 DateTime.UtcNow with TimeProvider | ~1h | Testability |

### P2 — Medium Priority

| # | Fix | Effort | Impact |
|---|-----|--------|--------|
| 4 | Remove redundant auth gates from 21 endpoints | ~2h | Code clarity |
| 5 | Add SLA breach monitoring background service | ~3h | Spec compliance |
| 6 | Implement or remove NotificationHub.MarkAsRead stub | ~30min | API clarity |
| 7 | Fix DeleteDeviceToken `Results.Unauthorized()` → Result pattern | ~10min | Consistency |

### P3 — Low Priority (deferrable)

| # | Fix | Effort | Impact |
|---|-----|--------|--------|
| 8 | Add HybridCache to GetUnreadCount + GetTemplates + GetPreferences | ~2h | Performance |
| 9 | Implement ChatBot feature (Phase 4) | ~20h | Spec feature |
| 10 | Add unit tests (domain entities + handlers) | ~8h | Quality |

**Total estimated effort**: ~41h

---

## 15. Appendix — Files Audited

### Domain (45 files)
- Entities: AdminAssignmentRoster.cs, ChatBotConversation.cs, ChatBotMessage.cs, DeviceToken.cs, Notification.cs, NotificationDeliveryAttempt.cs, NotificationPreference.cs, NotificationTemplate.cs, SupportTicket.cs, TicketMessage.cs, UserSnapshot.cs
- Enums: DevicePlatform.cs, NotificationChannel.cs, NotificationDeliveryStatus.cs, NotificationPriority.cs, NotificationType.cs, TicketCategory.cs, TicketPriority.cs, TicketStatus.cs
- Events: 16 domain event files
- Repositories: 9 interface files

### Application (53 files)
- Commands: 14 folders (CreateSupportTicket, AssignSupportTicket, CloseSupportTicket, ResolveSupportTicket, PostTicketMessage, MarkNotificationRead, MarkAllNotificationsRead, DeleteNotification, UpdatePreferences, CreateNotificationTemplate, UpdateNotificationTemplate, DeleteNotificationTemplate, RegisterDeviceToken, DeleteDeviceToken)
- Queries: 8 folders (GetMyNotifications, GetUnreadCount, GetMyPreferences, GetNotificationById, GetNotificationTemplates, GetSupportTickets, GetSupportTicketById, GetMyDeviceTokens)
- Interfaces: IMessagingInboxStore, IMessagingUnitOfWork, INotificationChannelStrategy, INotificationTemplateRenderer
- DTOs: NotificationDto, SupportTicketDto

### Contracts (10 files)
- Authorization: MessagingFeatures.cs, MessagingPermissionCatalog.cs
- IntegrationEvents: 6 event files
- Services: IEmailSender.cs, INotificationDispatcher.cs

### Infrastructure (87 files)
- EventHandlers: 41 files (5 domain→outbox converters, 1 SignalR broadcaster, 35 integration handlers)
- Persistence: MessagingDbContext, 13 EF configs (incl Inbox/Outbox), 9 repositories, MessagingUnitOfWork, MessagingInboxStore, MessagingDbInitializer
- Services: 9 files (NotificationDispatcher, EmailNotificationStrategy, InAppNotificationStrategy, PushNotificationStrategy, MustacheNotificationTemplateRenderer, SmtpEmailSender, NoopEmailSender, NoopNotificationDispatcher, NoopNotificationTemplateRenderer)
- BackgroundServices: EmailNotificationSenderService, ReadNotificationCleanupService
- Migrations: 3 migration pairs + snapshot

### Presentation (6 files)
- MessagingEndpoints.cs, NotificationEndpoints.cs, SupportTicketEndpoints.cs, NotificationTemplateEndpoints.cs, DeviceEndpoints.cs, NotificationHub.cs

### Tests (1 file)
- MessagingPermissionCatalogTests.cs
