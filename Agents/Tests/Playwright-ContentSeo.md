# Playwright MCP Test Scenarios — ContentSeo

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans

| Source | Status / test impact |
|---|---|
| `Agents/Plans/ContentSeo-Workflow.md` | Implemented SEO/FAQ/Redirect/Sitemap/Weather; workflow includes sitemap lifecycle, redirects, aggregate rating, WeatherAPI.com. |
| `Agents/Plans/ContentSeo-Audit-Report.md` | Confirms 23 endpoints, 21 integration event handlers, 7 entities, `SeoEntityType` includes TourGuide/Creator, WeatherApiComProvider exists. |
| `Agents/Plans/ContentSeo-FixPlan.md` | Mostly doc/minor fixes; dead read permissions intentionally future. |
| `Agents/Plans/Master-RoadmapTo10.md` | W4-B: remove/comment dead permissions and doc corrections. |
| `Agents/Plans/CrossDocumentAnalysisReport.md` | SEO is clean; ContentPlaces slug changes and EntityTag events are cross-module expectations. |

**Scenario count:** 46 total = 36 active, 6 NOT_BUILT, 4 DEFERRED.

## 0. Prerequisites

- **App state:** not running; SQL bug blocks execution; reCAPTCHA disabled.
- **Web URL:** `https://localhost:57065/swagger`; **API URL:** `https://localhost:57065`.
- **Seed users:** `admin@yallajo.test` / `TestPass!23` content-admin; `userA@yallajo.test` / `TestPass!23` read-only public.
- **Seed content:** Petra, Wadi Rum, Jordan/Amman URLs; at least one seeded sitemap entry and SEO metadata row per Place/Tour/Business/Blog if available.
- **API route prefix:** `/api/v1/seo`; public sitemap route is root `/sitemap.xml`, sub-sitemaps `/sitemaps/{entityType}.xml`.
- **MCP tools:** use `browser_navigate` directly for XML endpoints and `browser_evaluate(() => document.documentElement.outerHTML)` for XML/robots/meta inspection. Use `browser_network_requests` for 301 redirect checks.
- **SEO critical rule:** never parse `/sitemap.xml` as normal UI text only; inspect raw XML outerHTML.

## 1. Built — Active Scenarios

### Sitemap generation and public XML

#### CS001 — Public `/sitemap.xml` returns XML
- `browser_navigate(API URL + /sitemap.xml)` or Web host equivalent. Use `browser_evaluate(() => document.documentElement.outerHTML)`. Assert `<urlset>` or sitemap index XML, content type/xml rendering, no auth required.

#### CS002 — Sitemap XML contains priorities/lastmod/changefreq
- Inspect XML from `/sitemap.xml`; assert each `<url>` has `<loc>`, `<lastmod>`, `<changefreq>`, `<priority>` when applicable.

#### CS003 — Sitemap includes seeded public places
- Assert Petra/Wadi Rum public URLs are present and inactive/deleted/admin-only URLs absent.

#### CS004 — Sub-sitemap by entity type
- Navigate `/sitemaps/places.xml` or actual entity type route. Assert only matching entity URLs and valid XML.

#### CS005 — Admin list sitemap entries
- Admin GET `/api/v1/seo/sitemap/entries?entityType=Place&isActive=true&page=1&pageSize=50`. Assert paginated DTO fields: id, url, entityType, entityId, priority, changeFrequency, lastModified, isActive.

#### CS006 — Admin update sitemap priority/changefreq
- PATCH `/api/v1/seo/sitemap/entries/{id}` with priority/changeFrequency. Assert list and public XML reflect new values after regeneration/cache refresh.

#### CS007 — Admin delete sitemap entry
- DELETE `/api/v1/seo/sitemap/entries/{id}`. Assert public XML no longer includes URL and admin list reflects removal.

#### CS008 — Admin regenerate sitemap endpoint
- POST `/api/v1/seo/sitemap/regenerate`. Assert success and public XML is refreshed. Treat cron/background 6h service as eventual regeneration outside direct request.

#### CS009 — Sitemap cron 6h expectation
- If diagnostics/logs are exposed, assert `SitemapRegenerationService` scheduled interval is 6h. Otherwise mark as background config verification in test notes.

### Redirects

#### CS010 — Admin lists redirects
- GET `/api/v1/seo/redirects`. Assert existing redirects with oldUrl, newUrl, statusCode, active flag.

#### CS011 — Admin creates 301 redirect
- POST `/api/v1/seo/redirects` with old URL `/old-pw-seo`, target `/new-pw-seo`, status 301. Assert created id and list includes it.

#### CS012 — Public redirect returns 301
- Navigate Web URL `/old-pw-seo`; inspect network response. Assert status 301 and final URL `/new-pw-seo`.

