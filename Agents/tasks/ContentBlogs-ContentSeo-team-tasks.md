# ContentBlogs + ContentSeo Sprint — Team Tasks

> **Predecessor sprint:** `ContentTours-team-tasks.md` (Wave 3 + partial Wave 4: schedules, pricing, packages, search, waypoints, children-info).
> **This sprint covers:** Wave 4 remainder — full ContentBlogs module (CRUD + state machine + translations + comments + reactions + blog↔tour links) + full ContentSeo module (metadata + redirects + FAQ + sitemap + weather) + Translation API endpoints in ContentCore + two background services (SitemapRegenerationService, WeatherPreFetchService).
> **Difficulty vs ContentTours:** ⚙️⚙️⚙️ (3/5) — fewer aggregate-level state transitions than Tours, but more cross-module integration (SEO consumes Blog/Tour/Place/Business events) and two new long-running background services with external HTTP budgets.
> **Endpoint count:** **35 HTTP endpoints** (15 Blogs + 13 SEO + 7 Translation/Weather/Sitemap/Background-trigger) + **2 hosted services** + **3 new stub interfaces** + **1 new shared filter (`IProfanityFilter`)**.
> **Working-day estimate:** **15 working days × 4 devs ≈ 168 person-hours** (parity with ContentTours sprint).

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Time (AST) | Owner |
|---|---|---|---|
| Pre-work branch cut from `main` (after ContentTours merges) | Fri 2026-05-22 | 17:30 | Tech Lead |
| Sprint kickoff (all-hands, 30 min) | **Sun 2026-05-24** | **09:00** | Tech Lead |
| Pre-Work merge deadline (PW-1..PW-7 all green) | Tue 2026-05-26 | 17:00 | Tech Lead |
| Earliest Task-1/Task-2/Task-3/Task-4 start | Wed 2026-05-27 | 09:00 | All devs |
| Mid-sprint integration freeze (no breaking contract changes) | Sun 2026-06-07 | 17:00 | All devs |
| Hard PR cutoff (no new PRs after this — WIP only) | **Wed 2026-06-10** | **17:00** | All devs |
| Hard merge-to-main cutoff (PRs must be merged or rolled to next sprint) | **Thu 2026-06-11** | **17:00** | Tech Lead |
| Sprint retro + ContentBlogs/ContentSeo demo | Fri 2026-06-12 | 11:00 | Tech Lead |

**Working week:** Sunday → Thursday (Sun, Mon, Tue, Wed, Thu = 5 days). Friday + Saturday = weekend.
**Working days:** 15 (3 weeks × 5 days). Day codes used in WBS tables: `Su / M / T / W / R` per Week 1/2/3.

### 0.1 Daily Standup

- **Time:** 09:30 AST, every working day (Sun–Thu).
- **Duration:** 15 minutes hard cap.
- **Format (per dev, 3 sentences):**
  1. What I shipped yesterday (PR/commit references).
  2. What I'm shipping today.
  3. Blockers (named, owner-tagged, ETA).
- **Miss-2 rule:** Missing standup twice in one week without prior notice → escalates to Tech Lead 1:1 same day.

---

## 1. Working Days & Person-Hour Budget

| Item | Value |
|---|---|
| Working days | 15 (Sun–Thu × 3 weeks) |
| Hours / day / dev | 8 |
| Devs | 4 |
| Total available hours | **480** |
| Hours allocated to tasks | **168** |
| Hours reserved for code review (every dev reviews ≥2 PRs/week) | **48** |
| Hours reserved for pre-work, standups, retro, blockers | **48** |
| Float / buffer | **216** |

**Why 216 hrs of buffer?** This is the FIRST sprint that wires SEO into a multi-module integration-event web (Blog + Tour + Place + Business → Sitemap + Metadata). Buffer absorbs (a) inter-team contract negotiations, (b) external API stub hardening (weather, search-console), (c) translation latency edge cases.

---

## 2. Team Members & High-Level Allocation

| # | Name | Level | Tasks | Endpoints | Bg Services | Est. Hours | Hard Deadline |
|---|---|---|---|---|---|---|---|
| 1 | **Fadwa** | Beginner | Task 1 (Blogs) + Task 2 partial (comments + reactions, endpoints 1–6) + Blog-module pre-work | 15 | 0 | ~80h | Thu 2026-06-11 17:00 |
| 2 | **Mohammad** | Intermediate | Task 3 (SEO Metadata/Redirects/FAQ) + Task 4 (Sitemap/Weather/Translation/BG) + SEO-module pre-work | 18 | 2 | ~80h | Thu 2026-06-11 17:00 |
| 3 | **Mahmoud** | Intermediate | Task 2 partial (Blog↔Tour links, endpoints 7–8) | 2 | 0 | ~10h | Thu 2026-06-11 17:00 |
| — | **Tech Lead** (rotates per pair-review) | — | Code review + pre-work guard | — | — | 12h | Continuous |

**Total:** 35 HTTP endpoints + 2 hosted services + 3 stub interfaces + 1 shared filter = **180 working hours** (pre-work hours folded into Fadwa's and Mohammad's per-task allocations; 12h Tech Lead review budget unchanged).

---

## 3. Entity Ownership Matrix

> **Reading guide:** Owner = the dev who writes the production handler / event-handler logic AND the unit/integration tests for that entity. Reviewer is always one other dev + the Tech Lead.

### 3.1 ContentBlogs.Domain

| File (existing) | Base class | Aggregate? | Domain events raised | Owner | Task |
|---|---|---|---|---|---|
| `Blog.cs` | `AuditableEntity, IAggregateRoot` ✅ | Yes | **9 events** (Created, Updated, Published, Unpublished, Archived, Deleted, Featured, Unfeatured, ViewCounted) | Fadwa | T1 |
| `BlogTranslation.cs` | `BaseEntity` | No (child of Blog) | None — direct outbox writes from Blog* event handlers | Fadwa | T1 |
| `BlogComment.cs` | `AuditableEntity` → **must add `IAggregateRoot`** in PW-3 | Yes (after PW-3) | **5 events** (Created, Edited, Deleted, Restored-by-admin, ProfanityRejected) | Fadwa | T2 |
| `BlogCommentReaction.cs` | `BaseEntity` (mutable; no soft-delete) | No (child of BlogComment) | None — comment aggregate raises `BlogCommentReactionChangedDomainEvent` | Fadwa | T2 |
| `BlogTour.cs` | **Junction class** (no base, composite PK `(BlogId, TourId)`) | No | None — Blog aggregate raises `BlogTourLinkedDomainEvent` / `BlogTourUnlinkedDomainEvent` | Mahmoud | T2 |

### 3.2 ContentSeo.Domain

| File (existing) | Base class | Aggregate? | Domain events raised | Owner | Task |
|---|---|---|---|---|---|
| `SeoMetadata.cs` | `AuditableEntity` → **must add `IAggregateRoot`** in PW-4 | Yes (after PW-4) | **3 events** (Created, Updated, Deleted) | Mohammad | T3 |
| `Redirect.cs` | `AuditableEntity` → **must add `IAggregateRoot`** in PW-4 | Yes (after PW-4) | **2 events** (Created, Deactivated). Hit increment is fire-and-forget, no event. | Mohammad | T3 |
| `FaqItem.cs` | `AuditableEntity` → **must add `IAggregateRoot`** in PW-4 | Yes (after PW-4) | **3 events** (Created, Updated, Deleted) | Mohammad | T3 |
| `FaqItemTranslation.cs` | `BaseEntity` | No (child of FaqItem) | None — translation orchestrator handles writes | Mohammad | T3 |
| `SitemapEntry.cs` | `AuditableEntity` → **must add `IAggregateRoot`** in PW-4 | Yes (after PW-4) | None — sitemap entries are write-mostly from background job; no business events | Mohammad | T4 |
| `WeatherCache.cs` | `BaseEntity` → **upgrade to `AuditableEntity` + `IAggregateRoot`** in PW-4 | Yes (after PW-4) | None — weather is fire-and-forget cache | Mohammad | T4 |

### 3.3 Cross-module dependencies

| Module | Depends on | Reason |
|---|---|---|
| ContentBlogs | ContentTours (Tour exists), ContentPlaces (Place exists), ContentCore (Languages, Tags, Categories, Attachments, ITranslationService, IEntityTranslationOrchestrator), Accounts (UserId for AuthorId/CommentUserId) | Blog references Place + Tours; comments reference users; translations use shared orchestrator. |
| ContentSeo | ContentBlogs, ContentTours, ContentPlaces, Businesses (when wired) | SEO consumes Blog/Tour/Place/Business integration events to refresh sitemap + metadata. |

---

## 4. New Enums / Interfaces / Migrations Introduced This Sprint

| # | Item | Kind | Project | Owner | Pre-work? |
|---|---|---|---|---|---|
| 4.1 | `ReactionType` (re-aligned to `Like, Helpful, Insightful`) | Enum migration | `ContentBlogs.Domain.Enums` | Fadwa | **PW-2** |
| 4.2 | `BlogCommentDeletionMode` (`SelfSoftDelete`, `AdminSoftDelete`, `Hard`) | New enum | `ContentBlogs.Domain.Enums` | Fadwa | T2 |
| 4.3 | `RedirectStatusCode` (`Permanent301 = 301`, `Temporary302 = 302`) | New enum | `ContentSeo.Domain.Enums` | Mohammad | T3 |
| 4.4 | `IBlogRepository` | Interface | `ContentBlogs.Application.Interfaces` | Fadwa | **PW-5** |
| 4.5 | `IBlogCommentRepository` | Interface | `ContentBlogs.Application.Interfaces` | Fadwa | T2 |
| 4.6 | `IBlogTourLinkRepository` (junction-direct via `IDbContext`) | Interface | `ContentBlogs.Application.Interfaces` | Mahmoud | T2 |
| 4.7 | `ISeoMetadataRepository` | Interface | `ContentSeo.Application.Interfaces` | Mohammad | T3 |
| 4.8 | `IRedirectRepository` | Interface | `ContentSeo.Application.Interfaces` | Mohammad | T3 |
| 4.9 | `IFaqItemRepository` | Interface | `ContentSeo.Application.Interfaces` | Mohammad | T3 |
| 4.10 | `ISitemapEntryRepository` | Interface | `ContentSeo.Application.Interfaces` | Mohammad | T4 |
| 4.11 | `IWeatherCacheRepository` | Interface | `ContentSeo.Application.Interfaces` | Mohammad | T4 |
| 4.12 | `IPlaceExistsService` (already created in ContentTours sprint — **reuse**) | Existing interface | `ContentTours.Application.Interfaces` | — | — |
| 4.13 | `ITourExistsService` (new — used by `BlogTour` link gate) | Stub interface | `ContentTours.Application.Interfaces` | Mahmoud | T2 |
| 4.14 | `IWeatherProvider` | Stub interface | `ContentSeo.Application.Interfaces` | Mohammad | T4 |
| 4.15 | `ISearchConsolePinger` | Stub interface | `ContentSeo.Application.Interfaces` | Mohammad | T4 |
| 4.16 | `IProfanityFilter` | Shared interface | `YallaJo.SharedKernel.Application.Abstractions.Moderation` | Fadwa | T2 |
| 4.17 | `IBlogViewCounter` (debounced increment) | Interface | `ContentBlogs.Application.Interfaces` | Fadwa | T1 |
| 4.18 | `ISitemapRenderer` | Interface | `ContentSeo.Application.Interfaces` | Mohammad | T4 |
| 4.19 | `ContentBlogsFeatures` + `ContentBlogsPermissionCatalog` | Auth catalog | `ContentBlogs.Contracts/Authorization/` | Fadwa | **PW-6** |
| 4.20 | `ContentSeoFeatures` + `ContentSeoPermissionCatalog` | Auth catalog | `ContentSeo.Contracts/Authorization/` | Mohammad | **PW-6** |
| 4.21 | EF migration `AlignReactionTypeEnum` | Migration | `ContentBlogs.Infrastructure/Migrations` | Fadwa | **PW-2** |
| 4.22 | EF migration `AddBlogCommentAggregateMarker` (no schema change — informational marker via shadow column? Or just code change with no migration needed) | Migration | `ContentBlogs.Infrastructure/Migrations` | Fadwa | T2 |
| 4.23 | EF migration `AddSeoAggregateRootMarkers` (informational; no schema change) | Migration | `ContentSeo.Infrastructure/Migrations` | Mohammad | T3 |

---

## 5. Pre-Work (PW-1 … PW-7) — Tech Lead drives, **MUST land before Wed 2026-05-27 09:00**

> **Why pre-work matters:** The repo is in the same state ContentTours found itself before PW-2 of that sprint — UoW does not dispatch domain events, several entities are property-bags without invariants, and permission catalogs are missing. If we let task devs start before pre-work merges, we rebuild ContentTours' "events silently dropped" bug class (gotcha #1).

### PW-1 — Fix `ContentBlogsUnitOfWork` and `ContentSeoUnitOfWork` to dispatch domain events  ⚠️ CRITICAL

**Current bug** (file: `ContentBlogs.Infrastructure/Persistence/ContentBlogsUnitOfWork.cs`):

```csharp
internal sealed class ContentBlogsUnitOfWork(ContentBlogsDbContext context) : IContentBlogsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
```

This calls `context.SaveChangesAsync` directly, bypassing `UnitOfWork<ContentBlogsDbContext>` (from SharedKernel) which performs the **collect-domain-events → clear-events → MediatR.Publish (BEFORE save) → SaveChangesAsync** sequence required by Rule §2.5.

**Required fix** (mirror `ContentTours` PW-2 from previous sprint):

```csharp
internal sealed class ContentBlogsUnitOfWork(IUnitOfWork<ContentBlogsDbContext> inner) : IContentBlogsUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => inner.SaveChangesAsync(ct);
}
```

**ContentSeo gets the identical treatment** — same file pattern at `ContentSeo.Infrastructure/Persistence/ContentSeoUnitOfWork.cs`.

**Acceptance gate:**
1. New unit test in `ContentBlogs.UnitTests/Persistence/ContentBlogsUnitOfWorkDispatchesEventsTests.cs`:
   - Arrange: stub aggregate with one domain event, attach to `ContentBlogsDbContext`.
   - Act: call `IContentBlogsUnitOfWork.SaveChangesAsync`.
   - Assert: MediatR `Publish` was called with that event (via mock `IMediator` or inspecting `OutboxMessages` table).
2. Mirror test in `ContentSeo.UnitTests`.
3. Both pass against current SharedKernel `UnitOfWork<TContext>`.

### PW-2 — Re-align `ReactionType` enum to PDF spec + EF migration

**Current code (`ContentBlogs.Domain.Enums.ReactionType`):**
```csharp
public enum ReactionType : byte { Like = 0, Dislike = 1, Love = 2, Helpful = 3 }
```

**Required (per Business Rules PDF §9.2 — only positive reactions; no Dislike v1):**
```csharp
public enum ReactionType : byte { Like = 0, Helpful = 1, Insightful = 2 }
```

**Migration:** `AlignReactionTypeEnum` (file: `ContentBlogs.Infrastructure/Migrations/yyyyMMddHHmmss_AlignReactionTypeEnum.cs`).
- `Up`: nothing schema-side (column stays `tinyint`); migration body just records the enum redefinition. No production data exists yet (module never shipped), so no DELETE/UPDATE clauses needed.
- `Down`: revert enum literal mapping only; no schema change.

**Validator follow-up:** add `BlogCommentReactionRequestValidator` in T2 with `RuleFor(x => x.ReactionType).IsInEnum()` — rejects any client-supplied value outside `0,1,2`.

### PW-3 — Add `IAggregateRoot` to `BlogComment` + seed Domain Events

**Why:** Per gotcha #1, only `IAggregateRoot` entries are scanned by `UnitOfWork<TContext>` for event dispatch. Comments have a real lifecycle (create → edit → delete → admin-restore → react), so they must own their events.

**File changes:**

```csharp
// ContentBlogs.Domain/Entities/BlogComment.cs
public sealed class BlogComment : AuditableEntity, IAggregateRoot
{
    // existing fields unchanged
}
```

**Seed empty domain event records** in `ContentBlogs.Domain/Events/`:

- `BlogCommentCreatedDomainEvent(Guid CommentId, Guid BlogId, Guid? ParentCommentId, Guid UserId)`
- `BlogCommentEditedDomainEvent(Guid CommentId, string OldContent, string NewContent)`
- `BlogCommentDeletedDomainEvent(Guid CommentId, Guid BlogId, Guid DeletedByUserId, BlogCommentDeletionMode Mode)`
- `BlogCommentReactionChangedDomainEvent(Guid CommentId, Guid UserId, ReactionType? OldType, ReactionType? NewType)`

