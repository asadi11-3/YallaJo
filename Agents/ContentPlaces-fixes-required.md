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

### Non-Aggregates Cannot Use Domain Events

`ServiceItem`, `BusinessHours`, `BusinessAmenity`, `BusinessStaff`, `AccessibilityFeature` are NOT aggregate roots.
They extend `AuditableEntity` or `BaseEntity`, NOT `IAggregateRoot`. The UoW only collects events from `IAggregateRoot`.

For these entities, to publish integration events: **publish directly from the command handler**:

```csharp
// In AddBusinessStaffCommandHandler, AFTER successful save:
var integrationEvent = new BusinessStaffAddedIntegrationEvent(staff.Id, staff.BusinessId, staff.UserId, staff.Role.ToString());
var outboxMessage = OutboxMessage.Create(integrationEvent);
dbContext.OutboxMessages.Add(outboxMessage);
// Then save — the outbox row is committed atomically with the entity change
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
`PlaceRepository` by checking the `PlaceBusiness` junction or `Business` table:

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

### 🔴 Fix 3: Domain events never dispatched — wrong UoW in all command handlers

**Files**: All 5 Place command handlers (Create, Update, Delete, Feature, Verify)

**Problem**: Every handler injects `IContentPlacesUnitOfWork`. This wraps raw `context.SaveChangesAsync()` — it
does NOT dispatch domain events. So `Place.Create()` adds `PlaceCreatedDomainEvent` to the entity's list, but
when `unitOfWork.SaveChangesAsync()` is called, that event is silently discarded. The translation handler never
runs. No outbox rows are written.

**Fix for aggregate-mutating commands (Create, Update):**
Inject `IUnitOfWork<ContentPlacesDbContext>` from SharedKernel INSTEAD OF (or IN ADDITION TO) `IContentPlacesUnitOfWork`. The SharedKernel UoW dispatches events before saving:

```csharp
// Change this:
public sealed class CreatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,   // ← does not dispatch events
    ILogger<CreatePlaceCommandHandler> logger)

// To this:
using YallaJo.SharedKernel.Application.Abstractions.Data;
using ContentPlaces.Infrastructure.Persistence;

public sealed class CreatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IUnitOfWork<ContentPlacesDbContext> unitOfWork,  // ← dispatches domain events
    ILogger<CreatePlaceCommandHandler> logger)
```

Then keep the same `try/catch` pattern but catch `ContentPlacesConcurrencyException` as before.

> **Note**: For Feature/Verify commands (which also mutate aggregates via `SetFeatured`/`SetVerified`), these
> methods don't raise domain events, so `IContentPlacesUnitOfWork` is technically fine. But for consistency
> and future-proofing, prefer `IUnitOfWork<ContentPlacesDbContext>` on all aggregate commands.

---

### 🔴 Fix 4: `PlaceCreatedDomainEventHandler` and `PlaceUpdatedDomainEventHandler` — missing outbox write

**Files**:
- `ContentPlaces.Infrastructure/EventHandlers/PlaceCreatedDomainEventHandler.cs`
- `ContentPlaces.Infrastructure/EventHandlers/PlaceUpdatedDomainEventHandler.cs`

Both handlers run translation logic but never publish integration events. ContentSeo, Analytics, and other
consumers registered in the task description will never receive `PlaceCreatedIntegrationEvent`.

**Fix**: After translation logic, write to outbox:

```csharp
// Add to PlaceCreatedDomainEventHandler, inject ContentPlacesDbContext:
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

The same pattern applies to `PlaceUpdatedDomainEventHandler` (publish `PlaceUpdatedIntegrationEvent`).

For `DeletePlace`, there is no domain event raised — the integration event `PlaceDeletedIntegrationEvent`
must be written to the outbox directly in `DeletePlaceCommandHandler` after a successful save.

---

### 🟡 Fix 5: All 3 query records — missing `ICacheableQuery`

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
```

---

### 🟡 Fix 6: All 5 command handlers — missing `HybridCache` invalidation

Inject `HybridCache` and call `RemoveByTagAsync` after successful saves in Create, Update, Delete, Feature, Verify:

```csharp
// After save in CreatePlaceCommandHandler:
await cache.RemoveByTagAsync("places", cancellationToken);

// After save in UpdatePlaceCommandHandler:
await cache.RemoveByTagAsync($"place:{request.Id}", cancellationToken);
await cache.RemoveByTagAsync("places", cancellationToken);

