# ContentPlaces — Remaining Fix Plan (Mahmoud + Mohammad + Ezz)

> **Scope**: 21 items across 3 developers. Mahmoud 1, Mohammad 3, Ezz 13, + 4 foundation/cleanup.
> **Status at planning**: ContentCore ✅ 100% done. ContentPlaces 9/38 done.
> **Build state**: 0 errors. Must stay 0 errors after each phase.
> **Architecture rule**: `ContentPlaces.Application` does NOT reference `ContentPlaces.Infrastructure` (agent-context.md §1.3 + gotcha #22). All Infrastructure dependencies go through interfaces declared in Application/Domain.

---

## Phase 0 — Foundation (Shared)

### 0.1 `IContentPlacesOutboxWriter` abstraction

**Why**: ServiceItem and BusinessStaff are non-aggregates. They cannot raise domain events (UoW only collects events from `IAggregateRoot`). Fix doc says inject `ContentPlacesDbContext` into handlers — this violates gotcha #22. Solution: expose outbox writes through an Application-layer interface implemented in Infrastructure.

**Files to create**:

```
ContentPlaces.Application/Interfaces/IContentPlacesOutboxWriter.cs
ContentPlaces.Infrastructure/Persistence/ContentPlacesOutboxWriter.cs
```

**`IContentPlacesOutboxWriter.cs`** (Application):
```csharp
using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Application.Interfaces;

/// <summary>
/// Writes integration events to the ContentPlaces outbox.
/// Non-aggregate command handlers use this to publish events durably.
/// The row is staged on the DbContext — commits atomically when IContentPlacesUnitOfWork.SaveChangesAsync runs.
/// Do NOT call SaveChangesAsync here; the UoW handles it.
/// </summary>
public interface IContentPlacesOutboxWriter
{
    void Enqueue(IIntegrationEvent integrationEvent);
}
```

**`ContentPlacesOutboxWriter.cs`** (Infrastructure):
```csharp
using ContentPlaces.Application.Interfaces;
using YallaJo.SharedKernel.Domain.Event;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentPlaces.Infrastructure.Persistence;

internal sealed class ContentPlacesOutboxWriter(ContentPlacesDbContext dbContext) : IContentPlacesOutboxWriter
{
    public void Enqueue(IIntegrationEvent integrationEvent)
        => dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
}
```

**DI registration** in `ContentPlaces.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<IContentPlacesOutboxWriter, ContentPlacesOutboxWriter>();
```

**Verify**: `dotnet build ContentPlaces.sln` returns 0 errors.

---

## Phase 1 — Mahmoud (1 item)

### 1.1 `HasActiveTours` filter on `ListPlacesQuery`

**Fix ID**: Mahmoud Fix 7 (remainder — `CategoryId` already done)

**Files to modify**:
- `ContentPlaces.Application/Queries/Place/ListPlaces/ListPlacesQuery.cs`
- `ContentPlaces.Application/Queries/Place/ListPlaces/ListPlacesQueryHandler.cs`
- `ContentPlaces.Domain/Specifications/PlaceFilterSpecification.cs`
- `ContentPlaces.Presentation/Endpoints/Place/PlaceEndpoints.cs` (bind from query string)
- `ContentPlaces.Application/Caching/ContentPlacesCacheKeys.cs` (cache key includes HasActiveTours)

**Steps**:
1. Add `bool? HasActiveTours` to `ListPlacesQuery` record signature
2. Pass through to `PlaceFilterSpecification` constructor
3. In spec: `WhereIf(hasActiveTours == true, p => p.TourCount > 0)`
4. Update `ContentPlacesCacheKeys.PlaceList` signature + format string
5. Bind `[AsParameters]` or explicit query-string param in endpoint
6. Add validator rule (optional: no range needed, bool nullable)

**Verify**:
- Build passes
- Call `GET /api/v1/places?hasActiveTours=true` returns only places where `TourCount > 0`
- Call without param returns all places (backward compatible)

---

## Phase 2 — Mohammad (3 items)

### 2.1 Create 5 Business integration event records

**Fix ID**: Mohammad Fix 3

**Location**: `ContentPlaces.Contracts/IntegrationEvents/`

**Files to create** (5 new):
```
BusinessCreatedIntegrationEvent.cs
BusinessApprovedIntegrationEvent.cs
BusinessRejectedIntegrationEvent.cs
BusinessSuspendedIntegrationEvent.cs
BusinessReinstatedIntegrationEvent.cs
```

**Template** (use file-scoped namespace, `IntegrationEventBase`, no cargo usings):
```csharp
using YallaJo.SharedKernel.Domain.Event;

namespace ContentPlaces.Contracts.IntegrationEvents;

public sealed record BusinessCreatedIntegrationEvent(
    Guid BusinessId,
    string Name,
    string Slug,
    Guid OwnerId,
    Guid? PlaceId) : IntegrationEventBase;
```

**Payload per event** (minimum required by consumers):

| Event | Fields |
|-------|--------|
| `BusinessCreatedIntegrationEvent` | `BusinessId, Name, Slug, OwnerId, PlaceId?` |
| `BusinessApprovedIntegrationEvent` | `BusinessId, OwnerId` |
| `BusinessRejectedIntegrationEvent` | `BusinessId, OwnerId, string Reason` |
| `BusinessSuspendedIntegrationEvent` | `BusinessId, OwnerId, string Reason` |
| `BusinessReinstatedIntegrationEvent` | `BusinessId, OwnerId` |

**Verify**: `grep` in `Contracts/IntegrationEvents/` finds all 5 files; build passes.

### 2.2 Create 5 Business domain event handlers

**Fix ID**: Mohammad Fix 2

**Location**: `ContentPlaces.Infrastructure/EventHandlers/`

**Files to create** (5 new):
```
BusinessCreatedDomainEventHandler.cs
BusinessApprovedDomainEventHandler.cs
BusinessRejectedDomainEventHandler.cs
BusinessSuspendedDomainEventHandler.cs
BusinessReinstatedDomainEventHandler.cs
```

**Pattern** (mirror `PlaceCreatedDomainEventHandler.cs`):
1. Inject `IBusinessRepository`, `ContentPlacesDbContext`, `ILogger<T>`, and for Created event also `IEntityTranslationOrchestrator`
2. Load Business aggregate to obtain `OwnerId` (domain events don't carry it)
3. If Created: run translation for Name + Description into all active languages (pattern from `PlaceCreatedDomainEventHandler.cs:45-80`)
4. Write outbox message via `dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent))`
5. **Never call `SaveChangesAsync`** — UoW handles it atomically

**Example — `BusinessApprovedDomainEventHandler.cs`**:
```csharp
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

        var business = await businessRepository.GetByIdAsync(evt.BusinessId, ct);
        if (business is null)
        {
            logger.LogWarning(
                "BusinessApprovedDomainEvent: Business {Id} not found.", evt.BusinessId);
            return;
        }

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new BusinessApprovedIntegrationEvent(evt.BusinessId, business.OwnerId)));

        logger.LogInformation(
            "BusinessApprovedDomainEvent: queued outbox for Business {Id}", evt.BusinessId);
    }
}
```

**Variation — `BusinessCreatedDomainEventHandler.cs`** also does translation:
```csharp
// Additional constructor param:
IEntityTranslationOrchestrator orchestrator,

// After loading business, before outbox write:
var fields = new Dictionary<string, string> { ["Name"] = business.Name };
if (!string.IsNullOrWhiteSpace(business.Description))
    fields["Description"] = business.Description;

var translationSets = await orchestrator.TranslateToAllActiveLanguagesAsync(
    fields, sourceLanguageCode: "en", ct);

foreach (var set in translationSets)
{
    if (business.BusinessTranslations.Any(t => t.LanguageId == set.LanguageId))
        continue;
    // ... (mirror PlaceCreatedDomainEventHandler logic)
}

dbContext.OutboxMessages.Add(OutboxMessage.Create(
    new BusinessCreatedIntegrationEvent(
        business.Id, business.Name, business.Slug, business.OwnerId, business.PlaceId)));
```

**Handler → Event mapping**:

| Domain event | Integration event published | Extra work |
|--------------|---|---|
| `BusinessCreatedDomainEvent` | `BusinessCreatedIntegrationEvent` | Translate Name + Description |
| `BusinessApprovedDomainEvent` | `BusinessApprovedIntegrationEvent` | Load for OwnerId |
| `BusinessRejectedDomainEvent` | `BusinessRejectedIntegrationEvent` | Load for OwnerId; carry Reason from domain event |
| `BusinessSuspendedDomainEvent` | `BusinessSuspendedIntegrationEvent` | Load for OwnerId; carry Reason |
| `BusinessReinstatedDomainEvent` | `BusinessReinstatedIntegrationEvent` | Load for OwnerId |

**Verify**:
- Build passes
- Create a business via integration test → `OutboxMessages` table has new row with `Type = "ContentPlaces.Contracts.IntegrationEvents.BusinessCreatedIntegrationEvent"`
- Approve → another row for `BusinessApprovedIntegrationEvent`
- MediatR auto-discovers handlers via `AddMediatR(...RegisterServicesFromAssembly...)` already in DI

### 2.3 Remove duplicate `IBusinessRepository` DI registration

**Fix ID**: Mohammad Fix 4

**File**: `ContentPlaces.Infrastructure/DependencyInjection.cs`

**Action**: Delete line 52 (`services.AddScoped<IBusinessRepository, BusinessRepository>();` — the second occurrence).

**Verify**:
```powershell
# grep count should be 1, not 2:
Select-String -Path "ContentPlaces.Infrastructure/DependencyInjection.cs" -Pattern "IBusinessRepository"
```

---

## Phase 3 — Ezz (13 items)

**Ordering rationale**: Entity first (affects migrations + all handlers) → Contracts → Infrastructure/DI → Application handlers → Queries → Endpoints → DTOs → Cleanup. Minimizes rework churn.

### 3.1 ServiceItem entity cleanup (combines Fix 1, 2, 3)

**Fix IDs**: Ezz Fix 1 + 2 + 3

**File**: `ContentPlaces.Domain/Entities/ServiceItem.cs`

**Changes**:
1. Remove `, IAggregateRoot` from class declaration (Fix 1)
2. Remove `public string PriceCurrency` property (Fix 3 — duplicate)
3. Remove `public string? SalePriceCurrency` property (Fix 3 — dead)
4. Update `Create()` signature to accept `ServiceCategory category` and `string? description` (Fix 2)
5. Update `Update()` signature same way (Fix 2)
6. Remove `PriceCurrency = currency.ToUpperInvariant()` from factory + update
7. Ensure `Description = description?.Trim()` in factory
8. Keep `Currency` as the single currency column

**Final entity (expected ~75 lines)**:
```csharp
using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class ServiceItem : AuditableEntity
{
    private ServiceItem() { } // EF Core

    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public ServiceCategory Category { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsAvailable { get; private set; }
    public int SortOrder { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? SalePrice { get; private set; }
    public DateTime? DiscountValidFrom { get; private set; }
    public DateTime? DiscountValidTo { get; private set; }

    public Business Business { get; private set; } = default!;

    public static ServiceItem Create(
        Guid businessId,
        string name,
        decimal price,
        string currency,
        ServiceCategory category,
        int durationMinutes,
        int maxCapacity,
        string? description = null,
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

    public void Update(
        string name,
        decimal price,
        string currency,
        ServiceCategory category,
        int durationMinutes,
        int maxCapacity,
        string? description = null,
        int sortOrder = 0)
    {
        if (price < 0) throw new ArgumentException("Price cannot be negative", nameof(price));
        if (durationMinutes <= 0) throw new ArgumentException("Duration must be positive", nameof(durationMinutes));
        if (maxCapacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(maxCapacity));

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

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        MarkUpdated();
    }
}
```

### 3.2 EF configuration + migration

**Files**:
- `ContentPlaces.Infrastructure/Persistence/Configurations/ServiceItemConfiguration.cs`
- Generate new migration via CLI

**Configuration changes**:
1. Remove `builder.Property(x => x.PriceCurrency)...` block
2. Remove `builder.Property(x => x.SalePriceCurrency)...` block
3. Keep `Currency` + `SalePrice` configurations

**Generate migration**:
```powershell
# From repo root
dotnet ef migrations add ServiceItem_RemoveDuplicateCurrency `
  --project ContentPlaces.Infrastructure `
  --startup-project YallaJo.Api `
  --context ContentPlacesDbContext
```

Review generated migration — should contain:
```csharp
migrationBuilder.DropColumn("PriceCurrency", "ServiceItems", "content_places");
migrationBuilder.DropColumn("SalePriceCurrency", "ServiceItems", "content_places");
```

If NULL constraints or data migration needed, add `UPDATE ServiceItems SET Currency = PriceCurrency WHERE Currency IS NULL OR Currency = 'JOD'` BEFORE the DropColumn calls. (Check current row count — if production empty, skip.)

**Verify**:
- `dotnet ef database update` succeeds
- Select query on `content_places.ServiceItems` no longer shows `PriceCurrency`/`SalePriceCurrency` columns

### 3.3 Rename `ServiceItemCreateIntegrationEvent` → `ServiceItemCreatedIntegrationEvent`

**Fix ID**: Ezz Fix 7

**Actions**:
1. Rename file `ContentPlaces.Contracts/IntegrationEvents/ServiceItemCreateIntegrationEvent.cs` → `ServiceItemCreatedIntegrationEvent.cs`
2. Rewrite contents with file-scoped namespace + correct name:
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
3. Update references: `CreateServiceItemCommandHandler.cs` — change `ServiceItemCreateIntegrationEvent` → `ServiceItemCreatedIntegrationEvent` (will be rewritten in 3.5)

**Verify**: grep for `ServiceItemCreate` should return zero matches (excluding namespace `CreateServiceItem` folder).

### 3.4 Fix `CreateServiceItemResult.cs` namespace

**Fix ID**: Ezz Fix 8

**File**: `ContentPlaces.Application/Commands/ServiceItem/CreateServiceItem/CreateServiceItemResult.cs`

**Replace entire content with**:
```csharp
namespace ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;

public sealed record CreateServiceItemResult(Guid ServiceItemId, string Name);
```

Remove 5 cargo usings + brace namespace.

### 3.5 Rewrite 3 ServiceItem write handlers

**Fix IDs**: Ezz Fix 4 + 5

**Common changes across Create/Update/Delete handlers**:
- Remove `IPublisher publisher` injection
- Add `IContentPlacesOutboxWriter outbox` injection
- Add `ICurrentUser currentUser` injection (Application has `using YallaJo.SharedKernel.Application.Abstractions.Context`)
- Add `IBusinessRepository businessRepository` injection
- Add `HybridCache cache` injection (for Fix 9 invalidation later)

**Handler preamble (uniform across all 3)**:
```csharp
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure(Error.Unauthorized("Authentication required."), Outcome.Unauthorized);
```

**`CreateServiceItemCommandHandler.cs`** — updates needed:

```csharp
// Updated constructor:
public sealed class CreateServiceItemCommandHandler(
    IServiceItemRepository serviceItemRepository,
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    IContentPlacesOutboxWriter outbox,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<CreateServiceItemCommandHandler> logger)
    : ICommandHandler<CreateServiceItemCommand, CreateServiceItemResult>
```

**Inside Handle (after auth check)**:
```csharp
// IDOR: verify the caller owns the business (or is admin)
var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
if (business is null)
    return Result<CreateServiceItemResult>.Failure(
        new Error("Business.NotFound", "Business not found"), Outcome.NotFound);

var isAdmin = currentUser.IsInRole("Admin");
if (!isAdmin && business.OwnerId != currentUser.UserId!.Value)
    return Result<CreateServiceItemResult>.Failure(
        Error.Forbidden("You do not own this business."), Outcome.Forbidden);

// ... duplicate-name check ...

var item = ServiceItemEntity.Create(
    businessId:      request.BusinessId,
    name:            request.Name,
    price:           request.Price,
    currency:        request.Currency,
    category:        request.Category,       // ← NEW
    durationMinutes: request.DurationMinutes,
    maxCapacity:     request.MaxCapacity,
    description:     request.Description,    // ← NEW
    sortOrder:       request.SortOrder);

await serviceItemRepository.AddAsync(item, cancellationToken);

// Enqueue outbox BEFORE save — commits atomically with the item row
outbox.Enqueue(new ServiceItemCreatedIntegrationEvent(
    item.Id, item.BusinessId, item.Name, item.Price, item.Currency));

try
{
    await unitOfWork.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    return Result<CreateServiceItemResult>.Failure(
        new Error("ServiceItem.ConcurrencyConflict",
            "A concurrency conflict occurred. Please refresh and try again."),
        Outcome.Conflict);
}

// Fix 9: cache invalidation AFTER successful save
await cache.RemoveByTagAsync($"biz:{request.BusinessId}:services", cancellationToken);

logger.LogInformation(
    "ServiceItem {ServiceItemId} created for Business {BusinessId}",
    item.Id, request.BusinessId);

return Result<CreateServiceItemResult>.Created(
    new CreateServiceItemResult(item.Id, item.Name));
```

**Update `CreateServiceItemCommand`** to add `Category` + `Description`:
```csharp
public sealed record CreateServiceItemCommand(
    Guid BusinessId,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    ServiceCategory Category,           // ← NEW
    string? Description = null,         // ← NEW
    int SortOrder = 0)
    : ICommand<CreateServiceItemResult>;
```

**Update `CreateServiceItemCommandValidator`**:
```csharp
RuleFor(x => x.Category).IsInEnum();
RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
```

**`UpdateServiceItemCommandHandler.cs`** — same pattern:
- Load service item → check null
- **New**: load `business = await businessRepository.GetByIdAsync(item.BusinessId, ct)` → IDOR check
- Call `item.Update(name, price, currency, category, durationMinutes, maxCapacity, description, sortOrder)` — new signature
- After save: `cache.RemoveByTagAsync($"service:{request.Id}", ct)` + `cache.RemoveByTagAsync($"biz:{item.BusinessId}:services", ct)`

Update `UpdateServiceItemCommand` record to include `Category` + `Description`.

**`DeleteServiceItemCommandHandler.cs`** — replace `IPublisher.Publish` with outbox:
```csharp
item.SoftDelete();

outbox.Enqueue(new ServiceItemDeletedIntegrationEvent(item.Id, item.BusinessId));

await unitOfWork.SaveChangesAsync(cancellationToken);

await cache.RemoveByTagAsync($"service:{request.Id}", cancellationToken);
await cache.RemoveByTagAsync($"biz:{item.BusinessId}:services", cancellationToken);
```

Also add IDOR: load business from `item.BusinessId`, compare `OwnerId`.

### 3.6 `ListServiceItems` availability filter + owner visibility

**Fix ID**: Ezz Fix 6

**File**: `ContentPlaces.Application/Queries/ServiceItem/ListServiceItems/ListServiceItemsQueryHandler.cs`

**Changes**:
1. Inject `IBusinessRepository` and `ICurrentUser`
2. Load the business → determine if caller is owner/admin
3. Apply different filter based on role:

```csharp
var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
var isAdmin = currentUser.IsInRole("Admin");
var isOwner = business is not null
              && currentUser.UserId.HasValue
              && business.OwnerId == currentUser.UserId.Value;

Expression<Func<Domain.Entities.ServiceItem, bool>> filter = (isAdmin || isOwner)
    ? x => x.BusinessId == request.BusinessId
    : x => x.BusinessId == request.BusinessId && x.IsAvailable;

var items = await serviceItemRepository.SelectAsync(
    selector: x => ServiceItemDto.From(x),
    filter: filter,
    orderBy: q => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name),
    ct: cancellationToken);
```

### 3.7 ServiceItem queries → `ICacheableQuery`

**Fix ID**: Ezz Fix 9

**Files**:
- `ContentPlaces.Application/Caching/ContentPlacesCacheKeys.cs` — add helpers
- `ContentPlaces.Application/Queries/ServiceItem/ListServiceItems/ListServiceItemsQuery.cs`
- `ContentPlaces.Application/Queries/ServiceItem/GetServiceItemById/GetServiceItemByIdQuery.cs`

**`ContentPlacesCacheKeys.cs` additions**:
```csharp
public static string ServiceItemList(Guid businessId) => $"cp:biz:{businessId}:services";
public static string ServiceItem(Guid id) => $"cp:service:{id}";
```

**`ListServiceItemsQuery.cs`**:
```csharp
using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;

public sealed record ListServiceItemsQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<ServiceItemDto>>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.ServiceItemList(BusinessId);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["businesses", $"biz:{BusinessId}:services"];
}
```

**`GetServiceItemByIdQuery.cs`** — similar, with tag `$"service:{ServiceItemId}"`.

> **⚠️ Caveat**: `ListServiceItems` now returns different data based on caller role (owner sees unavailable items). Cache key **must** vary by role. Either:
> - Add `bool IsAdmin` / `Guid? UserId` to cache key (safer), OR
> - Split into two queries: `ListServiceItemsPublicQuery` + `ListServiceItemsForOwnerQuery`
>
> **Recommended**: add owner/admin hash to cache key:
> ```csharp
> public string CacheKey => $"cp:biz:{BusinessId}:services:role:{(IsAdminContext ? "admin" : UserId ?? "public")}";
> ```
> This requires passing caller identity into the query record at endpoint level. Alternative: skip caching for owner/admin views entirely (return `null` `CacheDuration` when called with elevated context — but `ICacheableQuery` doesn't support conditional caching out-of-box).
>
> **Simplest compliant approach**: only cache the public view; owner/admin bypasses cache. Implement by splitting query into two records — public cached, owner uncached.

### 3.8 Geo queries → `ICacheableQuery`

**Fix ID**: Ezz Fix 10

**Files**:
- `ContentPlaces.Application/Caching/ContentPlacesCacheKeys.cs` — add helpers
- `ContentPlaces.Application/Queries/Place/GetNearbyPlaces/GetNearbyPlacesQuery.cs`
- `ContentPlaces.Application/Queries/Place/GetMapViewport/GetMapViewportQuery.cs`

**Cache key helpers** (round coordinates to 4 decimals — ~11m precision, enough for cache reuse):
```csharp
public static string NearbyPlaces(double lat, double lng, double radiusKm, int pageSize)
    => $"cp:places:nearby:lat{lat:F4}:lng{lng:F4}:r{radiusKm}:s{pageSize}";

