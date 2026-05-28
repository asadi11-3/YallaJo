# Analytics Module — Workflow Plan

> **Module**: Analytics — Personalized recommendations, interaction tracking, dashboards, GDPR compliance, A/B experiments
> **Status**: Partially Implemented (audited 2025-01-27, score ~8.5/10 after W1-D + W2-A fixes)
> **Dependencies**: Booking (BookingSnapshot), Finance (PaymentSnapshot), Social (Reviews/Favorites/Ratings), ContentTours (Tour events), ContentPlaces (Place/Business events), Auth (UserRegistered), TourGuide-Flow (Guide events)
> **Compatible With**: TourGuide-Flow.md, Booking-Workflow.md, Finance-Workflow.md, Social-Workflow.md, Messaging-Workflow.md, Platform-Onboarding-Workflow.md, Role-System.md, BlogCreatorPost-Merger.md, ContentPlaces-Workflow.md

---

## Design Decisions (14 — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | **Require auth for all POST** | Remove AllowAnonymous from tracking POSTs (interactions, sponsored-click, metrics). Only authenticated users tracked. No anonymous personalization. |
| 2 | **Full collaborative filtering** | Build complete item-item collaborative scorer (ALS/co-occurrence matrix). Spec weight: 40% collaborative + 35% content + 25% popularity. |
| 3 | **Nightly matrix recomputation** | `CollaborativeMatrixBuildService` rebuilds interaction matrix daily at 03:00 UTC. |
| 4 | **Keep integration event handlers** | No stream processor. Add new handlers for TourGuide/Booking events. Current inbox-deduplicated pattern is proven. |
| 5 | **Dedicated guide dashboard** | `GET /dashboard/guide` — full analytics: completed tours, avg rating, booking rate, slot utilization, top tours, revenue trend, tourist demographics, repeat customer rate. |
| 6 | **Keep experiments as-is** | Fix CQRS bypass only. No new experiment features. |
| 7 | **Keep sponsored content as-is** | Fix CQRS bypass only. Boost/Pin/Auction exist, just need proper handlers. |
| 8 | **Full guide analytics suite** | Metrics: completed tours, avg rating, booking rate, slot utilization %, top tours, revenue trend, tourist demographics, repeat customer rate. |
| 9 | **GDPR data export** | `GET /gdpr/export` — returns all user data as JSON (interactions, preferences, bookings snapshot, recommendations, experiment assignments). |
| 10 | **Dashboard granularity: Daily+Weekly+Monthly** | Background service pre-computes at 3 levels. Endpoint accepts `?granularity=` param. |
| 11 | **Wire UserExcludedEntity into scoring** | "Not interested" signals stored as hard exclusion filter applied BEFORE scoring. |
| 12 | **Fix CQRS bypass** | Create 15 proper Command/Query + Handler pairs for all inline repo endpoints. ✅ DONE (W1-D) |
| 13 | **Fix permission granularity** | Add 6 new feature groups: BoostPackage, EditorialPin, SeasonalityRule, HolidayCalendar, Photogenic, Experiment. ✅ DONE (already existed) |
| 14 | **Fix audit trail** | Remove AdminUserId from request body. Derive from `ICurrentUser`. ✅ DONE (already implemented) |

---

## Current State Summary

### What's Built (COMPLETE)
- **Content-based scoring** (`V1ContentSimilarityScorer`, 355L): 6 signals, 8 multipliers, MMR diversity, Vickrey auction
- **Popularity scoring**: `PopularityScoreCalculationService` with trending (7-day delta)
- **Interaction ingestion**: `InteractionIngestDrainService` (channel-based, batch 100)
- **28 integration event handlers**: Booking(5), Finance(3), Social(4), ContentTours(4), ContentPlaces(6), Auth(2), Accounts(4)
- **8 background services**: Popularity calc, suggestion refresh, ingest drain, GDPR cleanup, email digest, metrics aggregation, trip stage update, user profile update
- **Sponsored content**: BoostPackage with CPC bidding, daily budget, decay + Vickrey auction
- **Editorial pins**: Manual content promotion
- **Seasonality + holidays**: Time-based scoring adjustments
- **A/B experiments**: Create/assign/track (full CRUD)
- **GDPR**: Deletion requests (30-day window) + 365-day anonymization
- **Admin dashboard**: Pre-computed with DashboardCache key/value store
- **Provider dashboard**: Scoped analytics
- **Guide dashboard**: 3 endpoints exist at `/guide/dashboard`, `/guide/analytics`, `/guide/my-tours`
- **GDPR export**: Exists at `GET /me/export` (not `/gdpr/export` as planned)
- **51 endpoints** across 3 files: 40 auth-required, 9 AllowAnonymous, 2 fixed in W2-A
- **16 permission feature groups**: All exist in AnalyticsPermissionCatalog
- **15 CQRS bypasses removed** (W1-D): All 15 inline repo routes now use `ISender.Send()`
- **Scoring interfaces**: `ICollaborativeScoringEngine` + `IBlendedScoringEngine` — NoOp implementations registered

