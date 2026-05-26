# Social Workflow Plan

> **Module**: Social  
> **Dependencies**: Booking (eligibility snapshots), ContentPlaces/Tours (entity snapshots), ContentCore (Attachments), Security (ban enforcement)  
> **Compatible With**: BlogCreatorPost-Merger.md, Booking-Workflow.md, TourGuide-Flow.md, Finance-Workflow.md, Role-System.md  
> **Status**: Implemented (audited 2025-01-27, score 7.8/10). W3-B fixes applied. See `Social-Audit-Report.md`.

---

## Design Decisions (17 — ALL LOCKED)

| # | Decision | Detail |
|---|----------|--------|
| 1 | No blog reviews | Reviews for Tour, Place, Business, TourGuide only. Blog engagement = comments + reactions (ContentBlogs). |
| 2 | ReportableEntityType cleanup | Remove CreatorPost=5. Final: Review=0, Tour=1, Place=2, Business=3, Blog=4, TourGuide=5 |
| 3 | FavoriteEntityType expansion | Add Blog=3, TourGuide=4. Users can favorite blogs and guides. |
| 4 | Warn + Temporary Ban | Admin can warn or temp-ban users. 3-strike auto-escalation. |
| 5 | Security module enforces bans | Social fires events. Security adds ban record + middleware rejects globally. |
| 6 | 3-strike escalation | 1st=Warning, 2nd=7-day ban, 3rd=30-day ban, 4th=permanent (SuperAdmin only). |
| 7 | Discount notifications deferred | Post-MVP. Depends on Finance discount CRUD being live. |
| 8 | Public endpoints: reviews + rating | GET reviews for entity + GET single review + GET rating summary = AllowAnonymous. |
| 9 | Single provider reply | Keep current: one reply per review. No threads. |
| 10 | Delete AccessibilityReview | Remove dead entity, EF config, drop table. ContentPlaces handles accessibility. |
| 11 | Review helpful votes | Add ReviewHelpfulVote entity (simple up-vote). Affects sort order. |
| 12 | Review photos via ContentCore | Add EntityType.Review to ContentCore enum. Max 5 photos per review. |
| 13 | Reply ownership: Owner + Admin | Only entity owner (provider) OR admin can post official reply to review. Verified via IReviewOwnershipService. |
| 14 | Tag-based cache, 5-min TTL | HybridCache on public review lists + rating summaries. Invalidate on writes. |
| 15 | **Booking-verified reviews only** | Only users with a Completed booking for the entity can write a review. Resolves CONTRADICTION-2. |
| 16 | **Half-star rating scale (0.5)** | Rating values: 1.0, 1.5, 2.0 ... 5.0. Stored as `decimal(2,1)`. Resolves CONTRADICTION-3. |
| 17 | **3-report auto-hide threshold** | Content auto-hidden after 3 reports. Admin can override. Resolves CONTRADICTION-4. |

---

## Current State Summary

### ✅ Built (Keep)
- **Review** (189L): Full state machine — Create, Edit (48h window), Delete, AutoHide (5 reports), Restore, AddReply
- **Favorite** (49L): Add/Remove with soft-delete, max 500 per user
- **Report** (78L): Submit → UnderReview → Resolve/Dismiss
- **EntityRatingCache** (74L): Bayesian rating with verified-booking weight
- **BookingEligibilitySnapshot** (45L): Cross-module booking verification
- **ContentModerationLog** (51L): Append-only audit trail
- **ProfanityBlocklistEntry** (21L): Word blocklist for auto-moderation
- **Snapshots** (Tour, Place, Business): Denormalized copies for orphan cleanup
- **Commands** (12): CreateReview, EditReview, DeleteReview, ApproveReview, RemoveReview, AddReply, UpdateReply, DeleteReply, AddFavorite, RemoveFavorite, SubmitReport, ResolveReport
- **Queries** (7): GetMyReviews, GetFlaggedReviews, GetMyFavorites, CheckFavorite, GetAdminReports, GetModerationLogs
- **Background Services** (2): RatingRecalculationService (daily 03:00), OrphanedFavoritesCleanupService (weekly Sat 03:00)
- **Integration Event Handlers** (8): Booking completed, Place/Business/Tour CRUD snapshots
- **Endpoints** (19): ReviewEndpoints (11), FavoriteEndpoints (4), ReportEndpoints (3), ModerationEndpoints (1)