public static string MapViewport(double n, double s, double e, double w)
    => $"cp:places:viewport:n{n:F4}:s{s:F4}:e{e:F4}:w{w:F4}";
```

**Apply `ICacheableQuery`**: 2 min TTL (geo data changes frequently), tags `["places"]`.

### 3.9 `GetNearbyPlacesQueryHandler` — raw SQL Haversine

**Fix ID**: Ezz Fix 11

**File**: `ContentPlaces.Application/Queries/Place/GetNearbyPlaces/GetNearbyPlacesQueryHandler.cs`

**Issue**: Application layer cannot reference `ContentPlacesDbContext` (gotcha #22). Options:

**Option A — extend `IPlaceRepository`** (clean, recommended):
```csharp
// In ContentPlaces.Domain/Repositories/IPlaceRepository.cs:
Task<IReadOnlyList<NearbyPlaceResult>> GetNearbyAsync(
    double lat, double lng, double radiusKm, int pageSize, CancellationToken ct);

// In ContentPlaces.Infrastructure/Repositories/PlaceRepository.cs:
public async Task<IReadOnlyList<NearbyPlaceResult>> GetNearbyAsync(
    double lat, double lng, double radiusKm, int pageSize, CancellationToken ct)
{
    return await context.Database
        .SqlQuery<NearbyPlaceResult>($"""
            SELECT Id, Name, Slug, Latitude, Longitude, AverageRating, PlaceType,
                   6371 * ACOS(
                       COS(RADIANS({lat})) * COS(RADIANS(Latitude))
                       * COS(RADIANS(Longitude) - RADIANS({lng}))
                       + SIN(RADIANS({lat})) * SIN(RADIANS(Latitude))
                   ) AS DistanceKm
            FROM content_places.Places
            WHERE IsDeleted = 0
              AND Latitude != 0.0
              AND Longitude != 0.0
              AND 6371 * ACOS(
                      COS(RADIANS({lat})) * COS(RADIANS(Latitude))
                      * COS(RADIANS(Longitude) - RADIANS({lng}))
                      + SIN(RADIANS({lat})) * SIN(RADIANS(Latitude))
                  ) <= {radiusKm}
            ORDER BY DistanceKm
            OFFSET 0 ROWS FETCH NEXT {pageSize} ROWS ONLY
            """)
        .AsNoTracking()
        .ToListAsync(ct);
}

