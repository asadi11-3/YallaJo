# ContentCore — Fix Plan

> **Source**: `ContentCore-Audit-Report.md` findings (revised 2025-07-18, third-pass 2025-07-18)
> **Status**: Plan locked — refined with third-pass audit findings (domain + infrastructure layers)
> **Estimated effort**: ~5-6 hours (original 3-4h + domain/infrastructure fixes)
> **Compatible with**: All existing plans (no new cross-module conflicts)
> **Prior gaps**: All 8 actionable gaps from `ContentCore-gap-fix-plan.md` confirmed resolved

---

## Design Decisions (6 — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **Single source of truth for file sizes** | `AttachmentLimits.MaxFileSize()` is canonical. Validator must reference it, not duplicate constants. |
| 2 | **Creator/TourGuide get Delete permission** | For EntityTag and EntityCategory. Matches original Decision #3 intent. OwnershipGuard still prevents cross-user deletion. |
| 3 | **DetectLanguage/GetSupportedLanguages return Result** | Same pattern as `TranslateAsync`/`BatchTranslateAsync`. No throws from infrastructure services. |
| 4 | **Domain methods must be idempotent** | Activate/Deactivate/MarkForDeletion should guard against duplicate calls. No duplicate domain events. |
| 5 | **Category hierarchy requires self-parent guard minimum** | Full cycle detection deferred to application layer. Domain entity guards `id == parentId` only. |
| 6 | **TranslationCache text columns bounded** | `nvarchar(4000)` for both `OriginalText` and `TranslatedText`. Prevents unbounded growth + enables future indexing. |

---

## Execution Phases

### Fix 1 — Attachment Limit Dual Source of Truth (CRITICAL)

**Problem**: `UploadAttachmentCommandValidator.cs` has hardcoded file size constants (Video=200MB, Document=20MB) that conflict with `AttachmentLimits.cs` (Video=500MB, Document=25MB). Users are rejected at the wrong thresholds.

**Files to modify (1):**

1. `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandValidator.cs`
   - Remove all inline file-size constants
   - Replace with calls to `AttachmentLimits.MaxFileSize(attachmentType)`
   - The validator already knows the `AttachmentType` from the command — wire it through

**Important**: `AttachmentLimits.cs` currently has `GetMaxCount()` and `IsAtLimit()` but **no `MaxFileSize()` method**. This fix requires TWO changes:

**Step 1** — Add `MaxFileSize()` to `AttachmentLimits.cs`:
```csharp
public static long MaxFileSize(AttachmentType type) => type switch
{
    AttachmentType.Image => MaxImageFileSize,
    AttachmentType.Video => MaxVideoFileSize,
    AttachmentType.Document => MaxDocumentFileSize,
    AttachmentType.Audio => MaxAudioFileSize,
    _ => MaxImageFileSize // safe default
};
```

**Step 2** — Refactor `UploadAttachmentCommandValidator.cs`:
```csharp
// BEFORE (wrong — dual source of truth)
RuleFor(x => x.FileSize)
    .LessThanOrEqualTo(200 * 1024 * 1024)  // hardcoded 200MB for video
    .When(x => x.AttachmentType == AttachmentType.Video);

// AFTER (correct — single source of truth)
RuleFor(x => x.FileSize)
    .Must((cmd, fileSize) => fileSize <= AttachmentLimits.MaxFileSize(cmd.AttachmentType))
    .WithMessage(cmd => $"File size exceeds the maximum of {AttachmentLimits.MaxFileSize(cmd.AttachmentType) / (1024 * 1024)} MB for {cmd.AttachmentType}");
```

**Files to modify (2, not 1)**:
1. `ContentCore.Application/Limits/AttachmentLimits.cs` — Add `MaxFileSize()` method
2. `ContentCore.Application/Commands/Attachment/UploadAttachment/UploadAttachmentCommandValidator.cs` — Remove hardcoded constants, use `AttachmentLimits.MaxFileSize()`

**Verification**: Unit test or manual check — upload a 300MB video file. Before fix: rejected. After fix: accepted.

