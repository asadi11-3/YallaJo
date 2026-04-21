# ContentPlaces Module — Fixes Required
> **Scope**: Tasks 1–8 (all four team members) | **Build**: ✅ 0 errors | **Date**: 2026-04-17

---

## How Events Work in This Codebase (Read This First)

Before fixing anything event-related, every developer MUST understand this. Getting it wrong wastes hours.

### The Two Types of Events

```
Domain Event                         Integration Event
────────────────────────────────     ──────────────────────────────────────────
Lives in:  ContentPlaces.Domain      Lives in:  ContentPlaces.Contracts
Base:      DomainEventBase           Base:      IntegrationEventBase
Scope:     Inside one module         Scope:     Cross-module (other services listen)
Transport: In-memory (MediatR)       Transport: Database outbox → background job → MediatR
Raised by: Aggregate root entity     Written by: Domain event handler (to OutboxMessages table)
When:      Inside entity method      When:      Inside domain event handler, BEFORE SaveChanges
```

### The Full Flow — Step by Step

```
1. Command Handler calls entity method
   └─ e.g. place.Create(...)

2. Entity adds domain event to its internal list
   └─ AddDomainEvent(new PlaceCreatedDomainEvent(...))
   └─ [RULE: Only IAggregateRoot entities can do this — Place, Business]
   └─ [NON-aggregates CANNOT: ServiceItem, BusinessHours, BusinessAmenity, BusinessStaff, AccessibilityFeature]

3. Command Handler calls unitOfWork.SaveChangesAsync()
   └─ [CRITICAL] Must use IUnitOfWork<ContentPlacesDbContext> — NOT IContentPlacesUnitOfWork
   └─ IUnitOfWork<TContext>.SaveChangesAsync() does this:
       a. Collects all domain events from tracked aggregates
       b. Clears domain events from entities
       c. Dispatches domain events to MediatR BEFORE SaveChanges  ← handlers run here
       d. Calls context.SaveChangesAsync()  ← all DB changes (entity + outbox rows) saved atomically

4. Domain Event Handler runs (dispatched in step 3c)
   └─ Lives in: ContentPlaces.Infrastructure/EventHandlers/
   └─ Can do: trigger translation, write OutboxMessage rows, update related entities
   └─ [RULE] NEVER call SaveChangesAsync() inside a domain event handler — step 3d does it

5. OutboxMessage row is written to ContentPlacesDbContext.OutboxMessages
   └─ OutboxMessage.Create(new PlaceCreatedIntegrationEvent(...))
   └─ context.OutboxMessages.Add(outboxMessage)  ← just adds to change tracker, NOT saved yet
   └─ Gets saved atomically in step 3d

6. CompositeOutboxProcessor (background job) runs every N seconds
   └─ Reads unprocessed OutboxMessage rows
   └─ Deserializes each to IIntegrationEvent
   └─ Publishes IntegrationEventNotification<TEvent> via MediatR
   └─ Modules that consume the event implement INotificationHandler<IntegrationEventNotification<TEvent>>

7. Consuming module's integration event handler runs
   └─ Checks IInboxStore.HasBeenProcessedAsync() for idempotency
   └─ Does its work (notify user, update SEO, etc.)
   └─ Calls MarkAsProcessed() + SaveChangesAsync()
```

### The Bug That Breaks ALL Domain Events in ContentPlaces Right Now

**`IContentPlacesUnitOfWork` does NOT dispatch domain events.** It calls `context.SaveChangesAsync()` directly.
**`IUnitOfWork<ContentPlacesDbContext>` DOES dispatch them.** It is registered in DI.

Every ContentPlaces command handler currently injects `IContentPlacesUnitOfWork`. This means:
- Domain events raised by `Place.Create()`, `Business.Approve()`, etc. are **silently discarded**
- `PlaceCreatedDomainEventHandler` is **never invoked** — no translations, no outbox writes
- No integration events are ever published — ContentSeo, Analytics, Messaging receive nothing

**Fix**: Command handlers that mutate aggregates (Place, Business) should use `IUnitOfWork<ContentPlacesDbContext>` (the SharedKernel one). `IContentPlacesUnitOfWork` is correct ONLY for non-aggregate saves (BusinessAmenity, BusinessHours via repository methods) where no domain events exist.

> **Important catch**: `UnitOfWork<TContext>` does NOT wrap `DbUpdateConcurrencyException` into `ContentPlacesConcurrencyException`. After switching, update every `catch (ContentPlaceConcurrencyException)` or `catch (ContentPlacesConcurrencyException)` block to catch `DbUpdateConcurrencyException` directly and add `using Microsoft.EntityFrameworkCore;`.

### Non-Aggregates Cannot Use Domain Events

`ServiceItem`, `BusinessHours`, `BusinessAmenity`, `BusinessStaff`, `AccessibilityFeature` are NOT aggregate roots.
They extend `AuditableEntity` or `BaseEntity`, NOT `IAggregateRoot`. The UoW only collects events from `IAggregateRoot`.

For these entities, to publish integration events: **write to the outbox directly from the command handler** (NOT via `IPublisher.Publish()` which is in-process only and not durable):

```csharp
// In AddBusinessStaffCommandHandler, BEFORE save:
var integrationEvent = new BusinessStaffAddedIntegrationEvent(
    staff.Id, staff.BusinessId, staff.UserId, staff.Role.ToString());
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
await unitOfWork.SaveChangesAsync(ct);  // saves staff + outbox row atomically
```

---

## Domain Event Handler Placement Rule

**Current state**: `PlaceCreatedDomainEventHandler` and `PlaceUpdatedDomainEventHandler` are in
`ContentPlaces.Infrastructure/EventHandlers/`.

**Rule** from `agent-context.md`:
> Domain event handlers that perform translation go in `ContentPlaces.Application/EventHandlers/`
> Integration event handlers (consuming events from other modules) go in `ContentPlaces.Infrastructure/EventHandlers/`

However, because domain event handlers in this codebase need to write `OutboxMessage` rows (which requires
`ContentPlacesDbContext` — an Infrastructure type), they must live in Infrastructure. This is the accepted
tradeoff in this project. The existing placement in `Infrastructure/EventHandlers/` is **correct**.

The missing part is that domain event handlers are not also writing outbox rows after doing translation work.

---

## Mahmoud — Task 1 (Place CQRS) Fixes

### 🔴 Fix 1: `DeletePlaceCommandHandler` — Always blocks every delete (logic bug)

**File**: `ContentPlaces.Application/Commands/Place/DeletePlace/DeletePlaceCommandHandler.cs`

**Bug** (line 27):
```csharp
// THIS IS WRONG — checks if ANY place has this ID, which is always true for a valid place
if (await placeRepository.AnyAsync(pb => pb.Id == request.PlaceId, cancellationToken))
{
    return Result.Failure(new Error("Place.HasActiveBusinesses", "Cannot delete..."), Outcome.Invalid);
}
```

**Why it's wrong**: `AnyAsync(pb => pb.Id == request.PlaceId)` queries the `Places` table. Since you just loaded the place a few lines above (it exists), this always returns `true`. Every delete attempt returns "Has Active Businesses" — even for a place with no businesses at all.

