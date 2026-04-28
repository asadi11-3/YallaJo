# YallaJo — Recommendations Engine Specification (V1)
## Lives inside `Analytics` module · Per `YallaJo.md` Phase 4

> **Architecture decision (locked from gap analysis)**: The recommendation engine is built **inside the existing `Analytics` module**, NOT as a new dedicated module. The `Analytics` domain entities are already scaffolded (`UserInteraction`, `RecommendationCache`, `PopularityScore`, `UserPreference`, `UserPreferredCategory`). Reusing them avoids duplicate parallel structures and matches the YallaJo.md product spec (lines 971-972: `/api/analytics/recommendations`).
>
> **Cross-module data access pattern**: NO cross-context EF reads. Use `analytics.EntityAttributeSnapshots` denormalized read model populated by integration event handlers — the canonical YallaJo pattern (mirrors how `Place.TourCount` is denormalized).
>
> **Effort**: V1 = **42 hrs** (Recommendations team). External team work: ~16 hrs across Booking/Finance/Accounts/ContentPlaces/ContentCore.

---

## 0. Phasing Summary

```
V1   — Content + Popularity (ship now). 42 hrs Analytics team.
V1.5 — Quality hardening + admin marketplace. 17 hrs (was 21; halal blocked-on-ContentPlaces).
V2   — Personalization. 22 hrs (was 25; Booking signals deferred).
V2.5 — Jordan-specific intelligence. 33 hrs.
V3   — Editorial + sponsored + email digest + ops. 53 hrs (was 77; push deferred to V4).
V4   — Push notification targeting (deferred until Messaging push infra ships).
```

Details in `RECOMMENDATIONS_ENGINE_ROADMAP.md`.

---

## 1. Module Anatomy — Inside Analytics

```
Analytics.Domain/
  Entities/
    UserInteraction.cs              ← EXISTS (reuse). Tracks views/clicks/bookings/favorites/etc.
    RecommendationCache.cs          ← EXISTS (reuse as the per-user suggestion store)
    PopularityScore.cs              ← EXISTS (reuse for trending boost)
    UserPreference.cs               ← EXISTS (reuse as user preference profile root)
    UserPreferredCategory.cs        ← EXISTS (reuse for category weights)
    AuditLog.cs                     ← EXISTS (unrelated to engine — pre-existing)
    SuggestionBatch.cs              ← NEW — groups RecommendationCache rows by (Source, Context)
    EntityAttributeSnapshot.cs      ← NEW — denormalized read model (see §3)
  Enums/
    InteractionType.cs              ← EXISTS — extend with NotInterested in V2
    SuggestionContext.cs            ← NEW — see §2.2
    EntityKind.cs                   ← NEW — Tour|Business|Place
  ValueObjects/
    EntityRef.cs                    ← NEW — typed cross-entity pointer
  Repositories/
    ISuggestionBatchRepository.cs   ← NEW
    IUserInteractionRepository.cs   ← NEW (entity already exists; just need repo abstraction)
    IUserPreferenceRepository.cs    ← NEW
    IEntityAttributeSnapshotRepository.cs ← NEW

Analytics.Application/
  Commands/
    RecordInteraction/              ← NEW. Public endpoint for view/click/dwell/share signals.
    RefreshSuggestionBatch/         ← NEW. Triggered by background job per (source, context).
    UpdateUserPreference/           ← NEW. Background job aggregates interactions → profile (V2).
    SetUserPreferences/             ← NEW. Manual user preference setter (per YallaJo.md /preferences endpoint).
  Queries/
    GetRecommendations/             ← NEW. GET /api/v1/analytics/recommendations
    GetSimilarTours/                ← NEW. GET /api/v1/analytics/recommendations/similar/{tourId}
    GetUserPreferences/             ← NEW.
  Scoring/
    V1ContentSimilarityScorer.cs    ← V1 scoring engine (content + popularity + proximity)
    V2PersonalizationScorer.cs      ← V2 add-on (user preference vector blending)
  Caching/
    AnalyticsCacheKeys.cs           ← NEW — `ct:analytics:recs:*` namespace

Analytics.Infrastructure/
  Persistence/
    AnalyticsDbContext.cs           ← EXISTS (extend with new DbSets)
    Configurations/
      EntityAttributeSnapshotConfiguration.cs  ← NEW
      SuggestionBatchConfiguration.cs          ← NEW
      (other entity configs already exist)
  Repositories/                     ← NEW EF implementations
  BackgroundJobs/
    SuggestionBatchRefreshJob.cs    ← NEW. BackgroundService, runs every 6h + on startup.
    UserProfileUpdateJob.cs         ← NEW (V2). Aggregates interactions → UserPreference.
  EventHandlers/
    TourCreatedIntegrationEventHandler.cs           ← NEW — upserts EntityAttributeSnapshot
    TourUpdatedIntegrationEventHandler.cs           ← NEW
    TourDeletedIntegrationEventHandler.cs           ← NEW
    BusinessCreatedIntegrationEventHandler.cs       ← NEW
    BusinessUpdatedIntegrationEventHandler.cs       ← NEW
    BusinessDeletedIntegrationEventHandler.cs       ← NEW
    LanguageActivatedIntegrationEventHandler.cs     ← NEW (snapshots multilingual)

Analytics.Contracts/
  IntegrationEvents/
    SuggestionBatchRefreshedIntegrationEvent.cs    ← NEW (optional consumer notification)
  Authorization/
    AnalyticsFeatures.cs             ← NEW — Recommendations, Preference, Interaction, Batch
    AnalyticsPermissionCatalog.cs    ← NEW

Analytics.Presentation/
  AnalyticsEndpoints.cs              ← EXISTS (extend with recommendation endpoints)
  Endpoints/Recommendations/
    RecommendationsEndpoints.cs      ← NEW
  Endpoints/Preferences/
    PreferencesEndpoints.cs          ← NEW
```

