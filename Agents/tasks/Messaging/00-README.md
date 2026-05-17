> **Predecessor sprint:** Social (Wave 6 reviews/favorites/moderation) — `Agents/decisions/closed/Social/`
> **This sprint covers:** Messaging module Phase 2 — Notifications CRUD + NotificationHub (SignalR) + Device Tokens (FCM/APNs) + Support Tickets + Notification Templates + 2 background services (EmailNotificationSender continuous + ReadNotificationCleanup weekly Sun 02:00 UTC). All listed in master `Phase1-Phase2-Completion-INDEX.md §1`.
> **Difficulty:** ⚙️⚙️⚙️⚙️ (4/5) — SignalR adds real-time complexity; cross-cutting nature (every module fires events that produce notifications)
> **Endpoint count:** **22 HTTP endpoints** + 1 SignalR hub + 2 BG services
> **Working-day estimate:** **35 working days × 4 devs ≈ 175 person-hours**

This module is the YallaJo notification spine. It already has skeleton infrastructure (per `agent-context.md §11.1`: InboxMessages table, UoW, InboxStore, `Notification.Create` factory, 4 Business-related inbox handlers from prior ContentPlaces sprint). This sprint fills out the remaining endpoint surface, wires SignalR, builds the EmailNotificationSender pipeline, ships Support Tickets with SLA-driven priority + round-robin assignment, and writes a single canonical NotificationTemplate engine driven by {{placeholders}}.

---

## 0 — Sprint Window & Hard Deadlines

| Milestone | Date | Owner |
|---|---|---|
| Pre-work cut | **Fri 2026-11-27 17:00 AST** | Tech Lead |
| Kickoff (Sun standup) | **Sun 2026-11-29 09:30 AST** | All |
| Pre-work merge deadline | **Tue 2026-12-01 17:00 AST** | Tech Lead |
| Earliest task start | **Wed 2026-12-02 09:00 AST** | All |
| Mid-sprint integration freeze | **Sun 2027-01-10 17:00 AST** | All |
| Hard PR cutoff | **Wed 2027-01-13 17:00 AST** | All |
| Hard merge-to-main cutoff | **Thu 2027-01-14 17:00 AST** | Tech Lead |
| Sprint retro + demo | **Fri 2027-01-15 11:00 AST** | All |

Working week Sun→Thu (5 days). Total = **6 calendar weeks, 30 working days, 1 break-week buffer (last week of Dec for holidays)**.

### 0.1 Daily Standup

Same as INDEX §2.1 — 09:30 AST, 15 min hard cap, 3 sentences per dev (yesterday / today / blockers). **Miss-2 rule** triggers Tech Lead 1:1 if any dev misses standup twice in a row.

---

## 1 — Working Days & Person-Hour Budget

| Item | Value |
|---|---|
| Working days | 30 (≈ 6 weeks Sun→Thu, minus holidays) |
| Hours/day/dev | 7 (1h slack for lunch/standup/Slack) |
| Devs | 4 (Tech Lead, Mohammad, Mahmoud, Fadwa) |
| Total raw hours | 840 |
| Task hours (this file budgets) | 175 |
| Code review hours | 30 |
| Ceremony hours (standup × 30) | 30 |
| Buffer (holidays + sick + rework) | 605 (~72% — Dec/Jan low velocity expected) |

The high buffer accounts for end-of-year holidays — track effective velocity in standups.

---

## 2 — Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG / Hubs | Est hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mohammad** | Lead | T2 NotificationHub + T5 EmailSender BG | 0 HTTP (hub only) + 1 BG | 1 hub + 1 BG | 50 | Sun 2027-01-10 |
| **Mahmoud** | Intermediate | T1 Notifications CRUD + preferences (8 endpoints) | 8 | 0 | 40 | Sun 2026-12-27 |
| **Fadwa** | Intermediate | T3 Devices/Tokens (3 endpoints) + T4 Support Tickets (7 endpoints) + T6 ReadCleanup BG | 10 | 1 BG | 60 | Sun 2027-01-03 |
| **Junior** (or Fadwa fallback) | Beginner | T7 NotificationTemplates CRUD (4 endpoints) | 4 | 0 | 18 | Sun 2026-12-20 |
| **Tech Lead** | Senior | PW + code review + cross-cutting + Hangfire abstention enforcement + SignalR scaling sanity | 0 | 0 | 7 | Thu 2027-01-14 |
| **TOTAL** | | **7 tasks** | **22 endpoints** | **1 hub + 2 BG** | **175 h** | |

**Owner mapping rationale:** Mohammad gets the SignalR hub because it's the trickiest piece (transport details, group routing, JWT auth on connection, reconnect strategy). Mahmoud's Notifications CRUD work feeds the hub. Fadwa owns both Devices (Phase 2 stub — register tokens, hold for v3 push) and Support Tickets (largest endpoint set but routine CRUD). NotificationTemplates is the simplest path so it goes to a junior or Fadwa as fallback.

---

## 3 — Entity Ownership Matrix

### 3.1 Messaging module