### What's NOT Built (Deferred)
- ❌ **Collaborative filtering engine** (spec: 40% weight) — `NoOpCollaborativeScoringEngine` registered, real implementation deferred
- ❌ **Blended scoring assembly** — `NoOpBlendedScoringEngine` registered, depends on collaborative engine
- ❌ **CollaborativeMatrixBuildService** (nightly) — depends on collaborative engine
- ❌ **Diversity rule**: MaxPerPlaceId=2 should be MaxPerProviderId=3
- ❌ **InteractionType values 12-16** (GuideProfileView, GuideBooking, SlotSelection, TourCompletion, GuideRating) — not yet added

### Gap Assessment
| Gap | Status | Action |
|-----|--------|--------|
| 15 CQRS bypasses in RecommendationsEndpoints | ✅ RESOLVED (W1-D) | 15 command/query handlers created |
| Auth on tracking POSTs + admin GETs | ✅ RESOLVED (W2-A) | All 4 routes now require auth |
| Collaborative filtering engine | ❌ OPEN | Real implementation deferred, NoOp registered |
| Blended scoring orchestrator | ❌ OPEN | Real implementation deferred, NoOp registered |
| InteractionType extension (12-16) | ❌ OPEN | Need to add enum values + handlers |
| DashboardCache granularity columns | ❌ OPEN | Simple key/value, no EntityType/Granularity |

---

## Domain Model

### Entities

```
UserInteraction
  Properties: UserId, EntityType(byte), EntityId, InteractionType(byte), OccurredAt,
              SessionId, Source(string?), Metadata(string?)
  Inherits: BaseEntity

UserPreference
  Properties: UserId, EntityType(byte), CategoryId(Guid?), PriceMin, PriceMax,
              PreferredLanguages(string[]), AffinityScoreJson
  Inherits: AuditableEntity

UserExcludedEntity
  Properties: UserId, EntityType(byte), EntityId, Reason
  Inherits: BaseEntity

EntityAttributeSnapshot
  Properties: EntityType(byte), EntityId, ProviderId, CategoryId, Tags, PriceMin, PriceMax,
              AverageRating, PopularityScore, IsActive, IsApproved, MetadataJson, IsPhotogenic
  Inherits: AuditableEntity, IAggregateRoot
  Methods: UpdatePhotogenic()

PopularityScore
  Properties: EntityType(byte), EntityId, Score, TrendingRank, Delta7Days, ComputedAt
  Inherits: BaseEntity

RecommendationCache
  Properties: UserId, EntityType(byte), EntityId, Position, Score, ComputedAt, ExpiresAt
  Inherits: BaseEntity

DashboardCache
  Properties: Key(string), ValueJson(string), ExpiresAt, RebuiltAt
  Note: Simple key/value store — used for Admin/Provider/Guide dashboard pre-computation

BoostPackage
  Properties: EntityType(byte), EntityId, ProviderId, BidAmountPerClick, DailyBudget,
              StartDate, EndDate, IsActive, SpentToday, TotalSpent
  Inherits: AuditableEntity, IAggregateRoot
  Methods: Create(), Deactivate(), RecordClick(decimal bid)

EditorialPin
  Properties: EntityType(byte), EntityId, Position, StartDate, EndDate, IsActive, PinnedByAdminId
  Inherits: AuditableEntity, IAggregateRoot
  Methods: Create(), Deactivate()

SeasonalityRule
  Properties: EntityType(byte?), CategoryId(Guid?), StartMonth, EndMonth, MultiplicationFactor, IsActive
  Inherits: AuditableEntity, IAggregateRoot
  Methods: Create(), Deactivate()

HolidayCalendar
  Properties: Year, HolidayDates(string), CountryCodes(string), IsActive
  Inherits: AuditableEntity, IAggregateRoot
  Methods: Create()

Experiment
  Properties: Name, Description, StartDate, EndDate, TrafficPercent, Status, VariantsJson
  Inherits: AuditableEntity, IAggregateRoot
  Methods: Create(), Start(), Complete()

ExperimentAssignment
  Properties: ExperimentId, UserId, Variant, AssignedAt
  Inherits: BaseEntity

GdprDeletionRequest
  Properties: UserId, RequestedAt, ExecuteAt(30-day), Status, ExecutedAt
  Inherits: BaseEntity
  Methods: Create(), Cancel(), Execute()
```

