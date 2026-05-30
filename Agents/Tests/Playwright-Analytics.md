# Playwright MCP Test Scenarios — Analytics Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans / Score (6.8→10.0)

Sources read in full:

- `Agents/Plans/Analytics-Workflow.md`
- `Agents/Plans/Analytics-Module-Workflow.md`
- `Agents/Plans/Analytics-Audit-Report.md`
- `Agents/Plans/Analytics-FixPlan.md`
- `Agents/Recomendation Engine/RECOMMENDATIONS_ENGINE_SPEC.md`
- `Agents/Recomendation Engine/RECOMMENDATIONS_ENGINE_GAPS.md`

References used:

- `Agents/Plans/Master-RoadmapTo10.md` — Analytics is the highest-priority gap module, `6.8 → 10.0`.
- `Agents/Plans/CrossDocumentAnalysisReport.md` — Analytics gaps: CQRS bypass history, anonymous mutation risk, missing collaborative filtering, MaxPerPlaceId vs MaxPerProviderId divergence, ~21 validation paths missing.
- Code sampled: `Analytics.Presentation/AnalyticsEndpoints.cs`, `Analytics.Presentation/Endpoints/Recommendations/RecommendationsEndpoints.cs`, `Analytics.Application/Queries/GetRecommendations/GetRecommendationsQueryHandler.cs`, `Analytics.Application/Scoring/*`, `Analytics.Infrastructure/BackgroundServices/*`, `Analytics.Domain/Enums/InteractionType.cs`.

Execution note: Web URL is `https://localhost:57065/swagger`; API URL is `https://localhost:57065`. The app is currently not running because of a SQL bug; recaptcha is disabled. Use UI flows when Razor pages exist; otherwise use `browser_evaluate` authenticated `fetch` calls against `/api/v1/*`. For interaction tests, always use `browser_network_requests` with filter `/analytics/` and inspect POST payloads/responses.

## 0. Prerequisites

1. SQL startup bug fixed and both hosts running:
   - Web: `https://localhost:57065/swagger`
   - API: `https://localhost:57065`
2. Recaptcha disabled for seeded users.
3. Seed users exist, password `TestPass!23`:
   - `admin@yallajo.test` — Administrator/all permissions.
   - `userA@yallajo.test`, `userB@yallajo.test` — regular customers with pre-seeded browsing/booking/review history.
   - `guide-approved@yallajo.test` — approved guide with guide dashboard access.
   - `business@yallajo.test`, `agency@yallajo.test` — provider/agency actors.
   - `suspended@yallajo.test` — suspended actor; role checks/status gates should fail.
4. Recommendation smoke data exists:
   - `EntityAttributeSnapshot` rows for at least 10 active Tours, 5 Places, 5 Businesses.
   - `PopularityScore` rows with non-null scores and some `TrendingRank` values.
   - `RecommendationCache` rows for `userA`; exclusions for at least one candidate via `UserExcludedEntity`.
   - UserInteraction history for `userA` and `userB` to prove fallback/personalization behavior.
5. Analytics background services are enabled unless explicitly testing disabled config:
   - `InteractionIngestDrainService`.
   - `PopularityScoreCalculationService`.
   - `SuggestionBatchRefreshJob`.
   - `MetricsAggregationJob`.
6. Helper functions available in Playwright MCP:
   - `loginAs(email)` via UI or API; stores cookies/JWT.
   - `apiFetch(token, path, { method, body })` through `browser_evaluate`.
   - `captureAnalyticsPosts()` using `browser_network_requests({ filter: "/analytics/" })`.
7. Do not assert real collaborative filtering as Built. Current code has `NoOpCollaborativeScoringEngine` and `NoOpBlendedScoringEngine`; real implementations are NOT_BUILT.

Recommended `browser_evaluate` helper shape:

```js
async ({ apiUrl, token, path, method = 'GET', body }) => {
  const res = await fetch(`${apiUrl}${path}`, {
    method,
    credentials: 'include',
    headers: {
      'content-type': 'application/json',
      ...(token ? { authorization: `Bearer ${token}` } : {})
    },
    body: body ? JSON.stringify(body) : undefined
  });
  const text = await res.text();
  return { status: res.status, headers: Object.fromEntries(res.headers), body: text ? JSON.parse(text) : null };
}
```

