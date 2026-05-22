# Recommendations Engine — Build Plan (V1 → V3)

## TL;DR

> **Quick Summary**: Build YallaJo's recommendation engine inside the existing `Analytics` module, through to V3 (sponsored placements, A/B testing, CTR funnel, email digest, GDPR). V4 (push targeting + weather) is deferred until a push notification provider exists. Codebase verification shows the gaps document (`RECOMMENDATIONS_ENGINE_GAPS.md`) is significantly **stale** — Booking, Social, ContentTours, ContentPlaces, ContentCore, Finance, and Messaging already expose 60+ integration events and the contracts the gaps doc claimed were missing. `AppAction.Refresh` and `AppAction.Record` already exist. `Messaging.Contracts/Services/IEmailSender.cs` already exists.
>
> **Deliverables**:
> - V1 recommendation API: `/api/v1/analytics/recommendations` + 7 sibling endpoints
> - `EntityAttributeSnapshot` denormalized read model + 9 integration event handlers
> - `SuggestionBatch` aggregate + 6-hourly background refresh job
> - V1 content-similarity scorer using `Location.DistanceTo()` (Haversine) + popularity + rating
> - V1.5 quality features: MMR diversity, inventory awareness, recency boost, halal filter, subscription-gated boosts, editorial pinning
> - V2 personalization: cold-start quiz, "not interested" feedback, explainability, budget tier, family inference, personalized search, zero-result rescue
> - V2.5 Jordan-specific: seasonality (Wadi Rum/Petra/Dead Sea/Aqaba), holiday calendar (Ramadan/Eid), trip-stage detection via Tracking, multi-day itinerary planner
> - V3 marketplace + ops: sponsored placements (hybrid flat-fee + CPC auction), A/B testing, CTR funnel, position-bias correction, email digest, re-engagement segments, GDPR delete-my-data
>
> **Estimated Effort**: ~176 hours of focused solo-developer work
> **Parallel Execution**: Mostly sequential (within phases) but Phase 2 cross-module gap closure tasks (B1–B5) can run in parallel
> **Critical Path**: Phase 0 (docs) → Phase 1 (V1) → Phase 2 (gaps) → Phase 3 (V1.5) → Phase 4 (V2) → Phase 5 (V2.5) → Phase 6 (V3)

---

## Context

### Original Request
> "Create the plan to start building the engine and close all the gaps and to reach the highest version we can reach now."

The user requested a build plan to start the recommendations engine, close every gap identified in the audit, and progress to the highest reachable version.

### Reality-Check Findings

A live verification pass against the codebase revealed that `RECOMMENDATIONS_ENGINE_GAPS.md` is significantly stale. Many of the "critical/high" gaps it documents have already been resolved by other work streams that landed after the audit. The plan below is grounded in the **current** code state, not the gap document's stale view.

#### Stale Gaps (Already Resolved — Verified In Code)

| Gap doc claim | Actual state (verified) |
|---|---|
| "Booking has ZERO integration events" (GAP-2) | `Booking.Contracts/IntegrationEvents/` ships 12 events including `TourBookingCreated/Confirmed/Cancelled/Completed/PaymentExpired/Rejected`, `AvailabilitySlotCapacityChanged`, `SlotLockCreated/Released`, `JoinRequest*`. All registered in `IntegrationEventTypeRegistry` (lines 110–121). |
| "Social.Contracts is EMPTY" (GAP-5) | `Social.Contracts/IntegrationEvents/` ships 5 events including `FavoriteAddedIntegrationEvent`. Analytics already subscribes via `SocialFavoriteAddedHandler` recording `AddToFavorite` interactions. |
| "`AppAction.Refresh` is not defined" (GAP-10) | Present at `YallaJo.SharedKernel.Application/Authorization/AppAction.cs:27`. `AppAction.Record` at line 28. Comment mentions recommendations use case. |
| "Build 9 integration handlers for Analytics" (spec §1.5) | `AnalyticsIntegrationEventHandlers.cs` already implements 18+ handlers consuming Booking, Finance, Social, ContentTours, ContentPlaces, Auth, Messaging events. Most populate `PopularityScore`, `BookingSnapshot`, `PaymentSnapshot`, `UserInteraction`, or `AuditLog`. Some need extension to also write `EntityAttributeSnapshot` (V1 work). |
| "Build `RecordInteractionCommand`" (spec §1.9) | Already exists in `AnalyticsSprintHandlers.cs` with hybrid-cache dedup + `IInteractionIngestQueue` background drain. |
| "Build popularity scoring" | `PopularityScoreCalculationService` is a working `BackgroundService` with options (`PopularityScoreCalculationOptions`), tracking ratings + interactions + trending rank. |
| "Move `IEmailService` from Auth to Messaging" (WIN-3) | `Messaging.Contracts/Services/IEmailSender.cs` already exists — cleaner than Auth's version. No move needed; consume directly. |
| "Use `Location.DistanceTo()` for Haversine" (WIN-2) | Confirmed at `YallaJo.SharedKernel.Domain/ValueObjects/Location.cs:24`. |

#### Real Gaps Remaining (Active Work)

| # | Gap | Why it matters | Effort |
|---|---|---|---|
| A1 | `RecommendationCache`, `UserPreference`, `UserPreferredCategory` are marked `[Obsolete("Out of scope for Phase 1. Reserved for Phase 3.")]` and are only stub-shaped (e.g., `UserPreference` has only `PreferenceKey/PreferenceValue` strings). | Must un-obsolete and reshape to spec's data model | 3 hrs |
| A2 | `EntityAttributeSnapshot` does not exist. `EntityPopularitySnapshot` exists but is too thin (just `Score`+`TakenAt`). | New denormalized read model for scoring (name/price/rating/location/categories/etc.) | 9 hrs |
| A3 | `SuggestionBatch` aggregate does not exist. | Needed to mark batches stale without scanning individual `RecommendationCache` rows | 2 hrs |
| A4 | No scorer, no batch refresh job, no recommendation read API, no permissions, no endpoints. | The actual recommendations engine | 18 hrs |
| B1 | `ISubscriptionStatusProvider` not in `Finance.Contracts/Services/`. (Finance has subscription events + Domain entity, but no read-side contract.) | V1.5.6 subscription-gated boosts | 4 hrs |
| B2 | `Business` entity has no `IsHalal`, `HasVegetarianOptions`, `HasAlcoholFreeArea` columns. | V1.5.5 halal/dietary filter | 4 hrs |
| B3 | `Profile` entity has no `MarketingConsent` value object/fields. | V3.6 email digest opt-in | 4 hrs |
| B4 | ContentCore does not publish `EntityCategoryAssigned/Removed` events. Category links are in `content_core.EntityCategories` with composite PK `(EntityType, EntityId, CategoryId)`. | Real-time category snapshot updates (V1.5+) | 4 hrs |
| B5 | `Tracking.Contracts/IntegrationEvents/` is empty. | V2.5.4 trip-stage detection needs `LiveTrackingSessionStarted/Ended` | 4 hrs |
| C1 | No `IPushService` / FCM / APNS provider exists. `DeviceToken` entity + `NotificationChannel.Push` enum exist; nothing sends. | V4 push targeting — **deferred** | — |