### Enums

```csharp
public enum InteractionType : byte
{
    View = 0, Click = 1, Search = 2, Book = 3, Favorite = 4,
    Share = 5, Review = 6, WishlistAdd = 7, CompareAdd = 8,
    ItineraryAdd = 9, DismissRecommendation = 10, MapView = 11,
    // Planned (not yet added):
    // GuideProfileView = 12, GuideBooking = 13,
    // SlotSelection = 14, TourCompletion = 15, GuideRating = 16
}
```

### Key Interfaces (Scoring)

```csharp
// Analytics.Application/Scoring/
public interface ICollaborativeScoringEngine
{
    Task<IReadOnlyDictionary<Guid, decimal>> ScoreAsync(
        Guid userId,
        IReadOnlyList<EntityRef> candidates,
        CancellationToken ct);
}

public interface IBlendedScoringEngine
{
    Task<IReadOnlyList<ScoredEntity>> ScoreAndRankAsync(
        Guid userId,
        IReadOnlyList<EntityRef> candidates,
        ScoringWeights weights,
        CancellationToken ct);
}

public record ScoringWeights(decimal Collaborative = 0.40m, decimal Content = 0.35m, decimal Popularity = 0.25m);

// Collaborative filtering — NO DATABASE TABLE
// DTO only, cached in HybridCache (24h TTL)
// Cache key: "analytics:collab:{entityAId}:{entityAType}"
public sealed record CollaborativeScore(Guid EntityBId, byte EntityBType, decimal Score);
```

---

## Application Layer

### Commands

| Command | Handler | Validator | Status |
|---------|---------|-----------|--------|
| `RecordInteractionCommand` | ✅ | ✅ | Done |
| `SubmitOnboardingResponsesCommand` | ✅ | ✅ | Done |
| `MarkNotInterestedCommand` | ✅ | ✅ | Done |
| `RecordSponsoredClickCommand` | ✅ | ✅ | Done (W1-D) |
| `RecordSuggestionMetricCommand` | ✅ | ✅ | Done (W1-D) |
| `CreateBoostPackageCommand` | ✅ | ✅ | Done (W1-D) |
| `DeactivateBoostPackageCommand` | ✅ | ✅ | Done (W1-D) |
| `CreateCpcBoostPackageCommand` | ✅ | ✅ | Done (W1-D) |
| `CreateEditorialPinCommand` | ✅ | ✅ | Done (W1-D) |
| `DeactivateEditorialPinCommand` | ✅ | ✅ | Done (W1-D) |
| `CreateSeasonalityRuleCommand` | ✅ | ✅ | Done (W1-D) |
| `DeactivateSeasonalityRuleCommand` | ✅ | ✅ | Done (W1-D) |
| `CreateHolidayCalendarCommand` | ✅ | ✅ | Done (W1-D) |
| `SetEntityPhotogenicCommand` | ✅ | ✅ | Done (W1-D) |
| `CreateExperimentCommand` | ✅ | ✅ | Done (W1-D) |
| `StartExperimentCommand` | ✅ | ✅ | Done (W1-D) |
| `CompleteExperimentCommand` | ✅ | ✅ | Done (W1-D) |
| `SetUserPreferencesCommand` | ✅ | ✅ | Done |
| `RequestGdprDeletionCommand` | ✅ | ✅ | Done |
| `CancelGdprDeletionCommand` | ✅ | ✅ | Done |
| `RefreshSuggestionBatchCommand` | ✅ | ✅ | Done |
| `RedactAuditLogCommand` | ✅ | ✅ | Done |

### Queries

| Query | Handler | Cacheable | Notes |
|-------|---------|-----------|-------|
| `GetRecommendationsQuery` | ✅ | Yes | Uses V1ContentSimilarityScorer (blended = NoOp) |
| `GetSimilarEntitiesQuery` | ✅ | Yes | |
| `GetEntitySuggestionsQuery` | ✅ | Yes | |
| `GetItineraryQuery` | ✅ | Yes | |
| `GetUserPreferencesQuery` | ✅ | Yes | |
| `GetMetricsQuery` | ✅ | No | Admin only |
| `GetReengagementSegmentQuery` | ✅ | No | Admin only |
| `GetUserDataExportQuery` | ✅ | No | GDPR export at GET /me/export |
| `GetAdminDashboardOverviewQuery` | ✅ | No | In AnalyticsSprintHandlers.cs |
| `GetProviderDashboardQuery` | ✅ | No | Provider-scoped |
| `GetAdminRevenueDashboardQuery` | ✅ | No | |
| `GetAdminBookingsDashboardQuery` | ✅ | No | |
| `GetAdminUsersDashboardQuery` | ✅ | No | |
| `GetProviderAnalyticsQuery` | ✅ | No | |
| `GetProviderMyToursQuery` | ✅ | No | |
| `GetAuditLogsQuery` | ✅ | No | Admin only |
| `ExportAuditLogsQuery` | ✅ | No | Admin only |
| `GetSuggestionBatchesQuery` | ✅ | No | Done (W1-D) |
| `GetSeasonalityRulesQuery` | ✅ | No | Done (W1-D) |
| `GetHolidayCalendarByYearQuery` | ✅ | No | Done (W1-D) |

