# ContentBlogs & ContentSeo — Fix Plan

> Companion to `Agents/tasks/ContentBlogs-ContentSeo-gaps.md`.
> Maps every gap (B1–B12, W1–W6, D1–D3) to **concrete file paths, code snippets, and verification steps**.
> Five phases. Each phase = one self-contained PR. Phases must run in order.
> **Execution:** delegate each phase to an executor subagent via `task()` after team approval.

---

## Phase Overview

| Phase | Goal | Items | PRs |
|---|---|---|---|
| 1 | **Stop runtime failures** | B9, W5 | 1 PR |
| 2 | PDF §9 (Blog) compliance | B1, B2, B8, B10, B11 | 1 PR |
| 3 | PDF §8 (SEO) compliance | B3, B7, B12, W1, W4, W6 | 1–2 PRs |
| 4 | PDF §11 (Weather) compliance | B4, B5, B6, W2, W3 | 1–2 PRs |
| 5 | Code hygiene | D1, D2, D3 | 1 PR |

---

## Phase 1 — Stop Runtime Failures

### B9. Register ContentSeo integration events

**Why first:** `IntegrationEventTypeRegistry.GetName(typeof(T))` throws `InvalidOperationException` (file L107-L112) when called for unregistered types. Every FAQ / Redirect / SeoMetadata domain-event handler in `ContentSeo.Infrastructure/EventHandlers/` will trip this when it tries to enqueue an outbox row.

**File:** `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`

**Change:** Append a new block after line 97 (`["content-blogs.blog.unfeatured.v1"] = typeof(BlogUnfeaturedIntegrationEvent),`):

```csharp
        // ── ContentSeo (4 events) ──
        ["content-seo.faq.changed.v1"]                  = typeof(FaqItemChangedIntegrationEvent),
        ["content-seo.redirect.created.v1"]             = typeof(RedirectCreatedIntegrationEvent),
        ["content-seo.redirect.chain-flattened.v1"]     = typeof(RedirectChainFlattenedIntegrationEvent),
        ["content-seo.metadata.changed.v1"]             = typeof(SeoMetadataChangedIntegrationEvent),
```

The `using ContentSeo.Contracts.IntegrationEvents;` import already exists (line 7) — no new imports needed.

**Tests:**
- Add `IntegrationEventTypeRegistryTests.AllContentSeoEvents_AreRegistered`:
  ```csharp
  [Theory]
  [InlineData(typeof(FaqItemChangedIntegrationEvent))]
  [InlineData(typeof(RedirectCreatedIntegrationEvent))]
  [InlineData(typeof(RedirectChainFlattenedIntegrationEvent))]
  [InlineData(typeof(SeoMetadataChangedIntegrationEvent))]
  public void EventType_IsRegistered(Type eventType) =>
      Assert.DoesNotThrow(() => IntegrationEventTypeRegistry.GetName(eventType));
  ```
- Add ContentSeo outbox round-trip integration test: create a FAQ item via `CreateFaqItemCommand`, assert one `OutboxMessage` row is written with `EventType == "content-seo.faq.changed.v1"`.

**Verification:**
```pwsh
dotnet build YallaJo.SharedKernel.Infrastructure
dotnet test --filter "FullyQualifiedName~IntegrationEventTypeRegistry"
dotnet test --filter "FullyQualifiedName~ContentSeo & FullyQualifiedName~Outbox"
```

---

### W5. Resolve `SeoMetadataDeletedDomainEvent` orphan

**Background:** Handler `ContentSeo.Infrastructure/EventHandlers/SeoMetadataDeletedDomainEventHandler.cs` exists but no method on `SeoMetadata` raises this event. PDF v1 has no SEO-metadata delete endpoint.

**Decision required from team — pick ONE:**

| Option | When |
|---|---|
| **A. Delete the orphan handler** | If SEO metadata is permanently linked to a parent entity (Place / Tour / Business / Blog) and gets cleaned up via FK cascade on parent delete (existing pattern). **Recommended.** |
| **B. Add `SeoMetadata.SoftDelete()`** | If admins ever need to clear an entity's SEO record without deleting the entity itself. |

**If A (recommended):**
- Delete `ContentSeo.Infrastructure/EventHandlers/SeoMetadataDeletedDomainEventHandler.cs`
- Delete `ContentSeo.Domain/Events/SeoMetadataDeletedDomainEvent.cs`
- Run `dotnet build` to confirm no references break

**If B:**
- Add to `ContentSeo.Domain/Entities/SeoMetadata.cs`:
  ```csharp
  public void SoftDelete(DateTime now)
  {
      if (IsDeleted) return;
      MarkDeleted(now);
      RaiseDomainEvent(new SeoMetadataDeletedDomainEvent(Id, EntityType, EntityId));
  }
  ```
- Add a `DeleteSeoMetadataCommand` + handler in `ContentSeo.Application/Commands/SeoMetadata/DeleteSeoMetadata/`
- Add endpoint `DELETE /api/v1/seo/metadata/{id}` to `ContentSeo.Presentation/Endpoints/SeoMetadata/SeoMetadataEndpoints.cs`

**Verification:** `dotnet build ContentSeo.Infrastructure` green; no dangling references.

---

## Phase 2 — PDF §9 Blog Compliance

### B1. AR + EN translation gate in `Blog.Publish()`

**Files:**
- `ContentBlogs.Domain/Entities/Blog.cs` — `Publish()` method
- `ContentBlogs.Application/Commands/Blog/PublishBlog/PublishBlogCommandHandler.cs`

**Pattern:** The domain method receives the set of existing translation language codes for self-validation; the handler is responsible for loading them.

**Domain change:**
```csharp
public Result Publish(
    IReadOnlyCollection<string> existingTranslationLanguageCodes,
    DateTime utcNow)
{
    if (Status == BlogStatus.Published)
        return Result.Failure(new Error("Blog.AlreadyPublished", "Blog is already published."));

    var langs = new HashSet<string>(existingTranslationLanguageCodes, StringComparer.OrdinalIgnoreCase);
    if (!langs.Contains("en"))
        return Result.Failure(new Error("Blog.Translation.EnglishRequired",
            "English translation is required before publish."));
    if (!langs.Contains("ar"))
        return Result.Failure(new Error("Blog.Translation.ArabicRequired",
            "Arabic translation is required before publish."));

    Status = BlogStatus.Published;
    PublishedAt ??= utcNow;   // first-publish only; never overwrite
    MarkUpdated();
    RaiseDomainEvent(new BlogPublishedDomainEvent(Id, Slug, utcNow));
    return Result.Success();
}
```

