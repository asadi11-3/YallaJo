# Authorization-Cleanup — Final Acceptance Gate

> **Tech Lead signs off before declaring the sprint closed (Thu 2027-03-11 17:00).** Folder does NOT move to `Agents/decisions/closed/Authorization-Cleanup/` until every box ticked.

---

## 1. Code Quality (PR & build)

- [ ] All 4 task PRs (TASK 1..TASK 4) merged into `main`.
- [ ] `dotnet build` green for every project changed:
  - Auth.Presentation, Accounts.Presentation, Security.Presentation
  - ContentCore.Presentation, ContentPlaces.Presentation, ContentPlaces.Application
  - ContentSeo.Contracts, ContentSeo.Infrastructure
  - YallaJo.Api
  - tests/Authorization.IntegrationTests, tests/ContentPlaces.Tests.Unit, tests/Shared.Tests
- [ ] **Zero TODOs** in committed code: `rg "TODO|FIXME|HACK" Auth.Presentation Accounts.Presentation Security.Presentation ContentCore.Presentation ContentPlaces.Presentation ContentSeo.Contracts ContentSeo.Infrastructure YallaJo.Api/Middleware` → 0 matches.
- [ ] **No `RequireAuthorization()` bare** anywhere in Presentation projects (per §10 audit command).
- [ ] **No `RequireAuthorization("permission:...")` string policies** anywhere.
- [ ] **PW-3 sanity tests all GREEN** — was RED on sprint kickoff:
  - `Every_registered_endpoint_has_either_MustHavePermission_or_AllowAnonymous` — GREEN
  - `No_endpoint_uses_string_based_RequireAuthorization_with_permission_prefix` — GREEN
  - `Every_MustHavePermission_attribute_references_a_registered_permission` — GREEN
- [ ] **At least 30 unit + 15 integration tests added/updated** across all 4 tasks (8 handler tests × 2 cases + 28 endpoint metadata tests + 5 lookup-service + 6 middleware = ~50+).
- [ ] **TASK 4 load test runbook** stored in `Agents/decisions/closed/Authorization-Cleanup/_loadtest-runbook.md` — 1000 RPS sustained 60s; HitCount accurate to ±1% within 15s.

---

## 2. Endpoint Re-audit (manual run)

Reviewer (Tech Lead) runs PW-1 ast-grep commands again **after all PRs merged**. Expected counts:

| Metric | Pre-sprint | Post-sprint target |
|---|---|---|
| Bare `.RequireAuthorization()` calls | 14 | **0** |
| `.RequireAuthorization("permission:...")` string-policies | 14 | **0** |
| Endpoints with NO auth metadata (§8.2 MISSING_METADATA) | 4 | **0** |
| `ICurrentUser` injections in handler files matching IsAuthenticated checks | 8 | **0** |
| Total §8.x violations | 36 | **0** |

Any non-zero post-sprint count blocks sign-off. Reviewer files bug, owner fixes within 24h.

---