---

### Fix 2 — AzureTranslateService Result Wrapping (HIGH)

**Problem**: `DetectLanguageAsync()` and `GetSupportedLanguagesAsync()` still throw on HTTP failures via `EnsureSuccessStatusCode()`. Every other method in the service already returns `Result<T>`.

**Files to modify (3):**

1. `ContentCore.Application/Interfaces/ITranslationService.cs` — **Interface signatures must change** (breaking change within module)
2. `ContentCore.Infrastructure/Services/AzureTranslateService.cs` — Implementation
3. `ContentCore.Infrastructure/Services/AutoSaveTranslationService.cs` — Decorator (forwards calls to inner service)

**Method 1 — `DetectLanguageAsync()` (~line 128)**:
- Current signature: `Task<string>`
- New signature: `Task<Result<string>>`
- Wrap `EnsureSuccessStatusCode()` in try-catch
- Return `Result<string>.Failure(new Error("Translation.DetectFailed", ...))` on failure
- Return `Result<string>.Success(detectedLanguage)` on success

**Method 2 — `GetSupportedLanguagesAsync()` (~line 140)**:
- Current signature: `Task<IReadOnlyList<SupportedLanguage>>`
- New signature: `Task<Result<IReadOnlyList<SupportedLanguage>>>`
- Same try-catch wrap pattern
- Return `Result.Failure` / `Result.Success`

**Callers to update**: `AutoSaveTranslationService` is the only known caller (decorator pattern — forwards directly). Grep solution-wide to confirm no others.

**Verification**: Build succeeds. No `EnsureSuccessStatusCode()` calls remain in `AzureTranslateService.cs`.

---

### Fix 3 — Creator/TourGuide Permission Gap (HIGH)

**Problem**: Creator and TourGuide roles only get Read + Create for ContentManagement permissions. They cannot delete EntityTags or EntityCategories from their own entities, contradicting Decision #3 (self-service tagging).

**Files to modify (1):**

1. `Security.Infrastructure/Seeding/RolePermissionMapping.cs` — the runtime role-to-permission mapper

**What to change**:
- Creator role: Add `AppAction.Delete` for `PermissionGroup.ContentManagement` (currently only has Read + Create)
- TourGuide role: Add `AppAction.Delete` for `PermissionGroup.ContentManagement` (currently only has Read + Create)

**Alternative (more surgical)**: If ContentManagement Delete is too broad (would also grant Delete on other ContentManagement features), create a separate permission group or add feature-specific overrides for EntityTag and EntityCategory only. Review other features under ContentManagement group before deciding.

**Permission system context**:
- `ContentCorePermissionCatalog.cs` registers EntityTag/EntityCategory under `PermissionGroup.ContentManagement`
- `SecurityDataSeeder.cs` aggregates catalogs and writes claims at startup
- `SecurityDbInitializer` is legacy — NOT the active system
- Runtime-seeded (no migration needed)

**Security note**: This is safe because:
- OwnershipGuard runs in every handler — non-admins can only operate on entities they own
- The tag/category catalog (create/update/delete tags themselves) remains admin-only
- This only allows assigning/removing tags FROM entities, not managing the tag catalog
- EntityTag/EntityCategory catalog has NO Update action (only Read, Create, Delete)

**Verification**: 
- Creator can call `DELETE /entity-tags/{entityId}/{tagId}` for their own entity
- Creator is still blocked from calling `DELETE /tags/{id}` (catalog management)
- TourGuide same pattern

---

### Fix 4 — Missing Language Command Validators (MEDIUM)

**Problem**: `ActivateLanguageCommand`, `DeactivateLanguageCommand`, and `DeleteLanguageCommand` have no validators. Commands accept a `Guid Id` with no validation.

**New files (3):**

1. `ContentCore.Application/Commands/Language/ActivateLanguage/ActivateLanguageCommandValidator.cs`
2. `ContentCore.Application/Commands/Language/DeactivateLanguage/DeactivateLanguageCommandValidator.cs`
3. `ContentCore.Application/Commands/Language/DeleteLanguage/DeleteLanguageCommandValidator.cs`

