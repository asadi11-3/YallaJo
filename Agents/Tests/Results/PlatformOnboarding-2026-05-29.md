# Test Run — Platform-Onboarding

| Field | Value |
|---|---|
| **Date** | 2026-05-29 |
| **Runner** | Playwright MCP |
| **Scenario source** | [`../Playwright-PlatformOnboarding.md`](../Playwright-PlatformOnboarding.md) (74 scenarios) |
| **Environment** | API `https://localhost:57065` |
| **Auth context** | Admin (`b0000000-…-001`) |
| **Seeder under test** | `Accounts.Infrastructure\Persistence\Seeding\AccountsProviderApplicationSeeder.cs` (Order=51, written this session) |

## Summary

| Total | PASS | FAIL | SKIP | Notes |
|---:|---:|---:|---:|---|
| 5 | **4** | 0 | 0 | + 1 finding |

**Primary objective:** verify the 4 `ProviderApplication` rows seeded by my new `AccountsProviderApplicationSeeder` are persisted and queryable. **All 4 rows present, all fields exact-match.** Idempotency proven by `totalCount=4` exactly (no duplicates).

## Scenarios

| TC | Endpoint | Auth | Expected | Actual | Status |
|---|---|---|---|---|---|
| TC-PO-1 | `GET /api/v1/admin/providers?page=1&pageSize=50` | Admin | 200 + 4 items | 200, `totalCount=4` | ✅ |
| TC-PO-2 | Row 1 verification | Admin | `userId=…004, type=1 (IndependentGuide), status=1 (Pending), businessName='Pending Guide Co.'` | exact match | ✅ |
| TC-PO-3 | Row 2 verification | Admin | `userId=…005, type=1 (IndependentGuide), status=3 (Approved), businessName='Approved Guide Co.'` | exact match | ✅ |
| TC-PO-4 | Row 3 verification | Admin | `userId=…006, type=5 (BusinessOwner), status=3 (Approved), businessName='Test Business LLC'` | exact match | ✅ |
| TC-PO-5 | Row 4 verification | Admin | `userId=…007, type=4 (Agency), status=3 (Approved), businessName='Test Travel Agency'` | exact match | ✅ |

### Negative path (surfaced a real bug)

| TC | Endpoint | Auth | Expected | Actual | Status |
|---|---|---|---|---|---|
| TC-PO-6 | `GET /api/v1/admin/providers` (no `?page`) | Admin | 400 with field-level validation error | **500** with `exception` field leaking stack trace + absolute path `C:\Users\admin1\source\repos\YallaJo\YallaJo.Api\Middleware\SeoRedirectMiddleware.cs:line 26` | ❌ → **F2 + F5** |

## Notes / Observations

- **State-machine invariant proves documents seeded correctly.** `ProviderApplication.Submit()` refuses to transition to Pending unless `HasAllRequiredDocuments() == true`. All 4 reached Pending or Approved → required documents (4 / 3 / 4 / 5 per type) were attached by my seeder via `AddDocument()` calls.
- **Enum verification:** `ProviderType { TourOperator=0, IndependentGuide=1, HotelResort=2, ActivityCenter=3, Agency=4, BusinessOwner=5 }` and `Status { Draft=0, Pending=1, MoreDocsNeeded=2, Approved=3, Rejected=4, Suspended=5 }`. These confirm the long-standing **4-vs-6 provider taxonomy divergence** (PDF says 4 types, code has 6).
- **Idempotency** proven — re-running the API does not duplicate rows.
- List endpoint response shape: `{ items, totalCount, page, pageSize }`. Documents NOT included in list response (counts only via detail endpoint, not yet probed).

## Findings (cross-reference)

- **F2 (NEW) — Information disclosure on 500.** TC-PO-6 reproduces.
- **F5 (NEW) — `?page` missing → 500 instead of 400.** Same TC-PO-6.

## Next actions

- [ ] Probe `GET /api/v1/admin/providers/{id}` for per-application detail (documents, cooling period, history).
- [ ] Probe `POST /api/v1/admin/providers/{id}/{approve,reject,request-docs,suspend,reinstate}` action endpoints once a Pending row is available (currently only `guide-pending`).
- [ ] Re-run TC-PO-6 after F5 fix.
