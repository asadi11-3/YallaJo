# Social Module — Audit & Gap Report

> ## 🔶 GAPS PLANNED — Implementation Pending
>
> | Gap | Status | Resolution |
> |-----|--------|-----------|
> | Gap #1 — No Public Review Listing (HIGH) | 📋 PLANNED | Phase 3 — Add 2 AllowAnonymous GET endpoints + rating summary |
> | Gap #2 — ICurrentUser Redundant Auth Gates (MEDIUM) | 📋 PLANNED | Phase 1 — Remove from 18 endpoints |
> | Gap #3 — Zero HybridCache (MEDIUM) | 📋 PLANNED | Phase 2 — Add caching + SocialCacheKeys + tag invalidation |
> | Gap #4 — Dead Code & Dead Permissions (LOW) | 📋 PLANNED | Phase 4 — Delete AccessibilityReview, wire Warn/Ban endpoints |
> | Gap #5 — Missing Spec Features (MEDIUM) | 📋 PLANNED | Phase 5 — Discount notifications deferred, validators added, TimeProvider fix |
>
> **Additional enhancements**: ReviewHelpfulVote, UserModerationRecord, 3-strike auto-escalation, review photos via ContentCore
> **Plan**: `Agents/Plans/Social-Workflow.md` (14 decisions locked, 8 phases, ~64-76 new files)

> **Audited**: 2025-01-XX  
> **Sources of Truth**: `agent-context.md`, `YallaJo.md`, `YallaJo Business Rules & Edge Cases.pdf`, `Endpoints.pdf`  
> **Overall Score**: 7.0 / 10

---

## 1. Module Overview

| Layer | Project | Files | Purpose |
|-------|---------|-------|---------|
| Domain | `Social.Domain` | 42 | Entities, enums, domain events, repository interfaces |
| Application | `Social.Application` | 57 | CQRS handlers, validators, DTOs, integration event handlers |
| Contracts | `Social.Contracts` | 11 | Features, permissions, integration events, cross-module services |
| Infrastructure | `Social.Infrastructure` | 49 | Repos, EF configs, services, background services, outbox converters |
| Presentation | `Social.Presentation` | 5 | 4 endpoint groups + root mapper |
| Tests | `Social.Tests.Unit` + `Social.IntegrationTests` | 2 | Permission catalog test + DB scaffold test |
| **Total** | | **~166** | |

### Entities (12)

| Entity | Lines | Role |
|--------|-------|------|
| Review | 189 | Main aggregate — reviews with state machine, replies, profanity detection, auto-hide |
| ReviewReply | 34 | Child entity of Review — provider responses |
| Favorite | 49 | Aggregate — user favorites with soft-delete toggle |
| Report | 78 | Aggregate — content reports with resolution lifecycle |
| EntityRatingCache | 74 | Aggregate — Bayesian rating cache per target entity |
| BookingEligibilitySnapshot | 45 | Cross-module snapshot — verified booking eligibility |
| ContentModerationLog | 51 | Append-only audit trail for moderation actions |
| AccessibilityReview | 18 | **DEAD CODE** — entity + EF config + table exist, zero handlers/endpoints |
| ProfanityBlocklistEntry | 21 | Blocklist word + language code for profanity filter |
| TourSnapshot | — | Cross-module denormalized copy |
| PlaceSnapshot | — | Cross-module denormalized copy |
| BusinessSnapshot | — | Cross-module denormalized copy |

### Enums (8)

ReviewStatus, ReportStatus, ModerationAction, ReportReason, ReviewTargetType, FavoriteEntityType, ReportableEntityType, ReviewDeletionSource

### Repository Interfaces (10)

IReviewRepository, IReportRepository, IFavoriteRepository, IEntityRatingCacheRepository, IContentModerationLogRepository, IBookingEligibilitySnapshotRepository, ITourSnapshotRepository, IPlaceSnapshotRepository, IBusinessSnapshotRepository, ISocialOutboxWriter

### Domain Events (12)

ReviewPublished, ReviewEdited, ReviewDeleted, ReviewAutoHidden, ReviewRestored, ReviewReplyAdded, FavoriteAdded, FavoriteRemoved, ReportSubmitted, ReportResolved, EntityRatingRecalculated, EntityAutoActioned

