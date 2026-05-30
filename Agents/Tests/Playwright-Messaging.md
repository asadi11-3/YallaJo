# Playwright MCP Test Scenarios — Messaging Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans / Score (7.5→10.0)

Sources read in full:
- `Agents/Plans/Messaging-Workflow.md`
- `Agents/Plans/Messaging-Audit-Report.md`
- `Agents/Plans/Messaging-FixPlan.md`

References:
- `Agents/Plans/Master-RoadmapTo10.md` — Messaging is W2-C HIGH priority, target 7.5→10.0.
- `Agents/Plans/CrossDocumentAnalysisReport.md` — Messaging gaps: zero validators, SignalR hub context bug, 6 `DateTime.UtcNow`, no SLA monitoring, zero HybridCache, ChatBot shell only.

Runtime context:
- Web UI: `https://localhost:57065/swagger` (`YallaJo.Web/Areas/Messaging` or `Notifications` when present).
- API: `https://localhost:57065`.
- Hub: `https://localhost:57065/hubs/notifications`.
- App currently not running; SQL bug known; reCAPTCHA disabled globally.
- Seed password: `TestPass!23`.
- Seed users: `admin@yallajo.test`, `userA@yallajo.test`, `userB@yallajo.test`, `guide-approved@yallajo.test`, `business@yallajo.test`, `agency@yallajo.test`.

Playwright MCP conventions for these scenarios:
- Use `browser_tabs` for multi-tab SignalR tests.
- Use `browser_console_messages` to capture hub events.
- Use `browser_network_requests` to verify negotiate/connect/send traffic for `/hubs/notifications` and REST calls.
- Use `browser_evaluate` for SignalR inspection and event capture:
  ```js
  () => {
    window.__notificationsReceived = [];
    const c = window.signalR?.connection || window.__notificationConnection;
    c?.on?.('ReceiveNotification', n => {
      window.__notificationsReceived.push(n);
      console.log('ReceiveNotification', JSON.stringify(n));
    });
    return c?.state;
  }
  ```
- Event assertion pattern:
  ```js
  mcp__playwright__browser_evaluate({ function: "() => { return window.__notificationsReceived || [] }" })
  ```
- SignalR trigger pattern: attach handler with `browser_evaluate`, trigger by API call/integration-event seed, then assert browser console + `window.__notificationsReceived`.

## 0. Prerequisites (SignalR hub at /hubs/notifications reachable, mock SMTP, push token store, support categories seeded)

TC-MSG-0001 — Environment health gate
- Steps: open API health/root; open web root; capture console/network errors.
- Expected: API + Web reachable over HTTPS; failures are test blockers, not Messaging failures.
- Status: BLOCKED until app + SQL bug fixed.

TC-MSG-0002 — Auth/login fixtures
- Steps: login each seed user; persist bearer/cookie/session state per role.
- Expected: all seed users authenticate with `TestPass!23`; admin has support-admin/financial-admin permissions; regular users lack admin queue permissions.

TC-MSG-0003 — Hub availability
- Steps: from authenticated page, inspect/load SignalR client; connect to `/hubs/notifications`; capture `/negotiate` and websocket/SSE request.
- Expected: hub reaches `Connected`; network shows `/hubs/notifications` negotiation/connect.
- Blocker note: known SignalR broadcast bug may allow connection but prevent delivery.

TC-MSG-0004 — Mock SMTP/outbox visibility
- Steps: configure/use seeded mock SMTP mailbox or log sink; trigger email notification; inspect outbox/SMTP sink through available admin/test endpoint/log.
- Expected: queued email attempt becomes sent in mock SMTP without real external delivery.

TC-MSG-0005 — Push token store seed
- Steps: login as users with seeded device tokens; call/list device tokens endpoint or verify UI device state.
- Expected: mock push tokens exist and can be registered/revoked without external FCM/APNS.

TC-MSG-0006 — Support category seed and enum coverage
- Steps: create/list ticket category options in UI/API.
- Expected: base categories plus TourGuide categories are accepted: `PayoutIssue`, `CommissionDispute`, `GuideScheduleIssue`, `TourApprovalHelp`, `DocumentVerification`.

## 1. Built — Active Scenarios

### In-App Notifications (SignalR live)

TC-MSG-0101 — User opens app → SignalR connects to `/hubs/notifications`
- Persona: `userA@yallajo.test`.
- Steps: login; open notifications page/shell; run `browser_evaluate` to return `window.signalR.connection.state` or `window.__notificationConnection.state`; inspect network for `/hubs/notifications`.
- Expected: state `Connected`; console has no hub auth errors; user is joined to `user:{userId}` server group.

