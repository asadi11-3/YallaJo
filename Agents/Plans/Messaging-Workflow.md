# Messaging Module — Workflow Plan

> **Created**: 2025-07-14
> **Status**: Partially Implemented (audited 2025-01-27, W2-C fixes applied)
> **Compatible with**: TourGuide-Flow.md, Booking-Workflow.md, Finance-Workflow.md, Social-Workflow.md, Platform-Onboarding-Workflow.md, Role-System.md, BlogCreatorPost-Merger.md, ContentPlaces-Workflow.md
> **Source of truth**: agent-context.md, YallaJo Business Rules PDF, YallaJo.md, Endpoints.pdf

---

## Design Decisions (16 items — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | ChatBot deferred | Keep shell entities (ChatBotConversation, ChatBotMessage). No endpoints. Post-MVP. |
| 2 | SLA monitoring included | Background service: 75% warn admin, 100% fire SlaBreachedIntegrationEvent + escalate to supervisor + auto-raise priority |
| 3 | TourGuide notification types | New range 40-49: GuideApplicationApproved=40, GuideApplicationRejected=41, GuideProposalApproved=42, GuideProposalRejected=43, GuideBookingAssigned=44, GuideEarningsReady=45, GuideTierPromoted=46, GuideSlotBooked=47, AgencyInvitationReceived=48, AgencyAffiliationApproved=49 |
| 4 | SMS deferred | Enum stays. NoopSmsStrategy. Implement when gateway chosen (Twilio/Vonage). |
| 5 | All 8 queries cached | Full HybridCache with tag-based invalidation (consistent with ContentTours=29, ContentBlogs=40 handlers) |
| 6 | Hardcoded event handlers | No declarative rules. Each integration event has its own handler. Admin controls via per-user NotificationPreference (enable/disable per type). |
| 7 | Batch operations + filtering | DELETE /notifications/batch, GET /notifications supports type, priority, dateRange, isRead filters |
| 8 | Same endpoint, role-aware tickets | Same CreateTicket endpoint. System auto-tags with user's role context. Category expansion for provider/guide needs. |
| 9 | SLA escalation: 75% warn + 100% breach | At 75% elapsed: InApp notification to assigned admin. At 100%: SlaBreachedIntegrationEvent, notify supervisor (next-tier admin), auto-raise priority one level. |
| 10 | Email: SMTP transactional + Cloud bulk | Two IEmailSender impls routed by notification category. Critical/Security/OTP → SMTP (direct). Digest/Marketing/Bulk → IMarketingEmailSender (SendGrid/Resend, deploy-time choice). |
| 11 | Push: Gateway-agnostic IPushProvider | Interface only. FcmPushProvider as first impl when ready. NoopPushProvider for dev. |
| 12 | Notification digest per category | Users configure per-category: instant / daily-digest / weekly-digest. DigestBatchingService groups and sends at 07:00 UTC daily, 07:00 Monday weekly. |
| 13 | 90-day financial retention | Payment/Payout/Refund/Earnings notifications: 90 days. Other read notifications: 30 days. Critical: never purged. |
| 14 | Ticket categories expanded | Add 5: PayoutIssue=6, CommissionDispute=7, GuideScheduleIssue=8, TourApprovalHelp=9, DocumentVerification=10 |
| 15 | Auth gate cleanup (Gap #1) | Remove redundant IsAuthenticated/UserId-is-null checks from 21 endpoint bodies. Keep ownership scoping. |
| 16 | DateTime.UtcNow fix (Gap #2) | Replace 6 occurrences with TimeProvider in Notification, NotificationDeliveryAttempt, TicketMessage |

---

## Current State Summary

### What's Built (working)
- **Notification lifecycle**: Create → MarkSent → MarkRead / MarkFailed (idempotent)
- **Multi-channel dispatch**: InApp always, Email/Push per preference, strategy pattern
- **Template engine**: Mustache-based, multi-channel (Subject, Body, HtmlBody), i18n by LanguageCode
- **Support tickets**: Full state machine (Open→Assigned→InProgress↔AwaitingUser→Resolved→Closed)
- **SLA calculation**: On entity (High=4h, Medium=12h, Low=24h) with `SlaBreachAt` property
- **Round-robin assignment**: AdminAssignmentRoster for auto-assign
- **Device tokens**: FCM/APNs/Web, stale detection (30-day), upsert dedup
- **Preference system**: Per-type per-channel on/off, critical types non-disableable
- **41 integration event handlers**: Consuming from Auth, Booking, Finance, ContentBlogs, ContentPlaces, Social, Accounts/Provider
- **SignalR hub**: `/hubs/notifications` with user/provider/admin groups
- **Background services**: EmailNotificationSenderService (30s poll), ReadNotificationCleanupService (weekly)
- **22 endpoints**: All correctly secured

### What's Shell/Missing
- ChatBot: entities only, no behavior (DEFERRED)
- No FluentValidation (0 validators for 14 commands)
- No HybridCache (0 cached queries)
- No SLA monitoring service
- No digest/batching
- No batch notification operations
- No TourGuide-specific notification types
- SignalR bug: `IHubContext<Hub>` instead of `IHubContext<NotificationHub>`
- 6 DateTime.UtcNow violations in domain
- 21 auth gate violations in endpoints

---

## Workflow A — Notification Dispatch Pipeline

```
Integration Event (from any module)
  → Messaging Event Handler (41 handlers)
    → Resolve NotificationTemplate (by type + languageCode)
    → Render template (Mustache: title, body, htmlBody)
    → Check user DigestPreference for this category
      → IF instant: NotificationDispatcher.DispatchAsync()
      → IF daily/weekly: Queue to DigestQueue table
    → NotificationDispatcher fan-out:
      1. InApp: Create Notification entity → save → SignalR broadcast
      2. Email (if preferred): Queue EmailDeliveryAttempt → EmailSenderService picks up
      3. Push (if preferred + active device): Queue PushDeliveryAttempt → PushSenderService picks up
    → Track delivery attempt per channel
```

### Digest Sub-Flow
```
DigestBatchingService (daily 07:00 UTC / weekly Monday 07:00 UTC)
  → Query DigestQueue WHERE scheduledFor <= now
  → Group by (UserId, Category)
  → Render digest template (list of notifications)
  → Send single email/push per group
  → Mark queued items as dispatched
  → Create single InApp "summary" notification
```

---

## Workflow B — Support Ticket Lifecycle

```
User/Provider/Guide creates ticket
  → Validate (FluentValidation): subject 10-200, body required, valid category
  → System auto-tags with user role (Tourist, Provider, TourGuide, Creator)
  → DerivePriority(category) → set SlaBreachAt
  → Round-robin auto-assign to admin from AdminAssignmentRoster
  → Fire SupportTicketOpenedDomainEvent → outbox → notification to admin
  → Fire TicketCreatedDomainEvent → outbox → confirmation to user

Admin works ticket:
  → AddMessage (internal notes + external replies)
  → Status transitions: Assigned→InProgress, InProgress↔AwaitingUser
  → Resolve(notes) → status=Resolved, fire SupportTicketResolvedDomainEvent
  → User can Close (or auto-close after 7 days of Resolved)

SLA Monitoring (background, 5-min interval):
  → Query Open/Assigned/InProgress tickets
  → IF elapsed >= 75% of SLA: notify assigned admin (InApp warning)
  → IF elapsed >= 100% of SLA:
    → Fire SlaBreachedIntegrationEvent
    → Notify supervisor (next-tier admin on roster)
    → Auto-raise priority one level (Low→Medium, Medium→High)
    → Update SlaBreachAt for new priority
```

---

## Workflow C — Real-Time (SignalR)

```
Notification created → NotificationCreatedDomainEvent
  → NotificationCreatedSignalRBroadcastHandler
    → Resolve IHubContext<NotificationHub>  [FIX: currently uses IHubContext<Hub>]
    → Send to group "user:{userId}"
    → Include: { id, type, title, body, priority, createdAt }

Client connects to /hubs/notifications:
  → JWT auth → extract userId from claims
  → Join group "user:{userId}"
  → IF provider role: join "provider:{providerId}"
  → IF admin role + has AdminSupportQueue.Read: join "admin"

Hub methods:
  → MarkAsRead(notificationId) → dispatch MarkNotificationReadCommand via MediatR
  → (future: typing indicators for ticket chat)
```

---

## Workflow D — Device Token Management

```
Mobile/Web client registers:
  → POST /devices/token { deviceId, token, platform }
  → Upsert: same deviceId → update token (avoid duplicates)
  → Store platform (iOS/Android/Web) for routing

Push delivery:
  → IPushProvider.SendAsync(token, payload)
  → On failure: mark delivery attempt failed
  → On "token-invalid" response: revoke DeviceToken (stale)

Cleanup:
  → ReadNotificationCleanupService: revoke tokens with LastActiveAt > 30 days
```

---

## New NotificationType Values (range 40-49)

```csharp
// ── TourGuide (40-49) ────────────────────────────────────────────────────
GuideApplicationApproved    = 40,
GuideApplicationRejected    = 41,
GuideProposalApproved       = 42,
GuideProposalRejected       = 43,
GuideBookingAssigned        = 44,  // New booking assigned to guide
GuideEarningsReady          = 45,  // Payout processed/ready
GuideTierPromoted           = 46,
GuideSlotBooked             = 47,  // Availability slot booked by tourist
AgencyInvitationReceived    = 48,  // Agency invites guide
AgencyAffiliationApproved   = 49,  // Guide accepted into agency
```

**Critical additions** (non-disableable): None in this range. All guide notifications are informational.

---

## New TicketCategory Values

```csharp
// Add after Other=5
PayoutIssue          = 6,   // Provider/Guide payout problems
CommissionDispute    = 7,   // Provider disputes commission rate
GuideScheduleIssue  = 8,   // Guide availability/slot conflicts
TourApprovalHelp    = 9,   // Provider needs help with tour approval
DocumentVerification = 10,  // Document upload/verification issues
```

---

## Entity Changes

### NotificationPreference — Add Digest Configuration
```csharp
// New property
public DigestFrequency DigestFrequency { get; private set; } = DigestFrequency.Instant;
```

### New Enum: DigestFrequency
```csharp
public enum DigestFrequency : byte
{
    Instant = 0,   // Send immediately (default)
    Daily   = 1,   // Batch into daily digest (07:00 UTC)
    Weekly  = 2,   // Batch into weekly digest (Monday 07:00 UTC)
}
```

### DigestQueueItem — UNMAPPED (use flags on existing Notification entity)

> **UNMAPPED DECISION:** Instead of a separate entity, add digest columns to existing `Notification`:

```csharp
// ADD to existing Notification entity:
public DigestFrequency? DigestFrequency { get; private set; }  // null = instant
public DateTime? DigestScheduledFor { get; private set; }      // When to batch-dispatch
public bool IsDigestDispatched { get; private set; }           // Has digest been sent?
public DateTime? DigestDispatchedAt { get; private set; }
public Guid? DigestBatchId { get; private set; }               // Groups notifications in same digest email
```

> DigestBatchingService queries: `WHERE DigestFrequency IS NOT NULL AND IsDigestDispatched=false AND DigestScheduledFor <= @now`
> This avoids a separate table + separate EF config + separate repository — same queryability, fewer files.

### Notification Entity — Extended with digest columns (above)
Retention logic handled in cleanup service configuration.

### SupportTicket — Add role context
```csharp
// New property (set at creation)
public string? CreatorRole { get; private set; }  // "Tourist", "Provider", "TourGuide", "Creator"
```

---

## New Integration Event Handlers Needed (for TourGuide-Flow)

| # | Event Source | Handler Name | NotificationType |
|---|-------------|--------------|-----------------|
| 1 | TourGuide-Flow | GuideApplicationApprovedHandler | GuideApplicationApproved(40) |
| 2 | TourGuide-Flow | GuideApplicationRejectedHandler | GuideApplicationRejected(41) |
| 3 | TourGuide-Flow | GuideProposalApprovedHandler | GuideProposalApproved(42) |
| 4 | TourGuide-Flow | GuideProposalRejectedHandler | GuideProposalRejected(43) |
| 5 | Booking-Workflow | GuideBookingAssignedHandler | GuideBookingAssigned(44) |
| 6 | Finance-Workflow | GuideEarningsReadyHandler | GuideEarningsReady(45) |
| 7 | TourGuide-Flow | GuideTierPromotedHandler | GuideTierPromoted(46) |
| 8 | Booking-Workflow | GuideSlotBookedHandler | GuideSlotBooked(47) |
| 9 | Platform-Onboarding | AgencyInvitationReceivedHandler | AgencyInvitationReceived(48) |
| 10 | Platform-Onboarding | AgencyAffiliationApprovedHandler | AgencyAffiliationApproved(49) |

---

## New/Modified Endpoints

### Notification Endpoints (additions)

| # | Method | Route | Auth | Purpose |
|---|--------|-------|------|---------|
| 1 | GET | `/notifications` | MustHavePermission | **MODIFY**: Add query params: `type`, `priority`, `isRead`, `fromDate`, `toDate` |
| 2 | DELETE | `/notifications/batch` | MustHavePermission(Notification, Delete) | Batch delete by IDs (max 100) |
| 3 | POST | `/notifications/batch-read` | MustHavePermission(Notification, Update) | Batch mark-as-read by IDs (max 100) |
| 4 | GET | `/notifications/preferences/digest` | MustHavePermission(NotificationPreference, Read) | Get digest settings per category |
| 5 | PUT | `/notifications/preferences/digest` | MustHavePermission(NotificationPreference, Update) | Update digest frequency per category |

### Support Ticket Endpoints (additions)

| # | Method | Route | Auth | Purpose |
|---|--------|-------|------|---------|
| 6 | GET | `/support/admin/tickets` | MustHavePermission(AdminSupportQueue, Read) | Admin queue with SLA status, priority, category filters |
| 7 | POST | `/support/admin/tickets/{id}/escalate` | MustHavePermission(AdminSupportQueue, Assign) | Manual escalation to supervisor |
| 8 | PUT | `/support/admin/tickets/{id}/priority` | MustHavePermission(AdminSupportQueue, Assign) | Manually change priority |

### Existing Endpoints Modified
- All 21 endpoints: Remove auth gate checks (Gap #1 fix)
- `GET /notifications`: Add filter query parameters
- `DeviceEndpoints.DeleteDeviceToken`: Fix `Results.Unauthorized()` → Result pattern

---

## New Validators (14 commands — ALL missing)

| # | Validator | Key Rules |
|---|-----------|-----------|
| 1 | CreateSupportTicketCommandValidator | Subject 10-200 chars, Body 1-5000, Category valid enum |
| 2 | PostTicketMessageCommandValidator | Body 1-5000, TicketId required |
| 3 | AssignSupportTicketCommandValidator | TicketId + AdminUserId required |
| 4 | CloseSupportTicketCommandValidator | TicketId required |
| 5 | ResolveSupportTicketCommandValidator | TicketId required, ResolutionNotes max 2000 |
| 6 | MarkNotificationReadCommandValidator | NotificationId required |
| 7 | MarkAllNotificationsReadCommandValidator | (no-op validator or skip) |
| 8 | DeleteNotificationCommandValidator | NotificationId required |
| 9 | UpdatePreferencesCommandValidator | Updates list non-empty, valid NotificationType + Channel |
| 10 | CreateNotificationTemplateCommandValidator | Title 1-200, Body 1-5000, HtmlBody max 50000, LanguageCode BCP-47 pattern |
| 11 | UpdateNotificationTemplateCommandValidator | Same as create |
| 12 | DeleteNotificationTemplateCommandValidator | TemplateId required |
| 13 | RegisterDeviceTokenCommandValidator | DeviceId max 200, Token max 500, Platform valid enum |
| 14 | DeleteDeviceTokenCommandValidator | TokenId required |
| 15 | BatchDeleteNotificationsCommandValidator | Ids non-empty, max 100 |
| 16 | BatchMarkReadCommandValidator | Ids non-empty, max 100 |
| 17 | UpdateDigestPreferencesCommandValidator | Valid DigestFrequency per category |

---

## Background Services (new + modified)

### New: SlaMonitoringService
- **Interval**: 5 minutes
- **Logic**:
  1. Query tickets WHERE Status IN (Open, Assigned, InProgress) AND SlaBreachAt IS NOT NULL
  2. Calculate elapsed percentage: `(now - CreatedAt) / (SlaBreachAt - CreatedAt) * 100`
  3. IF >= 75% AND not already warned: Create InApp notification to assigned admin, mark warned
  4. IF >= 100% AND not already breached:
     - Fire `SlaBreachedIntegrationEvent(ticketId, category, originalPriority)`
     - Notify supervisor (query next admin on roster after current assignee)
     - Auto-raise priority: Low→Medium, Medium→High (High stays High)
     - Recalculate SlaBreachAt for new priority
     - Mark breached

### New: DigestBatchingService
- **Interval**: Every hour (checks if 07:00 UTC for daily, Monday for weekly)
- **Logic**:
  1. Query Notification WHERE DigestFrequency IS NOT NULL AND IsDigestDispatched=false AND DigestScheduledFor <= now
  2. Group by (UserId, Channel, Frequency)
  3. For each group: render digest template (list of {title, body, timestamp})
  4. Dispatch via appropriate channel (Email for email digest, InApp summary notification)
  5. Mark items as dispatched

### New: PushNotificationSenderService
- **Interval**: 30 seconds (mirrors EmailNotificationSenderService)
- **Logic**: Poll pending push delivery attempts, batch 50, send via IPushProvider, track success/failure

### Modified: ReadNotificationCleanupService
- Change retention: Financial types (16-22, 45) → 90 days
- Other read notifications → 30 days
- Critical types → never purge (existing behavior)

### Modified: EmailNotificationSenderService
- Route by notification category:
  - Critical/Security/OTP types → ITransactionalEmailSender (SMTP direct)
  - Others → IMarketingEmailSender (cloud provider, with retry/rate-limit awareness)

---

## Interface Changes

### New: ITransactionalEmailSender (replaces direct IEmailSender for critical)
```csharp
public interface ITransactionalEmailSender
{
    Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken ct);
}
```

### New: IMarketingEmailSender (for digest/bulk)
```csharp
public interface IMarketingEmailSender
{
    Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken ct);
    Task<int> SendBatchAsync(IReadOnlyList<EmailMessage> messages, CancellationToken ct);
}
```

### New: IPushProvider
```csharp
public interface IPushProvider
{
    string ProviderName { get; }
    Task<PushResult> SendAsync(string deviceToken, DevicePlatform platform, PushPayload payload, CancellationToken ct);
}
public record PushPayload(string Title, string Body, string? ImageUrl, Dictionary<string, string>? Data);
public record PushResult(bool Success, string? ErrorCode, bool ShouldRevokeToken);
```

### Existing IEmailSender → becomes ITransactionalEmailSender
- SmtpEmailSender implements ITransactionalEmailSender
- New NoopMarketingEmailSender implements IMarketingEmailSender (dev)

---

## HybridCache Strategy

| Query | Cache Key | TTL | Tags |
|-------|-----------|-----|------|
| GetMyNotifications | `msg:notif:user:{userId}:p{page}` | 1 min | `msg:notif:user:{userId}` |
| GetUnreadCount | `msg:unread:{userId}` | 30 sec | `msg:notif:user:{userId}` |
| GetMyPreferences | `msg:pref:{userId}` | 10 min | `msg:pref:{userId}` |
| GetNotificationById | `msg:notif:{id}` | 5 min | `msg:notif:user:{userId}` |
| GetNotificationTemplates | `msg:templates:list` | 30 min | `msg:templates` |
| GetSupportTickets | `msg:tickets:user:{userId}:p{page}` | 2 min | `msg:tickets:user:{userId}` |
| GetSupportTicketById | `msg:ticket:{id}` | 2 min | `msg:ticket:{id}` |
| GetMyDeviceTokens | `msg:devices:{userId}` | 10 min | `msg:devices:{userId}` |

**Invalidation triggers:**
- Notification created/read/deleted → evict `msg:notif:user:{userId}`, `msg:unread:{userId}`
- Preference updated → evict `msg:pref:{userId}`
- Template CRUD → evict `msg:templates`
- Ticket created/updated/message posted → evict `msg:tickets:user:{userId}`, `msg:ticket:{id}`
- Device token registered/revoked → evict `msg:devices:{userId}`

---

## Cross-Module Integration Map

| Source Module | Event | Messaging Action |
|---------------|-------|-----------------|
| Booking | TourBookingCreated | Notify provider (ProviderBookingReceived) |
| Booking | TourBookingConfirmed | Notify tourist (BookingConfirmed) |
| Booking | TourBookingCancelled | Notify both parties (BookingCancelled) |
| Booking | TourBookingCompleted | Notify tourist (BookingCompleted) |
| Booking | TourBookingRejected | Notify tourist (BookingCancelled variant) |
| Booking | GuideSlotBooked | Notify guide (GuideSlotBooked=47) — **NEW** |
| Booking | GuideBookingAssigned | Notify guide (GuideBookingAssigned=44) — **NEW** |
| Finance | PaymentCompleted | Notify user (PaymentCompleted) |
| Finance | PaymentFailed | Notify user (PaymentFailed) |
| Finance | RefundInitiated | Notify user (RefundInitiated) |
| Finance | PayoutScheduled | Notify provider (PayoutScheduled) |
| Finance | GuidePayoutReady | Notify guide (GuideEarningsReady=45) — **NEW** |
| Social | ReportResolved | Notify reporter (ReportResolved) |
| Accounts | ProviderApproved | Notify provider (ProviderApproved) |
| Accounts | ProviderRejected | Notify provider (ProviderRejected) |
| TourGuide-Flow | GuideApplicationApproved | Notify guide (40) — **NEW** |
| TourGuide-Flow | GuideApplicationRejected | Notify guide (41) — **NEW** |
| TourGuide-Flow | GuideProposalApproved | Notify guide (42) — **NEW** |
| TourGuide-Flow | GuideProposalRejected | Notify guide (43) — **NEW** |
| TourGuide-Flow | GuideTierPromoted | Notify guide (46) — **NEW** |
| Platform-Onboarding | AgencyInvitationSent | Notify guide (48) — **NEW** |
| Platform-Onboarding | AgencyAffiliationApproved | Notify guide (49) — **NEW** |

---

## Execution Phases

### Phase 1 — Bug Fixes & Compliance (~2h)
- Fix `IHubContext<Hub>` → `IHubContext<NotificationHub>` in SignalR broadcast handler
- Replace 6 `DateTime.UtcNow` with TimeProvider in Notification, NotificationDeliveryAttempt, TicketMessage
- Fix `DeviceEndpoints.DeleteDeviceToken` `Results.Unauthorized()` → Result pattern
- Remove auth gates from 21 endpoints (Gap #1)

### Phase 2 — FluentValidation (~3h)
- Create 17 validators for all commands
- Register in DI

### Phase 3 — HybridCache (~3h)
- Add HybridCache to all 8 query handlers
- Add cache key constants (MessagingCacheKeys.cs)
- Add cache eviction to all command handlers that modify data

### Phase 4 — Notification Enhancements (~4h)
- Add NotificationType values 40-49
- Add TicketCategory values 6-10
- Add DigestFrequency enum
- Add digest columns to Notification entity + update EF config
- Modify NotificationPreference to include DigestFrequency
- Add batch endpoints (DELETE /batch, POST /batch-read)
- Add filter params to GET /notifications
- Add digest preference endpoints

### Phase 5 — Digest System (~5h)
- Modify NotificationDispatcher: check user digest preference before dispatch
- If non-instant: set DigestFrequency + DigestScheduledFor on Notification instead of immediate dispatch
- Create DigestBatchingService background service
- Create digest email template

### Phase 6 — Email/Push Provider Abstraction (~3h)
- Split IEmailSender → ITransactionalEmailSender + IMarketingEmailSender
- Create IPushProvider interface
- Create PushNotificationSenderService (mirrors EmailNotificationSenderService)
- Create NoopPushProvider, NoopMarketingEmailSender
- Route EmailNotificationSenderService based on notification category

### Phase 7 — SLA Monitoring (~3h)
- Create SlaMonitoringService background service (5-min interval)
- Add `SlaWarned` + `SlaBreached` bool flags to SupportTicket (or tracking table)
- Add SupportTicket.CreatorRole property
- Add admin ticket endpoints (admin queue, escalate, change priority)
- Wire SlaBreachedIntegrationEvent

### Phase 8 — TourGuide Integration Event Handlers (~4h)
- Create 10 new event handlers for TourGuide/Booking/Finance/Platform events
- Register integration event types in MessagingInboxStore
- Create notification templates for each new type

### Phase 9 — Retention & Cleanup Modification (~1h)
- Modify ReadNotificationCleanupService: 90-day for financial (types 16-22, 45), 30-day others
- Ensure critical types excluded from all purge

### Phase 10 — Solution Build + Verify (~1h)
- Build entire solution
- Fix any test compilation errors
- Verify 0 errors

---

## File Estimates

| Category | New Files | Modified Files |
|----------|-----------|---------------|
| Domain (enums, entities, events) | 3 | 5 |
| Application (commands, queries, validators) | 22 | 12 |
| Infrastructure (handlers, services, configs) | 16 | 8 |
| Presentation (endpoints) | 0 | 4 |
| Contracts (events, interfaces) | 5 | 2 |
| **Total** | **~46** | **~31** |

---

## Compatibility Notes

### With TourGuide-Flow.md
- TourGuide dashboard notifications: all 10 new event handlers consume TourGuide integration events
- Guide earnings notification (type 45) triggered by Finance.GuidePayoutReady event

### With Booking-Workflow.md
- Existing 5 booking handlers sufficient for tourist side
- NEW: GuideSlotBooked (47) + GuideBookingAssigned (44) for guide side
- BookingReminderUpcoming (13): existing, triggered by Booking background service

### With Finance-Workflow.md
- Payment/Payout notifications already handled (types 16-22)
- NEW: GuideEarningsReady (45) for guide payout notifications
- 90-day retention ensures guides can see financial notification history

### With Social-Workflow.md
- ReportResolved (26) handler already exists
- UserWarned/UserBanned events → Security module handles role enforcement, Messaging sends notification

### With Platform-Onboarding-Workflow.md
- Provider approval/rejection handlers already exist (types 34-37)
- NEW: AgencyInvitationReceived (48) + AgencyAffiliationApproved (49)

### With Role-System.md
- Ticket auto-tag with user role (Tourist/Provider/TourGuide/Creator) for admin routing
- No role-based notification filtering (all roles receive same notification infrastructure)

### With BlogCreatorPost-Merger.md
- Creator notification handlers (12 existing) reference CreatorPost → will need update after merger
- Post-merger: handlers fire for Blog entity where AuthoredByCreatorId IS NOT NULL

---

## Implementation Notes (Audited 2025-01-27)

1. **Validators**: 15 command validators created (W1-A) — all commands now have FluentValidation coverage
2. **HybridCache**: QueryCachingBehavior globally registered in SharedKernel — all ICacheableQuery implementations automatically cached. Cache invalidation added to 12 command handlers (W1-B)
3. **ISmsSender**: Interface in Messaging.Contracts.Services, NoOpSmsSender in Infrastructure — logs warning, returns success. Replace with Twilio/Vonage at deployment time
4. **SlaMonitoringService**: BackgroundService in Infrastructure, 5-min interval, queries GetOverdueSlaTicketsAsync, publishes SupportSlaBreachedIntegrationEvent via outbox. Enabled by default
5. **NotificationDigestService**: BackgroundService stub registered but DISABLED by default (Enabled=false). Requires Notification entity schema changes (DigestFrequency, IsDigestDispatched, DigestScheduledFor, DigestBatchId columns) before activation
6. **TourGuide notification types**: GuideOfferingSuspended=50, GuideApplicationSubmitted=51 added (types 40-49 already existed)
7. **Auth gates**: All 23 REST endpoints + 1 SignalR hub correctly configured — no changes needed
8. **CQRS**: All command/query handlers use MediatR ISender — no direct repo usage in Presentation layer
9. **Background services**: 4 total — EmailNotificationSenderService, ReadNotificationCleanupService, SlaMonitoringService (NEW), NotificationDigestService (NEW, disabled)
10. **Actual counts**: 11 entities, 23 REST endpoints + 1 SignalR hub, 53 integration event handlers, 23 CQRS handlers, 18 permissions across 6 features
