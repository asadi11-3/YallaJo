# Wave 8 — Creator Multi-Type Posts, Tier Promotion & Moderation

> **Sources:** Design session m2393–m2399 (locked decisions); Wave-7.md (foundational module shape)
> **Dependencies:** Wave 7 (Creator identity, profile, follow, invitation, niche taxonomy), Wave 3 (Places/Tours/Businesses for review-target FKs), Wave 6 (Reports + Moderation infrastructure)
> **Focus:** Multi-type content posts (videos / photo-stories / long-reviews / itineraries), trust-tier auto-promotion, disclosure enforcement, post moderation queue, featuring system
> **Approach:** Additive to ContentBlogs module — `creators` schema already exists, just add tables.

---

## 1. Current Status

| Area | Built in Wave-7 | Wave-8 additions |
|---|---|---|
| Creator identity (Application/Profile) | ✅ | — |
| Creator follow / feed | ✅ | — |
| Creator-authored Blog articles | ✅ | Disclosure enforcement retrofitted |
| Multi-type creator posts (Video / PhotoStory / LongReview / Itinerary) | 0/4 🔴 | `CreatorPost` aggregate |
| Trust-tier promotion infrastructure | foundation (flag column) | `CreatorTierPromotionService` BG service + admin one-click confirm endpoint |
| Stats rollup | foundation (columns) | `CreatorStatsRollupService` BG service |
| Disclosure enforcement | foundation (`IsSponsored` flag) | Domain rule + auto-detection |
| Post moderation queue (admin) | 0/4 🔴 | Dedicated queue separate from blog queue |
| Featuring system (Tier-2) | 0/3 🔴 | `IsFeatured` flag + admin endpoints |
| Endpoints | — | 17 new |

### 1.1 Missing endpoints (17)

**Customer / public (10):**

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | POST | `/api/v1/creators/me/posts` | `Creator.Post.Create` |
| 2 | PUT | `/api/v1/creators/me/posts/{id}` | `Creator.Post.Update` |
| 3 | POST | `/api/v1/creators/me/posts/{id}/submit-for-review` | `Creator.Post.Create` |
| 4 | POST | `/api/v1/creators/me/posts/{id}/publish` | `Creator.Post.Create` (Tier-1+ only) |
| 5 | DELETE | `/api/v1/creators/me/posts/{id}` | `Creator.Post.Update` |
| 6 | GET | `/api/v1/creators/me/posts` | `Creator.Post.Read` (own) |
| 7 | GET | `/api/v1/posts` | anonymous (paginated, filter by type/niche/creator/language) |
| 8 | GET | `/api/v1/posts/{slug}` | anonymous |
| 9 | GET | `/api/v1/posts/featured` | anonymous |
| 10 | GET | `/api/v1/creators/{slug}/posts` | anonymous (filter by type) |

**Admin (7):**

| # | Method | Path | Auth |
|---|---|---|---|
| 11 | GET | `/api/v1/admin/posts` | `AdminPostModeration.Read` |
| 12 | GET | `/api/v1/admin/posts/{id}` | `AdminPostModeration.Read` |
| 13 | POST | `/api/v1/admin/posts/{id}/approve` | `AdminPostModeration.Approve` |
| 14 | POST | `/api/v1/admin/posts/{id}/reject` | `AdminPostModeration.Reject` |
| 15 | POST | `/api/v1/admin/posts/{id}/feature` | `AdminPostModeration.Feature` |
| 16 | POST | `/api/v1/admin/creators/{id}/promote-tier` | `AdminCreatorQueue.PromoteTier` |
| 17 | POST | `/api/v1/admin/creators/{id}/demote-tier` | `AdminCreatorQueue.DemoteTier` |

---

## 2. New Aggregate: `CreatorPost`

`AuditableEntity, IAggregateRoot` in `ContentBlogs.Domain/Entities/Creators/CreatorPost.cs`:

```csharp
public sealed class CreatorPost : AuditableEntity, IAggregateRoot
{
    public Guid CreatorProfileId { get; private set; }
    public CreatorPostType PostType { get; private set; }
    public string Slug { get; private set; } = "";                // auto-generated from Title
    public string Title { get; private set; } = "";
    public string Excerpt { get; private set; } = "";              // 100–500 chars
    public string? Body { get; private set; }                      // markdown; required for LongReview, optional otherwise
    public Guid LanguageId { get; private set; }                   // FK to Wave-1 Languages
    public CreatorPostStatus Status { get; private set; }          // Draft, PendingReview, Published, Rejected, Hidden, Removed
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // Featuring
    public bool IsFeatured { get; private set; }
    public DateTime? FeaturedAt { get; private set; }
    public Guid? FeaturedByAdminId { get; private set; }
    public DateTime? FeaturedUntil { get; private set; }           // optional expiry; null = indefinite

    // Disclosure
    public bool IsSponsored { get; private set; }
    private readonly List<DisclosureTarget> _disclosedTargets = []; // {EntityType, EntityId, RelationKind: OwnedListing|PaidPartnership|FamilyTie}
    public IReadOnlyCollection<DisclosureTarget> DisclosedTargets => _disclosedTargets.AsReadOnly();

    // Stats (denormalized for fast reads; rolled up by CreatorStatsRollupService)
    public int ViewCount { get; private set; }
    public int ReactionCount { get; private set; }
    public int CommentCount { get; private set; }
    public int ReportCount { get; private set; }

    // Tagging
    private readonly List<Guid> _taggedEntityIds = [];              // Tour/Place/Business (Wave-3)
    private readonly List<string> _taggedEntityTypes = [];          // parallel array of enum names
    private readonly List<Guid> _nicheIds = [];                     // 1–3 niches per post
    private readonly List<string> _freeTags = [];                   // 0–10 free tags
    private readonly List<Guid> _placeRegionIds = [];               // FK to Places region rollup (Wave-3)

    // Type-specific data — JSON column
    public string TypeSpecificDataJson { get; private set; } = "{}";

    // Factory + state methods (see §2.2)
}
```

