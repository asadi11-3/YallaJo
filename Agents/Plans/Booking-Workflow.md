# Booking Workflow — Architecture Plan

> **Status:** Implemented (audited 2025-01-27). Core domain + infrastructure complete. Payment flow wired via Finance module. 5 stub services pending replacement.
> **Scope:** Tour booking workflow compatible with `TourGuide-Flow.md` multi-guide model. Extensible for future business reservations.
> **Dependencies:** `TourGuide-Flow.md` (must execute Phases 0-5 first — profile alignment + guide offerings).
> **Audit Score:** 7.2/10 — see `Booking-Audit-Report.md`

---

## Decision Summary

| # | Decision | Detail |
|---|----------|--------|
| 1 | **Three discovery paths** | Tour→Guide→Slots, Tour→All Slots (guide per slot), Guide→Tours→Slots. All three supported. |
| 2 | **60-day rolling slot gen** | Background service generates concrete AvailabilitySlots from GuideSchedule, 60 days ahead, daily. Respects GuideAvailabilityBlock. Guide can also manually add one-off slots. |
| 3 | **Private = same slot, toggle** | Tourist picks "Private" at booking time. Entire slot booked exclusively at guide's private pricing. |
| 4 | **Guide confirms bookings** | Non-instant bookings: the BOOKED GUIDE gets 24h confirmation window. Agency owner can also confirm on guide's behalf. |
| 5 | **Gateway-agnostic payment** | `IPaymentGateway` interface. Stripe implementation first. Add HyperPay/local gateways later. |
| 6 | **Separate BusinessReservation** | Business bookings (spa, hotel, restaurant) get own entity + slot system. NOT shared AvailabilitySlot. |
| 7 | **System-wide lock TTL** | 10 minutes regular, 15 minutes subscribers. No per-guide config. |
| 8 | **Finance handles escrow** | Booking fires `TourCompletedEvent` → Finance creates 7-day escrow → releases to GuidePayout. |
| 9 | **One person books for group** | Booker enters participant count per tier (4 adults, 2 children). Single payment. Booker = contact person. |
| 10 | **Join requests in MVP** | Tourist can request to join existing confirmed group booking. Guide approves/rejects. Separate charge. |
| 11 | **Packages deferred** | Multi-tour package booking deferred to post-MVP. |
| 12 | **Stackable discounts (tour + guide)** | Tour-level auto-discounts + guide-level promotions. Max 2 stackable. Promo code counts as 1. Min 5 JOD. |
| 13 | **Suspension = auto-cancel** | Guide suspended → all future confirmed bookings auto-cancelled → 100% refund to tourists. |
| 14 | **Booking window** | 60-day rolling generation + min 2h lead time before slot start. |

---

## The Booking Workflow (End-to-End)

### Phase A: Slot Generation (Background)

```
┌─────────────────────────────────────────────────────────┐
│  SlotGenerationService (runs daily at 2 AM)             │
│                                                         │
│  For each active GuideTourOffering:                     │
│    1. Read GuideSchedule (recurring: Mon 9AM, Wed 2PM)  │
│    2. Read GuideAvailabilityBlock (blocked date ranges)  │
│    3. For next 60 days:                                  │
│       - Skip blocked dates                              │
│       - Skip dates with existing slots                  │
│       - Create AvailabilitySlot per schedule entry       │
│         → TourGuideId = guide.Id                        │
│         → TourId = offering.TourId                      │
│         → Date, StartTime, EndTime from schedule        │
│         → MaxCapacity from GuideSchedule override        │
│           OR Tour.MaxGroupSize default                   │
│         → ScheduleId = guideSchedule.Id                 │
│         → SlotType = Tour                               │
│         → IsActive = true                               │
│    4. Deactivate slots for newly blocked dates           │
│    5. Clean up expired (past) slots                      │
└─────────────────────────────────────────────────────────┘
```

**Triggers for re-generation:**
- Daily scheduled run
- Guide updates their GuideSchedule → immediate re-gen for affected dates
- Guide creates/removes GuideAvailabilityBlock → deactivate/reactivate affected slots
- Guide manually adds one-off slot (bypasses schedule)

### Phase B: Discovery & Selection

**Path 1 — Tour → Guide List → Slots (Primary)**
```
Tourist browses tours
  → GET /tours (search, filter, nearby)
  → Selects tour
  → GET /tours/{tourId}/guides (list guide offerings, rated, sorted by rating/price)
  → Each guide card shows: name, avatar, rating, review count, price range, next available
  → Tourist selects guide
  → GET /tours/{tourId}/guides/{guideId}/availability?from=&to= (available slots)
  → Tourist picks date/time
```

**Path 2 — Tour → All Available Slots**
```
Tourist browses tours
  → Selects tour
  → GET /tours/{tourId}/availability?from=&to= (ALL slots across ALL guides)
  → Each slot shows: date, time, guide name, guide rating, price, remaining capacity
  → Tourist picks slot (implicitly selects the guide)
```

**Path 3 — Guide Profile → Their Tours**
```
Tourist discovers guide (search, recommendation)
  → GET /guides/{slug} (guide profile page)
  → GET /guides/{guideId}/tours (tours this guide offers)
  → Tourist selects tour
  → GET /tours/{tourId}/guides/{guideId}/availability
  → Tourist picks date/time
```

### Phase C: Booking Creation

