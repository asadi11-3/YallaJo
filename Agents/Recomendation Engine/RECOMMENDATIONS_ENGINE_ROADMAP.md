# Recommendations Engine — Feature Roadmap (V1.5 → V4)

> **Companion to** `RECOMMENDATIONS_ENGINE_SPEC.md` (V1 ship plan).
> Updated after 6-agent codebase verification — see `RECOMMENDATIONS_ENGINE_GAPS.md` for what changed.
>
> **Module location**: All work lives inside `Analytics` module (per V1 architecture decision).

---

## Roadmap Summary (revised after verification)

| Phase | Recs hrs | External hrs | Trigger to start |
|---|---|---|---|
| **V1** | **42** | 0 | Now (approved) |
| **V1.5** | **17** | +8 (ContentPlaces + Finance) | V1 build green + Finance ships `ISubscriptionStatusProvider` |
| **V2** | **22** | +4 (Social) | ≥ 10k `UserInteraction` events accumulated |
| **V2.5** | **33** | 0 | Tourism season approach (Q1 each year) |
| **V3** | **63** | +4 (Accounts marketing consent) | 6-12 months post V1 |
| **V4** | TBD | +30 (Messaging push infra) | Messaging team ships push provider |
| **Recs team total** | **177** | | |
| **Cross-team total** | | **+46** | across 5 teams |

---

## V1.5 — Quality Hardening (17 hrs unblocked, +7 when external work lands)

### V1.5.1 Diversity Enforcement (MMR) — 4 hrs

Replace V1 greedy diversity rules with proper Maximal Marginal Relevance.

```
selected = []
remaining = scored_candidates_sorted_desc
while len(selected) < 20 and remaining:
    best = argmax(c in remaining: λ × score(c) - (1-λ) × max_similarity(c, selected))
    selected.append(best); remaining.remove(best)

λ = 0.7 (relevance vs diversity, tunable per context, stored in SuggestionContextWeights)

Similarity(a, b):
  Same PlaceId        → 0.40
  Same provider       → 0.30
  Within ±5% price    → 0.20
  Same category set   → 0.10
```

Anti-cannibalization: when source is a Tour, exclude all Tours by source's `CreatedByUserId` entirely (not just demote).

### V1.5.2 Inventory Awareness — **BLOCKED ON BOOKING TEAM**

Status: Booking module currently publishes ZERO integration events. `BookingCapacityChangedIntegrationEvent` does NOT exist. Booking team must build:
- `Booking.Contracts/IntegrationEvents/BookingCapacityChangedIntegrationEvent.cs`
- Domain event on `AvailabilitySlot` aggregate (raised on every reservation/cancellation)
- Domain event handler that writes to outbox
- Registry entry: `booking.capacity-changed.v1`

**Booking team effort**: 12-16 hrs (build full integration event surface — they have outbox infrastructure but zero published events today).

When unblocked, scorer reads new snapshot columns:
```csharp
decimal capacityPressure = candidate.UpcomingBookings / Math.Max(candidate.UpcomingCapacity, 1);
decimal inventoryFactor = capacityPressure switch
{
    >= 0.95m => 0.0m,   // exclude — fully booked
    >= 0.90m => 0.3m,
    >= 0.75m => 0.7m,
    _        => 1.0m
};
finalScore *= inventoryFactor;
```

**Recs team effort when unblocked**: 4 hrs.

### V1.5.3 Negative-Review Suppression — 1 hr

Single WHERE clause on snapshot query:
```sql
WHERE NOT (AverageRating < 3.0 AND ReviewCount >= 5)
```

New entities (`ReviewCount < 5`) NOT excluded — gives them a fair chance.

### V1.5.4 Recency Boost — 1 hr

```csharp
decimal recencyBoost = candidate.CreatedAt switch
{
    var dt when dt > DateTime.UtcNow.AddDays(-7)  => 1.30m,
    var dt when dt > DateTime.UtcNow.AddDays(-14) => 1.20m,
    var dt when dt > DateTime.UtcNow.AddDays(-30) => 1.10m,
    _ => 1.00m
};
finalScore *= recencyBoost;
```