// NearbyPlaceResult lives in Domain.Queries or Application.Queries — plain DTO:
public sealed record NearbyPlaceResult(
    Guid Id, string Name, string Slug,
    decimal Latitude, decimal Longitude,
    decimal AverageRating, int PlaceType,
    double DistanceKm);
```

Then `GetNearbyPlacesQueryHandler` becomes:
```csharp
var results = await placeRepository.GetNearbyAsync(
    request.Lat, request.Lng, request.RadiusKm, request.PageSize, cancellationToken);

return Result<IReadOnlyList<NearbyPlaceSummaryDto>>.Success(
    results.Select(r => new NearbyPlaceSummaryDto(
        r.Id, r.Name, r.Slug, r.Latitude, r.Longitude, r.AverageRating, r.DistanceKm))
    .ToList());
```

**Option B — keep in-memory Haversine as TODO**:
Add comment `// TODO(perf): migrate to SQL Haversine once dataset > 10k places` on top of the existing `CalculateDistance` code. Acceptable short-term.

**Recommended**: Option A — uses existing repository pattern, no arch violation.

### 3.10 `NearbyPlaceSummaryDto` correct shape

**Fix ID**: Ezz Fix 12

**File**: `ContentPlaces.Application/Queries/Place/Common/NearbyPlaceSummaryDto.cs`

