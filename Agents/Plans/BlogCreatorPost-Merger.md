# Blog ← CreatorPost Merger Plan

> **Status:** ✅ EXECUTED. Merger substantially complete. See `BlogCreatorPost-Merger-Audit-Report.md` for audit score (8.5/10).
> **Wave-8 impact:** Supersedes CreatorPost sections. CreatorPost entity, enum, handlers fully deleted.
> **Post-audit cleanup (completed):** Removed duplicate MarkBlogAsFeatured/Unfeatured commands (superseded by Feature/Unfeature), cleared orphaned CreatorPost permissions/cache keys/validation errors, renamed Messaging handlers from CreatorPost* → Blog*.

## Decision Summary

**Goal:** Merge `CreatorPost` into `Blog` so there is ONE unified content entity. No redundant business logic.

### All 18 Design Decisions (LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **PostType** | DROP entirely. Blog is always a text article. Media via ContentCore Attachments. |
| 2 | **Denormalized counters** | ADD `ReactionCount` + `CommentCount` to Blog entity |
| 3 | **Integration events** | EXTEND existing Blog events (add `AuthoredByCreatorId` field). No separate CreatorPost events. |
| 4 | **Background services** | Re-target `CreatorTierPromotionService` + `CreatorStatsRollupService` to query Blog table |
| 5 | **Profile counter** | KEEP only `ApprovedArticleCount` on CreatorProfile. DROP planned `PublishedPostCount`. |
| 6 | **Summary as Excerpt** | USE `Summary` field. Add 100-500 char validation when `AuthoredByCreatorId` is set. Admin blogs keep Summary optional. |
| 7 | **Disclosure enforcement** | IN HANDLER (not domain). Handler queries `IProviderEntitiesReadClient`, compares tagged entities, validates. |
| 8 | **Admin routing** | UNIFIED under `/blogs/admin/*`. One queue for all blog moderation. |
| 9 | **Profile → Blogs** | Dedicated query endpoint `GET /creators/{slug}/blogs` (cacheable, paginated). |
| 10 | **Featuring model** | REPLACE `IsFeatured` bool with COMPUTED: `FeaturedAt != null && (FeaturedUntil == null \|\| FeaturedUntil > now)`. Remove IsFeatured property. |
| 11 | **Wave doc updates** | ADD merger note at top of Wave-8. Quick superseded header. |
| 12 | **Admin profiles** | NOT NEEDED. Admin blogs use `AuthorId` (Guid → Identity user) only. |
| 13 | **CreatorProfile vs Accounts** | KEEP SEPARATE bounded contexts. Overlap (DisplayName+Avatar) is intentional — creator's public pen name. |
| 14 | **Deletion strategy** | SOFT DELETE + 60-day background service hard delete for BOTH CreatorProfile AND Blogs. |
| 15 | **Media** | ContentCore `Attachment` system (`EntityType.Blog = 4`). No new Blog properties needed. |
| 16 | **Tagging** | ContentCore `EntityTag` system (`EntityType.Blog = 4`). No new Blog properties needed. |
| 17 | **Status machine** | `Draft=0 → PendingReview=1 → Published=2 → Rejected=3 → Archived=4 → Hidden=5 → Removed=6` |
| 18 | **No prod data** | Clean drop of `CreatorPost` table, no data migration needed. |

### Status Flow Rules
- **Admin**: Draft → Published (bypasses PendingReview)
- **Tier-0 creator**: Draft → PendingReview → Published (admin approval required)
- **Tier-1+ creator**: Draft → Published (trusted, auto-publish)
- All: Published → Archived, Published → Hidden (admin), any → Removed (admin)

---

## Scope Inventory

### Files to DELETE (~52 files)

#### Domain — CreatorPost entity + events (8 files)
1. `ContentBlogs.Domain\Entities\Creators\CreatorPost.cs`
2. `ContentBlogs.Domain\Events\Creators\CreatorPostCreatedDomainEvent.cs`
3. `ContentBlogs.Domain\Events\Creators\CreatorPostFeaturedDomainEvent.cs`
4. `ContentBlogs.Domain\Events\Creators\CreatorPostPublishedDomainEvent.cs`
5. `ContentBlogs.Domain\Events\Creators\CreatorPostRejectedDomainEvent.cs`
6. `ContentBlogs.Domain\Events\Creators\CreatorPostRemovedDomainEvent.cs`
7. `ContentBlogs.Domain\Events\Creators\CreatorPostSubmittedDomainEvent.cs`
8. `ContentBlogs.Domain\Events\Creators\CreatorPostUnfeaturedDomainEvent.cs`

