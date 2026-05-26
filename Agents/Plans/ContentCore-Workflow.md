# ContentCore Workflow Plan

> **Module**: ContentCore (shared foundation — attachments, categories, tags, specializations, languages, translations)
> **Status**: Implemented (audited 2025-01-27, score 9.2/10). All 12 fixes applied (W4-A).
> **Compatible with**: All 11 existing plans (BlogCreatorPost-Merger, Booking-Workflow, ContentPlaces-Workflow, ContentSeo-Workflow, Finance-Workflow, Messaging-Workflow, Analytics-Workflow, Platform-Onboarding-Workflow, Role-System, Social-Workflow, TourGuide-Flow)

---

## Design Decisions (9 — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **No CreatorProfile EntityType** | Creator profile media (avatar, cover) stays as direct string fields on CreatorProfile. Blog content uses existing `EntityType.Blog=4`. |
| 2 | **OwnershipGuard kept as-is** | Admin-tier bypass stays centralized in `OwnershipGuard.AuthorizeAsync()`. No extraction to endpoint filter. Already clean — 0 raw auth gates found in handlers. |
| 3 | **Self-service tag assignment** | Providers/creators can assign existing active tags to entities they own. Admin manages tag catalog (create/update/delete). OwnershipGuard handles authorization. |
| 4 | **Self-service category assignment** | Same pattern as tags — providers/creators assign active categories to own entities. Admin manages category catalog. |
| 5 | **Centralized attachment limits** | Hardcoded constants per EntityType + per AttachmentType. Count limits (Review=5, Blog=20, Tour=30, Place=30, Business=20, TourGuide=10) + file size limits (Image=10MB, Video=500MB, Document=25MB, Audio=50MB). |
| 6 | **Keep Azure Translate** | AzureTranslateService stays for MVP. Already behind `ITranslationService` interface. Abstract to `ITranslationProvider` later if needed. |
| 7 | **Add all 3 Language endpoints** | DELETE (soft-delete) + PATCH activate + PATCH deactivate. Full pattern consistency with Tag/Category/Specialization. |
| 8 | **Add EntityTag integration events** | New `EntityTagAssignedIntegrationEvent` + `EntityTagRemovedIntegrationEvent` for cross-module reactions (Analytics recommendations, SEO metadata refresh). |
| 9 | **Specialization stays guide-only** | No EntityType polymorphism for specializations. Businesses use Categories for classification. |

---

## Current State Summary

### What's Built (COMPLETE)
- 12 entities: Attachment, Category, CategoryTranslation, EntityCategory, EntityImage, EntityTag, Language, Specialization, SpecializationTranslation, Tag, TagTranslation, TranslationCache
- 4 enums: AttachmentType (Image/Video/Document/Audio), EntityType (Place=0..TourGuide=5), ImageSize, TranslationStatus
- 52 endpoints across 8 groups, all correctly secured
- OwnershipGuard pattern centralizes admin bypass + ownership check (CLEAN)
- 33 validators, 49 handlers, 12 domain event handlers
- Integration events: 12 (Attachment×2, Category×4, EntityCategory×2, EntityTag×2, Language×2)
- HybridCache with tag-based invalidation consistently applied
- Background jobs: MediaProcessingBackgroundService, MediaProcessingQueue
- File signature detection (magic bytes) in UploadAttachmentCommandHandler

### Gap Assessment (from audit)
| Gap | Status | Action |
|-----|--------|--------|
| Gap 1 — ICurrentUser Misuse | ✅ **ALREADY RESOLVED** | Zero raw auth gates. All 8 handlers use OwnershipGuard. |
| Gap 2 — Infrastructure Throws | ✅ **RESOLVED** | AzureTranslateService methods now return Result<T>. LocalFileStorageService already fixed. |
| Gap 3 — Missing Language Endpoints | ✅ **RESOLVED** | DELETE + activate + deactivate endpoints implemented with validators |