```
┌──────────────────────────────────────────────────────────────┐
│  CreateTourBooking Flow                                       │
│                                                               │
│  INPUT: TourId, GuideId, SlotId, Participants[], IsPrivate    │
│         Participants = [{TierType, Count}]                    │
│         e.g. [{Adult, 4}, {Child, 2}]                        │
│                                                               │
│  VALIDATIONS (in order):                                      │
│  1. Slot exists + IsActive + SlotType = Tour                 │
│  2. Slot.TourGuideId matches GuideId                         │
│  3. Guide offering is Active (GuideTourOffering.Status)      │
│  4. Min 2h lead time (slot.Date+StartTime - now ≥ 2h)       │
│  5. User has < 3 concurrent pending bookings                 │
│  6. No duplicate: same user + same tour + same date          │
│  7. Private tour checks:                                      │
│     - Guide.OffersPrivateTour = true                         │
│     - Slot is EMPTY (BookedCount == 0 && LockedCount == 0)   │
│  8. Shared tour checks:                                       │
│     - Capacity: BookedCount + LockedCount + totalParticipants │
│       ≤ MaxCapacity                                           │
│  9. Participant count: 1 ≤ totalParticipants ≤ remaining     │
│                                                               │
│  PRICING:                                                     │
│  1. Load GuidePricingTier for this offering                  │
│     (fallback: TourPricingTier if guide has no custom)       │
│  2. Calculate subtotal per tier: tier.Price × count           │
│  3. If private: apply PrivateTourPriceMultiplier or FlatPrice│
│  4. Apply discounts (Phase D)                                │
│  5. Calculate commission: subtotal × guide.CommissionRate     │
│     (tier-based, from GuideTrustTier)                        │
│  6. Total = subtotal - discountAmount - loyaltyAmount        │
│                                                               │
│  CREATE SLOT LOCK:                                            │
│  - SlotLock.Create(userId, slotId, totalParticipants, ttl)   │
│  - TTL: 10 min (regular) / 15 min (subscriber)              │
│  - slot.Lock(participantCount) → BookedCount unchanged,      │
│    LockedCount += participantCount                            │
│  - If private: lock entire MaxCapacity                       │
│                                                               │
│  CREATE BOOKING:                                              │
│  - TourBooking.Create(...)                                   │
│  - Status = AwaitingPayment                                  │
│  - Reference = YJ-YYYYMMDD-XXXXXX                            │
│  - PaymentExpiresAt = now + TTL                              │
│  - Store: pricing snapshot, refund policy snapshot,           │
│    LineItemsJson, IsPrivate flag                             │
│  - Fire: TourBookingCreatedDomainEvent                       │
│                                                               │
│  OUTPUT: BookingId, Reference, TotalAmount, PaymentExpiresAt │
└──────────────────────────────────────────────────────────────┘
```

### Phase D: Discount Evaluation

```
┌──────────────────────────────────────────────────────────────┐
│  Discount Pipeline (max 2 stackable)                         │
│                                                               │
│  LAYER 1 — Tour-Level Auto-Discounts:                        │
│  ┌─────────────────────────────────────┐                     │
│  │ EarlyBird: booked 14+ days ahead    │ → % off             │
│  │ LastMinute: booked < 48h ahead      │ → % off             │
│  │ GroupSize: 6+ participants          │ → % off             │
│  │ Public: tour-wide seasonal promo    │ → % or flat off     │
│  └─────────────────────────────────────┘                     │
│  Pick best auto-discount (highest value). Count = 1.         │
│                                                               │
│  LAYER 2 — Guide-Level Promotions:                           │
│  ┌─────────────────────────────────────┐                     │
│  │ GuideDiscount entity (NEW)           │                     │
│  │ - GuideOfferingId                    │                     │
│  │ - Type: Percentage / FlatAmount      │                     │
│  │ - Value (decimal)                    │                     │
│  │ - ValidFrom / ValidUntil            │                     │
│  │ - MinParticipants (int?)            │                     │
│  │ - MaxUses / CurrentUses             │                     │
│  │ - IsActive                          │                     │
│  └─────────────────────────────────────┘                     │
│  Apply if active + valid date + participant threshold met.    │
│  Count = 2 now.                                              │
│                                                               │
│  LAYER 3 — Promo Code (optional, user-entered):              │
│  If promo code entered AND total stack < 2, apply.           │
│  If stack already 2, reject promo code.                       │
│                                                               │
│  GUARD: final price ≥ 5 JOD. If below, cap discount.        │
│  RE-VALIDATE at payment time (prices/discounts may change).  │
└──────────────────────────────────────────────────────────────┘
```

### Phase E: Payment

