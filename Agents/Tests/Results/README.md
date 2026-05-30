# Test Results

Per-run reports from Playwright MCP test executions. **Every test run produces a report here.**

## Folder convention

- `{Phase|Module}-{YYYY-MM-DD}.md` — one file per run, dated.
- `_template.md` — copy this as the starting point for a new report.
- `Findings-Rolling.md` — rolling cross-run findings log (real bugs, NOT_BUILT gaps, infra issues). Updated after each run.
- `Gap-Report-{YYYY-MM-DD}.md` — unified batch-fix plan published after a discovery sweep.

## Report inventory

| Date | Subject | Result | File |
|---|---|---|---|
| 2026-05-29 | Phase 0 Smoke (4 tests) | ✅ 4/4 PASS | [Phase0-Smoke-2026-05-29.md](Phase0-Smoke-2026-05-29.md) |
| 2026-05-29 | Auth Matrix — 8 users | ✅ 8/8 expected (7×200, 1×401 suspended) | [AuthMatrix-2026-05-29.md](AuthMatrix-2026-05-29.md) |
| 2026-05-29 | Platform-Onboarding queue | ✅ 4/4 provider rows verified | [PlatformOnboarding-2026-05-29.md](PlatformOnboarding-2026-05-29.md) |
| 2026-05-29 | ContentCore | ⚠️ 2/4 PASS + 2× 500 + 2× 404 | [ContentCore-2026-05-29.md](ContentCore-2026-05-29.md) |
| 2026-05-29 | TourGuide | ❌ Cross-module gap NOT_BUILT (F1) | [TourGuide-2026-05-29.md](TourGuide-2026-05-29.md) |
| 2026-05-29 | Multi-module gap sweep (8 modules) | ⚠️ 8 new findings logged (F6-F13) | (see Findings-Rolling.md F6-F13) |
| 2026-05-29 | **Unified Gap Report** | 13 findings · 5 PR groups · ~10-16h fix | [**Gap-Report-2026-05-29.md**](Gap-Report-2026-05-29.md) |

## Rolling open findings

See [Findings-Rolling.md](Findings-Rolling.md) for the canonical issue list with full reproduce + fix guidance. Quick summary:

| # | Severity | Title |
|---|---|---|
| F1 | HIGH | ContentTours has no `ProviderApplicationApproved` handler → TourGuide aggregate never auto-created |
| F2 | HIGH | 500 responses leak stack trace + absolute file paths (info disclosure) |
| F3 | MED | TourGuide role lacks `Permission.Provider.*` claims |
| F4 | MED | User/TourGuide roles lack `Permission.Profile.Read` |
| F5 | MED | `?page` missing crashes 500 instead of 400 (query-binding) |
| F6 | MED | `/places/businesses` shadowed by `/places/{slug}` route ordering |
| F7 | MED | Domain `NotReady` errors return 500 instead of 503 |
| F8 | LOW | Booking paged response missing `totalCount` (envelope inconsistent) |
| F9 | HIGH | User role completely blocked from consumer booking workflow (Booking.Create/Read 403) |
| F10 | HIGH | SignalR hub negotiate endpoints return 404 (real-time broken) |
| F11 | HIGH | Several "Built" features in workflows are actually 404 (wishlist, support-tickets, blogs/creators) |
| F12 | MED | Endpoints return 405 on GET (recommendations, disputes, faq) |
| F13 | HIGH | Unified: missing required query param = 500 with full leak (rolls up F2+F5) |

## How to use

**After every Playwright run:**
1. Copy `_template.md` to `{Subject}-{YYYY-MM-DD}.md`.
2. Fill scenarios with `TC-id | endpoint | auth | expected | actual | status | evidence`.
3. Add new findings to `Findings-Rolling.md` (don't duplicate existing).
4. Add a row to the inventory table above.
5. Commit.

**After a discovery sweep / before a batch-fix PR:**
1. Update `Findings-Rolling.md` with the final F# count + severity tally.
2. Write a new `Gap-Report-{date}.md` with prioritized commit groups.
3. Cross-reference both from this README.

**Status legend:** ✅ PASS · ❌ FAIL · ⏭ SKIP (NOT_BUILT / DEFERRED) · ⚠️ PARTIAL.
