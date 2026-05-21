# Wave 5 — The Booking & Payment Engine

> **Sources:** `Agents/agent-context.md` §Wave 5 (Endpoints) + `Agents/guide.md` §4 (Booking), §5 (Payment/Refund), §19 (Dispute)
> **Dependencies:** Wave 4 (Tours, Pricing, Schedules)
> **Focus:** Availability slots, bookings, payments, refunds, payouts, invoices, commissions, join-requests, refund policies
> **Missing:** 13 P0 endpoints (~60h) — **second-largest gap after Wave 2**

---

## 1. Current Status

| Area | Built | Missing |
|---|---|---|
| Booking CRUD + state transitions | 9/9 ✅ | — |
| Payments (initiate, webhook, refund, reads) | 6/6 ✅ | — |
| Invoices | 4/4 ✅ | — |
| Payouts | 5/5 ✅ | — |
| Commissions CRUD | 4/4 ✅ | — |
| **Availability Slots** | **0/6** 🔴 | **6 endpoints missing** |
| **Refund Policies** | **0/3** 🔴 | **3 endpoints missing** |
| **Join Requests** | **0/3** 🔴 | **3 endpoints missing** |
| **Provider booking reads** | **1/4** ⚠️ | **3 endpoints missing** |

### 1.1 Availability Slots — Missing (6, ~16h)

| # | Method | Path |
|---|---|---|
| 1 | GET | `/api/v1/availability/{tourId}` |
| 2 | GET | `/api/v1/availability/{tourId}/{date:datetime}` |
| 3 | POST | `/api/v1/availability/slots` |
| 4 | PUT | `/api/v1/availability/slots/{id}` |
| 5 | DELETE | `/api/v1/availability/slots/{id}` |
| 6 | POST | `/api/v1/availability/slots/bulk` (recurring up to 90 days) |

### 1.2 Refund Policies — Missing (3, ~10h)

| # | Method | Path |
|---|---|---|
| 1 | GET | `/api/v1/refund-policies/{tourId}` |
| 2 | POST | `/api/v1/refund-policies` |
| 3 | PUT | `/api/v1/refund-policies/{id}` |

### 1.3 Join Requests — Missing (3, ~14h)

| # | Method | Path |
|---|---|---|
| 1 | POST | `/api/v1/bookings/join-request` |
| 2 | POST | `/api/v1/bookings/join-request/{id}/approve` |
| 3 | POST | `/api/v1/bookings/join-request/{id}/reject` |

### 1.4 Provider booking reads — Missing (3, ~8h)

| # | Method | Path |
|---|---|---|
| 1 | GET | `/api/v1/bookings/provider/pending` |
| 2 | GET | `/api/v1/bookings/provider/upcoming` |
| 3 | GET | `/api/v1/bookings/provider/history` |

---

## 2. Availability Slot Module (6 endpoints, ~16h)

### 2.1 Entity (already exists — verify)

`Booking.Domain/Entities/AvailabilitySlot.cs`:
```csharp
public sealed class AvailabilitySlot : AuditableEntity, IAggregateRoot
{
    public Guid TourId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int MaxCapacity { get; private set; }
    public int BookedCount { get; private set; }
    public int LockedCount { get; private set; }
    public decimal? PriceOverride { get; private set; }
    public bool IsBlackedOut { get; private set; }
    public byte[] RowVersion { get; set; } = [];  // optimistic concurrency

    public static AvailabilitySlot Create(...)
    public void IncrementBooked(int count)        // raises AvailabilitySlotCapacityChangedDomainEvent
    public void DecrementBooked(int count)        // on cancellation/refund
    public void IncrementLocked(int count)        // on SlotLock create
    public void DecrementLocked(int count)        // on SlotLock release
    public void UpdateCapacity(int newCapacity)   // raises capacity-changed event; cannot go below BookedCount
    public void BlackOut()                        // soft-disable
    public void ClearBlackOut()
}
```

### 2.2 Endpoints

- **GET `/availability/{tourId}`** — list all future slots with remaining capacity per date
- **GET `/availability/{tourId}/{date}`** — slots for specific date
- **POST `/slots`** — provider creates single slot; validates no overlap, capacity ≤ tour.MaxGroupSize
- **PUT `/slots/{id}`** — update capacity (cannot go below BookedCount per guide §2.4)
- **DELETE `/slots/{id}`** — only if no bookings (BookedCount==0 AND LockedCount==0)
- **POST `/slots/bulk`** — recurring pattern (daily/weekly/custom), max 90 days ahead, skip existing, return `{created, skipped}`

