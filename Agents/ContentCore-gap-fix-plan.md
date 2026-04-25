# Plan: Fix ContentCore Module Gaps (GAP-01 through GAP-15)

> **Scope**: 15 gaps surfaced by the 2026-04-23 ContentCore audit.
> **Why now**: 2 gaps (GAP-01, GAP-02) are live violations of written rules in `agent-context.md`. 3 gaps (GAP-03, GAP-04, GAP-05) are dead domain code or missing functionality. The rest are completeness/polish items.
> **Status at planning**: Build clean (0 errors, 183 tests). All 19 integration events in registry. ContentCore currently publishes 1 integration event (`LanguageActivatedIntegrationEvent`); this plan adds 1 more.
>
> **Related files**:
> - `Agents/agent-context.md` §2.1 (Rule 1 — endpoint auth) · §2.6 (Inbox/Outbox) · §3.3 (Domain Events)
> - `Agents/agent-context.md` gotcha #1, #14, #16, #20, #23, #28
> - `Agents/agent-context.md` §8.2 (Endpoint Authorization Violations — 28 endpoints total, ContentCore accounts for 9)
> - `Agents/ContentCore-fixes-required.md` (2026-04-17 audit — 11 bugs fixed; this plan adds items missed by that pass)
> - `Agents/error-log.md` (for new gotcha entries after completion)

---

## 1. Gap Inventory (15 items)

| # | Gap | Severity | Rule violated |
|---|-----|----------|---------------|
| 01 | ILogger missing in 4 handlers (CreateTag, UpdateTag, DeleteTag, CreateLanguage) | 🔴 Critical | Gotcha #16 |
| 02 | 9 `RequireAuthorization("Permission.X.Y")` string-based auth calls | 🔴 Critical | §2.1 Rule 1, Gotcha #20, #23 |
| 03 | `Tag.Activate()` / `Tag.Deactivate()` have no commands or endpoints — dead domain code | 🟠 High | Dead code smell |
| 04 | `DeleteSpecialization` command + endpoint completely absent (permission declared) | 🟠 High | Feature/permission mismatch |
| 05 | `LanguageDeactivatedDomainEvent` missing — `Language.Deactivate()` raises nothing | 🟠 High | Gotcha #14 asymmetry (Activate raises, Deactivate does not) |
| 06 | `GetLanguageById` endpoint missing | 🟡 Medium | CRUD completeness |
| 07 | `GetSpecializationById` endpoint missing | 🟡 Medium | CRUD completeness |
| 08 | Specialization has no PATCH `/activate` + `/deactivate` endpoints (PUT covers it, but inconsistent with Category pattern) | 🟡 Medium | Consistency |
| 09 | Category lifecycle not published as integration events | 🟡 Medium | Future — no consumer yet |
| 10 | `AttachmentUploaded` not published as integration event | 🟡 Medium | Future — no consumer yet |
| 11 | `GetEntityTranslations` has no language/status filters | 🔵 Low | Admin UX |
| 12 | No `BatchApproveTranslations` command | 🔵 Low | Admin UX |
| 13 | No `RestoreCategory` command/endpoint (soft-delete is irreversible via API) | 🔵 Low | Completeness |
| 14 | Tags have no multilingual translation (no `TagTranslation` entity) | 🔵 Low (design change) | UX gap |
| 15 | Specializations have no multilingual translation | 🔵 Low (design change) | UX gap |

---

## 2. Architectural Decisions (upfront)

Decisions that shape the plan. Locked in here so implementation is mechanical.

### 2.1 Tag activate/deactivate — no domain events

`Tag` remains `AuditableEntity` (NOT `IAggregateRoot`). Per gotcha #1: non-aggregates can't raise domain events. Tag activation state changes are purely local — no downstream module needs to react (no translation triggers, no SEO changes, no notifications).

**Decision**: Add `ActivateTagCommand` + `DeactivateTagCommand` + endpoints. Do NOT add domain events. Do NOT promote Tag to aggregate root.

### 2.2 Specialization Delete + PATCH — mirror Category pattern

`Specialization` is `AuditableEntity` with `SoftDelete()` + `Restore()` from `ISoftDeletable`. Uses same pattern as Category.

**Decision**: Add `DeleteSpecializationCommand` (soft-delete), `ActivateSpecializationCommand`, `DeactivateSpecializationCommand`, `GetSpecializationByIdQuery`. Mirror Category endpoint patterns exactly.

### 2.3 LanguageDeactivated — publish-only, no immediate consumers

Current risk analysis: `EntityTranslationOrchestrator.TranslateToAllActiveLanguagesAsync` re-reads active languages at call-time. New translations skip deactivated languages automatically. Existing `Place.PlaceTranslations` rows for a deactivated language persist (acceptable — they become viewable again on reactivate).