---

## Infrastructure

### EF Core Configurations

| Entity | Table | Schema | Notes |
|--------|-------|--------|-------|
| `UserInteraction` | `UserInteractions` | `analytics` | IX on (UserId, EntityType, EntityId, OccurredAt) |
| `UserPreference` | `UserPreferences` | `analytics` | Unique on UserId |
| `UserExcludedEntity` | `UserExcludedEntities` | `analytics` | Unique on (UserId, EntityType, EntityId) |
| `EntityAttributeSnapshot` | `EntityAttributeSnapshots` | `analytics` | Unique on (EntityType, EntityId) |
| `PopularityScore` | `PopularityScores` | `analytics` | Unique on (EntityType, EntityId) |
| `RecommendationCache` | `RecommendationCache` | `analytics` | IX on (UserId, ComputedAt) |
| `DashboardCache` | `DashboardCache` | `analytics` | Unique on Key |
| `BoostPackage` | `BoostPackages` | `analytics` | IX on (EntityId, IsActive) |
| `EditorialPin` | `EditorialPins` | `analytics` | IX on Position |
| `SeasonalityRule` | `SeasonalityRules` | `analytics` | |
| `HolidayCalendar` | `HolidayCalendars` | `analytics` | IX on Year |
| `Experiment` | `Experiments` | `analytics` | |
| `ExperimentAssignment` | `ExperimentAssignments` | `analytics` | Unique on (ExperimentId, UserId) |
| `GdprDeletionRequest` | `GdprDeletionRequests` | `analytics` | IX on (Status, ExecuteAt) |
| `SuggestionBatch` | `SuggestionBatches` | `analytics` | |
| `IngestDebounceMarker` | `IngestDebounceMarkers` | `analytics` | Unique on (UserId, EntityId, InteractionType) |
| `AuditLog` | `AuditLogs` | `analytics` | IX on (UserId, CreatedAt) |
| `PaymentSnapshot` | `PaymentSnapshots` | `analytics` | |
| `BookingSnapshot` | `BookingSnapshots` | `analytics` | |

### Repositories

| Interface | Notes |
|-----------|-------|
| `IUserInteractionRepository` | GetByUserIdAsync, GetForCoOccurrenceAsync, GetRecentAsync |
| `IEntityAttributeSnapshotRepository` | GetByEntityAsync, GetCandidatesAsync |
| `IPopularityScoreRepository` | GetByEntityAsync, GetTopNAsync |
| `IRecommendationCacheRepository` | GetForUserAsync, UpsertAsync |
| `IDashboardCacheRepository` | GetByKeyAsync, UpsertAsync |
| `IBoostPackageRepository` | GetActiveForEntityAsync, GetAllAsync |
| `IEditorialPinRepository` | GetActiveAsync, GetAllAsync |
| `ISeasonalityRuleRepository` | GetActiveAsync, GetAllActiveAsync |
| `IHolidayCalendarRepository` | GetByYearAsync |
| `IExperimentRepository` | GetActiveAsync, GetAllAsync |
| `ISuggestionBatchRepository` | GetAllAsync |
| `IGdprDeletionRequestRepository` | GetPendingForExecutionAsync |
| `IAuditLogRepository` | GetPagedAsync, ExportAsync |
| `IUserPreferenceRepository` | GetByUserIdAsync, UpsertAsync |

### Domain Event Handlers

| Domain Event | Handler | Integration Event Published |
|-------------|---------|----------------------------|
| `UserInteractionRecordedDomainEvent` | `UserInteractionRecordedDomainEventHandler` | — (updates preferences inline) |
| `PopularityScoreRecalculatedDomainEvent` | `PopularityScoreRecalculatedDomainEventHandler` | `PopularityScoreUpdatedIntegrationEvent` |
| `EntityPhotogenicToggledDomainEvent` | `EntityPhotogenicToggledDomainEventHandler` | — |

### Inbound Integration Event Handlers (28 total)

