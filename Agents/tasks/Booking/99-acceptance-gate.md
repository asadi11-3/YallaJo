# Booking Module — Final Acceptance Gate

> **Tech Lead signs off this checklist before declaring the Booking sprint closed (Sun 2026-08-09 17:00). Folder must NOT move to `Agents/decisions/closed/Booking/` until every box below is ticked.**

---

## 1. Code Quality (PR & build)

- [ ] All 7 task PRs (TASK 1..TASK 7) merged into `main`.
- [ ] `dotnet build` green for every project listed in `11-cross-cutting.md §4` (Booking.{Domain,Contracts,Application,Infrastructure,Presentation} + 2 test projects). Full-solution build green when `YallaJo.Web` is stopped.
- [ ] **No `<TreatWarningsAsErrors>` regressions** — Booking csproj group still inherits solution-wide setting.
- [ ] **Zero TODOs** in committed code search: `rg "TODO|FIXME|HACK" Booking/`. Any remaining is moved to a tracked GitHub issue.
- [ ] **All FluentValidation rules** for every new Command/Query have at least one passing `…ValidatorTests` test.
- [ ] **All command handlers** inject `ILogger<THandler>` and call `RemoveByTagAsync` after successful SaveChanges (INDEX §4 R10 — sample 3 handlers per code reviewer).
- [ ] **All queries** implement `ICacheableQuery` with key + tag (sample 3).
- [ ] **No `ICurrentUser` in handlers** EXCEPT for the ownership-comparison handlers documented in 02-critical-rules.md §B-R11 (audit list).
- [ ] **No bare `RequireAuthorization()`** in any new Presentation endpoint — `rg "RequireAuthorization\(\)\s*$" Booking/Booking.Presentation/` returns 0 matches.
- [ ] `Booking.Tests.Unit` green — minimum **80 tests** (rough lower bound: 16 availability + 14 booking engine + 14 cancel + 12 join + ~24 misc).
- [ ] `Booking.IntegrationTests` green — minimum **15 tests** (4 outbox round-trip + 4 BG live-test + 4 endpoint smoke + 3 cross-module inbox).
- [ ] **Permission catalog parity test passes** (11-cross-cutting.md §3 — `IntegrationEventTypeRegistryParityTests`).

---

## 2. Endpoint Smoke Test (manual Postman/HTTP REPL)