The actual asymmetry: `Activate()` publishes `LanguageActivatedIntegrationEvent` → 4 modules backfill translations. `Deactivate()` publishes nothing. Symmetry matters for audit/analytics and future consumers (e.g. admin UI showing per-language content counts).

**Decision**:
- Add `LanguageDeactivatedDomainEvent` (guarded — only raise if currently active)
- Add `LanguageDeactivatedIntegrationEvent` in `ContentCore.Contracts`
- Add `LanguageDeactivatedDomainEventHandler` in `ContentCore.Infrastructure` → writes outbox row
- Register in `IntegrationEventTypeRegistry` (count 19 → 20)
- **Do NOT add consumers in ContentPlaces/ContentTours/ContentSeo/ContentBlogs** in this PR. The event will queue in the outbox and wait for future consumers. This matches the pattern we already use for `ServiceItemCreated/Deleted` events.
- Document in work log: "active event, no subscriber yet — safe to queue; future modules will consume via outbox replay"

### 2.4 Defer Category/Attachment integration events (GAP-09, GAP-10)

No consumer exists today. `SeoEntityType` doesn't include Category. Adding integration events without consumers creates unhandled outbox rows that retry + dead-letter. Wastes resources.

**Decision**: Document as TODO in the respective domain event handlers. Do NOT add to registry. Add in a future PR when a consumer ships.

### 2.5 Defer multilingual Tag/Specialization (GAP-14, GAP-15)

Requires new entities (`TagTranslation`, `SpecializationTranslation`), new `DbSet`s, EF configs, migrations, translation pipeline integration (auto-translate on create/update, re-translate on update, human-review support), DTO changes across every consumer, admin UI. 4h+ each, minimum.

**Decision**: Out of scope. Separate plan documents will cover each when the business wants a fully multilingual tag/specialization system.

### 2.6 BatchApproveTranslations (GAP-12) — polish, defer

Useful but not blocking. Admins can approve one at a time via `ApproveTranslationCommand` today.

**Decision**: Document as follow-up; include in Phase 5 if time permits.

---

## 3. Execution Phases

Sequential. Each phase must leave build + tests green before moving on.

### Phase 1 — Critical rule violations (~1h)

Fixes GAP-01 + GAP-02. These are blocking because they violate `agent-context.md` rules that every other module follows.

### Phase 2 — Dead code + missing features (~2.25h)

Fixes GAP-03 (Tag activate/deactivate) + GAP-04 (DeleteSpecialization).

### Phase 3 — Language deactivation event flow (~1.5h)

Fixes GAP-05. Publish-only (no consumer implementation this PR).

### Phase 4 — CRUD completeness + consistency (~2h)

Fixes GAP-06, GAP-07, GAP-08, GAP-13. Adds missing `GetById` endpoints + PATCH activate/deactivate for Specialization + RestoreCategory.

### Phase 5 — Polish (~1h, optional)

GAP-11 filtering + GAP-12 batch approval. Only if time permits.

### Phase 6 — Document deferred items

GAP-09, GAP-10, GAP-14, GAP-15 get TODO comments in relevant source files. Record as "deferred" in work log.

---

## 4. Phase 1 — Critical Rule Violations

### 4.1 GAP-01: Add ILogger to 4 handlers

Mechanical edit. Pattern copied from `CreateSpecializationCommandHandler` (already has ILogger).

**Files to modify:**

1. `ContentCore.Application/Commands/Tag/CreateTag/CreateTagCommandHandler.cs`
2. `ContentCore.Application/Commands/Tag/UpdateTag/UpdateTagCommandHandler.cs`
3. `ContentCore.Application/Commands/Tag/DeleteTag/DeleteTagCommandHandler.cs`
4. `ContentCore.Application/Commands/Language/CreateLanguage/CreateLanguageCommandHandler.cs`

**Pattern** (apply to each):

```csharp
// Add using:
using Microsoft.Extensions.Logging;

// Inject:
public sealed class CreateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateTagCommandHandler> logger)   // ← ADD
    : ICommandHandler<CreateTagCommand, CreateTagResult>

// Log on success before return:
logger.LogInformation(
    "Tag created: {TagId} (Slug={Slug})", tag.Id, tag.Slug);
```

**Verify**: grep `ILogger<` in ContentCore.Application → must include these 4 new matches.

### 4.2 GAP-02: Replace 9 `RequireAuthorization` strings with `MustHavePermissionAttribute`

**File: `ContentCore.Presentation/Endpoints/Category/CategoryEndpoints.cs`** (8 fixes)