```
┌──────────────────────────────────────────────────────────────┐
│  Payment Flow (Gateway-Agnostic)                             │
│                                                               │
│  IPaymentGateway interface:                                  │
│  ┌─────────────────────────────────────────────┐             │
│  │ InitiatePayment(bookingRef, amount, currency,│             │
│  │                 returnUrl, metadata)          │             │
│  │ → PaymentSession (sessionId, redirectUrl)     │             │
│  │                                               │             │
│  │ ProcessWebhook(payload, signature)            │             │
│  │ → PaymentResult (success, transactionId,      │             │
│  │                  amount, metadata)             │             │
│  │                                               │             │
│  │ InitiateRefund(transactionId, amount, reason)│             │
│  │ → RefundResult (success, refundId)            │             │
│  │                                               │             │
│  │ GetPaymentStatus(sessionId)                   │             │
│  │ → PaymentStatus enum                          │             │
│  └─────────────────────────────────────────────┘             │
│                                                               │
│  FLOW:                                                       │
│  1. POST /bookings/{id}/pay                                  │
│     → Handler calls gateway.InitiatePayment()                │
│     → Returns redirectUrl to tourist                         │
│                                                               │
│  2. Tourist completes payment on gateway page                │
│                                                               │
│  3. POST /payments/webhook (gateway callback)                │
│     → Idempotent via TransactionId                           │
│     → Validate webhook signature                            │
│     → Re-validate discounts (prices may have changed)        │
│     → If success:                                            │
│       a. slotLock.AttachBookingId(bookingId)                 │
│       b. slot.ConfirmBooking(participantCount)               │
│          → BookedCount += count, LockedCount -= count        │
│       c. If tour.IsInstantBooking:                           │
│          → booking.Confirm(PaymentWebhook)                   │
│          → Status = Confirmed                                │
│       d. Else:                                               │
│          → booking.MoveToPendingConfirmation()               │
│          → Status = PendingConfirmation                      │
│          → Notify guide (in-app + email)                     │
│       e. Fire TourBookingPaidIntegrationEvent                │
│     → If failure:                                            │
│       a. slot.ReleaseLock(participantCount)                  │
│       b. booking.MoveToAwaitingPaymentExpired()              │
│                                                               │
│  NO credit card storage (PCI DSS compliance).                │
│  Currency locked at booking creation.                        │
│  JOD / USD / EUR supported.                                 │
└──────────────────────────────────────────────────────────────┘
```

### Phase F: Guide Confirmation (Non-Instant)

```
┌──────────────────────────────────────────────────────────────┐
│  Guide Confirmation Flow (24h window)                        │
│                                                               │
│  WHO CAN CONFIRM:                                            │
│  - The booked GUIDE (primary authority)                      │
│  - Agency OWNER (if tour is OwnershipType.Provider)          │
│  - Admin (override capability)                               │
│                                                               │
│  ACCEPT → POST /bookings/{id}/confirm                        │
│  - booking.Confirm(ConfirmationSource.Manual)                │
│  - Status: PendingConfirmation → Confirmed                   │
│  - Tourist notified (in-app + email)                         │
│  - Fire TourBookingConfirmedIntegrationEvent                 │
│                                                               │
│  DECLINE → POST /bookings/{id}/reject                        │
│  - booking.Reject(reason)                                    │
│  - Status: PendingConfirmation → Rejected                    │
│  - slot.ReleaseBooking(participantCount) → BookedCount -= n  │
│  - Full refund via gateway.InitiateRefund()                  │
│  - Tourist notified                                          │
│  - Fire TourBookingRejectedIntegrationEvent                  │
│                                                               │
│  NO RESPONSE (24h timeout) → ProviderAutoAcceptService       │
│  - Background service polls every 15 min                     │
│  - Finds bookings: Status=PendingConfirmation                │
│    AND CreatedAt + 24h < now                                 │
│  - booking.Confirm(ConfirmationSource.AutoAccept)            │
│  - Tourist notified: "auto-confirmed"                        │
│  - Fire TourBookingConfirmedIntegrationEvent                 │
└──────────────────────────────────────────────────────────────┘
```

### Phase G: Post-Booking Lifecycle

```
┌──────────────────────────────────────────────────────────────┐
│  Tour Day                                                     │
│                                                               │
│  BEFORE:                                                     │
│  - T-24h: Reminder notification to tourist + guide           │
│  - T-2h: Final reminder                                      │
│                                                               │
│  DURING:                                                     │
│  - Guide marks check-in per participant (optional)           │
│  - Guide can mark no-show                                    │
│                                                               │
│  AFTER:                                                      │
│  - Guide marks tour as completed:                            │
│    POST /bookings/{id}/complete                              │
│    booking.Complete(guideUserId)                             │
│    Status: Confirmed → Completed                             │
│  - OR auto-complete: if slot EndTime + 2h passed and guide   │
│    hasn't acted, background service completes it             │
│  - Fire TourBookingCompletedIntegrationEvent                 │
│    → Finance module creates 7-day escrow hold                │
│    → GuideEarning record created                             │
│    → Guide.IncrementCompletedTourCount()                     │
│                                                               │
│  ESCROW (Finance module handles):                            │
│  - 7-day hold after completion                               │
│  - Tourist can dispute during this window                    │
│  - After 7 days: release to GuidePayout pipeline             │
│  - Weekly batch payouts (Sunday midnight)                    │
│  - Min 10 JOD threshold                                     │
└──────────────────────────────────────────────────────────────┘
```

### Phase H: Cancellation & Refund

```
┌──────────────────────────────────────────────────────────────┐
│  Cancellation Rules                                          │
│                                                               │
│  WHO CAN CANCEL:                                             │
│  - Tourist: from AwaitingPayment, PendingConfirmation,       │
│    or Confirmed                                              │
│  - Guide: from PendingConfirmation or Confirmed              │
│    → always 100% refund (guide's responsibility)             │
│  - Admin: force-cancel from any active state                 │
│    → admin decides refund % (force majeure = 100%)           │
│                                                               │
│  TOURIST CANCELLATION REFUND TIERS:                          │
│  ┌─────────────────────────────────────────────┐             │
│  │ Tour's RefundPolicy (snapshotted at booking)│             │
│  │                                             │             │
│  │ timeUntilTour ≥ FullRefundHours → 100%      │             │
│  │ timeUntilTour ≥ PartialRefundHours → X%     │             │
│  │ timeUntilTour < PartialRefundHours → 0%     │             │
│  └─────────────────────────────────────────────┘             │
│                                                               │
│  PROCESS:                                                    │
│  1. booking.Cancel(source, refundPercentage)                 │
│  2. slot.ReleaseBooking(participantCount)                    │
│  3. If refund > 0: gateway.InitiateRefund(transactionId,     │
│     refundAmount) → to original payment method               │
│  4. If gateway down: RefundRetryService retries every 15min  │
│     for 24h                                                  │
│  5. Fire TourBookingCancelledIntegrationEvent                │
│  6. Tourist + guide both notified                            │
│                                                               │
│  CANNOT modify booking — must cancel + rebook.               │
└──────────────────────────────────────────────────────────────┘
```