A reviewer (Mohammad recommended) runs through this checklist live, recording results in a runbook file `Booking/_smoke-test-runbook.md` (deleted before folder moves to closed/).

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/availability/slots` | 201 + slot DTO | Mahmoud T1 |
| 2 | POST | `/api/v1/availability/slots/bulk` | 202 + `{createdCount, skippedDates, totalRequested}` | Mahmoud T1, 90-day window |
| 3 | DELETE | `/api/v1/availability/slots/{id}` | 204 | T1, only when no bookings |
| 4 | DELETE | `/api/v1/availability/slots/{id}` | 409 `AvailabilitySlot.HasBookings` | T1, has bookings |
| 5 | GET | `/api/v1/availability/{tourId}` | 200 + list, cached | T1 |
| 6 | POST | `/api/v1/refund-policies` | 201 | Fadwa T2, tier validation |
| 7 | POST | `/api/v1/refund-policies` | 400 `RefundPolicy.InvalidTiers` | T2, strict-decreasing breached |
| 8 | GET | `/api/v1/refund-policies/{tourId}` | 200 + default `{tiers: [{24,100}], isDefault:true}` when none set | T2 |
| 9 | POST | `/api/v1/provider/documents` | 201 + doc DTO | Fadwa T3 |
| 10 | POST | `/api/v1/provider/documents` | 413 `ProviderDocument.FileTooLarge` | T3, > 10MB |
| 11 | POST | `/api/v1/provider/documents` | 400 `ProviderDocument.UnsupportedType` | T3, .docx |
| 12 | POST | `/api/v1/booking/tour` | 201 + `{bookingId, reference YJ-…, paymentToken: "PENDING_FINANCE_INTEGRATION"}` | Mohammad T4 happy path |
| 13 | POST | `/api/v1/booking/tour` | 409 `AvailabilitySlot.CapacityConflict` | T4, parallel-write reproduction |
| 14 | POST | `/api/v1/booking/tour` | 422 `TourBooking.TooEarly` | T4, < 2h lead time |
| 15 | POST | `/api/v1/booking/tour` | 422 `TourBooking.DuplicateForDate` | T4, second booking same tour same date |
| 16 | POST | `/api/v1/booking/tour` | 422 `TourBooking.ConcurrentLimit` | T4, 4th AwaitingPayment |
| 17 | GET | `/api/v1/booking/{id}` | 200 owner OR provider OR admin | T4 |
| 18 | GET | `/api/v1/booking/{id}` | 403 | T4, neither owner nor provider |
| 19 | GET | `/api/v1/booking/my-bookings` | 200 + cursor envelope | T4 |
| 20 | GET | `/api/v1/admin/booking/all` | 200 + filters work | T4 |
| 21 | POST | `/api/v1/booking/{id}/confirm` | 200, ConfirmationSource=Manual | Mohammad T5 |
| 22 | POST | `/api/v1/booking/{id}/reject` | 200, reason ≥ 10 chars | T5 |
| 23 | POST | `/api/v1/booking/{id}/cancel` | 200 + refund% calc per policy snapshot | T5 |
| 24 | POST | `/api/v1/booking/{id}/complete` | 200 only after slot.StartTime | T5 |
| 25 | POST | `/api/v1/admin/booking/{id}/force-refund` | 200 + admin reason required | T5 |
| 26 | POST | `/api/v1/booking/{id}/join-request` | 201 | Fadwa T6 |
| 27 | POST | `/api/v1/booking/join-request/{id}/approve` | 200 | T6 |
| 28 | POST | `/api/v1/booking/join-request/{id}/reject` | 200 + reason ≥ 10 chars | T6 |

**Any RED row above blocks sign-off.** Reviewer reproduces, files bug, owner fixes within 24h.

---

## 3. Outbox / Inbox Round-Trip

Each row below proves a domain action → integration event → downstream side-effect. Use SQL Server Profiler or `dotnet user-secrets`-driven OTEL exporter to trace.

| Action | Outbox row | Logical name | Downstream side-effect | SLA |
|---|---|---|---|---|
| POST /booking/tour succeeds (T4) | `booking.OutboxMessages` 1 row | `booking.tour-booking.created.v1` | Analytics inbox writes UserInteraction(BookingStarted); Messaging inbox writes Notification "Awaiting payment" | 30s |
| Provider confirms (T5) | 1 row | `booking.tour-booking.confirmed.v1` | Messaging sends confirmation email + bell; Finance prepares escrow record | 30s |
| User cancels with refund 50% (T5) | 1 row | `booking.tour-booking.cancelled.v1` | Finance enqueues refund; Messaging notifies; Analytics increments cancellation counter; Availability handler restores capacity | 30s |
| BG: SlotLock TTL expires (T7) | 1 row per lock | `booking.slot-lock.expired.v1` | Capacity restored locally (intra-module); Analytics audit log only | 15 min worst-case |
| BG: AwaitingPayment auto-expires (T7) | 1 row | `booking.tour-booking.payment-expired.v1` | Messaging notifies; capacity restored | 15 min worst-case |
| BG: 24h auto-confirm (T7) | 1 row | `booking.tour-booking.confirmed.v1` w/ Source=Auto | Messaging notifies user + provider; Finance proceeds | 15 min worst-case |
| BG: CRITICAL doc expires (T7) | 1 row | `booking.provider.suspended-doc-expired.v1` | Accounts inbox suspends provider; Messaging notifies; ContentTours hides provider's tours | 24h SLA (daily cron) |
| Join request approved (T6) | 1 row | `booking.join-request.approved.v1` | Messaging notifies requester + booking owner; Finance prepares separate payment intent | 30s |

**Failure** = outbox row `ProcessedAt IS NULL` after SLA elapses, OR downstream consumer didn't write expected row. Tech Lead investigates `OutboxProcessor` logs.

---

## 4. Background Services Live Test (24h soak)

Run in pre-prod environment with realistic load (10× current dev workload). Verify after 24h:

- [ ] `SELECT COUNT(*) FROM booking.SlotLocks WHERE IsActive = 1 AND ExpiresAt < SYSUTCDATETIME()` → **0** (cleanup working).
- [ ] `SELECT COUNT(*) FROM booking.TourBookings WHERE Status = 'AwaitingPayment' AND CreatedAt < DATEADD(MINUTE, -10, SYSUTCDATETIME())` → **0** (auto-expire working).
- [ ] `SELECT COUNT(*) FROM booking.TourBookings WHERE Status = 'PendingConfirmation' AND TransitionedToPendingAt < DATEADD(HOUR, -24, SYSUTCDATETIME())` → **0** (auto-confirm working).
- [ ] `SELECT COUNT(*) FROM booking.ProviderDocuments WHERE Status = 'Approved' AND ExpiresAt < SYSUTCDATETIME() AND ExpiryProcessed = 0` → **0** (daily check ran).
- [ ] OTEL: `bg_service_failures_total` for every service = **0**.
- [ ] OTEL: `bg_service_ticks_total{service=SlotLockCleanupService}` ≈ 288 (24h ÷ 5min).
- [ ] OTEL: `bg_service_ticks_total{service=DocumentExpiryCheckService}` = **1** (daily).
- [ ] No `[ERROR]`-level log entries from any of the 4 BG services in 24h Serilog file.

---

## 5. Performance Sanity (p95 latency thresholds)

Run JMeter or k6 with 50 concurrent users for 5 minutes against pre-prod:

| Endpoint | p95 target | Hard ceiling | Notes |
|---|---|---|---|
| POST `/booking/tour` | < 250 ms | < 500 ms | Hot path: snapshot lookups + 1 SaveChanges + outbox enqueue |
| GET `/booking/{id}` | < 80 ms (cached) | < 200 ms | Cache 5 min |
| GET `/booking/my-bookings` | < 150 ms (cached) | < 400 ms | Cache 1 min, cursor pagination |
| GET `/availability/{tourId}` | < 100 ms (cached) | < 250 ms | Cache 5 min |
| GET `/availability/{tourId}/{date}` | < 80 ms (cached) | < 200 ms | More-specific tag |
| POST `/booking/{id}/cancel` | < 200 ms | < 500 ms | Refund calc + 1 SaveChanges + outbox |
| POST `/refund-policies` | < 150 ms | < 350 ms | Single tour upsert |
| POST `/provider/documents` upload 5 MB | < 1.5 s | < 3 s | Encryption + ContentCore call |

**SQL profiler:** no individual query > 100 ms during the test (excluding the upload). Slowest expected query: snapshot lookup in POST /tour Step 1.

---

## 6. Documentation Hygiene

- [ ] **Every new endpoint** has XML doc `///` summary + `<param>` + `<returns>` on the Application Command/Query record (auto-flows to Swagger via Swashbuckle).
- [ ] **All 26 Booking permissions** listed in `Agents/permissions-inventory.md` (create file if missing). Format: `Booking.{Feature}.{Action}` + 1-line description.
- [ ] **Sprint file moves to `Agents/decisions/closed/Booking/`** with this exact command:
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Booking" -Destination "Agents\decisions\closed\Booking"
  ```
  Master `Phase1-Phase2-Completion-INDEX.md` §1 row for Booking gets a 🟢 status and link updated to closed/ path. Predecessor sprint's row (ContentBlogs-ContentSeo) is the template.
- [ ] **`AGENTS.md` (repo-root)** gets a new line entry for `Booking/` module: 5-project summary + responsible Tech Lead + last-updated date.
- [ ] **All Booking-module `## 0 — Module status` rows in `agent-context.md §11.1`** updated from ⬜ Empty / 🟡 Partial to ✅ Complete.
- [ ] **`Agents/error-log.md`** — new entries for any gotchas hit during the sprint (especially RowVersion / SaveChanges-after-await / inbox idempotency surprises). Each entry follows existing format: trigger → root cause → prevention rule.
- [ ] **`Agents/decisions/`** — new ADR if any architectural decision was made (e.g. "ADR-006: Commission CRUD lives in Finance not Booking — Booking holds read-snapshots").

