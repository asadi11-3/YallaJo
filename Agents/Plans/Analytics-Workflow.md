# Analytics Module — Workflow Plan

> **Created**: 2025-07-14
> **Compatible with**: TourGuide-Flow.md, Booking-Workflow.md, Finance-Workflow.md, Social-Workflow.md, Messaging-Workflow.md, Platform-Onboarding-Workflow.md, Role-System.md, BlogCreatorPost-Merger.md, ContentPlaces-Workflow.md
> **Source of truth**: agent-context.md, YallaJo Business Rules PDF, YallaJo.md, Endpoints.pdf

---

## Design Decisions (14 items — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | Require auth for all POST | Remove AllowAnonymous from 3 POST endpoints (interactions, sponsored-click, metrics). Only authenticated users tracked. No anonymous personalization. |
| 2 | Full collaborative filtering | Build complete item-item collaborative scorer (ALS/co-occurrence matrix). Spec weight: 40% collaborative + 35% content + 25% popularity. |
| 3 | Nightly matrix recomputation | Background service rebuilds interaction matrix daily at 03:00 UTC. |
| 4 | Keep integration event handlers | No stream processor. Add new handlers for TourGuide/Booking events. Current inbox-deduplicated pattern is proven. |
| 5 | Dedicated guide dashboard | GET /dashboard/guide — full analytics: completed tours, avg rating, booking rate, slot utilization, top tours, revenue trend, tourist demographics, repeat customer rate. |
| 6 | Keep experiments as-is | Fix CQRS bypass only. No new experiment features. |
| 7 | Keep sponsored content as-is | Fix CQRS bypass only. Boost/Pin/Auction exist, just need proper handlers. |
| 8 | Full guide analytics suite | Completed tours, avg rating, booking rate, slot utilization %, top tours, revenue trend, tourist demographics, repeat customer rate. |
| 9 | GDPR data export | GET /gdpr/export — returns all user data as JSON (interactions, preferences, bookings snapshot, recommendations). |
| 10 | Dashboard granularity: Daily+Weekly+Monthly | Background service pre-computes at 3 levels. Endpoint accepts `?granularity=` param. |
| 11 | Wire UserExcludedEntity into scoring | Negative signals ("not interested") already stored. Properly wire into V1 + V2 scorers as hard exclusion filter. |
| 12 | Fix CQRS bypass (Gap #1) | Create 15 proper Command/Query + Handler pairs for all inline endpoints. |
| 13 | Fix permission granularity (Gap #3) | Add 6 new feature groups: BoostPackage, EditorialPin, SeasonalityRule, HolidayCalendar, Photogenic, Experiment. |
| 14 | Fix audit trail (Gap #6) | Remove AdminUserId from request. Derive from ICurrentUser. |

---

## Current State Summary

### What's Built (working)
- **Content-based scoring** (V1ContentSimilarityScorer, 355L): 6 signals, 8 multipliers, MMR diversity, Vickrey auction
- **Popularity scoring**: PopularityScoreCalculationService with trending (7-day delta)
- **Interaction ingestion**: InteractionIngestDrainService (channel-based, batch 100)
- **20 integration event handlers**: Booking, Finance, Social, Content, Auth events consumed
- **10 background services**: Popularity calc, suggestion refresh, ingest drain, GDPR cleanup, email digest, metrics aggregation
- **Sponsored content**: BoostPackage with CPC bidding, daily budget, decay + Vickrey auction
- **Editorial pins**: Manual content promotion
- **Seasonality + holidays**: Time-based scoring adjustments
- **A/B experiments**: Create/assign/track
- **GDPR**: Deletion requests (30-day window) + 365-day anonymization
- **Admin dashboard**: Pre-computed with granularity
- **Provider dashboard**: Scoped analytics
- **47 endpoints**: All auth-decorated

### What's NOT Built / Broken
- ❌ Collaborative filtering (spec: 40% weight)
- ❌ Blended scoring assembly (collaborative + content + popularity)
- ❌ Guide-specific dashboard
- ❌ GDPR data export (portability)
- ❌ 15 endpoints bypass CQRS (direct repo injection)
- ❌ 3 anonymous POSTs (security risk)
- ❌ Permission granularity (15 endpoints share 2 generic permissions)
- ❌ Audit trail identity spoofing (AdminUserId from request body)
- ❌ Diversity rule: MaxPerPlaceId=2 should be MaxPerProviderId=3
- ❌ Negative signal wiring (UserExcludedEntity exists but not in scoring)

---

## Workflow A — Interaction Ingestion Pipeline

```
User action (view, click, search, book, review, share, etc.)
  → POST /interactions (NOW requires auth)
  → Validate: InteractionType valid, EntityType valid, EntityId exists
  → Deduplicate via IngestDebounceMarker (configurable TTL per type)
  → Write to Channel<UserInteraction> (in-memory buffer)
  → InteractionIngestDrainService (background, batch 100):
    → Persist to DB
    → Fire UserInteractionRecordedDomainEvent
    → Update UserPreference affinity signals
```

### New Interaction Types for TourGuide Flow
```csharp
// Extend InteractionType enum:
GuideProfileView     = 12,  // Tourist views guide profile
GuideBooking         = 13,  // Tourist books specific guide
SlotSelection        = 14,  // Tourist selects availability slot
TourCompletion       = 15,  // Tourist completes tour with guide
GuideRating          = 16,  // Tourist rates guide post-tour
```

---

## Workflow B — Recommendation Engine (Blended Scoring)

```
GET /recommendations (authenticated)
  → Resolve user preferences (UserPreference + UserPreferredCategory)
  → Load UserExcludedEntity list → HARD EXCLUSION FILTER
  → Fetch candidate entities (from EntityAttributeSnapshot)
  → Score each candidate through 3 scorers:

  1. CollaborativeScorer (40% weight):
     → Load pre-computed co-occurrence matrix
     → Find user's past interactions (booked/viewed/favorited)
     → Score candidates by co-occurrence strength with user's history
     → Returns: 0.0-1.0 per candidate

  2. ContentScorer (35% weight) — existing V1:
     → Category match, price proximity, geo distance, rating, popularity, featured
     → Apply multipliers: recency, occupancy, boost, family, seasonality, holiday, photogenic
     → Returns: 0.0-1.0 per candidate

  3. PopularityScorer (25% weight):
     → Lookup PopularityScore.TrendingRank for each candidate
     → Normalize to 0.0-1.0 (rank 1 = 1.0, decaying)
     → Apply recency boost for newly trending items

  → BlendedScore = 0.40 × Collaborative + 0.35 × Content + 0.25 × Popularity
  → Apply diversity: MaxPerProviderId = 3 (FIX from MaxPerPlaceId=2)
  → Apply MMR selection for category diversity
  → Inject sponsored content (Vickrey auction, max 2 per batch)
  → Cache result in RecommendationCache (TTL: 10 min)
  → Return ranked list
```

### Collaborative Filtering — Technical Design

**Algorithm**: Item-Item Co-occurrence with time decay

**Data source**: UserInteraction (Book, Favorite, Review, Complete) — high-signal actions only

**Matrix structure**:
```
CoOccurrenceMatrix[EntityA][EntityB] = Σ (1/log(|user_interactions|)) × time_decay
```
- Only count users with 2+ high-signal interactions
- Time decay: e^(-λt) where λ=0.01/day, t=days since interaction
- Normalize by entity pair frequency (PMI — Pointwise Mutual Information)

**Storage**: NO TABLE. Computed on-demand from `UserInteraction` table + cached in HybridCache (24h TTL).

> **UNMAPPED DECISION:** No CollaborativeScoreMatrix table exists. The scoring engine:
> 1. Queries `UserInteraction` for co-occurrence patterns (last 180 days, high-signal only)
> 2. Computes PMI-normalized scores on-the-fly
> 3. Caches results in HybridCache with 24h TTL, keyed by `collab:entityA:{id}:{type}`
> 4. Background service (`CollaborativeMatrixBuildService`) pre-warms cache nightly at 03:00 UTC

```csharp
// DTO only — never persisted to a table
public sealed record CollaborativeScore(Guid EntityBId, byte EntityBType, decimal Score);

// Cache key pattern: "analytics:collab:{entityAId}:{entityAType}"
// Value: IReadOnlyList<CollaborativeScore> (top 50 related entities)
```

The `ICollaborativeScoringEngine.GetRelatedEntitiesAsync(entityId, entityType)` checks cache first,
computes on miss. No repository, no EF config, no table.

**Background Service: CollaborativeMatrixBuildService**
- Schedule: Nightly at 03:00 UTC
- Steps:
  1. Load high-signal interactions from last 180 days
  2. Build user→items map (only users with 2+ interactions)
  3. For each user pair of items: increment co-occurrence with time decay
  4. Compute PMI normalization
  5. Truncate old matrix, bulk-insert new scores
  6. Log: total pairs, compute duration, top-10 strongest pairs

---

## Workflow C — Popularity & Trending

```
PopularityScoreCalculationService (every 30 min):
  → Query interactions in last 7 days per entity
  → Weighted sum: View=1, Click=2, Favorite=3, Book=10, Review=5, Share=3
  → Compute raw score + 7-day delta (trending)
  → Rank by score → assign TrendingRank
  → Fire PopularityScoreRecalculatedDomainEvent
  → Write EntityPopularitySnapshot (historical)

Trending = entities with highest 7-day positive delta (new popularity growth)
```

---

## Workflow D — Guide Dashboard

```
GET /dashboard/guide (authenticated, guide role)
  → Resolve guideId from ICurrentUser
  → Read DashboardCache for guide granularity (or compute live if stale)
  → Return:
    {
      completedTours: int,
      avgRating: decimal,
      bookingRate: decimal,        // bookings / available slots %
      slotUtilization: decimal,    // booked slots / total slots %
      topTours: [{tourId, name, bookings, rating}],
      revenueTrend: [{period, amount}],  // last 12 periods
      touristDemographics: {nationalities: [{country, count}], repeatRate: decimal},
      repeatCustomerRate: decimal, // returning tourists %
      periodComparison: {current, previous, deltaPercent}
    }

DashboardPreComputeService (nightly at 02:00 UTC):
  → For each active guide:
    → Query BookingSnapshot WHERE GuideId = guide
    → Compute all metrics at Daily/Weekly/Monthly granularity
    → Upsert into DashboardCache(EntityType=Guide, EntityId=guideId, Granularity, Data JSON)
```

---

## Workflow E — Provider Dashboard (existing + enhanced)

```
GET /dashboard/provider (authenticated, provider role)
  → Same as current but SCOPED to provider's entities
  → Metrics: total bookings, revenue, cancellation rate, avg rating, popular tours
  → Granularity: Daily/Weekly/Monthly

GET /dashboard/admin (admin role)
  → Platform-wide: total bookings, revenue, active users, new registrations
  → Top providers, top tours, trending entities
  → Granularity: Daily/Weekly/Monthly
```

---

## Workflow F — GDPR Compliance

```
Deletion Request:
  POST /gdpr/deletion-request → Create GdprDeletionRequest (30-day cooling)
  GET /gdpr/deletion-status → Check request status
  POST /gdpr/cancel-deletion → Cancel if within 30-day window (NEW)

Data Export (NEW):
  GET /gdpr/export → Compile all user data:
    → UserInteractions (last 365 days)
    → UserPreference + UserPreferredCategory
    → UserExcludedEntity
    → BookingSnapshot (user's bookings)
    → RecommendationCache (user's last recommendations)
    → ExperimentAssignment (user's experiment participation)
    → Return as JSON with content-disposition: attachment

Execution (GdprCleanupJob, daily):
  → Find requests WHERE ExecuteAt <= now AND Status = Pending
  → Delete all user data across Analytics tables
  → Anonymize interactions older than 365 days (hash userId)
  → Mark request as Executed
```

---

## New Integration Event Handlers

| # | Event Source | Handler Purpose |
|---|-------------|----------------|
| 1 | TourGuide-Flow | GuideBookingCompletedHandler → update guide metrics |
| 2 | TourGuide-Flow | GuideTierPromotedHandler → update guide popularity weight |
| 3 | Booking-Workflow | SlotBookedHandler → update slot utilization metrics |
| 4 | Booking-Workflow | BookingCompletedWithGuideHandler → update collaborative matrix signals |
| 5 | Finance-Workflow | GuidePayoutCompletedHandler → update guide revenue snapshot |
| 6 | Social-Workflow | GuideReviewPostedHandler → update guide rating in dashboard |
| 7 | Platform-Onboarding | NewProviderApprovedHandler → initialize provider dashboard cache |

---

## New/Modified Endpoints

### Fix Anonymous POSTs (3 endpoints)
| Route | Change |
|-------|--------|
| `POST /interactions` | Remove AllowAnonymous → add MustHavePermission(Interaction, Create) |
| `POST /sponsored-click` | Remove AllowAnonymous → add MustHavePermission(Interaction, Create) |
| `POST /metrics` | Remove AllowAnonymous → add MustHavePermission(Batch, Read) |

### New Guide Dashboard Endpoints (3)
| # | Method | Route | Auth | Purpose |
|---|--------|-------|------|---------|
| 1 | GET | `/dashboard/guide` | MustHavePermission(GuideDashboard, Read) | Guide analytics summary |
| 2 | GET | `/dashboard/guide/export` | MustHavePermission(GuideDashboard, Export) | CSV/JSON export |
| 3 | GET | `/dashboard/guide/tours` | MustHavePermission(GuideDashboard, Read) | Per-tour breakdown |

### New GDPR Endpoints (2)
| # | Method | Route | Auth | Purpose |
|---|--------|-------|------|---------|
| 4 | GET | `/gdpr/export` | MustHavePermission(Preference, Read) | Download user data |
| 5 | POST | `/gdpr/cancel-deletion` | MustHavePermission(Preference, Update) | Cancel pending deletion |

### CQRS Fix — 15 Inline Endpoints → MediatR (Gap #1)

| # | New Command/Query | Replaces |
|---|-------------------|----------|
| 1 | CreateBoostPackageCommand | POST /admin/boosts |
| 2 | ListBoostPackagesQuery | GET /admin/boosts |
| 3 | DeleteBoostPackageCommand | DELETE /admin/boosts/{id} |
| 4 | CreateEditorialPinCommand | POST /admin/pins |
| 5 | ListEditorialPinsQuery | GET /admin/pins |
| 6 | CreateSeasonalityRuleCommand | POST /admin/seasonality |
| 7 | ListSeasonalityRulesQuery | GET /admin/seasonality |
| 8 | DeleteSeasonalityRuleCommand | DELETE /admin/seasonality/{id} |
| 9 | CreateHolidayCommand | POST /admin/holidays |
| 10 | ListHolidaysQuery | GET /admin/holidays |
| 11 | TogglePhotogenicCommand | POST /admin/photogenic |
| 12 | CreateExperimentCommand | POST /admin/experiments |
| 13 | ListExperimentsQuery | GET /admin/experiments |
| 14 | DeleteExperimentCommand | DELETE /admin/experiments/{id} |
| 15 | ListSuggestionBatchesQuery | GET /admin/batches |

---

## New Permission Features (Gap #3 Fix)

```csharp
// Add to AnalyticsPermissionCatalog:
BoostPackage     → Read, Create, Delete    (3)
EditorialPin     → Read, Create            (2)
SeasonalityRule  → Read, Create, Delete    (3)
HolidayCalendar  → Read, Create            (2)
Photogenic       → Update                  (1)
Experiment       → Read, Create, Delete    (3)
GuideDashboard   → Read, Export            (2)
// Total new: 16 permissions
```

Replace all `Batch.Refresh` / `Batch.Read` on admin CRUD endpoints with proper feature-specific permissions.

---

## New Entities

### CollaborativeScoreMatrix — DROPPED (no table, DTO + HybridCache only)

> **No database table.** Scores computed from `UserInteraction` table on-demand.
> Cached in HybridCache (24h TTL). Pre-warmed nightly by `CollaborativeMatrixBuildService`.
> See Phase 4 storage section above for DTO definition and cache key pattern.
- Partition: by ComputedAt for fast truncate-and-rebuild

### GuideDashboardSnapshot (extends DashboardCache pattern)
Reuse existing `DashboardCache` entity with `EntityType = Guide` (add to enum if needed).

---

## New Interfaces

### ICollaborativeScoringEngine
```csharp
public interface ICollaborativeScoringEngine
{
    Task<IReadOnlyDictionary<Guid, decimal>> ScoreAsync(
        Guid userId,
        IReadOnlyList<EntityRef> candidates,
        CancellationToken ct);
}
```

### IPopularityScoringEngine
```csharp
public interface IPopularityScoringEngine
{
    Task<IReadOnlyDictionary<Guid, decimal>> ScoreAsync(
        IReadOnlyList<EntityRef> candidates,
        CancellationToken ct);
}
```

### IBlendedScoringEngine (composes all three)
```csharp
public interface IBlendedScoringEngine
{
    Task<IReadOnlyList<ScoredEntity>> ScoreAndRankAsync(
        Guid userId,
        IReadOnlyList<EntityRef> candidates,
        ScoringWeights weights,
        CancellationToken ct);
}
public record ScoringWeights(decimal Collaborative = 0.40m, decimal Content = 0.35m, decimal Popularity = 0.25m);
```

---

## New Background Services

### CollaborativeMatrixBuildService
- **Schedule**: Nightly 03:00 UTC
- **Logic**: Build item-item co-occurrence matrix from 180 days of high-signal interactions
- **Output**: Pre-warm HybridCache with top-50 related entities per high-traffic entity

### GuideDashboardPreComputeService
- **Schedule**: Nightly 02:00 UTC (before matrix build)
- **Logic**: For each active guide, compute all metrics at 3 granularities, upsert DashboardCache

### Modified: PopularityScoreCalculationService
- Feed results into IPopularityScoringEngine-compatible format
- Ensure TrendingRank properly normalized for PopularityScorer

---

## Scoring Engine Architecture

```
IBlendedScoringEngine (orchestrator)
  ├── ICollaborativeScoringEngine (V2CollaborativeScorer)
  │     └── reads HybridCache (collaborative scores)
  ├── IRecommendationScoringEngine (V1ContentSimilarityScorer — existing)
  │     └── reads EntityAttributeSnapshot, SeasonalityRule, HolidayCalendar, BoostPackage
  └── IPopularityScoringEngine (V2PopularityScorer)
        └── reads PopularityScore + TrendingRank

Diversity applied AFTER blending:
  → MaxPerProviderId = 3 (FIX from MaxPerPlaceId = 2)
  → MMR with Jaccard similarity (existing)
  → Hard exclusion: UserExcludedEntity filter (BEFORE scoring, saves compute)
```

---

## InteractionType Enum Extension

```csharp
// Existing (0-11):
View=0, Click=1, Search=2, Book=3, Favorite=4, Share=5,
Review=6, WishlistAdd=7, CompareAdd=8, ItineraryAdd=9, DismissRecommendation=10, MapView=11

// New (12-16) for TourGuide flow:
GuideProfileView  = 12,
GuideBooking      = 13,
SlotSelection     = 14,
TourCompletion    = 15,
GuideRating       = 16,
```

---

## Cross-Module Integration Map

| Source Module | Event | Analytics Action |
|---------------|-------|-----------------|
| Booking | TourBookingCreated | BookingSnapshot + UserInteraction(Book) |
| Booking | TourBookingCompleted | UserInteraction(TourCompletion) + trigger dashboard refresh |
| Booking | TourBookingCancelled | Update BookingSnapshot status |
| Finance | PaymentCompleted | PaymentSnapshot |
| Finance | GuidePayoutCompleted | Update guide revenue in dashboard cache |
| Social | ReviewPosted | UserInteraction(Review) + update entity rating |
| Social | ReviewDeleted | Remove interaction + recalculate |
| Social | FavoriteAdded | UserInteraction(Favorite) |
| Social | RatingRecalculated | Update EntityAttributeSnapshot |
| ContentTours | TourCreated/Updated | Update EntityAttributeSnapshot |
| ContentPlaces | PlaceCreated/Updated | Update EntityAttributeSnapshot |
| ContentPlaces | BusinessCreated/Updated | Update EntityAttributeSnapshot |
| TourGuide-Flow | GuideBookingCompleted | UserInteraction(GuideBooking) + guide dashboard |
| TourGuide-Flow | GuideTierPromoted | Adjust guide weight in scoring |
| Platform-Onboarding | ProviderApproved | Initialize provider/guide dashboard cache |
| Auth | UserRegistered | Initialize UserPreference defaults |

---

## Execution Phases

### Phase 1 — Bug Fixes & Compliance (~4h)
- Remove AllowAnonymous from 3 POST endpoints → RequireAuthorization + MustHavePermission
- Fix audit trail: Remove AdminUserId from request, derive from ICurrentUser
- Fix Guid.NewGuid() → Guid.CreateVersion7()
- Fix PaymentSnapshot.Refund() dead parameter
- Fix GetItineraryQueryHandler dead dependency
- Fix diversity rule: MaxPerPlaceId=2 → MaxPerProviderId=3

### Phase 2 — Permission Granularity (~2h)
- Add 7 new features (16 permissions) to AnalyticsPermissionCatalog
- Update 15 admin endpoints to use proper feature-specific permissions
- Add GuideDashboard feature (Read, Export)

### Phase 3 — CQRS Fix (~8h)
- Create 15 Command/Query + Handler + Validator sets
- Move all direct repo logic from RecommendationsEndpoints into handlers
- Update endpoint bodies to use ISender.Send()
- Verify all MediatR pipeline behaviors (validation, caching) apply

### Phase 4 — Collaborative Filtering Engine (~16h)
- Create CollaborativeScore DTO + ICollaborativeScoringEngine (HybridCache, no table)
- Create ICollaborativeScoringEngine interface
- Implement V2CollaborativeScorer (item-item co-occurrence with PMI + time decay)
- Create CollaborativeMatrixBuildService (nightly)
- Create IPopularityScoringEngine interface
- Implement V2PopularityScorer (normalized TrendingRank)
- Create IBlendedScoringEngine orchestrator
- Implement BlendedScoringEngine (compose 3 scorers with configurable weights)
- Wire UserExcludedEntity as hard exclusion filter (before scoring)
- Update GET /recommendations to use BlendedScoringEngine

### Phase 5 — Guide Dashboard (~6h)
- Add InteractionType values 12-16
- Create GuideDashboardPreComputeService (nightly)
- Create GetGuideDashboardQuery + Handler
- Create GetGuideDashboardExportQuery + Handler
- Create GetGuideTourBreakdownQuery + Handler
- Add 3 new endpoints
- Add 7 new integration event handlers for guide/booking/finance events

### Phase 6 — GDPR Data Export (~3h)
- Create ExportUserDataQuery + Handler
- Compile all user data across Analytics tables into JSON
- Create POST /gdpr/cancel-deletion command + handler
- Add 2 new endpoints

### Phase 7 — Validators (~3h)
- Create validators for all new commands (15 CQRS fix + new commands)
- Ensure existing 3 validators cover all edge cases

### Phase 8 — Solution Build + Verify (~1h)
- Build entire solution
- Fix any test compilation errors
- Verify 0 errors

---

## File Estimates

| Category | New Files | Modified Files |
|----------|-----------|---------------|
| Domain (entities, enums, events) | 2 | 3 |
| Application (commands, queries, handlers, validators, interfaces) | 40 | 8 |
| Infrastructure (services, handlers, configs, repos) | 12 | 6 |
| Presentation (endpoints) | 0 | 3 |
| Contracts (permissions, events) | 2 | 2 |
| **Total** | **~56** | **~22** |

---

## Compatibility Notes

### With TourGuide-Flow.md
- Guide dashboard (Decision #5, #8) provides all metrics guides need
- InteractionType extensions (12-16) track guide-specific user actions
- New event handlers consume TourGuide integration events
- CollaborativeScorer benefits from guide-booking data (more signal)

### With Booking-Workflow.md
- BookingSnapshot already consumed (20 handlers exist)
- NEW: SlotBooked + BookingCompletedWithGuide events feed collaborative matrix
- Guide slot utilization computed from booking data

### With Finance-Workflow.md
- PaymentSnapshot already consumed
- NEW: GuidePayoutCompleted feeds revenue trend in guide dashboard
- Revenue-tier commission doesn't affect analytics scoring (Decision from Finance plan)

### With Social-Workflow.md
- Review/Favorite/Rating events already consumed
- GuideReviewPosted feeds guide dashboard avg rating
- ReviewHelpfulVote may later feed recommendation quality signals

### With Messaging-Workflow.md
- Analytics fires no outbound notifications directly
- Messaging consumes PopularityScoreRecalculatedIntegrationEvent (for trending alerts — optional)
- Re-engagement segment (GET /segments/re-engagement) feeds Messaging for win-back campaigns

### With Platform-Onboarding-Workflow.md
- NewProviderApproved → initialize dashboard cache for new provider/guide
- Agency analytics: aggregate guide performance under agency umbrella

### With BlogCreatorPost-Merger.md
- Post-merger: EntityType stays Tour=1, Place=2, Business=3, Category=4
- Blog interactions tracked via standard EntityType + EntityId pattern (add Blog=5 if needed)

### With Role-System.md
- GuideDashboard permission requires TourGuide role
- ProviderDashboard requires Provider role
- AdminDashboard requires Admin role
