# Discovery & Personalization — Build Plan (Tier C)

> **Scope:** surface the **Analytics** module (49 endpoints) across the public app — recommendation rails, trending/popular sections, search autocomplete, and map-viewport search — turning the static catalog into a personalized, discoverable one.
> **Companion docs:** `UI-UX-Design.md` §4.11 (discovery/search/map), `Agents/Plans/Analytics-*`, `Agents/Recomendation Engine/*`.
> **Authoring rules:** `CONTROLLER_AUTHORING_GUIDE.md` (authoritative).
> **API base path:** `/api/v1`
> **Status:** 📋 Planned — not started

---

## Why this plan

Analytics is almost entirely **unsurfaced** — only `POST /interactions` telemetry is partially wired on `Tours/Detail`. The module exposes **popular / trending / recommendations** plus the data behind **search autocomplete** and **map search**. These are the rails that drive engagement and conversion. Most endpoints are **public reads** → unlike Tiers A/B these pages **can use output cache** (the `PublicList` policy) and render as **ViewComponents** reused across Home, Tour, and Place pages.

---

## Architecture conventions (read before building)

Layered-by-type per `CONTROLLER_AUTHORING_GUIDE.md`: `Areas/{Area}/{Controllers,Facades,ApiClients,Models/{Feature},Views/{Controller}}`; four-tier `Controller → Facade → ApiClient → IApiClient`; errors as `ApiResult`; suffix-based DI; `BaseController`; explicit `asp-area`/antiforgery/`_Alerts`/`<partial>` views; build csproj **alone** (`CS2012`).