---

## EntityType Registry (Cross-Module Reference)

```csharp
public enum EntityType : byte
{
    Place = 0,      // ContentPlaces
    Tour = 1,       // ContentTours
    Business = 2,   // ContentPlaces
    Review = 3,     // Social
    Blog = 4,       // ContentBlogs (admin + creator content)
    TourGuide = 5   // ContentTours (guide profiles)
}
```

No new values needed. CreatorProfile uses direct fields for avatar/cover.

---

## Attachment Limits (New — Decision #5)

### Constants Class

```csharp
public static class AttachmentLimits
{
    // Max attachments per EntityType
    public static int GetMaxCount(EntityType entityType) => entityType switch
    {
        EntityType.Place => 30,
        EntityType.Tour => 30,
        EntityType.Business => 20,
        EntityType.Review => 5,
        EntityType.Blog => 20,
        EntityType.TourGuide => 10,
        _ => 10
    };

    // Max file size in bytes per AttachmentType
    public static long GetMaxFileSize(AttachmentType type) => type switch
    {
        AttachmentType.Image => 10 * 1024 * 1024,      // 10 MB
        AttachmentType.Video => 500 * 1024 * 1024,     // 500 MB
        AttachmentType.Document => 25 * 1024 * 1024,   // 25 MB
        AttachmentType.Audio => 50 * 1024 * 1024,      // 50 MB
        _ => 10 * 1024 * 1024
    };
}
```

### Enforcement Point
- **Count check**: In `UploadAttachmentCommandHandler` — query existing count for (EntityType, EntityId), reject if >= max.
- **File size check**: In `UploadAttachmentCommandValidator` — `FileSize.LessThanOrEqualTo(AttachmentLimits.GetMaxFileSize(type))`.

---

## Self-Service Tagging & Categorization (Decisions #3, #4)

### Current State
- `EntityTagEndpoints`: `MustHavePermission(EntityTag, Create/Delete)` — admin-only
- `EntityCategoryEndpoints`: `MustHavePermission(EntityCategory, Create/Delete)` — admin-only

### Target State
Providers/creators can assign/remove tags and categories to entities THEY OWN. The OwnershipGuard already handles this — handlers call `ownershipGuard.AuthorizeAsync()`. The endpoint auth just needs to allow Provider/Creator/TourGuide roles.

### Implementation
1. Keep existing admin endpoints unchanged (they still work for admin)
2. Add parallel "self-service" endpoints OR relax permission to allow any authenticated user (OwnershipGuard is the real gate)

**Recommended approach**: Lower the permission gate — keep `RequireAuthorization()` but remove `MustHavePermission` since OwnershipGuard handles the real authorization. Admin can manage any entity; non-admin can only manage their own.

Alternative: Keep `MustHavePermission` but ensure Provider/Creator/TourGuide roles get the EntityTag.Create/Delete and EntityCategory.Create/Delete permissions in the role seed.

**Decision**: Use the alternative — assign `EntityTag.Create/Delete` and `EntityCategory.Create/Delete` permissions to Provider, Creator, TourGuide roles via seed. Endpoint auth stays unchanged. OwnershipGuard ensures non-admins only operate on their own entities.

---

## New Integration Events (Decision #8)

```csharp
// ContentCore.Contracts/IntegrationEvents/
public sealed record EntityTagAssignedIntegrationEvent(
    string EntityType,     // "Tour", "Blog", etc.
    Guid EntityId,
    Guid TagId,
    DateTime AssignedAt) : IntegrationEventBase;

public sealed record EntityTagRemovedIntegrationEvent(
    string EntityType,
    Guid EntityId,
    Guid TagId,
    DateTime RemovedAt) : IntegrationEventBase;
```

### Consumers (in other modules)
- **Analytics**: Update recommendation signals when tags change
- **ContentSeo**: Refresh schema markup / sitemap when entity tags updated

---

## Execution Phases

