# Playwright MCP Test Scenarios — ContentPlaces

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans

| Source | Status / test impact |
|---|---|
| `Agents/Plans/ContentPlaces-Workflow.md` | Implemented place/business workflows; actual API prefix `/api/v1`; routes include places, businesses, service items, amenities, staff, accessibility. |
| `Agents/Plans/ContentPlaces-Audit-Report.md` | Confirms 41 endpoints, business state machine, type mapping, route fixes; historic gaps include ProviderSuspended/ReviewCreated handlers and validators. |
| `Agents/Plans/ContentPlaces-FixPlan.md` | Confirms route prefix fix and more-docs/slug payload/accessibility constants follow-ups. |
| `Agents/Plans/Master-RoadmapTo10.md` | W4-C polish; ContentPlaces near 10/10. |
| `Agents/Plans/CrossDocumentAnalysisReport.md` | Provider/business suspension cascade and EntityImage redundancy inform Known Divergence. |

**Scenario count:** 60 total = 43 active, 10 NOT_BUILT, 7 DEFERRED.

## 0. Prerequisites

- **App state:** not running; SQL bug blocks execution; reCAPTCHA disabled.
- **Web URL:** `https://localhost:57065/swagger`; **API URL:** `https://localhost:57065`.
- **Seed users:** `admin@yallajo.test` / `TestPass!23` content-admin; `userA@yallajo.test` / `TestPass!23` read-only public.
- **Seed content:** countries Jordan/USA and cities Amman/Aqaba are expected test fixtures, but current code stores `Country`/`City` as string fields on `Place`/`Business` (no Country/City aggregate endpoints found). Places: Petra, Wadi Rum.
- **API route prefix:** `/api/v1`.
- **Map defaults:** Mapbox GL JS map defaults to Jordan center `[31.95, 35.93]`, zoom `8`; pin colors tours=blue, places=green, businesses=orange; max 5000 pins; clustering for high count.
- **MCP tools:** standard Playwright MCP plus `browser_evaluate` for `window.map.getCenter()`, `window.map.getZoom()`, marker/source counts, clustering state; `browser_take_screenshot` for visual map proof.
- **Weather:** task requires weather integration: 12h cache, 7-day forecast, 1000 calls/day, `IWeatherProvider`; actual implementation is in ContentSeo (`/api/v1/seo/weather*`) not ContentPlaces — test as cross-module divergence.

## 1. Built — Active Scenarios

### Places of Interest / Admin CRUD

#### CP001 — Public list places
- Anonymous GET `/api/v1/places` with pagination/search filters. Assert Petra/Wadi Rum visible, pagination metadata, no admin-only fields.

#### CP002 — Public get place by id
- GET `/api/v1/places/{id}` for Petra. Assert name, slug, city/country, lat/lng, rating/tour count.

#### CP003 — Public get place by slug
- GET `/api/v1/places/{slug}`. Assert same place as id route and canonical slug behavior.

#### CP004 — Nearby places Haversine search
- GET `/api/v1/places/nearby?lat=31.95&lng=35.93&radiusKm=...`. Assert nearby places sorted by distance.

#### CP005 — Map viewport returns bounded pins
- GET `/api/v1/places/map/viewport?north=&south=&east=&west=`. Assert only pins inside bounds and <=5000 rows.

#### CP006 — Admin creates Place of Interest
- Admin POST `/api/v1/places` with unique `name+country`, slug, type, Amman/Jordan strings, lat/lng. Assert created id and public visibility.

#### CP007 — Unique name+country enforced
- Admin POST duplicate place name in same country. Assert conflict. Same name in USA should be accepted if otherwise valid.

#### CP008 — Admin updates place
- PUT `/api/v1/places/{id}` changing localized/display fields, city/country, lat/lng. Assert GET by id/slug reflects update.

#### CP009 — Place slug change creates observable redirect expectation
- Update slug; assert new slug works. If old slug redirect is wired through ContentSeo, assert 301; otherwise document divergence.

