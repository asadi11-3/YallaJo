# Messaging Module — Final Acceptance Gate

> **Tech Lead signs off before declaring the Messaging sprint closed Thu 2027-01-14 17:00. Folder must NOT move to `Agents/decisions/closed/Messaging/` until every box below is ticked.**

---

## 1. Code Quality (PR & build)

- [ ] All 7 task PRs (T1..T7) merged into `main`.
- [ ] `dotnet build` green for every Messaging project (Domain, Contracts, Application, Infrastructure, Presentation) + 2 test projects.
- [ ] **Zero TODOs/FIXMEs** in committed code under `Messaging/`.
- [ ] **All command handlers** call `RemoveByTagAsync` after SaveChanges (audit 3 random handlers).
- [ ] **All queries** implement `ICacheableQuery`.
- [ ] **No `ICurrentUser` in handlers** outside the 12-handler audit list in 02-critical-rules.md §M-R11.
- [ ] **No bare `RequireAuthorization()`** in any Messaging Presentation endpoint.
- [ ] **`Messaging.Tests.Unit` green** — minimum **70 tests** (10 aggregate × 5 entities + 20 dispatcher/template renderer/round-robin).
- [ ] **`Messaging.IntegrationTests` green** — minimum **20 tests** (5 endpoint × 4 task areas + 5 SignalR hub + outbox parity).
- [ ] **`IntegrationEventTypeRegistryParityTests` passes** (cross-cutting §3).

---

## 2. Endpoint Smoke Test (manual Postman + SignalR client)

| # | Method | Path | Expected |
|---|---|---|---|
| 1 | GET | `/notifications?pageSize=10` | 200 + cursor envelope |
| 2 | GET | `/notifications/unread-count` | 200 + scalar |
| 3 | GET | `/notifications/preferences` | 200 + per-type-per-channel list |
| 4 | PUT | `/notifications/preferences` (disable OtpDelivery email) | 422 `CannotDisableCritical` |
| 5 | PUT | `/notifications/preferences` (disable BookingReminder email) | 200 |
| 6 | POST | `/notifications/{id}/read` | 200 + cache invalidated |
| 7 | POST | `/notifications/{id}/read` again | 200 idempotent |
| 8 | POST | `/notifications/{id}/read` on other user's | 403 |
| 9 | POST | `/notifications/read-all` | 200 + 0 unread |
| 10 | DELETE | `/notifications/{id}` | 204 + soft-delete |
| 11 | POST | `/devices/token` first time | 201 + DTO |
| 12 | POST | `/devices/token` same deviceId | 200 + updated Token |
| 13 | POST | `/devices/token` invalid platform | 400 |
| 14 | GET | `/devices/tokens` | 200 list (Token value NOT in DTO) |
| 15 | DELETE | `/devices/token/{id}` | 204 |
| 16 | POST | `/support/tickets` (PaymentProblem) | 201 + Priority=High + SlaBreachAt=now+4h |
| 17 | (auto) | TicketCreated → round-robin → Assigned | within 5s — verify in DB |
| 18 | POST | `/support/tickets/{id}/messages` | 201 + Status→InProgress on admin reply |
| 19 | POST | `/support/admin/tickets/{id}/assign` | 200 + AssignedToUserId stamped |
| 20 | POST | `/support/admin/tickets/{id}/resolve` w/o notes | 400 |
| 21 | POST | `/support/admin/tickets/{id}/resolve` w/ notes | 200 + TicketResolved event |
| 22 | POST | `/support/tickets/{id}/close` on Resolved | 200 + Status→Closed |
| 23 | POST | `/support/tickets/{id}/messages` on Closed | 422 `AlreadyClosed` |
| 24 | POST | `/admin/notification-templates` (duplicate key) | 409 `DuplicateKey` |
| 25 | POST | `/admin/notification-templates` with bad placeholder | 400 `InvalidPlaceholderSyntax` |
| 26 | PUT | `/admin/notification-templates/{id}` | 200 + EmailSender uses new copy within 30s |
| 27 | DELETE | `/admin/notification-templates/{id}` | 204 + renderer falls back |
| 28 | GET | `/admin/notification-templates` | 200 + discoveredPlaceholders populated |

---

## 3. Outbox / Inbox Round-Trip

| Action | Outbox row | Logical name | Consumer | SLA |
|---|---|---|---|---|
| EmailSender marks success | `messaging.OutboxMessages` | `messaging.notification.delivered.v1` | Analytics | 30s |
| EmailSender exhausts retries | 1 row | `messaging.notification.failed.v1` | Admin Slack | 30s |
| POST /support/tickets | 1 row | `messaging.ticket.created.v1` | Admin dashboard | 30s |
| POST /admin/tickets/{id}/assign | 1 row | `messaging.ticket.assigned.v1` | Audit log | 30s |
| POST /admin/tickets/{id}/resolve | 1 row | `messaging.ticket.resolved.v1` | Analytics CSAT | 30s |
| Booking emits `booking.tour-booking.confirmed.v1` | inbox row | (consumed) | Creates Notification → SignalR push | 30s |
| Finance emits `finance.payment.completed.v1` | inbox row | (consumed) | Creates critical Notification → Email forced | 30s |
| Auth emits `auth.user.registered.v1` | inbox row | (consumed) | Welcome email + populates UserSnapshot | 30s |