**Replace**:
```csharp
namespace ContentPlaces.Application.Queries.Place.Common;

public sealed record NearbyPlaceSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Latitude,
    decimal Longitude,
    decimal AverageRating,
    double DistanceKm);
```

Update all callers (`GetNearbyPlacesQueryHandler`) to pass the new fields from repository result.

### 3.11 ServiceItem route split

**Fix ID**: Ezz Fix 13

**File**: `ContentPlaces.Presentation/Endpoints/ServiceItem/ServiceItemEndpoints.cs`

**Current** (all under `/places/businesses/{businessId:guid}/services`):
```
GET    /places/businesses/{businessId}/services                      ✓ correct
GET    /places/businesses/{businessId}/services/{serviceItemId}      ✗ should be /services/{id}
POST   /places/businesses/{businessId}/services                      ✓ correct
PUT    /places/businesses/{businessId}/services/{serviceItemId}      ✗ should be /services/{id}
DELETE /places/businesses/{businessId}/services/{serviceItemId}      ✗ should be /services/{id}
```

**Split into two groups**:
```csharp
// Group 1: business-scoped
var bizServices = group.MapGroup("/places/businesses")
    .WithTags("ContentPlaces | ServiceItems");

bizServices.MapGet("/{businessId:guid}/services", ...);   // List
bizServices.MapPost("/{businessId:guid}/services", ...);  // Create

// Group 2: item-level
var itemServices = group.MapGroup("/places/businesses/services")
    .WithTags("ContentPlaces | ServiceItems");

itemServices.MapGet("/{id:guid}", ...);    // Get by Id
itemServices.MapPut("/{id:guid}", ...);    // Update
itemServices.MapDelete("/{id:guid}", ...); // Delete
```