`EntityAttributeSnapshot.CreatedAt` mirrors source entity's CreatedAt (already in V1 schema).

### V1.5.5 Halal/Dietary Filter — **BLOCKED ON CONTENTPLACES TEAM**

Status: `Business` entity has NO `IsHalal`, `HasVegetarianOptions`, `HasAlcoholFreeArea` fields today.

**ContentPlaces team effort**: 4 hrs (add columns + EF migration + integration event payload extension).

When ready:
- Snapshot updater pulls these flags
- API: `?halalOnly=true` filters before scoring
- Default: `halalOnly=true` for users with `Accept-Language: ar-*`

**Recs team effort when unblocked**: 3 hrs.

### V1.5.6 Subscription-Gated Boost Packages — **BLOCKED ON FINANCE TEAM**

Status: Finance has `Subscription` + `SubscriptionPlan` entities + `SubscriptionStatus` enum (Active/Paused/Cancelled/Expired). But `Finance.Contracts/` is EMPTY — no `ISubscriptionStatusProvider` exists.

**Finance team effort**: ~4 hrs.

```csharp
// What Finance must ship:
namespace Finance.Contracts.Subscriptions;

public interface ISubscriptionStatusProvider
{
    Task<bool> HasActiveSubscriptionAsync(
        Guid userId,
        SubscriptionTier tier,
        CancellationToken ct = default);
}

public enum SubscriptionTier : byte { Basic = 1, Promotion = 2, Enterprise = 3 }
```

When ready, Recs side adds:

```sql
CREATE TABLE analytics.BoostPackages (
    Id              UNIQUEIDENTIFIER PK,
    ProviderId      UNIQUEIDENTIFIER NOT NULL,
    EntityKind      TINYINT NOT NULL,
    EntityId        UNIQUEIDENTIFIER NOT NULL,
    BoostMultiplier DECIMAL(3,2) NOT NULL,
    StartsAt        DATETIME2 NOT NULL,
    ExpiresAt       DATETIME2 NOT NULL,
    DecayMode       TINYINT NOT NULL DEFAULT 0,
    Status          TINYINT NOT NULL,
    SubscriptionId  UNIQUEIDENTIFIER NULL,
    IsAdminGranted  BIT NOT NULL DEFAULT 0,
    GrantedByUserId UNIQUEIDENTIFIER NULL,
    PaidAmount      DECIMAL(19,4) NULL,
    Currency        CHAR(3) NULL,
    CreatedAt       DATETIME2 NOT NULL,
    UpdatedAt       DATETIME2 NULL
);
```

Endpoints:
- `POST /api/v1/analytics/admin/boosts` — admin (with or without subscription)
- `POST /api/v1/analytics/provider/boosts` — provider self-service (subscription gate; 402 if no Promotion-tier active)
- `DELETE /api/v1/analytics/admin/boosts/{id}` — admin revoke
- `GET /api/v1/analytics/admin/boosts?providerId=X` — list/audit

**Recs team effort when unblocked**: 8 hrs.

### V1.5.7 Editorial Pinning — 3 hrs (moved up from V3)

Synergistic with boost packages — same admin curation surface.

```sql
CREATE TABLE analytics.EditorialPins (
    Id           UNIQUEIDENTIFIER PK,
    Context      TINYINT NOT NULL,
    SourceKind   TINYINT NULL,
    SourceId     UNIQUEIDENTIFIER NULL,
    TargetKind   TINYINT NOT NULL,
    TargetId     UNIQUEIDENTIFIER NOT NULL,
    Position     INT NOT NULL DEFAULT 1,
    StartsAt     DATETIME2 NOT NULL,
    ExpiresAt    DATETIME2 NOT NULL,
    CampaignName VARCHAR(100),
    PinnedByUserId UNIQUEIDENTIFIER NOT NULL,
    Reason       NVARCHAR(500),
    CreatedAt    DATETIME2 NOT NULL,
    INDEX IX_EditorialPins_Active (Context, StartsAt, ExpiresAt) WHERE ExpiresAt > GETUTCDATE()
);
```

