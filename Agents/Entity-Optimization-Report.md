# YallaJo Entity Optimization Report

> **Date**: 2025-07-15
> **Scope**: Full entity audit across 14 modules (155 entity files) — identify tables to drop, merge, or replace with DTOs/cache
> **Philosophy**: If data is derivable from existing tables, DON'T create a new table — use queries + DTOs + HybridCache instead
> **Related Plans**: All 12 workflow plans in `Agents/Plans/`

---

## Executive Summary

| Category | Count | Tables Saved | Files Saved |
|----------|-------|-------------|-------------|
| Duplicates (remove table, use cross-module ID) | 3 | 3 | ~9 |
| Deferred post-MVP (remove table + EF config) | 11 | 11 | ~33 |
| Replaced/Dead code (delete entirely) | 3 | 3 | ~21 |
| Cache → HybridCache (remove table) | 3 | 3 | ~9 |
| Snapshots → Cross-module query (remove table) | 5 | 5 | ~15 |
| **Total** | **25** | **25** | **~87** |

**Current**: ~155 entity files → ~130 after optimization
**Database tables saved**: ~25 fewer migrations, indexes, and maintenance overhead

---

## Category 1 — DUPLICATES (Remove Table, Use Cross-Module ID Reference)

These entities are full copies of entities that already exist in another module. Replace with Guid ID references + cross-module contracts.

| # | Entity | Module | Duplicate Of | Action |
|---|--------|--------|-------------|--------|
| 1 | `TourGuide` | Booking.Domain | ContentTours.TourGuide | **DROP TABLE**. Store `Guid TourGuideId` on AvailabilitySlot/TourBooking. Query ContentTours via `ITourGuideExistenceService` contract. |
| 2 | `TourGuideLanguage` | Booking.Domain | ContentTours.TourGuideLanguage | **DROP TABLE**. Child of duplicate #1. |
| 3 | `TourGuideSpecialization` | Booking.Domain | ContentTours.TourGuideSpecialization | **DROP TABLE**. Child of duplicate #1. |

**Impact**: 3 tables, ~9 files (entity + EF config + repository). Booking module already has `ITourGuideOwnershipService` contract — extend it for existence checks.

**How it works now**: Booking.TourGuide is a shell (30 lines, NO methods) that duplicates UserId, Bio, YearsOfExperience, AverageRating, ReviewCount, CompletedTourCount, IsVerified, IsActive, HourlyRate, Currency, ResponseTimeMinutes + Language/Specialization collections.

**What to do**: AvailabilitySlot already has `TourGuideId` (Guid). TourBooking gets a `GuideId` (Guid). Booking reads guide details via `ITourGuideProfileReader` contract (implemented in ContentTours.Contracts).

---

## Category 2 — DEFERRED POST-MVP (Remove Table, Keep Entity Class as Documentation)

These entities are shells (properties only, 0 domain methods, 0 handlers, 0 endpoints). They exist as future placeholders. Remove EF configurations and DbSet registrations. Keep the C# classes as documentation with a `[NotMapped]` marker or move to a `_Deferred/` folder.

| # | Entity | Module | Lines | Why Defer |
|---|--------|--------|-------|-----------|
| 4 | `LoyaltyPoints` | Finance | 16 | Shell. No handlers/endpoints. Deferred per Finance Decision #5. |
| 5 | `LoyaltyTransaction` | Finance | ~15 | Shell. Child of #4. |
| 6 | `Referral` | Finance | 17 | Shell. No handlers/endpoints. Deferred per Finance Decision #5. |
| 7 | `Subscription` | Finance | 18 | Shell. Deferred per Finance Decision #6. |
| 8 | `SubscriptionPlan` | Finance | ~15 | Shell. Child of #7. |
| 9 | `SubscriptionFeature` | Finance | ~15 | Shell. Child of #7. |
| 10 | `PlanFeature` | Finance | ~10 | Join entity for #8/#9. |
| 11 | `ChatBotConversation` | Messaging | ~15 | Shell. Deferred per Messaging Decision #1. |
| 12 | `ChatBotMessage` | Messaging | ~15 | Shell. Child of #11. |
| 13 | `PackageBooking` | Booking | ~21 | Shell. Package booking deferred post-MVP per Booking-Workflow. |
| 14 | `Reservation` | Booking | ~26 | Shell. Business reservations deferred per Booking-Workflow. |