**Fix required**: Add `HasActiveLinkedBusinessesAsync(Guid placeId)` to `IPlaceRepository`. Implement it in
`PlaceRepository` by checking the `PlaceBusiness` junction table:

```csharp
// In IPlaceRepository:
Task<bool> HasActiveLinkedBusinessesAsync(Guid placeId, CancellationToken ct = default);

// In PlaceRepository:
public Task<bool> HasActiveLinkedBusinessesAsync(Guid placeId, CancellationToken ct = default)
    => context.PlaceBusinesses.AnyAsync(pb => pb.PlaceId == placeId, ct);

// In DeletePlaceCommandHandler — replace the broken check:
if (await placeRepository.HasActiveLinkedBusinessesAsync(request.PlaceId, cancellationToken))
{
    return Result.Failure(
        new Error("Place.HasActiveBusinesses", "Cannot delete a place that has active businesses."),
        Outcome.Invalid);
}
```

Also: after a successful soft-delete, publish `PlaceDeletedIntegrationEvent` to the outbox directly in this handler (no domain event is raised for delete). Inject `ContentPlacesDbContext` for this:

```csharp
// After unitOfWork.SaveChangesAsync() succeeds:
var deletedEvent = new PlaceDeletedIntegrationEvent(request.PlaceId);
dbContext.OutboxMessages.Add(OutboxMessage.Create(deletedEvent));
// Note: this requires a second save or restructuring so the outbox row is included before the single save.
// Best approach: add outbox row BEFORE calling SaveChangesAsync so both are committed atomically.
```

---

### 🔴 Fix 2: `CreatePlaceCommandHandler` — Slug uniqueness checks wrong variable

**File**: `ContentPlaces.Application/Commands/Place/CreatePlace/CreatePlaceCommandHandler.cs`

**Bug** (line 28):
```csharp
var slug = string.IsNullOrWhiteSpace(request.Slug)
    ? PlaceEntity.GenerateSlug(request.Name)
    : request.Slug.Trim().ToLowerInvariant();

// BUG: checks request.Slug (raw, possibly null) instead of slug (normalized)
if (await placeRepository.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
```

When `request.Slug` is `null` (auto-generate mode), the condition `t.Slug == null` is always false in SQL —
every insert passes the check regardless of duplicates. A generated slug can collide silently.

**Fix**:
```csharp
if (await placeRepository.AnyAsync(t => t.Slug == slug, cancellationToken))  // use `slug`, not `request.Slug`
```

---

### 🔴 Fix 3: Domain events never dispatched — wrong UoW in all Place command handlers

**Files**: All 5 Place command handlers (Create, Update, Delete, Feature, Verify)

**Problem**: Every handler injects `IContentPlacesUnitOfWork`. This wraps raw `context.SaveChangesAsync()` — it
does NOT dispatch domain events. So `Place.Create()` adds `PlaceCreatedDomainEvent` to the entity's list, but
when `unitOfWork.SaveChangesAsync()` is called, that event is silently discarded.

**Fix for aggregate-mutating commands (Create, Update)**:
```csharp
// Add usings:
using YallaJo.SharedKernel.Infrastructure.Data;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Change constructor injection from:
IContentPlacesUnitOfWork unitOfWork,
// To:
IUnitOfWork<ContentPlacesDbContext> unitOfWork,

// Update catch blocks from:
catch (ContentPlaceConcurrencyException)
// To:
catch (DbUpdateConcurrencyException)
```

> For Feature/Verify commands (which also mutate aggregates but raise no domain events), `IContentPlacesUnitOfWork` is technically fine. But switch them too for consistency and future-proofing.

---

### 🔴 Fix 4: `PlaceCreatedDomainEventHandler` and `PlaceUpdatedDomainEventHandler` — missing outbox write

**Files**:
- `ContentPlaces.Infrastructure/EventHandlers/PlaceCreatedDomainEventHandler.cs`
- `ContentPlaces.Infrastructure/EventHandlers/PlaceUpdatedDomainEventHandler.cs`

Both handlers run translation logic but never publish integration events. ContentSeo, Analytics, and other
consumers will never receive `PlaceCreatedIntegrationEvent`.

**Fix**: Inject `ContentPlacesDbContext` and write to outbox at the end of Handle():

```csharp
// Add to PlaceCreatedDomainEventHandler constructor:
public sealed class PlaceCreatedDomainEventHandler(
    IPlaceRepository placeRepository,
    IEntityTranslationOrchestrator orchestrator,
    ContentPlacesDbContext dbContext,           // ← ADD THIS
    ILogger<PlaceCreatedDomainEventHandler> logger)

// At the end of Handle(), after translation work:
var integrationEvent = new PlaceCreatedIntegrationEvent(evt.PlaceId, evt.Name, evt.Slug);
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
// Do NOT call SaveChangesAsync() — the UoW does it atomically after all handlers complete
```

Same pattern for `PlaceUpdatedDomainEventHandler` → publish `PlaceUpdatedIntegrationEvent`.

---

### 🟡 Fix 5: All 3 Place query records — missing `ICacheableQuery`

**Files**:
- `ContentPlaces.Application/Queries/Place/ListPlaces/ListPlacesQuery.cs`
- `ContentPlaces.Application/Queries/Place/GetPlaceById/GetPlaceByIdQuery.cs`
- `ContentPlaces.Application/Queries/Place/GetPlaceBySlug/GetPlaceBySlugQuery.cs`

None implement `ICacheableQuery`. Add cache keys to `ContentPlacesCacheKeys.cs` and implement on each query:

```csharp
// ContentPlacesCacheKeys.cs — add:
public static string PlaceList(int page, int pageSize, string? city, string? country, decimal? rMin, decimal? rMax)
    => $"cp:places:p{page}:s{pageSize}:city:{city}:ctry:{country}:rMin:{rMin}:rMax:{rMax}";
public static string Place(Guid id) => $"cp:place:{id}";
public static string PlaceBySlug(string slug) => $"cp:place:slug:{slug}";

// GetPlaceByIdQuery.cs:
public sealed record GetPlaceByIdQuery(Guid PlaceId)
    : IQuery<PlaceDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.Place(PlaceId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["places", $"place:{PlaceId}"];
}

// GetPlaceBySlugQuery.cs:
public sealed record GetPlaceBySlugQuery(string Slug)
    : IQuery<PlaceDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.PlaceBySlug(Slug);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["places"];
}

// ListPlacesQuery.cs:
public sealed record ListPlacesQuery(...) : IQuery<PaginatedResult<PlaceSummaryDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.PlaceList(Page, PageSize, City, Country, RatingMin, RatingMax);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["places"];
}
```

---

### 🟡 Fix 6: All 5 Place command handlers — missing `HybridCache` invalidation

Inject `HybridCache` and call `RemoveByTagAsync` after successful saves in Create, Update, Delete, Feature, Verify:

```csharp
// After save in CreatePlaceCommandHandler:
await cache.RemoveByTagAsync("places", cancellationToken);

// After save in UpdatePlaceCommandHandler:
await cache.RemoveByTagAsync($"place:{place.Id}", cancellationToken);
await cache.RemoveByTagAsync("places", cancellationToken);

// After save in DeletePlaceCommandHandler:
await cache.RemoveByTagAsync($"place:{request.PlaceId}", cancellationToken);
await cache.RemoveByTagAsync("places", cancellationToken);

// Feature and Verify same as Update
await cache.RemoveByTagAsync($"place:{request.PlaceId}", cancellationToken);
await cache.RemoveByTagAsync("places", cancellationToken);
```

---

### 🟡 Fix 7: `ListPlacesQuery` — `CategoryId` and `HasActiveTours` filters silently ignored

`PlaceFilterSpecification` does not accept or apply `CategoryId` or `HasActiveTours`. The spec requires both.
Add them to the specification constructor and filter expressions:

```csharp
// PlaceFilterSpecification constructor — add parameters:
public PlaceFilterSpecification(
    int page, int pageSize,
    Guid? categoryId = null,
    decimal? ratingMin = null, decimal? ratingMax = null,
    string? city = null, string? country = null,
    bool? hasActiveTours = null)
{
    WhereIf(categoryId.HasValue, p => p.CategoryId == categoryId);
    WhereIf(hasActiveTours == true, p => p.TourCount > 0);
    // ... existing filters
}

// ListPlacesQueryHandler — pass the new params:
var spec = new PlaceFilterSpecification(
    page: request.Page, pageSize: pageSize,
    categoryId: request.CategoryId,
    ratingMin: request.RatingMin, ratingMax: request.RatingMax,
    city: request.City, country: request.Country);
```

---

### 🟠 Fix 8: `UpdatePlaceResult.cs` — old-style brace namespace

**File**: `ContentPlaces.Application/Commands/Place/UpdatePlace/UpdatePlaceResult.cs`

Uses old `namespace ... { }` brace style with 5 unnecessary `using` statements.

**Fix**:
```csharp
namespace ContentPlaces.Application.Commands.Place.UpdatePlace;

public sealed record UpdatePlaceResult(Guid PlaceId, string Name, string Slug);
```

---

## Mohammad — Tasks 2+3 (Business + BusinessHours) Fixes

### 🔴 Fix 1: All Business command handlers — wrong UoW (domain events never dispatched)

**Files**: CreateBusiness, UpdateBusiness, ApproveBusiness, RejectBusiness, ResubmitBusiness,
SuspendBusiness, ReinstateBusiness, DeleteBusiness handlers

All inject `IContentPlacesUnitOfWork`. Business is an `IAggregateRoot` that raises domain events
(`BusinessCreatedDomainEvent`, `BusinessApprovedDomainEvent`, etc.) — these are silently discarded.

**Fix**: Switch all aggregate-mutating Business command handlers to `IUnitOfWork<ContentPlacesDbContext>`:

```csharp
// Add usings:
using YallaJo.SharedKernel.Infrastructure.Data;
using ContentPlaces.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Change injection from:
IContentPlacesUnitOfWork unitOfWork,
// To:
IUnitOfWork<ContentPlacesDbContext> unitOfWork,

// Each handler has a private SaveAsync() method — update its catch block from:
catch (ContentPlacesConcurrencyException ex)
// To:
catch (DbUpdateConcurrencyException ex)
```

---

### 🔴 Fix 2: Business domain event handlers are missing entirely

The task spec requires these handlers in `ContentPlaces.Infrastructure/EventHandlers/`.
Currently only Place handlers exist there. Create all 5:

**Template** (copy pattern from `PlaceCreatedDomainEventHandler`):

```csharp
// ContentPlaces.Infrastructure/EventHandlers/BusinessApprovedDomainEventHandler.cs
using ContentPlaces.Contracts.IntegrationEvents;
using ContentPlaces.Domain.Events.BusinessEvents;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.EventHandlers;

public sealed class BusinessApprovedDomainEventHandler(
    IBusinessRepository businessRepository,
    ContentPlacesDbContext dbContext,
    ILogger<BusinessApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessApprovedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<BusinessApprovedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Load business to get OwnerId (domain event carries ReviewedByUserId, not OwnerId)
        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning("BusinessApprovedDomainEvent: Business {Id} not found.", evt.BusinessId);
            return;
        }

        var integrationEvent = new BusinessApprovedIntegrationEvent(evt.BusinessId, business.OwnerId);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        // Do NOT call SaveChangesAsync() here
        logger.LogInformation("BusinessApprovedDomainEvent: queued outbox for Business {Id}", evt.BusinessId);
    }
}
```

Create the same pattern for all 5 handlers:

| Handler file | Domain event | Integration event published |
|---|---|---|
| `BusinessCreatedDomainEventHandler.cs` | `BusinessCreatedDomainEvent` | `BusinessCreatedIntegrationEvent` + trigger translation |
| `BusinessApprovedDomainEventHandler.cs` | `BusinessApprovedDomainEvent` | `BusinessApprovedIntegrationEvent` |
| `BusinessRejectedDomainEventHandler.cs` | `BusinessRejectedDomainEvent` | `BusinessRejectedIntegrationEvent` |
| `BusinessSuspendedDomainEventHandler.cs` | `BusinessSuspendedDomainEvent` | `BusinessSuspendedIntegrationEvent` |
| `BusinessReinstatedDomainEventHandler.cs` | `BusinessReinstatedDomainEvent` | `BusinessReinstatedIntegrationEvent` |

> `BusinessCreatedDomainEventHandler` must also load the business and call `IEntityTranslationOrchestrator`
> (same pattern as `PlaceCreatedDomainEventHandler`) before writing the outbox row.

> For `BusinessApprovedIntegrationEvent`, `BusinessRejectedIntegrationEvent`, `BusinessSuspendedIntegrationEvent`,
> and `BusinessReinstatedIntegrationEvent`: the Messaging module needs `OwnerId` to notify the owner.
> The domain events carry `ReviewedByUserId` or just `BusinessId`. Load the business in the handler to get `OwnerId`.

---

### 🔴 Fix 3: Business integration event records missing from `ContentPlaces.Contracts`

The integration event `.cs` files that other modules subscribe to DO NOT EXIST in the repo yet.
The Contracts project only contains Place events and ServiceItem events.

Create these 5 files in `ContentPlaces.Contracts/IntegrationEvents/`:

```csharp
// BusinessCreatedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessCreatedIntegrationEvent(
    Guid BusinessId, string Name, string Slug, Guid OwnerId, Guid? PlaceId) : IntegrationEventBase;

// BusinessApprovedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessApprovedIntegrationEvent(
    Guid BusinessId, Guid OwnerId) : IntegrationEventBase;

// BusinessRejectedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessRejectedIntegrationEvent(
    Guid BusinessId, Guid OwnerId, string Reason) : IntegrationEventBase;

// BusinessSuspendedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessSuspendedIntegrationEvent(
    Guid BusinessId, Guid OwnerId, string Reason) : IntegrationEventBase;

// BusinessReinstatedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessReinstatedIntegrationEvent(
    Guid BusinessId, Guid OwnerId) : IntegrationEventBase;
```

---

### 🟡 Fix 4: `DependencyInjection.cs` — duplicate `IBusinessRepository` registration

