# Booking — Cross-Cutting Concerns

> Mirrors §S7 of predecessor sprint. Tech Lead enforces during PR review and at the sprint integration freeze (Sun 2026-08-09 17:00).

---

## 1. DI Audit

`Booking.Infrastructure/DependencyInjection.cs` MUST register **all of**:

| Registration | Symbol | Lifetime | Reason |
|---|---|---|---|
| DbContext factory | `IDbContextFactory<BookingDbContext>` | Singleton | Used by inbox/outbox processors + BG services |
| Pooled DbContext | `BookingDbContext` via `AddDbContextPool` | Scoped | Per-request handlers |
| Unit of Work | `IBookingUnitOfWork → BookingUnitOfWork` | Scoped | Mediates SaveChangesAsync + dispatches domain events |
| Inbox store | `IBookingInboxStore → BookingInboxStore` | Scoped | Idempotency for cross-module integration events |
| Outbox writer | `IBookingOutboxWriter → BookingOutboxWriter` | Scoped | Enqueue integration events in same transaction |
| Repositories | `ITourBookingRepository, IAvailabilitySlotRepository, ISlotLockRepository, IRefundPolicyRepository, IProviderDocumentRepository, IJoinRequestRepository, IBookingTourSnapshotRepository, IBookingProviderSnapshotRepository, IBookingTourPricingSnapshotRepository, IBookingCommissionSnapshotRepository` | Scoped | One concrete `Ef…Repository<T>` per interface |
| Cross-module Finance stubs | `ICommissionLookupService → CommissionLookupService` (Booking.Infrastructure impl reading BookingCommissionSnapshot), `IDiscountEvaluator → NullDiscountEvaluator` | Scoped | PW-6 contracts; Finance sprint replaces NullDiscountEvaluator |
| Reference generator | `IBookingReferenceGenerator → DefaultBookingReferenceGenerator` | Singleton | Stateless, hot-path; uses `RandomNumberGenerator` |
| 4 BackgroundService instances | (see 10-task) | Singleton | `AddHostedService<T>` |
| Permission catalog | `IPermissionCatalog → BookingPermissionCatalog` | Singleton | Discovered by PermissionSeeder |
| MediatR handlers | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(BookingApplicationMarker).Assembly))` | per-call | Application assembly markers ONLY |
| FluentValidation | `services.AddValidatorsFromAssembly(typeof(BookingApplicationMarker).Assembly, ServiceLifetime.Scoped, includeInternalTypes: true)` | Scoped | One per Command/Query |
| Diagnostics | `BookingDiagnostics` static — no DI; just referenced statically | – | ActivitySource + Meter |
| Cache key+tag builders | `IBookingCacheKeys → BookingCacheKeys` | Singleton | Centralizes `availability:tour:{tourId}` etc. (R10) |

**Common mistakes to fail-PR on:**
- ❌ Registering `BookingDbContext` as Scoped while ALSO calling `AddDbContextPool` (pool already gives scoped resolution; double-registration breaks tests).
- ❌ Registering `IBookingOutboxWriter` as Singleton (state leaks across requests).
- ❌ Registering MediatR with `typeof(Program).Assembly` — picks up wrong assembly.
- ❌ Forgetting `IPermissionCatalog` registration — silent failure, permissions absent from DB after seeder runs.
- ❌ Registering FluentValidators as `Transient` — duplicate-instance allocations on hot paths.

---

## 2. Permission Seeder Verification

Expected boot log on first run AFTER this sprint merges (assuming Booking is the 7th catalog registered):

```text
[INFO] PermissionSeeder discovered 7 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking
[INFO] PermissionSeeder inserted/verified 26 Booking permissions
```

**Permission inventory check** — run `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Booking.%'` after first boot; expect **26** (matches `BookingPermissionCatalog` size from PW-7).

**If you see less than 26:** likely cause = mismatch between `BookingFeatures` and what `BookingPermissionCatalog` enumerates. Check that every feature×action pair is listed in the catalog.