**Commands update**: remove `BusinessId` from `UpdateServiceItemCommand` + `DeleteServiceItemCommand` record signatures since URL no longer contains it. Handler loads item by Id, uses `item.BusinessId` for the business lookup.

**Query update**: `GetServiceItemByIdQuery(Guid BusinessId, Guid ServiceItemId)` → `GetServiceItemByIdQuery(Guid ServiceItemId)`.

---

## Phase 4 — Final Build + Verification

### 4.1 Build + test gate

```powershell
dotnet build YallaJo.sln
# must return: Build succeeded. 0 Error(s)

dotnet test YallaJo.sln
# must return: Passed: 171 (current count)
```

If tests break, the most likely cause is new command signatures (Category, Description fields). Update test fixtures to match.

### 4.2 Integration smoke (optional but recommended)

Manual API calls with `curl` or Postman:

1. `POST /api/v1/content-places/places` → 201, check `OutboxMessages` for `PlaceCreatedIntegrationEvent`
2. `POST /api/v1/content-places/businesses` → 201, check outbox for `BusinessCreatedIntegrationEvent` ✅ NEW
3. `PATCH /api/v1/content-places/businesses/{id}/approve` → 204, check outbox for `BusinessApprovedIntegrationEvent` ✅ NEW
4. `POST /api/v1/content-places/places/businesses/{bid}/services` with `category` + `description` fields → 201, check outbox for `ServiceItemCreatedIntegrationEvent` ✅ NEW
5. `GET /api/v1/content-places/places?hasActiveTours=true` → filtered list ✅ NEW
6. `DELETE /api/v1/content-places/places/businesses/services/{id}` → 204 (new route)
7. `GET /api/v1/content-places/places/nearby?lat=31.95&lng=35.92&radiusKm=5` → results include `slug`, `latitude`, `longitude` ✅ NEW