## 3. Functional Smoke Test (manual via Postman / HTTP REPL)

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | GET | `/auth/sessions` (with valid JWT, role lacks `AuthFeatures.Session.Read`) | 403 | TASK 2 — proves MustHavePermission gating |
| 2 | GET | `/auth/sessions` (with valid JWT, role has `AuthFeatures.Session.Read`) | 200 + list | TASK 2 |
| 3 | DELETE | `/auth/sessions/{id}` (with valid JWT) | 204 | TASK 2 |
| 4 | POST | `/auth/change-password` (without JWT) | 401 | TASK 2 — JWT middleware rejects before MustHavePermission |
| 5 | GET | `/profile` (with valid JWT) | 200 | TASK 2 |
| 6 | POST | `/categories` (with admin JWT) | 201 | TASK 3 ContentCore |
| 7 | POST | `/categories` (with regular-user JWT) | 403 | TASK 3 |
| 8 | GET | `/places/{id}/businesses` (no JWT) | 200 + list | TASK 3 (MISSING_METADATA → `.AllowAnonymous()`) |
| 9 | POST | `/admin/businesses/{id}/approve` (admin JWT) | 200, Business.Status = Approved, ApprovedByUserId stamped | TASK 1 ApproveBusiness |
| 10 | POST | `/admin/businesses/{id}/approve` (provider JWT, lacks Business.Approve) | 403 | TASK 1 |
| 11 | POST | `/businesses/{id}/amenities` (provider JWT, owns business) | 201, amenity added | TASK 1 AddBusinessAmenity happy |
| 12 | POST | `/businesses/{id}/amenities` (different-provider JWT, doesn't own) | 403 `Business.OwnerMismatch` | TASK 1 IDOR enforcement |
| 13 | GET | `/old-slug-from-blog` (TASK 4 SEO redirect row in DB → `/new-slug`) | 301 Location: /new-slug?{originalQuery} | TASK 4 happy path |
| 14 | GET | `/api/v1/places` (TASK 4 middleware skip) | 200 + list (no redirect lookup hit) | TASK 4 skip pattern |

**Any RED row blocks sign-off.**

---

## 4. Performance Sanity

Quick check (1 min k6 run per scenario):

| Endpoint | Scenario | p95 target |
|---|---|---|
| `GET /api/v1/places` | 100 req/sec for 60s | unchanged from baseline (middleware skips `/api/*`) |
| `GET /some-frontend-slug` (cache hit) | 100 req/sec for 60s | < 50ms (includes 5ms middleware overhead) |
| `GET /old-slug` (returns 301) | 100 req/sec for 60s | < 50ms |
| `POST /admin/businesses/{id}/approve` | 10 req/sec for 60s | < 250ms (unchanged from TASK 1 refactor) |

---

## 5. Documentation Hygiene

- [ ] **`agent-context.md §8.1` updated**: Replace violation list with `✅ All resolved in Authorization-Cleanup sprint 2027-03-12 — see Agents/decisions/closed/Authorization-Cleanup/`. Keep historical violation count for context.
- [ ] **`agent-context.md §8.2` updated**: same.
- [ ] **`agent-context.md §11.1 module status`**: Update each affected module's row to reflect cleanup (e.g. Auth ✅ Complete + cleanup PR link).
- [ ] **`agent-context.md §11.2 Middleware` table**: Add row for `SeoRedirectMiddleware`.
- [ ] **`Agents/permissions-inventory.md`**: Reflect any newly-added permissions from PW-2.
- [ ] **`Agents/error-log.md`**: Append any drift gotchas (e.g. permission-string-typo in `MustHavePermission(SomeFeature, Update)` where `Update` was misspelled).
- [ ] **Folder move**:
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Authorization-Cleanup" -Destination "Agents\decisions\closed\Authorization-Cleanup"
  ```
- [ ] **Master `Phase1-Phase2-Completion-INDEX.md` §1 row** updated to 🟢 status + link to closed/ path.
- [ ] **AGENTS.md (repo root)** — add line for `Middleware/SeoRedirectMiddleware.cs` ownership.

---

## 6. Phase 1 + Phase 2 Completion Declaration

This is the **last sprint** in the Phase 1 + Phase 2 program. After sign-off:

- [ ] **`agent-context.md §11.1`** module status overview — all 14 modules ✅ for Phase 1 + Phase 2 scope.
- [ ] **`Phase1-Phase2-Completion-INDEX.md`** marks the program complete:
  - All 6 sprint folders moved to `Agents/decisions/closed/`
  - Index header banner updated: "🎉 Phase 1 + Phase 2 COMPLETE on 2027-03-12 — see closed/ for sprint history. Phase 3 kickoff: TBD."
- [ ] **Tech Lead** schedules Phase 3 kickoff meeting (Phase 3 = Wave 7-8: Packaging, Subscriptions, Loyalty, Referrals, Disputes, Accessibility reviews).
- [ ] **Project demo** (program-wide) to stakeholders Mon 2027-03-15 — walkthrough of all 14 modules end-to-end with sample user/provider/admin journeys.

---

## 7. Sprint Retro (Fri 2027-03-12 11:00 AST)

15-min demo by Mohammad:
1. Show RED → GREEN transition of PW-3 sanity tests.
2. Show `/old-slug` → 301 → `/new-slug` redirect live in browser.
3. Show ContentPlaces handler with `ICurrentUser` removed + IDOR check working (one valid, one rejected).
4. Pull up master INDEX with all 6 sprint folders 🟢.

Retro doc lives at `Agents/decisions/closed/Authorization-Cleanup/_retro.md`:
- What went well (≥ 3 items — likely: PW-3 test-first approach caught regressions early)
- What hurt (≥ 3 items)
- Phase 1 + Phase 2 program-level retro (≥ 5 items): biggest wins, biggest pains, lessons for Phase 3 sprint structure

---

## 8. Sign-Off Block

| Role | Name | Date | Signature |
|---|---|---|---|
| TASK 1 owner | Mahmoud | _____ | _____ |
| TASK 2 owner | Fadwa | _____ | _____ |
| TASK 3 owner | Mohammad | _____ | _____ |
| TASK 4 owner | Mohammad | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |
| Product (sign-off on Phase 1+2 complete) | _____ | _____ | _____ |

**Once all signatures collected:**
1. Folder moves to closed/.
2. INDEX updated to mark program complete.
3. agent-context.md §8 marked resolved.
4. Phase 3 kickoff scheduled.
5. 🎉 **YallaJo Phase 1 + Phase 2 delivery complete.** 🎉