**Handler change:** Before calling `blog.Publish(...)`, load existing translation languages:
```csharp
var existingLangs = await dbContext.BlogTranslations
    .Where(t => t.BlogId == command.BlogId)
    .Select(t => t.LanguageCode)
    .ToListAsync(ct);

var result = blog.Publish(existingLangs, dateTimeProvider.UtcNow);
if (result.IsFailure) return result;
```

**Tests:**
- `PublishBlog_WithEnOnly_Fails_WithArabicRequiredError`
- `PublishBlog_WithArOnly_Fails_WithEnglishRequiredError`
- `PublishBlog_WithBothEnAndAr_Succeeds`
- `PublishBlog_Twice_Fails_WithAlreadyPublishedError`
- `PublishBlog_PublishedAt_NotOverwritten_OnRepublishAfterUnpublish`

---

### B2. Reaction same-type toggle (remove, not no-op)

**Files:**
- `ContentBlogs.Domain/Entities/BlogComment.cs` — `AddOrReplaceReaction()` method
- `ContentBlogs.Domain/Events/BlogCommentReactionChangedDomainEvent.cs` — add `IsRemoved` flag
- `ContentBlogs.Application/Commands/BlogComment/AddOrReplaceBlogCommentReaction/AddOrReplaceBlogCommentReactionCommandHandler.cs`

**Event change:**
```csharp
public sealed record BlogCommentReactionChangedDomainEvent(
    Guid CommentId,
    Guid UserId,
    ReactionType ReactionType,
    bool IsRemoved) : IDomainEvent;
```

**Domain change:**
```csharp
public Result AddOrReplaceReaction(Guid userId, ReactionType type)
{
    var existing = _reactions.FirstOrDefault(r => r.UserId == userId);

    // Same-type re-send → TOGGLE OFF (PDF §9 + YallaJo.md L1376)
    if (existing != null && existing.Type == type)
    {
        _reactions.Remove(existing);
        RaiseDomainEvent(new BlogCommentReactionChangedDomainEvent(
            Id, userId, type, IsRemoved: true));
        return Result.Success();
    }

    // Different type → REPLACE
    if (existing != null)
    {
        _reactions.Remove(existing);
    }
    _reactions.Add(BlogCommentReaction.Create(Id, userId, type));
    RaiseDomainEvent(new BlogCommentReactionChangedDomainEvent(
        Id, userId, type, IsRemoved: false));
    return Result.Success();
}
```

**Endpoint behavior:** No change needed — same `POST /comments/{id}/reactions` body. Response should differentiate added vs removed via 200/204 or response DTO field `Removed: true`.

**Tests:**
- `AddReaction_SameType_RemovesExisting_AndRaisesEventWithIsRemovedTrue`
- `AddReaction_DifferentType_ReplacesExisting_AndRaisesEventWithIsRemovedFalse`
- `AddReaction_NoExisting_AddsNew_AndRaisesEventWithIsRemovedFalse`
- `RemoveReaction_ThenSameTypeAdd_AddsBackNew`

---

### B8. Remove image requirement from publish gate; add default OG image

**Files:**
- `ContentBlogs.Application/Commands/Blog/PublishBlog/PublishBlogCommandHandler.cs` — remove the `attachments.Count == 0` guard
- `ContentBlogs.Application/Commands/Blog/PublishBlog/PublishBlogCommandValidator.cs` — remove validator rule if present
- `ContentBlogs.Infrastructure/EventHandlers/BlogPublishedDomainEventHandler.cs` (or wherever OG metadata is generated) — fall back to default
- `ContentBlogs.Infrastructure/Options/BlogPublishingOptions.cs` (new) — config for default OG image

**Options:**
```csharp
public sealed class BlogPublishingOptions
{
    public const string SectionName = "ContentBlogs:Publishing";
    public string DefaultOgImageUrl { get; init; } = "/assets/og-default.png";
}
```

**Registration:** In `ContentBlogs.Infrastructure/DependencyInjection.cs`:
```csharp
services.Configure<BlogPublishingOptions>(
    configuration.GetSection(BlogPublishingOptions.SectionName));
```

**Usage in OG image resolution:**
```csharp
var ogImageUrl = blog.Attachments
    .OrderByDescending(a => a.IsPrimary)
    .ThenBy(a => a.SortOrder)
    .Select(a => a.Url)
    .FirstOrDefault()
    ?? options.Value.DefaultOgImageUrl;
```

**Tests:**
- `PublishBlog_WithoutImages_Succeeds`
- `PublishBlog_OgImage_FallsBackToDefault_WhenNoAttachments`
- `PublishBlog_OgImage_UsesPrimary_WhenMultipleAttachments`

---

### B10. Add `TourDeletedIntegrationEventHandler` in ContentBlogs

**Why:** PDF §9.3 "Tour deletion → BlogTours row removed". No handler exists today.

**New file:** `ContentBlogs.Infrastructure/EventHandlers/TourDeletedIntegrationEventHandler.cs`

**Template:** mirror existing `PlaceDeletedIntegrationEventHandler.cs` (and the ContentSeo `PlaceCreatedIntegrationEventHandler` for inbox idempotency pattern).

```csharp
using ContentTours.Contracts.IntegrationEvents;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Infrastructure.EventHandlers;

/// <summary>
/// PDF §9.3: When a Tour is deleted, remove any BlogTours row that references it.
/// Each removal raises a <c>BlogTourUnlinkedDomainEvent</c> through Blog.RegisterTourUnlinked(...).
/// </summary>
public sealed class TourDeletedIntegrationEventHandler(
    ContentBlogsDbContext dbContext,
    IContentBlogsUnitOfWork unitOfWork,
    IContentBlogsInboxStore inboxStore,
    ILogger<TourDeletedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<TourDeletedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<TourDeletedIntegrationEvent> notification,
        CancellationToken ct)
    {
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogDebug(
                "ContentBlogs: Message {MessageId} (TourDeleted {TourId}) already processed; skipping.",
                notification.MessageId, notification.Event.TourId);
            return;
        }

        var blogs = await dbContext.Blogs
            .Include(b => b.Tours)
            .Where(b => b.Tours.Any(t => t.TourId == notification.Event.TourId))
            .ToListAsync(ct);

        foreach (var blog in blogs)
        {
            blog.RegisterTourUnlinked(notification.Event.TourId);
        }

        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "ContentBlogs: Removed BlogTours rows for deleted Tour {TourId} from {BlogCount} blog(s).",
            notification.Event.TourId, blogs.Count);
    }
}
```

