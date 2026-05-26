# BlogCreatorPost-Merger.md — Fix Plan

> **Date:** 2025-06-06
> **Source:** `BlogCreatorPost-Merger-Audit-Report.md`
> **Total Fixes:** 7 (1 HIGH, 3 MEDIUM, 3 LOW)
> **Estimated Effort:** 2-3 hours
> **Risk:** LOW — all fixes are cleanup/removal, no new domain logic

---

## Design Decisions

1. **Duplicate featuring endpoints:** Remove old pattern (PATCH mark-as-featured/unfeatured + MarkBlogAsFeatured/Unfeatured commands). Keep new pattern (POST feature/unfeature + FeatureBlog/UnfeatureBlog). Old commands likely have callers that need migrating.
2. **Messaging handler naming:** Rename classes, not delete — they consume valid Blog events.
3. **CreatorPost remnants:** Safe to remove — no code references them at runtime (entity, DbSet, repository all gone).
4. **Empty directories:** Delete after confirming no files remain.
5. **Docstring fix:** Simple text replacement.

---

## Fixes

### Fix 1 (HIGH): Remove Duplicate Featuring Commands + Endpoints

**Problem:** Both old `MarkBlogAsFeatured`/`MarkBlogAsUnfeatured` and new `FeatureBlog`/`UnfeatureBlog` commands exist. 4 redundant endpoints in BlogEndpoints.cs.

**Files to DELETE:**
- `ContentBlogs.Application\Commands\Blog\MarkBlogAsFeatured\MarkBlogAsFeaturedCommand.cs`
- `ContentBlogs.Application\Commands\Blog\MarkBlogAsFeatured\MarkBlogAsFeaturedCommandHandler.cs`
- `ContentBlogs.Application\Commands\Blog\MarkBlogAsFeatured\MarkBlogAsFeaturedCommandValidator.cs`
- `ContentBlogs.Application\Commands\Blog\MarkBlogAsUnfeatured\MarkBlogAsUnfeaturedCommand.cs`
- `ContentBlogs.Application\Commands\Blog\MarkBlogAsUnfeatured\MarkBlogAsUnfeaturedCommandHandler.cs`
- `ContentBlogs.Application\Commands\Blog\MarkBlogAsUnfeatured\MarkBlogAsUnfeaturedCommandValidator.cs`

**Files to MODIFY:**
- `ContentBlogs.Presentation\Endpoints\Blog\BlogEndpoints.cs` — Remove PATCH `/{id:guid}/mark-as-featured` and PATCH `/{id:guid}/mark-as-unfeatured` endpoint registrations

**Pre-check:** Verify no other code calls `MarkBlogAsFeaturedCommand` or `MarkBlogAsUnfeaturedCommand` before deleting.

---

### Fix 2 (MEDIUM): Remove CreatorPost Permission/Feature Constants

**Problem:** `ContentBlogsFeatures.CreatorPost` and `ContentBlogsFeatures.AdminPostModeration` feature constants still declared. 12 permissions still registered in catalog. No endpoints use them.

**Files to MODIFY:**
- `ContentBlogs.Contracts\Authorization\ContentBlogsFeatures.cs` — Remove `CreatorPost` and `AdminPostModeration` constants
- `ContentBlogs.Contracts\Authorization\ContentBlogsPermissionCatalog.cs` — Remove lines 71-85 (12 CreatorPost/AdminPostModeration permission registrations)

**Pre-check:** Search for `ContentBlogsFeatures.CreatorPost` and `ContentBlogsFeatures.AdminPostModeration` across entire codebase to confirm zero runtime usage.

---

### Fix 3 (MEDIUM): Remove CreatorPost Cache Keys

**Problem:** 7 CreatorPost cache keys remain in `ContentBlogsCacheKeys.cs` but nothing invalidates or reads them.

**Files to MODIFY:**
- `ContentBlogs.Application\Caching\ContentBlogsCacheKeys.cs` — Remove: `CreatorPost(slug)`, `CreatorPostTag(slug)`, `CreatorPostsListTag`, `CreatorPostsFeaturedTag`, `MyCreatorPosts(profileId)`, `MyCreatorPostsTag(profileId)`, `AdminPostsQueueTag`

**Pre-check:** Search for each key name across codebase to confirm zero usage.

