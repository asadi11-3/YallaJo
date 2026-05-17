# Social Module — Combined Sprint Task File

> **Sprint window:** Mon 2026-10-19 → Fri 2026-11-27 (6 weeks, 30 working days, 130 person-hours)
> **Combined from 10 separate files** in `Agents/tasks/Social/` for single-file review.

---

## Table of Contents

- [00-README](#00-readme)
- [01-pre-work](#01-pre-work)
- [02-critical-rules](#02-critical-rules)
- [03-entities-matrix](#03-entities-matrix)
- [04-task-reviews](#04-task-reviews)
- [05-task-favorites](#05-task-favorites)
- [06-task-reports-moderation](#06-task-reports-moderation)
- [07-task-background-services](#07-task-background-services)
- [08-cross-cutting](#08-cross-cutting)
- [99-acceptance-gate](#99-acceptance-gate)

---

<a id="00-readme"></a>

## 00-README

> Source: `Social/00-README.md`

# Social Module Sprint — Wave 6

> **Predecessor sprint:** Finance (Wave 5 — closed Thu 2026-10-15 after Booking)
> **This sprint covers:** Wave 6 Social slice — **Reviews** (verified-booking gate, weighted Bayesian rating, 5-report auto-hide), **Favorites** (toggle, max 500/user, FavoriteAddedIntegrationEvent for Discount notifications), **Reports** (5 reasons + auto-action), **Content Moderation Logs**, plus 1 BG service (`OrphanedFavoritesCleanupService` weekly Sat 03:00 UTC) and a shared `RatingRecalculationService` (daily 03:00 UTC, owned by Social → publishes integration events Analytics + ContentTours/ContentPlaces consume).
> **Difficulty vs Finance:** ⚙️⚙️ (2/5) — no money, no escrow, no gateway integration. The complexity is in the Bayesian rating math, the verified-booking eligibility chain (Booking inbox), and the profanity / NSFW pipeline.
> **Endpoint count:** **22 HTTP endpoints + 1 BG service + 1 RatingRecalculationService**
> **Working-day estimate:** **30 working days × 4 devs ≈ 130 person-hours** (6 weeks)

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Notes |
|---|---|---|
| Pre-work cut | Fri 2026-10-16 17:00 | All PW-1..PW-7 stubs (see `01-pre-work.md`) |
| Kickoff | Mon 2026-10-19 09:30 AST | Standup format same as Finance |
| PW merge deadline | Tue 2026-10-20 17:00 | Tech Lead applies migration `SocialAddAggregateRootAndAuditMembers` |
| Earliest task start | Wed 2026-10-21 09:00 | TASK 1 (Reviews) begins |
| Mid-sprint integration freeze | Sun 2026-11-15 17:00 | No new commits to public contracts |
| Hard PR cutoff | Wed 2026-11-25 17:00 | Merge cutoff |
| Sprint retro + demo | Fri 2026-11-27 11:00 AST | Folder moves to `Agents/decisions/closed/Social/` |

Working week Sun→Thu (5 days). Standup 09:30 AST 15 min hard cap.

---

## 1. Working Days & Person-Hour Budget

| Metric | Value |
|---|---|
| Working days | 30 |
| Hours/day/dev | 6 |
| Devs | 4 |
| Total hours | 720 |
| Task hours | 130 |
| Review hours | 20 |
| Ceremony hours | 15 |
| Buffer / unplanned | 555 (other projects + part-time on this sprint) |

This sprint runs in parallel with Messaging/Analytics if staffing allows — 4 devs are NOT fully dedicated to Social. Reviewer rotation: Mohammad reviews Mahmoud's TASK 1, Fadwa reviews Mohammad's TASK 4.

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG | Est hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mahmoud** | Intermediate | T1 Reviews CRUD + provider reply + edit window | 8 | 0 | 38h | Sun 2026-11-08 |
| **Mohammad** | Intermediate | T4 Reports + ContentModerationLog + admin moderation queue | 6 | 0 | 28h | Sun 2026-11-22 |
| **Fadwa** | Beginner | T2 Favorites + FavoriteAddedIntegrationEvent + T3 OrphanedFavoritesCleanup BG | 4 | 1 | 24h | Sun 2026-11-15 |
| **Mohammad** (lead) | Intermediate | T5 RatingRecalculationService + Bayesian formula + cross-module events | 0 | 1 | 24h | Sun 2026-11-22 |
| **Tech Lead** | Senior | PW + reviews + content moderation policy doc | – | – | 16h | – |

---

## 3. Endpoints Inventory (22)

### TASK 1 — Reviews (Mahmoud, 8 endpoints)
- `POST /api/v1/reviews` — Submit review (verified-booking eligibility check via Booking snapshot)
- `PUT /api/v1/reviews/{id}` — Edit own review within 48h window
- `DELETE /api/v1/reviews/{id}` — Soft-delete own review
- `POST /api/v1/reviews/{id}/reply` — Provider replies (must own target tour/place/business)
- `PUT /api/v1/reviews/{id}/reply/{replyId}` — Edit own reply (provider, unlimited window)
- `DELETE /api/v1/reviews/{id}/reply/{replyId}` — Soft-delete own reply
- `GET /api/v1/reviews/my-reviews` — Current user's reviews (cursor pagination, includes deleted=false)
- `GET /api/v1/reviews/admin/flagged` — Admin moderation queue (auto-hidden reviews)

### TASK 2 — Favorites (Fadwa, 4 endpoints)
- `POST /api/v1/favorites` — Add (entityType ∈ {Tour, Place, Business}, max 500 total)
- `DELETE /api/v1/favorites/{entityType}/{entityId}` — Remove
- `GET /api/v1/favorites` — List own favorites (cursor pagination, filter by entityType)
- `GET /api/v1/favorites/check/{entityType}/{entityId}` — Boolean check (cheap, used by frontend cards)

### TASK 4 — Reports + Moderation (Mohammad, 6 endpoints)
- `POST /api/v1/reports` — Submit report (5 reasons: Spam, Inappropriate, Misleading, Harassment, FakeReview, Other)
- `POST /api/v1/reviews/{id}/report` — Convenience alias for reports on reviews
- `POST /api/v1/reports/admin/{id}/resolve` — Admin resolves report (action: Dismiss, RemoveContent, WarnUser, BanUser)
- `POST /api/v1/reviews/admin/{id}/approve` — Restore a 5-report auto-hidden review
- `POST /api/v1/reviews/admin/{id}/remove` — Permanently remove a flagged review
- `GET /api/v1/reports/admin` — Admin reports list (filter by reason, status, entityType, dateRange)
- `GET /api/v1/moderation/logs` — Admin audit log of all moderation actions (cursor pagination)

### TASK 3 — BG: OrphanedFavoritesCleanupService (Fadwa, 0 endpoints)
- Weekly Sat 03:00 UTC. Removes Favorites pointing to soft-deleted entities (queries inbox snapshots from ContentPlaces/ContentTours).

### TASK 5 — BG: RatingRecalculationService (Mohammad, 0 endpoints)
- Daily 03:00 UTC. Recalculates `AverageRating` + `ReviewCount` for every Tour/Place/Business with non-trivial review counts. Emits `social.rating.recalculated.v1` integration events. ContentTours/ContentPlaces inbox handlers update their `AverageRating` / `ReviewCount` columns (denormalized read-path).

---

## 4. Integration Events Emitted (5)

| Logical name | Source | Used by |
|---|---|---|
| `social.review.published.v1` | Review.MarkPublished | Analytics (UserInteraction.ReviewSubmitted), Messaging (provider notification "New review on your tour") |
| `social.review.deleted.v1` | Review.Delete | Analytics (audit log), ContentTours/ContentPlaces (rating recalc trigger) |
| `social.favorite.added.v1` | Favorite.Add | Messaging (subscribe to discount notifications on this entity), Analytics (UserInteraction.AddToFavorite) |
| `social.report.resolved.v1` | Report.Resolve | Analytics (audit log), Messaging (notify reporter of outcome) |
| `social.rating.recalculated.v1` | RatingRecalculationService | ContentTours.InboxHandler (updates Tour.AverageRating + Tour.ReviewCount); ContentPlaces.InboxHandler (Place / Business equivalents) |

## 5. Integration Events Consumed (4)

| Logical name | Source module | Consumer |
|---|---|---|
| `booking.tour-booking.completed.v1` | Booking | `SocialBookingCompletedHandler` writes `social.BookingEligibilitySnapshot` row (verified-booking gate for `POST /reviews`) |
| `content-places.place.deleted.v1` | ContentPlaces | Triggers OrphanedFavoritesCleanupService backlog flag |
| `content-places.business.deleted.v1` | ContentPlaces | Same |
| `content-tours.tour.deleted.v1` | ContentTours | Same |

---

## 6. Out of Scope

- **AccessibilityReviews** (separate Phase 3 feature 14 from PDF 2 §14 — "Were features accurate?" Yes/No flow). Deferred to Phase 3 sprint.
- **Notification dispatching** — Social only emits integration events; Messaging sprint owns SignalR + email queue.
- **NSFW ML classification** — placeholder `INsfwClassifier` interface registered with `AlwaysSafeClassifier` stub. Real Azure Content Moderator integration deferred to Phase 4.
- **Profanity blocklist editor UI** — admin only edits via direct DB this sprint; UI in Phase 3.
- **Review photo upload** — deferred to Phase 3. This sprint accepts text-only reviews. The `ReviewPhotos` table + endpoint planned in T1's "future hooks" section, not built.

---

## 7. File Manifest (10 files)

| # | File | Purpose | Status |
|---|---|---|---|
| 1 | `00-README.md` | This file | this |
| 2 | `01-pre-work.md` | PW-1..PW-7 | next |
| 3 | `02-critical-rules.md` | S-R1..S-R10 | next |
| 4 | `03-entities-matrix.md` | Aggregates + events + migrations | next |
| 5 | `04-task-reviews.md` | T1 — Mahmoud | next |
| 6 | `05-task-favorites.md` | T2 — Fadwa | next |
| 7 | `06-task-reports-moderation.md` | T4 — Mohammad | next |
| 8 | `07-task-background-services.md` | T3 OrphanedFavorites + T5 RatingRecalc | next |
| 9 | `08-cross-cutting.md` | DI + permission + outbox parity + migrations + hygiene | next |
| 10 | `99-acceptance-gate.md` | Final sign-off | next |

When sprint closes, move whole folder to `Agents/decisions/closed/Social/`.

---

<a id="01-pre-work"></a>

## 01-pre-work

> Source: `Social/01-pre-work.md`

# Social — Pre-Work (PW-1..PW-7)

> Hard deadline: **Tue 2026-10-20 17:00**. Tech Lead applies migration `SocialAddAggregateRootAndAuditMembers` immediately after merge so all task PRs can branch from a clean migration baseline. **Kickoff for task work is Wed 2026-10-21 09:00.**

---

## PW-1 — `SocialUnitOfWork` delegate to SharedKernel `IUnitOfWork<SocialDbContext>`

**Same bug class** as ContentBlogs / ContentSeo / ContentTours / Booking / Finance. The current `SocialUnitOfWork` (if it exists at all — Social may still be empty per `agent-context.md` §11.1) calls `context.SaveChangesAsync` directly without dispatching domain events through MediatR.

**Required fix:**
```csharp
internal sealed class SocialUnitOfWork(
    SocialDbContext dbContext,
    IUnitOfWork<SocialDbContext> innerUnitOfWork)
    : ISocialUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => innerUnitOfWork.SaveChangesAsync(ct);
}
```

**Acceptance gate:**
1. `tests/Social.Tests.Unit/Persistence/SocialUnitOfWorkDispatchesEventsTests` — green. Two tests:
   - Single Review entity with a `ReviewPublishedDomainEvent` → MediatR.Publish invoked once.
   - Aggregate roots only (Review, Favorite, Report aggregates) dispatch; non-aggregates (e.g. ContentModerationLog) do NOT dispatch.

---

## PW-2 — IAggregateRoot markers + AuditableEntity upgrades

| Entity | Currently | Becomes | Reason |
|---|---|---|---|
| `Review` | (likely) BaseEntity | `AuditableEntity, IAggregateRoot` | Raises ReviewPublished/Edited/Deleted events; owner of provider replies child collection |
| `Favorite` | BaseEntity | `AuditableEntity, IAggregateRoot` | Raises FavoriteAddedDomainEvent (consumed by Messaging for discount notifications) |
| `Report` | BaseEntity | `AuditableEntity, IAggregateRoot` | Raises ReportResolvedDomainEvent + tracks moderation lifecycle |
| `ContentModerationLog` | BaseEntity | stays BaseEntity (audit row, never updated) | No business invariants beyond being append-only |
| `AccessibilityReview` | BaseEntity | **stays BaseEntity** + marked `[Obsolete]` in this sprint | Deferred to Phase 3; do NOT promote |

**Migration name:** `SocialAddAggregateRootAndAuditMembers`. Adds `IsDeleted bit NOT NULL DEFAULT 0`, `DeletedAt datetime2 NULL`, `RowVersion rowversion NOT NULL` to Review, Favorite, Report. Adds `UpdatedAt datetime2 NULL`, `UpdatedByUserId Guid NULL` columns where not already present.

**Acceptance gate:** `Social.Tests.Unit/Domain/AggregateMarkersTests` asserts each in-scope aggregate implements `IAggregateRoot` AND inherits `AuditableEntity`. Reflection-based parametric test.

---

## PW-3 — Seed 12 domain event records in `Social.Domain/Events/`

```csharp
// Reviews
public sealed record ReviewPublishedDomainEvent(Guid ReviewId, Guid UserId, ReviewTargetType TargetType, Guid TargetId, decimal Rating, DateTime PublishedAt) : IDomainEvent;
public sealed record ReviewEditedDomainEvent(Guid ReviewId, Guid UserId, decimal OldRating, decimal NewRating, DateTime EditedAt) : IDomainEvent;
public sealed record ReviewDeletedDomainEvent(Guid ReviewId, Guid UserId, ReviewTargetType TargetType, Guid TargetId, DateTime DeletedAt, ReviewDeletionSource Source) : IDomainEvent;
public sealed record ReviewAutoHiddenDomainEvent(Guid ReviewId, ReviewTargetType TargetType, Guid TargetId, int ReportCount, DateTime AutoHiddenAt) : IDomainEvent;
public sealed record ReviewRestoredDomainEvent(Guid ReviewId, Guid RestoredByUserId, DateTime RestoredAt) : IDomainEvent;
public sealed record ReviewReplyAddedDomainEvent(Guid ReviewId, Guid ReplyId, Guid ProviderUserId, DateTime AddedAt) : IDomainEvent;

// Favorites
public sealed record FavoriteAddedDomainEvent(Guid FavoriteId, Guid UserId, FavoriteEntityType EntityType, Guid EntityId, DateTime AddedAt) : IDomainEvent;
public sealed record FavoriteRemovedDomainEvent(Guid FavoriteId, Guid UserId, FavoriteEntityType EntityType, Guid EntityId, DateTime RemovedAt) : IDomainEvent;

// Reports / Moderation
public sealed record ReportSubmittedDomainEvent(Guid ReportId, Guid ReporterUserId, ReportableEntityType EntityType, Guid EntityId, ReportReason Reason, DateTime SubmittedAt) : IDomainEvent;
public sealed record ReportResolvedDomainEvent(Guid ReportId, Guid AdminUserId, ModerationAction Action, DateTime ResolvedAt) : IDomainEvent;
public sealed record EntityAutoActionedDomainEvent(ReportableEntityType EntityType, Guid EntityId, int ReportCount, DateTime ActionedAt) : IDomainEvent;

// Rating recalculation
public sealed record EntityRatingRecalculatedDomainEvent(ReviewTargetType TargetType, Guid TargetId, decimal NewAverageRating, int NewReviewCount, DateTime RecalculatedAt) : IDomainEvent;
```

New enums to add: `ReviewDeletionSource { User, Admin, System }`, `FavoriteEntityType { Tour, Place, Business }` (separate from `ReviewTargetType` because reviews ALSO target TourGuide but favorites don't), `ReportableEntityType { Review, Tour, Place, Business, Blog }`.

**Acceptance gate:** all 12 records compile in `Social.Domain.csproj`. No imports from Infrastructure.

---

## PW-4 — Seed 5 integration event records in `Social.Contracts/IntegrationEvents/` + register

```csharp
public sealed record ReviewPublishedIntegrationEvent(Guid Id, Guid ReviewId, Guid UserId, string TargetType, Guid TargetId, decimal Rating, DateTime PublishedAt) : IIntegrationEvent;
public sealed record ReviewDeletedIntegrationEvent(Guid Id, Guid ReviewId, Guid UserId, string TargetType, Guid TargetId, DateTime DeletedAt, string Source) : IIntegrationEvent;
public sealed record FavoriteAddedIntegrationEvent(Guid Id, Guid FavoriteId, Guid UserId, string EntityType, Guid EntityId, DateTime AddedAt) : IIntegrationEvent;
public sealed record ReportResolvedIntegrationEvent(Guid Id, Guid ReportId, Guid AdminUserId, string Action, DateTime ResolvedAt) : IIntegrationEvent;
public sealed record RatingRecalculatedIntegrationEvent(Guid Id, string TargetType, Guid TargetId, decimal NewAverageRating, int NewReviewCount, DateTime RecalculatedAt) : IIntegrationEvent;
```

Register in `IntegrationEventTypeRegistry`:
```
social.review.published.v1   → ReviewPublishedIntegrationEvent
social.review.deleted.v1     → ReviewDeletedIntegrationEvent
social.favorite.added.v1     → FavoriteAddedIntegrationEvent
social.report.resolved.v1    → ReportResolvedIntegrationEvent
social.rating.recalculated.v1 → RatingRecalculatedIntegrationEvent
```

**Acceptance gate:** `Social.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests` (same pattern as Booking/Finance — reverse-parity reflection over assembly).

---

## PW-5 — Seed 6 repository interfaces

```csharp
public interface IReviewRepository : IReadRepository<Review, Guid>, IWriteRepository<Review, Guid>
{
    Task<IReadOnlyList<Review>> GetForUserAsync(Guid userId, ReviewFilter filter, int take, ReviewCursor? cursor, CancellationToken ct);
    Task<IReadOnlyList<Review>> GetByTargetAsync(ReviewTargetType targetType, Guid targetId, int take, ReviewCursor? cursor, CancellationToken ct);
    Task<IReadOnlyList<Review>> GetFlaggedAsync(int take, ReviewCursor? cursor, CancellationToken ct);   // status == AutoHidden
    Task<int> CountReportsForReviewAsync(Guid reviewId, CancellationToken ct);
    Task<bool> UserHasReviewedAsync(Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct);
}

public interface IFavoriteRepository : IReadRepository<Favorite, Guid>, IWriteRepository<Favorite, Guid>
{
    Task<bool> ExistsForUserAsync(Guid userId, FavoriteEntityType entityType, Guid entityId, CancellationToken ct);
    Task<Favorite?> FindForUserAsync(Guid userId, FavoriteEntityType entityType, Guid entityId, CancellationToken ct);
    Task<int> CountForUserAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Favorite>> GetForUserAsync(Guid userId, FavoriteEntityType? filter, int take, FavoriteCursor? cursor, CancellationToken ct);
    Task<IReadOnlyList<Guid>> FindOrphanedAsync(FavoriteEntityType entityType, int batchSize, CancellationToken ct);   // OrphanedFavoritesCleanupService query
}

public interface IReportRepository : IReadRepository<Report, Guid>, IWriteRepository<Report, Guid>
{
    Task<int> CountActiveReportsAsync(ReportableEntityType entityType, Guid entityId, CancellationToken ct);
    Task<IReadOnlyList<Report>> GetForAdminAsync(ReportFilter filter, int take, ReportCursor? cursor, CancellationToken ct);
    Task<bool> UserAlreadyReportedAsync(Guid userId, ReportableEntityType entityType, Guid entityId, CancellationToken ct);
}

public interface IContentModerationLogRepository : IReadRepository<ContentModerationLog, Guid>, IWriteRepository<ContentModerationLog, Guid>
{
    Task<IReadOnlyList<ContentModerationLog>> GetForAdminAsync(ModerationLogFilter filter, int take, ModerationLogCursor? cursor, CancellationToken ct);
}

public interface IBookingEligibilitySnapshotRepository
{
    Task<bool> UserHasCompletedBookingAsync(Guid userId, ReviewTargetType targetType, Guid targetId, CancellationToken ct);
    Task UpsertAsync(BookingEligibilitySnapshot snapshot, CancellationToken ct);
}

public interface IEntityRatingCacheRepository : IReadRepository<EntityRatingCache, Guid>, IWriteRepository<EntityRatingCache, Guid>
{
    Task<EntityRatingCache?> GetAsync(ReviewTargetType targetType, Guid targetId, CancellationToken ct);
    Task<IReadOnlyList<(ReviewTargetType, Guid)>> GetStaleAsync(DateTime olderThan, int take, CancellationToken ct);   // RatingRecalculationService query
}
```

`BookingEligibilitySnapshot` is a small read-projection (PK = composite `(UserId, TargetType, TargetId)`) populated by the inbox handler for `booking.tour-booking.completed.v1`. Stores: `UserId, TargetType, TargetId, FirstCompletedAt, LastCompletedAt, CompletedBookingCount`. Used by `POST /reviews` "verified booking required" check + by review's `IsVerifiedBooking` badge flag.

`EntityRatingCache` is a denormalized per-entity rating row written by `RatingRecalculationService`. Schema: `(TargetType, TargetId)` composite PK, `AverageRating decimal(3,2)`, `ReviewCount int`, `BayesianScore decimal(5,4)`, `LastRecalculatedAt datetime2`.

**Acceptance gate:** all 6 interfaces compile; concrete `EfXxxRepository<T>` skeletons exist with method bodies stubbed `throw new NotImplementedException()` (filled per-task).

---

## PW-6 — `IProfanityFilter` + `INsfwClassifier` interfaces + stub implementations

`SharedKernel.Application/Abstractions/Moderation/IProfanityFilter.cs`:
```csharp
public interface IProfanityFilter
{
    /// <summary>Returns true if text contains profanity from the blocklist.</summary>
    Task<bool> ContainsProfanityAsync(string text, string languageCode, CancellationToken ct);

    /// <summary>Returns the cleaned text with profanity replaced by asterisks. Used for soft-display contexts.</summary>
    Task<string> CleanAsync(string text, string languageCode, CancellationToken ct);
}

public interface INsfwClassifier
{
    /// <summary>Returns a 0-1 confidence score; >= 0.7 = NSFW.</summary>
    Task<NsfwResult> ClassifyAsync(Stream imageStream, CancellationToken ct);
}

public sealed record NsfwResult(decimal Score, string Label, IReadOnlyDictionary<string, decimal> Categories);
```

**Stub implementations in `Social.Infrastructure/Moderation/`:**
- `BlocklistProfanityFilter` — loads `social.ProfanityBlocklist` table (seeded with ~50 common English + Arabic words during PW-2 migration as `ProfanityBlocklistInitialSeed`). Case-insensitive whole-word match. `CleanAsync` uses regex replace.
- `AlwaysSafeNsfwClassifier` — returns `NsfwResult(0.0m, "safe", Empty)`. Image classification deferred to Phase 4.

Registered in `Social.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<IProfanityFilter, BlocklistProfanityFilter>();
services.AddSingleton<INsfwClassifier, AlwaysSafeNsfwClassifier>();
```

**Acceptance gate:** `Social.Tests.Unit/Moderation/BlocklistProfanityFilterTests` — 6 tests (clean text passes; obvious profanity flagged; partial-word does NOT flag; case-insensitive; Arabic words flagged; whitespace boundaries respected).

---

## PW-7 — `SocialFeatures` + `SocialPermissionCatalog` (18 perms)

```csharp
// Social.Contracts/Authorization/SocialFeatures.cs
public static class SocialFeatures
{
    public const string Review              = nameof(Review);
    public const string ReviewReply         = nameof(ReviewReply);
    public const string Favorite            = nameof(Favorite);
    public const string Report              = nameof(Report);
    public const string ContentModerationLog = nameof(ContentModerationLog);
    public const string AdminModerationQueue = nameof(AdminModerationQueue);
}
```

`SocialPermissionCatalog` enumerates 18 permissions:
- Review × {Create, Read, Update, Delete} = 4 (Update + Delete are self-only enforced via `ICurrentUser`)
- ReviewReply × {Create, Update, Delete} = 3 (provider-owned)
- Favorite × {Create, Read, Delete} = 3
- Report × {Create, Read} = 2
- ContentModerationLog × {Read} = 1 (admin-only)
- AdminModerationQueue × {Read, Approve, Remove, Resolve} = 4 (Approve = restore auto-hidden review; Remove = permanent removal; Resolve = close report ticket)
- AdminModerationQueue × {Warn, Ban} = 2 (user-targeted moderation actions — last 2)

Total = **19** (I miscounted, double-check during PW review).

Expected boot log:
```
[INFO] PermissionSeeder discovered 9 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social
[INFO] PermissionSeeder inserted/verified 19 Social permissions
```

**Note**: `AppAction.Warn` and `AppAction.Ban` are new actions. If they don't exist in `SharedKernel.Domain/Authorization/AppAction.cs`, add them in PW-7 PR. Standardize naming.

**Acceptance gate:** integration test boots full host, queries `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Social.%'` and asserts exactly 19.

---

## PW-Done Checklist (Tech Lead signs Tue 2026-10-20)

- [ ] PW-1 UoW fix + tests green
- [ ] PW-2 5 aggregates marked + 1 migration applied dev/staging
- [ ] PW-3 12 domain event records compile
- [ ] PW-4 5 integration event records registered + parity test green
- [ ] PW-5 6 repo interfaces stubbed + skeleton impls compile
- [ ] PW-6 IProfanityFilter + INsfwClassifier + stubs + 6 unit tests green
- [ ] PW-7 19 permissions appear in security.Permissions on boot
- [ ] **Pre-work merge commit** tagged `social/pre-work` for easy rollback

---

<a id="02-critical-rules"></a>

## 02-critical-rules

> Source: `Social/02-critical-rules.md`

# Social — Critical Rules (S-R1..S-R10)

> Additive to `Phase1-Phase2-Completion-INDEX.md` §4 (16 universal rules). Every PR in this sprint that violates one of these is blocked at review.

---

## S-R1 — Verified-Booking Gate (Reviews)

Per **PDF 2 §1.6**: "ANYONE can leave review (no booking required), VERIFIED badge for completed bookings, REVIEW WINDOW 30 DAYS from completion."

**Implementation:** `POST /reviews` does NOT reject non-buyers. Instead:

1. Look up `IBookingEligibilitySnapshotRepository.UserHasCompletedBookingAsync(userId, targetType, targetId, ct)`.
2. If true AND `now - LastCompletedAt <= 30 days` → review.IsVerifiedBooking = true (badge shown on frontend).
3. If false OR > 30 days → review.IsVerifiedBooking = false (no badge, but submission allowed).

**Why no hard reject:** PDF 2 §1.6 explicitly allows non-verified reviews. They get weighted 0.5 in the Bayesian score (S-R6).

**Edge case:** review window starts from `LastCompletedAt`, not `FirstCompletedAt`. A user who books the same tour twice has the window reset on each completion.

---

## S-R2 — One Review Per User Per Target

Hard uniqueness. `IReviewRepository.UserHasReviewedAsync(userId, targetType, targetId, ct)` checks BEFORE create. Database also enforces:

```sql
CREATE UNIQUE INDEX UX_Reviews_UserTarget
  ON social.Reviews(UserId, TargetType, TargetId)
  WHERE IsDeleted = 0;
```

`IsDeleted = 0` clause means a soft-deleted review allows the user to submit a fresh one (PDF 2 §1.6 edge case: "deleted review allows new submission — soft deletes don't count").

Race condition: two concurrent POSTs from same user → second hits `DbUpdateException` from the unique index → catch in Infrastructure only (INDEX §4 R12) → return `Error("Review.AlreadyExists", "...") Outcome.Conflict`.

---

## S-R3 — 48-Hour Edit Window

`PUT /reviews/{id}` rejects with `Error("Review.EditWindowExpired", "Reviews can only be edited within 48 hours of posting.") Outcome.UnprocessableEntity` when `now - CreatedAt > TimeSpan.FromHours(48)`.

**Boundary handling:** `<= 48` accepted, `> 48` rejected. Use `>` not `>=`. Test cases include 47h59m59s (accept), 48h00m00s (accept, last-millisecond), 48h00m00.001s (reject).

**Window is based on `CreatedAt` NOT `UpdatedAt`**. Multiple edits within 48h all allowed, but the 49th-hour edit is rejected even if previous edit was at 47h.

Soft-deletion is NOT bound by 48h — user can delete own review anytime.

Provider replies (`PUT /reviews/{id}/reply/{replyId}`) are UNLIMITED edit window per PDF 2 §1.6 — no time check.

---

## S-R4 — Profanity Filter Lifecycle

All user-generated text (review content, review title, review reply content, report description, ticket message) goes through `IProfanityFilter.ContainsProfanityAsync` BEFORE publication.

**Behavior on profanity hit:**
- Reviews: `Status = AwaitingModeration`, NOT visible publicly. Admin appears in moderation queue.
- Review replies: same.
- Reports: never auto-flagged for profanity — they're admin-only by design.

**Editing a published review triggers a re-scan.** If the edit introduces profanity, status transitions back to `AwaitingModeration` and `ReviewEditedDomainEvent` payload includes `ProfanityFlagged = true` for downstream auditing.

**Profanity scan is `Task.Run`'d off the request thread for latency-sensitive endpoints** — NO. The scan IS in-request because BlocklistProfanityFilter is sub-millisecond. Only when we swap to a real ML classifier (Phase 4) do we move scanning to a background channel + delayed publish.

---

## S-R5 — 5-Report Auto-Hide (Reviews only)

PDF 2 §1.6: "5 unique reports → auto-hide+flag admin".

**Implementation:** `POST /reviews/{id}/report` handler:
1. Validate report (PW-6 rules + reason enum).
2. Insert Report row.
3. Re-count active reports for the review via `IReportRepository.CountActiveReportsAsync(EntityType.Review, reviewId, ct)`.
4. If count >= 5: load Review aggregate, call `review.AutoHide(reportCount)` which transitions Status → `AutoHidden` AND raises `ReviewAutoHiddenDomainEvent` (PW-3). Idempotent — if already AutoHidden, no-op.
5. SaveChanges.

Auto-hidden reviews are invisible to public, NOT counted in `RatingRecalculationService`, but ARE visible to admin queue (`GET /reviews/admin/flagged`).

Admin actions:
- `POST /reviews/admin/{id}/approve` — restore via `review.Restore(adminUserId)` (raises `ReviewRestoredDomainEvent`). Resets report count? **No** — reports stay, but auto-hide threshold becomes inactive until ANOTHER 5 new reports arrive.
- `POST /reviews/admin/{id}/remove` — permanent removal via `review.AdminDelete(adminUserId)` (raises `ReviewDeletedDomainEvent` with `Source = Admin`). Tombstone.

**Uniqueness of reports:** `IReportRepository.UserAlreadyReportedAsync` prevents a user from reporting same review twice. Database unique index `UX_Reports_UserEntity (UserId, EntityType, EntityId) WHERE Status = Open` enforces.

---

## S-R6 — Bayesian Rating Algorithm

Per **PDF 2 §18**:

```
weight_i = VerificationWeight × RecencyWeight

VerificationWeight:
  Verified booking → 1.0
  Non-verified     → 0.5

RecencyWeight:
  < 90 days   → 1.0
  90-180 days → 0.7
  > 180 days  → 0.5

AverageRating = Σ(rating_i × weight_i) / Σ(weight_i)

BayesianScore = (ReviewCount × AvgRating + C × GlobalAverage) / (ReviewCount + C)
  where C = 10 (confidence threshold, admin-configurable)
  GlobalAverage = avg of all tour ratings across platform (refreshed daily by RatingRecalculationService)
```

**Display rules:**
- `MIN 3 REVIEWS` before AverageRating is shown publicly (PDF 2 §1.6 + §18). `EntityRatingCache.ReviewCount < 3` → API omits AverageRating field, frontend shows "Not enough reviews yet".
- Bands per PDF 2 §18: 4.5-5.0 Top Rated badge, 4.0-4.49 Popular section, 3.0-3.99 standard, 2.0-2.99 deprioritized, <2.0 admin flagged after 90 days.

**Computation:** `RatingRecalculationService` (T5) runs daily at 03:00 UTC, scans every Tour/Place/Business with ≥ 1 non-deleted review created/updated in last 24h OR not yet scored, recomputes both `AverageRating` and `BayesianScore`, persists to `EntityRatingCache`, raises `EntityRatingRecalculatedDomainEvent` → outbox `social.rating.recalculated.v1` → consumed by ContentTours / ContentPlaces inbox handlers.

**Why store BOTH AverageRating and BayesianScore:** ratings (visible to user) and ranking signal (used by search / discovery) are different. Search uses BayesianScore so brand-new entities don't dominate.

---

## S-R7 — Favorites Limit + Toggle Semantics

PDF 2 §1.7: "MAX 500 per user, toggle once (re-click removes)".

**POST /favorites** is idempotent toggle ONLY in the sense that it gracefully handles "already favorited":
- Already favorited → `Error("Favorite.AlreadyExists", ...) Outcome.Conflict` (409). Frontend should call DELETE instead.
- Over 500 limit → `Error("Favorite.LimitExceeded", "Maximum 500 favorites allowed.") Outcome.UnprocessableEntity` (422).
- Otherwise create Favorite + raise `FavoriteAddedDomainEvent` + outbox `social.favorite.added.v1`.

Limit check is racy — two parallel POSTs from same user at count=499 can both pass the count check. Acceptable: PDF 2 §1.7 says "no max limit" originally but PDF 1 Wave 6 specifies 500. We enforce best-effort.

**`DELETE /favorites/{entityType}/{entityId}`** uses entity reference (not Favorite ID) for frontend simplicity. Handler looks up via `IFavoriteRepository.FindForUserAsync` and `favorite.Remove()` raises `FavoriteRemovedDomainEvent`. Idempotent: if not found, return 204 anyway.

**No FavoriteRemovedIntegrationEvent in this sprint** — Messaging only needs additions (for discount notifications). Future: add `social.favorite.removed.v1` if needed.

---

## S-R8 — ICurrentUser Discipline

Per INDEX §4 R2 ("ICurrentUser ONLY for self/ownership comparisons"). Audit list for Social handlers:

| Handler | Why ICurrentUser is needed |
|---|---|
| CreateReviewCommandHandler | Stamps UserId on new Review |
| UpdateReviewCommandHandler | Compares review.UserId to currentUser.UserId (ownership) |
| DeleteReviewCommandHandler | Same |
| AddReviewReplyCommandHandler | Verifies provider owns the target tour (via BookingProviderSnapshot lookup OR ContentTours snapshot — TBD in T1) |
| UpdateReviewReplyCommandHandler | Owner-only |
| DeleteReviewReplyCommandHandler | Owner-only |
| AddFavoriteCommandHandler | Stamps UserId |
| RemoveFavoriteCommandHandler | Self-only |
| GetMyReviewsQueryHandler | Self-filter |
| GetMyFavoritesQueryHandler | Self-filter |
| CheckFavoriteQueryHandler | Self-only |
| SubmitReportCommandHandler | Stamps ReporterUserId |

All OTHER handlers (admin queries, RatingRecalculationService, etc.) should NOT inject `ICurrentUser`. Admin endpoints are protected by `MustHavePermission(AdminModerationQueue.Approve)` and the admin user identity comes from `HttpContext.User` in the endpoint, passed explicitly as `adminUserId` into the command.

---

## S-R9 — Cursor Pagination + Cache Strategy

Same cursor envelope as Booking/Finance: `{ items, nextCursor (base64 opaque), totalCount? }`. PageSize clamped [1, 50], default 20.

**Cache tags (per INDEX §4 R10 — most specific):**

| Endpoint | Cache key | Tag(s) | TTL |
|---|---|---|---|
| GET /favorites | `favorites:user:{userId}:filter:{entityTypeOrAll}:cursor:{cursor}:take:{n}` | `favorites:user:{userId}` | 1 min |
| GET /favorites/check/{type}/{id} | `favorite-check:user:{userId}:type:{type}:id:{id}` | `favorites:user:{userId}` | 30 sec |
| GET /my-reviews | `reviews:user:{userId}:cursor:{cursor}:take:{n}` | `reviews:user:{userId}` | 1 min |
| GET /reviews/admin/flagged | `reviews:admin:flagged:cursor:{cursor}:take:{n}` | `reviews:admin` | 30 sec |
| GET /reports/admin | `reports:admin:filter:{hash}:cursor:{cursor}:take:{n}` | `reports:admin` | 30 sec |
| GET /moderation/logs | `moderation-logs:admin:filter:{hash}:cursor:{cursor}:take:{n}` | `moderation-logs` | 1 min |

**Invalidation rules** (every command MUST call `RemoveByTagAsync(tag, ct)` AFTER `SaveChangesAsync`):

| Command | Tags evicted |
|---|---|
| Create/Update/Delete Review | `reviews:user:{userId}`, `reviews:admin`, `entity-rating:{targetType}:{targetId}` |
| Auto-hide review | `reviews:admin`, `entity-rating:{type}:{id}` |
| Add/Remove Favorite | `favorites:user:{userId}` |
| Submit Report | `reports:admin`, `moderation-logs` |
| Resolve Report | `reports:admin`, `moderation-logs`, `reviews:admin` |

---

## S-R10 — Error Code Registry

Domain errors use `{Entity}.{Reason}` PascalCase (INDEX §4 R13). Outcome mapping:

```
Review.NotFound                   → NotFound (404)
Review.AlreadyExists              → Conflict (409)
Review.OwnerMismatch              → Forbidden (403)
Review.EditWindowExpired          → UnprocessableEntity (422)
Review.InvalidRating              → ValidationError (400)
Review.ContentTooLong             → ValidationError (400)
Review.ContentTooShort            → ValidationError (400)
Review.AutoHidden                 → Forbidden (403) (cannot edit auto-hidden)
Review.ProfanityDetected          → UnprocessableEntity (422) — but the review IS submitted (Status=AwaitingModeration), this code is returned for transparency
ReviewReply.NotFound              → NotFound (404)
ReviewReply.OwnerMismatch         → Forbidden (403)
ReviewReply.NotOwnerOfTargetEntity → Forbidden (403) (provider tried to reply to review on a tour they don't own)
ReviewReply.ContentTooLong        → ValidationError (400)

Favorite.AlreadyExists            → Conflict (409)
Favorite.NotFound                 → NotFound (404)
Favorite.LimitExceeded            → UnprocessableEntity (422)
Favorite.InvalidEntityType        → ValidationError (400)
Favorite.OwnerMismatch            → Forbidden (403)

Report.NotFound                   → NotFound (404)
Report.AlreadyReported            → Conflict (409)
Report.InvalidReason              → ValidationError (400)
Report.DescriptionTooLong         → ValidationError (400)
Report.TargetNotReportable        → ValidationError (400)
Report.AlreadyResolved            → Conflict (409)

Moderation.ActionNotAllowed       → UnprocessableEntity (422) (e.g. trying to Ban for a Spam report — policy)
Moderation.ReviewNotAutoHidden    → UnprocessableEntity (422) (approve called on a non-hidden review)
Moderation.InvalidAction          → ValidationError (400)

RatingRecalc.NoReviewsFound       → NotFound (404) — only used by manual admin trigger

BookingEligibility.NotFound       → NotFound (404) — internal, never exposed to user; verified flag just becomes false
```

Total ≈ 27 codes. Final list locked at T1 PR review.

---

<a id="03-entities-matrix"></a>

## 03-entities-matrix

> Source: `Social/03-entities-matrix.md`

# Social — Entity Ownership Matrix

> Status legend: 🟢 in-scope aggregate, 🔵 owned/child entity (BaseEntity), 🟡 deferred to a future sprint, ⚪ external read-snapshot populated by inbox.

---

## 1. Aggregates In Scope (PW-2)

| File (existing) | Base class | Aggregate? | Domain events raised | Owner | Task |
|---|---|---|---|---|---|
| `Social.Domain/Entities/Review.cs` | `AuditableEntity, IAggregateRoot` | 🟢 yes | ReviewPublished, ReviewEdited, ReviewDeleted, ReviewAutoHidden, ReviewRestored, ReviewReplyAdded | Mahmoud | T1 |
| `Social.Domain/Entities/Favorite.cs` | `AuditableEntity, IAggregateRoot` | 🟢 yes | FavoriteAdded, FavoriteRemoved | Fadwa | T2 |
| `Social.Domain/Entities/Report.cs` | `AuditableEntity, IAggregateRoot` | 🟢 yes | ReportSubmitted, ReportResolved, EntityAutoActioned (raised when 5-report threshold hits) | Mohammad | T4 |

## 2. Child / Junction / Audit Entities (stay BaseEntity)

| File | Base class | Reason | Owner |
|---|---|---|---|
| `Social.Domain/Entities/ReviewReply.cs` (NEW) | BaseEntity | Owned by Review aggregate; replies don't have independent lifecycle | Mahmoud (T1) |
| `Social.Domain/Entities/ContentModerationLog.cs` | BaseEntity | Append-only audit row; no business invariants | Mohammad (T4) |
| `Social.Domain/Entities/ProfanityBlocklistEntry.cs` (NEW) | BaseEntity | Reference data (admin-managed); part of Moderation domain | Tech Lead (PW-6) |
| `Social.Domain/Entities/BookingEligibilitySnapshot.cs` (NEW) | BaseEntity | Read-snapshot ingested from booking inbox | Tech Lead (PW-5) |
| `Social.Domain/Entities/EntityRatingCache.cs` (NEW) | BaseEntity | Denormalized per-entity rating snapshot written by RatingRecalculationService | Mohammad (T5) |

## 3. Deferred (stub, do not implement this sprint)

| File | Reason |
|---|---|
| `Social.Domain/Entities/AccessibilityReview.cs` | Phase 3 feature 14. Annotate with `[Obsolete("Deferred to Phase 3 — see YallaJo.md §Wave 6 backlog")]` to prevent accidental usage. |

## 4. External Read-Snapshots Consumed (no Social ownership)

| Snapshot | Source integration event | Used by |
|---|---|---|
| `social.BookingEligibilitySnapshots` | `booking.tour-booking.completed.v1` | T1 verified-booking gate |
| `social.PlaceSnapshots` (PK = PlaceId) | `content-places.place.created.v1`, `.updated.v1`, `.deleted.v1` | T3 OrphanedFavoritesCleanupService + Favorite entity-type validation |
| `social.BusinessSnapshots` (PK = BusinessId) | `content-places.business.created.v1`, `.updated.v1`, `.deleted.v1` | Same |
| `social.TourSnapshots` (PK = TourId) | `content-tours.tour.published.v1`, `.updated.v1`, `.deleted.v1` | Same + provider lookup for reply ownership check |

---

## 5. Cross-Module Dependencies

| Module | Depends on | Reason |
|---|---|---|
| Social → Booking | `booking.tour-booking.completed.v1` integration event consumed | Populates BookingEligibilitySnapshot for verified-booking gate (S-R1) |
| Social → ContentPlaces | 6 integration events (place + business CRUD) | Maintains PlaceSnapshots + BusinessSnapshots; validates Favorite + Report target entity exists |
| Social → ContentTours | 4 integration events (tour CRUD) | Maintains TourSnapshots; resolves provider ownership for review replies |
| ContentTours → Social | `social.rating.recalculated.v1` integration event consumed | Updates Tour.AverageRating + Tour.ReviewCount denormalized columns |
| ContentPlaces → Social | `social.rating.recalculated.v1` integration event consumed | Updates Place.AverageRating + Business.AverageRating |
| Messaging → Social | `social.favorite.added.v1` integration event consumed | Subscribes user to discount notifications on the entity (PDF 2 §1.7) |
| Messaging → Social | `social.review.published.v1` integration event consumed | Sends "New review on your tour" notification to provider |
| Messaging → Social | `social.report.resolved.v1` integration event consumed | Notifies reporter of outcome |
| Analytics → Social | 3 integration events consumed | UserInteraction logging |

---

## 6. Value Objects (new in this sprint)

| File | Purpose | Validation |
|---|---|---|
| `Social.Domain/ValueObjects/ReviewRating.cs` | Wraps decimal rating | Must be in [1.0, 5.0] in 0.5 increments (1.0, 1.5, 2.0, ..., 5.0). Static factory `Create(decimal value)` returns Result. |
| `Social.Domain/ValueObjects/ReviewCursor.cs` | Cursor for review list pagination | Base64 of `{Id, CreatedAt}` |
| `Social.Domain/ValueObjects/FavoriteCursor.cs` | Cursor for favorite list pagination | Base64 of `{Id, AddedAt}` |
| `Social.Domain/ValueObjects/ReportCursor.cs` | Cursor for report admin list | Base64 of `{Id, SubmittedAt}` |
| `Social.Domain/ValueObjects/ModerationLogCursor.cs` | Cursor for moderation log list | Base64 of `{Id, ActionedAt}` |

`ReviewRating` is the only meaningful one. The cursors are uniform record types `(Guid Id, DateTime Timestamp)`.

---

## 7. Enums (new + extended)

```csharp
// NEW
public enum ReviewDeletionSource { User = 0, Admin = 1, System = 2 }
public enum FavoriteEntityType { Tour = 0, Place = 1, Business = 2 }
public enum ReportableEntityType { Review = 0, Tour = 1, Place = 2, Business = 3, Blog = 4 }
public enum ReviewStatus { Published = 0, AwaitingModeration = 1, AutoHidden = 2, RemovedByAdmin = 3, DeletedByUser = 4 }

// EXTENDED (verify presence in current code; add if missing)
public enum ReviewTargetType { Tour = 0, Place = 1, Business = 2, TourGuide = 3 }   // Existing — verify
public enum ReportReason { Spam = 0, Inappropriate = 1, Misleading = 2, Harassment = 3, FakeReview = 4, Other = 5 }   // Per PDF 2 §1.6
public enum ReportStatus { Open = 0, UnderReview = 1, Resolved = 2, Dismissed = 3 }
public enum ModerationAction { Dismiss = 0, RemoveContent = 1, WarnUser = 2, BanUser = 3, RestoreContent = 4 }
```

---

## 8. EF Configurations (12 files in `Social.Infrastructure/Persistence/Configurations/`)

1. `ReviewConfiguration.cs` — TPH not needed (only one Review entity); table `social.Reviews`; `Rating decimal(3,2)`; unique index `(UserId, TargetType, TargetId) WHERE IsDeleted=0`; OwnsMany ReviewReply collection (table `social.ReviewReplies`); RowVersion required.
2. `ReviewReplyConfiguration.cs` — owned via Review aggregate; FK `ReviewId`; cascade delete with parent.
3. `FavoriteConfiguration.cs` — table `social.Favorites`; unique index `(UserId, EntityType, EntityId)`; AddedAt column required.
4. `ReportConfiguration.cs` — table `social.Reports`; unique index `(UserId, EntityType, EntityId) WHERE Status=0` (Open).
5. `ContentModerationLogConfiguration.cs` — table `social.ContentModerationLogs`; append-only (no UpdatedAt); index on `(AdminUserId, ActionedAt DESC)`.
6. `ProfanityBlocklistEntryConfiguration.cs` — table `social.ProfanityBlocklist`; unique index on `(Word, LanguageCode)`.
7. `BookingEligibilitySnapshotConfiguration.cs` — table `social.BookingEligibilitySnapshots`; composite PK `(UserId, TargetType, TargetId)`; index on `LastCompletedAt`.
8. `EntityRatingCacheConfiguration.cs` — table `social.EntityRatingCache`; composite PK `(TargetType, TargetId)`; index on `BayesianScore DESC` for ranking queries.
9. `PlaceSnapshotConfiguration.cs` — table `social.PlaceSnapshots`; PK `PlaceId`; tracks `Name, IsDeleted, LastEventId`.
10. `BusinessSnapshotConfiguration.cs` — same shape for businesses.
11. `TourSnapshotConfiguration.cs` — same shape for tours; adds `ProviderId` for reply ownership check.
12. `InboxMessageConfiguration.cs` + `OutboxMessageConfiguration.cs` — standard infrastructure tables.

---

## 9. Migration Sequence

Apply in this exact order (squashes forbidden):

| # | Migration | Owner | When |
|---|---|---|---|
| 1 | `SocialAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 (Tue 2026-10-20) |
| 2 | `SocialAddReviewProvenanceColumns` | Mahmoud | T1 (adds Status, IsVerifiedBooking, ProfanityFlagged, AutoHiddenAt nullable; updates unique index to filter on IsDeleted) |
| 3 | `SocialAddReviewReplyTable` | Mahmoud | T1 |
| 4 | `SocialCreateFavoritesIndexes` | Fadwa | T2 (unique index + entityType filter index) |
| 5 | `SocialAddReportIndexesAndStatus` | Mohammad | T4 |
| 6 | `SocialAddContentModerationLog` | Mohammad | T4 (if table doesn't exist yet) |
| 7 | `SocialAddEntityRatingCacheTable` | Mohammad | T5 |
| 8 | `SocialAddBookingEligibilitySnapshotTable` | Tech Lead | PW-5 (or T1 first step if PW slipped) |
| 9 | `SocialAddCrossModuleSnapshotTables` | Tech Lead | PW-5 (places + businesses + tours snapshots) |
| 10 | `SocialSeedProfanityBlocklist` | Tech Lead | PW-6 (data-only, seeds ~50 EN + AR entries) |

---

<a id="04-task-reviews"></a>

## 04-task-reviews

> Source: `Social/04-task-reviews.md`

# TASK 1 — Reviews

> **Owner:** Mahmoud (Intermediate) — **Hours:** 38h — **Hard deadline:** Sun **2026-11-08 17:00**
> **Earliest start:** Wed 2026-10-21 (after PW-2 migration applied)
> **Endpoints:** 8 — **Depends on:** PW-1..PW-7, BookingEligibilitySnapshot inbox (PW-5)

---

## 1. Endpoint List

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/reviews` | `Review.Create` | Body: `{targetType, targetId, rating, title?, content}`. Returns 201 + Review DTO with `isVerifiedBooking`. |
| 2 | PUT | `/api/v1/reviews/{id}` | `Review.Update` (self-only) | Edit within 48h (S-R3). Rating + title + content. |
| 3 | DELETE | `/api/v1/reviews/{id}` | `Review.Delete` (self-only) | Soft-delete. Source = User. |
| 4 | POST | `/api/v1/reviews/{id}/reply` | `ReviewReply.Create` | Provider only — must own target entity (verified via TourSnapshot/PlaceSnapshot.ProviderId). |
| 5 | PUT | `/api/v1/reviews/{id}/reply/{replyId}` | `ReviewReply.Update` (self-only) | Unlimited edit window (S-R3). |
| 6 | DELETE | `/api/v1/reviews/{id}/reply/{replyId}` | `ReviewReply.Delete` (self-only) | Soft-delete. |
| 7 | GET | `/api/v1/reviews/my-reviews` | `Review.Read` (self-filter) | Cursor pagination; includes deleted=false; includes Status field. |
| 8 | GET | `/api/v1/reviews/admin/flagged` | `AdminModerationQueue.Read` | Auto-hidden reviews queue. Filter by entityType + dateRange. |

**Public GET endpoints (list reviews on a tour, see all replies, etc.)** are owned by ContentTours / ContentPlaces — they read denormalized `Review` data via integration events. NOT this sprint's scope.

---

## 2. Review Aggregate (Mahmoud builds)

```csharp
public sealed class Review : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public ReviewTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public ReviewRating Rating { get; private set; }           // VO
    public string? Title { get; private set; }                  // max 200
    public string Content { get; private set; }                 // max 2000
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedBooking { get; private set; }
    public bool ProfanityFlagged { get; private set; }
    public DateTime? AutoHiddenAt { get; private set; }
    public int CurrentReportCount { get; private set; }
    public DateTime? LastEditedAt { get; private set; }

    private readonly List<ReviewReply> _replies = new();
    public IReadOnlyList<ReviewReply> Replies => _replies;

    public static Review Create(...)               // factory; raises ReviewPublishedDomainEvent OR none if profanity → AwaitingModeration
    public Result Edit(...)                        // 48h window check; profanity re-scan; raises ReviewEditedDomainEvent
    public Result Delete(ReviewDeletionSource src) // soft delete; raises ReviewDeletedDomainEvent
    public Result AutoHide(int reportCount)        // idempotent; raises ReviewAutoHiddenDomainEvent
    public Result Restore(Guid adminUserId)        // raises ReviewRestoredDomainEvent
    public Result AddReply(Guid providerUserId, string content)   // creates ReviewReply child; raises ReviewReplyAddedDomainEvent
    public Result UpdateReply(Guid replyId, Guid providerUserId, string content)
    public Result DeleteReply(Guid replyId, Guid providerUserId)
}

public sealed class ReviewReply : BaseEntity
{
    public Guid ReviewId { get; private set; }
    public Guid ProviderUserId { get; private set; }
    public string Content { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastEditedAt { get; private set; }
    // ... factories
}
```

**Profanity flow in Create / Edit:**
1. Call `await _profanityFilter.ContainsProfanityAsync(content, languageCode, ct)`.
2. If true: set `Status = AwaitingModeration`, `ProfanityFlagged = true`, DON'T raise ReviewPublishedDomainEvent (frontend sees Status = AwaitingModeration in response, knows it's pending). Add to admin queue via `entity.Status` filter.
3. Else: Status = Published, raise ReviewPublishedDomainEvent.

---

## 3. Verified-Booking Check (S-R1)

In `CreateReviewCommandHandler`:
```csharp
var hasCompletedBooking = await _eligibilityRepo.UserHasCompletedBookingAsync(
    currentUser.UserId, command.TargetType, command.TargetId, ct);

var isVerified = hasCompletedBooking;  // 30-day check is inside snapshot lookup
var review = Review.Create(
    userId: currentUser.UserId,
    targetType: command.TargetType,
    targetId: command.TargetId,
    rating: ratingResult.Value,
    title: command.Title,
    content: command.Content,
    isVerifiedBooking: isVerified,
    profanityFilter: _profanityFilter,
    languageCode: command.LanguageCode ?? "en",
    now: _timeProvider.GetUtcNow().UtcDateTime,
    ct: ct);

if (review.IsFailure) return review.ToError();
_repo.Add(review.Value);
await _unitOfWork.SaveChangesAsync(ct);
await _cache.RemoveByTagAsync($"reviews:user:{currentUser.UserId}", ct);
await _cache.RemoveByTagAsync($"entity-rating:{command.TargetType}:{command.TargetId}", ct);
```

---

## 4. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Review + ReviewReply + ReviewRating VO + ReviewStatus enum + factories | 5 | 2026-10-24 |
| 2 | EF configurations + migration `SocialAddReviewProvenanceColumns` + `SocialAddReviewReplyTable` | 4 | 2026-10-26 |
| 3 | CreateReviewCommand + Validator + Handler + tests | 5 | 2026-10-28 |
| 4 | UpdateReviewCommand + 48h gate + Validator + Handler + tests | 4 | 2026-10-30 |
| 5 | DeleteReviewCommand + Handler + tests | 2 | 2026-10-30 |
| 6 | AddReviewReplyCommand + provider ownership check + Validator + Handler + tests | 4 | 2026-11-01 |
| 7 | UpdateReviewReplyCommand + DeleteReviewReplyCommand + Handlers + tests | 3 | 2026-11-02 |
| 8 | GetMyReviewsQuery (cursor) + Handler + ICacheableQuery + tests | 3 | 2026-11-04 |
| 9 | GetAdminFlaggedReviewsQuery + Handler + tests | 3 | 2026-11-05 |
| 10 | 8 endpoint wiring + Swagger XML docs | 2 | 2026-11-06 |
| 11 | Integration test: end-to-end booking-complete → review-publish → outbox round-trip | 2 | 2026-11-07 |
| 12 | PR review fixes | 1 | 2026-11-08 |
| **Total** | | **38h** | **Sun 2026-11-08** |

---

## 5. Acceptance Tests (12 cases)

1. POST review with completed booking → IsVerifiedBooking = true, Status = Published, outbox `social.review.published.v1`.
2. POST review without booking → IsVerifiedBooking = false, Status = Published.
3. POST review with profanity → Status = AwaitingModeration, ProfanityFlagged = true, NO outbox row.
4. POST second review same tour same user → 409 `Review.AlreadyExists`.
5. PUT review at 47h59m → success, updates Rating + raises ReviewEditedDomainEvent.
6. PUT review at 48h00m01s → 422 `Review.EditWindowExpired`.
7. PUT review introducing profanity → Status reverts to AwaitingModeration.
8. DELETE own review → IsDeleted=true, can submit new review on same target.
9. DELETE other user's review → 403 `Review.OwnerMismatch`.
10. POST reply by tour-owning provider → success, ReviewReplyAddedDomainEvent raised.
11. POST reply by non-owning provider → 403 `ReviewReply.NotOwnerOfTargetEntity`.
12. GET admin/flagged returns only auto-hidden + AwaitingModeration reviews, sorted by AutoHiddenAt DESC.

---

<a id="05-task-favorites"></a>

## 05-task-favorites

> Source: `Social/05-task-favorites.md`

# TASK 2 — Favorites

> **Owner:** Fadwa (Beginner) — **Hours:** 16h — **Hard deadline:** Sun **2026-11-08 17:00**
> **Earliest start:** Wed 2026-10-21 (parallel with T1)
> **Endpoints:** 4 — **Depends on:** PW-1..PW-7

---

## 1. Endpoint List

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/favorites` | `Favorite.Create` | Body: `{entityType, entityId}`. Returns 201 + Favorite DTO. 409 if exists. 422 if > 500. |
| 2 | DELETE | `/api/v1/favorites/{entityType}/{entityId}` | `Favorite.Delete` (self-only) | Idempotent — 204 even if not found. |
| 3 | GET | `/api/v1/favorites` | `Favorite.Read` (self-filter) | Cursor pagination, filter `?entityType=Tour\|Place\|Business`. |
| 4 | GET | `/api/v1/favorites/check/{entityType}/{entityId}` | `Favorite.Read` (self-only) | Returns `{isFavorited: bool}`. Cheap, used by frontend cards. |

---

## 2. Favorite Aggregate

```csharp
public sealed class Favorite : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public FavoriteEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private Favorite() { }

    public static Result<Favorite> Add(
        Guid userId,
        FavoriteEntityType entityType,
        Guid entityId,
        int currentUserCount,
        int maxAllowed,
        DateTime now)
    {
        if (currentUserCount >= maxAllowed)
            return Result.Failure<Favorite>(new Error("Favorite.LimitExceeded", $"Maximum {maxAllowed} favorites allowed."), Outcome.UnprocessableEntity);

        var fav = new Favorite
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            AddedAt = now,
            CreatedAt = now,
            CreatedByUserId = userId,
        };
        fav.RaiseDomainEvent(new FavoriteAddedDomainEvent(fav.Id, userId, entityType, entityId, now));
        return Result.Success(fav);
    }

    public void Remove(DateTime now)
    {
        if (IsDeleted) return;   // idempotent
        IsDeleted = true;
        DeletedAt = now;
        MarkUpdated(now, UserId);
        RaiseDomainEvent(new FavoriteRemovedDomainEvent(Id, UserId, EntityType, EntityId, now));
    }
}
```

**Cap check** = current user's non-deleted favorite count loaded BEFORE calling `Add`. PDF 2 §1.7 + PDF 1 Wave 6 = max 500.

**Entity-type validation:** Use cross-module snapshots (PW-5):
- `FavoriteEntityType.Tour` → check `TourSnapshot.IsDeleted = false`
- `FavoriteEntityType.Place` → check `PlaceSnapshot.IsDeleted = false`
- `FavoriteEntityType.Business` → check `BusinessSnapshot.IsDeleted = false`

If snapshot missing or marked deleted, return `Error("Favorite.InvalidEntityType", ...) Outcome.ValidationError` — 400.

---

## 3. Handler Skeletons

```csharp
internal sealed class AddFavoriteCommandHandler(
    IFavoriteRepository repo,
    IPlaceSnapshotRepository placeSnapshots,
    ITourSnapshotRepository tourSnapshots,
    IBusinessSnapshotRepository businessSnapshots,
    ISocialUnitOfWork uow,
    ICurrentUser currentUser,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<AddFavoriteCommandHandler> logger,
    IOptions<FavoritesOptions> options)
    : IRequestHandler<AddFavoriteCommand, Result<FavoriteDto>>
{
    public async Task<Result<FavoriteDto>> Handle(AddFavoriteCommand command, CancellationToken ct)
    {
        // 1. Validate entity exists (snapshot lookup)
        var exists = command.EntityType switch
        {
            FavoriteEntityType.Tour     => await tourSnapshots.ExistsAsync(command.EntityId, ct),
            FavoriteEntityType.Place    => await placeSnapshots.ExistsAsync(command.EntityId, ct),
            FavoriteEntityType.Business => await businessSnapshots.ExistsAsync(command.EntityId, ct),
            _ => false
        };
        if (!exists) return Result.Failure<FavoriteDto>(new Error("Favorite.InvalidEntityType", "..."), Outcome.ValidationError);

        // 2. Already favorited?
        if (await repo.ExistsForUserAsync(currentUser.UserId, command.EntityType, command.EntityId, ct))
            return Result.Failure<FavoriteDto>(new Error("Favorite.AlreadyExists", "..."), Outcome.Conflict);

        // 3. Limit
        var count = await repo.CountForUserAsync(currentUser.UserId, ct);
        var add = Favorite.Add(currentUser.UserId, command.EntityType, command.EntityId, count, options.Value.MaxPerUser, timeProvider.GetUtcNow().UtcDateTime);
        if (add.IsFailure) return add.ToError<FavoriteDto>();

        repo.Add(add.Value);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"favorites:user:{currentUser.UserId}", ct);

        return Result.Success(FavoriteDto.FromEntity(add.Value));
    }
}
```

`FavoritesOptions` (in Social.Application/Options/) bound from `Social:Favorites` config section: `{ MaxPerUser: 500 }`.

---

## 4. Cache Strategy (S-R9 recap)

- `favorites:user:{userId}` (5 min) — list query
- `favorite-check:user:{userId}:type:{type}:id:{id}` (30 sec) — boolean check
- Both invalidated on Add / Remove

---

## 5. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Favorite aggregate + factory + Remove method | 2 | 2026-10-22 |
| 2 | EF config + migration `SocialCreateFavoritesIndexes` | 2 | 2026-10-23 |
| 3 | AddFavoriteCommand + Validator + Handler + 4 tests | 4 | 2026-10-27 |
| 4 | RemoveFavoriteCommand + Handler + 2 tests | 2 | 2026-10-28 |
| 5 | GetMyFavoritesQuery (cursor) + ICacheableQuery + 2 tests | 2 | 2026-10-30 |
| 6 | CheckFavoriteQuery + ICacheableQuery + 2 tests | 1 | 2026-10-31 |
| 7 | 4 endpoint wiring + Swagger XML docs | 1 | 2026-11-02 |
| 8 | Integration test: outbox `social.favorite.added.v1` round-trip | 1 | 2026-11-05 |
| 9 | PR review fixes | 1 | 2026-11-08 |
| **Total** | | **16h** | **Sun 2026-11-08** |

---

## 6. Acceptance Tests (8 cases)

1. POST favorite for existing tour → 201, outbox `social.favorite.added.v1` row exists.
2. POST favorite for deleted/non-existent tour → 400 `Favorite.InvalidEntityType`.
3. POST favorite already exists → 409 `Favorite.AlreadyExists`.
4. POST 501st favorite → 422 `Favorite.LimitExceeded`.
5. DELETE favorite that exists → 204, outbox `social.favorite.removed.v1` row (if we add it later — note: NOT in this sprint per S-R7, so just no row).
6. DELETE favorite that doesn't exist → 204 (idempotent).
7. GET favorites with no items → 200 + empty list.
8. GET favorites/check/Tour/{id} → returns `{isFavorited: true|false}` matching state.

---

<a id="06-task-reports-moderation"></a>

## 06-task-reports-moderation

> Source: `Social/06-task-reports-moderation.md`

# TASK 4 — Reports + Content Moderation

> **Owner:** Mohammad (Intermediate) — **Hours:** 28h — **Hard deadline:** Sun **2026-11-22 17:00**
> **Earliest start:** Wed 2026-10-21 (parallel with T1, T2)
> **Endpoints:** 6 — **Depends on:** PW-1..PW-7, T1 (Review.AutoHide method must exist for the 5-report threshold)

---

## 1. Endpoint List

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/reports` | `Report.Create` | Body: `{entityType, entityId, reason, description}`. Returns 201 + Report DTO. Triggers auto-action chain if threshold hit. |
| 2 | POST | `/api/v1/reviews/{id}/report` | `Report.Create` | Convenience alias — populates entityType=Review, entityId={id}. |
| 3 | POST | `/api/v1/reports/admin/{id}/resolve` | `AdminModerationQueue.Resolve` | Body: `{action, notes?}` — action ∈ {Dismiss, RemoveContent, WarnUser, BanUser}. |
| 4 | POST | `/api/v1/reviews/admin/{id}/approve` | `AdminModerationQueue.Approve` | Restore an AutoHidden review. |
| 5 | POST | `/api/v1/reviews/admin/{id}/remove` | `AdminModerationQueue.Remove` | Permanently remove a flagged review. |
| 6 | GET | `/api/v1/reports/admin` | `Report.Read` (admin filter) | Cursor pagination, filters by reason, status, entityType, dateRange. |
| 7 | GET | `/api/v1/moderation/logs` | `ContentModerationLog.Read` | Cursor pagination, filters by adminUserId, action, dateRange. |

(Endpoint count = 7 not 6 — fix in INDEX manifest before sprint kickoff.)

---

## 2. Report Aggregate

```csharp
public sealed class Report : AuditableEntity, IAggregateRoot
{
    public Guid ReporterUserId { get; private set; }
    public ReportableEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string Description { get; private set; }     // 20-500 chars
    public ReportStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public ModerationAction? ResolutionAction { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public static Result<Report> Submit(...)  // raises ReportSubmittedDomainEvent
    public Result Resolve(Guid adminUserId, ModerationAction action, string? notes, DateTime now)
    {
        if (Status != ReportStatus.Open && Status != ReportStatus.UnderReview)
            return Result.Failure(new Error("Report.AlreadyResolved", ...), Outcome.Conflict);
        Status = action == ModerationAction.Dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;
        ResolvedByUserId = adminUserId;
        ResolvedAt = now;
        ResolutionAction = action;
        ResolutionNotes = notes;
        MarkUpdated(now, adminUserId);
        RaiseDomainEvent(new ReportResolvedDomainEvent(Id, adminUserId, action, now));
        return Result.Success();
    }
}
```

---

## 3. ContentModerationLog (BaseEntity, append-only)

```csharp
public sealed class ContentModerationLog : BaseEntity
{
    public Guid AdminUserId { get; private set; }
    public ReportableEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public ModerationAction Action { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ActionedAt { get; private set; }
    public Guid? SourceReportId { get; private set; }   // null for direct admin actions (approve/remove)

    public static ContentModerationLog Record(...)
}
```

Every moderation action (resolve report, approve auto-hidden review, remove review) writes a log row in the SAME SaveChanges as the action itself.

---

## 4. 5-Report Auto-Hide Chain (S-R5)

`SubmitReportCommandHandler` flow:
1. Validate (entity exists snapshot lookup, user hasn't already reported, reason valid, description 20-500).
2. `Report.Submit(...)` → returns Report aggregate.
3. SaveChanges (commits Report row + raises ReportSubmittedDomainEvent + outbox row).
4. Re-query `IReportRepository.CountActiveReportsAsync(EntityType, EntityId, ct)`.
5. If `entityType == Review` AND count >= 5:
   - Load Review aggregate via `IReviewRepository`.
   - Call `review.AutoHide(count)` (idempotent — checks Status != AutoHidden).
   - SaveChanges (commits Review.Status = AutoHidden + raises ReviewAutoHiddenDomainEvent + outbox row).
   - Log via ContentModerationLog with `Action = RemoveContent` (auto-actioned, AdminUserId = `Guid.Empty` or system user ID, source report ID null because triggered by aggregate threshold, NOT a specific report resolution).

Step 4 needs to happen AFTER step 3's SaveChanges so the new report is included in the count. Acceptable double-trip cost.

**Auto-action threshold for non-Review entities** (Tour, Place, Business, Blog) — PDF 1 Wave 6 says "3+ reports auto-hide+escalate moderation". This sprint only auto-hides Reviews; auto-hide of Tours/Places/Businesses requires those modules to expose a `MarkAutoHidden` integration event, which is deferred. We just write the ContentModerationLog row + raise `EntityAutoActionedDomainEvent` for visibility, but no actual hide happens.

---

## 5. Admin Endpoints

`ResolveReportCommandHandler` flow:
1. Load Report aggregate.
2. `report.Resolve(adminUserId, action, notes, now)`.
3. Apply secondary effect based on action:
   - `Dismiss` → no further action.
   - `RemoveContent` → load target entity (Review only this sprint), call `review.AdminDelete(adminUserId)`.
   - `WarnUser` → publish `social.user.warned.v1` integration event (Messaging consumes → email).
   - `BanUser` → publish `social.user.banned.v1` (deferred — for now log warning).
4. Write ContentModerationLog row.
5. SaveChanges (commits all of: Report.Resolve, optional Review delete, log row, both outbox events atomically).

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Report aggregate + ReportStatus + ModerationAction enums + factories | 3 | 2026-10-26 |
| 2 | ContentModerationLog entity + EF configs + migration `SocialAddReportIndexesAndStatus` + `SocialAddContentModerationLog` | 3 | 2026-10-28 |
| 3 | SubmitReportCommand + Validator + 6 tests | 4 | 2026-11-02 |
| 4 | 5-report auto-hide chain integration + 4 tests | 3 | 2026-11-05 |
| 5 | ResolveReportCommand + Validator + 4 tests | 3 | 2026-11-08 |
| 6 | ApproveAutoHiddenReviewCommand + RemoveReviewCommand + Handlers + 4 tests | 3 | 2026-11-12 |
| 7 | GetAdminReportsQuery + Handler + ICacheableQuery + tests | 3 | 2026-11-15 |
| 8 | GetModerationLogsQuery + Handler + ICacheableQuery + tests | 2 | 2026-11-17 |
| 9 | 7 endpoint wiring + Swagger XML docs | 2 | 2026-11-19 |
| 10 | Integration test: full report → auto-hide → admin resolve chain | 1 | 2026-11-20 |
| 11 | PR review fixes | 1 | 2026-11-22 |
| **Total** | | **28h** | **Sun 2026-11-22** |

---

## 7. Acceptance Tests (10 cases)

1. POST report on Review → 201, outbox `social.report.submitted.v1` not emitted (it's a domain-only event, not integration this sprint — verify in PW-4).
2. POST 5th report on same review → review.Status = AutoHidden, ReviewAutoHiddenDomainEvent raised, ContentModerationLog row written.
3. POST report by same user twice → 409 `Report.AlreadyReported`.
4. POST report with reason = Spam, description 5 chars → 400 `Report.DescriptionTooLong` (named badly — actually too short).
5. POST report with invalid entityType → 400 `Report.TargetNotReportable`.
6. Admin Resolve report with Dismiss → Report.Status = Dismissed, no content removed, log row written.
7. Admin Resolve with RemoveContent → Review soft-deleted, outbox `social.review.deleted.v1` row.
8. Admin Resolve already-resolved report → 409 `Report.AlreadyResolved`.
9. Admin Approve auto-hidden review → Status = Published, ReviewRestoredDomainEvent raised.
10. Admin Approve never-hidden review → 422 `Moderation.ReviewNotAutoHidden`.

---

<a id="07-task-background-services"></a>

## 07-task-background-services

> Source: `Social/07-task-background-services.md`

# TASK 3 + TASK 5 — Background Services

> **Owners:** Fadwa (T3 OrphanedFavoritesCleanup, 8h) + Mohammad (T5 RatingRecalculation, 24h)
> **Hard deadlines:** T3 Sun 2026-11-15; T5 Sun 2026-11-22
> **Earliest start:** T3 after T2 Favorites merged; T5 after T1 Reviews + EntityRatingCache table created (T5 itself creates it via PW-5 fallback)

This task covers **2 hosted services**. Both follow the shared `BackgroundService + PeriodicTimer` pattern from `Booking/10-task-background-services.md §1`. **No Hangfire / Quartz (ADR-003).**

---

## 1. OrphanedFavoritesCleanupService (T3 — Fadwa, 8h)

**Goal:** Remove Favorite rows pointing to deleted entities (PDF 2 §1.7: "deleted entities auto-removed lazily + weekly cleanup").

**Cadence:** weekly **Saturday 03:00 UTC**.

```csharp
internal sealed class OrphanedFavoritesCleanupService(
    IServiceProvider services,
    ILogger<OrphanedFavoritesCleanupService> logger,
    TimeProvider timeProvider,
    IOptions<OrphanedFavoritesCleanupOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay to next Saturday 03:00 UTC
        await Task.Delay(NextOccurrence(DayOfWeek.Saturday, new TimeOnly(3, 0)) - timeProvider.GetUtcNow(), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(7));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = services.CreateScope();
            try
            {
                await SweepAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OrphanedFavoritesCleanup tick failed; retry next week");
            }
        }
    }

    private async Task SweepAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var repo = scoped.GetRequiredService<IFavoriteRepository>();
        var uow  = scoped.GetRequiredService<ISocialUnitOfWork>();
        int totalCleaned = 0;

        foreach (FavoriteEntityType type in Enum.GetValues<FavoriteEntityType>())
        {
            var orphanIds = await repo.FindOrphanedAsync(type, options.Value.BatchSize, ct);
            foreach (var favId in orphanIds)
            {
                var fav = await repo.GetByIdAsync(favId, ct);
                if (fav is null) continue;
                fav.Remove(timeProvider.GetUtcNow().UtcDateTime);
                totalCleaned++;
            }
        }
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("OrphanedFavoritesCleanup: removed {Count} orphan rows", totalCleaned);
    }
}
```

`FindOrphanedAsync` SQL for FavoriteEntityType=Tour:
```sql
SELECT TOP (@batchSize) f.Id
FROM social.Favorites f
LEFT JOIN social.TourSnapshots t ON t.TourId = f.EntityId
WHERE f.EntityType = 0   -- Tour
  AND f.IsDeleted = 0
  AND (t.TourId IS NULL OR t.IsDeleted = 1)
ORDER BY f.AddedAt ASC;
```

Same pattern for Place (EntityType=1) and Business (EntityType=2).

**Options** (`Social:BackgroundServices:OrphanedFavoritesCleanup`): `{ BatchSize: 1000, Enabled: true }`.

---

## 2. RatingRecalculationService (T5 — Mohammad, 24h)

**Goal:** Daily recompute weighted AverageRating + Bayesian score for every entity with reviews (S-R6). Publishes `social.rating.recalculated.v1` integration events; ContentTours / ContentPlaces inbox handlers update denormalized columns.

**Cadence:** daily **03:00 UTC**.

### Algorithm

```csharp
private async Task RecalculateAsync(IServiceProvider scoped, CancellationToken ct)
{
    var reviewRepo = scoped.GetRequiredService<IReviewRepository>();
    var ratingRepo = scoped.GetRequiredService<IEntityRatingCacheRepository>();
    var uow = scoped.GetRequiredService<ISocialUnitOfWork>();
    var now = timeProvider.GetUtcNow().UtcDateTime;

    // 1. Compute global average across all published, non-deleted reviews
    var globalAvg = await reviewRepo.GetGlobalAverageRatingAsync(ct);

    // 2. Find entities needing recalculation:
    //    - rated within last 24h (covers freshly added/edited/deleted reviews)
    //    - OR not in EntityRatingCache yet
    //    - OR cache row older than 24h (handles recency-decay rounding shifts)
    var staleTargets = await reviewRepo.GetStaleEntitiesForRecalcAsync(
        cutoff: now.AddHours(-25),   // 1-hour overlap to be safe
        take: options.Value.BatchSize,
        ct);

    int processed = 0;
    foreach (var target in staleTargets)
    {
        var reviews = await reviewRepo.GetPublishedForTargetAsync(target.Type, target.Id, ct);
        if (reviews.Count == 0)
        {
            // Entity had reviews then all deleted — reset cache row
            var existing = await ratingRepo.GetAsync(target.Type, target.Id, ct);
            if (existing is not null)
            {
                existing.Reset(now);
                RaiseRecalculated(target, 0m, 0, 0m, now);
            }
            continue;
        }

        var weighted = ComputeWeightedAverage(reviews, now);
        var bayesian = ComputeBayesian(weighted.Avg, reviews.Count, globalAvg, options.Value.BayesianConfidence);

        var cache = await ratingRepo.GetAsync(target.Type, target.Id, ct)
            ?? EntityRatingCache.Create(target.Type, target.Id, weighted.Avg, reviews.Count, bayesian, now);
        cache.Update(weighted.Avg, reviews.Count, bayesian, now);
        ratingRepo.UpsertAsync(cache, ct);

        RaiseRecalculated(target, weighted.Avg, reviews.Count, bayesian, now);
        processed++;
    }

    await uow.SaveChangesAsync(ct);
    logger.LogInformation("RatingRecalculation: processed {Count} entities; global avg = {Avg:F2}", processed, globalAvg);
}

private static (decimal Avg, decimal SumWeights) ComputeWeightedAverage(IReadOnlyList<Review> reviews, DateTime now)
{
    decimal sumRating = 0m, sumWeights = 0m;
    foreach (var r in reviews)
    {
        var verificationWeight = r.IsVerifiedBooking ? 1.0m : 0.5m;
        var ageDays = (now - r.CreatedAt).TotalDays;
        var recencyWeight = ageDays < 90 ? 1.0m : ageDays < 180 ? 0.7m : 0.5m;
        var w = verificationWeight * recencyWeight;
        sumRating += r.Rating.Value * w;
        sumWeights += w;
    }
    return (sumWeights == 0 ? 0 : sumRating / sumWeights, sumWeights);
}

private static decimal ComputeBayesian(decimal avgRating, int reviewCount, decimal globalAvg, int confidence)
{
    if (reviewCount == 0) return 0m;
    return (reviewCount * avgRating + confidence * globalAvg) / (reviewCount + confidence);
}
```

`options.Value.BayesianConfidence` default 10 (S-R6 / PDF 2 §18).

### Per-entity event raising

`RaiseRecalculated` creates an `EntityRatingRecalculatedDomainEvent` on a single shared aggregate `RatingRecalculationBatch` (a transient pseudo-aggregate created at the start of the batch) — OR on each `EntityRatingCache` row if we promote it to IAggregateRoot.

**DECISION:** Keep `EntityRatingCache` as BaseEntity, create a transient `RatingRecalculationBatch` aggregate per sweep that collects all events. This pattern lets the outbox emit one integration event per recalculated entity in one transaction. Concrete: `_uow.AttachTransientAggregate(batch)` adds it to the change tracker briefly so domain events flush.

Alternative simpler: promote `EntityRatingCache` to `IAggregateRoot` (PW-2 amendment). This is cleaner. Use this for now.

### Options

`Social:BackgroundServices:RatingRecalculation`:
```json
{
  "TargetUtcTime": "03:00:00",
  "BatchSize": 5000,
  "BayesianConfidence": 10,
  "Enabled": true
}
```

---

## 3. DI Registration

```csharp
// Social.Infrastructure/DependencyInjection.cs
services.Configure<OrphanedFavoritesCleanupOptions>(cfg.GetSection("Social:BackgroundServices:OrphanedFavoritesCleanup"));
services.Configure<RatingRecalculationOptions>(cfg.GetSection("Social:BackgroundServices:RatingRecalculation"));

services.AddHostedService<RatingRecalculationService>();          // LIFO: stops first
services.AddHostedService<OrphanedFavoritesCleanupService>();
```

`NextOccurrence(DayOfWeek, TimeOnly)` helper lives in `SharedKernel.Infrastructure/Time/CronHelpers.cs`. Already exists from Booking sprint.

---

## 4. WBS (combined T3 + T5)

| # | Owner | Step | Hours | Finish-by |
|---|---|---|---|---|
| 1 | Fadwa | OrphanedFavoritesCleanupService + options + DI + unit test | 4 | 2026-11-12 |
| 2 | Fadwa | Manual integration test: insert orphan favorites + run service + verify removal | 2 | 2026-11-13 |
| 3 | Fadwa | PR review fixes | 2 | 2026-11-15 |
| 4 | Mohammad | EntityRatingCache aggregate + EF config + migration | 3 | 2026-11-08 |
| 5 | Mohammad | RatingRecalculationService skeleton + cron helper + unit test scaffolding | 4 | 2026-11-12 |
| 6 | Mohammad | `ComputeWeightedAverage` + `ComputeBayesian` + 12 unit tests covering verification/recency weight bands | 6 | 2026-11-15 |
| 7 | Mohammad | RatingRecalculationBatch transient aggregate pattern OR EntityRatingCache → IAggregateRoot (decide) | 3 | 2026-11-17 |
| 8 | Mohammad | Integration test: 5 reviews → service tick → outbox row → assert AverageRating decimal places | 4 | 2026-11-20 |
| 9 | Mohammad | PR review fixes | 4 | 2026-11-22 |
| **Total** | | | **32h** | |

---

## 5. Acceptance Tests

### OrphanedFavoritesCleanup (4 cases)
1. Insert orphan favorite (tour snapshot IsDeleted=1) → service tick removes it (IsDeleted=1, DeletedAt stamped).
2. Insert valid favorite → service tick does NOT remove.
3. Batch limit (1001 orphans, BatchSize=1000) → first tick removes 1000, second tick removes the last 1.
4. Service disabled via config → boot log shows "skipped registration", no removal happens.

### RatingRecalculation (8 cases)
1. Single verified review, rating 5.0, age < 90 days → AverageRating = 5.00, BayesianScore = (1×5 + 10×globalAvg) / 11.
2. Mix of verified + non-verified → weighted average correct (weight 1.0 vs 0.5).
3. Old review > 180 days → recency weight 0.5 applied.
4. All reviews deleted → cache row Reset, AverageRating = 0, ReviewCount = 0.
5. ReviewCount < 3 → BayesianScore still computed but `MIN 3 REVIEWS` rule enforced by API layer (S-R6).
6. Outbox: each recalculated entity produces one `social.rating.recalculated.v1` row.
7. Idempotent: running service twice in same day → second tick recomputes same value (no-op stable).
8. Manual admin trigger via T3 bonus endpoint (out of scope unless added) — defer.

---

<a id="08-cross-cutting"></a>

## 08-cross-cutting

> Source: `Social/08-cross-cutting.md`

# Social — Cross-Cutting Concerns

> Mirrors `Booking/11-cross-cutting.md` and `Finance/10-cross-cutting.md`. Tech Lead enforces at PR review and at the sprint integration freeze (Sun 2026-11-15 17:00).

---

## 1. DI Audit (Social.Infrastructure/DependencyInjection.cs)

| Registration | Symbol | Lifetime |
|---|---|---|
| Pooled DbContext | `SocialDbContext` via `AddDbContextPool` | Scoped |
| DbContext factory | `IDbContextFactory<SocialDbContext>` | Singleton |
| Unit of Work | `ISocialUnitOfWork → SocialUnitOfWork` | Scoped |
| Inbox store | `ISocialInboxStore → SocialInboxStore` | Scoped |
| Outbox writer | `ISocialOutboxWriter → SocialOutboxWriter` | Scoped |
| 6 repositories | `IReviewRepository, IFavoriteRepository, IReportRepository, IContentModerationLogRepository, IBookingEligibilitySnapshotRepository, IEntityRatingCacheRepository` (+ 3 snapshot repos: ITourSnapshotRepository, IPlaceSnapshotRepository, IBusinessSnapshotRepository) | Scoped |
| Moderation services | `IProfanityFilter → BlocklistProfanityFilter` (Scoped), `INsfwClassifier → AlwaysSafeNsfwClassifier` (Singleton) | per above |
| 2 BackgroundServices | OrphanedFavoritesCleanupService, RatingRecalculationService | Singleton (AddHostedService) |
| Permission catalog | `IPermissionCatalog → SocialPermissionCatalog` | Singleton |
| MediatR | `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SocialApplicationMarker).Assembly))` | per-call |
| FluentValidation | `services.AddValidatorsFromAssembly(typeof(SocialApplicationMarker).Assembly, ServiceLifetime.Scoped, includeInternalTypes: true)` | Scoped |
| Diagnostics | `SocialDiagnostics` static (ActivitySource "YallaJo.Social" + Meter "YallaJo.Social") | – |
| Cache key + tag builder | `ISocialCacheKeys → SocialCacheKeys` | Singleton |

**Common DI mistakes (fail PR on these):**
- ❌ Registering `IProfanityFilter` as Singleton (it depends on `SocialDbContext` for blocklist reload).
- ❌ Registering `INsfwClassifier` as Scoped (stateless — Singleton is correct).
- ❌ Forgetting `IPermissionCatalog` registration → 19 permissions absent.
- ❌ Forgetting the 3 cross-module snapshot repositories — favorites and reviews won't validate entity types.

---

## 2. Permission Seeder Verification

Expected boot log AFTER this sprint merges (Social = 9th catalog):

```
[INFO] PermissionSeeder discovered 9 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social
[INFO] PermissionSeeder inserted/verified 19 Social permissions
```

Verification SQL: `SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Social.%'` → **19**.

---

## 3. Outbox Type-Registry Parity Test

`tests/Social.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs`:

```csharp
[Fact]
public void All_Social_integration_event_records_are_registered()
{
    var asm = typeof(FavoriteAddedIntegrationEvent).Assembly;
    var declared = asm.GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IIntegrationEvent)) && !t.IsAbstract)
        .ToHashSet();
    var registered = IntegrationEventTypeRegistry.All
        .Where(kvp => kvp.Key.StartsWith("social."))
        .Select(kvp => kvp.Value)
        .ToHashSet();
    declared.Should().BeEquivalentTo(registered);
}
```

Expected 5 logical names:
- `social.review.published.v1`
- `social.review.deleted.v1`
- `social.favorite.added.v1`
- `social.report.resolved.v1`
- `social.rating.recalculated.v1`

Inbox consumers register 4 logical names from upstream modules:
- `booking.tour-booking.completed.v1`
- `content-places.place.deleted.v1`
- `content-places.business.deleted.v1`
- `content-tours.tour.deleted.v1`

Plus the 6 inbound from ContentTours / ContentPlaces snapshot maintenance (Created / Updated / Deleted × 2 entity types).

---

## 4. Build Lock Workaround

Same as Booking. Build only Social-related projects:
```powershell
dotnet build Social/Social.Domain/Social.Domain.csproj
dotnet build Social/Social.Contracts/Social.Contracts.csproj
dotnet build Social/Social.Application/Social.Application.csproj
dotnet build Social/Social.Infrastructure/Social.Infrastructure.csproj
dotnet build Social/Social.Presentation/Social.Presentation.csproj
dotnet build tests/Social.Tests.Unit/Social.Tests.Unit.csproj
dotnet build tests/Social.IntegrationTests/Social.IntegrationTests.csproj
```

---

## 5. Migration Sequence

From `03-entities-matrix.md §9`:

| # | Migration | Owner | When |
|---|---|---|---|
| 1 | SocialAddAggregateRootAndAuditMembers | Tech Lead | PW-2 |
| 2 | SocialAddReviewProvenanceColumns | Mahmoud | T1 |
| 3 | SocialAddReviewReplyTable | Mahmoud | T1 |
| 4 | SocialCreateFavoritesIndexes | Fadwa | T2 |
| 5 | SocialAddReportIndexesAndStatus | Mohammad | T4 |
| 6 | SocialAddContentModerationLog | Mohammad | T4 |
| 7 | SocialAddEntityRatingCacheTable | Mohammad | T5 |
| 8 | SocialAddBookingEligibilitySnapshotTable | Tech Lead | PW-5 |
| 9 | SocialAddCrossModuleSnapshotTables | Tech Lead | PW-5 |
| 10 | SocialSeedProfanityBlocklist | Tech Lead | PW-6 (data migration) |

Apply in order. Tech Lead deploys.

---

## 6. Inbox / Outbox Hygiene

**`CompositeOutboxProcessor`** auto-picks up `SocialDbContext` — verify boot log: `monitoring N DbContexts: ..., SocialDbContext`.

**OutboxCleaner** deletes processed rows older than 7 days. No Social-specific config.

**InboxCleaner** deletes processed rows older than 30 days. No Social-specific config.

**Alerting thresholds:**
- `social.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5 min` count > 100 → page on-call
- Same for InboxMessages

**Per-aggregate guard:** every inbox handler (Booking → Eligibility, ContentPlaces/Tours → Snapshots) MUST use `await inboxStore.HasBeenProcessedAsync(evt.Id, ct)` → work → `inboxStore.MarkAsProcessed(evt.Id)` → ONE SaveChangesAsync.

---

## 7. Profanity Blocklist Management

`social.ProfanityBlocklist` table seeded by `SocialSeedProfanityBlocklist` migration with ~50 English + Arabic entries. Admin updates via direct DB this sprint; UI deferred.

Caching: `BlocklistProfanityFilter` loads blocklist once on first call per scope (per-request), then caches in `IMemoryCache` keyed by language code for 5 minutes. Admin changes take effect within 5 min.

**Adding new words:** Tech Lead runs:
```sql
INSERT INTO social.ProfanityBlocklist (Id, Word, LanguageCode, AddedAt, AddedByUserId)
VALUES (NEWID(), 'badword', 'en', SYSUTCDATETIME(), @adminUserId);
```
Cache invalidates within 5 min OR via admin endpoint `POST /api/v1/admin/social/profanity-cache/clear` (NOT built this sprint).

---

## 8. Cross-Module Coupling Risks

| Risk | Mitigation |
|---|---|
| Booking sends `tour-booking.completed.v1` with userId/targetType pair that doesn't match snapshot reality (e.g., Tour was deleted then re-created with same ID) | Snapshot lookup gracefully returns false → user just doesn't get Verified badge. Reviewer logs warning. |
| ContentTours updates `Tour.AverageRating` from rating-recalculated event, but Tour was deleted in the meantime | Inbox handler in ContentTours guards with `if (tour is null) return Result.Success()` — skip silently. |
| ProfanityFilter blocklist cache stale during admin word addition | Documented 5-min eventual consistency window. Future sprint adds cache-clear endpoint. |
| Bayesian global average drifts when one mega-review batch (e.g., bulk import) skews it | RatingRecalculationService computes globalAvg fresh every day. One-day skew is acceptable; admins can manually trigger via T7 bonus endpoint (deferred) for emergency recalc. |

---

<a id="99-acceptance-gate"></a>

## 99-acceptance-gate

> Source: `Social/99-acceptance-gate.md`

# Social Module — Final Acceptance Gate

> Tech Lead signs off before declaring the Social sprint closed (Sun 2026-11-22 17:00). Folder moves to `Agents/decisions/closed/Social/` only after every box is ticked.

---

## 1. Code Quality

- [ ] All 5 task PRs (T1, T2, T3, T4, T5) merged into `main`.
- [ ] `dotnet build` green for every Social project + 2 test projects (`08-cross-cutting.md §4`).
- [ ] `rg "TODO|FIXME|HACK" Social/` → 0 results (or all migrated to GitHub issues).
- [ ] **All command handlers** inject `ILogger<THandler>` + call `RemoveByTagAsync` after SaveChanges (sample 3).
- [ ] **All queries** implement `ICacheableQuery` (sample 3).
- [ ] **ICurrentUser audit** (S-R8) — only 12 documented handlers inject it; all others compile without it.
- [ ] **No bare `RequireAuthorization()`** in any new Presentation endpoint.
- [ ] `Social.Tests.Unit` green — minimum **45 tests** (12 reviews + 8 favorites + 10 reports + 4 cleanup + 8 rating + 3 misc).
- [ ] `Social.IntegrationTests` green — minimum **10 tests** (5 outbox round-trip + 3 cross-module inbox + 2 BG live-test).
- [ ] **Permission catalog parity test passes**.

---

## 2. Endpoint Smoke Test (manual Postman)

A reviewer runs this checklist live, records results in `Social/_smoke-test-runbook.md` (deleted before folder move).

| # | Method | Path | Expected | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/reviews` (verified booking) | 201 + IsVerifiedBooking=true | T1 |
| 2 | POST | `/api/v1/reviews` (no booking) | 201 + IsVerifiedBooking=false | T1 |
| 3 | POST | `/api/v1/reviews` (profanity) | 201 + Status=AwaitingModeration | T1 |
| 4 | POST | duplicate review same target | 409 `Review.AlreadyExists` | T1 |
| 5 | PUT | review at 47h | 200 | T1 |
| 6 | PUT | review at 49h | 422 `Review.EditWindowExpired` | T1 |
| 7 | DELETE | own review | 204 | T1 |
| 8 | POST | reply by tour-owning provider | 201 | T1 |
| 9 | POST | reply by non-owning provider | 403 | T1 |
| 10 | GET | `/api/v1/reviews/my-reviews` | 200 + cursor envelope | T1 |
| 11 | GET | `/api/v1/reviews/admin/flagged` | 200 + only AutoHidden + AwaitingModeration | T1 |
| 12 | POST | `/api/v1/favorites` (tour exists) | 201 | T2 |
| 13 | POST | favorite for deleted tour | 400 `Favorite.InvalidEntityType` | T2 |
| 14 | POST | duplicate favorite | 409 | T2 |
| 15 | POST | 501st favorite | 422 `Favorite.LimitExceeded` | T2 |
| 16 | DELETE | favorite that exists | 204 | T2 |
| 17 | DELETE | favorite that doesn't exist | 204 (idempotent) | T2 |
| 18 | GET | `/api/v1/favorites` | 200 + cursor + filter | T2 |
| 19 | GET | `/api/v1/favorites/check/Tour/{id}` | 200 + `{isFavorited:bool}` | T2 |
| 20 | POST | `/api/v1/reports` (review, valid reason) | 201 | T4 |
| 21 | POST | 5th report on same review | 201 + review auto-hidden (verify via `GET admin/flagged`) | T4 |
| 22 | POST | duplicate report by same user | 409 `Report.AlreadyReported` | T4 |
| 23 | POST | resolve report Dismiss | 200 | T4 |
| 24 | POST | resolve report RemoveContent | 200 + review deleted | T4 |
| 25 | POST | approve auto-hidden review | 200 + review restored | T4 |
| 26 | POST | remove flagged review | 200 + permanent delete | T4 |
| 27 | GET | `/api/v1/reports/admin` | 200 + cursor + filters | T4 |
| 28 | GET | `/api/v1/moderation/logs` | 200 + cursor + filters | T4 |

Any RED row blocks sign-off.

---

## 3. Outbox / Inbox Round-Trip

| Action | Outbox row | Logical name | Downstream side-effect | SLA |
|---|---|---|---|---|
| Publish review (T1) | 1 row | `social.review.published.v1` | Messaging notifies tour owner; Analytics writes UserInteraction.ReviewSubmitted | 30s |
| Delete review (T1) | 1 row | `social.review.deleted.v1` | ContentTours / ContentPlaces inbox trigger rating recalc; Analytics audit row | 30s |
| Add favorite (T2) | 1 row | `social.favorite.added.v1` | Messaging subscribes user to discount notifications on entity; Analytics writes UserInteraction.AddToFavorite | 30s |
| Resolve report (T4) | 1 row | `social.report.resolved.v1` | Messaging notifies reporter of outcome | 30s |
| Daily rating recalc (T5) | N rows | `social.rating.recalculated.v1` per entity | ContentTours.Tour.AverageRating updated; ContentPlaces.Place / Business equivalents | 60s |

**Inbox round-trip:**
| Action | Inbox row | Side-effect |
|---|---|---|
| Booking publishes `tour-booking.completed.v1` | row in `social.InboxMessages` | `BookingEligibilitySnapshot` upserted within 30s |
| ContentPlaces publishes `place.deleted.v1` | row in inbox | `PlaceSnapshot.IsDeleted = true` within 30s |
| ContentTours publishes `tour.deleted.v1` | row in inbox | `TourSnapshot.IsDeleted = true` within 30s |

---

## 4. Background Services Live Test (24h soak)

- [ ] `SELECT COUNT(*) FROM social.Favorites f LEFT JOIN social.TourSnapshots t ON t.TourId = f.EntityId WHERE f.EntityType = 0 AND f.IsDeleted = 0 AND (t.TourId IS NULL OR t.IsDeleted = 1)` → 0 (after one Saturday tick).
- [ ] Daily 03:00 UTC: `SELECT COUNT(*) FROM social.EntityRatingCache WHERE LastRecalculatedAt < SYSUTCDATETIME() - INTERVAL '25 hours'` → 0 (all entities with reviews recalculated within last 24h).
- [ ] OTEL: `bg_service_failures_total{service=OrphanedFavoritesCleanupService}` = 0.
- [ ] OTEL: `bg_service_ticks_total{service=RatingRecalculationService}` = 1 per 24h.
- [ ] No `[ERROR]`-level Serilog entries from either service in 24h.

---

## 5. Performance Sanity (p95)

| Endpoint | p95 target | Hard ceiling |
|---|---|---|
| POST `/reviews` | < 300 ms | < 600 ms |
| PUT `/reviews/{id}` | < 200 ms | < 500 ms |
| GET `/reviews/my-reviews` | < 100 ms (cached) | < 300 ms |
| GET `/reviews/admin/flagged` | < 150 ms (cached) | < 400 ms |
| POST `/favorites` | < 200 ms | < 500 ms |
| GET `/favorites` | < 80 ms (cached) | < 250 ms |
| GET `/favorites/check/{type}/{id}` | < 40 ms (cached) | < 150 ms |
| POST `/reports` | < 250 ms | < 600 ms |
| POST `/reports/admin/{id}/resolve` | < 300 ms | < 700 ms |
| RatingRecalculationService full sweep (10K entities) | < 60s | < 5 min |
| OrphanedFavoritesCleanup full sweep | < 30s | < 2 min |

SQL profiler: no query > 100 ms in the test (excluding the rating recalc batch).

---

## 6. Documentation Hygiene

- [ ] Every new endpoint has XML doc summary + `<param>` + `<returns>`.
- [ ] All 19 Social permissions listed in `Agents/permissions-inventory.md`.
- [ ] **Sprint folder moves to `Agents/decisions/closed/Social/`:**
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Social" -Destination "Agents\decisions\closed\Social"
  ```
- [ ] Master `Phase1-Phase2-Completion-INDEX.md` §1 row for Social marked 🟢 + linked to closed/ path.
- [ ] `AGENTS.md` gets a Social/ line entry.
- [ ] `agent-context.md §11.1` Social row updated to ✅ Phase 1 (PDF 1 Wave 6 first half).
- [ ] `Agents/error-log.md` gets new entries for any gotchas hit (especially Bayesian recency edge cases, snapshot-staleness races).
- [ ] ADR-007 considered: "Why we keep EntityRatingCache as IAggregateRoot (vs transient batch aggregate)" — file decision in `Agents/decisions/`.

---

## 7. Sprint Retro & Demo (Fri 2026-11-27 11:00 AST)

15-min demo by Mahmoud:
1. Live POST review with verified booking → outbox row → Messaging notification.
2. POST 5 reports rapid-fire → review auto-hides → admin queue shows it → admin approve restores.
3. RatingRecalculationService manual trigger (if T7 bonus endpoint added) → show new AverageRating + BayesianScore.
4. OrphanedFavoritesCleanupService dry-run query showing orphan count, then real run.

Retro doc at `Agents/decisions/closed/Social/_retro.md`:
- What went well (≥ 3 items)
- What hurt (≥ 3 items + linked error-log.md entries)
- Action items for next sprint (Messaging — likely scheduled Mon 2026-11-30 kickoff)

---

## 8. Sign-Off Block

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 Reviews owner | Mahmoud | _____ | _____ |
| T2 Favorites owner | Fadwa | _____ | _____ |
| T3 OrphanedFavoritesCleanup owner | Fadwa | _____ | _____ |
| T4 Reports + Moderation owner | Mohammad | _____ | _____ |
| T5 RatingRecalculation owner | Mohammad | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |

Once collected, folder moves, INDEX updates, module marked ✅ Phase 1 in `agent-context.md §11.1`, Messaging sprint kickoff scheduled.

---

