# TASK 4 — Booking Engine (POST /tour 5-step flow + GET endpoints)

**Owner:** Mohammad (Intermediate, sprint lead)
**Endpoints:** 4
**Estimated hours:** 48
**Earliest start:** Tue 2026-06-30 (after TASK 1 + TASK 2 merge minimum)
**Hard PR deadline:** Wed 2026-07-29 17:00
**Dependencies:** TASK 1 (AvailabilitySlot domain methods), TASK 2 (RefundPolicy + ICommissionLookupService impl), PW-1..PW-8.

This is the largest single task in the sprint. It implements the core booking creation flow that everything else hangs off of.

---

## Endpoint list

| # | Method + Path | Permission | Returns | Errors |
|---|---|---|---|---|
| 1 | `POST /api/v1/booking/tour` | `TourBooking + Create` | 201 + `{id, reference, status, totalAmount, currency, expiresAt, paymentToken}` | 400, 403, 404, 409 (capacity, duplicate, concurrent limit, suspended provider), 422 (validation) |
| 2 | `GET /api/v1/booking/{id}` | `TourBooking + Read` (owner OR provider OR admin) | 200 + full DTO | 403, 404 |
| 3 | `GET /api/v1/booking/my-bookings` | `TourBooking + Read` (self, cursor paginated) | 200 + `{items, nextCursor}` | — |
| 4 | `GET /api/v1/booking/admin/all` | `AdminBookingDashboard + Read` | 200 + paginated, with filter query params | 403 |

---

## The 5-step POST /tour flow

```mermaid
flowchart TD
    A[POST /tour request] --> B[Step 1: Validate availability]
    B -->|fail| FAIL[Return 4xx]
    B -->|ok| C[Step 2: Lock slot (RowVersion + insert SlotLock)]
    C -->|conflict| FAIL
    C -->|ok| D[Step 3: Calculate pricing + commission + discount stub]
    D --> E[Step 4: Create TourBooking aggregate AwaitingPayment]
    E --> F[Step 5: Raise TourBookingCreated event, return reference + paymentToken]
    F --> G[Outbox: booking.tour-booking.created.v1]
    G --> H[Finance receives → creates Payment Pending → returns gateway URL]
```

### Step 1 — Validate availability

Reads:
- `BookingTourSnapshot` for `tourId`: must exist, `IsActive=true`, `Status=Approved`.
- `BookingProviderSnapshot` for `tour.ProviderId`: must exist, `Status != Suspended`. If suspended → `Result.Failure(new Error("Booking.ProviderSuspended", "Provider is suspended"), Outcome.Conflict)`.
- `AvailabilitySlot` for `tourId + date + slotId`: must exist, `IsActive=true`, `AvailableCount >= participantCount`.
- Lead time: `(slot.StartTime - DateTime.UtcNow).TotalHours >= 2`. Else `TourBooking.TooEarly`.
- Duplicate check: `ITourBookingRepository.HasActiveBookingForTourOnDateAsync(userId, tourId, slot.Date)` → false. Else `TourBooking.DuplicateForDate`.
- Concurrent unpaid check: `ITourBookingRepository.CountActiveAwaitingPaymentByUserAsync(userId) < 3`. Else `TourBooking.ConcurrentLimit`.

### Step 2 — Lock slot (transactional)

```csharp
// Inside command handler, wrap in execution strategy + transaction
await using var tx = await context.Database.BeginTransactionAsync(ct);
try
{
    var slot = await slots.GetForUpdateAsync(cmd.SlotId, ct); // includes RowVersion
    var lockResult = slot.Lock(cmd.ParticipantCount);
    if (lockResult.IsFailure) { await tx.RollbackAsync(ct); return Result.Failure<...>(lockResult.Error, lockResult.Outcome); }

    var slotLock = SlotLock.Create(
        userId: currentUser.UserId,
        availabilitySlotId: slot.Id,
        bookingId: null, // not yet known
        ttl: TimeSpan.FromMinutes(10)
    );

    slots.Update(slot);
    slotLocks.Add(slotLock);

    // Continue with steps 3+4 BEFORE commit so all 4 atomically saved
    var booking = ... // step 3+4 create
    bookings.Add(booking);

    await uow.SaveChangesAsync(ct); // raises 1+ domain events + writes outbox row + commits
    await tx.CommitAsync(ct);
}
catch (DbUpdateConcurrencyException) // RowVersion mismatch on AvailabilitySlot
{
    await tx.RollbackAsync(ct);
    return Result.Failure<...>(new Error("AvailabilitySlot.CapacityConflict", "Slot was modified by another booking"), Outcome.Conflict);
}
```