**Each validator**:
```csharp
public sealed class [Command]Validator : AbstractValidator<[Command]>
{
    public [Command]Validator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Language ID is required.");
    }
}
```

**Verification**: Send request with `Guid.Empty` — should get 400 validation error, not 404.

---

### Fix 5 — Solution Build + Verify (MEDIUM)

**Problem**: Phase 6 of the original plan was never verified.

**Steps**:
1. `dotnet build` the full solution — zero errors
2. `dotnet test` — all existing tests pass
3. Verify all 52 endpoints are registered across 8 groups
4. Grep for remaining `EnsureSuccessStatusCode()` in ContentCore — should be zero after Fix 2

---

### Fix 6 — Update Plan Document (LOW)

**Problem**: `ContentCore-Workflow.md` has 5 inaccuracies identified in the audit.

**File to modify (1):**

1. `Agents/Plans/ContentCore-Workflow.md`

**Changes**:
- Phase 1: Fix line references from "Line 80, 85" to actual locations (~128, ~140)
- Phase 2: Add note about validators needed for 3 new commands
- Phase 5: Fix event signature from `Guid[] TagIds` to `Guid TagId`
- Phase 4: Clarify that the mechanism is catalog-based dynamic mapping, not static seed
- File Impact Summary: Update counts to reflect actual state
- Gap Assessment table: Update statuses to reflect current completion

---

### Fix 7 — Category.ChangeParent Validation (MEDIUM, Third Pass)

**Problem**: `Category.ChangeParent(Guid? parentId)` blindly sets `ParentCategoryId` with zero validation. A category can be set as its own parent, creating a self-referencing row. Deeper cycles (A→B→A) are also possible. No max depth constraint exists.

**Files to modify (1):**

1. `ContentCore.Domain/Entities/Category.cs` — `ChangeParent()` method (line ~151)

**What to change**:
```csharp
// BEFORE
public void ChangeParent(Guid? parentCategoryId)
{
    ParentCategoryId = parentCategoryId;
}

// AFTER
public void ChangeParent(Guid? parentCategoryId)
{
    if (parentCategoryId.HasValue && parentCategoryId.Value == Id)
        throw new DomainException("A category cannot be its own parent.");

    ParentCategoryId = parentCategoryId;
}
```

**Note**: Full cycle detection (A→B→A) requires loading the tree — best done at the handler/application layer, not the entity. The self-parent guard is the critical minimum.

**Verification**: Attempt to set a category's parent to itself — should get domain exception.

---

### Fix 8 — TranslationCache Column Limits (MEDIUM, Third Pass)

**Problem**: `TranslationCacheConfiguration` has no `HasMaxLength()` on `OriginalText` or `TranslatedText` columns, resulting in unbounded `nvarchar(max)` in SQL Server. This wastes storage and prevents index creation on these columns.

**Files to modify (1):**

1. EF Core configuration for `TranslationCache` entity

**What to change**:
- Add `HasMaxLength(4000)` (or appropriate business limit) to `OriginalText`
- Add `HasMaxLength(4000)` to `TranslatedText`
- These limits should accommodate typical translation content while preventing abuse

**Note**: Requires a migration after change. If data already exceeds the limit, a data migration step is needed first.

**Verification**: Generate migration, inspect SQL — columns should be `nvarchar(4000)` not `nvarchar(max)`.

---

### Fix 9 — Category.ParentCategoryId Index (MEDIUM, Third Pass)

**Problem**: `Category.ParentCategoryId` has no database index. Any query filtering by parent (tree navigation, get-children, breadcrumb) will table-scan. With growing category data, this becomes a performance bottleneck.

**Files to modify (1):**

1. EF Core configuration for `Category` entity

**What to change**:
```csharp
builder.HasIndex(x => x.ParentCategoryId);
```

**Note**: This is a non-breaking migration — just an index addition.

