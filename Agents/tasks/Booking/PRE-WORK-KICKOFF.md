# Booking — Pre-Work Kickoff Briefing

> Sprint window: 2026-06-15 → 08-13 (Wave 5). Owner: Mohammad.

## What's already wired (do NOT redo)

- `IBookingUnitOfWork` interface + `BookingUnitOfWork` delegate (no domain-event bypass risk).
- 6 aggregates marked `IAggregateRoot`: TourBooking, AvailabilitySlot, RefundPolicy, JoinRequest, ProviderDocument, SlotLock.
- 14 domain event records in `Booking.Domain/Events/` (TourBookingCreated/Confirmed/Cancelled/Completed/Rejected/PaymentExpired, SlotLockCreated/Released, AvailabilitySlotCapacityChanged, JoinRequestCreated/Approved/Rejected, ProviderDocumentExpiring/Expired).
- 12 integration event records in `Booking.Contracts/IntegrationEvents/` registered in `IntegrationEventTypeRegistry` with keys `booking.{aggregate}.{action}.v1`.
- 7 repository interfaces in `Booking.Domain/Repositories/` + 7 EF stubs in `Booking.Infrastructure/Repositories/`: ITourBookingRepository, IAvailabilitySlotRepository, IRefundPolicyRepository, IJoinRequestRepository, IProviderDocumentRepository, ISlotLockRepository, IBookingOutboxWriter.
- `ICommissionLookupService` + `CommissionResult` record in `Finance.Contracts/Services/` (stub `CommissionLookupService` returns Rate=0.10m).
- `BookingFeatures` (8 features) + `BookingPermissionCatalog` (26 perms) registered as `IPermissionCatalog` singleton in DI.
- Test projects: `tests/Booking.Tests.Unit/` + `tests/Booking.IntegrationTests/` in YallaJo.sln, build green.

## Day-0 sprint tasks

1. **Create EF migration** `BookingAddAggregateRootAndAuditMembers` — schema-no-op but locks in the marker change history.
2. **Replace stub `CommissionLookupService`** with the real Finance-side implementation (table-lookup against `CommissionRule` aggregate).
3. **Wire endpoints** under `Booking.Presentation/Endpoints/` — every endpoint must carry `[MustHavePermission(BookingFeatures.X, AppAction.Y)]` or `[AllowAnonymous]`.
4. **Implement repository methods** beyond the stub (the EF stubs ship with empty domain-specific methods; sprint adds query bodies).

## Watchpoints

- `AvailabilitySlot.RowVersion` is the optimistic concurrency token for slot capacity. Use `IAvailabilitySlotRepository.GetByIdWithLockAsync` (already declared) and let EF surface `DbUpdateConcurrencyException` → translate to `Result.Conflict`.
- `BookingOutboxWriter` requires `where TEvent : IIntegrationEvent` — pass strongly-typed integration events; do not stringly invoke.