### Phase 1 — Infrastructure Throw Fixes (Gap 2)

**Files to modify (2):**

1. `ContentCore.Infrastructure/Services/AzureTranslateService.cs`
   - Line 80: `response.EnsureSuccessStatusCode()` → wrap in try/catch, return `Result.Failure` with HTTP status
   - Line 85: `throw new InvalidOperationException` → return `Result.Failure` with "empty response" error
   - Change method signatures: `Task<string>` → `Task<Result<string>>`

2. `ContentCore.Infrastructure/Services/LocalFileStorageService.cs`
   - Line 40: `throw new ArgumentException("Empty stream")` → return `Result.Failure` with validation error
   - Line 43: `throw new ArgumentException("Empty filename")` → return `Result.Failure` with validation error
   - Change method signatures to return Result pattern

**Callers to update**: Any handler/service calling these methods must handle the Result.

### Phase 2 — Language Endpoint Completion (Gap 3)

**New files (6):**
- `ContentCore.Application/Commands/Language/ActivateLanguage/ActivateLanguageCommand.cs`
- `ContentCore.Application/Commands/Language/ActivateLanguage/ActivateLanguageCommandHandler.cs`
- `ContentCore.Application/Commands/Language/DeactivateLanguage/DeactivateLanguageCommand.cs`
- `ContentCore.Application/Commands/Language/DeactivateLanguage/DeactivateLanguageCommandHandler.cs`
- `ContentCore.Application/Commands/Language/DeleteLanguage/DeleteLanguageCommand.cs`
- `ContentCore.Application/Commands/Language/DeleteLanguage/DeleteLanguageCommandHandler.cs`

**Modified files (1):**
- `ContentCore.Presentation/Endpoints/Language/LanguageEndpoints.cs` — add 3 endpoints:
  - `DELETE /languages/{id}` → MustHavePermission(Language, Delete)
  - `PATCH /languages/{id}/activate` → MustHavePermission(Language, Update)
  - `PATCH /languages/{id}/deactivate` → MustHavePermission(Language, Update)

### Phase 3 — Attachment Limits

**New files (1):**
- `ContentCore.Application/Limits/AttachmentLimits.cs` — static constants class

**Modified files (2):**
- `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandHandler.cs` — add count check before upload:
  ```csharp
  var existingCount = await attachmentRepository.CountByEntityAsync(request.EntityType, request.EntityId, ct);
  if (existingCount >= AttachmentLimits.MaxCount(request.EntityType))
      return Result.Failure<...>(new Error("Attachment.LimitReached", $"Maximum of {max} attachments..."));
  ```
- `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandValidator.cs` — add file size validation

**Repository (already exists):**
- `IAttachmentRepository.CountByEntityAsync(EntityType, Guid entityId, CancellationToken)` — already implemented

### Phase 4 — Self-Service Tagging/Categorization

**Modified files (role seed only):**
- Role permission seed (in Security module) — assign `EntityTag.Create`, `EntityTag.Delete`, `EntityCategory.Create`, `EntityCategory.Delete` to Provider, Creator, TourGuide roles

No code changes in ContentCore handlers — OwnershipGuard already handles non-admin users correctly (ownership check). The only blocker was PERMISSION — non-admin users didn't have the permission to reach the handler.

### Phase 5 — Integration Events for Tag Assignment

**New files (2):**
- `ContentCore.Contracts/IntegrationEvents/EntityTagAssignedIntegrationEvent.cs`
- `ContentCore.Contracts/IntegrationEvents/EntityTagRemovedIntegrationEvent.cs`

**Modified files (2):**
- `ContentCore.Application/Commands/EntityTag/AssignTagsToEntity/AssignTagsToEntityCommandHandler.cs` — publish `EntityTagAssignedIntegrationEvent` after successful save
- `ContentCore.Application/Commands/EntityTag/RemoveTagFromEntity/RemoveTagFromEntityCommandHandler.cs` — publish `EntityTagRemovedIntegrationEvent` after successful save