**Verification**: Generate migration, inspect SQL — should contain `CREATE INDEX` on `ParentCategoryId`.

---

### Fix 10 — Attachment.MarkForDeletion State Guard (LOW, Third Pass)

**Problem**: `Attachment.MarkForDeletion()` only raises a domain event but doesn't change any state on the entity. It also has no guard against being called multiple times, which would raise duplicate domain events.

**Files to modify (1):**

1. `ContentCore.Domain/Entities/Attachment.cs` — `MarkForDeletion()` method (lines 108-116)

**What to change**: Add an `IsMarkedForDeletion` flag or check, and guard against double-call:
```csharp
public void MarkForDeletion()
{
    if (IsMarkedForDeletion) return; // idempotent
    IsMarkedForDeletion = true;
    AddDomainEvent(new AttachmentDeletedDomainEvent(Id, ...));
}
```

**Note**: Review if `IsMarkedForDeletion` property needs to be persisted (EF config change) or if it's transient within the unit of work.

---

### Fix 11 — Tag/Specialization Activate/Deactivate Idempotency (LOW, Third Pass)

**Problem**: `Tag.Activate()`/`Deactivate()` and similar methods on `Specialization` raise domain events even if the entity is already in the target state. This produces unnecessary events.

**Files to modify (2-3):**

1. `ContentCore.Domain/Entities/Tag.cs` — `Activate()` and `Deactivate()`
2. `ContentCore.Domain/Entities/Specialization.cs` — `Activate()` and `Deactivate()` (if same pattern)

**What to change**:
```csharp
public void Activate()
{
    if (IsActive) return; // already active
    IsActive = true;
    AddDomainEvent(new TagActivatedDomainEvent(Id));
}
```

**Verification**: Call Activate on already-active tag — no domain event should be raised.

---

### Fix 12 — AttachmentRepository.GetEntityImagesAsync Tracking (LOW, Third Pass)

**Problem**: `AttachmentRepository.GetEntityImagesAsync()` returns a tracked query result with no ordering. For a read-only image gallery display, this wastes change-tracker resources and gives non-deterministic ordering.

**Files to modify (1):**

1. `ContentCore.Infrastructure/Repositories/AttachmentRepository.cs` — `GetEntityImagesAsync()` method

**What to change**:
- Add `.AsNoTracking()` to the query
- Add `.OrderBy(x => x.SortOrder)` for deterministic ordering

---

## Execution Order

```
┌─── ORIGINAL FIXES (Passes 1-2) ───────────────────────────────────────────────────┐
│                                                                                    │
│  Fix 1 (Attachment limits)     ─── CRITICAL, 2 files, standalone, ~30 min         │
│  Fix 2 (AzureTranslate Result) ─── HIGH, 3 files (iface+impl+decorator), ~45 min │
│  Fix 3 (Permissions)           ─── HIGH, 1 file, standalone, ~30 min              │
│                                    ⚠️ Review ContentManagement scope breadth first │
│  Fix 4 (Validators)            ─── MEDIUM, 3 new files, standalone, ~20 min       │
│                                                                                    │
└────────────────────────────────────────────────────────────────────────────────────┘
                              All independent / parallelizable
                                            │
┌─── THIRD-PASS FIXES (Domain + Infrastructure) ────────────────────────────────────┐
│                                                                                    │
│  Fix 7  (Category.ChangeParent)     ─── MEDIUM, 1 file, standalone, ~15 min       │
│  Fix 8  (TranslationCache limits)   ─── MEDIUM, 1 file + migration, ~20 min       │
│  Fix 9  (ParentCategoryId index)    ─── MEDIUM, 1 file + migration, ~10 min       │
│  Fix 10 (Attachment.MarkForDeletion)─── LOW, 1 file, standalone, ~15 min          │
│  Fix 11 (Activate/Deactivate idem.) ─── LOW, 2-3 files, standalone, ~15 min      │
│  Fix 12 (AttachmentRepo tracking)   ─── LOW, 1 file, standalone, ~10 min         │
│                                                                                    │
│  Note: Fix 8 + Fix 9 can share a single EF migration                              │
└────────────────────────────────────────────────────────────────────────────────────┘
                              All independent / parallelizable
                                            │
                                            ▼
             Fix 5 (Build + Verify) ─── MEDIUM, depends on ALL above, ~30 min
                                            │
                                            ▼
             Fix 6 (Update plan doc) ─── LOW, 1 file, standalone, ~15 min
```

