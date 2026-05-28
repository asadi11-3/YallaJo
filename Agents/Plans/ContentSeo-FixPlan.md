# ContentSeo Module — Fix Plan

> **Created**: 2025-01-27
> **Based on**: `ContentSeo-Audit-Report.md`
> **Effort**: ~1-2 hours (all doc/minor fixes)
> **Build gate**: Fix 8

---

## Design Decisions

1. All fixes are LOW priority — module is already the cleanest in the codebase
2. Dead permissions: keep in catalog but add comment explaining they're for future endpoint auth gates
3. Plan document corrections are factual updates only, no architectural changes

---

## Fixes

### Fix 1 (LOW) — Update plan doc endpoint count
- **File**: `ContentSeo-Workflow.md` line 32
- **Change**: "17 endpoints" → "23 endpoints (5 FaqItem + 4 Redirect + 3 SeoMetadata + 6 Sitemap + 5 Weather)"

### Fix 2 (LOW) — Update plan doc handler count
- **File**: `ContentSeo-Workflow.md` line 33
- **Change**: "14 integration event handlers" → "21 (14 IntegrationEventHandler + 7 SeoHandler)"

### Fix 3 (LOW) — Update plan doc dead permissions count
- **File**: `ContentSeo-Workflow.md` line 39
- **Change**: "7 dead permissions" → "4 dead permissions (SeoMetadata.Read, SeoMetadata.Delete, FaqItem.Read, Weather.Read)"

### Fix 4 (LOW) — Mark SeoEntityType as already expanded
- **File**: `ContentSeo-Workflow.md` line 31
- **Change**: Add "TourGuide=4, Creator=5" to current state, mark Decision #1 as DONE

### Fix 5 (LOW) — Mark resolved gaps
- **File**: `ContentSeo-Workflow.md` lines 40-43
- **Changes**: Mark gaps 2, 3, 5 as RESOLVED:
  - Gap 2: MaxHops already 3
  - Gap 3: NoOpWeatherProvider no longer throws
  - Gap 5: No bare `throw;` found

### Fix 6 (LOW) — Mark already-built features
- **File**: `ContentSeo-Workflow.md`
- **Changes**: Mark as ALREADY BUILT:
  - SchemaMarkupMerger (exists, used by ReviewAggregateUpdatedSeoHandler)
  - WeatherApiComProvider (exists, conditional DI registration)

### Fix 7 (LOW) — Update plan status
- **File**: `ContentSeo-Workflow.md` line 4
- **Change**: Status from "Plan" to "Implemented (audited 2025-01-27)"

### Fix 8 (LOW) — Build verify
- No code changes expected — doc-only fixes

---

## Execution Order

All fixes are independent, all target same file. Can be done in single pass.

---

## NOT In Scope

- Removing dead permissions (intentionally kept for future auth gates)
- Adding test coverage (Gap 4, deferred)
- Module already at 9.0/10 — no code changes needed