### Phase I: Join Request Flow (MVP)

```
┌──────────────────────────────────────────────────────────────┐
│  Join Request (tourist joins existing group booking)          │
│                                                               │
│  PRECONDITIONS:                                              │
│  - Booking is Confirmed, NOT private                         │
│  - Slot has remaining capacity                               │
│  - Requester ≠ booking owner                                 │
│  - Requester doesn't already have a booking for same slot    │
│                                                               │
│  FLOW:                                                       │
│  1. Tourist finds bookable tour slot that has an existing     │
│     confirmed group (partial capacity used)                  │
│  2. POST /bookings/{bookingId}/join-requests                 │
│     → JoinRequest.Create(bookingId, userId,                  │
│       participantCount, message)                             │
│     → Status: Pending                                        │
│     → Slot.Lock(participantCount) — locks capacity           │
│     → SlotLock created (same TTL rules)                      │
│                                                               │
│  3. Guide sees join request notification                     │
│     GET /guides/me/join-requests (or on booking detail)      │
│                                                               │
│  4a. APPROVE → POST /join-requests/{id}/approve              │
│     → Creates SEPARATE TourBooking for the joiner            │
│     → Joiner goes through same payment flow                  │
│     → On payment: slot.ConfirmBooking(participantCount)      │
│     → JoinRequest.Status = Approved                          │
│                                                               │
│  4b. REJECT → POST /join-requests/{id}/reject                │
│     → slot.ReleaseLock(participantCount)                     │
│     → JoinRequest.Status = Rejected                          │
│     → Requester notified                                     │
│                                                               │
│  4c. EXPIRE (48h timeout) → JoinRequestExpiryService         │
│     → slot.ReleaseLock(participantCount)                     │
│     → JoinRequest.Status = Expired                           │
│     → Requester notified                                     │
│                                                               │
│  RESULT: Joiner has their own TourBooking, own payment,      │
│  own cancellation rights. Original booking unaffected.       │
└──────────────────────────────────────────────────────────────┘
```

### Phase J: Suspension Impact

```
┌──────────────────────────────────────────────────────────────┐
│  Guide Suspension → Auto-Cancel                              │
│                                                               │
│  TRIGGERS:                                                   │
│  - Guide profile suspended (TourGuideStatus = Suspended)     │
│  - Guide removed from specific tour (GuideOfferingStatus     │
│    = Removed/Suspended)                                      │
│                                                               │
│  PROCESS:                                                    │
│  1. Integration event: GuideSuspendedIntegrationEvent or     │
│     GuideOfferingSuspendedIntegrationEvent (NOT YET BUILT)   │
│  2. Booking module event handler:                            │
│     - Query all future Confirmed bookings for this guide     │
│     - For each booking:                                      │
│       a. booking.Cancel(CancellationSource.System, 100%)     │
│       b. slot.ReleaseBooking(participantCount)               │
│       c. gateway.InitiateRefund(100%)                        │
│       d. Tourist notified with reason                        │
│     - Deactivate all future AvailabilitySlots for guide      │
│  3. Fire batch TourBookingCancelledIntegrationEvent          │
│                                                               │
│  If guide reinstated: slots regenerated by                   │
│  SlotGenerationService on next run.                          │
└──────────────────────────────────────────────────────────────┘
```

---

## Private Tour Details

```
┌──────────────────────────────────────────────────────────────┐
│  Private Tour Booking                                        │
│                                                               │
│  GUIDE SETUP (GuideTourOffering):                            │
│  - OffersPrivateTour = true                                  │
│  - PrivateTourPriceMultiplier (e.g., 2.0x base price)       │
│  - OR PrivateTourFlatPrice (e.g., 150 JOD flat)             │
│  - Only one pricing model active (multiplier XOR flat)       │
│                                                               │
│  TOURIST BOOKING:                                            │
│  - Same slot selection flow                                  │
│  - Toggles "Private Tour" option                             │
│  - VALIDATION: slot must be completely empty                 │
│    (BookedCount == 0 && LockedCount == 0)                    │
│  - LOCK: locks ENTIRE MaxCapacity (no one else can book)     │
│  - PRICE: guide's private pricing (not per-person tiers)     │
│                                                               │
│  BOOKING ENTITY:                                             │
│  - TourBooking.IsPrivate = true                              │
│  - ParticipantCount = tourist's actual group size            │
│  - TotalAmount based on private pricing                      │
│                                                               │
│  RESTRICTIONS:                                               │
│  - No join requests allowed on private bookings              │
│  - Cancellation follows same tour-level policy               │
│  - Refund based on private tour total amount                 │
└──────────────────────────────────────────────────────────────┘
```

---

## Entity Changes

### TourBooking — Modifications