Fixes 1-4 are independent and can be parallelized. Fixes 7-12 are independent and can be parallelized. Fix 8 + Fix 9 can share a single migration. Fix 5 must run after all code changes. Fix 6 can run anytime.

---

## File Impact Summary

| Category | Count | Files |
|----------|-------|-------|
| **Original Fixes (1-4)** | | |
| Modified files | 5 | `AttachmentLimits.cs`, `UploadAttachmentCommandValidator.cs`, `AzureTranslateService.cs`, `ITranslationService.cs`, `AutoSaveTranslationService.cs` |
| Modified files (permissions) | 1 | `RolePermissionMapping.cs` |
| New files | 3 | 3 Language command validators |
| **Third-Pass Fixes (7-12)** | | |
| Modified domain files | 3-4 | `Category.cs`, `Attachment.cs`, `Tag.cs`, `Specialization.cs` (if applicable) |
| Modified infrastructure files | 3 | TranslationCache EF config, Category EF config, `AttachmentRepository.cs` |
| New migration | 1 | Combined migration for Fix 8 + Fix 9 |
| **Shared** | | |
| Plan doc updates | 1 | `ContentCore-Workflow.md` |
| Deleted files | 0 | — |
| **Total** | **17-18** | 10 original + 7-8 third-pass |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| `DetectLanguageAsync` callers break on signature change | Grep all callers before changing. Fix 2 includes caller updates. |
| Validator `Must()` rule changes error message format | Keep message format consistent with existing validators in the project. |
| Permission grant too broad for Creator/TourGuide | OwnershipGuard is the real security gate. Catalog endpoints remain admin-only. Verify with permission matrix. |
| Build breaks from accumulated changes | Run Fix 5 after all code fixes, not in parallel. |
| TranslationCache max-length migration truncates data | Check max existing data length before generating migration. Use `CASE WHEN LEN() > 4000` query. |
| `Category.ChangeParent` DomainException unhandled | Verify handler catches `DomainException` and maps to Result.Failure, or use global exception filter. |
| `Attachment.MarkForDeletion` state change needs EF mapping | Decide if `IsMarkedForDeletion` is persisted (needs EF config + migration) or transient (session-scoped). |
| Idempotency guards change event flow | Downstream consumers may rely on duplicate events — verify no consumer depends on re-activation events. |

---

## Follow-Up Items (Not In Scope)

These belong in other module plans but are tracked here for visibility:

| Item | Owner Plan | Status |
|------|-----------|--------|
| Analytics consumer for `EntityTagAssignedIntegrationEvent` | Analytics-Workflow.md | Not started |
| Analytics consumer for `EntityTagRemovedIntegrationEvent` | Analytics-Workflow.md | Not started |
| ContentSeo consumer for `EntityTagAssignedIntegrationEvent` | ContentSeo-Workflow.md | Not started |
| ContentSeo consumer for `EntityTagRemovedIntegrationEvent` | ContentSeo-Workflow.md | Not started |
| Full cycle detection for Category parent hierarchy (beyond self-parent guard) | Application layer | Deferred — requires tree loading |
| Endpoint OpenAPI `Produces*` annotations for error responses | Presentation layer | Low priority |
| No value objects in ContentCore.Domain (e.g., FileName, MimeType, FileSize) | Domain refactor | Deferred — aspirational, not a bug |
| Category.SoftDelete uses `new` instead of `override` | Domain cleanup | Info — verify base class has virtual method |
| Attachment inherits BaseEntity not AuditableEntity | Domain decision | Deferred — may be intentional; needs product decision |