(T2 fills in handlers; PW-3 only seeds the records as compilable empty types so T1 isn't blocked compiling.)

### PW-4 — Add `IAggregateRoot` markers + audit upgrade to ContentSeo entities

**Reasoning:** SeoMetadata, Redirect, FaqItem, SitemapEntry are *long-lived editable entities* whose CUD operations must emit integration events for cross-module consumers. WeatherCache is shorter-lived (12h TTL) but still benefits from soft-delete semantics for "user said refresh while old row in flight".

**File changes:**

```csharp
public sealed class SeoMetadata : AuditableEntity, IAggregateRoot { /* … */ }
public sealed class Redirect    : AuditableEntity, IAggregateRoot { /* … */ }
public sealed class FaqItem     : AuditableEntity, IAggregateRoot { /* … */ }
public sealed class SitemapEntry: AuditableEntity, IAggregateRoot { /* … */ }
public sealed class WeatherCache: AuditableEntity, IAggregateRoot { /* … */ }   // upgraded from BaseEntity
```

**Migration `UpgradeWeatherCacheToAuditableEntity`** adds three columns to `content_seo.WeatherCaches`:
- `IsDeleted bit NOT NULL DEFAULT 0`
- `DeletedAt datetime2 NULL`
- `RowVersion rowversion NOT NULL`

(The other four entities already inherit from `AuditableEntity`; only the marker interface is added — no migration needed for them.)

**Seed empty domain event records** in `ContentSeo.Domain/Events/` so T3 and T4 aren't blocked:

- `SeoMetadataCreatedDomainEvent`, `SeoMetadataUpdatedDomainEvent`, `SeoMetadataDeletedDomainEvent`
- `RedirectCreatedDomainEvent`, `RedirectDeactivatedDomainEvent`, `RedirectChainFlattenedDomainEvent`
- `FaqItemCreatedDomainEvent`, `FaqItemUpdatedDomainEvent`, `FaqItemDeletedDomainEvent`, `FaqItemReorderedDomainEvent`

### PW-5 — Seed repository interfaces (compile-only stubs)

Tech Lead writes interface signatures only; method bodies live in the per-task repos. This unblocks parallel handler authoring in week 1.

**Files (interfaces in Application, EF impls in Infrastructure):**

| Interface (Application) | EF impl class (Infrastructure) | Base class | Owner |
|---|---|---|---|
| `IBlogRepository` | `BlogRepository : EfRepository<Blog, Guid, ContentBlogsDbContext>` | aggregate | T1 / Fadwa |
| `IBlogCommentRepository` | `BlogCommentRepository : EfRepository<BlogComment, Guid, ContentBlogsDbContext>` | aggregate | T2 / Fadwa |
| `IBlogTourLinkRepository` | `BlogTourLinkRepository(ContentBlogsDbContext db)` (no base — junction-direct) | junction | T2 / Mahmoud |
| `ISeoMetadataRepository` | `SeoMetadataRepository : EfRepository<SeoMetadata, Guid, ContentSeoDbContext>` | aggregate | T3 / Mohammad |
| `IRedirectRepository` | `RedirectRepository : EfRepository<Redirect, Guid, ContentSeoDbContext>` | aggregate | T3 / Mohammad |
| `IFaqItemRepository` | `FaqItemRepository : EfRepository<FaqItem, Guid, ContentSeoDbContext>` | aggregate | T3 / Mohammad |
| `ISitemapEntryRepository` | `SitemapEntryRepository : EfRepository<SitemapEntry, Guid, ContentSeoDbContext>` | aggregate | T4 / Mohammad |
| `IWeatherCacheRepository` | `WeatherCacheRepository : EfRepository<WeatherCache, Guid, ContentSeoDbContext>` | aggregate | T4 / Mohammad |

**Important re gotcha #6:** `EfRepository<T, TKey, TContext>` requires `T : IAggregateRoot`. After PW-3 + PW-4, every repository above is valid. Junction `BlogTour` uses `IBlogTourLinkRepository` (DbContext-direct, no base — gotcha #7).

### PW-6 — Seed permission catalogs for both modules

**File: `ContentBlogs.Contracts/Authorization/ContentBlogsFeatures.cs`**

```csharp
namespace ContentBlogs.Contracts.Authorization;

public static class ContentBlogsFeatures
{
    public const string Blog            = "ContentBlogs.Blog";
    public const string BlogComment     = "ContentBlogs.BlogComment";
    public const string BlogReaction    = "ContentBlogs.BlogReaction";
    public const string BlogTourLink    = "ContentBlogs.BlogTourLink";
}
```

**File: `ContentBlogs.Contracts/Authorization/ContentBlogsPermissionCatalog.cs`**

```csharp
public sealed class ContentBlogsPermissionCatalog : IPermissionCatalog
{
    public string Module => "ContentBlogs";

    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(ContentBlogsFeatures.Blog,         AppAction.Create),
        new(ContentBlogsFeatures.Blog,         AppAction.Read),
        new(ContentBlogsFeatures.Blog,         AppAction.Update),
        new(ContentBlogsFeatures.Blog,         AppAction.Delete),
        new(ContentBlogsFeatures.Blog,         AppAction.Approve),  // Publish/Unpublish/Archive
        new(ContentBlogsFeatures.BlogComment,  AppAction.Create),
        new(ContentBlogsFeatures.BlogComment,  AppAction.Update),   // edit own
        new(ContentBlogsFeatures.BlogComment,  AppAction.Delete),
        new(ContentBlogsFeatures.BlogComment,  AppAction.Manage),   // admin-side moderation
        new(ContentBlogsFeatures.BlogReaction, AppAction.Create),
        new(ContentBlogsFeatures.BlogReaction, AppAction.Delete),
        new(ContentBlogsFeatures.BlogTourLink, AppAction.Create),
        new(ContentBlogsFeatures.BlogTourLink, AppAction.Delete),
    ];
}
```

**Register** in `ContentBlogs.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddSingleton<IPermissionCatalog, ContentBlogsPermissionCatalog>();
```

**File: `ContentSeo.Contracts/Authorization/ContentSeoFeatures.cs`**

```csharp
public static class ContentSeoFeatures
{
    public const string SeoMetadata = "ContentSeo.SeoMetadata";
    public const string Redirect    = "ContentSeo.Redirect";
    public const string Sitemap     = "ContentSeo.Sitemap";
    public const string FaqItem     = "ContentSeo.FaqItem";
    public const string Weather     = "ContentSeo.Weather";
}
```

`ContentSeoPermissionCatalog` mirrors above pattern with read+create+update+delete on each feature, plus `Manage` on Sitemap (for force-regenerate trigger).

**Register** in `ContentSeo.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddSingleton<IPermissionCatalog, ContentSeoPermissionCatalog>();
```

`PermissionSeeder` auto-discovers both catalogs at startup.

### PW-7 — Sanity test for end-to-end domain-event → outbox → integration-event round trip

**File: `ContentBlogs.IntegrationTests/EventDispatchSanityTests.cs`**

- Arrange in-memory app with `WebApplicationFactory<Program>`, real SharedKernel UoW + outbox processor.
- Create a `Blog` aggregate, raise a stub `BlogCreatedDomainEvent`, save via `IContentBlogsUnitOfWork`.
- Verify:
  1. `OutboxMessages` table got one row with logical name `content-blogs.blog.created.v1`.
  2. `CompositeOutboxProcessor.PollOnceAsync()` runs successfully.
  3. The row is marked `ProcessedAt` non-null after successful publish.
- Mirror in `ContentSeo.IntegrationTests/EventDispatchSanityTests.cs`.

**Both tests run on every PR via the `dotnet test` step in CI.**

### Pre-work merge checklist (Tech Lead signs off)

- [ ] PW-1 merged. `dotnet test` green for both modules.
- [ ] PW-2 enum + migration merged. Verify `dotnet ef migrations list --project ContentBlogs.Infrastructure` shows `AlignReactionTypeEnum`.
- [ ] PW-3 `BlogComment : IAggregateRoot` + 4 stub event records compiled.
- [ ] PW-4 ContentSeo aggregates marked + 11 stub event records compiled. `UpgradeWeatherCacheToAuditableEntity` migration applied.
- [ ] PW-5 8 repository interface stubs compiled (no method bodies — that's owner work).
- [ ] PW-6 Both permission catalogs registered. Run app locally, verify `PermissionSeeder` logs `Discovered 2 catalogs: ContentBlogs (13 perms), ContentSeo (≥15 perms)`.
- [ ] PW-7 Sanity tests pass.
- [ ] Lead announces in standup: "Pre-work green. Task 1/2/3/4 unblocked at $TIMESTAMP."

---

## 6. Critical Rules for ALL Tasks (additive on top of `agent-context.md` §0.3 — Five Non-Negotiable Rules)

> **If your PR violates ANY of these, the reviewer auto-rejects without a re-review window.** Re-fix and re-submit.

1. **Authorization** — every endpoint MUST have either:
   - `.WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.X, AppAction.Y))`, OR
   - `.AllowAnonymous()` (for public read endpoints only — listed explicitly in YallaJo.md endpoint table).
   Forbidden: `.RequireAuthorization()` alone, `.RequireAuthorization("Permission.X.Y")` (string-based), or any role check inside the handler body.
2. **`ICurrentUser` discipline** — inject ONLY for ownership/IDOR/self-edit/creator-stamp comparisons. Never inject just to check `IsAuthenticated` (the auth pipeline already enforced that). Specific allowed call sites this sprint:
   - `BlogComment.Edit`: must verify `currentUser.UserId == comment.UserId` (or admin via permission).
   - `BlogComment.Delete`: same as Edit — owner OR `BlogComment:Manage` permission holder.
   - `BlogCommentReaction.Add/Remove`: `userId = currentUser.UserId` for the reaction's `UserId` column.
   - `Blog.Create`: `authorId = currentUser.UserId` (creator stamp).
3. **Identity** — `Guid.CreateVersion7()` for all new IDs. Never `Guid.NewGuid()`. The factories the lead seeds in PW-3/PW-4 already use V7; downstream handlers must follow.
4. **Time** — `DateTime.UtcNow` ONLY (or better: inject `IDateTimeProvider` and call `.UtcNow`). Never `DateTime.Now`. Sitemap `LastModified`, weather `FetchedAt`, blog `PublishedAt` — all UTC.
5. **CancellationToken** — every async method receives a `CancellationToken ct` parameter and forwards it to every `Async` call inside. The endpoint handler signature is `async (..., CancellationToken ct) => ...` and the call is `mediator.Send(query, ct)`. No `.GetAwaiter().GetResult()` anywhere.
6. **`ILogger<THandler>` injection** — mandatory in EVERY handler (commands AND queries). One `LogInformation` at success, `LogWarning` at expected failures (not-found, validation), `LogError(ex, ...)` at unexpected exceptions only inside whitelisted try/catch (Rule §5.5 of agent-context.md). Reference: ERR-011, 21 retrofits.
7. **Result pattern** — Commands return `Result` / `Result<T>`. Queries return `Result<T>`. Never throw for business failures. Use the **Error tuple form**:
   ```csharp
   return Result.Failure<BlogDto>(new Error("Blog.NotFound", "Blog with id {id} not found."), Outcome.NotFound);
   ```
   **NEVER** the `Result.NotFound(string)` / `Result.Conflict(string)` overloads — they populate `Messages` not `Errors` and break ProblemDetails (ERR-004).
8. **Endpoint return** — `result.ToApiResult()` ONLY. No manual `Results.Ok` / `Results.NotFound` / `Results.Problem` calls.
9. **DTOs not entities** — Queries return DTOs (`BlogSummaryDto` for list, `BlogDetailDto` for detail, `SeoMetadataDto`, etc.). NEVER serialize entity classes.
10. **Caching (per agent-context.md §5.1 + ERR-010)** — every query MUST implement `ICacheableQuery` (CacheKey, CacheDuration, Tags). Every command MUST inject `HybridCache` and call `RemoveByTagAsync(tag, ct)` AFTER successful `SaveChangesAsync`. Use the **most specific tag possible**, e.g. `"blog:{id}"` not `"blogs"`. Use coarse tags only for true bulk operations.
    | Endpoint | Cache key (per call) | Tags to invalidate on mutation |
    |---|---|---|
    | List blogs | `blogs:list:{filterHash}:{page}:{size}` | `blogs:list` (any blog mutation) |
    | Get blog by id | `blog:{id}` | `blog:{id}`, `blogs:list`, `sitemap:blogs` |
    | Get blog by slug | `blog:slug:{slug}` | same as by-id |
    | List comments | `blog:{id}:comments:list:{page}:{size}` | `blog:{id}:comments` |
    | Get SEO metadata | `seo:{entityType}:{entityId}` | `seo:{entityType}:{entityId}`, `sitemap:{entityType}` |
    | Get FAQ | `faq:{entityType}:{entityId}` | `faq:{entityType}:{entityId}` |
    | Get weather | `weather:{placeId}` | (12h absolute TTL, no manual invalidation in normal flow; refresh trigger force-removes) |
    | Sitemap.xml | `sitemap:rendered` | `sitemap:rendered` (refreshed by background svc) |
11. **State-change guards** (ERR-009) — before calling a domain method that raises an event, **guard current state**:
    ```csharp
    if (blog.Status != BlogStatus.Draft)
        return Result.Failure<Guid>(new Error("Blog.InvalidTransition", $"Cannot publish blog in {blog.Status} state."), Outcome.Conflict);
    blog.Publish(_dateTimeProvider.UtcNow);
    ```
    NEVER call a transition method blindly. Each task lists its state machine in the task body — match it exactly.
12. **Try/catch policy** (agent-context.md §5.5 whitelist):
    - **Allowed**: Infrastructure for external-service calls (translation, weather, search-console pings) → catch and return `Result.Failure(...)`. Infrastructure for `DbUpdateConcurrencyException` → `Result.Failure("X.ConcurrencyConflict")`. Background services per-item loops with `LogError`. Application-side `OperationCanceledException when ct.IsCancellationRequested` → rethrow.
    - **Forbidden**: try/catch in endpoints, in command/query handlers for general `Exception`, empty catch, "defensive" catch.
13. **Error codes** — `{Entity}.{Reason}` PascalCase. Per-task catalog must be exhaustive. Examples used this sprint: `Blog.NotFound`, `Blog.SlugConflict`, `Blog.InvalidTransition`, `Blog.PlaceNotFound`, `Blog.MaxToursExceeded`, `BlogComment.RateLimit`, `BlogComment.ProfanityRejected`, `BlogComment.ParentNotInSameBlog`, `BlogComment.NestingTooDeep`, `Redirect.CircularChain`, `Redirect.DuplicateOldUrl`, `FaqItem.OutOfRange`, `Weather.BudgetExhausted`, `Weather.UpstreamUnavailable`, `Sitemap.RenderFailed`.
14. **Translation rules** — every entity with a translation table (Blog, FaqItem) writes the source-language record (English by default) inside the **same** SaveChangesAsync as the parent. The non-source language is filled via `IEntityTranslationOrchestrator` triggered by the entity's domain-event handler. **Never** call `SaveChangesAsync` inside the translation orchestrator's invocation — let the UoW commit it.
15. **Outbox / Integration events** — every `INotificationHandler<IDomainEvent>` writes an `OutboxMessage` to the DbContext only; **NEVER** calls `SaveChangesAsync`. Logical names follow `{module-kebab}.{entity}.{verb}.v1`, e.g., `content-blogs.blog.published.v1`. Each is registered in `IntegrationEventTypeRegistry.cs` (Outbox Hardening PR2 requirement). Inbox consumers in ContentSeo guard via `IContentSeoInboxStore.HasBeenProcessedAsync` → do work → `MarkAsProcessed` → ONE `SaveChangesAsync`.
16. **`dotnet build` green** on the branch BEFORE opening a PR. The full `YallaJo.sln` may still be blocked by the legacy `YallaJo.Web.exe` file lock (audit context note from agent-context.md §8); building only the changed projects (e.g., `dotnet build ContentBlogs.Application/ContentBlogs.Application.csproj` and same for Infrastructure/Presentation) is the workaround until the file lock is unblocked separately.

---

## TASK 1 — Blogs Lifecycle, Translations, View Counter

**Owner:** Fadwa (beginner)  ·  **Endpoints:** 9  ·  **Hours:** 48  ·  **Deadline:** Thu 2026-06-11 17:00
**Earliest start:** Wed 2026-05-27 09:00 (after pre-work merges)
**Dependencies:** PW-1 (UoW dispatch), PW-2 (ReactionType — not blocking T1 but co-merged), PW-5 (`IBlogRepository`), PW-6 (`ContentBlogsFeatures.Blog`).

### 🎯 1.0 Entities Touched

| Entity | Fadwa's responsibility |
|---|---|
| `Blog` (`AuditableEntity, IAggregateRoot`) | Add `Create`, `Update`, `Publish`, `Unpublish`, `Archive`, `SoftDelete` (override), `Feature`, `Unfeature`, `IncrementViewCount`, `LinkToPlace`, `UnlinkFromPlace`. Each raises a domain event. Validate invariants (slug shape, char limits, status guard). |
| `BlogTranslation` (`BaseEntity`) | Add `Create` factory (already present) and `Update(title, content, summary)` business method. T1 wires `BlogCreatedDomainEvent` handler that invokes `IEntityTranslationOrchestrator` to create AR translation record asynchronously. |
| `BlogTour` (junction) | T1 reads only — count of linked tours during `BlogDetailDto` projection. T2 owns mutations. |

### 1.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|---|---|---|---|
| 1 | `GET` | `/api/v1/blogs` | 🌐 Anonymous | `ListBlogsHandler` (paginated, filter by `status`, `placeId`, `tagId`, `categoryId`, `authorId`, `q`, `lang`) |
| 2 | `GET` | `/api/v1/blogs/{id:guid}` | 🌐 Anonymous | `GetBlogByIdHandler` (returns 404 if `Status == Draft` for anonymous; admins see all) |
| 3 | `GET` | `/api/v1/blogs/slug/{slug}` | 🌐 Anonymous | `GetBlogBySlugHandler` (slug index lookup; same Draft-hide rule) |
| 4 | `POST` | `/api/v1/blogs` | 👑 `Blog:Create` | `CreateBlogHandler` (creates Draft) |
| 5 | `PUT` | `/api/v1/blogs/{id:guid}` | 👑 `Blog:Update` | `UpdateBlogHandler` (allowed in Draft + Published; locks slug edit when Published unless `forceSlugChange=true` which auto-creates redirect via `RedirectCreatedIntegrationEvent`) |
| 6 | `DELETE` | `/api/v1/blogs/{id:guid}` | 👑 `Blog:Delete` | `DeleteBlogHandler` (soft-delete via `AuditableEntity.SoftDelete()`) |
| 7 | `POST` | `/api/v1/blogs/{id:guid}/publish` | 👑 `Blog:Approve` | `PublishBlogHandler` (Draft → Published) |
| 8 | `POST` | `/api/v1/blogs/{id:guid}/unpublish` | 👑 `Blog:Approve` | `UnpublishBlogHandler` (Published → Draft) |
| 9 | `POST` | `/api/v1/blogs/{id:guid}/archive` | 👑 `Blog:Approve` | `ArchiveBlogHandler` (Published → Archived) |

> **Total = 9 endpoints.** The 6 endpoints in YallaJo.md remaining (comments × 4 + reactions × 2 + blog↔tour × 2 = 8) are owned by **Task 2 — Fadwa (comments + reactions, endpoints 1–6) and Mahmoud (Blog↔Tour links, endpoints 7–8)**. Sprint-wide: 9 + 8 + 11 + 7 = 35 endpoints. The mismatch with the 14-row YallaJo.md ContentBlogs table is `archive` and the explicit `feature/unfeature` toggles — both confirmed in Business Rules PDF §9.2 (Archive status) and §9.2 (`IsFeatured` field present). T1 ships the Archive endpoint; Feature toggle is **deferred to T2 backlog** if hours remain (see "Out-of-Scope" §16).

### 1.2 Blog State Machine

```
                      ┌────────────────┐
                      │     <create>   │
                      └────────┬───────┘
                               │
                               ▼
        ┌─────────┐  publish  ┌─────────────┐  archive  ┌──────────┐
        │  DRAFT  ├──────────►│  PUBLISHED  ├──────────►│ ARCHIVED │
        │         │◄──────────┤             │           │          │
        └────┬────┘ unpublish └──────┬──────┘           └────┬─────┘
             │                       │                       │
             │ delete (soft)         │ delete (soft)         │ delete (soft)
             ▼                       ▼                       ▼
        ┌──────────────────────────────────────────────────────┐
        │                  IsDeleted = true                    │
        │     row hidden from public list, kept for audit       │
        └──────────────────────────────────────────────────────┘

State guards (ERR-009):
  – Publish requires Status == Draft               (else Blog.InvalidTransition, 409)
  – Unpublish requires Status == Published         (else Blog.InvalidTransition, 409)
  – Archive  requires Status == Published          (else Blog.InvalidTransition, 409)
  – Update   allowed in Draft and Published only   (Archived is read-only besides Restore)
  – Delete   allowed in any state (always permitted by admin)
  – Restore (out-of-scope this sprint, see §16)
```

### 1.3 Pre-Publish Validation Gate (`PublishBlogHandler`)

| # | Check | Error code | HTTP | Reason |
|---|---|---|---|---|
| 1 | Blog exists and `IsDeleted == false` | `Blog.NotFound` | 404 | Cannot publish a deleted/missing blog. |
| 2 | `Status == Draft` | `Blog.InvalidTransition` | 409 | Already Published or Archived. |
| 3 | `Title` non-empty, ≤ 500 chars | `Blog.TitleInvalid` | 400 | PDF §9.2 Title rule. |
| 4 | `Slug` non-empty, unique across non-deleted blogs | `Blog.SlugConflict` | 409 | PDF §9.2 + tours' dedup pattern: append 4-char random suffix, retry up to 3× before failing. |
| 5 | `Content` length ≥ 150 chars (after stripping HTML tags) | `Blog.ContentTooShort` | 400 | Quality bar; matches Tour Description≥100 logic. |
| 6 | `Summary` ≤ 1000 chars (or `null`) | `Blog.SummaryTooLong` | 400 | PDF §9.2. |
| 7 | At least one `BlogTranslation` exists for the source language (English) | `Blog.NoTranslation` | 409 | Backfill is async via orchestrator; English source must already exist. |
| 8 | If `PlaceId.HasValue`: `IPlaceExistsService.ExistsAndIsNotDeletedAsync(placeId, ct) == true` | `Blog.PlaceNotFound` | 422 | PDF §9.3 edge case. |
| 9 | If linked tours present: every `BlogTour.TourId` references a non-deleted Tour via `ITourExistsService.ExistsBatchAsync(tourIds, ct)` | `Blog.TourNotFound` | 422 | PDF §9.3 edge case. |
| 10 | Blog has at least one image attachment via `IAttachmentQueryService` (entityType = "Blog") | `Blog.NoImage` | 422 | OG image generator needs at least one image (SEO §8.2 OgImageUrl rule). |

All checks are aggregated and returned together in the `Errors` collection of `Result.Failure` — endpoint emits a single 4xx with all problems listed (matches Tour Submit gate behaviour from ContentTours sprint).

### 1.4 Business Rules

#### B1 — Domain Invariants

1. `Title` non-empty, trimmed, ≤ 500 chars.
2. `Slug` matches regex `^[a-z0-9]+(?:-[a-z0-9]+)*$`, length ≤ 200, **globally unique among non-deleted Blogs**. Auto-generated by `SlugGenerator.FromTitle(title)`. On collision, append `-{4 random alnum}`, retry 3 times; final failure → `Blog.SlugConflict`.
4. `AuthorId` set at creation from `ICurrentUser.UserId`; immutable thereafter.
5. `ViewCount` starts at 0, monotonically increasing, mutated only by `IncrementViewCount(...)`.
6. `ReadTimeMinutes` recomputed on every `Update(content)` as `Math.Max(1, wordCount / 200)`.
7. `PublishedAt` is **first** publish timestamp — set on first `Publish()`, **never overwritten** on subsequent publish/unpublish round-trips. (Test scenario T1.10.6.)
8. `MetaTitle` ≤ 60 chars, `MetaDescription` ≤ 160 chars (mirrors PDF §8.2 Meta Tag Rules). If null, downstream SEO fallback uses entity name + first 160 chars of content.
9. `PlaceId` nullable; FK conceptually points at `content_places.Places.Id` but FOREIGN KEY constraint is **not** enforced cross-schema (per microservice-style schema isolation pattern used in this repo). Existence check via `IPlaceExistsService` at command time.
10. `IsFeatured` toggle limited to one Featured blog per `(PlaceId, Language)` pair (enforced by command, not DB constraint — see edge case in §1.6).

#### B2 — Authorization Matrix

| Endpoint | Anonymous | Authenticated user | Blog owner (author) | Admin (`Blog:Approve`) | Notes |
|---|:-:|:-:|:-:|:-:|---|
| GET list | ✅ (Published only) | ✅ (Published only) | ✅ (sees own Drafts too only via separate `?includeDrafts=true&authorId=me`) | ✅ (sees all + filter by status) | Anonymous never sees Draft. |
| GET by id | ✅ (Published+Archived only) | ✅ (Published+Archived only) | ✅ (sees own Draft) | ✅ | Archived returns 200 + `IsArchived=true` flag in DTO so UI can render banner. |
| GET by slug | Same as GET by id | Same | Same | Same | — |
| POST create | ❌ 401 | ❌ 403 | n/a | ✅ | v1 admin-only per PDF §9.2. |
| PUT update | ❌ 401 | ❌ 403 | ❌ 403 (admin-only authoring v1) | ✅ | Future: provider-authored will widen. |
| DELETE | ❌ 401 | ❌ 403 | ❌ 403 | ✅ | Soft-delete only. |
| POST publish | ❌ 401 | ❌ 403 | ❌ 403 | ✅ | — |
| POST unpublish | ❌ 401 | ❌ 403 | ❌ 403 | ✅ | — |
| POST archive | ❌ 401 | ❌ 403 | ❌ 403 | ✅ | — |

#### B3 — State Transition Table

| From | Trigger / endpoint | Precondition | Post-state | Domain event raised | Side effects in event handler |
|---|---|---|---|---|---|
| (none) | `POST /api/v1/blogs` | All gate-2 checks pass | `Draft` | `BlogCreatedDomainEvent` | (1) `BlogTranslation` for source lang created via orchestrator. (2) Outbox emits `content-blogs.blog.created.v1`. |
| `Draft` | `POST /publish` | Pre-publish gate (1.3) | `Published` | `BlogPublishedDomainEvent` | (1) Outbox emits `content-blogs.blog.published.v1` → consumed by `ContentSeo.SitemapEntries` ⇒ new entry, by `Messaging` ⇒ "new article" notification subscribers, by `Analytics` ⇒ pageview tracker primed. (2) `PublishedAt` set on aggregate. |
| `Published` | `POST /unpublish` | none beyond status guard | `Draft` | `BlogUnpublishedDomainEvent` | Outbox emits `content-blogs.blog.unpublished.v1` → SEO removes sitemap entry; Messaging undoes notification stagger. |
| `Published` | `POST /archive` | none beyond status guard | `Archived` | `BlogArchivedDomainEvent` | Outbox emits `content-blogs.blog.archived.v1` → SEO keeps sitemap entry but lowers priority to 0.3 (per §8.2 priority rule for stale content). |
| `Draft` ∪ `Published` | `PUT /{id}` | content invariants | unchanged | `BlogUpdatedDomainEvent` (with `OldSlug, NewSlug, FieldsChanged[]`) | If `OldSlug != NewSlug`: outbox emits `content-blogs.blog.slug-changed.v1` → ContentSeo creates 301 Redirect oldUrl→newUrl. Always: orchestrator re-translates if `Title|Content|Summary` changed. |
| any | `DELETE /{id}` | exists+notDeleted | `IsDeleted=true` | `BlogDeletedDomainEvent` | Outbox emits `content-blogs.blog.deleted.v1` → SEO deactivates sitemap entry, Messaging cancels notifications, `BlogTours` rows cleaned up by ContentBlogs domain handler (within same UoW commit). |
| `Published` | View pageview (anonymous) | debounce check via `IBlogViewCounter` | unchanged | `BlogViewedDomainEvent` (rare — only when actually incremented) | Direct DB UPDATE on `Blogs.ViewCount` via raw SQL is **prohibited**; must go through aggregate. Outbox emits `content-blogs.blog.viewed.v1` for Analytics roll-up only when increment actually occurred. |

#### B4 — Error Code Catalog

| Code | HTTP | Trigger |
|---|---|---|
| `Blog.NotFound` | 404 | `repository.GetByIdAsync` returns null OR `IsDeleted == true`. |
| `Blog.SlugConflict` | 409 | Slug already exists after 3 retries with random suffix. |
| `Blog.InvalidTransition` | 409 | Status guard fails (e.g., publish on Archived). |
| `Blog.TitleInvalid` | 400 | Empty or > 500 chars. |
| `Blog.SlugInvalid` | 400 | Regex violation (only on explicit `slug` override in update payload). |
| `Blog.ContentTooShort` | 400 | < 150 chars stripped. |
| `Blog.SummaryTooLong` | 400 | > 1000 chars. |
| `Blog.NoTranslation` | 409 | Source language translation missing at publish time. |
| `Blog.PlaceNotFound` | 422 | `IPlaceExistsService` returns false. |
| `Blog.TourNotFound` | 422 | Any linked tour missing. |
| `Blog.NoImage` | 422 | No attachment of kind Image found. |
| `Blog.MaxToursExceeded` | 409 | Sum of existing + requested > 10. (Owned by T2 but error code declared here for catalog completeness.) |
| `Blog.ConcurrencyConflict` | 409 | `DbUpdateConcurrencyException` — RowVersion mismatch during update/publish. |
| `Blog.PlaceLinkRemovedDueToPlaceDeletion` | (200 + warning header) | Caused by ContentSeo's `PlaceDeletedIntegrationEvent` consumer reaching back to ContentBlogs — informational, not an error. |

#### B5 — Cache Policy

**Queries (per Critical Rules §10):**

| Query | Cache key | Absolute TTL | L1 TTL | Tags |
|---|---|---|---|---|
| `ListBlogsQuery(status, placeId, tagId, categoryId, authorId, q, lang, page, size, sort)` | `blogs:list:{stableHash(args)}:{page}:{size}` | 5 min | 2 min | `blogs:list`, plus `blog:place:{placeId}` if placeId non-null |
| `GetBlogByIdQuery(id, lang)` | `blog:{id}:lang:{lang}` | 5 min | 2 min | `blog:{id}` |
| `GetBlogBySlugQuery(slug, lang)` | `blog:slug:{slug}:lang:{lang}` | 5 min | 2 min | `blog:{id}` (resolved via internal slug→id lookup, then tag with id) |

**Command invalidation matrix:**

| Command | Tags removed (specific first) |
|---|---|
| `CreateBlogCommand` | `blogs:list` |
| `UpdateBlogCommand` | `blog:{id}`, `blogs:list` (always — fields like sort, filters may differ) |
| `DeleteBlogCommand` | `blog:{id}`, `blogs:list`, `sitemap:rendered` |
| `PublishBlogCommand` | `blog:{id}`, `blogs:list`, `sitemap:rendered` |
| `UnpublishBlogCommand` | `blog:{id}`, `blogs:list`, `sitemap:rendered` |
| `ArchiveBlogCommand` | `blog:{id}`, `blogs:list`, `sitemap:rendered` |

> **Specificity rule (gotcha #15, ERR-010):** Always remove `blog:{id}` first; `blogs:list` is paginated and must die for any list rebuild.

#### B6 — Translation Handling

- **Source language:** English (`en`). Determined by application setting `ContentBlogs:DefaultSourceLanguage` (config). Migration adds index on `(BlogId, LanguageId)` to `BlogTranslations`.
- **Required languages on Publish:** Arabic (`ar`) + English (`en`). PDF §9.2 says "Arabic+English required". Pre-publish gate-7 checks at least the English source exists; the Arabic translation may still be in-flight via `IEntityTranslationOrchestrator` — if not yet present, **it's still allowed to publish** (per pattern from ContentTours: orchestrator backfills asynchronously and the public detail handler falls back to source language with a `LangFallback=true` flag in the DTO).
- **`BlogCreatedDomainEvent` handler** (`ContentBlogs.Infrastructure/EventHandlers/BlogCreatedDomainEventHandler.cs`):

```csharp
public sealed class BlogCreatedDomainEventHandler(
    IEntityTranslationOrchestrator orchestrator,
    IDbContext db,
    IDateTimeProvider clock,
    ILogger<BlogCreatedDomainEventHandler> log)
    : INotificationHandler<BlogCreatedDomainEvent>
{
    public async Task Handle(BlogCreatedDomainEvent e, CancellationToken ct)
    {
        try
        {
            await orchestrator.QueueAsync(
                entityType: "Blog",
                entityId:   e.BlogId,
                fields: new()
                {
                    ["Title"]   = e.Title,
                    ["Content"] = e.Content,
                    ["Summary"] = e.Summary ?? string.Empty,
                },
                sourceLanguage: "en",
                ct);

            db.Set<OutboxMessage>().Add(OutboxMessage.From(
                logicalName: "content-blogs.blog.created.v1",
                payload:     new BlogCreatedIntegrationEvent(e.BlogId, e.AuthorId, e.Slug, e.Title, clock.UtcNow)));

            log.LogInformation("Queued translations + outbox for new blog {BlogId}", e.BlogId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogError(ex, "BlogCreatedDomainEventHandler failed for blog {BlogId}", e.BlogId);
            throw;  // bubble up → UoW rolls back parent SaveChanges
        }
        // NEVER call SaveChangesAsync here. UoW does it after all handlers complete.
    }
}
```

- `BlogUpdatedDomainEventHandler`: if `e.FieldsChanged` includes any of `Title|Content|Summary`, requeue translation. If `e.OldSlug != e.NewSlug`, also write `RedirectCreatedIntegrationEvent` to outbox so ContentSeo creates the 301.

#### B7 — Concurrency Handling

- All update/publish/unpublish/archive/delete commands inject `IBlogRepository` and load with tracking enabled. The `RowVersion` shadow column on `AuditableEntity` is configured `ConcurrencyToken = true` in `BlogConfiguration.cs` (verify after PW-1 — if missing, T1 owner adds it).
- Catch `DbUpdateConcurrencyException` ONLY in `BlogRepository.SaveChangesAsync` (Infrastructure layer, per try/catch whitelist):
```csharp
try { await uow.SaveChangesAsync(ct); }
catch (DbUpdateConcurrencyException ex)
{
    log.LogWarning(ex, "Concurrency conflict on Blog {BlogId}", id);
    return Result.Failure<Guid>(new Error("Blog.ConcurrencyConflict", "Blog was modified by another request. Please refetch and retry."), Outcome.Conflict);
}
```

#### B8 — Audit Logging

| Field | PII? | Log level on success | Log level on failure |
|---|---|---|---|
| `BlogId`, `Slug`, `AuthorId`, `Status` | No | Information | Warning |
| `Title`, `Summary` | No (public content) | Information (truncated to 80 chars) | Warning |
| `Content` (full body) | No (public) but voluminous | NEVER log full body — log only length | Warning (length only) |
| `ICurrentUser.UserId` (admin acting) | Yes (PII) | Information (audit context only) | Warning (audit context only) |
| `IpAddress` | Yes (PII) | Forwarded to audit pipeline only — never raw log | Warning |

All `LogInformation` calls use the structured-logging form: `log.LogInformation("Published blog {BlogId} by {ActorUserId}", id, actorUserId)`. NEVER string interpolation in the message template.

#### B9 — Pagination & Filter Semantics

| Concern | Default | Max | Notes |
|---|---|---|---|
| Page size | 20 | 100 | `?size=` over 100 ⇒ clamp + warning header `X-PageSize-Clamped: true`. |
| Page index | 1 | (n/a) | `?page=` zero or negative ⇒ 400. |
| Sort | `publishedAt:desc` | values: `publishedAt`, `createdAt`, `viewCount`, `title` | Direction `:asc` or `:desc`. Multi-sort not allowed v1. |
| Filter `status` | (anonymous: forced to `Published`) | values: `Draft`, `Published`, `Archived` | Anonymous request with `?status=Draft` → 403 (admins only). |
| Filter `q` (full-text) | none | length ≤ 200 chars | Translates to `EF.Functions.Like("%{q}%")` against `Title`, `Summary`, `Content`. **Note:** v1 is naive LIKE; full-text search migration is Wave 6. |
| Response shape | `PagedResult<BlogSummaryDto>` (existing SharedKernel.Application type) | — | DTO contains `id, slug, title, summary, authorId, status, publishedAt, viewCount, primaryImageUrl, placeId, tourCount, langFallback`. |

#### B10 — Acceptance Test Scenarios

> Each test maps 1:1 to a `[Fact]` in `ContentBlogs.IntegrationTests/BlogLifecycleTests.cs`. **All 12 must pass before the T1 PR is approved.**

1. `Create_Blog_AsDraft_ReturnsCreated_AndEmitsCreatedEvent` — POST creates, returns 201 + Guid, outbox row exists with logical name `content-blogs.blog.created.v1`.
2. `Create_Blog_DuplicateSlug_RetriesUpToThree_ThenReturnsConflict` — seed 4 conflicting slugs, expect `Blog.SlugConflict` 409.
3. `Get_Blog_AsAnonymous_ReturnsOnlyPublished` — Draft+Archived hidden from anonymous list; only Published shown.
4. `Get_Blog_AsAdmin_ReturnsAllStatuses` — admin sees Draft + Published + Archived.
5. `Update_Blog_AsAdmin_ChangesSlug_AutoCreatesRedirect` — slug change emits `RedirectCreatedIntegrationEvent` to outbox (verify table row).
6. `Publish_Draft_AllGateChecksPass_TransitionsToPublished_SetsPublishedAt` — first publish sets `PublishedAt`. Republish later does NOT overwrite.
7. `Publish_AlreadyPublished_ReturnsInvalidTransition` — 409 with `Blog.InvalidTransition`.
8. `Publish_BlogWithMissingTranslation_Returns_NoTranslation` — gate-7 fails.
9. `Unpublish_Published_TransitionsToDraft_KeepsPublishedAt` — `PublishedAt` survives unpublish→republish (immutable per B1.7).
10. `Archive_Published_TransitionsToArchived_LowersSitemapPriority` — outbox row's payload-level priority is 0.3 (verify by deserializing `BlogArchivedIntegrationEvent`).
11. `Delete_Blog_SoftDeletes_HidesFromAllPublic_KeepsInDb` — `IsDeleted=true`, query repo with `IgnoreQueryFilters` shows row.
12. `Concurrency_TwoUpdatesAtOnce_OneSucceeds_OtherGets409Conflict` — verify `Blog.ConcurrencyConflict`.

### 1.5 Domain & Integration Event Handler Logic

**Already shown in B6.** Below is the `BlogPublishedDomainEventHandler` skeleton:

```csharp
public sealed class BlogPublishedDomainEventHandler(
    IDbContext db,
    IDateTimeProvider clock,
    ILogger<BlogPublishedDomainEventHandler> log)
    : INotificationHandler<BlogPublishedDomainEvent>
{
    public Task Handle(BlogPublishedDomainEvent e, CancellationToken ct)
    {
        var integrationEvent = new BlogPublishedIntegrationEvent(
            BlogId:       e.BlogId,
            Slug:         e.Slug,
            Title:        e.Title,
            AuthorId:     e.AuthorId,
            PlaceId:      e.PlaceId,
            PublishedAt:  e.PublishedAt,
            CanonicalUrl: $"/blog/{e.Slug}");

        db.Set<OutboxMessage>().Add(OutboxMessage.From(
            logicalName: "content-blogs.blog.published.v1",
            payload:     integrationEvent));

        log.LogInformation("Outbox row for BlogPublished {BlogId} queued", e.BlogId);
        return Task.CompletedTask;
        // NEVER call SaveChangesAsync here. UoW does it after all handlers complete.
    }
}
```

### 1.6 Integration Events Emitted by ContentBlogs (T1 owns these contracts)

```csharp
// ContentBlogs.Contracts/IntegrationEvents/BlogCreatedIntegrationEvent.cs
namespace ContentBlogs.Contracts.IntegrationEvents;

/// <summary>
/// Raised when an admin creates a new blog draft.
/// Consumers:
///   - Analytics (warm pageview counters bucket)
///   - (No SEO consumption; sitemap waits for Published)
/// </summary>
public sealed record BlogCreatedIntegrationEvent(
    Guid BlogId,
    Guid AuthorId,
    string Slug,
    string Title,
    DateTime CreatedAtUtc) : IIntegrationEvent;

// BlogPublishedIntegrationEvent.cs
/// <summary>
/// Raised when a Draft blog transitions to Published.
/// Consumers:
///   - ContentSeo (creates SitemapEntry, generates SeoMetadata if missing)
///   - Messaging  (sends "new article" notification to subscribers — Wave 6)
///   - Analytics  (primes pageview tracker; sets `IsLive=true`)
/// </summary>
public sealed record BlogPublishedIntegrationEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? PlaceId,
    DateTime PublishedAt,
    string CanonicalUrl) : IIntegrationEvent;

// BlogUnpublishedIntegrationEvent.cs
/// <summary>
/// Raised when Published → Draft.
/// Consumers:
///   - ContentSeo (deactivates SitemapEntry)
///   - Messaging  (cancels in-flight notifications)
/// </summary>
public sealed record BlogUnpublishedIntegrationEvent(
    Guid BlogId,
    string Slug,
    DateTime UnpublishedAt) : IIntegrationEvent;

// BlogArchivedIntegrationEvent.cs
public sealed record BlogArchivedIntegrationEvent(
    Guid BlogId,
    string Slug,
    DateTime ArchivedAt,
    decimal NewSitemapPriority = 0.3m) : IIntegrationEvent;

// BlogUpdatedIntegrationEvent.cs
public sealed record BlogUpdatedIntegrationEvent(
    Guid BlogId,
    string OldSlug,
    string NewSlug,
    IReadOnlyList<string> FieldsChanged,
    DateTime UpdatedAt) : IIntegrationEvent;

// BlogDeletedIntegrationEvent.cs
public sealed record BlogDeletedIntegrationEvent(
    Guid BlogId,
    string Slug,
    DateTime DeletedAt) : IIntegrationEvent;
```

### 1.7 Logical-Name Registration

In `SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`, add:

```csharp
.Register<BlogCreatedIntegrationEvent>     ("content-blogs.blog.created.v1")
.Register<BlogPublishedIntegrationEvent>   ("content-blogs.blog.published.v1")
.Register<BlogUnpublishedIntegrationEvent> ("content-blogs.blog.unpublished.v1")
.Register<BlogArchivedIntegrationEvent>    ("content-blogs.blog.archived.v1")
.Register<BlogUpdatedIntegrationEvent>     ("content-blogs.blog.updated.v1")
.Register<BlogDeletedIntegrationEvent>     ("content-blogs.blog.deleted.v1")
```

### 1.8 Validator Rules (FluentValidation)

```csharp
// ContentBlogs.Application/Validators/CreateBlogCommandValidator.cs
public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500);

        RuleFor(x => x.Slug)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(x => !string.IsNullOrEmpty(x.Slug))
            .MaximumLength(200);

        RuleFor(x => x.Summary)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(BeAtLeast150CharsAfterStripHtml).WithMessage("Content must be at least 150 chars (excluding HTML).");

        RuleFor(x => x.PlaceId)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.MetaTitle));

        RuleFor(x => x.MetaDescription)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.MetaDescription));

        RuleFor(x => x.SourceLanguageCode)
            .NotEmpty()
            .Length(2)
            .Matches("^[a-z]{2}$");
    }

    private static bool BeAtLeast150CharsAfterStripHtml(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        var stripped = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", string.Empty);
        return stripped.Trim().Length >= 150;
    }
}
```

Mirror validators for `UpdateBlogCommandValidator`, `PublishBlogCommandValidator` (just `RuleFor(x => x.BlogId).NotEqual(Guid.Empty)`), `ArchiveBlogCommandValidator`, etc.

### 1.9 WBS — Work Breakdown for Fadwa

> Day codes: Su=Sunday M=Monday T=Tuesday W=Wednesday R=Thursday. **Week 1 starts Sun 2026-05-24.**
> Fadwa's pre-work runs Su–T of Week 1 (PW-1, PW-5, PW-6). Implementation starts Wed.

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 1.1 | PW-1 + PW-5 + PW-6 (lead pre-work, see §5) | 12 | Tue Week 1 (T-W1) |
| 1.2 | `Blog.cs` — add 9 business methods, 9 domain event records, factory `Create()` | 6 | Wed Week 1 (W-W1) |
| 1.3 | `BlogConfiguration.cs` — verify ConcurrencyToken on RowVersion, add slug unique index | 1 | Wed Week 1 |
| 1.4 | `IBlogRepository` impl in `BlogRepository.cs` (`EfRepository<Blog, Guid, ContentBlogsDbContext>`); custom methods `GetBySlugAsync`, `SlugExistsAsync`, `GetByIdWithTranslationsAsync` | 4 | Thu Week 1 (R-W1) |
| 1.5 | `CreateBlogCommand` + handler + validator + `BlogSummaryDto` + `BlogDetailDto` + endpoint `POST /api/v1/blogs` | 5 | Sun Week 2 (Su-W2) |
| 1.6 | `UpdateBlogCommand` + slug-change-detection + `BlogUpdatedDomainEvent` raise + endpoint `PUT /api/v1/blogs/{id}` | 5 | Mon Week 2 (M-W2) |
| 1.7 | `DeleteBlogCommand` + soft-delete cascade to BlogTours rows + endpoint `DELETE /api/v1/blogs/{id}` | 3 | Tue Week 2 (T-W2) |
| 1.8 | `PublishBlogCommand` + 10-check pre-publish gate + endpoint `POST /api/v1/blogs/{id}/publish` | 4 | Wed Week 2 (W-W2) |
| 1.9 | `UnpublishBlogCommand` + endpoint | 1 | Wed Week 2 |
| 1.10 | `ArchiveBlogCommand` + endpoint | 1 | Wed Week 2 |
| 1.11 | `ListBlogsQuery` + `BlogSummaryDto` + filter+sort+pagination + `ICacheableQuery` + endpoint `GET /api/v1/blogs` | 4 | Thu Week 2 (R-W2) |
| 1.12 | `GetBlogByIdQuery` + `BlogDetailDto` (with translations + tour-link summary + comment count) + endpoint | 2 | Sun Week 3 (Su-W3) |
| 1.13 | `GetBlogBySlugQuery` + endpoint | 1 | Sun Week 3 |
| 1.14 | `BlogCreatedDomainEventHandler` + `BlogUpdatedDomainEventHandler` + `BlogPublishedDomainEventHandler` + `BlogUnpublishedDomainEventHandler` + `BlogArchivedDomainEventHandler` + `BlogDeletedDomainEventHandler` (6 handlers) | 4 | Mon Week 3 (M-W3) |
| 1.15 | `IBlogViewCounter` design + Redis-backed implementation + tracking endpoint hook in `GetBlogByIdHandler` | 3 | Tue Week 3 (T-W3) |
| 1.16 | Integration tests B10.1–B10.12 (12 scenarios) | 5 | Wed Week 3 (W-W3) |
| 1.17 | Code review feedback + fixes | 4 | Thu Week 3 (R-W3) |
| 1.18 | Final PR merge | 0 | Thu Week 3 17:00 |
| **TOTAL** | | **65** | (12 pre-work + 53 task work; balanced under the 48-hour task budget by overlapping pre-work with reviewing T2/T3/T4 PRs) |

> **Note on hour overrun:** Fadwa's effective task hours are 48; PW work is folded into her per-task allocation. If PR review takes more than the budgeted slot in week 3, descope item 1.15 (`IBlogViewCounter`) to a stub that always increments (no debounce) — view-debouncing moves to backlog (§16).

### 1.10 Edge Cases to Handle (T1 specifically)

- **Place deletion mid-flight.** ContentSeo emits `PlaceDeletedIntegrationEvent` (existing). T1 ships an **inbox consumer** in `ContentBlogs.Infrastructure/EventHandlers/PlaceDeletedIntegrationEventHandler.cs` that:
  1. Checks inbox via `IContentBlogsInboxStore.HasBeenProcessedAsync`.
  2. Loads all blogs with `PlaceId == event.PlaceId`.
  3. Calls `blog.UnlinkFromPlace()` on each (raises `BlogPlaceUnlinkedDomainEvent`).
  4. `MarkAsProcessed` on inbox.
  5. ONE `SaveChangesAsync` via `IContentBlogsUnitOfWork`.
  Per PDF §9.3 edge case: blog stays Published, just no longer in "Related Read".
- **Slug already-exists from imported legacy data.** Seeder runs before tasks; if conflict, manual seed override sets explicit slug. Document in `Persistence/Seeding/ContentBlogsSeedData.cs`.
- **Title contains emoji.** Slug generator must strip non-ASCII but NOT fail. Test: `🇯🇴 Petra by Night` → slug `petra-by-night`.
- **Republish after unpublish.** `PublishedAt` immutable rule (B1.7) means second publish keeps original timestamp. UI/SEO uses `UpdatedAt` for "last edited" instead.
- **Update during publish race.** Two admins click Publish + Update at the same moment. RowVersion concurrency catches it (B7) — second loses with `Blog.ConcurrencyConflict`.
- **Translation orchestrator down at create time.** `BlogCreatedDomainEventHandler` catches inside whitelisted try/catch (Infra layer external call). On failure, queue is left for the next time the orchestrator becomes available — outbox message still gets written, but translation backfill stays pending. Health endpoint flags the orchestrator status.
- **Place ID provided but Places module not yet deployed.** `IPlaceExistsService` is a **stub returning true** in the ContentTours sprint (per ContentTours stub interface list); when Places module is wired in Wave 6 the stub gets replaced. T1 must **not** assume real place existence — use the stub interface and trust ContentSeo's `PlaceDeletedIntegrationEvent` consumer above for cleanup.
- **Inbound `RedirectCreatedIntegrationEvent` for slug change loops.** If admin changes slug A→B and later B→C, ContentSeo MUST flatten chains (T3 owns); T1 just emits `BlogUpdatedIntegrationEvent` with old+new slug — flattening is downstream.

---

## TASK 2 — Blog Comments, Reactions, and Blog↔Tour Associations

**Owners:** **Fadwa** (endpoints 1–6: comments + reactions) · **Mahmoud** (endpoints 7–8: Blog↔Tour links)  ·  **Endpoints:** 8 (6 + 2)  ·  **Hours:** ~30 (Fadwa) + ~10 (Mahmoud) = 40  ·  **Deadline:** Thu 2026-06-11 17:00
**Earliest start:** Wed 2026-05-27 09:00 (after pre-work merges)
**Dependencies:** PW-1 (UoW dispatch), PW-3 (`BlogComment : IAggregateRoot`), T1's `Blog` aggregate methods (uses `Blog.LinkTour` / `Blog.UnlinkTour`), T1's `IBlogRepository.GetByIdAsync`.

### 🎯 2.0 Entities Touched

| Entity | Owner & responsibility |
|---|---|
| `BlogComment` (`AuditableEntity, IAggregateRoot` post-PW-3) | **Fadwa** — Add `Create`, `Edit`, `SoftDeleteByOwner`, `SoftDeleteByAdmin`, `AddOrReplaceReaction`, `RemoveReaction`, factory + 5 domain events. |
| `BlogCommentReaction` (`BaseEntity`) | **Fadwa** — No business methods; comments call its `Create` factory and the comment aggregate raises `BlogCommentReactionChangedDomainEvent`. |
| `BlogTour` (junction) | **Mahmoud** — Add wrapping logic in `Blog` aggregate: `LinkTour(tourId, sortOrder)` and `UnlinkTour(tourId)` raising `BlogTourLinkedDomainEvent` / `BlogTourUnlinkedDomainEvent`. Mahmoud owns `IBlogTourLinkRepository` (DbContext-direct, gotcha #7). |
| `Blog` (read-only access from T2 path) | **Mahmoud** MAY add the two link methods if Fadwa doesn't get to them in T1; co-ordinate via Slack same day. |

### 2.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|---|---|---|---|
| 1 | `GET` | `/api/v1/blogs/{id:guid}/comments` | 🌐 Anonymous | `ListBlogCommentsHandler` (paginated; sort `createdAt:asc` default; tree-flatten with `parentId` exposed) |
| 2 | `POST` | `/api/v1/blogs/{id:guid}/comments` | 🔒 Authenticated | `AddBlogCommentHandler` (rate-limited, profanity-checked) |
| 3 | `PUT` | `/api/v1/blogs/comments/{commentId:guid}` | 🔒 Authenticated (owner) | `EditBlogCommentHandler` |
| 4 | `DELETE` | `/api/v1/blogs/comments/{commentId:guid}` | 🔒 Authenticated (owner OR `BlogComment:Manage`) | `DeleteBlogCommentHandler` |
| 5 | `POST` | `/api/v1/blogs/comments/{commentId:guid}/reactions` | 🔒 Authenticated | `AddBlogCommentReactionHandler` (idempotent: replaces previous reaction by same user) |
| 6 | `DELETE` | `/api/v1/blogs/comments/{commentId:guid}/reactions` | 🔒 Authenticated | `RemoveBlogCommentReactionHandler` |
| 7 | `POST` | `/api/v1/blogs/{id:guid}/tours` | 👑 `BlogTourLink:Create` | `LinkBlogToToursHandler` (batch — body `{ "tours": [{ "tourId": "…", "sortOrder": 1 }, …] }`) |
| 8 | `DELETE` | `/api/v1/blogs/{id:guid}/tours/{tourId:guid}` | 👑 `BlogTourLink:Delete` | `UnlinkBlogFromTourHandler` |

### 2.2 BlogComment State Machine

```
                         <create>
                           │
                           ▼
                   ┌───────────────┐
                   │   ACTIVE      │
                   │ (visible)     │
                   └──┬──────────┬─┘
                      │          │
              edit    │          │ delete
       (within 30 min)│          │ (owner OR admin)
                      ▼          ▼
              ┌──────────────┐ ┌─────────────────────────────┐
              │ ACTIVE       │ │ SOFT-DELETED                │
              │ (edited tag) │ │ Content = "[deleted]"       │
              └──────┬───────┘ │ replies preserved (visible) │
                     │         │ Reactions hidden in UI      │
                     │         └────────────┬────────────────┘
                     │                      │
              same edit window:             │ admin-only "purge" (Wave 6, out-of-scope)
              up to N edits allowed         │
                                            ▼
                                   (HARD-DELETE deferred to Wave 6)
```

**State guards:**
- `Edit`: requires `comment.IsDeleted == false` AND (`commentUser == ICurrentUser.UserId` OR admin holds `BlogComment:Manage`) AND (`clock.UtcNow - comment.CreatedAt <= 30 minutes` for non-admin owner — admins can edit any time).
- `SoftDelete (owner)`: requires `commentUser == ICurrentUser.UserId` AND `comment.IsDeleted == false`.
- `SoftDelete (admin)`: requires admin holds `BlogComment:Manage` permission. No time limit.
- `AddReaction`/`RemoveReaction`: requires `comment.Blog.Status == Published` (no reactions on Draft blogs even if comments somehow exist; Archived blogs allow reactions but not new comments).

### 2.3 Pre-Submit Validation Gate (`AddBlogCommentHandler`)

| # | Check | Error code | HTTP | Reason |
|---|---|---|---|---|
| 1 | Blog exists, `IsDeleted == false`, `Status == Published` | `Blog.NotFound` (or `BlogComment.NotAcceptingComments` for Draft/Archived) | 404 / 409 | PDF §9.3 — comments rejected on Draft/Archived. |
| 2 | If `ParentCommentId` present: parent exists, not deleted, and `parent.BlogId == request.BlogId` (no cross-blog parents) | `BlogComment.ParentNotInSameBlog` | 422 | Cross-blog reply attack. |
| 3 | If `ParentCommentId` present: nesting depth ≤ 2 (root → reply → reply-of-reply, NO deeper) | `BlogComment.NestingTooDeep` | 422 | PDF §9.2. |
| 4 | `Content` non-empty, ≤ 1000 chars after trim | `BlogComment.ContentInvalid` | 400 | PDF §9.2. |
| 5 | Profanity filter: `IProfanityFilter.IsCleanAsync(content, ct) == true` | `BlogComment.ProfanityRejected` | 422 | PDF §9.2 (same as reviews). |
| 6 | Rate-limit: ≤ 5 comments per user per blog per minute, ≤ 30 per user per hour total. Implemented via `IRateLimiter` (in SharedKernel — verify exists, otherwise stub). | `BlogComment.RateLimit` | 429 | Spam prevention; not in PDF but standard. |

### 2.4 Business Rules

#### B1 — Domain Invariants

1. `Content` length 1..1000 chars after trim. Empty content rejected.
2. `BlogId` immutable after creation.
3. `UserId` immutable after creation. Always `currentUser.UserId` at create time.
4. `ParentCommentId` immutable after creation. Reply-of-reply allowed; reply-of-reply-of-reply rejected (depth ≤ 2 from root).
5. `LikeCount` denormalized cache of `Reactions.Count(r => r.ReactionType == ReactionType.Like)`. Updated via aggregate methods, never set by external code. (Optional v1: skip denormalization, compute in DTO. Fadwa's choice; prefer skip for v1 simplicity, cache via SUM in DTO projection.)
6. After `SoftDelete`, `Content` is replaced with `"[deleted]"` literal (rendered by client). The original content is retained in `OriginalContent` shadow column (for moderation audit, accessible only via admin DB tools — NEVER through public API).
7. Reactions are **per (CommentId, UserId)** unique. Adding a different ReactionType replaces the previous row; same ReactionType is an idempotent no-op (returns 200 OK).
8. Edit history: keep `OriginalContent` shadow column on first edit only. After more edits, only "edited at $time" surfaces in DTO — full history is Wave 6 (audit logs).

#### B2 — Authorization Matrix

| Endpoint | Anonymous | Authenticated user | Comment owner | Admin (`Manage`) |
|---|:-:|:-:|:-:|:-:|
| GET list | ✅ | ✅ | ✅ | ✅ |
| POST add | ❌ 401 | ✅ | (n/a — they're owner-by-creation) | ✅ |
| PUT edit | ❌ 401 | ❌ 403 (not owner) | ✅ (within 30 min) | ✅ (any time) |
| DELETE | ❌ 401 | ❌ 403 | ✅ (any time) | ✅ |
| POST reaction | ❌ 401 | ✅ | ✅ | ✅ |
| DELETE reaction | ❌ 401 | ✅ (only own reaction) | ✅ | ✅ (own only — admin doesn't bulk-purge reactions v1) |
| POST blog→tours link | ❌ 401 | ❌ 403 | n/a | ✅ |
| DELETE blog→tour | ❌ 401 | ❌ 403 | n/a | ✅ |

#### B3 — State Transition Table

| From | Trigger | Precondition | Post-state | Domain event raised | Side effects |
|---|---|---|---|---|---|
| (none) | `POST /comments` | gate 1–6 | `Active` | `BlogCommentCreatedDomainEvent` | Outbox `content-blogs.blog-comment.created.v1` (consumed by Messaging for "@author got reply", Analytics for engagement counters). Cache invalidate `blog:{blogId}:comments`. |
| `Active` | `PUT /comments/{id}` | owner+within-30min OR admin | `Active` (with `EditedAt` timestamp set) | `BlogCommentEditedDomainEvent` | Outbox `content-blogs.blog-comment.edited.v1`. Cache invalidate `blog:{blogId}:comments`. |
| `Active` | `DELETE /comments/{id}` | owner OR admin | `IsDeleted=true`, `Content="[deleted]"`, `OriginalContent` shadow set | `BlogCommentDeletedDomainEvent(Mode=SelfSoftDelete | AdminSoftDelete)` | Outbox `content-blogs.blog-comment.deleted.v1`. Cache invalidate. |
| `Active` | `POST /reactions` (new) | gate (existing comment, not deleted, blog accepting) | unchanged on comment status | `BlogCommentReactionChangedDomainEvent(OldType=null, NewType=X)` | Outbox `content-blogs.blog-comment-reaction.changed.v1`. Cache invalidate `blog:{blogId}:comments` (LikeCount in DTO). |
| `Active` | `POST /reactions` (replacing) | existing reaction with different type | unchanged | `BlogCommentReactionChangedDomainEvent(OldType=Y, NewType=X)` | Same outbox. |
| `Active` | `DELETE /reactions` | reaction exists | unchanged | `BlogCommentReactionChangedDomainEvent(OldType=X, NewType=null)` | Same outbox. |
| (Blog aggregate) | `POST /blogs/{id}/tours` | each tour exists, current count + new ≤ 10 | `Blog.BlogTours` collection appended | `BlogTourLinkedDomainEvent` per tour | Outbox `content-blogs.blog-tour.linked.v1`. Cache invalidate `blog:{id}`, `blogs:list`. |
| (Blog aggregate) | `DELETE /blogs/{id}/tours/{tourId}` | link exists | row removed | `BlogTourUnlinkedDomainEvent` | Outbox `content-blogs.blog-tour.unlinked.v1`. Cache invalidate same. |

#### B4 — Error Code Catalog

| Code | HTTP | Trigger |
|---|---|---|
| `BlogComment.NotFound` | 404 | Comment missing or `IsDeleted==true` (for edit/react/delete by non-admin). |
| `BlogComment.NotAcceptingComments` | 409 | Blog status is Draft (admin-only access path) or Archived (read-only). |
| `BlogComment.ParentNotInSameBlog` | 422 | Cross-blog parent reference. |
| `BlogComment.NestingTooDeep` | 422 | Depth > 2 from root. |
| `BlogComment.ContentInvalid` | 400 | Empty or > 1000 chars. |
| `BlogComment.ProfanityRejected` | 422 | `IProfanityFilter.IsCleanAsync == false`. |
| `BlogComment.RateLimit` | 429 | Per-user-per-blog-per-minute rate exceeded. Includes `Retry-After` header. |
| `BlogComment.NotOwner` | 403 | Non-admin trying to edit/delete someone else's comment. |
| `BlogComment.EditWindowExpired` | 409 | Owner trying to edit > 30 min after creation. |
| `BlogComment.ConcurrencyConflict` | 409 | RowVersion mismatch on edit. |
| `BlogReaction.InvalidType` | 400 | Body's `reactionType` not in {Like, Helpful, Insightful}. |
| `Blog.MaxToursExceeded` | 409 | Existing + new tour links > 10. |
| `Blog.TourLinkAlreadyExists` | 409 | Idempotency: re-linking already-linked tour → 200 (same response, log warning). Or 409 — Mahmoud picks; **decision: 200 idempotent**, since UI may double-click. |
| `Blog.TourNotFound` | 422 | `ITourExistsService.ExistsBatchAsync` returns false for any. |

#### B5 — Cache Policy

**Queries:**

| Query | Cache key | Absolute TTL | L1 TTL | Tags |
|---|---|---|---|---|
| `ListBlogCommentsQuery(blogId, page, size, sort, lang)` | `blog:{blogId}:comments:list:{page}:{size}:{sort}` | 5 min | 2 min | `blog:{blogId}:comments` |

**Command invalidation:**

| Command | Tags removed |
|---|---|
| `AddBlogCommentCommand` | `blog:{blogId}:comments`, also `blog:{blogId}` (DTO has commentCount) |
| `EditBlogCommentCommand` | `blog:{blogId}:comments` (resolve blogId via comment first) |
| `DeleteBlogCommentCommand` | `blog:{blogId}:comments`, `blog:{blogId}` |
| `AddBlogCommentReactionCommand` | `blog:{blogId}:comments` (LikeCount changes) |
| `RemoveBlogCommentReactionCommand` | same |
| `LinkBlogToToursCommand` | `blog:{blogId}`, `blogs:list` |
| `UnlinkBlogFromTourCommand` | `blog:{blogId}`, `blogs:list` |

#### B6 — Translation Handling

Comments are **NOT** translated. Each user posts in their own language; the language is auto-detected via `IRequestContext.Language` (Accept-Language header) and stored on the `BlogComment.LanguageCode` shadow column (add via T2 migration). UI may offer per-user "translate this comment" via on-demand translate later (Wave 6).

#### B7 — Concurrency Handling

- Edit and Delete on `BlogComment` use `RowVersion` from `AuditableEntity`. Standard concurrency catch in repo with `Result.Failure("BlogComment.ConcurrencyConflict")`.
- Reactions are race-prone: two concurrent reaction adds by the same user with different types both arrive. Solution:
  1. Use `INSERT … WHERE NOT EXISTS` for first-time add via raw SQL **or**
  2. Use `IDbContext.SaveChangesAsync` with retry-on-unique-violation: catch SQL Server error number 2627 (unique violation), reload the existing row, replace `ReactionType` if different, save again. Maximum 1 retry. (Standard idempotency pattern; acceptable for low-conflict path.)
- BlogTour link: composite PK `(BlogId, TourId)` enforces uniqueness; second link is caught and returns 200 idempotent (per B4 decision).

#### B8 — Audit Logging

| Field | PII? | Log level on success | Log level on failure |
|---|---|---|---|
| `CommentId`, `BlogId`, `ParentCommentId` | No | Information | Warning |
| `UserId` | Yes (PII) | Information (audit context) | Warning |
| `Content` (raw user text) | Treat as semi-PII | Length-only at Information (`"len=523 chars"`); raw text only at Debug | Length-only at Warning |
| `ReactionType` | No | Information | Warning |
| `IpAddress` | Yes | Audit pipeline only | Warning |

#### B9 — Pagination & Filter Semantics (List Comments)

| Concern | Default | Max | Notes |
|---|---|---|---|
| Page size | 50 | 200 | Comments are smaller than blog summaries; allow more. |
| Page index | 1 | (n/a) | — |
| Sort | `createdAt:asc` | values: `createdAt:asc`, `createdAt:desc`, `likeCount:desc` | `createdAt:asc` is primary because thread reading is chronological. |
| Filter `parentId` | (none — full tree returned) | n/a | Optional: pass `?parentId={guid}` to fetch only that subtree. |
| Filter `includeDeleted` | `false` | true (admin only) | Anonymous/users see soft-deleted as `Content="[deleted]"`; admin gets the original via `?includeDeleted=true&includeOriginal=true`. |
| Response shape | `PagedResult<BlogCommentDto>` | — | DTO has `id, blogId, parentCommentId, userId, userDisplayName, content (or "[deleted]"), likeCount, helpfulCount, insightfulCount, currentUserReaction, createdAt, editedAt, isDeleted, depthFromRoot, repliesCount`. |

#### B10 — Acceptance Test Scenarios

> 14 tests in `ContentBlogs.IntegrationTests/BlogCommentsTests.cs`. **All 14 must pass.**

1. `Add_Comment_OnPublishedBlog_AsAuthenticated_ReturnsCreated`.
2. `Add_Comment_OnDraftBlog_Returns_NotAcceptingComments_409`.
3. `Add_Reply_ToReply_ToReply_Returns_NestingTooDeep_422`.
4. `Add_Comment_WithProfanity_Returns_ProfanityRejected_422` (uses `IProfanityFilter` test stub returning false for keyword "BadWord").
5. `Edit_OwnComment_WithinThirtyMinutes_Succeeds_WithEditedTag`.
6. `Edit_OwnComment_AfterThirtyMinutes_Returns_EditWindowExpired_409`.
7. `Edit_AnotherUsersComment_AsRegular_Returns_NotOwner_403`.
8. `Edit_AnotherUsersComment_AsAdmin_Succeeds_NoTimeLimit`.
9. `Delete_OwnComment_SoftDeletesAndShowsBracketDeletedToOthers_RepliesPreserved`.
10. `Delete_AsAdmin_AnyComment_Succeeds_AndPersistsOriginalInShadow`.
11. `AddReaction_NewType_CreatesRow_FiresReactionChangedEvent_OldNull_NewLike`.
12. `AddReaction_DifferentType_ReplacesPrevious_FiresReactionChangedEvent_OldLike_NewHelpful`.
13. `AddReaction_SameType_Idempotent_Returns200_NoEvent`.
14. `LinkBlogToTours_BatchOf_5_AndExistingHas_6_Returns_MaxToursExceeded_409` (5+6=11 > 10).
15. `LinkBlogToTours_BatchOf_3_DuplicateTourId_Returns200_Idempotent_LogsWarning`.
16. `RateLimit_SixCommentsInOneMinute_BlocksSixthWith_429_AndRetryAfterHeader`.

### 2.5 Domain & Integration Event Handler Logic

```csharp
// ContentBlogs.Infrastructure/EventHandlers/BlogCommentCreatedDomainEventHandler.cs
public sealed class BlogCommentCreatedDomainEventHandler(
    IDbContext db,
    IDateTimeProvider clock,
    ILogger<BlogCommentCreatedDomainEventHandler> log)
    : INotificationHandler<BlogCommentCreatedDomainEvent>
{
    public Task Handle(BlogCommentCreatedDomainEvent e, CancellationToken ct)
    {
        var integration = new BlogCommentCreatedIntegrationEvent(
            CommentId:      e.CommentId,
            BlogId:         e.BlogId,
            ParentCommentId:e.ParentCommentId,
            AuthorUserId:   e.UserId,
            CreatedAt:      clock.UtcNow);

        db.Set<OutboxMessage>().Add(OutboxMessage.From(
            "content-blogs.blog-comment.created.v1", integration));

        log.LogInformation("Outbox queued for new comment {CommentId} on blog {BlogId}", e.CommentId, e.BlogId);
        return Task.CompletedTask;
        // NEVER call SaveChangesAsync here. UoW does it after all handlers complete.
    }
}
```

`BlogCommentReactionChangedDomainEventHandler` similarly writes `content-blogs.blog-comment-reaction.changed.v1` with `(OldType, NewType, UserId, CommentId, BlogId)`.

`BlogTourLinkedDomainEventHandler` writes `content-blogs.blog-tour.linked.v1` with `(BlogId, TourId, SortOrder, LinkedAt)`.

### 2.6 Integration Events Emitted (T2 owns these contracts)

```csharp
// ContentBlogs.Contracts/IntegrationEvents/BlogCommentCreatedIntegrationEvent.cs
/// <summary>
/// Raised when a logged-in user posts a comment (or reply) on a Published blog.
/// Consumers:
///   - Messaging  (notify blog author, notify parent-comment author if reply)
///   - Analytics  (engagement counter)
/// </summary>
public sealed record BlogCommentCreatedIntegrationEvent(
    Guid CommentId,
    Guid BlogId,
    Guid? ParentCommentId,
    Guid AuthorUserId,
    DateTime CreatedAt) : IIntegrationEvent;

// BlogCommentEditedIntegrationEvent
public sealed record BlogCommentEditedIntegrationEvent(
    Guid CommentId,
    Guid BlogId,
    Guid AuthorUserId,
    DateTime EditedAt) : IIntegrationEvent;

// BlogCommentDeletedIntegrationEvent
public sealed record BlogCommentDeletedIntegrationEvent(
    Guid CommentId,
    Guid BlogId,
    Guid DeletedByUserId,
    BlogCommentDeletionMode Mode,    // SelfSoftDelete | AdminSoftDelete
    DateTime DeletedAt) : IIntegrationEvent;

// BlogCommentReactionChangedIntegrationEvent
public sealed record BlogCommentReactionChangedIntegrationEvent(
    Guid CommentId,
    Guid BlogId,
    Guid UserId,
    ReactionType? OldType,
    ReactionType? NewType,
    DateTime ChangedAt) : IIntegrationEvent;

// BlogTourLinkedIntegrationEvent
/// <summary>
/// Raised when an admin associates a tour with a blog post.
/// Consumers:
///   - ContentTours    (could update Tour.BlogReferenceCount cache — Wave 6)
///   - ContentSeo      (refresh sitemap entry's lastModified)
/// </summary>
public sealed record BlogTourLinkedIntegrationEvent(
    Guid BlogId,
    Guid TourId,
    int SortOrder,
    DateTime LinkedAt) : IIntegrationEvent;

// BlogTourUnlinkedIntegrationEvent
public sealed record BlogTourUnlinkedIntegrationEvent(
    Guid BlogId,
    Guid TourId,
    DateTime UnlinkedAt) : IIntegrationEvent;
```

### 2.7 Logical-Name Registration

```csharp
.Register<BlogCommentCreatedIntegrationEvent>          ("content-blogs.blog-comment.created.v1")
.Register<BlogCommentEditedIntegrationEvent>           ("content-blogs.blog-comment.edited.v1")
.Register<BlogCommentDeletedIntegrationEvent>          ("content-blogs.blog-comment.deleted.v1")
.Register<BlogCommentReactionChangedIntegrationEvent>  ("content-blogs.blog-comment-reaction.changed.v1")
.Register<BlogTourLinkedIntegrationEvent>              ("content-blogs.blog-tour.linked.v1")
.Register<BlogTourUnlinkedIntegrationEvent>            ("content-blogs.blog-tour.unlinked.v1")
```

### 2.8 Validator Rules

```csharp
public sealed class AddBlogCommentCommandValidator : AbstractValidator<AddBlogCommentCommand>
{
    public AddBlogCommentCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ParentCommentId)
            .NotEqual(Guid.Empty)
            .When(x => x.ParentCommentId.HasValue);
    }
}

public sealed class EditBlogCommentCommandValidator : AbstractValidator<EditBlogCommentCommand>
{
    public EditBlogCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);
        RuleFor(x => x.NewContent).NotEmpty().MaximumLength(1000);
    }
}

public sealed class AddBlogCommentReactionCommandValidator : AbstractValidator<AddBlogCommentReactionCommand>
{
    public AddBlogCommentReactionCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);
        RuleFor(x => x.ReactionType).IsInEnum();
    }
}

public sealed class LinkBlogToToursCommandValidator : AbstractValidator<LinkBlogToToursCommand>
{
    public LinkBlogToToursCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
        RuleFor(x => x.Links).NotEmpty().Must(l => l.Count <= 10).WithMessage("At most 10 tours per request.");
        RuleForEach(x => x.Links).ChildRules(link =>
        {
            link.RuleFor(l => l.TourId).NotEqual(Guid.Empty);
            link.RuleFor(l => l.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}
```

### 2.9 WBS — Work Breakdown for Fadwa (endpoints 1–6) & Mahmoud (endpoints 7–8)

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 2.1 | Confirm PW-3 + PW-5 land on Tue Week 1; pull rebase | 0.5 | Wed Week 1 (W-W1) |
| 2.2 | `IProfanityFilter` interface in `SharedKernel.Application/Abstractions/Moderation/` + `NoOpProfanityFilter` (returns true) + `KeywordProfanityFilter` (loads list from `appsettings`) + DI registration in SharedKernel | 4 | Wed Week 1 |
| 2.3 | `ITourExistsService` interface in `ContentTours.Application.Interfaces/` + `NoOpTourExistsService` returning true (real impl deferred until ContentTours sprint completes) + DI registration | 1.5 | Wed Week 1 |
| 2.4 | `BlogComment.cs` — add `Create`, `Edit`, `SoftDelete*`, `AddOrReplaceReaction`, `RemoveReaction` business methods + 5 domain event records (re-fill PW-3 stubs) | 5 | Thu Week 1 (R-W1) |
| 2.5 | `BlogCommentConfiguration.cs` — add `OriginalContent` shadow column, `LanguageCode` shadow column, indexes on `(BlogId, IsDeleted)`, `(ParentCommentId, IsDeleted)`. EF migration `AddBlogCommentShadowColumns` | 2 | Thu Week 1 |
| 2.6 | `IBlogCommentRepository` impl + custom queries `GetByIdWithReactionsAsync`, `ListByBlogPaginatedAsync`, `CountByUserPerMinuteAsync` (rate-limit support) | 4 | Sun Week 2 (Su-W2) |
| 2.7 | `AddBlogCommentCommand` + handler + validator + endpoint | 4 | Mon Week 2 (M-W2) |
| 2.8 | `EditBlogCommentCommand` + handler (with 30-min window check + admin override) + validator + endpoint | 3 | Mon Week 2 |
| 2.9 | `DeleteBlogCommentCommand` + handler + validator + endpoint | 2 | Tue Week 2 (T-W2) |
| 2.10 | `AddBlogCommentReactionCommand` + handler (idempotent + replace) + endpoint | 3 | Tue Week 2 |
| 2.11 | `RemoveBlogCommentReactionCommand` + handler + endpoint | 1 | Tue Week 2 |
| 2.12 | `ListBlogCommentsQuery` + `BlogCommentDto` + tree-flatten projection + `ICacheableQuery` + endpoint | 4 | Wed Week 2 (W-W2) |
| 2.13 | `LinkBlogToToursCommand` + handler (uses `Blog.LinkTour` aggregate method, batch + max-10 enforcement) + validator + endpoint | 3 | Wed Week 2 |
| 2.14 | `UnlinkBlogFromTourCommand` + handler + endpoint | 1 | Wed Week 2 |
| 2.15 | `IBlogTourLinkRepository` impl (DbContext-direct, gotcha #7) | 2 | Thu Week 2 (R-W2) |
| 2.16 | `BlogCommentCreatedDomainEventHandler` + `BlogCommentEditedDomainEventHandler` + `BlogCommentDeletedDomainEventHandler` + `BlogCommentReactionChangedDomainEventHandler` + `BlogTourLinkedDomainEventHandler` + `BlogTourUnlinkedDomainEventHandler` (6 handlers) | 4 | Thu Week 2 |
| 2.17 | Integration tests B10.1–B10.16 (16 scenarios) | 5 | Sun Week 3 (Su-W3) |
| 2.18 | Code review feedback + fixes + integration with T1's `Blog` aggregate | 4 | Wed Week 3 (W-W3) |
| 2.19 | Final PR merge | 0 | Thu Week 3 17:00 |
| **TOTAL** | | **49** | (overrun absorbed by buffer; if pressed, defer item 2.10/2.11 reaction endpoints to backlog) |

### 2.10 Edge Cases to Handle (T2 specifically)

- **Reply to a deleted parent.** Allowed: per PDF §9.2 "replies preserved". The reply's `ParentCommentId` still points to the deleted comment; UI shows `[deleted]` parent placeholder. Test: create A, B (reply to A), delete A → B still visible in list with parent marked deleted.
- **Comment on Archived blog.** PDF §9.3: "comments preserved but new comments rejected." T2 enforces: gate-1 returns `BlogComment.NotAcceptingComments` 409 if `Status == Archived`.
- **User logs out mid-edit.** Edit endpoint requires authentication; if token expired, 401. Client retries after re-auth. Edit window check uses `clock.UtcNow - comment.CreatedAt` so token timing doesn't affect.
- **Profanity filter false positive.** PDF says "same as reviews" but reviews module isn't in scope. Stub `KeywordProfanityFilter` reads list from `appsettings.json:Moderation:ProfanityKeywords`. Initially empty list → all comments accepted. Real ML filter is Wave 6.
- **Race: same user double-submits same comment via double-click.** Idempotency: hash `(userId, blogId, parentCommentId, contentSha256)` checked in last 10 seconds via in-memory cache. If duplicate hit, return 200 with existing comment ID. (Optional optimization — falls under 2.7 hours; descope if time-tight.)
- **Race: user posts, then another user posts within 1 second.** Both succeed independently; no conflict.
- **Admin deletes user's last reaction.** Reactions DELETE endpoint uses `currentUser.UserId` to find the row. Admin is "another user" — they can't delete someone else's reaction via this endpoint v1. (Admin moderation of reactions is Wave 6.)
- **Tour link batch overflow.** Submit 11 tours when blog already has 0: validator catches batch>10 (`Blog.MaxToursExceeded`). Submit 5 tours when blog has 6: handler catches `existing+new > 10` → `Blog.MaxToursExceeded`. Test scenarios cover both paths.
- **Same tour linked twice in the same batch.** Validator rejects: `RuleFor(x => x.Links).Must(l => l.Select(x => x.TourId).Distinct().Count() == l.Count)`. Returns 400 `BlogTourLink.DuplicateInBatch`.
- **Tour gets deleted while link exists.** ContentBlogs ships `TourDeletedIntegrationEventHandler` (inbox consumer): on receipt, removes all `BlogTour` rows where `TourId == event.TourId` via DbContext-direct delete. **Per gotcha #7**, this junction repository delete uses `db.BlogTours.Where(...).ExecuteDeleteAsync()` (EF 7+ batch delete) wrapped in same UoW.

---

## TASK 3 — SEO Metadata, Redirects, FAQ Items

**Owner:** Mohammad (intermediate)  ·  **Endpoints:** 11  ·  **Hours:** 44  ·  **Deadline:** Thu 2026-06-11 17:00
**Earliest start:** Wed 2026-05-27 09:00 (after pre-work merges)
**Dependencies:** PW-1 (UoW dispatch in ContentSeo), PW-4 (`SeoMetadata`/`Redirect`/`FaqItem` are aggregate roots), PW-5 (`ISeoMetadataRepository`, `IRedirectRepository`, `IFaqItemRepository`), PW-6 (`ContentSeoFeatures`).

> 📚 **Implementation note:** This task is "mostly CRUD" but with two important twists — (a) **redirect chain flattening** in `Redirect` (algorithmic, see §3.4 B3), and (b) **integration-event listening** for upstream slug changes (the `BlogUpdatedIntegrationEvent` from T1 triggers your redirect creation). Read §3.6 carefully — the inbox-consumer pattern is the hardest part.

### 🎯 3.0 Entities Touched

| Entity | Mohammad's responsibility |
|---|---|
| `SeoMetadata` (`AuditableEntity, IAggregateRoot` post-PW-4) | Add `Update*` business methods (`UpdateMeta`, `UpdateOg`, `UpdateSchema`, `UpdateSitemapHints`) — each raises a domain event. Existing `Create` factory + `UpdateMeta` are pre-seeded; Mohammad adds the rest. |
| `Redirect` (`AuditableEntity, IAggregateRoot` post-PW-4) | Add `Create`, `Activate`, `Deactivate`, `IncrementHit`, `RewriteTo` (used during chain-flattening). 3 domain events: Created, Deactivated, ChainFlattened. |
| `FaqItem` (`AuditableEntity, IAggregateRoot` post-PW-4) | Add `Create`, `Update`, `Reorder`, `Activate`, `Deactivate`, `SoftDelete`. 4 domain events. |
| `FaqItemTranslation` (`BaseEntity`) | Existing `Create` factory; Mohammad adds `Update(question, answer)` business method. T3 wires `IEntityTranslationOrchestrator` for backfill. |

### 3.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|---|---|---|---|
| 1 | `GET` | `/api/v1/seo/metadata/{entityType}/{entityId:guid}` | 🌐 Anonymous | `GetSeoMetadataHandler` (returns 404 if no row) |
| 2 | `POST` | `/api/v1/seo/metadata` | 👑 `SeoMetadata:Create` | `UpsertSeoMetadataHandler` (one per `(entityType, entityId)` — idempotent upsert) |
| 3 | `PUT` | `/api/v1/seo/metadata/{id:guid}` | 👑 `SeoMetadata:Update` | `UpdateSeoMetadataHandler` |
| 4 | `GET` | `/api/v1/seo/redirects` | 👑 `Redirect:Read` | `ListRedirectsHandler` (paginated, filter by `oldUrl`/`newUrl`/`isActive`) |
| 5 | `POST` | `/api/v1/seo/redirects` | 👑 `Redirect:Create` | `CreateRedirectHandler` (chain-flattening + circular detection) |
| 6 | `DELETE` | `/api/v1/seo/redirects/{id:guid}` | 👑 `Redirect:Delete` | `DeleteRedirectHandler` (soft via `Deactivate()` + soft-delete) |
| 7 | `GET` | `/api/v1/seo/faq/{entityType}/{entityId:guid}` | 🌐 Anonymous | `ListFaqItemsHandler` (returns FAQ items with translations for `IRequestContext.Language`) |
| 8 | `POST` | `/api/v1/seo/faq` | 👑 `FaqItem:Create` | `CreateFaqItemHandler` |
| 9 | `PUT` | `/api/v1/seo/faq/{id:guid}` | 👑 `FaqItem:Update` | `UpdateFaqItemHandler` |
| 10 | `DELETE` | `/api/v1/seo/faq/{id:guid}` | 👑 `FaqItem:Delete` | `DeleteFaqItemHandler` (soft) |
| 11 | `PUT` | `/api/v1/seo/faq/reorder` | 👑 `FaqItem:Update` | `ReorderFaqItemsHandler` (batch — body `{ "entityType": "Tour", "entityId": "…", "items": [{"id": "…", "sortOrder": 0}, …] }`) |

### 3.2 Redirect State Machine

```
                <create>
                   │
                   │ chain-flatten + circular-check pass
                   ▼
            ┌─────────────────────┐
            │      ACTIVE         │
            │  (matches in MW)    │
            └──┬─────────────────┬┘
               │                 │
       deactivate │                 │ chain-flatten triggered later
               ▼                 ▼
        ┌────────────────┐ ┌─────────────────────┐
        │  INACTIVE      │ │  REWRITTEN to new  │
        │  (kept for     │ │  target (e.g., A→B  │
        │   audit)       │ │  becomes A→C when   │
        │                │ │  B→C is added)      │
        └────────────────┘ └─────────────────────┘
```

### 3.3 Pre-Submit Validation Gate (`CreateRedirectHandler`)

| # | Check | Error code | HTTP | Reason |
|---|---|---|---|---|
| 1 | `OldUrl` matches regex `^/[a-z0-9\-/]+$` (no query string, no protocol) | `Redirect.OldUrlInvalid` | 400 | Path-only redirects v1. |
| 2 | `NewUrl` matches regex `^(/[a-z0-9\-/]+|https?://[^\s]+)$` (path-only OR full URL) | `Redirect.NewUrlInvalid` | 400 | External redirects allowed for legacy migrations. |
| 3 | `OldUrl != NewUrl` | `Redirect.SameSourceTarget` | 400 | Self-redirect = infinite loop. |
| 4 | `StatusCode ∈ {301, 302}` | `Redirect.InvalidStatusCode` | 400 | Per PDF §8.2. |
| 5 | No active redirect already exists with `OldUrl == request.OldUrl` | `Redirect.DuplicateOldUrl` | 409 | PDF §8.2 — unique active. |
| 6 | Circular detection: walking redirect chain starting from `NewUrl` doesn't loop back to `OldUrl` within 10 hops | `Redirect.CircularChain` | 409 | PDF §8.3 — cycle prevention. |
| 7 | Chain-flatten: if existing `A → request.OldUrl` redirects exist, update them all to point to `request.NewUrl` directly. | (no error — automatic action) | n/a | PDF §8.2 — chain flattening. |

### 3.4 Business Rules

#### B1 — Domain Invariants (per entity)

**`SeoMetadata`:**
1. `(EntityType, EntityId)` is **unique** — enforce via DB unique index. The `Upsert` endpoint handles idempotent insert-or-update.
2. `MetaTitle` ≤ 60 chars; `MetaDescription` ≤ 160 chars (PDF §8.2). Auto-fill from entity name/description if not set when creating.
3. `SitemapPriority ∈ [0.0, 1.0]` (existing factory enforces). Defaults: Tours=0.8, Places=0.9, Businesses=0.7, Blogs=0.6, Static=0.3.
4. `SitemapChangeFrequency ∈ {"always", "hourly", "daily", "weekly", "monthly", "yearly", "never"}`.
5. `CanonicalUrl` matches regex `^(/[a-z0-9\-/]+|https?://[^\s]+)$` if not null.

**`Redirect`:**
1. `OldUrl` is unique among active redirects.
2. `StatusCode ∈ {301, 302}` (RedirectStatusCode enum 4.3).
3. `HitCount` monotonically increasing, mutated only via `IncrementHit()` aggregate method.
4. `IsActive` toggle independent of `IsDeleted`. Inactive redirects are kept for audit; soft-deleted ones go through `AuditableEntity.SoftDelete()` and are excluded from middleware lookup entirely.

**`FaqItem`:**
1. `Question` non-empty, ≤ 500 chars; `Answer` non-empty, ≤ 5000 chars.
2. `(EntityType, EntityId, SortOrder)` ordering: `SortOrder` is unique per `(EntityType, EntityId)` set. Reorder command rebalances.
3. `EntityType ∈ SeoEntityType` (Place, Tour, Business, Blog).

**`FaqItemTranslation`:**
1. `(FaqItemId, LanguageId)` unique.
2. `Question`, `Answer` lengths same as parent.

#### B2 — Authorization Matrix

| Endpoint | Anonymous | Auth user | Admin |
|---|:-:|:-:|:-:|
| GET metadata | ✅ | ✅ | ✅ |
| POST metadata (upsert) | ❌ | ❌ | ✅ |
| PUT metadata | ❌ | ❌ | ✅ |
| GET redirects (admin list) | ❌ | ❌ | ✅ |
| POST redirect | ❌ | ❌ | ✅ |
| DELETE redirect | ❌ | ❌ | ✅ |
| GET faq | ✅ | ✅ | ✅ |
| POST/PUT/DELETE faq | ❌ | ❌ | ✅ |
| PUT faq reorder | ❌ | ❌ | ✅ |

#### B3 — State Transition Table (Redirect with chain-flatten algorithm)

**Algorithm `CreateRedirect(oldUrl, newUrl, statusCode)`:**

```text
1. Validate inputs (gate §3.3 1–4).
2. Check no active redirect with same OldUrl (gate 5).
3. Walk forward from newUrl: while there exists active redirect with OldUrl == currentTarget:
     currentTarget = that redirect.NewUrl
     hopsCounter += 1
     if currentTarget == oldUrl → return Redirect.CircularChain (gate 6).
     if hopsCounter > 10 → return Redirect.CircularChain (depth guard).
4. Define finalTarget = currentTarget (after walking).
5. Find all active redirects pointing to oldUrl: existing = redirects.Where(r => r.NewUrl == oldUrl && r.IsActive).
6. For each `existing.r`:
     r.RewriteTo(finalTarget)
     raises RedirectChainFlattenedDomainEvent(r.Id, oldTarget=oldUrl, newTarget=finalTarget)
7. new Redirect.Create(oldUrl, finalTarget, statusCode)
     raises RedirectCreatedDomainEvent.
8. SaveChanges (UoW dispatches all events at once, ONE commit).
```

**Transition table:**

| From | Trigger | Precondition | Post-state | Domain event | Side effects |
|---|---|---|---|---|---|
| (none) | `POST /redirects` | gate 1–7 | Active | `RedirectCreatedDomainEvent` | Outbox `content-seo.redirect.created.v1` (consumed by Web/Middleware to invalidate redirect cache). Cache `redirects:lookup` invalidated. |
| `Active` | chain-flatten loop | another redirect's `NewUrl == this.OldUrl` AND admin creates that one | unchanged status; `NewUrl` rewritten | `RedirectChainFlattenedDomainEvent(OldTarget, NewTarget)` | Outbox `content-seo.redirect.chain-flattened.v1`. Cache invalidated. |
| `Active` | `DELETE /redirects/{id}` | exists | `IsActive=false`, `IsDeleted=true` | `RedirectDeactivatedDomainEvent` | Outbox `content-seo.redirect.deactivated.v1`. Cache invalidated. |
| `Active` | hit by middleware | request URL matches `OldUrl` | `HitCount++` | (no event — fire-and-forget; UoW dispatch optional, can use raw SQL `UPDATE` to avoid full save round-trip if perf becomes an issue) | None initially; analytics consumes per-request stats from log. |

#### B4 — Error Code Catalog

| Code | HTTP | Trigger |
|---|---|---|
| `SeoMetadata.NotFound` | 404 | No row for `(entityType, entityId)`. |
| `SeoMetadata.Conflict` | 409 | Tried to insert duplicate `(entityType, entityId)` — caller should `PUT` instead. |
| `SeoMetadata.MetaTitleTooLong` | 400 | > 60 chars. |
| `SeoMetadata.MetaDescriptionTooLong` | 400 | > 160 chars. |
| `SeoMetadata.PriorityOutOfRange` | 400 | Outside [0.0, 1.0]. |
| `SeoMetadata.InvalidChangeFrequency` | 400 | Not in enum. |
| `SeoMetadata.ConcurrencyConflict` | 409 | RowVersion. |
| `Redirect.NotFound` | 404 | — |
| `Redirect.OldUrlInvalid` | 400 | regex fail. |
| `Redirect.NewUrlInvalid` | 400 | regex fail. |
| `Redirect.SameSourceTarget` | 400 | OldUrl == NewUrl. |
| `Redirect.InvalidStatusCode` | 400 | Not 301 or 302. |
| `Redirect.DuplicateOldUrl` | 409 | Active redirect exists with same OldUrl. |
| `Redirect.CircularChain` | 409 | Cycle detected. |
| `FaqItem.NotFound` | 404 | — |
| `FaqItem.QuestionInvalid` | 400 | Empty or > 500 chars. |
| `FaqItem.AnswerInvalid` | 400 | Empty or > 5000 chars. |
| `FaqItem.OutOfRange` | 422 | Reorder request includes IDs not belonging to `(entityType, entityId)`. |
| `FaqItem.SortOrderConflict` | 409 | Duplicate sort orders in reorder batch. |
| `FaqItem.ConcurrencyConflict` | 409 | RowVersion. |

#### B5 — Cache Policy

| Query | Cache key | Absolute TTL | L1 TTL | Tags |
|---|---|---|---|---|
| `GetSeoMetadataQuery(entityType, entityId, lang)` | `seo:{entityType}:{entityId}:lang:{lang}` | 30 min | 15 min | `seo:{entityType}:{entityId}` |
| `ListRedirectsQuery(filter, page, size)` | `redirects:list:{stableHash}:{page}:{size}` | 5 min | 2 min | `redirects:list` |
| `ListFaqItemsQuery(entityType, entityId, lang)` | `faq:{entityType}:{entityId}:lang:{lang}` | 30 min | 15 min | `faq:{entityType}:{entityId}` |
| Lookup-by-`OldUrl` (middleware path, NOT a CQRS query but a `IRedirectLookupService.FindAsync`) | `redirects:lookup:{oldUrlHash}` | 5 min | 2 min | `redirects:lookup` |

| Command | Tags removed |
|---|---|
| `UpsertSeoMetadataCommand` | `seo:{entityType}:{entityId}`, `sitemap:{entityType}` |
| `UpdateSeoMetadataCommand` | same |
| `CreateRedirectCommand` | `redirects:list`, `redirects:lookup` (chain-flatten may invalidate many entries) |
| `DeleteRedirectCommand` | same |
| `CreateFaqItemCommand` | `faq:{entityType}:{entityId}` |
| `UpdateFaqItemCommand` | same |
| `DeleteFaqItemCommand` | same |
| `ReorderFaqItemsCommand` | same |

#### B6 — Translation Handling

- `SeoMetadata` does NOT have a translations table v1. The `MetaTitle`/`MetaDescription` are in source language only; translated SEO is "use entity's translated title" downstream — handled by sitemap renderer using `BlogTranslation`/`TourTranslation`/etc.
- `FaqItem` has translations via `FaqItemTranslation`. T3's `FaqItemCreatedDomainEventHandler` invokes `IEntityTranslationOrchestrator.QueueAsync` to backfill non-source languages, exactly mirroring T1's `BlogCreatedDomainEventHandler`.

#### B7 — Concurrency Handling

- All three aggregates inherit from `AuditableEntity` post-PW-4; RowVersion present.
- `CreateRedirectCommand` is the trickiest: chain-flatten modifies multiple rows in one transaction. Standard concurrency catch on the whole UoW. If two admins create overlapping redirects simultaneously, one of them will see `Redirect.ConcurrencyConflict`; client retries.

#### B8 — Audit Logging

Standard structured logging. `MetaTitle`, `MetaDescription`, `Question`, `Answer` are public-facing — logged at Information level. Redirect URLs may contain PII in query strings (rare but possible) — V1 strategy is to **strip everything after `?`** before logging.

#### B9 — Pagination & Filter Semantics

- **List redirects**: page=1, size=50, max=200. Sort `createdAt:desc` default. Filter `?oldUrl=...` (Like) `?isActive=true|false` `?statusCode=301|302`.
- **List FAQ**: no pagination — assume ≤ 50 items per entity (validator caps creation at 50). Sorted by `SortOrder asc`.

#### B10 — Acceptance Test Scenarios

`ContentSeo.IntegrationTests/SeoTests.cs` (cross-cutting):

1. `Get_SeoMetadata_NotFound_Returns_404`.
2. `Upsert_SeoMetadata_FirstCall_Inserts_SecondCall_Updates_Same_Id`.
3. `Update_SeoMetadata_OverlongMetaTitle_Returns_400`.
4. `CreateRedirect_DuplicateOldUrl_Returns_DuplicateOldUrl_409`.
5. `CreateRedirect_AtoB_thenBtoC_FlatensAtoC_AndKeepsBtoC` — verify A's NewUrl is now C, not B.
6. `CreateRedirect_CircularChain_AtoB_existing_BtoC_existing_thenCtoA_Returns_CircularChain_409`.
7. `CreateRedirect_AtoA_Returns_SameSourceTarget_400`.
8. `DeleteRedirect_SetsInactiveAndSoftDeleted_AndExcludedFromLookup`.
9. `ListFaqItems_OnEntity_ReturnsSortedByOrderAsc_WithLangFallbackFlag`.
10. `CreateFaqItem_RaisesEvent_AndQueuesTranslation`.
11. `Reorder_FaqItems_BatchOf_5_AssignsContiguousSortOrder0to4`.
12. `Reorder_FaqItems_WithIdNotInEntity_Returns_OutOfRange_422`.
13. `BlogPublishedIntegrationEvent_Consumed_CreatesSitemapEntry_AndSeoMetadataIfMissing` (cross-cutting with T1+T4).
14. `BlogUpdatedIntegrationEvent_WithSlugChange_Creates301Redirect` (cross-cutting with T1).
15. `PlaceDeletedIntegrationEvent_Consumed_DeactivatesRelatedSitemapEntry_AndSeoMetadata`.

### 3.5 Domain & Integration Event Handler Logic

#### Outgoing (T3 emits)

```csharp
// ContentSeo.Infrastructure/EventHandlers/SeoMetadataCreatedDomainEventHandler.cs
public sealed class SeoMetadataCreatedDomainEventHandler(
    IDbContext db,
    IDateTimeProvider clock,
    ILogger<SeoMetadataCreatedDomainEventHandler> log)
    : INotificationHandler<SeoMetadataCreatedDomainEvent>
{
    public Task Handle(SeoMetadataCreatedDomainEvent e, CancellationToken ct)
    {
        // Internal-only: SEO doesn't widely publish metadata events.
        // Logged for audit; outbox emission is OPTIONAL — Wave 6 may consume.
        log.LogInformation("SEO metadata created for {EntityType} {EntityId}", e.EntityType, e.EntityId);
        return Task.CompletedTask;
        // NEVER call SaveChangesAsync here.
    }
}
```

#### Incoming (T3 consumes — INBOX pattern)

This is the heart of T3's value-add. ContentSeo must consume `BlogPublishedIntegrationEvent` (from T1) and `BlogUpdatedIntegrationEvent` (also T1). The existing `PlaceCreated/Updated/Deleted` and `BusinessCreated` handlers are scaffolds Mohammad must complete.

```csharp
// ContentSeo.Infrastructure/EventHandlers/BlogPublishedIntegrationEventHandler.cs
public sealed class BlogPublishedIntegrationEventHandler(
    IContentSeoInboxStore inbox,
    ISeoMetadataRepository seoRepo,
    ISitemapEntryRepository sitemapRepo,
    IContentSeoUnitOfWork uow,
    HybridCache cache,
    IDateTimeProvider clock,
    ILogger<BlogPublishedIntegrationEventHandler> log)
    : INotificationHandler<IntegrationEventNotification<BlogPublishedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<BlogPublishedIntegrationEvent> notification,
        CancellationToken ct)
    {
        var e = notification.Event;
        var inboxKey = notification.MessageId;

        // ── 1. Inbox guard (idempotency) ────────────────────────────────────
        if (await inbox.HasBeenProcessedAsync(inboxKey, "BlogPublished", ct))
        {
            log.LogInformation("BlogPublished {BlogId} already processed (inbox key {Key})", e.BlogId, inboxKey);
            return;
        }

        // ── 2. Side-effect work ─────────────────────────────────────────────
        var existingSeo = await seoRepo.GetByEntityAsync(SeoEntityType.Blog, e.BlogId, ct);
        if (existingSeo is null)
        {
            // Auto-create SEO metadata using fallback rules from §8.2
            var seo = SeoMetadata.Create(
                entityType:      SeoEntityType.Blog,
                entityId:        e.BlogId,
                metaTitle:       e.Title.Length > 60 ? e.Title[..60] : e.Title,
                metaDescription: null,    // backfilled later by T4 sitemap renderer using blog summary
                canonicalUrl:    e.CanonicalUrl,
                sitemapPriority: 0.6m,
                sitemapChangeFrequency: "monthly");
            await seoRepo.AddAsync(seo, ct);
        }

        var existingSitemap = await sitemapRepo.GetByEntityAsync("Blog", e.BlogId, ct);
        if (existingSitemap is null)
        {
            var entry = SitemapEntry.Create(
                url:             e.CanonicalUrl,
                entityType:      "Blog",
                entityId:        e.BlogId,
                changeFrequency: "monthly",
                priority:        0.6m);
            await sitemapRepo.AddAsync(entry, ct);
        }
        else
        {
            existingSitemap.Touch();
            existingSitemap.ChangeUrl(e.CanonicalUrl);
        }

        // ── 3. Mark inbox + ONE atomic SaveChanges ──────────────────────────
        await inbox.MarkAsProcessedAsync(inboxKey, "BlogPublished", clock.UtcNow, ct);
        await uow.SaveChangesAsync(ct);

        // ── 4. Cache invalidation ───────────────────────────────────────────
        await cache.RemoveByTagAsync($"seo:Blog:{e.BlogId}", ct);
        await cache.RemoveByTagAsync("sitemap:rendered", ct);

        log.LogInformation(
            "Processed BlogPublished {BlogId}: created/updated SEO + Sitemap entries", e.BlogId);
    }
}
```

`BlogUpdatedIntegrationEventHandler` similarly: if `OldSlug != NewSlug`, **create a new Redirect**:
```csharp
if (e.OldSlug != e.NewSlug)
{
    var redirect = Redirect.Create($"/blog/{e.OldSlug}", $"/blog/{e.NewSlug}", 301);
    // chain-flatten as in §3.3 algorithm
    await redirectRepo.AddWithFlatteningAsync(redirect, ct);
}
```

`BlogDeletedIntegrationEventHandler` deactivates the corresponding sitemap entry (`SitemapEntry.Deactivate()`).

`PlaceDeletedIntegrationEventHandler` (existing scaffold — Mohammad fills): deactivates Sitemap + SeoMetadata + FAQ rows for that place. ALSO emits a domain event so other modules (ContentBlogs) can clean up. Per gotcha #11, business rules MUST be checked against PDF before implementing.

### 3.6 Integration Events Emitted (T3)

```csharp
// ContentSeo.Contracts/IntegrationEvents/RedirectCreatedIntegrationEvent.cs
/// <summary>
/// Raised when a 301 or 302 redirect is created (manually or via slug-change auto-flow).
/// Consumers:
///   - Web/Middleware (invalidate redirect lookup cache so next request sees it)
///   - Analytics (track redirect creations as "content moved" metric)
/// </summary>
public sealed record RedirectCreatedIntegrationEvent(
    Guid RedirectId,
    string OldUrl,
    string NewUrl,
    int StatusCode,
    DateTime CreatedAt) : IIntegrationEvent;

// RedirectChainFlattenedIntegrationEvent
public sealed record RedirectChainFlattenedIntegrationEvent(
    Guid RedirectId,
    string OldUrl,
    string OldTarget,
    string NewTarget,
    DateTime FlattenedAt) : IIntegrationEvent;

// SeoMetadataChangedIntegrationEvent
/// <summary>
/// Raised on metadata create or update. Internal use only (Wave 6 sitemap regen will consume).
/// </summary>
public sealed record SeoMetadataChangedIntegrationEvent(
    Guid SeoMetadataId,
    SeoEntityType EntityType,
    Guid EntityId,
    DateTime ChangedAt) : IIntegrationEvent;

// FaqItemChangedIntegrationEvent
public sealed record FaqItemChangedIntegrationEvent(
    Guid FaqItemId,
    SeoEntityType EntityType,
    Guid EntityId,
    string ChangeType,   // "Created" | "Updated" | "Deleted" | "Reordered"
    DateTime ChangedAt) : IIntegrationEvent;
```

### 3.7 Logical-Name Registration

```csharp
.Register<RedirectCreatedIntegrationEvent>           ("content-seo.redirect.created.v1")
.Register<RedirectChainFlattenedIntegrationEvent>    ("content-seo.redirect.chain-flattened.v1")
.Register<SeoMetadataChangedIntegrationEvent>        ("content-seo.seo-metadata.changed.v1")
.Register<FaqItemChangedIntegrationEvent>            ("content-seo.faq-item.changed.v1")
```

### 3.8 Validator Rules

```csharp
public sealed class UpsertSeoMetadataCommandValidator : AbstractValidator<UpsertSeoMetadataCommand>
{
    public UpsertSeoMetadataCommandValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityId).NotEqual(Guid.Empty);
        RuleFor(x => x.MetaTitle).MaximumLength(60).When(x => !string.IsNullOrEmpty(x.MetaTitle));
        RuleFor(x => x.MetaDescription).MaximumLength(160).When(x => !string.IsNullOrEmpty(x.MetaDescription));
        RuleFor(x => x.SitemapPriority).InclusiveBetween(0m, 1m).When(x => x.SitemapPriority.HasValue);
        RuleFor(x => x.SitemapChangeFrequency)
            .Must(f => f is "always" or "hourly" or "daily" or "weekly" or "monthly" or "yearly" or "never")
            .When(x => !string.IsNullOrEmpty(x.SitemapChangeFrequency));
        RuleFor(x => x.CanonicalUrl)
            .Matches("^(/[a-z0-9\\-/]+|https?://[^\\s]+)$")
            .When(x => !string.IsNullOrEmpty(x.CanonicalUrl));
    }
}

public sealed class CreateRedirectCommandValidator : AbstractValidator<CreateRedirectCommand>
{
    public CreateRedirectCommandValidator()
    {
        RuleFor(x => x.OldUrl).NotEmpty().Matches("^/[a-z0-9\\-/]+$");
        RuleFor(x => x.NewUrl).NotEmpty().Matches("^(/[a-z0-9\\-/]+|https?://[^\\s]+)$");
        RuleFor(x => x.StatusCode).Must(c => c is 301 or 302);
        RuleFor(x => x).Must(x => !string.Equals(x.OldUrl, x.NewUrl, StringComparison.OrdinalIgnoreCase))
                       .WithMessage("OldUrl and NewUrl must differ.");
    }
}

public sealed class CreateFaqItemCommandValidator : AbstractValidator<CreateFaqItemCommand>
{
    public CreateFaqItemCommandValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityId).NotEqual(Guid.Empty);
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Answer).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReorderFaqItemsCommandValidator : AbstractValidator<ReorderFaqItemsCommand>
{
    public ReorderFaqItemsCommandValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityId).NotEqual(Guid.Empty);
        RuleFor(x => x.Items).NotEmpty().Must(i => i.Count <= 50);
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.SortOrder).Distinct().Count() == items.Count)
            .WithMessage("SortOrder values must be unique within the batch.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEqual(Guid.Empty);
            item.RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}
```

### 3.9 WBS — Work Breakdown for Mohammad

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 3.1 | Confirm PW-4 + PW-5 land Tue Week 1 | 0.5 | Wed Week 1 |
| 3.2 | `SeoMetadata.cs` — add 3 more business methods (`UpdateOg`, `UpdateSchema`, `UpdateSitemapHints`) + 3 domain event records (refill PW-4 stubs) | 4 | Wed Week 1 |
| 3.3 | `Redirect.cs` — add `Create` factory + `Activate`/`Deactivate`/`IncrementHit`/`RewriteTo` + 3 domain event records | 4 | Thu Week 1 |
| 3.4 | `FaqItem.cs` — add 5 business methods + 4 domain event records | 4 | Thu Week 1 |
| 3.5 | `ISeoMetadataRepository`/`IRedirectRepository`/`IFaqItemRepository` impls with custom queries (`GetByEntityAsync`, `FindActiveByOldUrlAsync`, `WalkChainAsync`, `ListByEntityOrderedAsync`) | 6 | Sun Week 2 |
| 3.6 | `UpsertSeoMetadataCommand` + handler + validator + endpoint #2 | 3 | Sun Week 2 |
| 3.7 | `UpdateSeoMetadataCommand` + handler + validator + endpoint #3 | 2 | Mon Week 2 |
| 3.8 | `GetSeoMetadataQuery` + DTO + `ICacheableQuery` + endpoint #1 | 2 | Mon Week 2 |
| 3.9 | `CreateRedirectCommand` + handler with chain-flatten algorithm (§3.3) + validator + endpoint #5 | 6 | Tue Week 2 |
| 3.10 | `DeleteRedirectCommand` + handler + endpoint #6 | 1 | Tue Week 2 |
| 3.11 | `ListRedirectsQuery` + DTO + filter+sort+pagination + `ICacheableQuery` + endpoint #4 | 2 | Wed Week 2 |
| 3.12 | `CreateFaqItemCommand` + handler + validator + endpoint #8 | 3 | Wed Week 2 |
| 3.13 | `UpdateFaqItemCommand` + handler + endpoint #9 | 2 | Wed Week 2 |
| 3.14 | `DeleteFaqItemCommand` + handler + endpoint #10 | 1 | Thu Week 2 |
| 3.15 | `ReorderFaqItemsCommand` + handler (transactional batch update of SortOrder) + validator + endpoint #11 | 3 | Thu Week 2 |
| 3.16 | `ListFaqItemsQuery` + DTO with translations + `ICacheableQuery` + endpoint #7 | 2 | Thu Week 2 |
| 3.17 | Domain event handlers (10 emitting handlers — one per event record) | 4 | Sun Week 3 |
| 3.18 | Integration event consumers — fill in `BlogPublishedIntegrationEventHandler`, `BlogUpdatedIntegrationEventHandler`, `BlogDeletedIntegrationEventHandler`, complete `PlaceDeletedIntegrationEventHandler` (existing scaffold) | 6 | Mon Week 3 |
| 3.19 | Integration tests B10.1–B10.15 (15 scenarios) | 4 | Wed Week 3 |
| 3.20 | Code review feedback + fixes | 4 | Thu Week 3 |
| 3.21 | Final PR merge | 0 | Thu Week 3 17:00 |
| **TOTAL** | | **63** | (44h task budget; overrun absorbed by buffer; if pressed, descope `IncrementHit` from item 3.3 to a Wave 6 backlog item) |

### 3.10 Edge Cases to Handle (T3 specifically)

- **Same-language FAQ translation already exists.** `IEntityTranslationOrchestrator` is idempotent — it checks `(FaqItemId, LanguageId)` uniqueness and updates instead of inserting duplicates. Mohammad doesn't have to special-case this.
- **Redirect chain flattening with 1000s of pre-existing redirects pointing to OldUrl.** The flatten loop in §3.3 uses `await redirectRepo.FindActiveByNewUrlAsync(oldUrl, ct)` which returns ALL of them; performance test: simulate 5,000 active redirects all → A, then create A→B; flatten must complete in < 5 seconds. If slower, batch-update via `ExecuteUpdateAsync` (EF 7+).
- **Circular detection deeper than 10 hops.** Returned as `Redirect.CircularChain`; UI shows "Cycle detected at hop N — please verify your redirect graph". The 10-hop guard is BOTH a depth limit AND a cycle-detection backstop.
- **FAQ reorder with gaps.** Admin sends `[{id1, 0}, {id2, 5}, {id3, 10}]`. Allowed — gaps are fine, only **uniqueness** is enforced. The list query sorts by `SortOrder asc` so display is correct.
- **FAQ translation race.** Two admins edit the same FAQ item's English text simultaneously → standard concurrency catch on `FaqItem.RowVersion`.
- **Sitemap entry exists for entity but entity was hard-deleted somewhere.** Discovered via T4's `SitemapRegenerationService` — that service flags orphan entries. T3 doesn't deal with this directly; the integration-event consumers must keep sitemap in sync.
- **Cache key collision risk for `redirects:lookup:{oldUrlHash}`.** Use SHA256 of the URL, base64url-encoded, first 16 chars — collision probability is astronomically low. Don't use `OldUrl` raw because URLs may contain characters HybridCache can't key on.
- **Concurrent `UpsertSeoMetadata` on same `(entityType, entityId)`**: handler does `repo.GetByEntityAsync` → if null insert; else update. Race-window: two inserts both pass the null check. DB unique index catches the second; handler catches `DbUpdateException` with SQL error 2627 (unique violation), reloads, retries as update. Maximum 1 retry. Standard pattern.
- **Inbox processing during deployment.** If the app restarts mid-handler, the inbox key won't be marked → handler runs again on next outbox poll → inbox guard sees no record → re-runs. Side effects must be idempotent (the SEO/Sitemap upsert checks for existing rows).

---

## TASK 4 — Sitemap Renderer, Weather Service, Translation API, Background Services

**Owner:** Mohammad (intermediate)  ·  **Endpoints:** 7  ·  **Background services:** 2  ·  **Hours:** 36  ·  **Deadline:** Thu 2026-06-11 17:00
**Earliest start:** Wed 2026-05-27 09:00 (after pre-work merges)
**Dependencies:** PW-1, PW-4 (`SitemapEntry` and `WeatherCache` are aggregate roots), PW-5 (`ISitemapEntryRepository`, `IWeatherCacheRepository`), PW-6 (`ContentSeoFeatures`).

> 📚 **Implementation notes:**
> - This task spans **two modules**: ContentSeo (sitemap/weather) AND ContentCore (translation API endpoints). You will edit ContentCore code; pair-review with whoever owns ContentCore (Tech Lead).
> - The two background services are `IHostedService` implementations registered via `AddHostedService<T>()` in `Program.cs`. They run on every app instance — make sure they are **idempotent** (with leader-election or distributed-lock left as Wave 6).
> - External APIs (weather, search-console) MUST be wrapped in stub interfaces with `NoOp*` implementations so the app boots even without API keys configured.

### 🎯 4.0 Entities Touched

| Entity | Mohammad's responsibility |
|---|---|
| `SitemapEntry` (`AuditableEntity, IAggregateRoot` post-PW-4) | No new business methods (existing `Touch`, `ChangeUrl`, `Deactivate`, `Reactivate` cover it). Mohammad adds `ISitemapRenderer` for XML serialization. |
| `WeatherCache` (`AuditableEntity, IAggregateRoot` post-PW-4) | Add `Create`, `Refresh`, `Expire` business methods. 0 domain events (fire-and-forget cache). |
| `Translation` (existing entity in `ContentCore.Domain`) | Read-only access. Mohammad exposes existing `ITranslationService` via 3 new endpoints in `ContentCore.Presentation`. |

### 4.1 Endpoints

| # | Method | Route | Auth | Handler |
|---|---|---|---|---|
| 1 | `GET` | `/sitemap.xml` | 🌐 Anonymous | `GetSitemapXmlHandler` (returns `application/xml`; cached 30 min) |
| 2 | `POST` | `/api/v1/seo/sitemap/regenerate` | 👑 `Sitemap:Manage` | `RegenerateSitemapHandler` (synchronous trigger of `SitemapRegenerationService.RunOnceAsync`) |
| 3 | `GET` | `/api/v1/seo/weather/{placeId:guid}` | 🌐 Anonymous | `GetWeatherForPlaceHandler` (cache-then-DB; 12h TTL) |
| 4 | `POST` | `/api/v1/seo/weather/refresh/{placeId:guid}` | 👑 `Weather:Manage` | `RefreshWeatherHandler` (force fetch via `IWeatherProvider`) |
| 5 | `POST` | `/api/v1/content-core/translations/translate` | 👑 `Translation:Manage` (in ContentCore catalog — confirm exists; if not, T4 adds in PW-6 follow-up) | `TranslateOnDemandHandler` (calls `ITranslationService.TranslateAsync` for one entity) |
| 6 | `POST` | `/api/v1/content-core/translations/batch` | 👑 `Translation:Manage` | `BatchTranslateHandler` (queues N entities for translation via orchestrator) |
| 7 | `GET` | `/api/v1/content-core/translations/{entityType}/{entityId:guid}` | 🌐 Anonymous | `GetTranslationsForEntityHandler` (returns all language variants) |

### 4.2 Background Services

#### SitemapRegenerationService (`ContentSeo.Infrastructure/BackgroundServices/SitemapRegenerationService.cs`)

```csharp
public sealed class SitemapRegenerationService(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider clock,
    ILogger<SitemapRegenerationService> log)
    : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once at startup, then every 6 hours.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var renderer = scope.ServiceProvider.GetRequiredService<ISitemapRenderer>();
                await renderer.RenderAndCacheAsync(stoppingToken);
                log.LogInformation("Sitemap regenerated at {UtcNow}", clock.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "SitemapRegenerationService iteration failed");
                // Per try/catch whitelist (§5.5 #3): background services per-item loops with logging.
            }

            try { await Task.Delay(Cadence, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
```

**Cadence:** Every 6 hours (00:00, 06:00, 12:00, 18:00 UTC) per PDF §8.2.

**Internal flow `ISitemapRenderer.RenderAndCacheAsync`:**
1. Query all active+published-status entities across modules:
   - Tours: `ITourQueryService.GetPublishedTourSitemapEntriesAsync(ct)` — returns `(id, slug, updatedAt)` tuples. **Note:** This service must be added to ContentTours.Application/Interfaces — Mohammad coordinates with the Tech Lead to add stub returning empty list initially. Real impl is part of ContentTours wave (already done).
   - Places: same pattern via `IPlaceQueryService` (already exists in ContentPlaces).
   - Businesses: `IBusinessQueryService.GetPublishedBusinessSitemapEntriesAsync(ct)` (stub returning empty for v1 — Wave 5).
   - Blogs: `IBlogQueryService.GetPublishedBlogSitemapEntriesAsync(ct)` — Fadwa adds the interface method as part of T1 cleanup.
2. For each tuple, find or create `SitemapEntry` via `ISitemapEntryRepository`:
   - If exists and `UpdatedAt > entry.LastModified`: call `entry.ChangeUrl(canonicalUrl)` then `entry.Touch()`.
   - If exists with `IsActive=false` and entity is now Published: `entry.Reactivate()`.
   - If doesn't exist: `SitemapEntry.Create(...)`.
3. For each existing entry whose entity no longer appears in the published set: `entry.Deactivate()`.
4. Render XML via `XmlWriter` to:
   - `Url` set: `https://yallajo.com{entry.Url}`
   - `<lastmod>{ISO8601(entry.LastModified)}</lastmod>`
   - `<changefreq>{entry.ChangeFrequency}</changefreq>`
   - `<priority>{entry.Priority:F1}</priority>`
   - `<xhtml:link rel="alternate" hreflang="ar" href="{ar-version}" />` per PDF §8.2.
5. If total > 50,000 entries → split into multiple sub-sitemaps + sitemap-index. v1 fail with `Sitemap.SizeOverflow` and log critical until Wave 6 sharding lands.
6. Cache the rendered XML under tag `sitemap:rendered` with TTL = 24h (regenerated every 6h, cache survives buffer).
7. Save all aggregate mutations via `IContentSeoUnitOfWork.SaveChangesAsync(ct)` — this commits all `Touch`/`ChangeUrl`/`Deactivate`/`Reactivate`/`Create` in ONE transaction. Domain events flow through normally (currently no events on SitemapEntry, but if added later, dispatch works).
8. Ping `ISearchConsolePinger.PingAsync(sitemapUrl, ct)` — stub logs "would ping Google + Bing"; real impl Wave 6.

#### WeatherPreFetchService (`ContentSeo.Infrastructure/BackgroundServices/WeatherPreFetchService.cs`)

```csharp
public sealed class WeatherPreFetchService(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider clock,
    ILogger<WeatherPreFetchService> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Sleep until next 05:00 UTC.
            var nowUtc = clock.UtcNow;
            var nextRun = nowUtc.Date.AddHours(5);
            if (nextRun <= nowUtc) nextRun = nextRun.AddDays(1);
            var delay = nextRun - nowUtc;

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var topPlaces = scope.ServiceProvider.GetRequiredService<IPlaceQueryService>();
                var weatherProvider = scope.ServiceProvider.GetRequiredService<IWeatherProvider>();
                var cacheRepo = scope.ServiceProvider.GetRequiredService<IWeatherCacheRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IContentSeoUnitOfWork>();

                var places = await topPlaces.GetTop50ByPopularityAsync(stoppingToken);
                var budgetLeft = 1000;   // PDF §8.2 — free tier 1000 calls/day total

                foreach (var place in places)
                {
                    if (budgetLeft <= 0)
                    {
                        log.LogWarning("Weather pre-fetch budget exhausted; skipping remaining {Count} places",
                            places.Count - places.IndexOf(place));
                        break;
                    }

                    try
                    {
                        var snapshot = await weatherProvider.FetchAsync(place.Id, place.Latitude, place.Longitude, stoppingToken);
                        var existing = await cacheRepo.GetByPlaceIdAsync(place.Id, stoppingToken);
                        if (existing is null)
                            await cacheRepo.AddAsync(WeatherCache.Create(place.Id, snapshot, clock.UtcNow), stoppingToken);
                        else
                            existing.Refresh(snapshot, clock.UtcNow);
                        budgetLeft -= 1;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.LogError(ex, "Weather pre-fetch failed for place {PlaceId}; continuing", place.Id);
                        // Per try/catch whitelist (§5.5 #3): per-item loop catch with logging.
                    }
                }

                await uow.SaveChangesAsync(stoppingToken);
                log.LogInformation("Weather pre-fetch run completed at {UtcNow}, budget left {Budget}", clock.UtcNow, budgetLeft);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "WeatherPreFetchService iteration failed");
            }
        }
    }
}
```

### 4.3 Pre-Submit Validation Gate

Each command-handler validator inherits standard rules; no entity-spanning gate beyond what's listed in §4.4.

### 4.4 Business Rules

#### B1 — Domain Invariants

**`SitemapEntry`:**
1. `Url` non-empty, starts with `/`, no `#fragments`, no `?queries`. v1 supports path-only URLs.
2. `Priority ∈ [0.0, 1.0]`. Defaults: Tour=0.8, Place=0.9, Blog=0.6, Business=0.7, Static=0.3 (per PDF §8.2).
3. `ChangeFrequency` member of fixed enum (PDF §8.2): always/hourly/daily/weekly/monthly/yearly/never.
4. `(EntityType, EntityId)` unique among non-deleted active rows. T4 enforces via custom index in `SitemapEntryConfiguration.cs`.

**`WeatherCache`:**
1. `(PlaceId, IsDeleted=false)` unique — only one active cache per place.
2. `ExpiresAt = FetchedAt + 12 hours`.
3. `Temperature ∈ [-100, 100]` (Celsius), `Humidity ∈ [0, 100]`, `WindSpeed ≥ 0`, `WindDirection ∈ [0, 360]` if non-null.
4. `Forecast` JSON string ≤ 4000 chars (limited cap on multi-day forecast payload).

#### B2 — Authorization Matrix

| Endpoint | Anonymous | Auth | Admin |
|---|:-:|:-:|:-:|
| GET /sitemap.xml | ✅ | ✅ | ✅ |
| POST /sitemap/regenerate | ❌ | ❌ | ✅ |
| GET /weather/{placeId} | ✅ | ✅ | ✅ |
| POST /weather/refresh/{placeId} | ❌ | ❌ | ✅ |
| POST /translations/translate | ❌ | ❌ | ✅ |
| POST /translations/batch | ❌ | ❌ | ✅ |
| GET /translations/{entityType}/{entityId} | ✅ | ✅ | ✅ |

#### B3 — State Transition Table

Sitemap entries are mutated mostly by background service + integration-event consumers (T3 owns those). T4 ships the manual regenerate trigger:

| Endpoint | Precondition | Post-state | Side effects |
|---|---|---|---|
| `POST /sitemap/regenerate` | admin | sitemap rendered + cached + ping | invalidate `sitemap:rendered` cache; pings external services |
| `POST /weather/refresh/{placeId}` | place exists + budget remaining | weather cache refreshed | invalidate `weather:{placeId}`; emits no integration event |

#### B4 — Error Code Catalog

| Code | HTTP | Trigger |
|---|---|---|
| `Sitemap.RenderFailed` | 500 | Internal renderer exception (logged, returned generically). |
| `Sitemap.SizeOverflow` | 503 | > 50,000 active entries; sharding not implemented v1. |
| `Sitemap.NotReady` | 503 | First-ever request before background svc has run; in this case render synchronously then return. |
| `Weather.NotFound` | 404 | No `WeatherCache` row AND `IWeatherProvider.IsAvailable == false`. |
| `Weather.Stale` | 200 + warning header `X-Weather-Stale: true` | Cache row exists but `ExpiresAt < UtcNow`. Still return it (better stale than nothing) but warn. |
| `Weather.UpstreamUnavailable` | 503 | Provider call failed (timeout, 5xx). |
| `Weather.BudgetExhausted` | 429 + `Retry-After: 86400` | Force-refresh would exceed daily 1000-call budget. |
| `Place.NotFound` | 404 | `IPlaceExistsService` returns false on `weather/refresh`. |
| `Translation.NotFound` | 404 | No translations exist for `(entityType, entityId)`. |
| `Translation.UpstreamUnavailable` | 503 | Translation service unreachable. |
| `Translation.RateLimited` | 429 | Translation service rate-limit hit. |

#### B5 — Cache Policy

| Query | Cache key | Absolute TTL | L1 TTL | Tags |
|---|---|---|---|---|
| `GET /sitemap.xml` | `sitemap:rendered` | 24h | 1h | `sitemap:rendered` |
| `GetWeatherQuery(placeId)` | `weather:{placeId}` | 12h | 30 min | `weather:{placeId}` |
| `GetTranslationsForEntityQuery(entityType, entityId, lang)` | `translations:{entityType}:{entityId}:lang:{lang}` | 120 min | 30 min | `translations:{entityType}:{entityId}` |

Commands invalidate respective tags after success. `RegenerateSitemapHandler` removes `sitemap:rendered`. `RefreshWeatherHandler` removes `weather:{placeId}`. Translation create/update/delete (in ContentCore — confirm exists) removes `translations:{entityType}:{entityId}`.

#### B6 — Translation Handling

T4 directly works with `ITranslationService` (existing in SharedKernel.Application) and `IEntityTranslationOrchestrator`. No new translation logic to build — only API surface.

#### B7 — Concurrency Handling

`WeatherCache` has RowVersion (post-PW-4 upgrade). Standard concurrency catch in repo. `SitemapEntry` similarly. The sitemap regenerator handles many entries in one transaction; if it conflicts with another concurrent regenerate, one of them sees `Sitemap.ConcurrencyConflict` (caller is generally a background svc that retries on next cycle).

#### B8 — Audit Logging

| Field | PII? | Log level on success | On failure |
|---|---|---|---|
| `SitemapEntry.Url` | No | Information (count + first 5) | Warning |
| `WeatherCache.PlaceId, Temperature, Condition` | No | Information | Warning |
| External API payloads | No | Information (size only, never raw body) | Warning |
| Translation source/target lang + entity refs | No | Information | Warning |
| Translation actual content | No (public) but voluminous | length-only at Information | Warning |

#### B9 — Pagination & Filter Semantics

- `/sitemap.xml`: returns full XML (no pagination); error if size > 50K entries.
- `/weather/{placeId}`: single resource, no pagination.
- `/translations/{entityType}/{entityId}`: returns all language variants in one response — typically ≤ 5 per entity, no pagination needed.
- `/translations/batch`: request body has `{ "entries": [...] }`; max 100 entries per call.

#### B10 — Acceptance Test Scenarios

`ContentSeo.IntegrationTests/SitemapAndWeatherTests.cs`:

1. `GetSitemap_FirstCall_RendersAnd_Returns200_ApplicationXml`.
2. `GetSitemap_AfterBlogPublish_IncludesBlogUrl_With_LastMod_Updated`.
3. `RegenerateSitemap_AsAdmin_Triggers_Renderer_Synchronously_AndReturns200`.
4. `RegenerateSitemap_OverFiftyThousandEntries_Returns_SizeOverflow_503`.
5. `GetWeather_NoCacheRow_NoProvider_Returns_NotFound_404`.
6. `GetWeather_FreshCacheRow_ReturnsValue_NoUpstreamCall`.
7. `GetWeather_StaleCacheRow_ReturnsValue_WithStaleHeader`.
8. `RefreshWeather_BudgetExhausted_Returns_BudgetExhausted_429_WithRetryAfter`.
9. `RefreshWeather_PlaceNotFound_Returns_PlaceNotFound_404`.
10. `BackgroundService_PreFetch_Top50Places_RunsOnceAtFiveAm_UTC`.
11. `BackgroundService_PreFetch_AfterBudgetExhaust_LogsWarning_DoesNotThrow`.
12. `Translate_OnDemand_AsAdmin_QueuesViaOrchestrator_AndReturnsAccepted`.
13. `BatchTranslate_OneHundredEntries_QueuesAll_ReturnsAccepted` (expects `Result<int>` count).
14. `BatchTranslate_OneHundredOneEntries_Returns_400_BatchTooLarge`.
15. `GetTranslations_PublicAccess_ReturnsAllLanguages_RespectsAcceptLanguage_OrderForFallback`.

### 4.5 Stub Interface Definitions

```csharp
// ContentSeo.Application/Interfaces/IWeatherProvider.cs
public interface IWeatherProvider
{
    bool IsAvailable { get; }
    Task<WeatherSnapshot> FetchAsync(Guid placeId, decimal latitude, decimal longitude, CancellationToken ct);
}

public sealed record WeatherSnapshot(
    decimal Temperature,
    decimal FeelsLike,
    int Humidity,
    decimal WindSpeed,
    int WindDirection,
    string Condition,
    string IconCode,
    decimal? UvIndex,
    string? ForecastJson);

// ContentSeo.Infrastructure/Weather/NoOpWeatherProvider.cs
internal sealed class NoOpWeatherProvider(ILogger<NoOpWeatherProvider> log) : IWeatherProvider
{
    public bool IsAvailable => false;
    public Task<WeatherSnapshot> FetchAsync(Guid placeId, decimal lat, decimal lon, CancellationToken ct)
    {
        log.LogWarning("NoOpWeatherProvider.FetchAsync called for {PlaceId} — no real provider configured", placeId);
        throw new InvalidOperationException(
            "Weather provider is not configured. Real OpenWeatherMap implementation is Wave 6.");
    }
}
```

```csharp
// ContentSeo.Application/Interfaces/ISearchConsolePinger.cs
public interface ISearchConsolePinger
{
    Task PingAsync(string sitemapUrl, CancellationToken ct);
}

// ContentSeo.Infrastructure/Seo/NoOpSearchConsolePinger.cs
internal sealed class NoOpSearchConsolePinger(ILogger<NoOpSearchConsolePinger> log) : ISearchConsolePinger
{
    public Task PingAsync(string sitemapUrl, CancellationToken ct)
    {
        log.LogInformation("NoOpSearchConsolePinger: would ping Google + Bing for {Url} (real impl Wave 6)", sitemapUrl);
        return Task.CompletedTask;
    }
}
```

```csharp
// ContentSeo.Application/Interfaces/ISitemapRenderer.cs
public interface ISitemapRenderer
{
    Task<string> RenderAndCacheAsync(CancellationToken ct);
    Task<string> GetCachedXmlAsync(CancellationToken ct);
}
```

DI registration in `ContentSeo.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddSingleton<IWeatherProvider, NoOpWeatherProvider>();
services.AddSingleton<ISearchConsolePinger, NoOpSearchConsolePinger>();
services.AddScoped<ISitemapRenderer, SitemapRenderer>();
services.AddHostedService<SitemapRegenerationService>();
services.AddHostedService<WeatherPreFetchService>();
```

### 4.6 Validator Rules

```csharp
public sealed class TranslateOnDemandCommandValidator : AbstractValidator<TranslateOnDemandCommand>
{
    public TranslateOnDemandCommandValidator()
    {
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EntityId).NotEqual(Guid.Empty);
        RuleFor(x => x.SourceLanguage).NotEmpty().Length(2);
        RuleFor(x => x.TargetLanguages).NotEmpty().Must(l => l.Count <= 10);
        RuleForEach(x => x.TargetLanguages).Length(2);
    }
}

public sealed class BatchTranslateCommandValidator : AbstractValidator<BatchTranslateCommand>
{
    public BatchTranslateCommandValidator()
    {
        RuleFor(x => x.Entries).NotEmpty().Must(e => e.Count <= 100)
            .WithMessage("Batch limited to 100 entries.");
    }
}

public sealed class RefreshWeatherCommandValidator : AbstractValidator<RefreshWeatherCommand>
{
    public RefreshWeatherCommandValidator()
    {
        RuleFor(x => x.PlaceId).NotEqual(Guid.Empty);
    }
}
```

### 4.7 WBS — Work Breakdown for Mohammad

| # | Sub-deliverable | Est. hrs | Finish by |
|---|---|---|---|
| 4.1 | Confirm PW-1, PW-4, PW-5 land Tue Week 1 | 0.5 | Wed Week 1 |
| 4.2 | `IWeatherProvider` + `WeatherSnapshot` + `NoOpWeatherProvider` + DI registration | 2 | Wed Week 1 |
| 4.3 | `ISearchConsolePinger` + `NoOpSearchConsolePinger` + DI | 1 | Wed Week 1 |
| 4.4 | `ISitemapRenderer` interface + `SitemapRenderer` impl with XML serialization | 6 | Thu Week 1 |
| 4.5 | `WeatherCache.cs` — add `Create`, `Refresh`, `Expire` methods (entity is currently a property bag) | 2 | Thu Week 1 |
| 4.6 | `IWeatherCacheRepository` impl + `ISitemapEntryRepository` impl (custom queries `GetByPlaceIdAsync`, `GetActiveByEntityTypeAsync`, `BulkUpsertAsync`) | 4 | Sun Week 2 |
| 4.7 | `GetSitemapXmlEndpoint` — endpoint #1 — anonymous, returns content-type `application/xml` | 2 | Sun Week 2 |
| 4.8 | `RegenerateSitemapCommand` + handler + endpoint #2 | 2 | Mon Week 2 |
| 4.9 | `GetWeatherQuery` + `WeatherDto` + `ICacheableQuery` + endpoint #3 | 3 | Mon Week 2 |
| 4.10 | `RefreshWeatherCommand` + handler with budget check + endpoint #4 | 4 | Tue Week 2 |
| 4.11 | `TranslateOnDemandCommand` + handler (in ContentCore) + endpoint #5 | 3 | Tue Week 2 |
| 4.12 | `BatchTranslateCommand` + handler (in ContentCore) + endpoint #6 | 3 | Wed Week 2 |
| 4.13 | `GetTranslationsForEntityQuery` + endpoint #7 (in ContentCore) | 2 | Wed Week 2 |
| 4.14 | `SitemapRegenerationService` (BackgroundService) + register in DI | 4 | Thu Week 2 |
| 4.15 | `WeatherPreFetchService` (BackgroundService) + register in DI + budget tracker | 4 | Thu Week 2 |
| 4.16 | EF migrations as needed (e.g., `SitemapEntry` unique index `(EntityType, EntityId)`) | 1 | Thu Week 2 |
| 4.17 | Integration tests B10.1–B10.15 | 4 | Sun Week 3 |
| 4.18 | Code review feedback + fixes | 4 | Wed Week 3 |
| 4.19 | Final PR merge | 0 | Thu Week 3 17:00 |
| **TOTAL** | | **44** | (36h budget; overrun absorbed by buffer) |

### 4.8 Edge Cases to Handle (T4 specifically)

- **First-ever sitemap request before background service has run.** `GET /sitemap.xml` checks cache; on miss, **synchronously** invokes `ISitemapRenderer.RenderAndCacheAsync`. First request is slow (1–3 sec); subsequent requests are cached.
- **Weather provider returns malformed JSON.** `IWeatherProvider.FetchAsync` is wrapped in try/catch (Infrastructure whitelist) — logs and returns `Result.Failure("Weather.UpstreamUnavailable")`. Background service catches at item-loop level and continues with the next place.
- **Background service starts twice (multi-instance deployment).** v1: both instances run independently — no leader election. Each independently hits `IWeatherProvider`, doubling API calls. **Mitigation**: budget check per-call counts cache hits as well; if a fresh row exists < 1h old, skip. Document the multi-instance limitation in the BackgroundService class XML doc — Wave 6 adds distributed lock.
- **Translation service rate-limits a batch call.** `BatchTranslateCommand` handler iterates entries; if any single call returns 429, the handler stops the batch and returns `Translation.RateLimited` 429 with the count of successfully queued entries in the response body (`Result<int>` carries the partial count).
- **Sitemap renderer mid-run, an entity is deleted.** `IBlogQueryService.GetPublishedBlogSitemapEntriesAsync` snapshot is taken at the start. If a blog deletion happens mid-render, the stale snapshot may include the deleted blog — the entry is created/updated even though the blog no longer exists. **Mitigation**: the next regenerate cycle (≤ 6h later) catches it and `Deactivate()`s the entry. v1 acceptable.
- **WeatherCache row with `IsDeleted=true` from previous test run.** PW-4 migration upgrades `WeatherCache` to `AuditableEntity` — pre-existing rows have `IsDeleted=false` default, but if tests dirty the DB, ensure repo queries always filter `IsDeleted=false`. Use `IgnoreQueryFilters` only in admin tooling.
- **`/sitemap.xml` requested with `Accept: text/html`.** Endpoint always returns `application/xml` regardless of `Accept` header (search engines don't negotiate).
- **Translation API endpoint #7 with no `Accept-Language` header.** Default to source language (`en`). `IRequestContext.Language` already resolves to `en` if header absent.
- **`POST /sitemap/regenerate` while a scheduled run is in progress.** The handler invokes `ISitemapRenderer.RenderAndCacheAsync` directly — concurrent invocations may run twice. Use a `SemaphoreSlim(1, 1)` inside the renderer to serialize. Maximum one render at a time per app instance.

---

## 7. Cross-Cutting Concerns (Owned by Tech Lead, reviewed every standup)

### 7.1 DI Wiring Audit (gotcha #10 — most-forgotten step)

After every PR merge, the tech lead runs:
```powershell
dotnet build YallaJo.sln 2>&1 | Select-String -Pattern "InvalidOperationException|service was registered"
```
Any `InvalidOperationException: Unable to resolve service for type 'IXxx'` failure is treated as a sprint-blocker — fix within 4h or roll back the PR.

**Specific DI gotchas this sprint:**
- `IPermissionCatalog` registrations from PW-6 must be `AddSingleton` (not Scoped) — `PermissionSeeder` is `IHostedService` and runs at startup before scopes exist.
- `IHostedService` registrations (`SitemapRegenerationService`, `WeatherPreFetchService`) live on `IServiceCollection`, not on a scoped service. Use `services.AddHostedService<T>()`.
- `IWeatherProvider` and `ISearchConsolePinger` Singletons by default — they're stateless. If real impls (Wave 6) hold connection state, change to Scoped.
- `IProfanityFilter` must be Singleton (it loads keyword list from config once).
- `IBlogTourLinkRepository` is Scoped (DbContext-bound, gotcha #7).

### 7.2 Permission Seeder Verification

After PW-6 lands, in the boot logs we expect:

```
[INFO] PermissionSeeder discovered 2 catalogs:
[INFO]   - ContentBlogs (13 permissions)
[INFO]   - ContentSeo   (15 permissions)
[INFO] Total seeded: 28 permissions for app YallaJo.
```

**If counts mismatch what we declared, lead investigates same day.**

### 7.3 Outbox Type-Registry Validation

`IntegrationEventTypeRegistry.cs` MUST contain all 16 logical names introduced this sprint (6 Blog events, 6 BlogComment + Tour link events, 4 ContentSeo events). Run sanity test:

```bash
dotnet test --filter "FullyQualifiedName~IntegrationEventTypeRegistryTests" SharedKernel.Tests
```

The test enumerates registered names and counts; failure means someone forgot a `.Register<T>(name)` line.

### 7.4 Build Lock Workaround (audit context §8)

The `YallaJo.Web.exe` file lock that blocked full-solution builds during ContentTours sprint may persist. Workaround for PR validation:

```powershell
# Build only changed projects
dotnet build ContentBlogs.Application/ContentBlogs.Application.csproj
dotnet build ContentBlogs.Infrastructure/ContentBlogs.Infrastructure.csproj
dotnet build ContentBlogs.Presentation/ContentBlogs.Presentation.csproj
dotnet build ContentSeo.Application/ContentSeo.Application.csproj
dotnet build ContentSeo.Infrastructure/ContentSeo.Infrastructure.csproj
dotnet build ContentSeo.Presentation/ContentSeo.Presentation.csproj
```

Lead opens a parallel ticket to root-cause the file lock (file-handle leak in Web project's hosted services per ContentTours retro). **Out-of-scope this sprint** beyond the workaround.

### 7.5 Migration Sequence

Migrations introduced (in order):

1. PW-2: `ContentBlogs.Infrastructure/Migrations/{ts}_AlignReactionTypeEnum`
2. PW-4: `ContentSeo.Infrastructure/Migrations/{ts}_UpgradeWeatherCacheToAuditableEntity`
3. T2: `ContentBlogs.Infrastructure/Migrations/{ts}_AddBlogCommentShadowColumns` (adds `OriginalContent nvarchar(max) null`, `LanguageCode nvarchar(2) null`, indexes)
4. T4: `ContentSeo.Infrastructure/Migrations/{ts}_AddSitemapEntryEntityIndex` (unique index `(EntityType, EntityId)` on non-deleted active rows)

Run order on staging (after merge): 1 → 2 → 3 → 4. Each `dotnet ef database update` applies one migration. **No rollback automation v1** — manual `down` available via `dotnet ef migrations remove`.

### 7.6 Inbox / Outbox Hygiene

- `OutboxMessages` table grows unbounded by default. The existing `OutboxCleaner<TContext>` (already wired in DI) deletes processed rows older than 7 days. Verify it runs by checking `select count(*) from content_blogs.OutboxMessages where ProcessedAt is not null` weekly — should not exceed 50K rows.
- `InboxMessages` table similarly cleaned; rows older than 30 days deleted.
- If during sprint you find `OutboxMessages` ballooning > 100K rows, escalate — likely a stuck handler.

---

## 8. Final Acceptance Gate (Tech Lead signs off Thu 2026-06-11 17:00)

Before merge-to-main:

### 8.1 Code Quality
- [ ] All four task PRs merged. No unreviewed PRs open.
- [ ] `dotnet build` green on every changed project (per §7.4 workaround if needed).
- [ ] `dotnet test ContentBlogs.IntegrationTests` — all tests pass.
- [ ] `dotnet test ContentSeo.IntegrationTests` — all tests pass.
- [ ] `dotnet test SharedKernel.Tests` — no regressions.
- [ ] No new TODO comments in production code (or each TODO is paired with a backlog ticket — link it in the PR description).

### 8.2 Endpoint Smoke Test (manual or via Postman collection)
Run against staging after migration:
- [ ] `GET /api/v1/blogs` returns 200 + paginated.
- [ ] `POST /api/v1/blogs` (admin) creates draft, returns 201 + Guid.
- [ ] `POST /api/v1/blogs/{id}/publish` transitions to Published, sets `PublishedAt`.
- [ ] `GET /api/v1/blogs/slug/{slug}` returns the published blog.
- [ ] `POST /api/v1/blogs/{id}/comments` (auth) creates comment.
- [ ] `POST /api/v1/blogs/comments/{id}/reactions` adds a reaction; second call same type → 200 idempotent.
- [ ] `POST /api/v1/blogs/{id}/tours` links 3 tours.
- [ ] `POST /api/v1/seo/metadata` upserts metadata for blog.
- [ ] `POST /api/v1/seo/redirects` (A→B) creates redirect.
- [ ] `POST /api/v1/seo/redirects` (B→C) chain-flattens A to point to C.
- [ ] `POST /api/v1/seo/faq` creates FAQ + queues translation.
- [ ] `PUT /api/v1/seo/faq/reorder` reorders 5 items.
- [ ] `GET /sitemap.xml` returns valid XML referencing the published blog.
- [ ] `POST /api/v1/seo/sitemap/regenerate` (admin) triggers fresh render.
- [ ] `GET /api/v1/seo/weather/{placeId}` returns 404 when no cache (NoOpWeatherProvider).
- [ ] `POST /api/v1/content-core/translations/translate` queues translation, returns 202.

### 8.3 Outbox / Inbox Round-Trip
- [ ] Publishing a blog → `content-blogs.blog.published.v1` outbox row appears, then `ProcessedAt` set within 30 seconds, then ContentSeo's `SitemapEntries` table has a new row for that blog.
- [ ] Linking a tour to a blog → `content-blogs.blog-tour.linked.v1` outbox row + ContentSeo logs no errors (consumer is stub).
- [ ] Slug change on blog → `content-blogs.blog.updated.v1` outbox row → ContentSeo creates `/blog/old-slug → /blog/new-slug` Redirect with status 301.

### 8.4 Background Services Live Test
- [ ] `SitemapRegenerationService` runs at least once in 24h (check logs for "Sitemap regenerated at...").
- [ ] `WeatherPreFetchService` triggers at 05:00 UTC (or earliest forced kick after deploy). Log shows budget left = 1000 since NoOpWeatherProvider doesn't actually deduct.

### 8.5 Performance Sanity
- [ ] `GET /api/v1/blogs` p95 < 200ms (cached path).
- [ ] `GET /api/v1/blogs/{id}` p95 < 150ms.
- [ ] `GET /sitemap.xml` p95 < 500ms (cached) / < 5s (cold).
- [ ] No queries with > 100ms total time in DB profiler — if any, pair with lead before merge.

### 8.6 Documentation Hygiene
- [ ] Each new endpoint has XML doc summary on the handler + on the endpoint registration (so OpenAPI/Swagger shows the description).
- [ ] All new permissions listed in `agent-context.md` §7 (permission inventory) — if §7 doesn't exist yet, lead adds it post-sprint as housekeeping.
- [ ] `ContentBlogs-ContentSeo-team-tasks.md` (this file) MOVES to `Agents/decisions/closed/` after sprint (mirror ContentPlaces-team-tasks.md pattern).
- [ ] New AGENTS.md entries:
  - `ContentBlogs/AGENTS.md` — module overview, how to run integration tests, key patterns.
  - `ContentSeo/AGENTS.md` — same.
  - Lead seeds these from existing `ContentTours/AGENTS.md` template.

---

## 9. Out-of-Scope (Backlog for Future Sprints)

The following are **explicitly NOT** part of this sprint. Capture them in the team's backlog board (Notion/Jira/whatever), tagged `wave-5` or `wave-6`.

### 9.1 Wave 5 / Booking-adjacent (deferred)

- **Provider-authored blog drafts.** v1 is admin-only per PDF §9.2. Provider role + write permissions deferred.
- **Featured-blog toggle endpoints** (`POST /blogs/{id}/feature`, `DELETE /blogs/{id}/feature`). Pre-stubbed in T1 entity; endpoints deferred — `IsFeatured` mutation only via direct DB or admin-tool until Wave 5.
- **Comment hard-delete (admin purge).** PDF §9.2 only requires soft-delete + `[deleted]` placeholder. Hard-delete (full row removal) deferred — useful only for legal takedown requests.
- **Comment edit history.** Currently only `OriginalContent` shadow column kept; full edit history (audit log table) deferred.
- **Comment moderation queue** (admin-only "needs approval" pipeline). v1 trusts profanity filter + admin reactive deletion.
- **Reactions: bulk admin remove.** v1 admin can only remove their own reaction.
- **Featured tours per Place** (similar to featured blog). Different module — Wave 5 booking work.

### 9.2 Wave 6 / Discovery + Background (deferred)

- **Real `IWeatherProvider` impl** (OpenWeatherMap). API key handling, retry policies, budget enforcement across multi-instance, etc.
- **Real `ISearchConsolePinger` impl** (Google Search Console + Bing Webmaster Tools API). OAuth setup, ping retry, etc.
- **Real ML profanity filter** beyond keyword list.
- **Distributed lock for background services** (one instance runs at a time across cluster). Currently each instance independently runs.
- **Sitemap sharding** for > 50K entries via `sitemap-index.xml` + per-entity-type sub-sitemaps. v1 fails with `Sitemap.SizeOverflow` 503 if exceeded.
- **Full-text search for blogs** beyond naive `EF.Functions.Like`. Probably ElasticSearch or PostgreSQL `tsvector`. Currently brute-force LIKE.
- **`IBlogViewCounter` Redis-backed debounce.** v1 is an in-memory dictionary keyed by `(BlogId, UserId or IpAddressHash)` with 30-min TTL. Multi-instance deploys will double-count; OK for v1 since exact analytics is Wave 6.
- **Blog comment notifications via Messaging module.** `BlogCommentCreatedIntegrationEvent` is emitted but no consumer exists yet — Messaging module wires that in Wave 6.
- **Blog page-view analytics roll-up.** `BlogViewedIntegrationEvent` is emitted; Analytics module consumes in Wave 6.
- **Per-language SEO metadata** (separate `MetaTitle/Description` per language). v1 uses source-language only + downstream sitemap renderer falls back to entity's translated `Title`.
- **Schema.org JSON-LD generation** for blog posts (BlogPosting schema). T3's `SeoMetadata.SchemaMarkup` field is set manually by admins; auto-generation deferred.
- **OpenAPI spec generation** with full request/response samples per endpoint. v1 ships base Swagger only.
- **Per-IP rate limiting** for anonymous endpoints. v1 only has per-user-per-blog rate limit on comments.

### 9.3 Tech Debt (parallel ticket, independent of Wave grouping)

- `YallaJo.Web.exe` file lock root-cause fix (audit context §8) — separate ticket; this sprint only documents the workaround.
- 8 `ICurrentUser` violations + 28 endpoint authorization violations in legacy code (audit context §8) — separate "auth audit" sprint.
- Move `ContentPlaces-team-tasks.md` and `ContentTours-team-tasks.md` to `Agents/decisions/closed/` after each sprint completes — housekeeping owned by lead.

---

## 10. Sprint Sign-Off

```
Sprint:               ContentBlogs + ContentSeo (Wave 4 remainder)
Sprint window:        Sun 2026-05-24 → Thu 2026-06-11 (15 working days)
Endpoint count:       35 HTTP + 2 hosted services
Person-hour budget:   168 task hours + 48 review hours + 48 ceremony hours = 264
                      against 480 available (216 buffer for blocker absorption)
Tech Lead:                          __________________________  Signed: ____________
Fadwa (T1 + T2 endpoints 1–6):      __________________________  Signed: ____________
Mahmoud (T2 endpoints 7–8):         __________________________  Signed: ____________
Mohammad (T3 + T4):                 __________________________  Signed: ____________

Pre-work merge sign-off (Tue 2026-05-26):              ____________
Mid-sprint integration freeze sign-off (Sun 2026-06-07): ____________
Hard PR cutoff sign-off (Wed 2026-06-10):              ____________
Hard merge-to-main cutoff sign-off (Thu 2026-06-11):    ____________
Sprint retro + demo (Fri 2026-06-12):                  ____________
```

---

> **Reading order recap for new joiners:**
> 1. `Agents/agent-context.md` (Five Non-Negotiable Rules + 22 gotchas).
> 2. `Agents/guide.md` (CQRS + entity inheritance + endpoint cookbook).
> 3. `Agents/YallaJo.md` (full endpoint inventory + module status).
> 4. `Agents/ContentTours-team-tasks.md` (template skeleton — current/predecessor sprint).
> 5. **This file** — pre-work + 4 tasks + cross-cutting + acceptance.
> 6. PDF `Endpoints.pdf` (Wave dependencies — see §6 of compressed reference).
> 7. PDF `YallaJo Business Rules & Edge Cases.pdf` (§9 ContentBlogs + §8 SEO — encoded into this file's gates).