```csharp
// ADD these properties:
public Guid GuideId { get; private set; }         // NEW — FK to ContentTours TourGuide
public bool IsPrivate { get; private set; }        // NEW — private tour flag
public Guid? JoinedFromBookingId { get; private set; } // NEW — if created via join request

// MODIFY Create() factory:
// Add GuideId, IsPrivate parameters
// Store GuideId (maps to AvailabilitySlot.TourGuideId)
// Validate IsPrivate logic
```

### JoinRequest — Full Domain Methods (IMPLEMENTED — not a shell)

```csharp
public sealed class JoinRequest : AuditableEntity
{
    // EXISTING properties (keep):
    public Guid TourBookingId { get; private set; }
    public Guid UserId { get; private set; }
    public JoinRequestStatus Status { get; private set; }
    public string? Message { get; private set; }
    public int ParticipantCount { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public string? ResponseMessage { get; private set; }

    // ADD:
    public Guid AvailabilitySlotId { get; private set; }  // the slot being joined
    public DateTime ExpiresAt { get; private set; }        // 48h TTL
    public Guid? ResultingBookingId { get; private set; }  // booking created on approval

    // METHODS (NEW):
    public static JoinRequest Create(Guid bookingId, Guid userId,
        Guid slotId, int participantCount, string? message);
    public Result Approve(string? responseMessage);
    public Result Reject(string? responseMessage);
    public Result Expire();
    public Result AttachResultingBooking(Guid bookingId);
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    // DOMAIN EVENTS:
    // JoinRequestCreatedDomainEvent(Id, BookingId, UserId)
    // JoinRequestApprovedDomainEvent(Id, BookingId, UserId)
    // JoinRequestRejectedDomainEvent(Id, BookingId, UserId)

    // Navigation:
    public TourBooking TourBooking { get; init; }
}
```

### GuideDiscount — New Entity (IMPLEMENTED — FK model differs)

> **Audit Note (2025-01-27)**: Actual implementation uses `GuideUserId` + optional `TourId` instead of `GuideTourOfferingId`. This is more flexible — allows guide-level discounts not tied to a specific offering. `MinParticipants` was not implemented. Naming: `MaxUses` → `MaxUsageCount`, `CurrentUses` → `CurrentUsageCount`, `FlatAmount` → `FixedAmount`.

```csharp
public sealed class GuideDiscount : AuditableEntity
{
    public Guid GuideUserId { get; private set; }           // ACTUAL: guide-level, not offering-level
    public Guid? TourId { get; private set; }               // ACTUAL: optional tour scope
    public string Name { get; private set; }                // "Summer Special"
    public GuideDiscountType DiscountType { get; private set; } // Percentage / FixedAmount
    public decimal DiscountValue { get; private set; }
    public string Currency { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public int? MaxUsageCount { get; private set; }         // null = unlimited
    public int CurrentUsageCount { get; private set; }
    public bool IsActive { get; private set; }

    // Methods:
    public static Result<GuideDiscount> Create(...);
    public Result Update(...);
    public Result Deactivate();
    public Result IncrementUsage();
    public bool IsValidAt(DateTime now);
}

// GuideDiscountType enum: Percentage = 0, FixedAmount = 1
```

---

## New Interfaces

### IPaymentGateway (Finance.Contracts — NOT Booking)

> **Audit Note (2025-01-27)**: IPaymentGateway is owned by `Finance.Contracts.Services`, not `Booking.Contracts`. The Finance module owns the entire payment lifecycle. Booking's role is to create the booking → Finance handles payment → Finance publishes `PaymentCompletedIntegrationEvent` → Booking's `PaymentCompletedHandler` auto-confirms the booking.

```csharp
// ACTUAL: Finance.Contracts.Services.IPaymentGateway
public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<InitiateResult> InitiateAsync(InitiateRequest request, CancellationToken ct = default);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken ct = default);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default);
    Task<PayoutResult> PayoutAsync(PayoutRequest request, CancellationToken ct = default);
}

// Finance also owns:
// - InitiatePaymentCommand (Finance.Application)
// - RefundRetryService (Finance.Infrastructure)
// - PaymentCompletedIntegrationEvent (Finance.Contracts, registered as finance.payment.completed.v1)
// - Payment entity with full state machine (Finance.Domain)
```

### IDiscountEvaluator (Replace Stub)

```csharp
public interface IDiscountEvaluator
{
    Task<DiscountResult> EvaluateAsync(
        Guid tourId,
        Guid guideOfferingId,
        int participantCount,
        DateTime tourDate,
        string? promoCode,
        CancellationToken ct = default);
}

public record DiscountResult(
    decimal TourLevelDiscount,
    string? TourDiscountName,
    decimal GuideLevelDiscount,
    string? GuideDiscountName,
    decimal PromoCodeDiscount,
    string? PromoCodeName,
    decimal TotalDiscount,
    bool PromoCodeRejected,
    string? PromoCodeRejectionReason);
```

### Replace Existing Stubs

| Stub Interface | Action |
|----------------|--------|
| `IBookingTourSnapshotReader` | Replace with real cross-module reader via ContentTours.Contracts |
| `IBookingPricingSnapshotReader` | Replace with reader that loads GuidePricingTier (primary) OR TourPricingTier (fallback) |
| `IBookingProviderSnapshotReader` | Replace with reader that loads guide + tour owner info |
| `IBookingCommissionLookup` | Replace with reader that gets guide's tier-based commission rate |
| `IDiscountEvaluator` | Replace with real evaluator (above) |

---

## Endpoints