| Line | Current | Replacement |
|------|---------|-------------|
| 78 | `.RequireAuthorization("Permission.Category.Read")` | `.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Read))` `.RequireAuthorization()` |
| 93 | same | same |
| 115 | `.RequireAuthorization("Permission.Category.Create")` | `.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Create))` `.RequireAuthorization()` |
| 137 | `.RequireAuthorization("Permission.Category.Update")` | `.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update))` `.RequireAuthorization()` |
| 150 | `.RequireAuthorization("Permission.Category.Delete")` | `.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Delete))` `.RequireAuthorization()` |
| 163 | `.RequireAuthorization("Permission.Category.Update")` | `.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update))` `.RequireAuthorization()` |
| 176 | same | same |
| 193 | same | same |

Also add to the `using` section:
```csharp
using ContentCore.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
```

**File: `ContentCore.Presentation/Endpoints/Attachment/AttachmentEndpoints.cs`** (1 fix)

| Line | Current | Replacement |
|------|---------|-------------|
| 100 | `.RequireAuthorization("Permission.Attachment.Delete")` | `.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Attachment, AppAction.Delete))` `.RequireAuthorization()` |

**Verify**: `dotnet build` → 0 errors. `grep -r "RequireAuthorization(\"Permission" ContentCore.Presentation` → 0 matches.

---

## 5. Phase 2 — Dead Code + Missing Features

### 5.1 GAP-03: Tag Activate / Deactivate

Tag entity already has `Activate()` and `Deactivate()` methods. Need commands + endpoints only.

**New files:**

```
ContentCore.Application/Commands/Tag/ActivateTag/
  ActivateTagCommand.cs
  ActivateTagCommandHandler.cs
  ActivateTagCommandValidator.cs   (simple — just Id not empty)

ContentCore.Application/Commands/Tag/DeactivateTag/
  DeactivateTagCommand.cs
  DeactivateTagCommandHandler.cs
  DeactivateTagCommandValidator.cs
```

**Command records:**

```csharp
// ActivateTagCommand.cs
namespace ContentCore.Application.Commands.Tag.ActivateTag;
public sealed record ActivateTagCommand(Guid Id) : ICommand;

// DeactivateTagCommand.cs (mirror)
```

**Handler pattern** (mirror `DeleteTagCommandHandler` minus the remove call):

```csharp
public sealed class ActivateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ActivateTagCommandHandler> logger)
    : ICommandHandler<ActivateTagCommand>
{
    public async Task<Result> Handle(ActivateTagCommand request, CancellationToken ct)
    {
        try
        {
            var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (tag is null)
                return Result.Failure(
                    new Error("Tag.NotFound", $"Tag '{request.Id}' was not found."),
                    Outcome.NotFound);

            // Gotcha #14: guard before raising state-change methods
            if (tag.IsActive)
                return Result.Success();   // idempotent — no-op

            tag.Activate();

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Tag.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync("tags", ct);
            logger.LogInformation("Tag {TagId} activated.", tag.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
```

**Endpoint changes** in `ContentCore.Presentation/Endpoints/Tag/TagEndpoints.cs`:

```csharp
tags.MapPatch("/{id:guid}/activate", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new ActivateTagCommand(id), ct);
    return result.ToApiResult();
})
.WithName("ActivateTag")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithSummary("Activate a tag")
.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Update))
.RequireAuthorization();

tags.MapPatch("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new DeactivateTagCommand(id), ct);
    return result.ToApiResult();
})
.WithName("DeactivateTag")
// ... same pattern, same permission
```

**Permission**: reuse `ContentCoreFeatures.Tag, AppAction.Update` — activate/deactivate is a form of update. No new permission needed.

**Verify**:
- `PATCH /api/v1/content-core/tags/{id}/activate` with active tag → 200 (idempotent no-op)
- `PATCH /api/v1/content-core/tags/{id}/deactivate` on active → 200, tag.IsActive=false
- `PATCH .../{id}/activate` on deactivated → 200, tag.IsActive=true
- Cache invalidation: subsequent `GET /tags?activeOnly=true` excludes deactivated tag

### 5.2 GAP-04: DeleteSpecialization

Specialization inherits `SoftDelete()` from `AuditableEntity : ISoftDeletable`. Permission `Specialization.Delete` already declared in `ContentCorePermissionCatalog`.

**New files:**

```
ContentCore.Application/Commands/Specialization/DeleteSpecialization/
  DeleteSpecializationCommand.cs
  DeleteSpecializationCommandHandler.cs
  DeleteSpecializationCommandValidator.cs
```

**Pattern**: copy `DeleteTagCommandHandler` with substitutions:
- `ITagRepository` → `ISpecializationRepository`
- `"tags"` tag → `"specializations"` cache tag
- `Tag.NotFound` → `Specialization.NotFound`
- `tagRepository.Remove(tag)` → `specialization.SoftDelete()` (not remove — Specialization is AuditableEntity, soft-delete is the correct pattern)