// After save in DeletePlaceCommandHandler:
await cache.RemoveByTagAsync($"place:{request.PlaceId}", cancellationToken);
await cache.RemoveByTagAsync("places", cancellationToken);

// Feature and Verify same as Update
```

---

### 🟡 Fix 7: `ListPlacesQuery` — `CategoryId` and `HasActiveTours` filters silently ignored

`PlaceFilterSpecification` does not accept or apply `CategoryId` or `HasActiveTours`. The spec requires both.
Add them to the specification constructor and filter expressions.

---

## Mohammad — Tasks 2+3 (Business + BusinessHours) Fixes

### 🔴 Fix 1: Business domain events are dispatched but integration events not written to outbox

**Status**: Business command handlers use `IContentPlacesUnitOfWork` — same problem as Place.

All Business commands that mutate `Business` (an aggregate root) need `IUnitOfWork<ContentPlacesDbContext>`
to actually dispatch domain events. Then the domain event handlers (which need to be created — see Fix 2) will run.

---

### 🔴 Fix 2: Business domain event handlers are missing entirely

The task spec requires these handlers in `ContentPlaces.Infrastructure/EventHandlers/`:

| Handler | Triggered by | Does |
|---------|-------------|------|
| `BusinessCreatedDomainEventHandler` | `BusinessCreatedDomainEvent` | Triggers translation + writes `BusinessCreatedIntegrationEvent` to outbox |
| `BusinessApprovedDomainEventHandler` | `BusinessApprovedDomainEvent` | Writes `BusinessApprovedIntegrationEvent` to outbox |
| `BusinessRejectedDomainEventHandler` | `BusinessRejectedDomainEvent` | Writes `BusinessRejectedIntegrationEvent` to outbox |
| `BusinessSuspendedDomainEventHandler` | `BusinessSuspendedDomainEvent` | Writes `BusinessSuspendedIntegrationEvent` to outbox |
| `BusinessReinstatedDomainEventHandler` | `BusinessReinstatedDomainEvent` | Writes `BusinessReinstatedIntegrationEvent` to outbox |

**Template** (copy pattern from `PlaceCreatedDomainEventHandler`):
```csharp
// ContentPlaces.Infrastructure/EventHandlers/BusinessApprovedDomainEventHandler.cs
public sealed class BusinessApprovedDomainEventHandler(
    ContentPlacesDbContext dbContext,
    ILogger<BusinessApprovedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<BusinessApprovedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<BusinessApprovedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        // Write integration event to outbox (owner will be notified via Messaging module)
        var integrationEvent = new BusinessApprovedIntegrationEvent(evt.BusinessId, evt.ReviewedByUserId);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        // Do NOT call SaveChangesAsync() here

        logger.LogInformation("BusinessApprovedDomainEvent: queued outbox for Business {Id}", evt.BusinessId);
        return Task.CompletedTask;
    }
}
```

---

### 🔴 Fix 3: Business integration event records missing from `ContentPlaces.Contracts`

The integration event `.cs` files that other modules will subscribe to DO NOT EXIST in the repo yet.
The Contracts project only contains Place events.

Create these files in `ContentPlaces.Contracts/IntegrationEvents/`:

```csharp
// BusinessCreatedIntegrationEvent.cs
public sealed record BusinessCreatedIntegrationEvent(
    Guid BusinessId, string Name, string Slug, Guid OwnerId, Guid? PlaceId) : IntegrationEventBase;

// BusinessApprovedIntegrationEvent.cs
public sealed record BusinessApprovedIntegrationEvent(
    Guid BusinessId, Guid OwnerId) : IntegrationEventBase;

// BusinessRejectedIntegrationEvent.cs
public sealed record BusinessRejectedIntegrationEvent(
    Guid BusinessId, Guid OwnerId, string Reason) : IntegrationEventBase;

// BusinessSuspendedIntegrationEvent.cs
public sealed record BusinessSuspendedIntegrationEvent(
    Guid BusinessId, Guid OwnerId, string Reason) : IntegrationEventBase;

// BusinessReinstatedIntegrationEvent.cs
public sealed record BusinessReinstatedIntegrationEvent(
    Guid BusinessId, Guid OwnerId) : IntegrationEventBase;
