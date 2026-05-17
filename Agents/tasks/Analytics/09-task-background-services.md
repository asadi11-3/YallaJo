# TASK 3 — PopularityScoreCalculationService (Background Service)

> **Owner:** Mohammad — **Hours:** 14h — **Hard deadline:** Sun **2027-02-14 17:00**
> **Earliest start:** Wed 2027-01-20
> **Services:** 1 BG service (every 6h)
> **Depends on:** PW-1..PW-9, T2 PopularityScore aggregate, T1 UserInteraction repo

---

## 1. Service catalog

| Service | Cadence | Project | Trigger event(s) emitted |
|---|---|---|---|
| `PopularityScoreCalculationService` | every **6 hours** | Analytics.Infrastructure | `PopularityScoresRecalculatedDomainEvent` (1× per tick) → `analytics.popularity-scores.recalculated.v1` |

---

## 2. Algorithm

Each tick:
1. **Compute global stats** (for Bayesian context — borrowed from Social.RatingRecalculationService pattern).
2. **Find stale or oldest scores** — top 500 from `PopularityScores WHERE IsStale=1 OR LastRecalculatedAt < now-6h` ORDER BY (IsStale DESC, LastRecalculatedAt ASC).
3. **For each entity in the batch:**
   - Query `UserInteractions` in last 90 days with the score formula (A-R3).
   - SQL aggregate (NOT LINQ — perf):
     ```sql
     SELECT
       SUM(CASE WHEN InteractionType = 0 THEN POWER(0.5, DATEDIFF(DAY, OccurredAt, @now) / 30.0) * 1.0 ELSE 0 END) AS ViewScore,
       SUM(CASE WHEN InteractionType = 1 THEN POWER(0.5, DATEDIFF(DAY, OccurredAt, @now) / 30.0) * 2.0 ELSE 0 END) AS ClickScore,
       SUM(CASE WHEN InteractionType = 3 THEN POWER(0.5, DATEDIFF(DAY, OccurredAt, @now) / 30.0) * 5.0 ELSE 0 END) AS FavoriteScore,
       SUM(CASE WHEN InteractionType = 5 THEN POWER(0.5, DATEDIFF(DAY, OccurredAt, @now) / 30.0) * 8.0 ELSE 0 END) AS BookingStartedScore,
       SUM(CASE WHEN InteractionType = 6 THEN POWER(0.5, DATEDIFF(DAY, OccurredAt, @now) / 30.0) * 15.0 ELSE 0 END) AS BookingCompletedScore,
       SUM(CASE WHEN InteractionType = 9 THEN POWER(0.5, DATEDIFF(DAY, OccurredAt, @now) / 30.0) * 4.0 ELSE 0 END) AS ReviewScore,
       COUNT(*) AS InteractionCount
     FROM analytics.UserInteractions
     WHERE EntityType = @entityType AND EntityId = @entityId AND OccurredAt > DATEADD(DAY, -90, @now)
     ```
   - Add RatingBonus from social.EntityRatingCache (joined snapshot).
   - Call `popularityScore.Recalculate(newScore, interactionCount, now)`.
4. **Snapshot current scores** for trending DELTA — call `EntityPopularitySnapshotRepository.SnapshotCurrentAsync(now)`.
5. **Compute trending DELTA** (A-R4):
   - For each EntityType in {Tour, Place, Business}:
     - Read snapshots from 7 days ago.
     - Compute `delta = currentScore - snapshotScore_7d` per entity.
     - Sort descending. Top 50 → SetTrendingRank(1..50).
     - Rest → SetTrendingRank(null) if not already null.
6. **Cleanup IngestDebounceMarker** rows older than 1h (for T2 stale-flag dedup).
7. **Single SaveChanges + outbox emit** `analytics.popularity-scores.recalculated.v1` + `analytics.trending.refreshed.v1`.
8. **Update OpenTelemetry counters:** `popularity_scores_recalculated_total`, `trending_refreshed_total`.

**Budget:** full sweep of ~10K scored entities should complete in < 60 seconds. Pretest with 100K via load-test fixture.

---

## 3. Implementation skeleton