| Source Module | Integration Event | Handler | Action |
|---|---|---|---|
| Booking | `TourBookingCreatedIntegrationEvent` | `BookingCreatedAnalyticsHandler` | BookingSnapshot upsert + UserInteraction(Book) |
| Booking | `TourBookingConfirmedIntegrationEvent` | `BookingConfirmedAnalyticsHandler` | Update snapshot status |
| Booking | `TourBookingCancelledIntegrationEvent` | `BookingCancelledAnalyticsHandler` | Update snapshot status |
| Booking | `TourBookingCompletedIntegrationEvent` | `BookingCompletedAnalyticsHandler` | UserInteraction(TourCompletion) |
| Booking | `SlotCapacityChangedIntegrationEvent` | `SlotCapacityAnalyticsHandler` | Update slot utilization |
| Finance | `PaymentCompletedIntegrationEvent` | `PaymentCompletedAnalyticsHandler` | PaymentSnapshot upsert |
| Finance | `PayoutCompletedIntegrationEvent` | `PayoutCompletedAnalyticsHandler` | Update revenue snapshot |
| Finance | `RefundCompletedIntegrationEvent` | `RefundCompletedAnalyticsHandler` | Update payment snapshot |
| Social | `ReviewPublishedIntegrationEvent` | `ReviewPublishedAnalyticsHandler` | UserInteraction(Review) + rating update |
| Social | `ReviewDeletedIntegrationEvent` | `ReviewDeletedAnalyticsHandler` | Remove interaction |
| Social | `FavoriteAddedIntegrationEvent` | `FavoriteAddedAnalyticsHandler` | UserInteraction(Favorite) |
| Social | `RatingRecalculatedIntegrationEvent` | `RatingRecalculatedAnalyticsHandler` | Update EntityAttributeSnapshot rating |
| ContentTours | `TourCreatedIntegrationEvent` | `TourCreatedAnalyticsHandler` | Initialize EntityAttributeSnapshot |
| ContentTours | `TourUpdatedIntegrationEvent` | `TourUpdatedAnalyticsHandler` | Update EntityAttributeSnapshot |
| ContentTours | `TourDeletedIntegrationEvent` | `TourDeletedAnalyticsHandler` | Mark snapshot inactive |
| ContentTours | `TourSuspendedIntegrationEvent` | `TourSuspendedAnalyticsHandler` | Mark snapshot inactive |
| ContentPlaces | `PlaceCreatedIntegrationEvent` | `PlaceCreatedAnalyticsHandler` | Initialize snapshot |
| ContentPlaces | `PlaceUpdatedIntegrationEvent` | `PlaceUpdatedAnalyticsHandler` | Update snapshot |
| ContentPlaces | `PlaceDeletedIntegrationEvent` | `PlaceDeletedAnalyticsHandler` | Mark inactive |
| ContentPlaces | `BusinessCreatedIntegrationEvent` | `BusinessCreatedAnalyticsHandler` | Initialize snapshot |
| ContentPlaces | `BusinessUpdatedIntegrationEvent` | `BusinessUpdatedAnalyticsHandler` | Update snapshot |
| ContentPlaces | `BusinessDeletedIntegrationEvent` | `BusinessDeletedAnalyticsHandler` | Mark inactive |
| ContentCore | `EntityCategoryAssignedIntegrationEvent` | `EntityCategoryAnalyticsHandler` | Update snapshot categories |
| ContentCore | `EntityCategoryRemovedIntegrationEvent` | `EntityCategoryRemovedAnalyticsHandler` | Update snapshot categories |
| Auth | `UserRegisteredIntegrationEvent` | `UserRegisteredAnalyticsHandler` | Initialize UserPreference defaults |
| Accounts | `ProviderApprovedIntegrationEvent` | `ProviderApprovedAnalyticsHandler` | Initialize provider dashboard cache |
| Messaging | `SupportTicketCreatedIntegrationEvent` | `TicketCreatedAnalyticsHandler` | AuditLog entry |
| Messaging | `SupportTicketAssignedIntegrationEvent` | `TicketAssignedAnalyticsHandler` | AuditLog entry |

### Background Services (8 existing + 2 planned)

| Service | Interval | Status | Purpose |
|---------|----------|--------|---------|
| `InteractionIngestDrainService` | Continuous (channel) | ✅ Built | Drains in-memory channel, batch-persists interactions |
| `PopularityScoreCalculationService` | Every 30 min | ✅ Built | Calculates weighted popularity + trending rank |
| `SuggestionBatchRefreshJob` | Configurable | ✅ Built | Refreshes EntityAttributeSnapshot candidates |
| `UserProfileUpdateJob` | Configurable | ✅ Built | Updates user preference affinities |
| `TripStageUpdateJob` | Configurable | ✅ Built | Updates trip stage based on booking data |
| `GdprCleanupJob` | Daily | ✅ Built | Executes pending deletions + anonymizes old interactions |
| `EmailDigestBackgroundService` | Daily | ✅ Built | Sends re-engagement emails via Messaging |
| `MetricsAggregationJob` | Hourly | ✅ Built | Pre-aggregates platform metrics for dashboards |
| `CollaborativeMatrixBuildService` | Nightly 03:00 UTC | ❌ Not built | Pre-warms HybridCache with co-occurrence scores |
| `GuideDashboardPreComputeService` | Nightly 02:00 UTC | ❌ Not built | Pre-computes guide metrics at 3 granularities |

