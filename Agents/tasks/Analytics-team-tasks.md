# Analytics Module — Combined Sprint Task File

> **Sprint window:** Mon 2027-01-18 → Fri 2027-02-26 (6 weeks, 30 working days, 130 person-hours)
> **Combined from 12 separate files** in `Agents/tasks/Analytics/` for single-file review.

---

## Table of Contents

- [00-README](#00-readme)
- [01-pre-work](#01-pre-work)
- [02-critical-rules](#02-critical-rules)
- [03-entities-matrix](#03-entities-matrix)
- [04-task-interactions-ingest](#04-task-interactions-ingest)
- [05-task-popular-trending](#05-task-popular-trending)
- [06-task-admin-dashboards](#06-task-admin-dashboards)
- [07-task-provider-analytics](#07-task-provider-analytics)
- [08-task-audit-log-query](#08-task-audit-log-query)
- [09-task-background-services](#09-task-background-services)
- [10-cross-cutting](#10-cross-cutting)
- [99-acceptance-gate](#99-acceptance-gate)

---

<a id="00-readme"></a>

## 00-README

> Source: `Analytics/00-README.md`

# Analytics — Wave 6 (Phase 2) Module Sprint

> **Predecessor sprint:** Messaging (`Agents/decisions/closed/Messaging/` once closed). Reuses the proven structure.
> **This sprint covers:** Phase 2 closure of the Analytics module — high-volume **UserInteractions** ingest, **PopularityScores** recalculation, **Popular / Trending** discovery endpoints, **admin & provider dashboards**, **audit log** API. Excludes Phase 4 recommendation engine (feature 21) and user preferences (feature 24).
> **Difficulty vs Messaging:** ⚙️⚙️⚙️ (3/5) — simpler than Messaging (no SignalR, no email reliability puzzle, no SLA round-robin). The hard parts are: (a) high-write-throughput ingest design without bottlenecking the BIGINT PK, (b) recency-window aggregation queries that stay sub-second on millions of rows, (c) the Bayesian-style trending DELTA calculation, (d) audit log redaction rules.
> **Endpoint count:** **17 HTTP endpoints + 1 BG service (PopularityScoreCalculationService 6h)**.
> **Working-day estimate:** **30 working days × 4 devs ≈ 130 person-hours**.

---

## 0. Sprint Window & Hard Deadlines

| Milestone | Date | Time (AST) | Owner |
|---|---|---|---|
| Pre-work cut (PR window opens) | Fri 2027-01-15 | 17:00 | Tech Lead |
| Sprint kickoff (standup #1) | Mon 2027-01-18 | 09:30 | All |
| Pre-work PRs merge deadline | Tue 2027-01-19 | 17:00 | Tech Lead |
| Earliest task PRs may open | Wed 2027-01-20 | 09:00 | All |
| Mid-sprint integration freeze | Sun 2027-02-14 | 17:00 | Tech Lead |
| **Hard PR cutoff** | Wed 2027-02-24 | 17:00 | All |
| **Hard merge-to-main cutoff** | Thu 2027-02-25 | 17:00 | Tech Lead |
| Sprint retro + demo | Fri 2027-02-26 | 11:00 | All |

Working week Sun→Thu (5 days). Daily standup 09:30 AST 15 min hard cap. Three sentences per dev (yesterday/today/blockers). Miss-2 rule applies.

---

## 1. Working Days & Person-Hour Budget

| Item | Hours |
|---|---|
| Working days | 30 |
| Hours/day/dev | 6 (focused) |
| Devs | 4 (3 builders + 1 Tech Lead pre-work + review) |
| **Total available** | **6×30×3 + 16 TL = 556** |
| Task hours (sum of T1..T6) | 110 |
| Review hours (TL) | 14 |
| Ceremony hours (standups + retro) | 6 |
| **Sprint floor** | **130** |
| Buffer | 426 |

Generous buffer reserved for: high-write ingest tuning (UserInteractions tends to surprise on first prod load), dashboard query plan optimization, audit log redaction false-positive triage, parallel sprints (Authorization-Cleanup may run concurrently with Analytics if Tech Lead has capacity).

---

## 2. Team Members & High-Level Allocation

| Name | Level | Tasks | Endpoints | BG services | Est. hours | Hard deadline |
|---|---|---|---|---|---|---|
| **Mahmoud** | Intermediate | T1 Interactions ingest + read endpoints | 3 | 0 | 24 | Sun 2027-01-31 |
| **Mohammad** | Intermediate (Lead) | T2 Popular/Trending + T3 PopularityScoreCalc BG | 4 | 1 | 36 | Sun 2027-02-14 |
| **Fadwa** | Beginner→Intermediate | T4 Admin Dashboards (kid gloves on aggregation queries) | 4 | 0 | 28 | Sun 2027-02-21 |
| **Mohammad / Mahmoud split** | — | T5 Provider Analytics dashboard | 3 | 0 | 14 | Sun 2027-02-21 |
| **Fadwa** | — | T6 Audit Logs query API + redaction | 3 | 0 | 14 | Sun 2027-02-21 |
| Tech Lead | — | PW-1..PW-7 + review + retro | — | — | 14 | continuous |
| **Sum** | | | **17** | **1** | **130** | |

**Pairing recommendation:** Fadwa pairs with Mohammad on first 2 days of T4 to set up the dashboard query helpers (window functions, GROUP BY ROLLUP, percentile calcs) — these are SQL she hasn't touched in YallaJo before. Once the SQL pattern is established, she runs solo.

---

## 3. Module Status After This Sprint

Per `agent-context.md` §11.1 — Analytics flips from ⬜ Empty to ✅ Phase 2 Complete.

**In scope (this sprint):**
- ✅ Interactions ingest (UserInteractions table, BIGINT PK) — fire-and-forget
- ✅ Popular tours/places (PopularityScores read API)
- ✅ Trending (DELTA last 7d vs previous 7d)
- ✅ Admin dashboards (revenue / bookings / users / time series)
- ✅ Provider analytics dashboard
- ✅ Audit log query API (AuditLogs table, retention 2 years, payment-data redacted)
- ✅ 1 BG: PopularityScoreCalculationService (6h)

**OUT OF SCOPE (deferred):**
- ❌ RecommendationCache + collaborative filtering (Phase 4 feature 21)
- ❌ UserPreferences + UserPreferredCategory CRUD (Phase 4 feature 24)
- ❌ A/B testing infrastructure (post-MVP)
- ❌ Real-time dashboard via SignalR (Phase 3) — v1 polls every 30s

---

## 4. Integration Events — Emitted & Consumed

### 4.1 Emitted by Analytics (3 outbox events)

| Logical name | Trigger | Payload |
|---|---|---|
| `analytics.popularity-scores.recalculated.v1` | PopularityScoreCalculationService finishes batch | `{recalculatedAt, entityCounts: {Tour, Place, Business}, durationMs}` |
| `analytics.audit-log.entry-redacted.v1` | Redaction job flags payment-data leak in old log | `{auditLogId, originalAction, redactedFields}` — admin alert |
| `analytics.trending.refreshed.v1` | After PopularityScoreCalc, trending deltas recomputed | `{refreshedAt, topMoversByEntityType}` |

### 4.2 Consumed by Analytics (~16 inbox handlers across modules)

| Source module | Logical name | Side effect |
|---|---|---|
| Booking | `booking.tour-booking.created.v1` | Insert UserInteraction(BookingStarted) + audit log row |
| Booking | `booking.tour-booking.confirmed.v1` | Insert UserInteraction(BookingCompleted) + audit log row |
| Booking | `booking.tour-booking.cancelled.v1` | Insert UserInteraction(BookingCancelled) + audit log row |
| Booking | `booking.tour-booking.completed.v1` | Update PopularityScores stale-flag for tour |
| Finance | `finance.payment.completed.v1` | Audit log row (redacted) |
| Finance | `finance.payout.completed.v1` | Audit log row + dashboard cache invalidate |
| Finance | `finance.refund.completed.v1` | Audit log row |
| Social | `social.review.published.v1` | Update PopularityScores stale-flag |
| Social | `social.favorite.added.v1` | Insert UserInteraction(AddToFavorite) |
| Social | `social.rating.recalculated.v1` | Bump PopularityScore.LastRatingUpdate stamp |
| ContentTours | `content-tours.tour.published.v1` | Initialize PopularityScores row + UserPreference snapshot |
| ContentTours | `content-tours.tour.deleted.v1` | Soft-delete PopularityScores row |
| ContentPlaces | `content-places.place.created.v1` | Initialize PopularityScores row |
| ContentPlaces | `content-places.place.deleted.v1` | Soft-delete PopularityScores row |
| Auth | `auth.user.registered.v1` | Audit log row (no PII in body, just userId) |
| Accounts | `accounts.provider.status-changed.v1` | Audit log row |

**~16 inbox handlers** to write. Most are 1-liner audit-log inserts. The 4 PopularityScores stale-flag handlers need a small `IPopularityScoreStaleFlagService` to dedupe (don't write 50 stale flags for the same tour in 1 minute — debounce 30s).

---

## 5. File map of this folder

| File | Purpose |
|---|---|
| `00-README.md` | This file |
| `01-pre-work.md` | PW-1..PW-7 (Tech Lead drives) |
| `02-critical-rules.md` | Analytics-specific rules A-R1..A-R10 |
| `03-entities-matrix.md` | Aggregates + new entities + 7 migrations |
| `04-task-interactions-ingest.md` | T1 Mahmoud — 3 endpoints + ~3 inbox handlers |
| `05-task-popular-trending.md` | T2 Mohammad — 4 endpoints + ~4 stale-flag inbox handlers |
| `06-task-admin-dashboards.md` | T4 Fadwa — 4 endpoints + ~3 inbox handlers (audit/dashboard cache invalidation) |
| `07-task-provider-analytics.md` | T5 split — 3 endpoints |
| `08-task-audit-log-query.md` | T6 Fadwa — 3 endpoints + ~6 audit-log inbox handlers + redaction |
| `09-task-background-services.md` | T3 Mohammad — PopularityScoreCalculationService 6h |
| `10-cross-cutting.md` | DI / perms / outbox parity / migrations / performance / data retention |
| `99-acceptance-gate.md` | Final sign-off checklist |

On close: `Move-Item -LiteralPath "Agents\tasks\Analytics" -Destination "Agents\decisions\closed\Analytics"`.

---

<a id="01-pre-work"></a>

## 01-pre-work

> Source: `Analytics/01-pre-work.md`

# Analytics — Pre-Work (PW-1 .. PW-7)

> **Tech Lead drives every PW item.** Pre-work MUST land on `main` by **Tue 2027-01-19 17:00**. Task work CANNOT begin until then.

---

## PW-1 — `AnalyticsUnitOfWork` delegates to SharedKernel

Same bug pattern documented in `error-log.md` (Booking PW-1, Finance PW-1, etc.). `Analytics.Infrastructure/Persistence/AnalyticsUnitOfWork.cs` currently calls `_context.SaveChangesAsync` directly — must delegate to `IUnitOfWork<AnalyticsDbContext>` so domain events dispatch.

**Acceptance:** 2 unit tests in `tests/Analytics.Tests.Unit/Persistence/AnalyticsUnitOfWorkDispatchesEventsTests.cs` prove `IPublisher.Publish` is invoked for any aggregate with pending events.

---

## PW-2 — IAggregateRoot markers + AuditableEntity upgrades

| Entity | Current | Target | Reason |
|---|---|---|---|
| `UserInteraction` | BaseEntity (BIGINT PK) | **stays BaseEntity** — high-write, no business invariants | append-only stream |
| `PopularityScore` | BaseEntity | **AuditableEntity, IAggregateRoot** | recalc state machine, soft-delete |
| `AuditLog` | BaseEntity (BIGINT PK) | **stays BaseEntity** — append-only, no business invariants | immutable history |
| `RecommendationCache` | BaseEntity | **stays BaseEntity** + `[Obsolete("Phase 4 feature 21")]` | not implemented this sprint |
| `UserPreference` | BaseEntity | **stays BaseEntity** + `[Obsolete("Phase 4 feature 24")]` | not implemented this sprint |
| `UserPreferredCategory` | BaseEntity | **stays BaseEntity** + `[Obsolete("Phase 4 feature 24")]` | not implemented this sprint |

New entities (added by tasks):
- `EntityPopularitySnapshot` (T2) — BaseEntity, snapshot for trending DELTA calc
- `DashboardCache` (T4) — BaseEntity, pre-aggregated rollups
- `IngestDebounceMarker` (T1) — BaseEntity, transient table for stale-flag dedup

**Migration `AnalyticsAddAggregateRootAndAuditMembers`** — adds `IsDeleted bit NOT NULL DEFAULT 0`, `DeletedAt datetime2 NULL`, `RowVersion rowversion NOT NULL` to `PopularityScore` only. All other tables unchanged.

---

## PW-3 — Domain event records

Seed in `Analytics.Domain/Events/`:

| # | Event | Aggregate | Sprint task |
|---|---|---|---|
| 1 | `UserInteractionRecordedDomainEvent` | (none — fire from handler) | T1 |
| 2 | `PopularityScoreRecalculatedDomainEvent` | PopularityScore | T2/T3 |
| 3 | `PopularityScoreStaleFlaggedDomainEvent` | PopularityScore | T2 |
| 4 | `TrendingRefreshedDomainEvent` | (cross-cutting) | T2/T3 |
| 5 | `AuditLogEntryAppendedDomainEvent` | (none — append-only stream) | T6 |
| 6 | `AuditLogEntryRedactedDomainEvent` | (admin action) | T6 |
| 7 | `DashboardCacheInvalidatedDomainEvent` | DashboardCache | T4 |
| 8 | `DashboardCacheRebuiltDomainEvent` | DashboardCache | T4 |
| 9 | `PopularityScoreInitializedDomainEvent` | PopularityScore | T2 inbox |
| 10 | `PopularityScoreSoftDeletedDomainEvent` | PopularityScore | T2 inbox |

---

## PW-4 — Integration event records + IntegrationEventTypeRegistry

Seed in `Analytics.Contracts/IntegrationEvents/`:

| # | Logical name | Emitter | Payload key fields |
|---|---|---|---|
| 1 | `analytics.popularity-scores.recalculated.v1` | T3 BG service | RecalculatedAt, EntityCounts, DurationMs |
| 2 | `analytics.audit-log.entry-redacted.v1` | T6 redaction job | AuditLogId, OriginalAction, RedactedFields |
| 3 | `analytics.trending.refreshed.v1` | T3 BG service | RefreshedAt, TopMoversByEntityType |

Register in `IntegrationEventTypeRegistry` in `YallaJo.Api/Startup`. Add parity test `tests/Analytics.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs` (drift-proof reverse-parity pattern from prior sprints).

---

## PW-5 — Repository interfaces

Seed in `Analytics.Application/Interfaces/`:

```csharp
public interface IUserInteractionRepository
{
    Task AddBatchAsync(IReadOnlyList<UserInteraction> batch, CancellationToken ct);   // bulk SqlBulkCopy
    Task<long> CountByUserSinceAsync(Guid userId, DateTime since, CancellationToken ct);  // rate limiting
    Task<IReadOnlyList<UserInteraction>> GetByUserPagedAsync(Guid userId, BigIntCursor cursor, int limit, CancellationToken ct);
    Task<IReadOnlyList<EntityInteractionCount>> AggregateByEntityAsync(EntityType type, DateTime since, DateTime until, CancellationToken ct);
    Task<bool> ExistsRecentDuplicateAsync(Guid userId, EntityType type, Guid entityId, InteractionType interactionType, TimeSpan window, CancellationToken ct);  // 5-min dedupe per PDF
}

public interface IPopularityScoreRepository
{
    Task<PopularityScore?> GetByEntityAsync(EntityType type, Guid entityId, CancellationToken ct);
    Task<IReadOnlyList<PopularityScore>> GetTopAsync(EntityType type, int limit, CancellationToken ct);
    Task<IReadOnlyList<PopularityScore>> GetStaleAsync(int limit, CancellationToken ct);    // T3 BG service
    Task<IReadOnlyList<TrendingResult>> GetTrendingAsync(EntityType type, int limit, int windowDays, CancellationToken ct);
    Task UpsertAsync(PopularityScore entity, CancellationToken ct);
    Task MarkStaleAsync(EntityType type, Guid entityId, CancellationToken ct);
}

public interface IAuditLogRepository
{
    Task AppendAsync(AuditLog entry, CancellationToken ct);
    Task<IReadOnlyList<AuditLog>> QueryAsync(AuditLogFilter filter, BigIntCursor cursor, int limit, CancellationToken ct);
    Task<int> RedactPaymentDataAsync(IReadOnlyList<long> auditLogIds, CancellationToken ct);   // batch update
    Task<int> DeleteOlderThanAsync(DateTime threshold, int batchSize, CancellationToken ct);    // 2-year retention enforcement (cron — future)
}

public interface IDashboardCacheRepository
{
    Task<DashboardCache?> GetByKeyAsync(string cacheKey, CancellationToken ct);
    Task UpsertAsync(DashboardCache cache, CancellationToken ct);
    Task InvalidateByPatternAsync(string keyPattern, CancellationToken ct);   // e.g. "admin:dashboard:*"
}

public interface IEntityPopularitySnapshotRepository
{
    Task SnapshotCurrentAsync(DateTime takenAt, CancellationToken ct);   // T3 BG service captures current PopularityScores for trending DELTA
    Task<IReadOnlyDictionary<(EntityType, Guid), decimal>> GetSnapshotAtAsync(DateTime takenAt, CancellationToken ct);
}
```

**Custom finder gotcha:** `AggregateByEntityAsync` is the hot path for dashboards. MUST use `FromSqlInterpolated` with explicit indexes (UserInteraction.EntityType, UserInteraction.EntityId, UserInteraction.OccurredAt) — straight LINQ misses the covering index.

---

## PW-6 — `IClientContextProvider` abstraction

Audit log needs UserAgent + IP. `Analytics.Application` cannot know about HttpContext.

Add to `SharedKernel.Application/Abstractions/Http/`:

```csharp
public interface IClientContextProvider
{
    string? UserAgent { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }
}
```

Impl `HttpContextClientContextProvider` in `SharedKernel.Infrastructure/Http/` reads from `IHttpContextAccessor`. Returns nulls in BG service context (acceptable — `AuditLog.UserAgent` is nullable). Already follows existing pattern used by `ICurrentUser`.

DI in `YallaJo.Api` startup:
```csharp
services.AddHttpContextAccessor();
services.AddSingleton<IClientContextProvider, HttpContextClientContextProvider>();
```

---

## PW-7 — Permission catalog

`Analytics.Contracts/Authorization/AnalyticsFeatures.cs`:

```csharp
public static class AnalyticsFeatures
{
    public const string Interaction = nameof(Interaction);
    public const string PopularityScore = nameof(PopularityScore);
    public const string Trending = nameof(Trending);
    public const string AdminDashboard = nameof(AdminDashboard);
    public const string ProviderDashboard = nameof(ProviderDashboard);
    public const string AuditLog = nameof(AuditLog);
}
```

`Analytics.Contracts/Authorization/AnalyticsPermissionCatalog.cs` — 14 permissions:

| Feature | Action | Granted to roles |
|---|---|---|
| Interaction | Create | (none — endpoint is .AllowAnonymous for fire-and-forget ingest) |
| Interaction | Read | Admin |
| PopularityScore | Read | (none — endpoint .AllowAnonymous) |
| Trending | Read | (none — .AllowAnonymous) |
| AdminDashboard | Read | Admin |
| AdminDashboard | Refresh | Admin |
| ProviderDashboard | Read | Provider (self via ICurrentUser) |
| AuditLog | Read | Admin |
| AuditLog | Redact | Admin |
| AuditLog | Export | Admin |

Note: Several endpoints are `.AllowAnonymous()` because they're public (trending tours, popular places) — they still appear in the catalog with empty role list so the permission seeder records them, but the endpoints don't enforce them.

Expected boot log:
```text
[INFO] PermissionSeeder discovered 11 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social, Messaging, Analytics
[INFO] PermissionSeeder inserted/verified 14 Analytics permissions
```

---

## PW-8 — Test projects

`tests/Analytics.Tests.Unit/Analytics.Tests.Unit.csproj` (xunit 2.9.3 + NSubstitute 5.3.0 + FluentAssertions 7.0.0 + EF InMemory 9.0.15).
`tests/Analytics.IntegrationTests/Analytics.IntegrationTests.csproj` (Mvc.Testing 9.0.15 + WAF).

`InternalsVisibleTo` in `Analytics.Application.csproj` + `Analytics.Infrastructure.csproj`.

`<InternalsVisibleTo Include="Analytics.Tests.Unit" />`
`<InternalsVisibleTo Include="Analytics.IntegrationTests" />`

Add `public partial class Program;` to `YallaJo.Api/Program.cs` if not already present (this is done if Messaging sprint did it).

---

## PW-9 — High-volume ingest schema decisions

This is the schema-design PW. Tech Lead must finalize BEFORE T1 starts.

| Question | Decision |
|---|---|
| BIGINT vs UUID for UserInteraction PK? | **BIGINT IDENTITY** — sequential, write-friendly, smaller index pages |
| BIGINT vs UUID for AuditLog PK? | **BIGINT IDENTITY** — same reasoning, immutable log |
| Clustered index on UserInteraction? | **(OccurredAt DESC, Id ASC)** — time-range queries (most common) hit clustered index, no bookmark lookup |
| Partition by month? | **NO v1** — single table OK up to ~100M rows; revisit at 200K interactions/day for 18 months |
| Indexes on UserInteraction? | Covering `(UserId, OccurredAt DESC) INCLUDE (EntityType, EntityId, InteractionType)`, covering `(EntityType, EntityId, OccurredAt DESC) INCLUDE (UserId, InteractionType)` |
| AuditLog redaction strategy? | **In-place UPDATE** (vs append redaction-record) — simpler, AuditLog already has `RedactedAt datetime2 NULL` column added in PW |
| Batch insert API for UserInteraction? | **`SqlBulkCopy` via `IUserInteractionRepository.AddBatchAsync`** for batch size >100; fall back to standard `AddRange + SaveChanges` below |
| Where do we put trending DELTA snapshots? | **`EntityPopularitySnapshots` table (BaseEntity, BIGINT PK)** — daily snapshot row per (EntityType, EntityId), used by `GetTrendingAsync` to compute DELTA last 7 vs prev 7 |
| 2-year retention enforcement? | **NOT in this sprint** — manual or scheduled SQL Agent job; if added later it goes in 09-task-background-services as cleanup BG service |

**Acceptance:** `Analytics.Domain/Entities/` reflects these decisions. UserInteraction.cs explicitly documents the clustered-index assumption in XML doc comments.

---

## PW Sign-off

| PW | Owner | Status before Sun 2027-01-18 |
|---|---|---|
| PW-1 | TL | _____ |
| PW-2 | TL | _____ |
| PW-3 | TL | _____ |
| PW-4 | TL | _____ |
| PW-5 | TL | _____ |
| PW-6 | TL | _____ |
| PW-7 | TL | _____ |
| PW-8 | TL | _____ |
| PW-9 | TL | _____ |

PW retro Tue 2027-01-19 16:00 (1h) — TL walks team through schema + abstractions. Builders read this file beforehand.

---

<a id="02-critical-rules"></a>

## 02-critical-rules

> Source: `Analytics/02-critical-rules.md`

# Analytics — Critical Rules (A-R1 .. A-R10)

> Additive to `Phase1-Phase2-Completion-INDEX.md` §4. Reviewers reject PRs that violate any rule.

---

## A-R1 — Fire-and-Forget Ingest

**The POST /interactions endpoint MUST return 202 within 50ms.** It cannot:
- Wait for `SaveChangesAsync` (does an async-enqueue into a Channel<T>)
- Call any aggregation logic
- Touch PopularityScores
- Resolve user from JWT (the endpoint accepts both authenticated and anonymous traffic — guest interactions stamped via cookie SessionId)
- Call rate limit middleware (it's already attached at pipeline level; the handler trusts that)

**Implementation:** `IInteractionIngestQueue` (Singleton, in-memory `Channel<InteractionEnvelope>(capacity: 10_000, BoundedChannelFullMode.DropWrite)`). A separate `InteractionIngestDrainService : BackgroundService` consumes the channel and writes via `IUserInteractionRepository.AddBatchAsync` in 100-row batches every 1 second (or sooner if channel >50).

**On channel full (DropWrite):** increment counter `analytics_ingest_dropped_total`. This is acceptable degradation under burst — interaction data is statistical, not transactional.

**Audit log is NOT fire-and-forget.** Audit log writes ride on the transaction of whatever business action caused them (see Booking/Finance inbox handlers in T6). Only USER INTERACTIONS are fire-and-forget.

---

## A-R2 — Interaction Deduplication

Per PDF 1 Wave 6 — "rate limit deduplicate same user+entity+type within 5 min".

**Logic:** before enqueue, check `IUserInteractionRepository.ExistsRecentDuplicateAsync(userId, entityType, entityId, interactionType, 5min)`. If true, drop silently (still return 202 — clients don't need to know).

**Dedup cache:** uses HybridCache key `interaction-dedupe:{userId}:{entityType}:{entityId}:{type}` with 5-min absolute expiration. Cache hit = skip DB check + drop. Cache miss = check DB, set cache, accept-or-drop.

**Guest users:** dedup keyed by `SessionId` not UserId. Cookie expires 30d.

---

## A-R3 — Popularity Score Formula

```
PopularityScore.Score = 
    (Views × 1.0) +
    (Clicks × 2.0) +
    (Favorites × 5.0) +
    (BookingsStarted × 8.0) +
    (BookingsCompleted × 15.0) +
    (Reviews × 4.0) +
    (RatingBonus = 0 if AvgRating < 3.5, else (AvgRating - 3.5) × 10) +
    (RecencyDecay penalty: each interaction's weight × 0.5 ^ (daysAgo / 30))
```

Coefficients are config: `Analytics:Popularity:Coefficients` section. **Admin can tune without redeploy** (cache TTL 1 hour for coefficients).

Recency decay = half-life 30 days. Implemented in T3 BG service via window-function SQL not LINQ (perf).

**Score column:** `decimal(18,4)`. Range observed in practice 0.0 to ~10_000.0.

---

## A-R4 — Trending DELTA Calculation

Trending ≠ Popular (PDF 1 Wave 6). Trending = "highest score delta last 7 days vs previous 7 days, captures viral/seasonal spikes".

**Algorithm (in T3 BG service):**
1. Read current PopularityScores for entity type (top 200 by absolute score for performance).
2. Read EntityPopularitySnapshots taken 7 days ago for same entity IDs.
3. For each entity: `delta = currentScore - snapshotScore_7d_ago`. If snapshot missing (entity created <7d ago), `delta = currentScore × 0.5` (penalty for new-but-no-baseline).
4. Sort descending by delta. Top 50 stored back into PopularityScore.TrendingRank (1..50, null for rest).
5. After computing, snapshot today's scores into EntityPopularitySnapshots(takenAt=today) for tomorrow's run.

**Why score-delta not interaction-delta:** interaction counts double-count weighted events; score-delta already weights them per A-R3.

---

## A-R5 — Dashboard Cache Strategy

Admin dashboards aggregate over time windows that don't change retroactively (yesterday's revenue is fixed). Cache aggressively.

| Endpoint | Cache key | TTL | Invalidation trigger |
|---|---|---|---|
| `GET /admin/dashboard` | `admin:dashboard:overview` | 30s sliding | finance.payment.completed, booking.tour-booking.created |
| `GET /admin/dashboard/revenue?period=...` | `admin:dashboard:revenue:{periodHash}` | 30s sliding | finance.payment.completed, finance.refund.completed |
| `GET /admin/dashboard/bookings?period=...` | `admin:dashboard:bookings:{periodHash}` | 30s sliding | booking.tour-booking.{created,cancelled,confirmed} |
| `GET /popular/{tours,places}` | `popular:{entityType}` | 5min | analytics.popularity-scores.recalculated.v1 (every 6h) |
| `GET /trending` | `trending:{entityType}` | 5min | analytics.trending.refreshed.v1 (every 6h) |
| `GET /provider/analytics` | `provider:dashboard:{providerId}` | 60s sliding | finance.payout.completed.v1, booking.tour-booking.completed.v1 |
| `GET /audit-logs` (admin) | NEVER cache | — | always live |

**DashboardCache table:** for endpoints with TTL >30s that involve heavy aggregation (popular, trending), persist the JSON-serialized result in `analytics.DashboardCache(Key PK, ValueJson, ExpiresAt, RebuiltAt)`. HybridCache wraps it (L1 in-memory + DashboardCache as L2 across instances).

**Why `analytics.DashboardCache` instead of full Redis:** ADR-003 deferred Redis to Phase 3. SQL Server caching table is "good enough" for v1 traffic.

---

## A-R6 — Audit Log Redaction Rules

Per PDF 1 Wave 6 — "payment data redacted".

**Hard redact (never store) in `AuditLog.OldValue` / `NewValue`:**
- `cardNumber` / `cvv` / `expiryDate` / `cardholderName` (PCI)
- `password` / `passwordHash` / `secret` / `apiKey`
- `webhookSignature` / `accessToken` / `refreshToken`

**Soft redact (store hash only) — for forensics correlation without exposure:**
- `email` (store `SHA256(email)[:16]` prefix)
- `phone` (store last-4 only)
- `ipAddress` (store /24 prefix only)

**Implementation:** `IAuditLogRedactor` (Application interface, Infrastructure impl using string scanners + JSON traversal). Called by every inbox handler before `AppendAsync`. Reusable across modules — lives in `SharedKernel.Application/Abstractions/Audit/` so Finance/Booking can use the same redactor in their own future audit writes.

**Retroactive redaction:** T6 includes a `POST /audit-logs/{id}/redact` admin endpoint that re-runs the redactor + flags `RedactedAt = now`. Emits `analytics.audit-log.entry-redacted.v1` to alert other admins (transparency).

---

## A-R7 — Audit Log Retention

**Retention: 2 years** per PDF 1 Wave 6.

**THIS SPRINT:** schema + query API. Retention enforcement DEFERRED — runs once via SQL Agent or manual cron until v2 BG service. Add a `Decision Required` annotation in `10-cross-cutting.md` so ops knows.

**Reason for deferring:** clean retention is straightforward bulk `DELETE TOP @batch WHERE OccurredAt < @threshold`. Doesn't need full BG-service framing v1.

---

## A-R8 — Cursor Pagination

Same shape as Booking/Finance/Social/Messaging (INDEX §4 R9). Differences:
- UserInteraction + AuditLog use **BIGINT IDENTITY PK**, so cursor encodes `(Id BIGINT, OccurredAt DateTime)` not `(Guid, DateTime)`.
- Special `BigIntCursor` record in `Analytics.Application/Pagination/`.
- PageSize clamped [1,50], default 20 — same as other modules.
- AuditLog default sort: OccurredAt DESC (newest first).
- PopularityScores default sort: Score DESC.

---

## A-R9 — ICurrentUser Audit

`ICurrentUser` is allowed ONLY in these handlers:

| Handler | Use |
|---|---|
| `GetProviderAnalyticsQueryHandler` | resolve provider via `BookingProviderSnapshot.UserId == ICurrentUser.UserId` (self-only) |
| `RecordInteractionCommandHandler` | stamp UserId or fallback to anonymous SessionId |
| `GetMyAuditLogQueryHandler` (deferred Phase 3 — not in this sprint, just reserved) | self-only |

**All other handlers MUST use `MustHavePermission`** — no ICurrentUser leak into Application layer.

---

## A-R10 — Error Codes

| Code | Outcome | When |
|---|---|---|
| `Interaction.InvalidEntityType` | 422 | EntityType enum value rejected |
| `Interaction.EntityNotFound` | 404 | entityId not in current snapshot tables |
| `Interaction.RateLimited` | 429 | hit per-user/min cap (handled by middleware, but error code reserved for sub-second client-side throttling) |
| `PopularityScore.NotFound` | 404 | entity has no scores row (means popularity hasn't been recalc'd since entity created — return empty score 0.0 instead of 404 in practice) |
| `Trending.WindowNotReady` | 503 | sprint just deployed, no 7-day-old snapshots — return empty list with status code |
| `AdminDashboard.PeriodInvalid` | 422 | startDate > endDate or > 1 year span |
| `AdminDashboard.GranularityNotSupported` | 422 | granularity not in {Day, Week, Month, Year} |
| `ProviderDashboard.NotProvider` | 403 | ICurrentUser is not registered as a provider |
| `ProviderDashboard.ProviderSnapshotMissing` | 503 | snapshot inbox hasn't caught up; retry in 30s |
| `AuditLog.NotFound` | 404 | log entry doesn't exist |
| `AuditLog.AlreadyRedacted` | 409 | redact called on a row where RedactedAt is non-null |
| `AuditLog.RedactionNotApplicable` | 422 | no sensitive fields detected — admin should not be redacting blindly |
| `AuditLog.ExportTooLarge` | 422 | export result > 100K rows; force date range narrowing |

---

<a id="03-entities-matrix"></a>

## 03-entities-matrix

> Source: `Analytics/03-entities-matrix.md`

# Analytics — Entity Ownership Matrix

> Per PW-2: only `PopularityScore` becomes IAggregateRoot. The rest are append-only streams or read snapshots.

---

## 1. Aggregates in scope (this sprint)

| Entity | Base | IAggregateRoot? | Owner | Notes |
|---|---|---|---|---|
| `PopularityScore` | AuditableEntity | ✅ | T2/T3 | recalc lifecycle, stale flag, soft-delete on entity removal |

## 2. BaseEntity child / append-only / read-snapshot entities

| Entity | Base | PK | Owner | Notes |
|---|---|---|---|---|
| `UserInteraction` | BaseEntity (BIGINT PK override) | `bigint IDENTITY` | T1 | append-only, high-write, fire-and-forget |
| `AuditLog` | BaseEntity (BIGINT PK override) | `bigint IDENTITY` | T6 | append-only, immutable + Redaction in-place |
| `EntityPopularitySnapshot` | BaseEntity (BIGINT PK) | `bigint IDENTITY` | T2/T3 | daily snapshot for trending DELTA |
| `DashboardCache` | BaseEntity | composite (Key string PK) | T4 | pre-aggregated rollups for dashboards |
| `IngestDebounceMarker` | BaseEntity | composite (EntityType, EntityId) | T2 | dedup PopularityScore stale-flag bursts |

## 3. OUT-OF-SCOPE stubs (stay as-is, marked `[Obsolete]`)

| Entity | Status | Reason |
|---|---|---|
| `RecommendationCache` | `[Obsolete("Phase 4 feature 21")]` | hybrid recommendations engine deferred |
| `UserPreference` | `[Obsolete("Phase 4 feature 24")]` | accessibility preferences UI deferred |
| `UserPreferredCategory` | `[Obsolete("Phase 4 feature 24")]` | deferred |

---

## 4. Integration Events (3 emitted + ~16 consumed)

See `00-README.md` §4. Logical name format: `analytics.{aggregate-kebab}.{verb}.v1`.

| Emitted | Source |
|---|---|
| `analytics.popularity-scores.recalculated.v1` | T3 BG service |
| `analytics.audit-log.entry-redacted.v1` | T6 redact endpoint |
| `analytics.trending.refreshed.v1` | T3 BG service |

Inbox consumers documented in `00-README.md §4.2`. ~16 handlers across Booking/Finance/Social/ContentTours/ContentPlaces/Auth/Accounts.

---

## 5. Value Objects

```csharp
public readonly record struct PopularityWeight(decimal Value)
{
    public static PopularityWeight Zero { get; } = new(0m);
    public static PopularityWeight operator +(PopularityWeight a, PopularityWeight b) => new(a.Value + b.Value);
}

public readonly record struct InteractionEnvelope(
    Guid? UserId,
    string? SessionId,
    EntityType EntityType,
    Guid EntityId,
    InteractionType InteractionType,
    DateTime OccurredAt,
    string? ClientIpHash,    // SHA256[:16] for fraud detection
    string? UserAgent);

public readonly record struct DashboardPeriod(DateTime StartUtc, DateTime EndUtc, DashboardGranularity Granularity)
{
    public bool IsValid() => StartUtc < EndUtc && (EndUtc - StartUtc).TotalDays <= 365;
}

public readonly record struct BigIntCursor(long Id, DateTime OccurredAt)
{
    public string Encode() => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Id}:{OccurredAt:o}"));
    public static BigIntCursor? TryDecode(string? cursor) { /* base64 decode + parse, return null on garbage */ }
}
```

---

## 6. Enums

```csharp
// EXISTING — extend
public enum InteractionType
{
    View = 0,           // page view
    Click = 1,          // CTA click (book button, share, etc.)
    Search = 2,
    AddToFavorite = 3,
    RemoveFromFavorite = 4,
    BookingStarted = 5,
    BookingCompleted = 6,
    BookingCancelled = 7,
    Share = 8,
    ReviewSubmitted = 9
}

// NEW
public enum EntityType
{
    Tour = 1,
    Place = 2,
    Business = 3,
    Category = 4
}

public enum DashboardGranularity
{
    Day = 1,
    Week = 2,
    Month = 3,
    Year = 4
}

public enum AuditLogAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Approve = 4,
    Reject = 5,
    Confirm = 6,
    Cancel = 7,
    Refund = 8,
    Login = 9,
    Logout = 10,
    PasswordChange = 11,
    PermissionGrant = 12,
    PermissionRevoke = 13,
    Custom = 99   // for action types that don't fit; stored in CustomActionName column
}
```

---

## 7. Persistence layout (`Analytics.Infrastructure/Persistence/`)

- `AnalyticsDbContext` (existing)
- `AnalyticsDbInitializer`
- `AnalyticsDbContextFactory` (design-time)
- `AnalyticsUnitOfWork` (PW-1 — delegate to SharedKernel)
- `AnalyticsInboxStore`
- `AnalyticsOutboxWriter`
- EF Configurations (8): UserInteraction, PopularityScore, AuditLog, EntityPopularitySnapshot, DashboardCache, IngestDebounceMarker, InboxMessages, OutboxMessages

**EF schema:** `analytics.*` for all tables.

---

## 8. Migration sequence (this sprint creates 7 migrations)

| # | Name | Owner | Task |
|---|---|---|---|
| 1 | `AnalyticsAddAggregateRootAndAuditMembers` | TL | PW-2 |
| 2 | `AnalyticsAddUserInteractionIndexes` | Mahmoud | T1 |
| 3 | `AnalyticsAddPopularityScoreColumnsAndIndexes` | Mohammad | T2 |
| 4 | `AnalyticsAddEntityPopularitySnapshots` | Mohammad | T2 |
| 5 | `AnalyticsAddDashboardCache` | Fadwa | T4 |
| 6 | `AnalyticsAddAuditLogColumnsAndIndexes` | Fadwa | T6 |
| 7 | `AnalyticsAddIngestDebounceMarker` | Mohammad | T2 |

Applied in order via standard `dotnet ef database update` (TL deploys).

---

<a id="04-task-interactions-ingest"></a>

## 04-task-interactions-ingest

> Source: `Analytics/04-task-interactions-ingest.md`

# TASK 1 — Interactions Ingest

> **Owner:** Mahmoud (Intermediate) — **Hours:** 24h — **Hard deadline:** Sun **2027-01-31 17:00**
> **Earliest start:** Wed 2027-01-20 (after PW-1..PW-9)
> **Endpoints:** 3 HTTP + ~3 inbox handlers + 1 BackgroundService (ingest drain)
> **Depends on:** PW-1 UoW, PW-5 repo interfaces, PW-9 schema

---

## 1. Endpoint list

| # | Method | Path | Auth | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/interactions` | **.AllowAnonymous()** | fire-and-forget per A-R1, 202 within 50ms |
| 2 | GET | `/api/v1/admin/interactions` | `MustHavePermission(Interaction, Read)` | admin-only, cursor pagination, filters |
| 3 | GET | `/api/v1/admin/interactions/user/{userId}` | `MustHavePermission(Interaction, Read)` | user behavior trace |

---

## 2. Endpoint #1 — POST /interactions (fire-and-forget)

```csharp
public sealed record RecordInteractionCommand(
    EntityType EntityType,
    Guid EntityId,
    InteractionType InteractionType,
    string? SessionId,
    Dictionary<string, string>? Metadata
) : ICommand<Result<Unit>>;

internal sealed class RecordInteractionCommandHandler(
    IInteractionIngestQueue queue,
    HybridCache cache,
    IClientContextProvider clientContext,
    ICurrentUser currentUser,
    TimeProvider time
) : IRequestHandler<RecordInteractionCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RecordInteractionCommand cmd, CancellationToken ct)
    {
        // A-R1: NO DB lookup here — only enqueue
        var userId = currentUser.UserId;   // null if anonymous
        var sessionId = cmd.SessionId ?? clientContext.CorrelationId ?? Guid.CreateVersion7().ToString();

        // A-R2: dedupe check via cache (cheap, no DB)
        var dedupeKey = $"interaction-dedupe:{userId ?? Guid.Empty}:{cmd.EntityType}:{cmd.EntityId}:{cmd.InteractionType}";
        var seen = await cache.GetOrCreateAsync<bool>(
            dedupeKey,
            _ => ValueTask.FromResult(false),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            cancellationToken: ct);
        
        if (seen) return Result.Success(Unit.Value);   // drop silently
        await cache.SetAsync(dedupeKey, true, new() { Expiration = TimeSpan.FromMinutes(5) }, ct);

        var envelope = new InteractionEnvelope(
            UserId: userId,
            SessionId: userId is null ? sessionId : null,
            EntityType: cmd.EntityType,
            EntityId: cmd.EntityId,
            InteractionType: cmd.InteractionType,
            OccurredAt: time.GetUtcNow().UtcDateTime,
            ClientIpHash: HashIp(clientContext.IpAddress),
            UserAgent: clientContext.UserAgent
        );

        if (!queue.TryEnqueue(envelope))
        {
            AnalyticsDiagnostics.IngestDropped.Add(1);
            // Still return Success — client shouldn't retry on dropped fire-and-forget
        }
        return Result.Success(Unit.Value);
    }
}
```

**`InteractionIngestDrainService : BackgroundService`** — consumes `Channel<InteractionEnvelope>`, batches 100 rows or 1 sec elapsed, calls `IUserInteractionRepository.AddBatchAsync`. SqlBulkCopy path inside the impl. On exception, log + continue (loss acceptable per A-R1).

**Validation (FluentValidation):** EntityType valid enum, EntityId not empty, InteractionType valid enum, Metadata serialized JSON ≤2KB.

**Endpoint:** `endpoints.MapPost("/interactions", ...).AllowAnonymous().WithRateLimiting("interactions");` — middleware enforces 100/min/IP for anonymous, 1000/min/user for authenticated.

---

## 3. Endpoint #2 — GET /admin/interactions

Filters: `entityType?`, `entityId?`, `userId?`, `interactionType?`, `startDate?`, `endDate?` (default last 7 days), `cursor?`, `pageSize?` (default 20, max 50).

Returns cursor envelope `{items, nextCursor, totalCount?}`. `totalCount` opt-in via `?countTotal=true` (expensive — full table scan with filters).

DTO:
```csharp
public sealed record AdminInteractionDto(
    long Id,
    Guid? UserId,
    string? SessionId,
    EntityType EntityType,
    Guid EntityId,
    InteractionType InteractionType,
    DateTime OccurredAt,
    string? ClientIpHash,
    string? UserAgent
);
```

Cache: NOT cached (admin-only, real-time investigation).

---

## 4. Endpoint #3 — GET /admin/interactions/user/{userId}

Simplified version of #2 scoped to one user. Same envelope. 30-day default window.

Use covering index `(UserId, OccurredAt DESC) INCLUDE (EntityType, EntityId, InteractionType)`.

---

## 5. ~3 inbox handlers (write UserInteraction directly without going through queue — these are authoritative server events, not user-driven)

| Inbox event | Inserted UserInteraction |
|---|---|
| `booking.tour-booking.created.v1` | InteractionType.BookingStarted, EntityType=Tour, EntityId=TourId, UserId from event |
| `booking.tour-booking.confirmed.v1` | InteractionType.BookingCompleted, same |
| `social.favorite.added.v1` | InteractionType.AddToFavorite, EntityType from event payload, EntityId from event |

These ride on the inbox transaction (HasBeenProcessedAsync guard → AddAsync → MarkAsProcessed → single SaveChanges).

---

## 6. Cache invalidation

`POST /interactions` does NOT invalidate caches (fire-and-forget). The DrainService's writes don't either — they're "eventually visible" via the next 30-second dashboard refresh.

Admin endpoints are NOT cached.

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | `RecordInteractionCommand` + handler + validator | 4 | 2027-01-21 |
| 2 | `IInteractionIngestQueue` impl + `Channel<T>` + DI | 2 | 2027-01-22 |
| 3 | `InteractionIngestDrainService` impl + SqlBulkCopy in repo | 4 | 2027-01-24 |
| 4 | POST /interactions endpoint + rate limit + integration test | 2 | 2027-01-25 |
| 5 | GET /admin/interactions + cursor + integration test | 4 | 2027-01-27 |
| 6 | GET /admin/interactions/user/{userId} + integration test | 2 | 2027-01-28 |
| 7 | 3 inbox handlers (Booking + Social) + parity tests | 3 | 2027-01-29 |
| 8 | OpenTelemetry (counters, ingest_dropped_total, ingest_drained_total) | 1 | 2027-01-30 |
| 9 | A-R2 dedupe unit tests + manual perf check (1000 POST/sec sustained) | 2 | 2027-01-31 |
| **Total** | | **24h** | **Sun 2027-01-31** |

---

## 8. Acceptance tests (10+ minimum)

1. POST happy path → 202 with empty body, returns within 50ms p95.
2. POST anonymous (no JWT) → 202.
3. POST duplicate within 5min same user/entity/type → 202 but no second DB write (dedupe).
4. POST with invalid EntityType → 422.
5. POST with EntityId not in snapshots → still 202 (we don't validate existence here — analytics over snapshots tolerates orphan refs; ingestion is permissive).
6. POST with Metadata 5KB → 422 (size limit).
7. POST under 1000/min sustained: zero drops, p95 < 50ms.
8. POST under burst 5000/sec: drops counted in `analytics_ingest_dropped_total`, no exceptions.
9. GET admin/interactions returns paginated rows with valid cursor.
10. GET admin/interactions/user/{userId} returns only that user's rows.
11. Inbox: `booking.tour-booking.created.v1` consumed → UserInteraction(BookingStarted) row appears.
12. Inbox: same event delivered twice → HasBeenProcessedAsync skips second insert.

---

<a id="05-task-popular-trending"></a>

## 05-task-popular-trending

> Source: `Analytics/05-task-popular-trending.md`

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

---

<a id="06-task-admin-dashboards"></a>

## 06-task-admin-dashboards

> Source: `Analytics/06-task-admin-dashboards.md`

# TASK 4 — Admin Dashboards

> **Owner:** Fadwa (Beginner→Intermediate) with Mohammad pairing first 2 days — **Hours:** 28h — **Hard deadline:** Sun **2027-02-21 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 4 + ~3 cache-invalidation inbox handlers

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/admin/dashboard` | `MustHavePermission(AdminDashboard, Read)` |
| 2 | GET | `/api/v1/admin/dashboard/revenue?from=&to=&granularity=` | same |
| 3 | GET | `/api/v1/admin/dashboard/bookings?from=&to=&granularity=` | same |
| 4 | GET | `/api/v1/admin/dashboard/users?from=&to=&granularity=` | same |

---

## 2. GET /admin/dashboard (Overview)

Returns a snapshot of platform health for the admin homepage. Cache 30s.

```csharp
public sealed record AdminDashboardOverviewDto(
    AdminRevenueSummaryDto Revenue,
    AdminBookingsSummaryDto Bookings,
    AdminUsersSummaryDto Users,
    AdminAlertsDto Alerts
);

public sealed record AdminRevenueSummaryDto(
    decimal TotalToday,
    decimal TotalThisMonth,
    decimal TotalThisYear,
    string PrimaryCurrency,
    int ProvidersWithRevenue,
    decimal AvgBookingValue
);

public sealed record AdminBookingsSummaryDto(
    int TotalToday,
    int TotalThisWeek,
    int TotalThisMonth,
    decimal ConversionRate,        // confirmed/total
    decimal CancellationRate,
    int PendingProviderConfirmations
);

public sealed record AdminUsersSummaryDto(
    int NewRegistrationsToday,
    int Active7d,
    int Active30d,
    int ProviderSignupsThisMonth,
    int ProvidersPending
);

public sealed record AdminAlertsDto(
    int FailedPayouts,           // queue from Finance
    int OutboxLag,               // unprocessed > 5min
    int OpenSupportTickets,
    int OverdueAdminReviews      // bookings PendingConfirmation > 20h, etc.
);
```

**Source data:** queries against PaymentSnapshot + BookingSnapshot + UserSnapshot read tables maintained via inbox handlers. **NEVER cross-module SELECT** — Analytics owns local snapshots populated by integration events.

---

## 3. GET /admin/dashboard/revenue?from=&to=&granularity=

Time-series data for charts. `granularity` = Day/Week/Month/Year (clamped to >=15 buckets, <=730 buckets — error `AdminDashboard.PeriodInvalid` outside).

```csharp
public sealed record AdminRevenueTimeSeriesDto(
    DashboardPeriod Period,
    IReadOnlyList<RevenueTimePointDto> Series,
    IReadOnlyList<RevenueByCurrencyDto> Currencies,
    IReadOnlyList<RevenueByProviderDto> TopProviders,
    IReadOnlyList<RevenueByCategoryDto> TopCategories
);

public sealed record RevenueTimePointDto(DateTime Bucket, decimal Gross, decimal Commission, decimal Refunded, decimal Net);
public sealed record RevenueByCurrencyDto(string Currency, decimal Total);
public sealed record RevenueByProviderDto(Guid ProviderId, string ProviderName, decimal Total);
public sealed record RevenueByCategoryDto(Guid CategoryId, string CategoryName, decimal Total);
```

**SQL:**
```sql
WITH buckets AS (
    SELECT 
        DATEADD(@granularity, DATEDIFF(@granularity, 0, p.CompletedAt), 0) AS Bucket,
        SUM(CASE WHEN p.Type = 'Booking' THEN p.Amount ELSE 0 END) AS Gross,
        SUM(p.CommissionAmount) AS Commission,
        SUM(CASE WHEN p.Type = 'Refund' THEN p.Amount ELSE 0 END) AS Refunded
    FROM analytics.PaymentSnapshots p
    WHERE p.CompletedAt >= @from AND p.CompletedAt < @to AND p.Status = 'Completed'
    GROUP BY DATEADD(@granularity, DATEDIFF(@granularity, 0, p.CompletedAt), 0)
)
SELECT Bucket, Gross, Commission, Refunded, (Gross - Commission - Refunded) AS Net FROM buckets ORDER BY Bucket;
```

`PaymentSnapshots` table populated by `FinancePaymentCompletedHandler` inbox (T6 documents the snapshot upkeep responsibility).

---

## 4. GET /admin/dashboard/bookings

Series of confirmed/cancelled/pending bookings over time + funnel metrics.

```csharp
public sealed record AdminBookingsTimeSeriesDto(
    DashboardPeriod Period,
    IReadOnlyList<BookingTimePointDto> Series,
    BookingFunnelDto Funnel
);

public sealed record BookingTimePointDto(
    DateTime Bucket,
    int Created,
    int Confirmed,
    int Cancelled,
    int Completed,
    decimal AvgValue
);

public sealed record BookingFunnelDto(
    int Total,
    int AwaitingPayment,
    int PendingConfirmation,
    int Confirmed,
    int Completed,
    int Cancelled,
    int Rejected,
    decimal ConversionRate,        // Completed / Total
    decimal AbandonmentRate        // AwaitingPayment expired / Total
);
```

Source: `analytics.BookingSnapshots` table (mirror of `booking.TourBookings` populated by Booking inbox events).

---

## 5. GET /admin/dashboard/users

```csharp
public sealed record AdminUsersTimeSeriesDto(
    DashboardPeriod Period,
    IReadOnlyList<UserTimePointDto> Series,
    UsersBreakdownDto Breakdown
);

public sealed record UserTimePointDto(
    DateTime Bucket,
    int NewRegistrations,
    int ActiveUsers,            // count from UserInteractions COUNT(DISTINCT UserId)
    int ProviderSignups
);

public sealed record UsersBreakdownDto(
    int TotalUsers,
    int TotalProviders,
    int VerifiedProviders,
    int VerifiedUsers,           // email-verified
    int ActiveLast30d
);
```

Source: `analytics.UserSnapshots` table (mirror of Auth.Users) + UserInteractions COUNT(DISTINCT).

---

## 6. ~3 cache-invalidation inbox handlers

| Inbox event | Cache tags to evict |
|---|---|
| `finance.payment.completed.v1` | `admin:dashboard:overview`, `admin:dashboard:revenue` |
| `booking.tour-booking.created.v1` | `admin:dashboard:overview`, `admin:dashboard:bookings` |
| `auth.user.registered.v1` | `admin:dashboard:overview`, `admin:dashboard:users` |

These ALSO update snapshot tables (`PaymentSnapshots`, `BookingSnapshots`, `UserSnapshots`) — combined inbox handler per source event.

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Snapshot tables (PaymentSnapshots, BookingSnapshots, UserSnapshots) + EF configs + migration `AnalyticsAddDashboardSnapshotTables` | 4 | 2027-01-25 |
| 2 | DashboardCache table + repo + EF + migration | 3 | 2027-01-28 |
| 3 | GET /admin/dashboard overview handler + 4 sub-queries | 5 | 2027-02-04 |
| 4 | GET /admin/dashboard/revenue + SQL + DTO + integration test | 5 | 2027-02-09 |
| 5 | GET /admin/dashboard/bookings + funnel calc + integration test | 4 | 2027-02-13 |
| 6 | GET /admin/dashboard/users + UserInteractions COUNT(DISTINCT) | 3 | 2027-02-17 |
| 7 | 3 inbox handlers for snapshot upkeep + cache invalidation | 3 | 2027-02-20 |
| 8 | Unit + integration tests (12+) | 1 | 2027-02-21 |
| **Total** | | **28h** | **Sun 2027-02-21** |

---

## 8. Acceptance tests (12+)

1. GET overview → 200 with all sections populated (zeros allowed).
2. GET revenue with from > to → 422 PeriodInvalid.
3. GET revenue 1-year span Day granularity → 365 buckets returned.
4. GET revenue 2-year span Day granularity → 422 PeriodInvalid.
5. GET bookings funnel — sum of statuses === Total.
6. Inbox payment.completed → PaymentSnapshots row appears.
7. Inbox payment.completed → cache `admin:dashboard:revenue` evicted; next GET refresh.
8. GET users with no UserInteractions → ActiveUsers=0.
9. GET revenue Week granularity → buckets aligned to Monday (or PreferredFirstDayOfWeek).
10. Cache hit p95 < 30ms (just deserialization).
11. Cache miss p95 < 500ms (cold query).
12. Concurrent GETs (50 simultaneous) → no stampede, cache holds.

---

<a id="07-task-provider-analytics"></a>

## 07-task-provider-analytics

> Source: `Analytics/07-task-provider-analytics.md`

# TASK 5 — Provider Analytics

> **Owner:** Mohammad / Mahmoud split — **Hours:** 14h — **Hard deadline:** Sun **2027-02-21 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 3

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/provider/dashboard` | `MustHavePermission(ProviderDashboard, Read)` + self via ICurrentUser |
| 2 | GET | `/api/v1/provider/analytics?from=&to=&granularity=` | same |
| 3 | GET | `/api/v1/provider/my-tours` | same |

---

## 2. Endpoint #1 — GET /provider/dashboard

Provider's "home" view. Self-only via `BookingProviderSnapshot.UserId == ICurrentUser.UserId`.

```csharp
public sealed record ProviderDashboardDto(
    Guid ProviderId,
    string ProviderName,
    ProviderRevenueSummary Revenue,
    ProviderBookingsSummary Bookings,
    ProviderToursSummary Tours,
    ProviderRatingSummary Rating,
    IReadOnlyList<UpcomingBookingDto> UpcomingBookings,    // next 7 days
    IReadOnlyList<RecentReviewDto> RecentReviews           // last 5
);

public sealed record ProviderRevenueSummary(
    decimal GrossThisMonth,
    decimal NetThisMonth,       // after commission
    decimal PendingPayoutTotal,
    string PrimaryCurrency,
    DateTime? NextPayoutScheduledAt
);

public sealed record ProviderBookingsSummary(
    int PendingConfirmation,     // requires action
    int ConfirmedThisWeek,
    int CompletedThisMonth,
    int CancelledThisMonth
);

public sealed record ProviderToursSummary(
    int Total,
    int Active,
    int Pending,                 // approval pending
    int MostBookedTourId,
    string MostBookedTourName
);

public sealed record ProviderRatingSummary(
    decimal AverageRating,
    int ReviewCount,
    int? RankInCategory          // category percentile from PopularityScore
);

public sealed record UpcomingBookingDto(Guid BookingId, string BookingReference, string TourName, DateTime ScheduledAt, int Participants, decimal TotalAmount);
public sealed record RecentReviewDto(Guid ReviewId, decimal Rating, string? Title, string Content, string ReviewerName, DateTime PublishedAt);
```

**Caching:** `provider:dashboard:{providerId}` 60s sliding. Tags: `provider:dashboard:{providerId}`, `provider-revenue:{providerId}`, `provider-bookings:{providerId}`.

Handler:
```csharp
internal sealed class GetProviderDashboardQueryHandler(
    IBookingProviderSnapshotRepository providerRepo,
    IPaymentSnapshotRepository paymentRepo,
    IBookingSnapshotRepository bookingRepo,
    ITourSnapshotRepository tourRepo,
    IReviewSnapshotRepository reviewRepo,
    IPopularityScoreRepository popRepo,
    ICurrentUser currentUser,
    HybridCache cache,
    TimeProvider time
) : IRequestHandler<GetProviderDashboardQuery, Result<ProviderDashboardDto>>
{
    public async Task<Result<ProviderDashboardDto>> Handle(GetProviderDashboardQuery query, CancellationToken ct)
    {
        var provider = await providerRepo.GetByUserIdAsync(currentUser.UserId!.Value, ct);
        if (provider is null) return Result.Failure<ProviderDashboardDto>(
            new Error("ProviderDashboard.NotProvider", "Current user is not a registered provider"), Outcome.Forbidden);

        var key = $"provider:dashboard:{provider.ProviderId}";
        return await cache.GetOrCreateAsync(
            key,
            async _ => await BuildDashboardAsync(provider, ct),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(60) },
            tags: new[] { key, $"provider-revenue:{provider.ProviderId}" },
            cancellationToken: ct);
    }
}
```

---

## 3. Endpoint #2 — GET /provider/analytics

Provider's time-series dashboard. Scoped to their data only.

```csharp
public sealed record ProviderAnalyticsDto(
    DashboardPeriod Period,
    IReadOnlyList<ProviderRevenueTimePointDto> RevenueSeries,
    IReadOnlyList<ProviderBookingsTimePointDto> BookingsSeries,
    IReadOnlyList<TourPerformanceDto> TourPerformance,
    ConversionFunnelDto Funnel
);

public sealed record ProviderRevenueTimePointDto(DateTime Bucket, decimal Gross, decimal Commission, decimal Refunded, decimal Net);
public sealed record ProviderBookingsTimePointDto(DateTime Bucket, int Created, int Confirmed, int Cancelled, int Completed);

public sealed record TourPerformanceDto(
    Guid TourId,
    string TourName,
    int BookingCount,
    decimal Revenue,
    decimal AverageRating,
    int ReviewCount,
    decimal ConversionRate         // bookings / views from UserInteractions
);

public sealed record ConversionFunnelDto(
    long TotalViews,                // UserInteractions InteractionType=View where EntityId in provider tours
    long Clicks,
    long BookingsStarted,
    long BookingsCompleted,
    decimal ViewToBookingRate
);
```

Filter checks: from/to span valid, granularity supported, provider owns the tours queried.

---

## 4. Endpoint #3 — GET /provider/my-tours

Lightweight list of provider's tours with current stats (for the provider's "My Tours" page in the dashboard).

```csharp
public sealed record ProviderTourListItemDto(
    Guid TourId,
    string TourName,
    string Status,            // Active, Pending, Rejected, Archived
    int BookingCount30d,
    decimal Revenue30d,
    decimal AverageRating,
    int ReviewCount,
    long ViewCount30d
);
```

Cursor pagination same shape.

Cache: `provider:my-tours:{providerId}:cursor:{cursorHash}` 30s sliding.

---

## 5. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | BookingProviderSnapshot / TourSnapshot / ReviewSnapshot read repos (mostly already exist from prior sprints) | 1 | 2027-02-15 |
| 2 | GET /provider/dashboard query handler + DTO | 4 | 2027-02-17 |
| 3 | GET /provider/analytics handler + time-series SQL | 5 | 2027-02-19 |
| 4 | GET /provider/my-tours handler + cursor | 2 | 2027-02-20 |
| 5 | Tests (8+) | 2 | 2027-02-21 |
| **Total** | | **14h** | **Sun 2027-02-21** |

---

## 6. Acceptance tests (8+)

1. GET /provider/dashboard as non-provider → 403 NotProvider.
2. GET /provider/dashboard as provider → 200, ProviderId stamped, totals reasonable.
3. GET /provider/analytics from > to → 422 PeriodInvalid.
4. GET /provider/analytics returns only this provider's data (negative test: another provider's data NOT included).
5. GET /provider/my-tours returns provider's tours only.
6. GET /provider/my-tours with cursor returns next page correctly.
7. Cache hit p95 < 50ms.
8. Concurrent GETs from same provider (10) → no stampede.

---

<a id="08-task-audit-log-query"></a>

## 08-task-audit-log-query

> Source: `Analytics/08-task-audit-log-query.md`

# TASK 6 — Audit Log Query API & Redaction

> **Owner:** Fadwa (Beginner→Intermediate) — **Hours:** 14h — **Hard deadline:** Sun **2027-02-21 17:00**
> **Earliest start:** Wed 2027-01-20
> **Endpoints:** 3 + ~6 audit-log inbox handlers

---

## 1. Endpoint list

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | GET | `/api/v1/admin/audit-logs?filter...&cursor=...` | `MustHavePermission(AuditLog, Read)` |
| 2 | POST | `/api/v1/admin/audit-logs/{id}/redact` | `MustHavePermission(AuditLog, Redact)` |
| 3 | GET | `/api/v1/admin/audit-logs/export?from=&to=` | `MustHavePermission(AuditLog, Export)` |

---

## 2. AuditLog entity (append-only stream)

```csharp
public sealed class AuditLog : BaseEntity   // BIGINT PK (see PW-9)
{
    public override long Id { get; protected set; }            // override Guid → long
    public Guid? UserId { get; private set; }                    // null for system actions
    public string? Username { get; private set; }                // denormalized snapshot
    public AuditLogAction Action { get; private set; }
    public string? CustomActionName { get; private set; }        // when Action = Custom
    public string EntityType { get; private set; } = string.Empty;  // "Tour", "Booking", "Payment", etc.
    public Guid EntityId { get; private set; }
    public string? OldValue { get; private set; }                // JSON or redacted
    public string? NewValue { get; private set; }                // JSON or redacted
    public string? UserAgent { get; private set; }
    public string? IpAddressHash { get; private set; }           // /24 hash per A-R6
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime? RedactedAt { get; private set; }            // null = not redacted; populated by Redact endpoint or auto-redact
    public string? RedactionReason { get; private set; }
    public Guid? RedactedByUserId { get; private set; }

    private AuditLog() { }

    public static AuditLog Append(
        Guid? userId,
        string? username,
        AuditLogAction action,
        string? customActionName,
        string entityType,
        Guid entityId,
        string? oldValue,
        string? newValue,
        string? userAgent,
        string? ipAddressHash,
        string? correlationId,
        DateTime now,
        IAuditLogRedactor redactor)
    {
        var (redactedOld, _) = redactor.Redact(oldValue);
        var (redactedNew, _) = redactor.Redact(newValue);
        var entry = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            CustomActionName = customActionName,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = redactedOld,
            NewValue = redactedNew,
            UserAgent = userAgent,
            IpAddressHash = ipAddressHash,
            CorrelationId = correlationId,
            OccurredAt = now,
            RedactedAt = null
        };
        entry.RaiseDomainEvent(new AuditLogEntryAppendedDomainEvent(entry.Id, entry.UserId, entry.Action, entry.EntityType, entry.EntityId, now));
        return entry;
    }

    public void RetroactivelyRedact(Guid adminUserId, string reason, DateTime now, IAuditLogRedactor redactor)
    {
        if (RedactedAt is not null) throw new InvalidOperationException("Already redacted");
        var (redactedOld, oldFields) = redactor.Redact(OldValue);
        var (redactedNew, newFields) = redactor.Redact(NewValue);
        if (oldFields.Count == 0 && newFields.Count == 0)
            throw new InvalidOperationException("No sensitive fields detected");
        OldValue = redactedOld;
        NewValue = redactedNew;
        RedactedAt = now;
        RedactedByUserId = adminUserId;
        RedactionReason = reason;
        RaiseDomainEvent(new AuditLogEntryRedactedDomainEvent(Id, Action, EntityType, EntityId, oldFields.Concat(newFields).ToList()));
    }
}
```

---

## 3. Endpoint #1 — GET /admin/audit-logs

Filters:
- `entityType?` (e.g. "Booking", "Payment")
- `entityId?` (Guid)
- `userId?` (who performed it)
- `action?` (AuditLogAction enum)
- `from?` `to?` (default last 7 days)
- `ipAddressHash?` (hash matching — admin enters known fragment to investigate)
- `cursor?`, `pageSize?` (default 20, max 50)

Returns `{items, nextCursor, totalCount?}`. Total expensive — opt-in via `?countTotal=true`.

DTO:
```csharp
public sealed record AdminAuditLogDto(
    long Id,
    Guid? UserId,
    string? Username,
    AuditLogAction Action,
    string? CustomActionName,
    string EntityType,
    Guid EntityId,
    string? OldValue,
    string? NewValue,
    string? UserAgent,
    string? IpAddressHash,
    string? CorrelationId,
    DateTime OccurredAt,
    DateTime? RedactedAt
);
```

---

## 4. Endpoint #2 — POST /admin/audit-logs/{id}/redact

```csharp
public sealed record RedactAuditLogCommand(long Id, string Reason) : ICommand<Result<Unit>>;
```

Reason ≥ 20 chars mandatory. Calls `AuditLog.RetroactivelyRedact(currentUser.UserId, reason, now, redactor)`. Emits `analytics.audit-log.entry-redacted.v1` to alert other admins.

Error codes: `AuditLog.NotFound`, `AuditLog.AlreadyRedacted`, `AuditLog.RedactionNotApplicable`.

---

## 5. Endpoint #3 — GET /admin/audit-logs/export

CSV export for legal/compliance dump. Streams response with `Content-Type: text/csv; charset=utf-8`.

- Hard limit: 100K rows. > 100K → `AuditLog.ExportTooLarge` 422 (force narrower date range).
- Always includes RedactedAt + redaction status columns.
- Header row: `Id,UserId,Username,Action,EntityType,EntityId,OccurredAt,IpAddressHash,UserAgent,CorrelationId,RedactedAt,RedactionReason`.
- OldValue / NewValue **deliberately excluded** from export to prevent CSV-injection vectors (admin can view individual entries via #1 endpoint).

---

## 6. ~6 audit-log inbox handlers

Standard pattern: each handler builds an AuditLog row via `AuditLog.Append`, persists via repo, MarkAsProcessed inbox, single SaveChanges.

| Source event | Action | OldValue / NewValue |
|---|---|---|
| `auth.user.registered.v1` | Create | null / `{ "userId": ..., "emailHash": ... }` |
| `finance.payment.completed.v1` | Create | null / `{ "paymentId": ..., "amount": ..., "currency": ... }` (no card data — already redacted upstream) |
| `finance.payout.completed.v1` | Update | `{ "status": "Pending" }` / `{ "status": "Completed" }` |
| `finance.refund.completed.v1` | Refund | `{ "amount": ... }` / `{ "amount": ..., "status": "Completed" }` |
| `accounts.provider.status-changed.v1` | Approve/Reject (depends on transition) | `{ "oldStatus": ... }` / `{ "newStatus": ... }` |
| `booking.tour-booking.cancelled.v1` | Cancel | `{ "status": "Confirmed" }` / `{ "status": "Cancelled", "reason": ... }` |

Each handler reads `IClientContextProvider` for UserAgent + IpAddressHash (likely nulls when running in BG service inbox context — acceptable).

---

## 7. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | AuditLog aggregate + EF config + migration `AnalyticsAddAuditLogColumnsAndIndexes` | 2 | 2027-02-16 |
| 2 | IAuditLogRedactor abstraction in SharedKernel + impl + unit tests | 3 | 2027-02-17 |
| 3 | GET /admin/audit-logs handler + cursor + integration test | 3 | 2027-02-19 |
| 4 | POST /admin/audit-logs/{id}/redact handler + integration test | 2 | 2027-02-20 |
| 5 | GET /admin/audit-logs/export streaming CSV | 1 | 2027-02-20 |
| 6 | 6 inbox handlers + integration tests | 2 | 2027-02-21 |
| 7 | Documentation: redaction policy in 10-cross-cutting.md | 1 | 2027-02-21 |
| **Total** | | **14h** | **Sun 2027-02-21** |

---

## 8. Acceptance tests (12+)

1. GET no filters → last 7 days.
2. GET filter entityType=Booking → only booking rows.
3. GET filter userId=X → only that user's actions.
4. POST redact valid row → 200, RedactedAt set, outbox event emitted.
5. POST redact already-redacted → 409 AlreadyRedacted.
6. POST redact row with no sensitive data → 422 RedactionNotApplicable.
7. POST redact with reason < 20 chars → 422.
8. GET export 1-day → 200 with CSV stream, header row correct.
9. GET export 2-year span → 422 ExportTooLarge (force narrow).
10. Inbox auth.user.registered → AuditLog row appears with redacted email.
11. Inbox finance.payment.completed → AuditLog row appears, no card data leaks.
12. Inbox same event twice → no duplicate AuditLog row (HasBeenProcessedAsync guard).
13. Inbox finance.payout.completed → existing PaymentSnapshot.AuditLogId stamped (cross-link for forensics).

---

<a id="09-task-background-services"></a>

## 09-task-background-services

> Source: `Analytics/09-task-background-services.md`

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

---

<a id="10-cross-cutting"></a>

## 10-cross-cutting

> Source: `Analytics/10-cross-cutting.md`

# Analytics — Cross-Cutting Concerns

> Tech Lead enforces during PR review and at the sprint integration freeze (Sun 2027-02-14 17:00).

---

## 1. DI Audit

`Analytics.Infrastructure/DependencyInjection.cs`:

| Registration | Symbol | Lifetime | Reason |
|---|---|---|---|
| DbContext factory | `IDbContextFactory<AnalyticsDbContext>` | Singleton | BG service ingest drainer |
| Pooled DbContext | `AnalyticsDbContext` via `AddDbContextPool` | Scoped | Per-request handlers |
| Unit of Work | `IAnalyticsUnitOfWork → AnalyticsUnitOfWork` | Scoped | Domain event dispatch |
| Inbox store | `IAnalyticsInboxStore → AnalyticsInboxStore` | Scoped | Idempotency |
| Outbox writer | `IAnalyticsOutboxWriter → AnalyticsOutboxWriter` | Scoped | Integration event publishing |
| Repos | `IUserInteractionRepository`, `IPopularityScoreRepository`, `IAuditLogRepository`, `IDashboardCacheRepository`, `IEntityPopularitySnapshotRepository`, snapshot repos | Scoped | One per interface |
| `IInteractionIngestQueue` | `InteractionIngestQueue` | **Singleton** | Channel<T> shared across requests |
| `InteractionIngestDrainService` | BG service | Singleton | `AddHostedService<>` |
| `PopularityScoreCalculationService` | BG service | Singleton | `AddHostedService<>` |
| Permission catalog | `IPermissionCatalog → AnalyticsPermissionCatalog` | Singleton | Discovery |
| `IClientContextProvider` | `HttpContextClientContextProvider` | Singleton | PW-6 |
| `IAuditLogRedactor` | `AuditLogRedactor` | Singleton | Stateless redaction |
| `IAnalyticsCacheKeys` | `AnalyticsCacheKeys` | Singleton | Centralized key/tag building |
| MediatR | from `Analytics.Application` assembly | per-call | Handler discovery |
| FluentValidation | from `Analytics.Application` assembly, Scoped, internal | Scoped | Per-command validators |
| `AnalyticsDiagnostics` | static class | – | ActivitySource + Meter |

**Common mistakes:**
- ❌ Registering `IInteractionIngestQueue` as Scoped (Channel<T> singleton or it gets recreated → drain consumes wrong queue).
- ❌ Forgetting `AddHostedService<InteractionIngestDrainService>()` — POST /interactions silently drops everything.
- ❌ Adding `ICurrentUser` to handlers that don't appear in A-R9 audit list.

---

## 2. Permission seeder verification

Expected boot log:
```text
[INFO] PermissionSeeder discovered 11 catalogs: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, Booking, Finance, Social, Messaging, Analytics
[INFO] PermissionSeeder inserted/verified 14 Analytics permissions
```

`SELECT COUNT(*) FROM security.Permissions WHERE Feature LIKE 'Analytics.%'` → **14**.

---

## 3. Outbox / Inbox parity test

Add `tests/Analytics.IntegrationTests/Outbox/IntegrationEventTypeRegistryParityTests.cs` per prior sprint pattern.

**Logical names (3 emitted):**
```
analytics.popularity-scores.recalculated.v1
analytics.audit-log.entry-redacted.v1
analytics.trending.refreshed.v1
```

**Inbox consumers (~16 from `00-README.md §4.2`):**
```
auth.user.registered.v1
accounts.provider.status-changed.v1
booking.tour-booking.created.v1
booking.tour-booking.confirmed.v1
booking.tour-booking.cancelled.v1
booking.tour-booking.completed.v1
finance.payment.completed.v1
finance.payout.completed.v1
finance.refund.completed.v1
social.review.published.v1
social.favorite.added.v1
social.rating.recalculated.v1
content-tours.tour.published.v1
content-tours.tour.deleted.v1
content-places.place.created.v1
content-places.place.deleted.v1
```

---

## 4. Build lock workaround

```powershell
dotnet build Analytics/Analytics.Domain/Analytics.Domain.csproj
dotnet build Analytics/Analytics.Contracts/Analytics.Contracts.csproj
dotnet build Analytics/Analytics.Application/Analytics.Application.csproj
dotnet build Analytics/Analytics.Infrastructure/Analytics.Infrastructure.csproj
dotnet build Analytics/Analytics.Presentation/Analytics.Presentation.csproj
dotnet build tests/Analytics.Tests.Unit/Analytics.Tests.Unit.csproj
dotnet build tests/Analytics.IntegrationTests/Analytics.IntegrationTests.csproj
```

---

## 5. Migration sequence (7 migrations)

| # | Name | Owner | Task |
|---|---|---|---|
| 1 | `AnalyticsAddAggregateRootAndAuditMembers` | TL | PW-2 (only PopularityScore) |
| 2 | `AnalyticsAddUserInteractionIndexes` | Mahmoud | T1 |
| 3 | `AnalyticsAddPopularityScoreColumnsAndIndexes` | Mohammad | T2 |
| 4 | `AnalyticsAddEntityPopularitySnapshots` | Mohammad | T2 |
| 5 | `AnalyticsAddDashboardCache` | Fadwa | T4 |
| 6 | `AnalyticsAddAuditLogColumnsAndIndexes` | Fadwa | T6 |
| 7 | `AnalyticsAddIngestDebounceMarker` | Mohammad | T2 |

Plus T4 adds 3 snapshot tables in a sibling migration `AnalyticsAddDashboardSnapshotTables`.

---

## 6. Inbox / Outbox hygiene

- `CompositeOutboxProcessor` auto-picks up Analytics DbContext.
- OutboxCleaner deletes processed > 7 days. InboxCleaner > 30 days.
- **Alert:** `analytics.OutboxMessages WHERE ProcessedAt IS NULL AND CreatedAt < now - 5min` > 100 rows = page on-call.
- **Special alert:** UserInteractions table size — if growth rate > 50K rows/hour for 2 hours, alert SQL admin (potential bot attack or run-away client).

---

## 7. Performance Budget

| Endpoint | p95 target | Hard ceiling |
|---|---|---|
| POST /interactions | < 50 ms | < 100 ms |
| GET /popular/tours (cached) | < 80 ms | < 150 ms |
| GET /trending (cached) | < 80 ms | < 150 ms |
| GET /admin/dashboard (cached) | < 80 ms | < 200 ms |
| GET /admin/dashboard/revenue 1-month (cached) | < 100 ms | < 300 ms |
| GET /provider/dashboard (cached) | < 100 ms | < 200 ms |
| GET /admin/audit-logs cursor (uncached) | < 150 ms | < 400 ms |
| GET /admin/audit-logs/export 50K rows | < 8 s | < 15 s |

BG services:
- PopularityScoreCalculationService 10K entities < 60 s
- InteractionIngestDrainService 500 rows / sec sustained without backlog

---

## 8. Data Retention

Per A-R7:
- **UserInteractions:** no enforced retention this sprint. Likely 18-24 months acceptable; revisit Phase 3.
- **AuditLog:** 2-year retention per PDF. NOT enforced by code this sprint — manual SQL Agent job or future BG service.
- **EntityPopularitySnapshots:** keep 14 days (just enough for 7-day trending window). T3 BG service should `DELETE WHERE TakenAt < now-14d` at end of run.
- **DashboardCache:** TTL-based; rows with `ExpiresAt < now` swept by T3 BG service.
- **IngestDebounceMarker:** swept by T3 BG service > 1h.

**ADR Required (post-sprint):** "ADR-007 — Audit log retention enforcement strategy". TL drafts after retro.

---

## 9. Cross-Module Coupling Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Snapshot tables drift from authoritative (booking.TourBookings, finance.Payments, etc.) | Each inbox handler stamps `LastEventId` and `LastEventTimestamp` columns; daily reconciliation report shows entities with `LastEventTimestamp < now-1h` (stale) |
| UserInteractions FK to ContentTours fails after tour deletion | No FK — `EntityId` is loose reference; orphan rows tolerated (analytics over deleted entities OK) |
| RatingRecalculationService (Social) and PopularityScoreCalculationService (Analytics) both write to derived data | Mutual independence — Social writes EntityRatingCache, Analytics reads it through snapshot. No write conflict possible |
| Dashboard cache stale during heavy traffic | Cache TTL 30s is short enough; stampede protection via HybridCache lock |
| Audit log fills disk | Alert at 80% disk usage triggers SQL admin → run retention job manually until v2 BG service ships |

---

## 10. Secrets inventory

This sprint introduces NO new secrets. Reuses connection string + KeyVault setup from prior sprints.

---

## 11. Folder migration on close

After acceptance gate signed off:
```powershell
Move-Item -LiteralPath "Agents\tasks\Analytics" -Destination "Agents\decisions\closed\Analytics"
```
Update `Phase1-Phase2-Completion-INDEX.md` §1 row Analytics ⬜→✅ Phase 2.
Update `agent-context.md §11.1` Analytics row to ✅.
Add `AGENTS.md` entry for `Analytics/` module summary.

---

<a id="99-acceptance-gate"></a>

## 99-acceptance-gate

> Source: `Analytics/99-acceptance-gate.md`

# Analytics Module — Final Acceptance Gate

> Tech Lead signs off before declaring Analytics sprint closed (Thu 2027-02-25 17:00).
> Folder MUST NOT move to `Agents/decisions/closed/Analytics/` until every box ticked.

---

## 1. Code Quality

- [ ] All 6 task PRs (T1..T6) merged into `main`.
- [ ] `dotnet build` green per `10-cross-cutting.md §4`.
- [ ] `<TreatWarningsAsErrors>` regressions = 0.
- [ ] `rg "TODO|FIXME|HACK" Analytics/` → 0 matches (any remaining → GitHub issue).
- [ ] All new FluentValidation rules have passing tests.
- [ ] All command handlers inject `ILogger<THandler>` + `RemoveByTagAsync` after SaveChanges (sample 3).
- [ ] All queries implement `ICacheableQuery` where applicable (sample 3).
- [ ] `ICurrentUser` only in handlers listed in `02-critical-rules.md §A-R9`.
- [ ] No bare `RequireAuthorization()`: `rg "RequireAuthorization\(\)\s*$" Analytics/Analytics.Presentation/` → 0 matches.
- [ ] `Analytics.Tests.Unit` ≥ **50 tests** passing.
- [ ] `Analytics.IntegrationTests` ≥ **15 tests** passing.
- [ ] `IntegrationEventTypeRegistryParityTests` passes.

---

## 2. Endpoint Smoke Test (17 endpoints)

Reviewer (Mohammad) records results in `Analytics/_smoke-test-runbook.md` (deleted before folder moves).

| # | Method | Path | Expected | Task |
|---|---|---|---|---|
| 1 | POST | `/api/v1/interactions` | 202 within 50ms | T1 |
| 2 | POST | `/api/v1/interactions` (dupe within 5min) | 202 + no DB write | T1 |
| 3 | GET | `/api/v1/admin/interactions` | 200 + cursor | T1 |
| 4 | GET | `/api/v1/admin/interactions/user/{userId}` | 200 | T1 |
| 5 | GET | `/api/v1/popular/tours` | 200 + top 20 sorted | T2 |
| 6 | GET | `/api/v1/popular/places` | 200 | T2 |
| 7 | GET | `/api/v1/popular/businesses` | 200 | T2 |
| 8 | GET | `/api/v1/trending` (fresh deploy) | 503 WindowNotReady | T2 |
| 9 | GET | `/api/v1/trending` (after 14d data) | 200 + TrendingRank populated | T2 |
| 10 | GET | `/api/v1/admin/dashboard` | 200 + 4 sections | T4 |
| 11 | GET | `/api/v1/admin/dashboard/revenue?from=X&to=Y` | 200 + time series | T4 |
| 12 | GET | `/api/v1/admin/dashboard/bookings?...` | 200 + funnel | T4 |
| 13 | GET | `/api/v1/admin/dashboard/users?...` | 200 + breakdown | T4 |
| 14 | GET | `/api/v1/provider/dashboard` (as provider) | 200 + self data | T5 |
| 15 | GET | `/api/v1/provider/analytics` | 200 | T5 |
| 16 | GET | `/api/v1/provider/my-tours` | 200 + cursor | T5 |
| 17 | GET | `/api/v1/admin/audit-logs` | 200 + cursor | T6 |
| 18 | POST | `/api/v1/admin/audit-logs/{id}/redact` | 200 + RedactedAt set | T6 |
| 19 | GET | `/api/v1/admin/audit-logs/export?from=X&to=Y` | 200 + CSV stream | T6 |

Any RED row blocks sign-off.

---

## 3. Outbox / Inbox Round-Trip

| Action | Outbox row | Logical name | Downstream side-effect | SLA |
|---|---|---|---|---|
| PopularityScoreCalculationService tick (T3) | 1 row | `analytics.popularity-scores.recalculated.v1` | popular/trending caches evicted; next GET returns fresh data | < 30s after tick |
| Trending recompute | 1 row | `analytics.trending.refreshed.v1` | (no external consumer this sprint, log only) | < 30s |
| POST /admin/audit-logs/{id}/redact (T6) | 1 row | `analytics.audit-log.entry-redacted.v1` | other admins see notification (Messaging consumer) | < 30s |

Inbox examples:
| Inbox event | Side effect | SLA |
|---|---|---|
| `booking.tour-booking.created.v1` | UserInteraction(BookingStarted) row + dashboard cache invalidate + AuditLog row | < 30s |
| `finance.payment.completed.v1` | PaymentSnapshot upserted + AuditLog row + revenue cache evict | < 30s |
| `content-tours.tour.published.v1` | PopularityScore row initialized (Score=0) | < 30s |

---

## 4. 24h Background Service Soak

- [ ] `PopularityScoreCalculationService` ticked **4 times** in 24h (every 6h).
- [ ] `InteractionIngestDrainService` ticked continuously, drained 100% of channel.
- [ ] OTEL: `popularity_recalc_failures_total` = 0.
- [ ] OTEL: `analytics_ingest_dropped_total` = 0 (or known acceptable count from load test).
- [ ] Zero `[ERROR]` Serilog entries from BG services.
- [ ] `EntityPopularitySnapshots` table has 4 daily snapshots (or skipped if < 14d uptime).
- [ ] `IngestDebounceMarker` rows older than 1h cleaned at end of each tick.

---

## 5. Performance Sanity (p95)

k6 / JMeter 5 minutes, 50 concurrent users:

| Endpoint | p95 target | Actual |
|---|---|---|
| POST /interactions | < 50 ms | _____ |
| GET /popular/tours (cached) | < 80 ms | _____ |
| GET /trending (cached) | < 80 ms | _____ |
| GET /admin/dashboard (cached) | < 80 ms | _____ |
| GET /admin/dashboard/revenue (cached) | < 100 ms | _____ |
| GET /provider/dashboard (cached) | < 100 ms | _____ |
| GET /admin/audit-logs cursor | < 150 ms | _____ |
| POST /interactions sustained 1000/sec | 0 drops | _____ |
| PopularityScoreCalc full sweep 10K entities | < 60 s | _____ |

---

## 6. Documentation Hygiene

- [ ] All new endpoints have XML doc summaries (auto-flows to Swagger).
- [ ] All 14 Analytics permissions listed in `Agents/permissions-inventory.md`.
- [ ] **Folder moves to `Agents/decisions/closed/Analytics/`**:
  ```powershell
  Move-Item -LiteralPath "Agents\tasks\Analytics" -Destination "Agents\decisions\closed\Analytics"
  ```
- [ ] `Phase1-Phase2-Completion-INDEX.md` §1 row Analytics ⬜→✅, link updated to closed/.
- [ ] `AGENTS.md` entry for Analytics/ module created.
- [ ] `agent-context.md §11.1` Analytics row → ✅ Phase 2.
- [ ] `Agents/error-log.md` updated with any new gotchas (high-write index tuning, dashboard cache stampedes, etc.).
- [ ] **ADR-007 drafted** if retention enforcement decision was made during sprint.

---

## 7. Sprint Retro & Demo (Fri 2027-02-26 11:00 AST)

15-min demo by Mohammad walking through:
1. Live POST /interactions burst (500/sec) → channel drains, dashboard reflects within 30s.
2. PopularityScoreCalc tick + trending DELTA computation.
3. Admin dashboard with live data update via cache invalidation.
4. Provider dashboard showing self-scoped data.
5. Audit log query + redaction live demo.

Retro doc `Agents/decisions/closed/Analytics/_retro.md`:
- What went well
- What hurt
- Action items for NEXT sprint (Authorization-Cleanup likely runs in parallel or follows)

---

## 8. Sign-Off

| Role | Name | Date | Signature |
|---|---|---|---|
| T1 owner | Mahmoud | _____ | _____ |
| T2 owner | Mohammad | _____ | _____ |
| T3 owner | Mohammad | _____ | _____ |
| T4 owner | Fadwa | _____ | _____ |
| T5 owner | Mohammad/Mahmoud | _____ | _____ |
| T6 owner | Fadwa | _____ | _____ |
| Tech Lead | _____ | _____ | _____ |

Once signatures collected → folder moves → INDEX updated → module marked ✅ in agent-context.md §11.1 → Authorization-Cleanup sprint may already be running in parallel.

---