### ❌ Missing / To Build
- Public review listing endpoints (AllowAnonymous)
- HybridCache on all queries
- Auth gate cleanup (18 endpoints)
- ReviewHelpfulVote entity + endpoints
- Warn/Ban system (entity + handlers + Security integration)
- FavoriteEntityType expansion (Blog, TourGuide)
- ReportableEntityType cleanup (remove CreatorPost, add TourGuide)
- Delete AccessibilityReview dead code
- Missing validators (5 commands)
- ReviewReply TimeProvider fix
- EntityAutoActionedDomainEvent cleanup
- Review photos (EntityType.Review in ContentCore)

---

## Entity Design

### New: ReviewHelpfulVote

```csharp
public sealed class ReviewHelpfulVote : BaseEntity
{
    private ReviewHelpfulVote() { }
    
    public Guid ReviewId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime VotedAt { get; private set; }
    
    public static ReviewHelpfulVote Create(Guid reviewId, Guid userId, TimeProvider timeProvider)
    {
        return new ReviewHelpfulVote
        {
            ReviewId = reviewId,
            UserId = userId,
            VotedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
    }
}
```

Constraint: Unique (ReviewId, UserId). One vote per user per review.

### New: UserModerationRecord (in Social.Domain)

```csharp
public sealed class UserModerationRecord : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public ModerationAction Action { get; private set; }       // WarnUser=2 or BanUser=3
    public string Reason { get; private set; }
    public Guid IssuedByAdminId { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }            // NULL = permanent (BanUser only)
    public bool IsActive { get; private set; }
    public int StrikeNumber { get; private set; }               // 1, 2, 3, 4
    public Guid? RelatedReportId { get; private set; }          // Optional link to triggering report
    
    public static UserModerationRecord IssueWarning(userId, reason, adminId, strikeNumber, reportId?, timeProvider) { }
    public static UserModerationRecord IssueTempBan(userId, reason, adminId, duration, strikeNumber, reportId?, timeProvider) { }
    public static UserModerationRecord IssuePermanentBan(userId, reason, adminId, strikeNumber, reportId?, timeProvider) { }
    public Result Revoke(Guid adminId, string reason, TimeProvider timeProvider) { }
}
```

### Modified: Review Entity — Add HelpfulVoteCount

```csharp
// Add to Review class:
public int HelpfulVoteCount { get; private set; }

public void IncrementHelpfulVotes() { HelpfulVoteCount++; MarkUpdated(); }
public void DecrementHelpfulVotes() { HelpfulVoteCount--; MarkUpdated(); }
```

### Modified Enums

**FavoriteEntityType** (expanded):
```csharp
public enum FavoriteEntityType : byte
{
    Tour      = 0,
    Place     = 1,
    Business  = 2,
    Blog      = 3,
    TourGuide = 4,
}
```

**ReportableEntityType** (cleaned):
```csharp
public enum ReportableEntityType : byte
{
    Review    = 0,
    Tour      = 1,
    Place     = 2,
    Business  = 3,
    Blog      = 4,
    TourGuide = 5,   // was CreatorPost=5, repurposed
}
```

---

## Warn/Ban Workflow (Decisions #4, #5, #6)

### Flow

```
1. Admin resolves report with Action = WarnUser or BanUser
   OR admin directly moderates from dashboard

2. Social creates UserModerationRecord:
   - Counts existing active records for user (strike count)
   - Strike 1 → Warning (no ban)
   - Strike 2 → 7-day temp ban
   - Strike 3 → 30-day temp ban
   - Strike 4+ → permanent ban (requires SuperAdmin role check)
   
3. Social fires integration event:
   - UserWarnedIntegrationEvent(UserId, Reason, StrikeNumber, IssuedAt)
   - UserBannedIntegrationEvent(UserId, Reason, Duration?, StrikeNumber, IssuedAt, ExpiresAt?)

4. Security module handles event:
   - Creates BanRecord in Security.Domain
   - Middleware checks BanRecord on every request
   - If active ban exists → 403 Forbidden with message "Account suspended until {ExpiresAt}"
   - If permanent → 403 "Account permanently suspended"

5. Ban expiry: background service in Security (or TTL-based check)
```

