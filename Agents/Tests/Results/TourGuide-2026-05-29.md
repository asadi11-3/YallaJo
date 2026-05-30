# Test Run — TourGuide

| Field | Value |
|---|---|
| **Date** | 2026-05-29 |
| **Runner** | Playwright MCP |
| **Scenario source** | [`../Playwright-TourGuide.md`](../Playwright-TourGuide.md) (59 scenarios) |
| **Environment** | API `https://localhost:57065` |
| **Auth context** | `guide-approved@yallajo.test` Bearer (`b0000000-…-005`) |
| **Code project** | `ContentTours.*` (NOT `TourGuide.*` — workflow plans use legacy name) |

## Summary

| Total | PASS | FAIL | SKIP | Notes |
|---:|---:|---:|---:|---|
| 7 | **0** | 7 | 0 | All 7 fail — but the failures point at one **HIGH** finding (F1), not 7 separate bugs. |

**Headline finding:** the `ProviderApplication.Approve()` integration event has **no handler** in `ContentTours.Application`. After admin approves a guide application, the corresponding `TourGuide` aggregate is never created in the ContentTours bounded context, so every `/guides/me/*` endpoint 404s. Verified via:

```
ast_grep + ctx_search for "ProviderApplicationApproved" across ContentTours.Application
→ 0 matches in 276 files
```

This is a **cross-module integration gap**, not a seeder problem. My `AccountsProviderApplicationSeeder` correctly created Approved ProviderApplication rows; the next link in the chain (Accounts → ContentTours `TourGuide.CreateFromApprovedApplication(...)`) is **NOT_BUILT**.

## Scenarios

| TC | Endpoint | Auth | Expected | Actual | Status |
|---|---|---|---|---|---|
| TC-TG-1 | `GET /guides/me` | guide-approved | 200 + guide profile | **404** `TourGuide.NotFound` `Tour guide 'b0000000-…-005' was not found.` | ❌ → **F1** |
| TC-TG-2 | `GET /guides/me/tier` | guide-approved | 200 | **404** `Tour guide not found.` | ❌ → **F1** |
| TC-TG-3 | `GET /guides/me/availability-blocks` | guide-approved | 200 (empty array OK) | **404** `Tour guide not found.` | ❌ → **F1** |
| TC-TG-4 | `GET /guides/me/applications` | guide-approved | 200 | **500** with `exception` (same `?page` bug) | ❌ → **F2 + F5** |
| TC-TG-5 | `GET /provider/status` | guide-approved | 200 + `Approved` | **403** Forbidden | ❌ → **F3** |
| TC-TG-6 | `GET /provider/dashboard` | guide-approved | 200 | **403** Forbidden | ❌ → **F3** |
| TC-TG-7 | `GET /provider/documents` | guide-approved | 200 + list of 4 docs | **405** Method Not Allowed | ❌ | Endpoint likely POST-only. Probe with the right verb in next run. |

## Notes / Observations

- The 404s on TC-TG-1/2/3 all carry the same `TourGuide.NotFound` problem-doc type. The Accounts-side proof is rock-solid (`/admin/providers` returns the row with `status=3 Approved`), so the gap is unambiguously downstream.
- Code search command and result:
  ```
  ast_grep_search lang=csharp paths=[ContentTours.Application]
    pattern: class $NAME : INotificationHandler<ProviderApplicationApproved$$$> { $$$ }
  → No matches
  ctx_search ProviderApplicationApproved in ContentTours.Application → 0 matches in 276 files
  ```
- TC-TG-5/6 (403) and TC-TG-4 (500) are independent issues from the integration gap — they would still fail even if F1 were fixed, because the role lacks the right claims (F3) and the endpoint crashes on missing `?page` (F5).
- TC-TG-7 (405) suggests the route exists but only accepts POST; can't fully diagnose without checking the endpoint definition.

## Findings (cross-reference)

- **F1 (NEW, HIGH) — `ContentTours` has no `ProviderApplicationApproved` handler.** Suggested fix: add `ProviderApplicationApprovedHandler : INotificationHandler<ProviderApplicationApproved>` in `ContentTours.Application` that resolves the `TourGuide` aggregate, calls a `TourGuide.CreateFromApprovedApplication(providerApplicationId, userId, providerType, businessName, …)` factory, and persists it via the `IContentToursRepository`.
- **F2 confirmed (4th instance).** TC-TG-4.
- **F3 (NEW, MED) — `TourGuide` role lacks `Permission.Provider.*` claims.** TC-TG-5, TC-TG-6.
- **F5 confirmed (4th instance).** TC-TG-4.

## Next actions

- [ ] Implement F1. Then re-run TC-TG-1/2/3 — expect 200.
- [ ] Implement F3 fix in `SecurityDbInitializer.SeedRolePermissions`. Then re-run TC-TG-5/6.
- [ ] Re-discover TC-TG-7 verb (likely POST per `Permission.ProviderDocument.Upload` claim convention).
- [ ] After F1 fix, continue through the full 40 Built scenarios in [`../Playwright-TourGuide.md`](../Playwright-TourGuide.md).
