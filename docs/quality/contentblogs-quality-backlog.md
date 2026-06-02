# ContentBlogs — Quality Review Backlog

**Module:** ContentBlogs
**Source:** Full Module Quality Review (discovery-only pass) + targeted fixes
**Purpose:** Track the outcome of the ContentBlogs full quality review. This pass records only the findings that have already been resolved. Remaining ContentBlogs findings will be evaluated and added as active backlog entries separately.

> Scope note: No active backlog entries are recorded yet — by direction, remaining findings (e.g. missing `Status` indexes, leading-wildcard search, integration-test gap, `GetActive*` naming, and the deferred `UserId`/`LinkedProviderId` DTO-exposure decision) are intentionally not listed here and will be evaluated in a later pass.

---

## Resolved in this pass

* **CB-01 — Public creator profile exposed Suspended creators (and the soft-delete-only gate).**
  Public, anonymous creator-facing slug lookups now require `CreatorProfileStatus.Active`. `GetCreatorProfileBySlugQueryHandler` and `GetCreatorBlogsBySlugQueryHandler` return `CreatorProfile.NotFound` / `Outcome.NotFound` for non-Active profiles, so a Suspended profile is indistinguishable from a non-existent one (no existence/status leak). Deactivated profiles remain hidden by the existing soft-delete query filter. The gate does not rely on soft-delete alone. `UserId`/`LinkedProviderId` DTO exposure was deliberately left unchanged (separate review). Covered by `tests/ContentBlogs.Tests.Unit/Application/CreatorProfileVisibilityQueryTests.cs`.

* **CB-04 — `Blog.IsFeatured` used in server-side `IQueryable` predicates (relational translation failure).**
  `Blog.IsFeatured` is `[NotMapped]` (ignored in `BlogConfiguration`) and cannot be translated by relational providers, so the previous predicates would throw `InvalidOperationException` ("could not be translated") on SQL Server. The featured logic is now expressed over the mapped `FeaturedAt`/`FeaturedUntil` columns with a captured `now`:
    * `BlogRepository.GetFeaturedBlogInPlaceScopeAsync` — inlined translatable predicate.
    * `ListBlogsQueryHandler` — featured filter built conditionally (`null` → no featured term; `true` → featured expression; `false` → inverse), and the projection no longer references `Blog.IsFeatured`. The untranslatable property never appears in any `IQueryable` filter or projection.
  `public bool Blog.IsFeatured` is unchanged and still used for in-memory/DTO/domain logic. New relational (SQLite in-memory) coverage proves the predicates translate and execute without throwing, including expired/indefinite feature semantics: `tests/ContentBlogs.Tests.Unit/Infrastructure/FeaturedBlogRelationalQueryTests.cs`. Existing InMemory tests were kept.