`GetRecommendations` injects active pins at specified position. UI badge: `"Editor's Choice"`.

### V1.5 — Combined WBS

| # | Deliverable | Recs hrs | External hrs |
|---|---|---|---|
| V1.5.1 | MMR diversity + anti-cannibalization | 4 | 0 |
| ~~V1.5.2~~ | Inventory awareness | ~~4~~ blocked-on-Booking | (Booking 12-16) |
| V1.5.3 | Negative-review suppression | 1 | 0 |
| V1.5.4 | Recency boost | 1 | 0 |
| ~~V1.5.5~~ | Halal/dietary filter | ~~3~~ blocked-on-ContentPlaces | (ContentPlaces 4) |
| V1.5.6 | Subscription-gated boosts | 8 | (Finance 4) |
| V1.5.7 | Editorial pinning | 3 | 0 |
| **V1.5 (unblocked items)** | | **17** | **+8** |
| **V1.5 (when fully unblocked)** | | 24 | +20 |

---

## V2 — Personalization Layer (22 hrs)

Trigger: ≥ 10k `UserInteraction` events accumulated. Typically 4-8 weeks post-V1.

### V2.1 Cold-Start Onboarding Quiz — 6 hrs

5 swipe cards on first login. Each `interested=true` recorded as `UserInteraction(InteractionType.Bookmark)` weight 2.5. Each `interested=false` records `UserInteraction(NotInterested)` (new value `7` added to enum). Profile update job runs immediately after.

```
POST /api/v1/analytics/onboarding/responses
{
  "responses": [
    { "entityKind": "Tour", "entityId": "...", "interested": true }, ...
  ]
}
```

### V2.2 "Not Interested" Negative Feedback — 4 hrs

Click X → 3 effects:
1. `analytics.UserExcludedEntities` row inserted (90-day exclusion)
2. `UserInteraction(NotInterested)` weight `-2.0`
3. Profile updater shifts vector AWAY from this item's attributes

```sql
CREATE TABLE analytics.UserExcludedEntities (
    UserId      UNIQUEIDENTIFIER NOT NULL,
    EntityKind  TINYINT NOT NULL,
    EntityId    UNIQUEIDENTIFIER NOT NULL,
    ExcludedAt  DATETIME2 NOT NULL,
    ExpiresAt   DATETIME2 NOT NULL,
    Reason      NVARCHAR(50) NULL,
    PRIMARY KEY (UserId, EntityKind, EntityId)
);
```

`GetRecommendations` for authenticated users LEFT JOINs and excludes matched rows.

### V2.3 Explainability — 2 hrs

`RecommendationCache.Reason` (existing field, repurposed) stores JSON `Signals[]`. UI renders via static label dictionary:

```csharp
public static class SignalLabels
{
    public static readonly Dictionary<string, string> En = new()
    {
        ["nearby"]                 = "Near {sourceName}",
        ["similar-price"]          = "Similar price range",
        ["high-rating"]            = "Highly rated",
        ["category-match"]         = "Same category",
        ["popular-with-similar-users"] = "Popular with travelers like you",
        ["matches-your-budget"]    = "Within your usual budget",
        ["family-friendly"]        = "Suitable for families",
        ["recently-listed"]        = "New on YallaJo",
    };
}
```

### V2.4 Budget Tier Classification — 3 hrs

```csharp
profile.BudgetTier = avgInteractedPrice switch
{
    < 30   => BudgetTier.Budget,
    < 100  => BudgetTier.Mid,
    >= 100 => BudgetTier.Luxury,
    _      => BudgetTier.Unknown
};
```

Filter recs to ±1 tier. Override via `?showAllPrices=true`. New column on `analytics.UserPreferences`.