### Step 3 — Calculate pricing

```csharp
// Inputs
var tier = await pricingSnapshots.GetByTourAndTypeAsync(tourId, TierType.Adult, ct);
var basePricePerAdult = tier?.Price ?? snapshot.BasePrice; // fallback to tour base if no tier

var lineItems = new List<BookingLineItem>
{
    new(TierType.Adult, count: cmd.AdultCount, unitPrice: basePricePerAdult, currency: snapshot.Currency)
};

if (cmd.ChildCount > 0)
{
    var childTier = await pricingSnapshots.GetByTourAndTypeAsync(tourId, TierType.Child, ct)
        ?? throw new InvalidOperationException("ChildCount > 0 but no Child pricing tier configured.");
    lineItems.Add(new(TierType.Child, cmd.ChildCount, childTier.Price, snapshot.Currency));
}
// ... Infant, Senior, Group, Private similarly

var subtotal = lineItems.Sum(li => li.UnitPrice * li.Count);

// Discounts (stub for this sprint)
var discount = await discountEvaluator.EvaluateAsync(
    new DiscountEvaluationContext(currentUser.UserId, tourId, subtotal, snapshot.Currency, cmd.PromoCode),
    ct
);
var afterDiscount = subtotal - discount.AppliedAmount;

// Loyalty (stub — also Finance sprint; for now skip)
var afterLoyalty = afterDiscount; // - loyaltyDeduction once Finance ships

// Platform minimum 5 JOD enforcement (per PDF 2 §24)
if (snapshot.Currency == "JOD" && afterLoyalty < 5m)
    return Result.Failure<...>(new Error("TourBooking.BelowPlatformMinimum", "Final price must be >= 5 JOD"), Outcome.Validation);

// Commission breakdown (stamped onto booking; not exposed to user)
var commission = await commissions.GetCommissionForProviderAsync(snapshot.ProviderId, afterDiscount, snapshot.Currency, ct);
// commission.Rate, commission.Amount, commission.TierName stamped

var pricing = new BookingPricing(
    Subtotal: subtotal,
    DiscountAmount: discount.AppliedAmount,
    LoyaltyAmount: 0m,
    TotalAmount: afterLoyalty,
    CommissionRate: commission.Rate,
    CommissionAmount: commission.Amount,
    Currency: snapshot.Currency,
    LineItems: lineItems
);
```

### Step 4 — Create TourBooking aggregate

```csharp
var reference = await referenceGenerator.GenerateAsync(ct);
var booking = TourBooking.Create(
    userId: currentUser.UserId,
    tourId: tourId,
    providerId: snapshot.ProviderId,
    availabilitySlotId: slot.Id,
    participantCount: cmd.ParticipantCount,
    pricing: pricing,
    reference: reference,
    refundPolicySnapshot: refundPolicyJsonSnapshot, // serialized to JSON column
    isInstantBooking: snapshot.IsInstantBooking,
    paymentExpiresAt: DateTime.UtcNow.AddMinutes(10)
);
// booking.Status = AwaitingPayment
// booking raises TourBookingCreatedDomainEvent (→ integration event)

bookings.Add(booking);
slotLock.AttachBookingId(booking.Id); // back-fill the FK on the lock

await uow.SaveChangesAsync(ct); // atomic
```

### Step 5 — Return payment token

The endpoint response includes a `paymentToken` placeholder for now. Once Finance sprint lands, Finance's `IPaymentGateway` returns a real token via separate `POST /payments/initiate` call. **In THIS sprint** the response token is just a string like `"PENDING_FINANCE_INTEGRATION"`. Client must call `POST /payments/initiate` separately.

Optionally: dispatch `booking.tour-booking.created.v1` integration event so Finance can pre-create a Payment record. **DEFER:** Finance not yet shipped; outbox row still written and Finance inbox handler will catch up.

---

## Endpoint contracts

### Request body for POST /tour

```json
{
  "tourId": "<guid>",
  "availabilitySlotId": "<guid>",
  "participantBreakdown": {
    "adult": 2,
    "child": 1,
    "infant": 0,
    "senior": 0
  },
  "promoCode": null,
  "loyaltyPointsToRedeem": 0
}
```

`participantCount` = sum of breakdown values.

### Response 201

