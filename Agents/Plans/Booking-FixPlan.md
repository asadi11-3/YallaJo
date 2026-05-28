# Booking-Workflow.md — Fix Plan

**Created**: 2025-01-27
**Source**: `Agents/Plans/Booking-Audit-Report.md`
**Total Fixes**: 10
**Estimated Effort**: 4-6 hours (doc fixes) + substantial new feature work deferred
**Build Gate**: Fix 9 — full solution build after all code changes

---

## Design Decisions

1. **Payment flow stays in Finance** — IPaymentGateway is correctly owned by Finance. Plan doc should be updated to reflect actual architecture rather than moving code.
2. **Stubs are intentional for now** — replacing stubs requires cross-module readers which depend on other modules being stable. Document as "Phase 3-4 pending" rather than fixing now.
3. **TourArchived / GuideOfferingSuspended** — events don't exist anywhere. Plan should mark these as "to be created when ContentTours publishes them."
4. **BookingStatus enum cleanup** — risky (may break migrations/data). Document but don't change.
5. **GuideDiscount FK model** — actual design (GuideUserId + optional TourId) is arguably better than plan (GuideTourOfferingId). Update plan to match code.
6. **Duplicate TourGuide** — has 11 active references across EF configs, handlers, and repos. Safe removal requires migration + handler rewrites. Defer.

---

## Fix Execution Order

```
Fix 1 (HIGH): Update plan — payment architecture     ─┐
Fix 2 (HIGH): Update plan — stub services status       │
Fix 3 (MEDIUM): Update plan — background services      ├─ All parallel (doc only)
Fix 4 (MEDIUM): Update plan — missing events            │
Fix 5 (MEDIUM): Update plan — GuideDiscount model      │
Fix 6 (MEDIUM): Update plan — route corrections        │
Fix 7 (MEDIUM): Update plan — endpoint gaps             │
Fix 8 (LOW): Update plan — cleanup phase status        ─┘
                    │
Fix 9: Build verify (no code changes needed)
                    │
Fix 10: Create 2 missing background services (CODE)
```

---

## Fixes

### Fix 1 (HIGH): Correct Payment Architecture in Plan Doc
**What**: Plan Section 6 (Payment Gateway) claims IPaymentGateway is in Booking.Contracts with 4 methods. Actually:
- IPaymentGateway is in `Finance.Contracts.Services` with: InitiateAsync, RefundAsync, PayoutAsync, VerifyWebhookSignatureAsync
- InitiatePaymentCommand lives in `Finance.Application.Commands.InitiatePayment`
- PaymentCompletedIntegrationEvent is in `Finance.Contracts`, registered as `finance.payment.completed.v1`
- PaymentCompletedHandler in Booking.Infrastructure auto-confirms bookings
- RefundRetryService is in Finance.Infrastructure, not Booking

**Action**: Rewrite plan Section 6 to describe actual Finance→Booking integration pattern. Remove claim that Booking owns payment gateway. Add cross-reference to Finance-Workflow.md.

**Files**: `Booking-Workflow.md` Section 6
**Risk**: None (documentation only)

### Fix 2 (HIGH): Document Stub Service Status
**What**: Plan Phases 3-4 say "replace all stubs with real cross-module readers." All 5 stubs are still active.

**Action**: Add Implementation Notes section documenting:
- 5 interfaces are correctly defined in Booking.Application.Interfaces
- Current stub registrations are intentional until cross-module readers are built
- Dependency: ContentTours/ContentPlaces/Accounts modules must expose read services first

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 3 (MEDIUM): Correct Background Services Section
**What**: Plan claims 9 background services. Actual:
- 7 in Booking.Infrastructure (including extra DocumentExpiryCheckService)
- RefundRetryService in Finance.Infrastructure (not Booking)
- BookingReminderService + SlotCleanupService don't exist

**Action**: Update plan to list actual 7 + reference Finance for RefundRetry. Mark Reminder and SlotCleanup as "NOT BUILT — create in Phase 6."

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 4 (MEDIUM): Mark Missing Inbound Events
**What**: Plan expects handlers for TourArchivedIntegrationEvent and GuideOfferingSuspendedIntegrationEvent. Neither event exists in the entire codebase — ContentTours doesn't publish them.