## 1. Built — Active Scenarios

### Interaction Ingestion

**A-001 — Logged-in user browses tour and emits interaction POST**

- Actor: `userA@yallajo.test`.
- Route/API: UI tour detail page or `POST /api/v1/interactions`.
- Steps:
  1. Log in as userA.
  2. Navigate to a seeded tour detail page.
  3. Wait for tracking JS or call interaction endpoint with `EntityType=Tour`, `InteractionType=View`/`PageView` equivalent.
  4. Capture requests with `browser_network_requests` filter `/analytics/`.
- Expected:
  - A POST to `/api/v1/interactions` or analytics tracking path is captured.
  - Response is `202 Accepted` for the ingestion command.
  - Payload includes `UserId` or authenticated user context; no raw password/email in metadata.
  - If UI uses route alias under `/api/v1/analytics/interactions`, record actual route in test output.

**A-002 — Channel batching accepts up to 100 interaction envelopes**

- Actor: `userA` via `browser_evaluate`.
- Route/API: `POST /api/v1/interactions` repeated 100 times, or UI script loop if SDK exists.
- Expected:
  - All requests return `202`/success without `500`.
  - `InteractionIngestDrainService` eventually flushes batch of up to 100 (`logger`/DB probe if available).
  - Re-running after flush does not lose events.

**A-003 — Interaction types accepted for built enum values**

- Actor: `userA`.
- Types: `View`, `Click`, `Search`, `AddToFavorite`, `RemoveFromFavorite`, `BookingStarted`, `BookingCompleted`, `BookingCancelled`, `Share`, `ReviewSubmitted`, `Bookmark`, `NotInterested`.
- Expected:
  - Each valid current enum value returns accepted/valid.
  - Invalid values return validation/business error, never `500`.
  - Note divergence: workflow names `PageView`, `GuideProfileView`, etc.; current enum uses names above and lacks 12–16 guide-specific values.

**A-004 — Planned guide interaction types are skeleton-only until enum extension ships**

- Actor: `userA`.
- Types: `GuideProfileView`, `GuideBooking`, `SlotSelection`, `TourCompletion`, `GuideRating`.
- Expected:
  - Mark as NOT_BUILT if API rejects them today.
  - When wired, expect accepted interactions for guide profile views, bookings, slot selection, completion, and rating.

**A-005 — Anonymous tracking via session cookie divergence check**

- Actor: anonymous browser context.
- Steps: visit a public tour/place page, inspect cookies/local storage and network.
- Expected:
  - If current implementation follows updated Analytics-Workflow decision #1, anonymous POSTs are not accepted and return `401/403`.
  - If a UI public read creates anonymous local session state only, confirm no server-side POST mutation succeeds unauthenticated.
  - Record divergence against older recommendations spec, which allowed `UserId=null` anonymous interactions.

**A-006 — Logged-in tracking uses authenticated user, not spoofed body UserId**

- Actor: `userA`.
- Steps: POST interaction body with `UserId` set to `userB` if request DTO still allows `UserId`; capture request and query admin interactions as admin.
- Expected:
  - Preferred behavior: server derives identity from auth and ignores spoofed body user id.
  - If body `UserId` is accepted as-is, flag as Known Divergence/security gap.
  - No PII appears in `Metadata`, `ClientIpHash`, or user-agent fields beyond hashed/scrubbed values.

**A-007 — PII scrubbing in interaction metadata**

- Actor: `userA`.
- Steps: send metadata/query/search terms containing email/phone-like values through SearchQuery/FilterApply equivalent.
- Expected:
  - Stored/captured metadata is scrubbed or rejected.
  - No raw email, phone, token, or address appears in API response/admin list.
  - Client IP is hashed or absent, not raw where exposed.

### Guide Dashboard

**A-008 — Approved guide sees own dashboard summary**

- Actor: `guide-approved@yallajo.test`.
- Route/API: UI `/dashboard` or API `GET /api/v1/guide/dashboard`.
- Expected:
  - `200 OK`.
  - Summary includes bookings/completed tours, revenue, average rating, profile views or equivalent, and conversion/utilization where implemented.
  - Data is scoped to guide's own `UserId`/guide id.