### V2.5 Family / Kid-Friendly Inference — 2 hrs

Detect from `UserInteraction` patterns:
- User selected `Child`/`Infant` `ParticipantType` in any tour booking → `IsFamilyTraveler = true`
- User booked any tour with `MinAge <= 12` → same flag

Multiplier: × 1.3 for `IsChildFriendly = true`. × 0.7 for `MinAge > 16`.

### V2.6 Personalized Search Ranking — 3 hrs

Fold `UserPreference` into `SearchToursQueryHandler` (Task 3 ContentTours). Behind feature flag `analytics.FeatureFlags.PersonalizedSearch`.

Cross-module: requires `Analytics.Application` to expose `IUserPreferenceLookupService` consumable by `ContentTours`. Coordinate with ContentTours team.

### V2.7 Zero-Result Search Rescue — 2 hrs

When `SearchToursQueryHandler` returns 0 hits, response includes `rescue: { items: [...] }` block from `GetRecommendations` (or popular feed for anonymous).

### V2 Booking-Signal Subscriptions — **BLOCKED ON BOOKING + SOCIAL**

When Booking ships `BookingConfirmedIntegrationEvent` (V1.5.2 dependency) and Social ships `FavoriteAddedIntegrationEvent`:

```csharp
public sealed class BookingConfirmedIntegrationEventHandler { /* records UserInteraction(Booking, weight=5.0) */ }
public sealed class FavoriteAddedIntegrationEventHandler  { /* records UserInteraction(Bookmark, weight=2.5) */ }
```

Until then, all interaction signals come via `POST /interactions` API only.

**External team effort**: Social ~4 hrs (FavoriteAdded event), Booking ~3 hrs incremental (assuming Booking did capacity events for V1.5.2).

### V2 — Combined WBS

| # | Deliverable | Recs hrs | External hrs |
|---|---|---|---|
| V2.1 | Onboarding quiz | 6 | 0 |
| V2.2 | "Not interested" feedback | 4 | 0 |
| V2.3 | Explainability labels | 2 | 0 |
| V2.4 | Budget tier classification | 3 | 0 |
| V2.5 | Family inference | 2 | 0 |
| V2.6 | Personalized search ranking | 3 | 0 |
| V2.7 | Zero-result search rescue | 2 | 0 |
| V2.* | Booking + Favorite event subscribers | (handlers small) | (Social 4) |
| **V2 TOTAL** | | **22** | **+4** |

---

## V2.5 — Jordan-Specific Intelligence (33 hrs)

### V2.5.1 ~~Weather-Aware~~ — Moved to V4.2

Too much external dependency for v2. Deferred until V4.

### V2.5.2 Seasonality Boost Table — 4 hrs

```csharp
public static class SeasonalityRules
{
    // (PlaceId, MonthRange, Multiplier)
    // Wadi Rum — winter boost (1.30x Nov-Mar), summer suppress (0.60x Jun-Sep)
    // Dead Sea — spring/fall peak (1.40x Mar-May, 1.30x Sep-Nov)
    // Petra — shoulder peak (1.20x Mar-May, 1.20x Sep-Nov)
    // Aqaba — winter escape (1.30x Dec-Feb)
}
```

Stored in `analytics.SeasonalityRules` (DB), hot-reloaded hourly. Apply: `score *= GetSeasonMultiplier(candidate.PlaceId, currentMonth)`.

### V2.5.3 Religious / Holiday Calendar — 4 hrs

```sql
CREATE TABLE analytics.HolidayCalendar (
    Id          UNIQUEIDENTIFIER PK,
    HolidayName VARCHAR(50) NOT NULL,
    StartDate   DATE NOT NULL,
    EndDate     DATE NOT NULL,
    Year        INT NOT NULL,
    BoostRulesJson NVARCHAR(MAX) NOT NULL
);
```

Pre-populate Ramadan, Eid al-Fitr, Eid al-Adha, Christmas, Easter for next 5 years (Islamic dates shift annually). Admin-editable.

