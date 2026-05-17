# Messaging — Entities Matrix

> Authoritative inventory of every Messaging entity, its base class, aggregate role, persistence config, and what events it emits. Mirrors Booking 03-entities-matrix.md format.

---

## 1. Aggregates (5 total, all `AuditableEntity, IAggregateRoot` after PW-2)

| Aggregate | LOC (current) | Owns | Raises | Custom invariants |
|---|---|---|---|---|
| **`Notification`** | 71 | – | NotificationCreated, NotificationRead, NotificationDelivered, NotificationFailed | UserId required; Title 1-200; Body 1-2000; CreatedAt frozen; MarkRead idempotent |
| **`NotificationPreference`** | tiny stub | – | NotificationPreferenceUpdated | UNIQUE (UserId, Type, Channel); CannotDisableCritical (M-R1) |
| **`NotificationTemplate`** | tiny stub | – | NotificationTemplateCreated, …Updated, …Deleted | UNIQUE (Key, LanguageCode); Key non-empty; Body/Title required; placeholders validated against allow-list |
| **`DeviceToken`** | tiny stub | – | DeviceTokenRegistered, DeviceTokenDeleted | UNIQUE filtered (UserId, DeviceId) WHERE IsDeleted = 0; Token non-empty; LastSeenAt auto-stamped |
| **`SupportTicket`** | 23 stub | `TicketMessages` collection (BaseEntity) | TicketCreated, TicketAssigned, TicketResolved, TicketClosed, TicketMessageAdded | Status state machine (Open → Assigned → InProgress → Resolved → Closed); Category drives Priority; CreatedAt frozen |

---

## 2. Child / value entities (NOT aggregates)

| Entity | Base | Parent aggregate | Notes |
|---|---|---|---|
| `TicketMessage` | BaseEntity | SupportTicket | Author (User OR Admin), IsInternal (admin-only notes), AttachedFiles (later) |
| `NotificationDeliveryAttempt` | BaseEntity | Notification (loose ref) | NEW table from PW-9 — audit trail for EmailSender retries |
| `AdminAssignmentRosterEntry` | BaseEntity | (none — top-level for round-robin) | PerSupportTicket assignment tracking |

---

## 3. OUT-OF-SCOPE entities (stay stub; do NOT promote)

| Entity | Phase |
|---|---|
| `ChatBotConversation` | Phase 4 (T22 AI Assistant) |
| `ChatBotMessage` | Phase 4 |

These stay as-is (BaseEntity stubs). Do NOT add IAggregateRoot. Phase 4 sprint will re-design.

---

## 4. Integration events emitted (6 total, all `messaging.*.v1`)

| Logical name | Trigger | Consumer | Required payload |
|---|---|---|---|
| `messaging.notification.delivered.v1` | EmailSender marks Success | Analytics | NotificationId, UserId, Channel, DeliveredAt |
| `messaging.notification.failed.v1` | EmailSender exhausts retries | Analytics + Admin Slack | NotificationId, UserId, Channel, FailureReason, AttemptCount |
| `messaging.ticket.created.v1` | SupportTicket.Create | Admin live dashboard | TicketId, UserId, Category, Priority, CreatedAt |
| `messaging.ticket.assigned.v1` | SupportTicket.AssignTo | Audit log | TicketId, AssignedToUserId, AssignedByUserId, AssignedAt |
| `messaging.ticket.resolved.v1` | SupportTicket.Resolve | Analytics (SLA breach metric, CSAT prompt) | TicketId, ResolvedByUserId, ResolutionNotes, TimeToResolveHours |
| `messaging.support-sla-breached.v1` | (deferred Phase 3) | Phase 3 alerting | TicketId, BreachedAt, ElapsedHours |

---

## 5. Integration events consumed (23 inbox handlers — see 00-README §3.2)

Grouped by upstream module:

**Auth (2):**
- `auth.user.registered.v1` → welcome notification
- `auth.otp.generated.v1` → OTP delivery (critical, force-channel-email)