### 2.1 Enums

```csharp
public enum CreatorPostType : byte
{
    Video = 0,         // YouTube / Vimeo embed OR hosted upload
    PhotoStory = 1,    // gallery of 3–30 images with captions
    LongReview = 2,    // tour/place/business deep-dive with rating 1–5
    Itinerary = 3      // multi-day itinerary spanning multiple entities
}

public enum CreatorPostStatus : byte
{
    Draft = 0,
    PendingReview = 1,
    Published = 2,
    Rejected = 3,
    Hidden = 4,        // suspension cascade
    Removed = 5        // soft-delete via moderation action (vs. AuditableEntity's IsDeleted which is self-delete)
}
```

### 2.2 Factory + state methods

```csharp
public static Result<CreatorPost> Create(
    Guid creatorProfileId,
    CreatorPostType postType,
    string title,
    string excerpt,
    Guid languageId,
    object typeSpecificData,     // serialized to TypeSpecificDataJson
    TimeProvider time);          // raises CreatorPostCreatedDomainEvent

public Result UpdateContent(string title, string excerpt, string? body, object typeSpecificData);
public Result Tag(IReadOnlyList<(EntityType type, Guid id)> targets, IReadOnlyList<Guid> nicheIds, IReadOnlyList<string> freeTags);
public Result MarkDisclosure(bool isSponsored, IReadOnlyList<DisclosureTarget> targets);
public Result SubmitForReview(CreatorTrustTier currentTier, TimeProvider time);
    // Tier 0  → Draft → PendingReview (mandatory admin review)
    // Tier 1+ → Draft → PendingReview → Published (auto-approved at next handler iteration; reviewable post-hoc)
public Result PublishDirect(CreatorTrustTier currentTier, TimeProvider time);
    // Tier 1+ only: skip pre-review, go straight to Published
public Result ApproveByAdmin(Guid adminId, TimeProvider time);
public Result RejectByAdmin(Guid adminId, string reason, TimeProvider time);
public Result Feature(Guid adminId, DateTime? until, TimeProvider time);
public Result Unfeature();
public Result Hide(string reason, TimeProvider time);    // suspension cascade
public Result Unhide(TimeProvider time);                 // reinstate cascade
public Result Remove(Guid adminId, string reason);
public Result ValidateDisclosure(IReadOnlyCollection<Guid> creatorOwnedEntityIds);
    // Returns Failure(SponsoredDisclosureRequired) if any taggedEntityId ∈ creatorOwnedEntityIds AND !IsSponsored
```

### 2.3 Owned value object: `DisclosureTarget`

```csharp
public sealed record DisclosureTarget(
    EntityType EntityType,
    Guid EntityId,
    DisclosureRelationKind RelationKind);

public enum DisclosureRelationKind : byte
{
    OwnedListing = 0,        // creator's own provider listing
    PaidPartnership = 1,     // sponsored by entity owner
    FamilyTie = 2,           // family member owns
    AffiliateLink = 3        // monetized link
}
```

---

## 3. Type-specific data schemas

Stored in `TypeSpecificDataJson` column; deserialized in queries to typed view-models.

### 3.1 Video
```json
{
  "videoProvider": "YouTube",         // YouTube | Vimeo | Hosted
  "videoUrl": "https://youtube.com/watch?v=...",
  "embedHtml": "<iframe src=...></iframe>",
  "thumbnailUrl": "https://...",
  "durationSeconds": 1234,
  "captionsAvailable": true,
  "transcriptText": null
}
```

### 3.2 PhotoStory
```json
{
  "images": [
    { "attachmentId": "guid", "caption": "...", "altText": "...", "sortOrder": 0 },
    { "attachmentId": "guid", "caption": "...", "altText": "...", "sortOrder": 1 }
    // 3–30 images
  ],
  "coverImageIndex": 0
}
```

### 3.3 LongReview
```json
{
  "reviewedEntityType": "Tour",     // Tour | Place | Business
  "reviewedEntityId": "guid",
  "overallRating": 4.5,             // 1.0–5.0, half-points allowed
  "subRatings": {
    "value": 4,
    "communication": 5,
    "accuracy": 4,
    "experience": 5
  },
  "pros": ["...", "..."],
  "cons": ["...", "..."],
  "wouldRecommend": true,
  "visitedOn": "2025-11-15"
}
```

### 3.4 Itinerary
```json
{
  "durationDays": 5,
  "days": [
    {
      "dayNumber": 1,
      "title": "Day 1: Amman Old City",
      "activities": [
        { "entityType": "Place", "entityId": "guid", "title": "Roman Theatre", "notes": "...", "estimatedHours": 2, "sortOrder": 0 },
        { "entityType": "Business", "entityId": "guid", "title": "Hashem Restaurant", "notes": "Lunch", "estimatedHours": 1, "sortOrder": 1 }
      ]
    }
    // ...
  ],
  "estimatedCostUsd": 850,
  "bestSeasons": ["Spring", "Autumn"],
  "physicalDifficulty": "Moderate",  // Easy | Moderate | Challenging
  "recommendedFor": ["families", "history-buffs"]
}
```

