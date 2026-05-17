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