TC-MSG-0102 — Booking event triggers notification → real-time badge update without refresh
- Persona: userA in browser; API/admin/test trigger creates booking confirmation event for userA.
- Steps: attach `ReceiveNotification` handler; record current badge; trigger booking notification via API/event seed; wait; read `window.__notificationsReceived`; inspect badge text.
- Expected: exactly one notification payload arrives; badge increments without page reload; payload contains `id/type/title/body/priority/createdAt` or current implemented subset.
- Known risk: broadcast handler currently resolves `IHubContext<Hub>` instead of typed `IHubContext<NotificationHub>`.

TC-MSG-0103 — Mark notification read → badge decrements
- Steps: after TC-MSG-0102, click notification or call `POST /api/v1/notifications/{id}/read`; observe badge.
- Expected: API 2xx; notification visually marked read; unread badge decrements by one; no full page refresh required.

TC-MSG-0104 — Notification list paginated, sorted by `CreatedAt desc`
- Steps: seed/create > page size notifications; open list with `pageSize=20` or UI page; record timestamps; go next page/cursor.
- Expected: page size honored; every page sorted newest→oldest; no duplicates across cursors.

TC-MSG-0105 — Notification list filters: type/isRead/date range
- Steps: query/open list with `type=PaymentCompleted`, `isRead=false`, `from`, `to`; compare displayed items.
- Expected: only matching notifications render; filter chips/querystring persist after refresh.

TC-MSG-0106 — Cannot disable critical notifications (OTP, payment)
- Steps: open preferences; attempt to disable `OtpDelivery`, `PaymentCompleted`, `PaymentFailed`, `RefundInitiated`, `RefundCompleted`, `SecurityAlert`, `LoginFromNewDevice`.
- Expected: UI disables toggle or API rejects with 400/validation; preference remains enabled.

TC-MSG-0107 — Non-critical notification preference can be disabled
- Steps: disable a non-critical type/channel; trigger same type; observe no chosen-channel delivery while in-app critical baseline remains unaffected.
- Expected: preference saved; no unwanted email/push for disabled non-critical type.

TC-MSG-0108 — Auto-delete after 30 days read
- Steps: seed read notification older than 30 days; run/advance cleanup service/test trigger; refresh list.
- Expected: old non-financial read notification removed; unread and critical notifications retained.

TC-MSG-0109 — 90-day financial retention
- Steps: seed read financial notification (Payment/Payout/Refund/Earnings) older than 30 but younger than 90 days; run cleanup.
- Expected: financial notification remains visible.

TC-MSG-0110 — Financial notification removed after 90 days when read
- Steps: seed read financial notification older than 90 days; run cleanup.
- Expected: notification removed unless critical type is configured never purge.

TC-MSG-0111 — Critical notification never purged
- Steps: seed/read `OtpDelivery`/payment critical older than retention; run cleanup.
- Expected: critical notification remains.

TC-MSG-0112 — Max 500 notifications per user, oldest pruned
- Steps: seed/create 501+ non-critical notifications for userA; refresh list/count via API.
- Expected: count capped at 500; oldest item pruned first; latest item retained.

TC-MSG-0113 — Notification detail ownership guard
- Steps: userA obtains own notification ID; userB calls/opens detail for same ID.
- Expected: userB receives 403/404 and no content leak.

TC-MSG-0114 — Delete own notification
- Steps: userA deletes a non-critical notification; refresh list.
- Expected: notification removed from userA list; unread count adjusted if unread.

TC-MSG-0115 — Batch delete notifications
- Steps: select multiple own notifications; invoke batch delete UI/API.
- Expected: all selected notifications removed; invalid/foreign IDs rejected or ignored without leak.
- Plan status note: audit lists batch operations as missing; current endpoint file exposes `DELETE /api/v1/notifications/batch`. Keep as active if present in running build; otherwise skip as NOT_BUILT.

TC-MSG-0116 — Mark-all-read / read-all
- Steps: create multiple unread notifications; click mark all/read all or call endpoint.
- Expected: unread count becomes zero; all listed notifications marked read.
- Plan status note: audit lists batch mark-all/read as missing; current endpoint file exposes `POST /api/v1/notifications/read-all`. Keep as active if present in running build; otherwise skip as NOT_BUILT.

### Email Notifications