---

## 4. Domain events (`ContentBlogs.Domain/Events/Creators/`)

Append to existing creators event folder:

| # | Event | Triggered by |
|---|---|---|
| 1 | `CreatorPostCreatedDomainEvent(PostId, CreatorProfileId, PostType, CreatedAt)` | `CreatorPost.Create()` |
| 2 | `CreatorPostSubmittedDomainEvent(PostId, CreatorProfileId, PostType, SubmittedAt)` | `SubmitForReview()` |
| 3 | `CreatorPostPublishedDomainEvent(PostId, CreatorProfileId, PostType, PublishedAt, IsPreModerated)` | `ApproveByAdmin()` or `PublishDirect()` (Tier 1+) |
| 4 | `CreatorPostRejectedDomainEvent(PostId, CreatorProfileId, Reason, RejectedAt, RejectedByAdminId)` | `RejectByAdmin()` |
| 5 | `CreatorPostRemovedDomainEvent(PostId, CreatorProfileId, Reason, RemovedAt, RemovedByAdminId)` | `Remove()` |
| 6 | `CreatorPostFeaturedDomainEvent(PostId, CreatorProfileId, FeaturedUntil, FeaturedByAdminId)` | `Feature()` |
| 7 | `CreatorPostUnfeaturedDomainEvent(PostId, CreatorProfileId)` | `Unfeature()` |
| 8 | `CreatorTierPromotedDomainEvent(ProfileId, UserId, OldTier, NewTier, ChangedAt, ChangedByAdminId)` | admin one-click promotion via `PromoteTier()` |
| 9 | `CreatorTierDemotedDomainEvent(ProfileId, UserId, OldTier, NewTier, Reason, ChangedAt)` | background service auto-demotion via `Demote()` |
| 10 | `CreatorEligibleForTierPromotionDomainEvent(ProfileId, UserId, EligibleForTier, MetThresholds, MarkedAt)` | background service `CreatorTierPromotionService` sets `EligibleForTierN=true` |

**Note:** events 8 and 9 are deliberately split (vs. a single `CreatorTrustTierChangedDomainEvent`) because the integration consumers and notification copy differ significantly between promotion (celebration) and demotion (warning).

## 5. Integration events (`ContentBlogs.Contracts/IntegrationEvents/Creators/`)

8 events total:

| Topic | Type | Consumers |
|---|---|---|
| `creators.post.submitted-for-review.v1` | `CreatorPostSubmittedForReviewIntegrationEvent` | Messaging (admin queue notification "New post pending review" — Tier-0 only) |
| `creators.post.published.v1` | `CreatorPostPublishedIntegrationEvent` | Messaging (notify followers); Tracking (analytics); Search (index new content) |
| `creators.post.rejected.v1` | `CreatorPostRejectedIntegrationEvent` | Messaging (notify creator with reason + appeal CTA) |
| `creators.post.removed.v1` | `CreatorPostRemovedIntegrationEvent` | Search (remove from index); Messaging (notify creator); Tracking (analytics) |
| `creators.post.featured.v1` | `CreatorPostFeaturedIntegrationEvent` | Messaging (notify creator) |
| `creators.tier.promoted.v1` | `CreatorTierPromotedIntegrationEvent` | Messaging (celebrate); Tracking |
| `creators.tier.demoted.v1` | `CreatorTierDemotedIntegrationEvent` | Messaging (warning); Tracking |
| `creators.eligible-for-tier-promotion.v1` | `CreatorEligibleForTierPromotionIntegrationEvent` | Messaging (admin queue alert "Creator X is ready for tier promotion review") |

Append all 8 to `IntegrationEventTypeRegistry.cs`.

---

## 6. Business Rules

### 6.1 Post creation by trust tier

| Tier | Pre-moderation? | Direct publish? | Featuring eligible? |
|---|---|---|---|
| Tier 0 | ✅ mandatory | ❌ | ❌ |
| Tier 1 | ❌ post-moderation only | ✅ | ❌ |
| Tier 2 | ❌ post-moderation only | ✅ | ✅ |

- Tier 0 creators: `SubmitForReview` → admin approves → Published
- Tier 1+ creators: `PublishDirect` available; admin can still Remove post-hoc
- Tier 2: only Tier 2 posts can be `Featured` (admin endpoint enforces)

### 6.2 Tier promotion thresholds (CreatorTierPromotionService rules)

| Transition | Conditions (ALL must hold) |
|---|---|
| Tier 0 → Tier 1 (auto-eligible) | `ApprovedArticleCount + Published posts >= 5` AND `ReportRate < 5%` |
| Tier 1 → Tier 2 (auto-eligible) | `>= 25 published` AND `ReportRate < 2%` AND `TotalReactionCount >= 500` |
| Demote one tier | `ReportRate > 15%` sustained for 30 days (window check) |

Auto-eligibility sets `EligibleForTierN` flag — admin must one-click confirm via `POST /api/v1/admin/creators/{id}/promote-tier`. Demotion is automatic (no admin confirm).

