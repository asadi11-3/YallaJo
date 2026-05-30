# Test Run — ContentCore

| Field | Value |
|---|---|
| **Date** | 2026-05-29 |
| **Runner** | Playwright MCP |
| **Scenario source** | [`../Playwright-ContentCore.md`](../Playwright-ContentCore.md) (65 scenarios) |
| **Environment** | API `https://localhost:57065` |
| **Auth context** | Admin (`b0000000-…-001`) |
| **Module route prefix** | `/api/v1/content-core/` (NOT `/content/`, NOT `/categories/`) — discovered this run, update adapter §4 |

## Summary

| Total | PASS | FAIL | SKIP | Notes |
|---:|---:|---:|---:|---|
| 6 | **2** | 2 | 2 | + 2 findings |

Categories + tags read endpoints work end-to-end (translations + seeding). Three other entities (attachments, entity-tags, images, entity-images) either 500 (server bug) or 404 (endpoint not exposed). Likely the per-entity endpoints are admin-detail / NOT_BUILT.

## Scenarios

| TC | Endpoint | Auth | Expected | Actual | Status | Evidence |
|---|---|---|---|---|---|---|
| TC-CC-1 | `GET /content-core/categories?page=1&pageSize=20` | Admin | 200 + seeded categories | 200 — 3 categories: Adventure, Historical, Culinary | ✅ | `isActive=true` × 3, each with 3 translations (ar / es / en). `sortOrder` 1,2,3. Flat hierarchy (no children seeded). NB: response is a flat array, `page`/`pageSize` query params ignored for this endpoint. |
| TC-CC-2 | `GET /content-core/tags?page=1&pageSize=20` | Admin | 200 + seeded tags | 200 — 6 tags: budget, eco, family-friendly, … | ✅ | All `isActive=true`. Translations empty. |
| TC-CC-3 | `GET /content-core/attachments?page=1&pageSize=10` | Admin | 200 (paged) | **500** with `exception` field | ❌ → **F2 + F5** | Same query-binding-or-handler 500 leak. |
| TC-CC-4 | `GET /content-core/entity-tags?page=1&pageSize=10` | Admin | 200 (paged) | **500** with `exception` field | ❌ → **F2 + F5** | Same pattern. |
| TC-CC-5 | `GET /content-core/images?page=1&pageSize=10` | Admin | 200 or 405 | **404** | ⏭ NOT_BUILT? | Endpoint not registered. May be intentional (admin-only via per-entity images). |
| TC-CC-6 | `GET /content-core/entity-images?page=1&pageSize=10` | Admin | 200 or 405 | **404** | ⏭ NOT_BUILT? | Same. |

## Notes / Observations

- **Route prefix discovery cost 10 probes.** Tried `/categories`, `/content`, `/cms`, `/core`, etc. before hitting `/content-core/categories`. Adapter §4 should list the actual `/content-core/` prefix to save other runs. **Action item below.**
- Categories endpoint **ignores `page`/`pageSize`** and returns an unpaged flat array — design choice (categories are small finite set) but worth noting in the per-module scenario file.
- i18n is wired: each category carries translations for ar / es / en. Proves `ContentCore` localisation seeding works.
- 6 tags + 3 categories is plausible "smoke seed" data — likely from a `ContentCoreDbInitializer` that I haven't read yet.

## Findings (cross-reference)

- **F2 confirmed (3rd instance) — Information disclosure on 500.**
- **F5 confirmed (3rd instance) — `?page` query binding crashes 500.**

## Next actions

- [ ] Update `../Playwright-APIOnly-Adapter.md` §4 endpoint map to list `/api/v1/content-core/{categories,tags,attachments,entity-tags}` and explicitly note `images`/`entity-images` aren't exposed.
- [ ] Read `ContentCore.Infrastructure\Persistence\Seeding\ContentCoreDbInitializer.cs` to confirm seed content + plan deeper assertions in next run.
- [ ] Probe `POST /content-core/categories` (create) once admin has `Permission.Category.Create` — likely 201 + new id.
- [ ] Wait for F5 fix before re-running TC-CC-3 / TC-CC-4.