**Per-role assignment** is NOT this sprint's responsibility — the Admin grants permissions to roles via existing Security module endpoints after deployment.

---

## 3. Outbox Type-Registry Validation

Add to `tests/Booking.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs`:

```csharp
[Fact]
public void All_Booking_integration_event_records_registered_in_registry()
{
    var asm = typeof(TourBookingCreatedIntegrationEvent).Assembly;
    var declared = asm.GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IIntegrationEvent)) && !t.IsAbstract)
        .ToHashSet();

    var registered = IntegrationEventTypeRegistry.All
        .Where(kvp => kvp.Key.StartsWith("booking."))
        .Select(kvp => kvp.Value)
        .ToHashSet();

    declared.Should().BeEquivalentTo(registered, "every declared event needs a logical name in IntegrationEventTypeRegistry");
}
```

This is the "reverse parity" pattern from prior sprints — prevents the silent drift where someone adds a new integration event record but forgets to register the logical name, causing the outbox processor to skip publishing it.

**Logical names expected (12 from 03-entities-matrix.md):**
```
booking.tour-booking.created.v1
booking.tour-booking.confirmed.v1
booking.tour-booking.cancelled.v1
booking.tour-booking.completed.v1
booking.tour-booking.rejected.v1
booking.tour-booking.payment-expired.v1
booking.slot-lock.expired.v1
booking.provider-document.expiring.v1
booking.provider-document.expired.v1
booking.provider.suspended-doc-expired.v1
booking.join-request.approved.v1
booking.join-request.rejected.v1
```

**Inbox consumers (10 from 03-entities-matrix.md):**
```
content-tours.tour.published.v1
content-tours.tour.updated.v1
content-tours.tour.suspended.v1
content-tours.tour.deleted.v1
content-tours.tour-pricing.upserted.v1
content-tours.tour-pricing.deleted.v1
accounts.provider.status-changed.v1
accounts.provider.subscription-changed.v1
finance.commission-rule.upserted.v1     ← see 05-task §Inbox handlers
finance.commission-rule.deleted.v1
```

Plus from Finance sprint (out of scope here, but Booking pre-registers them for future):
```
finance.payment.completed.v1
finance.payment.failed.v1
finance.refund.completed.v1
```

---

## 4. Build Lock Workaround

`YallaJo.Web.exe` lock blocks full-solution builds. Work around per `agent-context.md` §0.2:

```powershell
# Build only the projects Booking touches:
dotnet build Booking/Booking.Domain/Booking.Domain.csproj
dotnet build Booking/Booking.Contracts/Booking.Contracts.csproj
dotnet build Booking/Booking.Application/Booking.Application.csproj
dotnet build Booking/Booking.Infrastructure/Booking.Infrastructure.csproj
dotnet build Booking/Booking.Presentation/Booking.Presentation.csproj
dotnet build tests/Booking.Tests.Unit/Booking.Tests.Unit.csproj
dotnet build tests/Booking.IntegrationTests/Booking.IntegrationTests.csproj
```

**Before PR:** Tech Lead runs full solution build in a sandboxed terminal (after stopping any local `YallaJo.Web` instances).

---

## 5. Migration Sequence

Applied in this exact order in production. **Squash forbidden — keep separate migrations for rollback safety:**