### 6.3 Disclosure enforcement (`CreatorPost.ValidateDisclosure`)

When `SubmitForReview` is called, OR when admin approves:
1. Compute `creatorOwnedEntityIds` from cross-reference to Accounts module's `ProviderApplicationRepository.GetApprovedEntitiesForUserAsync(userId)` — list of TourIds + PlaceIds + BusinessIds belonging to creator's provider listings
2. Compute `taggedEntityIds` from post's `_taggedEntityIds`
3. If `taggedEntityIds ∩ creatorOwnedEntityIds` ≠ ∅ AND `!IsSponsored` → `Result.Failure(SponsoredDisclosureRequired)`
4. Validation runs at `SubmitForReview` AND `ApproveByAdmin` (defense-in-depth)
5. Retroactively applied to Wave-7 `Article` entity if it tags entities (Article gains `IsSponsored` flag + `DisclosedTargets` collection in Wave-8 migration)

### 6.4 Featuring rules

- Only Tier 2 creator posts can be featured
- `FeaturedUntil` optional — null = indefinite, with date = auto-unfeatured by `CreatorStatsRollupService` when past
- Max 12 featured posts globally at any time (soft cap; admin override)
- Featuring emits `CreatorPostFeaturedIntegrationEvent` → notification to creator + Search re-index for prominence

### 6.5 Post moderation queue
- Separate from existing blog article queue due to volume
- Filters: `PostType`, `Status`, `CreatorTier`, `HasOpenReports` (cross-ref to Wave-6 Reports module)
- Each row shows: thumbnail, title, type icon, creator (with tier badge), submitted-at, report-count badge, disclosure-flag indicator
- Bulk actions (admin): approve all selected, reject all selected, remove all selected

### 6.6 Removal vs Soft-delete
- `Remove(adminId, reason)` is moderation action: sets `Status=Removed`, persists `RejectionReason`, audit-logged with admin id
- AuditableEntity `IsDeleted` is creator's own delete (creator deletes draft) — Status remains the source of truth for moderation lifecycle

---

## 7. Background services

### 7.1 `CreatorTierPromotionService`

Hosted in `ContentBlogs.Infrastructure/BackgroundServices/`. Runs **daily at 02:00 UTC**.

```csharp
public sealed class CreatorTierPromotionService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<CreatorTierPromotionService> logger)
    : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ICreatorProfileRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IContentBlogsUnitOfWork>();
                var time = timeProvider.GetUtcNow().UtcDateTime;

                // Pass 1: Mark Tier 0 → Tier 1 eligibility
                int t0t1Eligible = 0;
                var page = 1;
                while (true)
                {
                    var candidates = await repo.ListTier0CandidatesAsync(page, BatchSize, stoppingToken);
                    if (candidates.Count == 0) break;
                    foreach (var p in candidates)
                    {
                        var totalPublished = p.ApprovedArticleCount + (await repo.CountPublishedPostsAsync(p.Id, stoppingToken));
                        if (totalPublished >= 5 && p.ReportRate < 0.05m && !p.EligibleForTier1)
                        {
                            p.MarkEligibleForTier1();   // raises domain event
                            t0t1Eligible++;
                        }
                    }
                    await uow.SaveChangesAsync(stoppingToken);
                    page++;
                }

                // Pass 2: Mark Tier 1 → Tier 2 eligibility (similar)
                // Pass 3: Auto-demote on ReportRate > 15% for 30 days
                // ... (see implementation in WBS step)

                logger.LogInformation("CreatorTierPromotionService: {T0T1} Tier 0→1 eligible", t0t1Eligible);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreatorTierPromotionService run failed");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }
}
```

Notes:
- Uses scoped service factory pattern (mandatory for hosted services consuming scoped DbContext)
- Catches exceptions to keep the loop alive across transient failures
- Emits `CreatorTierChangedDomainEvent` only after `SaveChangesAsync` succeeds — never raises before

### 7.2 `CreatorStatsRollupService`

Runs **hourly**. Recomputes from raw data:
- `ApprovedArticleCount` — count Articles WHERE AuthoredByCreatorId = profile.Id AND Status = Published
- `Published posts count` — count CreatorPost WHERE CreatorProfileId = profile.Id AND Status = Published
- `TotalViewCount, TotalReactionCount, TotalCommentCount` — sums across Article + CreatorPost denormalized columns
- `ReportCount` — cross-ref Wave-6 Reports module via integration query (`ReportsClient` interface — see Wave-6 spec)
- `ReportRate = ReportCount / max(totalPublished, 1)`
- `FollowerCount` — count CreatorFollow rows
- Also unfeatures posts where `FeaturedUntil < now` and `IsFeatured == true`

### 7.3 `CreatorInvitationCleanupService` (already running from Wave-7)

No changes.

---

## 8. Application layer additions

### 8.1 Commands (`ContentBlogs.Application/Commands/Creators/Posts/...`)