### Admin Moderation Endpoints (New)

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | /admin/moderation/warn/{userId} | Admin | Issue warning (with reason, optional reportId) |
| POST | /admin/moderation/ban/{userId} | Admin | Issue ban (system auto-determines duration based on strikes) |
| POST | /admin/moderation/revoke/{recordId} | Admin | Revoke active warning/ban |
| GET | /admin/moderation/history/{userId} | Admin | Get user's moderation history |
| GET | /admin/moderation/active-bans | Admin | List all currently active bans |

### Integration Events (Outbound to Security)

```csharp
public sealed record UserWarnedIntegrationEvent(
    Guid UserId, string Reason, int StrikeNumber, DateTime IssuedAt) : IntegrationEvent;

public sealed record UserBannedIntegrationEvent(
    Guid UserId, string Reason, int? DurationDays, int StrikeNumber, 
    DateTime IssuedAt, DateTime? ExpiresAt) : IntegrationEvent;

public sealed record UserBanRevokedIntegrationEvent(
    Guid UserId, Guid RevokedByAdminId, string Reason) : IntegrationEvent;
```

---

## Public Endpoints (Decision #8)

### New AllowAnonymous Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | /reviews/{targetType}/{targetId} | AllowAnonymous | List reviews for entity (paginated, sortable) |
| GET | /reviews/{id} | AllowAnonymous | Get single review by ID with replies |
| GET | /reviews/{targetType}/{targetId}/summary | AllowAnonymous | Rating summary (avg, count, distribution, Bayesian) |

### Query Parameters for Public Review Listing

- `page`, `pageSize` (max 50)
- `sortBy`: newest (default), highest, lowest, most_helpful
- `verifiedOnly`: bool (filter to verified booking reviews only)
- `minRating`: decimal (filter >= this rating)

### Response includes:
- Review content + rating + title + visitDate + isVerifiedBooking + helpfulVoteCount
- Attached photo URLs (from ContentCore Attachment where EntityType=Review)
- Provider reply (if exists)
- EntityRatingCache summary in header/metadata

---

## Review Helpful Votes (Decision #11)

### Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | /reviews/{id}/helpful | User | Mark review as helpful |
| DELETE | /reviews/{id}/helpful | User | Remove helpful vote |

### Logic
- One vote per user per review (unique constraint)
- Increments/decrements `Review.HelpfulVoteCount`
- Affects "most_helpful" sort order in public listing
- Vote does NOT require verified booking — any authenticated user can vote

---

## Review Photos (Decision #12)

### Integration with ContentCore

Add to ContentCore's EntityType enum:
```csharp
Review = 7   // (after existing: Tour=1, Place=2, Business=3, Blog=4, Package=5, ?)
```

The existing ContentCore Attachment endpoints (`POST /attachments`, `GET /attachments/{entityType}/{entityId}`) handle upload/retrieval. Social module's public review listing includes attachment URLs in response DTO.

### Validation in CreateReviewCommandHandler
- After review creation, frontend calls ContentCore attachment endpoint
- Max 5 photos per review (enforced at ContentCore level per EntityType config or at Social endpoint level)
- Accepted types: image/jpeg, image/png, image/webp

---

## Cache Strategy (Decision #14)

### SocialCacheKeys

```csharp
public static class SocialCacheKeys
{
    // Tags
    public const string ReviewsTag = "social:reviews";
    public const string FavoritesTag = "social:favorites";
    public const string RatingsTag = "social:ratings";
    public const string ReportsTag = "social:reports";
    
    // Keys
    public static string EntityReviews(ReviewTargetType type, Guid id) => $"social:reviews:{type}:{id}";
    public static string ReviewById(Guid id) => $"social:review:{id}";
    public static string RatingSummary(ReviewTargetType type, Guid id) => $"social:rating:{type}:{id}";
    public static string MyReviews(Guid userId) => $"social:my-reviews:{userId}";
    public static string MyFavorites(Guid userId) => $"social:my-favorites:{userId}";
    public static string CheckFavorite(Guid userId, FavoriteEntityType type, Guid entityId) => $"social:fav-check:{userId}:{type}:{entityId}";
}
```