### 2.3 Business Rules (guide §4.3)
- ✅ Capacity check: `BookedCount + LockedCount < MaxCapacity` enforced at DB level
- ✅ Optimistic concurrency via RowVersion
- ✅ User cannot lock same slot twice — existing lock reused
- ✅ Lock cleanup every 5 minutes (already implemented as `SlotLockCleanupService`)

---

## 3. Refund Policy Module (3 endpoints, ~10h)

### 3.1 Entity (new in `Booking.Domain/Entities/`)

```csharp
public sealed class RefundPolicy : AuditableEntity, IAggregateRoot
{
    public Guid TourId { get; private set; }
    public int FullRefundHours { get; private set; }      // 100% refund if cancelled ≥X hours before
    public int PartialRefundHours { get; private set; }   // partial refund window (must be < FullRefundHours)
    public decimal PartialRefundPercent { get; private set; }  // 0-100; admin enforces ≥50% if 48h+

    public static RefundPolicy Create(Guid tourId, int fullHrs, int partialHrs, decimal partialPct)
    public void Update(int fullHrs, int partialHrs, decimal partialPct)
}
```

### 3.2 Endpoints

- **GET `/refund-policies/{tourId}`** — return policy or default (full refund up to 24h, 0% after)
- **POST `/refund-policies`** — provider sets policy (one per tour); admin enforces bounds: min 50% if 48h+, max 100%
- **PUT `/refund-policies/{id}`** — update; takes effect for NEW bookings only; existing bookings retain snapshot per guide §5.4

### 3.3 Refund Snapshot on Booking (already designed — guide §5.4)
At booking time, snapshot refund policy in `TourBooking.RefundPolicySnapshotJson` (already exists as RefundPolicy JSON column). When refund is requested, system uses snapshot — not current policy. **This is critical** — verify already implemented; if not, add migration.

### 3.4 Cancel Endpoint Integration
The existing `POST /bookings/{id}/cancel` uses the snapshot:
- ≥FullRefundHours before tour: 100% refund
- PartialRefundHours - FullRefundHours window: PartialRefundPercent
- <PartialRefundHours: 0%
- Provider cancels: always 100% (guide §5.2 — overrides snapshot)
- Force majeure (admin override): always 100%

### 3.5 Migration
`BookingAddRefundPolicyTable` (skip if already exists):
- `booking.RefundPolicies(Id, TourId UNIQUE, FullRefundHours, PartialRefundHours, PartialRefundPercent, CreatedAt, UpdatedAt, RowVersion)`

---

## 4. Join Requests Module (3 endpoints, ~14h)

### 4.1 Entity (new)

```csharp
public sealed class JoinRequest : AuditableEntity, IAggregateRoot
{
    public Guid BookingId { get; private set; }   // existing confirmed group booking
    public Guid RequesterUserId { get; private set; }
    public int ParticipantCount { get; private set; }
    public JoinRequestStatus Status { get; private set; }  // Pending, Approved, Rejected
    public DateTime RequestedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public Guid? RespondedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }

    public static JoinRequest Create(...)
    public void Approve(Guid providerUserId, TimeProvider)
    public void Reject(Guid providerUserId, string reason, TimeProvider)
}
```

### 4.2 Endpoints

- **POST `/bookings/join-request`** — body `{ bookingId, participantCount }`. Validate: booking.Status=Confirmed, tour allows join (Tour.AllowsJoinRequests bool), capacity remaining ≥ requested count. Lock for 10min during charge. Charge separately on approve.
- **POST `/{id}/approve`** — provider/owner only. Adds participant to booking (increment ParticipantCount + BookedCount on slot). Charges via payment gateway.
- **POST `/{id}/reject`** — provider only. Body `{ reason }`. Refunds any locked payment.

### 4.3 Tour entity update
Add `bool AllowsJoinRequests { get; private set; }` to Tour with default false. Provider toggles via existing PUT /tours/{id}.

### 4.4 Migration
`BookingAddJoinRequestTable` covers:
- `booking.JoinRequests` table
- `Tour.AllowsJoinRequests` column

---

## 5. Provider Booking Reads (3 endpoints, ~8h)

Currently `GET /bookings/admin/all` filterable. Add provider-scoped reads:

- **GET `/bookings/provider/pending`** — provider's bookings with Status=PendingConfirmation (non-instant awaiting their accept)
- **GET `/bookings/provider/upcoming`** — provider's Confirmed bookings with future tour date
- **GET `/bookings/provider/history`** — provider's Completed + Cancelled bookings (paginated)

