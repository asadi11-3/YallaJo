# YallaJo — ContentCore Module Audit Report

> Full audit report generated from code analysis.  
> Last updated: 2025-07-15  
> Module status: **Marked as Complete** in agent-context.md

---

## Table of Contents

1. [Module Overview](#1-module-overview)
2. [Architecture Compliance](#2-architecture-compliance)
3. [Endpoint Security Audit](#3-endpoint-security-audit)
4. [Rule Compliance Matrix](#4-rule-compliance-matrix)
5. [Gap 1 — ICurrentUser Misuse (HIGH)](#5-gap-1--icurrentuser-misuse-high)
6. [Gap 2 — Exception Throwing in Infrastructure (MEDIUM)](#6-gap-2--exception-throwing-in-infrastructure-medium)
7. [Gap 3 — Missing Language Endpoints (LOW)](#7-gap-3--missing-language-endpoints-low)
8. [What Passed — Full Checklist](#8-what-passed--full-checklist)
9. [Scorecard](#9-scorecard)
10. [Fix Priority & Recommendations](#10-fix-priority--recommendations)

---

## 1. Module Overview

### Purpose

ContentCore is the **shared content foundation** for YallaJo. It owns categories, tags, specializations, languages, translations, attachments, entity images, and entity-category/entity-tag assignments. Every other content module (ContentPlaces, ContentTours, ContentBlogs) depends on it.

### Structure

| Layer | Files | Key Contents |
|-------|-------|-------------|
| **Domain** | 39 | 12 entities, 4 enums, 12 domain events, 9 repository interfaces, 1 service interface |
| **Application** | 164 | 46 handlers (command + query), 33 validators, 5 interfaces, 2 auth classes, 1 caching |
| **Contracts** | 14 | 7 integration events, 2 auth contracts, 1 attachment service interface |
| **Infrastructure** | 57 | 12 event handlers, 17 persistence configs, 9 repository implementations, 10 services, 2 background jobs, 5 migrations |
| **Presentation** | 29 | 48 endpoints across 8 endpoint groups |
| **Tests** | 19 | Unit tests |
| **Total** | **~303** | |

### Domain Entities (12)

| Entity | Purpose |
|--------|---------|
| `Attachment` | Files attached to any entity (images, documents, videos) |
| `Category` | Hierarchical content classification (3-level max depth) |
| `CategoryTranslation` | Localized category names/descriptions |
| `EntityCategory` | Junction — assigns categories to any entity type |
| `EntityImage` | Image metadata + primary flag for entities |
| `EntityTag` | Junction — assigns tags to any entity type |
| `Language` | Supported languages for translations |
| `Specialization` | Guide/provider specialization areas |
| `SpecializationTranslation` | Localized specialization names |
| `Tag` | Flat content labels |
| `TagTranslation` | Localized tag names |
| `TranslationCache` | Cached translation results with approval workflow |

### Domain Enums (4)

`AttachmentType`, `EntityType`, `ImageSize`, `TranslationStatus`

### Repositories (9)

`IAttachmentRepository`, `ICategoryRepository`, `IContentCoreUnitOfWork`, `IEntityCategoryRepository`, `IEntityTagRepository`, `ILanguageRepository`, `ISpecializationRepository`, `ITagRepository`, `ITranslationCacheRepository`

### Integration Events (7)

All extend `IntegrationEventBase`:

| Event | Trigger |
|-------|---------|
| `AttachmentUploadedIntegrationEvent` | New attachment uploaded |
| `AttachmentDeletedIntegrationEvent` | Attachment removed |
| `CategoryCreatedIntegrationEvent` | New category created |
| `CategoryUpdatedIntegrationEvent` | Category modified |
| `CategoryDeletedIntegrationEvent` | Category soft-deleted |
| `CategoryRestoredIntegrationEvent` | Category restored from soft-delete |
| `EntityCategoryAssignedIntegrationEvent` | Categories assigned to an entity |

### Permission Catalog

`ContentCorePermissionCatalog` — 10 feature groups, all under `PermissionGroup.ContentManagement`:

| Feature | Read | Create | Update | Delete |
|---------|------|--------|--------|--------|
| Category | ✅ | ✅ | ✅ | ✅ |
| CategoryTranslation | ✅ | ✅ | ✅ | ✅ |
| Specialization | ✅ | ✅ | ✅ | ✅ |
| Tag | ✅ | ✅ | ✅ | ✅ |
| EntityCategory | ✅ | ✅ | — | ✅ |
| EntityImage | ✅ | ✅ | ✅ | ✅ |
| EntityTag | ✅ | ✅ | — | ✅ |
| TranslationCache | ✅ | ✅ | ✅ | ✅ |
| Language | ✅ | ✅ | ✅ | ✅ |
| Attachment | ✅ | ✅ | ✅ | ✅ |

---

## 2. Architecture Compliance

### Dependency Graph

```
Presentation → Application → Domain
      ↓              ↓
Infrastructure → Domain
      ↓
  Contracts (shared)
```

- **Domain**: Zero external dependencies. Pure entities, value objects, domain events, repository interfaces.
- **Application**: References Domain only. Uses MediatR, FluentValidation, HybridCache.
- **Infrastructure**: References Domain + Application. Implements repositories, EF Core, external services.
- **Presentation**: References Application only. Minimal API endpoints with MediatR dispatch.
- **Contracts**: Shared DTOs and integration events consumed by other modules.

### CQRS Pattern

- **Commands**: 33 command handlers across Attachment (4), Category (7), EntityCategory (2), EntityTag (2), Language (2), Specialization (2+), Tag (5+), Translation (5+)
- **Queries**: 13 query handlers for read operations
- **Validators**: 33 FluentValidation validators (one per command)

---

## 3. Endpoint Security Audit

### Full Endpoint Inventory (48 endpoints, 8 groups)

#### CategoryEndpoints.cs — 10 endpoints ✅ ALL PASS

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/categories` | AllowAnonymous |
| `GET` | `/categories/{id}` | AllowAnonymous |
| `GET` | `/categories/admin` | MustHavePermission(Category, Read) + RequireAuthorization |
| `GET` | `/categories/admin/{id}` | MustHavePermission(Category, Read) + RequireAuthorization |
| `POST` | `/categories` | MustHavePermission(Category, Create) + RequireAuthorization |
| `PUT` | `/categories/{id}` | MustHavePermission(Category, Update) + RequireAuthorization |
| `DELETE` | `/categories/{id}` | MustHavePermission(Category, Delete) + RequireAuthorization |
| `PATCH` | `/categories/{id}/deactivate` | MustHavePermission(Category, Update) + RequireAuthorization |
| `PATCH` | `/categories/{id}/activate` | MustHavePermission(Category, Update) + RequireAuthorization |
| `PATCH` | `/categories/{id}/restore` | MustHavePermission(Category, Update) + RequireAuthorization |
| `PUT` | `/categories/reorder` | MustHavePermission(Category, Update) + RequireAuthorization |

#### AttachmentEndpoints.cs — 7 endpoints ✅ ALL PASS

| Method | Route | Auth | Notes |
|--------|-------|------|-------|
| `POST` | `/attachments` | MustHavePermission(Attachment, Create) + RequireAuthorization | Uses ICurrentUser.UserId as uploadedById (ownership context) |
| `GET` | `/attachments?entityType&entityId` | MustHavePermission(Attachment, Read) + RequireAuthorization | |
| `GET` | `/attachments/{id}` | MustHavePermission(Attachment, Read) + RequireAuthorization | |
| `DELETE` | `/attachments/{id}` | MustHavePermission(Attachment, Delete) + RequireAuthorization | |
| `PUT` | `/attachments/reorder` | MustHavePermission(Attachment, Update) + RequireAuthorization | |
| `PUT` | `/attachments/primary` | MustHavePermission(EntityImage, Update) + RequireAuthorization | |
| `POST` | `/attachments/images` | MustHavePermission(Attachment, Create) + RequireAuthorization | Bulk upload, uses ICurrentUser.UserId |

#### TagEndpoints.cs — 7 endpoints ✅ ALL PASS

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/tags` | AllowAnonymous |
| `GET` | `/tags/{id}` | AllowAnonymous |
| `POST` | `/tags` | MustHavePermission(Tag, Create) |
| `PUT` | `/tags/{id}` | MustHavePermission(Tag, Update) |
| `DELETE` | `/tags/{id}` | MustHavePermission(Tag, Delete) |
| `PATCH` | `/tags/{id}/activate` | MustHavePermission(Tag, Update) |
| `PATCH` | `/tags/{id}/deactivate` | MustHavePermission(Tag, Update) |

#### SpecializationEndpoints.cs — 7 endpoints ✅ ALL PASS

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/specializations` | AllowAnonymous |
| `GET` | `/specializations/{id}` | AllowAnonymous |
| `POST` | `/specializations` | MustHavePermission(Specialization, Create) |
| `PUT` | `/specializations/{id}` | MustHavePermission(Specialization, Update) |
| `DELETE` | `/specializations/{id}` | MustHavePermission(Specialization, Delete) |
| `PATCH` | `/specializations/{id}/activate` | MustHavePermission(Specialization, Update) |
| `PATCH` | `/specializations/{id}/deactivate` | MustHavePermission(Specialization, Update) |

#### TranslationEndpoints.cs — 7 endpoints ✅ ALL PASS

| Method | Route | Auth |
|--------|-------|------|
| `POST` | `/translations/translate` | MustHavePermission(TranslationCache, Create) |
| `POST` | `/translations/batch` | MustHavePermission(TranslationCache, Create) |
| `GET` | `/translations/{entityType}/{entityId}` | AllowAnonymous |
| `PUT` | `/translations/{id}` | MustHavePermission(TranslationCache, Update) |
| `POST` | `/translations/{id}/approve` | MustHavePermission(TranslationCache, Update) |
| `POST` | `/translations/backfill/{entityKind}` | MustHavePermission(TranslationCache, Create) |
| `POST` | `/translations/approve-batch` | MustHavePermission(TranslationCache, Update) |

#### LanguageEndpoints.cs — 4 endpoints ⚠️ GAPS FOUND

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/languages` | AllowAnonymous |
| `GET` | `/languages/{id}` | AllowAnonymous |
| `POST` | `/languages` | MustHavePermission(Language, Create) |
| `PUT` | `/languages/{id}` | MustHavePermission(Language, Update) |
| ~~`DELETE`~~ | ~~`/languages/{id}`~~ | **MISSING** — Permission exists in catalog |
| ~~`PATCH`~~ | ~~`/languages/{id}/activate`~~ | **MISSING** — Pattern inconsistency |
| ~~`PATCH`~~ | ~~`/languages/{id}/deactivate`~~ | **MISSING** — Pattern inconsistency |

#### EntityCategoryEndpoints.cs — 3 endpoints ✅ ALL PASS

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/entity-categories` | AllowAnonymous |
| `POST` | `/entity-categories` | MustHavePermission(EntityCategory, Create) |
| `DELETE` | `/entity-categories` | MustHavePermission(EntityCategory, Delete) |

#### EntityTagEndpoints.cs — 3 endpoints ✅ ALL PASS

| Method | Route | Auth |
|--------|-------|------|
| `GET` | `/entity-tags` | AllowAnonymous |
| `POST` | `/entity-tags` | MustHavePermission(EntityTag, Create) |
| `DELETE` | `/entity-tags` | MustHavePermission(EntityTag, Delete) |

### Security Verdict

- **47/48 endpoints**: Correctly secured with `MustHavePermission` + `RequireAuthorization` or intentional `AllowAnonymous`.
- **0 auth violations** at the endpoint level.
- **1 dead permission**: `Language.Delete` exists in catalog but no endpoint consumes it.

---

## 4. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | MustHavePermission on every mutating endpoint | ✅ PASS | All 48 endpoints verified — public GETs use AllowAnonymous, all writes use MustHavePermission |
| 2 | ICurrentUser only for ownership checks | ❌ FAIL | 8 handlers use it for auth gates + role-based bypass (see Gap 1) |
| 3 | Result pattern everywhere | ⚠️ PARTIAL | Application layer: ✅ zero `throw new`. Infrastructure: ❌ 4 runtime throws (see Gap 2) |
| 4 | Per-module IPermissionCatalog | ✅ PASS | `ContentCorePermissionCatalog` with 10 feature groups |
| 5 | No SaveChanges in domain event handlers | ✅ PASS | All 12 handlers have explicit "no SaveChanges" comments, zero violations |
| 6 | DateTime.UtcNow (never DateTime.Now) | ✅ PASS | Zero violations in Application + Domain |
| 7 | Guid.CreateVersion7 (never Guid.NewGuid) | ✅ PASS | Zero violations in Application + Domain |
| 8 | Enums stored as int | ✅ PASS | All enums use integer storage |
| 9 | FluentValidation on every command | ✅ PASS | 33 validators covering all command types |
| 10 | HybridCache with tag invalidation | ✅ PASS | 20+ handlers use HybridCache with `RemoveByTagAsync` + `ContentCoreCacheKeys` |
| 11 | Integration events extend base | ✅ PASS | All 7 events extend `IntegrationEventBase` |
| 12 | Outbox pattern for events | ✅ PASS | `OutboxMessageConfiguration` present in persistence layer |

---

## 5. Gap 1 — ICurrentUser Misuse (HIGH)

**Severity**: HIGH  
**Impact**: 8 command handlers  
**Rule Violated**: *"ICurrentUser is ONLY for ownership checks. Never for auth gates or role decisions in handlers."* (agent-context.md §0, Non-Negotiable Rule #2)

### Affected Handlers

| # | Handler | Feature | File Path |
|---|---------|---------|-----------|
| 1 | `AssignTagsToEntityCommandHandler` | EntityTag | `Application/EntityTags/Commands/` |
| 2 | `RemoveTagFromEntityCommandHandler` | EntityTag | `Application/EntityTags/Commands/` |
| 3 | `AssignCategoriesToEntityCommandHandler` | EntityCategory | `Application/EntityCategories/Commands/` |
| 4 | `RemoveCategoryFromEntityCommandHandler` | EntityCategory | `Application/EntityCategories/Commands/` |
| 5 | `UploadAttachmentCommandHandler` | Attachment | `Application/Attachments/Commands/` |
| 6 | `DeleteAttachmentCommandHandler` | Attachment | `Application/Attachments/Commands/` |
| 7 | `SetPrimaryImageCommandHandler` | Attachment | `Application/Attachments/Commands/` |
| 8 | `ReorderAttachmentsCommandHandler` | Attachment | `Application/Attachments/Commands/` |

### Violation Pattern

All 8 handlers contain this identical 3-part pattern:

```csharp
// ❌ VIOLATION 1 — Auth gate in handler
// This check belongs at the endpoint level via .RequireAuthorization()
// The endpoint ALREADY has RequireAuthorization, making this redundant AND misplaced.
if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    return Result.Failure<...>(Errors.Unauthorized);

// ❌ VIOLATION 2 — Role-based authorization in handler
// Admin bypass logic should be a custom authorization policy or endpoint filter.
// Handlers should be role-agnostic.
var highestRole = AppRoles.HighestPrivilegeLevel(currentUser.Roles);
if (highestRole >= RolePrivilegeLevel.Admin)
{
    // Skip ownership check entirely — admin can operate on any entity
}

// ✅ ACCEPTABLE — Ownership check (this is the ONLY valid use of ICurrentUser)
var ownership = await entityOwnershipResolver.ResolveAsync(...);
if (ownership.OwnerUserId != currentUser.UserId.Value)
    return Result.Failure<...>(Errors.Forbidden);
```

### Why This Is Wrong

1. **Redundant auth gate**: Every endpoint already has `.RequireAuthorization()` + `MustHavePermission(...)`. An unauthenticated request will never reach the handler. This is dead code that creates a false sense of security.
2. **Role logic in wrong layer**: The Application layer should not know about admin privilege levels. This creates tight coupling between business logic and authorization policy.
3. **Inconsistency risk**: If the admin-bypass rules change, you must update 8+ handlers instead of one policy.

### Good Pattern (What It Should Look Like)

The `IEntityOwnershipResolver` design is excellent — the ONLY thing that should remain:

```csharp
// Handler should ONLY contain:
var ownership = await entityOwnershipResolver.ResolveAsync(entityType, entityId, cancellationToken);
if (ownership.OwnerUserId != currentUser.UserId.Value)
    return Result.Failure<...>(Errors.Forbidden);
```

### Required Fix

1. **Remove** the `IsAuthenticated` / `UserId is null` check from all 8 handlers (endpoint auth already covers this).
2. **Extract** the admin-tier bypass into a custom `IAuthorizationHandler` or endpoint filter that can be applied declaratively.
3. **Keep** only the ownership comparison in the handler.

---

## 6. Gap 2 — Exception Throwing in Infrastructure (MEDIUM)

**Severity**: MEDIUM  
**Impact**: 4 runtime violations in 2 files  
**Rule Violated**: *"No throwing exceptions in runtime code — use Result pattern."* (agent-context.md §4)

### Violations

| # | File | Line | Exception Thrown | Context | Recommended Fix |
|---|------|------|-----------------|---------|----------------|
| 1 | `AzureTranslateService.cs` | 80 | `HttpRequestException` | Azure Translate API returns error HTTP status | Change method signature to return `Result<T>`. Return `Result.Failure` with HTTP status + error body |
| 2 | `AzureTranslateService.cs` | 85 | `InvalidOperationException` | Azure Translate API returns null response body | Return `Result.Failure` with descriptive error: "Translation API returned empty response" |
| 3 | `LocalFileStorageService.cs` | 40 | `ArgumentException` | Empty file stream passed to upload | Add FluentValidation rule on the command. Guard can remain but should return `Result.Failure` |
| 4 | `LocalFileStorageService.cs` | 43 | `ArgumentException` | Empty filename passed to upload | Add FluentValidation rule on the command. Guard can remain but should return `Result.Failure` |

### Acceptable Throws (NOT violations)

These are **startup-time config guards** that correctly fail fast during DI registration:

| File | Line | Exception | Why Acceptable |
|------|------|-----------|---------------|
| `DependencyInjection.cs` | 33 | `InvalidOperationException` | Missing connection string — app can't start |
| `AzureTranslateService.cs` | 38 | `InvalidOperationException` | Missing Azure Translate API key — service unusable |
| `AzureTranslateService.cs` | 40 | `InvalidOperationException` | Missing Azure Translate region — service unusable |

### Borderline Case

| File | Line | Exception | Assessment |
|------|------|-----------|-----------|
| `ContentCoreUnitOfWork.cs` | 19 | `DbUpdateConcurrencyException` | This is EF Core's built-in optimistic concurrency mechanism. The UoW re-throws it for the caller to handle. Consider wrapping in `Result.Failure(ConcurrencyConflict)` to fully align with the Result pattern, but current approach is functionally correct. |

### Impact

When these infrastructure services throw at runtime, the exception bubbles up through MediatR and hits the global exception handler, which returns a 500. This means:
- **AzureTranslateService**: A translation API outage returns 500 instead of a graceful error.
- **LocalFileStorageService**: A malformed upload returns 500 instead of 400 validation error.

---

## 7. Gap 3 — Missing Language Endpoints (LOW)

**Severity**: LOW  
**Impact**: Pattern inconsistency + 1 dead permission

### The Inconsistency

Every ContentCore entity follows this endpoint pattern:

| Operation | Category | Tag | Specialization | Language |
|-----------|----------|-----|----------------|----------|
| List (public) | ✅ GET | ✅ GET | ✅ GET | ✅ GET |
| Get by ID (public) | ✅ GET | ✅ GET | ✅ GET | ✅ GET |
| Create | ✅ POST | ✅ POST | ✅ POST | ✅ POST |
| Update | ✅ PUT | ✅ PUT | ✅ PUT | ✅ PUT |
| Delete | ✅ DELETE | ✅ DELETE | ✅ DELETE | ❌ **MISSING** |
| Activate | ✅ PATCH | ✅ PATCH | ✅ PATCH | ❌ **MISSING** |
| Deactivate | ✅ PATCH | ✅ PATCH | ✅ PATCH | ❌ **MISSING** |

### Dead Permission

`ContentCorePermissionCatalog` defines `Language.Delete` (Read/Create/Update/Delete), but no endpoint uses the Delete permission. This means:
- The permission is seeded to the database but serves no purpose.
- Admin UI may show a "Delete Language" permission that does nothing.

### Current Workaround

`PUT /languages/{id}` accepts an `isActive` boolean parameter — functionally equivalent to activate/deactivate but breaks the PATCH convention used everywhere else.

### Options

| Option | Pros | Cons |
|--------|------|------|
| **A: Add the missing endpoints** | Full pattern consistency. Delete permission becomes functional. | More code for a rarely-used feature. |
| **B: Remove the dead permission + document** | Less code. Explicit decision. | Pattern remains inconsistent. |
| **Recommended**: Option A | Consistency is a project value. Languages are reference data — delete/activate/deactivate will be needed for admin operations. | |

---

## 8. What Passed — Full Checklist

These areas were audited and found fully compliant:

### Endpoint Authorization (48/48)
- All mutating endpoints use `MustHavePermission(Feature, Action)`.
- All public read endpoints use `AllowAnonymous` intentionally.
- No endpoint is missing auth decoration.

### Domain Event Handlers (12/12)
- Zero `SaveChangesAsync()` calls.
- Every handler has explicit comment: *"no SaveChanges — UoW commits atomically"*.
- Handlers: category created/updated/deleted/restored, attachment uploaded/deleted, entity-category assigned, specialization created/updated/deleted, tag created/updated.

### Result Pattern in Application Layer
- Zero `throw new` in the entire Application project.
- All handlers consistently return `Result.Success(...)` or `Result.Failure(...)`.
- Error types are well-defined and descriptive.

### DateTime/GUID Conventions
- Zero `DateTime.Now` or `DateTime.Today` in Application + Domain layers.
- Zero `Guid.NewGuid()` in Application + Domain layers.
- All use `DateTime.UtcNow` and `Guid.CreateVersion7()`.

### HybridCache Implementation
- 20+ handlers inject `HybridCache` directly.
- All use `RemoveByTagAsync` with `ContentCoreCacheKeys` tag constants.
- Cache invalidation is consistently applied on create/update/delete operations.

### Outbox Pattern
- `OutboxMessageConfiguration` present in persistence layer.
- Integration events are dispatched through the outbox for reliable cross-module communication.

### FluentValidation
- 33 validators covering all command types.
- Validators are registered per-module via DI.

### Cross-Module Design
- `IEntityOwnershipResolver` pattern for cross-module ownership lookup is clean DDD.
- Integration events use `IntegrationEventBase` for module decoupling.
- Contracts project properly isolates shared types.

---

## 9. Scorecard

| Area | Score | Notes |
|------|-------|-------|
| Endpoint Authorization | **9.5/10** | All 48 endpoints secured. 1 dead permission (Language.Delete). |
| Result Pattern | **8.5/10** | Application layer: 10/10. Infrastructure: 4 runtime throws. |
| Domain Event Safety | **10/10** | Zero SaveChanges violations across 12 handlers. |
| ICurrentUser Discipline | **5/10** | 8 handlers embed auth gates + role decisions. Ownership check correct. |
| Convention Compliance | **10/10** | DateTime.UtcNow, Guid.CreateVersion7, naming — all clean. |
| Caching | **10/10** | HybridCache with tag-based invalidation consistently applied. |
| Permission Catalog | **9/10** | Complete. 1 dead permission. |
| Validation | **10/10** | 33 validators, one per command. |
| Cross-Module Design | **10/10** | IEntityOwnershipResolver, integration events, contracts — excellent. |
| Test Coverage | **7/10** | 19 test files for 46 handlers. Decent but not exhaustive. |
| | | |
| **Overall** | **8.5/10** | Solid module. ICurrentUser misuse is the primary structural debt. |

---

## 10. Fix Priority & Recommendations

### Priority 1 — HIGH (Do Next)

**Refactor 8 ICurrentUser handlers**
- Remove redundant `IsAuthenticated` / `UserId is null` checks.
- Extract admin-tier bypass to custom authorization policy or endpoint filter.
- Keep only `ownership.OwnerUserId != currentUser.UserId.Value` in handlers.
- Estimated effort: ~2 hours (pattern is identical across all 8).

### Priority 2 — MEDIUM (This Sprint)

**Wrap infrastructure runtime throws in Result pattern**
- `AzureTranslateService.cs` lines 80, 85 — return `Result.Failure` instead of throwing.
- `LocalFileStorageService.cs` lines 40, 43 — add FluentValidation upstream + return `Result.Failure`.
- Consider wrapping `ContentCoreUnitOfWork.cs` concurrency exception.
- Estimated effort: ~1 hour.

### Priority 3 — LOW (Backlog)

**Language endpoint consistency**
- Add `DELETE /languages/{id}`, `PATCH /languages/{id}/activate`, `PATCH /languages/{id}/deactivate`.
- Or remove `Language.Delete` from permission catalog if delete is intentionally unsupported.
- Estimated effort: ~30 minutes.

### Priority 4 — LOW (Backlog)

**Increase test coverage**
- 19 test files for 46 handlers (41% file coverage).
- Priority test targets: the 8 ICurrentUser handlers (after refactor), translation batch operations, attachment bulk upload.
- Estimated effort: ~4 hours for meaningful coverage increase.

---

## Appendix — Files Audited

### Endpoint Files (All 8 — 100% coverage)
- `CategoryEndpoints.cs` (222 lines, 10 endpoints)
- `AttachmentEndpoints.cs` (209 lines, 7 endpoints)
- `TagEndpoints.cs` (116 lines, 7 endpoints)
- `SpecializationEndpoints.cs` (120 lines, 7 endpoints)
- `TranslationEndpoints.cs` (126 lines, 7 endpoints)
- `LanguageEndpoints.cs` (75 lines, 4 endpoints)
- `EntityCategoryEndpoints.cs` (61 lines, 3 endpoints)
- `EntityTagEndpoints.cs` (61 lines, 3 endpoints)

### Handler Files Inspected
- `RemoveTagFromEntityCommandHandler.cs` (131 lines — exemplar for ICurrentUser pattern)
- All 8 ICurrentUser-using handlers verified via grep + targeted reads

### Infrastructure Files Inspected
- `AzureTranslateService.cs` (throw analysis)
- `LocalFileStorageService.cs` (throw analysis)
- `ContentCoreUnitOfWork.cs` (throw analysis)
- `DependencyInjection.cs` (throw analysis)
- All 12 domain event handlers (SaveChanges audit)

### Configuration Files Inspected
- `ContentCorePermissionCatalog.cs`
- `ContentCoreFeatures.cs`
- `ContentCoreDbContext` + 13 EF configurations
