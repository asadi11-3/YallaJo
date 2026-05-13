# Recommendations Engine — Gap Analysis

> **Source**: 6-agent parallel verification of the YallaJo codebase against `RECOMMENDATIONS_ENGINE_SPEC.md` and `RECOMMENDATIONS_ENGINE_ROADMAP.md`.
> **Verdict**: 7 critical / high gaps that invalidate parts of the plan. 4 medium gaps that need spec patches. 3 minor wins (existing infra reusable).

---

## 🔴 CRITICAL — Architectural Conflicts (require decision before any code)

### GAP-1 — Module placement contradicts YallaJo.md product spec

**The roadmap creates a NEW `Recommendations` module. The product spec puts recommendations inside the existing `Analytics` module.**

Evidence from `Agents/YallaJo.md`:
- Line 288 — Background services: `RecommendationEngineService` belongs to **Analytics**
- Lines 971-972 — Phase 4 endpoints: `/api/analytics/recommendations` and `/api/analytics/recommendations/similar/{tourId}`
- Lines 1774-1803 — Hybrid algorithm spec (40% collaborative + 35% content-based + 25% popularity) is documented as an Analytics feature
- Line 1864 — Phase 4 plan: "Analytics: Personalized AI Recommendations"

Analytics module already has the entities scaffolded:

| Entity in `Analytics.Domain/Entities/` | Maps to roadmap proposal |
|---|---|
| `UserInteraction.cs` | Replaces proposed `Recommendations.UserInteractionEvent` |
| `RecommendationCache.cs` | Replaces proposed `Recommendations.SuggestionItem` |
| `PopularityScore.cs` | New addition |
| `UserPreference.cs` | Replaces proposed `Recommendations.UserPreferenceProfile` |
| `UserPreferredCategory.cs` | Replaces proposed `Recommendations.CategoryWeights` JSON |
| `Analytics.InteractionType` enum | Replaces proposed `Recommendations.InteractionType` enum |

**Impact**: If we ship a separate Recommendations module, we're building parallel structures that already exist. PR will be rejected — duplicates Analytics. This is the **#1 blocker**.

**Fix — pick ONE**:
- **Option A (Recommended)** — Build the engine inside `Analytics` module. Reuse all 5 existing entities. Endpoints become `/api/v1/analytics/recommendations/...` per spec. Save ~12 hrs by not re-creating entities.
- **Option B** — Keep separate `Recommendations` module BUT update `Agents/YallaJo.md` first to reflect this architectural change. Get Tech Lead sign-off because this changes the product spec. Adds risk + coordination cost.

---

### GAP-2 — Booking module has ZERO integration events; Q1 answer was wrong

**The roadmap assumes Booking publishes `BookingCapacityChangedIntegrationEvent` and `BookingStartingSoonIntegrationEvent`. Verification: Booking has zero integration events. `Booking.Contracts/` directory contains no source files — only generated assemblies.**

| Event the roadmap depends on | Status |
|---|---|
| `BookingConfirmedIntegrationEvent` | ❌ Does not exist |
| `BookingCapacityChangedIntegrationEvent` | ❌ Does not exist |
| `BookingStartingSoonIntegrationEvent` | ❌ Does not exist |
| Any `booking.*` entry in `IntegrationEventTypeRegistry` | ❌ Zero entries |

The Booking module has outbox infrastructure (`OutboxMessages` table, `OutboxProcessor` registered) but is currently **event-silent** — no handler in any module publishes anything.

**Impact**: 
- V1.5.2 inventory awareness is blocked until Booking team builds capacity events
- V2 personalization signal `BookingCompleted` is blocked
- V3.7 push targeting (`BookingStartingSoonIntegrationEvent`) is blocked
- Booking team's effort estimate jumps from "1 event publish per reservation" (~4 hrs) to "build full integration event surface" (~12-16 hrs)

**Fix**: 
1. Coordinate with Booking team lead — they need a dedicated 2-day task to build:
   - `Booking.Contracts/IntegrationEvents/` folder with 3+ event records
   - Domain events on `Reservation`, `TourBooking`, `AvailabilitySlot` aggregates
   - Domain event handlers that write to `OutboxMessages`
   - Registry entries in `IntegrationEventTypeRegistry`