| Command | Validator highlights |
|---|---|
| `CreateCreatorPostCommand(PostType, Title, Excerpt, LanguageId, TypeSpecificData, NicheIds, FreeTags)` | Type-specific data validated against type schema |
| `UpdateCreatorPostCommand(Id, Title, Excerpt, Body, TypeSpecificData)` | Only Draft posts editable by creator |
| `SubmitCreatorPostForReviewCommand(Id)` | Validates required fields per type; calls `ValidateDisclosure` |
| `PublishCreatorPostCommand(Id)` | Tier 1+ only; bypasses pre-review |
| `DeleteCreatorPostCommand(Id)` | Only Draft or Rejected; cascades nothing |
| `ApproveCreatorPostCommand(Id)` (admin) | Pre-moderation flow only |
| `RejectCreatorPostCommand(Id, Reason)` (admin) | Reason 50–1000 |
| `RemoveCreatorPostCommand(Id, Reason)` (admin) | For published posts |
| `FeatureCreatorPostCommand(Id, FeaturedUntil?)` (admin) | Tier 2 creator only |
| `UnfeatureCreatorPostCommand(Id)` (admin) | — |
| `PromoteCreatorTierCommand(ProfileId, TargetTier)` (admin) | Validates `EligibleForTierN == true` |
| `DemoteCreatorTierCommand(ProfileId, TargetTier, Reason)` (admin) | Reason required |

### 8.2 Queries

| Query | Cache | Tags |
|---|---|---|
| `GetMyCreatorPostsQuery(CreatorProfileId, StatusFilter?, TypeFilter?, Page, PageSize)` | 1 min | `creators:my-posts:{profileId}:tag` |
| `GetCreatorPostBySlugQuery(Slug)` | 15 min | `creators:posts:{slug}:tag`, `creators:posts:list:tag` |
| `ListPostsQuery(TypeFilter?, NicheFilter?, CreatorIdFilter?, LanguageFilter?, Page, PageSize)` | 5 min | `creators:posts:list:tag` |
| `GetFeaturedPostsQuery(TypeFilter?, Page, PageSize)` | 10 min | `creators:posts:featured:tag` |
| `GetAdminPostQueueQuery(StatusFilter, TypeFilter?, TierFilter?, HasOpenReports?, Page, PageSize)` | 30 sec | `admin:posts:queue:tag` |

### 8.3 Cache key constants (`ContentBlogs.Application/Caching/ContentBlogsCacheKeys.cs`)

```csharp
public static string CreatorPost(string slug) => $"creators:posts:{slug}";
public static string CreatorPostTag(string slug) => $"creators:posts:{slug}:tag";
public const string CreatorPostsList = "creators:posts:list";
public const string CreatorPostsListTag = "creators:posts:list:tag";
public const string CreatorPostsFeatured = "creators:posts:featured";
public const string CreatorPostsFeaturedTag = "creators:posts:featured:tag";
public static string MyCreatorPosts(Guid profileId) => $"creators:my-posts:{profileId}";
public static string MyCreatorPostsTag(Guid profileId) => $"creators:my-posts:{profileId}:tag";
public const string AdminPostsQueue = "admin:posts:queue";
public const string AdminPostsQueueTag = "admin:posts:queue:tag";
```

---

## 9. Infrastructure layer additions

### 9.1 EF Configuration

`CreatorPostConfiguration.cs` in `ContentBlogs.Infrastructure/Persistence/Configurations/Creators/`:
- Table `creators.CreatorPosts`
- Enums via `HasConversion<string>()`
- Indexes:
  - `Slug` unique
  - `(CreatorProfileId, Status)` for own-posts queries
  - `(Status, ReviewedAt)` for admin queue
  - `(Status, IsFeatured)` for featured listing
  - `(LanguageId, Status)` for language-filtered list
  - `(PublishedAt DESC)` for trending queries
- `TypeSpecificDataJson` → `HasColumnType("nvarchar(max)")`
- Owned collections (taggedEntityIds, freeTags, nicheIds, etc.) as JSON columns
- `_disclosedTargets` configured via `OwnsMany`

### 9.2 Repository

`ICreatorPostRepository`:
```csharp
Task<CreatorPost?> GetByIdAsync(Guid id, CancellationToken ct);
Task<CreatorPost?> GetBySlugAsync(string slug, CancellationToken ct);
Task<IReadOnlyList<CreatorPost>> ListByCreatorAsync(Guid profileId, CreatorPostStatus? status, CreatorPostType? type, int page, int pageSize, CancellationToken ct);
Task<int> CountByCreatorAsync(Guid profileId, CancellationToken ct);
Task<int> CountPublishedByCreatorAsync(Guid profileId, CancellationToken ct);
Task<IReadOnlyList<CreatorPost>> ListPublishedAsync(CreatorPostType? typeFilter, Guid? nicheFilter, Guid? creatorFilter, Guid? languageFilter, int page, int pageSize, CancellationToken ct);
Task<IReadOnlyList<CreatorPost>> ListFeaturedAsync(CreatorPostType? typeFilter, int page, int pageSize, CancellationToken ct);
Task<IReadOnlyList<CreatorPost>> ListAdminQueueAsync(CreatorPostStatus statusFilter, CreatorPostType? typeFilter, CreatorTrustTier? tierFilter, bool? hasOpenReports, int page, int pageSize, CancellationToken ct);
Task<IReadOnlyList<CreatorPost>> ListFeaturedExpiringAsync(DateTime asOf, int batchSize, CancellationToken ct);  // for rollup service
```

### 9.3 Integration converters

`ContentBlogs.Infrastructure/EventHandlers/CreatorPostIntegrationConverters.cs` — 5 handlers (published, removed, featured, tier-promoted, tier-demoted) following the Wave-7 §8.4 pattern.

### 9.4 Background service registration