**This tier's specifics:**
- **Reuse via ViewComponents (Pattern C).** Recommendation/trending/popular rails appear on multiple pages → build them as **ViewComponents** (`PopularToursViewComponent`, `RecommendationsViewComponent`, `TrendingViewComponent`) that wrap a Facade, not as page controllers. Render with `<vc:popular-tours .../>`.
- **Caching allowed here.** Public discovery reads are `[AllowAnonymous]` and shared → tag them for the **`PublicList`** output-cache policy and evict on relevant writes. Personalized `recommendations` for a signed-in user must be **per-user / no-cache** (vary by user) — keep that rail separate from the anonymous popular/trending rails.
- **Telemetry is fire-and-forget.** `POST /interactions` must never block render or surface errors to the user — call it async, ignore failure (still return `ApiResult`, just don't `GuardSignOut` on it).
- **Search suggest** is a JSON endpoint for the navbar typeahead — a thin `[HttpGet]` controller action returning `Json(...)` backed by the Facade; debounce client-side.
- **Map** uses **Mapbox** (per `UI-UX-Design.md` §4.11.5); the viewport endpoint feeds GeoJSON to the map on pan/zoom.

### Design & UI skills (mandatory for all views)

- **`ui-ux-pro-max`** + **`impeccable`** — rail/carousel hierarchy, "why you're seeing this" affordances, typeahead UX, map cluster/pin design, skeleton-loading + empty states.
- **`design-taste-frontend`** — component architecture for reusable rails + hardware-accelerated carousels.
- **`huashu-design`** — hi-fi exploration of the discovery home + map UI variants; anti-AI-slop pass.

Constraint: stay within the Bootstrap 5 **Booking** template assets (`wwwroot/assets`, existing tour/place cards + swiper).

---

## Template → page wiring (master map)

| Widget / page | Area / route | Template source | Reuse note |
|---|---|---|---|
| Popular / Trending rails | (on) `Public/Home` | `index-tour.html` "Best Packages" + recent-search chips | ViewComponents |
| "You may also like" rail | (on) `Public/Tours/Detail`, `Directory/Detail` | card grids in `tour-detail.html` | ViewComponent, entity-scoped |
| Personalized home rail | (on) `Public/Home` (auth) | `index-tour.html` hero area | per-user, no-cache |
| Search autocomplete | global (`_Navbar` search) | search box in `index-tour.html` | JSON typeahead |
| Map search | `Public/Tours/Index` / `Directory/Index` | decorative map in `index-directory.html` | Mapbox + viewport feed |

---

## Phase 0 — Grounding (do first)

- [ ] Inspect `src/Modules/Analytics` → confirm paths + response shapes for `GET /popular/tours`·`/popular/places`·`/popular/businesses`, `GET /trending`, `GET /analytics/recommendations`, `POST /interactions`.
- [ ] Confirm search-suggest path (`GET /tours/search/suggest`) in `ContentTours` and map-viewport (`GET /places/map/viewport`) in `ContentPlaces`.
- [ ] Read `UI-UX-Design.md` §4.11.5 (Mapbox) + `Agents/Recomendation Engine/*` for the intended recommendation contract.
- [ ] Confirm the **output-cache** tags/policy names already registered in `Program.cs` (`Lookups`, `PublicList`) so rails evict correctly.
- [ ] Decide the **recommendation context key** (anonymous = popular/trending; authed = personalized) and how the home page composes both.

**Acceptance:** confirmed endpoint list + cache tags + a chosen anon-vs-personalized composition rule.

---

## Phase 1 — Popular / Trending / Recommendation rails (ViewComponents)

| Endpoint | Use |
|---|---|
| `GET /popular/tours` · `/popular/places` · `/popular/businesses` | anonymous "Most popular" rails (cacheable) |
| `GET /trending` | "Trending now" rail (cacheable, short TTL) |
| `GET /analytics/recommendations` | personalized rail (auth, no-cache) |
| `POST /interactions` | log impressions/clicks (fire-and-forget) |

**Files** (layered — under `Areas/Public/`)
- [ ] `ApiClients/DiscoveryApiClient.cs` · `Facades/DiscoveryFacade.cs` (the personalized one may inject `IOutputCacheStore` only where it evicts)
- [ ] `ViewComponents/{PopularToursViewComponent,TrendingViewComponent,RecommendationsViewComponent}.cs`
- [ ] `Models/Discovery/` — `PopularItemResponse`, `TrendingResponse`, `RecommendationResponse`, `DiscoveryRailVm`, `DiscoveryMapper`
- [ ] `Views/Shared/Components/{PopularTours,Trending,Recommendations}/Default.cshtml` (reuse existing card partials)
- [ ] Render the rails in `Views/Home/Index.cshtml` (+ entity-scoped "you may also like" on `Tours/Detail`, `Directory/Detail`)
- [ ] `ApiClients/InteractionsApiClient.cs` · `Facades/InteractionsFacade.cs` — fire-and-forget telemetry helper used by rails + detail pages

**Acceptance:** the home page shows popular + trending rails (cached) and, when signed in, a personalized rail; clicking a card logs an interaction without blocking.

---

## Phase 2 — Search autocomplete (navbar typeahead)

| Endpoint | Use |
|---|---|
| `GET /tours/search/suggest` | suggestions (tours/places/guides) as the user types |
| `GET /tours/search` | full results page (already in `Public/Tours/Index` — verify) |

**Files** (layered — under `Areas/Public/`)
- [ ] `Controllers/SearchController.cs` — `Suggest` (`[HttpGet]` → `Json`)
- [ ] `Facades/SearchFacade.cs` + `ApiClients/SearchApiClient.cs` (or extend existing Tours search)
- [ ] `Models/Search/` — `SuggestionResponse`, `SuggestionVm`
- [ ] `wwwroot/assets/js/yallajo-search-suggest.js` (debounced typeahead) wired into `Views/Shared/_Navbar.cshtml`

**Acceptance:** typing in the navbar shows debounced suggestions; Enter goes to the full results page; works without JS (plain submit).

---

## Phase 3 — Map-viewport search (Mapbox)

| Endpoint | Use |
|---|---|
| `GET /places/map/viewport` | GeoJSON features for the current map bounds |
| `GET /places/nearby` | geolocation "near me" |

**Files** (layered — under `Areas/Public/`)
- [ ] `ApiClients/MapApiClient.cs` · `Facades/MapFacade.cs` · `Controllers/MapController.cs` (`Viewport` GET → GeoJSON `Json`)
- [ ] `Models/Map/` — `ViewportRequest` (bbox), `MapFeatureResponse`, `MapVm`
- [ ] `wwwroot/assets/js/yallajo-map.js` (Mapbox init, pan/zoom → viewport fetch, clustered pins)
- [ ] Map panel partial on `Views/Tours/Index.cshtml` / `Views/Directory/Index.cshtml`

**Acceptance:** panning the map refetches pins for the visible bounds; clicking a pin opens the place/tour card; "near me" centers on geolocation.

---

## Out of scope / blocked

| Item | Reason |
|---|---|
| Admin analytics dashboards | ✅ already Admin-area `Statistics` |
| A/B experiment tooling, ML model config | backend/ops concern, not UI |
| Saved searches / alerts | later; confirm backend support |

---

## Sequencing & effort

1. **Phase 0** — grounding *(blocking; ~0.5 day)*.
2. **Phase 1** — rails *(~1 sprint; highest engagement value, reusable)*.
3. **Phase 2** — search suggest *(~0.5 sprint)*.
4. **Phase 3** — map search *(~1 sprint; Mapbox integration)*.

**Highest value:** Phase 1 rails (reused on Home + every detail page). **Highest effort:** Phase 3 map.

## Open questions

- Recommendation contract: does `GET /analytics/recommendations` vary by auth user automatically, or need an explicit context/seed param?
- Is `search/suggest` a single endpoint across entity types, or per-type?
- Mapbox token provisioning + usage limits (ops) for `yallajo-map.js`.
