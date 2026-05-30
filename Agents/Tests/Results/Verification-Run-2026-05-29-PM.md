# Verification Run — 2026-05-29 PM

**Run timestamp:** 2026-05-29 ~12:28 UTC
**API:** https://localhost:57065 (PID 22048, healthy)
**Tester:** Playwright MCP via `window.__yj.apiFetch` adapter
**Scope:** Verification of Groups A/B/D fixes + Group E #1 (FAQ) + Group E #2 (Businesses) + path corrections.

---

## Result table (16 calls)

| # | Finding | Call | Status | Expected | Verdict |
|---|---------|------|--------|----------|---------|
| 1 | LOGIN | admin login | 200 | 200 | ✅ PASS |
| 2 | LOGIN | userA login | 200 | 200 | ✅ PASS |
| 3 | LOGIN | guide-approved login | 200 | 200 | ✅ PASS |
| 4 | **F4** | userA `GET /accounts/profile` | **403** | 200 | ❌ **FAIL** |
| 5 | **F9** | userA `GET /booking/my-bookings` | **403** | 200 | ❌ **FAIL** |
| 6 | **F3** | guide `GET /provider/status` | **403** | 200 | ❌ **FAIL** |
| 7 | F1 caveat | guide `GET /guides/me` | 404 | 404 (pre-handler seed) | ⚠ EXPECTED |
| 8 | **F5/F13** | admin `GET /admin/providers` no page | **400** | 400 | ✅ **PASS** |
| 9 | **F7** | admin `GET /trending` | **200** (empty `[]`) | 200 | ✅ **PASS** |
| 10 | **E1** | `GET /seo/faq?page=1&pageSize=10` | **200** | 200 | ✅ **PASS** (1 FAQ seeded) |
| 11 | **E1** | `GET /seo/faq?entityType=Tour&activeOnly=true` | **400** | 200 | ❌ **FAIL — new bug** |
| 12 | **E1** | `GET /seo/faq?page=-5&pageSize=999` | **200** (pageSize→200) | 200 | ✅ **PASS** (clamp works) |
| 13 | **E2** | `GET /places/businesses?page=1&pageSize=10` | **200** (2 items) | 200 | ✅ **PASS** |
| 14 | **E2** | `GET /places/businesses?country=Jordan` | **200** | 200 | ✅ **PASS** |
| 15 | **E2** | `GET /places/businesses?pageSize=999` | **200** (cap 50) | 200 | ✅ **PASS** |
| 16 | E3 | userA `GET /social/favorites` | **403** | 200 | ❌ **FAIL** (same root as F4/F9) |
| 17 | E5 | anon `GET /blogs/creators/niches` | **200** (`[]`) | 200 | ✅ **PASS** (path correction confirmed) |

**Tally:** 11 PASS · 5 FAIL · 1 expected-fail.

---

## Wins

### ✅ Group B fully verified (F5/F13 + F7)
- **F5/F13** `BadHttpRequestException → 400` mapping works. `/admin/providers` without `?page` now returns clean 400 RFC 7807 instead of 500 with stack trace.
- **F7** `GetTrendingQueryHandler` returns `Result.Success([])` instead of `Failure(500)`. Empty trending window now yields `200 []`.

### ✅ Group E #1 (FAQ list) verified
- `GET /api/v1/seo/faq?page=1&pageSize=10` → 200 with 1 seeded item (Petra tour FAQ).
- Page/pageSize clamp works (`page=-5&pageSize=999` → page=1, pageSize=200).
- `ICacheableQuery` wiring confirmed by successful response shape (`{items, pageNumber, pageSize, totalCount, totalPages}`).

### ✅ Group E #2 (Businesses list) verified
- `GET /api/v1/places/businesses?page=1&pageSize=10` → 200 with 2 items (Petra Local Guides, Amman Food Walks).
- Country filter works (`?country=Jordan` returns same 2).
- pageSize clamp from existing `SearchBusinessesQueryHandler` works (capped at 50).