**Wait — design nuance**: Tag uses hard `Remove()`. Specialization should use `SoftDelete()` per convention (AuditableEntity + ISoftDeletable). Verify how DeleteCategory handles this (Category also uses SoftDelete via Delete method).

Actually, looking at `DeleteTagCommandHandler`:
```csharp
tagRepository.Remove(tag);   // Hard delete
```

Tag uses hard delete. Specialization should use soft-delete (follow Category pattern). The difference: Tags are small reference data that can be physically removed; Specializations might be historically referenced (by past tour guides), so soft-delete is safer.

**Handler body (key fragment):**

```csharp
specialization.SoftDelete();   // Inherited from AuditableEntity/ISoftDeletable

// No repository.Remove() — EF picks up the IsDeleted flag via SoftDelete()
await unitOfWork.SaveChangesAsync(ct);
await cache.RemoveByTagAsync("specializations", ct);
```

**Endpoint** in `SpecializationEndpoints.cs`:

```csharp
specializations.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new DeleteSpecializationCommand(id), ct);
    return result.ToApiResult();
})
.WithName("DeleteSpecialization")
.Produces(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithSummary("Soft-delete a specialization")
.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Specialization, AppAction.Delete))
.RequireAuthorization();
```

**Verify**: `DELETE /api/v1/content-core/specializations/{id}` returns 200; subsequent `GET /specializations?activeOnly=true` excludes it; `IsDeleted=true` in DB.

---

## 6. Phase 3 — LanguageDeactivatedDomainEvent Flow

Biggest item in the plan. 6 new files + 2 edits + 1 registry change + 1 test update.

### 6.1 Domain event

**New file**: `ContentCore.Domain/Events/LanguageDeactivatedDomainEvent.cs`

```csharp
using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record LanguageDeactivatedDomainEvent(
    Guid LanguageId,
    string LanguageCode) : DomainEventBase;
```

### 6.2 Raise from Language.Deactivate()

**Edit**: `ContentCore.Domain/Entities/Language.cs`

```csharp
public void Deactivate()
{
    // Gotcha #14: guard before raising to avoid duplicate outbox rows on repeat calls
    if (!IsActive)
        return;

    IsActive = false;
    AddDomainEvent(new LanguageDeactivatedDomainEvent(Id, Code));
    MarkUpdated();
}
```

Also — add the same guard to `Activate()` for symmetry:

```csharp
public void Activate()
{
    if (IsActive)
        return;   // already active — no event, no save needed

    IsActive = true;
    AddDomainEvent(new LanguageActivatedDomainEvent(Id, Code));
    MarkUpdated();
}
```

> This second change also closes an adjacent gap — calling `Activate()` on an already-active language currently raises the event redundantly. Per gotcha #14 + existing `UpdateLanguageCommandHandler` fix pattern, state-change methods must be guarded at the entity level too.

### 6.3 Infrastructure domain event handler

**New file**: `ContentCore.Infrastructure/EventHandlers/LanguageDeactivatedDomainEventHandler.cs`

Pattern: copy `LanguageActivatedDomainEventHandler.cs`, substitute types.

```csharp
using ContentCore.Contracts.IntegrationEvents;
using ContentCore.Domain.Events;
using ContentCore.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace ContentCore.Infrastructure.EventHandlers;

public sealed class LanguageDeactivatedDomainEventHandler(
    ContentCoreDbContext dbContext,
    ILogger<LanguageDeactivatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<LanguageDeactivatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<LanguageDeactivatedDomainEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.Event;

        logger.LogInformation(
            "LanguageDeactivatedDomainEvent: queueing outbox for language {LanguageId} ({LanguageCode})",
            evt.LanguageId, evt.LanguageCode);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new LanguageDeactivatedIntegrationEvent(evt.LanguageId, evt.LanguageCode)));

        return Task.CompletedTask;
    }
}
```

### 6.4 Integration event

**New file**: `ContentCore.Contracts/IntegrationEvents/LanguageDeactivatedIntegrationEvent.cs`

```csharp
using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

public sealed record LanguageDeactivatedIntegrationEvent(
    Guid LanguageId,
    string LanguageCode) : IntegrationEventBase;
```

### 6.5 Registry update

**Edit**: `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`

Add to the ContentCore block (currently 1 event → 2):

```csharp
// ── ContentCore (2 events) ──
["content-core.language.activated.v1"]       = typeof(LanguageActivatedIntegrationEvent),
["content-core.language.deactivated.v1"]     = typeof(LanguageDeactivatedIntegrationEvent),
```

Update comment header: "19 events" → "20 events".

### 6.6 Test update

**Edit**: `tests/SharedKernel.Tests.Unit/IntegrationEventTypeRegistryTests.cs`

