# ADR-005: SearchTours Compound Relevance Score Deferred to Future Search-Quality Task

**Date**: 2026-04-29
**Status**: Accepted (Tech-Lead-approved scope cut for ContentTours Task 3 sprint)
**Deciders**: Tech Lead, ContentTours Task 3 owner (Mohammad), audit reviewer
**Related**: PDF Task 3 / B2 (Search Relevance Scoring); ContentTours sprint audit P1 item #4

## Context

PDF Task 3 §B2 specifies a **compound relevance score** for `GET /api/v1/tours/search` when the caller supplies `?q=` text. The PDF formula multiplies and sums several components:

- name-match weight (×3)
- description-match weight (×1)
- rating component (×0.3)
- log(BookingCount + 1) popularity component
- recency component (newer tours weighted higher)
- plus deterministic tie-breakers

The current `SearchToursQueryHandler` (Mohammad's implementation) does **not** implement this formula. When `Sort` is `Relevance` (the default) the handler falls back to:

```csharp
filtered.OrderByDescending(t => t.BookingCount).ThenByDescending(t => t.CreatedAt)
```

i.e. popularity → recency. The handler comments this explicitly as "v1 proxy (no SQL scoring yet)".

The PDF (R-3 mitigation note) acknowledges that pagination-only / popularity fallback is acceptable while a proper scoring path is being designed. The audit (P1 #4) flagged this as a gap that needed either implementation or an explicit, documented scope cut.

## Decision

**Defer** the PDF B2 compound relevance score to a dedicated future search-quality task.

The current `SearchToursQueryHandler` keeps its v1 fallback sorting behaviour for `Sort == Relevance`:

- `OrderByDescending(t => t.BookingCount)`
- `.ThenByDescending(t => t.CreatedAt)`

All other `Sort` values (`PriceAsc`, `PriceDesc`, `RatingDesc`, `PopularityDesc`, `Newest`) and all filtering, faceting, pagination, and tokenization behaviour remain unchanged and functionally complete per PDF Task 3 §B1, §B3, §B4.

This is **accepted as a Tech-Lead-approved scope cut for this sprint.** The PDF relevance formula is **NOT** marked as implemented; it is marked as deferred.

## Rationale

- **Search quality is a discipline, not a feature.** A correct compound score requires deliberate design choices (component weights, normalisation, SQL-vs-application split, EF translatability of the `LOG(...)` term, tie-breaker stability under paging) that are out of scope for a Task-3 endpoint sprint.
- **The visible API surface is already correct.** Filters, facets, pagination, tokenization, "no q + no filters → 400" guard, and 4 of 5 explicit sort modes match the PDF exactly. End users searching by price, rating, popularity, or newest already get correct results.
- **Default sort is functional.** Relevance-default callers get popularity → recency, which is the same behaviour PDF R-3 names as the acceptable fallback.
- **No regression vs prior state.** Nothing was rolled back; the v1 fallback that shipped with Mohammad's branch is preserved verbatim.
- **No silent claim of completion.** Tracking this as a deferred item keeps it visible in audit, in this ADR, and in the work log.

## Scope of the future search-quality task

When picked up, the future task must implement:

1. **Name-match weight** — exact prefix > token prefix > token contains, weighted by token coverage; `×3` per PDF.
2. **Description-match weight** — token coverage, weighted `×1` per PDF.
3. **Rating component** — `AverageRating` weighted `×0.3` per PDF.
4. **Popularity component** — `LOG(BookingCount + 1)` per PDF; choose between SQL-side (provider-specific) and an in-process re-rank of the top-N candidate set.
5. **Recency component** — exponential decay or piecewise weighting of `CreatedAt` per PDF.
6. **Deterministic tie-breakers** — at minimum `Tour.Id ASC` so paging is stable across calls.
7. **Translation-aware name matching** — coordinate with the Suggest-side translation work (P1 #5, ADR-pending) so Arabic queries are scored against `TourTranslation.Name` not just `Tour.Name`.
8. **Cache implications** — the `SearchToursCacheKeys.Search(hash)` bucket may need to absorb the language id once translation-aware scoring is in.
9. **Tests** — the future task should ship explicit ranking-correctness tests (each component contributes; tie-breaker is deterministic; recency decay is monotonic) before claiming PDF B2 done.

## Consequences

- The `Sort.Relevance` path is now formally documented as a v1 fallback, not the PDF formula.
- Audit reports and the ContentTours team-task tracker should reference this ADR rather than re-flagging the gap each cycle.
- The handler comment that already says "Relevance + default: popularity as v1 proxy (no SQL scoring yet)" remains accurate; no code change accompanies this ADR.
- A future search-quality task is required before the PDF B2 commitment can be ticked. Until then, do not list it as complete in PR descriptions, release notes, or status reports.

## Alternatives considered

- **Implement the formula now in SQL.** Rejected for this sprint: requires `LOG()`, custom ranking, and EF translation work whose risk exceeds the audit window.
- **Implement it client-side after Take(N).** Rejected: would silently change pagination semantics (Skip/Take applied before scoring) and break the existing total-count contract.
- **Drop the Relevance sort option entirely.** Rejected: existing API contract advertises it; removing it would be a breaking change.

## Review trigger

Reopen this ADR when any of the following becomes true:

- A search-quality KPI is added to the project roadmap.
- Customer feedback flags poor relevance ordering.
- The ContentSeo or recommendations engine starts depending on the relevance score.
- A subsequent sprint explicitly schedules a "search relevance v2" task.
