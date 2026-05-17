# Phase 1 + Phase 2 Completion — Master Sprint Index

> **Predecessor sprint:** [`ContentBlogs-ContentSeo-team-tasks.md`](./ContentBlogs-ContentSeo-team-tasks.md) (Wave 4 remainder — closes 2026-06-12)
> **This index covers:** SIX independent per-module sprint files that together close out **all remaining Phase 1 MVP work + all remaining Phase 2 (Experience & Discovery) work**
> **Aggregate window:** 2026-06-15 (kickoff Booking) → **2027-05-27 (hard cutoff for Auth-Cleanup merge)** — user-mandated deadline = before end of May 2027
> **Aggregate endpoint count:** ~100 HTTP endpoints + 1 SignalR hub + 1 middleware
> **Aggregate background services:** 10 hosted services (Booking 4, Finance 2, Messaging 2, Analytics 1, Social 1)
> **Aggregate person-hour budget:** ~960 person-hours (≈ 50 working weeks × 4 devs × 6 hrs/day @ 0.8 utilization)

---

## 0. Why six files, not one

Per directive from project owner (2026-05-18): **one file per bounded-context module**. Reasons:

1. **Independent review queues** — each module sprint has its own PR cluster, code owners, and merge cadence.
2. **Parallel-friendly** — Social and Analytics share zero entities and can run in parallel windows once their upstream dependency (Booking) lands.
3. **Per-module owners** — Fadwa / Mohammad / Mahmoud / Tech Lead can each be primary on a different module without trampling.
4. **Smaller diffs at the task-doc level** — easier to amend a single module's WBS without rewriting the universe.
5. **Tracks repository structure** — the codebase is itself a modular monolith with one project quintet per module (`.Application/.Contracts/.Domain/.Infrastructure/.Presentation`); the planning artefacts now mirror that.

---

## 1. Six-folder Manifest

Each module is a **folder containing many small files** (each ≤ ~500 lines). This keeps individual reviews focused and prevents single-file edit lockouts. Open the `00-README.md` inside each folder to navigate that module's full WBS.

| # | Folder | Wave | Scope (one-line summary) | Owner Lead | Endpoints | BG Services | Hours |
|---|---|---|---|---|---|---|---|
| 1 | [`Booking/`](./Booking/00-README.md) | Wave 5 | Booking state machine, 5-step `POST /tour`, provider confirm/reject, cancel-refund, join-request, availability slots (+ bulk recurring), refund policies, provider documents | Mohammad | ~22 | 4 | ~190 |
| 2 | [`Finance/`](./Finance/00-README.md) | Wave 5 | Payments (initiate-escrow / webhook / refund), Payouts (admin-trigger + approve + tiered commission), Invoices (PDF), Commission CRUD | Mohammad | ~17 | 2 | ~170 |
| 3 | [`Social/`](./Social/00-README.md) | Wave 6 | Reviews (verified-booking gate, weighted Bayesian rating, edit-48h, provider reply), Favorites (toggle, 500-max, EntityType polymorphic), Reports + Moderation logs | Fadwa | ~22 | 1 | ~150 |
| 4 | [`Messaging/`](./Messaging/00-README.md) | Wave 6 | Notifications CRUD (cursor pagination, 13 types, preferences), Devices/Tokens (FCM/APNs), Support Tickets (SLA tiering, round-robin assignment), **NotificationHub SignalR** | Mohammad | ~16 | 2 | ~180 |
| 5 | [`Analytics/`](./Analytics/00-README.md) | Wave 6 | Interactions tracking (fire-and-forget, 7 types, BIGINT PK), Popular/Trending (top 50 + 7-day delta), Admin Dashboards (revenue/bookings/users), Provider analytics, Audit logs (2-year retention) | Mahmoud | ~10 | 1 | ~120 |
| 6 | [`Authorization-Cleanup/`](./Authorization-Cleanup/00-README.md) | Cross-cutting | §8.1 fix 8 `ICurrentUser` violations in ContentPlaces handlers + §8.2 fix 28 endpoint authorization violations across 5 Presentation projects + **`SeoRedirectMiddleware`** + integration test harness | Tech Lead | ~28 (refactor) | 0 | ~80 |
| **Total** | | | | | **~115** | **10** | **~890** |