---

## Presentation Layer

### Endpoint Groups

| File | Route Prefix | Tag | Routes |
|------|-------------|-----|--------|
| `AnalyticsEndpoints.cs` | `/api/v1/analytics` | Analytics | 20 |
| `RecommendationsEndpoints.cs` | `/api/v1/analytics/recommendations` | Recommendations | 31 |
| `PreferencesEndpoints.cs` | `/api/v1/analytics/preferences` | Preferences | 2 |
| **Total** | | | **51+** |

### Key Endpoint Table

| Method | Route | Auth | Permission | Handler |
|--------|-------|------|------------|---------|
| `POST` | `/interactions` | Required | `Interaction.Create` | `RecordInteractionCommand` |
| `GET` | `/recommendations` | Required | `Recommendation.Read` | `GetRecommendationsQuery` |
| `GET` | `/recommendations/similar/{entityId}` | AllowAnonymous | — | `GetSimilarEntitiesQuery` |
| `GET` | `/recommendations/for/{kind}/{entityId}` | AllowAnonymous | — | `GetEntitySuggestionsQuery` |
| `POST` | `/recommendations/onboarding` | Required | `Preference.Update` | `SubmitOnboardingResponsesCommand` |
| `POST` | `/recommendations/not-interested` | Required | `Interaction.Create` | `MarkNotInterestedCommand` |
| `POST` | `/recommendations/sponsored-click` | Required | `Interaction.Create` | `RecordSponsoredClickCommand` |
| `POST` | `/recommendations/metrics` | Required | `Batch.Read` | `RecordSuggestionMetricCommand` |
| `GET` | `/recommendations/me/export` | Required | `Preference.Read` | `GetUserDataExportQuery` |
| `DELETE` | `/recommendations/me` | Required | `Preference.Delete` | `RequestGdprDeletionCommand` |
| `POST` | `/recommendations/me/cancel-deletion` | Required | `Preference.Update` | `CancelGdprDeletionCommand` |
| `GET` | `/admin/interactions` | Required | `Interaction.Read` | `GetAdminInteractionsQuery` |
| `GET` | `/admin/metrics` | Required | `AdminDashboard.Read` | `GetMetricsQuery` |
| `POST` | `/admin/batches/refresh` | Required | `Batch.Refresh` | `RefreshSuggestionBatchCommand` |
| `GET` | `/admin/batches` | Required | `Batch.Read` | `GetSuggestionBatchesQuery` |
| `POST` | `/admin/boosts` | Required | `BoostPackage.Create` | `CreateBoostPackageCommand` |
| `DELETE` | `/admin/boosts/{id}` | Required | `BoostPackage.Delete` | `DeactivateBoostPackageCommand` |
| `POST` | `/admin/boosts/cpc` | Required | `BoostPackage.Create` | `CreateCpcBoostPackageCommand` |
| `POST` | `/admin/pins` | Required | `EditorialPin.Create` | `CreateEditorialPinCommand` |
| `DELETE` | `/admin/pins/{id}` | Required | `EditorialPin.Delete` | `DeactivateEditorialPinCommand` |
| `POST` | `/admin/seasonality` | Required | `SeasonalityRule.Create` | `CreateSeasonalityRuleCommand` |
| `GET` | `/admin/seasonality` | Required | `SeasonalityRule.Read` | `GetSeasonalityRulesQuery` |
| `DELETE` | `/admin/seasonality/{id}` | Required | `SeasonalityRule.Delete` | `DeactivateSeasonalityRuleCommand` |
| `POST` | `/admin/holidays` | Required | `HolidayCalendar.Create` | `CreateHolidayCalendarCommand` |
| `GET` | `/admin/holidays/{year}` | Required | `HolidayCalendar.Read` | `GetHolidayCalendarByYearQuery` |
| `PUT` | `/admin/entities/{kind}/{id}/photogenic` | Required | `Photogenic.Update` | `SetEntityPhotogenicCommand` |
| `POST` | `/admin/experiments` | Required | `Experiment.Create` | `CreateExperimentCommand` |
| `PUT` | `/admin/experiments/{id}/start` | Required | `Experiment.Update` | `StartExperimentCommand` |
| `PUT` | `/admin/experiments/{id}/complete` | Required | `Experiment.Update` | `CompleteExperimentCommand` |
| `GET` | `/preferences` | Required | `Preference.Read` | `GetUserPreferencesQuery` |
| `PUT` | `/preferences` | Required | `Preference.Update` | `SetUserPreferencesCommand` |

