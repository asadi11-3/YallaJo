# Mohammad - ContentSeo Sprint Tasks (Wave 4 Remainder)

> Personal task brief extracted from `ContentBlogs-ContentSeo-team-tasks.md`. The team file remains authoritative; this is a focused, standalone working copy for Mohammad covering **TASK 3 (SEO Metadata, Redirects, FAQ Items)** and **TASK 4 (Sitemap, Weather, Translation, Background Services)** plus all pre-work, cross-cutting concerns, and acceptance gates Mohammad is responsible for.
>
> Sprint scope: ~80 person-hours over 15 working days (3 calendar weeks). Mohammad owns **18 HTTP endpoints**, **2 background services**, **6 entities**, and **3 stubbed external-service interfaces** inside the **ContentSeo** module (plus 3 translation endpoints in ContentCore done with Tech Lead pair-review).
>
> Read everything in order. Do not skip Section 6 (Critical Rules) -- those policies are graded in code review and will block your PRs if violated.

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

## Mohammad - Sprint Allocation Summary

| Member | Module(s) | Tasks | Endpoints | Background Services | Budget (h) |
|---|---|---|---|---|---|
| **Mohammad** | ContentSeo (+ minor ContentCore translation endpoints) | TASK 3 + TASK 4 | 18 (11 + 7) | 2 (SitemapRegeneration, WeatherPreFetch) | ~80 of 120 available |

**Time distribution (target):**
- TASK 3 -- SEO Metadata, Redirects, FAQ Items: **44 h** (~5.5 working days)
- TASK 4 -- Sitemap renderer + Weather service + Translation API + 2 BG services: **36 h** (~4.5 working days)
- Pre-work (PW-1 ContentSeo half, PW-4, PW-5 SEO stubs, PW-6 ContentSeo registration, PW-7 sanity): **~6 h** spread across Days 1-3 (counted inside TASK budgets above)
- Buffer / cross-cutting (S7) / acceptance gate (S8) / standups / code review: **~40 h** of remaining budget

**Cross-team dependencies you MUST track:**
- **Fadwa** owns `IBlogQueryService.GetPublishedBlogSitemapEntriesAsync` (added in her T1 cleanup) -- required by your sitemap renderer in TASK 4. Confirm her interface signature on Day 1 standup.
- **Tech Lead** owns `ITourQueryService` stub in `ContentTours.Application.Interfaces` (returns empty list for v1) -- required by sitemap renderer.
- **Tech Lead** owns PW-2 (base `AuditableEntity` contract + V7 GUID extension + `IDateTimeProvider`) -- your PW-4 `[IAggregateRoot]` markers depend on it. Block on PW-2 merge before starting PW-4.
- Every entity factory you write uses `Guid.CreateVersion7()` and `IDateTimeProvider.UtcNow` (NOT `Guid.NewGuid()` / `DateTime.UtcNow`).

## ContentSeo Entities (You Own All Six)

(From team file S3.2 + S3.3 cross-module deps row for ContentSeo.)

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

## S4 - Enums, Interfaces, Migrations You Own

(Filtered from the team file S4 master table. Other rows belong to Fadwa or Tech Lead.)