#### Domain — Enums (2 files)
9. `ContentBlogs.Domain\Enums\CreatorPostStatus.cs`
10. `ContentBlogs.Domain\Enums\CreatorPostType.cs`

#### Domain — Repository interface (1 file)
11. `ContentBlogs.Domain\Repositories\ICreatorPostRepository.cs`

#### Domain — Errors (1 file, check if exists)
12. `ContentBlogs.Domain\Errors\CreatorPostErrors.cs`

#### Application — Commands (30 files in 15 folders)
13-14. `Commands\Creator\Posts\ApprovePost\` (Command + Handler)
15-17. `Commands\Creator\Posts\CreatePost\` (Command + Handler + Validator)
18-19. `Commands\Creator\Posts\DeletePost\` (Command + Handler)
20-22. `Commands\Creator\Posts\DemoteTier\` (Command + Handler + Validator)
23-24. `Commands\Creator\Posts\FeaturePost\` (Command + Handler)
25-26. `Commands\Creator\Posts\HidePost\` (Command + Handler)
27-28. `Commands\Creator\Posts\PromoteTier\` (Command + Handler)
29-30. `Commands\Creator\Posts\PublishPost\` (Command + Handler)
31-33. `Commands\Creator\Posts\RejectPost\` (Command + Handler + Validator)
34-35. `Commands\Creator\Posts\RemovePost\` (Command + Handler)
36-37. `Commands\Creator\Posts\SubmitPostForReview\` (Command + Handler)
38-39. `Commands\Creator\Posts\UnfeaturePost\` (Command + Handler)
40-41. `Commands\Creator\Posts\UnhidePost\` (Command + Handler)
42-44. `Commands\Creator\Posts\UpdatePost\` (Command + Handler + Validator)

#### Application — Queries (10 files in 5 folders)
45-46. `Queries\Creator\Posts\GetAdminPostQueue\` (Query + Handler)
47-48. `Queries\Creator\Posts\GetCreatorPostBySlug\` (Query + Handler)
49-50. `Queries\Creator\Posts\GetFeaturedPosts\` (Query + Handler)
51-52. `Queries\Creator\Posts\GetMyCreatorPosts\` (Query + Handler)
53-54. `Queries\Creator\Posts\ListPosts\` (Query + Handler)

#### Application — Validator (1 file, if exists)
55. `ContentBlogs.Application\Validators\CreatorPostTypeValidator.cs` (referenced in CreateCreatorPostCommandHandler)

#### Infrastructure — Domain event handlers (5 files)
56. `ContentBlogs.Infrastructure\DomainEventHandlers\CreatorPostFeaturedDomainEventHandler.cs`
57. `ContentBlogs.Infrastructure\DomainEventHandlers\CreatorPostPublishedDomainEventHandler.cs`
58. `ContentBlogs.Infrastructure\DomainEventHandlers\CreatorPostRejectedDomainEventHandler.cs`
59. `ContentBlogs.Infrastructure\DomainEventHandlers\CreatorPostRemovedDomainEventHandler.cs`
60. `ContentBlogs.Infrastructure\DomainEventHandlers\CreatorPostSubmittedDomainEventHandler.cs`

#### Infrastructure — Repository + EF config (2 files)
61. `ContentBlogs.Infrastructure\Repositories\CreatorPostRepository.cs`
62. `ContentBlogs.Infrastructure\Persistence\Configurations\CreatorPostConfiguration.cs`

#### Contracts — Integration events (4 files)
63. `ContentBlogs.Contracts\IntegrationEvents\CreatorPostFeaturedIntegrationEvent.cs`
64. `ContentBlogs.Contracts\IntegrationEvents\CreatorPostPublishedIntegrationEvent.cs`
65. `ContentBlogs.Contracts\IntegrationEvents\CreatorPostRejectedIntegrationEvent.cs`
66. `ContentBlogs.Contracts\IntegrationEvents\CreatorPostRemovedIntegrationEvent.cs`

#### Contracts — Integration events (1 file, check)
67. `ContentBlogs.Contracts\IntegrationEvents\CreatorPostSubmittedForReviewIntegrationEvent.cs`

#### Presentation — Endpoints (2 files)
68. `ContentBlogs.Presentation\Endpoints\Creator\CreatorPostEndpoints.cs`
69. `ContentBlogs.Presentation\Endpoints\Creator\AdminPostEndpoints.cs`

### Files to MODIFY (~25 files)

#### Domain
1. **`BlogStatus.cs`** — New enum values: `Draft=0, PendingReview=1, Published=2, Rejected=3, Archived=4, Hidden=5, Removed=6`
2. **`Blog.cs`** — Add properties: `SubmittedAt`, `ReviewedAt`, `ReviewedByAdminId`, `RejectionReason`, `FeaturedAt`, `FeaturedByAdminId`, `FeaturedUntil`, `ReportCount`. Add methods: `SubmitForReview()` (keep existing), `Approve()`, `Reject()`, `Remove()`, `Feature()`, `Unfeature()`. Modify `Publish()` to handle tier-based flow.

#### Application
3. **`ContentBlogsCacheKeys.cs`** — Remove CreatorPost cache keys, add any missing Blog keys
4. **Blog handlers that need update:**
   - `CreateBlogCommandHandler.cs` — Support creator authoring (excerpt validation, tier check)
   - `PublishBlogCommandHandler.cs` (if exists) — Tier-based publish vs PendingReview
   - Add new handlers: `ApproveBlogCommand`, `RejectBlogCommand`, `RemoveBlogCommand`, `FeatureBlogCommand`, `UnfeatureBlogCommand`, `SubmitBlogForReviewCommand`
5. **Blog queries that need update/add:**
   - Add `GetAdminBlogQueueQuery` (replaces GetAdminPostQueue)
   - Add `GetMyBlogsQuery` (replaces GetMyCreatorPosts)
   - Add `GetFeaturedBlogsQuery` (replaces GetFeaturedPosts)
   - Existing `ListBlogsQuery` — add creator filtering

#### Infrastructure
6. **`BlogConfiguration.cs`** — Add new column configs for all new properties
7. **`ContentBlogsDbContext.cs`** — Remove `DbSet<CreatorPost>`, update `OnModelCreating`

#### Contracts
8. **`ContentBlogsFeatures.cs`** — Remove `CreatorPost` feature, merge permissions into `Blog`
9. **`ContentBlogsPermissionCatalog.cs`** — Remove CreatorPost permissions, add Blog equivalents (Submit, Approve, Reject, etc.)

#### Presentation
10. **`BlogEndpoints.cs`** — Add creator-facing endpoints (create-as-creator, submit-for-review, my-blogs)
11. **`CreatorRequests.cs`** — Remove CreatorPost request DTOs, add Blog creator DTOs
12. **`CreatorEndpoints.cs`** — May need updates if it references CreatorPost

#### Cross-module (check for consumers)
13. Any modules consuming `CreatorPostFeaturedIntegrationEvent`, `CreatorPostPublishedIntegrationEvent`, etc. need to consume Blog equivalents instead.

### Files UNTOUCHED
- `CreatorProfile.cs` — stays (profile is NOT content)
- `CreatorApplication.cs` — stays
- `CreatorFollow.cs`, `CreatorInvitation.cs`, `CreatorNiche.cs` — stay
- `BlogComment.cs`, `BlogCommentReaction.cs`, `BlogView.cs` — stay (already on Blog)
- `BlogTranslation.cs`, `BlogTour.cs` — stay
- ContentCore `Attachment` and `EntityTag` systems — untouched (already support `EntityType.Blog`)
- Tests — no CreatorPost tests exist

---

## Execution Phases

### Phase 1: Extend Blog Entity (Domain + EF)
**Risk: LOW** — additive only, no deletions

1. Update `BlogStatus` enum: `Draft=0, PendingReview=1, Published=2, Rejected=3, Archived=4, Hidden=5, Removed=6`
2. Add new properties to `Blog.cs`: `SubmittedAt`, `ReviewedAt`, `ReviewedByAdminId`, `RejectionReason`, `FeaturedAt`, `FeaturedByAdminId`, `FeaturedUntil`, `ReactionCount`, `CommentCount`, `ReportCount`
3. REMOVE `IsFeatured` property — replace with computed expression in queries/DTOs
4. Add new domain methods: `Approve()`, `Reject()`, `Remove()`, `Feature(adminId, until?)`, `Unfeature()`
5. Update existing `SubmitForCreatorReview()` → `SubmitForReview()`
6. Update `BlogConfiguration.cs` with new column mappings
7. **BUILD + VERIFY**

### Phase 2: Add New Blog Handlers (Application)
**Risk: MEDIUM** — new code, mirrors existing CreatorPost handlers

1. Create `ApproveBlogCommand` + handler (admin approves pending blog)
2. Create `RejectBlogCommand` + handler (admin rejects with reason)
3. Create `RemoveBlogCommand` + handler (admin moderation removal)
4. Create `FeatureBlogCommand` + handler (time-bound: FeaturedAt + FeaturedUntil)
5. Create `UnfeatureBlogCommand` + handler
6. Update `CreateBlogCommandHandler` — support `AuthoredByCreatorId`, excerpt validation (100-500 chars), tier check
7. Add tier-based publish logic: Draft → PendingReview (Tier-0) or Draft → Published (Tier-1+)
8. Add creator update/delete handlers with ownership checks + soft delete
9. Add admin queue query: `GetAdminBlogQueueQuery`
10. Add `GetMyBlogsQuery` (creator's own blogs, all statuses)
11. Add `GetCreatorBlogsQuery` (public: published blogs by creator slug)
12. Update cache keys
13. Disclosure enforcement: handler validates against `IProviderEntitiesReadClient`
14. **BUILD + VERIFY**

### Phase 3: Add New Blog + CreatorProfile Endpoints (Presentation)
**Risk: MEDIUM** — new endpoints, some existing endpoint updates

**Blog creator endpoints:**
1. `POST /blogs/creator` — create blog as creator
2. `PUT /blogs/creator/{id}` — update own blog
3. `DELETE /blogs/creator/{id}` — soft delete own blog
4. `POST /blogs/{id}/submit-for-review`
5. `GET /blogs/my` — creator's own blogs

**Blog admin endpoints:**
6. `POST /blogs/admin/{id}/approve`
7. `POST /blogs/admin/{id}/reject`
8. `POST /blogs/admin/{id}/remove`
9. `POST /blogs/admin/{id}/feature`
10. `POST /blogs/admin/{id}/unfeature`
11. `DELETE /blogs/admin/{id}` — soft delete
12. `GET /blogs/admin/queue` — pending review queue

**CreatorProfile CRUD (7 new endpoints):**
13. `PUT /admin/creators/{id}` — admin edit ALL fields
14. `GET /admin/creators/{id}/profile` — admin detail view
15. `DELETE /admin/creators/{id}` — admin soft delete
16. `DELETE /creators/me` — self-deactivate
17. `PUT /creators/me/avatar` — avatar upload
18. `PUT /creators/me/cover-image` — cover image upload
19. `GET /creators/{slug}/blogs` — public blog listing

**Permissions:** Merge CreatorPost perms into Blog perms
20. **BUILD + VERIFY**

### Phase 4: Delete CreatorPost (~69 files)
**Risk: HIGH** — mass deletion, verify no remaining references

1. Delete all ~69 CreatorPost files (entity, enums, repo, errors, commands, queries, validators, domain event handlers, infra, contracts, endpoints)
2. Remove `DbSet<CreatorPost>` from DbContext
3. Remove CreatorPost permissions from catalog
4. Remove CreatorPost integration events
5. Remove CreatorPost cache keys
6. Check ALL cross-module consumers of CreatorPost integration events
7. Remove `CreatorProfile.Posts` navigation property (change to Blog-side query)
8. **BUILD + VERIFY — expect errors, fix remaining references**

### Phase 5: Background Services + Re-targeting
**Risk: MEDIUM** — new services, existing service updates

1. Create `BlogCleanupService` — daily, hard-delete blogs where `DeletedAt + 60 days < now`
2. Create `ProfileCleanupService` — daily, hard-delete profiles where `DeletedAt + 60 days < now`, cascade: anonymize blogs, delete followers, revoke role
3. Re-target `CreatorTierPromotionService` — count `Blog WHERE AuthoredByCreatorId IS NOT NULL AND Status = Published`
4. Re-target `CreatorStatsRollupService` — aggregate from Blog table
5. **BUILD + VERIFY**

### Phase 6: Final Cleanup + Solution Build
1. Remove empty directories
2. Update DI registrations (remove CreatorPost repository, add new services)
3. Update Wave-8.md with superseded header
4. Full solution build
5. Verify test compilation

---

## Blog Entity — Final Shape After Merger

```csharp
public sealed class Blog : AuditableEntity, IAggregateRoot
{
    // ── Existing ──
    public string Title { get; private set; }
    public string Slug { get; private set; }
    public string Content { get; private set; }
    public string? Summary { get; private set; }       // also serves as Excerpt for creator content (100-500 chars when AuthoredByCreatorId set)
    public Guid AuthorId { get; private set; }
    public BlogStatus Status { get; private set; }
    public int ViewCount { get; private set; }
    public int? ReadTimeMinutes { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid? PlaceId { get; private set; }
    public Guid? AuthoredByCreatorId { get; private set; }
    public Guid LanguageId { get; private set; }
    public bool IsSponsored { get; private set; }
    public IReadOnlyList<DisclosureTarget> DisclosedTargets { get; }

    // ── NEW: Moderation ──
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public string? RejectionReason { get; private set; }

    // ── NEW: Time-bound featuring (replaces IsFeatured bool) ──
    public DateTime? FeaturedAt { get; private set; }
    public Guid? FeaturedByAdminId { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }
    // COMPUTED: IsFeatured => FeaturedAt != null && (FeaturedUntil == null || FeaturedUntil > DateTime.UtcNow)
    // Old `IsFeatured` property REMOVED — use computed expression in queries/DTOs

    // ── NEW: Denormalized counters ──
    public int ReactionCount { get; private set; }
    public int CommentCount { get; private set; }
    public int ReportCount { get; private set; }

    // ── Existing collections ──
    public IReadOnlyCollection<BlogTranslation> BlogTranslations { get; }
    public IReadOnlyCollection<BlogComment> BlogComments { get; }
    public IReadOnlyCollection<BlogTour> BlogTours { get; }

    // ── Domain methods ──
    // Existing: Create(), CreateByCreator(), Publish(), Archive(), Hide(), Unhide()
    // New/Updated: SubmitForReview(), Approve(), Reject(), Remove(), Feature(), Unfeature()
    // Soft delete: Delete() sets IsDeleted=true, DeletedAt=DateTime.UtcNow (AuditableEntity)
}
```

### BlogStatus (final)
```csharp
public enum BlogStatus : byte
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
    Rejected = 3,
    Archived = 4,
    Hidden = 5,
    Removed = 6
}
```

### Tags & Attachments — No changes needed
- `ContentCore.EntityTag.Create(EntityType.Blog, blogId, tagId)` — already works
- `ContentCore.Attachment.Create(EntityType.Blog, blogId, ...)` — already works
- No new properties on Blog for tagging/attachments — handled externally by ContentCore

---

## CreatorProfile CRUD — 7 New Endpoints

### Existing Endpoints (already built in Wave-7)
| Endpoint | Auth | Notes |
|----------|------|-------|
| `GET /creators/me` | Creator | Read own profile |
| `PUT /creators/me` | Creator | Update own profile |
| `GET /creators/{slug}` | Anonymous | Public profile view |
| `GET /creators` | Anonymous | List creators |
| `POST /admin/creators/{id}/suspend` | Admin | Suspend profile |
| `POST /admin/creators/{id}/reinstate` | Admin | Reinstate profile |

### New Endpoints to Add
| # | Endpoint | Auth | Purpose |
|---|----------|------|---------|
| 1 | `PUT /admin/creators/{id}` | Admin | Edit ALL fields (DisplayName, Slug, Bio, Avatar, CoverImage, Niches, SocialHandles, TrustTier) |
| 2 | `GET /admin/creators/{id}/profile` | Admin | Full profile detail with private stats (ReportRate, tier eligibility) |
| 3 | `DELETE /admin/creators/{id}` | Admin | Soft delete profile. 60-day background hard delete. |
| 4 | `DELETE /creators/me` | Creator | Self-deactivate. Soft delete. 60-day background hard delete. |
| 5 | `PUT /creators/me/avatar` | Creator | Dedicated avatar upload |
| 6 | `PUT /creators/me/cover-image` | Creator | Dedicated cover image upload |
| 7 | `GET /creators/{slug}/blogs` | Anonymous | List published blogs by creator (paginated, cacheable) |

---

## Blog CRUD — Unified Endpoints (After Merger)

### Creator-Facing Blog Endpoints
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `POST /blogs/creator` | Creator | Create blog as creator (sets AuthoredByCreatorId) |
| `PUT /blogs/creator/{id}` | Creator | Update own blog (ownership check) |
| `DELETE /blogs/creator/{id}` | Creator | Soft delete own blog (60-day hard delete) |
| `POST /blogs/{id}/submit-for-review` | Creator | Submit for admin review (Tier-0 only) |
| `GET /blogs/my` | Creator | List my blogs (all statuses) |

### Admin Blog Endpoints
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `POST /blogs/admin/{id}/approve` | Admin | Approve pending blog |
| `POST /blogs/admin/{id}/reject` | Admin | Reject pending blog (with reason) |
| `POST /blogs/admin/{id}/remove` | Admin | Remove blog (moderation) |
| `POST /blogs/admin/{id}/feature` | Admin | Time-bound feature |
| `POST /blogs/admin/{id}/unfeature` | Admin | Remove featuring |
| `POST /blogs/admin/{id}/hide` | Admin | Already exists from gap fix |
| `POST /blogs/admin/{id}/unhide` | Admin | Already exists from gap fix |
| `DELETE /blogs/admin/{id}` | Admin | Soft delete blog (60-day hard delete) |
| `GET /blogs/admin/queue` | Admin | Pending review queue (all blogs) |

### Existing Blog Endpoints (kept as-is)
- `GET /blogs` — list published blogs (anonymous)
- `GET /blogs/{slug}` — get blog by slug (anonymous)
- `POST /blogs` — create admin blog
- `PUT /blogs/{id}` — update admin blog
- `POST /blogs/{id}/publish` — publish
- `POST /blogs/{id}/archive` — archive

---

## Background Services

### New Services to Create
1. **`ProfileCleanupService`** — Runs daily. Hard-deletes profiles where `DeletedAt + 60 days < now`. Cascades: anonymize blogs (`AuthoredByCreatorId=null`), delete followers, revoke Creator role.
2. **`BlogCleanupService`** — Runs daily. Hard-deletes blogs where `DeletedAt + 60 days < now`. Cascades: delete comments, reactions, views, translations, tours.

### Existing Services to Re-target
3. **`CreatorTierPromotionService`** — Change from counting CreatorPosts to counting `Blog WHERE AuthoredByCreatorId IS NOT NULL AND Status = Published`.
4. **`CreatorStatsRollupService`** — Aggregate from Blog table instead of CreatorPost table.

---

## What Survives from Wave-8

- ✅ Trust-tier promotion infrastructure (queries Blog instead of CreatorPost)
- ✅ Stats rollup (queries Blog)
- ✅ Disclosure enforcement (on Blog SubmitForReview/Approve)
- ✅ Time-bound featuring (FeaturedAt/FeaturedByAdminId/FeaturedUntil)
- ✅ Tier-based endpoints (promote/demote) — already exist from Gap fixes
- ✅ Admin moderation queue — unified Blog queue
- ✅ Creator-authored content — via Blog.AuthoredByCreatorId

## What Gets DROPPED from Wave-8

- ❌ CreatorPost entity entirely (~69 files)
- ❌ CreatorPostType enum (Video, PhotoStory, LongReview, Itinerary)
- ❌ TypeSpecificDataJson column
- ❌ Type-specific validators
- ❌ Separate CreatorPost repository, EF config, domain events
- ❌ Separate post endpoints (CreatorPostEndpoints, AdminPostEndpoints)
- ❌ PublishedPostCount on CreatorProfile
- ❌ Separate admin post queue

---

## Estimated Effort

| Phase | Files | Effort | Risk |
|-------|-------|--------|------|
| Phase 1: Extend Blog entity | ~3 modify | Low | Low |
| Phase 2: New Blog handlers | ~20-25 new/modify | Medium | Medium |
| Phase 3: Endpoints (Blog + CreatorProfile) | ~15-20 new/modify | Medium | Medium |
| Phase 4: Delete CreatorPost | ~69 delete | High (breakage) | High |
| Phase 5: Background services | ~6 new/modify | Medium | Medium |
| Phase 6: Cleanup + build | ~5 | Low | Low |
| **Total** | **~120-130** | **Multi-session** | **High** |

---

## Risks & Mitigations

1. **Cross-module integration event consumers** — Other modules may consume `CreatorPostPublishedIntegrationEvent`. Must find and update all consumers before deletion.
2. **CreatorProfile.Posts navigation** — CreatorProfile may have `ICollection<CreatorPost> Posts`. Change to Blog-side query via `AuthoredByCreatorId`.
3. **Cache key collisions** — CreatorPost cache keys must map cleanly to Blog cache keys. Remove old keys, add new ones.
4. **Notification system** — If notifications reference CreatorPost events, they need updating to Blog equivalents.
5. **BlogStatus renumbering** — Current: `Draft=0, Published=1, Archived=2, PendingCreatorReview=99, Hidden=100`. New: `Draft=0, PendingReview=1, Published=2, Rejected=3, Archived=4, Hidden=5, Removed=6`. No prod data, but seed data and test data with old values will break.
6. **IsFeatured removal** — All queries/DTOs referencing `IsFeatured` must switch to computed expression: `FeaturedAt != null && (FeaturedUntil == null || FeaturedUntil > DateTime.UtcNow)`.
7. **60-day hard delete cascade** — ProfileCleanupService must handle: anonymize blogs (null out AuthoredByCreatorId), delete followers, revoke Creator role. BlogCleanupService must handle: delete comments, reactions, views, translations, blog-tour links.

---

## Implementation Notes (Audit 2025-01-27)

> Added during codebase audit — reflects actual implementation state and post-audit cleanup.

1. **Merger substantially complete**: Blog entity (727L) has all planned properties, methods, and domain events. CreatorPost entity/enum/handlers fully deleted. Migration `20260524233257_MergeCreatorPostIntoBlog` exists.
2. **BlogStatus enum**: Exact match — Draft=0, PendingReview=1, Published=2, Rejected=3, Archived=4, Hidden=5, Removed=6.
3. **IsFeatured**: Implemented as computed property: `FeaturedAt.HasValue && (FeaturedUntil == null || FeaturedUntil > DateTime.UtcNow)`.
4. **Duplicate featuring removed (W4-D)**: `MarkBlogAsFeatured`/`MarkBlogAsUnfeatured` commands + PATCH endpoints deleted. Superseded by `FeatureBlogCommand`/`UnfeatureBlogCommand` (POST feature/unfeature).
5. **CreatorPost permissions removed (W4-D)**: `ContentBlogsFeatures.CreatorPost` + `AdminPostModeration` constants removed. 12 permission entries removed from `ContentBlogsPermissionCatalog`.
6. **CreatorPost cache keys removed (W4-D)**: 7 keys removed from `ContentBlogsCacheKeys`.
7. **TypeValidationErrors.cs deleted (W4-D)**: 67-line file with 20+ CreatorPost error codes — 0 references, safely deleted.
8. **Messaging handlers renamed (W4-D)**: 5 handlers renamed from `CreatorPost*NotificationHandler` → `Blog*NotificationHandler`. All consume correct `Blog*IntegrationEvent` types.
9. **Integration events**: 29 total (14 Blog + 15 Creator). No CreatorPost events remain. Registry has "Creator Posts merged into Blog" comment.
10. **Background services**: 5 exist — BlogCleanupService, ProfileCleanupService, CreatorTierPromotionService, CreatorStatsRollupService, InvitationCleanupService.
