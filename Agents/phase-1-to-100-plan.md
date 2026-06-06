# Phase 1 → 100% Plan

> **Status:** In progress
> **Current Phase 1 coverage:** 88% → target 100%
> **Scope note:** Real payment gateway (PSP) is **intentionally deferred**. `FakePaymentGateway` is the accepted stand-in for Phase 1 and is **not** treated as a gap.

---

## Context

`Agents/YallaJo.md` is a stale planning artifact (acknowledged as RISK-006). The actual codebase is far ahead of it: all 14 modules built, ~531 route registrations, 38 background services, full MVC frontend. Phase 1 is **surface-complete** but needs a few items to be genuinely production-ready and verifiable.

Key discovery during planning: the Booking "snapshot stubs" are **not** a big gap — real EF-backed readers already exist and DI auto-selects them outside Development. The remaining work is configuration, backfill, a capacity fix, additive middleware/handlers, and verification.

---

## Scope Decisions

| Item | Decision | Rationale |
|---|---|---|
| `FakePaymentGateway` | **Keep as-is** | Payments intentionally deferred (no PSP yet) |
| `StubBookingCommissionLookup` | **Defer → Phase 1.5** | Only matters at payout; no PSP means no payouts yet |
| `RateLimitExceptionHandler` (429) | **Defer** | Already emitted by rate-limiter `OnRejected`; trigger covered |
| `ExternalServiceExceptionHandler` (502) | **Defer** | No real external services exist pre-PSP |
| `CorrelationIdMiddleware` | **Accept existing** | `SecurityHeadersMiddleware` already adds `X-Correlation-ID` from `Activity.TraceId` |
| `NoOpDiscountEvaluator` | **Defer → Phase 4** | Discounts are a Phase 4 feature |

---

## Workstreams

### WS-1 — Activate real Booking snapshot readers `[critical path]`

**Reality:** Real readers (`BookingTourSnapshotReader`, `BookingProviderSnapshotReader`, `BookingPricingSnapshotReader`) exist at
`src/Modules/Booking/Booking.Infrastructure/Services/`, are EF-backed by Booking-owned snapshot tables, and are populated by existing event handlers
(`TourApprovedSnapshotHandler`, `TourUpdatedSnapshotHandler`, `TourPricingTierChangedSnapshotHandler`, `TourDeleted/SuspendedSnapshotHandler`).

DI toggle is in `src/Modules/Booking/Booking.Infrastructure/DependencyInjection.cs` (lines 80–109): config key `Booking:UseStubSnapshotReaders`. Stubs default in Development only; production uses real readers unless the flag forces stubs.

- [ ] **WS-1a** Set `Booking:UseStubSnapshotReaders = false` in non-Development config (verify Production `appsettings`).
- [ ] **WS-1b** Build a one-time **backfill** to resync tours approved *before* the snapshot handlers existed into `TourSnapshots`/provider/pricing tables (otherwise real readers return `null` → booking fails). Admin "resync snapshots" op or migration-seeded sync.
- [ ] **WS-1c** Fix `MaxGroupSize: int.MaxValue` TODO (lines 40–43 of `BookingTourSnapshotReader`) — carry `MaxGroupSize` on the `TourSnapshot` entity via a ContentTours event so capacity validation is real (currently effectively unlimited).
- [ ] **WS-1d** Integration test: ContentTours approve → event → snapshot row → `CreateTourBooking` reads real price/currency/instant-flag/capacity.

### WS-2 — Pipeline hardening middleware `[quick win]`

- [ ] **WS-2a** Add `ResponseCompression` (Brotli + Gzip) services + `UseResponseCompression`. Currently fully missing.
- [ ] **WS-2b** Add production CORS `YallaJoPolicy` with `AllowCredentials` + explicit origins (required for NotificationHub SignalR + YallaJo.Web frontend). Today only dev-only Swagger/LocalDev policies exist.
- [x] **WS-2c** CorrelationId — accept existing `SecurityHeadersMiddleware` TraceId approach (closed by decision).

### WS-3 — Exception handlers for correct status codes `[quick win]`

Only 3 handlers exist today (`GlobalExceptionHandler`, `DbUpdateExceptionHandler` 409, `ValidationExceptionHandler` 400). Add:

- [ ] **WS-3a** `NotFoundExceptionHandler` → 404
- [ ] **WS-3b** `ForbiddenExceptionHandler` → 403 (currently folded into GlobalExceptionHandler)
- [ ] **WS-3c** `ConflictExceptionHandler` → 409 (domain conflicts beyond DB unique-violation)

### WS-4 — Endpoint authorization violations (RISK-012)

- [ ] **WS-4** Resolve open items in `Agents/endpoint-violations.csv` — endpoints with missing/incorrect `RequireAuthorization`/role markers (🔒/👑/🏢).

### WS-5 — Golden-path verification `[acceptance gate]`

- [ ] **WS-5** End-to-end test on real (non-stub) wiring + FakePaymentGateway:
  `register → login → provider register → admin approve → provider creates tour (auto-translated) → snapshot propagates → user searches → books → fake-pays → webhook confirms → reviews`.

---

## Sequencing

1. **WS-2 + WS-3** — isolated quick wins, no dependencies (~1 day) ← *starting here*
2. **WS-1** — flip flag, backfill, capacity fix (~2–3 days, the real work)
3. **WS-4** — mechanical, parallelizable (~1 day)
4. **WS-5** — acceptance gate for everything above

**Estimate:** ~1 week to a verifiable Phase-1-at-100% (excluding any real PSP).

---

## Definition of Done

Phase 1 = 100% when: WS-1 (real snapshots + backfill + capacity), WS-2 (compression + CORS policy), WS-3 (404/403/409 handlers), WS-4 (auth violations resolved), and WS-5 (green golden-path test) are all complete.