```json
{
  "id": "<guid>",
  "reference": "YJ-20260701-A7X3K9",
  "status": "AwaitingPayment",
  "tourId": "<guid>",
  "tourTitle": "Petra Half-Day Tour",
  "availabilitySlot": {
    "id": "<guid>",
    "date": "2026-07-15",
    "startTime": "09:00",
    "endTime": "13:00"
  },
  "participantCount": 3,
  "pricing": {
    "subtotal": 90.00,
    "discountAmount": 0.00,
    "loyaltyAmount": 0.00,
    "totalAmount": 90.00,
    "currency": "JOD",
    "lineItems": [
      { "tierType": "Adult", "count": 2, "unitPrice": 30.00 },
      { "tierType": "Child", "count": 1, "unitPrice": 30.00 }
    ]
  },
  "paymentExpiresAt": "2026-07-01T14:23:44Z",
  "paymentToken": "PENDING_FINANCE_INTEGRATION"
}
```

> Commission breakdown is NOT in the user-facing DTO. It's stored on the aggregate for payout calculation later.

### GET /{id}

Same shape minus `paymentToken`. Adds:
- `payments: [...]` (empty until Finance ships)
- `cancellation: { source, reason, refundAmount, refundedAt }` if cancelled
- `completion: { completedAt, completionSource }` if completed

### GET /my-bookings query params

- `status?: BookingStatus` (one or many comma-separated)
- `fromDate?: date` / `toDate?: date` (slot date range)
- `tourId?: guid`
- `cursor?: string` / `pageSize?: int` (1..50)
- `countTotal?: bool` (default false)

### GET /admin/all query params

Same as my-bookings PLUS:
- `userId?: guid`
- `providerId?: guid`
- `paymentStatus?: string` (filters by joined Payment status — leave NULL-safe until Finance ships)

---

## Domain method: `TourBooking.Create`

```csharp
public static TourBooking Create(
    Guid userId,
    Guid tourId,
    Guid providerId,
    Guid availabilitySlotId,
    int participantCount,
    BookingPricing pricing,
    BookingReference reference,
    string refundPolicySnapshot,
    bool isInstantBooking,
    DateTime paymentExpiresAt)
{
    if (participantCount < 1) throw new DomainInvariantException("ParticipantCount must be at least 1.");
    if (pricing.TotalAmount <= 0) throw new DomainInvariantException("TotalAmount must be positive.");

    var booking = new TourBooking
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        TourId = tourId,
        ProviderId = providerId,
        AvailabilitySlotId = availabilitySlotId,
        ParticipantCount = participantCount,
        Subtotal = pricing.Subtotal,
        DiscountAmount = pricing.DiscountAmount,
        LoyaltyAmount = pricing.LoyaltyAmount,
        TotalAmount = pricing.TotalAmount,
        Currency = pricing.Currency,
        CommissionRate = pricing.CommissionRate,
        CommissionAmount = pricing.CommissionAmount,
        LineItemsJson = JsonSerializer.Serialize(pricing.LineItems),
        Reference = reference.Value,
        RefundPolicySnapshot = refundPolicySnapshot,
        IsInstantBooking = isInstantBooking,
        Status = BookingStatus.AwaitingPayment,
        PaymentExpiresAt = paymentExpiresAt
    };

    booking.RaiseDomainEvent(new TourBookingCreatedDomainEvent(
        booking.Id, booking.UserId, booking.TourId, booking.ProviderId,
        booking.AvailabilitySlotId, booking.ParticipantCount, booking.TotalAmount,
        booking.Currency, booking.Reference, booking.IsInstantBooking
    ));

    return booking;
}
```

State-mutating methods (`Confirm`, `Cancel`, `Reject`, `Complete`) live in TASK 5 (`08-task-confirm-reject-cancel.md`).

---

## Cache

- `GET /my-bookings` → `ICacheableQuery` tag `"bookings:user:{userId}"`, TTL 1 min (high churn).
- `GET /{id}` → tag `"booking:{id}"`, TTL 5 min.
- `GET /admin/all` → tag `"bookings:admin"`, TTL 30s (high churn, but small TTL keeps admin dashboard fresh).
- Cache invalidation in POST /tour: invalidate `bookings:user:{userId}` and `availability:tour:{tourId}` and `availability:tour:{tourId}:date:{date}`. After SaveChanges.

---

## Validators

