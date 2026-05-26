# ContentSeo Workflow Plan

> Module: ContentSeo (SEO, FAQ, Redirects, Sitemaps, Weather)  
> Status: Implemented (audited 2025-01-27, score 8.8/10). 5 dead permissions removed (W4-B).  
> Compatible with: All 10 existing plans  
> Source of truth: agent-context.md, Business Rules PDF, YallaJo.md, Endpoints.pdf

---

## Design Decisions (9 locked)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **SeoEntityType expansion** | Add `TourGuide=4`, `Creator=5`. Auto-create sitemap entries when guide/creator profiles become public. |
| 2 | **Add missing endpoints** | Wire all 7 dead permissions to real endpoints (Redirect Update, Sitemap Read/Delete/Priority, Weather Update/Delete). |
| 3 | **Schema.org AggregateRating** | Auto-generate JSON-LD review aggregation from Social module events. Stored in existing `SeoMetadata.SchemaMarkup` JSON field (merged). |
| 4 | **WeatherAPI.com** | Full implementation — `WeatherApiComProvider` class, DTO mapping, error handling, rate limiting. Replaces `NoOpWeatherProvider`. |
| 5 | **FAQ admin-only** | Keep current model. Only admins manage FAQ items. No provider-contributed FAQs. |
| 6 | **Redirect full update** | `PUT /redirects/{id}` changes target URL, status code (301/302), and active state. Chain flattening re-runs if target changes. |
| 7 | **Sitemap admin: Read + Delete + Priority** | `GET /sitemap/entries` (paginated), `DELETE /sitemap/entries/{id}`, `PATCH /sitemap/entries/{id}` (priority + changeFrequency). No manual creation — always auto-managed by integration events. |
| 8 | **Rating in SchemaMarkup** | AggregateRating data merged into existing `SeoMetadata.SchemaMarkup` string field. No new columns. |
| 9 | **Weather full implementation** | Plan includes complete `WeatherApiComProvider`, DTO mapping, HTTP client, error handling, config. Ready to use with API key. |

---

## Current State Summary

**Module**: ~153 files (7 entities, 1 enum, 17 endpoints, 13 handlers, 23 event handlers, 2 background services)  
**Score**: 9.0/10 — zero ICurrentUser usage, zero auth gate violations, correct Result pattern everywhere  
**Entities**: FaqItem, FaqItemTranslation, Redirect, SeoMetadata, SitemapEntry, WeatherCache, WeatherDailyBudget  
**SeoEntityType**: Place=0, Tour=1, Business=2, Blog=3  
**Endpoints**: 17 (5 FaqItem + 3 Redirect + 3 SeoMetadata + 3 Sitemap + 3 Weather)  
**Integration event handlers**: 14 (Blog×5, Place×3, Tour×4, Business×1, Language×1)

### Existing Gaps (from Audit)