#### CP010 — Admin deletes place
- DELETE `/api/v1/places/{id}`. Assert public list/detail hide it; active businesses block if applicable.

#### CP011 — Feature place
- PATCH `/api/v1/places/{id}/feature?isFeatured=true/false`. Assert featured flag/order visible in list or detail.

#### CP012 — Verify place
- PATCH `/api/v1/places/{id}/verify?isVerified=true/false`. Assert verified badge/field.

### Countries / Cities as current field-level behavior

#### CP013 — Country filter Jordan
- GET `/api/v1/places?country=Jordan`. Assert Jordan places (Petra/Wadi Rum) appear and USA places do not.

#### CP014 — City filter Amman/Aqaba
- GET `/api/v1/places?city=Amman` and `?city=Aqaba`. Assert city-specific filtering.

#### CP015 — Create place with country/city strings
- POST place using `country=Jordan`, `city=Amman`. Assert values round-trip exactly after trim/normalization.

#### CP016 — Reject invalid/empty city-country combinations where validator requires them
- Submit blank/overlength city/country. Assert validation errors or document optional-field behavior if accepted.

### Businesses

#### CP017 — Public list businesses at place
- GET `/api/v1/places/{placeId}/businesses`. Assert approved businesses only for public user.

#### CP018 — Public get business by id
- GET `/api/v1/places/businesses/{id}`. Assert business type, place id, city/country, status-safe fields.

#### CP019 — Search/filter businesses
- GET `/api/v1/places/businesses/search?query=&type=&city=&country=`. Assert text/type/city/country filters and stable pagination.

#### CP020 — Nearby businesses
- GET `/api/v1/places/businesses/nearby?lat=&lng=&radiusKm=`. Assert distance sorting.

#### CP021 — Admin/provider creates business with valid provider type mapping
- POST `/api/v1/places/businesses`; assert status Pending, 7 default hours, place exists, max 10 active check, unique `(PlaceId,BusinessType,OwnerId)`.

#### CP022 — Reject invalid provider→business type mapping
- Attempt disallowed type for provider. Assert `Business.TypeNotAllowedForProvider` / forbidden.

#### CP023 — Reject duplicate business per place/type/owner
- Create same type at same place for same owner twice. Assert conflict; different type allowed.

#### CP024 — Update business
- PUT `/api/v1/places/businesses/{id}` changing details, city/country, options. Assert GET reflects update.

#### CP025 — Delete business
- DELETE `/api/v1/places/businesses/{id}`. Assert hidden from public and owner/admin state changes.

#### CP026 — Business resubmit after rejection/more-docs
- POST `/api/v1/places/businesses/{id}/resubmit`. Assert state Rejected/MoreDocsNeeded→Pending and resubmission count increments; max 3 enforced.

#### CP027 — Admin approve business
- POST `/api/v1/places/businesses/admin/{id}/approve`. Assert Pending→Approved, public visibility, integration event expected.

#### CP028 — Admin reject business
- POST `/api/v1/places/businesses/admin/{id}/reject` with reason. Assert Pending/MoreDocsNeeded→Rejected and reason visible to owner/admin.

#### CP029 — Admin request more docs
- POST `/api/v1/places/businesses/admin/{id}/request-more-docs` with reason. Assert Pending→MoreDocsNeeded and notification/event expectation.

#### CP030 — Admin suspend/reinstate business
- POST suspend then reinstate. Assert Approved→Suspended→Approved and public visibility toggles.

#### CP031 — My businesses
- Authenticated owner GET `/api/v1/places/businesses/mine`. Assert only owned businesses, admin/read-only behavior correct.

### Business sub-entities

#### CP032 — Get business hours
- GET `/api/v1/places/businesses/{id}/hours`. Assert 7 days exist by default.

#### CP033 — Set business hours split shifts
- PUT `/api/v1/places/businesses/{id}/hours` with two non-overlapping intervals same day. Assert success and round-trip.

#### CP034 — Reject overlapping/invalid business hours
- PUT overlapping intervals, close before open, >2 shifts/day. Assert validation error and previous hours unchanged.