**File**: `ContentPlaces.Infrastructure/DependencyInjection.cs`

```csharp
services.AddScoped<IBusinessRepository, BusinessRepository>();  // line 35
// ...
services.AddScoped<IBusinessRepository, BusinessRepository>();  // line 48 — DUPLICATE, delete this line
```

**Fix**: Delete line 48. Only one registration is needed.

---

## Fadwa — Tasks 5+6+8 (Amenity, Staff, Accessibility) Fixes

### 🔴 Fix 1: `RemoveBusinessAmenityCommandHandler` — IDOR (anyone can delete anyone's amenity)

**File**: `ContentPlaces.Application/Commands/BusinessAmenity/RemoveBusinessAmenity/RemoveBusinessAmenityCommandHandler.cs`

**Current** — checks authentication but NOT ownership:
```csharp
var amenity = await amenityRepository.GetByIdAsync(request.AmenityId, cancellationToken, asNoTracking: false);
// No ownership check at all — any authenticated user can delete any amenity
amenityRepository.Remove(amenity);
```

**Fix**: Load the amenity's business and verify the caller is the owner or admin:
```csharp
// Load amenity with its Business navigation:
var amenity = await amenityRepository.GetAsync(
    filter: a => a.Id == request.AmenityId,
    include: q => q.Include(a => a.Business),
    asNoTracking: false,
    ct: cancellationToken);

if (amenity is null)
    return Result.Failure(new Error("BusinessAmenity.NotFound", "Amenity not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && amenity.Business.OwnerId != currentUser.UserId!.Value)
    return Result.Failure(Error.Forbidden("You do not own this business."), Outcome.Forbidden);

amenityRepository.Remove(amenity);
```

---

### 🔴 Fix 2: `RemoveBusinessStaffCommandHandler` — No auth check at all

**File**: `ContentPlaces.Application/Commands/BusinessStaff/RemoveBusinessStaff/RemoveBusinessStaffCommandHandler.cs`

Currently has NO `ICurrentUser` injection. Any request that reaches this handler can deactivate any staff member.

**Fix**: Inject `ICurrentUser` and add ownership check:
```csharp
public sealed class RemoveBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,              // ← ADD
    ILogger<RemoveBusinessStaffCommandHandler> logger)

// In Handle() — add at the top:
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure(Error.Unauthorized("Authentication required"), Outcome.Unauthorized);

// Load staff with its Business navigation:
var staff = await staffRepository.GetAsync(
    filter: s => s.Id == request.Id,
    include: q => q.Include(s => s.Business),
    asNoTracking: false,
    ct: cancellationToken);

if (staff is null)
    return Result.Failure(new Error("BusinessStaff.NotFound", "Staff not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && staff.Business.OwnerId != currentUser.UserId.Value)
    return Result.Failure(Error.Forbidden("You do not own this business."), Outcome.Forbidden);

// After ownership check — publish integration event to outbox BEFORE save:
staff.Deactivate();
var integrationEvent = new BusinessStaffRemovedIntegrationEvent(staff.Id, staff.BusinessId, staff.UserId);
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
await unitOfWork.SaveChangesAsync(cancellationToken);
```

---

### 🔴 Fix 3: `AddBusinessStaffCommandHandler` — Missing business existence check + IDOR

**File**: `ContentPlaces.Application/Commands/BusinessStaff/AddBusinessStaff/AddBusinessStaffCommandHandler.cs`

The handler checks for duplicate staff but never verifies:
- Does the business exist?
- Is the caller the business owner or admin?

**Fix**: Add business load and IDOR check before the duplicate check:
```csharp
// Inject IBusinessRepository:
public sealed class AddBusinessStaffCommandHandler(
    IBusinessStaffRepository staffRepository,
    IBusinessRepository businessRepository,    // ← ADD
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<AddBusinessStaffCommandHandler> logger)

// In Handle() — add after auth check:
var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
if (business is null)
    return Result<BusinessStaffDto>.Failure(
        new Error("Business.NotFound", "Business not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && business.OwnerId != currentUser.UserId!.Value)
    return Result<BusinessStaffDto>.Failure(
        Error.Forbidden("You do not own this business."), Outcome.Forbidden);

// After save — publish integration event to outbox:
var integrationEvent = new BusinessStaffAddedIntegrationEvent(
    staff.Id, staff.BusinessId, staff.UserId, staff.Role.ToString());
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
await unitOfWork.SaveChangesAsync(cancellationToken);
```

---

### 🔴 Fix 4: `ListBusinessStaff` endpoint — wrong visibility (Anonymous instead of Authenticated)

**File**: `ContentPlaces.Presentation/Endpoints/BusinessStaff/BusinessStaffEndpoints.cs`

```csharp
// Current — WRONG:
.AllowAnonymous();

// Spec says: "Staff list is NOT public — Owner or admin only"
// Fix:
.WithMetadata(new MustHavePermissionAttribute(AppFeatures.BusinessStaff, AppAction.Read))
.RequireAuthorization();
```

---

### 🔴 Fix 5: `ListBusinessStaffQueryHandler` — No IDOR filtering

**File**: `ContentPlaces.Application/Queries/BusinessStaff/ListBusinessStaff/ListBusinessStaffQueryHandler.cs`

Returns staff to any caller without verifying they own the business.

**Fix**: Inject `ICurrentUser` and `IBusinessRepository`, verify ownership:
```csharp
public sealed class ListBusinessStaffQueryHandler(
    IBusinessStaffRepository staffRepository,
    IBusinessRepository businessRepository,    // ← ADD
    ICurrentUser currentUser,                  // ← ADD
    ILogger<ListBusinessStaffQueryHandler> logger)

// In Handle():
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result<IReadOnlyList<BusinessStaffDto>>.Failure(
        Error.Unauthorized("Authentication required"), Outcome.Unauthorized);

var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
if (business is null)
    return Result<IReadOnlyList<BusinessStaffDto>>.Failure(
        new Error("Business.NotFound", "Business not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && business.OwnerId != currentUser.UserId.Value)
    return Result<IReadOnlyList<BusinessStaffDto>>.Failure(
        Error.Forbidden("You do not own this business."), Outcome.Forbidden);
```

---

### 🔴 Fix 6: `UpdateAccessibilityFeaturesCommandHandler` — No auth check

**File**: `ContentPlaces.Application/Commands/AccessibilityFeature/UpdateAccessibilityFeatures/UpdateAccessibilityFeaturesCommandHandler.cs`

Spec says: **Admin only** for accessibility updates. Handler has no `ICurrentUser` check.

**Fix**: Inject `ICurrentUser` and guard:
```csharp
public sealed class UpdateAccessibilityFeaturesCommandHandler(
    IAccessibilityFeatureRepository featureRepository,
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,              // ← ADD
    ILogger<UpdateAccessibilityFeaturesCommandHandler> logger)

// In Handle() — add at the top:
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure(Error.Unauthorized("Authentication required"), Outcome.Unauthorized);

if (!currentUser.IsInRole("Admin"))
    return Result.Failure(Error.Forbidden("Admin access required."), Outcome.Forbidden);
```

---

### 🔴 Fix 7: Missing `BusinessStaff` integration events in `ContentPlaces.Contracts`