**A-009 — Guide analytics date range filter**

- Actor: `guide-approved`.
- Route/API: `GET /api/v1/guide/analytics?from=...&to=...`.
- Expected:
  - `200 OK`.
  - Metrics for narrower range are less than/equal broad range where seeded data supports it.
  - Invalid date ranges return validation error, not `500`.

**A-010 — Guide my-tours breakdown**

- Actor: `guide-approved`.
- Route/API: `GET /api/v1/guide/my-tours?pageSize=20`.
- Expected:
  - `200 OK` with only the guide's tours.
  - Rows include per-tour booking/rating/engagement analytics where exposed.
  - Pagination inputs are bounded.

**A-011 — Guide dashboard export to CSV/JSON skeleton**

- Actor: `guide-approved`.
- Route/API: planned `GET /api/v1/dashboard/guide/export` or actual route when added.
- Expected:
  - If export endpoint is absent today, mark NOT_BUILT.
  - When wired: `200 OK`, `text/csv` or configured export MIME, file name includes date range, and rows are guide-scoped.

**A-012 — Another guide/provider cannot read target guide data**

- Actors: `guide-approved`, `agency`/another guide seed.
- Steps: attempt to request guide dashboard with another guide/provider id if any query param/path can specify it.
- Expected:
  - `403`, not-found masking, or empty own-scoped data.
  - No target guide revenue/bookings leak.

**A-013 — Suspended guide cannot access guide analytics**

- Actor: `suspended@yallajo.test`.
- Routes: `/api/v1/guide/dashboard`, `/api/v1/guide/analytics`, `/api/v1/guide/my-tours`.
- Expected:
  - `403 Forbidden` or status-gated denial.
  - No dashboard JSON leaks.

### Popularity & Trending

**A-014 — Popular tours endpoint returns ranked public list**

- Actor: anonymous and `userA`.
- Route/API: `GET /api/v1/popular/tours`.
- Expected:
  - Anonymous public read returns `200 OK`.
  - Items are sorted by popularity score/rank as implemented.
  - Response excludes inactive/deleted entities.

**A-015 — Popular places and businesses endpoints return public lists**

- Actor: anonymous.
- Routes: `GET /api/v1/popular/places`, `GET /api/v1/popular/businesses`.
- Expected:
  - `200 OK`.
  - Entity type in each item matches route.
  - No auth-only fields leak.

**A-016 — Trending endpoint reflects TrendingRank/delta**

- Actor: anonymous.
- Route/API: `GET /api/v1/trending`.
- Expected:
  - `200 OK`.
  - Items with lower `TrendingRank`/higher 7-day delta appear ahead of stale items.
  - Empty trend set returns an empty array/valid response, not `500`.

**A-017 — PopularityScoreCalculationService cadence smoke**

- Actor: admin/test harness.
- Setup: create interactions for a candidate; either wait configured interval or invoke hosted-service trigger if test hook exists.
- Expected:
  - Service cadence is configured for 30 minutes per plan, unless test config overrides.
  - Scores recalculate from weighted interactions and write `TrendingRefreshedIntegrationEvent`/popularity outbox where observable.
  - No duplicate score rows for same `(EntityType, EntityId)`.

### Recommendations (likely NOT_BUILT — see audit)

**A-018 — Personalized recommendations endpoint smoke**

- Actor: `userA` with pre-seeded cache/history.
- Route/API: `GET /api/v1/analytics/recommendations?limit=20`.
- Expected:
  - `200 OK`.
  - Response shape matches `RecommendationsResponse` with ranked items.
  - `UserExcludedEntity` candidates do not appear.
  - Current implementation may be cache/fallback content-only; do not require collaborative score fields.

**A-019 — Recommendations requires auth**

- Actor: anonymous.
- Route/API: `GET /api/v1/analytics/recommendations`.
- Expected: `401/403`; no personalized data returned.

**A-020 — Cached recommendation TTL smoke**