### V2.5.4 Trip-Stage Awareness — uses existing Tracking module — 6 hrs

`Tracking.LiveTrackingSession` + `LocationSnapshot` already capture user-tour location. Detection:

| Stage | Detection |
|---|---|
| Pre-trip | First interaction > 7 days ago, no bookings |
| Just-landed | First interaction in last 24h, geo-IP = Jordan |
| Mid-trip | Active `LiveTrackingSession` exists for this user |
| Last-day | Booking with EndDate=today, no later bookings |
| Post-trip | Last booking ended > 1 day ago |

Stored in `UserPreference.CurrentTripStage` (new column). Refreshed daily.

Cross-module read accepted (background job, infra layer, read-only — same exception as V1 category bootstrap).

### V2.5.5 Multi-Day Itinerary Planner — 16 hrs

```
GET /api/v1/analytics/itinerary?fromDate=...&toDate=...&startLocation={lat,lng}

Response: per-day suggestions grouped by geographic cluster + logistics hints
```

Pre-defined "trip arc templates" stored as graph in `analytics.TripArcs` (Amman → Dead Sea → Petra → Wadi Rum → Aqaba). Match user's date range + interests to closest template; per-day filtered to that day's geographic cluster; logistics hints generated from inter-day distance.

### V2.5.6 Photo-Worthy Boost — 3 hrs

`IsPhotogenicHotspot` bool added to `EntityAttributeSnapshot`. Set during snapshot population if source has flag (admin-curated).

For users with high `Share` interaction count → photogenic items × 1.4.

Admin endpoint `PUT /admin/entities/{kind}/{id}/photogenic` toggles source flag and resyncs snapshot.

### V2.5 — Combined WBS

| # | Deliverable | Recs hrs |
|---|---|---|
| V2.5.2 | Seasonality table + scorer multiplier | 4 |
| V2.5.3 | Holiday calendar table | 4 |
| V2.5.4 | Trip-stage detection (uses Tracking) | 6 |
| V2.5.5 | Multi-day itinerary planner | 16 |
| V2.5.6 | Photogenic flag + share-user boost | 3 |
| **V2.5 TOTAL** | | **33** |

---

## V3 — Marketplace + Cross-Channel + Operations (53 hrs)

### V3.1 ~~Editorial Pinning~~ — moved up to V1.5.7

### V3.2 Sponsored Placements (hybrid billing — flat-fee + CPC auction for Gold) — 15 hrs

Two-track billing model:

**Track A — Flat-fee tiers (Bronze + Silver)**:
- Bronze: $50/30d → 1 sponsored slot per 10 organic, position 7-10
- Silver: $150/30d → 1 sponsored slot per 10 organic, position 4-6
- Same schema as `BoostPackages` (V1.5.6) with `Type = SponsoredSlot`, `BillingMode = FlatFee`
- Billed via Finance recurring invoice (uses existing `Subscription` + `BillingCycle` infra)

**Track B — Gold premium slot (CPC auction)**:
- 1 premium slot per recommendation list at position 1-2 (top of list)
- Real-time auction across providers bidding for the same `(Context, Source)` pair
- Pay-per-click — provider charged only when user clicks the result
- Bid floor: $0.50 per click; ceiling: $5.00
- Auction winner = `bidPerClick × providerQualityScore` (CTR-adjusted, see V3.5 position bias)

```sql
-- Extend BoostPackages or add separate table
CREATE TABLE analytics.SponsoredAuctionBids (
    Id              UNIQUEIDENTIFIER PK,
    BoostPackageId  UNIQUEIDENTIFIER NOT NULL,    -- FK
    Context         TINYINT NOT NULL,
    SourceKind      TINYINT NULL,                  -- null = applies to all sources
    SourceId        UNIQUEIDENTIFIER NULL,
    BidPerClick     DECIMAL(8,2) NOT NULL,         -- $0.50 — $5.00
    DailyBudgetCap  DECIMAL(10,2) NULL,            -- stop bidding when daily spend hit
    SpentToday      DECIMAL(10,2) NOT NULL DEFAULT 0,
    StartsAt        DATETIME2 NOT NULL,
    ExpiresAt       DATETIME2 NOT NULL,
    Status          TINYINT NOT NULL,              -- Active/Paused/BudgetExhausted
    CreatedAt       DATETIME2 NOT NULL
);

CREATE TABLE analytics.SponsoredClickEvents (
    Id              UNIQUEIDENTIFIER PK,
    BidId           UNIQUEIDENTIFIER NOT NULL,
    UserId          UNIQUEIDENTIFIER NULL,
    SessionId       VARCHAR(50),
    SourceKind      TINYINT NOT NULL,
    SourceId        UNIQUEIDENTIFIER NOT NULL,
    Position        INT NOT NULL,
    ChargedAmount   DECIMAL(8,2) NOT NULL,
    ClickedAt       DATETIME2 NOT NULL,
    INDEX IX_SponsoredClickEvents_Bid_Date (BidId, ClickedAt)
);
```

**Auction algorithm** (runs at recommendation request time):
```
candidates = active bids matching (context, source, current time, daily budget remaining)
qualityScore(bid) = baseQuality × (1 + log10(bid.ProviderCTR + 0.01))   // CTR-adjusted
adjustedBid = bid.BidPerClick × qualityScore(bid)
winner = argmax(adjustedBid)
chargeAmount = secondPriceAuction(winner, runnerUp)   // Vickrey-style — winner pays runner-up's bid + $0.01
```

Click charging happens async via `SponsoredClickEvent` insert + nightly Finance batch.

**Anti-fraud**:
- Same `(UserId/SessionId, BidId)` charged max once per 24h
- Bot detection: dwell-time check after click (< 2s = no charge)
- Daily budget cap enforced strictly

**Effort breakdown**:
- Flat-fee tracks (Bronze/Silver): 4 hrs (similar to V1.5.6 boost packages)
- Auction engine: 7 hrs (bid evaluation, second-price logic, scoring)
- Click attribution + charging: 3 hrs (event insert, daily batch sync to Finance)
- Anti-fraud + daily budget cap: 1 hr

**Coordination**: Finance team must support per-click invoicing in addition to recurring subscription billing. Coordinate billing model with Finance.

### V3.3 A/B Testing Framework — 12 hrs

```sql
CREATE TABLE analytics.Experiments (Id, Name, Description, Status, StartsAt, ExpiresAt, TrafficPercent, VariantsJson);
CREATE TABLE analytics.ExperimentAssignments (UserId, ExperimentId, VariantName, AssignedAt, PRIMARY KEY (UserId, ExperimentId));
```

Bucketing: stable hash of `(UserId, ExperimentId)` → bucket → variant. Same user always gets same variant.

`IExperimentVariantResolver.GetVariantAsync(userId, experimentName, ct)` → variant. Scorer uses variant's params.

### V3.4 CTR / Conversion Funnel Tracking — 8 hrs

```sql
CREATE TABLE analytics.SuggestionMetrics (
    Id, BatchId, RecommendationCacheId, UserId NULL, Position, Stage, OccurredAt, SessionId, ExperimentVariant NULL
);
```

Aggregations (every 30 min):
- CTR per `(Context, Position)`
- Conversion per `(Context, Position)` — impression → booking
- Per-variant CTR
- Per-provider CTR

Endpoint: `GET /api/v1/analytics/admin/metrics?context=X&from=...&to=...`.

### V3.5 Position Bias Correction — 4 hrs

```
correctedClickWeight = rawClickWeight / propensity(position)
propensity(1)=1.00, (2)=0.55, (3)=0.40, (5)=0.25, (10)=0.13
```

Used when feeding click signals into V2 personalization profile updates.

### V3.6 Email Digest — **PARTIALLY BLOCKED ON ACCOUNTS** — 10 hrs