**Accounts (4):**
- `accounts.provider.approved.v1` → "Your provider account is live"
- `accounts.provider.rejected.v1` → "Rejected: {Reason}"
- `accounts.provider.suspended.v1` → "Account suspended"
- `accounts.provider.reinstated.v1` → "Account reinstated"

**ContentPlaces (4 — already wired prior sprint):**
- `content-places.business.{approved/rejected/suspended/reinstated}.v1`

**Booking (8):**
- `booking.tour-booking.created.v1` → "Awaiting payment"
- `booking.tour-booking.confirmed.v1` → "Confirmed — see you on {Date}"
- `booking.tour-booking.cancelled.v1` → "Cancelled" (with refund info)
- `booking.tour-booking.completed.v1` → "How was your tour?" review prompt
- `booking.tour-booking.rejected.v1` → "Provider rejected" + refund
- `booking.tour-booking.payment-expired.v1` → "Payment window expired — re-book"
- `booking.join-request.approved.v1` → "Your join request was approved"
- `booking.join-request.rejected.v1` → "Your join request was rejected: {Reason}"
- `booking.provider-document.expiring.v1` → 30-day warning provider notification
- `booking.provider-document.expired.v1` → critical doc expired → suspension imminent

**Finance (7):**
- `finance.payment.completed.v1` → "Payment received" (CRITICAL)
- `finance.payment.failed.v1` → "Payment failed" (CRITICAL)
- `finance.refund.completed.v1` → "Refund processed" (CRITICAL)
- `finance.refund.failed.v1` → admin alert + retry exhausted
- `finance.invoice.generated.v1` → "Invoice ready"
- `finance.payout.scheduled.v1` → provider "Payout scheduled for {Date}"
- `finance.payout.completed.v1` → provider "Payout completed: {Amount}"

**Social (2):**
- `social.review.published.v1` → provider notification "New review received"
- `social.report.resolved.v1` → reporter notification of outcome

Total: **2 + 4 + 4 + 10 + 7 + 2 = 29** handlers? Let me recount: Auth 2 + Accounts 4 + ContentPlaces 4 already + Booking 10 + Finance 7 + Social 2 = **29 handlers**. (00-README said 23 — that count EXCLUDES the 4 ContentPlaces already wired and counts the 10 Booking handlers as 8.) Reconcile in T1 final scope review — likely 27 NEW handlers this sprint.

---

## 6. Value objects

| VO | Fields | Notes |
|---|---|---|
| `NotificationTitle` | `Value` (string 1-200) | Validation on construct |
| `NotificationBody` | `Value` (string 1-2000) | Validation on construct |
| `DeviceTokenValue` | `Value` (string 1-500) | No structural validation — opaque |
| `TicketSubject` | `Value` (string 10-200) | Validation |

VOs implement `IEquatable<T>` (struct or record struct).

---

## 7. Enums

**`NotificationType`** (extend existing — verify list):
```
WelcomeEmail = 0,
OtpDelivery = 1,
BookingCreated = 2,
BookingConfirmed = 3,
BookingCancelled = 4,
BookingCompleted = 5,
BookingPaymentExpired = 6,
BookingTomorrowReminder = 7,
JoinRequestApproved = 8,
JoinRequestRejected = 9,
PaymentCompleted = 10,
PaymentFailed = 11,
RefundInitiated = 12,
RefundCompleted = 13,
InvoiceReady = 14,
PayoutScheduled = 15,
PayoutCompleted = 16,
ReviewReceived = 17,
ReviewReply = 18,
ReportResolved = 19,
ProviderApproved = 20,
ProviderRejected = 21,
ProviderSuspended = 22,
ProviderReinstated = 23,
DocumentExpiring = 24,
DocumentExpired = 25,
DocumentSuspension = 26,
SupportTicketReply = 27,
SupportTicketResolved = 28,
NewSupportTicket = 29,        // admin-targeted
SecurityAlert = 30,            // CRITICAL
PasswordChanged = 31,          // CRITICAL
LoginFromNewDevice = 32        // CRITICAL
```