```csharp
RuleFor(x => x.TourId).NotEmpty();
RuleFor(x => x.AvailabilitySlotId).NotEmpty();
RuleFor(x => x.ParticipantBreakdown.Adult).GreaterThanOrEqualTo(1).WithMessage("At least one adult required.");
RuleFor(x => x.ParticipantBreakdown.Child).GreaterThanOrEqualTo(0);
RuleFor(x => x.ParticipantBreakdown.Infant).GreaterThanOrEqualTo(0);
RuleFor(x => x.ParticipantBreakdown.Senior).GreaterThanOrEqualTo(0);
RuleFor(x => x.PromoCode).MaximumLength(20).Matches("^[A-Z0-9]+$").When(x => !string.IsNullOrEmpty(x.PromoCode));
RuleFor(x => x.LoyaltyPointsToRedeem).GreaterThanOrEqualTo(0);
RuleFor(x => x).Custom((cmd, ctx) =>
{
    var total = cmd.ParticipantBreakdown.Adult + cmd.ParticipantBreakdown.Child + cmd.ParticipantBreakdown.Infant + cmd.ParticipantBreakdown.Senior;
    if (total < 1) ctx.AddFailure("ParticipantCount must be >= 1.");
    if (total > 100) ctx.AddFailure("ParticipantCount cannot exceed 100.");
});
```

---

## WBS

| Step | Sub-deliverable | Hours | Finish-by |
|---|---|---|---|
| 1 | Refactor `TourBooking.cs` aggregate: `Create` factory + properties + computed (no state-change methods yet — those in TASK 5) | 4 | Tue 2026-06-30 EOD |
| 2 | Implement `BookingReference` value object + `IBookingReferenceGenerator` interface + impl with collision retry | 3 | Wed 2026-07-01 EOD |
| 3 | Implement `BookingPricing` record + `BookingLineItem` record + JSON serialization | 2 | Wed 2026-07-01 EOD |
| 4 | Implement `ITourBookingRepository` extensions: `HasActiveBookingForTourOnDateAsync`, `CountActiveAwaitingPaymentByUserAsync`, `GetByReferenceAsync` | 3 | Thu 2026-07-02 EOD |
| 5 | Migration `BookingAddTourBookingReferenceIndex` + decimal precision on all money columns + JSON columns for RefundPolicySnapshot + LineItemsJson | 2 | Fri 2026-07-03 EOD |
| 6 | `CreateTourBookingCommand` + `CreateTourBookingCommandValidator` + handler skeleton (no Step 2 yet) | 4 | Mon 2026-07-06 EOD |
| 7 | Handler Step 1 validation (snapshot lookups, lead time, dupes, concurrent limit) + 6 unit tests | 5 | Tue 2026-07-07 EOD |
| 8 | Handler Step 2 (transaction + RowVersion + SlotLock create + concurrency-conflict translation) + 4 unit tests | 6 | Thu 2026-07-09 EOD |
| 9 | Handler Step 3 (pricing calc + commission lookup + discount stub + minimum-5-JOD enforcement) + 5 unit tests | 5 | Mon 2026-07-13 EOD |
| 10 | Handler Step 4 (TourBooking.Create + outbox event + UoW.SaveChanges) + integration test (event published) | 3 | Wed 2026-07-15 EOD |
| 11 | Endpoint `POST /api/v1/booking/tour` + DTO mapping + cache invalidation | 2 | Wed 2026-07-15 EOD |
| 12 | `GetTourBookingByIdQuery` (cache + IDOR guard) + endpoint + 3 unit tests | 3 | Fri 2026-07-17 EOD |
| 13 | `GetMyBookingsQuery` (cursor paginated, filters) + endpoint + 3 unit tests | 4 | Mon 2026-07-20 EOD |
| 14 | `GetAllBookingsQuery` (admin, more filters) + endpoint + 2 unit tests | 3 | Tue 2026-07-21 EOD |
| 15 | DI registration in `BookingDependencyInjection.cs` for all new repos + services + reference generator | 1 | Tue 2026-07-21 EOD |
| 16 | Integration test: full POST /tour happy path → DB has booking + slot decremented + lock created + outbox row | 3 | Thu 2026-07-23 EOD |
| 17 | Integration test: concurrent POST /tour for same slot (two parallel requests, only one succeeds, other gets 409) | 3 | Fri 2026-07-24 EOD |
| 18 | Code review cycle (anticipate 2 rounds given complexity) | 6 | Wed 2026-07-29 EOD |
| **Sum** | | **62** | |