---

## 2. New Domain Concepts

### 2.1 `EntityRef` (Value Object)

```csharp
// Analytics.Domain/ValueObjects/EntityRef.cs
public sealed record EntityRef(EntityKind Kind, Guid Id)
{
    public static EntityRef Tour(Guid id)     => new(EntityKind.Tour, id);
    public static EntityRef Business(Guid id) => new(EntityKind.Business, id);
    public static EntityRef Hotel(Guid id)    => new(EntityKind.Business, id); // Hotel = BusinessType.Hotel
    public static EntityRef Place(Guid id)    => new(EntityKind.Place, id);
}

public enum EntityKind : byte
{
    Tour     = 1,
    Business = 2,   // includes Hotels (BusinessType.Hotel)
    Place    = 3
}
```

### 2.2 `SuggestionContext` Enum

```csharp
public enum SuggestionContext : byte
{
    SimilarTours        = 1,    // Tour detail → similar tours
    SimilarBusinesses   = 2,
    SimilarHotels       = 3,
    AddAMeal            = 10,   // Tour detail → restaurants nearby
    AddAnActivity       = 11,   // Hotel detail → tours nearby
    WhereToStay         = 12,   // Tour detail → hotels nearby
    ExploreNearby       = 13,   // Place detail → all entities nearby
    PersonalizedFeed    = 20,   // V2 — homepage feed
    BecauseYouViewed    = 21,
    BecauseYouBooked    = 22,
    PostBookingAddOn    = 30,
    PostBookingFollowUp = 31,
}
```

### 2.3 `SuggestionBatch` (Aggregate)

Wrapper around `RecommendationCache` rows for one `(Source, Context)` pair. Lets us mark batches stale without scanning individual rows.