- Update `KnownMappings` array: add `("content-core.language.deactivated.v1", typeof(LanguageDeactivatedIntegrationEvent))`
- Update count assertion: `19` → `20` in 2 places
- Update comment: "All 19 expected types" → "All 20 expected types"

### 6.7 Deliberately NOT done

- No consumers in ContentPlaces/ContentTours/ContentSeo/ContentBlogs. The event queues in outbox and waits.
- Per decision 2.3: future PRs can add consumers without any changes here.
- Work log entry must explicitly note "active event, no subscriber yet — safe to queue".

---

## 7. Phase 4 — CRUD Completeness

### 7.1 GAP-06: GetLanguageById

**New files**:

```
ContentCore.Application/Queries/Language/GetLanguageById/
  GetLanguageByIdQuery.cs
  GetLanguageByIdQueryHandler.cs
```

**Query record** (must implement `ICacheableQuery` per Rule 11):

```csharp
public sealed record GetLanguageByIdQuery(Guid Id)
    : IQuery<LanguageDto>, ICacheableQuery
{
    public string CacheKey => ContentCoreCacheKeys.LanguageById(Id);
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);   // reference data
    public IReadOnlyList<string> Tags => ["languages", $"language:{Id}"];
}
```

**Add cache key** to `ContentCoreCacheKeys.cs`:
```csharp
public static string LanguageById(Guid id) => $"cc:lang:{id}";
```

**Handler**: standard pattern — load via `ILanguageRepository.GetByIdAsync`, map to `LanguageDto`, return `NotFound` if missing or soft-deleted.

**Endpoint** in `LanguageEndpoints.cs`:
```csharp
languages.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new GetLanguageByIdQuery(id), ct);
    return result.ToApiResult();
})
.WithName("GetLanguageById")
.Produces<LanguageDto>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status404NotFound)
.WithSummary("Get a language by ID")
.AllowAnonymous();   // Languages are public reference data
```

Also — `ListLanguages` cache tag must match so new single-item cache evicts when list evicts. Current pattern: list uses `"languages"` tag; single-item uses `["languages", $"language:{Id}"]`. Create/Update/Delete of languages already evicts `"languages"` → single-item caches will also invalidate. ✅

### 7.2 GAP-07: GetSpecializationById

Mirror of 7.1 with `Specialization` types. Cache duration: 30 min (reference data but not as stable as languages).

**New cache key**:
```csharp
public static string SpecializationById(Guid id) => $"cc:spec:{id}";
```

### 7.3 GAP-08: Specialization Activate / Deactivate PATCH endpoints

Specialization entity already has `Activate()` and `Deactivate()` methods (see `Specialization.cs:40-50`).

`UpdateSpecializationCommand` has an `IsActive` parameter — so it CAN currently toggle state via PUT. But:
- Forces caller to send all 4 fields on activate/deactivate (Name, Description, Icon, IsActive)
- Inconsistent with Category (has dedicated PATCH endpoints)

**New files**:

```
ContentCore.Application/Commands/Specialization/ActivateSpecialization/
  ActivateSpecializationCommand.cs
  ActivateSpecializationCommandHandler.cs
  ActivateSpecializationCommandValidator.cs

ContentCore.Application/Commands/Specialization/DeactivateSpecialization/
  ... mirror
```

Mirror Tag activate/deactivate pattern from Phase 2.1.

**Endpoints** in `SpecializationEndpoints.cs`:

```csharp
specializations.MapPatch("/{id:guid}/activate", ...);
specializations.MapPatch("/{id:guid}/deactivate", ...);
```

### 7.4 GAP-13: RestoreCategory command + endpoint

`AuditableEntity` exposes `Restore()` (mirror of `SoftDelete()`). No command uses it. Soft-deleted categories are invisible to the API — they can't be recovered.

**New files**:

```
ContentCore.Application/Commands/Category/RestoreCategory/
  RestoreCategoryCommand.cs
  RestoreCategoryCommandHandler.cs
  RestoreCategoryCommandValidator.cs
  RestoreCategoryResult.cs
```

**Handler key fragment**:

```csharp
// Important: must load with IgnoreQueryFilters so soft-deleted entity is visible
var category = await dbContext.Categories
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

if (category is null)
    return Result<RestoreCategoryResult>.Failure(
        new Error("Category.NotFound", "..."),
        Outcome.NotFound);

if (!category.IsDeleted)
    return Result<RestoreCategoryResult>.Success(new RestoreCategoryResult(category.Id));   // idempotent

category.Restore();   // Inherited from AuditableEntity/ISoftDeletable

await unitOfWork.SaveChangesAsync(ct);
await cache.RemoveByTagAsync("categories", ct);
```

