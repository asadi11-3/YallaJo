# ContentCore Module — Full Audit: Bugs, Errors & Fixes
> **Date**: 2026-04-17 | **Auditor**: AI Agent (full codebase read)  
> **Build state**: ✅ 0 errors (all issues are logic/architecture/pattern violations, not compile errors)  
> **Scope**: ContentCore.Domain, ContentCore.Application, ContentCore.Infrastructure, ContentCore.Presentation
> **Status**: ✅ ALL 11 BUGS FIXED — 2026-04-17 | Build: 0 errors confirmed

---

## Verdict Summary

ContentCore is the **best-quality module** in the codebase. The core architecture is correct:
- `IContentCoreUnitOfWork` wraps `IUnitOfWork<ContentCoreDbContext>` which **does dispatch domain events** ✅ (unlike ContentPlaces)
- `LanguageActivatedDomainEventHandler` correctly writes `OutboxMessage` to the DB ✅
- Every query implements `ICacheableQuery` ✅
- Every command handler injects `HybridCache` and calls `RemoveByTagAsync` ✅
- `ContentCoreCacheKeys` static class covers all cached entities ✅
- All handlers have `try/catch` for cancellation and concurrency ✅

Issues found are **medium/minor** — logic bugs, missing `ILogger`, cache tag mismatches, and one domain-level design issue.

---

## Issues Found

---

### BUG-001 🔴 `UpdateLanguageCommandHandler` — Always triggers `LanguageActivatedDomainEvent` on every update, even when deactivating

**File**: `ContentCore.Application/Commands/Language/UpdateLanguage/UpdateLanguageCommandHandler.cs`

**Code** (lines 32–35):
```csharp
if (request.IsActive)
    language.Activate();   // ← raises LanguageActivatedDomainEvent
else
    language.Deactivate(); // ← raises nothing
```

**`Language.Activate()`** (domain entity, line 56–61):
```csharp
public void Activate()
{
    IsActive = true;
    AddDomainEvent(new LanguageActivatedDomainEvent(Id, Code));  // ← ALWAYS fires
    MarkUpdated();
}
```

**The problem**: `Activate()` raises `LanguageActivatedDomainEvent` unconditionally. If the language was **already active** and you call `UpdateLanguage` with `IsActive = true`, it fires the event again — triggering `LanguageActivatedDomainEventHandler` to write another `OutboxMessage` to the outbox. The downstream consumer (`ContentPlaces.Infrastructure.EventHandlers.LanguageActivatedIntegrationEventHandler`) will receive a duplicate activation signal and attempt to backfill translations for all Places and Businesses again — unnecessary work and potential duplicate translation rows.

**Root cause**: No guard checking whether the language is already in the target state.

**Fix**:
```csharp
// In UpdateLanguageCommandHandler — only call state-change methods if state actually changes:
if (request.IsActive && !language.IsActive)
    language.Activate();
else if (!request.IsActive && language.IsActive)
    language.Deactivate();
// If state is unchanged, do nothing — no domain event, no outbox write
```

---

### BUG-002 🔴 `DeleteAttachmentCommandHandler` — Cache tag mismatch: evicts `"attachments"` but queries cache under `"attachments:{EntityType}:{EntityId}"`

**File**: `ContentCore.Application/Commands/Attachment/DeleteAttachment/DeleteAttachmentCommandHandler.cs`

**Line 49**:
```csharp
await cache.RemoveByTagAsync("attachments", cancellationToken);
```

**But `GetEntityAttachmentsQuery` tags** (query caches under):
```csharp
public IReadOnlyList<string> Tags => ["attachments", $"attachments:{EntityType}:{EntityId}"];
```

**And `GetAttachmentByIdQuery` tags**:
```csharp
public IReadOnlyList<string> Tags => ["attachments", $"attachment:{AttachmentId}"];
```

`RemoveByTagAsync("attachments")` does evict ALL attachment entries (both list and detail) because both query types include the coarse `"attachments"` tag. So this is **functionally correct but dangerously broad** — it evicts ALL entity attachment lists system-wide, not just the one for the deleted attachment's entity.