### Availability Endpoints (NEW — `/bookings/availability`)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/tours/{tourId}/availability` | Anon | All available slots across all guides for a tour (merged calendar view) |
| `GET` | `/tours/{tourId}/guides/{guideId}/availability` | Anon | Available slots for a specific guide on a specific tour |
| `POST` | `/guides/me/tours/{tourId}/slots` | Guide | Manually create one-off availability slot |
| `PUT` | `/guides/me/tours/{tourId}/slots/{slotId}` | Guide | Modify slot (time, capacity) |
| `DELETE` | `/guides/me/tours/{tourId}/slots/{slotId}` | Guide | Deactivate/remove slot |
| `POST` | `/guides/me/tours/{tourId}/slots/bulk` | Guide | Bulk create slots for specific dates |

### Booking Endpoints (MODIFY existing + NEW)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| `POST` | `/bookings/tour` | User | **MODIFY** — add GuideId, IsPrivate, Participants[] |
| `GET` | `/bookings/{id}` | Owner/Guide/Admin | KEEP |
| `GET` | `/bookings/my` | User | KEEP |
| `POST` | `/bookings/{id}/pay` | Owner | **NEW** — initiate payment |
| `POST` | `/payments/webhook` | System | **NEW** — gateway callback |
| `POST` | `/bookings/{id}/confirm` | Guide/Owner | **MODIFY** — guide can confirm |
| `POST` | `/bookings/{id}/reject` | Guide/Owner | **MODIFY** — guide can reject |
| `POST` | `/bookings/{id}/cancel` | Owner/Guide/Admin | KEEP |
| `POST` | `/bookings/{id}/complete` | Guide | KEEP |
| `POST` | `/bookings/admin/{id}/force-refund` | Admin | KEEP |
| `GET` | `/bookings/admin/all` | Admin | KEEP |

### Guide Booking Dashboard (NEW)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/bookings` | Guide | List all bookings for guide's tours (paginated, filterable) |
| `GET` | `/guides/me/bookings/upcoming` | Guide | Upcoming confirmed bookings |
| `GET` | `/guides/me/bookings/{id}` | Guide | Booking detail with tourist contact info |
| `POST` | `/guides/me/bookings/{id}/check-in` | Guide | Mark tourist as checked-in |
| `POST` | `/guides/me/bookings/{id}/no-show` | Guide | Mark tourist as no-show |

### Join Request Endpoints (NEW)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/bookings/{bookingId}/join-requests` | User | Submit join request |
| `GET` | `/guides/me/join-requests` | Guide | List pending join requests |
| `GET` | `/join-requests/{id}` | Requester/Guide | Get join request details |
| `POST` | `/join-requests/{id}/approve` | Guide | Approve join request |
| `POST` | `/join-requests/{id}/reject` | Guide | Reject join request |

### Discount Endpoints (NEW — Guide-Level)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/guides/me/tours/{tourId}/discounts` | Guide | List discounts for an offering |
| `POST` | `/guides/me/tours/{tourId}/discounts` | Guide | Create a discount |
| `PUT` | `/guides/me/tours/{tourId}/discounts/{id}` | Guide | Update discount |
| `DELETE` | `/guides/me/tours/{tourId}/discounts/{id}` | Guide | Deactivate discount |

### Payment Endpoints (NEW)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/bookings/{id}/payment-status` | Owner | Check payment status |
| `GET` | `/bookings/my/payment-history` | User | Payment history |

### Refund Policy Endpoints (NEW)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/refund-policies` | Admin | List all refund policies |
| `POST` | `/refund-policies` | Admin | Create refund policy |
| `PUT` | `/refund-policies/{id}` | Admin | Update refund policy |

---

## Background Services

| Service | Interval | Description |
|---------|----------|-------------|
| `SlotGenerationService` | Daily 2 AM | Generate AvailabilitySlots from GuideSchedule for next 60 days |
| `SlotLockCleanupService` | 2 min | Release expired SlotLocks, restore slot.LockedCount |
| `BookingAutoExpireService` | 5 min | Expire AwaitingPayment bookings past PaymentExpiresAt |
| `ProviderAutoAcceptService` | 15 min | Auto-confirm PendingConfirmation bookings after 24h |
| `JoinRequestExpiryService` | 30 min | Expire pending join requests after 48h |
| `BookingAutoCompleteService` | 1 hour | Auto-complete confirmed bookings 2h after slot EndTime |
| `BookingReminderService` | 1 hour | Send T-24h and T-2h reminder notifications |
| `RefundRetryService` | 15 min | Retry failed refunds for up to 24h |
| `SlotCleanupService` | Daily 3 AM | Deactivate past-date slots, clean up orphaned locks |

---

## Cross-Module Integration Events

### Booking → Other Modules

| Event | Consumer | Action |
|-------|----------|--------|
| `TourBookingCreatedIntegrationEvent` | Messaging | Send booking created notification |
| `TourBookingPaidIntegrationEvent` | Finance | Create PaymentExpectation record |
| `TourBookingConfirmedIntegrationEvent` | Messaging, Finance | Confirmation notifications, update payment status |
| `TourBookingRejectedIntegrationEvent` | Messaging, Finance | Rejection notification, initiate refund |
| `TourBookingCancelledIntegrationEvent` | Messaging, Finance | Cancellation notification, process refund |
| `TourBookingCompletedIntegrationEvent` | Finance, ContentTours | Create escrow, increment guide.CompletedTourCount |
| `JoinRequestCreatedIntegrationEvent` | Messaging | Notify guide |
| `JoinRequestApprovedIntegrationEvent` | Messaging | Notify requester |

