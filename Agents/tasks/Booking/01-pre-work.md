# Booking — Pre-Work (PW-1..PW-8)

> **Owner:** Tech Lead (sole driver)
> **Branch:** `sprint/booking-prework` cut from `main` on 2026-06-12 17:00
> **Hard merge deadline:** Tue **2026-06-16 17:00** — feature work cannot start before this lands

Pre-work removes structural blockers that would compound across the sprint. Each item has **Current bug** → **Required fix** → **Acceptance gate**.

---

## PW-1 — Audit `BookingUnitOfWork` for domain-event dispatch (CRITICAL)

### Current bug

`Booking.Infrastructure/Persistence/BookingUnitOfWork.cs` either does not yet exist or calls `_context.SaveChangesAsync(ct)` directly. This is the same bug ContentBlogs/ContentSeo hit in W4 PW-1 and ContentTours hit two sprints ago.

If domain-event dispatch is bypassed, every domain event raised by `TourBooking`, `SlotLock`, `AvailabilitySlot`, `ProviderDocument` is silently dropped. The booking engine WILL break.

### Required fix

`BookingUnitOfWork` must delegate to `IUnitOfWork<BookingDbContext>` from `YallaJo.SharedKernel.Infrastructure`:

```csharp
internal sealed class BookingUnitOfWork(
    BookingDbContext context,
    IUnitOfWork<BookingDbContext> innerUnitOfWork) : IBookingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => innerUnitOfWork.SaveChangesAsync(ct);

    // expose AddRange/Update/Remove via context if needed (or write per-repo)
}
```

Register in `BookingInfrastructureRegistration.cs`:
```csharp
services.AddScoped<IUnitOfWork<BookingDbContext>, UnitOfWork<BookingDbContext>>();
services.AddScoped<IBookingUnitOfWork, BookingUnitOfWork>();
```

### Acceptance gate

1. New unit test file `tests/Booking.Tests.Unit/Persistence/BookingUnitOfWorkDispatchesEventsTests.cs` (xunit 2.9.3 + NSubstitute 5.3.0 + FluentAssertions 7.0.0 + EF InMemory 9.0.15).
2. Test 1: `SaveChangesAsync_RaisesAggregateRoot_DispatchesAllDomainEventsViaIPublisher`.
3. Test 2: `SaveChangesAsync_NoAggregateChanges_DoesNotInvokePublisher`.
4. Both tests green; CI badge on `sprint/booking-prework`.

---

## PW-2 — Add `IAggregateRoot` markers + verify base classes

### Current state