#### CS013 — Admin updates redirect target/status/active
- PUT `/api/v1/seo/redirects/{id}` changing target, status 302/301, active false/true. Assert chain flattening reruns when target changes.

#### CS014 — Redirect chain detection max 5 hops
- Create/update chain length 5 and assert accepted/flattened; attempt 6th hop or cycle and assert rejection. Note implementation historically uses MaxHops=3; task acceptance requires max 5, so divergence is possible.

#### CS015 — Admin deletes redirect
- DELETE `/api/v1/seo/redirects/{id}`. Assert old URL no longer redirects or returns expected 404/normal route.

### SEO metadata, canonical, hreflang, meta tags

#### CS016 — Public get metadata
- GET `/api/v1/seo/metadata/{entityType}/{entityId}` anonymous. Assert title, description, canonical, og fields, schema markup if present.

#### CS017 — Admin upserts metadata
- POST `/api/v1/seo/metadata` for Place/Petra with title, description, canonical, `og:title`, `og:image`, schema. Assert response and public GET.

#### CS018 — Admin updates metadata
- PUT `/api/v1/seo/metadata/{id}` changing canonical/OG fields. Assert public page/source or metadata API reflects changes.

#### CS019 — Public page canonical tag
- Navigate seeded place/tour/blog public page. Use `browser_evaluate(() => document.querySelector('link[rel="canonical"]')?.href)`. Assert canonical matches metadata.

#### CS020 — Public page Open Graph tags
- Use `browser_evaluate` to read `meta[property="og:title"]`, `og:image`, `og:description`. Assert values match metadata.

#### CS021 — Hreflang ar/en variants
- Navigate English and Arabic variants. Assert `link[rel="alternate"][hreflang="en"]` and `hreflang="ar"` point to corresponding localized URLs.

#### CS022 — Multi-language SEO metadata fallback
- Request metadata/page in `Accept-Language: ar` then `en`. Assert localized title/description if present; fallback safe if missing.

#### CS023 — Schema AggregateRating
- Given Social rating aggregate event/fixture exists, GET metadata and assert SchemaMarkup contains `aggregateRating` with ratingValue/reviewCount/bestRating/worstRating.

### FAQ

#### CS024 — Public FAQ list
- GET `/api/v1/seo/faq/{entityType}/{entityId}` anonymous. Assert active FAQ items sorted and localized.

#### CS025 — Admin creates FAQ item
- POST `/api/v1/seo/faq` with question/answer and entity target. Assert public list includes it.

#### CS026 — Admin updates FAQ item
- PUT `/api/v1/seo/faq/{id}`. Assert updated question/answer visible.

#### CS027 — Admin deletes FAQ item
- DELETE `/api/v1/seo/faq/{id}`. Assert public list hides it.

#### CS028 — Admin reorders FAQ items
- PUT `/api/v1/seo/faq/reorder` with ordered ids. Assert public order changes.

### Weather / robots

#### CS029 — Public get weather by place
- GET `/api/v1/seo/weather/{placeId}` for Petra. Assert current conditions and 7-day forecast shape; if provider unavailable, assert graceful placeholder with `IsAvailable=false`.

#### CS030 — Public get weather by coordinates
- GET `/api/v1/seo/weather?lat=31.95&lng=35.93`. Assert coordinate-keyed cache behavior and forecast shape.

#### CS031 — Weather 12h cache
- Call same weather endpoint twice. Assert second response is served from same cache timestamp or does not increment provider call count if diagnostics available.

#### CS032 — Weather daily budget 1000 calls
- Use fixture/admin diagnostics to set budget near 1000; assert next external fetch is blocked with quota/budget response and cached/placeholder fallback.

#### CS033 — Admin refresh weather
- POST `/api/v1/seo/weather/refresh/{placeId}`. Assert cache refreshed and budget count handled.

#### CS034 — Admin purge weather cache
- DELETE `/api/v1/seo/weather/cache/{id}`. Assert subsequent GET refetches or returns cache miss/new entry.

#### CS035 — Admin reset weather budget
- PUT `/api/v1/seo/weather/budget/reset`. Assert daily call count resets.

#### CS036 — Robots.txt
- Navigate Web/API `/robots.txt`. Use `browser_evaluate(() => document.body.innerText || document.documentElement.outerHTML)`. Assert sitemap URL is declared and disallow/allow rules match public crawling strategy.

## 2. NOT_BUILT

