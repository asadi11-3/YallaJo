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