`BusinessStaff` is `AuditableEntity` (not aggregate root) — cannot use domain events.
These events must be published directly from command handlers via the outbox.

**Create in `ContentPlaces.Contracts/IntegrationEvents/`**:
```csharp
// BusinessStaffAddedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessStaffAddedIntegrationEvent(
    Guid BusinessStaffId, Guid BusinessId, Guid UserId, string Role) : IntegrationEventBase;

// BusinessStaffRemovedIntegrationEvent.cs
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessStaffRemovedIntegrationEvent(
    Guid BusinessStaffId, Guid BusinessId, Guid UserId) : IntegrationEventBase;
```

See Fix 2 and Fix 3 above for where to publish these from the handlers.

---

### 🟡 Fix 8: All 3 query records — missing `ICacheableQuery`

Add to `ContentPlacesCacheKeys.cs`:
```csharp
public static string BusinessAmenities(Guid businessId) => $"cp:biz:{businessId}:amenities";
public static string BusinessStaff(Guid businessId) => $"cp:biz:{businessId}:staff";
public static string PlaceAccessibility(Guid placeId) => $"cp:place:{placeId}:accessibility";
```

Implement `ICacheableQuery` on each query record:
```csharp
// ListBusinessAmenitiesQuery:
public sealed record ListBusinessAmenitiesQuery(Guid BusinessId, int Page = 1, int PageSize = 10)
    : IQuery<IReadOnlyList<BusinessAmenityDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.BusinessAmenities(BusinessId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["businesses", $"biz:{BusinessId}"];
}

// ListBusinessStaffQuery — same pattern with BusinessStaff(BusinessId) key
// GetAccessibilityFeaturesQuery — same pattern with PlaceAccessibility(PlaceId) key
```

Cache tags:
- Amenities: `["businesses", $"biz:{BusinessId}"]`
- Staff: `["businesses", $"biz:{BusinessId}"]`
- Accessibility: `["places", $"place:{PlaceId}"]`

---

### 🟡 Fix 9: Error codes don't follow `{Entity}.{Reason}` convention

**Files**: `AddBusinessAmenityCommandHandler.cs`, `AddBusinessStaffCommandHandler.cs`

| Current (wrong) | Correct |
|---|---|
| `"Auth.Unauthorized"` | `Error.Unauthorized(msg)` — uses the built-in factory |
| `"Auth.Forbidden"` | `Error.Forbidden(msg)` — uses the built-in factory |
| `"Business.NotFound"` inside an amenity handler for the business lookup | `"BusinessAmenity.BusinessNotFound"` |

---

### 🟡 Fix 10: `AccessibilityFeatureDto` missing `Guid Id`

**File**: `ContentPlaces.Application/Queries/AccessibilityFeature/Common/AccessibilityFeatureDto.cs`

Every DTO in this codebase exposes `Guid Id` as the first property so clients can reference records.

**Fix**:
```csharp
public sealed record AccessibilityFeatureDto(
    Guid Id,                        // ← ADD as first property
    AccessibilityFeatureType FeatureType,
    string Name,
    string? Description,
    bool IsAvailable)
{
    public static AccessibilityFeatureDto From(AccessibilityFeatureEntity feature) => new(
        feature.Id,                 // ← ADD
        feature.FeatureType,
        feature.Name,
        feature.Description,
        feature.IsAvailable);
}
```

---

### 🟠 Fix 11: `ListBusinessAmenitiesQuery` endpoint doesn't bind `Page`/`PageSize`

**File**: `ContentPlaces.Presentation/Endpoints/BusinessAmenity/BusinessAmenityEndpoints.cs`

```csharp
// Current — Page/PageSize never bound from query string:
var result = await sender.Send(new ListBusinessAmenitiesQuery(id), ct);

// Fix:
amenities.MapGet("/{id:guid}/amenities", async (
    Guid id, ISender sender, CancellationToken ct,
    int page = 1, int pageSize = 20) =>
{
    var result = await sender.Send(new ListBusinessAmenitiesQuery(id, page, pageSize), ct);
```

Also add page size range validation to `ListBusinessAmenitiesQueryValidator`.

---

### 🟠 Fix 12: `BusinessAmenity.Create()` and `BusinessStaff.Create()` — manually set `CreatedAt`

EF's `SaveChanges` interceptor (via `AuditableEntityInterceptor` in SharedKernel) manages `CreatedAt`.
`BaseEntity` also initializes `CreatedAt = DateTime.UtcNow` in the property initializer.
Setting it manually in the factory is redundant and creates two sources of truth.

```csharp
// Remove this line from BusinessAmenity.Create() and BusinessStaff.Create():
CreatedAt = DateTime.UtcNow,  // ← DELETE — managed by base class + EF interceptor
```

---

### 🟠 Fix 13: Arabic comments in production code

Remove all Arabic comments — production code must be in English:
- `// بيمنع duplicate deactivation` → `// Prevent double-deactivation`
- `// ترقيم الصفحات` → delete (code is self-explanatory)
- `// polymorphic: Place حالياً بس` → `// Polymorphic: EntityType = 1 = Place. Extend for Business, Tour, etc.`

---

## Ezz — Tasks 4+7 (ServiceItem + Geo-Search) Fixes

> **Status**: Most of the code exists but has critical security holes, wrong architecture,
> and missing fields that cause silent data corruption.

---

### 🔴 Fix 1: `ServiceItem` declares `IAggregateRoot` — architecturally wrong

**File**: `ContentPlaces.Domain/Entities/ServiceItem.cs`, line 6

```csharp
// CURRENT — WRONG:
public sealed class ServiceItem : AuditableEntity, IAggregateRoot

// FIX:
public sealed class ServiceItem : AuditableEntity
```

**Why**: The spec says `ServiceItem` is `AuditableEntity` (non-aggregate). It never calls `AddDomainEvent()`.
Marking it `IAggregateRoot` causes the `UnitOfWork<TContext>` to scan every `ServiceItem` in the change tracker
looking for domain events — wasted work, and misleads future developers.

Note: `AuditableEntity` already provides `SoftDelete()` via `ISoftDeletable` — so calling `item.SoftDelete()` in
`DeleteServiceItemCommandHandler` works correctly and does NOT need a custom implementation.

---

### 🔴 Fix 2: `ServiceItem.Create()` and `Update()` — missing `Category` and `Description` parameters

**File**: `ContentPlaces.Domain/Entities/ServiceItem.cs`

The entity has `Category` (ServiceCategory enum) and `Description` (string?) properties, but neither factory
method accepts or sets them. Every created `ServiceItem` always has `Category = default(0)` and `Description = null`
with no way to set them at creation time.