- Actor: `userA`.
- Steps: call recommendations twice within cache TTL.
- Expected:
  - Second response is stable where cache exists.
  - Current plan says RecommendationCache TTL 10 min / query cache 5 min; record actual headers/logs if exposed.
  - Expired cache/fallback still returns valid content-only suggestions.

**A-021 — Diversity rule skeleton: MaxPerProviderId**

- Actor: `userA`.
- Setup: seed >3 highly scored items from same provider.
- Expected:
  - Current behavior may still use place/category diversity, not provider cap.
  - Mark NOT_BUILT until `MaxPerProviderId=3` is implemented.
  - When wired, no more than 3 results per provider in top N.

**A-022 — Real collaborative filtering skeleton**

- Actor: `userA` and `userB` with overlapping high-signal history.
- Expected:
  - Today: `NoOpCollaborativeScoringEngine` means no collaborative uplift; mark NOT_BUILT.
  - When wired: candidates co-occurring with user history receive score contribution and are explainable.

**A-023 — Blended scoring skeleton (40/35/25)**

- Actor: `userA`.
- Expected:
  - Today: `NoOpBlendedScoringEngine` passes content scores unchanged; mark NOT_BUILT.
  - When wired: final score = `0.40 collaborative + 0.35 content + 0.25 popularity` with configurable weights.

### Admin / Recommendation Ops

**A-024 — Admin refreshes suggestion batch**

- Actor: `admin@yallajo.test`.
- Route/API: `POST /api/v1/analytics/admin/batches/refresh`.
- Expected:
  - `200 OK` with refreshed batch or success marker.
  - Regular user receives `403`.

**A-025 — Admin lists suggestion batches**

- Actor: admin.
- Route/API: `GET /api/v1/analytics/admin/batches`.
- Expected:
  - `200 OK` list.
  - Includes batch id/source/context/item count where exposed.

**A-026 — Admin CRUD for boosts/pins/seasonality/holidays/experiments uses auth gates**

- Actor: admin and userA.
- Routes: `/api/v1/analytics/admin/boosts`, `/pins`, `/seasonality`, `/holidays/{year}`, `/experiments`.
- Expected:
  - Admin can create/list/delete as implemented.
  - Regular user gets `403` for every admin mutation.
  - Captured requests show MediatR-backed routes, no repo bypass visible at Presentation boundary.

**A-027 — Sponsored click requires auth**

- Actor: anonymous and userA.
- Route/API: `POST /api/v1/analytics/recommendations/sponsored-click`.
- Expected:
  - Anonymous: `401/403`.
  - Authenticated user: valid click accepted if bid/entity exists.
  - `browser_network_requests` filter `/analytics/` captures POST and response.

**A-028 — Suggestion metric requires auth**

- Actor: anonymous and userA.
- Route/API: `POST /api/v1/analytics/recommendations/metrics`.
- Expected:
  - Anonymous: `401/403`.
  - Authenticated user: metric accepted when batch/cache id exists.

### GDPR / Preferences

**A-029 — User exports analytics data**

- Actor: `userA`.
- Route/API: actual `GET /api/v1/analytics/recommendations/me/export`.
- Expected:
  - `200 OK` JSON export.
  - Includes interactions/preferences/exclusions/recommendation cache/experiment data where available.
  - Does not include userB data.
  - Note route divergence from planned `/gdpr/export`.

**A-030 — User requests and cancels GDPR deletion**

- Actor: `userA`.
- Routes: `DELETE /api/v1/analytics/recommendations/me`, `POST /api/v1/analytics/recommendations/me/cancel-deletion`.
- Expected:
  - Delete request creates 30-day pending window.
  - Cancel endpoint succeeds within window.
  - Repeated cancel returns clean not-found/invalid-state, not `500`.

**A-031 — Preferences read/update round trip**

- Actor: `userA`.
- Routes: `GET /api/v1/analytics/preferences`, `PUT /api/v1/analytics/preferences`.
- Expected:
  - Auth required.
  - Update invalidates recommendation/preferences cache where observable.
  - userB cannot read userA preferences.

## 2. NOT_BUILT — major (per audit)

**A-NB-001 — Real collaborative filtering engine**

- Not built: `NoOpCollaborativeScoringEngine` returns empty dictionary.
- Skeleton assertion: when real implementation is wired, pre-seeded co-occurrence users should change ordering versus content-only fallback.