**`NotificationChannel`** = `InApp = 0, Email = 1, Push = 2, Sms = 3`

**`NotificationDeliveryStatus`** (NEW) = `Pending = 0, Succeeded = 1, Failed = 2`

**`DevicePlatform`** = `Android = 0, Ios = 1, Web = 2`

**`TicketCategory`** = `PaymentProblem = 0, BookingIssue = 1, ProviderComplaint = 2, AccountHelp = 3, BugReport = 4, Other = 5`

**`TicketPriority`** = `High = 0, Medium = 1, Low = 2`

**`TicketStatus`** = `Open = 0, Assigned = 1, InProgress = 2, AwaitingUser = 3, Resolved = 4, Closed = 5`

---

## 8. EF Configurations (10 files in `Messaging.Infrastructure/Persistence/Configurations/`)

| File | Notes |
|---|---|
| `NotificationConfiguration.cs` | PK Id; `IX_Notifications_UserId_CreatedAt DESC INCLUDE (Type, IsRead)` for list query; `IX_Notifications_UserId_IsRead WHERE IsRead = 0` for unread count |
| `NotificationPreferenceConfiguration.cs` | PK Id; UNIQUE (UserId, Type, Channel) |
| `NotificationTemplateConfiguration.cs` | PK Id; UNIQUE (Key, LanguageCode); Body/Html stored as nvarchar(max) |
| `NotificationDeliveryAttemptConfiguration.cs` | PK Id; FK NotificationId NO CASCADE; `IX_NotificationDeliveryAttempts_Status_AttemptedAt WHERE Status = 'Pending'` for BG service |
| `DeviceTokenConfiguration.cs` | PK Id; UNIQUE filtered (UserId, DeviceId) WHERE IsDeleted = 0; `IX_DeviceTokens_LastSeenAt` for stale cleanup |
| `SupportTicketConfiguration.cs` | PK Id; HasMany TicketMessages; `IX_SupportTickets_Status_SlaBreachAt` for admin queue; `IX_SupportTickets_CreatedByUserId` for user list |
| `TicketMessageConfiguration.cs` | PK Id; FK SupportTicketId CASCADE; `IX_TicketMessages_SupportTicketId_CreatedAt` |
| `AdminAssignmentRosterConfiguration.cs` | PK AdminUserId; ordered by LastAssignedAt for round-robin |
| `InboxMessageConfiguration.cs` | EXISTING — verify still wired |
| `OutboxMessageConfiguration.cs` | EXISTING — verify |

---

## 9. Migration sequence

Generated in this order. **Squash forbidden** (rollback safety):

| # | Name | Owner | Task | Notes |
|---|---|---|---|---|
| 1 | `MessagingAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 | May be empty — commit anyway |
| 2 | `MessagingAddNotificationDeliveryAttempts` | Tech Lead | PW-9 | New table + 2 indexes |
| 3 | `MessagingAddNotificationIndexes` | Mahmoud | T1 | List + unread-count + read-cleanup support indexes |
| 4 | `MessagingAddAdminAssignmentRoster` | Fadwa | T4 | New table for round-robin |
| 5 | `MessagingAddDeviceTokenUniqueIndex` | Fadwa | T3 | UNIQUE filtered (UserId, DeviceId) |
| 6 | `MessagingAddSupportTicketSlaIndex` | Fadwa | T4 | SLA-sorted admin queue |
| 7 | `MessagingSeedDefaultNotificationTemplates` | Junior | T7 | Idempotent INSERT-IF-NOT-EXISTS for 30+ templates EN/AR |

Apply via `dotnet ef database update --project Messaging.Infrastructure --startup-project YallaJo.Api --context MessagingDbContext`. Tech Lead applies in shared envs.