**Registration:** MediatR assembly scan in `ContentBlogs.Infrastructure/DependencyInjection.cs` will pick it up automatically.

**Tests:**
- Integration test: publish `TourDeletedIntegrationEvent` to a blog with that tour linked → BlogTours row removed, `BlogTourUnlinkedIntegrationEvent` emitted to outbox.
- Idempotency: replay same message → second handler invocation is a no-op.

---

### B11. BlogComment domain events — add internal consumers

**Why:** 4 BlogComment events (`Created/Updated/Deleted/ReactionChanged`) have zero consumers. No outbox, no cache invalidation, no projection update.

**Decision:** v1 keeps comments local to ContentBlogs (no cross-module integration). Add minimal in-Infrastructure handlers that maintain read-side denormalization and cache invalidation.

**New files in `ContentBlogs.Infrastructure/EventHandlers/`:**

1. `BlogCommentCreatedDomainEventHandler.cs`:
   - Invalidate cache tag `blog:{blogId}:comments`
   - (Optional: increment `Blog.CommentCount` if denormalized counter is added — see schema note below)

2. `BlogCommentUpdatedDomainEventHandler.cs`:
   - Invalidate cache tag `blog:{blogId}:comments`

3. `BlogCommentDeletedDomainEventHandler.cs`:
   - Invalidate cache tag `blog:{blogId}:comments`

4. `BlogCommentReactionChangedDomainEventHandler.cs`:
   - Invalidate cache tag `blog-comment:{commentId}:reactions`

**Pattern (each handler):**
```csharp
public sealed class BlogCommentCreatedDomainEventHandler(
    HybridCache cache,
    ILogger<BlogCommentCreatedDomainEventHandler> logger)
    : INotificationHandler<BlogCommentCreatedDomainEvent>
{
    public async Task Handle(BlogCommentCreatedDomainEvent e, CancellationToken ct)
    {
        await cache.RemoveByTagAsync($"blog:{e.BlogId}:comments", ct);
        logger.LogDebug("ContentBlogs: Invalidated comments cache for blog {BlogId}", e.BlogId);
    }
}
```

> **Critical:** Domain-event handlers **MUST NOT** call `SaveChangesAsync` (agent-context.md §0.3 rule 5). The cache invalidation pattern above is safe. If `Blog.CommentCount` is added, the increment must happen inside the same `SaveChangesAsync` as comment creation — done via the **command handler** itself, not a domain-event handler.

**Optional schema change (if adopting denormalized comment counter):**
- `AddCommentCountToBlog` migration: `CommentCount int NOT NULL DEFAULT 0`
- Backfill: `UPDATE Blogs SET CommentCount = (SELECT COUNT(*) FROM BlogComments WHERE BlogComments.BlogId = Blogs.Id AND IsDeleted = 0)`

**Tests:**
- Cache invalidation triggers on comment create/update/delete
- If counter adopted: `Blog.CommentCount` increments correctly on `CreateBlogCommentCommand`

---

## Phase 3 — PDF §8 SEO Compliance

### B7. `SeoRedirectMiddleware` for 404 → 301/302

**New file:** `YallaJo.Web/Middleware/SeoRedirectMiddleware.cs`

```csharp
using ContentSeo.Application.Services;
using YallaJo.SharedKernel.Application.Abstractions.DateTime;

namespace YallaJo.Web.Middleware;

public sealed class SeoRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IRedirectLookupService lookup,
        IRedirectHitCounter hits)
    {
        await next(context);

        if (context.Response.StatusCode != StatusCodes.Status404NotFound
            || context.Response.HasStarted)
        {
            return;
        }

        var oldUrl = context.Request.Path.Value + context.Request.QueryString.Value;
        var redirect = await lookup.FindActiveAsync(oldUrl, context.RequestAborted);
        if (redirect is null) return;

        // Fire-and-forget hit increment (don't block the 301)
        _ = hits.IncrementAsync(redirect.Id, context.RequestAborted);

        context.Response.StatusCode = redirect.StatusCode; // 301 or 302
        context.Response.Headers.Location = redirect.NewUrl;
    }
}
```

**Registration in `YallaJo.Web/Program.cs`:** BEFORE `app.MapEndpoints(...)`:
```csharp
app.UseMiddleware<SeoRedirectMiddleware>();
```

**New supporting services in `ContentSeo.Application/Services/`:**

1. `IRedirectLookupService` — cached lookup:
   ```csharp
   public interface IRedirectLookupService
   {
       Task<RedirectLookupDto?> FindActiveAsync(string oldUrl, CancellationToken ct);
   }
   ```
   Implementation in `ContentSeo.Infrastructure/Services/RedirectLookupService.cs`:
   - Use `HybridCache` with key `seo:redirect:{oldUrl}` (5-min TTL)
   - Tag `seo:redirects:all` — invalidated by `RedirectCreatedDomainEventHandler` and `RedirectDeactivatedDomainEventHandler`

2. `IRedirectHitCounter` — batched increment:
   - Queue increments in a `ConcurrentDictionary<Guid, int>`
   - Flush every 30s via `RedirectHitCounterFlushService : BackgroundService` calling `IRedirectRepository.IncrementHitsBatchAsync(IDictionary<Guid,int>)` raw-SQL batch update

**Tests:**
- 404 with matching active redirect → response 301/302 with `Location` header
- 404 with no matching redirect → unchanged 404
- 404 with deactivated redirect → unchanged 404
- Hit counter increments observed after flush interval

---

### B3. Sitemap-index sharding for >50K URLs

**File:** `ContentSeo.Infrastructure/Sitemap/SitemapRenderer.cs` — replace overflow-error path with sharded render

**Strategy:**
- If total active entries ≤ 50,000 → render single `/sitemap.xml`
- If > 50,000 → render `/sitemap.xml` as a **sitemap-index** that lists per-EntityType sub-sitemaps; chunk any EntityType that itself exceeds 50K into numbered files

**New endpoint pattern:** `GET /sitemaps/{fileName}.xml`