Numbers approximate; per-folder `00-README.md` gives authoritative hour counts.

### Standard files inside each module folder

| File | Purpose |
|---|---|
| `00-README.md` | Scope, window, team, deliverable manifest, table of contents linking all sibling files |
| `01-pre-work.md` | PW-1..PW-N items Tech Lead must merge before kickoff Day-0 |
| `02-critical-rules.md` | Module-specific rules additive to this INDEX §4 |
| `03-entities-matrix.md` | Per-entity ownership: file, base class, IAggregateRoot?, events raised, owner |
| `04-task-*.md` ... `0N-task-*.md` | One file per TASK (each TASK = one cohesive feature slice) |
| `99-acceptance-gate.md` | Final Tech-Lead sign-off checklist (uses 99 prefix so it always sorts last) |

When a module is closed, the entire folder moves to `Agents/decisions/closed/{ModuleName}/`.

---

## 2. Dependency Graph & Suggested Run Order

```
                                ┌──────────────────────────┐
                                │  Current Sprint (W4):    │
                                │  Blogs + SEO (b1 ref)    │
                                │  Closes 2026-06-12       │
                                └────────────┬─────────────┘
                                             │
                                             ▼
                            ┌────────────────────────────────────┐
                            │  1️⃣  Booking                      │
                            │  Wave 5 — 2026-06-15 → 08-13       │
                            │  Hardest sprint (state machine,    │
                            │  optimistic concurrency, 5-step    │
                            │  POST /tour, 4 BG services)        │
                            └────────────┬───────────────────────┘
                                         │   emits booking events
                                         │   (BookingConfirmed,
                                         │    BookingCancelled,
                                         │    TourCompleted)
                       ┌─────────────────┴─────────────────┐
                       ▼                                   ▼
        ┌──────────────────────────┐         ┌───────────────────────────┐
        │  2️⃣  Finance            │         │  3️⃣  Social (parallel)    │
        │  Wave 5 — 2026-08-16 →  │         │  Wave 6 — 2026-08-16 →    │
        │  10-15 (8 wks)          │         │  10-08 (7 wks)            │
        │  Payments use Booking's │         │  Reviews require           │
        │  AwaitingPayment status │         │  BookingCompleted          │
        │  + IPaymentGateway      │         │  integration event         │
        └────────────┬─────────────┘         └────────────┬──────────────┘
                     │                                    │
                     └──────────────────┬─────────────────┘
                                        ▼
                       ┌──────────────────────────────────────┐
                       │  4️⃣  Messaging Phase 2              │
                       │  Wave 6 — 2026-10-19 → 12-17 (9 wks) │
                       │  Subscribes to Booking + Finance +   │
                       │  Social events for notifications.    │
                       │  Adds NotificationHub SignalR.       │
                       └────────────┬─────────────────────────┘
                                    │
                                    ▼
                       ┌──────────────────────────────────────┐
                       │  5️⃣  Analytics Phase 2              │
                       │  Wave 6 — 2026-12-20 → 02-25 (10 wk) │
                       │  Consumes interaction events from    │
                       │  all upstream modules. Dashboards.   │
                       └────────────┬─────────────────────────┘
                                    │
                                    ▼
                       ┌──────────────────────────────────────┐
                       │  6️⃣  Authorization Cleanup +        │
                       │     SeoRedirectMiddleware            │
                       │  Cross-cutting — 2027-02-28 →        │
                       │  05-27 (12 weeks final polish)       │
                       │  CAN run in parallel with 5️⃣        │
                       │  if Tech-Lead has capacity.          │
                       └──────────────────────────────────────┘
```

### Dependency Rationale