### Cache Invalidation

| Command | Invalidates Tags |
|---------|-----------------|
| CreateReview | ReviewsTag, RatingsTag |
| EditReview | ReviewsTag, RatingsTag |
| DeleteReview | ReviewsTag, RatingsTag |
| ApproveReview | ReviewsTag |
| RemoveReview | ReviewsTag, RatingsTag |
| AddFavorite | FavoritesTag |
| RemoveFavorite | FavoritesTag |
| VoteHelpful | ReviewsTag |

TTL: 5 minutes for public listings, 10 minutes for rating summaries.

---

## Auth Gate Cleanup (Gap #2)

**18 endpoints** with redundant `if (!currentUser.IsAuthenticated || currentUser.UserId is null)` checks.

Fix: Remove auth gate block, change `.UserId.Value` → `.UserId!.Value`. Same pattern as ContentPlaces/ContentBlogs/ContentTours.

---

## Dead Code Removal (Decision #10)

### Delete:
1. `Social.Domain/Entities/AccessibilityReview.cs`
2. `Social.Infrastructure/Persistence/Configurations/AccessibilityReviewConfiguration.cs`
3. Migration to drop `social.AccessibilityReviews` table
4. `Social.Domain/Events/EntityAutoActionedDomainEvent.cs` (never raised)

### Cleanup:
5. Remove `CreatorPost = 5` from ReportableEntityType → rename to `TourGuide = 5`
6. Remove dead permissions: `AdminModerationQueue.Warn` and `AdminModerationQueue.Ban` from PermissionCatalog (will be replaced by new moderation endpoints with proper permissions)

---

## Missing Validators (Gap #5.2)

Add validators for:
1. `ApproveReviewCommand` — validate ReviewId not empty
2. `RemoveReviewCommand` — validate ReviewId + Reason not empty
3. `DeleteReviewCommand` — validate ReviewId not empty
4. `DeleteReviewReplyCommand` — validate ReviewId + ReplyId not empty
5. `RemoveFavoriteCommand` — validate EntityType valid + EntityId not empty

---

## Integration Events

### Outbound (New)
- `UserWarnedIntegrationEvent(UserId, Reason, StrikeNumber, IssuedAt)` → Security
- `UserBannedIntegrationEvent(UserId, Reason, Duration?, StrikeNumber, IssuedAt, ExpiresAt?)` → Security
- `UserBanRevokedIntegrationEvent(UserId, RevokedByAdminId, Reason)` → Security
- `ReviewHelpfulVoteAddedIntegrationEvent(ReviewId, VoterId)` → Analytics (optional)

### Outbound (Existing — keep)
- ReviewPublishedIntegrationEvent
- ReviewDeletedIntegrationEvent
- FavoriteAddedIntegrationEvent
- ReportSubmittedIntegrationEvent
- ReportResolvedIntegrationEvent
- RatingRecalculatedIntegrationEvent

### Inbound (New — from BlogCreatorPost-Merger)
- `BlogDeletedIntegrationEvent` → clean up favorites where EntityType=Blog
- `TourGuideDeactivatedIntegrationEvent` → clean up favorites where EntityType=TourGuide

### Inbound (Existing — keep)
- BookingTourBookingCompleted (eligibility snapshot)
- ContentPlaces Place/Business CRUD (snapshots)
- ContentTours Tour CRUD (snapshots)

---

## Execution Phases

### Phase 1: Auth Gate Cleanup
- Remove auth gates from 18 endpoints
- Change `.UserId.Value` → `.UserId!.Value`
- ~4 files modified (endpoint files)

### Phase 2: Dead Code + Enum Cleanup
- Delete AccessibilityReview entity + EF config
- Delete EntityAutoActionedDomainEvent
- Update ReportableEntityType (CreatorPost→TourGuide)
- Update FavoriteEntityType (add Blog=3, TourGuide=4)
- Remove dead permissions from catalog
- ~8 files modified, ~3 deleted