### Integration Events — Outbound (6)

ReviewPublishedIntegrationEvent, ReviewDeletedIntegrationEvent, FavoriteAddedIntegrationEvent, ReportSubmittedIntegrationEvent, ReportResolvedIntegrationEvent, RatingRecalculatedIntegrationEvent

### Integration Events — Inbound Handlers (8)

BookingTourBookingCompleted, ContentPlacesPlaceCreated/Updated/Deleted, ContentPlacesBusinessCreated, ContentToursTourCreated/Updated/Deleted

### Permission Catalog (19 permissions)

| Feature | Permissions | Group |
|---------|-------------|-------|
| Review | Read, Create, Update, Delete | ContentManagement |
| ReviewReply | Create, Update, Delete | ContentManagement |
| Favorite | Create, Read, Delete | ContentManagement |
| Report | Create | ContentManagement |
| Report | Read | ModerationTools |
| ContentModerationLog | Read | ModerationTools |
| AdminModerationQueue | Read, Approve, Remove, Resolve, **Warn**, **Ban** | ModerationTools |

**Dead permissions**: `AdminModerationQueue.Warn`, `AdminModerationQueue.Ban` — no endpoints use these.

---

## 2. Architecture Compliance

| Check | Status |
|-------|--------|
| Clean Architecture dependency graph | OK — Domain has zero outward deps |
| CQRS via MediatR | OK — all 18 handlers route through MediatR pipeline |
| One handler per command/query | OK — 12 commands, 6 queries, each with dedicated handler |
| FluentValidation | OK — 6 validators (AddFavorite, AddReviewReply, CreateReview, EditReview, SubmitReport, UpdateReviewReply) |
| Outbox/Inbox pattern | OK — ISocialOutboxWriter + ISocialInboxStore |

---

## 3. Endpoint Security Audit

### ReviewEndpoints.cs — 11 endpoints

| Method | Route | Auth | Permission |
|--------|-------|------|------------|
| POST | `/api/v1/reviews` | MustHavePermission | Review.Create |
| PUT | `/api/v1/reviews/{id}` | MustHavePermission | Review.Update |
| DELETE | `/api/v1/reviews/{id}` | MustHavePermission | Review.Delete |
| POST | `/api/v1/reviews/{id}/reply` | MustHavePermission | ReviewReply.Create |
| PUT | `/api/v1/reviews/{id}/reply/{replyId}` | MustHavePermission | ReviewReply.Update |
| DELETE | `/api/v1/reviews/{id}/reply/{replyId}` | MustHavePermission | ReviewReply.Delete |
| GET | `/api/v1/reviews/my-reviews` | MustHavePermission | Review.Read |
| GET | `/api/v1/reviews/admin/flagged` | MustHavePermission | AdminModerationQueue.Read |
| POST | `/api/v1/reviews/{id}/report` | MustHavePermission | Report.Create |
| POST | `/api/v1/reviews/admin/{id}/approve` | MustHavePermission | AdminModerationQueue.Approve |
| POST | `/api/v1/reviews/admin/{id}/remove` | MustHavePermission | AdminModerationQueue.Remove |

### FavoriteEndpoints.cs — 4 endpoints

| Method | Route | Auth | Permission |
|--------|-------|------|------------|
| POST | `/api/v1/favorites` | MustHavePermission | Favorite.Create |
| DELETE | `/api/v1/favorites/{entityType}/{entityId}` | MustHavePermission | Favorite.Delete |
| GET | `/api/v1/favorites` | MustHavePermission | Favorite.Read |
| GET | `/api/v1/favorites/check/{entityType}/{entityId}` | MustHavePermission | Favorite.Read |

### ReportEndpoints.cs — 3 endpoints

| Method | Route | Auth | Permission |
|--------|-------|------|------------|
| POST | `/api/v1/reports` | MustHavePermission | Report.Create |
| GET | `/api/v1/reports/admin` | MustHavePermission | AdminModerationQueue.Read |
| POST | `/api/v1/reports/admin/{id}/resolve` | MustHavePermission | AdminModerationQueue.Resolve |