#### CP035 — List/create/update/delete service items
- GET/POST `/places/businesses/{businessId}/services`, then GET/PUT/DELETE `/places/businesses/services/{id}`. Assert price/currency/category/duration/capacity/discount fields.

#### CP036 — Service item discount validation
- Submit `DiscountPercent`, `DiscountValidFrom`, `DiscountValidTo`, sale price. Assert valid windows accepted; invalid percent/date rejected.

#### CP037 — List/add/remove business amenities
- GET/POST `/places/businesses/{businessId}/amenities`, DELETE `/places/businesses/amenities/{amenityId}`. Assert public list updates.

#### CP038 — List/add/remove business staff
- GET/POST `/places/businesses/{id}/staff`, DELETE `/places/businesses/staff/{id}`. Assert owner/admin auth and staff list update.

#### CP039 — Place accessibility get/update
- GET/PUT `/api/v1/places/{id}/accessibility`. Assert wheelchair/visual/hearing/cognitive/mobility flags round-trip.

#### CP040 — Business accessibility get/update
- GET/PUT `/api/v1/places/businesses/{id}/accessibility`. Assert business entity type is distinct from place and values round-trip.

### Interactive map / Weather

#### CP041 — Interactive Mapbox default viewport
- Navigate to map/search UI or page hosting Mapbox. Use `browser_evaluate(() => ({ center: window.map.getCenter(), zoom: window.map.getZoom() }))`; assert center approx `{lat:31.95,lng:35.93}`, zoom `8`, map initialized.

#### CP042 — Map pin colors and max pin count
- Load map viewport with seeded tours/places/businesses. Use `browser_evaluate` to inspect source/layer/marker metadata; assert places green, businesses orange, tours blue, and rendered/source pin count <=5000. Capture screenshot.

#### CP043 — Map clustering for many pins
- Seed/use large viewport. Assert cluster layer/source enabled and expanding cluster reduces grouped count correctly.

## 2. NOT_BUILT

1. **NB001 — Country CRUD endpoints.** Task asks Countries CRUD, but code search shows country is a string field on Place/Business; no Country aggregate/endpoints.
2. **NB002 — City CRUD endpoints.** Task asks Cities CRUD, but city is a string field; no City aggregate/endpoints.
3. **NB003 — Dedicated Places of Interest route separate from `/places`.** Current Place entity represents POI; tests map POI CRUD to `/api/v1/places`.
4. **NB004 — Weather endpoints in ContentPlaces.** Weather provider/cache endpoints live in ContentSeo (`/api/v1/seo/weather*`).
5. **NB005 — `IWeatherProvider` in ContentPlaces.** Abstraction exists under ContentSeo weather implementation, not ContentPlaces.
6. **NB006 — Provider document expiry → business suspension.** Cross-document CROSSING-1 remains a legal/liability gap.
7. **NB007 — Full provider suspension cascade across all modules.** ContentPlaces has handler coverage, but downstream ContentTours/Social/Messaging behavior is cross-module and not fully guaranteed.
8. **NB008 — ReviewCreated/rating update handler ambiguity.** Audit reported missing; later plan notes may have partial coverage. Treat stale ratings as a failure if not updated.
9. **NB009 — Standalone country/city admin UI pages.** Web Content area has Places/Tags/Categories/Search; no Countries/Cities page found.
10. **NB010 — Mapbox UI route not confirmed.** API map viewport exists; interactive Mapbox may be in Web/UI layer or pending.

## 3. DEFERRED

1. **D001 — BusinessStaff name-only entries and task assignment.** Workflow marks future enhancement.
2. **D002 — Business dashboard analytics.** Future enhancement.
3. **D003 — ServiceItem → Booking AvailabilitySlot bridge.** Future integration.
4. **D004 — Holiday/opening-hour exceptions.** Future enhancement.
5. **D005 — Business subscription tier management.** Property exists; management workflow deferred.
6. **D006 — Unified EntityImage/media pipeline.** Cross-document REDUNDANCY-4; use ContentCore for media but duplication remains.
7. **D007 — Real weather external quota exhaustion simulation.** Avoid burning WeatherAPI calls; use mocks/fixtures where possible.