### Phase 3: Public Endpoints + Cache
- Create SocialCacheKeys static class
- Create GetEntityReviewsQuery + handler (paginated, filterable, cacheable)
- Create GetReviewByIdQuery + handler (with replies + photo URLs)
- Create GetRatingSummaryQuery + handler (from EntityRatingCache)
- Add HybridCache to all existing query handlers
- Add cache invalidation (RemoveByTagAsync) to all 12 command handlers
- Add 3 AllowAnonymous endpoints
- ~18-22 new files, ~19 modified

### Phase 4: ReviewHelpfulVote
- Create ReviewHelpfulVote entity + EF config
- Add HelpfulVoteCount to Review entity + EF config update
- Create IReviewHelpfulVoteRepository
- Create VoteHelpfulCommand + handler
- Create RemoveHelpfulVoteCommand + handler
- Add 2 endpoints (POST/DELETE /reviews/{id}/helpful)
- ~12-14 new files, ~3 modified

### Phase 5: Warn/Ban System
- Create UserModerationRecord entity + EF config
- Create IUserModerationRecordRepository
- Create IssueWarningCommand + handler (with strike calculation)
- Create IssueBanCommand + handler (auto-determines duration)
- Create RevokeModerationCommand + handler
- Create GetUserModerationHistoryQuery + handler
- Create GetActiveBansQuery + handler
- Add 5 admin endpoints
- Create integration events (UserWarned, UserBanned, UserBanRevoked)
- Create outbox converters (3)
- Update ResolveReportCommandHandler to optionally issue warn/ban
- ~28-32 new files, ~5 modified

### Phase 6: Review Photos (ContentCore Integration)
- Add EntityType.Review to ContentCore enum (1 file)
- Update public review listing DTO to include photoUrls
- Update GetEntityReviewsQueryHandler to join ContentCore attachments
- ~2-3 modified files

### Phase 7: Missing Validators + TimeProvider Fix
- Add 5 missing command validators
- Fix ReviewReply.cs DateTime.UtcNow → TimeProvider
- ~6 new files, ~1 modified

### Phase 8: Solution Build + Verify
- Build entire solution
- Fix test compilation errors
- Verify 0 errors

---

## File Count Estimate

| Category | New Files | Modified Files |
|----------|-----------|----------------|
| Phase 1: Auth gate | 0 | 4 |
| Phase 2: Dead code + enums | 0 | 8 (+3 deleted) |
| Phase 3: Public endpoints + cache | 18-22 | 19 |
| Phase 4: Helpful votes | 12-14 | 3 |
| Phase 5: Warn/Ban | 28-32 | 5 |
| Phase 6: Review photos | 0 | 3 |
| Phase 7: Validators + fix | 6 | 1 |
| **Total** | **~64-76** | **~43** |

---

## Endpoint Summary (Final State)

### Public (AllowAnonymous) — 3 NEW
- GET /reviews/{targetType}/{targetId} — paginated entity reviews
- GET /reviews/{id} — single review detail
- GET /reviews/{targetType}/{targetId}/summary — rating summary

### User Endpoints (Authenticated) — Existing + 2 NEW
- POST /reviews — create review
- PUT /reviews/{id} — edit review (48h window)
- DELETE /reviews/{id} — delete own review
- POST /reviews/{id}/helpful — NEW: vote helpful
- DELETE /reviews/{id}/helpful — NEW: remove helpful vote
- GET /reviews/my-reviews — my reviews
- POST /reviews/{id}/report — report a review
- POST /favorites — add favorite
- DELETE /favorites/{entityType}/{entityId} — remove favorite
- GET /favorites — my favorites
- GET /favorites/check/{entityType}/{entityId} — check if favorited
- POST /reports — submit report

### Provider Endpoints (Authenticated) — Existing
- POST /reviews/{id}/reply — add reply (ownership verified)
- PUT /reviews/{id}/reply/{replyId} — update reply
- DELETE /reviews/{id}/reply/{replyId} — delete reply