---

## 4. Background Services Live Test (24h soak)

- [ ] EmailSender tick every 30s — OTEL `bg_service_ticks_total{service=EmailNotificationSenderService}` ≈ 2880 in 24h.
- [ ] ReadCleanup runs once at Sunday 02:00 UTC — `bg_service_ticks_total{service=ReadNotificationCleanupService}` = 1 (or 0 if not Sunday yet).
- [ ] `SELECT COUNT(*) FROM messaging.NotificationDeliveryAttempts WHERE Status = 'Pending' AND AttemptedAt < DATEADD(MINUTE, -30, SYSUTCDATETIME())` → **0**.
- [ ] `SELECT COUNT(*) FROM messaging.Notifications WHERE IsRead = 1 AND ReadAt < DATEADD(DAY, -30, SYSUTCDATETIME()) AND Type NOT IN (critical)` → **0** after Sunday cleanup.
- [ ] `SELECT COUNT(*) FROM messaging.DeviceTokens WHERE LastSeenAt < DATEADD(DAY, -30, SYSUTCDATETIME())` → **0** after Sunday cleanup.
- [ ] No `[ERROR]` from any BG service in 24h Serilog file (excluding expected `EmailSender: SMTP timeout, will retry` info).

---

## 5. SignalR Live Test

- [ ] Open browser console, connect via SignalR JS client to `/hubs/notifications` with valid JWT — connection established.
- [ ] Receive `ReceiveNotification` payload when admin triggers a test notification via admin endpoint (sample admin endpoint or via T1 inbox handler).
- [ ] Call `MarkAsRead(notId)` from browser → DB row updated + `NotificationRead` event broadcast back.
- [ ] Open second tab → call MarkAsRead in tab A → tab B receives `NotificationRead` event (cross-tab sync).
- [ ] Disconnect for 5s → reconnect → re-added to all groups automatically.
- [ ] 100 concurrent connections via load test → all receive same notification (latency p95 < 500ms).

---

## 6. Performance Sanity

| Endpoint | p95 target | Hard ceiling |
|---|---|---|
| POST `/notifications/{id}/read` | < 80ms | < 200ms |
| GET `/notifications?...` cached | < 120ms | < 300ms |
| GET `/notifications/unread-count` cached | < 50ms | < 150ms |
| POST `/support/tickets` (incl. auto-assign) | < 200ms | < 500ms |
| EmailSender per-tick 50 emails | < 30s | < 60s |
| SignalR push end-to-end | < 500ms | < 2s |
| ReadCleanup weekly (1M notifications) | < 5min | < 15min |

---

## 7. Documentation Hygiene

- [ ] Every endpoint has XML doc summary on Command/Query record.
- [ ] All 18 Messaging permissions in `Agents/permissions-inventory.md` (create file if missing).
- [ ] **Sprint folder moves to `Agents/decisions/closed/Messaging/`** via:
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Messaging" -Destination "Agents\decisions\closed\Messaging"
  ```
- [ ] Master `Phase1-Phase2-Completion-INDEX.md §1` Messaging row updated 🟡 → ✅ + link to closed/ path.
- [ ] `agent-context.md §11.1` Messaging row updated: "🟡 Partial" → "✅ Complete (Phase 2)".
- [ ] `AGENTS.md` (repo root) Messaging entry updated.
- [ ] `Agents/error-log.md` updated with any new gotchas hit (especially SignalR auth quirks, Stubble template parse failures).
- [ ] New ADR if any architectural decision made (e.g. "ADR-XXX: SignalR backplane deferred to v2 multi-instance").

---

## 8. Sprint Retro & Demo (Fri 2027-01-15 11:00 AST)

15-min demo by Mohammad:
1. Live SignalR connection in browser, receive booking-confirmed notification, mark as read.
2. Support ticket workflow: POST → auto-assign → message → resolve → close.
3. EmailSender demo: trigger 5 emails, watch them flow through Pending → Succeeded.
4. Admin dashboard: live counter ticks when new ticket arrives (admin SignalR group).

Retro doc lives at `Agents/decisions/closed/Messaging/_retro.md`:
- What went well
- What hurt (especially SignalR debugging if any issues)
- Action items for next sprint (Analytics — kickoff Mon 2027-01-18)

---

## 9. Sign-Off Block

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 owner (Notifications CRUD) | Mahmoud | _____ | _____ |
| T2 owner (SignalR Hub) | Mohammad | _____ | _____ |
| T3 owner (Devices) | Fadwa | _____ | _____ |
| T4 owner (Support Tickets) | Fadwa | _____ | _____ |
| T5 owner (EmailSender BG) | Mohammad | _____ | _____ |
| T6 owner (ReadCleanup BG) | Fadwa | _____ | _____ |
| T7 owner (Templates) | Junior | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |
