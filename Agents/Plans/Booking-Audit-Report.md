# Booking-Workflow.md — Audit Report

**Audited**: 2025-01-27
**Plan Document**: `Agents/Plans/Booking-Workflow.md` (892 lines, 14 design decisions, 11 execution phases)
**Method**: Three-pass verification — entity/domain, application/infrastructure, cross-module integration
**Overall Score**: 7.2 / 10

---

## Executive Summary

The Booking module's core domain is **solidly implemented**: all 6 primary entities (TourBooking, JoinRequest, AvailabilitySlot, SlotLock, GuideDiscount, RefundPolicy) have full state machines, value objects, and domain events. The integration event pipeline (12 events + registry) is complete. However, the **payment flow** — the plan's centerpiece — is split across Booking and Finance modules in a way the plan doesn't describe, and critical pieces are missing. Five stub services remain active, and 2-3 background services are absent.

### Score Breakdown

| Dimension | Score | Notes |
|-----------|-------|-------|
| Domain Model Completeness | 9/10 | All entities, enums, VOs, state machines implemented |
| Application Layer | 7/10 | 15 commands + 6 queries exist; payment commands missing |
| Infrastructure | 7/10 | 7/9 background services; 5 stubs; 2 missing event handlers |
| Endpoints | 6/10 | Core CRUD present; payment + bulk + dashboard routes missing |
| Cross-Module Integration | 7/10 | Payment flow partially wired via Finance; 2 inbound handlers missing |
| Plan Accuracy | 7/10 | Payment gateway ownership wrong; several property name drifts |

---

## Phase-by-Phase Verification

### Phase 1: Core Entities — FULLY DONE

**TourBooking.cs** (357 lines) — ALL plan properties + methods present.
- Factory: `Create(userId, tourId, providerId, guideId, slotId, count, pricing, reference, refundPolicy, isInstant, paymentExpires, lineItems, isPrivate, joinedFromBookingId)`
- State machine: AwaitingPayment → PendingConfirmation → Confirmed → Completed; also Rejected/Cancelled paths
- Value objects: `BookingPricing`, `BookingReference`, `BookingLineItem`, `BookingCancellationContext`
- Domain events: Created, Confirmed, Rejected, Cancelled, Completed, PaymentExpired — ALL raised
- Extra (not in plan): `IsInstantBooking`, `PaymentExpiresAt`, `SpecialRequests`

**JoinRequest.cs** (133 lines) — FULLY IMPLEMENTED (not a shell).
- Methods: Create → Result, Approve → Result, Reject → Result, Expire → Result, AttachResultingBooking → Result, IsExpired
- Has: AvailabilitySlotId, ExpiresAt (48h TTL), ResultingBookingId
- Domain events: Created, Approved, Rejected, Expired

**AvailabilitySlot.cs** (211 lines) — FULLY IMPLEMENTED.
- Factory methods: `CreateForTour()`, `CreateForBusiness()`
- Capacity: Lock(), ReleaseLock(), ConfirmBooking(), ReleaseBooking()
- Computed: `AvailableCount = MaxCapacity - BookedCount - LockedCount`
- Has: TourGuideId, SlotType, ScheduleId, ServiceItemId, PriceOverride

**SlotLock.cs** (98 lines) — Complete with Create, AttachBookingId, Release, IsExpired.

**GuideDiscount.cs** (160 lines) — Implemented but DIFFERS from plan:
| Plan Says | Actual | Impact |
|-----------|--------|--------|
| `GuideTourOfferingId` FK | `GuideUserId` + optional `TourId` | Different relationship model |
| `MinParticipants` | Not present | Missing discount condition |
| `MaxUses` / `CurrentUses` | `MaxUsageCount` / `CurrentUsageCount` | Naming only |
| `GuideDiscountType.FlatAmount` | `FixedAmount` | Naming only |

**RefundPolicy.cs** — Exists in Domain (not deferred). Not detailed in plan but present.

### Phase 2: Enums — ALL PRESENT

| Enum | Values | Match |
|------|--------|-------|
| BookingStatus | Pending=0, Confirmed=1, InProgress=2, Completed=3, Cancelled=4, Refunded=5, NoShow=6, AwaitingPayment=10, PendingConfirmation=11, Rejected=12 | ⚠️ Mixed legacy (0-6) + engine (10-12) values |
| JoinRequestStatus | Pending=0, Approved=1, Rejected=2, Cancelled=3, Expired=4 | ✅ EXACT |
| CancellationSource | Exists | ✅ |
| ConfirmationSource | Manual, AutoAccept, PaymentWebhook | ✅ |
| GuideDiscountType | Percentage, FixedAmount | ⚠️ Plan says FlatAmount |
| SlotType | Exists | ✅ |

### Phase 3-4: Payment Gateway — ARCHITECTURE DIFFERS FROM PLAN

**Plan says**: IPaymentGateway in `Booking.Contracts` with 4 methods (InitiatePayment, ProcessWebhook, InitiateRefund, GetPaymentStatus).

**Actual**: IPaymentGateway in **`Finance.Contracts.Services`** with different methods:
- `InitiateAsync` / `RefundAsync` / `PayoutAsync` / `VerifyWebhookSignatureAsync` + `GatewayName`
- Also defines: InitiateRequest/Result, RefundRequest/Result, PayoutRequest/Result

**Payment flow is partially wired**:
- `Finance.Application.Commands.InitiatePayment` exists — command + handler
- `PaymentCompletedIntegrationEvent` in Finance.Contracts, registered as `finance.payment.completed.v1`
- `PaymentCompletedHandler` in **Booking.Infrastructure** consumes it → auto-confirms booking
- Analytics also consumes PaymentCompleted

**MISSING from Booking side**:
- No `POST /{id}/pay` endpoint in Booking
- No `POST /payments/webhook` endpoint in Booking (Finance owns this)
- No `GET /{id}/payment-status` endpoint
- No `TourBookingPaidIntegrationEvent` (Finance uses `PaymentCompletedIntegrationEvent` instead)

### Phase 5: Stub Services — ALL 5 STILL ACTIVE

| Stub | Interface | Status |
|------|-----------|--------|
| `StubBookingTourSnapshotReader` | `IBookingTourSnapshotReader` | Returns fake data |
| `StubBookingPricingSnapshotReader` | `IBookingPricingSnapshotReader` | Returns null/empty |
| `StubBookingProviderSnapshotReader` | `IBookingProviderSnapshotReader` | Returns active for any ID |
| `StubBookingCommissionLookup` | `IBookingCommissionLookup` | Returns 10% flat |
| `NoOpDiscountEvaluator` | `IDiscountEvaluator` | Returns zero discount |

Plan Phases 3-4 say to replace all stubs with real cross-module readers.

### Phase 6: Background Services — 7 of 9 Exist

| Service | Status | Location |
|---------|--------|----------|
| SlotGenerationService | ✅ EXISTS (116L) | Booking.Infrastructure |
| SlotLockCleanupService | ✅ EXISTS | Booking.Infrastructure |
| BookingAutoExpireService | ✅ EXISTS | Booking.Infrastructure |
| ProviderAutoAcceptService | ✅ EXISTS | Booking.Infrastructure |
| JoinRequestExpiryService | ✅ EXISTS | Booking.Infrastructure |
| BookingAutoCompleteService | ✅ EXISTS | Booking.Infrastructure |
| DocumentExpiryCheckService | ✅ EXISTS (extra) | Booking.Infrastructure |
| BookingReminderService | ❌ MISSING | — |
| SlotCleanupService | ❌ MISSING | — |
| RefundRetryService | ⚠️ In Finance | Finance.Infrastructure (not Booking!) |

### Phase 7-8: Integration Events — Complete

12 integration events in Booking.Contracts, all registered in IntegrationEventTypeRegistry:
- TourBooking: Created, Confirmed, Cancelled, Completed, PaymentExpired, Rejected (6)
- JoinRequest: Created, Approved, Rejected (3)
- SlotLock: Created, Released (2)
- AvailabilitySlot: CapacityChanged (1)

### Phase 9: Event Handlers — 2 Missing

**Existing inbound handlers**:
- `GuideSuspendedCancelBookingsHandler` — consumes ContentTours.TourGuideSuspendedIntegrationEvent ✅
- `ProviderSuspendedCancelBookingsHandler` — consumes Accounts.ProviderSuspendedIntegrationEvent ✅
- `PaymentCompletedHandler` — consumes Finance.PaymentCompletedIntegrationEvent ✅
- `SlotCapacityRestoreHandlers` — internal capacity management ✅

**Missing**:
- ❌ `TourArchivedIntegrationEventHandler` — event doesn't exist in entire codebase
- ❌ `GuideOfferingSuspendedIntegrationEventHandler` — event doesn't exist in entire codebase

### Phase 10: Endpoints — Core Present, Gaps Exist

**TourBookingEndpoints.cs** (420L) — 8 routes:
- POST /tour, GET /{id}, GET /my-bookings, GET /admin/all, POST /{id}/confirm, POST /{id}/reject, POST /{id}/cancel, POST /{id}/complete
- ❌ Missing: POST /{id}/pay, GET /{id}/payment-status, POST /payments/webhook, GET /my/payment-history

**AvailabilitySlotEndpoints.cs** (136L) — 4 routes:
- ⚠️ Route: `/api/v1/booking/slots` (plan says `/tours/{tourId}/availability`)
- ❌ Missing: POST /bulk (bulk slot creation)

**JoinRequestEndpoints.cs** (101L) — routes present ✅
**GuideDiscountEndpoints.cs** (109L) — routes present ✅
**AdminBookingEndpoints.cs** (51L) — admin routes present ✅

### Phase 11: Cleanup — NOT DONE

- ❌ Duplicate `TourGuide.cs` (30L) still in Booking.Domain with TourGuideLanguage + TourGuideSpecialization
- ✅ PackageBooking + Reservation moved to `_Deferred/` subfolder
- `BookingStatus` has mixed legacy (0-6) + engine (10-12) values — plan says clean up

---

## Additional Findings

### Application Layer Inventory
- **15 command folders** (40 files): AdminForceRefund, ApproveJoinRequest, CancelTourBooking, CompleteTourBooking, ConfirmTourBooking, CreateAvailabilitySlot, CreateGuideDiscount, CreateTourBooking, DeactivateAvailabilitySlot, DeactivateGuideDiscount, RejectJoinRequest, RejectTourBooking, SubmitJoinRequest, UpdateAvailabilitySlot, UpdateGuideDiscount
- **6 query folders** (23 files): GetAllBookings, GetAvailabilitySlots, GetJoinRequests, GetMyBookings, GetMyGuideDiscounts, GetTourBookingById
- **Missing commands**: InitiatePayment (in Finance), CheckInBooking, MarkNoShow
- **Missing queries**: Guide booking dashboard queries, payment history

### DI Registration
All repositories, background services, stubs, and cross-module services properly registered.

### Repos (7 domain + 7 infrastructure)
AvailabilitySlot, GuideDiscount, JoinRequest, ProviderDocument, RefundPolicy, SlotLock, TourBooking

### EF Configurations (11)
All entities + OutboxMessage + TourGuide(duplicate) + TourGuideLanguage + TourGuideSpecialization

---

## Discrepancy Summary

| # | Issue | Severity |
|---|-------|----------|
| 1 | Payment gateway ownership (plan: Booking.Contracts, actual: Finance.Contracts) | HIGH |
| 2 | Payment endpoints missing from Booking (pay, webhook, status) | HIGH |
| 3 | 5 stub services still active | HIGH |
| 4 | 2 background services missing (Reminder, SlotCleanup) | MEDIUM |
| 5 | RefundRetryService in Finance, not Booking | MEDIUM |
| 6 | GuideDiscount FK model differs (GuideUserId vs GuideTourOfferingId) | MEDIUM |
| 7 | Availability slot routes differ (flat vs per-tour) | MEDIUM |
| 8 | 2 inbound events don't exist in codebase (TourArchived, GuideOfferingSuspended) | MEDIUM |
| 9 | Duplicate TourGuide in Booking.Domain not removed | MEDIUM |
| 10 | BookingStatus mixed legacy + engine values | LOW |
| 11 | GuideDiscountType naming: FlatAmount vs FixedAmount | LOW |
| 12 | Missing bulk slot creation endpoint | LOW |
| 13 | Missing guide booking dashboard endpoints | LOW |
| 14 | Missing CheckInBooking / MarkNoShow commands | LOW |