**Fix** — update `Create()`:
```csharp
public static ServiceItem Create(
    Guid businessId,
    string name,
    decimal price,
    string currency,
    ServiceCategory category,      // ← ADD
    int durationMinutes,
    int maxCapacity,
    string? description = null,    // ← ADD
    int sortOrder = 0)
{
    if (price < 0) throw new ArgumentException("Price cannot be negative", nameof(price));
    if (durationMinutes <= 0) throw new ArgumentException("Duration must be positive", nameof(durationMinutes));
    if (maxCapacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(maxCapacity));

    return new ServiceItem
    {
        Id = Guid.CreateVersion7(),
        BusinessId = businessId,
        Name = name.Trim(),
        Description = description?.Trim(),
        Price = price,
        Currency = currency.ToUpperInvariant(),
        Category = category,
        DurationMinutes = durationMinutes,
        MaxCapacity = maxCapacity,
        IsAvailable = true,
        SortOrder = sortOrder,
    };
}
```

**Fix** — update `Update()` the same way (add `category` and `description` parameters).

**Fix** — update `CreateServiceItemCommand` to include the new fields:
```csharp
public sealed record CreateServiceItemCommand(
    Guid BusinessId,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    ServiceCategory Category,      // ← ADD
    string? Description = null,    // ← ADD
    int SortOrder = 0)
    : ICommand<CreateServiceItemResult>;
```

**Fix** — update `CreateServiceItemCommandValidator`:
```csharp
RuleFor(x => x.Category).IsInEnum();
// Description is optional — no required rule needed
```

**Fix** — update the handler call and the endpoint request model accordingly.

---

### 🔴 Fix 3: `ServiceItem` — duplicate `PriceCurrency` column

**File**: `ContentPlaces.Domain/Entities/ServiceItem.cs`, lines 14–15

```csharp
public string PriceCurrency { get; private set; } = "JOD";   // ← REMOVE — redundant
public string Currency { get; private set; } = "JOD";         // ← KEEP this one
```

The EF config (`ServiceItemConfiguration.cs`) maps **both** to the database. The `ServiceItems` table has two
currency columns always set to the same value.

**Fix**:
1. Remove `public string PriceCurrency { get; private set; }` from `ServiceItem.cs`
2. Remove `PriceCurrency = currency.ToUpperInvariant()` from `Create()` and `Update()`
3. Remove the `builder.Property(x => x.PriceCurrency)` block from `ServiceItemConfiguration.cs`
4. Also remove `SalePriceCurrency` property (line 23 in entity, line 56–58 in config) — no method sets it
5. **Add a new EF migration** to drop both columns from the database

---

### 🔴 Fix 4: `CreateServiceItemCommandHandler` + `DeleteServiceItemCommandHandler` — use `IPublisher.Publish()` instead of outbox (not durable)

**Files**:
- `ContentPlaces.Application/Commands/ServiceItem/CreateServiceItem/CreateServiceItemCommandHandler.cs` (lines 56–58)
- `ContentPlaces.Application/Commands/ServiceItem/DeleteServiceItem/DeleteServiceItemCommandHandler.cs` (lines 46–48)

```csharp
// CURRENT — WRONG (fires in-process MediatR, not durable):
await publisher.Publish(new ServiceItemCreateIntegrationEvent(...), cancellationToken);
```

If the app restarts after `SaveChanges` but before `Publish`, the integration event is **permanently lost**.
Every other module in this codebase uses the outbox pattern.

**Fix** — remove `IPublisher`, inject `ContentPlacesDbContext`, write to outbox BEFORE save:
```csharp
// In CreateServiceItemCommandHandler — replace IPublisher with ContentPlacesDbContext:
public sealed class CreateServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IContentPlacesUnitOfWork unitOfWork,
    ContentPlacesDbContext dbContext,           // ← ADD
    ICurrentUser currentUser,                   // ← ADD (see Fix 5)
    IBusinessRepository businessRepository,     // ← ADD (see Fix 5)
    ILogger<CreateServiceItemCommandHandler> logger)

// After AddAsync, BEFORE SaveChanges:
await serviceItemRepository.AddAsync(item, cancellationToken);
var outboxEvent = new ServiceItemCreatedIntegrationEvent(
    item.Id, item.BusinessId, item.Name, item.Price, item.Currency);
dbContext.OutboxMessages.Add(OutboxMessage.Create(outboxEvent));
await unitOfWork.SaveChangesAsync(cancellationToken);  // saves item + outbox atomically
```

Same pattern for `DeleteServiceItemCommandHandler` with `ServiceItemDeletedIntegrationEvent`.

---

### 🔴 Fix 5: All 3 write handlers — No IDOR check (security hole)

**Files**: `CreateServiceItemCommandHandler`, `UpdateServiceItemCommandHandler`, `DeleteServiceItemCommandHandler`

None inject `ICurrentUser`. Any authenticated user can create, update, or delete service items for any business
they do not own. This is the most urgent fix — it's an authorization bypass.

**Fix** — add to all three handlers:
```csharp
// Inject:
ICurrentUser currentUser,
IBusinessRepository businessRepository,

// In Handle() — after loading the item (or at start for Create):
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure(Error.Unauthorized("Authentication required"), Outcome.Unauthorized);

var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
if (business is null)
    return Result.Failure(
        new Error("Business.NotFound", "Business not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && business.OwnerId != currentUser.UserId.Value)
    return Result.Failure(
        Error.Forbidden("You do not own this business."), Outcome.Forbidden);
```

For `UpdateServiceItemCommandHandler` and `DeleteServiceItemCommandHandler`: load the item first, get its
`BusinessId`, then load the business for ownership check.

---

### 🟡 Fix 6: `ListServiceItemsQueryHandler` — No `IsAvailable` filter for public users

**File**: `ContentPlaces.Application/Queries/ServiceItem/ListServiceItems/ListServiceItemsQueryHandler.cs`

```csharp
// CURRENT — shows ALL items regardless of availability:
filter: x => x.BusinessId == request.BusinessId,
```

Spec: **Public shows `IsAvailable = true` only. Owner/Admin sees all.**

**Fix** — inject `ICurrentUser` and `IBusinessRepository`:
```csharp
public sealed class ListServiceItemsQueryHandler(
    IServiceItemRepository serviceItemRepository,
    IBusinessRepository businessRepository,
    ICurrentUser currentUser,
    ILogger<ListServiceItemsQueryHandler> logger)

// In Handle():
var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
var isAdmin = currentUser.IsInRole("Admin");
var isOwner = business is not null
              && currentUser.UserId.HasValue
              && business.OwnerId == currentUser.UserId.Value;

var filter = (isAdmin || isOwner)
    ? (Expression<Func<Domain.Entities.ServiceItem, bool>>)(x => x.BusinessId == request.BusinessId)
    : x => x.BusinessId == request.BusinessId && x.IsAvailable;

var items = await serviceItemRepository.SelectAsync(
    selector: x => ServiceItemDto.From(x),
    filter: filter,
    orderBy: q => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name),
    ct: cancellationToken);
```

---

### 🟡 Fix 7: `ServiceItemCreateIntegrationEvent` — wrong name (missing 'd') + old namespace

**File**: `ContentPlaces.Contracts/IntegrationEvents/ServiceItemCreateIntegrationEvent.cs`

```csharp
// CURRENT — wrong name, old brace namespace, unnecessary usings:
namespace ContentPlaces.Contracts.IntegrationEvents
{
   public sealed record ServiceItemCreateIntegrationEvent(...)   // missing 'd'
```