### Effort Summary Comparison

| Phase | Original (gap doc) | Revised (code-grounded) | Delta | Why |
|---|---|---|---|---|
| Phase 0 (pre-flight) | — | 2 | +2 | Docs need patching |
| Phase 1 (V1) | 42 | 28 | -14 | Existing handlers + ingestion + popularity already done |
| Phase 2 (cross-module gaps) | +45 external | 20 | -25 | 70% of "external work" already done |
| Phase 3 (V1.5) | 17–24 | 20 | +3 | Inventory readable today (events exist) |
| Phase 4 (V2) | 22 | 20 | -2 | Booking/Favorite signals already wired |
| Phase 5 (V2.5) | 33 | 31 | -2 | Tracking event work counted in Phase 2 |
| Phase 6 (V3, no push) | 63 | 55 | -8 | Email service ready |
| **Total to V3** | 212 cross-team | **~176 solo** | -36 | |

---

## Work Objectives

### Core Objective

Ship a production-grade recommendations engine in the Analytics module, advancing through V1 (content + popularity), V1.5 (quality hardening), V2 (personalization), V2.5 (Jordan-specific intelligence), and V3 (marketplace + ops). V4 (push + weather) is deferred until a push notification provider is built.

### Concrete Deliverables

**Phase 1 (V1)** — Working recommendations:
- `EntityAttributeSnapshot` entity + EF config + migration
- `SuggestionBatch` aggregate + EF config + migration
- Reshaped `RecommendationCache` (`BatchId`, `Position`, nullable `UserId`)
- Reshaped `UserPreference` + `UserPreferredCategory`
- `SuggestionContext`, `EntityKind`, `EntityRef` types
- 4 repository interfaces + EF implementations
- `V1ContentSimilarityScorer` using `Location.DistanceTo()`
- `RefreshSuggestionBatchCommandHandler` with greedy diversity
- Extended `AnalyticsIntegrationEventHandlers.cs` (9+ snapshot populators)
- `SuggestionBatchRefreshJob` `BackgroundService` (6-hourly + startup bootstrap)
- 3 read queries (`GetRecommendations`, `GetSimilarTours`, `GetEntitySuggestions`)
- 7 endpoints in `Analytics.Presentation`
- Permission catalog entries (`Analytics.Recommendation.Read`, etc.)
- Hybrid-cache integration (5/30/30-min TTLs)
- Unit tests + integration test + E2E smoke

**Phase 2 (gap closure)** — Cross-module hardening:
- `Finance.Contracts/Services/ISubscriptionStatusProvider.cs` + `Finance.Infrastructure` impl
- `Business` entity halal columns + migration + event payload extension
- `Profile.MarketingConsent` value object + endpoint + migration
- `ContentCore` `EntityCategoryAssigned/Removed` integration events + outbox publish + registry
- `Tracking` `LiveTrackingSessionStarted/Ended` integration events + outbox publish + registry

**Phase 3 (V1.5)** — Quality hardening:
- MMR diversity replaces greedy caps
- Inventory awareness via extended `AvailabilitySlotCapacityChanged` handler
- Negative-review suppression query clause
- Recency boost multiplier
- Halal/dietary query filter
- `BoostPackages` table + admin/provider endpoints (subscription-gated)
- `EditorialPins` table + admin endpoint

**Phase 4 (V2)** — Personalization:
- `UserExcludedEntities` table + 90-day exclusion logic
- Onboarding quiz endpoint
- `NotInterested` interaction type
- `SignalLabels` + per-item signals in API
- Budget tier classification + filter
- Family-traveler inference
- `IUserPreferenceLookupService` consumed by `ContentTours` search (feature-flagged)
- Zero-result rescue block in search response
- `UserProfileUpdateJob` background