Booking entities exist but their `IAggregateRoot` markers may be missing. Without them, `EfRepository<T>` cannot be used and domain events are not dispatched (UoW scans only aggregates — Gotcha #1 + #6 + #27 from agent-context.md §9.1).

### Required fix

Audit and apply in `Booking.Domain/Entities/`:

| Entity | Required Base | IAggregateRoot? | Notes |
|---|---|---|---|
| `TourBooking` | `AuditableEntity` | ✅ Yes | The booking aggregate root |
| `AvailabilitySlot` | `AuditableEntity` | ✅ Yes | Stand-alone aggregate (owned by provider, not by tour) |
| `RefundPolicy` | `AuditableEntity` | ✅ Yes | Stand-alone aggregate (tour-scoped lookup) |
| `JoinRequest` | `AuditableEntity` | ✅ Yes | Stand-alone aggregate (lifecycle independent of parent booking after creation) |
| `ProviderDocument` | `AuditableEntity` | ✅ Yes | Stand-alone aggregate (DocumentExpiryCheckService scans this) |
| `SlotLock` | `BaseEntity` (immutable lock row) | ❌ No | Junction-like, 10-min TTL, no business invariants beyond "delete when expired" |
| `Reservation` | `AuditableEntity` | (Out of scope) | Defer to Wave 5 second-half (see README §8) |
| `PackageBooking` | `AuditableEntity` | (Out of scope) | Defer |
| `TourGuide` | `AuditableEntity` | (Out of scope) | Defer |
| `TourGuideLanguage` | `BaseEntity` (junction) | ❌ No | Defer |
| `TourGuideSpecialization` | `BaseEntity` (junction) | ❌ No | Defer |

Migration name: `BookingAddAggregateRootAndAuditMembers`.

If any in-scope entity is currently on `BaseEntity` only, upgrade to `AuditableEntity` (adds `CreatedBy`, `UpdatedBy`, `IsDeleted`, `DeletedAt`, `RowVersion` columns).

### Acceptance gate

1. `dotnet build YallaJo.sln` green.
2. Migration `BookingAddAggregateRootAndAuditMembers` exists in `Booking.Infrastructure/Migrations/`.
3. EF `Update-Database` against the dev SQL Server succeeds without conflicts.
4. Reflection test `tests/Booking.Tests.Unit/AggregateRootMarkersTest.cs` asserts: `TourBooking`, `AvailabilitySlot`, `RefundPolicy`, `JoinRequest`, `ProviderDocument` all implement `IAggregateRoot`.

---

## PW-3 — Seed 14 domain event records (stubs, body comes later)

### Required fix

Create `Booking.Domain/Events/` (new folder) with one record per event below. Each is a `public sealed record XDomainEvent(...) : IDomainEvent;` declaration only — handler implementation comes in feature tasks.

| # | File | Trigger source (feature task) |
|---|---|---|
| 1 | `TourBookingCreatedDomainEvent.cs` | TASK 4 Step 4 |
| 2 | `TourBookingConfirmedDomainEvent.cs` | TASK 5 (provider confirm OR Finance webhook flips to Confirmed) |
| 3 | `TourBookingCancelledDomainEvent.cs` | TASK 5 cancel |
| 4 | `TourBookingCompletedDomainEvent.cs` | TASK 5 complete |
| 5 | `TourBookingRejectedDomainEvent.cs` | TASK 5 reject |
| 6 | `TourBookingPaymentExpiredDomainEvent.cs` | TASK 7 BookingAutoExpireService |
| 7 | `SlotLockCreatedDomainEvent.cs` | TASK 4 Step 2 |
| 8 | `SlotLockReleasedDomainEvent.cs` | TASK 4 step rollback OR SlotLockCleanupService |
| 9 | `AvailabilitySlotCapacityChangedDomainEvent.cs` | TASK 1 PUT slot, TASK 4 Step 2 decrement |
| 10 | `JoinRequestCreatedDomainEvent.cs` | TASK 6 create |
| 11 | `JoinRequestApprovedDomainEvent.cs` | TASK 6 approve |
| 12 | `JoinRequestRejectedDomainEvent.cs` | TASK 6 reject |
| 13 | `ProviderDocumentExpiringDomainEvent.cs` | TASK 7 DocumentExpiryCheckService |
| 14 | `ProviderDocumentExpiredDomainEvent.cs` | TASK 7 DocumentExpiryCheckService |

### Acceptance gate

`dotnet build YallaJo.Booking.Domain` green with all 14 records compiled. Empty test stub `tests/Booking.Tests.Unit/Events/DomainEventsCompileTest.cs` instantiates each one with sample data to catch param signature drift.

---

## PW-4 — Seed 12 integration event records in `Booking.Contracts`

### Required fix

Create `Booking.Contracts/IntegrationEvents/` (new folder) with one record per logical event. Each follows pattern:

```csharp
public sealed record TourBookingCreatedIntegrationEvent(
    Guid BookingId,
    Guid TourId,
    Guid UserId,
    Guid SlotId,
    string Currency,
    decimal TotalAmount,
    int ParticipantCount,
    Guid ProviderId,
    DateTime OccurredAt) : IIntegrationEvent;
```

Mirror the master README §5 table 1:1. Register every record in `IntegrationEventTypeRegistry` using logical name `booking.{entity-kebab}.{verb}.v1`.

### Acceptance gate

1. `IntegrationEventTypeRegistry.Get("booking.tour-booking.created.v1")` returns `typeof(TourBookingCreatedIntegrationEvent)`.
2. Reverse-parity test: every `IIntegrationEvent` record in `Booking.Contracts` has a registry entry (drift-proof).
3. New test file `tests/Booking.Tests.Unit/IntegrationEventRegistryParityTest.cs`.

---

## PW-5 — Seed 8 repository interfaces (compile-only stubs)

### Required fix

Create the following Application-layer interfaces. Implementations come in feature tasks; PW only seeds signatures so command/query handlers can be drafted in parallel.

| Interface (in `Booking.Application/Interfaces/`) | Implementation (in `Booking.Infrastructure/Repositories/`) |
|---|---|
| `ITourBookingRepository` (extends `IReadRepository<TourBooking,Guid>`, `IWriteRepository<TourBooking,Guid>`) | `TourBookingRepository : EfRepository<TourBooking>` |
| `IAvailabilitySlotRepository` | `AvailabilitySlotRepository : EfRepository<AvailabilitySlot>` |
| `IRefundPolicyRepository` | `RefundPolicyRepository : EfRepository<RefundPolicy>` |
| `IJoinRequestRepository` | `JoinRequestRepository : EfRepository<JoinRequest>` |
| `IProviderDocumentRepository` | `ProviderDocumentRepository : EfRepository<ProviderDocument>` |
| `ICommissionRuleRepository` | `CommissionRuleRepository : EfRepository<CommissionRule>` — BUT `CommissionRule` lives in `Finance.Domain`; Booking only needs a read-only wrapper. **Drop this — Booking will inject `ICommissionLookupService` from Finance.Contracts instead** (PW-6). |
| `ISlotLockRepository` (non-aggregate: `IReadRepository + IWriteRepository`) | `SlotLockRepository : EfEntityRepository<SlotLock, Guid>` |
| `IBookingOutboxWriter` | `BookingOutboxWriter` — write `OutboxMessage` rows for non-aggregate handlers per Gotcha #25 |

Custom finder methods to add:

- `ITourBookingRepository.GetByReferenceAsync(string reference, CancellationToken ct)` → finds by `YJ-YYYYMMDD-XXXXXX`.
- `ITourBookingRepository.GetActiveByUserAsync(Guid userId, CancellationToken ct)` → for "max 3 concurrent pending" rule.
- `ITourBookingRepository.GetAwaitingPaymentOlderThanAsync(TimeSpan age, CancellationToken ct)` → for BookingAutoExpireService.
- `IAvailabilitySlotRepository.GetByTourAndDateAsync(Guid tourId, DateOnly date, CancellationToken ct)`.
- `ISlotLockRepository.GetExpiredAsync(DateTime cutoffUtc, CancellationToken ct)`.
- `IProviderDocumentRepository.GetExpiringWithinAsync(TimeSpan window, CancellationToken ct)`.

### Acceptance gate

`dotnet build YallaJo.Booking.Application` and `YallaJo.Booking.Infrastructure` green. Implementation methods may throw `NotImplementedException` for now; feature tasks fill them.

---

## PW-6 — Cross-module Finance contracts: `ICommissionLookupService` + `IPaymentGateway` stub

### Why

Booking's TASK 4 Step 3 calculates commission **per-booking** using subscription tier. Booking module must NOT reference `Finance.Domain` (Gotcha: cross-module references only via Contracts). Finance sprint will provide the implementation; Booking PW seeds the interface stub.

### Required fix

In `Finance.Contracts/Services/` (NEW folder):

```csharp
public interface ICommissionLookupService
{
    /// Calculate commission for a single booking given provider subscription tier.
    /// Returns CommissionResult { Rate, Amount, Currency, TierName }.
    Task<CommissionResult> CalculateAsync(
        Guid providerId,
        decimal bookingAmountAfterDiscount,
        string currency,
        CancellationToken ct);
}

public sealed record CommissionResult(
    decimal Rate,         // e.g., 0.15 for 15%
    decimal Amount,       // bookingAmountAfterDiscount * Rate
    string Currency,
    string TierName);     // "Free" / "Basic" / "Premium" / "Enterprise"
```

Stub impl in `Finance.Infrastructure` returning `new CommissionResult(0.15m, amount * 0.15m, currency, "Free")` for now. Real tiered logic comes in Finance sprint.

In `Finance.Contracts/Services/IDiscountEvaluator.cs`:

```csharp
public interface IDiscountEvaluator
{
    Task<DiscountResult> EvaluateAsync(
        Guid tourId,
        Guid userId,
        decimal subtotal,
        string currency,
        string? promoCode,
        CancellationToken ct);
}

public sealed record DiscountResult(
    decimal AppliedAmount,        // 0 if none
    string? PromoCodeUsed,
    IReadOnlyList<DiscountAttribution> Attributions)
{
    public static readonly DiscountResult None = new(0m, null, []);
}

public sealed record DiscountAttribution(Guid DiscountId, decimal Amount, string Type);
```

Stub impl returns `DiscountResult.None`.

### Acceptance gate

`Booking.Application.DependencyInjection` registers stub via `services.AddScoped<ICommissionLookupService, FinanceContractsCommissionStub>()`. Booking integration test `EngineCalculatesCommissionViaContractInterfaceTest` passes.

---

## PW-7 — Permission catalog `BookingFeatures` + `BookingPermissionCatalog`

### Required fix

`Booking.Contracts/Authorization/BookingFeatures.cs`:

```csharp
public static class BookingFeatures
{
    public const string AvailabilitySlot = nameof(AvailabilitySlot);
    public const string RefundPolicy = nameof(RefundPolicy);
    public const string Commission = nameof(Commission);
    public const string ProviderDocument = nameof(ProviderDocument);
    public const string TourBooking = nameof(TourBooking);
    public const string JoinRequest = nameof(JoinRequest);
    public const string ProviderBookingDashboard = nameof(ProviderBookingDashboard);
    public const string AdminBookingDashboard = nameof(AdminBookingDashboard);
}
```

`Booking.Contracts/Authorization/BookingPermissionCatalog.cs` — register the following 26 permissions (each `(feature, action)` pair):

| Feature | Actions |
|---|---|
| AvailabilitySlot | View, Create, Edit, Delete |
| RefundPolicy | View, Create, Edit, Delete |
| Commission | View, Create, Edit, Delete |
| ProviderDocument | View, Create, Edit, Delete |
| TourBooking | View, Create, Cancel, Complete, Confirm, Reject |
| JoinRequest | Create, Approve, Reject |
| ProviderBookingDashboard | View |
| AdminBookingDashboard | View |

Register in `BookingApplicationRegistration.cs`:
```csharp
services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();
```

### Acceptance gate

On `dotnet run --project YallaJo.Api` startup, Serilog logs:
```
[INFO] PermissionSeeder discovered 7 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking
[INFO] PermissionSeeder inserted/verified 26 Booking permissions in security.Permissions
```

---

## PW-8 — Test project scaffolds

### Required fix

Create two test projects:

```
tests/
  Booking.Tests.Unit/
    Booking.Tests.Unit.csproj
    (xunit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, EF InMemory 9.0.15)
  Booking.IntegrationTests/
    Booking.IntegrationTests.csproj
    (Microsoft.AspNetCore.Mvc.Testing 9.0.15 + WebApplicationFactory<Program>)
```

Add `<InternalsVisibleTo Include="Booking.Tests.Unit" />` and `<InternalsVisibleTo Include="Booking.IntegrationTests" />` to:
- `Booking.Application/Booking.Application.csproj`
- `Booking.Infrastructure/Booking.Infrastructure.csproj`

Seed one round-trip sanity test in each:
- Unit: `BookingUnitOfWorkDispatchesEventsTests.cs` (covered by PW-1 acceptance).
- Integration: `EventDispatchSanityTests.cs` — creates a `TourBooking` aggregate via `ServiceCollection.BuildServiceProvider()` + `AddBookingInfrastructure()` with InMemory EF swap, calls `SaveChangesAsync`, asserts MediatR `IPublisher.Publish` was invoked (substitute).

Note: use `ServiceCollection.BuildServiceProvider()` swap pattern, NOT `WebApplicationFactory<Program>` yet — full WAF tests come in feature tasks.

### Acceptance gate

`dotnet test tests/Booking.Tests.Unit` and `dotnet test tests/Booking.IntegrationTests` both green with at least 1 test each.

---

## Pre-Work Sign-Off

| PW | Description | Owner | Done? | Date | Reviewer |
|---|---|---|---|---|---|
| PW-1 | BookingUnitOfWork delegates to IUnitOfWork<TContext> | Tech Lead | ☐ | | |
| PW-2 | IAggregateRoot markers + AuditableEntity upgrades | Tech Lead | ☐ | | |
| PW-3 | 14 domain event records seeded | Tech Lead | ☐ | | |
| PW-4 | 12 integration events seeded + registry registration | Tech Lead | ☐ | | |
| PW-5 | 8 repository interfaces + stub impls | Tech Lead | ☐ | | |
| PW-6 | ICommissionLookupService + IDiscountEvaluator stubs | Tech Lead | ☐ | | |
| PW-7 | BookingFeatures + BookingPermissionCatalog (26 perms) | Tech Lead | ☐ | | |
| PW-8 | Test projects scaffolded | Tech Lead | ☐ | | |

All 8 boxes ticked + PR `sprint/booking-prework → main` merged by **2026-06-16 17:00**.