**Fix**: Use the fine-grained tag so only the affected entity's cache is evicted:
```csharp
// After loading the attachment (you already have it in scope):
await cache.RemoveByTagAsync($"attachments:{attachment.EntityType}:{attachment.EntityId}", cancellationToken);
await cache.RemoveByTagAsync($"attachment:{request.AttachmentId}", cancellationToken);
```

---

### BUG-003 🟡 `Tag` entity uses `BaseEntity` but manually sets `UpdatedAt = DateTime.UtcNow` — should call `MarkUpdated()`

**File**: `ContentCore.Domain/Entities/Tag.cs` (lines 49, 55, 61)

```csharp
public void Update(string name, string slug)
{
    ...
    UpdatedAt = DateTime.UtcNow;  // ← raw assignment
}

public void Activate()
{
    IsActive = true;
    UpdatedAt = DateTime.UtcNow;  // ← raw assignment
}

public void Deactivate()
{
    IsActive = false;
    UpdatedAt = DateTime.UtcNow;  // ← raw assignment
}
```

**Problem**: `BaseEntity` does NOT have `MarkUpdated()` — only `AuditableEntity` does. So using `UpdatedAt = DateTime.UtcNow` directly is the correct approach for `BaseEntity` children. However, `Tag` has a `RowVersion` byte array (line 19) and `UpdatedAt` — it's behaving like an `AuditableEntity` without inheriting from it.

**Root cause**: `Tag` should extend `AuditableEntity` (which gives it `MarkUpdated()`, `UpdatedAt`, `CreatedAt`, `RowVersion` managed by EF interceptors) rather than `BaseEntity`. As `BaseEntity`, the `UpdatedAt` set in the domain method is correct but the `RowVersion` is managed externally (via `[Timestamp]` attribute) while `UpdatedAt` is managed internally — inconsistent.

**Fix**: Change `Tag` to extend `AuditableEntity` and use `MarkUpdated()`:
```csharp
// Before:
public sealed class Tag : BaseEntity

// After:
public sealed class Tag : AuditableEntity

// Remove: [Timestamp] public byte[] RowVersion — AuditableEntity manages this

// In business methods, replace:
UpdatedAt = DateTime.UtcNow;
// With:
MarkUpdated();
```

> ⚠️ **Migration needed**: Changing the base class may affect the EF configuration. Check `TagConfiguration.cs` to ensure `RowVersion` is not double-configured.

---

### BUG-004 🟡 `AssignCategoriesToEntityCommandHandler` and `AssignTagsToEntityCommandHandler` — Cache tags don't match query tags

**Files**:
- `ContentCore.Application/Commands/EntityCategory/AssignCategoriesToEntity/AssignCategoriesToEntityCommandHandler.cs` (line 72)
- `ContentCore.Application/Commands/EntityTag/AssignTagsToEntity/AssignTagsToEntityCommandHandler.cs` (line 72)

**Invalidation in command handler**:
```csharp
await cache.RemoveByTagAsync($"entity-categories:{request.EntityType}:{request.EntityId}", cancellationToken);
```

**But `GetEntityCategoriesQuery` registers under these tags**:
```csharp
public IReadOnlyList<string> Tags => ["entity-categories", $"entity-categories:{EntityType}:{EntityId}"];
```

The command uses `RemoveByTagAsync("entity-categories:{type}:{id}")` which DOES match the fine-grained tag — this evicts only that entity's categories. ✅ This part is correct.

However `RemoveCategoryFromEntityCommandHandler` (line 47) uses the same tag string — also correct ✅.

**The actual issue**: The coarse `"entity-categories"` tag (registered by queries) is never evicted by any command. If a client subscribes to the coarse tag for cross-entity invalidation it would never be cleared. This is an acceptable trade-off (fine-grained eviction is preferred), but the coarse tag in queries is dead weight. Either use the coarse tag in invalidation too, or remove it from query Tags.

**Fix** (simplify — remove unused coarse tag from query):
```csharp
// GetEntityCategoriesQuery — remove the coarse tag since no command uses it:
public IReadOnlyList<string> Tags => [$"entity-categories:{EntityType}:{EntityId}"];
// Same for GetEntityTagsQuery
```

---

### BUG-005 🟡 `CreateCategoryCommandHandler` — Missing `ILogger`