### Admin Endpoints — Existing + 5 NEW
- GET /reviews/admin/flagged — flagged review queue
- POST /reviews/admin/{id}/approve — approve flagged review
- POST /reviews/admin/{id}/remove — remove review
- GET /reports/admin — report queue
- POST /reports/admin/{id}/resolve — resolve report
- GET /moderation/logs — audit trail
- POST /admin/moderation/warn/{userId} — NEW: issue warning
- POST /admin/moderation/ban/{userId} — NEW: issue ban
- POST /admin/moderation/revoke/{recordId} — NEW: revoke ban/warning
- GET /admin/moderation/history/{userId} — NEW: user moderation history
- GET /admin/moderation/active-bans — NEW: active ban list

**Total: 29 endpoints** (19 existing + 3 public + 2 helpful + 5 moderation)

---

## Cross-Module Compatibility Matrix

| This Plan Depends On | What It Needs |
|---------------------|---------------|
| BlogCreatorPost-Merger.md | Blog favorites work after merger. Remove CreatorPost references. |
| Booking-Workflow.md | BookingEligibilitySnapshot for verified booking badges. |
| TourGuide-Flow.md | TourGuide favorites + reviews + reports. Guide responds to reviews on their tours. |
| ContentPlaces-Workflow.md | Place/Business snapshots for orphan cleanup. |
| Role-System.md | Admin role required for moderation endpoints. SuperAdmin for permanent bans. |
| Finance-Workflow.md | No direct dependency. Future: discount notifications (deferred). |

| Other Plans Depend On This | What They Need |
|---------------------------|---------------|
| Security (Role-System.md) | Consume UserWarned/UserBanned events → enforce bans globally. |
| TourGuide-Flow.md | Guide reviews affect trust tier calculation. |
| Analytics | ReviewPublished/FavoriteAdded events for popularity scoring. |

---

## Deferred Items (Post-MVP)

- **Discount-to-wishlist notifications**: Consume DiscountCreatedIntegrationEvent, match favorites, queue notification (max 3/day). Depends on Finance discount CRUD.
- **Review gamification**: Badges for frequent reviewers (10th review, verified reviewer, etc.)
- **AI moderation**: Replace ProfanityBlocklistEntry with ML-based content moderation.
- **NsfwClassifier real implementation**: Currently a NoOp stub.
- **Community Q&A**: Questions about entities (separate from reviews).

---

## Notes

1. **Review photos**: Frontend uploads via ContentCore `POST /attachments` with EntityType=Review after review creation. Social module reads attachment URLs in public listing query via cross-module read (ContentCore.Contracts).
2. **Bayesian rating**: Already implemented and correct. RatingRecalculationService runs daily. BayesianScore suppressed until ReviewCount >= 3.
3. **Auto-hide threshold**: 5 unique reports triggers AutoHide status. Already implemented.
4. **Max favorites**: 500 per user (S-R7). Already enforced.
5. **48h edit window**: Already enforced in Review.Edit(). No change needed.
6. **One review per entity per user**: Already enforced in CreateReviewCommandHandler. No change needed.
7. **Verified booking window**: 30 days after tour completion. BookingEligibilitySnapshot created by inbound event handler.

---

## Implementation Notes

- **Built in W3-B**: Social routes now live under `/api/v1/social/*`; public approved-review listing is available at `GET /api/v1/social/reviews/{entityType}/{entityId}` while preserving the existing query-string listing. Helpful vote add/remove endpoints and handlers were already present and wired.
- **Warn/Ban workflow**: Added direct admin endpoints for `POST /api/v1/social/moderation/warn`, `POST /api/v1/social/moderation/ban`, and `DELETE /api/v1/social/moderation/ban/{userId}`. These use the existing `UserModerationRecord` aggregate and `ContentModerationLog` audit trail; unban soft-deletes the active ban record and appends an `UnbanUser` log entry.
- **Deferred**: Strike escalation, outbound Security integration events, active-ban/history query endpoints, and Security-side global ban enforcement remain post-MVP/follow-up work.
- **Architecture decisions**: Existing Social aggregates/repositories were reused instead of introducing parallel `UserWarning`/`UserBan` tables. Moderation writes invalidate the `moderation:logs` HybridCache tag after `ISocialUnitOfWork.SaveChangesAsync`.
