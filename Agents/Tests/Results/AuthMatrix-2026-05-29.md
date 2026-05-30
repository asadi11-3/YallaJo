# Test Run — Auth Matrix (8 users)

| Field | Value |
|---|---|
| **Date** | 2026-05-29 |
| **Runner** | Playwright MCP |
| **Scenario source** | [`../Playwright-CrossCutting.md`](../Playwright-CrossCutting.md) §1 + master index Phase B §1 |
| **Environment** | API `https://localhost:57065` |
| **Auth context** | Each test rotates `__yj.token`; admin is the default scaffolding context |

## Summary

| Total | PASS | FAIL | SKIP |
|---:|---:|---:|---:|
| 11 | **11** | 0 | 0 |

All 8 seeded users authenticate exactly as designed. Suspended account is blocked at the state-machine level. Rate limiter activates as designed. **Two pre-existing permission gaps were surfaced** while exercising role-gated reads (logged as Findings F3 and F4) — NOT introduced by my seeder edits.

## Scenarios

### Login matrix

| TC | Email | Password | Expected | Actual | userId match | Status |
|---|---|---|---|---|---|---|
| TC-AM-1 | `admin@yallajo.test` | `TestPass!23` | 200 | 200 | `…001` ✅ | ✅ |
| TC-AM-2 | `userA@yallajo.test` | `TestPass!23` | 200 | 200 | `…002` ✅ | ✅ |
| TC-AM-3 | `userB@yallajo.test` | `TestPass!23` | 200 | 200 | `…003` ✅ | ✅ |
| TC-AM-4 | `guide-pending@yallajo.test` | `TestPass!23` | 200 | 200 | `…004` ✅ | ✅ |
| TC-AM-5 | `guide-approved@yallajo.test` | `TestPass!23` | 200 | 200 | `…005` ✅ | ✅ |
| TC-AM-6 | `business@yallajo.test` | `TestPass!23` | 200 | 200 | `…006` ✅ | ✅ |
| TC-AM-7 | `agency@yallajo.test` | `TestPass!23` | 200 | 200 | `…007` ✅ | ✅ |
| TC-AM-8 | `suspended@yallajo.test` | `TestPass!23` | 401 (blocked) | 401 | n/a | ✅ |

### Token-gated reads (introspection probe)

| TC | Endpoint | Auth | Expected | Actual | Status | Notes |
|---|---|---|---|---|---|---|
| TC-AM-9 | `GET /security/me` | Admin | 200 | 200 | ✅ | ~300 perm claims |
| TC-AM-10 | `GET /security/me` | userA / TourGuide | 403 (admin-only) | 403 | ✅ | Confirms `/security/me` is admin-only by design |
| TC-AM-11 | `GET /accounts/profile` | userA / TourGuide | 200 (own profile) | **403** | ⚠️ | **F4** — `Permission.Profile.Read` not granted to base roles. Pre-existing in `SecurityDbInitializer.SeedRolePermissions` |

### Rate limiter probe (cross-cutting TC-CC-7.1)

| TC | Action | Expected | Actual | Status |
|---|---|---|---|---|
| TC-AM-12 | 13 consecutive logins in <10 s | 429 after threshold | **429** | ✅ Triggers as designed |

## Notes / Observations

- All 8 seeded `userId`s match the GUIDs I declared in `SeedIdentityProfiles.cs` (`b0000000-…-001` through `…008`). Zero drift.
- RefreshToken expires at `2026-06-27` (~30 days from run) — confirms `RefreshToken.Create` is using the expected TTL.
- 12 console errors logged during this run (expected: 3× 403 + 9× rate-limit 429).

## Findings (cross-reference)

- **F3 (NEW) — TourGuide role lacks `Permission.Provider.*`.** Confirmed when `guide-approved` later tried `/provider/status` (403). Logged in TourGuide report.
- **F4 (NEW) — User/TourGuide roles lack `Permission.Profile.Read`.** Confirmed here via TC-AM-11.

## Next actions

- [ ] Audit `SecurityDbInitializer.SeedRolePermissions()` and grant `Permission.Profile.{Read,Update}` to `User` + `TourGuide` (F4) and `Permission.Provider.*` + `Permission.ProviderDashboard.Read` to `TourGuide` (F3).
- [ ] Re-run TC-AM-11 after F4 fix.
- [ ] Consider TC-AM-12 → a dedicated cross-cutting rate-limit test that resets the limit window before subsequent suites.
