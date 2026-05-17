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
