# Outbox/Inbox Production Hardening — Implementation Plan

> **Status**: Proposed
> **Scope**: Adopt the `§5.18 Production Hardening` roadmap from `Agents/guide.md` into working YallaJo code via 5 sequential PRs.
> **Risk**: Low to Medium. Each PR is independently revertable. No runtime behavior regression expected — all changes are additive or backwards-compatible migrations.
> **Source**: Derived from §5.18.8 Implementation Checklist + 15+ industry references (Chris Richardson, Milan Jovanović, Kamil Grzybek, Wolverine, MassTransit, NServiceBus, Brighter, João Antunes).

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current State Assessment](#2-current-state-assessment)
3. [Target End State](#3-target-end-state)
4. [Design Principles](#4-design-principles)
5. [PR Sequence Overview](#5-pr-sequence-overview)
6. [Pre-Work: Baseline Verification](#6-pre-work-baseline-verification)
7. [PR 1 — Outbox Retention Cleanup (P0)](#7-pr-1--outbox-retention-cleanup-p0)
8. [PR 2 — Integration Event Type Registry (P0)](#8-pr-2--integration-event-type-registry-p0)
9. [PR 3 — Dead-Letter Ops Features (P1)](#9-pr-3--dead-letter-ops-features-p1)
10. [PR 4 — Trace Context + Adaptive Polling (P2)](#10-pr-4--trace-context--adaptive-polling-p2)
11. [PR 5 — Explicit Status Column + Cleanup Index (P3)](#11-pr-5--explicit-status-column--cleanup-index-p3)
12. [Deferred Items](#12-deferred-items)
13. [Post-Migration Verification](#13-post-migration-verification)
14. [Rollback Strategy](#14-rollback-strategy)
15. [Timeline](#15-timeline)
16. [Success Criteria](#16-success-criteria)

---

## 1. Executive Summary

The YallaJo outbox/inbox subsystem is functionally correct but lacks production hygiene. `guide.md §5.18` documented 10 hardening items sourced from industry best practices. This plan implements 8 of them across 5 PRs, deferring 2 (Claim Check pattern + per-handler ordering) until they become necessary.

**Delivery shape**:
- **PR 1 (P0)** — Retention cleanup. Prevents outbox tables from growing unbounded.
- **PR 2 (P0)** — Integration event type registry. Decouples stored type names from CLR `AssemblyQualifiedName`; makes refactoring safe.
- **PR 3 (P1)** — Dead-letter health endpoint, ops replay endpoint, OpenTelemetry metrics.
- **PR 4 (P2)** — W3C trace context propagation through outbox + adaptive polling.
- **PR 5 (P3)** — Explicit `Status` enum column + filtered cleanup index.

**Total effort**: 10–14 hours of focused work across 5 PRs. Executable over 5–8 working days with review cycles.

**No breaking changes**. All PRs are additive or use backward-compatible migrations.

---

## 2. Current State Assessment

### 2.1 Inventory

| Concern | Current State |
|---|---|
| Outbox message schema | `OutboxMessage` with 8 fields (`Id`, `Type`, `Content`, `OccurredOnUtc`, `ProcessedOnUtc`, `Error`, `RetryCount`, `LockedUntil`). Single table per module DbContext. |
| Polling interval | Fixed 10 seconds in `CompositeOutboxProcessor` (no adaptation) |
| Batch size | 20 messages per poll (`OutboxProcessor<TContext>.BatchSize`) |
| Lock duration | 5 minutes (`OutboxProcessor<TContext>.LockDuration`) |
| Max retries | 10 (`OutboxProcessor<TContext>.MaxRetryCount`) |
| Type identifier | `integrationEvent.GetType().AssemblyQualifiedName!` — **fragile to rename** |
| Dead-letter detection | Implicit: `RetryCount >= 10` filter excludes rows from dispatcher query |
| Dead-letter alerting | **None** — rows sit silently in main table |
| Dead-letter replay | **None** — manual SQL only |
| Retention / cleanup | **None** — processed rows accumulate forever |
| Observability metrics | **None** — no OpenTelemetry metrics for outbox |
| Trace context propagation | **Broken** — HTTP request span ends at commit; processor span is orphaned |
| Explicit status column | **None** — status is inferred from `ProcessedOnUtc` + `RetryCount` |

### 2.2 Integration event inventory (13 events across 4 modules)

| Module | Events |
|---|---|
| **Security.Contracts** (5) | `UserCreatedIntegrationEvent`, `EmailVerifiedIntegrationEvent`, `PasswordChangedIntegrationEvent`, `PasswordResetIntegrationEvent`, `PhoneNumberUpdatedIntegrationEvent` |
| **Auth.Contracts** (2) | `UserLoggedInIntegrationEvent`, `SessionRevokedIntegrationEvent` |
| **ContentCore.Contracts** (1) | `LanguageActivatedIntegrationEvent` |
| **ContentPlaces.Contracts** (5) | `PlaceCreatedIntegrationEvent`, `PlaceUpdatedIntegrationEvent`, `PlaceDeletedIntegrationEvent`, `ServiceItemCreateIntegrationEvent`, `ServiceItemDeletedIntegrationEvent` |

All 13 types must be registered in `IntegrationEventTypeRegistry` in PR 2.

### 2.3 Modules with `IOutboxProcessor` registered (13)

Every module registers its own processor:
```
Accounts, Analytics, Auth, Booking, ContentBlogs, ContentCore, ContentPlaces,
ContentSeo, ContentTours, Finance, Messaging, Security, Social, Tracking
```

All 13 will receive the same set of additions in PRs 1, 4, 5 (register `IOutboxCleaner<TContext>`, run EF migrations for new columns + indexes).

### 2.4 Blocking unknowns

None. All design decisions are documented in `guide.md §5.18`. No external library dependencies needed beyond what's already referenced (`OpenTelemetry.Api` is transitively available via the existing OTel setup).

---

## 3. Target End State

After all 5 PRs are merged:

### 3.1 New infrastructure files (SharedKernel.Infrastructure)

```
YallaJo.SharedKernel.Infrastructure/
├── BackgroundJobs/
│   ├── OutboxProcessor.cs                     ← existing, modified in PR 2 + PR 4
│   ├── CompositeOutboxProcessor.cs            ← existing, modified in PR 4
│   └── OutboxCleanupBackgroundService.cs      ← NEW in PR 1
├── Outbox/
│   ├── OutboxMessage.cs                       ← modified in PR 2, PR 4, PR 5
│   ├── OutboxMessageStatus.cs                 ← NEW in PR 5
│   ├── IOutboxCleaner.cs                      ← NEW in PR 1
│   ├── OutboxCleaner.cs                       ← NEW in PR 1
│   ├── OutboxCleanupOptions.cs                ← NEW in PR 1
│   ├── OutboxMetrics.cs                       ← NEW in PR 3
│   └── TraceContextHelpers.cs                 ← NEW in PR 4
└── Abstractions/
    └── Integration/
        └── IntegrationEventTypeRegistry.cs    ← NEW in PR 2
```

### 3.2 New Application-layer files

```
YallaJo.SharedKernel.Application/Abstractions/
└── Authorization/
    └── OpsFeatures.cs                         ← NEW in PR 3 (permission constants)

YallaJo.Api/Endpoints/
└── OpsEndpoints.cs                            ← NEW in PR 3 (replay + health endpoints)
```

### 3.3 Database schema changes

Applied via one EF migration per module DbContext (13 migrations per PR that has schema changes):

| PR | Migration Name | Changes |
|---|---|---|
| PR 1 | `AddOutboxCleanupIndex` | Filtered index on `ProcessedOnUtc` (optional — can defer to PR 5) |
| PR 4 | `AddOutboxTraceContext` | `TraceContext NVARCHAR(500) NULL` column |
| PR 5 | `AddOutboxStatusColumn` | `Status INT NOT NULL DEFAULT 0` column + backfill data migration + filtered cleanup index |

All migrations are **additive** — existing rows work unchanged; new columns have defaults.

### 3.4 appsettings.json additions

```json
{
  "OutboxCleanup": {
    "Enabled": true,
    "RetentionPeriod": "30.00:00:00",
    "CleanupInterval": "01:00:00",
    "BatchSize": 1000
  }
}
```

### 3.5 No breaking changes

- Old `AssemblyQualifiedName` type strings continue to deserialize via a fallback path in PR 2 until all rows drain.
- `Status` column defaults to `Pending` for new rows in PR 5; existing rows are backfilled.
- `TraceContext` column is nullable — old code that doesn't populate it works unchanged.
- All new services are opt-in via feature flags / options.

---

## 4. Design Principles

| # | Principle | Why |
|---|---|---|
| D1 | **Additive over destructive** | Every PR adds new files/columns; no deletion of existing data or behavior. |
| D2 | **Per-module symmetry** | Every module gets the same pattern. Scaffold templates will emit the right shape from day one for new modules. |
| D3 | **Backwards-compatible migrations** | Old outbox rows continue to work during the rollout window. |
| D4 | **Feature-flagged where possible** | Cleanup and metrics can be disabled via `appsettings.json` if something goes wrong. |
| D5 | **One source of truth per concern** | Type registry is a single static class. Metrics is a single static class. No scattered strings. |
| D6 | **No new external dependencies** | OpenTelemetry.Api is already referenced. No new NuGet packages needed. |
| D7 | **Test with Testcontainers, not InMemory** | Outbox semantics depend on SQL-specific features (filtered indexes, ExecuteDeleteAsync, optimistic concurrency). InMemory provider lies about these. |

---

## 5. PR Sequence Overview

Execute in order. Each PR leaves `main` green, buildable, and deployable.

| PR | Scope | Risk | Effort | EF Migration? |
|---|---|---|---|---|
| **PR 1** — Retention cleanup | 🟢 Low | 2–3 hrs | No (just a filtered index — optional) |
| **PR 2** — Type registry | 🟡 Medium | 2–3 hrs | No (dual-read during transition) |
| **PR 3** — Dead-letter ops + metrics | 🟢 Low | 3–4 hrs | No |
| **PR 4** — Trace context + adaptive polling | 🟡 Medium | 2–3 hrs | Yes (13 migrations for `TraceContext` column) |
| **PR 5** — Status column + cleanup index | 🟡 Medium | 2–3 hrs | Yes (13 migrations for `Status` column + index) |

**Total**: 10–14 hours of focused work. Spread over 5–8 working days with review cycles.

---

## 6. Pre-Work: Baseline Verification

Before starting, establish a known-good baseline (30 min, once).

### 6.1 Tasks

- [ ] Create feature branch: `git checkout -b refactor/outbox-hardening`
- [ ] Run full build: `dotnet build YallaJo.sln -c Debug --nologo` → expect 0 errors.
- [ ] Run all tests: `dotnet test --nologo --verbosity minimal` → expect 135+ passing.
- [ ] Snapshot current outbox state (per module):
  ```sql
  SELECT 'Security' AS Module, COUNT(*) AS Total,
         SUM(CASE WHEN ProcessedOnUtc IS NULL THEN 1 ELSE 0 END) AS Pending,
         SUM(CASE WHEN RetryCount >= 10 THEN 1 ELSE 0 END) AS DeadLettered
  FROM security.OutboxMessages
  -- UNION ALL for every module DbContext...
  ```
  Save as `outbox-baseline.txt`.
- [ ] Verify dispatch loop is running: check logs for `"Processing {Count} outbox messages for {Context}"` within the last minute.

### 6.2 Verification

Golden baseline recorded. Any deviation after each PR = investigate immediately.

---

## 7. PR 1 — Outbox Retention Cleanup (P0)

**Branch**: `refactor/outbox-hardening-pr1-cleanup`
**Risk**: 🟢 Low — new hosted service, opt-in via configuration, cannot affect production data if disabled.
**Effort**: 2–3 hours.

### 7.1 Files to create

**File 1** — `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxCleanupOptions.cs`

```csharp
namespace YallaJo.SharedKernel.Infrastructure.Outbox;

public sealed class OutboxCleanupOptions
{
    /// <summary>Master toggle. Set false to disable the cleanup service entirely.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How long to keep successfully processed rows. Default 30 days.</summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>How often the cleanup loop runs. Default 1 hour.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Batch size for the delete operation (avoid table locks).</summary>
    public int BatchSize { get; set; } = 1000;
}
```

**File 2** — `YallaJo.SharedKernel.Infrastructure/Outbox/IOutboxCleaner.cs`

```csharp
namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Per-module cleanup abstraction. Each module registers its own implementation
/// via IOutboxCleaner -> OutboxCleaner&lt;{Module}DbContext&gt;.
/// The central OutboxCleanupBackgroundService resolves all IOutboxCleaner instances
/// and iterates over them each tick.
/// </summary>
public interface IOutboxCleaner
{
    string ModuleName { get; }
    Task<int> DeleteProcessedBeforeAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct);
}
```

**File 3** — `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxCleaner.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

namespace YallaJo.SharedKernel.Infrastructure.Outbox;

/// <summary>
/// Deletes successfully processed outbox rows older than the cutoff.
/// NEVER deletes dead-lettered rows (RetryCount >= MaxRetryCount).
/// Uses ExecuteDeleteAsync for efficient bulk delete in batches.
/// </summary>
public sealed class OutboxCleaner<TContext>(IServiceScopeFactory scopeFactory) : IOutboxCleaner
    where TContext : DbContext
{
    public string ModuleName => typeof(TContext).Name;

    public async Task<int> DeleteProcessedBeforeAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        int totalDeleted = 0;
        int deletedInBatch;

        // Batch-delete to avoid long transactions + table locks.
        do
        {
            deletedInBatch = await db.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc != null
                         && m.ProcessedOnUtc < cutoffUtc
                         && m.RetryCount < OutboxProcessor<TContext>.MaxRetryCount)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deletedInBatch;

        } while (deletedInBatch == batchSize && !ct.IsCancellationRequested);

        return totalDeleted;
    }
}
```

**File 4** — `YallaJo.SharedKernel.Infrastructure/BackgroundJobs/OutboxCleanupBackgroundService.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Infrastructure.Outbox;

namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs;

/// <summary>
/// Single hosted service that deletes successfully processed outbox rows
/// older than the retention window. Runs every {CleanupInterval} (default 1 hour).
///
/// NEVER deletes dead-lettered rows — those require manual investigation.
///
/// Per-module: resolves every IOutboxCleaner from DI and iterates sequentially.
/// </summary>
public sealed class OutboxCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<OutboxCleanupBackgroundService> logger,
    IOptions<OutboxCleanupOptions> options) : BackgroundService
{
    private readonly OutboxCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Outbox cleanup service is DISABLED by configuration");
            return;
        }

        logger.LogInformation(
            "Outbox cleanup service started — retention: {Retention}, interval: {Interval}, batch: {Batch}",
            _options.RetentionPeriod, _options.CleanupInterval, _options.BatchSize);

        // Initial delay to let the app warm up + DbContexts initialize
        try { await Task.Delay(TimeSpan.FromMinutes(1), ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CleanupAllModulesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox cleanup iteration failed");
            }

            try { await Task.Delay(_options.CleanupInterval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }

        logger.LogInformation("Outbox cleanup service stopped");
    }

    private async Task CleanupAllModulesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var cutoff = DateTime.UtcNow - _options.RetentionPeriod;
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>().ToList();

        logger.LogDebug(
            "Outbox cleanup starting — {ModuleCount} modules, cutoff {Cutoff}",
            cleaners.Count, cutoff);

        int totalDeleted = 0;

        foreach (var cleaner in cleaners)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var deleted = await cleaner.DeleteProcessedBeforeAsync(cutoff, _options.BatchSize, ct);
                if (deleted > 0)
                {
                    logger.LogInformation(
                        "Outbox cleanup: deleted {Count} rows for {Module}",
                        deleted, cleaner.ModuleName);
                    totalDeleted += deleted;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox cleanup failed for {Module}", cleaner.ModuleName);
            }
        }

        if (totalDeleted > 0)
            logger.LogInformation("Outbox cleanup complete: {Total} total rows deleted", totalDeleted);
    }
}
```

### 7.2 Files to modify

**File 5** — `YallaJo.SharedKernel.Infrastructure/DependencyInjection.cs`

Add one block:
```csharp
// ── Outbox cleanup hosted service ──
services.Configure<OutboxCleanupOptions>(config.GetSection("OutboxCleanup"));
services.AddHostedService<OutboxCleanupBackgroundService>();
```

**Files 6–18** — Each module's `{Module}.Infrastructure/DependencyInjection.cs` (13 files)

Add one line after the existing `IOutboxProcessor` registration:
```csharp
services.AddScoped<IOutboxProcessor, OutboxProcessor<{Module}DbContext>>();
services.AddScoped<IOutboxCleaner, OutboxCleaner<{Module}DbContext>>();  // NEW
```

**File 19** — `YallaJo.Api/appsettings.json` (+ appsettings.Development.json)

Add:
```json
{
  "OutboxCleanup": {
    "Enabled": true,
    "RetentionPeriod": "30.00:00:00",
    "CleanupInterval": "01:00:00",
    "BatchSize": 1000
  }
}
```

### 7.3 Tests to add

**File 20** — `tests/SharedKernel.Tests.Unit/OutboxCleanerTests.cs`

```csharp
[Fact]
public async Task DeletesProcessedRows_OlderThanCutoff()
{
    // Arrange: 3 rows — 1 old processed, 1 fresh processed, 1 dead-lettered old
    var cleaner = new OutboxCleaner<TestDbContext>(scopeFactory);

    // Act
    var deleted = await cleaner.DeleteProcessedBeforeAsync(
        cutoffUtc: DateTime.UtcNow.AddDays(-30), batchSize: 100, default);

    // Assert: only the old processed row was deleted
    deleted.Should().Be(1);
    (await db.OutboxMessages.CountAsync()).Should().Be(2); // fresh + dead-lettered remain
}

[Fact]
public async Task NeverDeletes_DeadLetteredRows()
{
    // Arrange: old dead-lettered row (RetryCount = MaxRetryCount)
    // Act: cleanup with aggressive cutoff
    // Assert: dead-lettered row still exists (evidence preserved)
}
```

### 7.4 Verification

- [ ] `dotnet build YallaJo.sln -c Debug --nologo` → 0 errors.
- [ ] `dotnet test --nologo` → all tests pass (new cleanup tests included).
- [ ] Start API. Wait 1 minute. Check logs for:
  ```
  [INFO] Outbox cleanup service started — retention: 30.00:00:00, interval: 01:00:00, batch: 1000
  ```
- [ ] If `appsettings.Development.json` overrides `Enabled: false`, verify logs show:
  ```
  [INFO] Outbox cleanup service is DISABLED by configuration
  ```
- [ ] Manually insert an old processed row via SQL, wait for next cleanup tick (1 hour — or temporarily set `CleanupInterval: "00:00:30"` for testing), confirm deletion.
- [ ] Confirm baseline outbox counts restore: `SELECT COUNT(*) FROM security.OutboxMessages` — should match baseline snapshot (minus cleanup-eligible rows if any).

### 7.5 Commit

```
feat(outbox): add retention cleanup background service (P0 hardening)

Adds OutboxCleanupBackgroundService that runs every hour and deletes
successfully processed outbox rows older than the retention window
(default 30 days). Dead-lettered rows (RetryCount >= MaxRetryCount)
are NEVER deleted — evidence preserved for manual replay.

New files:
  - SharedKernel.Infrastructure/Outbox/OutboxCleanupOptions.cs
  - SharedKernel.Infrastructure/Outbox/IOutboxCleaner.cs
  - SharedKernel.Infrastructure/Outbox/OutboxCleaner.cs
  - SharedKernel.Infrastructure/BackgroundJobs/OutboxCleanupBackgroundService.cs
  - tests/SharedKernel.Tests.Unit/OutboxCleanerTests.cs

Modified files:
  - SharedKernel.Infrastructure/DependencyInjection.cs
    (registers hosted service + options binding)
  - 13 {Module}.Infrastructure/DependencyInjection.cs files
    (register IOutboxCleaner per module)
  - YallaJo.Api/appsettings.json (+ Development)
    (OutboxCleanup section)

Opt-out: set `OutboxCleanup:Enabled` = false in appsettings to disable.

Build: 0 errors. Tests: 135+ pass (new cleanup tests added).
No schema changes. Non-breaking.

Refs: guide.md §5.18.1, outbox-hardening-implementation-plan.md PR 1
```

---

## 8. PR 2 — Integration Event Type Registry (P0)

**Branch**: `refactor/outbox-hardening-pr2-type-registry`
**Risk**: 🟡 Medium — replaces the mechanism by which outbox messages are deserialized. Mitigation: dual-read (try registry first, fall back to `Type.GetType`) during transition.
**Effort**: 2–3 hours.

### 8.1 Files to create

**File 1** — `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`

```csharp
using Accounts; // TBD per actual namespaces — pulls all integration events
using Auth.Contracts.IntegrationEvents;
using ContentCore.Contracts.IntegrationEvents;
using ContentPlaces.Contracts.IntegrationEvents;
using Security.Contracts.IntegrationEvents;

namespace YallaJo.SharedKernel.Infrastructure.Abstractions.Integration;

/// <summary>
/// Maps stable logical names to CLR types for integration events.
/// Every integration event MUST be registered here.
/// NEVER remove or rename an existing key — only add new ones.
/// To rename an event type, add a new v2 alias pointing to the same CLR type.
///
/// The stored OutboxMessage.Type column uses these short names, not
/// AssemblyQualifiedName — making refactors safe.
/// </summary>
public static class IntegrationEventTypeRegistry
{
    private static readonly Dictionary<string, Type> _nameToType = new(StringComparer.Ordinal)
    {
        // ── Security (5 events) ──
        ["security.user.created.v1"]             = typeof(UserCreatedIntegrationEvent),
        ["security.user.email-verified.v1"]      = typeof(EmailVerifiedIntegrationEvent),
        ["security.user.password-changed.v1"]    = typeof(PasswordChangedIntegrationEvent),
        ["security.user.password-reset.v1"]      = typeof(PasswordResetIntegrationEvent),
        ["security.user.phone-updated.v1"]       = typeof(PhoneNumberUpdatedIntegrationEvent),

        // ── Auth (2 events) ──
        ["auth.user.logged-in.v1"]               = typeof(UserLoggedInIntegrationEvent),
        ["auth.session.revoked.v1"]              = typeof(SessionRevokedIntegrationEvent),

        // ── ContentCore (1 event) ──
        ["content-core.language.activated.v1"]   = typeof(LanguageActivatedIntegrationEvent),

        // ── ContentPlaces (5 events) ──
        ["content-places.place.created.v1"]      = typeof(PlaceCreatedIntegrationEvent),
        ["content-places.place.updated.v1"]      = typeof(PlaceUpdatedIntegrationEvent),
        ["content-places.place.deleted.v1"]      = typeof(PlaceDeletedIntegrationEvent),
        ["content-places.service-item.created.v1"] = typeof(ServiceItemCreateIntegrationEvent),
        ["content-places.service-item.deleted.v1"] = typeof(ServiceItemDeletedIntegrationEvent),
    };

    private static readonly Dictionary<Type, string> _typeToName =
        _nameToType
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.First().Key);

    public static string GetName(Type type)
        => _typeToName.TryGetValue(type, out var name)
            ? name
            : throw new InvalidOperationException(
                $"Integration event type {type.FullName} is not registered in " +
                $"{nameof(IntegrationEventTypeRegistry)}. Add it before publishing.");

    public static bool TryGetType(string name, out Type? type)
        => _nameToType.TryGetValue(name, out type);

    /// <summary>All registered types — used by tests to verify completeness.</summary>
    public static IReadOnlyCollection<Type> AllRegisteredTypes => _typeToName.Keys;
}
```

### 8.2 Files to modify

**File 2** — `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessage.cs`

```csharp
// BEFORE:
public static OutboxMessage Create(IIntegrationEvent integrationEvent)
{
    return new OutboxMessage
    {
        Id = Guid.CreateVersion7(),
        Type = integrationEvent.GetType().AssemblyQualifiedName!,
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        // ...
    };
}

// AFTER:
public static OutboxMessage Create(IIntegrationEvent integrationEvent)
{
    return new OutboxMessage
    {
        Id = Guid.CreateVersion7(),
        Type = IntegrationEventTypeRegistry.GetName(integrationEvent.GetType()),  // ← changed
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        // ...
    };
}
```

**File 3** — `YallaJo.SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs`

Replace the type resolution block with dual-read (registry first, fall back to AQN):

```csharp
// BEFORE:
var eventType = Type.GetType(message.Type);
if (eventType is null)
{
    logger.LogWarning("Unknown event type: {Type}", message.Type);
    message.MarkAsFailed($"Unknown event type: {message.Type}");
    continue;
}

// AFTER (transitional — dual-read for backward compat with AQN rows):
Type? eventType = null;

// Path 1: new-format — short name in registry
if (IntegrationEventTypeRegistry.TryGetType(message.Type, out eventType))
{
    // resolved
}
// Path 2: old-format — legacy AssemblyQualifiedName for rows written before this PR
else
{
    eventType = Type.GetType(message.Type);
}

if (eventType is null)
{
    logger.LogError(
        "Unknown integration event type {Type} (message {MessageId}) — not in registry and " +
        "AssemblyQualifiedName resolution failed. Dead-lettering.",
        message.Type, message.Id);

    // Force dead-letter by bumping RetryCount to MaxRetryCount
    while (message.RetryCount < OutboxProcessor<TContext>.MaxRetryCount)
        message.MarkAsFailed($"Unknown type: {message.Type}");
    continue;
}
```

### 8.3 Tests to add

**File 4** — `tests/SharedKernel.Tests.Unit/IntegrationEventTypeRegistryTests.cs`

```csharp
public class IntegrationEventTypeRegistryTests
{
    [Fact]
    public void AllIntegrationEventTypes_AreRegistered()
    {
        // Arrange: scan all loaded assemblies for IIntegrationEvent implementations
        var allTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t)
                     && !t.IsAbstract && !t.IsInterface)
            .ToList();

        var registered = IntegrationEventTypeRegistry.AllRegisteredTypes.ToHashSet();

        var missing = allTypes.Where(t => !registered.Contains(t)).ToList();

        // Assert: every concrete IIntegrationEvent must be registered
        missing.Should().BeEmpty(
            "every concrete IIntegrationEvent type must be registered in " +
            "IntegrationEventTypeRegistry. Missing: " +
            string.Join(", ", missing.Select(t => t.FullName)));
    }

    [Fact]
    public void GetName_ReturnsShortName()
    {
        var name = IntegrationEventTypeRegistry.GetName(typeof(UserCreatedIntegrationEvent));
        name.Should().Be("security.user.created.v1");
    }

    [Fact]
    public void GetName_UnregisteredType_Throws()
    {
        var act = () => IntegrationEventTypeRegistry.GetName(typeof(object));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TryGetType_KnownName_ReturnsCorrectType()
    {
        IntegrationEventTypeRegistry.TryGetType("security.user.created.v1", out var type)
            .Should().BeTrue();
        type.Should().Be<UserCreatedIntegrationEvent>();
    }
}
```

**File 5** — Update `tests/SharedKernel.Tests.Unit/OutboxProcessorTests.cs` to add a backward-compat test:

```csharp
[Fact]
public async Task ProcessesLegacy_AssemblyQualifiedNameRows()
{
    // Arrange: write an outbox row with old-format Type (AQN)
    var legacyRow = new OutboxMessage { /* Type = typeof(...).AssemblyQualifiedName! */ };

    // Act: run processor

    // Assert: dual-read worked, row is marked processed
}
```

### 8.4 Verification

- [ ] `dotnet build` → 0 errors.
- [ ] `dotnet test --nologo` → all tests pass, including `AllIntegrationEventTypes_AreRegistered`.
- [ ] Manually verify a freshly written outbox row has `Type = "security.user.created.v1"` (not AQN).
- [ ] Manually verify a pre-existing AQN row still processes successfully (deserialize via fallback path).
- [ ] After 48 hours (typical cleanup window), confirm no AQN-format rows remain: `SELECT COUNT(*) FROM security.OutboxMessages WHERE Type LIKE '%, %'`.

### 8.5 Commit

```
feat(outbox): add IntegrationEventTypeRegistry for safe refactoring (P0 hardening)

Replaces fragile AssemblyQualifiedName-based type identification with a
stable short-name registry. Makes renaming integration event classes,
namespaces, or assemblies safe.

Stored format changes:
  BEFORE: OutboxMessage.Type = "Security.Contracts.IntegrationEvents.UserCreatedIntegrationEvent, Security.Contracts, Version=..."
  AFTER:  OutboxMessage.Type = "security.user.created.v1"

Backward compat: OutboxProcessor dual-reads — registry first, falls back
to AssemblyQualifiedName for rows written before this PR. Fallback path
can be removed in a future PR once all legacy rows drain.

13 integration events registered across 4 modules:
  - Security (5), Auth (2), ContentCore (1), ContentPlaces (5)

Test: AllIntegrationEventTypes_AreRegistered fails the build if any
new integration event is added without a registry entry.

Build: 0 errors. Tests: 135+ pass.
No schema changes. Non-breaking.

Refs: guide.md §5.18.2, outbox-hardening-implementation-plan.md PR 2
```

---

## 9. PR 3 — Dead-Letter Ops Features (P1)

**Branch**: `refactor/outbox-hardening-pr3-ops`
**Risk**: 🟢 Low — purely additive: new endpoints + new metrics. No changes to existing behavior.
**Effort**: 3–4 hours.

### 9.1 OpenTelemetry Metrics

**File 1** — `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMetrics.cs`

```csharp
using System.Diagnostics.Metrics;

namespace YallaJo.SharedKernel.Infrastructure.Outbox;

public static class OutboxMetrics
{
    public static readonly Meter Meter = new("YallaJo.Outbox", "1.0.0");

    public static readonly Counter<long> ProcessedTotal =
        Meter.CreateCounter<long>("outbox.processed.total",
            description: "Total outbox messages successfully dispatched");

    public static readonly Counter<long> FailedTotal =
        Meter.CreateCounter<long>("outbox.failed.total",
            description: "Total outbox messages that failed dispatch (will retry)");

    public static readonly Counter<long> DeadLetteredTotal =
        Meter.CreateCounter<long>("outbox.dead_lettered.total",
            description: "Total outbox messages moved to dead-letter (RetryCount >= max)");

    public static readonly Histogram<double> DispatchLatencyMs =
        Meter.CreateHistogram<double>("outbox.dispatch.latency_ms",
            description: "Time from message creation to successful dispatch (ms)");

    public static readonly Histogram<int> RetryCountDistribution =
        Meter.CreateHistogram<int>("outbox.retry.count",
            description: "RetryCount at the moment of success — histogram of retry distribution");

    public static readonly Counter<long> HandlerSuccessTotal =
        Meter.CreateCounter<long>("outbox.handler.success.total",
            description: "Per-handler success count (tag: handler)");

    public static readonly Counter<long> HandlerFailureTotal =
        Meter.CreateCounter<long>("outbox.handler.failure.total",
            description: "Per-handler failure count (tag: handler)");

    public static readonly Counter<long> CleanupDeletedTotal =
        Meter.CreateCounter<long>("outbox.cleanup.deleted.total",
            description: "Total rows deleted by OutboxCleanupBackgroundService");
}
```

### 9.2 Record metrics in `OutboxProcessor` and `OutboxCleanupBackgroundService`

Modify `OutboxProcessor.cs`:

```csharp
// On successful dispatch (after MarkAsProcessed):
var latency = (DateTime.UtcNow - message.OccurredOnUtc).TotalMilliseconds;
OutboxMetrics.ProcessedTotal.Add(1,
    new KeyValuePair<string, object?>("module", typeof(TContext).Name));
OutboxMetrics.DispatchLatencyMs.Record(latency);
OutboxMetrics.RetryCountDistribution.Record(message.RetryCount);

// On handler success (inside foreach handlers loop):
OutboxMetrics.HandlerSuccessTotal.Add(1,
    new KeyValuePair<string, object?>("handler", handler.GetType().Name));

// On handler failure:
OutboxMetrics.HandlerFailureTotal.Add(1,
    new KeyValuePair<string, object?>("handler", handler.GetType().Name));

// On dead-letter (when RetryCount transitions to MaxRetryCount):
if (message.RetryCount == OutboxProcessor<TContext>.MaxRetryCount)
{
    OutboxMetrics.DeadLetteredTotal.Add(1,
        new KeyValuePair<string, object?>("module", typeof(TContext).Name),
        new KeyValuePair<string, object?>("type", message.Type));
}
```

Modify `OutboxCleanupBackgroundService.cs`:
```csharp
// After totalDeleted is computed:
if (totalDeleted > 0)
    OutboxMetrics.CleanupDeletedTotal.Add(totalDeleted);
```

### 9.3 Register the Meter with OpenTelemetry

**File 2** — `YallaJo.SharedKernel.Infrastructure/DependencyInjection.cs`

Add the meter to the OTel configuration (the exact location depends on your current OTel wiring — check `Program.cs` for `AddOpenTelemetry().WithMetrics(...)`):

```csharp
// Wherever OTel metrics are configured:
.ConfigureResource(r => r.AddService("YallaJo.Api"))
.WithMetrics(m => m
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddMeter("YallaJo.Outbox")  // ← NEW
    .AddOtlpExporter())
```

### 9.4 Dead-letter health endpoint

**File 3** — `YallaJo.Api/HealthChecks/OutboxDeadLetterHealthCheck.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace YallaJo.Api.HealthChecks;

/// <summary>
/// Returns unhealthy if any module has dead-lettered outbox messages.
/// Integrates with /health/live + /health/ready endpoints.
/// </summary>
public sealed class OutboxDeadLetterHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>();

        var data = new Dictionary<string, object>();
        int totalDeadLetters = 0;

        foreach (var cleaner in cleaners)
        {
            // We can add a new method to IOutboxCleaner OR query directly via scopeFactory
            // Simplest: add CountDeadLetteredAsync to IOutboxCleaner interface
            var count = await CountDeadLettersForModule(cleaner.ModuleName, cancellationToken);
            data[cleaner.ModuleName] = count;
            totalDeadLetters += count;
        }

        return totalDeadLetters == 0
            ? HealthCheckResult.Healthy("No dead-lettered outbox messages", data)
            : HealthCheckResult.Degraded(
                $"{totalDeadLetters} dead-lettered outbox message(s) require manual intervention",
                data: data);
    }

    private Task<int> CountDeadLettersForModule(string moduleName, CancellationToken ct)
    {
        // Implementation uses the appropriate DbContext per module
        // (consider adding CountDeadLetteredAsync to IOutboxCleaner to avoid reflection)
        throw new NotImplementedException("Use IOutboxCleaner extension method");
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddHealthChecks()
    // ... existing checks ...
    .AddCheck<OutboxDeadLetterHealthCheck>(
        "outbox-dead-letters",
        failureStatus: HealthStatus.Degraded,
        tags: ["outbox", "ops"]);
```

### 9.5 Dead-letter replay endpoint

**File 4** — `YallaJo.SharedKernel.Application/Abstractions/Authorization/OpsFeatures.cs`

Add new permission features:
```csharp
public static class OpsFeatures
{
    public const string Outbox = nameof(Outbox);
}
```

Register in `Security.Infrastructure/Seeding/SecurityPermissionCatalog.cs`:
```csharp
new(OpsFeatures.Outbox, AppAction.Read,   PermissionGroup.SystemAccess, "View outbox dead-letters"),
new(OpsFeatures.Outbox, AppAction.Replay, PermissionGroup.SystemAccess, "Replay a dead-lettered outbox message"),
```

Add `Replay` constant to `AppAction`:
```csharp
public const string Replay = nameof(Replay);
```

**File 5** — `YallaJo.Api/Endpoints/OpsEndpoints.cs`

```csharp
using MediatR;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Api.Endpoints;

public static class OpsEndpoints
{
    public static IEndpointRouteBuilder MapOpsEndpoints(this IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/v1/ops/outbox")
            .WithTags("Operations — Outbox");

        ops.MapGet("/dead-letters",
            async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ListDeadLettersQuery(), ct);
                return result.ToApiResult();
            })
            .WithMetadata(new MustHavePermissionAttribute(OpsFeatures.Outbox, AppAction.Read))
            .WithName("ListOutboxDeadLetters");

        ops.MapPost("/dead-letters/{module}/{id:guid}/replay",
            async (string module, Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReplayDeadLetterCommand(module, id), ct);
                return result.ToApiResult();
            })
            .WithMetadata(new MustHavePermissionAttribute(OpsFeatures.Outbox, AppAction.Replay))
            .WithName("ReplayOutboxDeadLetter");

        return app;
    }
}
```

**File 6–7** — Create MediatR command + handler:
- `YallaJo.SharedKernel.Application/Commands/Outbox/ReplayDeadLetterCommand.cs`
- `YallaJo.SharedKernel.Infrastructure/Handlers/ReplayDeadLetterCommandHandler.cs`

The handler clones the dead-lettered row into a new row with `Guid.CreateVersion7()` Id, fresh `OccurredOnUtc`, zero `RetryCount`, null `Error` — preserves the original evidence.

### 9.6 Tests

- Unit test for `OutboxDeadLetterHealthCheck` — returns Healthy when zero dead-letters, Degraded when >0.
- Integration test for `/api/v1/ops/outbox/dead-letters/{module}/{id}/replay` — requires `Permission.Outbox.Replay` claim.

### 9.7 Verification

- [ ] `dotnet build` → 0 errors.
- [ ] `dotnet test --nologo` → all tests pass.
- [ ] `GET /health/ready` returns the outbox check in its data.
- [ ] Deliberately insert a dead-lettered row (SQL update). `GET /health/outbox-dead-letters` returns `503 Degraded`.
- [ ] Replay endpoint creates a clone with new Id, preserves original.
- [ ] OTel metrics visible — check your OTel exporter (Prometheus/OTLP collector) shows `outbox.processed.total` incrementing.
- [ ] No permission to replay → 403; with permission → 200.

### 9.8 Commit

```
feat(outbox): add dead-letter ops endpoints + OpenTelemetry metrics (P1 hardening)

Adds 8 OpenTelemetry metrics, a dead-letter health check, and two ops
endpoints (list + replay) for operational visibility and intervention.

New metrics (meter: YallaJo.Outbox):
  - outbox.processed.total      (tag: module)
  - outbox.failed.total         (tag: module)
  - outbox.dead_lettered.total  (tags: module, type)
  - outbox.dispatch.latency_ms
  - outbox.retry.count
  - outbox.handler.success.total  (tag: handler)
  - outbox.handler.failure.total  (tag: handler)
  - outbox.cleanup.deleted.total

New endpoints (both require Permission.Outbox.{Read|Replay}):
  GET  /api/v1/ops/outbox/dead-letters
  POST /api/v1/ops/outbox/dead-letters/{module}/{id}/replay

New health check:
  /health/ready — includes outbox-dead-letters check (Degraded if > 0)

Replay strategy: CLONE the dead-lettered row into a new row with a fresh
Id + OccurredOnUtc + RetryCount=0 + Error=null. Original row remains
for audit.

Permissions:
  - OpsFeatures.Outbox registered in SecurityPermissionCatalog
  - Permission.Outbox.Read, Permission.Outbox.Replay

Build: 0 errors. Tests: 135+ pass.
No schema changes. Non-breaking.

Refs: guide.md §5.18.3 + §5.18.5, outbox-hardening-implementation-plan.md PR 3
```

---

## 10. PR 4 — Trace Context + Adaptive Polling (P2)

**Branch**: `refactor/outbox-hardening-pr4-tracing`
**Risk**: 🟡 Medium — adds a new nullable column to 13 outbox tables via EF migration. Rollback requires dropping the column (non-destructive).
**Effort**: 2–3 hours.

### 10.1 Schema change — add `TraceContext` column

**File 1** — Modify `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessage.cs`:

```csharp
public sealed class OutboxMessage
{
    // ... existing 8 fields ...
    public string? TraceContext { get; private set; }  // NEW — W3C traceparent + baggage as JSON

    public static OutboxMessage Create(IIntegrationEvent integrationEvent)
    {
        return new OutboxMessage
        {
            // ... existing ...
            TraceContext = TraceContextHelpers.Capture(),  // NEW
        };
    }
}
```

**File 2** — `YallaJo.SharedKernel.Infrastructure/Outbox/TraceContextHelpers.cs`

```csharp
using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace YallaJo.SharedKernel.Infrastructure.Outbox;

public static class TraceContextHelpers
{
    public static string? Capture()
    {
        var activity = Activity.Current;
        if (activity is null) return null;

        var entries = new List<KeyValuePair<string, string>>();
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            entries,
            (carrier, key, value) => carrier.Add(new(key, value)));

        return entries.Count == 0 ? null : JsonSerializer.Serialize(entries);
    }

    public static Activity? Restore(string? serialized, ActivitySource source, string spanName)
    {
        if (string.IsNullOrEmpty(serialized))
            return source.StartActivity(spanName, ActivityKind.Producer);

        try
        {
            var entries = JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(serialized)!;

            var parentContext = Propagators.DefaultTextMapPropagator.Extract(
                default, entries,
                (carrier, key) => carrier.Where(e => e.Key == key).Select(e => e.Value));

            Baggage.Current = parentContext.Baggage;

            return source.StartActivity(
                spanName,
                ActivityKind.Producer,
                parentContext.ActivityContext);
        }
        catch
        {
            // Malformed trace context — start a fresh activity rather than fail
            return source.StartActivity(spanName, ActivityKind.Producer);
        }
    }
}
```

**File 3** — EF configuration (add to a shared base config or each DbContext):

```csharp
// OutboxMessageConfiguration.cs
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // ... existing config ...

        builder.Property(m => m.TraceContext)
            .HasMaxLength(500)  // matches Brighter's approach
            .IsRequired(false);
    }
}
```

### 10.2 EF Migrations — 13 new migrations

One per module DbContext. Example for Security:
```bash
dotnet ef migrations add AddOutboxTraceContext `
  --project Security.Infrastructure `
  --startup-project YallaJo.Api `
  --context SecurityDbContext
```

Repeat for all 13: Accounts, Analytics, Auth, Booking, ContentBlogs, ContentCore, ContentPlaces, ContentSeo, ContentTours, Finance, Messaging, Security, Social, Tracking.

The generated migrations should all look like:
```csharp
migrationBuilder.AddColumn<string>(
    name: "TraceContext",
    schema: "security",
    table: "OutboxMessages",
    type: "nvarchar(500)",
    maxLength: 500,
    nullable: true);
```

### 10.3 Modify `OutboxProcessor` to restore context

```csharp
// Inside ProcessOutboxMessagesAsync, at the top of the foreach message loop:
using var activity = TraceContextHelpers.Restore(
    message.TraceContext,
    OutboxActivitySource.Instance,
    "outbox.dispatch");

activity?.SetTag("outbox.message.id", message.Id);
activity?.SetTag("outbox.message.type", message.Type);
activity?.SetTag("outbox.retry.count", message.RetryCount);
activity?.SetTag("outbox.module", typeof(TContext).Name);

// ... existing handler invocation ...

activity?.SetStatus(handlerFailures.Count == 0
    ? ActivityStatusCode.Ok
    : ActivityStatusCode.Error);
if (handlerFailures.Count > 0)
    activity?.SetTag("outbox.handler.failures", string.Join("; ", handlerFailures.Take(3)));
```

Plus a new singleton `OutboxActivitySource`:
```csharp
internal static class OutboxActivitySource
{
    public static readonly ActivitySource Instance = new("YallaJo.Outbox");
}
```

### 10.4 Register the ActivitySource with OpenTelemetry

```csharp
// Program.cs (wherever OTel tracing is configured):
.WithTracing(t => t
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddEntityFrameworkCoreInstrumentation()
    .AddSource("YallaJo.Outbox")   // ← NEW
    .AddOtlpExporter())
```

### 10.5 Adaptive polling

**File 4** — Modify `CompositeOutboxProcessor.cs`:

```csharp
// Change ProcessOutboxMessagesAsync signature to return count:
public async Task<int> ProcessOutboxMessagesAsync(CancellationToken ct = default)
{
    // ... existing ...
    return messages.Count; // return how many we processed this cycle
}

// In CompositeOutboxProcessor.ExecuteAsync:
var processedAny = false;
foreach (var processor in processors)
{
    try
    {
        var count = await processor.ProcessOutboxMessagesAsync(ct);
        if (count > 0) processedAny = true;
    }
    catch { /* existing logging */ }
}

// Adaptive delay: poll fast while draining, back off when idle
var delay = processedAny
    ? TimeSpan.FromMilliseconds(200)   // keep draining quickly
    : TimeSpan.FromSeconds(10);         // idle backoff (current behavior)

await Task.Delay(delay, ct);
```

Also update `IOutboxProcessor` interface to return `int`:
```csharp
public interface IOutboxProcessor
{
    Task<int> ProcessOutboxMessagesAsync(CancellationToken ct = default);
}
```

### 10.6 Tests

- `TraceContextHelpersTests` — capture, roundtrip, restore.
- Integration test — end-to-end trace propagation: HTTP request → command → outbox write → processor → consumer → verify activity.ParentId chain is preserved.

### 10.7 Verification

- [ ] `dotnet build` → 0 errors.
- [ ] `dotnet ef database update` applies all 13 new migrations cleanly.
- [ ] `dotnet test --nologo` → all tests pass.
- [ ] Trigger a command that writes to outbox. Inspect the outbox row — `TraceContext` is populated with a JSON array containing `traceparent`.
- [ ] Check your trace exporter (Jaeger / Tempo / OTel collector). The request span, outbox write span, and consumer handler span should all be in one trace with proper parent/child relationships.
- [ ] Under load, verify the processor's polling is adaptive — logs show sub-second cycle times while draining and 10s cycles when idle.

### 10.8 Commit

```
feat(outbox): propagate W3C trace context + adaptive polling (P2 hardening)

Schema change (13 migrations — one per module DbContext):
  Add OutboxMessage.TraceContext NVARCHAR(500) NULL

Introduces TraceContextHelpers (Capture/Restore) using
OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator.
HTTP request span now propagates through the outbox → processor → consumer
chain, restoring visibility across the async boundary.

Adaptive polling: CompositeOutboxProcessor now polls every 200ms while
messages are available, backs off to 10s when idle. Reduces end-to-end
latency under load without increasing DB load at rest.

ActivitySource: YallaJo.Outbox (add to OTel tracing config in Program.cs)

Build: 0 errors. Tests: 135+ pass (new trace tests added).
Migrations: 13. Additive — nullable column, no data rewrite needed.
Non-breaking.

Refs: guide.md §5.18.4 + §5.18.5, outbox-hardening-implementation-plan.md PR 4
```

---

## 11. PR 5 — Explicit Status Column + Cleanup Index (P3)

**Branch**: `refactor/outbox-hardening-pr5-status`
**Risk**: 🟡 Medium — adds a new column with data migration (backfill from existing fields). Existing dispatcher/cleanup queries continue to work during and after the migration.
**Effort**: 2–3 hours.

### 11.1 Schema change — add `Status` column + cleanup index

**File 1** — `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessageStatus.cs`

```csharp
namespace YallaJo.SharedKernel.Infrastructure.Outbox;

public enum OutboxMessageStatus
{
    Pending    = 0, // not yet processed, not locked
    Processing = 1, // currently locked + being processed
    Processed  = 2, // successful dispatch
    Failed     = 3, // retryable failure
    Dead       = 4, // retry count >= max
}
```

**File 2** — Modify `OutboxMessage.cs`:

```csharp
public sealed class OutboxMessage
{
    // ... existing fields + TraceContext ...
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

    public void Lock(DateTime until)
    {
        LockedUntil = until;
        Status = OutboxMessageStatus.Processing;
    }

    public void MarkAsProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        Status = OutboxMessageStatus.Processed;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        Status = RetryCount >= OutboxProcessor<>.MaxRetryCount  // see note below
            ? OutboxMessageStatus.Dead
            : OutboxMessageStatus.Failed;
    }
}
```

> **Note**: the `MaxRetryCount` reference would need to be lifted from `OutboxProcessor<TContext>` (currently `internal const`) into a shared constant to avoid the generic type reference. Consider moving it to `OutboxMessage.MaxRetryCount = 10` or `OutboxConstants.MaxRetryCount = 10`.

### 11.2 EF Migrations — 13 new migrations

Each migration does three things:
1. Add `Status INT NOT NULL DEFAULT 0` column.
2. Backfill data:
   ```sql
   UPDATE [security].[OutboxMessages] SET Status =
     CASE
       WHEN ProcessedOnUtc IS NOT NULL     THEN 2  -- Processed
       WHEN RetryCount >= 10               THEN 4  -- Dead
       WHEN Error IS NOT NULL              THEN 3  -- Failed
       WHEN LockedUntil > GETUTCDATE()     THEN 1  -- Processing
       ELSE 0                                       -- Pending
     END;
   ```
3. Add filtered cleanup index:
   ```sql
   CREATE NONCLUSTERED INDEX IX_OutboxMessages_Cleanup
   ON [security].[OutboxMessages] (ProcessedOnUtc)
   WHERE Status = 2;  -- Processed
   ```

Example generated migration (Security):
```csharp
public partial class AddOutboxStatusColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Status",
            schema: "security",
            table: "OutboxMessages",
            type: "int",
            nullable: false,
            defaultValue: 0);

        // Backfill from existing fields
        migrationBuilder.Sql(@"
            UPDATE [security].[OutboxMessages] SET Status =
                CASE
                    WHEN ProcessedOnUtc IS NOT NULL   THEN 2
                    WHEN RetryCount >= 10             THEN 4
                    WHEN [Error] IS NOT NULL          THEN 3
                    WHEN LockedUntil > GETUTCDATE()   THEN 1
                    ELSE 0
                END;");

        migrationBuilder.Sql(@"
            CREATE NONCLUSTERED INDEX IX_OutboxMessages_Cleanup
            ON [security].[OutboxMessages] (ProcessedOnUtc)
            WHERE Status = 2;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IX_OutboxMessages_Cleanup ON [security].[OutboxMessages];");
        migrationBuilder.DropColumn("Status", "security", "OutboxMessages");
    }
}
```

### 11.3 Modify queries to use `Status`

**Dispatcher query** (OutboxProcessor):
```csharp
// Before:
.Where(m => m.ProcessedOnUtc == null
         && m.RetryCount < MaxRetryCount
         && (m.LockedUntil == null || m.LockedUntil < now))

// After (clearer with Status):
.Where(m => (m.Status == OutboxMessageStatus.Pending
          || m.Status == OutboxMessageStatus.Failed)
         && (m.LockedUntil == null || m.LockedUntil < now))
```

**Cleanup query** (OutboxCleaner):
```csharp
// Before:
.Where(m => m.ProcessedOnUtc != null
         && m.ProcessedOnUtc < cutoffUtc
         && m.RetryCount < OutboxProcessor<TContext>.MaxRetryCount)

// After (uses filtered index):
.Where(m => m.Status == OutboxMessageStatus.Processed
         && m.ProcessedOnUtc < cutoffUtc)
```

**Dead-letter health check query**:
```csharp
// Before: .CountAsync(m => m.RetryCount >= 10 && m.ProcessedOnUtc == null)
// After:  .CountAsync(m => m.Status == OutboxMessageStatus.Dead)
```

### 11.4 Tests

- Update `OutboxCleanerTests` to verify `Status`-based filtering.
- Update `OutboxProcessorTests` to verify `Status` transitions (`Pending → Processing → Processed`).
- Data migration test: insert rows with old-format values, run migration, verify `Status` is backfilled correctly for each of the 5 states.

### 11.5 Verification

- [ ] `dotnet build` → 0 errors.
- [ ] `dotnet ef database update` applies all 13 migrations cleanly.
- [ ] `dotnet test --nologo` → all tests pass.
- [ ] Sample query: `SELECT Status, COUNT(*) FROM security.OutboxMessages GROUP BY Status`. All historical rows should be classified correctly.
- [ ] `EXPLAIN`-equivalent on the cleanup query — confirm `IX_OutboxMessages_Cleanup` filtered index is used.
- [ ] Dispatcher + cleanup + health check all behave identically to before.

### 11.6 Commit

```
feat(outbox): add explicit Status column + filtered cleanup index (P3 hardening)

Schema change (13 migrations — one per module DbContext):
  Add OutboxMessage.Status INT NOT NULL DEFAULT 0
  Backfill existing rows from (ProcessedOnUtc, RetryCount, Error, LockedUntil)
  Create filtered index IX_OutboxMessages_Cleanup on (ProcessedOnUtc) WHERE Status = Processed

OutboxMessageStatus enum: Pending(0), Processing(1), Processed(2), Failed(3), Dead(4)

Benefits:
  - Self-documenting queries: WHERE Status = Processed instead of
    WHERE ProcessedOnUtc != null AND RetryCount < 10
  - Filtered index makes cleanup query O(rows-to-delete), not O(all-rows)
  - Dead-letter detection is now O(1) index lookup (WHERE Status = Dead)
  - OpsEndpoints.ListDeadLetters is faster and clearer

State machine enforced in OutboxMessage methods:
  Lock()           → Status = Processing
  MarkAsProcessed() → Status = Processed + clear Error
  MarkAsFailed()   → Status = Failed (or Dead when RetryCount >= Max)

Backward compat: old code using ProcessedOnUtc/RetryCount/LockedUntil
still works — Status is derived from these fields by the EF config.

Build: 0 errors. Tests: 135+ pass.
Migrations: 13. Additive column + backfill + filtered index.
Non-breaking.

Refs: guide.md §5.18.3 + §5.18.1, outbox-hardening-implementation-plan.md PR 5
```

---

## 12. Deferred Items

The §5.18.8 checklist has 10 items. 8 are covered by PRs 1–5. The remaining 2 are deferred until they become necessary:

### 12.1 Claim Check pattern for large payloads (§5.18.6)

**Why deferred**: current integration events are flat DTOs with < 1 KB payloads. The 64 KB threshold is nowhere near being hit. Premature implementation would add blob storage dependency + complexity for no benefit.

**When to implement**: when the first integration event payload exceeds 16 KB (25% of threshold), start the implementation. Triggers:
- A content-heavy event (e.g., `BlogPostPublishedIntegrationEvent` with full body)
- A file upload event with embedded thumbnail bytes
- A batch event with many nested items

**Cost when triggered**: ~4 hours. Add `IBlobStore` abstraction, implement `LocalBlobStore` (dev) + `AzureBlobStore` (prod), modify `OutboxMessage.Create` to offload when payload > 64 KB, modify consumer-side to detect `ClaimCheckReference` and fetch.

### 12.2 Per-handler execution ordering (§9.1 in guide)

**Why deferred**: current integration events have ≤ 3 handlers and none require ordering. The `[HandlerOrder(n)]` attribute pattern adds complexity without current benefit.

**When to implement**: when a consumer explicitly requires ordering (e.g., audit-log handler must run before downstream consumers so the audit record exists when they query for it). Triggers:
- A consumer relies on side effects of another consumer
- Ordering matters for regulatory compliance

**Cost when triggered**: ~2 hours. Add `[HandlerOrder]` attribute, modify `OutboxProcessor` handler enumeration to sort by attribute value.

---

## 13. Post-Migration Verification

After all 5 PRs merged to `main`:

### 13.1 Smoke tests

- [ ] End-to-end: register a user, confirm profile is created in Accounts module via outbox
- [ ] Trace: the request→command→outbox→processor→consumer chain shows as one trace in Jaeger/Tempo
- [ ] Metrics: `outbox.processed.total` counter is incrementing in Prometheus/OTLP exporter
- [ ] Cleanup: fake-age a processed row (`UPDATE ... SET ProcessedOnUtc = DATEADD(day, -40, GETUTCDATE())`), wait for next cleanup tick, confirm deletion
- [ ] Dead-letter: simulate a poison handler that always throws, confirm message reaches `Status = Dead` after 10 retries
- [ ] Dead-letter alert: `/health/outbox-dead-letters` returns 503 Degraded when dead-letters exist
- [ ] Replay: `POST /api/v1/ops/outbox/dead-letters/Security/{id}/replay` creates a clone with `Status = Pending`; original row preserved

### 13.2 Performance regression check

- [ ] Baseline: 10-second poll cycle, batch 20, no adaptive polling
- [ ] After PR 4: polling drops to 200ms while draining — verify this does NOT increase DB load at rest (should only kick in when messages exist)
- [ ] After PR 5: filtered `IX_OutboxMessages_Cleanup` index used by cleanup query — verify query plan

### 13.3 Documentation sync

- [ ] `Agents/agent-context.md §11.2` work log updated with commit SHAs + summary
- [ ] `Agents/guide.md §5.18.8` checklist items marked complete
- [ ] `Agents/error-log.md` updated with any mistakes encountered during implementation

---

## 14. Rollback Strategy

Each PR is revertable with `git revert <merge-commit>`. PRs 4 and 5 have schema migrations that should be rolled back via `dotnet ef database update {previous-migration-name}` (applied per module DbContext).

**Emergency rollback matrix**:

| PR | Revert action | Data impact |
|---|---|---|
| PR 1 | `git revert` → restart app | None — cleanup service just stops. Old rows remain. |
| PR 2 | `git revert` → restart app | None — `OutboxMessage.Create` falls back to AQN writes. New readers dual-read. |
| PR 3 | `git revert` → restart app | None — endpoints + metrics are additive. |
| PR 4 | `git revert` → `dotnet ef database update {pre-trace-context}` per module | Column dropped. Pre-revert rows lose their `TraceContext` values. |
| PR 5 | `git revert` → `dotnet ef database update {pre-status-column}` per module | Column + index dropped. Status is derivable from existing fields. |

**Non-destructive rollbacks**: PRs 1–3 can be reverted with zero data consequences. PRs 4–5 drop a column but the data they contained is either ephemeral (TraceContext) or derivable from other fields (Status).

---

## 15. Timeline

| Day | Work |
|---|---|
| Day 1 | Pre-work baseline + PR 1 (cleanup). Ship by EOD. |
| Day 2 | PR 1 review cycle. Start PR 2 (type registry). |
| Day 3 | Ship PR 2. Start PR 3 (ops + metrics). |
| Day 4 | Ship PR 3. Start PR 4 (tracing + adaptive). |
| Day 5 | Ship PR 4. Start PR 5 (status column). |
| Day 6 | Ship PR 5. Post-migration verification. |
| Day 7 | Documentation updates + final smoke tests. |

**Total**: 5–7 working days for careful, review-driven execution. **3 days** if working solo with self-review only.

---

## 16. Success Criteria

1. ✅ Outbox tables no longer grow unbounded — cleanup deletes processed rows older than 30 days
2. ✅ Renaming an integration event type does not break outstanding outbox rows
3. ✅ Dead-lettered messages trigger a health check Degraded status within one check interval
4. ✅ Ops can replay a dead-lettered message via `/api/v1/ops/outbox/dead-letters/{module}/{id}/replay` (with `Permission.Outbox.Replay`)
5. ✅ OpenTelemetry metrics expose backlog, latency, retry distribution, per-handler success rate
6. ✅ W3C trace context propagates from HTTP request → outbox → consumer in a single trace
7. ✅ Adaptive polling achieves sub-second dispatch latency under load without increasing DB load at rest
8. ✅ Explicit `Status` column makes ad-hoc SQL queries self-documenting
9. ✅ Every PR is revertable in isolation with no data loss (PRs 1–3) or reversible via migration (PRs 4–5)
10. ✅ All changes are documented in `agent-context.md §11.2` work log with commit SHAs

---

## Appendix A — Files Summary

### Files created across all 5 PRs

```
PR 1:
  YallaJo.SharedKernel.Infrastructure/Outbox/OutboxCleanupOptions.cs
  YallaJo.SharedKernel.Infrastructure/Outbox/IOutboxCleaner.cs
  YallaJo.SharedKernel.Infrastructure/Outbox/OutboxCleaner.cs
  YallaJo.SharedKernel.Infrastructure/BackgroundJobs/OutboxCleanupBackgroundService.cs
  tests/SharedKernel.Tests.Unit/OutboxCleanerTests.cs

PR 2:
  YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs
  tests/SharedKernel.Tests.Unit/IntegrationEventTypeRegistryTests.cs

PR 3:
  YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMetrics.cs
  YallaJo.Api/HealthChecks/OutboxDeadLetterHealthCheck.cs
  YallaJo.SharedKernel.Application/Abstractions/Authorization/OpsFeatures.cs
  YallaJo.SharedKernel.Application/Commands/Outbox/ReplayDeadLetterCommand.cs
  YallaJo.SharedKernel.Application/Commands/Outbox/ListDeadLettersQuery.cs
  YallaJo.SharedKernel.Infrastructure/Handlers/ReplayDeadLetterCommandHandler.cs
  YallaJo.SharedKernel.Infrastructure/Handlers/ListDeadLettersQueryHandler.cs
  YallaJo.Api/Endpoints/OpsEndpoints.cs

PR 4:
  YallaJo.SharedKernel.Infrastructure/Outbox/TraceContextHelpers.cs
  YallaJo.SharedKernel.Infrastructure/Outbox/OutboxActivitySource.cs
  13 EF migrations: AddOutboxTraceContext (per module)

PR 5:
  YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessageStatus.cs
  13 EF migrations: AddOutboxStatusColumn (per module)
```

### Files modified (all PRs combined)

```
YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessage.cs       (PR 2, 4, 5)
YallaJo.SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs  (PR 2, 3, 4, 5)
YallaJo.SharedKernel.Infrastructure/BackgroundJobs/CompositeOutboxProcessor.cs  (PR 4)
YallaJo.SharedKernel.Infrastructure/DependencyInjection.cs        (PR 1)
YallaJo.Api/Program.cs                                             (PR 3, 4)
YallaJo.Api/appsettings.json + appsettings.Development.json       (PR 1)
Security.Infrastructure/Seeding/SecurityPermissionCatalog.cs      (PR 3)
13 {Module}.Infrastructure/DependencyInjection.cs                  (PR 1)
13 {Module}.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs  (PR 4, 5)
```

**Total files touched**: ~50 (creations + modifications across 5 PRs)

---

## Appendix B — Decision Log

| Decision | Choice | Alternative Considered | Reason |
|---|---|---|---|
| Cleanup: delete or archive? | Delete | Move to `OutboxMessagesArchive` table | Archive doubles storage. Audit retention is sufficient at 30 days. |
| Type registry: dynamic scan or explicit list? | Explicit static dictionary | `[IntegrationEvent("name")]` attribute + reflection | Explicit list is searchable, testable, deterministic. Reflection hides registration at runtime. |
| Status column backfill: online or offline? | Online (during migration) | Nullable column + lazy backfill | Migration runs once at deploy — acceptable cost. Lazy backfill complicates queries. |
| Claim Check: implement now or defer? | Defer | Implement proactively | Premature. Current payloads are < 1 KB. YAGNI. |
| Handler ordering: implement now or defer? | Defer | `[HandlerOrder]` attribute | No consumer currently requires ordering. YAGNI. |
| Adaptive polling: 200ms or 500ms draining? | 200ms | 500ms | 200ms matches Milan Jovanović's scaling benchmarks. DB load is negligible at this rate. |

---

**End of Plan.**