---

## Contracts

### Integration Events Published

| Event | Key (registry) | Properties |
|-------|---------------|------------|
| `PopularityScoreUpdatedIntegrationEvent` | `analytics.popularity-score.updated.v1` | EntityType, EntityId, Score, TrendingRank, ComputedAt |

### Cross-Module Consumers

| Module | Event Consumed | Purpose |
|--------|---------------|---------|
| Messaging | `PopularityScoreUpdatedIntegrationEvent` | Trending alerts (optional) |

### Service Contracts

```csharp
// None exposed — Analytics is a consumer, not a provider
// Re-engagement segment available via authenticated endpoint
```

---

## Permissions

### Permission Catalog (16 feature groups, AnalyticsAccess + SystemAccess)

| Feature | Actions | Roles |
|---------|---------|-------|
| `Interaction` | Read, Create | User, Provider, Creator, TourGuide |
| `PopularityScore` | Read | Admin |
| `Trending` | Read | Admin |
| `Recommendation` | Read | User, Provider, Creator, TourGuide |
| `Preference` | Read, Update, Delete | User, Provider, Creator, TourGuide |
| `Batch` | Read, Refresh | Admin |
| `AdminDashboard` | Read, Export | Admin, SuperAdmin |
| `ProviderDashboard` | Read, Export | Provider |
| `GuideDashboard` | Read, Export | TourGuide |
| `AuditLog` | Read, Export, Delete | Admin, SuperAdmin |
| `BoostPackage` | Read, Create, Delete | Admin |
| `EditorialPin` | Read, Create, Delete | Admin |
| `SeasonalityRule` | Read, Create, Delete | Admin |
| `HolidayCalendar` | Read, Create | Admin |
| `Photogenic` | Update | Admin |
| `Experiment` | Read, Create, Delete | Admin |

---

## Execution Plan

### Phase 1 — Bug Fixes & Compliance ✅ DONE
- [x] Remove AllowAnonymous from tracking POSTs → RequireAuthorization (W2-A)
- [x] Fix audit trail: Derive AdminUserId from ICurrentUser (already done)
- [x] Fix CQRS bypass: 15 inline handlers created (W1-D)
- [x] Fix permission granularity: 16 feature groups already existed

### Phase 2 — Scoring Interfaces ✅ DONE
- [x] `ICollaborativeScoringEngine` interface + `NoOpCollaborativeScoringEngine` registered (W2-A)
- [x] `IBlendedScoringEngine` interface + `NoOpBlendedScoringEngine` registered (W2-A)

### Phase 3 — InteractionType Extension
- [ ] Add enum values 12-16 to `InteractionType.cs`
- [ ] Add handlers for new interaction types in relevant event consumers

### Phase 4 — Collaborative Filtering Engine (~16h)
- [ ] Implement `V2CollaborativeScorer` — item-item co-occurrence with PMI + time decay
- [ ] Cache key: `"analytics:collab:{entityAId}:{entityAType}"` (HybridCache, 24h TTL)
- [ ] No database table — computed from `UserInteraction` table (last 180 days, high-signal only)
- [ ] Create `CollaborativeMatrixBuildService` (nightly 03:00 UTC)
- [ ] Implement `V2PopularityScorer` (normalized TrendingRank)
- [ ] Implement `BlendedScoringEngine` orchestrator (0.40 collab + 0.35 content + 0.25 popularity)
- [ ] Wire `UserExcludedEntity` as hard exclusion filter (BEFORE scoring)
- [ ] Fix diversity rule: MaxPerPlaceId=2 → MaxPerProviderId=3
- [ ] Update `GetRecommendationsQueryHandler` to use `IBlendedScoringEngine`

### Phase 5 — Guide Dashboard (~6h)
- [ ] Create `GuideDashboardPreComputeService` (nightly 02:00 UTC)
- [ ] Create `GetGuideDashboardQuery` + Handler
- [ ] Create `GetGuideDashboardExportQuery` + Handler
- [ ] Create `GetGuideTourBreakdownQuery` + Handler
- [ ] Add 3 endpoints to `AnalyticsEndpoints.cs`
- [ ] Add 7 new integration event handlers for guide/booking/finance guide events