**A-NB-002 — Blended scoring assembly (40% collaborative + 35% content + 25% popularity)**

- Not built: `NoOpBlendedScoringEngine` passes content scores unchanged.
- Skeleton assertion: final score components and weights are returned/logged or otherwise verifiable.

**A-NB-003 — Nightly matrix recomputation / cache prewarm**

- Not built: `CollaborativeMatrixBuildService` absent.
- Skeleton assertion: nightly 03:00 UTC job reads last 180 days high-signal interactions and pre-warms `analytics:collab:*` HybridCache entries.

**A-NB-004 — Diversity rule fix**

- Not built/uncertain: plan says `MaxPerPlaceId → MaxPerProviderId=3`.
- Skeleton assertion: no more than 3 results from same provider in personalized top N.

**A-NB-005 — Guide-specific interaction enum values 12–16**

- Not built in current `InteractionType` enum.
- Skeleton assertion: guide profile/book/slot/completion/rating events are accepted and feed dashboard metrics.

**A-NB-006 — Guide dashboard precompute and export**

- Partial: 3 guide endpoints exist, but `GuideDashboardPreComputeService` and export endpoint are not confirmed.
- Skeleton assertion: nightly 02:00 UTC cache rebuild writes key-patterned guide dashboard cache and CSV export is guide-scoped.

## 3. DEFERRED

1. DashboardCache schema redesign with `EntityType`, `EntityId`, `Granularity` columns; current implementation is key/value (`Key`, `ValueJson`, `ExpiresAt`, `RebuiltAt`).
2. GDPR route rename/additional alias from `/recommendations/me/export` to `/gdpr/export`.
3. Blog interaction tracking (`EntityType.Blog`) and richer recommendation quality signals.
4. Halal/dietary filters dependent on ContentPlaces fields where not already present.
5. Marketing-consent-driven email digest and push targeting dependent on Accounts/Messaging maturity.
6. A/B experiment statistical significance reporting beyond create/start/complete tracking.

## 4. Integration Events (28 handlers per workflow — list categories)

Validate via event-driven smoke tests where feasible; otherwise assert route-visible projection changes after outbox processing.

1. Booking events (5 in module workflow): created, confirmed, cancelled, completed, slot capacity changed.
2. Finance events (3 in module workflow; audit variants mention 5): payment completed, payout completed, refund completed; older audit also lists commission rule upsert/delete.
3. Social events (4): review published, review deleted, favorite added, rating recalculated.
4. ContentTours events (4): tour created, updated, deleted, suspended.
5. ContentPlaces events (6): place created/updated/deleted; business created/updated/deleted.
6. ContentCore category events (2): entity category assigned/removed where present.
7. Auth/Security events (1–2): user registered / user-created defaults.
8. Accounts events (provider approved and related profile/provider signals; module workflow lists 4 Account handlers).
9. Messaging support-ticket events (2): ticket created, ticket assigned.

Event scenarios:

**A-EV-001 — Booking completion creates interaction and dashboard signal**: complete seeded booking; expect BookingSnapshot/user interaction/popularity input updated after outbox.

**A-EV-002 — Review published updates recommendation/rating signal**: publish review; expect review interaction/rating snapshot update.

**A-EV-003 — Provider approved initializes dashboard cache**: approve provider/guide seed; expect provider/guide cache key or default metrics row.

## 5. Validation Matrix (~21 missing per audit)

| Area | Positive | Negative/edge |
|---|---|---|
| Interaction ingest | Valid entity/type/session accepted | Empty entity id, invalid type, oversized metadata rejected |
| Recommendations | `limit=1..100` accepted | `limit<=0` clamps/rejects; no `500` |
| Similar/entity suggestions | Valid `kind/id/context` accepted | Unknown kind/context returns validation/not found |
| Preferences | Valid categories/budget saved | Invalid enum/budget/category id rejected |
| Not-interested | Valid active entity excluded | Duplicate exclusion idempotent/conflict cleanly |
| Admin boosts/CPC | Valid bid/daily budget/date range | Negative bid, expired date range, missing entity rejected |
| Editorial pins | Valid position/context accepted | Duplicate/invalid position rejected |
| Seasonality | Valid month range/factor accepted | Month outside 1–12, start>end, factor<=0 rejected |
| Holidays | Valid year/date range accepted | Year mismatch, invalid JSON rejected |
| Photogenic | Existing entity toggles | Missing entity returns 404 |
| Experiments | Valid traffic 1–100 and variants JSON | Invalid traffic/JSON rejected |
| Metrics | Valid stage/position accepted | Missing batch/cache refs handled cleanly |
| GDPR | Own export/delete/cancel allowed | Anonymous and cross-user denied |