> **Important architectural note**: handler needs direct `ContentCoreDbContext` injection to use `IgnoreQueryFilters()`. Repository methods apply the soft-delete filter. Per agent-context §1.3 + gotcha #22, Application layer CAN'T inject DbContext. Two options:
>
> **Option A (recommended)**: Add `GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct)` method to `ICategoryRepository`. Infrastructure implementation uses `IgnoreQueryFilters()`. Handler calls this. Keeps Clean Architecture intact.
>
> **Option B**: Inject `ContentCoreDbContext` directly — violates Rule 2.5 + gotcha #22. Rejected.
>
> **Decision**: Option A.

Interface addition to `ICategoryRepository`:
```csharp
Task<Category?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default);
```

Implementation in `CategoryRepository`:
```csharp
public Task<Category?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct = default)
    => context.Categories
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(c => c.Id == id, ct);
```

**Endpoint**:
```csharp
categories.MapPatch("/{id:guid}/restore", ...)
    .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Category, AppAction.Update))
    .RequireAuthorization();
```

Permission: reuse `Category.Update` (restore is reversal of delete — mutation).

---

## 8. Phase 5 — Polish (optional, only if time permits)

### 8.1 GAP-11: GetEntityTranslations filtering

**Edit**: `GetEntityTranslationsQuery` + handler + endpoint.

Add optional filters:
```csharp
public sealed record GetEntityTranslationsQuery(
    string EntityType,
    Guid EntityId,
    Guid? LanguageId = null,            // ← NEW
    TranslationStatus? Status = null)   // ← NEW
    : IQuery<IReadOnlyList<EntityTranslationDto>>, ICacheableQuery
{
    // Cache key must vary by filter:
    public string CacheKey => ContentCoreCacheKeys.EntityTranslations(
        EntityType, EntityId, LanguageId, Status);
    // ...
}
```

Handler adds `.WhereIf(...)` for each filter. Endpoint accepts optional query-string params.

### 8.2 GAP-12: BatchApproveTranslations (skip unless requested)

Spec: `POST /api/v1/content-core/translations/approve-batch` with body `{ EntityType, EntityId, LanguageId, FieldNames: string[] }`. Marks all auto-translated fields as human-reviewed in one shot.

New command + handler + endpoint. Mirror `ApproveTranslationCommand` pattern but iterate the list.

> **Recommend**: defer to a separate PR. Not blocking. Adding as-is adds complexity to this plan.

---

## 9. Phase 6 — Document Deferred Items

Add TODO comments to make future work discoverable.

### 9.1 GAP-09 — Category integration events

**Edit**: `ContentCore.Infrastructure/EventHandlers/CategoryCreatedDomainEventHandler.cs`

Add comment block at top:
```csharp
// TODO: Publish CategoryCreatedIntegrationEvent to outbox here when a consumer
// exists (ContentSeo.SeoEntityType.Category would need to be added first).
// Current scope: auto-translation only. No outbox write = no downstream
// notification. See Agents/ContentCore-gap-fix-plan.md GAP-09.
```

Same for `CategoryUpdatedDomainEventHandler`. No new event registered — only a comment.

### 9.2 GAP-10 — Attachment integration events

**Edit**: `ContentCore.Infrastructure/EventHandlers/AttachmentUploadedDomainEventHandler.cs`

```csharp
// TODO: Publish AttachmentUploadedIntegrationEvent to outbox for image uploads
// when a consumer exists (ContentSeo could auto-populate SeoMetadata.OgImageUrl
// for Place/Business/Tour images). See Agents/ContentCore-gap-fix-plan.md GAP-10.
```

### 9.3 GAP-14, GAP-15 — Multilingual Tag/Specialization

Add section to the module's work log entry noting these are known design gaps requiring separate plans. No code changes.

---

## 10. File Manifest (36 files total)

### Phase 1 (2 commits)
- **Edit** (5): 4 handlers + 2 endpoint files = 5 file edits (actually CategoryEndpoints + AttachmentEndpoints = 2, plus 4 handlers = 6 edits? — CategoryEndpoints counted once)
  - `CreateTagCommandHandler.cs`
  - `UpdateTagCommandHandler.cs`
  - `DeleteTagCommandHandler.cs`
  - `CreateLanguageCommandHandler.cs`
  - `CategoryEndpoints.cs`
  - `AttachmentEndpoints.cs`

### Phase 2 (2 commits)
- **New** (8): Tag activate/deactivate (6 files) + Specialization delete (3 files) = 9, but some are shared = 8 new files:
  - `ActivateTagCommand.cs` + Handler + Validator
  - `DeactivateTagCommand.cs` + Handler + Validator
  - `DeleteSpecializationCommand.cs` + Handler + Validator
- **Edit** (2): `TagEndpoints.cs` + `SpecializationEndpoints.cs`