TC-MSG-0201 — Template rendering with placeholders
- Steps: create/update template with placeholders (`{{UserName}}`, `{{BookingId}}`, `{{Amount}}`); trigger event with known values; inspect rendered email/log.
- Expected: placeholders resolved; no raw `{{...}}` remains; subject/body/html all render.

TC-MSG-0202 — Arabic locale template rendering
- Steps: set user language/Accept-Language `ar`; trigger notification with Arabic template.
- Expected: Arabic subject/body selected; RTL text not corrupted; fallback only when `ar` template missing.

TC-MSG-0203 — English locale template rendering
- Steps: set user language/Accept-Language `en`; trigger notification.
- Expected: English template selected.

TC-MSG-0204 — Missing locale fallback
- Steps: trigger type with only default language template.
- Expected: default template used; no failed delivery solely due missing locale.

TC-MSG-0205 — Email queued via outbox → consumed → SMTP sent
- Steps: trigger email-enabled notification; inspect network/API/log/mock SMTP after sender poll.
- Expected: delivery attempt queued; background service consumes; mock SMTP receives single email; attempt status `Sent`.

TC-MSG-0206 — SMTP failure retries and records failed attempt
- Steps: configure mock SMTP failure; trigger email; wait poll.
- Expected: attempt marked failed/retryable; error logged; no duplicate successful email.

TC-MSG-0207 — Critical email routed to transactional SMTP
- Steps: trigger OTP/payment/security email; inspect mock SMTP/log route.
- Expected: transactional path used, not marketing/digest path.

TC-MSG-0208 — Non-critical digest/bulk route
- Steps: trigger non-critical digest-configured notification.
- Expected: when digest built, cloud/bulk/marketing route is used if implemented.
- Status: NOT_BUILT if digest service/schema disabled.

### Support Tickets

TC-MSG-0301 — User creates ticket with category Booking
- Persona: userA.
- Steps: open support create; submit valid subject/body/category `BookingIssue`.
- Expected: ticket created; status `Open`; confirmation notification sent to user; admin notified.

TC-MSG-0302 — User creates ticket with category Payment
- Steps: submit valid category `PaymentProblem`.
- Expected: ticket created; priority/SLA derived correctly for payment issue.

TC-MSG-0303 — New TourGuide category: PayoutIssue
- Persona: `guide-approved@yallajo.test`.
- Steps: submit `PayoutIssue`.
- Expected: accepted; ticket tagged with guide/provider role context if implemented; financial retention applies to related notifications.

TC-MSG-0304 — New TourGuide category: CommissionDispute
- Steps: submit `CommissionDispute`.
- Expected: accepted; routed/prioritized for financial/support admin.

TC-MSG-0305 — New TourGuide category: GuideScheduleIssue
- Steps: submit `GuideScheduleIssue`.
- Expected: accepted; SLA calculated.

TC-MSG-0306 — New TourGuide category: TourApprovalHelp
- Steps: submit `TourApprovalHelp`.
- Expected: accepted; visible in admin queue filters.

TC-MSG-0307 — New TourGuide category: DocumentVerification
- Steps: submit `DocumentVerification`.
- Expected: accepted; visible in admin queue filters.

TC-MSG-0308 — Admin views support queue
- Persona: `admin@yallajo.test`.
- Steps: open admin support queue or call `GET /api/v1/support/tickets` as admin; filter by status/category.
- Expected: all users' tickets visible; SLA/priority/category shown; sorted by priority/SLA/created as designed.

TC-MSG-0309 — Non-admin sees only own tickets
- Steps: userA and userB each create ticket; userA lists tickets.
- Expected: userA sees only userA tickets; no userB leakage.

TC-MSG-0310 — Admin assigns ticket
- Steps: admin assigns ticket to support admin via `/api/v1/support/admin/tickets/{id}/assign`.
- Expected: assignee changes; `SupportTicketAssignedDomainEvent`/notification emitted; state transitions to assigned/in progress per implementation.

TC-MSG-0311 — Admin responds externally
- Steps: admin posts non-internal message; user opens ticket.
- Expected: user sees reply; ticket status changes to `AwaitingCustomer` if applicable; user notification sent.

TC-MSG-0312 — Admin adds internal note
- Steps: admin posts message with `isInternal=true`; user opens ticket.
- Expected: internal note hidden from user; visible to admin only.

TC-MSG-0313 — Customer replies to ticket
- Steps: user posts message after admin response.
- Expected: admin sees reply; status returns to `InProgress`/`Open` as workflow defines.

