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