**Fix** — rename the file to `ServiceItemCreatedIntegrationEvent.cs` and update its content:
```csharp
using YallaJo.SharedKernel.Domain.Event;
namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record ServiceItemCreatedIntegrationEvent(
    Guid ServiceItemId,
    Guid BusinessId,
    string Name,
    decimal Price,
    string Currency) : IntegrationEventBase;
```

Update all references in `CreateServiceItemCommandHandler.cs` from `ServiceItemCreateIntegrationEvent`
to `ServiceItemCreatedIntegrationEvent`.

---

### 🟡 Fix 8: `CreateServiceItemResult.cs` — old-style namespace + unnecessary usings

**File**: `ContentPlaces.Application/Commands/ServiceItem/CreateServiceItem/CreateServiceItemResult.cs`

```csharp
// CURRENT — 5 unnecessary usings + brace namespace:
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem
{
    public sealed record CreateServiceItemResult(Guid ServiceItemId, string Name);
}
```

**Fix**:
```csharp
namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;

public sealed record CreateServiceItemResult(Guid ServiceItemId, string Name);
```

---

### 🟡 Fix 9: `ListServiceItemsQuery` + `GetServiceItemByIdQuery` — missing `ICacheableQuery`

**Files**:
- `ContentPlaces.Application/Queries/ServiceItem/ListServiceItems/ListServiceItemsQuery.cs`
- `ContentPlaces.Application/Queries/ServiceItem/GetServiceItemById/GetServiceItemByIdQuery.cs`

Neither implements `ICacheableQuery`. Add cache keys to `ContentPlacesCacheKeys.cs`:
```csharp
public static string ServiceItemList(Guid businessId) => $"cp:biz:{businessId}:services";
public static string ServiceItem(Guid id) => $"cp:service:{id}";
```

Implement on each query:
```csharp
// ListServiceItemsQuery.cs:
public sealed record ListServiceItemsQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<ServiceItemDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.ServiceItemList(BusinessId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["businesses", $"biz:{BusinessId}:services"];
}

// GetServiceItemByIdQuery.cs:
public sealed record GetServiceItemByIdQuery(Guid BusinessId, Guid ServiceItemId)
    : IQuery<ServiceItemDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.ServiceItem(ServiceItemId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["businesses", $"service:{ServiceItemId}"];
}
```

Also add `HybridCache` invalidation to all 3 write handlers after save:
```csharp
// CreateServiceItemCommandHandler:
await cache.RemoveByTagAsync($"biz:{request.BusinessId}:services", cancellationToken);

// UpdateServiceItemCommandHandler:
await cache.RemoveByTagAsync($"service:{request.Id}", cancellationToken);
await cache.RemoveByTagAsync($"biz:{item.BusinessId}:services", cancellationToken);

// DeleteServiceItemCommandHandler:
await cache.RemoveByTagAsync($"service:{request.Id}", cancellationToken);
await cache.RemoveByTagAsync($"biz:{item.BusinessId}:services", cancellationToken);
```

---

### 🟡 Fix 10: `GetNearbyPlacesQuery` + `GetMapViewportQuery` — missing `ICacheableQuery`

**Files**:
- `ContentPlaces.Application/Queries/Place/GetNearbyPlaces/GetNearbyPlacesQuery.cs`
- `ContentPlaces.Application/Queries/Place/GetMapViewport/GetMapViewportQuery.cs`

Add cache keys to `ContentPlacesCacheKeys.cs`:
```csharp
public static string NearbyPlaces(double lat, double lng, double radiusKm, int pageSize)
    => $"cp:places:nearby:lat{lat:F4}:lng{lng:F4}:r{radiusKm}:s{pageSize}";

public static string MapViewport(double n, double s, double e, double w)
    => $"cp:places:viewport:n{n:F4}:s{s:F4}:e{e:F4}:w{w:F4}";
```

Implement on each query:
```csharp
// GetNearbyPlacesQuery.cs:
public sealed record GetNearbyPlacesQuery(double Lat, double Lng, double RadiusKm = 10, int PageSize = 10)
    : IQuery<IReadOnlyList<NearbyPlaceSummaryDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.NearbyPlaces(Lat, Lng, RadiusKm, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);  // short TTL — geo data changes
    public IReadOnlyList<string> Tags => ["places"];
}

// GetMapViewportQuery.cs:
public sealed record GetMapViewportQuery(double NorthLat, double SouthLat, double EastLng, double WestLng)
    : IQuery<MapViewportResponse>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.MapViewport(NorthLat, SouthLat, EastLng, WestLng);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => ["places"];
}
```

---

### 🟡 Fix 11: `GetNearbyPlacesQueryHandler` — loads all candidates into memory then filters

**File**: `ContentPlaces.Application/Queries/Place/GetNearbyPlaces/GetNearbyPlacesQueryHandler.cs`

Current implementation materializes a bounding-box result set into memory then computes Haversine in C#.
The spec explicitly says: **"Uses Haversine formula (raw SQL)"**.

**Fix** — inject `ContentPlacesDbContext` and use `FromSqlInterpolated`:
```csharp
public sealed class GetNearbyPlacesQueryHandler(
    ContentPlacesDbContext dbContext,   // ← replace IPlaceRepository
    ILogger<GetNearbyPlacesQueryHandler> logger)

// In Handle() — SQL Server compatible Haversine:
var lat = request.Lat;
var lng = request.Lng;
var radiusKm = request.RadiusKm;
var pageSize = request.PageSize;

var results = await dbContext.Database
    .SqlQuery<NearbyPlaceRaw>($"""
        SELECT Id, Name, Slug, Latitude, Longitude, AverageRating, PlaceType,
               6371 * ACOS(
                   COS(RADIANS({lat})) * COS(RADIANS(Latitude))
                   * COS(RADIANS(Longitude) - RADIANS({lng}))
                   + SIN(RADIANS({lat})) * SIN(RADIANS(Latitude))
               ) AS DistanceKm
        FROM (
            SELECT * FROM content_places.Places
            WHERE IsDeleted = 0
              AND Latitude != 0.0
              AND Longitude != 0.0
        ) AS sub
        WHERE 6371 * ACOS(
                  COS(RADIANS({lat})) * COS(RADIANS(Latitude))
                  * COS(RADIANS(Longitude) - RADIANS({lng}))
                  + SIN(RADIANS({lat})) * SIN(RADIANS(Latitude))
              ) <= {radiusKm}
        ORDER BY DistanceKm
        OFFSET 0 ROWS FETCH NEXT {pageSize} ROWS ONLY
        """)
    .AsNoTracking()
    .ToListAsync(cancellationToken);
```

The existing in-memory Haversine function (`CalculateDistance`) is correct mathematically — it just runs in C#
instead of SQL. If raw SQL is too complex to wire up right now, keep the in-memory approach as a temporary
workaround but add a `// TODO: move to raw SQL` comment.

---

### 🟡 Fix 12: `NearbyPlaceSummaryDto` — wrong fields (missing Slug, Lat, Lng; has unused Category)

**File**: `ContentPlaces.Application/Queries/Place/Common/NearbyPlaceSummaryDto.cs`