### 4.3 DB column verification

```sql
-- Should NOT return PriceCurrency or SalePriceCurrency columns:
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'content_places' AND TABLE_NAME = 'ServiceItems';
```

---

## Ordering / Dependency Graph

```
Phase 0 (Foundation)
  └─ IContentPlacesOutboxWriter
        │
        ├─────────────────────────────────────┐
        ▼                                     ▼
  Phase 1 (Mahmoud)                    Phase 3.5 (Ezz handlers)
  └─ HasActiveTours filter
                                              ▲
  Phase 2 (Mohammad) ─── INDEPENDENT ────────┘ (no ordering dep)
  ├─ 2.1 Contracts events (5 files)
  ├─ 2.2 Domain event handlers (5 files) ──── depends on 2.1
  └─ 2.3 DI dedup

  Phase 3 (Ezz) ─── sequential:
  3.1 Entity ──► 3.2 Migration ──► 3.3 Rename event ──► 3.4 Result cleanup
                                                            │
                                                            ▼
  3.5 Handlers (use outbox from 0.1) ──► 3.6 List filter ──► 3.7 Cacheable
                                                                │
                                                                ▼
                                  3.8 Geo cache ──► 3.9 Raw SQL ──► 3.10 DTO
                                                                         │
                                                                         ▼
                                                            3.11 Route split

  Phase 4 (Gate) ─── after all above pass
```