**Action**: Update plan to note: "Blocked on ContentTours publishing TourArchived and GuideOfferingSuspended events. Create handler stubs when events are defined."

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 5 (MEDIUM): Update GuideDiscount Entity Description
**What**: Plan describes GuideDiscount with GuideTourOfferingId FK and MinParticipants. Actual uses GuideUserId + optional TourId (more flexible design).

**Action**: Update plan entity table to match actual implementation. Note naming differences (MaxUses→MaxUsageCount, FlatAmount→FixedAmount).

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 6 (MEDIUM): Correct Route Paths
**What**: Plan says availability at `/tours/{tourId}/availability`. Actual is flat `/api/v1/booking/slots`.

**Action**: Update all route tables in plan to match actual endpoint structure.

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 7 (MEDIUM): Document Missing Endpoints
**What**: Several planned endpoints don't exist:
- Payment: POST /{id}/pay, POST /payments/webhook, GET /{id}/payment-status, GET /my/payment-history
- Slots: POST /bulk
- Dashboard: guide booking dashboard queries
- Operations: CheckInBooking, MarkNoShow

**Action**: Mark each as "NOT BUILT" with cross-reference to owning module (payment → Finance) or phase dependency.

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 8 (LOW): Update Cleanup Phase Status
**What**: Phase 11 (cleanup) items not done:
- Duplicate TourGuide still in Booking.Domain (11 active references)
- BookingStatus mixed legacy + engine values
- PackageBooking/Reservation in _Deferred (correctly)

**Action**: Add status notes to Phase 11. TourGuide removal requires migration + handler rewrites — mark as "deferred until migration sprint."

**Files**: `Booking-Workflow.md`
**Risk**: None

### Fix 9 (MEDIUM): Build Verify
**What**: Confirm solution still builds after any code changes.
**Action**: `dotnet build YallaJo.sln --no-restore`
**Risk**: None (no code changes in Fixes 1-8)

### Fix 10 (MEDIUM): Create Missing Background Services
**What**: BookingReminderService and SlotCleanupService are absent.

**BookingReminderService**: Send reminders N hours before booking start time.
- Pattern: Follow BookingAutoExpireService template
- Query: confirmed bookings where slot date/time is within reminder window
- Publish: BookingReminderIntegrationEvent (new) → Messaging module consumes

**SlotCleanupService**: Remove old/past availability slots.
- Pattern: Follow SlotLockCleanupService template  
- Query: slots where Date < today - RetentionDays and BookedCount = 0
- Action: Soft-delete or hard-delete inactive past slots

**Files**: 2 new service files + 1 new integration event + 1 new domain event + DI registration + registry entry
**Risk**: LOW — additive, no existing code changes

---

## File Impact Summary

| Category | Count | Files |
|----------|-------|-------|
| Plan doc updates | 1 | Booking-Workflow.md |
| New services | 2 | BookingReminderService.cs, SlotCleanupService.cs |
| New events | 2 | BookingReminderIntegrationEvent + DomainEvent |
| Modified (DI) | 1 | DependencyInjection.cs |
| Modified (Registry) | 1 | IntegrationEventTypeRegistry.cs |
| **Total** | **7** | |

---

## Deferred Items (Out of Scope)

1. **Replace 5 stub services** — requires cross-module read service contracts in ContentTours, ContentPlaces, Accounts, Finance
2. **Remove duplicate TourGuide** from Booking.Domain — 11 code references, needs migration
3. **Clean BookingStatus enum** — risky with existing data/migrations
4. **Payment endpoints in Booking** — design question: should Booking have pay/status endpoints that delegate to Finance, or should clients call Finance directly?
5. **TourArchived/GuideOfferingSuspended handlers** — blocked on ContentTours publishing these events
6. **Bulk slot creation endpoint** — nice-to-have
7. **CheckInBooking / MarkNoShow commands** — operations phase feature
8. **Guide booking dashboard** — depends on cross-module queries

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Reminder service sends duplicate notifications | Use inbox/dedup pattern like other services |
| SlotCleanup deletes slots with active bookings | Query filter: BookedCount = 0 AND no active locks |
| Plan doc changes create confusion | Add "Audited: date" header and changelog |