1. **NB001 — `Sitemap.Create` manual endpoint.** Intentionally not built; sitemap entries are auto-managed by integration events.
2. **NB002 — Read-permission guarded public metadata/FAQ/weather routes.** GET routes are `AllowAnonymous`, leaving read permissions dead by design/future gates.
3. **NB003 — ContentSeo consumer for ContentCore EntityTagAssigned/Removed.** ContentCore audit says SEO tag-change consumer follow-up remains.
4. **NB004 — Task max redirect chain 5 vs implementation MaxHops 3.** Audit says MaxHops fixed to 3; task asks max 5. Tests should record divergence if 4-5 hop chains reject.
5. **NB005 — Robots endpoint not confirmed in ContentSeo presentation.** If `/robots.txt` is served by host/static file, keep scenario but mark host-owned.
6. **NB006 — Browser-visible hreflang/canonical UI coverage may be incomplete.** API metadata exists; Razor public pages may not render every SEO tag yet.

## 3. DEFERRED

1. **D001 — Removing/commenting 4 dead permissions.** FixPlan keeps future auth gates; not runtime behavior.
2. **D002 — Extra test coverage for ContentSeo internals.** Audit says low priority.
3. **D003 — Real external WeatherAPI quota burn.** Use mocks/fixtures; do not consume 1000 real calls.
4. **D004 — Background cron timing wait.** Do not wait 6 hours in Playwright; verify config/log or trigger rebuild endpoint.

## 4. Integration Events

- Consumes: Blog archived/deleted/published/unpublished/updated; Business created; Language activated; Place created/updated/deleted; Tour approved/created/deleted/suspended; Creator activated/deactivated/profile updated; TourGuide activated/deactivated/profile updated; Social `ReviewAggregateUpdated`.
- Publishes/internal effects: redirect updated domain event, sitemap regeneration, metadata/schema updates.
- Event scenarios: create/update/delete place and assert sitemap/redirect metadata; publish/unpublish blog and assert sitemap entry active state; review aggregate event updates schema; language activation produces localized SEO variants.

## 5. Validation Matrix

| Area | Valid | Invalid / expected rejection |
|---|---|---|
| Sitemap priority | 0.0-1.0 | <0, >1, non-numeric |
| Change frequency | allowed enum/string values | unsupported value |
| Redirect status | 301, 302 | 200, 404, 500 |
| Redirect URL | local/allowed target, no self-cycle | self redirect, cycle, chain > max hops |
| Metadata | title/description/canonical/OG within limits | empty required, invalid URL, over max |
| FAQ | question+answer non-empty, ordered | empty, over max, duplicate order if disallowed |
| Weather coordinates | lat -90..90, lng -180..180 | out of range, missing coordinate |
| XML | valid escaped URL XML | unescaped `&`, invalid XML document |

## 6. Auth Matrix

| Capability | Anonymous | userA read-only | content-admin |
|---|---:|---:|---:|
| View `/sitemap.xml`, sub-sitemaps | Yes | Yes | Yes |
| View public metadata/FAQ/weather | Yes | Yes | Yes |
| List/create/update/delete redirects | No | No | Yes |
| Regenerate/list/update/delete sitemap entries | No | No | Yes |
| Upsert/update metadata | No | No | Yes |
| Create/update/delete/reorder FAQ | No | No | Yes |
| Refresh/purge/reset weather budget | No | No | Yes |

## 7. State Machines (where applicable)

### Redirect
```text
Created(active, 301/302) → Updated(target/status/active)
Active → Inactive → Active
Any → Deleted
Target changes trigger chain detection/flattening; cycles invalid.
```

### Sitemap entry
```text
AutoCreated/Active → PriorityChanged/ChangeFreqChanged → Touched(lastmod)
Active → Inactive/Deleted
Background: Dirty/Expired → Regenerated every 6h or admin trigger.
```

### Weather cache
```text
Missing → FetchProvider → Cached(12h, 7-day forecast)
Cached → Expired → Refetch
Cached → Purged → Missing
Budget 0..1000 → Exhausted → AdminReset
```

### Metadata
```text
Missing → Upserted → Updated
SchemaMarkup empty → AggregateRating merged → Replaced/merged on next aggregate event
Localized variants emit canonical/hreflang tags.
```

## 8. Known Divergence

1. **Redirect chain hops:** task says max 5; ContentSeo audit says MaxHops is already fixed to 3. Scenario CS014 should record actual behavior.
2. **Public read permissions:** catalog retains some read permissions while GET routes are anonymous.
3. **Robots.txt:** may be served by host/static middleware rather than ContentSeo endpoints.
4. **Hreflang/meta UI rendering:** API metadata is built; Razor/public pages may lag behind API data.
5. **Sitemap cron:** verify by config/log/trigger, not by waiting 6h.
6. **App execution blocker:** app is not running and SQL bug exists; these are scenario designs, not executed results.