### Phase 6 — Solution Build + Verify

- Build entire solution
- Fix any test compilation errors
- Verify all 52 endpoints work
- Verify 0 remaining raw auth gates

---

## File Impact Summary

| Category | Count |
|----------|-------|
| New files | ~11 |
| Modified files | ~8 |
| Deleted files | 0 |
| **Total** | ~19 |

**Estimated effort**: ~6-8 hours

---

## Cross-Plan Compatibility

| Plan | ContentCore Impact |
|------|-------------------|
| BlogCreatorPost-Merger | Blog=4 in EntityType. Creators get EntityTag/EntityCategory permissions via role seed. Attachment limits: Blog=20. |
| TourGuide-Flow | TourGuide=5 in EntityType. Guides get EntityTag/EntityCategory permissions. Attachment limits: TourGuide=10. Specialization stays guide-only. |
| Social-Workflow | Review=3 in EntityType. Attachment limits: Review=5 (max 5 photos). EntityTagAssigned event enables analytics refresh. |
| ContentPlaces-Workflow | Place=0, Business=2. Providers get EntityTag/EntityCategory permissions. Attachment limits: Place=30, Business=20. |
| ContentSeo-Workflow | Consumes EntityTagAssigned/Removed events to refresh SEO metadata. |
| Analytics-Workflow | Consumes EntityTagAssigned/Removed events to update recommendation signals. |
| Finance-Workflow | No impact. |
| Messaging-Workflow | No impact. |
| Booking-Workflow | No impact. |
| Platform-Onboarding-Workflow | Post-approval role seed includes EntityTag/EntityCategory permissions for Provider, TourGuide, Creator. |
| Role-System | Role seed modified to give Provider/TourGuide/Creator the self-service permissions. |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| AzureTranslateService signature change breaks callers | grep all callers, update to handle Result<string> |
| Attachment count query adds DB round-trip to uploads | Single COUNT query is negligible vs file upload I/O |
| Self-service tag assignment could be abused | OwnershipGuard ensures entity ownership. Tag catalog stays admin-only. Tags must be active. |
| EntityTag events create outbox volume | Events only on assign/remove (low frequency). Same pattern as EntityCategory events. |

---

## Implementation Notes

> Added during codebase audit — reflects actual implementation details not originally in the plan.

1. **Actual endpoint count**: 52 (not 48) — Language has 7 endpoints (including Activate/Deactivate), Specialization has 7, Tags has 7.
2. **Actual handler count**: 49 command handlers + 13 query handlers (not 46 total).
3. **Actual integration event count**: 12 (not 10) — Language: Activated, Deactivated (2 added).
4. **AttachmentLimits.GetMaxFileSize()**: Added in W4-A to unify validator file sizes with canonical limits (Video=500MB, Document=25MB).
5. **DetectLanguageAsync / GetSupportedLanguagesAsync**: Both return `Result<T>` (not raw values) — interface + impl + decorator all updated.
6. **Creator/TourGuide Delete permission**: Added `AppAction.Delete` for EntityTag/EntityCategory in `RolePermissionMapping.cs`.
7. **Language validators**: `ActivateLanguageCommandValidator`, `DeactivateLanguageCommandValidator`, `DeleteLanguageCommandValidator` added.
8. **Category.ChangeParent self-parent guard**: `BusinessRuleViolationException` thrown if parentCategoryId == Id.
9. **TranslationCache text columns**: `HasMaxLength(4000)` added to OriginalText and TranslatedText.
10. **Category.ParentCategoryId index**: `IX_Categories_ParentCategoryId` added in EF config.
11. **Attachment.MarkForDeletion**: Idempotency guard added + `IsMarkedForDeletion` property (requires EF migration).
12. **Tag/Specialization idempotency**: Activate/Deactivate now guard against already-active/inactive state.