```csharp
internal sealed class PopularityScoreCalculationService(
    IServiceProvider services,
    ILogger<PopularityScoreCalculationService> logger,
    TimeProvider time,
    IOptions<PopularityCalculationOptions> options
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(options.Value.InitialDelay, stoppingToken);
        using var timer = new PeriodicTimer(options.Value.Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = services.CreateScope();
            try
            {
                await RunBatchAsync(scope.ServiceProvider, stoppingToken);
                AnalyticsDiagnostics.PopularityRecalcTicks.Add(1);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "PopularityScoreCalculationService tick failed; retry next interval");
                AnalyticsDiagnostics.PopularityRecalcFailures.Add(1);
            }
        }
    }

    private async Task RunBatchAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var popRepo = scoped.GetRequiredService<IPopularityScoreRepository>();
        var snapshotRepo = scoped.GetRequiredService<IEntityPopularitySnapshotRepository>();
        var interactionRepo = scoped.GetRequiredService<IUserInteractionRepository>();
        var uow = scoped.GetRequiredService<IAnalyticsUnitOfWork>();
        var now = time.GetUtcNow().UtcDateTime;

        var stale = await popRepo.GetStaleAsync(options.Value.BatchSize, ct);
        foreach (var score in stale)
        {
            var (newScore, count) = await interactionRepo.AggregateScoreAsync(score.EntityType, score.EntityId, now, ct);
            score.Recalculate(newScore, count, now);
        }

        // snapshot today for trending tomorrow
        await snapshotRepo.SnapshotCurrentAsync(now, ct);

        // compute trending DELTA
        foreach (var type in Enum.GetValues<EntityType>())
        {
            var trending = await popRepo.GetTrendingAsync(type, limit: 50, windowDays: 7, ct);
            int rank = 1;
            foreach (var t in trending)
            {
                t.Score.SetTrendingRank(rank++, now);
            }
        }

        await uow.SaveChangesAsync(ct);
        logger.LogInformation("PopularityScoreCalc tick: recalculated {Count} entities", stale.Count);
    }
}

public sealed record PopularityCalculationOptions
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(6);
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(2);
    public int BatchSize { get; init; } = 500;
    public bool Enabled { get; init; } = true;
}
```

---

## 4. DI Registration

```csharp
services.Configure<PopularityCalculationOptions>(cfg.GetSection("Analytics:BackgroundServices:PopularityCalculation"));
services.AddHostedService<PopularityScoreCalculationService>();
```

`appsettings.json` defaults:
```json
"Analytics": {
  "BackgroundServices": {
    "PopularityCalculation": {
      "Interval": "06:00:00",
      "InitialDelay": "00:02:00",
      "BatchSize": 500,
      "Enabled": true
    }
  },
  "Popularity": {
    "Coefficients": {
      "View": 1.0,
      "Click": 2.0,
      "Favorite": 5.0,
      "BookingStarted": 8.0,
      "BookingCompleted": 15.0,
      "Review": 4.0,
      "RatingBonusThreshold": 3.5,
      "RatingBonusMultiplier": 10.0,
      "RecencyHalfLifeDays": 30
    }
  }
}
```

`appsettings.Development.json` overrides:
- `Interval = 00:05:00` (every 5 minutes for fast feedback)
- `BatchSize = 50`

---

## 5. Manual trigger admin endpoint (BONUS — +2h if budget allows)

`POST /api/v1/admin/analytics/popularity/trigger` — fires `RunBatchAsync` immediately for ops sanity. Requires `MustHavePermission(AdminDashboard, Refresh)`.

Same locking pattern as Booking's admin BG triggers (10-task §7).

---

## 6. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | PopularityCalculationOptions + appsettings | 1 | 2027-01-22 |
| 2 | IUserInteractionRepository.AggregateScoreAsync impl (SQL FromSql) | 3 | 2027-01-26 |
| 3 | IEntityPopularitySnapshotRepository.SnapshotCurrentAsync impl | 2 | 2027-01-29 |
| 4 | IPopularityScoreRepository.GetStaleAsync + GetTrendingAsync impls | 2 | 2027-02-02 |
| 5 | PopularityScoreCalculationService BG impl | 2 | 2027-02-06 |
| 6 | Diagnostics counters + DI | 1 | 2027-02-08 |
| 7 | Integration test: 100 interactions → tick → PopularityScore updated | 2 | 2027-02-12 |
| 8 | Manual trigger admin endpoint (BONUS) | 1 | 2027-02-14 |
| **Total** | | **14h** | **Sun 2027-02-14** |

---

## 7. Acceptance tests (8+)

1. Tick with no stale rows → no-op, no SaveChanges, log debug.
2. Tick with 500 stale rows → all updated, single SaveChanges, outbox row appears.
3. Tick with 0 interactions for entity → Score=0, IsStale=false.
4. Tick with mixed interactions → Score calculated per A-R3 formula.
5. EntityPopularitySnapshots row inserted per (EntityType, EntityId, TakenAt=today).
6. Trending DELTA computes correctly when 7-day-old snapshot exists.
7. Trending fallback (no 7d snapshot) uses currentScore × 0.5 penalty.
8. Service down for 24h → backlog (4 missed ticks of stale work) caught up over next 4 ticks; no data loss.
9. Concurrent endpoint /admin/analytics/popularity/trigger + scheduled tick → no overlap (lock prevents).
10. OpenTelemetry counters increment correctly.
