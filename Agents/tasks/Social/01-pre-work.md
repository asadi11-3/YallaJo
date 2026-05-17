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