TC-MSG-0314 — SLA monitoring: 75% warn admin
- Steps: create/seed ticket near 75% SLA elapsed; run monitoring tick.
- Expected: assigned admin receives in-app warning; ticket not escalated yet.
- Status: NOT_BUILT if SLA monitoring service unavailable in target build.

TC-MSG-0315 — SLA monitoring: 100% breach escalation
- Steps: create/seed overdue ticket; run monitoring tick.
- Expected: `SlaBreached` integration event published; supervisor notified; priority auto-raised one level; SLA recalculated.
- Status: NOT_BUILT if SLA monitoring service unavailable in target build.

TC-MSG-0316 — Ticket attachments upload
- Steps: user/admin attaches valid image/PDF to ticket/message; open ticket.
- Expected: attachment appears with safe filename/link; virus/size/type validation enforced.
- Status: NOT_BUILT unless ticket attachment UI/API exists.

TC-MSG-0317 — Attachment authorization
- Steps: userB attempts to open userA ticket attachment URL.
- Expected: 403/404; no direct blob leak.
- Status: NOT_BUILT unless attachments exist.

TC-MSG-0318 — State machine: Open → InProgress
- Steps: create ticket; admin starts/assigns/responds.
- Expected: status moves from `Open` to `InProgress`/`Assigned` only by allowed action.

TC-MSG-0319 — State machine: InProgress → AwaitingCustomer
- Steps: admin replies requiring customer input.
- Expected: status `AwaitingCustomer`; user notified.

TC-MSG-0320 — State machine: AwaitingCustomer → InProgress
- Steps: customer replies.
- Expected: status returns to `InProgress`; admin notified.

TC-MSG-0321 — State machine: InProgress → Resolved
- Steps: admin resolves with notes.
- Expected: status `Resolved`; resolution notes stored; `TicketResolved`/notification emitted.

TC-MSG-0322 — State machine: Resolved → Closed
- Steps: user closes resolved ticket.
- Expected: status `Closed`; no further public replies allowed unless reopen exists.

TC-MSG-0323 — Invalid transition rejected
- Steps: try closing unresolved ticket or resolving already closed ticket.
- Expected: 400/409 business error; state unchanged.

TC-MSG-0324 — 90-day financial retention for ticket-linked notifications
- Steps: create financial ticket/notification, mark read, run cleanup before/after 90 days.
- Expected: retained until 90 days; purged after unless critical.

TC-MSG-0325 — Round-robin admin assignment
- Steps: create several tickets; inspect assignees.
- Expected: assignment rotates across seeded support-admin roster; no inactive admin assigned.

## 2. NOT_BUILT — Skip skeletons

These scenarios should be present in suites as skipped/pending until implementation is verified in the running build.

### 14 commands missing FluentValidation (per audit/fix plan)

Plan/audit list these as missing validators; if files exist in branch, keep the tests and flip from NOT_BUILT after runtime validation.
1. `CreateSupportTicketCommand`
2. `PostTicketMessageCommand`
3. `CloseTicketCommand` / `CloseSupportTicketCommand`
4. `AssignTicketCommand` / `AssignSupportTicketCommand`
5. `EscalateTicketCommand` / escalation endpoint
6. `MarkNotificationReadCommand`
7. `BatchMarkReadCommand` / `MarkAllNotificationsReadCommand`
8. `BatchDeleteNotificationsCommand`
9. `RegisterDeviceTokenCommand`
10. `DeleteDeviceTokenCommand`
11. `UpdateNotificationPreferenceCommand` / `UpdatePreferencesCommand`
12. `CreateNotificationTemplateCommand`
13. `UpdateNotificationTemplateCommand`
14. `DeleteNotificationTemplateCommand`

Skip skeletons:
- TC-MSG-NB-VAL-01 — invalid ticket subject/body/category returns validation problem.
- TC-MSG-NB-VAL-02 — empty ticket message body returns validation problem.
- TC-MSG-NB-VAL-03 — empty ticket/agent GUID rejected on assignment.
- TC-MSG-NB-VAL-04 — empty notification ID rejected on read/delete.
- TC-MSG-NB-VAL-05 — device token too long/invalid platform rejected.
- TC-MSG-NB-VAL-06 — template title/body/html/language constraints enforced.

### 8 queries missing HybridCache (per workflow)

1. `GetMyNotifications`
2. `GetUnreadCount`
3. `GetMyPreferences`
4. `GetNotificationById`
5. `GetNotificationTemplates`
6. `GetSupportTickets`
7. `GetSupportTicketById`
8. `GetMyDeviceTokens`