| Type | Name | Module | Purpose | PW / Task reference |
|---|---|---|---|---|
| Enum | `RedirectStatusCode` | ContentSeo.Domain | 301 / 302 / 307 / 308 redirect HTTP status discriminator | T3 S3.4 B2 |
| Interface | `ISeoMetadataRepository` | ContentSeo.Application.Interfaces | Repository contract for SeoMetadata aggregate | PW-5 |
| Interface | `IRedirectRepository` | ContentSeo.Application.Interfaces | Repository contract for Redirect aggregate | PW-5 |
| Interface | `IFaqItemRepository` | ContentSeo.Application.Interfaces | Repository contract for FaqItem aggregate (+ translations child) | PW-5 |
| Interface | `ISitemapEntryRepository` | ContentSeo.Application.Interfaces | Repository contract for SitemapEntry aggregate | PW-5 |
| Interface | `IWeatherCacheRepository` | ContentSeo.Application.Interfaces | Repository contract for WeatherCache aggregate | PW-5 |
| Interface | `IWeatherProvider` (+ `NoOpWeatherProvider`) | ContentSeo.Application.Interfaces / ContentSeo.Infrastructure.ExternalServices | Stub for OpenWeatherMap fetch | T4 S4.5 |
| Interface | `ISearchConsolePinger` (+ `NoOpSearchConsolePinger`) | ContentSeo.Application.Interfaces / ContentSeo.Infrastructure.ExternalServices | Stub for Google Search Console ping on sitemap regen | T4 S4.5 |
| Interface | `ISitemapRenderer` (+ real impl `SitemapRenderer`) | ContentSeo.Application.Interfaces / ContentSeo.Infrastructure.Sitemap | Renders sitemap.xml from sitemap entry repo + blog/tour query services | T4 S4.5 |
| Permission catalog | `ContentSeoFeatures` + `ContentSeoPermissionCatalog` registration | ContentSeo.Infrastructure | Wires content-seo permissions into ABP `IPermissionDefinitionProvider` | PW-6 |
| Migration | `AddSeoAggregateRootMarkers` | ContentSeo.Infrastructure.Migrations | Adds `[IAggregateRoot]` markers to all ContentSeo entities; WeatherCache also gains `IsDeleted` / `DeletedAt` / `RowVersion` columns | PW-4 |

## S5 -- Pre-Work You Drive or Depend On

### PW-1 -- Unit-of-Work Dispatch Ordering (Tech Lead owns; ContentSeo half is yours to verify)

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

### PW-4 -- Add `[IAggregateRoot]` Markers + WeatherCache Schema Upgrade (YOU OWN)

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


### PW-5 -- Repository Interface Stubs (YOU OWN the 5 SEO rows)

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

### PW-6 -- `ContentSeoFeatures` + Permission Catalog Registration (YOU OWN the ContentSeo half)

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

### PW-7 -- Sanity Test for Outbox Round-Trip (YOU OWN the ContentSeo mirror)

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

### PW Merge Checklist (your items only -- full list in team file)

### Pre-Work Completion Log (Mohammad's ContentSeo half — Tech Lead to verify and tick official checklist below)

**Status:** All Mohammad-owned PW items complete on first build pass. `dotnet build YallaJo.sln` = 0 errors. `dotnet test tests/ContentSeo.Tests.Unit` = 2/2 passing.