## 6. Auth Matrix

| Endpoint family | Anonymous | User | Guide | Provider/Agency | Admin |
|---|---:|---:|---:|---:|---:|
| `GET /api/v1/popular/*`, `/trending` | 200 | 200 | 200 | 200 | 200 |
| `POST /api/v1/interactions` | 401/403 | 202 | 202 | 202 | 202 |
| `GET /api/v1/analytics/recommendations` | 401/403 | 200 own | 200 own | 200 own | 200 |
| `GET /api/v1/analytics/recommendations/similar/*` | 200 | 200 | 200 | 200 | 200 |
| `POST /analytics/recommendations/onboarding`, `/not-interested` | 401/403 | 200 | 200 | 200 | 200 |
| `POST /analytics/recommendations/sponsored-click`, `/metrics` | 401/403 | 200 valid | 200 valid | 200 valid | 200 valid |
| `/api/v1/analytics/preferences` | 401/403 | 200 own | 200 own | 200 own | 200 |
| `/api/v1/guide/*` | 401/403 | 403 | 200 own | 403 unless guide role | 200/403 per policy |
| `/api/v1/provider/*` analytics | 401/403 | 403 | 403 unless provider claim | 200 own | 200/403 per policy |
| `/api/v1/admin/*` analytics | 401/403 | 403 | 403 | 403 | 200 |
| Suspended user on protected analytics | 401/403 | 403 | 403 | 403 | n/a |

## 7. Background Services

1. `InteractionIngestDrainService` — channel drain, batch size 100; flushes user interactions to DB.
2. `PopularityScoreCalculationService` — intended 30-min cadence; recalculates weighted score/trending rank and writes popularity/trending integration events.
3. `SuggestionBatchRefreshJob` — recommendation batch refresh/bootstrap.
4. `UserProfileUpdateJob` — preference affinity/profile update.
5. `TripStageUpdateJob` — trip-stage updates from booking/tracking context.
6. `GdprCleanupJob` — daily deletion execution and anonymization.
7. `EmailDigestBackgroundService` — re-engagement email digest.
8. `MetricsAggregationJob` — hourly/platform metric aggregation.
9. Planned NOT_BUILT: `RecommendationMatrixRebuilder` / `CollaborativeMatrixBuildService` — nightly 03:00 UTC.
10. Planned/partial: `GuideDashboardPreComputeService` — nightly 02:00 UTC.

## 8. Known Divergence

1. Routes differ: interaction endpoint in current code is `/api/v1/interactions`, while workflow text often says `/api/v1/analytics/interactions`.
2. Guide dashboard actual routes are `/api/v1/guide/dashboard`, `/api/v1/guide/analytics`, `/api/v1/guide/my-tours`, not planned `/dashboard/guide`.
3. GDPR export actual route is `/api/v1/analytics/recommendations/me/export`, not `/gdpr/export`.
4. Current `InteractionType` names/values differ from older recommendation spec and lack planned values 12–16.
5. Recommendation endpoint still contains redundant `ICurrentUser.IsAuthenticated/UserId` checks in Presentation, despite systemic rule to prefer endpoint auth.
6. Admin analytics CRUD endpoints still use broad `Batch.Refresh/Read` metadata in sampled code for boosts/pins/seasonality/holidays/experiments, despite plan goal of granular feature permissions.
7. Collaborative/blended engines are NoOp; do not treat recommendation quality as final.
8. DashboardCache is key/value, not typed granularity schema.
9. `RecordInteractionRequest` currently accepts `UserId`; tests must check spoofing risk.