Skip skeletons:
- TC-MSG-NB-CACHE-01 — repeated unread count hit served from cache within TTL.
- TC-MSG-NB-CACHE-02 — notification create/read/delete invalidates user notification/unread tags.
- TC-MSG-NB-CACHE-03 — preference update invalidates preference cache.
- TC-MSG-NB-CACHE-04 — template CRUD invalidates template cache.
- TC-MSG-NB-CACHE-05 — ticket create/update/message invalidates user ticket/detail cache.
- TC-MSG-NB-CACHE-06 — device register/delete invalidates device-token cache.

### Other NOT_BUILT skeletons

- TC-MSG-NB-001 — SLA monitoring background service full E2E: 75% warning + 100% breach escalation.
- TC-MSG-NB-002 — Notification digest/batching daily digest at 07:00 UTC.
- TC-MSG-NB-003 — Notification digest/batching weekly digest Monday 07:00 UTC.
- TC-MSG-NB-004 — Batch operations: `POST /notifications/batch-read` max 100 IDs.
- TC-MSG-NB-005 — Batch operations: `DELETE /notifications/batch` max 100 IDs.
- TC-MSG-NB-006 — SignalR typed hub fix: broadcast uses `IHubContext<NotificationHub>` and sends payload to connected clients.
- TC-MSG-NB-007 — Admin support queue endpoint `GET /support/admin/tickets` with SLA filters.
- TC-MSG-NB-008 — Manual escalation endpoint `POST /support/admin/tickets/{id}/escalate`.
- TC-MSG-NB-009 — Priority change endpoint `PUT /support/admin/tickets/{id}/priority`.
- TC-MSG-NB-010 — Digest preferences endpoints `GET/PUT /notifications/preferences/digest`.

## 3. DEFERRED

TC-MSG-DEF-001 — SMS provider integration
- Current expected state: enum/channel + `NoopSmsStrategy`/NoOp sender only.
- Test: trigger SMS-preferred notification; assert no real outbound SMS; log/attempt marks provider deferred/noop.
- Deferred acceptance: no Twilio/Vonage/real gateway required.

TC-MSG-DEF-002 — Push notifications provider integration
- Current expected state: token storage exists; FCM/APNS integration deferred or NoOp.
- Test: register/upsert/revoke device token; trigger push; assert queued/noop/mock behavior only.
- Deferred acceptance: no real FCM/APNS network call required.

TC-MSG-DEF-003 — ChatBot shell
- Current expected state: `ChatBotConversation` and `ChatBotMessage` shell entities only, no endpoints/behavior.
- Test: verify chatbot UI/API routes absent or clearly disabled.
- Deferred acceptance: no bot conversation workflow required.

## 4. Integration Events

Consumes: 41+ events per audit/workflow, grouped as:
- Booking-* — booking created/confirmed/cancelled/completed/rejected, reminders, join requests, guide slot booked, guide booking assigned.
- Payment-* / Finance-* — payment completed/failed, refund initiated/completed/failed, invoice generated, payout scheduled/completed/failed, guide payout ready.
- Provider-* / Business-* — provider approved/rejected/suspended/reinstated/more-docs, business approved/rejected/suspended/reinstated.
- Tour-* / TourGuide-* — guide application approved/rejected/submitted, proposal/tour proposal approved/rejected, guide tier promoted/demoted, guide offering suspended, agency invitation/affiliation.
- Review-* / Social-* — review posted/replied/auto-hidden, report resolved, user warned/banned where applicable.
- Dispute-* — financial/support dispute opened/resolved/breached when implemented.

Integration event scenarios:
- TC-MSG-EVT-001 — BookingConfirmed event creates in-app notification for tourist.
- TC-MSG-EVT-002 — BookingCancelled event notifies both affected parties.
- TC-MSG-EVT-003 — PaymentCompleted event creates critical notification and transactional email.
- TC-MSG-EVT-004 — PaymentFailed event creates critical notification and cannot be disabled.
- TC-MSG-EVT-005 — RefundInitiated event creates 90-day-retained financial notification.
- TC-MSG-EVT-006 — PayoutScheduled event notifies provider/business account.
- TC-MSG-EVT-007 — Guide payout/earnings event notifies guide.
- TC-MSG-EVT-008 — ProviderApproved event notifies provider.
- TC-MSG-EVT-009 — ProviderRejected event notifies provider with reason/details if templated.
- TC-MSG-EVT-010 — BusinessSuspended event notifies business owner.
- TC-MSG-EVT-011 — GuideApplicationApproved event notifies guide.
- TC-MSG-EVT-012 — GuideApplicationRejected event notifies guide.
- TC-MSG-EVT-013 — Guide proposal approved/rejected event notifies guide.
- TC-MSG-EVT-014 — GuideBookingAssigned event notifies guide in real time.
- TC-MSG-EVT-015 — GuideSlotBooked event notifies guide.
- TC-MSG-EVT-016 — AgencyInvitationReceived event notifies guide.
- TC-MSG-EVT-017 — AgencyAffiliationApproved event notifies guide.
- TC-MSG-EVT-018 — ReportResolved event notifies reporter.
- TC-MSG-EVT-019 — Duplicate integration event is idempotent via inbox; only one notification created.
- TC-MSG-EVT-020 — Handler failure leaves message retryable and does not mark inbox processed.