**Phase 5 (V2.5)** — Jordan-specific:
- `SeasonalityRules` table + scorer multiplier + admin CRUD
- `HolidayCalendar` table + scorer multiplier + 5-year seed
- Trip-stage subscriber (consumes Phase 2's Tracking events)
- `TripArcs` table + multi-day itinerary endpoint
- Photogenic flag + share-affinity multiplier

**Phase 6 (V3)** — Marketplace + ops:
- `SponsoredAuctionBids` + `SponsoredClickEvents` tables
- Vickrey second-price auction engine
- Anti-fraud (per-user dedup, dwell-time, daily budget cap)
- `Experiments` + `ExperimentAssignments` tables + `IExperimentVariantResolver`
- `SuggestionMetrics` table + 30-min aggregation job
- Position-bias propensity normalization
- `EmailDigestBackgroundService` (Mondays 09:00 user-tz)
- Re-engagement segments admin endpoint
- `DELETE /api/v1/analytics/me` GDPR endpoint + soft-delete window
- 365-day anonymization daily job

### Out of Scope

- **V4 (push targeting + weather-aware)** — Requires push notification provider (FCM/APNS) and OpenWeatherMap integration that do not exist. Move to a separate plan once Messaging push infra ships.
- **Module-renaming refactor** — The engine lives in Analytics (per spec §0 lock); we do not create a new `Recommendations` module.
- **External-team coordination** — Operates as a solo-dev plan since all modules belong to the same codebase under the same maintainer.

---

## Phase 0 — Pre-Flight (2 hrs)

**Goal**: Patch stale docs and confirm architecture decisions are locked.

| # | Task | Hrs | DoD |
|---|---|---|---|
| 0.1 | Patch `Agents/Recomendation Engine/RECOMMENDATIONS_ENGINE_GAPS.md` — strike GAP-2, GAP-5, GAP-10, WIN-3 as resolved; add a "Reality Update (post-verification)" section at top referencing this plan. | 0.5 | Doc reflects current reality; no contradictions with code. |
| 0.2 | Patch `Agents/Recomendation Engine/RECOMMENDATIONS_ENGINE_SPEC.md` §3.2 handler table — annotate which handlers exist (and need extension) vs which are new. | 0.5 | Spec accurately describes what to build vs extend. |
| 0.3 | Patch `Agents/Recomendation Engine/RECOMMENDATIONS_ENGINE_ROADMAP.md` — drop "blocked-on-Booking", "blocked-on-Social" labels; rewrite the cross-module dependency matrix to reflect what already ships. | 0.5 | Roadmap stops claiming false blockers. |
| 0.4 | Open feature branch `feat/recommendations-v1` from `main`. Verify `dotnet build YallaJo.sln` is green. Record current commit SHA in evidence folder. | 0.5 | Clean branch + green build. |

**Definition of Done**: All three docs accurate to current code state; branch ready; baseline build verified.

---

## Phase 1 — V1 Ship (28 hrs)

**Goal**: Live recommendations endpoints fed by snapshot read model, served from cache, refreshed by background job.

### 1.1 — Domain layer (4 hrs)

| # | Task | Hrs |
|---|---|---|
| 1.1.1 | Add `SuggestionContext` enum to `Analytics.Domain/Enums/SuggestionContext.cs` (values per spec §2.2: `SimilarTours=1, SimilarBusinesses=2, SimilarHotels=3, AddAMeal=10, AddAnActivity=11, WhereToStay=12, ExploreNearby=13, PersonalizedFeed=20, BecauseYouViewed=21, BecauseYouBooked=22, PostBookingAddOn=30, PostBookingFollowUp=31`). | 0.5 |
| 1.1.2 | Add `EntityRef` value object to `Analytics.Domain/ValueObjects/EntityRef.cs` (factory methods `Tour/Business/Hotel/Place`). Reuse existing `Analytics.Domain.Enums.EntityType` instead of new `EntityKind` enum (avoid duplicate). | 0.5 |
| 1.1.3 | Add `SuggestionBatch` aggregate to `Analytics.Domain/Entities/SuggestionBatch.cs` per spec §2.3 (fields: `SourceKind`, `SourceId`, `Context`, `AlgorithmVersion`, `ComputedAt`, `IsStale`, `ItemCount`; methods: `Create`, `MarkStale`, `MarkRefreshed`). | 1 |
| 1.1.4 | Add `EntityAttributeSnapshot` aggregate to `Analytics.Domain/Entities/EntityAttributeSnapshot.cs` per spec §3.1 (fields: `EntityKind`, `EntityId`, `Name`, `Slug`, `BasePriceAmount/Currency`, `SalePrice`, `AverageRating`, `ReviewCount`, `BookingCount`, `IsFeatured`, `LocationLatitude/Longitude`, `PlaceId`, `BusinessType`, `Difficulty`, `DurationMinutes`, `IsChildFriendly`, `IsAccessible`, `IsInstantBooking`, `Status`, `IsDeleted`, `CategoryIdsJson`, `CreatedAt`, `LastUpdatedAt`). | 1.5 |
| 1.1.5 | Un-obsolete `RecommendationCache`. Add `BatchId` (Guid), `Position` (int, 1-based), make `UserId` nullable, repurpose `Reason` for Signals JSON. Add factory + update methods. Add domain event `RecommendationGeneratedDomainEvent`. | 0.5 |
| 1.1.6 | Un-obsolete `UserPreference` — reshape to spec (`Guid UserId` PK, computed columns: `BudgetTier`, `IsFamilyTraveler`, `CurrentTripStage` reserved for V2.5, `LastComputedAt`). Reshape `UserPreferredCategory` (composite PK `(UserId, CategoryId)`, `PreferenceScore` decimal). | — (folded into 1.1.5) |

### 1.2 — Application layer (3 hrs)

| # | Task | Hrs |
|---|---|---|
| 1.2.1 | Repository interfaces in `Analytics.Application/Interfaces/Repositories/`: `ISuggestionBatchRepository`, `IRecommendationCacheRepository`, `IUserPreferenceRepository`, `IEntityAttributeSnapshotRepository`. | 1 |
| 1.2.2 | Scoring engine interface `IRecommendationScoringEngine` in `Analytics.Application/Scoring/`. Implementation `V1ContentSimilarityScorer` in `Analytics.Application/Scoring/V1ContentSimilarityScorer.cs` per spec §4 (formula `Score = W_category × CategoryMatch + W_price × PriceSimilarity + W_proximity × ProximityScore + W_rating × NormalizedRating + W_popularity × Log10(BookingCount+1)/4 + W_featured`). Uses existing `Location.DistanceTo()`. | 1.5 |
| 1.2.3 | `RefreshSuggestionBatchCommand` + handler in `Analytics.Application/Commands/RefreshSuggestionBatch/`. Loads candidates from `EntityAttributeSnapshots`, scores via `IRecommendationScoringEngine`, applies greedy diversity rules (spec §4.5: top 60 → ≤3/PlaceId, ≤2 per price ±5%, ≤2 per provider → top 20), stores as batch + cache rows. | 0.5 |

### 1.3 — Read API queries (3 hrs)

| # | Task | Hrs |
|---|---|---|
| 1.3.1 | `GetRecommendationsQuery` (R1, authenticated) in `Analytics.Application/Queries/GetRecommendations/`. Returns top 20 by `(BookingCount DESC, AverageRating DESC)` filtered by `UserPreferredCategory` (if set). Excludes already-booked tours. | 1 |
| 1.3.2 | `GetSimilarToursQuery` (R2, anonymous-allowed) in `Analytics.Application/Queries/GetSimilarTours/`. Reads pre-computed `RecommendationCache` rows for `(Tour, SimilarTours)` batch. Falls back to popularity if no batch yet. | 1 |
| 1.3.3 | `GetEntitySuggestionsQuery` (R3, anonymous-allowed) in `Analytics.Application/Queries/GetEntitySuggestions/`. Generic `(kind, id, context)` lookup. | 1 |

### 1.4 — Infrastructure layer (8 hrs)

| # | Task | Hrs |
|---|---|---|
| 1.4.1 | EF configurations in `Analytics.Infrastructure/Persistence/Configurations/`: `EntityAttributeSnapshotConfiguration.cs` (unique `(EntityKind, EntityId)`, indexes per spec §3.1), `SuggestionBatchConfiguration.cs`, updated `RecommendationCacheConfiguration.cs`, updated `UserPreferenceConfiguration.cs`, new `UserPreferredCategoryConfiguration.cs`. | 1.5 |
| 1.4.2 | Add `DbSet<EntityAttributeSnapshot> EntityAttributeSnapshots`, `DbSet<SuggestionBatch> SuggestionBatches` to `AnalyticsDbContext.cs`. | 0.25 |
| 1.4.3 | Generate EF migration `AddRecommendationsV1Tables`. Verify SQL preview, apply to dev DB. | 0.5 |
| 1.4.4 | Repository implementations in `Analytics.Infrastructure/Repositories/`: `SuggestionBatchRepository`, `RecommendationCacheRepository`, `UserPreferenceRepository`, `EntityAttributeSnapshotRepository`. | 2 |
| 1.4.5 | **Extend** `AnalyticsIntegrationEventHandlers.cs`: current `ContentToursCreatedHandler`/`ContentToursDeletedHandler`/`ContentPlacesCreatedHandler`/`ContentPlacesDeletedHandler` only init/delete `PopularityScore`. Extend them to upsert `EntityAttributeSnapshot` rows. Add new handlers for `TourUpdatedIntegrationEvent`, `TourSuspendedIntegrationEvent`, `TourReinstatedIntegrationEvent`, `BusinessCreatedIntegrationEvent`, `BusinessUpdatedIntegrationEvent`, `BusinessApproved/Rejected/Suspended/Reinstated`, `PlaceCreatedIntegrationEvent`, `PlaceUpdatedIntegrationEvent`. | 3 |
| 1.4.6 | `SuggestionBatchRefreshJob` in `Analytics.Infrastructure/BackgroundServices/`. `BackgroundService` with 30s startup delay, full bootstrap on first run (cross-context one-time read of `ContentTours.Tours`, `ContentPlaces.Businesses`, `ContentPlaces.Places`, `ContentCore.EntityCategories` — acceptable per spec §3.2 because it's infra-layer, read-only, denormalizes to own schema), 6-hour incremental refresh loop with priority queue (stale batches > top-viewed > round-robin). | 0.75 |

### 1.5 — Presentation layer (3 hrs)

| # | Task | Hrs |
|---|---|---|
| 1.5.1 | `Analytics.Contracts/Authorization/AnalyticsFeatures.cs` extension: add `Recommendation`, `Preference`, `Interaction`, `Batch` constants. Extend `AnalyticsPermissionCatalog.cs`: `Analytics.Recommendation.Read`, `Analytics.Preference.Read`, `Analytics.Preference.Update`, `Analytics.Batch.Read`, `Analytics.Batch.Refresh`. | 0.5 |
| 1.5.2 | New endpoint file `Analytics.Presentation/Endpoints/Recommendations/RecommendationsEndpoints.cs` wiring 5 routes: `GET /api/v1/analytics/recommendations`, `GET /api/v1/analytics/recommendations/similar/{tourId:guid}`, `GET /api/v1/analytics/recommendations/for/{kind}/{id:guid}`, `POST /api/v1/analytics/admin/batches/refresh`, `GET /api/v1/analytics/admin/batches`. | 1.5 |
| 1.5.3 | New endpoint file `Analytics.Presentation/Endpoints/Preferences/PreferencesEndpoints.cs`: `GET /api/v1/analytics/preferences`, `PUT /api/v1/analytics/preferences`. Wire into `AnalyticsEndpoints.cs`. | 1 |

### 1.6 — Caching (folded into 1.5 — 2 hrs)

Hybrid-cache integration per spec §8: `ct:analytics:recs:user:{userId}:lang:{lang}` (5 min), `ct:analytics:recs:similar:{tourId}:lang:{lang}` (30 min), `ct:analytics:recs:{kind}:{id}:ctx:{context}:lang:{lang}` (30 min). Tag-based invalidation on snapshot update + batch refresh.

### 1.7 — Tests + smoke (5 hrs)

| # | Task | Hrs |
|---|---|---|
| 1.7.1 | Unit tests for `V1ContentSimilarityScorer` — score formula, diversity caps, anti-cannibalization, null-location handling. | 1.5 |
| 1.7.2 | Integration test: publish `TourCreatedIntegrationEvent` via outbox → wait for inbox processing → assert `EntityAttributeSnapshot` row exists with correct fields. | 1 |
| 1.7.3 | Integration test: `GetSimilarToursQuery` returns ≥ 10 results with monotonically decreasing scores for a seeded scenario. | 1 |
| 1.7.4 | E2E smoke (manual or scripted): seed 50 tours + 30 businesses + 10 places. Call `GET /recommendations/similar/{tourId}` → assert 10–20 results with non-zero scores and signals. Call `GET /recommendations` for an authenticated user with preferred categories → assert filtered output. | 1 |
| 1.7.5 | Open PR. CI green. Code review. | 0.5 |

### Phase 1 Definition of Done

1. Migration applied; `EntityAttributeSnapshot` rows exist for every active `Tour`/`Business`/`Place`.
2. `GET /recommendations/similar/{tourId}` returns ≥ 10 results for a real tour with shared categories.
3. `POST /interactions` still works (no regression).
4. `GET /recommendations` for an authenticated user returns 20 results filtered by their preferred categories.
5. `SuggestionBatchRefreshJob` writes batches on startup and refreshes every 6 hours.
6. Cache hit rate > 90% on repeated reads of the same `(source, context)` pair.
7. PR merged; build green; `dotnet test` green.

---

## Phase 2 — Cross-Module Gap Closure (20 hrs)

**Goal**: Eliminate every "blocked-on-X" label by shipping the small contracts/columns/events needed by V1.5+.

Tasks B1–B5 are independent and can run in any order (or in parallel by different worker agents).

### B1 — Finance: `ISubscriptionStatusProvider` (4 hrs)

| Sub-task | Detail |
|---|---|
| B1.1 | Create `Finance.Contracts/Services/ISubscriptionStatusProvider.cs` with method `Task<bool> HasActiveSubscriptionAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default)`. |
| B1.2 | Create `Finance.Contracts/Services/SubscriptionTier.cs` enum: `Basic = 1, Promotion = 2, Enterprise = 3`. |
| B1.3 | Implement `SubscriptionStatusProvider` in `Finance.Infrastructure/Services/` reading from `FinanceDbContext.Subscriptions` where `Status = Active AND UserId = userId AND TierMatches(tier)`. |
| B1.4 | Register in `Finance.Infrastructure.DependencyInjection.cs`. |
| B1.5 | Unit test against in-memory `Subscription` rows. |

### B2 — ContentPlaces: Halal/Dietary Columns (4 hrs)

| Sub-task | Detail |
|---|---|
| B2.1 | Add `bool? IsHalal`, `bool? HasVegetarianOptions`, `bool? HasAlcoholFreeArea` to `Business` entity + `Update()` method overload. |
| B2.2 | Update `BusinessConfiguration` to map new columns. |
| B2.3 | Generate migration `AddBusinessDietaryFlags`. |
| B2.4 | Extend `BusinessCreatedIntegrationEvent` and `BusinessUpdatedIntegrationEvent` payloads with new fields. |
| B2.5 | Update Analytics handler `BusinessCreated/UpdatedHandler` (created in Phase 1.4.5) to copy fields into `EntityAttributeSnapshot`. |
| B2.6 | Add columns to `EntityAttributeSnapshot` schema (Phase 1.4.1) — backport via additional migration. |

### B3 — Accounts: `MarketingConsent` (4 hrs)

| Sub-task | Detail |
|---|---|
| B3.1 | Create `Accounts.Domain/ValueObjects/MarketingConsent.cs`: `record MarketingConsent(bool EmailDigest, bool PushNotifications, bool ReEngagementCampaigns, DateTime? LastUpdatedUtc)`. |
| B3.2 | Add `MarketingConsent? MarketingConsent { get; private set; }` to `Profile` + method `UpdateMarketingConsent(MarketingConsent consent)`. |
| B3.3 | Update `ProfileConfiguration` to map (`OwnsOne` since it's a value object). |
| B3.4 | Generate migration `AddProfileMarketingConsent`. |
| B3.5 | New endpoint `PUT /api/v1/accounts/me/marketing-consent` in `Accounts.Presentation/Endpoints/Profiles/`. |
| B3.6 | `GET /api/v1/accounts/me/marketing-consent` for read. |

### B4 — ContentCore: Entity-Category Events (4 hrs)

| Sub-task | Detail |
|---|---|
| B4.1 | Create `ContentCore.Contracts/IntegrationEvents/EntityCategoryAssignedIntegrationEvent.cs` (payload: `EntityType`, `EntityId`, `CategoryId`, `AssignedAt`). |
| B4.2 | Create `EntityCategoryRemovedIntegrationEvent.cs` (payload: `EntityType`, `EntityId`, `CategoryId`, `RemovedAt`). |
| B4.3 | Register both in `IntegrationEventTypeRegistry` (`content-core.entity-category.assigned.v1`, `content-core.entity-category.removed.v1`). |
| B4.4 | Raise domain events from `EntityCategory.Add/Remove` and route to outbox via existing handler pipeline in ContentCore. |
| B4.5 | New Analytics handler `EntityCategoryAssignedHandler` + `EntityCategoryRemovedHandler` updating `EntityAttributeSnapshot.CategoryIdsJson`. |

### B5 — Tracking: Live Session Events (4 hrs)

| Sub-task | Detail |
|---|---|
| B5.1 | Create `Tracking.Contracts/IntegrationEvents/LiveTrackingSessionStartedIntegrationEvent.cs` (`SessionId, UserId, TourBookingId, StartedAt`). |
| B5.2 | Create `LiveTrackingSessionEndedIntegrationEvent.cs` (`SessionId, UserId, EndedAt, Reason`). |
| B5.3 | Register in `IntegrationEventTypeRegistry`. |
| B5.4 | Raise + outbox-publish from `LiveTrackingSession.Start/End` methods in Tracking.Domain. |
| B5.5 | (No Analytics handler yet — wired in Phase 5.3.) |

### Phase 2 Definition of Done

1. `SubscriptionStatusProvider.HasActiveSubscriptionAsync(userId, Promotion)` returns correct boolean against existing Subscription rows.
2. `Business` snapshots include `IsHalal/HasVegetarianOptions/HasAlcoholFreeArea` flags after upstream event flows.
3. `PUT /accounts/me/marketing-consent` persists consent; `GET` reads it back.
4. Adding/removing a category on a Tour fires the new ContentCore event and updates `EntityAttributeSnapshot.CategoryIdsJson` within 30 seconds.
5. Starting/ending a `LiveTrackingSession` fires the events (no handler yet — that's Phase 5).
6. All new events registered in `IntegrationEventTypeRegistry`.

---

## Phase 3 — V1.5 Quality Hardening (20 hrs)

| # | Feature | Hrs |
|---|---|---|
| 3.1 | **MMR diversity** in scorer. λ=0.7 default, per-context overrides in `SuggestionContextWeights` table. Replaces greedy caps from Phase 1. Same-provider anti-cannibalization tightened (exclude entirely). | 4 |
| 3.2 | **Inventory awareness**: extend existing `BookingAvailabilitySlotCapacityChangedHandler` to maintain `EntityAttributeSnapshot.UpcomingCapacity` + `UpcomingBookings` (aggregate over slots per Tour). Scorer applies inventoryFactor: `>=0.95 → exclude, >=0.90 → 0.3, >=0.75 → 0.7, else 1.0`. | 4 |
| 3.3 | **Negative-review suppression**: `WHERE NOT (AverageRating < 3.0 AND ReviewCount >= 5)` in candidate query. New entities (< 5 reviews) NOT excluded. | 1 |
| 3.4 | **Recency boost**: multiplier from `EntityAttributeSnapshot.CreatedAt` (`<=7d → 1.30, <=14d → 1.20, <=30d → 1.10, else 1.00`). | 1 |
| 3.5 | **Halal/dietary filter**: `?halalOnly=true` query param. Auto-on when `Accept-Language: ar-*`. Uses Phase 2.B2 fields. | 3 |
| 3.6 | **Subscription-gated boost packages**: `analytics.BoostPackages` table + `POST /api/v1/analytics/provider/boosts` (checks `ISubscriptionStatusProvider`, returns 402 if no Promotion subscription) + `POST /admin/boosts` (no check) + `DELETE /admin/boosts/{id}` + `GET /admin/boosts?providerId=X`. Scorer multiplies finalScore by boost.Multiplier with linear decay. | 5 |
| 3.7 | **Editorial pinning**: `analytics.EditorialPins` table + `POST /admin/pins` + `DELETE /admin/pins/{id}`. `GetRecommendations` injects pinned items at specified `Position` regardless of organic score. UI badge: "Editor's Choice". | 2 |

### Phase 3 Definition of Done

1. Diversity is measurably better — no `(PlaceId, Provider)` repeats exceed configured caps in any 20-result response.
2. Fully-booked tours (≥95% capacity) are excluded from recommendations.
3. Tours under 3.0 stars with 5+ reviews never appear.
4. New tours (≤30 days) appear more often.
5. `?halalOnly=true` returns only entities with `IsHalal=true`.
6. Provider without active Promotion subscription cannot create a boost package (HTTP 402).
7. Pinned items appear at their pin position regardless of organic score.

---

## Phase 4 — V2 Personalization (20 hrs)

**Trigger**: ≥ 10k `UserInteraction` events accumulated, OR run synthetically for development.

| # | Feature | Hrs |
|---|---|---|
| 4.1 | **Cold-start onboarding quiz**: 5 swipe cards on first login. New endpoint `POST /api/v1/analytics/onboarding/responses` (payload: `responses[] = { entityKind, entityId, interested }`). Each `interested=true` recorded as `Bookmark` interaction weight 2.5; `interested=false` as `NotInterested` (new enum value `7`). Trigger immediate profile update. | 5 |
| 4.2 | **"Not interested" feedback**: Click X → (a) insert `analytics.UserExcludedEntities` row with 90-day expiry, (b) record `NotInterested` interaction weight -2.0, (c) trigger profile updater to shift vector away. `GetRecommendations` LEFT JOINs and excludes matched rows. | 4 |
| 4.3 | **Explainability**: `RecommendationCache.Reason` stores `Signals` JSON array. UI renders via `Analytics.Application/Localization/SignalLabels.cs` (`nearby`, `similar-price`, `high-rating`, `category-match`, `popular-with-similar-users`, `matches-your-budget`, `family-friendly`, `recently-listed`). API response includes `signals[]` per item. | 2 |
| 4.4 | **Budget tier classification**: `UserPreference.BudgetTier` enum (Budget/Mid/Luxury/Unknown). Computed from `avgInteractedPrice` (`<30 → Budget, <100 → Mid, >=100 → Luxury`). Filter recs to ±1 tier. Override `?showAllPrices=true`. New migration. | 3 |
| 4.5 | **Family / kid-friendly inference**: Compute `UserPreference.IsFamilyTraveler` (`true` if any booking has `MinAge ≤ 12` or `ParticipantType=Child/Infant`). Multiplier 1.3× for `IsChildFriendly=true`; 0.7× for `MinAge > 16` when family flag is on. | 2 |
| 4.6 | **Personalized search ranking**: Expose `IUserPreferenceLookupService` from `Analytics.Application/Interfaces/` consumed by `ContentTours.Application.SearchToursQueryHandler` behind feature flag `analytics.FeatureFlags.PersonalizedSearch`. Adjusts final ordering by preference vector overlap. | 3 |
| 4.7 | **Zero-result search rescue**: `SearchToursQueryHandler` returns 0 hits → response includes `rescue: { items: [...] }` from `GetRecommendations` (popularity feed for anonymous). | 1 |

**`UserProfileUpdateJob`** background — folded into 4.2 + 4.4 + 4.5. Runs nightly (cron) + on-demand for opted-in users. Aggregates last-90-days `UserInteractions` into `UserPreference` vector.

### Phase 4 Definition of Done

1. New user completes quiz → `UserPreference` populated → next recs reflect choices.
2. Clicking "Not interested" hides the item for 90 days and shifts subsequent recs away.
3. Every recommendation item returns ≥ 1 signal label.
4. User with budget history of $20–40 tours no longer sees $200 tours by default.
5. Family-bookers see kid-friendly results promoted.
6. Search results re-ranked by preference vector (feature-flag on).
7. Zero-result search returns rescue items.

---

## Phase 5 — V2.5 Jordan-Specific Intelligence (31 hrs)

| # | Feature | Hrs |
|---|---|---|
| 5.1 | **Seasonality boost table**: `analytics.SeasonalityRules (Id, PlaceId, MonthRange, Multiplier)`. Seeded for Wadi Rum (winter boost 1.30× Nov–Mar, summer 0.60× Jun–Sep), Dead Sea (spring/fall 1.40× Mar–May, 1.30× Sep–Nov), Petra (1.20× Mar–May, 1.20× Sep–Nov), Aqaba (1.30× Dec–Feb). Hot-reloaded hourly. Admin CRUD endpoints. Scorer applies `score *= GetSeasonMultiplier(candidate.PlaceId, currentMonth)`. | 4 |
| 5.2 | **Religious / holiday calendar**: `analytics.HolidayCalendar (Id, HolidayName, StartDate, EndDate, Year, BoostRulesJson)`. Pre-populate Ramadan, Eid al-Fitr, Eid al-Adha, Christmas, Easter for next 5 years (Islamic dates shift annually — use a lookup table). Admin-editable. Scorer multiplies during active dates per rule. | 4 |
| 5.3 | **Trip-stage awareness**: New Analytics handler subscribes to Phase 2.B5's `LiveTrackingSessionStarted/Ended` events. Sets `UserPreference.CurrentTripStage` (Pre/JustLanded/MidTrip/LastDay/PostTrip — detection logic per roadmap V2.5.4 table). Daily background job recomputes for inactive users. Scorer applies stage-aware filtering (e.g., mid-trip favors nearby food/coffee, not new tour bookings). | 4 |
| 5.4 | **Multi-day itinerary planner**: `GET /api/v1/analytics/itinerary?fromDate=&toDate=&startLocation={lat,lng}&interests=`. Pre-defined "trip arc templates" in `analytics.TripArcs` table (initial 3: Amman-only, Petra-focused, Full Jordan: Amman→Dead Sea→Petra→Wadi Rum→Aqaba). Match user's date range + interests to closest template; per-day filtered to that day's geographic cluster; logistics hints (travel time, distance) from inter-day distance. Response includes per-day suggestions grouped + logistics summary. | 16 |
| 5.5 | **Photo-worthy boost**: `IsPhotogenicHotspot` bool added to `EntityAttributeSnapshot`. Admin endpoint `PUT /admin/entities/{kind}/{id}/photogenic`. Snapshot updater also reads admin-set source flag. For users with high `Share` interaction count (>5 in last 30d), multiplier 1.4× on photogenic items. | 3 |

### Phase 5 Definition of Done

1. In December, Wadi Rum gets a 1.30× boost; in July it gets 0.60×.
2. During Ramadan dates, family-suitable evening tours are boosted; alcohol-related items suppressed.
3. User in active `LiveTrackingSession` sees Mid-trip recommendations.
4. `GET /itinerary?fromDate=...&toDate=...` returns a coherent multi-day plan grouped by geographic cluster.
5. Frequent sharers see Insta-friendly spots promoted.

---

## Phase 6 — V3 Marketplace + Cross-Channel + Ops (55 hrs)

| # | Feature | Hrs |
|---|---|---|
| 6.1 | **Sponsored placements (hybrid)**: Extend `BoostPackages` for `Type = SponsoredSlot`. New tables `analytics.SponsoredAuctionBids (BoostPackageId, Context, SourceKind, SourceId, BidPerClick, DailyBudgetCap, SpentToday, StartsAt, ExpiresAt, Status)` + `analytics.SponsoredClickEvents (BidId, UserId, SessionId, SourceKind, SourceId, Position, ChargedAmount, ClickedAt)`. Auction at request time: filter active bids → compute quality-adjusted bid (`bid × baseQuality × (1 + log10(providerCTR + 0.01))`) → second-price (Vickrey). Click charging async via event insert + nightly Finance batch. Anti-fraud: per-`(UserId/SessionId, BidId)` charge cap 1/day, dwell-time check < 2s = no charge, daily budget cap. Flat-fee Bronze/Silver track reuses Phase 3.6 BoostPackages with `BillingMode = FlatFee`. | 15 |
| 6.2 | **A/B testing framework**: `analytics.Experiments (Id, Name, Status, StartsAt, ExpiresAt, TrafficPercent, VariantsJson)` + `analytics.ExperimentAssignments (UserId, ExperimentId, VariantName, AssignedAt, PK (UserId, ExperimentId))`. Stable-hash bucketing: `Hash(UserId, ExperimentId) % 100 < TrafficPercent` → variant via consistent hashing. `IExperimentVariantResolver.GetVariantAsync(userId, experimentName, ct)` consumed by scorer (variant chooses scoring params). | 12 |
| 6.3 | **CTR / conversion funnel tracking**: `analytics.SuggestionMetrics (Id, BatchId, RecommendationCacheId, UserId?, Position, Stage, OccurredAt, SessionId, ExperimentVariant?)`. Stages: `Impression`, `Click`, `Booking`. 30-min background aggregation job. Admin endpoint `GET /api/v1/analytics/admin/metrics?context=X&from=...&to=...` returns CTR per `(Context, Position)`, conversion per `(Context, Position)`, per-variant CTR, per-provider CTR. | 8 |
| 6.4 | **Position bias correction**: Propensity table (`1.00, 0.55, 0.40, 0.25, 0.13` at positions 1/2/3/5/10, linear interp between). Used in `UserProfileUpdateJob`: `correctedClickWeight = rawClickWeight / propensity(position)`. Normalized weights feed back into preference vector. | 4 |
| 6.5 | **Email digest BackgroundService**: New `EmailDigestBackgroundService` in `Analytics.Infrastructure/BackgroundServices/`. Mondays 09:00 user-tz (use `Profile.Country` + TimeZoneInfo). For each user with `MarketingConsent.EmailDigest = true`, call `GetRecommendations` top 5, render via Razor template, send via `Messaging.Contracts/Services/IEmailSender.SendAsync`. | 8 |
| 6.6 | **Re-engagement segments**: `GET /api/v1/analytics/admin/segments?rule=X&entityKind=Tour&entityId=Y`. Returns `{ userIds: [...], count: N }`. Predefined rules: "viewed X but no booking in 7 days", "booked once 90+ days ago no return", "started but abandoned at checkout", "≥4-star review but no return booking in 60 days". Output consumable by Messaging campaign pipeline (which exists). | 5 |
| 6.7 | **GDPR delete-my-data**: `DELETE /api/v1/analytics/me`. 30-day soft-delete window (cancellable). After window: hard-delete `UserInteractions`, `UserPreference`, `UserPreferredCategory`, `UserExcludedEntities`, `ExperimentAssignments` where `UserId = currentUser`. Anonymize `SuggestionMetrics` (`UserId = NULL`). Anonymize/delete `RecommendationCache` rows. Audit log entry. Daily 365-day anonymization job runs in parallel: `UPDATE UserInteractions SET UserId = NULL WHERE OccurredAt < UtcNow - 365d`. Opt-out toggle in account settings (`Profile.EnablePersonalization`) — when false, recs fall back to popularity-only. | 3 |

### Phase 6 Definition of Done

1. Provider can bid on Gold slots; auction picks winner at request time within < 50ms.
2. A/B experiment can be created via admin endpoint; users bucket deterministically.
3. CTR by position is observable in admin metrics endpoint within 1 hour of impressions.
4. Click signals are propensity-corrected before feeding profile updater.
5. Opted-in users receive Monday email with top 5 recommendations.
6. Admin can query "users who viewed Petra in last 7 days but didn't book".
7. User can hit `DELETE /me` and all PII goes away within 30 days; aggregate counts survive.
8. Opt-out toggle disables personalization without affecting global popularity.

---

## Phase 7 — V4 (Deferred — Out of Scope)

Requires push notification infrastructure (`IPushService`, FCM/APNS provider, BackgroundService poller dispatching from `Notifications` table) that does NOT exist today. Estimated effort to build push infra: ~30 hours (separate Messaging-module plan). Once that lands:

| # | Feature | Hrs |
|---|---|---|
| 7.1 | **Push notification targeting** (V3.7 original) | 7 |
| 7.2 | **Weather-aware re-ranking** (V2.5.1 original; deferred again) | 8 |

These are intentionally **not part of this plan**. A separate plan should be created when push infra ships.

---

## Risk Register

| # | Risk | Probability | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Existing `AnalyticsIntegrationEventHandlers` writes only audit log for `AvailabilitySlotCapacityChanged`; extending to also update inventory snapshot risks regressing audit behavior. | Med | Med | Add new fields/calls without modifying existing logic. Add regression test covering audit-log emission. |
| R2 | Un-obsoleting `RecommendationCache`/`UserPreference` may break migrations if there is existing data with old schema. | Low | High | Verify no production rows exist before reshaping (these were placeholders). If rows exist, write data migration with safe defaults. |
| R3 | Cross-context bootstrap of category links pulls from `ContentCore.EntityCategories` at job start — could be slow with many entities. | Low | Low | Batch read in 1000-row chunks. One-time on startup only; subsequent updates via Phase 2.B4 events. |
| R4 | `EntityCategories` table has composite PK; ContentCore-side outbox publishing per-row may be chatty. | Low | Med | Batch publish on commit (domain event raised once per `EntityCategory.Add/Remove` call). Inbox dedupes by message ID. |
| R5 | Sponsored CPC auction at request time may slow `GetRecommendations` if poorly cached. | Med | High | Cache auction winners per `(Context, Source, 5-min window)`. Run auction async pre-request when feasible. Measure p95 latency in Phase 6 acceptance. |
| R6 | Phase 5.4 multi-day itinerary (16 hrs) is largest single deliverable — scope creep risk. | Med | Med | Hard-cap to 3 trip arc templates (Petra-only, Wadi Rum, full Jordan). Defer "custom arcs" to a hypothetical V3.5. |
| R7 | Trip-stage logic in Phase 5.3 depends on Tracking events shipped in Phase 2.B5. | Low | Low | Phase 2 ships events; Phase 5 subscribes. Decoupled timelines; verify Phase 2 acceptance before starting Phase 5.3. |
| R8 | Push infra never ships → V4 forever deferred. | Med | Low | Acceptable. V3 alone covers email digest + segments. Push is a future enhancement. |
| R9 | `EntityAttributeSnapshot.CategoryIdsJson` may grow large for entities with many categories. | Low | Low | Cap to 10 categories per entity (matches business rule). Use NVARCHAR(500) — sufficient for 10 GUIDs as JSON. |
| R10 | First-time bootstrap of `EntityAttributeSnapshot` could take > 30s if many entities exist. | Low | Low | Run in background with progress logging; do NOT block startup. Recommendations endpoints return 503 with `Retry-After` until bootstrap completes. |

---

## Execution Order (Sequenced)

The following order **minimizes blockers** and **front-loads value**:

1. **Phase 0** (2h) — Pre-flight; patch docs.
2. **Phase 1** (28h) — V1 ship. Real recommendations live.
3. **Phase 2** (20h) — Close cross-module gaps. Order: B1 (Finance) → B4 (ContentCore events) → B2 (ContentPlaces halal) → B3 (Accounts consent) → B5 (Tracking events). All five are independent and could be delegated to parallel worker agents.
4. **Phase 3** (20h) — V1.5 quality. Now unblocked (halal columns, subscription contract exist).
5. **Phase 4** (20h) — V2 personalization. Can begin once ≥ 10k interactions accrue, or run in parallel with synthetic interaction generator for dev.
6. **Phase 5** (31h) — V2.5 Jordan-specific. Trip-stage handler depends on Phase 2.B5 events.
7. **Phase 6** (55h) — V3 marketplace + ops + email digest. Largest phase, no remaining blockers.
8. **Phase 7** (15h) — V4 *(deferred until push infra ships)*.

### Cumulative Milestones

| Milestone | Cumulative hours |
|---|---|
| End of Phase 1 (V1 live) | 30 |
| End of Phase 2 (gaps closed) | 50 |
| End of Phase 3 (V1.5) | 70 |
| End of Phase 4 (V2 personalization) | 90 |
| End of Phase 5 (V2.5 Jordan intel) | 121 |
| **End of Phase 6 (V3 — target ship)** | **176** |
| End of Phase 7 (V4 — if push ships) | 191 |

---

## Architecture Decisions (Locked)

| # | Decision | Rationale |
|---|---|---|
| 1 | Module placement: inside `Analytics` module (NOT new `Recommendations` module) | Per `YallaJo.md` Phase 4 spec; reuses 5 existing entities + handlers + ingestion pipeline; no parallel structures |
| 2 | Cross-module data access: `EntityAttributeSnapshot` denormalized read model populated by integration event handlers | Per spec §3; matches existing pattern (e.g., `Place.TourCount` denormalized). No cross-context EF reads at request time. |
| 3 | Location handling: `Location` value object + `Location.DistanceTo()` Haversine | Already implemented in SharedKernel. Save 1h by not rebuilding. |
| 4 | Booking signals (V1): use real `BookingCount` field as popularity proxy, real `Booking.Confirmed/Completed` events for richer signals (already wired) | Booking events already exist — gap doc was wrong on this. |
| 5 | `AppAction.Refresh` + `AppAction.Record`: already exist | Verified at `AppAction.cs:27-28`. No additions needed. |
| 6 | Email service: use `Messaging.Contracts/Services/IEmailSender` | Already exists in Messaging.Contracts; no need to move Auth's interface. |
| 7 | Category match (V1): bootstrap from `ContentCore.EntityCategories` cross-context read at job startup; real-time updates via Phase 2.B4 events | Pragmatic exception per spec §3.2 (background job, infra layer, read-only). |
| 8 | Snapshot soft-delete: `IsDeleted=1` instead of hard-delete | Allows post-mortem analytics; matches existing pattern. |
| 9 | Sponsored placements model (V3): hybrid flat-fee Bronze/Silver + CPC auction Gold | Already specified in roadmap §V3.2. |
| 10 | GDPR: 30-day soft-delete window before hard execution | Already specified in roadmap §V3.9. |
| 11 | V4 (push + weather): deferred until push infra ships | No push provider exists; defer to separate plan. |

---

## Open Questions (Resolve Before Phase 1 Starts)

These questions should be confirmed before execution begins. None block Phase 0.

1. **`InteractionType` enum extension for V2**: V2 needs `NotInterested = 7`. Current enum (referenced in `RecordInteractionCommandValidator`) must be examined for next free value. Question: is the enum stored as `tinyint` or `nvarchar`? Migration-safe to add a new value?
2. **Anonymous interaction rate-limiting**: Current `RecordInteractionCommand` accepts nullable `UserId`. Spec §6.4 specifies 100/min per IP. Confirm where this should be enforced — endpoint middleware vs handler vs reverse proxy.
3. **Sponsored auction billing flow**: Finance team (= same maintainer) must support per-click invoicing in addition to recurring subscription billing. Confirm Finance is comfortable receiving per-click charge events from Analytics outbox, or whether a separate billing adapter is needed.
4. **Trip-stage event granularity**: Should `LiveTrackingSession` events fire on every location ping or just on session start/end? V2.5 only needs start/end. Confirm Phase 2.B5 scope.
5. **Email digest sender identity**: From which address should the digest send? Reuse existing Auth/Messaging Gmail config or set up a marketing-specific sender? Defer to Phase 6.5 implementation.

---

## Definition of Done — Overall Plan

The plan is complete when:

1. ✅ All Phase 6 acceptance criteria pass.
2. ✅ `GET /api/v1/analytics/recommendations` p95 latency < 200ms.
3. ✅ Snapshot freshness < 1 minute lag from upstream entity updates (95th percentile).
4. ✅ `dotnet test` green; new test count > 50.
5. ✅ Three docs in `Agents/Recomendation Engine/` are updated to reflect production reality.
6. ✅ V4 plan exists (separate `.sisyphus/plans/recommendations-engine-v4.md` file) ready for execution when push infra ships.
7. ✅ User can: get personalized recommendations, see explanations, opt out, delete data, receive a Monday email digest, see editorial pins, see sponsored placements clearly labeled.

---

## Notes on Delegation

This plan is designed for execution via the YallaJo Sisyphus worker pattern. Each phase is a natural delegation unit. Within Phase 1, sub-sections 1.1 / 1.2 / 1.3 / 1.4 / 1.5 / 1.7 are sequential (depend on prior). Within Phase 2, B1–B5 are fully parallel. Within Phase 3, 3.1 / 3.2 / 3.5 / 3.6 / 3.7 can be parallelized; 3.3 + 3.4 are trivial query/multiplier additions.

To delegate Phase 1.1 (Domain layer):
```
task(category="deep", load_skills=[], description="V1 Recommendations Domain Layer",
     prompt="Implement Phase 1.1 of .sisyphus/plans/recommendations-engine-v1-to-v3.md (1.1.1 through 1.1.6). 6 hrs estimate.",
     run_in_background=false)
```

To delegate Phase 2 cross-module gaps in parallel (5 workers):
```
task(category="quick", ...) × 5 for B1-B5
```

---

*This plan supersedes the effort tables in `RECOMMENDATIONS_ENGINE_SPEC.md` and `RECOMMENDATIONS_ENGINE_ROADMAP.md` for solo-developer execution. Architectural decisions remain locked.*