```csharp
// CURRENT — wrong shape:
public sealed record NearbyPlaceSummaryDto(
    Guid Id, string Name, string? PrimaryImageUrl,
    double AverageRating, double DistanceKm, string Category)
```

The handler maps `PlaceType.ToString()` into `Category` which is semantically wrong.
The spec requires `Slug`, `Latitude`, `Longitude` for map/link rendering.

**Fix**:
```csharp
public sealed record NearbyPlaceSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Latitude,
    decimal Longitude,
    decimal AverageRating,
    double DistanceKm);
```

Update the handler mapping in `GetNearbyPlacesQueryHandler` to populate these fields.

---

### 🟠 Fix 13: `ServiceItemEndpoints.cs` — route structure doesn't match spec for detail/update/delete

**File**: `ContentPlaces.Presentation/Endpoints/ServiceItem/ServiceItemEndpoints.cs`

Current: all 5 endpoints are under `/places/businesses/{businessId:guid}/services/...`
Spec: list and create are under `/{businessId}/services` but get/update/delete are under `/services/{id}` (no businessId in URL).

```
Spec routes:
  GET  /places/businesses/{id}/services         ← businessId in path ✓
  GET  /places/businesses/services/{id}         ← no businessId in path ✗ (currently has businessId)
  POST /places/businesses/{id}/services         ← businessId in path ✓
  PUT  /places/businesses/services/{id}         ← no businessId in path ✗
  DELETE /places/businesses/services/{id}       ← no businessId in path ✗
```

**Fix** — split into two route groups:
```csharp
// Group 1: business-scoped (List + Create)
var bizServices = group.MapGroup("/places/businesses")
    .WithTags("ContentPlaces | ServiceItems");

bizServices.MapGet("/{businessId:guid}/services", ...);   // List
bizServices.MapPost("/{businessId:guid}/services", ...);  // Create

// Group 2: item-level (Get + Update + Delete — no businessId in route)
var itemServices = group.MapGroup("/places/businesses/services")
    .WithTags("ContentPlaces | ServiceItems");

itemServices.MapGet("/{id:guid}", ...);    // Get by Id
itemServices.MapPut("/{id:guid}", ...);    // Update
itemServices.MapDelete("/{id:guid}", ...); // Delete
```

For update/delete commands: remove `BusinessId` from the command record since it's no longer in the route.
Load the item by Id in the handler and use `item.BusinessId` for the business lookup.

---

## Fix Priority Summary (Complete — All Tasks)

| # | Owner | Severity | Fix |
|---|-------|----------|-----|
| 1 | Mahmoud | 🔴 Critical | `DeletePlace` always blocks — fix `HasActiveLinkedBusinessesAsync` |
| 2 | Mahmoud | 🔴 Critical | `CreatePlace` slug checks wrong variable (`request.Slug` → `slug`) |
| 3 | Mahmoud + Mohammad | 🔴 Critical | Domain events never dispatch — use `IUnitOfWork<ContentPlacesDbContext>` on all Place + Business handlers |
| 4 | Mahmoud + Mohammad | 🔴 Critical | Domain event handlers must write outbox rows for integration events |
| 5 | Mohammad | 🔴 Critical | Create 5 Business integration event records in Contracts |
| 6 | Mohammad | 🔴 Critical | Create 5 Business domain event handlers in Infrastructure |
| 7 | Fadwa | 🔴 Critical | `RemoveBusinessAmenity` — no IDOR check |
| 8 | Fadwa | 🔴 Critical | `RemoveBusinessStaff` — no auth check at all |
| 9 | Fadwa | 🔴 Critical | `AddBusinessStaff` — no business existence check + no IDOR check |
| 10 | Fadwa | 🔴 Critical | `ListBusinessStaff` endpoint must be `RequireAuthorization()` not anonymous |
| 11 | Fadwa | 🔴 Critical | `ListBusinessStaffQueryHandler` — no IDOR filtering |
| 12 | Fadwa | 🔴 Critical | `UpdateAccessibilityFeatures` — no auth check |
| 13 | Fadwa | 🔴 Critical | Create `BusinessStaffAdded` + `BusinessStaffRemoved` integration events in Contracts |
| 14 | Ezz | 🔴 Critical | `ServiceItem` declares `IAggregateRoot` — must be plain `AuditableEntity` |
| 15 | Ezz | 🔴 Critical | `ServiceItem.Create/Update` missing `Category` + `Description` — items always have wrong data |
| 16 | Ezz | 🔴 Critical | `ServiceItem` has duplicate `PriceCurrency` column — remove from entity + config + add migration |
| 17 | Ezz | 🔴 Critical | `CreateServiceItem`/`DeleteServiceItem` use `IPublisher.Publish()` — must use outbox |
| 18 | Ezz | 🔴 Critical | All 3 ServiceItem write handlers — no IDOR check (authorization bypass) |
| 19 | Mohammad | 🔴 Critical | `DependencyInjection.cs` — duplicate `IBusinessRepository` registration |
| 20 | Ezz | 🟡 Medium | `ListServiceItems` — no `IsAvailable` filter for public users |
| 21 | Fadwa | 🟡 Medium | `ListBusinessStaffQueryHandler` inject `ICurrentUser` + ownership guard |
| 22 | All | 🟡 Medium | Add `ICacheableQuery` to all uncached query records |
| 23 | All | 🟡 Medium | Add `HybridCache` invalidation to all uncached write handlers |
| 24 | Mahmoud | 🟡 Medium | `ListPlaces` — `CategoryId` and `HasActiveTours` filters ignored in spec |
| 25 | Fadwa | 🟡 Medium | `AccessibilityFeatureDto` missing `Guid Id` |
| 26 | Fadwa | 🟡 Medium | Fix error codes to `{Entity}.{Reason}` pattern |
| 27 | Ezz | 🟡 Medium | `ServiceItemCreateIntegrationEvent` wrong name (missing 'd') + old namespace |
| 28 | Ezz | 🟡 Medium | `GetNearbyPlacesQueryHandler` — in-memory Haversine, spec says raw SQL |
| 29 | Ezz | 🟡 Medium | `NearbyPlaceSummaryDto` wrong fields — missing Slug/Lat/Lng, has wrong Category field |
| 30 | Ezz | 🟡 Medium | `CreateServiceItemResult.cs` — old namespace + unnecessary usings |
| 31 | Fadwa | 🟠 Minor | Bind `Page`/`PageSize` in `ListAmenities` endpoint |
| 32 | Fadwa | 🟠 Minor | Remove manual `CreatedAt = DateTime.UtcNow` from factory methods |
| 33 | Fadwa | 🟠 Minor | Replace Arabic comments with English |
| 34 | Mahmoud | 🟠 Minor | `UpdatePlaceResult.cs` — fix old brace namespace style |
| 35 | Ezz | 🟠 Minor | `ServiceItemEndpoints.cs` — route structure wrong for get/update/delete (no businessId in URL) |

---

## Inbound Integration Events (pre-existing, no work needed)

| Event | Source | Handler | What It Does |
|-------|--------|---------|-------------|
| `LanguageActivatedIntegrationEvent` | ContentCore | `LanguageActivatedIntegrationEventHandler` | Backfills Place + Business translations when a new language is activated |