### Other Modules → Booking

| Event | Source | Action |
|-------|--------|--------|
| `GuideSuspendedIntegrationEvent` | ContentTours | Auto-cancel guide's future bookings |
| `GuideOfferingSuspendedIntegrationEvent` | ContentTours | Auto-cancel bookings for specific tour offering |
| `TourArchivedIntegrationEvent` | ContentTours | Deactivate future slots, notify booked tourists |
| `ProviderSuspendedIntegrationEvent` | Accounts | Auto-cancel all provider's tour bookings |

---

## Business Reservation — Future Extension Blueprint

> NOT built now. Designed so the booking module has clear extension points.

```
┌──────────────────────────────────────────────────────────────┐
│  BusinessReservation (separate entity, separate flow)         │
│                                                               │
│  Differences from TourBooking:                               │
│  - No guide concept (staff serves the customer)              │
│  - ServiceItem instead of Tour                               │
│  - BusinessAvailabilitySlot (separate from tour slots)       │
│  - Different capacity model (tables, rooms, seats)           │
│  - Different cancellation policies                           │
│  - Different confirmation flow (instant for most businesses) │
│  - No escrow (direct payment to business)                    │
│                                                               │
│  SHARED CONCEPTS (reuse):                                    │
│  - IPaymentGateway (same payment infrastructure)             │
│  - SlotLock mechanism (same lock pattern)                    │
│  - Notification pipeline                                     │
│  - Discount evaluation (IDiscountEvaluator)                  │
│  - Refund policy pattern                                     │
│  - Reference generation (YJ-YYYYMMDD-XXXXXX)                │
│                                                               │
│  EXTENSION POINTS:                                           │
│  - IBookableEntity interface                                 │
│  - IAvailabilitySlot interface                               │
│  - IBookingPolicy interface (validation rules)               │
│  - ICancellationPolicy interface                             │
│                                                               │
│  New entities (future):                                      │
│  - BusinessReservation (AuditableEntity, IAggregateRoot)     │
│  - BusinessAvailabilitySlot                                  │
│  - BusinessSlotLock                                          │
│  - BusinessRefundPolicy                                      │
└──────────────────────────────────────────────────────────────┘
```

---

## Execution Phases

> **Prerequisite:** TourGuide-Flow.md Phases 0-5 must be complete first (GuideTourOffering, GuideSchedule, GuidePricingTier entities exist).

### Phase 1: Domain Layer (Additive)
1. Add `GuideId`, `IsPrivate`, `JoinedFromBookingId` to TourBooking entity
2. Add domain methods to JoinRequest (Create, Approve, Reject, Expire)
3. Create GuideDiscount entity + GuideDiscountType enum
4. Create domain events for JoinRequest lifecycle
5. Update TourBooking.Create() factory for new parameters
6. Update JoinRequestStatus enum if needed (add Expired)

### Phase 2: Infrastructure — Slot Generation
1. Create SlotGenerationService background service
2. Update AvailabilitySlot configuration (ensure TourGuideId FK works with ContentTours)
3. Create JoinRequest EF configuration (currently shell)
4. Create GuideDiscount EF configuration
5. Update TourBooking EF configuration for new columns
6. Add new repositories: IJoinRequestRepository, IGuideDiscountRepository

### Phase 3: Application — Payment Abstraction
1. Create IPaymentGateway interface in Booking.Contracts
2. Create Stripe implementation (StripePaymentGateway)
3. Create payment DTOs (PaymentSession, PaymentResult, RefundResult)
4. Create InitiatePaymentCommand + handler
5. Create ProcessPaymentWebhookCommand + handler (idempotent)
6. Replace stubs: IBookingTourSnapshotReader, IBookingPricingSnapshotReader, IBookingProviderSnapshotReader, IBookingCommissionLookup

### Phase 4: Application — Discount System
1. Create DiscountEvaluator (replaces NoOpDiscountEvaluator)
2. Create tour-level discount evaluation logic
3. Create guide-level discount CRUD handlers
4. Create promo code validation
5. Integrate discount pipeline into CreateTourBooking handler

### Phase 5: Application — Booking Flow Updates
1. Update CreateTourBookingCommandHandler for multi-guide model (GuideId, IsPrivate, GuidePricingTier lookup)
2. Update ConfirmTourBookingCommandHandler (guide can confirm)
3. Update RejectTourBookingCommandHandler (guide can reject)
4. Create availability query handlers (per-tour, per-guide)
5. Create guide manual slot management handlers
6. Add booking reminder logic

### Phase 6: Application — Join Requests
1. Create SubmitJoinRequestCommand + handler
2. Create ApproveJoinRequestCommand + handler (creates booking)
3. Create RejectJoinRequestCommand + handler
4. Create JoinRequestExpiryService
5. Create ListGuideJoinRequestsQuery + handler

### Phase 7: Application — Guide Dashboard
1. Create ListGuideBookingsQuery + handler
2. Create GetGuideUpcomingBookingsQuery + handler
3. Create CheckInBookingCommand + handler
4. Create MarkNoShowCommand + handler
5. Create payment status/history queries

### Phase 8: Presentation Layer
1. Add availability endpoints (6)
2. Modify existing booking endpoints (5)
3. Add guide booking dashboard endpoints (5)
4. Add join request endpoints (5)
5. Add guide discount endpoints (4)
6. Add payment endpoints (3)
7. Add refund policy endpoints (3)
8. Update permissions catalog