**File**: `ContentCore.Application/Commands/Category/CreateCategory/CreateCategoryCommandHandler.cs`

```csharp
public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICategoryHierarchyService hierarchyService,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)   // ← no ILogger
```

The guide rule (`agent-context.md` §Critical Gotchas):
> *"Add `ILogger<THandler>` to every handler"*

`CreateCategoryCommandHandler` has no `ILogger`. The only logging that would happen here is from the MediatR `LoggingBehavior` pipeline (which logs at entry/exit), but handler-level structured logging for specific events (e.g., "Category created: {SlugId}") is absent.

Same issue in:
- `ReorderCategoriesCommandHandler` — no `ILogger`
- `DeleteCategoryCommandHandler` — no `ILogger`
- `CreateSpecializationCommandHandler` — no `ILogger`
- `AssignCategoriesToEntityCommandHandler` — no `ILogger`
- `AssignTagsToEntityCommandHandler` — no `ILogger`
- `RemoveCategoryFromEntityCommandHandler` — no `ILogger`
- `RemoveTagFromEntityCommandHandler` — no `ILogger`
- `ApproveTranslationCommandHandler` — no `ILogger`
- `UpdateTranslationCommandHandler` — no `ILogger`
- `BatchTranslateCommandHandler` — no `ILogger`
- `ListCategoriesQueryHandler` — no `ILogger`
- `GetCategoryByIdQueryHandler` — no `ILogger`
- `ListTagsQueryHandler` — no `ILogger`
- `GetTagByIdQueryHandler` — no `ILogger`
- `ListLanguagesQueryHandler` — no `ILogger`
- `ListSpecializationsQueryHandler` — no `ILogger`
- `GetEntityCategoriesQueryHandler` — no `ILogger`
- `GetEntityTagsQueryHandler` — no `ILogger`
- `GetEntityTranslationsQueryHandler` — no `ILogger`
- `GetAttachmentByIdQueryHandler` — no `ILogger`
- `GetEntityAttachmentsQueryHandler` — no `ILogger`

**Fix for each**: Add `ILogger<THandler>` to the primary constructor and log at least the successful completion with relevant IDs.

---

### BUG-006 🟡 `ListCategoriesQueryHandler` — No `ILogger` AND no `OperationCanceledException` catch could surface as unhandled 500

**File**: `ContentCore.Application/Queries/Category/ListCategories/ListCategoriesQueryHandler.cs`

The handler does have a `try/catch (OperationCanceledException)` block ✅. The issue is only the missing `ILogger`.

However, the handler builds the category tree recursively via `BuildNode()`. If the data has a **circular parent reference** in the DB (e.g., Category A's parent is Category B, and Category B's parent is Category A), `BuildNode()` will produce a **stack overflow** since it recurses without cycle detection.

**Fix**: Add cycle detection in `BuildNode`:
```csharp
private static CategoryDto BuildNode(
    CategoryEntity category,
    ILookup<Guid?, CategoryEntity> byParent,
    HashSet<Guid>? visited = null)
{
    visited ??= [];
    if (!visited.Add(category.Id))
    {
        // Cycle detected — return leaf node
        return CategoryDto.From(category, []);
    }

    var children = byParent[category.Id]
        .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
        .Select(c => BuildNode(c, byParent, visited))  // pass visited set
        .ToList() as IReadOnlyList<CategoryDto>;

    return CategoryDto.From(category, children);
}
```

---

### BUG-007 🟡 `GetCategoryByIdQueryHandler` — Soft-deleted categories return 404 but active-only check is inconsistent

**File**: `ContentCore.Application/Queries/Category/GetCategoryById/GetCategoryByIdQueryHandler.cs` (line 26):

```csharp
if (category is null || !category.IsActive)
{
    return Result<CategoryDto>.Failure(
        new Error("Category.NotFound", $"Category '{request.Id}' was not found."),
        Outcome.NotFound);
}
```

The handler returns 404 if the category is soft-deleted (`IsDeleted = true`, managed by EF query filter) OR if `IsActive = false`. This is correct for anonymous callers but **admin callers cannot retrieve deactivated categories by ID**, which they may need for admin UIs (to show the category before reactivating it).

The `ListCategoriesQuery` already accepts `ActiveOnly = false` for admins to see all categories. `GetCategoryById` should follow the same pattern.