## 4. Integration Events

- Publishes: `PlaceCreated/Updated/Deleted`, `BusinessCreated/Updated/Approved/Rejected/MoreDocsRequested/Suspended/Reinstated/Resubmitted/Deleted`, `ServiceItemCreated/Deleted`, `BusinessStaffAdded/Removed`.
- Consumes: `LanguageActivated` from ContentCore, `PlaceTourCountUpdated` from ContentTours, provider status/suspension from Accounts, ratings from Social.
- Event scenarios: approve business and assert Messaging notification observable; update place slug and assert ContentSeo sitemap/redirect behavior; language activation should eventually expose translations.

## 5. Validation Matrix

| Area | Valid | Invalid / expected rejection |
|---|---|---|
| Place name+country | Unique name in country | Duplicate same country |
| Latitude | -90..90 | -91, 91, non-numeric |
| Longitude | -180..180 | -181, 181, non-numeric |
| Map viewport | bounded north/south/east/west | inverted/huge bounds if rejected; result still <=5000 |
| Business type | Allowed by provider type | Disallowed provider→business mapping |
| Business duplicate | Same owner/place different type | Same owner/place/type duplicate |
| Business state | Pending→Approved/Rejected/MoreDocs; Approved→Suspended→Approved | Invalid transitions; resubmit >3 |
| Business hours | 0-2 non-overlapping intervals/day | Overlap, close<=open, >2/day |
| Weather | 7-day forecast, 12h cache | >1000/day budget, external unavailable |

## 6. Auth Matrix

| Capability | Anonymous | userA read-only | Owner/provider | content-admin |
|---|---:|---:|---:|---:|
| View places/businesses/search/map | Yes | Yes | Yes | Yes |
| Create/update/delete/feature/verify places | No | No | No | Yes |
| Create/update/resubmit own business | No | No | Yes if approved provider | Yes |
| Business admin transitions | No | No | No | Yes |
| Manage own hours/services/amenities/staff/accessibility | No | No | Yes | Yes |
| View public weather/map | Yes | Yes | Yes | Yes |
| Weather budget/cache admin | No | No | No | Yes (via ContentSeo) |

## 7. State Machines (where applicable)

### Business
```text
Create → Pending
Pending → Approved | Rejected | MoreDocsNeeded
MoreDocsNeeded → Pending(resubmit) | Approved | Rejected
Rejected → Pending(resubmit, max 3)
Approved → Suspended → Approved(reinstate)
Any allowed terminal/admin path → Deleted/SoftDeleted
```

### Place
```text
Draft/Create → Active
Active ↔ Featured
Active ↔ Verified
Active → Deleted (blocked when active businesses exist)
```

### Map
```text
Uninitialized → MapboxInitialized(center Jordan, zoom 8) → PinsLoaded(<=5000) → Clustered(if dense) → Filtered/ViewportChanged
```

### Weather cache (cross-module)
```text
Missing/Stale → Fetch via IWeatherProvider → Cached(12h, 7-day forecast) → Expired → Refetch
DailyBudget: 0..1000 calls → Exhausted → AdminReset
```

## 8. Known Divergence

1. **Countries/Cities:** task requests CRUD, but current ContentPlaces model stores city/country strings on Place/Business; no Country/City modules/endpoints found.
2. **Weather ownership:** weather provider/cache/admin routes live in ContentSeo, not ContentPlaces.
3. **Places of Interest naming:** implementation uses `Place` for POI and discovery places.
4. **Mapbox UI:** API viewport exists; interactive Mapbox page may be absent while Web Content area contains Search/Places pages.
5. **Plan/audit history:** older audit score/counts are stale; workflow implementation notes say route prefix fix and W4-C additions are applied.
6. **App execution blocker:** app is not running and SQL bug exists; these are scenario designs, not executed results.