| File (existing) | Base class | Aggregate? | Domain events raised | Owner | Task |
|---|---|---|---|---|---|
| `Notification.cs` (71L, Create+MarkSent+MarkRead already exist) | AuditableEntity | **Yes (promote in PW-2)** | NotificationCreated, NotificationRead, NotificationDelivered, NotificationFailed | Mahmoud | T1 |
| `NotificationPreference.cs` | AuditableEntity | **Yes (promote in PW-2)** | NotificationPreferenceUpdated | Mahmoud | T1 |
| `NotificationTemplate.cs` | AuditableEntity | **Yes (promote in PW-2)** | NotificationTemplateUpdated | Junior | T7 |
| `DeviceToken.cs` | AuditableEntity | **Yes (promote in PW-2)** | DeviceTokenRegistered, DeviceTokenDeleted | Fadwa | T3 |
| `SupportTicket.cs` (23L stub) | AuditableEntity | **Yes (promote in PW-2)** | TicketCreated, TicketAssigned, TicketResolved, TicketClosed | Fadwa | T4 |
| `TicketMessage.cs` | BaseEntity (owned by SupportTicket) | No | (none — SupportTicket raises TicketMessageAdded on its behalf) | Fadwa | T4 |
| **`ChatBotConversation.cs`** | — | — | — | **OUT OF SCOPE** (Phase 4) |
| **`ChatBotMessage.cs`** | — | — | — | **OUT OF SCOPE** (Phase 4) |

### 3.2 Cross-module dependencies (Messaging is the dispatch hub — many inputs)

| Inbox source | Event | Trigger |
|---|---|---|
| Auth | `auth.user.registered.v1` | Welcome notification (existing handler — verify wired) |
| Auth | `auth.otp.generated.v1` | OTP delivery (existing — verify wired) |
| ContentPlaces | `content-places.business.{approved/rejected/suspended/reinstated}.v1` | Already wired (4 handlers from prior sprint) |
| Booking | `booking.tour-booking.{created/confirmed/cancelled/completed/rejected/payment-expired}.v1` | 6 NEW handlers in T1 (each creates a Notification) |
| Booking | `booking.join-request.{approved/rejected}.v1` | 2 NEW handlers in T1 |
| Booking | `booking.provider-document.{expiring/expired}.v1` | 2 NEW handlers in T1 |
| Finance | `finance.{payment.completed, payment.failed, refund.completed, refund.failed, invoice.generated, payout.{scheduled,completed}}.v1` | 7 NEW handlers in T1 |
| Social | `social.{review.published, report.resolved}.v1` | 2 NEW handlers in T1 |
| Accounts | `accounts.provider.{approved/rejected/suspended/reinstated}.v1` | 4 NEW handlers in T1 (admin-action notifications) |

Total NEW inbox handlers: **23** distributed across T1 (Notifications CRUD). Each handler is small (~30 LOC) — resolve `INotificationTemplateRenderer` + `INotificationDispatcher`, call `Notification.Create`, persist via `IMessagingOutboxWriter` → SaveChanges in UoW (single).

---

## 4 — Endpoint Count Reconciliation vs PDF 1

| PDF 1 Wave 6 item | Endpoints | In this sprint |
|---|---|---|
| Notifications CRUD (GET list, GET unread-count, GET preferences, PUT preferences, POST read, POST read-all, DELETE) | 7 | ✅ Mahmoud T1 |
| Device tokens (POST token, DELETE token) | 2 | ✅ Fadwa T3 |
| Support tickets (POST, GET tickets, GET {id}, POST close, POST messages, admin POST assign, admin POST resolve) | 7 | ✅ Fadwa T4 |
| Notification templates CRUD admin-only (added in this sprint) | 4 | ✅ Junior T7 |
| NotificationHub SignalR | 0 HTTP (1 hub) | ✅ Mohammad T2 |
| **TOTAL** | **22 HTTP + 1 hub** | |

DeviceToken total = **3** because the `GET /devices/tokens` (list current user's tokens for the "Logged-in devices" settings UI) was added on Mohammad's recommendation — useful for security audits.

---

## 5 — Standardized Folder Files

Same convention as Booking/Finance/Social (see INDEX §3):
- `01-pre-work.md` — PW-1..PW-N (Tech Lead drives, must merge before kickoff)
- `02-critical-rules.md` — Messaging-specific rules (M-R1..M-RN) ON TOP OF INDEX §4
- `03-entities-matrix.md` — full entity inventory with aggregate roles + EF configs + migrations
- `04-task-notifications-crud.md` — T1 Mahmoud
- `05-task-signalr-hub.md` — T2 Mohammad
- `06-task-devices.md` — T3 Fadwa
- `07-task-support-tickets.md` — T4 Fadwa
- `08-task-background-services.md` — T5 + T6 (Mohammad EmailSender + Fadwa ReadCleanup)
- `09-task-notification-templates.md` — T7 Junior
- `10-cross-cutting.md` — DI audit, permission seeder verification, outbox parity, build lock, migration sequence, inbox/outbox hygiene, SignalR scaling notes
- `99-acceptance-gate.md` — Tech Lead final sign-off

When sprint closes, **whole folder moves** to `Agents/decisions/closed/Messaging/`. Update master INDEX §1 status badge from 🟡 to ✅, and `agent-context.md §11.1` Messaging row from 🟡 Partial → ✅ Complete (Phase 2).

---

## 6 — Reading Order for New Joiners

1. `Agents/agent-context.md` (§0.3 Five Non-Negotiable Rules, §9.1 33 Gotchas)
2. `Agents/guide.md` (entity anatomy, CQRS templates, EF configs)
3. `Agents/YallaJo.md` §Wave 6 + §SignalR Hubs (NotificationHub spec) + §Background Services (EmailNotificationSender + ReadNotificationCleanup)
4. `Endpoints.pdf` Wave 6 rows for Notifications/Devices/Tickets/Support
5. `YallaJo Business Rules & Edge Cases.pdf` §12 Smart Notification System
6. **This folder** — start with `00-README.md` (this file) → `01-pre-work.md` → `02-critical-rules.md` → your assigned task file
7. Prior sprints' closed folders for examples: `decisions/closed/ContentBlogs-ContentSeo/`, `decisions/closed/Booking/`, `decisions/closed/Finance/`, `decisions/closed/Social/`

**Estimated onboarding time:** 4 hours senior dev / 8 hours junior. Standup attendance from Day 1.