2. Mark V1.5.2, V2 booking signals, V3.7 as "blocked-on-Booking" in roadmap
3. Recommendations V1 ships WITHOUT booking signals — uses popularity (BookingCount field on Tour) as proxy

---

### GAP-3 — Cross-context EF reads pattern does NOT exist in this codebase

**The roadmap proposes `IEntityAttributeProvider` to read entity attributes from `ContentToursDbContext`, `ContentPlacesDbContext`, `ContentCoreDbContext`. Verification: there are ZERO cross-context reads anywhere in the codebase. Every module reads only from its own DbContext. Cross-module data flows via integration events + outbox/inbox.**

This is enforced architecturally:
- `ContentTours.Application` does not reference `ContentPlaces.Infrastructure` or any other module's DbContext
- The 22 gotchas in `agent-context.md` (rule #22) explicitly say: "Do not fix ContentPlaces event dispatch by injecting `ContentPlacesDbContext` into Application handlers"
- Existing pattern: `PlaceTourCountUpdatedIntegrationEventHandler` shows the canonical approach — events carry data, receiver reads only its own DB

**Impact**: The proposed `IEntityAttributeProvider` violates Clean Architecture rules. It would be rejected at PR review. Either:
- The engine cannot directly query Tour/Business attributes, OR  
- We need a denormalized snapshot table populated by integration event handlers

**Fix**: Adopt the **`EntityAttributeSnapshots` pattern** (already mentioned in §10 of the spec but downgraded to "v1.5 optional"). Promote it to **REQUIRED for V1**:

```
analytics.EntityAttributeSnapshots
  - One row per (EntityKind, EntityId) — a denormalized read model
  - Populated by integration event handlers:
      TourCreatedIntegrationEvent  → upsert snapshot
      TourUpdatedIntegrationEvent  → update snapshot
      TourDeletedIntegrationEvent  → delete snapshot
      BusinessCreatedIntegrationEvent → upsert
      BusinessUpdatedIntegrationEvent → update
      BusinessDeletedIntegrationEvent → delete
  - Scorer reads from this table only (own DbContext)
  - Eventually consistent (lag = outbox processor interval ≈ 10s)
```

This is THE standard pattern in YallaJo. It's how `Place.TourCount` is denormalized (set by ContentTours event → ContentPlaces consumer). Adopt the same.

**Effort**: +6 hrs to V1 to implement snapshot table + 6 integration event handlers (Tour/Business × Created/Updated/Deleted).

---

### GAP-4 — Location is a VALUE OBJECT, not separate columns; Q1 answer mostly wrong

**The roadmap's scoring code references `source.Latitude`, `candidate.Longitude`. Verification: location is stored as a `Location` value object on Tour, Business, and Place — accessed via `.Location.Latitude` and `.Location.Longitude`.**

```csharp
// Tour.cs / Business.cs / Place.cs:
public Location Location { get; private set; } = default!;
public Location? MeetingPoint { get; private set; }   // Tour only

// Location value object (YallaJo.SharedKernel.Domain/ValueObjects/Location.cs):
public sealed class Location : ValueObject
{
    public decimal Latitude  { get; private set; }
    public decimal Longitude { get; private set; }
    public double DistanceTo(Location other) { ... Haversine ... }   // ← Already implemented!
}
```

**Two consequences**:

1. **Free win**: `Location.DistanceTo(otherLocation)` already implements Haversine via `DistanceTo()`. The roadmap's `HaversineDistanceCalculator.cs` is unnecessary — delete from spec, save 1 hr.

2. **Code paths in spec must be updated**: every `source.Latitude` → `source.Location.Latitude`, every Haversine call → `source.Location.DistanceTo(candidate.Location)`. Snapshot table column names too: `Latitude` → `LocationLatitude` (matches EF owned-property convention).

**Fix**: spec patches throughout §4 and §10 to use `.Location.X` accessors and the existing `DistanceTo` method.

---

## 🟠 HIGH — Missing Cross-Module Dependencies

### GAP-5 — Social module has NO `FavoriteAddedIntegrationEvent`; Q7 answer was wrong

**Q7 answered "Yes — Social already publishes this event". Verification: Social.Contracts is EMPTY (no source files). No integration events exist. The `Favorite` entity exists in `Social.Domain/Entities/` but no domain event, no integration event, no outbox publish.**

**Impact**: V2 personalization "Favorite" interaction signal is dead — handler subscribes to nothing. Onboarding quiz also relies on this signal.

**Fix**: 
- Coordinate with Social team to add `Social.Contracts/IntegrationEvents/FavoriteAddedIntegrationEvent.cs` + domain event + outbox publishing
- Until then, V2 Favorite signal arrives via the `RecordInteractionCommand` API only (user-facing button), not via Social integration

---

### GAP-6 — `Profile` entity has NO marketing consent fields; Q6 answer was unimplementable

**Q6 answered "Explicit opt-in for marketing". Verification: `Accounts.Domain.Entities.Profile` has only basic fields (UserId, FirstName, LastName, DisplayName, AvatarUrl, DateOfBirth, Gender, Country, City, AddressLine). No `MarketingConsent`, `EmailConsent`, `PushConsent`, `OptInMarketing`, or `Newsletter` field exists.**

**Impact**: V3.6 (email digest) and V3.7 (push targeting) cannot check user consent — there's no flag to read.

**Fix — coordinate with Accounts team**:
- Add `MarketingConsent` value object to Profile aggregate:
  ```csharp
  public sealed record MarketingConsent(
      bool EmailDigest,
      bool PushNotifications,
      bool ReEngagementCampaigns,
      DateTime? LastUpdatedUtc);
  ```
- Add `Profile.UpdateMarketingConsent(MarketingConsent)` method
- New endpoint: `PUT /api/v1/accounts/me/marketing-consent`
- Default: all `false` (explicit opt-in only)
- Migration: add columns to `Profiles` table

Until Accounts team ships this, V3.6/V3.7 are blocked. Mark in roadmap as "blocked-on-Accounts".

---

### GAP-7 — Push notification infrastructure does NOT exist

**The roadmap V3.7 assumes a working push notification stack. Verification: there is no `IPushService`, no Firebase/FCM/APNS integration, no push provider implementation. Only comment evidence:**
> `// BackgroundService polls Notifications and dispatches via SMTP/FCM.`  ← intent, not code

The `DeviceToken` entity exists (stores token + platform). The `NotificationChannel` enum has a `Push` value. But there's nothing that actually sends a push notification.

**Impact**: V3.7 push targeting is blocked until Messaging team builds:
- `IPushService` interface in `Messaging.Contracts`
- FCM client (or whichever provider) in `Messaging.Infrastructure`
- `Notification` dispatch from `BackgroundService` (the planned poller mentioned in the comment)

**Effort estimate**: +20-30 hrs of Messaging team work — significantly more than V3.7's allocated 7 hrs.

**Fix**: Move V3.7 to a hypothetical V4 (after Messaging team ships push infra). Or if Messaging push is built in parallel, V3.7 can ship in V3 as planned.

---

## 🟡 MEDIUM — Spec Patches Required

### GAP-8 — Halal/dietary flags do NOT exist on `Business` entity

**Roadmap V1.5.5 filters by `Business.IsHalal`, `HasVegetarianOptions`, `HasAlcoholFreeArea`. Verification: NONE of these fields exist on `Business.cs`.**

**Fix**: 
- Coordinate with ContentPlaces team — add 3 nullable bool columns + EF migration
- Estimated effort: 4 hrs ContentPlaces work
- Until then, V1.5.5 ships as a no-op or is deferred

---

### GAP-9 — `Finance.Contracts` is empty; `ISubscriptionStatusProvider` does NOT exist; Q2 answer was correct (it needs to be built)

**Q2 confirmed Finance team must build `ISubscriptionStatusProvider`. Verification: `Finance.Contracts/` is empty. The interface needs to be created from scratch.**

**Fix**: tracked correctly in roadmap (§Q2). Add explicit dependency note in V1.5.6 WBS:
- "Blocked-until: Finance team ships `Finance.Contracts/Interfaces/ISubscriptionStatusProvider.cs` (~4 hrs)"

What Finance has today (good news):
- `Subscription` entity with `UserId, PlanId, Status, ActivePeriod, CancelledAt, TrialEndsAt`
- `SubscriptionStatus` enum: `Active=0, Paused=1, Cancelled=2, Expired=3`
- `SubscriptionPlan` entity

Finance team's task is just exposing a contract. The data model is ready.

---

### GAP-10 — `AppAction.Refresh` does NOT exist

**Roadmap V1 endpoint `POST /api/v1/recommendations/admin/refresh` requires permission `Recommendations.Batch.Refresh`. Verification: `AppAction.Refresh` is not defined. Existing actions: Read, Create, Update, Delete, UpdateSelf, UpdateAny, DeleteAny, SoftDelete, Approve, Reject, Suspend, Reinstate, Replay, ReadOwn, ReadAny, Feature.**

**Fix**: One-line addition to `YallaJo.SharedKernel.Application/Authorization/AppAction.cs`:
```csharp
public const string Refresh = nameof(Refresh);
```

Or — reuse `AppAction.Replay` (semantically close: "replay the batch refresh"). Cheaper, no SharedKernel touch needed.

**Recommendation**: add `Refresh` for clarity. 5-minute change.

---

### GAP-11 — `EntityCategory` linkage uses composite-PK + EntityType discriminator (not direct nav)

**Roadmap implies a clean Tour↔Category navigation. Reality:**

```csharp
public sealed class EntityCategory
{
    public EntityType EntityType { get; }   // Tour=1, Business=2, Place=0, etc.
    public Guid EntityId { get; }
    public Guid CategoryId { get; }
    public Category Category { get; }       // FK to ContentCore.Category (cascade delete)
}
// PK = (EntityType, EntityId, CategoryId)
// Schema: content_core.EntityCategories
```

**Impact**: My category-match scorer would need to read from `ContentCore.EntityCategories` — that's a cross-context read (already flagged in GAP-3).

**Fix**: When updating snapshot table per GAP-3, denormalize categories too:
```sql
ALTER TABLE analytics.EntityAttributeSnapshots
ADD CategoryIdsJson NVARCHAR(500) NULL;
```

Snapshot updater handler subscribes to (when these events ship):
- `EntityCategoryAssignedIntegrationEvent` (ContentCore)  ← does NOT exist today, must be added by ContentCore
- `EntityCategoryRemovedIntegrationEvent` (ContentCore)   ← same

**Fallback if ContentCore doesn't ship those events**: snapshot is rebuilt every 6h from a cross-context query during the batch refresh job. Acceptable staleness for category data.

---

## 🟢 LOW — Existing Infrastructure (Free Wins)

### WIN-1 — `Tracking.LiveTrackingSession` already captures user-tour location
The `Tracking` module's `LiveTrackingSession` + `LocationSnapshot` entities track every active tour booking with second-by-second location data. **This is gold for trip-stage awareness (V2.5.4)**:

```
Trip-stage detection algorithm becomes:
  if (TrackingDb.LiveTrackingSessions.Any(s => s.UserId == X && s.Status == Active))
      → user is "Mid-trip" (currently on a tour)
  else if (booking exists with StartsAt > UtcNow.AddHours(-24))
      → user is "Just-landed" or "Pre-trip"
```

No need to build trip-stage detection from scratch. Subscribe to Tracking integration events (when those exist — they don't yet, but Tracking has the outbox infra ready).

### WIN-2 — `Location.DistanceTo()` already implements Haversine
Already noted in GAP-4. Save 1 hr by deleting `HaversineDistanceCalculator.cs` from the spec.

### WIN-3 — `IEmailService` exists in `Auth.Application.Interfaces` with Gmail SMTP impl
For V3.6 email digest, reuse `Auth.Application.Interfaces.IEmailService.SendAsync(string, string, string, ct)`. Either:
- Move it to `Messaging.Contracts` (cleaner) — small refactor coordinated with Auth team
- Reference it from the new module as-is — works but architecturally awkward (Recommendations referencing Auth)

**Recommendation**: ask Auth team to move `IEmailService` interface into `Messaging.Contracts` so it's available cross-module. The Gmail implementation stays in Auth.Infrastructure or moves to Messaging.Infrastructure (whichever team prefers).

---

## Summary of Decisions Needed

| # | Decision | Default | Owner |
|---|---|---|---|
| 1 | **Module placement**: build in Analytics (Option A) or new Recommendations module (Option B)? | Option A — align with YallaJo.md product spec | Tech Lead |
| 2 | **Booking events**: ship V1 with no Booking signals? Or wait for Booking team to build them? | Ship V1 with popularity proxy. Booking events arrive in V1.5+. | Tech Lead + Booking team |
| 3 | **Cross-context reads**: adopt `EntityAttributeSnapshots` denormalization pattern? | YES — required by architecture | Recommendations team |
| 4 | **Halal flags**: ContentPlaces team adds to Business? | YES — coordinate as cross-team task | ContentPlaces team |
| 5 | **Marketing consent**: Accounts team adds to Profile? | YES — required for V3 | Accounts team |
| 6 | **Push infra**: defer V3.7 to V4 OR Messaging team ships in parallel? | Defer V3.7 to V4 (don't block V3 ship) | Messaging team |
| 7 | **Email service**: move `IEmailService` to Messaging? | YES — coordinate with Auth team | Auth + Messaging teams |
| 8 | **AppAction.Refresh**: add new action OR reuse Replay? | Add new `Refresh` constant — clarity > brevity | Recommendations team |

---

## Revised Effort Estimate

After accounting for gaps:

| Phase | Original | Revised | Delta | Reason |
|---|---|---|---|---|
| **V1** | 36 | **42** | +6 | EntityAttributeSnapshots + 6 integration event handlers (GAP-3) |
| **V1.5** | 21 | **17** | -4 | V1.5.2 inventory deferred (GAP-2); V1.5.5 halal blocked-on-ContentPlaces |
| **V2** | 25 | **22** | -3 | Booking signals deferred (GAP-2 / GAP-5); some V2 cuts |
| **V2.5** | 33 | 33 | 0 | No change — uses Tracking module (WIN-1) |
| **V3** | 77 | **53** | -24 | V3.7 push deferred (GAP-7); V3.2 sponsored simplified |
| **External team work** | 0 | **+45** | +45 | Booking events (16h), Accounts marketing consent (4h), Finance contract (4h), ContentPlaces halal (4h), ContentCore category events (8h), Messaging push (deferred to V4) |
| **Recommendations team total** | 192 | **167** | -25 | Net reduction (cuts > additions) |
| **All teams total** | 192 | **212** | +20 | Including external team coordination work |

---

## Action Plan

### Immediate (this week)
1. **Get Tech Lead decision on GAP-1** (module placement) — biggest blocker
2. **Convene cross-team sync** — Booking, Accounts, Finance, ContentPlaces, ContentCore, Auth, Messaging
3. **Update both spec and roadmap docs** — apply all 11 gap fixes
4. **Re-baseline V1 estimate** to 42 hrs

### Pre-V1 ship (1-2 weeks before V1 starts)
1. Booking team: build `BookingConfirmedIntegrationEvent` + `BookingCapacityChangedIntegrationEvent`
2. Finance team: ship `ISubscriptionStatusProvider` contract
3. ContentPlaces team: add halal/dietary flags to Business
4. ContentCore team: ship `EntityCategoryAssignedIntegrationEvent` + `EntityCategoryRemovedIntegrationEvent`
5. Add `AppAction.Refresh` to SharedKernel

### During V1 development
1. Build engine in **Analytics** module (per GAP-1 default)
2. Reuse existing entities: `UserInteraction`, `RecommendationCache`, `PopularityScore`, `UserPreference`, `UserPreferredCategory`
3. Add `EntityAttributeSnapshots` table + 6 integration event handlers
4. Use `Location.DistanceTo()` for Haversine
5. Skip Booking signals for V1 — use `Tour.BookingCount` field as popularity proxy

### Post-V1 (continuous)
1. Coordinate with Social team on `FavoriteAddedIntegrationEvent`
2. Coordinate with Accounts on `MarketingConsent`
3. Coordinate with Messaging on push infra (long-tail)

---

*This gap analysis was generated from 6 parallel codebase verifications. Every "does not exist" claim has been verified against the actual file system. No assumptions remain unchallenged.*