**Impact**: 11 tables, ~33 files (entity + EF config + optional repository interface). Zero runtime impact since nothing references them.

**Action per entity**:
1. Remove `DbSet<T>` from DbContext
2. Remove EF configuration class
3. Remove any empty repository interface
4. Move entity class to `Domain/Entities/_Deferred/` folder with comment header

---

## Category 3 — REPLACED / DEAD CODE (Delete Entirely)

These entities are superseded by plan decisions or confirmed dead code. Delete all artifacts.

| # | Entity | Module | Reason | Plan Reference |
|---|--------|--------|--------|----------------|
| 15 | `ProviderBankAccount` | Finance | **REPLACED** by unified `PaymentMethod` entity (bank + mobile wallets) | Finance Decision #7 |
| 16 | `CreatorPost` | ContentBlogs | **DELETED** by Blog ← CreatorPost merger | BlogCreatorPost-Merger plan |
| 17 | `AccessibilityReview` | Social | **DEAD CODE** — no handlers, no endpoints, no references | Social Decision #10 |

**Impact**: 3 tables. CreatorPost alone accounts for ~69 files (entity, 2 enums, repo, 30 handlers, 10 queries, validator, 5 event handlers, infra, endpoints). ProviderBankAccount: ~6 files. AccessibilityReview: ~3 files.

---

## Category 4 — CACHE TABLES → HybridCache (Remove Table, Use In-Memory Cache)

These entities serve as database-backed caches with explicit TTL/expiry. HybridCache (already used across the solution) provides the same functionality without database overhead.

| # | Entity | Module | Current | Replacement |
|---|--------|--------|---------|-------------|
| 18 | `DashboardCache` | Analytics | Key-Value JSON with ExpiresAt | **HybridCache** with 1h TTL. Key: `analytics:dashboard:{key}`. Pre-warmed by DashboardPreComputeService. |
| 19 | `RecommendationCache` | Analytics | Per-user scored results with batch tracking and ExpiresAt | **HybridCache** with 4h TTL. Key: `analytics:recs:{userId}:{entityKind}`. Pre-warmed by RecommendationService. |
| 20 | `IngestDebounceMarker` | Analytics | EntityType+EntityId+LastFlagged debounce | **HybridCache** with 5min TTL. Key: `analytics:debounce:{entityType}:{entityId}`. Natural expiry replaces manual cleanup. |

**Impact**: 3 tables, ~9 files. HybridCache already configured with Redis L2 in all modules.

**Why this works**: All three have explicit expiry semantics. DashboardCache has `ExpiresAt`. RecommendationCache has `ExpiresAt`. IngestDebounceMarker is a simple "has this been flagged recently?" check — perfect for cache TTL.

**Caution**: If the app has multiple instances without shared Redis, debounce could fire twice. But HybridCache with Redis L2 (already configured) handles this.

---

## Category 5 — CROSS-MODULE SNAPSHOTS → Direct Query via Contracts

These are denormalized "read-model" copies of data from other modules, fed by integration events. The CQRS snapshot pattern is legitimate for high-throughput analytics, but for MVP these modules have low traffic. Replace with cross-module query contracts + HybridCache.

| # | Entity | Module | Source Module | Replacement |
|---|--------|--------|--------------|-------------|
| 21 | `BusinessSnapshot` | Social | ContentPlaces | `IBusinessSummaryReader` contract. Cache 5min. |
| 22 | `PlaceSnapshot` | Social | ContentPlaces | `IPlaceSummaryReader` contract. Cache 5min. |
| 23 | `TourSnapshot` | Social | ContentTours | `ITourSummaryReader` contract. Cache 5min. |
| 24 | `BookingSnapshot` | Analytics | Booking | `IBookingStatsReader` contract. Cache 5min. |
| 25 | `PaymentSnapshot` | Analytics | Finance | `IPaymentStatsReader` contract. Cache 5min. |

**Impact**: 5 tables, ~15 files (entity + EF config + repository + event handlers that maintain them).

**Trade-off**: Removes ~5 integration event handlers per snapshot (Created/Updated/Deleted events). Adds ~5 cross-module contract interfaces. Net file change: roughly neutral, but removes ongoing maintenance of event sync + potential staleness bugs.