Publishes:
- `NotificationSent`
- `NotificationRead`
- `TicketCreated`
- `TicketResolved`
- `SlaBreached`

Publish scenarios:
- TC-MSG-PUB-001 — Sending notification emits/persists `NotificationSent` once.
- TC-MSG-PUB-002 — Mark read emits/persists `NotificationRead` once and idempotently handles repeats.
- TC-MSG-PUB-003 — Create ticket emits/persists `TicketCreated` and user/admin notifications.
- TC-MSG-PUB-004 — Resolve ticket emits/persists `TicketResolved` and notifies customer.
- TC-MSG-PUB-005 — SLA breach emits/persists `SlaBreached` and escalation actions.

## 5. Validation Matrix (currently empty — write scenarios for what SHOULD be validated, mark as NOT_BUILT for the validator itself but the input validation test can still be a TC against the API)

| Area | Scenario | Expected API behavior | Validator status |
|---|---|---|---|
| Create ticket subject | TC-MSG-VAL-001: empty subject | 400 validation problem, no ticket | NOT_BUILT per audit |
| Create ticket subject length | TC-MSG-VAL-002: subject <10 or >200 | 400 validation problem | NOT_BUILT per audit |
| Create ticket body | TC-MSG-VAL-003: empty/body >5000 | 400 validation problem | NOT_BUILT per audit |
| Create ticket category | TC-MSG-VAL-004: invalid enum value | 400 validation problem | NOT_BUILT per audit |
| Post message | TC-MSG-VAL-005: empty/body >5000 | 400 validation problem | NOT_BUILT per audit |
| Ticket ID | TC-MSG-VAL-006: empty/nonexistent GUID in message/close/resolve | 400 for empty, 404 for nonexistent | NOT_BUILT per audit |
| Assignment | TC-MSG-VAL-007: empty `AdminUserId` | 400 validation problem | NOT_BUILT per audit |
| Resolve notes | TC-MSG-VAL-008: notes >2000 | 400 validation problem | NOT_BUILT per audit |
| Mark notification read | TC-MSG-VAL-009: empty notification ID | 400 validation problem | NOT_BUILT per audit |
| Batch IDs | TC-MSG-VAL-010: empty list | 400 validation problem | NOT_BUILT per audit |
| Batch max | TC-MSG-VAL-011: >100 IDs | 400 validation problem | NOT_BUILT per audit |
| Preferences | TC-MSG-VAL-012: invalid notification type/channel | 400 validation problem | NOT_BUILT per audit |
| Critical preferences | TC-MSG-VAL-013: disable critical notification | 400/409, remains enabled | Domain should enforce; validator optional |
| Template title | TC-MSG-VAL-014: empty/title >200 | 400 validation problem | NOT_BUILT per audit |
| Template body | TC-MSG-VAL-015: empty/body >5000/html >50000 | 400 validation problem | NOT_BUILT per audit |
| Template language | TC-MSG-VAL-016: invalid BCP-47 language code | 400 validation problem | NOT_BUILT per audit |
| Device token | TC-MSG-VAL-017: empty/token >500/deviceId >200 | 400 validation problem | NOT_BUILT per audit |
| Device platform | TC-MSG-VAL-018: invalid platform enum | 400 validation problem | NOT_BUILT per audit |
| Date filters | TC-MSG-VAL-019: from > to | 400 validation problem or empty list by contract | SHOULD validate |
| Pagination | TC-MSG-VAL-020: pageSize <=0 or >100 | 400 or clamped by contract | SHOULD validate |

## 6. Auth Matrix