Status: `IEmailService` exists in `Auth.Infrastructure` (Gmail SMTP working). `Profile` has NO `MarketingConsent` field.

**Accounts team effort**: ~4 hrs.

```csharp
public sealed record MarketingConsent(
    bool EmailDigest,
    bool PushNotifications,
    bool ReEngagementCampaigns,
    DateTime? LastUpdatedUtc);

// On Profile aggregate:
public MarketingConsent? MarketingConsent { get; private set; }
public void UpdateMarketingConsent(MarketingConsent consent);

// New endpoint:
PUT /api/v1/accounts/me/marketing-consent
```

When ready, Recs team builds:
- `EmailDigestBackgroundService` — runs Mondays 09:00 user-tz
- Pulls top 5 from `GetRecommendations` per opted-in user
- Renders Razor template
- Sends via `IEmailService.SendAsync`

**Architectural note**: `IEmailService` lives in `Auth.Application.Interfaces` today. Recommend Auth team move it to `Messaging.Contracts` so Analytics doesn't need to reference Auth. Coordinate.

### V3.7 ~~Push Notification Targeting~~ — DEFERRED to V4.1

Status: NO push notification infrastructure exists. `DeviceToken` entity + `NotificationChannel.Push` enum exist, but no `IPushService`, no FCM/APNS integration, no actual sender.

**Messaging team effort to unblock**: ~30 hrs (build provider, dispatcher, BackgroundService poller). Out of scope for V3.

### V3.8 Re-Engagement Segments — 8 hrs

```
GET /api/v1/analytics/admin/segments?rule=X&entityKind=Tour&entityId=Y
Response: { "userIds": [...], "count": 1247 }
```

Predefined segments:
- "Viewed X but didn't book in 7 days"
- "Booked once 90+ days ago, no return"
- "Started booking but abandoned at checkout"
- "Reviewed item ≥4 stars but no return booking in 60 days"

Output flows to Messaging for campaign send (assumes Messaging campaign pipeline; currently uses `IEmailService` directly).

### V3.9 GDPR Delete-My-Data — 6 hrs

```
DELETE /api/v1/analytics/me

Effect:
1. Hard-delete UserInteractions WHERE UserId = currentUser
2. Hard-delete UserPreference + UserPreferredCategory WHERE UserId = currentUser
3. Hard-delete UserExcludedEntities WHERE UserId = currentUser
4. Hard-delete ExperimentAssignments WHERE UserId = currentUser
5. Anonymize SuggestionMetrics (set UserId = NULL)
6. Anonymize/delete RecommendationCache rows for this user
7. Audit log: "user-{Id}-data-deleted-at-{utc}"
```

30-day soft-delete window before hard execution (cancellable).

### V3.10 Anonymization After 365 Days — included in V3.9

Daily background job:
```sql
UPDATE analytics.UserInteractions
SET UserId = NULL
WHERE OccurredAt < DATEADD(DAY, -365, GETUTCDATE())
  AND UserId IS NOT NULL;
```

Aggregate counts intact for popularity. Individual user ties severed.

### V3.11 Opt-Out Mechanism — 3 hrs

User toggle in account settings: `EnablePersonalization: bool`. When false:
- `UserPreference` not built
- Recs fall back to popularity-only
- Interactions still recorded (for global popularity)
- Excluded from A/B experiments — always control

### V3 — Combined WBS

| # | Deliverable | Recs hrs | External hrs |
|---|---|---|---|
| V3.2 | Sponsored placements (hybrid: flat-fee Bronze/Silver + CPC auction Gold) | 15 | 0 |
| V3.3 | A/B experiment framework | 12 | 0 |
| V3.4 | CTR funnel tracking | 8 | 0 |
| V3.5 | Position bias correction | 4 | 0 |
| V3.6 | Email digest BackgroundService | 10 | (Accounts 4) |
| ~~V3.7~~ | Push targeting | deferred to V4 | (Messaging 30) |
| V3.8 | Re-engagement segments | 8 | 0 |
| V3.9 | GDPR delete + anonymization | 6 | 0 |
| V3.11 | Opt-out preference | 3 | 0 |
| **V3 TOTAL (excluding push)** | | **63** | **+4** |

