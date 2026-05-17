# Pre-Work Execution Plan — All 6 Modules

> **Total Scope:** 46 pre-work items across 6 sprint modules (Booking, Finance, Social, Messaging, Analytics, Auth-Cleanup)
> **Estimated Effort:** ~114-152 hours Tech Lead time
> **Outcome:** All foundational scaffolding ready before any feature task begins
> **Created:** 2026-06-01 (pre-Booking sprint)
> **Owner:** Tech Lead

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Strategy & Approach](#strategy--approach)
3. [Phase 0: Discovery & Verification](#phase-0-discovery--verification)
4. [Phase 1: Cross-Module Foundation](#phase-1-cross-module-foundation)
5. [Phase 2: Per-Module Pre-Work](#phase-2-per-module-pre-work)
6. [Phase 3: Authorization-Cleanup Pre-Work](#phase-3-authorization-cleanup-pre-work)
7. [Phase 4: Integration & Verification](#phase-4-integration--verification)
8. [Phase 5: Documentation & Handover](#phase-5-documentation--handover)
9. [Risk Register](#risk-register)
10. [Acceptance Gates](#acceptance-gates)

---

## Executive Summary

### Pre-Work Inventory

| Sprint | PW Items | Effort | Hard Deadline |
|--------|----------|--------|---------------|
| **Booking** | 8 (PW-1..PW-8) | 12-16h | 2026-06-16 17:00 |
| **Finance** | 9 (PW-1..PW-9) | 16-20h | 2026-08-18 17:00 |
| **Social** | 7 (PW-1..PW-7) | 10-14h | 2026-10-20 17:00 |
| **Messaging** | 10 (PW-1..PW-10) | 18-22h | 2026-12-01 17:00 |
| **Analytics** | 9 (PW-1..PW-9) | 14-18h | 2027-01-19 17:00 |
| **Auth-Cleanup** | 3 (PW-1..PW-3) | 12h | 2027-03-02 17:00 |
| **TOTAL** | **46 items** | **82-102h** | — |

### Key Observation: 80% of Pre-Work is Pattern Replication

Every module's PW-1..PW-5 follows the **same 5-step structural pattern**:
1. UoW delegate fix (1h each)
2. IAggregateRoot markers + AuditableEntity upgrades (1-2h each)
3. Domain event records (1-2h each)
4. Integration event records + registry (1h each)
5. Repository interfaces (2-3h each)

The remaining 20% is module-specific (gateway stubs, profanity filters, hub abstractions).

**This means a templated approach can compress the work significantly** if we build foundation utilities first.

---

## Strategy & Approach

### Three Strategic Choices

#### Option A: Sequential (Slow & Safe)
Do each module's pre-work in chronological sprint order. Booking → Finance → Social → Messaging → Analytics → Auth-Cleanup.

- **Pros:** Matches sprint timeline exactly, low risk of cross-module conflicts
- **Cons:** Cannot start any pre-work until predecessor sprint closes
- **Effort:** ~5 calendar weeks of Tech Lead time
- **Recommended when:** Tech Lead has other concurrent responsibilities

#### Option B: Parallel Big-Bang (Fast but Risky)
Do ALL pre-work for ALL modules in one giant batch BEFORE any feature sprint starts.

- **Pros:** All scaffolding ready ASAP; team can mentally model the whole Phase 2 closure
- **Cons:** Massive PR (~46 PW items in one branch); merge conflicts proliferate; harder to bisect bugs
- **Effort:** ~2 calendar weeks of focused Tech Lead time
- **Recommended when:** Tech Lead has 2 dedicated weeks AND the team commits to reading 46 PW changes in one review

#### Option C: Templated Phased (RECOMMENDED ✅)
Build cross-module foundation FIRST (Phase 1), then run per-module PW in **two parallel tracks** of related modules.

- **Pros:** Best of both — foundation is reusable, parallel tracks save weeks, PRs stay reviewable
- **Cons:** Requires careful sequencing of dependent module pairs
- **Effort:** ~3 calendar weeks of Tech Lead time with 2 streams in parallel

### Recommended Track Structure (Option C)

```
Week 1: Phase 0 + Phase 1 (cross-module foundation)
Week 2:
  Track A: Booking PW + Finance PW (sequential — Finance depends on Booking contracts)
  Track B: Social PW + Messaging PW (sequential — Messaging depends on Social events)
Week 3:
  Track A: Analytics PW + Auth-Cleanup PW (sequential — Auth-Cleanup is cross-cutting)
  Track B: Integration testing + Documentation
```

---

## Phase 0: Discovery & Verification

> **Duration:** 1-2 days (8-12 hours Tech Lead)
> **Goal:** Verify what already exists vs what the PW spec assumes is missing.

### 0.1 Solution-Wide Audit

Run from repo root to establish baseline:

```powershell
$modules = @('Booking','Finance','Social','Messaging','Analytics')
foreach ($m in $modules) {
    Write-Host "=== $m ===" -ForegroundColor Cyan
    Get-ChildItem -Path "src/$m.*/Persistence/*UnitOfWork.cs" -ErrorAction SilentlyContinue |
        ForEach-Object { Write-Host "  UoW exists: $($_.FullName)" }
    Get-ChildItem -Path "src/$m.Domain/Entities/" -ErrorAction SilentlyContinue |
        Measure-Object | ForEach-Object { Write-Host "  Entities: $($_.Count)" }
    Get-ChildItem -Path "src/$m.Domain/Events/" -ErrorAction SilentlyContinue |
        Measure-Object | ForEach-Object { Write-Host "  Domain Events: $($_.Count)" }
    Get-ChildItem -Path "src/$m.Contracts/IntegrationEvents/" -ErrorAction SilentlyContinue |
        Measure-Object | ForEach-Object { Write-Host "  Integration Events: $($_.Count)" }
}
```

### 0.2 Per-Module Verification Checklist

For each of the 6 modules, verify:

| Check | Command/Tool | Expected |
|-------|-------------|----------|
| Module project exists | `Test-Path src/{Module}.Application` | True for all 6 |
| DbContext exists | `Test-Path src/{Module}.Infrastructure/Persistence/{Module}DbContext.cs` | True (may need PW to create) |
| UnitOfWork exists | `Test-Path src/{Module}.Infrastructure/Persistence/{Module}UnitOfWork.cs` | Most exist (PW-1 fixes) |
| Domain entities count | `Get-ChildItem src/{Module}.Domain/Entities -Recurse -Filter "*.cs"` | Varies |
| Integration events registered | `Grep "{module}\." src/Shared/Infrastructure/IntegrationEventTypeRegistry.cs` | Some may exist |
| Permission catalog exists | `Test-Path src/{Module}.Contracts/Authorization/{Module}PermissionCatalog.cs` | True for older modules |
| Test projects exist | `Test-Path tests/{Module}.Tests.Unit` | Likely false for new modules |
| Existing migrations | `Get-ChildItem src/{Module}.Infrastructure/Migrations` | At least 1 (`CreateModel`) |

### 0.3 SharedKernel Audit

Verify these exist in `SharedKernel.*` projects:

```csharp
// SharedKernel.Domain/Abstractions/
- BaseEntity                           // Existing
- AuditableEntity                       // Existing — verify has RowVersion, IsDeleted, DeletedAt
- IAggregateRoot                        // Existing — marker interface
- IDomainEvent                          // Existing
- DomainEventList                       // Existing — internal raise-event tracking

// SharedKernel.Application/Abstractions/
- IUnitOfWork<TContext>                 // Existing — must dispatch events BEFORE SaveChanges
- ICurrentUser                          // Existing
- IPermissionCatalog                    // Existing
- AppAction (enum)                      // Existing — verify needed values
- MustHavePermissionAttribute           // Existing
- Result / Result<T>                    // Existing
- Outcome (enum)                        // Existing
- Error (record)                        // Existing
- IIntegrationEvent                     // Existing
- IntegrationEventTypeRegistry          // Existing

// SharedKernel.Infrastructure/
- UnitOfWork<TContext>                  // Existing impl — verify domain-event dispatch
- IInboxStore                           // Existing template
- HybridCache (Microsoft.Extensions.Caching.Hybrid) — package reference exists
```

### 0.4 Deliverable: `Agents/pre-work-baseline-2026-XX-XX.md`

Tech Lead writes a single status document with:
- For each module: which PW items can be skipped (already done) vs which are real work
- AppAction enum gaps that need adding to SharedKernel
- Any cross-cutting refactors (e.g. if `IUnitOfWork<TContext>` has a bug that affects all modules)
- Risk callouts (e.g. "Booking entity X already exists but uses BaseEntity, requires AuditableEntity migration with data loss risk")

**Acceptance:** Document merged to `main`. Reviewed by all 4 devs in async Slack thread before kickoff.

---

## Phase 1: Cross-Module Foundation

> **Duration:** 2-3 days (12-18 hours Tech Lead)
> **Goal:** Build reusable infrastructure that every module will consume.

### 1.1 SharedKernel: AppAction Enum Extensions

**Current AppAction enum** (verified in Phase 0) likely has: `Create, Read, Update, Delete, Approve, Reject, Suspend`.

**Additions needed** (consolidated from all 6 sprints):

```csharp
public enum AppAction
{
    // Existing
    Create,
    Read,
    Update,
    Delete,
    Approve,
    Reject,
    Suspend,

    // NEW from Booking
    Cancel,           // Booking: POST /{id}/cancel
    Complete,         // Booking: POST /{id}/complete
    Confirm,          // Booking: POST /{id}/confirm

    // NEW from Finance
    Trigger,          // Finance: POST /payouts/admin/trigger
    Download,         // Finance: GET /invoices/{id}/download
    Refund,           // (alternative: reuse Create for Refund.Create)
    Verify,           // Finance: ProviderBankAccount.Verify

    // NEW from Social
    Warn,             // Social: AdminModerationQueue.Warn
    Ban,              // Social: AdminModerationQueue.Ban

    // NEW from Messaging
    Close,            // Messaging: SupportTicket.Close
    Assign,           // Messaging: AdminSupportQueue.Assign
    Resolve,          // Messaging: AdminSupportQueue.Resolve

    // NEW from Analytics
    Refresh,          // Analytics: AdminDashboard.Refresh
    Export,           // Analytics: AuditLog.Export
    Redact            // Analytics: AuditLog.Redact
}
```

**Implementation:**

1. Edit `SharedKernel.Application/Authorization/AppAction.cs`
2. Add migration to `security.PermissionDescriptors` table if AppAction values are persisted (likely they are, as strings)
3. Update `MustHavePermissionAttribute` validator to accept new values
4. Run `dotnet build` — fails compile if any module uses an undefined value

**Acceptance gate:**
- All 17 AppAction values compile
- Unit test `AppActionEnumHasAllRequiredValues` enumerates expected set and asserts present
- Boot log shows updated count: `[INFO] AppAction has 17 values defined`

### 1.2 SharedKernel: Verify `IUnitOfWork<TContext>` Dispatch

The single most critical PW item across all 6 sprints is **PW-1 (UoW delegate fix)**. Verify the SharedKernel implementation is correct ONCE so all 6 modules can delegate without bugs.

**Expected implementation** in `SharedKernel.Infrastructure/Persistence/UnitOfWork.cs`:

```csharp
public sealed class UnitOfWork<TContext>(
    TContext context,
    IPublisher publisher,
    ILogger<UnitOfWork<TContext>> logger)
    : IUnitOfWork<TContext> where TContext : DbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // 1. Collect domain events from all tracked aggregates BEFORE SaveChanges
        var aggregates = context.ChangeTracker.Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        // 2. Clear events on aggregates (idempotent — events fire once)
        aggregates.ForEach(a => a.ClearDomainEvents());

        // 3. Dispatch events via MediatR BEFORE SaveChanges
        // (so handlers can write more entities in same transaction)
        foreach (var ev in events)
        {
            await publisher.Publish(ev, ct);
        }

        // 4. Single SaveChanges commits everything atomically
        return await context.SaveChangesAsync(ct);
    }
}
```

**Critical invariants:**
- Domain events dispatched BEFORE SaveChanges (so handlers can add more entities)
- Events cleared on aggregates (so re-saving doesn't re-fire)
- Only `IAggregateRoot` entities scanned (not BaseEntity)
- Single SaveChanges per UoW.SaveChangesAsync call (no nested SaveChanges)

**Verification:**
- Write a unit test in `tests/SharedKernel.Tests/UnitOfWorkTests.cs`:
  - Test 1: Aggregate with 1 domain event → IPublisher.Publish called 1× before SaveChanges
  - Test 2: BaseEntity with no events → IPublisher.Publish never called
  - Test 3: Handler that adds another aggregate with events → second-level events also dispatched (recursive)
  - Test 4: Multiple aggregates with multiple events → all dispatched in order

If any test fails, this is a SharedKernel bug that blocks ALL 6 modules. Fix here first.

### 1.3 SharedKernel: Verify Common Abstractions

Confirm these exist and work correctly (smoke-test each):

```csharp
// IInboxStore<TContext>
public interface IInboxStore<TContext> where TContext : DbContext
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken ct);
    void MarkAsProcessed(Guid eventId);
}

// IIntegrationEvent
public interface IIntegrationEvent
{
    Guid Id { get; }
}

// IntegrationEventTypeRegistry (Singleton)
public sealed class IntegrationEventTypeRegistry
{
    public Type Get(string logicalName);
    public string GetLogicalName(Type eventType);
    public void Register<TEvent>(string logicalName) where TEvent : IIntegrationEvent;
}
```

### 1.4 Test Infrastructure: `tests/Shared.Tests.Common`

Create a shared test project that all 6 module test projects reference:

```
tests/Shared.Tests.Common/
├── Shared.Tests.Common.csproj
│   - xunit 2.9.3
│   - NSubstitute 5.3.0
│   - FluentAssertions 7.0.0
│   - Microsoft.EntityFrameworkCore.InMemory 9.0.15
│   - Microsoft.AspNetCore.Mvc.Testing 9.0.15
├── Endpoints/
│   └── EndpointInspector.cs        // Reusable across modules for auth tests
├── Fixtures/
│   ├── InMemoryDbContextFixture.cs
│   └── WebApiFactory.cs            // Base class for WAF tests
├── Helpers/
│   ├── DomainEventCollector.cs     // Captures raised events for assertions
│   └── ResultAssertions.cs         // FluentAssertions extensions for Result<T>
└── Builders/
    └── (per-module entity builders added later)
```

**EndpointInspector.cs** (from Auth-Cleanup PW-3 spec, but used by ALL modules):

```csharp
public static class EndpointInspector
{
    public static RouteEndpoint? GetEndpoint(IServiceProvider services, string routeName)
    {
        var endpointDataSource = services.GetRequiredService<EndpointDataSource>();
        return endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName == routeName);
    }

    public static MustHavePermissionAttribute? GetPermissionMetadata(this RouteEndpoint endpoint)
        => endpoint.Metadata.GetMetadata<MustHavePermissionAttribute>();

    public static bool IsAnonymous(this RouteEndpoint endpoint)
        => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
}
```

### 1.5 CI: Drift-Proof Parity Tests

Every module's PW-4 includes a "registry parity test". Build the **base test class** in `Shared.Tests.Common` so each module only needs a 5-line subclass:

```csharp
// Shared.Tests.Common/Outbox/IntegrationEventRegistryParityBase.cs
public abstract class IntegrationEventRegistryParityBase
{
    protected abstract Assembly ContractsAssembly { get; }
    protected abstract string ModuleNamespacePrefix { get; }

    [Fact]
    public void Every_declared_integration_event_is_registered_in_registry()
    {
        var registry = new IntegrationEventTypeRegistry();
        RegisterModuleEvents(registry);  // override in subclass

        var declaredEvents = ContractsAssembly.GetTypes()
            .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var eventType in declaredEvents)
        {
            var logicalName = registry.GetLogicalName(eventType);
            logicalName.Should().StartWith(ModuleNamespacePrefix,
                because: $"{eventType.Name} should follow '{ModuleNamespacePrefix}*.v1' naming");
        }
    }

    [Fact]
    public void Every_registered_logical_name_has_declared_type()
    {
        var registry = new IntegrationEventTypeRegistry();
        RegisterModuleEvents(registry);

        // Use reflection to enumerate registered logical names...
        // Assert each maps to a Type in ContractsAssembly
    }

    protected abstract void RegisterModuleEvents(IntegrationEventTypeRegistry registry);
}
```

**Saves ~20 lines × 6 modules = 120 lines of boilerplate.**

### 1.6 Documentation: `Agents/pre-work-templates.md`

Tech Lead writes a single document with **copy-pasteable code templates** for the patterns that repeat:

1. **UoW delegate template** (1 file per module, identical body)
2. **Domain event record template** (1-line file per event)
3. **Integration event record template** (1-line file per event)
4. **Repository interface template** (per aggregate)
5. **EF repository impl template** (per aggregate, primary-constructor style)
6. **Permission catalog template** (per module)
7. **Test project csproj template** (xunit + dependencies)
8. **InternalsVisibleTo template**

Each task owner copy-pastes from this doc instead of reverse-engineering it.

---

## Phase 2: Per-Module Pre-Work

> **Duration:** 5-7 days across both tracks (40-60 hours Tech Lead total)

### Per-Module Common Sub-Tasks (Apply to ALL 6 modules)

Each module's PW follows this 8-step template (some modules add 1-2 extras):

| Step | Description | Pattern |
|------|-------------|---------|
| 1 | UoW delegate fix | Replace direct `_context.SaveChangesAsync` with `_inner.SaveChangesAsync` |
| 2 | IAggregateRoot markers + AuditableEntity upgrades | Add marker interface + adjust base class on N entities |
| 3 | Domain event records | Create 1-line `record` files in `Domain/Events/` |
| 4 | Integration event records + registry | Create records in `Contracts/IntegrationEvents/` + register in `IntegrationEventTypeRegistry` |
| 5 | Repository interfaces + stubs | Define application-level interfaces + EF skeleton impls |
| 6 | Module-specific abstractions | Gateway/Hub/Filter interfaces (where applicable) |
| 7 | Permission catalog + features | Define `{Module}Features` + `{Module}PermissionCatalog` |
| 8 | Test project scaffolds | Create unit + integration test projects with `InternalsVisibleTo` |

### Track A: Booking → Finance → Analytics

#### Booking PW (Owner: Tech Lead, Hours: 12-16h, Deadline: 2026-06-16)

**PW-1 — BookingUnitOfWork delegates to SharedKernel**

```csharp
// src/Booking.Infrastructure/Persistence/BookingUnitOfWork.cs
internal sealed class BookingUnitOfWork(
    BookingDbContext context,
    IUnitOfWork<BookingDbContext> innerUnitOfWork) : IBookingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => innerUnitOfWork.SaveChangesAsync(ct);
}

// src/Booking.Infrastructure/DependencyInjection.cs
services.AddScoped<IUnitOfWork<BookingDbContext>, UnitOfWork<BookingDbContext>>();
services.AddScoped<IBookingUnitOfWork, BookingUnitOfWork>();
```

**Files touched:**
- `src/Booking.Infrastructure/Persistence/BookingUnitOfWork.cs` (edit)
- `src/Booking.Infrastructure/DependencyInjection.cs` (verify registration)
- `tests/Booking.Tests.Unit/Persistence/BookingUnitOfWorkDispatchesEventsTests.cs` (new — 2 tests)

**PW-2 — IAggregateRoot markers + AuditableEntity upgrades**

Audit current state, then promote 5 entities to `AuditableEntity, IAggregateRoot`:
- TourBooking, AvailabilitySlot, RefundPolicy, JoinRequest, ProviderDocument

Stay BaseEntity (out of scope or junction):
- SlotLock (junction-like, 10-min TTL)
- Reservation, PackageBooking, TourGuide* (deferred)

**Migration:** `BookingAddAggregateRootAndAuditMembers` — adds RowVersion + IsDeleted + DeletedAt columns

**PW-3 — 14 Domain Event Records**

Create 14 1-line records in `src/Booking.Domain/Events/`:
- TourBookingCreated/Confirmed/Cancelled/Completed/Rejected/PaymentExpired
- SlotLockCreated/Released
- AvailabilitySlotCapacityChanged
- JoinRequestCreated/Approved/Rejected
- ProviderDocumentExpiring/Expired

**PW-4 — 12 Integration Event Records + Registry**

Mirror domain events as `IIntegrationEvent` records in `src/Booking.Contracts/IntegrationEvents/`. Register all 12 with `IntegrationEventTypeRegistry`:
- `booking.tour-booking.{created,confirmed,cancelled,completed,rejected,payment-expired}.v1`
- `booking.slot-lock.expired.v1`
- `booking.provider-document.{expiring,expired}.v1`
- `booking.provider.suspended-doc-expired.v1`
- `booking.join-request.{approved,rejected}.v1`

**PW-5 — 8 Repository Interfaces + EF Stubs**

- ITourBookingRepository
- IAvailabilitySlotRepository
- IRefundPolicyRepository
- IJoinRequestRepository
- IProviderDocumentRepository
- ISlotLockRepository (non-aggregate variant)
- IBookingOutboxWriter
- ~~ICommissionRuleRepository~~ — REMOVED (cross-module via ICommissionLookupService)

Each interface extends `IReadRepository<T,Guid> + IWriteRepository<T,Guid>` from SharedKernel. Custom finders per the spec.

**PW-6 — ICommissionLookupService + IDiscountEvaluator Stubs (in Finance.Contracts)**

Cross-module contract. Interface in `Finance.Contracts`, stub impl in `Finance.Infrastructure`:

```csharp
// src/Finance.Contracts/Services/ICommissionLookupService.cs
public interface ICommissionLookupService
{
    Task<CommissionResult> CalculateAsync(Guid providerId, decimal bookingAmountAfterDiscount, string currency, CancellationToken ct);
}

public sealed record CommissionResult(decimal Rate, decimal Amount, string Currency, string TierName);
```

Booking DI registers stub: `services.AddScoped<ICommissionLookupService, FinanceContractsCommissionStub>();`

**PW-7 — BookingFeatures + BookingPermissionCatalog (26 perms)**

8 features × 2-6 actions each = 26 permissions. Register `services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();`

Boot verification: `[INFO] PermissionSeeder inserted/verified 26 Booking permissions`

**PW-8 — Test Project Scaffolds**

- `tests/Booking.Tests.Unit/` (xunit + NSubstitute + FluentAssertions + EF InMemory)
- `tests/Booking.IntegrationTests/` (Mvc.Testing + WAF)
- `<InternalsVisibleTo>` in Application + Infrastructure csprojs

---

#### Finance PW (Owner: Tech Lead, Hours: 16-20h, Deadline: 2026-08-18)

**Identical 8-step template** as Booking PW + 1 extra (PW-9 PCI compliance baseline). Key differences:

- **PW-2 aggregates:** Payment, Invoice, Payout, CommissionRule, ProviderBankAccount (5 total)
- **PW-3 domain events:** 14 events
- **PW-4 integration events:** 10 events (`finance.payment.{completed,failed}.v1`, etc.)
- **PW-5 repositories:** 6 interfaces
- **PW-6 NEW:** `IPaymentGateway` abstraction + `FakePaymentGateway` stub impl
- **PW-7 permissions:** 22 perms across 7 features
- **PW-8 test projects:** Same shape
- **PW-9 NEW:** PCI compliance baseline (PaymentRedactor, HTTPS-only, secrets from env)

**Key PW-6 — IPaymentGateway abstraction:**

```csharp
// SharedKernel.Application/Abstractions/Payments/IPaymentGateway.cs
public interface IPaymentGateway
{
    Task<InitiateResult> InitiateAsync(InitiateRequest request, CancellationToken ct);
    Task<bool> VerifyWebhookSignatureAsync(string rawBody, IDictionary<string, string> headers, CancellationToken ct);
    Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct);
    Task<PayoutResult> PayoutAsync(PayoutRequest request, CancellationToken ct);
    string GatewayName { get; }
}
```

DI: `services.AddSingleton<IPaymentGateway, FakePaymentGateway>();` (dev/staging). Real impls deferred.

**Key PW-9 — PCI compliance baseline:**

Tech Lead audit checklist:
- [x] No raw card numbers stored/logged (regex check in `PaymentRedactor`)
- [x] Webhook signature verified BEFORE body parse
- [x] TLS-only webhook endpoint
- [x] UNIQUE constraint on `Payment.GatewayTransactionId`
- [x] Audit log for every Payment state change
- [x] Secrets from `Environment.GetEnvironmentVariable`, NEVER appsettings.json

---

#### Analytics PW (Owner: Tech Lead, Hours: 14-18h, Deadline: 2027-01-19)

**Same 8-step template + PW-9 schema decisions** specifically for high-write tables:

- **PW-2 aggregates:** Only `PopularityScore` is promoted (1 aggregate). `UserInteraction` and `AuditLog` stay BaseEntity (append-only with BIGINT PK).
- **PW-3 domain events:** 10 events
- **PW-4 integration events:** 3 events
- **PW-5 repositories:** 5 interfaces
- **PW-6 NEW:** `IClientContextProvider` (read UserAgent + IP from HttpContext)
- **PW-7 permissions:** 14 perms
- **PW-8 test projects:** Same shape
- **PW-9 NEW:** Schema decisions (BIGINT IDENTITY PKs, clustered indexes on `(OccurredAt DESC, Id ASC)`, no partitioning v1, SqlBulkCopy for ingest, BigIntCursor)

**Key PW-9 — Schema decisions:**

Document in `src/Analytics.Domain/Entities/UserInteraction.cs` as XML doc comments:

```csharp
/// <summary>
/// User interaction event (append-only stream).
/// Uses BIGINT IDENTITY PK for write-friendly sequential inserts and smaller index pages.
/// Clustered index: (OccurredAt DESC, Id ASC) — time-range queries hit clustered, no bookmark lookup.
/// No partitioning v1 — single table OK up to ~100M rows.
/// Inserted via SqlBulkCopy in IUserInteractionRepository.AddBatchAsync for batches >100.
/// </summary>
public sealed class UserInteraction : BaseEntity
{
    public new long Id { get; private set; }
    // ...
}
```

---

### Track B: Social → Messaging

#### Social PW (Owner: Tech Lead, Hours: 10-14h, Deadline: 2026-10-20)

**Same 8-step template** with smaller scope:

- **PW-2 aggregates:** Review, Favorite, Report (3 aggregates)
- **PW-3 domain events:** 12 events
- **PW-4 integration events:** 5 events
- **PW-5 repositories:** 6 interfaces (includes 2 read-snapshot repos)
- **PW-6 NEW:** `IProfanityFilter` + `INsfwClassifier` + stub impls
- **PW-7 permissions:** 19 perms

**Key PW-6 — Profanity filter:**

```csharp
// SharedKernel.Application/Abstractions/Moderation/IProfanityFilter.cs
public interface IProfanityFilter
{
    Task<bool> ContainsProfanityAsync(string text, string languageCode, CancellationToken ct);
    Task<string> CleanAsync(string text, string languageCode, CancellationToken ct);
}
```

Seed migration: `SocialSeedProfanityBlocklist` with ~50 entries (EN + AR words).

---

#### Messaging PW (Owner: Tech Lead, Hours: 18-22h, Deadline: 2026-12-01)

**Same 8-step template + PW-9 NotificationDeliveryAttempts + PW-10 appsettings:**

- **PW-2 aggregates:** Notification, NotificationPreference, NotificationTemplate, DeviceToken, SupportTicket (5 aggregates)
- **PW-3 domain events:** 14 events
- **PW-4 integration events:** 6 events
- **PW-5 repositories:** 6 interfaces
- **PW-6 NEW:** `INotificationDispatcher`, `INotificationChannelStrategy` (keyed by enum), `INotificationTemplateRenderer`, `IEmailSender` + Mustache renderer via Stubble.Core
- **PW-7 permissions:** 18 perms
- **PW-8 test projects:** Same shape + NotificationHubE2ETests for SignalR
- **PW-9 NEW:** NotificationDeliveryAttempts table (audit trail for EmailSender retries)
- **PW-10 NEW:** appsettings.json template for SMTP, BG service intervals, SignalR config

**Key PW-6 — Notification dispatch abstractions:**

DI uses .NET 8+ keyed services:
```csharp
services.AddKeyedScoped<INotificationChannelStrategy, InAppNotificationStrategy>(NotificationChannel.InApp);
services.AddKeyedScoped<INotificationChannelStrategy, EmailNotificationStrategy>(NotificationChannel.Email);
services.AddKeyedScoped<INotificationChannelStrategy, PushNotificationStrategy>(NotificationChannel.Push);
```

**Key PW-10 — appsettings.json:**

Add `Messaging` section to `src/YallaJo.Api/appsettings.json`. Secrets via env vars NEVER appsettings.

---

## Phase 3: Authorization-Cleanup Pre-Work

> **Duration:** 2 days (12 hours Tech Lead)
> **Hard deadline:** 2027-03-02 17:00

This sprint is **different** — pure cross-cutting cleanup with only 3 PW items.

### PW-1 — Re-verify violation catalog (3h)

Run ast-grep queries to find current violations:
- Bare `.RequireAuthorization()` (AUTH_ONLY)
- String-policy `.RequireAuthorization("permission:...")` (STRING_POLICY)
- `ICurrentUser` injection in command handlers (sample 50 random)

Update `Agents/endpoint-violations-2027-02-28.csv` with fresh counts.

### PW-2 — Verify permission catalog coverage (4h)

For each endpoint in violation CSV, map to `{Module}Features.X + AppAction.Y`. Add missing entries to module catalogs.

Produce `Agents/permission-coverage-gaps-2027-02-28.md` with decisions.

### PW-3 — Sanity test skeleton (5h)

Create `tests/Authorization.IntegrationTests/EndpointAuthorizationMetadataTests.cs` with **3 tests** (RED initially):

1. `Every_registered_endpoint_has_either_MustHavePermission_or_AllowAnonymous`
2. `No_endpoint_uses_string_based_RequireAuthorization_with_permission_prefix`
3. `Every_MustHavePermission_attribute_references_a_registered_permission`

Tests merge to `main` in RED state — they're the to-do list. CI configured to allow specific test failures.

---

## Phase 4: Integration & Verification

> **Duration:** 1-2 days (8-12 hours Tech Lead + 4h dev review)

### 4.1 Solution-Wide Build Verification

```powershell
dotnet clean YallaJo.sln
dotnet build YallaJo.sln --configuration Release
# Expected: 0 errors, 0 warnings
```

### 4.2 All Tests Green

```powershell
dotnet test YallaJo.sln --configuration Release --filter "Category!=authorization-debt"
# Expected: All Pre-Work tests green
# Authorization-debt tests still RED until Auth-Cleanup sprint executes
```

### 4.3 Boot Verification

```powershell
dotnet run --project src/YallaJo.Api
```

Expected boot log:
```
[INFO] PermissionSeeder discovered 11 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social, Messaging, Analytics
[INFO] PermissionSeeder inserted/verified 26 Booking permissions
[INFO] PermissionSeeder inserted/verified 22 Finance permissions
[INFO] PermissionSeeder inserted/verified 19 Social permissions
[INFO] PermissionSeeder inserted/verified 18 Messaging permissions
[INFO] PermissionSeeder inserted/verified 14 Analytics permissions
[INFO] IntegrationEventTypeRegistry: 6 modules × N events registered
```

### 4.4 Cross-Module Event Registry Audit

Write a one-time integration test that validates:
- Every `IIntegrationEvent` type in any `*.Contracts` project is registered
- Every registered logical name matches `{module}.{entity-kebab}.{verb}.v1` pattern
- No two modules use the same logical name (collision check)

### 4.5 Migration Sequence Test

```powershell
dotnet ef database drop --project src/YallaJo.Api --force
foreach ($module in @('Booking','Finance','Social','Messaging','Analytics')) {
    dotnet ef database update --project "src/$module.Infrastructure" --startup-project src/YallaJo.Api --context "${module}DbContext"
    if ($LASTEXITCODE -ne 0) { Write-Error "$module migration failed"; break }
}
```

Expected: all migrations apply in order with zero conflicts.

---

## Phase 5: Documentation & Handover

> **Duration:** 1 day (4-8 hours)

### 5.1 Update Master Index

Edit `Agents/tasks/Phase1-Phase2-Completion-INDEX.md`:

| Module | PW Status |
|--------|-----------|
| Booking | ✅ PW Complete (2026-06-XX) |
| Finance | ✅ PW Complete (2026-08-XX) |
| Social | ✅ PW Complete (2026-10-XX) |
| Messaging | ✅ PW Complete (2026-11-XX) |
| Analytics | ✅ PW Complete (2027-01-XX) |
| Auth-Cleanup | ✅ PW Complete (2027-02-XX) |

### 5.2 Decision Logs (ADRs)

Write 3 new ADRs in `Agents/decisions/`:

1. **ADR-006: Cross-module commission lookup via Finance.Contracts interface**
2. **ADR-007: BigInt PKs for high-write analytics tables**
3. **ADR-008: Single SharedKernel UnitOfWork dispatches all module events**

### 5.3 Onboarding Notes

Update `Agents/agent-context.md` §11.1 with new module statuses.
Update `Agents/guide.md` if any patterns changed.

### 5.4 Sprint Kickoff Briefings

For each upcoming sprint, write a 1-page briefing for the assigned dev (what's done, what to do, key gotchas, files to touch).

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| **SharedKernel UoW has hidden bug** | Low | Critical (blocks all 6 modules) | Phase 1.2 verification with 4 explicit tests before any module PW starts |
| **AppAction enum migration breaks existing permissions** | Medium | High (security regression) | Phase 1.1 adds enum values without renaming existing — backward compatible |
| **Migration conflicts when 6 modules ship migrations** | High | Medium (rollback needed) | Strict migration sequence enforced in Phase 4.5. CI runs full drop+update against fresh DB |
| **Permission catalog double-registration on boot** | Low | Medium (boot crash) | PermissionSeeder is idempotent (INSERT...IF NOT EXISTS pattern). Verified in Phase 4.3 |
| **Integration event registry drift** | Medium | High (silent event loss) | Parity test in Phase 4.4. Drift-proof reverse-parity |
| **Tech Lead capacity slippage** | Medium | Critical (delays all 6 sprints) | Option C parallel tracks reduce dependency. Track A can ship before Track B starts. Document templates in Phase 1.6 lower effort per module |
| **Module entity already uses BaseEntity but feature task expects AuditableEntity** | High for some modules | Medium (data migration risk) | Phase 0 audit catches this. PW-2 migration adds RowVersion+IsDeleted+DeletedAt to existing tables (additive, no data loss) |
| **Test project setup fails due to InternalsVisibleTo** | Low | Low (annoying) | Phase 1.4 standardizes via Shared.Tests.Common references |
| **AppAction enum value collision** | Low | Low | Enum values are universal; semantic differences captured per-module in permission catalog text |

---

## Acceptance Gates

### Phase 0 Acceptance
- [x] `Agents/pre-work-baseline-2026-XX-XX.md` written and merged to `main`
- [x] All 6 modules' current state documented
- [x] All gaps identified and prioritized
- [x] Reviewed in async Slack thread by all 4 devs

### Phase 1 Acceptance
- [x] SharedKernel `UnitOfWork<TContext>` verified with 4 unit tests (green)
- [x] AppAction enum extended with 17 values (build green)
- [x] `Shared.Tests.Common` test project compiles
- [x] `IntegrationEventRegistryParityBase` base class compiles
- [x] `Agents/pre-work-templates.md` written

### Phase 2 Per-Module Acceptance (×6)
For each module's PW branch:
- [x] All PW items complete (8-10 per module)
- [x] `dotnet build YallaJo.sln` green
- [x] Module-specific tests green
- [x] Migration applied to dev DB without conflicts
- [x] Boot log shows correct permission count
- [x] Parity test green
- [x] PR merged to `main` before module's sprint kickoff

### Phase 3 Acceptance
- [x] `Agents/endpoint-violations-2027-02-28.csv` updated with fresh counts
- [x] `Agents/permission-coverage-gaps-2027-02-28.md` written
- [x] 3 sanity tests created in `tests/Authorization.IntegrationTests/` (RED initially — intentional)
- [x] CI allow-list configured for authorization-debt category

### Phase 4 Acceptance
- [x] `dotnet build YallaJo.sln --configuration Release` green (0 errors, 0 warnings)
- [x] `dotnet test YallaJo.sln` green (except authorization-debt category)
- [x] Boot log shows 11 permission catalogs registered
- [x] All migrations apply to fresh DB successfully
- [x] Cross-module event registry audit passes

### Phase 5 Acceptance
- [x] `Agents/tasks/Phase1-Phase2-Completion-INDEX.md` updated
- [x] 3 new ADRs written
- [x] `Agents/agent-context.md` §11.1 updated
- [x] 6 sprint kickoff briefings written

---

## Effort Summary

| Phase | Description | Duration | Tech Lead Hours |
|-------|-------------|----------|-----------------|
| 0 | Discovery & Verification | 1-2 days | 8-12h |
| 1 | Cross-Module Foundation | 2-3 days | 12-18h |
| 2 | Per-Module PW (Booking + Finance + Analytics + Social + Messaging) | 5-7 days | 70-90h |
| 3 | Authorization-Cleanup PW | 2 days | 12h |
| 4 | Integration & Verification | 1-2 days | 8-12h |
| 5 | Documentation & Handover | 1 day | 4-8h |
| **Total** | **All Pre-Work** | **12-17 working days** | **114-152 hours** |

If running **Option C parallel tracks** with capable Tech Lead, compress to **3 calendar weeks** elapsed time.

---

## Quick-Reference Decision Tree

```
START
│
├─ Have you done Phase 0 (discovery)?
│  ├─ NO → Run Phase 0 audits, produce baseline doc
│  └─ YES → Continue
│
├─ Is SharedKernel UoW verified?
│  ├─ NO → Phase 1.2 verification (4 tests)
│  └─ YES → Continue
│
├─ Is AppAction extended?
│  ├─ NO → Phase 1.1 (add 17 values)
│  └─ YES → Continue
│
├─ Is Shared.Tests.Common ready?
│  ├─ NO → Phase 1.4
│  └─ YES → Continue
│
├─ Choose execution strategy
│  ├─ Sequential → Booking → Finance → Social → Messaging → Analytics → Auth-Cleanup
│  ├─ Big Bang → All 6 in parallel (NOT recommended)
│  └─ Two Tracks → Track A: Booking + Finance + Analytics, Track B: Social + Messaging
│                  (followed by Auth-Cleanup last)
│
├─ For each module PW branch:
│  ├─ PW-1 UoW delegate fix (1h)
│  ├─ PW-2 IAggregateRoot markers + migration (1-2h)
│  ├─ PW-3 Domain events (1-2h)
│  ├─ PW-4 Integration events + registry (1h)
│  ├─ PW-5 Repository interfaces + stubs (2-3h)
│  ├─ PW-6 Module-specific abstractions (variable)
│  ├─ PW-7 Permission catalog (1h)
│  └─ PW-8 Test projects (1h)
│
├─ Phase 4 verification
│  ├─ Build green
│  ├─ Tests green
│  ├─ Boot log correct
│  └─ Migration sequence OK
│
└─ Phase 5 documentation
   └─ DONE — sprint kickoffs can proceed on schedule
```

---

## Execution Instructions for Sisyphus

When the user is ready to execute this plan, the Sisyphus agent should:

### Pre-execution checks
1. Read this plan file in full
2. Read `Agents/agent-context.md` §0-11 (33 gotchas)
3. Read `Agents/guide.md` (entity anatomy, CQRS templates)
4. Read all 6 team task files in `Agents/tasks/{Module}-team-tasks.md`
5. Verify Tech Lead branch capacity (do 1 PR per phase, not 1 giant PR)

### Execution order (recommended Option C)
1. **Phase 0:** Run audits, produce baseline doc. Stop here for user review.
2. **Phase 1:** Build foundation. Stop here for user review.
3. **Phase 2A:** Booking PW → Finance PW → Analytics PW (sequential, one branch each)
4. **Phase 2B:** (Parallel with 2A) Social PW → Messaging PW
5. **Phase 3:** Auth-Cleanup PW (after all 2A+2B complete)
6. **Phase 4:** Integration verification
7. **Phase 5:** Documentation

### Per-PW workflow
For each PW item:
1. Create feature branch from `main` (e.g. `sprint/booking-prework`)
2. Implement PW-1 → PW-N in order
3. Run module-specific tests + full solution build
4. Open PR with description listing each PW item + acceptance gate verification
5. Tech Lead reviews + merges
6. Move to next PW or next module

### When stuck
- If SharedKernel UoW has unexpected behavior: pause Phase 1, investigate
- If a module's existing entities clash with PW-2 promotion: pause that module, escalate to Tech Lead for migration strategy
- If integration event registry drift: re-run Phase 1.5 parity test before continuing

---

**Document Version:** 1.0 (created during Phase 2 Closure planning)
**Last Updated:** 2026-06-01 (pre-Booking sprint)
**Owner:** Tech Lead
**Review cadence:** Update after each phase completes
