# Test Run — Phase 0 Smoke

| Field | Value |
|---|---|
| **Date** | 2026-05-29 |
| **Runner** | Playwright MCP |
| **Scenario source** | [`../Playwright-APIOnly-Adapter.md`](../Playwright-APIOnly-Adapter.md) §0 + master index Phase 0 |
| **Environment** | API `https://localhost:57065` · Swagger `/swagger/index.html` |
| **Auth context** | Admin (`b0000000-0000-0000-0000-000000000001`) after TC-2 |

## Summary

| Total | PASS | FAIL | SKIP |
|---:|---:|---:|---:|
| 4 | **4** | 0 | 0 |

Phase 0 verifies the environment (API up, DB reachable, JWT pipeline working, suspended accounts blocked) before any module suite runs. **All 4 green** — environment is ready for full Phase A execution.

## Scenarios

| TC | Endpoint | Auth | Expected | Actual | Status | Evidence |
|---|---|---|---|---|---|---|
| TC-P0-1 | `GET /health` | none | 200 | **200** in 126 ms | ✅ | `status=Healthy`; checks: `sqlserver` 11ms, `azure-translator` 3ms, `outbox-dead-letters` 114ms (no dead letters), `booking-bg` 9ms (all background services ticked within SLA) |
| TC-P0-2 | `POST /auth/login` | `admin@yallajo.test` / `TestPass!23` | 200 + token + `userId` matches seed | **200** | ✅ | Response: `{ userId: 'b0000000-…-001', accessToken, refreshToken, refreshTokenExpiresAt }`. JWT decoded: `role=Admin`, ~300 `Permission` claims, `iss=YallaJo.Api`, `aud=YallaJo.Clients`, 60-min expiry. RefreshToken valid 30 days. |
| TC-P0-3 | `GET /security/me` | Admin Bearer | 200 + roles | **200** | ✅ | `{ userId: '…001', email: 'admin@yallajo.test', roles: ['Admin'], claims: [sid + ~300 Permission claims + 3 lowercase permissions] }`. Confirmed end-to-end: SeedIdentityProfiles + SecurityDbInitializer + AuthDbInitializer all wired. Adapter cheat-sheet originally pointed at `/auth/me` which 404s — corrected to `/security/me` (NB for future runs). |
| TC-P0-4 | `POST /auth/login` | `suspended@yallajo.test` | non-200 (login blocked) | **401** | ✅ | RFC 7807 `{ type: rfc9110#section-15.5.2, title: 'Auth.Unauthorized', status: 401, detail: 'This account is not currently active. Contact your administrator.', traceId }`. Proves `Status="Suspended"` branch in `SecurityDbInitializer.ApplyLifecycle` correctly applied `Activate() + MarkVerified() + Suspend()`, and `User.cs` state machine refuses login for Suspended accounts. |

## Notes / Observations

- **Console errors** at run end: 2 (one each from TC-3 retry and TC-4, both expected — these are negative-path tests producing non-2xx responses).
- The `/auth/me` 404 produces a clean RFC 7807 doc — proves error-pipeline wiring works for plain 404s (separately from the 500 info-disclosure issue logged as Finding F2).
- TraceId propagation works on both success and error responses.

## Findings

- **F-NEW (this run): `/auth/me` does not exist; current-user endpoint is `/security/me`.** Adapter master index was patched (`Playwright-Test-Scenarios.md:77`); the adapter doc itself uses `/security/me` already in §1.
- No new bugs surfaced by Phase 0.

## Next actions

- ✅ Proceed to Phase B §1 Auth Matrix (8-user login validation).
- ✅ Proceed to Phase A Platform-Onboarding (verifies seeded ProviderApplications).