> Estimate at 48 was optimistic. Revised to 62. If sprint cap is binding, defer admin endpoint (GET /admin/all) to TASK 5's WBS as bonus. **Discussed at standup Day-3.**

---

## Edge cases acceptance tests

| # | Case | Expected |
|---|---|---|
| 1 | Happy path: 2 adults, instant booking, sufficient capacity | 201, booking Status=AwaitingPayment, slot LockedCount+=2 |
| 2 | Slot fully booked | 409 CapacityExceeded |
| 3 | Tour suspended (snapshot.IsActive=false) | 404 NotFound (we treat suspended as not visible) |
| 4 | Provider suspended | 409 Booking.ProviderSuspended |
| 5 | User has 3 AwaitingPayment | 409 ConcurrentLimit |
| 6 | User has 2 AwaitingPayment + 1 Cancelled | 201 (Cancelled doesn't count) |
| 7 | Same user books same tour same date twice | 409 DuplicateForDate |
| 8 | Tour starts in 1h 50min | 409 TooEarly |
| 9 | Two parallel POST /tour for last seat | one 201, other 409 CapacityConflict |
| 10 | ChildCount=1 but tour has no Child pricing tier | 400 InvalidPricingConfiguration (handler-level) |
| 11 | Subtotal 4.99 JOD after discount (admin discount edge) | 400 BelowPlatformMinimum |
| 12 | Owner GETs by id | 200 |
| 13 | Non-owner GETs by id | 403 OwnerMismatch |
| 14 | Provider GETs own tour's booking by id | 200 |
| 15 | Reference is YJ-20260701-XXXXXX format (6 chars, base32 alphabet excluding O/0/I/1) | regex match |
| 16 | RowVersion collision recovered → second POST returns 409 with retryable error code | 409 CapacityConflict |

---

## Cross-module handoffs

- **Finance**: must implement `finance.payment.completed.v1` inbox handler in Booking (Mohammad implements in TASK 5; or stub here returning a placeholder for now and TASK 5 wires real logic).
- **Analytics**: outbox event `booking.tour-booking.created.v1` lands in Analytics inbox eventually; not in this sprint scope.
- **Messaging**: same — `BookingConfirmed` notification handler ships in Wave 6 Messaging sprint.

---

## Files Mohammad touches

```
Booking.Domain/Entities/TourBooking.cs                            (refactor + Create factory)
Booking.Domain/ValueObjects/BookingReference.cs                    (new)
Booking.Domain/ValueObjects/BookingPricing.cs                       (new record)
Booking.Domain/ValueObjects/BookingLineItem.cs                      (new record)
Booking.Domain/Events/TourBookingCreatedDomainEvent.cs             (PW-3 verify)
Booking.Application/Commands/CreateTourBooking/CreateTourBookingCommand.cs
Booking.Application/Commands/CreateTourBooking/CreateTourBookingCommandValidator.cs
Booking.Application/Commands/CreateTourBooking/CreateTourBookingCommandHandler.cs
Booking.Application/Queries/GetTourBookingById/...
Booking.Application/Queries/GetMyBookings/...
Booking.Application/Queries/GetAllBookings/...
Booking.Application/Interfaces/IBookingReferenceGenerator.cs        (new)
Booking.Application/Interfaces/ITourBookingRepository.cs            (extend)
Booking.Infrastructure/Services/BookingReferenceGenerator.cs        (new)
Booking.Infrastructure/Repositories/TourBookingRepository.cs
Booking.Infrastructure/Persistence/Configurations/TourBookingConfiguration.cs (RowVersion + JSON cols + indexes)
Booking.Infrastructure/Migrations/{timestamp}_BookingAddTourBookingReferenceIndex.cs
Booking.Presentation/Endpoints/TourBookingEndpoints.cs               (new sub-file)
Booking.Presentation/BookingEndpoints.cs                            (wire-up)
tests/Booking.Tests.Unit/Commands/CreateTourBooking_Step1_ValidationTests.cs
tests/Booking.Tests.Unit/Commands/CreateTourBooking_Step2_LockTests.cs
tests/Booking.Tests.Unit/Commands/CreateTourBooking_Step3_PricingTests.cs
tests/Booking.Tests.Unit/Commands/CreateTourBooking_Step4_CreateTests.cs
tests/Booking.Tests.Unit/Services/BookingReferenceGeneratorTests.cs
tests/Booking.IntegrationTests/TourBookingPostRoundTripTests.cs
tests/Booking.IntegrationTests/TourBookingConcurrencyTests.cs
```