### Phase 9: Background Services
1. Create SlotGenerationService
2. Create BookingAutoExpireService
3. Create JoinRequestExpiryService
4. Create BookingAutoCompleteService
5. Create BookingReminderService
6. Create RefundRetryService
7. Update existing ProviderAutoAcceptService (24h, not 20h)
8. Create SlotCleanupService

### Phase 10: Cross-Module Integration
1. Create integration event handlers for guide suspension → booking cancellation
2. Create integration event publishers for booking lifecycle
3. Wire Finance module escrow trigger
4. Wire Messaging module notification triggers
5. Replace all remaining stubs with real implementations

### Phase 11: Cleanup & Build
1. Remove PackageBooking shell entity (deferred)
2. Remove Reservation shell entity (future BusinessReservation)
3. Remove duplicate TourGuide from Booking.Domain (use ContentTours reference)
4. Update tests
5. Solution build — 0 errors

---

## File Count Estimate

| Layer | New Files | Modified Files |
|-------|-----------|----------------|
| Domain (entities, enums, events) | ~8 | ~6 |
| Infrastructure (config, repos, services) | ~12 | ~8 |
| Application (commands, queries, services) | ~40-50 | ~10 |
| Presentation (endpoints, DTOs) | ~8 | ~4 |
| Background Services | ~6 | ~3 |
| Contracts (interfaces, events) | ~8 | ~4 |
| **Total** | **~82-96** | **~35** |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Stripe may not support JOD directly | Verify Stripe JOD support. Fallback: use USD/EUR with exchange rate display. |
| SlotGenerationService creates too many rows | Rolling 60-day window + daily cleanup. Index on (TourGuideId, Date). Max ~180 slots per guide (3 per day × 60 days). |
| Join request race condition (two people request same remaining capacity) | SlotLock mechanism prevents — lock acquired atomically, capacity check before lock. |
| Private tour + join request conflict | Private bookings explicitly disable join requests. IsPrivate flag checked on join request creation. |
| Cross-module event ordering | Use outbox pattern (already in place). Events processed in order within same aggregate. |
| Guide doesn't respond to non-instant booking | ProviderAutoAcceptService auto-confirms after 24h. Tourist not blocked. |
| Discount re-validation at payment time | If discount expired or prices changed between lock and payment, recalculate. If new total > original, honor original (tourist trust). If lower, use lower. |

---

## Implementation Notes (Audit 2025-01-27)

1. **Payment flow is Finance-owned**: `IPaymentGateway` lives in `Finance.Contracts.Services`. `InitiatePaymentCommand` is in `Finance.Application`. `RefundRetryService` is in `Finance.Infrastructure`. Booking creates the booking → Finance handles payment → Finance publishes `PaymentCompletedIntegrationEvent` → Booking's `PaymentCompletedHandler` auto-confirms.

2. **3 stub services replaced (W3-A)**: `IBookingTourSnapshotReader`, `IBookingPricingSnapshotReader`, `IBookingProviderSnapshotReader` now backed by real snapshot tables (TourSnapshots, PricingTierSnapshots, ProviderSnapshots) populated via 5 inbox handlers (TourApproved, TourUpdated, TourSuspended, TourDeleted, TourPricingTierChanged). `IBookingCommissionLookup` and `IDiscountEvaluator` remain as stub/no-op (Finance cross-module + Promotions module not yet built).

3. **Background services**: 9 exist in Booking.Infrastructure (SlotGeneration, SlotLockCleanup, BookingAutoExpire, ProviderAutoAccept, JoinRequestExpiry, BookingAutoComplete, DocumentExpiryCheck, BookingReminderService, SlotCleanupService). `RefundRetryService` is in Finance.

4. **Inbound event handlers added (W3-A)**: `TourSuspendedCancelBookingsHandler`, `TourDeletedCancelBookingsHandler`, `GuideOfferingSuspendedCancelBookingsHandler` all created. `GuideTourOfferingSuspendedIntegrationEvent` added to ContentTours.Contracts and published by `SuspendGuideOfferingCommandHandler`.

5. **GuideDiscount FK model differs**: Actual uses `GuideUserId` + optional `TourId` (guide-level), not `GuideTourOfferingId` (offering-level). No `MinParticipants` condition. Naming: `MaxUses` → `MaxUsageCount`, `FlatAmount` → `FixedAmount`.

6. **Availability slot routes**: Actual routes are flat (`/api/v1/booking/slots`) not nested (`/tours/{tourId}/availability`).

7. **BookingStatus enum**: Contains mixed legacy values (0-6) and new engine values (10-12). `Confirmed` reuses 1, `Completed` reuses 3, `Cancelled` reuses 4.

8. **Duplicate TourGuide**: `Booking.Domain.Entities.TourGuide` (30 lines) still exists with `TourGuideLanguage` and `TourGuideSpecialization`. Has 11 active code references (EF configs, handlers, repos). Removal requires migration + handler rewrites.

9. **Deferred entities**: `PackageBooking` and `Reservation` correctly moved to `_Deferred/` subfolder. `RefundPolicy` and `ProviderDocument` are in active Domain.

10. **Cross-module integration confirmed**: `GuideSuspendedCancelBookingsHandler` and `ProviderSuspendedCancelBookingsHandler` both work. `PaymentCompletedHandler` in Booking.Infrastructure correctly consumes `finance.payment.completed.v1` and auto-confirms bookings.