| Sprint | Depends on (HARD blocker) | Reason |
|---|---|---|
| 1 Booking | (none) | Foundation; consumes Tour entity (✅ already done in prior sprint) |
| 2 Finance | 1 Booking merged | `Payment.BookingId` FK references `booking.TourBookings`; webhook updates booking status |
| 3 Social | 1 Booking merged | `Review.BookingId` FK + verified-booking gate requires `BookingCompletedIntegrationEvent` |
| 4 Messaging Phase 2 | 1 Booking + 2 Finance + 3 Social merged | 13 notification types map 1:1 to integration events from upstream modules |
| 5 Analytics Phase 2 | 1 Booking + 2 Finance + 3 Social merged | Interaction tracking + dashboards consume events from all three |
| 6 Auth Cleanup | (none — pure refactor) | Touches existing endpoints only; can run concurrent with #5 |

### Parallel Opportunities

- **#2 + #3 can run in parallel** (mid-Aug → mid-Oct 2026). Same kickoff date. Different team leads (Mohammad on Finance, Fadwa on Social). Zero shared entities. Both depend on Booking only.
- **#6 can run in parallel with #5** (Feb → May 2027). Tech Lead drives #6; junior devs continue on #5. Zero overlap (refactor vs new code).

If both parallel opportunities are taken, the aggregate window shrinks from ~50 weeks → ~38 weeks, finishing ≈ 2027-03-10.

---

## 3. Aggregate Calendar (all sprints, gantt-style)

Working week = Sun → Thu (5 days). Hours/day/dev = 6 (after standups + reviews + buffer).

```
2026                                              2027
 J   J   A   S   O   N   D   J   F   M   A   M   J
 |   |   |   |   |   |   |   |   |   |   |   |   |
 [Curr.W4]
        [█████ Booking ████████]
                                [████ Finance ████████]
                                [████ Social █████]
                                                      [████ Messaging ██████]
                                                                            [████ Analytics ██████]
                                                                                                  [█ Auth-Clean █]
                                                                            ↑                     ↑
                                                                     hard deadline      MUST close ≤ 2027-05-27
```

**Hard deadline gates (project owner directive — "before end of May"):**
- 2027-05-15 — All six modules must have passed Final Acceptance Gate (every §N+2 sign-off completed)
- 2027-05-22 — All sprint files MUST be moved to `Agents/decisions/closed/` (single source of truth becomes `agent-context.md`)
- 2027-05-27 — Aggregate merge tag `v2.0.0-phase1-phase2-complete` cut

---

## 4. Cross-File Rules (apply to ALL six sprint files)

The following rules supersede anything in individual files. They mirror `agent-context.md §0.3` plus lessons from the W4 retro.

### 4.1 Authorization — Five Non-Negotiable Rules
1. Every endpoint MUST have `.WithMetadata(new MustHavePermissionAttribute({Module}Features.X, AppAction.Y))` OR `.AllowAnonymous()`. NEVER bare `.RequireAuthorization()` or string-based policies.
2. `ICurrentUser` is for **self/ownership comparisons only**. Never for `IsAuthenticated` checks (that's `MustHavePermission`'s job).
3. Commands return `Result<T>`. Never throw for business failures. Endpoints use `result.ToApiResult()`.
4. Every module owns its `IPermissionCatalog` in `{Module}.Contracts/Authorization/`.
5. Domain event handlers NEVER call `SaveChangesAsync`. UoW dispatches events BEFORE save; handler changes piggyback on aggregate's commit.

### 4.2 Caching — Mandatory Pattern
- Every query implements `ICacheableQuery` (provides `CacheKey`, `CacheTags`, `CacheExpiration`).
- Every command handler injects `HybridCache` + calls `RemoveByTagAsync` AFTER successful `SaveChangesAsync`.
- Use the **most specific tag** — `$"bookings:{userId}"` NOT `"bookings"`. Wildcard eviction wipes other users' caches.

### 4.3 Outbox / Integration Events — Mandatory Pattern
- `INotificationHandler<IDomainEvent>` writes `OutboxMessage` row to DbContext only. NEVER calls `SaveChangesAsync`.
- Logical event name convention: `{module-kebab}.{entity}.{verb}.v1` (e.g., `booking.tour-booking.confirmed.v1`).
- Every new integration event MUST be registered in `IntegrationEventTypeRegistry` (reverse-parity test prevents drift).
- Inbox consumers guard via `I{Module}InboxStore.HasBeenProcessedAsync` → do work → `MarkAsProcessed` → ONE `SaveChangesAsync`.