| Item | Status | Files touched | Notes / deviations from brief |
|---|---|---|---|
| **PW-1** ContentSeo half | ✅ Done | `ContentSeo.Infrastructure/Persistence/ContentSeoUnitOfWork.cs` | Rewritten to forward to `IUnitOfWork<ContentSeoDbContext>`. DI already wired correctly in `DependencyInjection.cs` line 31 — only the wrapper class was bypassing the SharedKernel UoW. **ContentBlogs half is Tech Lead's parallel work — untouched.** |
| **PW-1b** unit test | ✅ Done | `tests/ContentSeo.Tests.Unit/Persistence/ContentSeoUnitOfWorkDispatchesEventsTests.cs` | 2 tests (positive + negative path) — both green. Verifies `IDomainEventDispatcher.DispatchAsync` is invoked exactly once when an aggregate raises one event, and not invoked when no events are raised. |
| **PW-4a** `IAggregateRoot` markers | ✅ Done | `SeoMetadata.cs`, `Redirect.cs`, `FaqItem.cs`, `SitemapEntry.cs` | All four entities already declared `: AuditableEntity, IAggregateRoot` in source on inspection — likely added by an earlier setup script. Verified compiles clean. |
| **PW-4b** WeatherCache schema upgrade (code) | ✅ Done | `ContentSeo.Domain/Entities/WeatherCache.cs` | Already `: AuditableEntity, IAggregateRoot` in source. EF model now diffs against snapshot → migration generated automatically. |
| **PW-4c** stub domain event records | ✅ Done | `ContentSeo.Domain/Events/` × 10 files | `SeoMetadata{Created,Updated,Deleted}DomainEvent`, `Redirect{Created,Deactivated,ChainFlattened}DomainEvent`, `FaqItem{Created,Updated,Deleted,Reordered}DomainEvent`. All `: DomainEventBase`. Use `using YallaJo.SharedKernel.Domain.Event;` — note SharedKernel namespace is `Event` (singular), file dir is `Events`. |
| **PW-4d** migration | ✅ Done | `ContentSeo.Infrastructure/Migrations/20260513122151_UpgradeWeatherCacheToAuditableEntity.cs` | Generated via `dotnet ef migrations add UpgradeWeatherCacheToAuditableEntity --project ContentSeo.Infrastructure --startup-project YallaJo.Api --context ContentSeoDbContext`. ADD-COLUMN exactly as specified: `IsDeleted bit NOT NULL DEFAULT 0`, `DeletedAt datetime2 NULL`, `RowVersion rowversion NOT NULL`. Migration NOT yet applied to DB — Tech Lead to `dotnet ef database update` in deployment window. |
| **PW-5a** repository interfaces | ✅ Done | `ContentSeo.Application/Interfaces/` × 5 files | `I{SeoMetadata,Redirect,FaqItem,SitemapEntry,WeatherCache}Repository.cs` — all empty bodies, all `: IRepository<T>` (Guid key). |
| **PW-5b** EF repo impls | ✅ Done | `ContentSeo.Infrastructure/Repositories/` × 5 files | All `internal sealed class FooRepository(ContentSeoDbContext context) : EfRepository<Foo, Guid>(context), IFooRepository;` primary-constructor pattern. Mirrors ContentPlaces convention. Registered in DI as `services.AddScoped<IFooRepository, FooRepository>()`. |
| **PW-6** features + catalog | ✅ Done | `ContentSeo.Contracts/Authorization/ContentSeoFeatures.cs`, `ContentSeoPermissionCatalog.cs` | 5 features (`SeoMetadata`, `Redirect`, `Sitemap`, `FaqItem`, `Weather`) — all use bare `nameof()` constants (no module prefix) to match existing ContentCore/ContentTours convention. **DEVIATION from brief** which suggested `"ContentSeo.SeoMetadata"` literal. Catalog declares 21 `PermissionDescriptor` rows. **AppAction.Manage doesn't exist in the codebase** — used `AppAction.Refresh` for Sitemap regen (perfect semantic match per AppAction docstring "for batch/cache refresh admin endpoints"). Weather force-refresh also uses `AppAction.Refresh`. `ContentSeo.Contracts.csproj` now references `SharedKernel.Application` for `IPermissionCatalog`. Registered as `IPermissionCatalog` singleton in `ContentSeo.Infrastructure/DependencyInjection.cs`. |
| **PW-7** sanity test | ⚠️ Partial — written with documented deviation | `tests/ContentSeo.IntegrationTests/EventDispatchSanityTests.cs` | **DEVIATION:** Uses `ServiceCollection.BuildServiceProvider()` + `AddContentSeoInfrastructure(...)` + InMemory EF swap, NOT `WebApplicationFactory<Program>`. Rationale: the brief's outbox-row assertion requires (a) `SeoMetadataChangedIntegrationEvent` class, (b) `IntegrationEventTypeRegistry` registration, (c) `INotificationHandler<SeoMetadataCreatedDomainEvent>` mapper → all are T3 deliverables, NOT pre-work. Pragmatic version asserts the contract that IS pre-work-verifiable: domain-event dispatch via the fixed UoW. 2nd test smoke-resolves all 5 repos + UoW + PermissionCatalog. **`YallaJo.Api/Program.cs` HAS `public partial class Program;` appended** so the brief-exact `WebApplicationFactory<Program>` test can land later in T3 once the integration-event plumbing exists. |