```csharp
services.AddHostedService<CreatorTierPromotionService>();
services.AddHostedService<CreatorStatsRollupService>();
// CreatorInvitationCleanupService already registered in Wave-7
```

### 9.5 Cross-module: `ReportsClient` (read-side cross-ref)

`CreatorStatsRollupService` needs to query Wave-6 Reports module. Two patterns acceptable:

**Pattern A (recommended):** integration event listener
- Wave-6 emits `reports.created.v1` and `reports.resolved.v1`
- ContentBlogs has handler that increments/decrements `CreatorProfile.ReportCount`
- Rollup recomputes `ReportRate` from the denormalized column

**Pattern B (fallback):** direct cross-context read via shared `IReportingReadClient` interface in `YallaJo.SharedKernel.Application/Reporting/` — implemented in Reports module, consumed by ContentBlogs

Choose Pattern A for consistency with existing event-driven architecture; document choice in WBS step.

---

## 10. Presentation layer

### 10.1 Endpoint files

- `ContentBlogs.Presentation/Endpoints/Creators/CreatorPostEndpoints.cs` — 6 creator-side endpoints
- `ContentBlogs.Presentation/Endpoints/Creators/PublicPostEndpoints.cs` — 4 public endpoints
- `ContentBlogs.Presentation/Endpoints/Creators/AdminPostEndpoints.cs` — 5 admin endpoints
- Extend `AdminCreatorEndpoints.cs` (from Wave-7) with `POST /promote-tier` and `POST /demote-tier`

### 10.2 Wire-up in `ContentBlogsEndpoints.cs`

```csharp
app.MapGroup("/api/v1/creators/me/posts").MapCreatorPostEndpoints();
app.MapGroup("/api/v1/posts").MapPublicPostEndpoints();
app.MapGroup("/api/v1/admin/posts").MapAdminPostEndpoints();
```

---

## 11. Migration

`ContentBlogsAddCreatorPostsAndTierManagement` — single migration:

- New table `creators.CreatorPosts` with FK to `creators.CreatorProfiles.Id`
- Article entity additions: `IsSponsored bool default false`, `DisclosedTargets` json column (retroactive Wave-7 patch)
- New permission columns (no new tables — permissions are seeded via DI catalog rebuild)
- Indexes per §9.1
- Seed: no new seed data (existing CreatorNiche seed reused)

```powershell
dotnet ef migrations add ContentBlogsAddCreatorPostsAndTierManagement `
  --project 'ContentBlogs.Infrastructure\ContentBlogs.Infrastructure.csproj' `
  --startup-project 'YallaJo.Api\YallaJo.Api.csproj' `
  --context ContentBlogsDbContext --output-dir Migrations