---

## 7. Sprint Retro & Demo (Fri 2026-08-14 11:00 AST)

15-min demo by Mohammad walking through:
1. Live POST /booking/tour with happy path + parallel-write conflict reproduction.
2. State machine demo: confirm, then cancel with refund calc, then auto-expire.
3. BG service dashboard: 4 services ticking in real time (Seq/Grafana view).
4. Outbox row → Messaging consumer write demo for confirmation email.

Retro doc lives at `Agents/decisions/closed/Booking/_retro.md`:
- What went well (≥ 3 items)
- What hurt (≥ 3 items + linked error-log.md entries)
- Action items for the NEXT sprint (Finance — likely scheduled Mon 2026-08-17 kickoff)

---

## 8. Sign-Off Block

| Role | Name | Date | Signature |
|---|---|---|---|
| TASK 1 owner | Mahmoud | _____ | _____ |
| TASK 2 owner | Fadwa | _____ | _____ |
| TASK 3 owner | Fadwa | _____ | _____ |
| TASK 4 owner | Mohammad | _____ | _____ |
| TASK 5 owner | Mohammad | _____ | _____ |
| TASK 6 owner | Fadwa | _____ | _____ |
| TASK 7 owner | Mahmoud | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |

**Once all signatures collected, folder moves; index updates; module marked ✅ in agent-context.md §11.1; Finance sprint kickoff scheduled.**