**What to KEEP as snapshots (performance-critical):**
- `EntityAttributeSnapshot` (Analytics) — 200+ lines, rich, used in every recommendation query. Cross-module query would be too expensive.
- `EntityPopularitySnapshot` (Analytics) — Time-series data, needs historical snapshots for trending.
- `BookingEligibilitySnapshot` (Social) — Has domain logic (`IsEligibleForVerifiedReview`), queried on every review creation.
- `UserSnapshot` (Messaging) — Minimal (4 fields), but used in every email render. Keep for email reliability.

---

## Entities NOT Candidates (Confirmed KEEP)

### Core Business Entities (must persist)
All entities in: Accounts, Auth, Security, ContentCore, ContentPlaces, ContentTours, ContentSeo, Tracking — **ALL KEEP** (no candidates).

### Rich Domain Entities (have behavior/state machines)
- Finance: Payment, PaymentExpectation, Payout/PayoutItem, Invoice/InvoiceItem, CommissionRule, Discount, DiscountUsage
- Finance: Dispute, DisputeEvidence, DisputeMessage — shells BUT planned for full rewrite in Finance-Workflow Phase 4
- Booking: TourBooking, AvailabilitySlot, SlotLock, RefundPolicy, JoinRequest (planned methods in Booking-Workflow), ProviderDocument (has behavior)
- Social: Review, ReviewReply, Report, Favorite, ContentModerationLog, ProfanityBlocklistEntry, EntityRatingCache
- Messaging: Notification, NotificationDeliveryAttempt, NotificationPreference, NotificationTemplate, DeviceToken, SupportTicket, TicketMessage, AdminAssignmentRoster
- Analytics: AuditLog, PopularityScore, SuggestionBatch, SuggestionMetric, Experiment, ExperimentAssignment, BoostPackage, SponsoredClickEvent, UserInteraction, UserPreference, UserPreferredCategory, UserExcludedEntity, GdprDeletionRequest, HolidayCalendar, SeasonalityRule, TripArc, EntityAttributeSnapshot, EntityPopularitySnapshot
- ContentBlogs: Blog, BlogComment, BlogCommentReaction, BlogTour, BlogTranslation, BlogView, CreatorProfile, CreatorApplication, CreatorFollow, CreatorInvitation, CreatorNiche

---

## Execution Priority

### Phase 1 — Quick Wins (no behavioral changes)
1. Remove 11 deferred shell entities' EF configs + DbSets (Category 2)
2. Delete AccessibilityReview dead code (Category 3, #17)
3. Move DashboardCache + IngestDebounceMarker to HybridCache (Category 4, #18, #20)

### Phase 2 — Medium Impact
4. Remove Booking.TourGuide duplicates, add cross-module contract (Category 1)
5. Remove RecommendationCache table → HybridCache (Category 4, #19)
6. Replace 3 Social snapshots with contracts (Category 5, #21-23)

### Phase 3 — Plan-Dependent (execute during respective workflow phases)
7. Delete ProviderBankAccount when PaymentMethod is built (Finance Phase 2)
8. Delete CreatorPost when Blog merger is executed (BlogCreatorPost-Merger Phase 4)
9. Replace 2 Analytics snapshots with contracts (Analytics Phase)

---

## Summary Table

| Module | Current Tables | After Optimization | Removed |
|--------|---------------|-------------------|---------|
| Accounts | 3 | 3 | 0 |
| Analytics | 24 | 19 | 5 |
| Auth | ~8 (actual tables) | ~8 | 0 |
| Booking | 11 | 5 | 6 |
| ContentBlogs | 12 | 11 | 1 |
| ContentCore | 12 | 12 | 0 |
| ContentPlaces | 10 | 10 | 0 |
| ContentSeo | 7 | 7 | 0 |
| ContentTours | 14 | 14 | 0 |
| Finance | 20 | 12 | 8 |
| Messaging | 11 | 9 | 2 |
| Security | ~6 (actual tables) | ~6 | 0 |
| Social | 12 | 10 | 2 |
| Tracking | 3 | 3 | 0 |
| **Total** | **~153** | **~129** | **~25** |

**Net savings**: ~25 database tables, ~87 files (entities + EF configs + repositories + event handlers), ~25 fewer EF migrations to maintain.