| # | Name | Owner | Task | Notes |
|---|---|---|---|---|
| 1 | `BookingAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 | Add `IsDeleted bit NOT NULL DEFAULT 0`, `DeletedAt datetime2 NULL`, `RowVersion rowversion NOT NULL` to TourBooking, AvailabilitySlot, RefundPolicy, JoinRequest, ProviderDocument |
| 2 | `BookingAddReadSnapshots` | Tech Lead | PW-9 (or T4 step 1 if PW slipped) | Create BookingTourSnapshots, BookingTourPricingSnapshots, BookingProviderSnapshots, BookingCommissionSnapshots tables in `booking` schema |
| 3 | `BookingAddSlotLockFilteredIndex` | Mahmoud | T1 | UNIQUE filtered index `(UserId, AvailabilitySlotId) WHERE IsActive = 1`; also `IX_SlotLocks_ExpiresAt WHERE IsActive = 1` for BG service |
| 4 | `BookingAddRefundPolicyJsonColumn` | Fadwa | T2 | `Tiers nvarchar(max) NOT NULL DEFAULT '[]'` owned-JSON column |
| 5 | `BookingAddProviderDocumentExpiryColumns` | Fadwa | T3 | `ExpiryWarningSent bit NOT NULL DEFAULT 0`, `ExpiryProcessed bit NOT NULL DEFAULT 0`, `ApprovedAt datetime2 NULL`, `ApprovedByUserId Guid NULL`, `RejectedAt datetime2 NULL`, `RejectionReason nvarchar(500) NULL` + 2 indexes |
| 6 | `BookingAddTourBookingReferenceIndex` | Mohammad | T4 | UNIQUE index `IX_TourBookings_Reference (Reference)` |

**Migration generation cmd (run from solution root):**
```powershell
dotnet ef migrations add <Name> `
  --project Booking/Booking.Infrastructure `
  --startup-project YallaJo.Api `
  --context BookingDbContext `
  --output-dir Migrations
```

**Apply cmd:**
```powershell
dotnet ef database update `
  --project Booking/Booking.Infrastructure `
  --startup-project YallaJo.Api `
  --context BookingDbContext
```

**Tech Lead deploys migrations** — devs commit migration code but do NOT run `database update` in shared envs.

---

## 6. Inbox / Outbox Hygiene

**`CompositeOutboxProcessor`** (existing in YallaJo.Api) auto-picks up Booking's outbox table because Booking.Infrastructure registers `BookingDbContext` and the processor scans all registered DbContexts. **Nothing to wire** — confirm by checking that boot log shows `[INFO] CompositeOutboxProcessor monitoring N DbContexts: ..., BookingDbContext`.

**OutboxCleaner**: existing background service deletes processed outbox rows older than **7 days**. No Booking-specific config needed; runs against all DbContexts.

**InboxCleaner**: deletes processed inbox rows older than **30 days**. Same — no Booking-specific config.

**Alerting:** Tech Lead should set up Grafana/Seq alert for:
- `booking.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5min` → > 100 rows = page on-call
- `booking.InboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5min` → > 100 rows = page on-call

**HasBeenProcessedAsync usage rule** (per 02-critical-rules.md B-R7 and INDEX §4 R15): every inbox handler MUST:
1. Call `await inboxStore.HasBeenProcessedAsync(integrationEvent.Id, ct)` — if true, return Result.Success and skip work.
2. Execute the work (DB writes, calls to repos).
3. Call `inboxStore.MarkAsProcessed(integrationEvent.Id)` (in-memory entity tracking).
4. **One** `await unitOfWork.SaveChangesAsync(ct)` at the end commits both the work AND the inbox row atomically.

---

## 7. Admin Manual Trigger Endpoints (BONUS — Mahmoud, +4h if budget allows)

For ops sanity and the acceptance gate live-test step, add 4 admin-only endpoints that immediately trigger a BG service tick (bypasses the cron):

```text
POST /api/v1/admin/booking/bg/slot-lock-cleanup/trigger
POST /api/v1/admin/booking/bg/booking-auto-expire/trigger
POST /api/v1/admin/booking/bg/provider-auto-accept/trigger
POST /api/v1/admin/booking/bg/document-expiry-check/trigger
```

Each requires `MustHavePermission(BookingFeatures.AdminBookingDashboard, AppAction.Refresh)`. Returns 202 + `{ trigggered: true, batchId: <guid>, queuedAt: <utc> }`.

**Implementation**: each service exposes an internal `Task TickOnceAsync(CancellationToken)` method. The endpoint resolves the running BackgroundService from `IServiceProvider.GetServices<IHostedService>()`, casts, calls `TickOnceAsync`. Locking ensures no overlap with the regular tick.

**Out of scope if hours tight** — but strongly recommended; speeds up the acceptance gate by ~30 min.