| Route/Action | Anonymous | userA/userB | guide-approved | business/agency | admin | Expected |
|---|---:|---:|---:|---:|---:|---|
| `GET /api/v1/notifications` | 401 | own only | own only | own only | own only/admin own | protected read |
| `GET /api/v1/notifications/unread-count` | 401 | own | own | own | own | protected read |
| `GET /api/v1/notifications/{id}` own | 401 | 200 | 200 | 200 | 200 | owner access |
| `GET /api/v1/notifications/{id}` other user | 401 | 403/404 | 403/404 | 403/404 | admin only if permitted | no IDOR |
| `POST /api/v1/notifications/{id}/read` own | 401 | 2xx | 2xx | 2xx | 2xx | owner update |
| `POST /api/v1/notifications/read-all` | 401 | 2xx | 2xx | 2xx | 2xx | owner update |
| `DELETE /api/v1/notifications/{id}` own | 401 | 2xx | 2xx | 2xx | 2xx | owner delete |
| `DELETE /api/v1/notifications/batch` own IDs | 401 | 2xx | 2xx | 2xx | 2xx | owner delete |
| `GET /api/v1/notifications/preferences` | 401 | own | own | own | own | protected read |
| `PUT /api/v1/notifications/preferences` | 401 | own | own | own | own | protected update |
| `POST /api/v1/support/tickets` | 401 | 201/2xx | 201/2xx | 201/2xx | 201/2xx | protected create |
| `GET /api/v1/support/tickets` | 401 | own only | own only | own only | all queue if permission | no cross-user leak |
| `GET /api/v1/support/tickets/{id}` own | 401 | 200 | 200 | 200 | 200 | owner/admin |
| `POST /api/v1/support/tickets/{id}/messages` own | 401 | 2xx | 2xx | 2xx | 2xx | owner/admin |
| internal ticket note | 401 | forbidden/ignored | forbidden/ignored | forbidden/ignored | 2xx | admin only |
| `POST /api/v1/support/tickets/{id}/close` | 401 | own allowed | own allowed | own allowed | allowed | state guarded |
| `POST /api/v1/support/admin/tickets/{id}/assign` | 401 | 403 | 403 | 403 | 2xx | admin support only |
| `POST /api/v1/support/admin/tickets/{id}/resolve` | 401 | 403 | 403 | 403 | 2xx | admin support only |
| notification template CRUD | 401 | 403 | 403 | 403 | 2xx | admin only |
| device token CRUD | 401 | own | own | own | own | protected owner |
| `/hubs/notifications` connect | 401 | connected | connected | connected | connected + admin group | authorized hub |

Auth scenarios:
- TC-MSG-AUTH-001 — anonymous cannot access any Messaging REST endpoint or hub.
- TC-MSG-AUTH-002 — userA cannot read/mark/delete userB notification.
- TC-MSG-AUTH-003 — userA cannot see userB ticket list/detail/messages.
- TC-MSG-AUTH-004 — regular user cannot assign/resolve as admin.
- TC-MSG-AUTH-005 — admin can access support queue and assignment/resolve endpoints.
- TC-MSG-AUTH-006 — support-admin internal note never leaks to customer.
- TC-MSG-AUTH-007 — hub connection without token/cookie rejected.
- TC-MSG-AUTH-008 — logout invalidates REST and hub access.

## 7. SignalR Live Tests

TC-MSG-SR-001 — Open 2 browser tabs same user → both receive event
- Steps: login userA in Tab 1; duplicate/new tab with same session; attach handler in both:
  ```js
  () => {
    window.__notificationsReceived = [];
    const c = window.signalR?.connection || window.__notificationConnection;
    c?.on?.('ReceiveNotification', n => {
      window.__notificationsReceived.push(n);
      console.log('ReceiveNotification', JSON.stringify(n));
    });
    return c?.state;
  }
  ```
  Trigger notification via API/event; read `window.__notificationsReceived` in both tabs.
- Expected: both tabs receive one matching payload.

TC-MSG-SR-002 — Different user tab does not receive event
- Steps: Tab 1 userA, Tab 2 userB; attach handlers; trigger notification for userA.
- Expected: userA receives; userB receives none.

TC-MSG-SR-003 — Admin group notification
- Steps: admin connects; trigger admin support queue notification/ticket opened.
- Expected: admin receives queue/admin notification; non-admin does not.

TC-MSG-SR-004 — Provider group notification
- Steps: business/provider connects with `provider_id` claim; trigger provider booking/business event.
- Expected: provider receives provider-group notification.