**ROI improvements applied this round (out-of-scope additions, raised in m0010):**

- ✅ **ROI-1** `AsyncAwaitBestPractices` version drift fixed. Removed redundant `<PackageReference Update="AsyncAwaitBestPractices" Version="9.0.0" />` from `SharedKernel.Domain.csproj` and `SharedKernel.Presentation.csproj` (was overriding the global `10.0.0` pin from `Directory.Build.props`). Also removed the now-redundant 3-line `<PackageReference Update>` block in `SharedKernel.Application.csproj`.
- ✅ **ROI-2 / ROI-3** Created `tests/ContentSeo.Tests.Unit` and `tests/ContentSeo.IntegrationTests` projects + added both to `YallaJo.sln`. Conventions mirror `ContentCore.Tests.Unit` (xunit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0, EF InMemory 9.0.15). Integration project adds `Microsoft.AspNetCore.Mvc.Testing 9.0.15` + reference to `YallaJo.Api`.
- ✅ **ROI-4** Audited `Directory.Build.props` — analyzers (AsyncAwaitBestPractices, StyleCop, Meziantou, Roslynator) are globally `Include`'d with `PrivateAssets="all"`, so they ARE inherited by all `ContentSeo.*` projects already. No action needed.
- ✅ **ROI-5** `IntegrationEventTypeRegistry` test pattern (`tests/SharedKernel.Tests.Unit/IntegrationEventTypeRegistryTests.cs`) already uses drift-proof reverse-parity (KnownMappings ↔ NameToType). When the 4 ContentSeo integration events get added in T3, just append 4 rows to both the registry AND the test array.
- 🔜 **ROI-6** `PeriodicTimer` over `while + Task.Delay` — deferred to T4 BG service implementation (no code exists yet to refactor).
- 🔜 **ROI-7** XxHash64 over SHA-256 for redirect cache keys — deferred to T3 query handler implementation.

**Infrastructure side-effects discovered:**

- `ContentSeo.Infrastructure.csproj` gained `<InternalsVisibleTo Include="ContentSeo.Tests.Unit" />` + `<InternalsVisibleTo Include="ContentSeo.IntegrationTests" />` (required because `ContentSeoUnitOfWork` is `internal sealed`, matching the convention used by Auth.Infrastructure, ContentPlaces.Presentation, ContentTours.* in the repo).
- `YallaJo.Api/Program.cs` gained `public partial class Program;` at end (idiomatic enabler for `WebApplicationFactory<Program>` integration tests).

### Pre-work merge checklist (Tech Lead signs off)