### Phase 6 — GDPR Data Export (~1h)
- [ ] Update `GetUserDataExportQuery` to match `/gdpr/export` route spec
- [ ] Ensure all user data tables are included in export

### Phase 7 — Build & Verify
- [ ] `dotnet build YallaJo.sln --no-restore` → 0 errors
- [ ] Verify all 51+ endpoints return correct HTTP status codes
- [ ] Verify CQRS pipeline behaviors (validation, caching) apply to all routes

---

## File Count Estimate

| Layer | Existing | Planned New | Notes |
|-------|----------|-------------|-------|
| Domain (entities, enums) | ~19 entities | 0 new | InteractionType extension only |
| Application (commands, queries, handlers, interfaces) | ~60 files | ~25 | Collab engine, guide dashboard, validators |
| Infrastructure (services, handlers, configs) | ~50 files | ~10 | 2 new bg services, guide event handlers |
| Presentation (endpoints) | 3 files | 0 | Modifications only |
| Contracts (permissions, events) | ~5 files | 0 | |
| **Total** | **~137** | **~35** | |

---

## Cross-Module Impact

| Affected Module | Impact |
|----------------|--------|
| Booking | Consumes booking events → snapshot tables; guide slot utilization computed from booking data |
| Finance | Consumes payout events → guide revenue trend |
| Social | Consumes review/favorite/rating events → entity scoring signals |
| ContentTours | Consumes tour lifecycle events → EntityAttributeSnapshot |
| ContentPlaces | Consumes place/business lifecycle events → EntityAttributeSnapshot |
| Messaging | Produces re-engagement segments → Messaging uses for win-back campaigns |
| TourGuide-Flow | Guide dashboard requires TourGuide role; InteractionType 12-16 track guide actions |

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| Collaborative matrix compute time on large datasets | HybridCache pre-warming nightly; on-demand fallback to content-only scoring |
| DashboardCache simple key/value schema doesn't support granularity queries | Add EntityType+Granularity composite key convention in string format, e.g. `guide:{id}:monthly` |
| BlendedScoringEngine weight tuning affects recommendation quality | Make weights configurable via options/config; start with content-only (NoOp collab) |
| UserInteraction table growth | Partition by OccurredAt; anonymize records older than 365 days |
| CQRS handlers bypassing validation pipeline | Ensure all commands registered as `ICommand<T>` — `ValidationBehavior` applies globally |

---

## Future Extensions (NOT in MVP)

- [ ] Real SMS-based GDPR confirmation
- [ ] Agency analytics: aggregate guide performance under agency umbrella
- [ ] Blog interaction tracking (EntityType.Blog)
- [ ] ReviewHelpfulVote as recommendation quality signal
- [ ] MaxPerProviderId diversity rule (currently MaxPerPlaceId=2)
- [ ] A/B experiment result significance testing
- [ ] Real-time trending alerts via SignalR

---

## Implementation Notes

> Added during codebase audit — reflects actual implementation state.

1. **Endpoint count**: 51 actual (plan says 47). RecommendationsEndpoints.cs has 31 routes, AnalyticsEndpoints.cs has 18, PreferencesEndpoints.cs has 2.
2. **Event handler count**: 28 actual (plan says 20). See Inbound Handlers table above.
3. **Background service count**: 8 actual (plan says 10). CollaborativeMatrixBuildService and GuideDashboardPreComputeService not yet built.
4. **CQRS bypasses**: All 15 removed by W1-D. `RecommendationsEndpoints.cs` now uses `ISender.Send()` for all admin CRUD routes.
5. **Auth gates**: W2-A fixed 4 routes — 2 tracking POSTs + 2 admin GETs now require `RequireAuthorization()`.
6. **DashboardCache schema**: Simple `Key/ValueJson/ExpiresAt/RebuiltAt` — no EntityType/Granularity columns as plan implies. Use string key conventions like `guide:{id}:weekly`.
7. **Collaborative filtering**: NoOp registered. `V2CollaborativeScorer` not yet implemented. Recommendation engine uses content-only scoring in production.
8. **Blended scoring**: NoOp registered. `BlendedScoringEngine` not yet implemented. `GetRecommendationsQueryHandler` calls content scorer directly.
9. **GDPR export**: Exists at `GET /me/export` (not `/gdpr/export` as plan specifies). Both routes serve same purpose.
10. **Guide dashboard**: Partial — 3 guide endpoints exist but full pre-computation service not yet built.
11. **InteractionType NotInterested=10**: `DismissRecommendation=10` is the actual enum value (plan says `NotInterested=11`). Enum is 0-indexed with 12 values (0-11). Values 12-16 not yet added.
12. **ICurrentUser.UserId**: Type is `Guid?` (not `string`). Use `currentUser.UserId!.Value` to access.