TC-MSG-SR-005 — Logout → disconnect
- Steps: connected user logs out; inspect SignalR state and network.
- Expected: connection closes/disconnects; no further events delivered to that browser context.

TC-MSG-SR-006 — Connection drop → reconnect → backlog delivered
- Steps: connect; simulate offline/network disconnect or stop/restart hub; trigger notification while disconnected; reconnect; fetch list/unread count.
- Expected: SignalR reconnects; missed notification appears in persisted list/unread backlog. Live replay may not occur unless designed; persisted backlog must be delivered via API.

TC-MSG-SR-007 — MarkAsRead hub method behavior
- Steps: invoke hub `MarkAsRead(notificationId)` from browser if client exposes connection; refresh notification.
- Expected: current plan says hub method delegates/no-op and REST handles state; test should assert no state change via hub alone unless implementation is fixed.

TC-MSG-SR-008 — Console and network diagnostics
- Steps: run live delivery test; collect `browser_console_messages` and `browser_network_requests` filtered by `/hubs/notifications`.
- Expected: no auth/transport errors; receive event logged once.

TC-MSG-SR-009 — Typed hub bug detection
- Steps: connect normally; trigger notification; inspect `window.__notificationsReceived`.
- Expected while bug exists: connection may succeed but no broadcast arrives. Mark failure as known divergence: `IHubContext<Hub>` should be `IHubContext<NotificationHub>`.

## 8. SLA Monitoring (per workflow — mark as NOT_BUILT for the service; describe expected behavior)

Service expectation:
- Runs every 5 minutes.
- Queries tickets with status `Open`, `Assigned`, `InProgress` and `SlaBreachAt != null`.
- Calculates elapsed percent: `(now - CreatedAt) / (SlaBreachAt - CreatedAt) * 100`.
- At >=75% and not warned: create in-app warning to assigned admin and mark warned.
- At >=100% and not breached: publish `SlaBreachedIntegrationEvent`, notify supervisor, auto-raise priority one level (`Low→Medium`, `Medium→High`, `High` unchanged), recalculate `SlaBreachAt`, mark breached.

SLA scenarios:
- TC-MSG-SLA-001 — Low priority SLA is 24h; Medium 12h; High 4h.
- TC-MSG-SLA-002 — Ticket below 75% SLA produces no warning.
- TC-MSG-SLA-003 — Ticket at 75% sends one warning to assigned admin.
- TC-MSG-SLA-004 — Repeated monitoring tick does not duplicate 75% warning.
- TC-MSG-SLA-005 — Ticket at 100% publishes breach event.
- TC-MSG-SLA-006 — 100% breach notifies supervisor/next-tier admin.
- TC-MSG-SLA-007 — 100% breach auto-raises Low→Medium and recalculates SLA.
- TC-MSG-SLA-008 — 100% breach auto-raises Medium→High and recalculates SLA.
- TC-MSG-SLA-009 — High breach remains High.
- TC-MSG-SLA-010 — Resolved/Closed tickets are ignored by SLA worker.
- TC-MSG-SLA-011 — Ticket already warned/breached is idempotent across worker restarts.
- TC-MSG-SLA-012 — User sees breach/response notification after admin escalation.

Status: NOT_BUILT per audit/fix plan unless target build includes and enables `SlaMonitoringService`.

## 9. Known Divergence

- SignalR bug: `NotificationCreatedSignalRBroadcastHandler` uses `IHubContext<Hub>`; should use `IHubContext<NotificationHub>`. Live delivery tests should fail/skip with known bug until fixed.
- 6 `DateTime.UtcNow` violations in domain/entities (`Notification`, `NotificationDeliveryAttempt`, `TicketMessage` per plan) should use `ITimeProvider`/`TimeProvider` for testable time and SLA/retention correctness.
- 21 auth gate violations: handler/endpoint bodies perform `ICurrentUser` auth/null checks instead of relying on explicit endpoint `MustHavePermissionAttribute`; tests should emphasize perimeter auth + ownership only.
- Plan/audit count drift: audit says 23 REST endpoints + 1 hub, current plan says 22/23 depending W2-C notes; tests should discover routes from OpenAPI/network before execution.
- Validator/cache drift: source audit says 0 validators/0 HybridCache; current branch may contain validator/cache files after W2-C. Treat NOT_BUILT rows as source-plan skeletons and unskip only after runtime behavior proves active.
- Digest service may be stub/disabled until schema columns exist; keep digest tests skipped unless enabled.
- SMS and real push providers are explicitly deferred; do not fail E2E because no real gateway is configured.