- [ ] PW-1 merged. `dotnet test` green for both modules.
- [ ] PW-2 enum + migration merged. Verify `dotnet ef migrations list --project ContentBlogs.Infrastructure` shows `AlignReactionTypeEnum`.
- [ ] PW-3 `BlogComment : IAggregateRoot` + 4 stub event records compiled.
- [ ] PW-4 ContentSeo aggregates marked + 11 stub event records compiled. `UpgradeWeatherCacheToAuditableEntity` migration applied.
- [ ] PW-5 8 repository interface stubs compiled (no method bodies — that's owner work).
- [ ] PW-6 Both permission catalogs registered. Run app locally, verify `PermissionSeeder` logs `Discovered 2 catalogs: ContentBlogs (13 perms), ContentSeo (≥15 perms)`.
- [ ] PW-7 Sanity tests pass.
- [ ] Lead announces in standup: "Pre-work green. Task 1/2/3/4 unblocked at $TIMESTAMP."

## S6 - Critical Rules for ALL Tasks (Sprint-Wide Policy -- Graded in Code Review)

These 16 rules are enforced by Tech Lead in PR review. Violations block merge.

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

# TASK 3 - SEO Metadata, Redirects, FAQ Items

**Owner: Mohammad** | Module: ContentSeo | Budget: **44 h (~5.5 days)** | Endpoints: 11



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

# TASK 4 - Sitemap Renderer, Weather Service, Translation API, Background Services

**Owner: Mohammad** (with Tech Lead pair-review on ContentCore translation endpoints) | Modules: ContentSeo + ContentCore (translation endpoints only) | Budget: **36 h (~4.5 days)** | Endpoints: 7 | Background services: 2



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

## S7 - Cross-Cutting Concerns (Apply Throughout Both Tasks)

(Verbatim from team file S7. These run in parallel with T3/T4 implementation and feed into S8 acceptance gate.)

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

## S8 - Final Acceptance Gate (Must Pass Before Sprint Sign-Off)

(Verbatim from team file S8. Mohammad's endpoint smoke-test list at S8.2 is your responsibility -- every one of your 18 endpoints must return the expected status code with proper authorization in place.)

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

## Mohammad - Sprint Sign-Off Checklist

When all of the above is green, append your signature to the team file S10 sign-off block:

> [ ] **Mohammad** -- T3 (SEO Metadata + Redirects + FAQ Items) done, T4 (Sitemap + Weather + Translation API + Background Services) done -- Date: ____________

### Personal Dependency Tracker (verify before merging each PR)

| Dependency | Owner | Status | Where it blocks you |
|---|---|---|---|
| PW-1 (UoW dispatch ordering) merged | Tech Lead | [ ] | All T3 + T4 outbox handlers depend on this for atomic dispatch |
| PW-2 (`AuditableEntity` base + V7 GUID ext + `IDateTimeProvider`) merged | Tech Lead | [ ] | Every entity factory in T3 + T4 |
| PW-3 (BlogService.cs delete) | -- | [x] (predecessor sprint) | -- |
| PW-4 (`[IAggregateRoot]` markers on ContentSeo entities + WeatherCache upgrade migration) | **Mohammad (you)** | [ ] | PR-1 of T3 |
| PW-5 (Repository interface stubs for SEO / Redirect / FAQ / Sitemap / Weather) | **Mohammad (you)** | [ ] | Every T3 + T4 command/query handler |
| PW-6 (`ContentSeoFeatures` + `ContentSeoPermissionCatalog` DI registration) | **Mohammad (you)** | [ ] | Every endpoint's `MustHavePermissionAttribute` |
| PW-7 (sanity test for outbox round-trip in ContentSeo.IntegrationTests) | **Mohammad (you)** | [ ] | T3 S3.6 integration-event emission |
| `IBlogQueryService.GetPublishedBlogSitemapEntriesAsync` signature confirmed | Fadwa | [ ] | T4 SitemapRenderer (S4.2) |
| `ITourQueryService` stub in `ContentTours.Application.Interfaces` | Tech Lead | [ ] | T4 SitemapRenderer (S4.2) |
| Integration freeze (Day 13 EOD) -- no new endpoints/events after this | Whole team | [ ] | Hard cutoff before S8 gate |

### Where to find me / where I find others (standup channels)
- Daily standup: 09:30 local, async-friendly. Post in `#yallajo-sprint-wave4` if remote.
- Pair-review with Tech Lead on TASK 4 translation endpoints (ContentCore): schedule before Day 8.
- Cross-team handshake with Fadwa: Day 1 (interface signatures) + Day 8 (mid-sprint integration smoke).

---

## Reading Order Recap (Stick This on Your Monitor)

> **Reading order recap for new joiners:**
> 1. `Agents/agent-context.md` (Five Non-Negotiable Rules + 22 gotchas).
> 2. `Agents/guide.md` (CQRS + entity inheritance + endpoint cookbook).
> 3. `Agents/YallaJo.md` (full endpoint inventory + module status).
> 4. `Agents/ContentTours-team-tasks.md` (template skeleton — current/predecessor sprint).
> 5. **This file** — pre-work + 4 tasks + cross-cutting + acceptance.
> 6. PDF `Endpoints.pdf` (Wave dependencies — see §6 of compressed reference).
> 7. PDF `YallaJo Business Rules & Edge Cases.pdf` (§9 ContentBlogs + §8 SEO — encoded into this file's gates).