```csharp
public sealed class SuggestionBatch : AuditableEntity, IAggregateRoot
{
    private SuggestionBatch() { } // EF

    public EntityKind SourceKind     { get; private set; }
    public Guid       SourceId       { get; private set; }
    public SuggestionContext Context { get; private set; }
    public string AlgorithmVersion   { get; private set; } = string.Empty;
    public DateTime ComputedAt       { get; private set; }
    public bool IsStale              { get; private set; }
    public int ItemCount             { get; private set; }

    public static SuggestionBatch Create(
        EntityKind sourceKind, Guid sourceId,
        SuggestionContext context, string algorithmVersion, int itemCount)
    {
        return new SuggestionBatch
        {
            SourceKind       = sourceKind,
            SourceId         = sourceId,
            Context          = context,
            AlgorithmVersion = algorithmVersion,
            ComputedAt       = DateTime.UtcNow,
            IsStale          = false,
            ItemCount        = itemCount,
        };
    }

    public void MarkStale() => IsStale = true;
    public void MarkRefreshed(string algorithmVersion, int itemCount)
    {
        AlgorithmVersion = algorithmVersion;
        ItemCount        = itemCount;
        ComputedAt       = DateTime.UtcNow;
        IsStale          = false;
    }
}
```

The actual suggestion rows live in **existing `RecommendationCache`** entity (one row per `(UserId or null, EntityType, EntityId)` — extended below):

```csharp
// EXISTING — Analytics.Domain.Entities.RecommendationCache
public sealed class RecommendationCache : BaseEntity
{
    public Guid? UserId { get; private set; }            // ← made nullable for shared anonymous batches
    public Guid BatchId { get; private set; }            // ← NEW field — links to SuggestionBatch
    public string EntityType { get; private set; } = string.Empty;  // already exists
    public Guid EntityId { get; private set; }
    public decimal Score { get; private set; }
    public int Position { get; private set; }            // ← NEW field — 1-based rank within batch
    public string? Reason { get; private set; }          // already exists — reuse for Signals JSON
    public DateTime GeneratedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
}
```

Add fields via migration: `BatchId Guid NOT NULL`, `Position int NOT NULL`. Make `UserId` nullable so anonymous batches share rows across all anonymous viewers.

### 2.4 `UserInteraction` — Reuse Existing Entity

Already exists exactly as spec needs. **No changes needed for V1.**

```csharp
// EXISTING — Analytics.Domain.Entities.UserInteraction (do not redeclare)
public sealed class UserInteraction : BaseEntity<long>
{
    public Guid UserId { get; private set; }
    public InteractionType InteractionType { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public Location? Location { get; private set; }
    public string? SessionId { get; private set; }
    public string? DeviceType { get; private set; }
    public int? DurationSeconds { get; private set; }
    public DateTime OccurredAt { get; private set; }
}
```

V2 extension: add `NotInterested = 7` value to `InteractionType` enum (currently 0–6: View, Click, Share, Bookmark, Search, Review, Booking).

### 2.5 `UserPreference` — Reuse Existing Entity

Already exists. V2 enriches it with computed fields populated by `UserProfileUpdateJob`.

---

## 3. `EntityAttributeSnapshot` — The Critical Read Model (NEW)