**Sketch:**
```csharp
public async Task<SitemapRenderResult> RenderAsync(CancellationToken ct)
{
    var entries = await _repo.GetActiveOrderedAsync(ct);

    if (entries.Count <= MaxUrlsPerFile)
    {
        return new SitemapRenderResult(
            IndexXml: null,
            Files: [new SitemapFile("sitemap.xml", BuildUrlSet(entries))]);
    }

    var groups = entries.GroupBy(e => e.EntityType).ToList();
    var subFiles = new List<SitemapFile>();
    foreach (var group in groups)
    {
        var chunks = group.Chunk(MaxUrlsPerFile).ToList();
        for (var i = 0; i < chunks.Count; i++)
        {
            var fileName = chunks.Count == 1
                ? $"{group.Key.ToLowerInvariant()}.xml"
                : $"{group.Key.ToLowerInvariant()}-{i}.xml";
            subFiles.Add(new SitemapFile(fileName, BuildUrlSet(chunks[i])));
        }
    }
    var indexXml = BuildSitemapIndex(subFiles, _options.PublicBaseUrl);
    return new SitemapRenderResult(IndexXml: indexXml, Files: subFiles);
}

private const int MaxUrlsPerFile = 50_000;
```

**Endpoint additions in `ContentSeo.Presentation/Endpoints/Sitemap/SitemapEndpoints.cs`:**
- Keep `GET /sitemap.xml` returning the index (or single file if <50K)
- Add `GET /sitemaps/{fileName}.xml` route that serves a specific shard file

**Storage:** sub-sitemap files can be (a) regenerated on each request from DB (acceptable for v1), (b) cached in `HybridCache` for 5 min, or (c) written to disk by `SitemapRegenerationService`. **Recommend (b)** — simplest.

**Tests:**
- 50,001 entries → index XML lists per-EntityType sub-sitemaps
- Single EntityType with 50,001 entries → chunks into `blog-0.xml` + `blog-1.xml`
- Hit `/sitemaps/blog-0.xml` returns correct XML
- Total URL count across all sub-files matches DB count

**Remove:** the `Sitemap.SizeOverflow` error code and 503 path.

---

### B12. Inbox consumers in ContentSeo for Blog* and Tour* events

**Why:** Sitemap is stale. When a blog publishes or a tour is approved, the sitemap entry isn't created/updated until the next 6h regeneration cycle — and even then only via full DB scan, not event-driven.

**New files in `ContentSeo.Infrastructure/EventHandlers/`** (pattern: copy `PlaceCreatedIntegrationEventHandler.cs` as template):

| New Handler | Source Event | Action |
|---|---|---|
| `BlogPublishedIntegrationEventHandler.cs` | `BlogPublishedIntegrationEvent` | Create `SeoMetadata` (if missing) + active `SitemapEntry` with priority 0.6, changefreq `monthly`, url `/blog/{slug}` |
| `BlogUnpublishedIntegrationEventHandler.cs` | `BlogUnpublishedIntegrationEvent` | Set `SitemapEntry.IsActive = false` |
| `BlogArchivedIntegrationEventHandler.cs` | `BlogArchivedIntegrationEvent` | Set `SitemapEntry.IsActive = false` (archived blogs not in sitemap) |
| `BlogUpdatedIntegrationEventHandler.cs` | `BlogUpdatedIntegrationEvent` | Touch `SitemapEntry.LastModified` to `event.UpdatedAt`; if `OldSlug != NewSlug` → create 301 redirect (`CreateRedirectCommand(oldUrl: $"/blog/{oldSlug}", newUrl: $"/blog/{newSlug}", status: 301)`) |
| `BlogDeletedIntegrationEventHandler.cs` | `BlogDeletedIntegrationEvent` | Set `SitemapEntry.IsActive = false` |
| `TourCreatedIntegrationEventHandler.cs` | `TourCreatedIntegrationEvent` | Create `SeoMetadata` + inactive `SitemapEntry` (priority 0.8, changefreq `weekly`) |
| `TourApprovedIntegrationEventHandler.cs` | `TourApprovedIntegrationEvent` | Activate `SitemapEntry` |
| `TourDeletedIntegrationEventHandler.cs` | `TourDeletedIntegrationEvent` | Set `SitemapEntry.IsActive = false` |
| `TourSuspendedIntegrationEventHandler.cs` | `TourSuspendedIntegrationEvent` | Set `SitemapEntry.IsActive = false` |

**Required fields on each integration event** (verify with explore before coding):
- `BlogPublishedIntegrationEvent` needs `Slug`, `BlogId`, `UpdatedAt`
- `BlogUpdatedIntegrationEvent` needs `OldSlug`, `NewSlug`, `BlogId`, `UpdatedAt`
- `TourCreatedIntegrationEvent` needs `TourId`, `Slug`, `UpdatedAt`
- If any field is missing, **first** add it to the contract event record + the publisher in the source module (then bump registry name if breaking).

**Registration:** MediatR assembly scan picks up automatically.

**Tests:** one inbox-replay test per handler (mirror pattern from existing `PlaceCreatedIntegrationEventHandlerTests`).

---

### W1. Hreflang `ar` + `en` + `x-default` in sitemap

**File:** `ContentSeo.Infrastructure/Sitemap/SitemapRenderer.cs`

**Change:** For each `<url>` entry, add three `<xhtml:link>` siblings:
```xml
<xhtml:link rel="alternate" hreflang="ar" href="{publicBaseUrl}/ar{path}" />
<xhtml:link rel="alternate" hreflang="en" href="{publicBaseUrl}/en{path}" />
<xhtml:link rel="alternate" hreflang="x-default" href="{publicBaseUrl}{path}" />
```

Declare the namespace on the root `<urlset>`:
```xml
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"
        xmlns:xhtml="http://www.w3.org/1999/xhtml">
```

**Tests:** rendered XML contains all three alternates per URL; namespace declaration present.

---

### W4. `SitemapEntry.Create()` with `LastModified` parameter

**File:** `ContentSeo.Domain/Entities/SitemapEntry.cs`