### Phase 3 (1 commit)
- **New** (3):
  - `LanguageDeactivatedDomainEvent.cs`
  - `LanguageDeactivatedDomainEventHandler.cs`
  - `LanguageDeactivatedIntegrationEvent.cs`
- **Edit** (3):
  - `Language.cs` (guard both methods + raise new event)
  - `IntegrationEventTypeRegistry.cs`
  - `IntegrationEventTypeRegistryTests.cs`

### Phase 4 (4 commits)
- **New** (~13):
  - `GetLanguageByIdQuery.cs` + Handler
  - `GetSpecializationByIdQuery.cs` + Handler
  - `ActivateSpecializationCommand.cs` + Handler + Validator
  - `DeactivateSpecializationCommand.cs` + Handler + Validator
  - `RestoreCategoryCommand.cs` + Handler + Validator + Result
- **Edit** (4):
  - `ContentCoreCacheKeys.cs` (add 3 keys)
  - `ICategoryRepository.cs` + `CategoryRepository.cs` (add `GetByIdIncludingDeletedAsync`)
  - `LanguageEndpoints.cs`, `SpecializationEndpoints.cs`, `CategoryEndpoints.cs`

### Phase 6 (1 commit — docs only)
- **Edit** (3): Add TODO comments to 3 handlers

---

## 11. Commit Strategy

Atomic commits, conventional messages, one logical change per commit.

| # | Commit message | Phase |
|---|----------------|-------|
| 1 | `fix(ContentCore): add ILogger to Tag and Language create/update/delete handlers` | Phase 1 / GAP-01 |
| 2 | `fix(ContentCore): replace string-based RequireAuthorization with MustHavePermission attribute` | Phase 1 / GAP-02 |
| 3 | `feat(ContentCore): add ActivateTag and DeactivateTag commands + endpoints` | Phase 2 / GAP-03 |
| 4 | `feat(ContentCore): add DeleteSpecialization command + soft-delete endpoint` | Phase 2 / GAP-04 |
| 5 | `feat(ContentCore): publish LanguageDeactivatedIntegrationEvent via outbox` | Phase 3 / GAP-05 |
| 6 | `feat(ContentCore): add GetLanguageById + GetSpecializationById queries + endpoints` | Phase 4 / GAP-06, 07 |
| 7 | `feat(ContentCore): add activate/deactivate PATCH endpoints for Specialization` | Phase 4 / GAP-08 |
| 8 | `feat(ContentCore): add RestoreCategory command + endpoint` | Phase 4 / GAP-13 |
| 9 | `docs(ContentCore): document deferred integration events (GAP-09, GAP-10) as TODOs` | Phase 6 |
| 10 | `chore: ContentCore gap fix pass complete — update agent-context` | Final doc sync |

---

## 12. Risk Register

| # | Risk | Mitigation |
|---|------|------------|
| R1 | GAP-05 event has no consumer → outbox row never processed → retries to dead-letter | False alarm: outbox processor marks message processed if ZERO handlers registered (short-circuit in `CompositeOutboxProcessor`). No dead-letter. Verify this behavior exists. If not — register a no-op handler in a separate module. |
| R2 | `Language.Activate()` guard change silently breaks existing integration tests that assume repeat Activate() raises events | Search for tests against `LanguageActivatedDomainEvent`. Update any that call `.Activate()` twice expecting 2 events. (Current tests: `SharedKernel.Tests.Unit`, `ContentCore.Tests.Unit` — only 11 tests in ContentCore; scan quickly.) |
| R3 | `GetByIdIncludingDeletedAsync` on `ICategoryRepository` — pattern not used elsewhere in codebase | One-off method in one repo. Minor precedent risk. Document in repository XML doc comment. |
| R4 | `RestoreCategory` endpoint permission model: `Category.Update` or new `Category.Restore`? | Decision: reuse `Category.Update`. Admin with Update can restore. Avoids permission sprawl. |
| R5 | Activate/Deactivate reusing `AppAction.Update` — semantic overload | Already pattern in Category endpoints (line 163, 176). Consistent. |
| R6 | `GAP-11` filter adding `LanguageId` + `Status` to cache key → cache fragmentation | Acceptable: admin views are low-traffic. Each filter combo caches independently for 30 min. Not a prod concern. |
| R7 | `Language.Deactivate()` guard — if a caller currently depends on `Deactivate()` being idempotent with side effects, behavior changes | Current code: `UpdateLanguageCommandHandler` already has `!language.IsActive` guard BEFORE calling `Activate()` / `Deactivate()`. Entity-level guard is defence-in-depth. No caller relied on raising events for already-deactivated languages. |
| R8 | Phase 3 registry count 19 → 20: Messaging.Tests.Unit might assert 19 | No Messaging tests exist yet. Only `SharedKernel.Tests.Unit/IntegrationEventTypeRegistryTests.cs` has the count assertion. Updated in Phase 3.6. |
| R9 | `GAP-03` ActivateTag idempotent behavior — returning `Result.Success()` on already-active tag might surprise callers expecting 409 Conflict | Industry convention: idempotent write returns 200/204 not 409. Follows REST + matches the `LanguageActivatedDomainEvent` idempotency approach. Document in endpoint summary. |
| R10 | `RestoreCategory` may need to also clear category tree cache across multiple levels (parent/sub-categories) | Use coarse `"categories"` tag eviction — already covers tree queries (per `ListCategoriesQuery.Tags = ["categories"]`). |