```

---

## Fadwa — Tasks 5+6+8 (Amenity, Staff, Accessibility) Fixes

### 🔴 Fix 1: Remove `IContentPlacesDbContext` from Application layer — Architecture violation

**File**: `ContentPlaces.Application/Interfaces/IContentPlacesDbContext.cs`

`DbSet<T>` is an EF Core type. Having it in `Application` creates a hard dependency from Application → EF Core,
violating clean architecture. The spec explicitly says *"No separate repository needed — use DbContext directly"*,
meaning handlers in Application can use it. The interface is the right approach but must NOT expose `DbSet<T>`.

**Correct fix**: The `IContentPlacesDbContext` interface is actually fine as a design choice for these entities
(no full repository needed). But it must be moved to **Infrastructure** or the interface must hide EF details.

The simplest correct approach for entities without repositories (BusinessAmenity, BusinessStaff, AccessibilityFeature)
is to keep `IContentPlacesDbContext` but move it to `ContentPlaces.Infrastructure` (not Application) and inject
`ContentPlacesDbContext` directly. OR accept the interface as-is since `ContentPlacesDbContext` already implements
it and it's already registered.

The **actual rule being violated** is that `DbSet<T>` (from `Microsoft.EntityFrameworkCore`) is referenced in
an Application project that should have no EF dependency. Add EF to `ContentPlaces.Application.csproj`:

```xml
<!-- This is already there implicitly via transitive refs but make explicit: -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.13" />
```

OR — the cleaner fix — move `IContentPlacesDbContext` from Application to Infrastructure and reference it only there.

---

### 🔴 Fix 2: `RemoveBusinessAmenityCommandHandler` — IDOR (anyone can delete anyone's amenity)

**File**: `ContentPlaces.Application/Commands/BusinessAmenity/RemoveBusinessAmenity/RemoveBusinessAmenityCommandHandler.cs`

```csharp
// Current — NO ownership check:
var amenity = await dbContext.BusinessAmenities.FirstOrDefaultAsync(x => x.Id == request.AmenityId, ct);
dbContext.BusinessAmenities.Remove(amenity);
```

**Fix**: Load the business via the amenity and check ownership:
```csharp
var amenity = await dbContext.BusinessAmenities
    .Include(a => a.Business)
    .FirstOrDefaultAsync(x => x.Id == request.AmenityId, cancellationToken);