**Change:**
```csharp
public static SitemapEntry Create(
    string url,
    string entityType,
    Guid entityId,
    string changeFrequency,
    decimal priority,
    bool isActive,
    DateTime lastModified)   // ← NEW
{
    // existing validation ...
    return new SitemapEntry
    {
        Id = Guid.CreateVersion7(),
        Url = url,
        EntityType = entityType,
        EntityId = entityId,
        ChangeFrequency = changeFrequency,
        Priority = priority,
        IsActive = isActive,
        LastModified = lastModified,
    };
}

public void UpdateLastModified(DateTime utcNow)
{
    LastModified = utcNow;
    MarkUpdated();
}
```

**Caller updates required** (all current callers will fail to compile after this — that's intentional):
- `ContentSeo.Infrastructure/EventHandlers/PlaceCreatedIntegrationEventHandler.cs` — pass `evt.UpdatedAt` (or `dateTimeProvider.UtcNow` if event doesn't carry it)
- `ContentSeo.Infrastructure/EventHandlers/PlaceUpdatedIntegrationEventHandler.cs` — call `existingEntry.UpdateLastModified(evt.UpdatedAt)`
- `ContentSeo.Infrastructure/EventHandlers/BusinessCreatedIntegrationEventHandler.cs` — same as Place
- All new Phase-3 B12 handlers — pass `evt.UpdatedAt`

**Tests:** XML rendered with `<lastmod>` element matching `entity.UpdatedAt`.

---

### W6. Cron-align sitemap cadence

**File:** `ContentSeo.Infrastructure/BackgroundServices/SitemapRegenerationService.cs`

**Current:** `await Task.Delay(TimeSpan.FromHours(6), ct);` — drifts from process start time.

**Change:** Compute delay to the next `00:00 / 06:00 / 12:00 / 18:00 UTC` slot.

```csharp
private static TimeSpan DelayUntilNextSlot(DateTime utcNow)
{
    var nextSlotHour = ((utcNow.Hour / 6) + 1) * 6;
    var nextSlot = utcNow.Date.AddHours(nextSlotHour);
    if (nextSlot <= utcNow) nextSlot = nextSlot.AddHours(6);
    return nextSlot - utcNow;
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Run once immediately on startup
    await RegenerateOnceAsync(stoppingToken);

    while (!stoppingToken.IsCancellationRequested)
    {
        var delay = DelayUntilNextSlot(dateTimeProvider.UtcNow);
        await Task.Delay(delay, stoppingToken);
        await RegenerateOnceAsync(stoppingToken);
    }
}
```

**Tests:** unit-test `DelayUntilNextSlot(...)` with mock times (e.g. 13:45 UTC → 6h 15min until 18:00 UTC).

---

## Phase 4 — PDF §11 Weather Compliance

### B4. `WeatherCache` keyed by coordinates (not `PlaceId`)

**Current (per `WeatherCache.cs` L9-L37):** `Id = Guid.CreateVersion7()` surrogate PK; `PlaceId` field; one row per Place.

**Target:** lookup key = `(RoundedLatitude(2dp), RoundedLongitude(2dp), Date)`. Keep `Id` as surrogate PK to minimize migration pain; add the 3 fields + a unique index.

**Files:**

1. `ContentSeo.Domain/Entities/WeatherCache.cs`:
   - Add fields:
     ```csharp
     public decimal RoundedLatitude { get; private set; }   // 2dp
     public decimal RoundedLongitude { get; private set; }  // 2dp
     public DateOnly Date { get; private set; }             // forecast target date
     ```
   - Remove `PlaceId` field (or mark obsolete and keep for one release for back-compat)
   - Update `Create()` factory:
     ```csharp
     public static WeatherCache Create(
         decimal latitude,
         decimal longitude,
         DateOnly date,
         /* ... existing weather fields ... */
         DateTime fetchedAt,
         DateTime expiresAt)
     {
         var roundedLat = Math.Round(latitude, 2, MidpointRounding.AwayFromZero);
         var roundedLng = Math.Round(longitude, 2, MidpointRounding.AwayFromZero);
         // validation ...
         return new WeatherCache { /* ... */ };
     }
     ```

2. `ContentSeo.Infrastructure/Persistence/Configurations/WeatherCacheConfiguration.cs`:
   ```csharp
   builder.HasIndex(w => new { w.RoundedLatitude, w.RoundedLongitude, w.Date })
          .IsUnique()
          .HasDatabaseName("UX_WeatherCache_Lat_Lng_Date");
   builder.Property(w => w.RoundedLatitude).HasColumnType("decimal(5,2)");
   builder.Property(w => w.RoundedLongitude).HasColumnType("decimal(6,2)");
   builder.Property(w => w.Date).HasColumnType("date");
   ```

3. `ContentSeo.Domain/Repositories/IWeatherCacheRepository.cs`:
   ```csharp
   Task<WeatherCache?> GetByLocationAsync(
       decimal latitude, decimal longitude, DateOnly date, CancellationToken ct);
   ```

4. `ContentSeo.Infrastructure/Persistence/Repositories/WeatherCacheRepository.cs`:
   ```csharp
   public Task<WeatherCache?> GetByLocationAsync(
       decimal latitude, decimal longitude, DateOnly date, CancellationToken ct)
   {
       var lat = Math.Round(latitude, 2, MidpointRounding.AwayFromZero);
       var lng = Math.Round(longitude, 2, MidpointRounding.AwayFromZero);
       return _db.WeatherCaches
           .Where(w => !w.IsDeleted
                    && w.RoundedLatitude == lat
                    && w.RoundedLongitude == lng
                    && w.Date == date)
           .FirstOrDefaultAsync(ct);
   }
   ```

5. `ContentSeo.Presentation/Endpoints/Weather/WeatherEndpoints.cs`:
   - Add `GET /api/v1/weather?lat={lat}&lng={lng}` (new)
   - Keep `GET /api/v1/weather/{placeId}` for back-compat — internally resolves `Place` → lat/lng then delegates to the new lookup

6. **Migration:** `AlignWeatherCacheKeyToCoordinates`:
   ```pwsh
   dotnet ef migrations add AlignWeatherCacheKeyToCoordinates --project ContentSeo.Infrastructure --startup-project YallaJo.Web
   ```
   In the migration:
   ```csharp
   migrationBuilder.AddColumn<decimal>("RoundedLatitude",  "WeatherCaches", type: "decimal(5,2)", nullable: false, defaultValue: 0m);
   migrationBuilder.AddColumn<decimal>("RoundedLongitude", "WeatherCaches", type: "decimal(6,2)", nullable: false, defaultValue: 0m);
   migrationBuilder.AddColumn<DateTime>("Date",            "WeatherCaches", type: "date",        nullable: false, defaultValue: new DateTime(2000,1,1));
   migrationBuilder.Sql("DELETE FROM WeatherCaches");  // cache → safe to flush
   migrationBuilder.CreateIndex("UX_WeatherCache_Lat_Lng_Date",
       "WeatherCaches", new[] { "RoundedLatitude", "RoundedLongitude", "Date" }, unique: true);
   migrationBuilder.DropColumn("PlaceId", "WeatherCaches");
   ```

**Tests:**
- Tour A (lat 31.951, lng 35.933) and Tour B (lat 31.957, lng 35.929) → both rounded to (31.95, 35.93) → share one cache row
- Same coords different dates → 2 rows
- Date-rounding test boundary cases

---

### B5. `IWeatherProvider` contract → 7-day forecast

**Files:**
- `ContentSeo.Application/Interfaces/IWeatherProvider.cs` (new typed interface) or `ContentSeo.Infrastructure/Weather/IWeatherProvider.cs`
- `ContentSeo.Infrastructure/Weather/NoOpWeatherProvider.cs`
- `ContentSeo.Domain/Entities/WeatherCache.cs` — adjust forecast serialization

```csharp
public interface IWeatherProvider
{
    Task<WeatherForecast> GetForecastAsync(
        decimal latitude, decimal longitude, CancellationToken ct);
}

public sealed record WeatherForecast(IReadOnlyList<DailyForecast> Days)
{
    public const int RequiredDays = 7;

    public bool IsValid => Days.Count == RequiredDays
                        && Days.Select(d => d.Date).Distinct().Count() == RequiredDays;
}

public sealed record DailyForecast(
    DateOnly Date,
    decimal TemperatureCelsius,
    int HumidityPercent,
    string ConditionText,
    string IconCode);
```

**Validation in `WeatherPreFetchService.cs` and `RefreshWeatherCommandHandler`:**
```csharp
var forecast = await provider.GetForecastAsync(lat, lng, ct);
if (!forecast.IsValid)
    throw new InvalidOperationException(
        $"Weather provider returned {forecast.Days.Count} days, expected {WeatherForecast.RequiredDays}.");
```

**`NoOpWeatherProvider`:** return 7 days of fixed dummy data so tests don't break.

**Tests:**
- `NoOpWeatherProvider_Returns7Days`
- `WeatherForecast_IsValid_Only_When_Exactly7DistinctDays`

---

### B6. Persist daily 1000-call budget counter

**New entity:** `ContentSeo.Domain/Entities/WeatherDailyBudget.cs`:
```csharp
public sealed class WeatherDailyBudget : AuditableEntity, IAggregateRoot
{
    private WeatherDailyBudget() { }

    public DateOnly Date { get; private set; }
    public int CallsUsed { get; private set; }
    public int DailyLimit { get; private set; }
    public DateTime? AlertSentAt { get; private set; }   // for W3 once-per-day alert

    public static WeatherDailyBudget CreateForToday(DateOnly today, int dailyLimit)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Date = today,
            CallsUsed = 0,
            DailyLimit = dailyLimit,
            AlertSentAt = null,
        };

    public bool TryConsume(int n = 1)
    {
        if (CallsUsed + n > DailyLimit) return false;
        CallsUsed += n;
        MarkUpdated();
        return true;
    }

    public void MarkAlertSent(DateTime utcNow)
    {
        if (AlertSentAt is null) { AlertSentAt = utcNow; MarkUpdated(); }
    }

    public int Remaining => Math.Max(0, DailyLimit - CallsUsed);
    public bool IsExhausted => CallsUsed >= DailyLimit;
}
```

**Configuration:** Unique on `Date`.

**Migration:** `AddWeatherDailyBudgetTable`.

**New service `IWeatherBudgetGate` + `WeatherBudgetGate` impl:**
```csharp
public interface IWeatherBudgetGate
{
    Task<bool> TryReserveAsync(CancellationToken ct);
    Task<int> GetRemainingAsync(CancellationToken ct);
}
```
Implementation uses **`SELECT ... FOR UPDATE` / `UPDLOCK` pattern** to handle concurrent BG instances, or SQL Server's `OUTPUT` clause for atomic increment.

**Wire-in:**
- `WeatherPreFetchService` — loop `while (await _gate.TryReserveAsync(ct)) { ... }`
- `RefreshWeatherCommandHandler` — guard before provider call

**Tests:**
- Concurrent reservation race → exactly 1000 succeed
- Budget exhausted → next call returns false; no provider invocation

---

### W2. Weather DTO — `FetchedAt` + `StaleSince`

**File:** `ContentSeo.Application/Queries/Weather/GetWeather/WeatherForecastDto.cs` (or wherever the response shape lives)

**Change:** Add fields:
```csharp
public sealed record WeatherForecastDto(
    /* existing fields ... */
    DateTime FetchedAt,
    DateTime? StaleSince,   // null when fresh; set to ExpiresAt when serving stale-fallback
    string? StaleHumanLabel // optional "Last updated 3 hours ago" for clients that don't want to compute
);
```

`StaleHumanLabel` computed:
```csharp
StaleHumanLabel = isStale
    ? $"Last updated {(int)Math.Floor((dateTimeProvider.UtcNow - cache.FetchedAt).TotalHours)} hour(s) ago"
    : null;
```

**Tests:** stale-cache fallback path sets both `StaleSince` and `StaleHumanLabel`.

---

### W3. `WeatherBudgetExhaustedIntegrationEvent`

**New file:** `ContentSeo.Contracts/IntegrationEvents/WeatherBudgetExhaustedIntegrationEvent.cs`:
```csharp
public sealed record WeatherBudgetExhaustedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    DateOnly BudgetDate,
    int CallsUsed,
    int DailyLimit) : IIntegrationEvent;
```

**Register in `IntegrationEventTypeRegistry.cs`** (extending Phase 1 B9 block):
```csharp
["content-seo.weather.budget-exhausted.v1"] = typeof(WeatherBudgetExhaustedIntegrationEvent),
```

**Publish:** in `WeatherBudgetGate.TryReserveAsync(...)` when the first call of the day fails (use the `AlertSentAt` field on `WeatherDailyBudget` for once-per-day guarantee):
```csharp
if (budget.IsExhausted)
{
    if (budget.AlertSentAt is null)
    {
        await _outbox.AddAsync(new WeatherBudgetExhaustedIntegrationEvent(
            Guid.CreateVersion7(), dateTimeProvider.UtcNow,
            budget.Date, budget.CallsUsed, budget.DailyLimit), ct);
        budget.MarkAlertSent(dateTimeProvider.UtcNow);
        await uow.SaveChangesAsync(ct);
    }
    return false;
}
```

**Consumer (out of scope this phase):** notification module subscribes and pushes admin alert (Slack/email).

---

## Phase 5 — Code Hygiene

### D1. `ContentBlogFeatures` (singular) → `ContentBlogsFeatures` (plural)

**Decision:** Match ContentSeo precedent → use plural.

**Use Serena `rename_symbol`** for safe workspace-wide rename:

```
serena_rename_symbol(
    name_path="ContentBlogFeatures",
    relative_path="ContentBlogs.Contracts/Authorization/ContentBlogFeatures.cs",
    new_name="ContentBlogsFeatures")
```

Repeat for:
- `ContentBlogPermissionCatalog` → `ContentBlogsPermissionCatalog`
- File renames:
  - `ContentBlogs.Contracts/Authorization/ContentBlogFeatures.cs` → `ContentBlogsFeatures.cs`
  - `ContentBlogs.Contracts/Authorization/ContentBlogPermissionCatalog.cs` → `ContentBlogsPermissionCatalog.cs`

**Verification:** `dotnet build` green; all `BlogEndpoints.cs` references updated automatically.

---

### D2. Narrow generic `catch (Exception)` in `BlogCreatedDomainEventHandler`

**File:** `ContentBlogs.Infrastructure/EventHandlers/BlogCreatedDomainEventHandler.cs`

**Steps:**
1. Read the file
2. Identify the `try { ... } catch (Exception ex) { ... }` block
3. Determine what call is the actual external boundary (likely `IEntityTranslationOrchestrator.QueueAsync(...)` or similar)
4. Narrow to the specific exception type or a specific exception's base (e.g. `HttpRequestException`, `DbUpdateConcurrencyException`, or an orchestrator-specific exception)
5. If the catch wraps purely internal logic, **remove the try/catch entirely** — let the exception bubble to the outbox dispatcher's retry policy

**agent-context.md §5.5 reference:** generic catch is permitted only for:
- Infrastructure external calls (HTTP / SMTP / Redis / external service)
- `DbUpdateConcurrencyException`
- `BackgroundService` per-item processing loops

**Verification:** code review + `dotnet build` + handler runs successfully in integration test (provoke success and failure paths).

---

### D3. Delete empty `Application/EventHandlers/` folders

**Files (each contains only a `.gitkeep`):**
- `ContentBlogs.Application/EventHandlers/`
- `ContentSeo.Application/EventHandlers/`

**Confirm with team:** Infrastructure-layered event handling is the intended convention (confirmed by code reality — all 13 ContentBlogs handlers + all 10 ContentSeo handlers live in `Infrastructure/EventHandlers/`).

**If confirmed:**
```pwsh
Remove-Item -LiteralPath "ContentBlogs.Application\EventHandlers" -Recurse
Remove-Item -LiteralPath "ContentSeo.Application\EventHandlers" -Recurse
```

Update any `.csproj` `Compile Include="EventHandlers\**\*.cs"` lines if present.

---

## Cross-Phase Verification Gates

Run all of these after EACH phase before opening the PR:

1. **Build green:** `dotnet build YallaJo.sln`
   - If `YallaJo.Web.exe` is locked, build individual changed projects per agent-context.md workaround
2. **Tests green:** `dotnet test`
3. **Migrations applied to dev DB:** `dotnet ef database update --project ContentSeo.Infrastructure --startup-project YallaJo.Web`
4. **Manual smoke per phase:**
   - Phase 1: Create a FAQ via Swagger → check `OutboxMessages` table for `content-seo.faq.changed.v1` row
   - Phase 2: Try to publish a blog with only EN → expect 422 + `Blog.Translation.ArabicRequired`; add AR translation, retry → 200
   - Phase 3: Create a redirect → hit the `OldUrl` → expect 301 with correct `Location`
   - Phase 4: Hit `/api/v1/weather?lat=31.95&lng=35.93` twice → second hit served from cache (sub-50ms response)
   - Phase 5: build remains green; no `ContentBlogFeatures` references survive grep
5. **Outbox round-trip check:** subscribe a test handler in the same process and verify the message is dispatched within the outbox poll interval.

---

## Per-Phase File-Change Manifest (for PR descriptions)

### Phase 1 — Runtime Failures
- `M YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` (+ ContentSeo block, +5 LOC)
- `M|D ContentSeo.Infrastructure/EventHandlers/SeoMetadataDeletedDomainEventHandler.cs` (Option A: delete)
- `M|D ContentSeo.Domain/Events/SeoMetadataDeletedDomainEvent.cs` (Option A: delete)
- `A IntegrationEventTypeRegistryTests.AllContentSeoEvents_AreRegistered`

### Phase 2 — Blog Compliance
- `M ContentBlogs.Domain/Entities/Blog.cs` (Publish: AR+EN gate)
- `M ContentBlogs.Application/Commands/Blog/PublishBlog/PublishBlogCommandHandler.cs` (load existing langs, remove image gate)
- `M ContentBlogs.Application/Commands/Blog/PublishBlog/PublishBlogCommandValidator.cs` (remove image rule if present)
- `M ContentBlogs.Domain/Entities/BlogComment.cs` (AddOrReplaceReaction toggle)
- `M ContentBlogs.Domain/Events/BlogCommentReactionChangedDomainEvent.cs` (+ `IsRemoved` flag)
- `M ContentBlogs.Application/Commands/BlogComment/AddOrReplaceBlogCommentReaction/AddOrReplaceBlogCommentReactionCommandHandler.cs`
- `A ContentBlogs.Infrastructure/Options/BlogPublishingOptions.cs`
- `M ContentBlogs.Infrastructure/DependencyInjection.cs` (Configure options)
- `M ContentBlogs.Infrastructure/EventHandlers/BlogPublishedDomainEventHandler.cs` (OG image fallback)
- `A ContentBlogs.Infrastructure/EventHandlers/TourDeletedIntegrationEventHandler.cs`
- `A ContentBlogs.Infrastructure/EventHandlers/BlogCommentCreatedDomainEventHandler.cs`
- `A ContentBlogs.Infrastructure/EventHandlers/BlogCommentUpdatedDomainEventHandler.cs`
- `A ContentBlogs.Infrastructure/EventHandlers/BlogCommentDeletedDomainEventHandler.cs`
- `A ContentBlogs.Infrastructure/EventHandlers/BlogCommentReactionChangedDomainEventHandler.cs`
- `M? ContentBlogs.Domain/Entities/Blog.cs` (+ `CommentCount` if adopting)
- `A? ContentBlogs.Infrastructure/Migrations/{ts}_AddCommentCountToBlog.cs`

### Phase 3 — SEO Compliance
- `A YallaJo.Web/Middleware/SeoRedirectMiddleware.cs`
- `M YallaJo.Web/Program.cs` (UseMiddleware)
- `A ContentSeo.Application/Services/IRedirectLookupService.cs`
- `A ContentSeo.Infrastructure/Services/RedirectLookupService.cs`
- `A ContentSeo.Application/Services/IRedirectHitCounter.cs`
- `A ContentSeo.Infrastructure/Services/RedirectHitCounter.cs`
- `A ContentSeo.Infrastructure/BackgroundServices/RedirectHitCounterFlushService.cs`
- `M ContentSeo.Infrastructure/Sitemap/SitemapRenderer.cs` (sharding + hreflang)
- `M ContentSeo.Presentation/Endpoints/Sitemap/SitemapEndpoints.cs` (sub-sitemap routes)
- `A` 9 new handlers in `ContentSeo.Infrastructure/EventHandlers/` (Blog* + Tour*)
- `M ContentSeo.Domain/Entities/SitemapEntry.cs` (`LastModified` parameter + `UpdateLastModified`)
- `M` 3 existing handlers in `ContentSeo.Infrastructure/EventHandlers/` (pass `LastModified`)
- `M ContentSeo.Infrastructure/BackgroundServices/SitemapRegenerationService.cs` (cron-align)

### Phase 4 — Weather Compliance
- `M ContentSeo.Domain/Entities/WeatherCache.cs` (composite key fields)
- `M ContentSeo.Infrastructure/Persistence/Configurations/WeatherCacheConfiguration.cs`
- `M ContentSeo.Domain/Repositories/IWeatherCacheRepository.cs` (GetByLocationAsync)
- `M ContentSeo.Infrastructure/Persistence/Repositories/WeatherCacheRepository.cs`
- `M ContentSeo.Presentation/Endpoints/Weather/WeatherEndpoints.cs` (lat/lng endpoint)
- `A ContentSeo.Infrastructure/Migrations/{ts}_AlignWeatherCacheKeyToCoordinates.cs`
- `M ContentSeo.Application/Interfaces/IWeatherProvider.cs` (7-day contract)
- `M ContentSeo.Infrastructure/Weather/NoOpWeatherProvider.cs`
- `A ContentSeo.Domain/Entities/WeatherDailyBudget.cs`
- `A ContentSeo.Infrastructure/Persistence/Configurations/WeatherDailyBudgetConfiguration.cs`
- `A ContentSeo.Infrastructure/Migrations/{ts}_AddWeatherDailyBudgetTable.cs`
- `A ContentSeo.Application/Services/IWeatherBudgetGate.cs`
- `A ContentSeo.Infrastructure/Services/WeatherBudgetGate.cs`
- `M ContentSeo.Infrastructure/BackgroundServices/WeatherPreFetchService.cs` (gate guard)
- `M ContentSeo.Application/Commands/Weather/RefreshWeather/RefreshWeatherCommandHandler.cs` (gate guard)
- `M ContentSeo.Application/Queries/Weather/GetWeather/WeatherForecastDto.cs` (FetchedAt/StaleSince)
- `A ContentSeo.Contracts/IntegrationEvents/WeatherBudgetExhaustedIntegrationEvent.cs`
- `M YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` (+1 entry)

### Phase 5 — Hygiene
- `M` All files referencing `ContentBlogFeatures` (via Serena rename)
- `R ContentBlogFeatures.cs` → `ContentBlogsFeatures.cs`
- `R ContentBlogPermissionCatalog.cs` → `ContentBlogsPermissionCatalog.cs`
- `M ContentBlogs.Infrastructure/EventHandlers/BlogCreatedDomainEventHandler.cs` (narrow catch)
- `D ContentBlogs.Application/EventHandlers/` (folder)
- `D ContentSeo.Application/EventHandlers/` (folder)

---

## Rollback Strategy

Each phase = one PR. If a phase regresses production:

- **Phase 1 rollback:** Remove the 4 new registry entries. Zero schema impact.
- **Phase 2 rollback:** Revert the PR. The toggle behavior change has no schema impact; the optional `CommentCount` migration would need its `Down()` method to drop the column.
- **Phase 3 rollback:** Revert the PR. Middleware registration removal is a one-line change. New handlers can be removed; sitemap sharding code can be reverted to the old `Sitemap.SizeOverflow` path.
- **Phase 4 rollback:** Most invasive due to `WeatherCache` schema change. Pre-cut a DB backup. Migration's `Down()` should restore the `PlaceId` column and drop the new ones (will lose cached data, which is acceptable).
- **Phase 5 rollback:** Trivial — revert the rename.

---

## Delegation Mapping (for `task()` calls after approval)

When approval to execute lands, delegate each phase to an executor subagent. Suggested mapping:

| Phase | Subagent | Skills |
|---|---|---|
| 1 | `Sisyphus-Junior` (category=`quick`) | — |
| 2 | `Sisyphus-Junior` (category=`deep`) | — |
| 3 | `Sisyphus-Junior` (category=`deep`) | — |
| 4 | `Sisyphus-Junior` (category=`deep`) | — |
| 5 | `Sisyphus-Junior` (category=`quick`) | — |

Each delegated task prompt should include:
1. Pointer to this fix plan (`.sisyphus/ContentBlogs-ContentSeo-fix-plan.md`)
2. The phase section to execute (verbatim file references)
3. The verification gate for that phase
4. Reminder of the canonical conventions in `agent-context.md`

---

*Plan generated from gaps inventory in `Agents/tasks/ContentBlogs-ContentSeo-gaps.md` and grounded in file reads of `IntegrationEventTypeRegistry.cs`, `PlaceCreatedIntegrationEventHandler.cs`, and `WeatherCache.cs`.*
