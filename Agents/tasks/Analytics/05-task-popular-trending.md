# TASK 2 — Popular & Trending

> **Owner:** Mohammad (Intermediate Lead) — **Hours:** 22h (popular/trending endpoints + score init/stale-flag inbox) — **Hard deadline:** Sun **2027-02-07 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 4 + ~4 stale-flag inbox handlers
> **Depends on:** PW-1..PW-9, T3 BG service (parallel — endpoints work against PopularityScore directly even if BG hasn't recalc'd yet)

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/popular/tours` | .AllowAnonymous() |
| 2 | GET | `/api/v1/popular/places` | .AllowAnonymous() |
| 3 | GET | `/api/v1/trending` | .AllowAnonymous() |
| 4 | GET | `/api/v1/popular/businesses` | .AllowAnonymous() |

All accept `?limit=` (default 20, max 50) and `?categoryId=` (filter).

---

## 2. PopularityScore aggregate (in scope)

```csharp
public sealed class PopularityScore : AuditableEntity, IAggregateRoot
{
    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public decimal Score { get; private set; }            // A-R3 formula result
    public int? TrendingRank { get; private set; }        // 1..50 set by T3 BG service for trending entries; null otherwise
    public DateTime? LastRecalculatedAt { get; private set; }
    public bool IsStale { get; private set; }             // flagged by stale-flag handlers; T3 BG picks up stale rows first
    public int InteractionCountSnapshot { get; private set; }   // diagnostic
    public decimal? CategoryRankPercentile { get; private set; }  // 0.0..1.0, computed by T3 — useful for "Top 10% in Adventure"

    private PopularityScore() { }   // EF
    public static PopularityScore Initialize(EntityType type, Guid entityId, DateTime now)
    {
        var entity = new PopularityScore { EntityType = type, EntityId = entityId, Score = 0m, LastRecalculatedAt = now };
        entity.RaiseDomainEvent(new PopularityScoreInitializedDomainEvent(entity.Id, type, entityId));
        return entity;
    }

    public void Recalculate(decimal newScore, int interactionCount, DateTime now)
    {
        if (Math.Abs(Score - newScore) < 0.0001m && InteractionCountSnapshot == interactionCount) return;
        Score = newScore;
        InteractionCountSnapshot = interactionCount;
        LastRecalculatedAt = now;
        IsStale = false;
        MarkUpdated(now);
        RaiseDomainEvent(new PopularityScoreRecalculatedDomainEvent(Id, EntityType, EntityId, newScore));
    }

    public void MarkStale(DateTime now)
    {
        if (IsStale) return;
        IsStale = true;
        MarkUpdated(now);
        RaiseDomainEvent(new PopularityScoreStaleFlaggedDomainEvent(Id, EntityType, EntityId));
    }

    public void SetTrendingRank(int? rank, DateTime now)
    {
        if (rank == TrendingRank) return;
        TrendingRank = rank;
        MarkUpdated(now);
    }

    public void SoftDelete(DateTime now)
    {
        IsDeleted = true;
        DeletedAt = now;
        RaiseDomainEvent(new PopularityScoreSoftDeletedDomainEvent(Id, EntityType, EntityId));
    }
}
```

---

## 3. Endpoint #1/#2/#4 — Popular reads

```csharp
public sealed record GetPopularTours(int Limit = 20, Guid? CategoryId = null)
    : IQuery<Result<IReadOnlyList<PopularEntityDto>>>, ICacheableQuery
{
    public string CacheKey => $"popular:tours:limit={Limit}:cat={CategoryId?.ToString() ?? "all"}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyCollection<string> CacheTags => ["popular:tours", $"popular:tours:cat:{CategoryId}"];
}

public sealed record PopularEntityDto(
    Guid EntityId,
    EntityType EntityType,
    string Name,                  // joined from BookingTourSnapshot or PlaceSnapshot
    string? PrimaryImageUrl,
    decimal Score,
    decimal? AverageRating,
    int? ReviewCount,
    int Rank
);
```

Handler joins PopularityScore with snapshot table (BookingTourSnapshot / PlaceSnapshot / ContentBusinessSnapshot) for display data — these read-snapshots are populated by inbox handlers in T2 inbox section §5.

**SQL:** `SELECT TOP @limit ps.*, t.Name, t.PrimaryImageUrl, t.AverageRating, t.ReviewCount FROM analytics.PopularityScores ps INNER JOIN booking.TourSnapshots t ON ps.EntityId = t.TourId WHERE ps.EntityType = 1 AND ps.IsDeleted = 0 AND (@categoryId IS NULL OR t.CategoryId = @categoryId) ORDER BY ps.Score DESC`

---

## 4. Endpoint #3 — GET /trending

```csharp
public sealed record GetTrendingQuery(int Limit = 20, EntityType? EntityType = null)
    : IQuery<Result<IReadOnlyList<TrendingEntityDto>>>, ICacheableQuery
{
    public string CacheKey => $"trending:{EntityType?.ToString() ?? "all"}:limit={Limit}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyCollection<string> CacheTags => ["trending"];
}