if (amenity is null)
    return Result.Failure(new Error("BusinessAmenity.NotFound", "Amenity not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && amenity.Business.OwnerId != currentUser.UserId!.Value)
    return Result.Failure(Error.Forbidden("You do not own this business."), Outcome.Forbidden);
```

---

### 🔴 Fix 3: `RemoveBusinessStaffCommandHandler` — No auth check at all

**File**: `ContentPlaces.Application/Commands/BusinessStaff/RemoveBusinessStaff/RemoveBusinessStaffCommandHandler.cs`

Currently has NO `ICurrentUser` injection. Any request that reaches this handler can deactivate staff.

**Fix**: Inject `ICurrentUser` and add ownership check:
```csharp
public sealed class RemoveBusinessStaffCommandHandler(
    IContentPlacesDbContext dbContext,
    IContentPlacesUnitOfWork unitOfWork,
    ICurrentUser currentUser,              // ← ADD
    ILogger<RemoveBusinessStaffCommandHandler> logger)

// In Handle():
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure(Error.Unauthorized("Authentication required"), Outcome.Unauthorized);

var staff = await dbContext.BusinessStaff
    .Include(s => s.Business)              // ← load business for ownership check
    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && staff.Business.OwnerId != currentUser.UserId.Value)
    return Result.Failure(Error.Forbidden("Not allowed."), Outcome.Forbidden);
```

---

### 🔴 Fix 4: `UpdateAccessibilityFeaturesCommandHandler` — No auth check

**File**: `ContentPlaces.Application/Commands/AccessibilityFeature/UpdateAccessibilityFeatures/UpdateAccessibilityFeaturesCommandHandler.cs`

Spec says: **Admin only** for accessibility updates. Handler has no `ICurrentUser` check.

**Fix**: Inject `ICurrentUser` and guard:
```csharp
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure(Error.Unauthorized("Authentication required"), Outcome.Unauthorized);
// Auth policy already enforced at endpoint level but handler must also guard (defence in depth)
```

---

### 🔴 Fix 5: `UpdateAccessibilityFeaturesCommandHandler` — Double DB query (unused variable)

```csharp
// Lines 35-37: fetches data into `oldFeatures` which is NEVER used after this
var oldFeatures = await dbContext.AccessibilityFeatures
    .Where(x => x.EntityId == request.PlaceId && x.EntityType == PlaceEntityType)
    .ToListAsync(cancellationToken);

// Lines 41-43: fetches SAME data again into `existingFeatures`, which IS used for RemoveRange
var existingFeatures = await dbContext.AccessibilityFeatures
    .Where(x => x.EntityId == request.PlaceId && x.EntityType == PlaceEntityType)
    .ToListAsync(cancellationToken);

dbContext.AccessibilityFeatures.RemoveRange(existingFeatures);
```

**Fix**: Delete lines 35–37 entirely. Use only `existingFeatures`.

---

### 🔴 Fix 6: `ListBusinessStaff` endpoint — wrong visibility (Anonymous instead of Authenticated)

**File**: `ContentPlaces.Presentation/Endpoints/BusinessStaff/BusinessStaffEndpoints.cs`

```csharp
// Current — WRONG:
.AllowAnonymous();

// Spec says: "Staff list is NOT public — Owner or admin only"
// Fix:
.RequireAuthorization();
```

The query handler also has no `ICurrentUser` check. Add IDOR filtering in the handler:
only show staff if `currentUser` is the business owner or an admin.

---

### 🔴 Fix 7: Missing `BusinessStaffAdded` and `BusinessStaffRemoved` integration events

`BusinessStaff` is `AuditableEntity` (not aggregate root) — cannot use domain events.
Publish directly from command handlers after successful save.

**Create in `ContentPlaces.Contracts/IntegrationEvents/`**:
```csharp
// BusinessStaffAddedIntegrationEvent.cs
public sealed record BusinessStaffAddedIntegrationEvent(
    Guid BusinessStaffId, Guid BusinessId, Guid UserId, string Role) : IntegrationEventBase;

// BusinessStaffRemovedIntegrationEvent.cs
public sealed record BusinessStaffRemovedIntegrationEvent(
    Guid BusinessStaffId, Guid BusinessId, Guid UserId) : IntegrationEventBase;
```

**In `AddBusinessStaffCommandHandler`, after successful save:**
```csharp
var integrationEvent = new BusinessStaffAddedIntegrationEvent(
    staff.Id, staff.BusinessId, staff.UserId, staff.Role.ToString());
dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
await unitOfWork.SaveChangesAsync(cancellationToken);  // saves staff + outbox atomically
```

---

### 🟡 Fix 8: All 3 query records — missing `ICacheableQuery`

Add to `ContentPlacesCacheKeys.cs`:
```csharp
public static string BusinessAmenities(Guid businessId) => $"cp:biz:{businessId}:amenities";
public static string BusinessStaff(Guid businessId, Guid userId) => $"cp:biz:{businessId}:staff:u:{userId}";
public static string PlaceAccessibility(Guid placeId) => $"cp:place:{placeId}:accessibility";
```

Implement `ICacheableQuery` on each query record. Cache tags:
- Amenities: `["businesses", $"biz:{businessId}"]`
- Staff: `["businesses", $"biz:{businessId}"]`
- Accessibility: `["places", $"place:{placeId}"]`

---

### 🟡 Fix 9: All command handlers — missing `try/catch` (no error handling)

None of the 5 command handlers (AddAmenity, RemoveAmenity, AddStaff, RemoveStaff, UpdateAccessibility)
have `try/catch` for cancellation or concurrency. Add the standard pattern:

```csharp
try
{
    // ... handler body ...
    await unitOfWork.SaveChangesAsync(cancellationToken);
}
catch (ContentPlacesConcurrencyException ex)
{
    logger.LogWarning(ex, "Concurrency conflict on ...");
    return Result.Failure(
        new Error("BusinessAmenity.ConcurrencyConflict", "Modified by another request. Retry."),
        Outcome.Conflict);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    return Result.Failure(new Error("Request.Cancelled", "Request was cancelled."), Outcome.Canceled);
}
```

---

### 🟡 Fix 10: Error codes don't follow `{Entity}.{Reason}` convention

| Current (wrong) | Correct |
|-----------------|---------|
| `Auth.Unauthorized` | `Business.Unauthorized` or use `Error.Unauthorized(msg)` |
| `Auth.Forbidden` | `Business.Forbidden` or use `Error.Forbidden(msg)` |
| `Business.NotFound` inside BusinessAmenity handler | `BusinessAmenity.BusinessNotFound` |

---

### 🟡 Fix 11: `GetAccessibilityFeaturesQuery` placed in wrong folder

**Current**: `Commands/AccessibilityFeature/GetAccessibilityFeatures/`
**Should be**: `Queries/AccessibilityFeature/GetAccessibilityFeatures/`

Move the query and handler files to the correct location. Queries go in `Queries/`, commands in `Commands/`.

---

### 🟡 Fix 12: `AccessibilityFeatureDto` missing `Guid Id`

Every DTO in this codebase exposes `Guid Id` as the first property so clients can reference records.
Add it to the DTO and the `From()` mapper:

```csharp
public sealed record AccessibilityFeatureDto(
    Guid Id,                        // ← ADD
    AccessibilityFeatureType FeatureType,
    string Name,
    string? Description,
    bool IsAvailable)
{
    public static AccessibilityFeatureDto From(AccessibilityFeatureEntity feature) => new(
        feature.Id,                 // ← ADD
        feature.FeatureType, ...);
}
```

---

### 🟠 Fix 13: `ListBusinessAmenitiesQuery` endpoint doesn't bind `Page`/`PageSize`

**File**: `ContentPlaces.Presentation/Endpoints/BusinessAmenity/BusinessAmenityEndpoints.cs`

```csharp
// Current — Page/PageSize never bound from request:
var result = await sender.Send(new ListBusinessAmenitiesQuery(id), ct);

// Fix:
amenities.MapGet("/{id:guid}/amenities", async (
    Guid id, ISender sender, CancellationToken ct,
    int page = 1, int pageSize = 20) =>        // ← ADD
{
    var result = await sender.Send(new ListBusinessAmenitiesQuery(id, page, pageSize), ct);
```

Also add `PageSize` range validation to `ListBusinessAmenitiesQueryValidator`.

---

### 🟠 Fix 14: `BusinessAmenity.Create()` and `BusinessStaff.Create()` — manually set `CreatedAt`

EF's `SaveChanges` interceptor (via `AuditableEntityInterceptor` in SharedKernel) manages `CreatedAt`.
Setting it manually in the factory is redundant and creates two sources of truth.

```csharp
// Remove this line from BusinessAmenity.Create() and BusinessStaff.Create():
CreatedAt = DateTime.UtcNow,  // ← DELETE — managed by EF interceptor
```

---

### 🟠 Fix 15: Arabic comments in production code

Remove all Arabic comments. Production code comments must be in English:
- `// بيمنع duplicate deactivation` → `// Prevent double-deactivation`
- `// ترقيم الصفحات` → (delete — the code is self-explanatory)
- `// polymorphic: Place حالياً بس` → `// Polymorphic: EntityType = 1 = Place. Extend for Business, Tour, etc.`

---

## Ezz — Tasks 4+7 (ServiceItem + Geo-Search) — Full Implementation Required

**Status**: 0% complete. Only a partial domain entity shell exists.

### What Needs to Be Built

#### Task 4: ServiceItem (5 endpoints)

**1. Complete `ServiceItem.cs` domain entity** — add all factory/business methods:
```csharp
// ContentPlaces.Domain/Entities/ServiceItem.cs — ADD:

// Remove duplicate field — `PriceCurrency` and `Currency` are the same concept, keep `Currency`
// Delete: public string PriceCurrency { get; private set; } = "JOD";

public static ServiceItem Create(
    Guid businessId, string name, decimal price, string currency,
    ServiceCategory category, int durationMinutes, int maxCapacity,
    string? description = null, int sortOrder = 0)
{
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
        SortOrder = sortOrder
    };
}

public void Update(string name, decimal price, string currency,
    ServiceCategory category, int durationMinutes, int maxCapacity,
    string? description, int sortOrder)
{
    Name = name.Trim();
    Description = description?.Trim();
    Price = price;
    Currency = currency.ToUpperInvariant();
    Category = category;
    DurationMinutes = durationMinutes;
    MaxCapacity = maxCapacity;
    SortOrder = sortOrder;
    MarkUpdated();
}

public void SetAvailability(bool isAvailable) { IsAvailable = isAvailable; MarkUpdated(); }

public void ApplyDiscount(decimal percent, DateTime? validFrom, DateTime? validTo)
{
    DiscountPercent = percent;
    SalePrice = Math.Round(Price * (1 - percent / 100), 2);
    DiscountValidFrom = validFrom;
    DiscountValidTo = validTo;
    MarkUpdated();
}

public void RemoveDiscount() { DiscountPercent = null; SalePrice = null; MarkUpdated(); }
```

**2. Create `IServiceItemRepository`** in `ContentPlaces.Domain/Repositories/`:
```csharp
public interface IServiceItemRepository : IRepository<ServiceItem, Guid>
{
    Task<bool> BusinessExistsAsync(Guid businessId, CancellationToken ct = default);
}
```

**3. Build Application layer** — 3 commands (Create, Update, Delete) + 2 queries (List, GetById):
- Each command handler: `IServiceItemRepository`, `IContentPlacesUnitOfWork`, `ICurrentUser`, `HybridCache`, `ILogger`
- IDOR check on all writes: load business by `BusinessId`, verify `OwnerId == currentUser.UserId || isAdmin`
- `ServiceItemSummaryDto` (compact: Id, Name, Price, Currency, DurationMinutes, IsAvailable, SortOrder)
- `ServiceItemDetailDto` (full: all fields including discount)
- Both query records implement `ICacheableQuery`
- Cache keys: `cp:biz:{businessId}:services:...` for list, `cp:service:{id}` for detail

**4. Create `ServiceItemRepository`** in `ContentPlaces.Infrastructure/Repositories/`:
```csharp
internal sealed class ServiceItemRepository(ContentPlacesDbContext context)
    : EfRepository<ServiceItem, Guid>(context), IServiceItemRepository
{
    public Task<bool> BusinessExistsAsync(Guid businessId, CancellationToken ct = default)
        => context.Businesses.AnyAsync(b => b.Id == businessId, ct);
}
```

**5. Register in `DependencyInjection.cs`**:
```csharp
services.AddScoped<IServiceItemRepository, ServiceItemRepository>();
```

**6. Create integration events in `ContentPlaces.Contracts/IntegrationEvents/`**:
```csharp
public sealed record ServiceItemCreatedIntegrationEvent(
    Guid ServiceItemId, Guid BusinessId, string Name, decimal Price, string Currency) : IntegrationEventBase;

public sealed record ServiceItemDeletedIntegrationEvent(
    Guid ServiceItemId, Guid BusinessId) : IntegrationEventBase;
```
Publish from `CreateServiceItemCommandHandler` and `DeleteServiceItemCommandHandler` after save:
```csharp
dbContext.OutboxMessages.Add(OutboxMessage.Create(new ServiceItemCreatedIntegrationEvent(...)));
```

**7. Wire 5 endpoints** — routes per spec:
- `GET /places/businesses/{id}/services` → `ListServiceItems` (Anonymous, `IsAvailable=true` for public)
- `GET /places/businesses/services/{id}` → `GetServiceItemById` (Anonymous)
- `POST /places/businesses/{id}/services` → `CreateServiceItem` (`RequireAuthorization()`)
- `PUT /places/businesses/services/{id}` → `UpdateServiceItem` (`RequireAuthorization()`)
- `DELETE /places/businesses/services/{id}` → `DeleteServiceItem` (`RequireAuthorization()`)

---

#### Task 7: Geo-Search (2 endpoints)

No domain or infrastructure changes needed. Application layer only.

**1. `GetNearbyPlacesQuery`** — Haversine in raw SQL:
```csharp
public sealed record GetNearbyPlacesQuery(
    decimal Latitude, decimal Longitude, double RadiusKm,
    int Page = 1, int PageSize = 20)
    : IQuery<IReadOnlyList<NearbyPlaceSummaryDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.NearbyPlaces(Latitude, Longitude, RadiusKm, Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);  // short TTL — location-sensitive
    public IReadOnlyList<string> Tags => ["places"];
}
```

Handler uses `FromSqlInterpolated` for Haversine. Inject `ContentPlacesDbContext` directly (read-only query):
```sql
SELECT Id, Name, Slug, Latitude, Longitude, AverageRating, IsFeatured,
       (6371 * ACOS(COS(RADIANS(@lat)) * COS(RADIANS(Latitude))
            * COS(RADIANS(Longitude) - RADIANS(@lng))
            + SIN(RADIANS(@lat)) * SIN(RADIANS(Latitude)))) AS DistanceKm
FROM content_places.Places
WHERE IsDeleted = 0
  AND Latitude != 0.0 AND Longitude != 0.0
HAVING DistanceKm <= @radiusKm
ORDER BY DistanceKm
```

**2. `GetMapViewportQuery`** — LINQ bounding box:
```csharp
var pins = await dbContext.Places
    .AsNoTracking()
    .Where(p => !p.IsDeleted
             && p.Location.Latitude != 0 && p.Location.Longitude != 0
             && p.Location.Latitude  >= request.SouthLat
             && p.Location.Latitude  <= request.NorthLat
             && p.Location.Longitude >= request.WestLng
             && p.Location.Longitude <= request.EastLng)
    .Select(p => new MapPinDto(p.Id, p.Name,
        p.Location.Latitude, p.Location.Longitude,
        p.AverageRating))
    .ToListAsync(cancellationToken);
```

**3. DTOs**:
```csharp
public sealed record NearbyPlaceSummaryDto(Guid Id, string Name, string Slug,
    decimal Latitude, decimal Longitude, decimal AverageRating, double DistanceKm);

public sealed record MapPinDto(Guid Id, string Name,
    decimal Latitude, decimal Longitude, decimal AverageRating);

public sealed record MapViewportResponse(
    IReadOnlyList<MapPinDto> Pins, bool IsClusteringRecommended);
```

**4. Wire 2 endpoints**:
- `GET /places/nearby` — Anonymous
- `GET /places/map/viewport` — Anonymous

---

## Fix Priority Summary

| # | Owner | Severity | Fix |
|---|-------|----------|-----|
| 1 | Mahmoud | 🔴 Critical | `DeletePlace` always blocks — fix `HasActiveLinkedBusinessesAsync` |
| 2 | Mahmoud | 🔴 Critical | `CreatePlace` slug checks wrong variable |
| 3 | Mahmoud + Mohammad | 🔴 Critical | Domain events never dispatch — use `IUnitOfWork<ContentPlacesDbContext>` |
| 4 | Mahmoud + Mohammad | 🔴 Critical | Create Business integration event records in Contracts |
| 5 | Mahmoud + Mohammad | 🔴 Critical | Domain event handlers must write outbox rows for integration events |
| 6 | Fadwa | 🔴 Critical | `RemoveAmenity` — no IDOR check |
| 7 | Fadwa | 🔴 Critical | `RemoveStaff` — no auth check |
| 8 | Fadwa | 🔴 Critical | `UpdateAccessibility` — no auth check |
| 9 | Fadwa | 🔴 Critical | `ListStaff` endpoint must be `RequireAuthorization()` not anonymous |
| 10 | Fadwa | 🔴 Critical | Double DB query in `UpdateAccessibilityFeatures` |
| 11 | Fadwa | 🔴 Critical | Missing `BusinessStaff` integration events in Contracts |
| 12 | Ezz | 🔴 Critical | Complete `ServiceItem` domain entity factory/business methods |
| 13 | Ezz | 🔴 Critical | Build all ServiceItem CQRS (5 endpoints) from scratch |
| 14 | Ezz | 🔴 Critical | Build Geo-Search queries (2 endpoints) from scratch |
| 15 | Fadwa | 🟡 Medium | Add try/catch to all 5 handlers |
| 16 | All | 🟡 Medium | Add `ICacheableQuery` to all uncached query records |
| 17 | All | 🟡 Medium | Add `HybridCache` invalidation to all uncached command handlers |
| 18 | Mahmoud | 🟡 Medium | `ListPlaces` — `CategoryId` and `HasActiveTours` filters ignored |
| 19 | Fadwa | 🟡 Medium | Fix error codes to `{Entity}.{Reason}` pattern |
| 20 | Fadwa | 🟡 Medium | Move `GetAccessibilityFeaturesQuery` to `Queries/` folder |
| 21 | Fadwa | 🟡 Medium | Add `Guid Id` to `AccessibilityFeatureDto` |
| 22 | Fadwa | 🟠 Minor | Bind `Page`/`PageSize` in `ListAmenities` endpoint |
| 23 | Fadwa | 🟠 Minor | Remove manual `CreatedAt = DateTime.UtcNow` from factory methods |
| 24 | Fadwa | 🟠 Minor | Replace Arabic comments with English |
| 25 | Mahmoud | 🟠 Minor | Fix old-style namespaces in `UpdatePlaceResult`, `PlaceTranslationDto` |