---

## Risk Register

| # | Risk | Mitigation |
|---|------|------------|
| R1 | EF migration drops `PriceCurrency` data | Check row count first; add `UPDATE SET Currency = PriceCurrency WHERE Currency IS NULL` before DropColumn if needed |
| R2 | Renaming integration event breaks consumers | No consumers exist yet for `ServiceItemCreate...` (outbox never actually publishing). Safe rename. |
| R3 | `CreateServiceItemCommand` signature change breaks API clients | Presentation `CreateServiceItemRequest` DTO needs update too. Frontend/API consumers need notification. |
| R4 | Split ServiceItem routes breaks clients | Mark old routes deprecated in commit note; frontend must migrate |
| R5 | Raw SQL Haversine hard-coded schema `content_places.Places` | If schema renamed later, SQL breaks. Add comment referencing migration. |
| R6 | `ListServiceItems` cache leaks owner-only data to public | Split into 2 queries OR vary cache key by role. Document chosen approach in code comment. |
| R7 | Business domain event handlers fire on existing data during startup | New handlers only run on NEW events. Existing approved businesses won't retroactively publish. Add a one-off data migration if backfill needed. |
| R8 | Route split: old `DELETE /businesses/{bid}/services/{sid}` still in clients | Keep old route as redirect / 308 Permanent Redirect for one release cycle |