**Fix**: Accept an optional `IncludeInactive` parameter (or use `IsAdmin` from `ICurrentUser`) to bypass the active check for admin callers.

---

### BUG-008 🟡 `SetPrimaryImageCommandHandler` — Loads attachment `asNoTracking: true` (default) then tries to modify it

**File**: `ContentCore.Application/Commands/Attachment/SetPrimaryImage/SetPrimaryImageCommandHandler.cs` (line 23):

```csharp
var attachment = await attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken);
// ← asNoTracking defaults to true in GetByIdAsync
```

The attachment is loaded for validation only (checking EntityType/EntityId). The actual mutation is on `EntityImage` entities. So using `asNoTracking: true` here is fine for the attachment — but should be explicit:

```csharp
var attachment = await attachmentRepository.GetByIdAsync(
    request.AttachmentId, cancellationToken, asNoTracking: true);  // ← explicit
```

Minor readability issue, not a bug.

---

### BUG-009 🟠 `UploadAttachmentCommandHandler` — Missing step comment (comment says "2." but code skips to "3.")

**File**: `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandHandler.cs`

```csharp
// 1. Upload file to storage
var folder = ...
var uploadResult = ...

var attachment = Domain.Entities.Attachment.Create(...)    // no comment — where's "2."?

// 3. Set optional media metadata
```

Step 2 is missing its comment. Minor — doesn't affect functionality.

---

### BUG-010 🟠 `Tag.Create()` — Missing `Guid.CreateVersion7()` — uses implicit base class Id assignment

**File**: `ContentCore.Domain/Entities/Tag.cs` (lines 30–36):

```csharp
return new Tag
{
    Name = name.Trim(),
    Slug = slug.Trim().ToLowerInvariant(),
    IsActive = true
};
// ← Id not set explicitly
```

The guide rule: *"`Guid.CreateVersion7()` in all factory methods (never `Guid.NewGuid()`)"*.

`Tag` extends `BaseEntity`. Looking at `BaseEntity`:
```csharp
// From SharedKernel
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
}
```

The base class sets `Id = Guid.CreateVersion7()` by default. So the `Tag.Create()` factory not setting `Id` explicitly is **technically correct** (the base class handles it). However, for consistency and explicitness (auditors reading the factory can see Id is set), it's better practice to set it explicitly. Not a bug — just inconsistency.

Same pattern in: `CategoryTranslation.Create()`, `EntityCategory.Create()`, `EntityTag.Create()`

---

### DESIGN-001 🟡 `IContentCoreUnitOfWork` is defined in `ContentCore.Domain` but its implementation wraps `IUnitOfWork<ContentCoreDbContext>` which is an Infrastructure type

**File**: `ContentCore.Domain/Repositories/IContentCoreUnitOfWork.cs`

The interface is in `Domain.Repositories` and extends `IUnitOfWork` (SharedKernel). The implementation `ContentCoreUnitOfWork` is in `ContentCore.Infrastructure.Repositories`. This is correct — the interface is in Domain, the implementation in Infrastructure.

However the interface boilerplate (`using System; using System.Collections.Generic; using System.Linq; using System.Text; using System.Threading.Tasks;`) is cargo code from a Visual Studio template — completely unnecessary for this one-line interface. Minor cleanup needed.

---

### DESIGN-002 🟡 `Category.Create()` doesn't set `Id` explicitly

Same as BUG-010 note for Tag. `Category` extends `AuditableEntity` which extends `BaseEntity` — Id defaults to `Guid.CreateVersion7()`. Not a bug, but explicit is better.

---

### DESIGN-003 🟠 `ContentCore.Infrastructure` — `MediaProcessingBackgroundService` is registered but commented out in DI

**File**: `ContentCore.Infrastructure/DependencyInjection.cs` (line 92):

```csharp
//services.AddHostedService<MediaProcessingBackgroundService>();
```

The `MediaProcessingQueue` singleton and its interface are registered, but the hosted service that consumes it is commented out. This means:
- `UploadAttachmentCommandHandler` enqueues media processing jobs via `mediaProcessingQueue.EnqueueAsync(...)` ✅
- But nobody ever dequeues them — the Channel fills up silently

