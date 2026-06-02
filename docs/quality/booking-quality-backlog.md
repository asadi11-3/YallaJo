# Booking — Quality Review Backlog

**Module:** Booking
**Source:** Full Module Quality Review (discovery-only pass)
**Purpose:** Record the accepted findings from the Booking full quality review so they can be scheduled and actioned in future phases. This file is a backlog record only — no application code, migrations, or existing docs were changed as part of creating it.

> Scope note: BK-04 is the remaining active backlog item. BK-01 was resolved (see "Resolved in this pass" below). BK-02, BK-05, BK-06, and BK-07 are recorded under "Design Choices / No Action Now" for traceability. BK-03 (standardize permission-name construction) is intentionally **not** tracked as an active backlog item.

---

## Resolved in this pass

* **BK-01 — Suspension cancel handlers catch the wrong exception type / poison-message risk.**
  `ProviderSuspendedCancelBookingsHandler` and `GuideSuspendedCancelBookingsHandler` now catch `BusinessRuleViolationException` (the `DomainException`-derived type that `TourBooking.Cancel` actually throws for terminal-state bookings) instead of `InvalidOperationException`. A booking that becomes terminal between the batch query and the `Cancel()` call is now logged and skipped, and the rest of the batch still commits — so duplicate delivery / concurrent terminal transitions no longer poison outbox retries. Batching, outbox/event, and logging behavior are unchanged. Covered by new regression tests in `tests/Booking.Tests.Unit/EventHandlers/SuspensionCancelBookingsHandlerTests.cs`.

---

## Active Backlog Items

### BK-04 — `ResolveRefundPercentage` JSON parsing swallows malformed snapshots as 0% refund
- **Phase:** Domain Events / Reliability phase
- **Status:** Deferred
- **Priority:** Low
- **Note:** Money-affecting behavior — confirm intended fallback before implementation. Needs approval.
- **Summary:** `CancelTourBookingCommandHandler.ResolveRefundPercentage` parses the booking's `RefundPolicySnapshot` JSON and returns `0m` on `JsonException`, a missing `tiers` array, or an empty `{}` snapshot.
- **Impact:** If a snapshot is ever malformed or empty for a user-initiated cancellation, the customer silently receives a **0% refund** with no error surfaced. Snapshots are normally well-formed (written at booking creation), so likelihood is low — but it is a silent, money-affecting failure mode.
- **Recommended action:** Confirm the intended fallback. Consider logging a warning and/or failing closed (require an explicit, parseable policy) rather than silently defaulting to 0%.

---

## Design Choices / No Action Now

> The items below were reviewed and are considered acceptable design choices or forward-compatibility placeholders. They are recorded for traceability and are **not** scheduled for change at this time.

### BK-02 — `SlotLock` lacks RowVersion / TTL release path is unguarded by concurrency
- **Severity:** Low · **Confidence:** Likely (verify intent)
- **Observation:** `SlotLock : BaseEntity` and `SlotLockConfiguration` define no `IsRowVersion()`. Capacity correctness is anchored on `AvailabilitySlot.RowVersion` (the source of truth); `SlotLock` is a secondary record, and cleanup is handled by `SlotLockCleanupService` plus the clamped restore helpers.
- **Why no action:** The slot-level RowVersion plus idempotent (clamped) restore make double-release a safe no-op. Treated as the intended single concurrency anchor. Document as a design choice if confirmed.

### BK-05 — `GetAllBookings` `paymentStatus` filter is an accepted no-op
- **Severity:** Low (Info) · **Confidence:** Confirmed (documented)
- **Observation:** `TourBookingEndpoints.MapGetAllBookingsEndpoint` accepts `paymentStatus` and its own description states it "is accepted as a hint but is currently a no-op until the Finance cross-module payment projection ships."
- **Why no action:** Intentional forward-compatibility placeholder. Track alongside the Finance payment-projection work; optionally annotate as not-yet-implemented in OpenAPI.

### BK-06 — No InboxStore; cross-module idempotency relies on state guards
- **Severity:** Low (Info) · **Confidence:** Confirmed (design choice)
- **Observation:** `PaymentCompletedConfirmBookingHandler` and the suspension-cancel handlers explicitly document that the Booking module has no InboxStore and rely on `BookingStatus` state guards for idempotency, unlike ContentPlaces/ContentTours which use an `IInboxStore`.
- **Why no action:** Sound for naturally state-idempotent handlers. The only exposure is where a guard is bypassed by a code defect (see BK-01). If standardization is desired later, confirm state-guard idempotency as the module standard or add an InboxStore for parity.

### BK-07 — `CreateTourBooking` performs many sequential reads before the lock
- **Severity:** Low · **Confidence:** Likely (acceptable by design)
- **Observation:** `CreateTourBookingCommandHandler` issues a sequence of awaited reads (tour snapshot, provider snapshot, slot, duplicate-for-date, concurrent-unpaid count, and per-tier pricing snapshots in a loop) before locking and saving.
- **Why no action:** Correctness is guaranteed by the final RowVersion check; for typical small tier counts the round-trips are acceptable. Revisit only if booking throughput becomes a hotspot (batch tier-price lookups and combine validation reads).