### ModerationEndpoints.cs — 1 endpoint

| Method | Route | Auth | Permission |
|--------|-------|------|------------|
| GET | `/api/v1/moderation/logs` | MustHavePermission | ContentModerationLog.Read |

**Result: 19/19 endpoints have MustHavePermission + RequireAuthorization** | **ZERO AllowAnonymous**

---

## 4. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | MustHavePermission on every endpoint | PASS | 19/19 endpoints decorated |
| 2 | ICurrentUser only for ownership | WARN | Zero in Application/Infrastructure; 18 endpoint-level auth gates (see Gap #1) |
| 3 | Result pattern (no throw in Application) | PASS | Zero `throw new` in 57 Application files |
| 4 | No SaveChanges in domain event handlers | PASS | 6 outbox converters — zero SaveChanges; 8 integration handlers — SaveChanges OK |
| 5 | PermissionCatalog per module | PASS | SocialPermissionCatalog with 19 permissions |
| 6 | DateTime.UtcNow (not .Now/.Today) | WARN | Zero .Now/.Today; 1 violation: ReviewReply.cs:30 uses DateTime.UtcNow without TimeProvider |
| 7 | Guid.CreateVersion7 (not NewGuid) | PASS | Zero Guid.NewGuid() |
| 8 | FluentValidation on commands | PASS | 6 validators for write commands |
| 9 | HybridCache for read-heavy queries | FAIL | Zero HybridCache usage in entire module (see Gap #3) |
| 10 | Outbox for cross-module events | PASS | ISocialOutboxWriter + 6 converters |
| 11 | throw new in Infrastructure | PASS | 3 occurrences — all startup/seeding guards |
| 12 | Cross-module via Contracts only | PASS | IReviewOwnershipService, IProfanityFilter, INsfwClassifier |

---

## 5. Gap #1 — No Public Review Listing Endpoint (HIGH)

**Severity**: HIGH  
**Impact**: Frontend cannot display reviews for any tour, place, or business. Core product functionality is blocked.  
**Rule Violated**: Spec §6 Reviews — "Anyone can view reviews for any entity"

### Missing Endpoints

The module has **ZERO AllowAnonymous endpoints**. There is no way for unauthenticated users (or even authenticated users browsing) to see reviews for a given entity.

**Required endpoints** (from spec + Endpoints.pdf Wave 6):

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/v1/reviews/{targetType}/{targetId}` | AllowAnonymous | List reviews for entity with pagination |
| GET | `/api/v1/reviews/{id}` | AllowAnonymous | Get single review by ID |

### Why This Matters

Without public review endpoints, the entire Social module is invisible to the browsing experience. Reviews, ratings, and verified-booking badges — all spec-required for product pages — cannot be rendered.

### Required Fix

1. Add `GetEntityReviewsQuery` + handler with cursor-based pagination, filtering (rating, verified-only), and sorting (newest, highest, most helpful)
2. Add `GetReviewByIdQuery` + handler
3. Add two AllowAnonymous GET endpoints
4. Include `EntityRatingCache` summary in response (aggregate rating, review count, Bayesian score)

**Effort**: ~4h

---

## 6. Gap #2 — ICurrentUser Redundant Auth Gates in Presentation (MEDIUM)

**Severity**: MEDIUM  
**Impact**: Convention violation — 18/19 endpoints manually check `IsAuthenticated` and `UserId is null` despite already having `.RequireAuthorization()` + `.MustHavePermission()`.  
**Rule Violated**: agent-context.md §0 Rule 2 — "ICurrentUser: Inject it only when you need to compare the caller's UserId against the resource owner"

### Violation Pattern

```csharp
// Every endpoint does this:
async ([AsParameters] CreateReviewRequest request, ICurrentUser currentUser, ...) =>
{
    if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        return Results.Unauthorized();  // ← REDUNDANT — RequireAuthorization already enforces this

    var command = new CreateReviewCommand(currentUser.UserId.Value, ...);
    // ...
}
```

### Affected Endpoints (18/19)

All endpoints in ReviewEndpoints.cs (10), ReportEndpoints.cs (3), ModerationEndpoints.cs (1), FavoriteEndpoints.cs (4) — except `GET /favorites/check` which doesn't use ICurrentUser.

### Why Wrong

The `.RequireAuthorization()` middleware already guarantees the user is authenticated before the endpoint delegate runs. The manual `IsAuthenticated` check is dead code that obscures the actual purpose of `ICurrentUser` (extracting UserId for command input).

### Required Fix

1. Remove `if (!currentUser.IsAuthenticated || currentUser.UserId is null)` checks from all 18 endpoints
2. Keep `currentUser.UserId.Value` extraction for command input (this is acceptable — userId propagation for ownership)
3. Keep admin-bypass derivation in delete endpoints (uses `HasPermission(...)`) — acceptable at endpoint level

**Effort**: ~2h

---

## 7. Gap #3 — Zero HybridCache Usage (MEDIUM)

**Severity**: MEDIUM  
**Impact**: No cache invalidation on writes, no cache reads on queries. All other content modules use HybridCache extensively (ContentCore: 20+, ContentTours: 29, ContentBlogs: 40).  
**Rule Violated**: agent-context.md §3 — HybridCache pattern with `RemoveByTagAsync` for cache invalidation

### Missing Cache Points

**Queries that should cache**:
- `GetMyReviewsQueryHandler` — user's own reviews (changes infrequently)
- `GetMyFavoritesQueryHandler` — user's favorites list
- `CheckFavoriteQueryHandler` — favorite check (hot path, called on every entity page)
- `GetFlaggedReviewsQueryHandler` — admin queue
- `GetModerationLogsQueryHandler` — admin audit trail

**Commands that should invalidate**:
- All 12 command handlers should call `RemoveByTagAsync` with cache tags like `SocialCacheKeys.Reviews`, `SocialCacheKeys.Favorites`, etc.

### Required Fix

1. Define `SocialCacheKeys` static class with tag constants
2. Inject `HybridCache` into all 12 command handlers for `RemoveByTagAsync` after writes
3. Add `ICacheableQuery` or direct `GetOrCreateAsync` in 5 query handlers

**Effort**: ~3h

---

## 8. Gap #4 — Dead Code & Dead Permissions (LOW)

**Severity**: LOW  
**Impact**: Unnecessary code maintenance, confusing permission catalog, dead DB table

### 8.1 AccessibilityReview Entity (Dead Code)

- **Domain**: `AccessibilityReview.cs` (18 lines) — minimal entity with no factory, no domain events
- **Infrastructure**: `AccessibilityReviewConfiguration.cs` — EF config + DB table `social.AccessibilityReviews`
- **Application**: ZERO references — no commands, queries, handlers, or validators
- **Presentation**: ZERO endpoints

The entity exists in migrations and has a live DB table but is completely inert. Spec §14 (Accessibility) describes accessibility *features* and *reviews* as part of ContentPlaces, not Social.

### 8.2 Dead Permissions

| Permission | Feature | Status |
|------------|---------|--------|
| `AdminModerationQueue.Warn` | ModerationTools | No endpoint, no handler |
| `AdminModerationQueue.Ban` | ModerationTools | No endpoint, no handler |

### 8.3 Dead Domain Event

`EntityAutoActionedDomainEvent` — defined in Domain, never raised by any entity method.

### Required Fix

1. Remove `AccessibilityReview.cs`, its EF config, and create a migration to drop the table (or document it as Phase 2 placeholder)
2. Remove `Warn`/`Ban` permissions or add Warn/Ban endpoints
3. Remove `EntityAutoActionedDomainEvent` or wire it to auto-hide logic

**Effort**: ~1h (removal) or ~4h (if implementing Warn/Ban endpoints)

---

## 9. Gap #5 — Missing Spec Features (MEDIUM)

**Severity**: MEDIUM  
**Impact**: Several spec-required features are not implemented

### 9.1 Discount-to-Wishlist Notifications (Spec §7)

Spec says: "When a tour in user's favorites gets a discount, notify up to 3/day."

**Current state**: Zero discount notification logic in Social module. No integration event handler for discount events. The Favorite entity stores favorites but has no notification trigger.

**Required**: Consume `DiscountCreatedIntegrationEvent` from future Discount module, match against favorites, queue notifications via Messaging module.

### 9.2 Missing Review Validators

- `ApproveReviewCommand` — no validator (should validate reviewId not empty)
- `RemoveReviewCommand` — no validator (should validate reviewId + reason)
- `DeleteReviewCommand` — no validator
- `DeleteReviewReplyCommand` — no validator
- `RemoveFavoriteCommand` — no validator

While these are simple ID-based operations, FluentValidation validators ensure pipeline consistency and provide meaningful error messages.

### 9.3 ReviewReply DateTime.UtcNow Violation

`ReviewReply.cs:30` uses `DateTime.UtcNow` directly instead of `TimeProvider`. This is the only DateTime violation in the entire module.

### Required Fix

1. Discount notifications: deferred to Finance/Discount module implementation (~2h integration)
2. Add missing validators: ~1h
3. Fix ReviewReply to use TimeProvider: ~10min

**Effort**: ~3h

---

## 10. What Passed — Full Checklist

### Endpoint Authorization (19/19)
Every endpoint has `MustHavePermission` + `RequireAuthorization`. The Social module is the most restrictive module audited — zero AllowAnonymous. This is correct for user-generated content operations but missing for public display (Gap #1).

### Result Pattern
Zero `throw new` in all 57 Application files. All 12 command handlers use `Result.Failure`/`Result.Success` consistently. Spot-checked CreateReview, ApproveReview, ResolveReport, AddFavorite, RemoveFavorite — all clean.

### SaveChanges in Event Handlers
- 6 domain event → outbox converters: zero SaveChangesAsync (write OutboxMessage rows only)
- 8 integration event handlers: SaveChangesAsync in own scope — correct

### DateTime/GUID Conventions
- Zero `DateTime.Now` or `DateTime.Today` in entire module
- Zero `Guid.NewGuid()` in entire module
- One `DateTime.UtcNow` in ReviewReply.cs (minor, see Gap #5.3)

### ICurrentUser in Application/Infrastructure
**ZERO** — third module with no ICurrentUser in business logic (after ContentSeo and Analytics). All ICurrentUser usage is at endpoint level for userId propagation.

### Business Logic Compliance (Reviews)
- S-R1: Anyone can create review ✅, verified booking badge via BookingEligibilitySnapshot with 30-day window ✅
- S-R2: One review per (UserId, TargetType, TargetId) — enforced in CreateReviewCommandHandler ✅
- S-R3: 48h edit window — enforced in Review.Edit() domain method ✅
- S-R4: Profanity detection → AwaitingModeration status ✅
- S-R5: 5 unique reports → AutoHide (AutoHideThreshold = 5) ✅
- S-R6: Rating 1.0-5.0 in 0.5 increments ✅
- S-R7: Max 500 favorites per user (MaxFavoritesPerUser = 500) ✅

### Bayesian Rating Algorithm
`RatingRecalculationService` (190 lines):
- Daily at 03:00 UTC ✅ (spec §18)
- Verified weight = 1.0, non-verified = 0.5 ✅
- Recency: <90d = 1.0, <180d = 0.7, >180d = 0.5 ✅
- Bayesian prior C = 10 ✅
- Global average = 3.0
- Min reviews to display = 3 ✅

### Orphaned Favorites Cleanup
`OrphanedFavoritesCleanupService` (146 lines): Weekly Saturday 03:00 UTC. Checks Tour/Place/Business snapshots for deleted entities, soft-deletes orphaned favorites. ✅

### Cross-Module Integration
- **Inbound**: 8 handlers consume events from Booking (eligibility snapshot), ContentPlaces (place/business snapshots), ContentTours (tour snapshots)
- **Outbound**: 6 integration events via outbox for Analytics, Messaging, etc.
- **Service**: IReviewOwnershipService for cross-module ownership checks
- **External contracts**: IProfanityFilter + INsfwClassifier with pluggable implementations (blocklist + noop stubs)

### Outbox/Inbox Pattern
ISocialOutboxWriter + ISocialInboxStore. 6 domain event converters write OutboxMessage rows without SaveChanges. Integration handlers use inbox deduplication.

---

## 11. Scorecard

| Area | Score | Notes |
|------|-------|-------|
| Endpoint Security | 9/10 | 19/19 gated; -1 for missing public GETs |
| ICurrentUser Compliance | 7/10 | Zero in Application (excellent); 18 redundant endpoint auth gates |
| Result Pattern | 10/10 | Zero exceptions in Application |
| Domain Event Handlers | 10/10 | Zero SaveChanges violations |
| Convention Compliance | 8/10 | -1 ReviewReply DateTime, -1 zero HybridCache |
| Business Logic | 8/10 | Review/Rating/Favorites spec-compliant; missing public listing + discount notifications |
| Cross-Module Design | 9/10 | Clean snapshot strategy; outbox/inbox; pluggable services |
| Code Hygiene | 6/10 | Dead entity, dead permissions, dead domain event |
| Test Coverage | 3/10 | 2 test files (1 unit + 1 integration scaffold) |
| Spec Completeness | 6/10 | Missing public review listing, discount notifications, review validators |
| **Overall** | **7.0/10** | |

---

## 12. Fix Priority & Recommendations

| # | Gap | Severity | Effort | Priority |
|---|-----|----------|--------|----------|
| 1 | Add public review listing endpoints (GET reviews for entity) | HIGH | ~4h | P1 |
| 2 | Add HybridCache to queries + invalidation to commands | MEDIUM | ~3h | P1 |
| 3 | Remove ICurrentUser redundant auth gates (18 endpoints) | MEDIUM | ~2h | P2 |
| 4 | Add missing command validators (5) | MEDIUM | ~1h | P2 |
| 5 | Remove/implement AccessibilityReview dead code | LOW | ~1h | P3 |
| 6 | Remove dead permissions (Warn/Ban) or add endpoints | LOW | ~1-4h | P3 |
| 7 | Fix ReviewReply DateTime.UtcNow | LOW | ~10min | P3 |
| 8 | Remove EntityAutoActionedDomainEvent dead code | LOW | ~10min | P3 |
| 9 | Discount-to-wishlist notification integration | MEDIUM | ~2h | P4 (deferred) |
| 10 | Expand test coverage | LOW | ~8h | P4 |

**Total estimated effort**: ~21-25h

---

## Appendix — Files Audited

### Domain (42 files)
- Entities: Review.cs, ReviewReply.cs, Favorite.cs, Report.cs, EntityRatingCache.cs, BookingEligibilitySnapshot.cs, ContentModerationLog.cs, AccessibilityReview.cs, ProfanityBlocklistEntry.cs, TourSnapshot.cs, PlaceSnapshot.cs, BusinessSnapshot.cs
- Enums: ReviewStatus.cs, ReportStatus.cs, ModerationAction.cs, ReportReason.cs, ReviewTargetType.cs, FavoriteEntityType.cs, ReportableEntityType.cs, ReviewDeletionSource.cs
- Events: 12 domain event files
- Repositories: 10 interface files

### Application (57 files)
- Commands: 12 handlers + 12 command records + 6 validators
- Queries: 6 handlers + 6 query records + 3 DTOs
- EventHandlers: 8 integration event handlers
- Interfaces: ISocialInboxStore, ISocialUnitOfWork
- DependencyInjection.cs

### Contracts (11 files)
- SocialFeatures.cs, SocialPermissionCatalog.cs
- 6 integration events
- IReviewOwnershipService, IProfanityFilter, INsfwClassifier

### Infrastructure (49 files)
- SocialIntegrationConverters.cs (6 outbox converters)
- 10 repositories, 12 EF configs, 5 services
- RatingRecalculationService.cs, OrphanedFavoritesCleanupService.cs + options
- SocialDbContext.cs, SocialUnitOfWork.cs, SocialInboxStore.cs
- 5 migrations + snapshot + SocialDbInitializer.cs

### Presentation (5 files)
- ReviewEndpoints.cs (11 endpoints), FavoriteEndpoints.cs (4), ReportEndpoints.cs (3), ModerationEndpoints.cs (1), SocialEndpoints.cs (mapper)

### Tests (2 files)
- SocialPermissionCatalogTests.cs, SocialDbContextScaffoldTests.cs