**Fix**: Either uncomment the hosted service registration or add a TODO comment explaining why it's disabled:
```csharp
// TODO: Enable when ffmpeg/ImageSharp is available in the deployment environment
// services.AddHostedService<MediaProcessingBackgroundService>();
```

---

### DESIGN-004 🟠 `Attachment` extends `BaseEntity` but sets `UploadedAt = DateTime.UtcNow` manually in factory — inconsistent with `AuditableEntity.CreatedAt`

**File**: `ContentCore.Domain/Entities/Attachment.cs` (line 61):

```csharp
UploadedAt = DateTime.UtcNow,
```

`Attachment` has its own `UploadedAt` property instead of using `AuditableEntity.CreatedAt`. This is intentional (semantic clarity — "uploaded at" vs "created at"). The manual `DateTime.UtcNow` assignment in the factory is correct here since `Attachment` is `BaseEntity`, not `AuditableEntity`.

Not a bug — acceptable design choice.

---

## What's Done Correctly (Positive Findings)

| Area | Status | Note |
|------|--------|------|
| `IContentCoreUnitOfWork` wraps `IUnitOfWork<TContext>` | ✅ Correct | Domain events ARE dispatched — unlike ContentPlaces |
| `LanguageActivatedDomainEventHandler` writes outbox | ✅ Correct | Only domain event handler that publishes integration event |
| All query records implement `ICacheableQuery` | ✅ Complete | Languages (1h), Categories (30m), Tags (30m), Specs (30m), Attachments (5-15m) |
| All command handlers have `HybridCache` + `RemoveByTagAsync` | ✅ Complete | Every mutating command invalidates cache |
| `ContentCoreCacheKeys` static class | ✅ Complete | All entities covered with deterministic key factories |
| Error handling patterns | ✅ Complete | All handlers have `try/catch` for cancellation AND concurrency |
| Error codes follow `{Entity}.{Reason}` convention | ✅ Consistent | `Category.NotFound`, `Tag.AlreadyExists`, etc. |
| Repository pattern | ✅ Correct | `EfRepository` for aggregates, `EfEntityRepository` for non-aggregates |
| `CategoryRepository` isolates EF Include() | ✅ Correct | Application layer has no EF dependency |
| Domain entity factories use `throw ArgumentException` | ✅ Correct | Not `Result` — matches guide rules |
| `CategoryCreatedDomainEventHandler` — no `SaveChangesAsync` | ✅ Correct | Follows Gotcha #2 |
| `BatchTranslateCommandHandler` — catches `HttpRequestException` + `TaskCanceledException` | ✅ Correct | External service error handling |
| `ReorderCategoriesCommandHandler` — batch update, single save | ✅ Efficient | No N+1 saves |
| `AssignCategoriesToEntityCommandHandler` — validates all IDs exist before inserting | ✅ Correct | Prevents orphan references |
| Circular parent prevention in `UpdateCategoryCommandHandler` | ✅ Correct | `IsAncestorAsync` + self-parent check |

---

## Fix Priority Summary

| # | ID | Severity | File | Fix |
|---|----|----|------|-----|
| 1 | BUG-001 | 🔴 High | `UpdateLanguageCommandHandler.cs` | Guard `Activate()`/`Deactivate()` calls — only call when state actually changes |
| 2 | BUG-002 | 🔴 High | `DeleteAttachmentCommandHandler.cs` | Use fine-grained cache tag `$"attachments:{type}:{entityId}"` + `$"attachment:{id}"` |
| 3 | BUG-003 | 🟡 Medium | `Tag.cs` | Change base to `AuditableEntity`, remove `[Timestamp]` attr, use `MarkUpdated()` |
| 4 | BUG-005 | 🟡 Medium | 21 handler files | Add `ILogger<THandler>` injection to all handlers that are missing it |
| 5 | BUG-006 | 🟡 Medium | `ListCategoriesQueryHandler.cs` | Add cycle detection in `BuildNode()` recursive tree builder |
| 6 | BUG-007 | 🟡 Medium | `GetCategoryByIdQueryHandler.cs` | Allow admins to retrieve inactive categories (add `IncludeInactive` param) |
| 7 | BUG-004 | 🟡 Medium | `GetEntityCategoriesQuery.cs`, `GetEntityTagsQuery.cs` | Remove unused coarse cache tags |
| 8 | DESIGN-003 | 🟠 Low | `DependencyInjection.cs` | Add explanatory comment for commented-out `MediaProcessingBackgroundService` |
| 9 | BUG-008 | 🟠 Low | `SetPrimaryImageCommandHandler.cs` | Make `asNoTracking: true` explicit for clarity |
| 10 | BUG-009 | 🟠 Low | `UploadAttachmentCommandHandler.cs` | Fix missing step 2 comment |
| 11 | DESIGN-001 | 🟠 Low | `IContentCoreUnitOfWork.cs` | Remove cargo `using` statements |

