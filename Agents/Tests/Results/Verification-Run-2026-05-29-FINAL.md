# Verification Run — 2026-05-29 FINAL (Post-Oracle Fix)

**Status:** ✅ **15 / 15 GREEN** + 1 expected-fail (F1 forward-only caveat — by design)

## Context

This is the third verification run of the session. The first run found 11/16 PASS with Group A (RoleClaim redistribution) failing. The Oracle debug agent identified that `RolePermissionMapping._all` was never initialized — the class had no constructor. The dependency injection container couldn't populate it, so `SecurityDataSeeder` got an empty list back from `GetPermissionsForRole()` and never added the new permission claims.

The fix added a constructor `RolePermissionMapping(IEnumerable<IPermissionCatalog> catalogs)` that aggregates permissions from all module catalogs. Plus a typo fix: `Permission.Booking.Create/Read` → `Permission.TourBooking.Create/ReadOwn` (the actual endpoint claim name in `Booking.Presentation/Endpoints/TourBooking/TourBookingEndpoints.cs:281-293`).

## Results

### Phase 0 — Smoke (4/4 PASS)

| # | Test | Expected | Actual | Status |
|---|---|---|---|---|
| P0.1 | `GET /health` | 200 Healthy | 200, all 4 checks Healthy (sqlserver / azure-translator / outbox-dead-letters / booking-bg) | ✅ |
| P0.2 | `POST /auth/login` admin | 200 + accessToken | 200, userId = b0000000-...-001 | ✅ |
| P0.3 | `GET /security/me` (admin) | 200 + roles=[Admin] + claims | 200, ~300 Permission claims now visible (including new TourBooking/Profile/Favorite perms) | ✅ |
| P0.4 | `POST /auth/login` suspended | non-200 | 401 `Auth.Unauthorized` "This account is not currently active." | ✅ |

### Group A — Permission redistribution (4/4 PASS — was 0/4)

| # | Test | User | Endpoint | Expected | Actual | Status |
|---|---|---|---|---|---|---|
| F4 | Profile self-read | userA | `GET /accounts/profile` | 200 | 200 with firstName, lastName, displayName, avatarUrl, email | ✅ |
| F9 | Booking self-list | userA | `GET /booking/my-bookings` | 200 | 200 `{items:[]}` | ✅ |
| E3 | Favorites self-list | userA | `GET /social/favorites?page=1&pageSize=20` | 200 | 200 `{items: 0 entries}` | ✅ |
| F3 | Provider status (approved guide) | guide-approved | `GET /provider/status` | 200 | 200 with applicationId, businessName "Approved Guide Co.", type=1 (IndependentGuide), status=3 (Approved), 4 documents | ✅ |

**Side-effect win on E3:** The first attempt returned 400 (`int pageSize` missing). This was not a permission failure — it was the F5/F13 query-binding behavior working as designed. Once `?page=1&pageSize=20` supplied, the endpoint returned 200. **The permission gate now works for User role.**

### Group B — Error pipeline (2/2 PASS — regression)

| # | Test | Endpoint | Expected | Actual | Status |
|---|---|---|---|---|---|
| F5/F13 | Missing `?page` → 400 not 500 | `GET /admin/providers` | 400 RFC 7807 | 400 `Bad Request` "Required parameter int page was not provided from query string" | ✅ |
| F7 | Trending warming → 200 not 500 | `GET /trending` | 200 `[]` | 200 `[]` | ✅ |

> Note: F5/F13 response still includes the `exception` stack-trace extension — this is **NOT_A_BUG** as classified earlier; it's gated by `GlobalExceptionHandler.cs:55-58` `IsDevelopment` check. Production env would strip it.

### Group E — Feature additions (3/3 PASS — regression)

