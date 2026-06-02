# ContentTours — Quality Review Backlog

**Module:** ContentTours
**Source:** Full Module Quality Review (discovery-only pass)
**Purpose:** Record the accepted findings from the ContentTours full quality review so they can be scheduled and actioned in future phases. This file is a backlog record only — no application code, migrations, or existing docs were changed as part of creating it.

> Scope note: Only the accepted findings below are tracked as active backlog items (CT-01, CT-03, CT-04, CT-05, CT-08). Findings CT-06 and CT-07 are intentionally **not** tracked as active items (design choices / cosmetic). CT-02 is recorded separately under "Needs Business Rule Verification" and is **not** classified as a bug yet.

---

## Active Backlog Items

### CT-01 — ProviderSuspended batch suspends non-Approved tours → throws
- **Phase:** Domain Events / Reliability phase
- **Status:** Deferred
- **Priority:** High
- **Note:** One-line behavior fix; should ship with a regression test. Needs approval before implementation.
- **Summary:** `ProviderSuspendedSuspendToursHandler` selects tours with `CreatedByUserId == userId && SuspendedAt == null` and calls `Tour.Suspend(reason)` on each, but `Tour.Suspend` throws `InvalidOperationException` when `Status != Approved`. The batch filter does not constrain `Status == Approved` (the ContentPlaces peer handler does). When a suspended provider owns any Draft/Pending/Rejected/Archived tour, the handler throws mid-batch, the inbox message is never marked processed (retries indefinitely / poison message), and the provider's tours are not fully suspended.

### CT-03 — Missing `Status` index on Tours (highest-traffic public filter)
- **Phase:** Performance phase
- **Status:** Deferred
- **Priority:** Medium
- **Note:** Requires a migration — out of scope for discovery passes; schedule deliberately.
- **Summary:** `TourConfiguration` indexes `PlaceId`, `Name`, and a filtered unique `Slug` only. Every public read (`ListToursQueryHandler`, `SearchToursQueryHandler`) leads with `Status == Approved`. `TourGuide` has a `Status` index but `Tour` does not. Consider an index on `Status` (or composite `(Status, IsFeatured)` / `(Status, CreatedAt)` aligned to the default sort).

### CT-04 — Tour search uses leading-wildcard `Contains` + multi-roundtrip facets
- **Phase:** Performance phase
- **Status:** Deferred
- **Priority:** Medium
- **Note:** Known/documented v1 limitation — plan, do not hot-fix.
- **Summary:** `SearchToursQueryHandler` token filtering uses `Name.Contains(token) || Description.Contains(token)` → non-sargable SQL `LIKE '%token%'` (full scan), plus three round-trips per search (`CountAsync`, a facet query materializing up to ~5001 rows, and the page query). Acceptable at MVP scale; revisit with a full-text/search-index strategy and facet caching as the catalogue grows.

### CT-05 — No integration test project for ContentTours
- **Phase:** Test Coverage phase
- **Status:** Deferred
- **Priority:** Medium
- **Note:** Additive tests only.
- **Summary:** All 44 test files live under `tests/ContentTours.Tests.Unit`; there is no integration test project. Query filters, filtered unique indexes (`IX_Tours_Slug`, GuideTourOffering `(TourId, TourGuideId)`), cascade deletes (Tour → child collections), the proposal → Tour + Offering cross-aggregate transaction, and the CT-01 mixed-status provider-suspension path are not verified end-to-end. Prioritize the provider-suspension mixed-status scenario (CT-01) and the proposal-approval flow.

### CT-08 — `ListTours` translates names; `SearchTours` does not
- **Phase:** Architecture consistency
- **Status:** Deferred
- **Priority:** Low
- **Note:** Confirm intended behavior before any change.
- **Summary:** `ListToursQueryHandler` projects a translated `Name` via `TourTranslations` for the resolved language, while `SearchToursQueryHandler` projects the raw `Name` despite accepting a `LanguageCode` filter. Localized clients get translated names in list/detail but untranslated names in search — an inconsistent UX. If localization is expected in search, apply the same translation projection.

---

## Needs Business Rule Verification

> The item below is **not** classified as a bug yet. It requires confirmation of the intended business rule before any decision on whether/how to act.

### CT-02 — TourProposal approval creates Tour with placeholder location & unchecked slug
- **Phase:** Pending business-rule verification (Domain Events / Reliability candidate)
- **Status:** Needs verification
- **Priority:** To be determined after verification
- **Note:** Do not implement until the intended business rule is confirmed. Not classified as a bug yet.
- **Observations (from code, pending confirmation):**
  - `ApproveTourProposalCommandHandler` creates the Tour with `location: new Location(0m, 0m)`. `TourProposal` carries no coordinates, so guide-proposed tours are created at (0,0). It must be confirmed whether the intended design is to derive the location from the proposal's `PlaceId`, require coordinates at approval, or treat (0,0) as an accepted placeholder until the provider edits the draft.
  - The slug is built ad-hoc (`Title.ToLowerInvariant().Replace(" ", "-").Replace("'", "")` + short id) **without** the `IsSlugReservedAsync` check used by `CreateTour`. A collision raises `DbUpdateException` (unique `IX_Tours_Slug`), but only `DbUpdateConcurrencyException` is caught — so a collision currently surfaces as an unhandled 500 rather than a clean 409. Confirm the intended slug-uniqueness handling for the proposal-approval path.
- **Verification needed:** Confirm the intended source of location for guide-proposed tours and the intended slug-uniqueness/error-mapping behavior before reclassifying this item or scheduling any work.