```

---

## 12. Cross-module reaction matrix (Wave-8 — integration event handlers, async via outbox)

| Source event | Target module | Handler file | Action |
|---|---|---|---|
| `creators.post.submitted-for-review.v1` | Messaging | `AdminNewCreatorPostNotificationHandler.cs` | InApp notification to `AdminPostModeration.Read` permission holders ("New post pending review — Tier 0 creator") |
| `creators.post.published.v1` | Messaging | `CreatorPostPublishedNotifyFollowersHandler.cs` | Batch notify followers (paginated; 100/batch) |
| `creators.post.published.v1` | Search (Wave-6) | `CreatorPostPublishedIndexHandler.cs` | Add to search index |
| `creators.post.published.v1` | Tracking | `CreatorPostPublishedTrackingHandler.cs` | Analytics event |
| `creators.post.rejected.v1` | Messaging | `CreatorPostRejectedNotifyCreatorHandler.cs` | InApp + Email "Your post was rejected" + admin's reason + guideline link + appeal-via-support CTA |
| `creators.post.removed.v1` | Search | `CreatorPostRemovedDeindexHandler.cs` | Remove from search index |
| `creators.post.removed.v1` | Messaging | `CreatorPostRemovedNotifyCreatorHandler.cs` | InApp + Email "Your post was removed" + reason |
| `creators.post.removed.v1` | Tracking | `CreatorPostRemovedTrackingHandler.cs` | Analytics event (moderation event) |
| `creators.post.featured.v1` | Messaging | `CreatorPostFeaturedNotifyCreatorHandler.cs` | InApp + Email celebration |
| `creators.tier.promoted.v1` | Messaging | `CreatorTierPromotedNotifyHandler.cs` | InApp + Email "You've been promoted to Tier N!" + SignalR push triggers tier-up modal |
| `creators.tier.promoted.v1` | Tracking | `CreatorTierPromotedTrackingHandler.cs` | Analytics event |
| `creators.tier.demoted.v1` | Messaging | `CreatorTierDemotedNotifyHandler.cs` | InApp + Email "Tier demoted — review guidelines" |
| `creators.tier.demoted.v1` | Tracking | `CreatorTierDemotedTrackingHandler.cs` | Analytics event |
| `creators.eligible-for-tier-promotion.v1` | Messaging | `AdminCreatorEligibleForTierPromotionHandler.cs` | InApp notification to `AdminCreatorQueue.PromoteTier` holders ("Creator X is ready for tier-up review — one-click confirm in queue") |
| `reports.created.v1` (Wave-6 → ContentBlogs) | ContentBlogs (internal) | `ReportCreatedIncrementCreatorReportCountHandler.cs` | Increment `CreatorProfile.ReportCount` if report targets a creator post |
| `reports.resolved.v1` | ContentBlogs (internal) | `ReportResolvedDecrementCreatorReportCountHandler.cs` | Decrement if resolved-as-spam=false |

### 12.1 Within-module domain event handlers (Wave-8 additions, synchronous, same UoW)

These keep `CreatorProfile` denormalized counters in sync — analogous to Wave-7 §5.3.

| Domain event | Handler file | Action |
|---|---|---|
| `CreatorPostPublishedDomainEvent` | `IncrementCreatorPublishedPostCountHandler.cs` | Loads `CreatorProfile` by `CreatorProfileId` → increments published-post counter (a Wave-8 column added on CreatorProfile) |
| `CreatorPostRemovedDomainEvent` | `DecrementCreatorPublishedPostCountHandler.cs` | Loads `CreatorProfile` → decrements counter |
| `CreatorPostFeaturedDomainEvent` | (audit-only — no counter changes) | — |
| `CreatorPostUnfeaturedDomainEvent` | (audit-only) | — |
| `CreatorTierPromotedDomainEvent` | `ResetEligibilityFlagsHandler.cs` | Loads `CreatorProfile` → clears `EligibleForTier1=false` (already-promoted, the flag is stale) |
| `CreatorTierDemotedDomainEvent` | (no internal counter change; integration consumer is notified) | — |
| `CreatorEligibleForTierPromotionDomainEvent` | (no internal counter change; integration event notifies admin queue) | — |

### 12.2 Reaction count rollup design

CreatorPost reaction/comment/view counts are denormalized columns on the post itself. **Single source of truth strategy:**

- **Live increments:** existing Wave-4 reaction/comment event handlers extended to increment `CreatorPost.ReactionCount` / `CommentCount` when the target is a creator post (vs. blog article)
- **View counts:** existing Wave-6 view tracking integration event consumed by ContentBlogs handler — increments `ViewCount` on the post
- **Drift correction:** `CreatorStatsRollupService` (hourly) recomputes from raw data to fix any drift caused by missed events or out-of-order processing
- **CreatorProfile aggregated counts** (`TotalReactionCount`, etc.) recomputed by `CreatorStatsRollupService` from sum of constituent posts + articles — NOT incremented live (avoids hot-spot contention on profile row)

---

## 13. WBS

| # | Step | Hours | Owner |
|---|---|---|---|
| 1 | Domain: `CreatorPost` aggregate + enums + **10 domain events** (split tier events + eligible-for-promotion + post-rejected/submitted) + value objects (`DisclosureTarget`) | 11 | Backend Dev A |
| 2 | Domain: type-specific data validators (one per `CreatorPostType`) | 6 | Backend Dev A |
| 3 | Domain: `ValidateDisclosure` method + reverse cross-module query interface `IProviderEntitiesReadClient` | 4 | Backend Dev B |
| 4 | Domain: extend `CreatorProfile` with `MarkEligibleForTier1/2`, `Promote`, `Demote` methods + `PublishedPostCount` column + `IncrementPublishedPostCount`/`DecrementPublishedPostCount` methods | 3 | Backend Dev A |
| 5 | Article retro-fit: add `IsSponsored` + `DisclosedTargets` to Wave-7 Article entity + migration | 2 | Backend Dev B |
| 6 | Contracts: **8 integration events** + extend `ContentBlogsFeatures` (no new feature constants needed — uses existing `AdminPostModeration`) + add ~10 catalog entries | 4 | Backend Dev B |
| 7 | Application: 12 commands + validators + 5 queries + cache keys | 14 | Backend Dev A |
| 8 | Infrastructure: `CreatorPostConfiguration.cs` + repository + integration converters | 8 | Backend Dev B |
| 9 | Background service: `CreatorTierPromotionService` | 6 | Backend Dev B |
| 10 | Background service: `CreatorStatsRollupService` | 6 | Backend Dev A |
| 11 | Presentation: 15 endpoints across 3 files + extend `AdminCreatorEndpoints` | 7 | Backend Dev A |
| 12 | SharedKernel: register **8 events** in `IntegrationEventTypeRegistry` | 0.5 | Either |
| 13 | Cross-module handlers — Messaging × **8** (NotifyFollowers, NotifyCreatorRemoved/Rejected/Featured, AdminNewPost, TierPromoted, TierDemoted, AdminEligibleForPromotion) | 8 | Backend Dev B |
| 14 | Cross-module handlers — Search × 2 (publish/remove index) | 3 | Backend Dev A |
| 15 | Cross-module handlers — Tracking × **4** (published, removed, tier-promoted, tier-demoted) | 3 | Backend Dev A |
| 16 | Cross-module handlers — ContentBlogs internal (Reports listeners × 2) | 2 | Backend Dev A |
| 16b | **Within-module domain event handlers × 3** (Increment/DecrementPublishedPostCount, ResetEligibilityFlagsOnPromotion) | 2 | Backend Dev A |
| 16c | **Reaction-count live-increment retrofit** — extend existing Wave-4 reaction/comment handlers to branch on `target is CreatorPost vs BlogArticle` | 2 | Backend Dev B |
| 17 | Migration `ContentBlogsAddCreatorPostsAndTierManagement` | 2 | Backend Dev B |
| 18 | Unit tests: state machine, disclosure validator, type-specific validators, tier promotion logic | 8 | Both |
| 19 | Integration tests: end-to-end submit→approve→publish, tier promotion, featuring, disclosure rejection | 5 | Both |
| **Total** | | **~95h** | |

---

## 14. Acceptance Criteria

- [ ] All 17 missing endpoints respond per spec
- [ ] `CreatorPost` state machine verified:
  - Tier 0: Draft → SubmitForReview → PendingReview → ApproveByAdmin → Published
  - Tier 1+: Draft → PublishDirect → Published (no admin review required)
  - Reject: Draft → SubmitForReview → PendingReview → RejectByAdmin → Rejected (with reason)
  - Remove: Published → RemoveByAdmin → Removed (with reason)
- [ ] Type-specific data validation rejects malformed payloads (e.g. Video without videoUrl, LongReview without rating, Itinerary without days)
- [ ] Disclosure validation:
  - Creator with no provider listing publishes post tagging any entity → ✅
  - Creator who is also approved provider tags their own listing without `IsSponsored=true` → ❌ 422 `SponsoredDisclosureRequired`
  - Creator who is also approved provider tags their own listing with `IsSponsored=true` and `DisclosedTargets` populated → ✅
- [ ] Featuring constraints:
  - Tier 2 creator's post → can be featured
  - Tier 1 creator's post → returns 422 `Creator.NotEligibleForFeaturing`
  - `FeaturedUntil` past + `CreatorStatsRollupService` runs → post automatically unfeatured
- [ ] Tier promotion:
  - Creator with 5+ approved posts + ReportRate < 5% → `EligibleForTier1=true` after next service run
  - Admin one-clicks `POST /api/v1/admin/creators/{id}/promote-tier` → tier flips to Tier 1, emits `creators.tier.promoted.v1`
  - Admin tries to promote ineligible creator → 422 `Creator.NotEligibleForTier1`
- [ ] Auto-demotion: synthetic test with `ReportRate = 0.20m` sustained 30 days → service auto-demotes; emits `creators.tier.demoted.v1`
- [ ] Followers notification: post published → `CreatorPostPublishedNotifyFollowersHandler` creates Notifications for all followers (batch-paged); inbox prevents duplicates
- [ ] Search index: published posts appear in search results within outbox-processing window; removed posts disappear
- [ ] Slug generation: unique with collision suffix; cross-checks both `CreatorPost.Slug` and `Article.Slug` for uniqueness in same module
- [ ] `dotnet build` green for ContentBlogs.* + YallaJo.Api
- [ ] Background services: both new services start cleanly, log successful runs, recover from transient exceptions
- [ ] All endpoints use `MustHavePermissionAttribute`
- [ ] All queries implement `ICacheableQuery`; commands invalidate cache
- [ ] Integration tests: 5+ end-to-end flows covering tier promotion, featuring, disclosure, removal cascade
- [ ] Unit test coverage ≥ 70% for new domain code

---

## 15. Risk Register

| Risk | Mitigation |
|---|---|
| TypeSpecificDataJson schema drift between client and server | Server validates against typed schemas (one validator per `CreatorPostType`); client requests `GET /api/v1/posts/schemas/{type}` for the JSON Schema (optional Wave-8.1) |
| `CreatorTierPromotionService` runs while admin is reviewing → race condition | Service uses optimistic concurrency (`RowVersion`); admin sees fresh `EligibleForTier1` flag |
| Auto-demotion punishes creators for one viral bad post | Add minimum-30-day window check on `ReportRate` AND minimum-10-published-posts gate; document for admin escalation |
| Disclosure validation false-positives (creator-employee tagging employer business) | Add `DisclosureRelationKind.AffiliateLink` / `FamilyTie` to broaden coverage; allow admin override at approve-time |
| Itinerary entity references break (e.g. Tour gets deleted) | Tour delete → emit `tours.deleted.v1` → ContentBlogs handler marks affected itineraries with `HasBrokenLinks=true`, prompts creator to edit (out of scope for v1 Wave-8 — flag as Wave-8.1) |
| Background service consuming all DB connections under load | Use `BatchSize=200` constants; back off via `Task.Delay` between batches; document throttle policy |

---

## 16. Forward Look — Wave-9 and beyond

- Wave-9: Monetization layer — based on Wave-7 traffic hooks, creators with Tier 2 + meeting payout thresholds receive revenue share
- Wave-9: Creator analytics dashboard (`/Creators/Me/Analytics`)
- Wave-10: Cross-language translations of creator posts (machine-translated proposals, creator-approved publishing)
- Wave-10: Live events / livestream Posts (`CreatorPostType.LiveEvent`)

---

## Appendix A — Reusable patterns

Wave-8 reuses patterns established by Wave-2 and Wave-7:

- Aggregate state machine: `Accounts.Domain/Entities/ProviderApplication.cs`, `ContentBlogs.Domain/Entities/Creators/CreatorApplication.cs`
- Background service template: see Wave-7.md §6.1 + this doc §7.1
- Cross-module event handler: `Messaging.Infrastructure/EventHandlers/CreatorApprovedNotificationHandler.cs` (Wave-7)
- Bulk batch processing: `ContentTours.Infrastructure/EventHandlers/ProviderSuspendedSuspendToursHandler.cs` (Wave-2)
- Owned-collection JSON columns: see existing `Place.Coordinates`, Wave-7 `CreatorApplication._portfolioUrls`

When in doubt, mirror Wave-7 exactly (it already mirrors Wave-2).