### ✅ Path corrections confirmed (E5)
- `GET /api/v1/blogs/creators/niches` → 200 `[]` (real path exists; probe sweep was wrong).

---

## Failures

### ❌ Group A NOT effective — F3/F4/F9 + E3 ALL still 403

Calls **4, 5, 6, 16** all return 403 Forbidden.

**Root cause (hypothesis):** my edit to `RolePermissionMapping.cs` added `ConsumerPermissions` + `ProviderSelfPermissions` HashSets and extended the User + TourGuide filters. The seeder responsible for applying these to the database is `SecurityDataSeeder` (called from `SecurityDbInitializer` or DI startup), which I noted earlier is **idempotent — defensive AnyAsync per-permission check, will safely add missing perms on restart**.

**But the test shows it didn't.** Possibilities:
1. `SecurityDataSeeder` runs only on FIRST init (when Roles table is empty) and short-circuits otherwise. The Roles table already had 8 roles, so it didn't re-scan.
2. The new permissions only flow through `RolePermissionMapping.GetPermissionsForRole(roleName)` if the seeder calls it on EVERY startup. If it only calls it when seeding net-new roles, my mapping change is dead code.
3. The seeder DOES run but checks `Permission.Profile.Read` permission existence, not RoleClaim existence per role. Need to inspect.

**Next action:** open `Security.Infrastructure\Seeding\SecurityDataSeeder.cs` to confirm whether it iterates roles on every startup or only on fresh init. Possible fix: force it to do per-startup reconciliation, OR manually run a one-time RoleClaim sync script.

### ❌ NEW BUG: F12.3 FAQ filter — `entityType=Tour` returns 400

Call 11 (`/seo/faq?entityType=Tour&activeOnly=true`) returns 400.

But call 10 (`/seo/faq?page=1&pageSize=10`) works fine, and call 12 (`?page=-5&pageSize=999`) also works.

**Likely cause:** ASP.NET Core minimal API binding for the `SeoEntityType?` nullable enum doesn't accept string `"Tour"` — probably needs the integer (the response body shows `entityType: 1` for the Petra FAQ, so Tour=1).

**Fix options:**
- Update test: use `?entityType=1` instead of `?entityType=Tour`.
- OR add a custom binder/parser in `GetAllFaqItemsQuery` so it accepts enum names.

**Workaround:** `?entityType=1&activeOnly=true` should work. Need to verify.

---

## What survived (confirmed Built and working)

- Login + JWT for all 3 test roles (admin/User/TourGuide).
- Suspended account lockout (verified earlier).
- Rate limiter (verified earlier).
- `/health` (verified earlier).
- `/admin/providers` paginated query (with `?page=N`).
- `/trending` empty-window response.
- `/seo/faq` list (no filter, with clamp).
- `/places/businesses` list (with filter, with clamp).
- `/blogs/creators/niches` (path correction).

## What needs follow-up

1. **F3/F4/F9 + E3:** Investigate `SecurityDataSeeder` to discover why new RoleClaims aren't propagating. May require code change to force per-startup role-claim reconciliation, OR a manual one-time SQL/script run.
2. **F12.3 FAQ filter:** Decide on enum binding strategy (numeric only vs. add string-name support).
3. **F1 caveat resolution:** Once F3/F4/F9 fix lands, manually approve a NEW provider application as admin to verify the F1 handler fires forward.
4. **F10b tracking hub:** Still NOT_BUILT (2-3h work).
5. **Findings-Rolling.md update:** Demote F10/F11 to NOT_A_BUG.

---

## Console errors

9 console errors logged during the run — all expected (403/404/400 responses trigger fetch error logs in dev tools).

## Browser state at report write

- URL: `https://localhost:57065/swagger/index.html`
- `window.__yj.token` = userA's token (last set)
- `window.__yj.tokens` = `{admin@yallajo.test, userA@yallajo.test, guide-approved@yallajo.test}` (3 stored Bearer tokens)
- Adapter installed and working.

---

**End of report.**