---

## Detailed Fixes

### Fix BUG-001: `UpdateLanguageCommandHandler` — Prevent spurious activation events

```csharp
// ContentCore.Application/Commands/Language/UpdateLanguage/UpdateLanguageCommandHandler.cs

// Replace lines 32-35:
if (request.IsActive)
    language.Activate();
else
    language.Deactivate();

// With:
if (request.IsActive && !language.IsActive)
    language.Activate();
else if (!request.IsActive && language.IsActive)
    language.Deactivate();
// If state unchanged: do nothing — no domain event raised, no outbox row written
```

### Fix BUG-002: `DeleteAttachmentCommandHandler` — Fine-grained cache eviction

```csharp
// ContentCore.Application/Commands/Attachment/DeleteAttachment/DeleteAttachmentCommandHandler.cs

// Replace line 49:
await cache.RemoveByTagAsync("attachments", cancellationToken);

// With (uses attachment already loaded above):
await cache.RemoveByTagAsync(
    $"attachments:{attachment.EntityType}:{attachment.EntityId}", cancellationToken);
await cache.RemoveByTagAsync(
    $"attachment:{request.AttachmentId}", cancellationToken);
```

### Fix BUG-003: `Tag` → `AuditableEntity`

```csharp
// ContentCore.Domain/Entities/Tag.cs

// 1. Change base class:
public sealed class Tag : AuditableEntity   // was: BaseEntity

// 2. Remove [Timestamp] attribute and RowVersion property
// (AuditableEntity provides RowVersion via EF configuration)

// 3. Replace all UpdatedAt = DateTime.UtcNow with MarkUpdated():
public void Update(string name, string slug)
{
    Name = name.Trim();
    Slug = slug.Trim().ToLowerInvariant();
    MarkUpdated();   // was: UpdatedAt = DateTime.UtcNow
}

public void Activate()
{
    IsActive = true;
    MarkUpdated();
}

public void Deactivate()
{
    IsActive = false;
    MarkUpdated();
}
```

> ⚠️ **Migration note**: If `TagConfiguration.cs` has explicit `IsRowVersion()` / `HasConversion()` for the old `RowVersion` byte[] property, remove that configuration. `AuditableEntity` handles `RowVersion` via its base EF configuration.

### Fix BUG-005: Add `ILogger` to all handlers missing it

Pattern to follow (from handlers that already have it correctly):

```csharp
// Add to constructor:
public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ICategoryHierarchyService hierarchyService,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateCategoryCommandHandler> logger)   // ← ADD

// Add at least one log call on success:
logger.LogInformation("Category created: {CategoryId} (Slug={Slug})", category.Id, category.Slug);
```

### Fix BUG-006: Cycle detection in `ListCategoriesQueryHandler.BuildNode`

```csharp
// In ListCategoriesQueryHandler.cs — update both call sites:

// Top-level call:
var tree = roots
    .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
    .Select(r => BuildNode(r, byParent, []))   // pass empty HashSet
    .ToList() as IReadOnlyList<CategoryDto>;

// Method signature:
private static CategoryDto BuildNode(
    CategoryEntity category,
    ILookup<Guid?, CategoryEntity> byParent,
    HashSet<Guid> visited)
{
    if (!visited.Add(category.Id))
        return CategoryDto.From(category, []);   // cycle — return leaf

    var children = byParent[category.Id]
        .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
        .Select(c => BuildNode(c, byParent, visited))
        .ToList() as IReadOnlyList<CategoryDto>;

    return CategoryDto.From(category, children);
}
```