---

## V4 — Deferred Features

### V4.1 Push Notification Targeting

Original V3.7 design preserved. Re-enters active roadmap when:
- `IPushService` interface exists in `Messaging.Contracts`
- FCM (or alternative) integration is implemented
- `DeviceToken` write/read flow operational
- `MarketingConsent.PushNotifications` field is live (V3.6 dependency)

### V4.2 Weather-Aware Re-Ranking — 8 hrs

Daily background job fetches 7-day forecast for each location grid (rounded to 0.1° = ~10km cells). Stored in `analytics.WeatherForecasts`.

OpenWeatherMap free tier (60 calls/min, 1M/month) with aggressive caching is sufficient for ~10k unique cells. Zero infra cost.

When forecast rain probability > 60%: outdoor candidates × 0.5, indoor × 1.5. Add signal `weather-rain-warning` or `indoor-recommended-today`.

---

## Cross-Module Dependencies (verified)

| Feature | Required from other module | Status |
|---|---|---|
| V1 EntityAttributeSnapshots | ContentTours/ContentPlaces — `*.Created/Updated/Deleted` integration events | ✅ Already published |
| V1 Categories bootstrap | ContentCore — `EntityCategories` cross-context read at job startup | ✅ Pragmatic exception (background job, infra layer, read-only) |
| V1.5.2 Inventory awareness | Booking — `BookingCapacityChangedIntegrationEvent` | ⚠️ Booking has zero events today — needs ~12-16 hrs |
| V1.5.5 Halal filter | ContentPlaces — add `IsHalal/HasVegetarianOptions/HasAlcoholFreeArea` columns | ⚠️ Needs ~4 hrs |
| V1.5.6 Subscription gate | Finance — `ISubscriptionStatusProvider` in `Finance.Contracts` | ⚠️ Needs ~4 hrs |
| V2 Booking signals | Booking — `BookingConfirmedIntegrationEvent` | ⚠️ Same Booking work as V1.5.2 |
| V2 Favorite signals | Social — `FavoriteAddedIntegrationEvent` | ⚠️ Social.Contracts is empty — needs ~4 hrs |
| V2.5.4 Trip-stage | Tracking — read `LiveTrackingSession` snapshot data | ✅ Tracking entities exist; cross-context exception accepted |
| V3.6 Email digest | Auth or Messaging — `IEmailService` (currently in Auth) | ✅ Working today; cleaner if moved to Messaging |
| V3.6 Email digest | Accounts — `MarketingConsent` value object on Profile | ⚠️ Profile has zero consent fields — needs ~4 hrs |
| V4.1 Push targeting | Messaging — push provider (FCM/APNS) | ⏭ Heavy lift — deferred to V4 |

---

## Coordination Plan

### Pre-V1.5 sprint kickoff
1. **Booking team** commits to building integration event surface (capacity, confirmed, starting-soon). 12-16 hrs over 1-2 weeks.
2. **Finance team** commits to `ISubscriptionStatusProvider` contract. 4 hrs.
3. **ContentPlaces team** commits to halal/dietary columns. 4 hrs.

### Pre-V2 sprint kickoff
4. **Social team** commits to `FavoriteAddedIntegrationEvent` + outbox publish. 4 hrs.

### Pre-V3 sprint kickoff
5. **Accounts team** commits to `MarketingConsent` on Profile. 4 hrs.
6. **Auth + Messaging** coordinate move of `IEmailService` to `Messaging.Contracts`. 2 hrs.

### Pre-V4
7. **Messaging team** plans push provider integration. ~30 hrs (separate sprint).

---

*All hours and dependencies verified against the actual codebase by 6 parallel explore agents. See `RECOMMENDATIONS_ENGINE_GAPS.md` for the full audit log.*