> **Why this exists**: Recommendations cannot read directly from `ContentToursDbContext` or `ContentPlacesDbContext` — Clean Architecture rule (gotcha #22 in `agent-context.md`). Instead, we maintain a denormalized snapshot inside `analytics` schema, kept in sync via integration event handlers. This mirrors how `Place.TourCount` is denormalized today.

### 3.1 Schema

```sql
CREATE TABLE analytics.EntityAttributeSnapshots (
    Id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    EntityKind        TINYINT NOT NULL,                    -- 1=Tour, 2=Business, 3=Place
    EntityId          UNIQUEIDENTIFIER NOT NULL,
    Name              NVARCHAR(300) NOT NULL,
    Slug              VARCHAR(300) NOT NULL,
    BasePriceAmount   DECIMAL(19,4) NULL,                  -- null for Place
    BasePriceCurrency CHAR(3) NULL,
    SalePrice         DECIMAL(19,4) NULL,
    AverageRating     DECIMAL(4,2) NOT NULL DEFAULT 0,
    ReviewCount       INT NOT NULL DEFAULT 0,
    BookingCount      INT NOT NULL DEFAULT 0,
    IsFeatured        BIT NOT NULL DEFAULT 0,
    LocationLatitude  DECIMAL(9,6) NULL,                   -- mirrors Location.Latitude (decimal)
    LocationLongitude DECIMAL(9,6) NULL,
    PlaceId           UNIQUEIDENTIFIER NULL,               -- for Tours / Businesses
    BusinessType      TINYINT NULL,                        -- for Business: Restaurant=0, Hotel=1, etc.
    Difficulty        TINYINT NULL,                        -- for Tours
    DurationMinutes   INT NULL,                            -- for Tours
    IsChildFriendly   BIT NULL,
    IsAccessible      BIT NULL,
    IsInstantBooking  BIT NULL,
    Status            TINYINT NULL,                        -- TourStatus / BusinessStatus
    IsDeleted         BIT NOT NULL DEFAULT 0,
    -- denormalized category links (avoids cross-context join)
    CategoryIdsJson   NVARCHAR(500) NULL,                  -- JSON array of category GUIDs
    -- timestamps
    LastUpdatedAt     DATETIME2 NOT NULL,
    CreatedAt         DATETIME2 NOT NULL,                  -- mirrors source entity's CreatedAt (for recency)
    -- indexes
    INDEX IX_EntityAttributeSnapshots_Kind_Entity UNIQUE (EntityKind, EntityId),
    INDEX IX_EntityAttributeSnapshots_Location (LocationLatitude, LocationLongitude),
    INDEX IX_EntityAttributeSnapshots_Status_Featured (Status, IsFeatured) WHERE IsDeleted = 0
);
```

### 3.2 Population — Integration Event Handlers

| Event source | Subscriber handler | Action |
|---|---|---|
| `ContentTours: TourCreatedIntegrationEvent` | `TourCreatedIntegrationEventHandler` | `INSERT EntityAttributeSnapshot` |
| `ContentTours: TourUpdatedIntegrationEvent` | `TourUpdatedIntegrationEventHandler` | `UPDATE` matching snapshot |
| `ContentTours: TourDeletedIntegrationEvent` | `TourDeletedIntegrationEventHandler` | `UPDATE … SET IsDeleted = 1` |
| `ContentPlaces: BusinessCreatedIntegrationEvent` | `BusinessCreatedIntegrationEventHandler` | INSERT |
| `ContentPlaces: BusinessUpdatedIntegrationEvent` | `BusinessUpdatedIntegrationEventHandler` | UPDATE |
| `ContentPlaces: BusinessDeletedIntegrationEvent` | `BusinessDeletedIntegrationEventHandler` | soft-delete |
| `ContentPlaces: PlaceCreatedIntegrationEvent` | `PlaceCreatedIntegrationEventHandler` | INSERT |
| `ContentPlaces: PlaceUpdatedIntegrationEvent` | `PlaceUpdatedIntegrationEventHandler` | UPDATE |
| `ContentPlaces: PlaceDeletedIntegrationEvent` | `PlaceDeletedIntegrationEventHandler` | soft-delete |

All these events ARE already published (verified — `IntegrationEventTypeRegistry` has `content-tours.*` and `content-places.*` entries).

For categories, a fallback bootstrap is fine in V1: the `SuggestionBatchRefreshJob` rebuilds `CategoryIdsJson` from `ContentCore.EntityCategories` every 6h. ContentCore can later add `EntityCategoryAssignedIntegrationEvent` / `EntityCategoryRemovedIntegrationEvent` for real-time updates (not blocking).

> **Note for the V1 ship**: cross-context bootstrap query (read `ContentCore.EntityCategories` at job start) is technically a cross-context read. It's acceptable here because (a) it's the background job, not a request handler; (b) it runs in Infrastructure layer, not Application; (c) it produces denormalized data inside our own schema. Read-only, no events, accepted by Clean Architecture rules.

### 3.3 First-time bootstrap

On first deployment, the `SuggestionBatchRefreshJob` runs a full bootstrap on startup:
1. Read all active Tours from `ContentToursDbContext` → upsert snapshots
2. Read all active Businesses from `ContentPlacesDbContext` → upsert snapshots
3. Read all Places → upsert snapshots
4. From there on, integration event handlers keep snapshots fresh

This is the ONLY place cross-context reads are tolerated, and only at startup.

---

## 4. Scoring — V1 Algorithm

All inputs come from `EntityAttributeSnapshots` (no cross-context reads at request time).

### 4.1 Score Formula (V1)

```
Score(source, candidate) =
    W_category   × CategoryMatch(source, candidate)
  + W_price      × PriceSimilarity(source, candidate)
  + W_proximity  × ProximityScore(source, candidate)
  + W_rating     × NormalizedRating(candidate)
  + W_popularity × Log10(candidate.BookingCount + 1) / 4
  + W_featured   × (candidate.IsFeatured ? 0.1 : 0)
```

### 4.2 ProximityScore — Use existing `Location.DistanceTo()`

`Location` value object in SharedKernel already implements Haversine via `DistanceTo()`. Reuse it.

```csharp
// In V1ContentSimilarityScorer:
var sourceLocation    = new Location(source.LocationLatitude!.Value, source.LocationLongitude!.Value);
var candidateLocation = new Location(candidate.LocationLatitude!.Value, candidate.LocationLongitude!.Value);

double distKm = sourceLocation.DistanceTo(candidateLocation);
decimal proximity = 1m / (1m + (decimal)(distKm / 10.0));
```

If either side has null coordinates, treat proximity as 0 and don't apply the proximity weight.

**Hard cutoff**: candidates > 100 km excluded (configurable per context).

### 4.3 CategoryMatch — Read from snapshot

```csharp
// CategoryIdsJson is a JSON array of GUIDs, parsed once when loading the snapshot
var sharedCategories = source.CategoryIds.Intersect(candidate.CategoryIds).Count();
return sharedCategories switch
{
    > 0 => 1.0m,    // ≥1 shared category
    _   => 0.0m
};
```

Parent-category match (0.5) deferred to V1.5 when ContentCore exposes parent links.

### 4.4 Default Weights per Context

| Weight | SimilarTours | AddAMeal | WhereToStay | SimilarBusinesses | ExploreNearby |
|---|---|---|---|---|---|
| W_category | 0.30 | 0.00 | 0.00 | 0.30 | 0.00 |
| W_price | 0.20 | 0.10 | 0.20 | 0.15 | 0.00 |
| W_proximity | 0.15 | 0.40 | 0.45 | 0.30 | 0.40 |
| W_rating | 0.20 | 0.25 | 0.20 | 0.15 | 0.25 |
| W_popularity | 0.10 | 0.20 | 0.10 | 0.05 | 0.30 |
| W_featured | 0.05 | 0.05 | 0.05 | 0.05 | 0.05 |
| **Sum** | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |

Loaded from `analytics.SuggestionContextWeights` table at startup, cached 1h, hot-reloadable by admin.

### 4.5 Diversity & Anti-Cannibalization

After scoring, apply rules in order:
1. Exclude `IsDeleted = 1` and `Status != Approved/Published`
2. Exclude same-provider tours when source is a Tour (anti-cannibalization)
3. Take top 60 by score
4. Apply diversity caps:
   - ≤ 3 items per `PlaceId`
   - ≤ 2 items at same price ±5%
   - ≤ 2 items per provider (`CreatedByUserId`)
5. Take top 20 → store as batch

V1.5 will replace this greedy approach with proper MMR.

---

## 5. Background Jobs

### 5.1 `SuggestionBatchRefreshJob` (BackgroundService)

```csharp
internal sealed class SuggestionBatchRefreshJob(
    IServiceProvider serviceProvider,
    ILogger<SuggestionBatchRefreshJob> logger) : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(StartupDelay, ct);

        // First run on startup — full bootstrap of snapshots + all batches
        await RunBootstrapAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunIncrementalRefreshAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "SuggestionBatchRefreshJob iteration failed");
            }

            try { await Task.Delay(RefreshInterval, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunBootstrapAsync(CancellationToken ct) { /* full snapshot rebuild */ }

    private async Task RunIncrementalRefreshAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        // Priority 1: refresh stale batches (entities mutated since last run)
        // Priority 2: top-N most-viewed batches
        // Priority 3: round-robin remaining batches
    }
}
```

Registered in `Analytics.Infrastructure.DependencyInjection.cs`:
```csharp
services.AddHostedService<SuggestionBatchRefreshJob>();
```

### 5.2 Stale-Marking via Integration Events

Already part of the snapshot-update handlers (§3.2). When a snapshot updates, find all `SuggestionBatch` rows where this entity appears as `Source` OR within candidate radius — mark stale. Next refresh cycle picks them up first.

---

## 6. API Endpoints (per YallaJo.md)

### 6.1 Endpoint Table

| # | Method | Route | Auth | Purpose |
|---|--------|-------|------|---------|
| R1 | GET | `/api/v1/analytics/recommendations` | `Analytics.Recommendation.Read` | Personalized feed for current user |
| R2 | GET | `/api/v1/analytics/recommendations/similar/{tourId:guid}` | Anonymous | Similar tours (content-based) |
| R3 | GET | `/api/v1/analytics/recommendations/for/{kind}/{id:guid}` | Anonymous | Suggestions for any entity (extends spec for Business/Hotel/Place) |
| R4 | POST | `/api/v1/analytics/interactions` | `Analytics.Interaction.Record` | Record user interaction (anonymous-allowed via `UserId=null`) |
| R5 | GET | `/api/v1/analytics/preferences` | `Analytics.Preference.Read` | Read user preferences |
| R6 | PUT | `/api/v1/analytics/preferences` | `Analytics.Preference.Update` | Update user preferences |
| R7 | GET | `/api/v1/analytics/admin/batches` | `Analytics.Batch.Read` | Admin: batch health |
| R8 | POST | `/api/v1/analytics/admin/batches/refresh` | `Analytics.Batch.Refresh` | Admin: trigger manual refresh |

### 6.2 R1 — Personalized Recommendations

V1 behavior: returns top 20 by `(BookingCount DESC, AverageRating DESC)` filtered by user's preferred categories (if set in `UserPreferredCategory`). Excludes already-booked tours.

V2 behavior: full user-preference vector blending (§7).

### 6.3 R3 — Generic Entity Suggestions

```
GET /api/v1/analytics/recommendations/for/Tour/{id}?context=SimilarTours&limit=10&lang=en

{
  "sourceKind": "Tour",
  "sourceId": "abc...",
  "computedAt": "2026-04-26T18:00:00Z",
  "isPersonalized": false,
  "suggestions": [
    {
      "context": "SimilarTours",
      "items": [
        {
          "kind": "Tour", "id": "...", "name": "Petra Full Day", "slug": "...",
          "thumbnailUrl": null, "basePrice": 45.00, "currency": "JOD",
          "averageRating": 4.8, "bookingCount": 312, "score": 0.87,
          "signals": ["category-match","nearby","high-rating"],
          "isFeatured": false
        }
      ]
    }
  ]
}
```

`thumbnailUrl` enriched at response time via separate snapshot lookup (or returns null in V1; ContentCore attachment join is deferred).

### 6.4 R4 — Interaction Recording

```
POST /api/v1/analytics/interactions
{
  "entityKind": "Tour",
  "entityId": "abc...",
  "interactionType": "View",
  "sessionId": "sess_xyz",
  "dwellSeconds": null
}
```

Anonymous allowed (`UserId = null`). Rate-limited 100/min per IP. Returns 202 Accepted.

---

## 7. V2 Personalization (deferred until ≥10k UserInteraction events)

Builds on existing `UserPreference` + `UserPreferredCategory` entities. New background job `UserProfileUpdateJob` aggregates interactions into a weighted preference vector. Score blending:

```
FinalScore_v2 = 0.6 × V1Score + 0.4 × PersonalizationBoost
```

`PersonalizationBoost` measures how well a candidate matches the user's preference vector (category overlap, price tier match, location centroid, entity-kind affinity).

Manual user preferences (`PUT /preferences`) feed the same vector with higher weight than implicit signals.

---

## 8. Caching

| Query | Key | TTL | Tags |
|---|---|---|---|
| `GetRecommendations` (R1) | `ct:analytics:recs:user:{userId}:lang:{lang}` | 5 min | `analytics:recs:{userId}` |
| `GetSimilarTours` (R2) | `ct:analytics:recs:similar:{tourId}:lang:{lang}` | 30 min | `analytics:recs:tour:{tourId}` |
| `GetEntitySuggestions` (R3) | `ct:analytics:recs:{kind}:{id}:ctx:{context}:lang:{lang}` | 30 min | `analytics:recs:{kind}:{id}` |
| `GetUserPreferences` (R5) | `ct:analytics:prefs:user:{userId}` | 60 min | `analytics:prefs:{userId}` |

Invalidation:
- Snapshot updated → bust `analytics:recs:{kind}:{id}` and `analytics:recs:tour:{id}` etc.
- User preference updated → bust `analytics:prefs:{userId}` + `analytics:recs:{userId}`
- Batch refreshed → bust all `analytics:recs:*` tags for refreshed batch's source.

---

## 9. Permissions

```csharp
// Analytics.Contracts/Authorization/AnalyticsFeatures.cs
public static class AnalyticsFeatures
{
    public const string Recommendation = nameof(Recommendation);
    public const string Preference     = nameof(Preference);
    public const string Interaction    = nameof(Interaction);
    public const string Batch          = nameof(Batch);
}
```

```csharp
// Permissions to register in AnalyticsPermissionCatalog:
// Analytics.Recommendation.Read    — authenticated users (personalized feed)
// Analytics.Preference.Read        — authenticated users (own preferences)
// Analytics.Preference.Update      — authenticated users (own preferences)
// Analytics.Interaction.Record     — authenticated users (record signals)
// Analytics.Batch.Read             — Admin only
// Analytics.Batch.Refresh          — Admin only
```

`AppAction.Refresh` and `AppAction.Record` were added to SharedKernel as part of the engine spec — see `YallaJo.SharedKernel.Application/Authorization/AppAction.cs`.

R3 (`/recommendations/for/{kind}/{id}`) is `.AllowAnonymous()` — returns popularity-only for anonymous users, personalized when authenticated.
R4 (`/interactions`) accepts anonymous interaction records (UserId=null) — endpoint requires only basic rate-limit; permission optional.

---

## 10. Module Registration

```csharp
// Analytics.Application/DependencyInjection.cs (already exists — extend)
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
services.AddSingleton<IRecommendationScoringEngine, V1ContentSimilarityScorer>();
```

```csharp
// Analytics.Infrastructure/DependencyInjection.cs (already exists — extend)
services.AddScoped<ISuggestionBatchRepository, SuggestionBatchRepository>();
services.AddScoped<IUserInteractionRepository, UserInteractionRepository>();
services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();
services.AddScoped<IEntityAttributeSnapshotRepository, EntityAttributeSnapshotRepository>();
services.AddSingleton<IPermissionCatalog, AnalyticsPermissionCatalog>();
services.AddHostedService<SuggestionBatchRefreshJob>();
```

`YallaJo.Api/Program.cs` already calls `AddAnalyticsApplication()` and `AddAnalyticsInfrastructure(...)` — no changes needed there.

---

## 11. V1 Implementation WBS (revised — 42 hrs)

| # | Deliverable | Est. hrs |
|---|---|---|
| 1.1 | New domain types: `EntityRef`, `SuggestionContext`, `EntityKind`, `SuggestionBatch` | 3 |
| 1.2 | `EntityAttributeSnapshot` entity + EF config + migration | 3 |
| 1.3 | Add `BatchId` + `Position` to `RecommendationCache` (migration + entity update) | 1 |
| 1.4 | Repository interfaces + EF implementations (4 repos) | 3 |
| 1.5 | 9 integration event handlers for snapshot population (§3.2) | 6 |
| 1.6 | `V1ContentSimilarityScorer` using `Location.DistanceTo()` | 5 |
| 1.7 | `RefreshSuggestionBatchCommandHandler` + diversity rules | 4 |
| 1.8 | `SuggestionBatchRefreshJob` (BackgroundService + bootstrap) | 4 |
| 1.9 | `RecordInteractionCommandHandler` + validator | 2 |
| 1.10 | `GetRecommendations` (R1) + `GetSimilarTours` (R2) + `GetEntitySuggestions` (R3) handlers | 5 |
| 1.11 | `AnalyticsPermissionCatalog` + `AnalyticsFeatures` | 1 |
| 1.12 | 8 endpoints in `Analytics.Presentation` | 3 |
| 1.13 | Smoke test + PR (verify outbox events flow → snapshots populated → recs computed) | 2 |
| **V1 TOTAL** | | **42 hrs** |

---

## 12. Architecture Decisions (Locked from Gap Analysis)

| # | Decision | Status |
|---|---|---|
| Module placement | Inside `Analytics` module — reuses 5 existing entities, matches YallaJo.md Phase 4 | ✅ |
| Cross-module data access | `EntityAttributeSnapshots` denormalized read model, populated by integration events | ✅ |
| Location handling | Use `Location` value object + existing `Location.DistanceTo()` Haversine method | ✅ |
| Booking signals (V1) | Defer — Booking module has no integration events yet. Use `BookingCount` field as popularity proxy. | ✅ |
| `AppAction.Refresh` + `AppAction.Record` | Added to `YallaJo.SharedKernel.Application/Authorization/AppAction.cs` | ✅ |
| Subscription gate (V1.5+) | Blocked-on-Finance — needs `ISubscriptionStatusProvider` contract (~4 hrs Finance work) | 🟠 Pending |
| Halal filter (V1.5+) | Blocked-on-ContentPlaces — needs `IsHalal/HasVegetarianOptions/HasAlcoholFreeArea` columns | 🟠 Pending |
| Marketing consent (V3+) | Blocked-on-Accounts — needs `MarketingConsent` value object on Profile | 🟠 Pending |
| Push notifications (V3.7) | Deferred to V4 — Messaging push infra (FCM/APNS) does not exist | ⏭ Deferred |
| Favorite signals (V2+) | Blocked-on-Social — needs `FavoriteAddedIntegrationEvent` published from Social module | 🟠 Pending |
| Category match (V1) | Bootstrap from `ContentCore.EntityCategories` cross-context read at job startup; real-time updates require future ContentCore events | ✅ Pragmatic |

---

*This spec was rewritten after a 6-agent codebase verification surfaced 11 architectural gaps. Every "exists" claim has been verified. See `RECOMMENDATIONS_ENGINE_GAPS.md` for the full audit. See `RECOMMENDATIONS_ENGINE_ROADMAP.md` for V1.5+ phases.*