| # | Gap | Severity | Status |
|---|-----|----------|--------|
| 1 | 7 dead permissions in catalog | MEDIUM | → Adding endpoints (Decision #2) |
| 2 | MaxHops 10→3 | LOW | Fix in Phase 1 |
| 3 | NoOpWeatherProvider throw | LOW | Replaced by real provider (Decision #4) |
| 4 | 1 test file | LOW | Deferred (not in plan scope) |
| 5 | CreateRedirect bare `throw;` in catch | LOW | Fix in Phase 1 |

---

## Cross-Plan Compatibility

| Plan | Impact on ContentSeo |
|------|---------------------|
| **TourGuide-Flow.md** | New `SeoEntityType.TourGuide=4`. Sitemap entries on guide profile activation. SEO metadata for guide pages. |
| **BlogCreatorPost-Merger.md** | Existing Blog event handlers unaffected (Blog entity stays, CreatorPost removed). New `SeoEntityType.Creator=5` for creator profiles. |
| **Social-Workflow.md** | New integration event: `ReviewAggregateUpdatedIntegrationEvent` → auto-update Schema.org AggregateRating in SeoMetadata. |
| **Booking-Workflow.md** | No direct impact. Bookable entities already in sitemap. |
| **Finance-Workflow.md** | No impact. |
| **Messaging-Workflow.md** | No impact. |
| **Analytics-Workflow.md** | No impact (internal analytics, not crawlable). |
| **ContentPlaces-Workflow.md** | Business integration handler already exists. No changes needed. |
| **Platform-Onboarding-Workflow.md** | Provider approval → TourGuide profile creation → triggers sitemap entry (via TourGuide event). |
| **Role-System.md** | No impact. |

---

## Workflow A — Sitemap Lifecycle for New Entity Types

### A1. TourGuide Profile → Sitemap

```
TourGuide profile activated (Status=Active, IsActive=true)
  ↓
TourGuideActivatedIntegrationEvent (from ContentTours)
  ↓
ContentSeo: TourGuideActivatedIntegrationEventHandler
  → SitemapEntry.Create(url="/guides/{slug}", entityType="TourGuide", entityId=guideId)
  → SeoMetadata.Create(SeoEntityType.TourGuide, guideId, metaTitle="{DisplayName} | Tour Guide", ...)
  ↓
TourGuide profile deactivated/suspended
  ↓
TourGuideDeactivatedIntegrationEvent
  ↓
ContentSeo: TourGuideDeactivatedIntegrationEventHandler
  → sitemapEntry.Deactivate()
```

### A2. Creator Profile → Sitemap

```
CreatorProfile status becomes Active
  ↓
CreatorProfileActivatedIntegrationEvent (from ContentBlogs)
  ↓
ContentSeo: CreatorProfileActivatedIntegrationEventHandler
  → SitemapEntry.Create(url="/creators/{slug}", entityType="Creator", entityId=profileId)
  → SeoMetadata.Create(SeoEntityType.Creator, profileId, metaTitle="{DisplayName} | Creator", ...)
  ↓
CreatorProfile suspended/deactivated
  ↓
CreatorProfileDeactivatedIntegrationEvent
  ↓
ContentSeo: CreatorProfileDeactivatedIntegrationEventHandler
  → sitemapEntry.Deactivate()
```

### A3. TourGuide/Creator Profile Updated

```
TourGuideProfileUpdatedIntegrationEvent / CreatorProfileUpdatedIntegrationEvent
  ↓
ContentSeo: handler finds SitemapEntry by entityType+entityId
  → entry.UpdateLastModified(updatedAt)
  → If slug changed: entry.ChangeUrl(newUrl)
```

---

## Workflow B — Schema.org AggregateRating

```
User submits/updates/deletes a review (Social module)
  ↓
Social recalculates EntityRatingCache for target entity
  ↓
ReviewAggregateUpdatedIntegrationEvent {
    EntityType (Tour/Place/Business/TourGuide),
    EntityId,
    AverageRating,
    ReviewCount,
    BestRating = 5,
    WorstRating = 1
}
  ↓
ContentSeo: ReviewAggregateUpdatedIntegrationEventHandler
  → Map Social EntityType → SeoEntityType
  → Lookup SeoMetadata by (entityType, entityId)
  → If not found: create SeoMetadata with schema only
  → Merge AggregateRating JSON-LD into SchemaMarkup:
    {
      "@context": "https://schema.org",
      "@type": "AggregateRating",
      "ratingValue": 4.7,
      "reviewCount": 142,
      "bestRating": 5,
      "worstRating": 1
    }
  → seoMetadata.UpdateSchema(mergedJson)
```

### SchemaMarkup Merge Strategy
- If SchemaMarkup is null/empty → set to AggregateRating JSON-LD
- If SchemaMarkup exists → parse existing JSON, add/replace `aggregateRating` key, serialize back
- Preserves any existing schema (e.g., LocalBusiness, TouristAttraction) while adding rating data
- Helper: `SchemaMarkupMerger.MergeAggregateRating(existing, rating)` utility class

---

## Workflow C — Redirect Full Update

```
Admin PUTs /redirects/{id}
  ↓
UpdateRedirectCommand { Id, NewUrl?, StatusCode?, IsActive? }
  ↓
UpdateRedirectCommandHandler:
  1. Lookup redirect by Id
  2. If NewUrl changed:
     a. Run chain detection (same as CreateRedirect — detect cycles, flatten)
     b. Call redirect.RewriteTo(newUrl, hops) if chain found, or update directly
  3. If StatusCode changed: update (301/302 only)
  4. If IsActive changed: call Activate() or Deactivate()
  5. Save + evict cache
```

### Domain Changes
- Add `Update(string? newUrl, int? statusCode)` method on `Redirect` entity
- Validates StatusCode ∈ {301, 302}
- Fires `RedirectUpdatedDomainEvent`

---

## Workflow D — Sitemap Admin Management

### D1. List Sitemap Entries
```
GET /api/v1/seo/sitemap/entries?entityType=Tour&isActive=true&page=1&pageSize=50
  ↓
ListSitemapEntriesQuery (paginated, filterable by entityType, isActive)
  ↓
Returns: PaginatedResult<SitemapEntryDto>
  { Id, Url, EntityType, EntityId, Priority, ChangeFrequency, LastModified, IsActive }
```

### D2. Delete Sitemap Entry
```
DELETE /api/v1/seo/sitemap/entries/{id}
  ↓
DeleteSitemapEntryCommand { Id }
  ↓
Hard-delete (sitemap entries are infrastructure, not business data)
```

### D3. Update Sitemap Entry Priority
```
PATCH /api/v1/seo/sitemap/entries/{id}
  ↓
UpdateSitemapEntryCommand { Id, Priority?, ChangeFrequency? }
  ↓
Handler updates fields, calls entry.Touch() to refresh LastModified
```

---

## Workflow E — Weather Admin & WeatherAPI.com

### E1. Weather Purge
```
DELETE /api/v1/seo/weather/cache/{id}
  ↓
Purges specific weather cache entry (admin cleanup of stale data)
```

### E2. Weather Budget Reset
```
PUT /api/v1/seo/weather/budget/reset
  ↓
Resets daily budget counter (emergency — when budget exhausted but API key upgraded)
```

### E3. WeatherAPI.com Provider

**Class**: `WeatherApiComProvider : IWeatherProvider`

```
Configuration (WeatherOptions):
  - ApiKey (required)
  - BaseUrl = "https://api.weatherapi.com/v1"
  - ForecastDays = 7
  - IsAvailable = true (when ApiKey configured)

Flow:
  GET /v1/forecast.json?key={key}&q={lat},{lng}&days=7&aqi=no
    ↓
  Map response → WeatherSnapshot:
    - Temperature (current.temp_c)
    - Humidity (current.humidity)
    - WindSpeed (current.wind_kph)
    - Condition (current.condition.text)
    - DailyForecasts[7] (forecast.forecastday[])
      - Date, MaxTemp, MinTemp, Condition, ChanceOfRain, Sunrise, Sunset
    ↓
  WeatherCache.Create(lat, lng, snapshot, expiresAt=now+12h)

Error Handling:
  - 401 → log error, return unavailable
  - 429 → log rate limit, return unavailable
  - 5xx → log, retry once, return unavailable
  - Network timeout (10s) → return unavailable
```

---

## Execution Phases

### Phase 1 — Gap Fixes (LOW effort, ~1h)

| # | Fix | File(s) |
|---|-----|---------|
| 1 | MaxHops 10→3 | `CreateRedirectCommandHandler.cs` |
| 2 | Bare `throw;` → Result.Failure | `CreateRedirectCommandHandler.cs` |
| 3 | NoOpWeatherProvider throw → log + return empty | `NoOpWeatherProvider.cs` (still used as fallback) |

### Phase 2 — SeoEntityType Expansion (~2h)

| # | Change | File(s) |
|---|--------|---------|
| 1 | Add `TourGuide=4, Creator=5` to enum | `SeoEntityType.cs` |
| 2 | TourGuideActivated handler | New: `TourGuideActivatedIntegrationEventHandler.cs` |
| 3 | TourGuideDeactivated handler | New: `TourGuideDeactivatedIntegrationEventHandler.cs` |
| 4 | TourGuideUpdated handler | New: `TourGuideProfileUpdatedIntegrationEventHandler.cs` |
| 5 | CreatorProfileActivated handler | New: `CreatorProfileActivatedIntegrationEventHandler.cs` |
| 6 | CreatorProfileDeactivated handler | New: `CreatorProfileDeactivatedIntegrationEventHandler.cs` |
| 7 | CreatorProfileUpdated handler | New: `CreatorProfileUpdatedIntegrationEventHandler.cs` |

**Note**: Integration events must be defined in source modules' Contracts:
- `ContentTours.Contracts`: `TourGuideActivatedIntegrationEvent`, `TourGuideDeactivatedIntegrationEvent`, `TourGuideProfileUpdatedIntegrationEvent`
- `ContentBlogs.Contracts`: `CreatorProfileActivatedIntegrationEvent`, `CreatorProfileDeactivatedIntegrationEvent`, `CreatorProfileUpdatedIntegrationEvent`

### Phase 3 — New Endpoints for Dead Permissions (~4h)

**Redirect Update (1 endpoint)**:
| # | File | Type |
|---|------|------|
| 1 | `Redirect.cs` | Domain: Add `Update(string? newUrl, int? statusCode)` method |
| 2 | `RedirectUpdatedDomainEvent.cs` | New domain event |
| 3 | `UpdateRedirectCommand.cs` | New command |
| 4 | `UpdateRedirectCommandHandler.cs` | New handler (chain detection on target change) |
| 5 | `UpdateRedirectCommandValidator.cs` | New validator |
| 6 | `UpdateRedirectRequest.cs` | New request DTO |
| 7 | `RedirectEndpoints.cs` | Add `PUT /redirects/{id}` endpoint |
| 8 | `RedirectUpdatedDomainEventHandler.cs` | New event handler (update sitemap/cache) |

**Sitemap Admin (3 endpoints)**:
| # | File | Type |
|---|------|------|
| 9 | `ListSitemapEntriesQuery.cs` | New query |
| 10 | `ListSitemapEntriesQueryHandler.cs` | New handler (paginated, filterable) |
| 11 | `SitemapEntryDto.cs` | New DTO |
| 12 | `DeleteSitemapEntryCommand.cs` | New command |
| 13 | `DeleteSitemapEntryCommandHandler.cs` | New handler |
| 14 | `UpdateSitemapEntryCommand.cs` | New command |
| 15 | `UpdateSitemapEntryCommandHandler.cs` | New handler |
| 16 | `UpdateSitemapEntryRequest.cs` | New request DTO |
| 17 | `SitemapEndpoints.cs` | Add 3 endpoints (GET list, DELETE, PATCH) |

**Weather Admin (2 endpoints)**:
| # | File | Type |
|---|------|------|
| 18 | `PurgeWeatherCacheCommand.cs` | New command |
| 19 | `PurgeWeatherCacheCommandHandler.cs` | New handler |
| 20 | `ResetWeatherBudgetCommand.cs` | New command |
| 21 | `ResetWeatherBudgetCommandHandler.cs` | New handler |
| 22 | `WeatherEndpoints.cs` | Add 2 endpoints (DELETE cache, PUT budget/reset) |

### Phase 4 — Schema.org AggregateRating (~3h)

| # | File | Type |
|---|------|------|
| 1 | `SchemaMarkupMerger.cs` | New utility (Application layer) |
| 2 | `ReviewAggregateUpdatedIntegrationEvent.cs` | New in `Social.Contracts` |
| 3 | `ReviewAggregateUpdatedIntegrationEventHandler.cs` | New handler in ContentSeo.Infrastructure |
| 4 | Modify `EntityRatingCache` update flow | Social module fires event after recalculation |

### Phase 5 — WeatherAPI.com Provider (~4h)

| # | File | Type |
|---|------|------|
| 1 | `WeatherApiComProvider.cs` | New: full IWeatherProvider implementation |
| 2 | `WeatherApiComResponse.cs` | New: deserialization DTOs for API response |
| 3 | `WeatherOptions.cs` | Modify: add ApiKey, BaseUrl, ForecastDays config |
| 4 | `DependencyInjection.cs` | Modify: register WeatherApiComProvider when ApiKey configured, else NoOp |
| 5 | `WeatherPreFetchService.cs` | Modify: implement actual pre-fetch logic (was no-op) |

### Phase 6 — Solution Build + Verify

Build `ContentSeo.Presentation` + full solution. Fix any test compilation.

---

## File Summary

| Category | Count |
|----------|-------|
| New files | ~22-26 |
| Modified files | ~10-12 |
| Total effort | ~14h |

### New Files (~24)

**Event handlers (6)**: TourGuideActivated, TourGuideDeactivated, TourGuideUpdated, CreatorProfileActivated, CreatorProfileDeactivated, CreatorProfileUpdated

**Commands (6)**: UpdateRedirect, DeleteSitemapEntry, UpdateSitemapEntry, PurgeWeatherCache, ResetWeatherBudget + validators

**Queries (1)**: ListSitemapEntries

**DTOs (2)**: SitemapEntryDto, UpdateSitemapEntryRequest

**Domain events (1)**: RedirectUpdatedDomainEvent + handler

**Weather (2)**: WeatherApiComProvider, WeatherApiComResponse

**Utility (1)**: SchemaMarkupMerger

**Cross-module events (4-6)**: In ContentTours.Contracts + ContentBlogs.Contracts + Social.Contracts

### Modified Files (~11)

1. `SeoEntityType.cs` — add TourGuide=4, Creator=5
2. `Redirect.cs` — add Update() method
3. `CreateRedirectCommandHandler.cs` — MaxHops fix + bare throw fix
4. `NoOpWeatherProvider.cs` — throw → log
5. `WeatherOptions.cs` — add ApiKey, BaseUrl
6. `DependencyInjection.cs` — register real provider
7. `WeatherPreFetchService.cs` — implement pre-fetch
8. `RedirectEndpoints.cs` — add PUT
9. `SitemapEndpoints.cs` — add GET list, DELETE, PATCH
10. `WeatherEndpoints.cs` — add DELETE cache, PUT budget/reset
11. `ContentSeoCacheKeys.cs` — add new cache tags

---

## Endpoint Summary After Plan

### Current: 17 endpoints
### New: 8 endpoints
### Total: 25 endpoints

| Group | Current | New | Total |
|-------|---------|-----|-------|
| FaqItem | 5 | 0 | 5 |
| Redirect | 3 | +1 (PUT update) | 4 |
| SeoMetadata | 3 | 0 | 3 |
| Sitemap | 3 | +3 (GET list, DELETE, PATCH) | 6 |
| Weather | 3 | +2 (DELETE cache, PUT budget/reset) | 5 |
| **Total** | **17** | **+6** | **23** |

### Permission Usage After Plan

| Permission | Status Before | Status After |
|------------|--------------|-------------|
| Redirect.Update | DEAD | ✅ Used by PUT /redirects/{id} |
| Sitemap.Read | DEAD | ✅ Used by GET /sitemap/entries |
| Sitemap.Create | DEAD | Still dead (no manual creation — auto-managed) |
| Sitemap.Update | DEAD | ✅ Used by PATCH /sitemap/entries/{id} |
| Sitemap.Delete | DEAD | ✅ Used by DELETE /sitemap/entries/{id} |
| Weather.Update | DEAD | ✅ Used by PUT /weather/budget/reset |
| Weather.Delete | DEAD | ✅ Used by DELETE /weather/cache/{id} |

**Result**: 6 of 7 dead permissions wired. Only `Sitemap.Create` remains dead (intentional — sitemap entries are always auto-created by integration events). Remove `Sitemap.Create` from catalog.

---

*Plan written: 2025-07-19. All 9 decisions locked. Compatible with all 10 existing workflow plans.*