---

## Commit Strategy

Break into **atomic commits** for reviewability:

| # | Commit message | Scope |
|---|----------------|-------|
| 1 | `feat(ContentPlaces): add IContentPlacesOutboxWriter abstraction` | Phase 0 |
| 2 | `feat(ContentPlaces): ListPlaces supports HasActiveTours filter` | Phase 1 |
| 3 | `feat(ContentPlaces): add 5 Business integration event records` | 2.1 |
| 4 | `feat(ContentPlaces): wire Business domain events to outbox` | 2.2 |
| 5 | `fix(ContentPlaces): remove duplicate IBusinessRepository registration` | 2.3 |
| 6 | `refactor(ContentPlaces): ServiceItem aggregate cleanup + migration` | 3.1 + 3.2 |
| 7 | `fix(ContentPlaces): rename ServiceItemCreateIntegrationEvent` | 3.3 |
| 8 | `refactor(ContentPlaces): clean CreateServiceItemResult namespace` | 3.4 |
| 9 | `fix(ContentPlaces): ServiceItem write handlers use outbox + IDOR checks` | 3.5 |
| 10 | `fix(ContentPlaces): ListServiceItems filters by IsAvailable for non-owners` | 3.6 |
| 11 | `feat(ContentPlaces): ServiceItem + geo queries implement ICacheableQuery` | 3.7 + 3.8 |
| 12 | `perf(ContentPlaces): GetNearbyPlaces uses SQL Haversine via repository` | 3.9 + 3.10 |
| 13 | `refactor(ContentPlaces): split ServiceItem routes per spec` | 3.11 |
| 14 | `chore: ContentPlaces fix plan complete — all 17 items resolved` | Final |

---

## Estimated Effort

| Phase | Developer | Items | Hours |
|-------|-----------|-------|-------|
| 0 | Any | 1 | 1 |
| 1 | Mahmoud | 1 | 0.5 |
| 2 | Mohammad | 3 (1 DI + 5 events + 5 handlers) | 4 |
| 3 | Ezz | 13 | 12 |
| 4 | Any | verification | 1 |
| **Total** | | **~21** | **~18.5h (~2.5 days)** |

---

## What's NOT in this plan

Explicitly excluded (Fadwa's 13 items — separate plan needed):
- `RemoveBusinessStaff` auth
- `ListBusinessStaff` endpoint auth + IDOR
- `UpdateAccessibilityFeatures` admin guard
- `BusinessStaffAdded`/`Removed` integration events
- 3 Fadwa-owned queries `ICacheableQuery`
- `AccessibilityFeatureDto` Id field
- `ListAmenities` Page/PageSize binding
- Remove manual `CreatedAt` from `BusinessAmenity`/`BusinessStaff` factories

Fadwa's fixes should be a second plan document — they share no files with Mahmoud/Mohammad/Ezz scope.

---

## Definition of Done

- [ ] `dotnet build` returns 0 errors
- [ ] `dotnet test` returns 171+ passed
- [ ] All 21 items verified by reading the file referenced in its "Evidence" column
- [ ] New migration applied to local DB without data loss
- [ ] Smoke test calls in §4.2 return expected outputs
- [ ] Git log shows 14 atomic commits with conventional commit messages
- [ ] `ContentPlaces-fixes-required.md` updated with ✅ status for fixed items