---

## 13. Verification per Phase

### Phase 1
- `dotnet build YallaJo.sln` → 0 errors
- `grep -r "ILogger<Create" ContentCore.Application/Commands/Tag` → 1 match (new)
- `grep -r "RequireAuthorization(\"Permission" ContentCore.Presentation` → 0 matches
- `dotnet test` → 183+ pass

### Phase 2
- `POST /api/v1/content-core/tags` → create tag, verify `IsActive=true`
- `PATCH /tags/{id}/deactivate` → 200, tag now inactive
- `PATCH /tags/{id}/activate` → 200, tag active again
- `PATCH /tags/{id}/activate` (repeat) → 200 (idempotent)
- `DELETE /specializations/{id}` → 200, DB row has `IsDeleted=true`, `DeletedAt` set
- `GET /specializations` → deleted specialization excluded (query filter)

### Phase 3
- Activate already-active language → no new outbox row
- Deactivate active language → 1 new outbox row with Type="content-core.language.deactivated.v1"
- Deactivate already-deactivated language → no new outbox row
- `dotnet test SharedKernel.Tests.Unit` → 53 → 53 (count preserved, one test gains one mapping)

### Phase 4
- `GET /languages/{id}` → 200 with LanguageDto
- `GET /languages/{id}` on non-existent → 404
- `GET /specializations/{id}` → 200
- `PATCH /specializations/{id}/deactivate` → 200
- `PATCH /categories/{id}/restore` on soft-deleted → 200, `IsDeleted=false`
- `PATCH /categories/{id}/restore` on non-deleted → 200 (idempotent)

### Final
- `dotnet build` → 0 errors
- `dotnet test` → 183+ pass
- `IntegrationEventTypeRegistry.AllRegisteredTypes.Count` → 20

---

## 14. Definition of Done

- [ ] `dotnet build YallaJo.sln` → 0 errors, 0 new warnings
- [ ] `dotnet test YallaJo.sln` → 183+ tests pass (1 new mapping + 1 count update in registry test)
- [ ] `lsp_diagnostics` clean on all 36 changed files
- [ ] `grep -r "ILogger<" ContentCore.Application/Commands` → every handler has it
- [ ] `grep -r "RequireAuthorization(\"Permission" ContentCore` → 0 matches
- [ ] Registry: `IntegrationEventTypeRegistry.AllRegisteredTypes.Count == 20`
- [ ] Smoke tests for every new endpoint pass
- [ ] `agent-context.md` updated:
  - Work log entries for each commit
  - `ContentCore` row in module status = ✅ (still complete — this is maintenance)
  - Registry count 19 → 20 in `§N PR 2` section
- [ ] `error-log.md` updated if any mistakes discovered during implementation
- [ ] TODO comments added to Category/Attachment domain event handlers (Phase 6)

---

## 15. Out of Scope (explicit — future work)

1. **GAP-09 / GAP-10** — Category and Attachment integration events. Consumer doesn't exist. Deferred via TODO comments.
2. **GAP-12** — BatchApproveTranslations. Admin UX polish. Defer.
3. **GAP-14 / GAP-15** — Multilingual Tag / Specialization. Each requires separate design doc + plan (entity, migration, auto-translation integration, DTO changes, UI changes). 4h+ each.
4. **Adding consumers for LanguageDeactivatedIntegrationEvent in ContentPlaces/ContentTours/ContentSeo/ContentBlogs** — deliberate per Decision 2.3. Event queues; consumers added when business value appears.
5. **Making Tag an IAggregateRoot** — per Decision 2.1, Tag stays AuditableEntity. Domain event refactor rejected.

---

## 16. Total Estimated Effort

| Phase | Items | Hours |
|-------|-------|-------|
| 1 | GAP-01, 02 | 1.0 |
| 2 | GAP-03, 04 | 2.25 |
| 3 | GAP-05 | 1.5 |
| 4 | GAP-06, 07, 08, 13 | 2.0 |
| 5 (optional) | GAP-11 | 0.5 |
| 6 (docs) | — | 0.25 |
| **Total core (1–4)** | | **~6.75h** |
| **Total with Phase 5** | | **~7.25h** |

~1 focused day of work.