---

### Fix 4 (MEDIUM): Remove CreatorPost Validation Errors

**Problem:** `TypeValidationErrors.cs` in Domain contains 20+ CreatorPost error definitions referencing `CreatorPostTypeValidator` which no longer exists.

**Files to MODIFY:**
- `ContentBlogs.Domain\Errors\TypeValidationErrors.cs` — Remove all `CreatorPost.*` error definitions

**Pre-check:** Verify `CreatorPostTypeValidator` does not exist anywhere. If `TypeValidationErrors.cs` becomes empty after removal, delete the file.

---

### Fix 5 (LOW): Rename Messaging CreatorPost Notification Handlers

**Problem:** 5 handlers in `Messaging.Infrastructure` are named `CreatorPost*` but consume `Blog*IntegrationEvent` types. Misleading names.

**Files to RENAME:**
- `CreatorPostFeaturedNotificationHandler.cs` → `BlogFeaturedNotificationHandler.cs`
- `CreatorPostPublishedNotificationHandler.cs` → `BlogPublishedNotificationHandler.cs`
- `CreatorPostRejectedNotificationHandler.cs` → `BlogRejectedNotificationHandler.cs`
- `CreatorPostRemovedNotificationHandler.cs` → `BlogRemovedNotificationHandler.cs`
- `CreatorPostSubmittedNotificationHandler.cs` → `BlogSubmittedForReviewNotificationHandler.cs`

Also rename the class names inside each file to match.

---

### Fix 6 (LOW): Fix Stale Docstring in Blog.cs

**Problem:** Line 144 docstring references `SubmitForCreatorReview` (old name).

**Files to MODIFY:**
- `ContentBlogs.Domain\Entities\Blog.cs` — Replace `SubmitForCreatorReview` with `SubmitForReview` in docstring

---

### Fix 7 (LOW): Update Plan Document Status + Route Corrections

**Problem:** Plan says "Ready for execution" but merger is executed. Route names in plan differ from actual code.

**Files to MODIFY:**
- `Agents\Plans\BlogCreatorPost-Merger.md` — Update status line, correct route prefixes, note extras not in plan

---

## Execution Order

```
Fix 2 (permissions) ──┐
Fix 3 (cache keys) ───┤
Fix 4 (validation) ───┼──→ Fix 1 (featuring dedup) ──→ BUILD VERIFY ──→ Fix 5 (rename) ──→ Fix 6+7 (doc fixes)
                      │
                      └─── (all independent, parallelizable)
```

- Fixes 2, 3, 4 are independent cleanup — no cross-dependencies
- Fix 1 requires pre-check for callers before deletion
- Fix 5 is file+class rename — do after build verify to isolate issues
- Fixes 6, 7 are doc-only

---

## File Impact Summary

| Action | Count | Files |
|--------|-------|-------|
| DELETE | 6-8 | 6 MarkBlogAs* files + 2 empty directories |
| MODIFY | 5-6 | BlogEndpoints.cs, ContentBlogsFeatures.cs, ContentBlogsPermissionCatalog.cs, ContentBlogsCacheKeys.cs, TypeValidationErrors.cs, Blog.cs |
| RENAME | 5 | Messaging notification handlers |
| DOC | 1 | BlogCreatorPost-Merger.md |
| **Total** | **17-20** | |

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| MarkBlogAsFeatured callers exist | Build breaks | Search all references before deleting |
| Permission removal affects seeded data | Runtime auth failure | Verify SecurityDataSeeder doesn't reference deleted constants |
| Cache key removal leaves orphan cache entries | Stale Redis/HybridCache | Acceptable — entries expire naturally |
| Messaging handler rename breaks DI registration | Runtime failure | MediatR auto-discovers by convention — rename is safe |

---

## Deferred / Out of Scope

1. **Migration files** (`MergeCreatorPostIntoBlog`, `AddCreatorPostsAndTierManagement`) — Keep for DB history
2. **Empty `Commands\Creator\Posts\` and `Queries\Creator\Posts\` directories** — Delete during Fix 4 if accessible
3. **Plan route corrections** — Covered in Fix 7 but low priority
4. **ContentBlogsDbContext/DI comments** ("CreatorPost removed") — Leave as documentation