### 4.4 Identity & Time
- IDs: `Guid.CreateVersion7()` — never `Guid.NewGuid()` (UUIDv7 ordered for index locality).
- Timestamps: `DateTime.UtcNow` ONLY (or `IDateTimeProvider.UtcNow`). Never `DateTime.Now`.

### 4.5 Logging & Error Codes
- `ILogger<THandler>` mandatory in every handler (command + query).
- Error code format `{Entity}.{Reason}` in PascalCase (`TourBooking.SlotUnavailable`, `Payment.GatewayDeclined`).
- Use Error tuple form: `Result.Failure<T>(new Error("X.Code", "message"), Outcome.NotFound)`. NEVER use `Result.NotFound(string)` overloads (they populate Messages not Errors and break ProblemDetails per ERR-004).

### 4.6 Try/Catch Policy
Allowed try/catch:
- Infrastructure-only for external services (HTTP, SMTP, payment gateway, translation API).
- `DbUpdateConcurrencyException` in optimistic-concurrency retry loops.
- Background service loops (catch+log+continue).
- `OperationCanceledException when ct.IsCancellationRequested` (re-throw).

Forbidden:
- General `catch (Exception)` in endpoints, command handlers, query handlers.
- Empty `catch { }` anywhere.
- Defensive catches that swallow validation errors.

### 4.7 Build Lock Workaround
`YallaJo.Web.exe` (Roslyn Server) locks shared files during parallel builds → `CS2012`. Workaround: build only the changed projects via `dotnet build {Module}.{Project}.csproj` instead of full solution. Mandatory if multiple devs build simultaneously.

### 4.8 Inbox/Outbox Hygiene
- `OutboxCleaner` background service deletes processed rows older than 7 days.
- `InboxMessages` cleaned after 30 days.
- Escalate to Tech Lead if either table exceeds 100K rows (indicates downstream handler hang).

---

## 5. Per-File Reading Order

When you open a per-module file, read it in this order:

1. The **header banner block** (top 7 lines) for scope + endpoint count + hour budget.
2. **§0 Sprint Window** for hard deadlines.
3. **§2 Team Members & Allocation** to find your assignments.
4. **§3 Entity Ownership Matrix** to see which entities you touch.
5. **§5 Pre-Work (PW-*)** — these MUST land before kickoff Day-0; Tech Lead drives.
6. **§6 Critical Rules** (additive to this index's §4).
7. Your TASK section (§7, §8, …).
8. **§N+2 Final Acceptance Gate** to know what "done" looks like.

---

## 6. Project-Owner Sign-Off

| Role | Name | Signature | Date |
|---|---|---|---|
| Project Owner | _____________ | _____________ | _____________ |
| Tech Lead | _____________ | _____________ | _____________ |
| Module Lead: Booking | _____________ | _____________ | _____________ |
| Module Lead: Finance | _____________ | _____________ | _____________ |
| Module Lead: Social | _____________ | _____________ | _____________ |
| Module Lead: Messaging | _____________ | _____________ | _____________ |
| Module Lead: Analytics | _____________ | _____________ | _____________ |
| Module Lead: Auth-Cleanup | _____________ | _____________ | _____________ |

---

## 7. Reading Order Recap for New Joiners

1. `Agents/agent-context.md` — entire file (1748 lines) — the bible. §0.3 five non-negotiable rules are religion.
2. `Agents/guide.md` — code-pattern bible (~2162 lines). CQRS template, entity anatomy, EF configs.
3. `Agents/YallaJo.md` — product spec (1865 lines). 196 endpoints across 4 phases.
4. `Agents/tasks/ContentBlogs-ContentSeo-team-tasks.md` — the predecessor sprint (Wave 4 remainder).
5. **This file** (`Phase1-Phase2-Completion-INDEX.md`) — start here when picking up Phase 1/Phase 2 finishing work.
6. The per-module sprint file matching your assignment (see §1 manifest).
7. PDFs: `Endpoints.pdf` (sequenced waves), `YallaJo Business Rules & Edge Cases.pdf` (24 features, state machines, edge cases).