Add to `Booking.Application/Queries/`:
- `GetProviderPendingBookingsQuery`
- `GetProviderUpcomingBookingsQuery`
- `GetProviderBookingHistoryQuery`

All filter by `tour.ProviderUserId == currentUser.UserId`. Reuse existing `BookingDto`.

---

## 6. Business Rules Cross-Reference (guide §4, §5, §19)

### 6.1 Booking (guide §4.3)
- ✅ Max 3 concurrent pending unpaid bookings/user
- ✅ Cannot book same tour same date twice
- ✅ Min 2-hour lead time
- ✅ ConfirmationCode format: `YJ-YYYYMM-XXXX`
- ✅ 10-min lock TTL → auto-cancel if no payment
- ✅ Instant bookings → Confirmed; non-instant → PendingConfirmation; provider 24h to accept else auto-confirm (ProviderAutoAcceptService BG)

### 6.2 Payment (guide §5)
- ✅ JOD primary + USD + EUR; locked at booking time
- ✅ Full upfront (no installments v1)
- ✅ Decimal(19,4) precision with currency column
- ✅ GatewayResponse JSON stored
- ✅ TransactionId-based idempotency on webhook
- ✅ Failed payment → no booking record; slot lock remains until TTL
- ✅ No raw card data stored (PCI DSS)

### 6.3 Refund (guide §5.2)
- ✅ Provider's policy snapshot used (not current policy)
- ✅ Admin min 50% if cancelled 48h+ before
- ✅ Provider cancels → always 100%
- ✅ Force majeure → admin override 100%
- ✅ Refund retries 3x via `RefundRetryService` BG
- ✅ Original payment method first; platform credit fallback

### 6.4 Payout (guide §5.3)
- ✅ 7-day escrow after completion
- ✅ Dispute filed during escrow → freeze payout
- ✅ Commission tiered: Free 15%, Basic 10%, Premium 7%, Enterprise custom
- ✅ Commission on discounted price (not original)
- ✅ Weekly Sunday midnight batch via `PayoutBatchingService` BG
- ✅ Min payout 10 JOD; below threshold rolls over
- ✅ Requires verified ProviderBankAccount

---

## 7. Authorization

Add to `BookingFeatures.cs`:
```csharp
public const string AvailabilitySlot = nameof(AvailabilitySlot);
public const string RefundPolicy = nameof(RefundPolicy);
public const string JoinRequest = nameof(JoinRequest);
```

Add to `BookingPermissionCatalog`:
- AvailabilitySlot × {Read, Create, Update, Delete} (provider scoped + admin)
- RefundPolicy × {Read, Create, Update}
- JoinRequest × {Create, Approve, Reject}

---

## 8. WBS

### 8.1 Sprint Plan (60h total — 4 weeks for 2 devs)

| Phase | Hours | Owner | Deliverable |
|---|---|---|---|
| 5A — Availability Slots | 16 | Backend A | 6 endpoints + bulk recurring + tests |
| 5B — Refund Policies | 10 | Backend B | 3 endpoints + snapshot integration + tests |
| 5C — Join Requests | 14 | Backend A | 3 endpoints + capacity integration + payment hook |
| 5D — Provider Reads | 8 | Backend B | 3 queries + endpoints |
| Migrations | 4 | Either | 3 migrations (slots already exist; just add JoinRequest + RefundPolicy if needed) |
| Cross-cutting tests | 8 | Both | 8 unit + 4 integration |

---

## 9. Acceptance Criteria

- [ ] All 15 new endpoints respond per PDF1
- [ ] Slot creation rejects overlap on same tour+date
- [ ] Slot bulk creation handles up to 90 days; skips existing; returns `{created, skipped}`
- [ ] Slot capacity reduction blocked if would drop below BookedCount
- [ ] Refund policy snapshot stored at booking time (`TourBooking.RefundPolicySnapshotJson`)
- [ ] Refund calculation uses snapshot (not current policy)
- [ ] Join request capacity check happens atomically (RowVersion concurrency)
- [ ] Provider-scoped reads filter correctly via `tour.ProviderUserId == currentUser.UserId`
- [ ] All endpoints use `MustHavePermissionAttribute`
- [ ] 3 new migrations applied cleanly
- [ ] `dotnet build` green for Booking.* + YallaJo.Api
- [ ] No regressions in existing Wave 5 tests (especially payment flow)