public sealed record TrendingEntityDto(
    Guid EntityId,
    EntityType EntityType,
    string Name,
    string? PrimaryImageUrl,
    decimal CurrentScore,
    decimal ScoreDelta,           // A-R4
    int TrendingRank,
    decimal PercentChange
);
```

**SQL filter:** `WHERE TrendingRank IS NOT NULL ORDER BY TrendingRank ASC LIMIT @limit`. TrendingRank is computed + set by T3 BG service per A-R4.

**If TrendingRank IS NULL across the board** (sprint just deployed, < 14 days uptime), return empty list + 503 with code `Trending.WindowNotReady`. Frontend shows "Trending data collecting — check back tomorrow" message.

---

## 5. ~4 stale-flag inbox handlers + 3 score-init inbox handlers + ~2 snapshot maintenance handlers

### Stale-flag handlers (when entity state changes that should re-trigger popularity calc):

| Inbox event | Action |
|---|---|
| `booking.tour-booking.completed.v1` | `popularityScoreRepo.MarkStaleAsync(EntityType.Tour, TourId)` |
| `social.review.published.v1` | MarkStale on EntityType based on TargetType |
| `social.rating.recalculated.v1` | MarkStale on EntityType.{Tour/Place/Business} |
| `social.favorite.added.v1` | MarkStale (skip if IngestDebounceMarker says <30s ago) |

**Debounce via `IngestDebounceMarker`:** before calling MarkStale, check the marker table. If marker exists with `LastFlagged > now-30s`, skip. Otherwise upsert marker + raise stale flag. Cleanup: rows older than 1h removed by T3 BG service at end of run.

### Score-init handlers (initialize PopularityScore row on entity birth):

| Inbox event | Action |
|---|---|
| `content-tours.tour.published.v1` | `PopularityScore.Initialize(Tour, TourId)` if not exists |
| `content-places.place.created.v1` | `PopularityScore.Initialize(Place, PlaceId)` if not exists |
| `content-places.business.created.v1` | `PopularityScore.Initialize(Business, BusinessId)` if not exists |

### Soft-delete handlers (cleanup on entity removal):

| Inbox event | Action |
|---|---|
| `content-tours.tour.deleted.v1` | `popularityScore.SoftDelete()` |
| `content-places.place.deleted.v1` | `popularityScore.SoftDelete()` |

All handlers follow the standard pattern: HasBeenProcessedAsync → entity operation → MarkAsProcessed → single SaveChanges.

---

## 6. Cache invalidation

`analytics.popularity-scores.recalculated.v1` outbox (from T3) consumed by an internal handler in this module:

```csharp
internal sealed class InvalidatePopularityCachesOnRecalcHandler(HybridCache cache)
    : INotificationHandler<PopularityScoresRecalculatedDomainEvent>
{
    public async Task Handle(PopularityScoresRecalculatedDomainEvent _, CancellationToken ct)
    {
        await Task.WhenAll(
            cache.RemoveByTagAsync("popular:tours", ct).AsTask(),
            cache.RemoveByTagAsync("popular:places", ct).AsTask(),
            cache.RemoveByTagAsync("popular:businesses", ct).AsTask(),
            cache.RemoveByTagAsync("trending", ct).AsTask()
        );
    }
}
```

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | PopularityScore aggregate methods + EF config | 3 | 2027-01-22 |
| 2 | EntityPopularitySnapshot entity + EF config | 1 | 2027-01-22 |
| 3 | IngestDebounceMarker entity + repo | 1 | 2027-01-23 |
| 4 | GET /popular/{tours,places,businesses} handler + endpoint + cache | 5 | 2027-01-28 |
| 5 | GET /trending handler + endpoint + cache | 3 | 2027-01-30 |
| 6 | 4 stale-flag inbox handlers | 3 | 2027-02-02 |
| 7 | 3 score-init inbox handlers | 2 | 2027-02-04 |
| 8 | 2 soft-delete inbox handlers | 1 | 2027-02-05 |
| 9 | Cache invalidation handler | 1 | 2027-02-05 |
| 10 | Unit + integration tests (10+) | 2 | 2027-02-07 |
| **Total** | | **22h** | **Sun 2027-02-07** |

---

## 8. Acceptance tests (10+ minimum)

1. GET /popular/tours empty (fresh deploy) → 200 + empty array.
2. GET /popular/tours with 100 scored → returns top 20 sorted DESC.
3. GET /popular/tours with categoryId filter → only that category.
4. GET /trending sprint just deployed → 503 + Trending.WindowNotReady.
5. GET /trending with TrendingRank populated → returns top 20 by rank.
6. Inbox tour.published → PopularityScore row created with Score=0.
7. Inbox tour.published delivered twice → no duplicate row.
8. Inbox review.published → MarkStale called, IsStale=true.
9. Inbox favorite.added × 10 within 30s → only first call propagates (debounce).
10. PopularityScore.Recalculate with same values → no domain event raised.
11. Cache invalidation on PopularityScoresRecalculatedDomainEvent → next GET returns fresh data.
12. SoftDelete tour → next GET /popular/tours excludes the entity.