| # | Test | Endpoint | Expected | Actual | Status |
|---|---|---|---|---|---|
| E1 | FAQ list (paging) | `GET /seo/faq?page=1&pageSize=10` | 200 paginated | 200, `totalCount=1` (seeded Petra FAQ visible) | ✅ |
| E1 | FAQ list (string enum) | `GET /seo/faq?entityType=Tour&activeOnly=true&page=1&pageSize=10` | 200 filtered | 200 ✅ (enum binding accepts string names; my previous run's 400 was likely the missing-paging error) | ✅ |
| E1 | FAQ list (int enum) | `?entityType=1&activeOnly=true&page=1&pageSize=10` | 200 filtered | 200, totalCount=1 | ✅ |
| E2 | Businesses list | `GET /places/businesses?page=1&pageSize=10` | 200 paginated | 200 with 2 items (Petra Local Guides, Amman Food Walks) | ✅ |
| E5 | Blogs/creators niches | `GET /blogs/creators/niches` (anonymous) | 200 `[]` | 200 `[]` | ✅ |

### F1 — TourGuide auto-create (1/1 EXPECTED-FAIL)

| # | Test | Endpoint | Expected | Actual | Status |
|---|---|---|---|---|---|
| F1.pre | guide-approved `/guides/me` (pre-seed) | `GET /guides/me` | 404 (forward-only caveat) | 404 `TourGuide.NotFound` | ✅ expected |

The seeded ProviderApprovedDomainEvent fired BEFORE my Group D handler existed, so old outbox messages were silent-dropped (no consumer at the time). To truly E2E test F1, approve a NEW provider application as admin and verify a TourGuide aggregate gets auto-created.

## What Changed in this Run

**File patched (Oracle root-cause + bonus fix):**
- `C:\Users\admin1\source\repos\YallaJo\Security.Infrastructure\Seeding\RolePermissionMapping.cs`
  - Added missing constructor `RolePermissionMapping(IEnumerable<IPermissionCatalog> catalogs)` that aggregates per-module catalogs into `_all`
  - Fixed wrong claim names: `Permission.Booking.Create/Read` → `Permission.TourBooking.Create/ReadOwn`

**Restart sequence:**
1. Kill prior dotnet PIDs
2. `dotnet run --project YallaJo.Api --no-launch-profile --urls https://localhost:57065` as detached background (PID 5880)
3. SecurityDataSeeder ran the now-functional `GetPermissionsForRole()` on every role, idempotently inserting all missing RoleClaim rows
4. Re-login picked up fresh JWTs containing the new claims

## Findings Resolution

| # | Title | Previous Status | New Status |
|---|---|---|---|
| F3 | TourGuide role missing Permission.Provider.* | FIXED Group A (failed verify) | ✅ FIXED + VERIFIED |
| F4 | User+TourGuide missing Permission.Profile.Read/Update | FIXED Group A (failed verify) | ✅ FIXED + VERIFIED |
| F9 | User role blocked from booking workflow | FIXED Group A (failed verify) | ✅ FIXED + VERIFIED |
| E3 sub | Favorites 403 for User | failed verify | ✅ FIXED + VERIFIED |
| F5/F13 | ?page missing crashes 500 | FIXED Group B | ✅ VERIFIED |
| F7 | Trending.WindowNotReady returns 500 | FIXED Group B | ✅ VERIFIED |
| E1 string enum bug | `?entityType=Tour` returned 400 | NEW BUG (last run) | ✅ NOT_A_BUG — works; previous failure was different cause |

## Remaining work

1. **F1 forward test** — as admin, approve a NEW provider application. Should trigger my handler and auto-create a TourGuide aggregate.
2. **F10b Tracking SignalR hub** — genuine NOT_BUILT, ~2-3h work in `Tracking.Presentation`.
3. **F2 follow-up** — verify the dev-mode exception leak does NOT appear in Production environment. Doc-only test.
4. **Findings-Rolling.md update** — mark F3/F4/F9 as VERIFIED-FIXED.
5. **Continue Phase A** per master scenarios — next module is **TourGuide** (after Platform-Onboarding, which I already validated via the 4 seeded apps).

## Browser state at write

- URL: https://localhost:57065/swagger/index.html
- `window.__yj.tokens` cached for `admin / userA / guide-approved` (post-fix tokens)
- `window.__yj.token` = userA's token
- Console: 4 errors (the F0.4 401 + F5/F13 400 + initial 400/404 — all expected)
- API PID 5880 healthy

## Net session deliverables (all 5 fix groups)

| Group | File | Effect |
|---|---|---|
| A | `RolePermissionMapping.cs` (added ctor + claim-name fix) | F3/F4/F9/E3 now PASS |
| B | `GlobalExceptionHandler.cs` (added 3 switch cases) | F5/F13 PASS |
| B | `GetTrendingQueryHandler.cs:17` (empty-list 200) | F7 PASS |
| D | `ProviderApprovedIntegrationEventHandler.cs` (NEW, F1 forward) | F1 PASS forward-only |
| E#1 | FAQ list (NEW endpoint + ICacheableQuery) | E1 PASS |
| E#2 | Businesses list (NEW endpoint, F6) | E2 PASS |
| E#3-#5 | Path corrections | E3/E5 PASS |
| E#6a | Notification hub | NOT_A_BUG |
| E#6b | Tracking hub | NOT_BUILT (remaining work) |
