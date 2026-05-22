# Wave 7 — Creator Identity & Blog Article Authoring

> **Source:** Design session m2393–m2399 (locked decisions)
> **Dependencies:** Wave 1 (Auth, Languages, Tags), Wave 2 (Provider Application — state-machine reference), Wave 4 (Article entity, BlogComment, BlogReaction)
> **Focus:** Add Content Creator role hosted inside ContentBlogs module — application, profile, follow, invitation, and creator-authored blog articles.
> **Module strategy:** Additive only — extend `ContentBlogs` rather than spawn new module. Two schemas inside `ContentBlogsDbContext` (`content_blogs.*` retained, new `creators.*` added).

---

## 1. Current Status

| Area | Built | Missing |
|---|---|---|
| Article entity (admin-authored) | ✅ | `AuthoredByCreatorId?` nullable FK + new `PendingCreatorReview` status |
| Article language | ⚠️ verify | Per-article `LanguageId` FK (if not already on Article) |
| Creator identity (Application / Profile) | 0/2 🔴 | Full state-machine aggregate + profile aggregate |
| Creator social (Follow / Feed) | 0/2 🔴 | `CreatorFollow` junction + feed query |
| Creator onboarding (Invitation) | 0/1 🔴 | `CreatorInvitation` aggregate, email + in-app variants |
| Creator endpoints | 0/22 🔴 | 14 customer/public + 8 admin |
| Cross-module reactions (Messaging / Tracking) | 0/6 🔴 | 5 Messaging handlers + 1 Tracking + 1 ContentBlogs-internal |
| Background services | 0/1 🔴 | `CreatorInvitationCleanupService` (daily, expire stale invitations) |
| `AppAction` constants (`Invite`, `RedeemInvitation`, `RequestMoreInfo`) | 0/3 🔴 | Add to SharedKernel |

### 1.1 Missing endpoints (22)

**Customer / public (14):**

| # | Method | Path | Auth |
|---|---|---|---|
| 1 | POST | `/api/v1/creators/apply` | `Creator.Application.Submit` |
| 2 | POST | `/api/v1/creators/redeem-invitation` | authenticated + token-gated |
| 3 | GET | `/api/v1/creators/status` | `Creator.Application.Read` (self) |
| 4 | GET | `/api/v1/creators/me` | `Creator.Profile.Read` (self) |
| 5 | PUT | `/api/v1/creators/me` | `Creator.Profile.Update` (self) |
| 6 | POST | `/api/v1/creators/me/articles` | `Creator.Article.Create` |
| 7 | PUT | `/api/v1/creators/me/articles/{id}` | `Creator.Article.Update` |
| 8 | POST | `/api/v1/creators/me/articles/{id}/submit-for-review` | `Creator.Article.Create` |
| 9 | DELETE | `/api/v1/creators/me/articles/{id}` | `Creator.Article.Update` |
| 10 | GET | `/api/v1/creators` | anonymous (paginated, filter by niche/region/language) |
| 11 | GET | `/api/v1/creators/{slug}` | anonymous |
| 12 | POST | `/api/v1/creators/{slug}/follow` | authenticated |
| 13 | DELETE | `/api/v1/creators/{slug}/follow` | authenticated |
| 14 | GET | `/api/v1/me/creator-feed` | authenticated |

**Admin (8):**

| # | Method | Path | Auth |
|---|---|---|---|
| 15 | GET | `/api/v1/admin/creators` | `AdminCreatorQueue.Read` |
| 16 | GET | `/api/v1/admin/creators/{id}` | `AdminCreatorQueue.Read` |
| 17 | POST | `/api/v1/admin/creators/invitations` | `AdminCreatorQueue.Invite` |
| 18 | POST | `/api/v1/admin/creators/{id}/approve` | `AdminCreatorQueue.Approve` |
| 19 | POST | `/api/v1/admin/creators/{id}/reject` | `AdminCreatorQueue.Reject` |
| 20 | POST | `/api/v1/admin/creators/{id}/request-more-info` | `AdminCreatorQueue.RequestMoreInfo` |
| 21 | POST | `/api/v1/admin/creators/{id}/suspend` | `AdminCreatorQueue.Suspend` |
| 22 | POST | `/api/v1/admin/creators/{id}/reinstate` | `AdminCreatorQueue.Reinstate` |

---

## 2. New Aggregates Hosted in `ContentBlogs.Domain/Entities/Creators/`

### 2.1 Aggregate: `CreatorApplication`

`AuditableEntity, IAggregateRoot` — state machine clone of `Accounts.ProviderApplication`:

```csharp
public sealed class CreatorApplication : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = "";        // unique, becomes slug
    public string Bio { get; private set; } = "";                 // markdown, 500 char max
    public CreatorApplicationStatus Status { get; private set; }  // Draft, Pending, Approved, Rejected, MoreInfoNeeded
    public CreatorApplicationSource Source { get; private set; }  // PublicApplication | AdminInvitation
    public Guid? InvitedByAdminId { get; private set; }
    public Guid? InvitationId { get; private set; }               // FK if redeemed
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? MoreInfoRequest { get; private set; }
    public int ReapplicationCount { get; private set; }           // max 3 attempts
    public DateTime? CoolingPeriodEndsAt { get; private set; }    // 14d after reject

    // Owned collections (json columns on the application row)
    private readonly List<string> _portfolioUrls = [];            // 1–5 required
    private readonly List<string> _sampleWorkUrls = [];           // 3 required
    private readonly List<Guid> _nicheIds = [];                   // 3–5 from admin-curated CreatorNiche catalog
    private readonly List<string> _freeTags = [];                 // 1–10 free tags
    private readonly List<Guid> _languageIds = [];                // FK list → Wave-1 Languages
    private readonly List<Guid> _preferredRegionIds = [];         // FK list → Places (Wave-3)
    private readonly Dictionary<string, string> _socialHandles = new(); // platform → handle

    public IReadOnlyCollection<string> PortfolioUrls => _portfolioUrls.AsReadOnly();
    public IReadOnlyCollection<string> SampleWorkUrls => _sampleWorkUrls.AsReadOnly();
    public IReadOnlyCollection<Guid> NicheIds => _nicheIds.AsReadOnly();
    public IReadOnlyCollection<string> FreeTags => _freeTags.AsReadOnly();
    public IReadOnlyCollection<Guid> LanguageIds => _languageIds.AsReadOnly();
    public IReadOnlyCollection<Guid> PreferredRegionIds => _preferredRegionIds.AsReadOnly();
    public IReadOnlyDictionary<string, string> SocialHandles => _socialHandles;

    // Factory + state methods
    public static Result<CreatorApplication> Create(Guid userId, string displayName, ...);  // raises CreatorApplicationCreatedDomainEvent
    public Result Submit(TimeProvider time);                                                  // Draft|MoreInfoNeeded|Rejected → Pending
    public Result Approve(Guid adminId, TimeProvider time);                                   // raises CreatorApplicationApprovedDomainEvent
    public Result Reject(Guid adminId, string reason, TimeProvider time);
    public Result RequestMoreInfo(Guid adminId, string requestNotes, TimeProvider time);
    public Result AttachInvitation(Guid invitationId);                                        // only valid before Submit
}
```

### 2.2 Aggregate: `CreatorProfile`

`AuditableEntity, IAggregateRoot` — created when `CreatorApplication.Approve()` succeeds:

```csharp
public sealed class CreatorProfile : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }                      // FK to Auth.User
    public Guid SourceApplicationId { get; private set; }         // FK to CreatorApplication
    public string DisplayName { get; private set; } = "";
    public string Slug { get; private set; } = "";                // auto-generated, admin-editable
    public string Bio { get; private set; } = "";
    public string? AvatarUrl { get; private set; }
    public string? CoverImageUrl { get; private set; }

    public CreatorTrustTier TrustTier { get; private set; } = CreatorTrustTier.Tier0;
    public bool EligibleForTier1 { get; private set; }            // set by Wave-8 background service
    public bool EligibleForTier2 { get; private set; }            // Wave-8

    public CreatorProfileStatus Status { get; private set; } = CreatorProfileStatus.Active;
    public string? SuspendedReason { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public Guid? SuspendedByAdminId { get; private set; }

    // Provider disclosure cross-reference
    public bool IsProvider { get; private set; }                  // computed at approval time + on provider events
    public Guid? ProviderApplicationId { get; private set; }      // null if not also a provider

    // Stats foundation — populated by background services + integration handlers
    public int ApprovedArticleCount { get; private set; }
    public int TotalViewCount { get; private set; }
    public int TotalReactionCount { get; private set; }
    public int TotalCommentCount { get; private set; }
    public int FollowerCount { get; private set; }
    public int FollowingProviderCount { get; private set; }
    public int ReportCount { get; private set; }
    public decimal ReportRate { get; private set; }               // ReportCount / max(ApprovedArticleCount, 1)

    // Owned: niches, free tags, social handles, languages, regions — same shape as CreatorApplication

    public static CreatorProfile CreateFromApprovedApplication(CreatorApplication app, string slug);
    public Result UpdateBio(string bio);
    public Result UpdateAvatar(string url);
    public Result UpdateCoverImage(string url);
    public Result ReplaceNiches(IReadOnlyList<Guid> nicheIds);
    public Result UpdateFreeTags(IReadOnlyList<string> tags);
    public Result UpdateSocialHandles(IReadOnlyDictionary<string, string> handles);
    public Result Suspend(Guid adminId, string reason, TimeProvider time);
    public Result Reinstate(Guid adminId, TimeProvider time);
    public void MarkProviderLink(Guid providerApplicationId);     // called when provider approved
    public void IncrementArticleCount();                          // called by ArticleApprovedDomainEventHandler
    public void IncrementFollower();                              // CreatorFollowAddedDomainEvent handler
    public void DecrementFollower();
}
```

### 2.3 Aggregate: `CreatorInvitation`

`AuditableEntity, IAggregateRoot` — separate aggregate for invite flow (email OR in-app):

```csharp
public sealed class CreatorInvitation : AuditableEntity, IAggregateRoot
{
    public string Token { get; private set; } = "";              // 32-byte random, base64url; null for in-app variant
    public CreatorInvitationKind Kind { get; private set; }       // Email | InApp
    public string? RecipientEmail { get; private set; }           // Email kind only
    public Guid? RecipientUserId { get; private set; }            // InApp kind only
    public string SuggestedDisplayName { get; private set; } = "";
    public string? Message { get; private set; }                  // admin's note to invitee
    public Guid SentByAdminId { get; private set; }
    public DateTime ExpiresAt { get; private set; }               // default 14d after creation
    public DateTime? RedeemedAt { get; private set; }
    public Guid? RedeemedByApplicationId { get; private set; }
    public CreatorInvitationStatus Status { get; private set; }   // Active, Redeemed, Expired, Revoked

    public static CreatorInvitation CreateEmail(string email, string displayName, string? message, Guid adminId, TimeProvider time);
    public static CreatorInvitation CreateInApp(Guid recipientUserId, string displayName, string? message, Guid adminId, TimeProvider time);
    public Result Redeem(Guid applicationId, TimeProvider time);
    public void MarkExpired(TimeProvider time);                   // called by InvitationCleanupService
    public Result Revoke(Guid adminId);
}
```

### 2.4 Aggregate: `CreatorFollow`

`BaseEntity` (junction, no soft-delete needed — unfollow = hard delete):

```csharp
public sealed class CreatorFollow : BaseEntity
{
    public Guid FollowerUserId { get; private set; }
    public Guid CreatorProfileId { get; private set; }
    public DateTime FollowedAt { get; private set; }

    public static CreatorFollow Create(Guid followerUserId, Guid creatorProfileId, TimeProvider time);
}
```

Unique index: `(FollowerUserId, CreatorProfileId)`.

### 2.5 Reference entity: `CreatorNiche` (admin-curated taxonomy)

Lightweight — managed via admin catalog (similar to Wave-1 Tags):

```csharp
public sealed class CreatorNiche : AuditableEntity
{
    public string Name { get; private set; } = "";               // History, Food, Adventure, etc.
    public string Slug { get; private set; } = "";
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
}
```

Seed via migration: `History, Food, Adventure, Religion, Family, Photography, Sustainability, Wildlife, Architecture, Culture`.

### 2.6 Enums (`ContentBlogs.Domain/Enums/`)

```csharp
public enum CreatorApplicationStatus : byte
{
    Draft = 0,
    Pending = 1,
    MoreInfoNeeded = 2,
    Approved = 3,
    Rejected = 4
}

public enum CreatorApplicationSource : byte
{
    PublicApplication = 0,
    AdminInvitation = 1
}

public enum CreatorInvitationKind : byte
{
    Email = 0,
    InApp = 1
}

public enum CreatorInvitationStatus : byte
{
    Active = 0,
    Redeemed = 1,
    Expired = 2,
    Revoked = 3
}

public enum CreatorTrustTier : byte
{
    Tier0 = 0,    // Pre-moderation (Wave-7 only state)
    Tier1 = 1,    // Post-moderation (Wave-8)
    Tier2 = 2     // Featured-eligible (Wave-8)
}

public enum CreatorProfileStatus : byte
{
    Active = 0,
    Suspended = 1
}
```

### 2.7 Domain Events (`ContentBlogs.Domain/Events/`)

| # | Event | Fields | Triggered by |
|---|---|---|---|
| 1 | `CreatorApplicationCreatedDomainEvent` | `ApplicationId, UserId, Source, CreatedAt` | `CreatorApplication.Create()` |
| 2 | `CreatorApplicationSubmittedDomainEvent` | `ApplicationId, UserId, SubmittedAt, ReapplicationCount` | `Submit()` |
| 3 | `CreatorApplicationApprovedDomainEvent` | `ApplicationId, UserId, ApprovedAt, ApprovedByAdminId, ProfileId, Slug` | `Approve()` |
| 4 | `CreatorApplicationRejectedDomainEvent` | `ApplicationId, UserId, Reason, RejectedAt, CoolingPeriodEndsAt` | `Reject()` |
| 5 | `CreatorMoreInfoRequestedDomainEvent` | `ApplicationId, UserId, RequestNotes, RequestedAt` | `RequestMoreInfo()` |
| 6 | `CreatorProfileSuspendedDomainEvent` | `ProfileId, UserId, Reason, SuspendedAt, SuspendedByAdminId` | `CreatorProfile.Suspend()` |
| 7 | `CreatorProfileReinstatedDomainEvent` | `ProfileId, UserId, ReinstatedAt, ReinstatedByAdminId` | `CreatorProfile.Reinstate()` |
| 8 | `CreatorFollowAddedDomainEvent` | `FollowerUserId, CreatorProfileId, FollowedAt` | `CreatorFollow.Create()` |
| 9 | `CreatorFollowRemovedDomainEvent` | `FollowerUserId, CreatorProfileId` | follow deletion |
| 10 | `CreatorInvitationCreatedDomainEvent` | `InvitationId, Kind, Recipient (email or userId), ExpiresAt, SentByAdminId` | `CreatorInvitation.CreateEmail/InApp()` |
| 11 | `CreatorInvitationRedeemedDomainEvent` | `InvitationId, ApplicationId, RedeemedAt` | `Redeem()` |
| 12 | `CreatorInvitationExpiredDomainEvent` | `InvitationId, ExpiredAt` | background service |

### 2.8 Integration Events (`ContentBlogs.Contracts/IntegrationEvents/Creators/`)

Topic prefix is **`creators.*`** (semantic, not module-scoped). 9 events total:

| Topic | Type | Consumers |
|---|---|---|
| `creators.application.submitted.v1` | `CreatorApplicationSubmittedIntegrationEvent` | Messaging (admin notification "New creator application") |
| `creators.application.approved.v1` | `CreatorApplicationApprovedIntegrationEvent` | Messaging (welcome email + in-app); Security (claim role "Creator") |
| `creators.application.rejected.v1` | `CreatorApplicationRejectedIntegrationEvent` | Messaging |
| `creators.application.more-info-requested.v1` | `CreatorApplicationMoreInfoRequestedIntegrationEvent` | Messaging (notify creator with admin's request notes) |
| `creators.profile.suspended.v1` | `CreatorProfileSuspendedIntegrationEvent` | Messaging; ContentBlogs internal (hide creator articles) |
| `creators.profile.reinstated.v1` | `CreatorProfileReinstatedIntegrationEvent` | Messaging; ContentBlogs internal (un-hide articles) |
| `creators.invitation.sent.v1` | `CreatorInvitationSentIntegrationEvent` | Messaging (deliver email or in-app notification) |
| `creators.invitation.redeemed.v1` | `CreatorInvitationRedeemedIntegrationEvent` | Tracking |
| `creators.follow.added.v1` | `CreatorFollowAddedIntegrationEvent` | Tracking (analytics) |

All integration events inherit `IntegrationEventBase`. Registered in `IntegrationEventTypeRegistry.cs`.

---

## 3. Existing entity extensions

### 3.1 `Article` (ContentBlogs.Domain/Entities/Articles/Article.cs)

Add the following members:

```csharp
public Guid? AuthoredByCreatorId { get; private set; }   // NULL = admin-authored; otherwise FK to CreatorProfile
public Guid LanguageId { get; private set; }             // FK to Wave-1 Languages (verify exists; add if not)
```

Add new enum value to existing `ArticleStatus`:

```csharp
public enum ArticleStatus : byte
{
    Draft = 0,
    Published = 1,
    // existing values retained
    PendingCreatorReview = 99,   // NEW — terminal in Draft phase; admin must Approve to reach Published
    Hidden = 100                  // NEW (or reuse existing) — suspended creator's articles auto-flip here
}
```

Add factory + transition methods:

```csharp
public static Article CreateByCreator(Guid creatorProfileId, Guid languageId, string title, string body, ...);
public Result SubmitForCreatorReview(TimeProvider time);   // Draft → PendingCreatorReview (creator-authored only)
public Result PublishAsCreatorAuthored(Guid adminId, TimeProvider time);  // PendingCreatorReview → Published
public Result Hide(string reason, TimeProvider time);      // for suspension cascade
public Result Unhide(TimeProvider time);                   // for reinstatement cascade
```

Existing admin moderation queue read query gets a `WHERE Status IN (Draft, PendingCreatorReview)` filter so creator-authored articles surface alongside admin drafts.

### 3.2 Permission additions

Existing `ContentBlogsFeatures.cs`:

```csharp
public const string Creator = nameof(Creator);
public const string AdminCreatorQueue = nameof(AdminCreatorQueue);
public const string AdminPostModeration = nameof(AdminPostModeration); // Wave-8 placeholder
```

Existing `ContentBlogsPermissionCatalog.cs` (add 13 entries):

```csharp
// Creator self-service
new(Creator, AppAction.Submit, PermissionGroup.SystemAccess, "Submit creator application", IsCreatorApplicationFlow: true),
new(Creator, AppAction.Read, PermissionGroup.SystemAccess, "Read own creator application/profile"),
new(Creator, AppAction.Update, PermissionGroup.SystemAccess, "Update own creator profile"),

// Article authoring by creator
new($"{Creator}.Article", AppAction.Create, PermissionGroup.SystemAccess, "Create blog article as creator"),
new($"{Creator}.Article", AppAction.Update, PermissionGroup.SystemAccess, "Update/delete own article"),

// Admin queue actions
new(AdminCreatorQueue, AppAction.Read, PermissionGroup.ModerationTools, "View creator application queue"),
new(AdminCreatorQueue, AppAction.Invite, PermissionGroup.ModerationTools, "Send creator invitation"),
new(AdminCreatorQueue, AppAction.Approve, PermissionGroup.ModerationTools, "Approve creator application"),
new(AdminCreatorQueue, AppAction.Reject, PermissionGroup.ModerationTools, "Reject creator application"),
new(AdminCreatorQueue, AppAction.RequestMoreInfo, PermissionGroup.ModerationTools, "Request more info from creator"),
new(AdminCreatorQueue, AppAction.Suspend, PermissionGroup.ModerationTools, "Suspend creator profile"),
new(AdminCreatorQueue, AppAction.Reinstate, PermissionGroup.ModerationTools, "Reinstate suspended creator"),
```

Add to `YallaJo.SharedKernel.Application/Authorization/AppAction.cs`:

```csharp
public const string Invite = nameof(Invite);
public const string RedeemInvitation = nameof(RedeemInvitation);
public const string RequestMoreInfo = nameof(RequestMoreInfo);
// Submit, Approve, Reject, Suspend, Reinstate, Register already exist (Wave-2)
```

---

## 4. Business Rules

### 4.1 Application flow
- ✅ User must have OTP-verified account before submitting (existing Wave-1 invariant)
- ✅ One creator application per user; second attempt fails with `Creator.AlreadyApplied` unless previous was Rejected
- ✅ `DisplayName` must be unique across `CreatorApplication` AND `CreatorProfile` (case-insensitive)
- ✅ 1–5 portfolio URLs required; URL format validated (no HEAD check at submit — only format)
- ✅ Exactly 3 sample work URLs required for new applications
- ✅ 3–5 niches required from active `CreatorNiche` catalog
- ✅ 0–10 free tags allowed (uses existing Wave-1 Tag entity — created on demand by admin)
- ✅ 1+ language required from Wave-1 Languages
- ✅ 0+ preferred regions (Places from Wave-3)
- ✅ Bio 50–500 chars (markdown), sanitized server-side
- ✅ Social handles: at least 1 of {Instagram, TikTok, YouTube, Twitter, Personal site}

### 4.2 Invitation flow
- ✅ Admin can send `Email` invitation to external address → tokenized link with 14-day expiry
- ✅ Admin can send `InApp` invitation to existing platform user → bell notification, no token
- ✅ Email variant: `POST /api/v1/creators/redeem-invitation` accepts `{ token, ...applicationFields }` — creates `CreatorApplication` with `Source=AdminInvitation` and `InvitationId` linked
- ✅ In-app variant: user clicks notification → existing `POST /api/v1/creators/apply` flow but with `invitationId` from notification claim
- ✅ Invitation-sourced applications skip 14-day cooling rule (no prior rejection) and reapplication count is 0
- ✅ Each invitation single-use; status flips `Active → Redeemed` atomically on application creation
- ✅ Background service expires `Active` invitations past `ExpiresAt`

### 4.3 Admin review
- ✅ 7-day SLA reminder (reuses Wave-2 reminder framework if available — otherwise document for Wave-7.1)
- ✅ `RequestMoreInfo` puts application back into creator's queue with admin's notes visible
- ✅ Rejection requires `reason` (50–1000 chars); applicant sees reason on status page
- ✅ Approval auto-creates `CreatorProfile`:
  - Slug = slugify(DisplayName), with collision suffix `-2`, `-3`, etc.
  - Admin can override slug in approval request body
  - `IsProvider` set by querying Accounts module's `ProviderApplicationRepository.ExistsApprovedForUserAsync(userId)` at approval time
- ✅ `ProviderApplicationId` populated cross-module via integration event handler (out of band) — see §5.2

### 4.4 Re-application
- ✅ 14-day cooling period from `RejectedAt`
- ✅ Max **3 attempts** total — 4th submission returns 422 `Creator.TooManyReapplications`
- ✅ Re-application increments `ReapplicationCount`, resets SLA fresh
- ✅ Previous rejection reason visible to applicant
- ✅ MoreInfoNeeded → Resubmit does NOT count toward reapplication cap (it's the same attempt)

### 4.5 Slug generation
- Auto: lowercase, ASCII-only, hyphenated, max 50 chars, no leading/trailing hyphens
- Collision: append `-2`, `-3`, …
- Admin override: `slug` field in approval payload bypasses auto-generation; still validated for uniqueness + format
- Post-approval rename: out of scope for Wave-7 (creator contacts support; manual SQL update)

### 4.6 Article authorship (creator-side)
- Creator-authored article inherits `LanguageId` from creator's primary language by default; creator can change at compose time
- Creator can save Drafts indefinitely (`Article.Status = Draft, AuthoredByCreatorId = profileId`)
- `submit-for-review` transitions Draft → PendingCreatorReview; admin queue surfaces it
- Admin approval (existing Article approve endpoint, but the handler routes by `AuthoredByCreatorId != null`):
  - Emits `BlogArticlePublishedIntegrationEvent` (existing)
  - Also emits `CreatorArticleApprovedDomainEvent` (NEW, see §2.7) → increments `CreatorProfile.ApprovedArticleCount`
- Disclosure flag (`Article.IsSponsored` bool) — minimal Wave-7 support; full enforcement is Wave-8

### 4.7 State machine

```
CreatorApplication:
(none) ──Create──▶ Draft
Draft ──Submit──▶ Pending
Pending ──Approve──▶ Approved → spawns CreatorProfile
Pending ──Reject──▶ Rejected ──(14d cooling)──▶ Draft (re-apply, max 3 attempts)
Pending ──RequestMoreInfo──▶ MoreInfoNeeded ──Resubmit──▶ Pending (no counter increment)

CreatorProfile:
Active ──Suspend──▶ Suspended ──Reinstate──▶ Active

CreatorInvitation:
Active ──Redeem──▶ Redeemed (atomic with CreatorApplication.Create)
Active ──(ExpiresAt past)──▶ Expired (background service)
Active ──Revoke──▶ Revoked (admin action)
```

### 4.8 Suspension cascade (cross-module)
- ContentBlogs internal: `CreatorProfileSuspendedDomainEvent` → all `Article.AuthoredByCreatorId == profile.Id AND Status == Published` flip to `Hidden` (preserving rest of state)
- Reinstate reverses: `Hidden → Published`
- See §5 for handler details

---

## 5. Cross-module impact

### 5.1 Cross-module event flow diagram

```
CreatorApplication.Approve()
  └─▶ CreatorApplicationApprovedDomainEvent
        └─▶ PublishCreatorApplicationApprovedHandler (ContentBlogs.Infrastructure)
              └─▶ outbox.WriteAsync(CreatorApplicationApprovedIntegrationEvent)
                    └─▶ [outbox processor]
                          ├─▶ Messaging.Infrastructure
                          │     └─▶ CreatorApprovedNotificationHandler (in-app + email)
                          ├─▶ Security.Infrastructure
                          │     └─▶ CreatorApprovedRoleAssignmentHandler (assigns "Creator" role claim)
                          └─▶ Tracking.Infrastructure
                                └─▶ CreatorApprovedTrackingHandler (funnel analytics)
```

### 5.2 Inter-module reaction matrix (integration event handlers)

These handlers run **asynchronously via outbox processor**. Each consumer module reads from its inbox, deduplicates, and acts. Wave-7 introduces 12 cross-module reactions:

| Source event | Target module | Handler file | Action |
|---|---|---|---|
| `creators.application.submitted.v1` | Messaging | `AdminNewCreatorApplicationNotificationHandler.cs` | InApp notification to all `AdminCreatorQueue.Read` permission holders ("New creator application — review needed") |
| `creators.application.approved.v1` | Messaging | `CreatorApprovedNotificationHandler.cs` | InApp + Email "Welcome, Creator!" |
| `creators.application.approved.v1` | Security | `CreatorApprovedRoleAssignmentHandler.cs` | Assign "Creator" role claim to user |
| `creators.application.approved.v1` | Tracking | `CreatorApprovedTrackingHandler.cs` | Funnel analytics: application-to-approval conversion |
| `creators.application.rejected.v1` | Messaging | `CreatorRejectedNotificationHandler.cs` | InApp + Email with rejection reason + cooling period |
| `creators.application.more-info-requested.v1` | Messaging | `CreatorMoreInfoRequestedNotificationHandler.cs` | InApp + Email "Admin requested more info" + admin's request notes + deep-link to wizard |
| `creators.profile.suspended.v1` | Messaging | `CreatorSuspendedNotificationHandler.cs` | InApp + Email "Profile suspended" |
| `creators.profile.suspended.v1` | ContentBlogs (internal) | `CreatorSuspendedHideArticlesHandler.cs` | Batch hide all published articles |
| `creators.profile.reinstated.v1` | Messaging | `CreatorReinstatedNotificationHandler.cs` | InApp + Email "Profile reinstated" |
| `creators.profile.reinstated.v1` | ContentBlogs (internal) | `CreatorReinstatedUnhideArticlesHandler.cs` | Batch un-hide articles |
| `creators.invitation.sent.v1` | Messaging | `CreatorInvitationDeliveryHandler.cs` | Branch on Kind: Email → SMTP / InApp → bell notification |
| `creators.invitation.redeemed.v1` | Tracking | `CreatorInvitationRedeemedTrackingHandler.cs` | Track conversion funnel |
| `creators.follow.added.v1` | Tracking | `CreatorFollowedTrackingHandler.cs` | Increment follower analytics |
| `accounts.provider.approved.v1` (reverse direction) | ContentBlogs | `ProviderApprovedLinkCreatorProfileHandler.cs` | Set `CreatorProfile.IsProvider=true, ProviderApplicationId=evt.ApplicationId` if creator profile exists for `evt.UserId` |

All Messaging handlers follow the existing pattern from Wave-2: check `IMessagingInboxStore.HasBeenProcessedAsync` → create `Notification` records via `Notification.Create(...)` → call `IMessagingUnitOfWork.SaveChangesAsync` → mark processed.

### 5.3 Within-module domain event handlers (synchronous, same UoW)

These handlers run **synchronously inside the same SaveChanges transaction** as the originating command via MediatR's domain-event dispatcher. They keep `CreatorProfile` denormalized counters in sync. **Critical:** these handlers MUST NOT call `SaveChangesAsync` themselves (non-negotiable rule #5).

| Domain event | Handler file | Action |
|---|---|---|
| `CreatorApplicationCreatedDomainEvent` (when `InvitationId != null`) | `MarkInvitationRedeemedHandler.cs` | Calls `CreatorInvitation.Redeem(applicationId)` on the invitation aggregate; raises `CreatorInvitationRedeemedDomainEvent` cascade |
| `CreatorApplicationApprovedDomainEvent` | (covered in command handler — `ApproveCreatorApplicationCommandHandler` creates `CreatorProfile` directly; no separate handler) | — |
| `CreatorFollowAddedDomainEvent` | `IncrementCreatorFollowerCountHandler.cs` | Loads `CreatorProfile` by `CreatorProfileId` → calls `IncrementFollower()` (no `SaveChanges` — change tracker picks it up) |
| `CreatorFollowRemovedDomainEvent` | `DecrementCreatorFollowerCountHandler.cs` | Loads `CreatorProfile` → calls `DecrementFollower()` |
| `BlogArticlePublishedDomainEvent` (when `AuthoredByCreatorId != null`) | `IncrementCreatorApprovedArticleCountHandler.cs` | Loads `CreatorProfile` by `AuthoredByCreatorId` → calls `IncrementArticleCount()` |
| `BlogArticleHiddenDomainEvent` (when `AuthoredByCreatorId != null`, hidden by suspension cascade) | (idempotent — no counter changes; the article just hides) | — |
| `CreatorApplicationCreatedDomainEvent` (when `Source = PublicApplication`) | none | emit pure audit log |
| `CreatorApplicationSubmittedDomainEvent` | none (the integration event handler in Messaging is what notifies admins) | — |
| `CreatorInvitationExpiredDomainEvent` | none | audit-only |

**Design rule:** if a within-module handler also needs to emit cross-module side effects, it should NOT do that directly — instead, the originating aggregate method also raises a separate domain event that has an integration event partner. This keeps within-module handlers idempotent and fast.

### 5.3 Provider–Creator cross-link

When a user is approved as BOTH a Provider (Accounts module) and a Creator (ContentBlogs module), each profile knows about the other:

- **Accounts.ProviderApplication** does not need to know about creators (provider remains canonical for business identity)
- **ContentBlogs.CreatorProfile.IsProvider** + `ProviderApplicationId` populated by `ProviderApprovedLinkCreatorProfileHandler`
- The reverse handler emits a `creators.profile.linked-to-provider.v1` integration event? **No** — Wave-7 keeps this one-way; Wave-8 adds bidirectional sync if needed

---

## 6. Background Services

### 6.1 `CreatorInvitationCleanupService` (Wave-7)

Runs **daily at 03:00 UTC**. Hosted in `ContentBlogs.Infrastructure/BackgroundServices/`.

```csharp
public sealed class CreatorInvitationCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<CreatorInvitationCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private static readonly int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICreatorInvitationRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IContentBlogsUnitOfWork>();
            var now = timeProvider.GetUtcNow().UtcDateTime;
            int processed = 0;

            while (true)
            {
                var batch = await repo.GetExpiredActiveInvitationsAsync(now, BatchSize, stoppingToken);
                if (batch.Count == 0) break;
                foreach (var inv in batch)
                {
                    inv.MarkExpired(timeProvider);    // raises CreatorInvitationExpiredDomainEvent
                }
                await uow.SaveChangesAsync(stoppingToken);
                processed += batch.Count;
            }
            logger.LogInformation("CreatorInvitationCleanupService processed {Count} expired invitations", processed);

            await Task.Delay(RunInterval, stoppingToken);
        }
    }
}
```

Registered in `ContentBlogs.Infrastructure.DependencyInjection`:
```csharp
services.AddHostedService<CreatorInvitationCleanupService>();
```

### 6.2 Wave-8 background services (preview)

These ship in Wave-8 but the infrastructure (worker scaffolding, scope factory pattern) is established in Wave-7:

- `CreatorTierPromotionService` — daily, marks `EligibleForTier1=true` when thresholds met (5+ approved articles AND ReportRate < 5%)
- `CreatorStatsRollupService` — hourly, recomputes `TotalViewCount, TotalReactionCount, TotalCommentCount, ReportRate` from raw data

---

## 7. Application layer (commands + queries)

### 7.1 Commands (`ContentBlogs.Application/Commands/Creators/...`)

| Command | Validator | Cache invalidation |
|---|---|---|
| `SubmitCreatorApplicationCommand` (apply OR redeem-invitation merges into this) | DisplayName 3–50, Bio 50–500, URLs format-valid, niche count 3–5, etc. | `creators:queue:tag` |
| `UpdateCreatorProfileCommand` | Bio length, social handle format | `creators:profile:{userId}:tag`, `creators:public-profile:{slug}:tag`, `creators:list:tag` |
| `CreateCreatorArticleCommand` | Title 10–200, Body 100+, LanguageId valid | `creators:articles:{creatorId}:tag`, existing `blog:articles:tag` |
| `UpdateCreatorArticleCommand` | Same as Create; only own articles in Draft | Same tags |
| `SubmitCreatorArticleForReviewCommand` | Article must be Draft + AuthoredByCreatorId match | `creators:articles:{creatorId}:tag`, `admin:creator-articles-queue:tag` |
| `DeleteCreatorArticleCommand` | Only own articles; Draft only | Same tags |
| `FollowCreatorCommand` | Cannot self-follow | `creators:public-profile:{slug}:tag`, `me:feed:{userId}:tag` |
| `UnfollowCreatorCommand` | Must currently follow | Same tags |
| `ApproveCreatorApplicationCommand` (admin) | Optional `slugOverride` | `admin:creators-queue:tag`, `creators:list:tag` |
| `RejectCreatorApplicationCommand` (admin) | Reason 50–1000 | `admin:creators-queue:tag` |
| `RequestMoreInfoCreatorApplicationCommand` (admin) | Notes 50–1000 | `admin:creators-queue:tag` |
| `SuspendCreatorProfileCommand` (admin) | Reason 50–1000 | `admin:creators-queue:tag`, `creators:list:tag`, `creators:public-profile:{slug}:tag` |
| `ReinstateCreatorProfileCommand` (admin) | — | Same tags |
| `SendCreatorInvitationCommand` (admin) | Email OR userId required (XOR); displayName 3–50 | `admin:invitations:tag` |
| `RevokeCreatorInvitationCommand` (admin) | Status must be Active | `admin:invitations:tag` |

All command handlers MUST:
- Inject `HybridCache` and call `cache.RemoveByTagAsync(tag, ct)` after `SaveChangesAsync`
- Use `ILogger<THandler>`
- Return `Result<T>` — no thrown exceptions for business failures

### 7.2 Queries (`ContentBlogs.Application/Queries/Creators/...`)

All implement `ICacheableQuery`:

| Query | Cache key | Duration | Tags |
|---|---|---|---|
| `GetMyCreatorApplicationStatusQuery(UserId)` | `creators:application:{userId}` | 5 min | `creators:application:{userId}:tag` |
| `GetMyCreatorProfileQuery(UserId)` | `creators:profile:{userId}` | 5 min | `creators:profile:{userId}:tag` |
| `GetPublicCreatorProfileQuery(Slug)` | `creators:public-profile:{slug}` | 15 min | `creators:public-profile:{slug}:tag`, `creators:list:tag` |
| `ListCreatorsQuery(NicheId?, RegionId?, LanguageId?, Page, PageSize)` | `creators:list:{niche}:{region}:{lang}:{page}` | 10 min | `creators:list:tag` |
| `GetMyCreatorFeedQuery(UserId, Page, PageSize)` | `me:feed:{userId}:{page}` | 2 min | `me:feed:{userId}:tag` |
| `GetAdminCreatorQueueQuery(StatusFilter?, SourceFilter?, EligibleForTier1?, Page, PageSize)` | `admin:creators-queue:{status}:{source}:{elig}:{page}` | 1 min | `admin:creators-queue:tag` |
| `GetAdminCreatorApplicationDetailQuery(Id)` | `admin:creator-detail:{id}` | 1 min | `admin:creators-queue:tag` |
| `ListCreatorInvitationsQuery(StatusFilter?, Page, PageSize)` | `admin:invitations:{status}:{page}` | 5 min | `admin:invitations:tag` |

### 7.3 Cache key constants (`ContentBlogs.Application/Caching/ContentBlogsCacheKeys.cs`)

Append:
```csharp
public static string CreatorApplication(Guid userId) => $"creators:application:{userId}";
public static string CreatorApplicationTag(Guid userId) => $"creators:application:{userId}:tag";
public static string CreatorProfile(Guid userId) => $"creators:profile:{userId}";
public static string CreatorProfileTag(Guid userId) => $"creators:profile:{userId}:tag";
public static string CreatorPublicProfile(string slug) => $"creators:public-profile:{slug}";
public static string CreatorPublicProfileTag(string slug) => $"creators:public-profile:{slug}:tag";
public const string CreatorList = "creators:list";
public const string CreatorListTag = "creators:list:tag";
public static string MyFeed(Guid userId, int page) => $"me:feed:{userId}:{page}";
public static string MyFeedTag(Guid userId) => $"me:feed:{userId}:tag";
public const string AdminCreatorsQueue = "admin:creators-queue";
public const string AdminCreatorsQueueTag = "admin:creators-queue:tag";
public const string AdminInvitations = "admin:invitations";
public const string AdminInvitationsTag = "admin:invitations:tag";
```

---

## 8. Infrastructure layer

### 8.1 EF Configurations (`ContentBlogs.Infrastructure/Persistence/Configurations/Creators/`)

- `CreatorApplicationConfiguration.cs` — `creators.CreatorApplications`; owned-json columns for `_portfolioUrls, _sampleWorkUrls, _nicheIds, _freeTags, _languageIds, _preferredRegionIds`; dictionary column `_socialHandles` as JSON
- `CreatorProfileConfiguration.cs` — `creators.CreatorProfiles`; unique index on `Slug`, unique on `UserId`, indexes on `(TrustTier, Status)`, `(EligibleForTier1)`
- `CreatorInvitationConfiguration.cs` — `creators.CreatorInvitations`; unique index on `Token`, index on `(Status, ExpiresAt)`, FK to `CreatorApplication` via `RedeemedByApplicationId`
- `CreatorFollowConfiguration.cs` — `creators.CreatorFollows`; unique composite `(FollowerUserId, CreatorProfileId)`, index on `CreatorProfileId` for follower-count queries
- `CreatorNicheConfiguration.cs` — `creators.CreatorNiches`; unique on `Slug`, indexes for active+ordered

All enums stored as string via `HasConversion<string>()`. All AuditableEntity tables have `IsRowVersion()` + `HasQueryFilter(p => !p.IsDeleted)`.

### 8.2 Repositories (`ContentBlogs.Infrastructure/Repositories/Creators/`)

| Interface (in Domain) | Methods |
|---|---|
| `ICreatorApplicationRepository` | `GetByIdAsync, GetByUserIdAsync, GetByDisplayNameAsync, ListQueueAsync, CountQueueAsync, ListForUserAsync` |
| `ICreatorProfileRepository` | `GetByIdAsync, GetByUserIdAsync, GetBySlugAsync, ListPublicAsync, CountPublicAsync, ExistsForUserAsync, ListNeedingTierPromotionAsync` (Wave-8 placeholder) |
| `ICreatorInvitationRepository` | `GetByIdAsync, GetByTokenAsync, ListAsync, CountAsync, GetExpiredActiveInvitationsAsync` |
| `ICreatorFollowRepository` | `ExistsAsync, GetFollowersAsync, GetFollowedCreatorsAsync, GetFollowedCreatorIdsAsync` |
| `ICreatorNicheRepository` | `ListActiveAsync, ListByIdsAsync, GetByIdAsync` |

All implementations extend `EfRepository<TEntity, Guid>` from `YallaJo.SharedKernel.Infrastructure.Data.Repositories`.

### 8.3 Outbox writer

Verify if `IContentBlogsOutboxWriter` already exists in `ContentBlogs.Domain/Repositories/`. If not, mirror the Accounts pattern:

```csharp
public interface IContentBlogsOutboxWriter
{
    Task WriteAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default) where TEvent : IIntegrationEvent;
}
```

Implementation in `ContentBlogs.Infrastructure/Repositories/ContentBlogsOutboxWriter.cs` uses `OutboxMessage.Create(integrationEvent)` + `context.OutboxMessages.Add(message)`.

### 8.4 Integration event converters

File: `ContentBlogs.Infrastructure/EventHandlers/CreatorIntegrationConverters.cs`

8 `INotificationHandler<DomainEventNotification<TDomainEvent>>` classes, one per domain event that has an integration twin:

```csharp
internal sealed class PublishCreatorApplicationApprovedHandler(
    IContentBlogsOutboxWriter outbox,
    ILogger<PublishCreatorApplicationApprovedHandler> logger)
    : INotificationHandler<DomainEventNotification<CreatorApplicationApprovedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<CreatorApplicationApprovedDomainEvent> notification, CancellationToken ct)
    {
        var e = notification.Event;
        var integration = new CreatorApplicationApprovedIntegrationEvent(
            e.ApplicationId, e.UserId, e.ProfileId, e.Slug, e.ApprovedByAdminId, e.ApprovedAt);
        await outbox.WriteAsync(integration, ct);
        logger.LogInformation("Enqueued creators.application.approved.v1 for user {UserId}", e.UserId);
    }
}
```

### 8.5 DbContext additions (`ContentBlogs.Infrastructure/Persistence/ContentBlogsDbContext.cs`)

```csharp
public DbSet<CreatorApplication> CreatorApplications => Set<CreatorApplication>();
public DbSet<CreatorProfile> CreatorProfiles => Set<CreatorProfile>();
public DbSet<CreatorInvitation> CreatorInvitations => Set<CreatorInvitation>();
public DbSet<CreatorFollow> CreatorFollows => Set<CreatorFollow>();
public DbSet<CreatorNiche> CreatorNiches => Set<CreatorNiche>();
```

`OnModelCreating` already applies configurations from assembly — new configs auto-discovered.

### 8.6 DI registrations (`ContentBlogs.Infrastructure.DependencyInjection`)

```csharp
services.AddScoped<ICreatorApplicationRepository, CreatorApplicationRepository>();
services.AddScoped<ICreatorProfileRepository, CreatorProfileRepository>();
services.AddScoped<ICreatorInvitationRepository, CreatorInvitationRepository>();
services.AddScoped<ICreatorFollowRepository, CreatorFollowRepository>();
services.AddScoped<ICreatorNicheRepository, CreatorNicheRepository>();
services.AddScoped<IContentBlogsOutboxWriter, ContentBlogsOutboxWriter>();  // if doesn't exist
services.AddHostedService<CreatorInvitationCleanupService>();
```

---

## 9. Presentation layer

### 9.1 `ContentBlogs.Presentation/Endpoints/Creators/CreatorEndpoints.cs`

Maps to `/api/v1/creators` group. 14 endpoints (customer + public).

### 9.2 `ContentBlogs.Presentation/Endpoints/Creators/AdminCreatorEndpoints.cs`

Maps to `/api/v1/admin/creators` group. 8 endpoints.

### 9.3 Wire into `ContentBlogs.Presentation/ContentBlogsEndpoints.cs`

```csharp
app.MapGroup("/api/v1/creators")
   .MapCreatorEndpoints();

app.MapGroup("/api/v1/admin/creators")
   .MapAdminCreatorEndpoints();

app.MapGroup("/api/v1/me")
   .MapMyCreatorFeedEndpoint();
```

Each endpoint MUST have `.WithMetadata(new MustHavePermissionAttribute(...))` per non-negotiable rule #1.

---

## 10. SharedKernel integration

### 10.1 `IntegrationEventTypeRegistry.cs`

Append:
```csharp
using ContentBlogs.Contracts.IntegrationEvents.Creators;  // verify path

// Inside the registry dictionary:
["creators.application.submitted.v1"]   = typeof(CreatorApplicationSubmittedIntegrationEvent),
["creators.application.approved.v1"]    = typeof(CreatorApplicationApprovedIntegrationEvent),
["creators.application.rejected.v1"]    = typeof(CreatorApplicationRejectedIntegrationEvent),
["creators.profile.suspended.v1"]       = typeof(CreatorProfileSuspendedIntegrationEvent),
["creators.profile.reinstated.v1"]      = typeof(CreatorProfileReinstatedIntegrationEvent),
["creators.invitation.sent.v1"]         = typeof(CreatorInvitationSentIntegrationEvent),
["creators.invitation.redeemed.v1"]     = typeof(CreatorInvitationRedeemedIntegrationEvent),
["creators.follow.added.v1"]            = typeof(CreatorFollowAddedIntegrationEvent),
```

Confirm `YallaJo.SharedKernel.Infrastructure.csproj` already references `ContentBlogs.Contracts` — likely yes since it references all module Contracts; add if not.

---

## 11. Migration

`ContentBlogsAddCreatorIdentityModule` — creates `creators` schema and 5 tables:

- `creators.CreatorApplications`
- `creators.CreatorProfiles`
- `creators.CreatorInvitations`
- `creators.CreatorFollows`
- `creators.CreatorNiches`

Indexes:
- `CreatorProfiles.Slug` (unique)
- `CreatorProfiles.UserId` (unique)
- `CreatorProfiles.(TrustTier, Status)`, `(EligibleForTier1)`
- `CreatorApplications.UserId`, `(Status, ReviewedAt)`, `(Source)`
- `CreatorApplications.DisplayName` (unique filtered WHERE Status != Rejected)
- `CreatorInvitations.Token` (unique), `(Status, ExpiresAt)`, `(RecipientEmail)`
- `CreatorFollows.(FollowerUserId, CreatorProfileId)` (unique), `(CreatorProfileId)`
- `CreatorNiches.Slug` (unique)

Also adds to `content_blogs.Articles`:
- `AuthoredByCreatorId` (nullable Guid, FK to `creators.CreatorProfiles.Id`)
- `LanguageId` (Guid, NOT NULL; default = first active language from Wave-1 catalog for existing rows)

Seed data: 10 `CreatorNiche` rows (History, Food, Adventure, Religion, Family, Photography, Sustainability, Wildlife, Architecture, Culture).

Generate via:
```powershell
dotnet ef migrations add ContentBlogsAddCreatorIdentityModule `
  --project 'ContentBlogs.Infrastructure\ContentBlogs.Infrastructure.csproj' `
  --startup-project 'YallaJo.Api\YallaJo.Api.csproj' `
  --context ContentBlogsDbContext --output-dir Migrations
```

---

## 12. WBS

| # | Step | Hours | Owner |
|---|---|---|---|
| 1 | SharedKernel: add `AppAction.Invite`, `RedeemInvitation`, `RequestMoreInfo` | 0.5 | Either |
| 2 | Domain: 4 aggregates + 12 domain events + enums + errors + CreatorNiche entity | 12 | Backend Dev A |
| 3 | Domain: state-machine invariants + slug generator (`SlugGenerator.Generate(displayName, existingSlugs)`) | 4 | Backend Dev A |
| 4 | Article entity extensions (AuthoredByCreatorId + LanguageId + PendingCreatorReview status + Hide/Unhide) | 3 | Backend Dev B |
| 5 | Contracts: 8 integration events + extend `ContentBlogsFeatures` + 13 permission catalog entries | 3 | Backend Dev B |
| 6 | Domain: 5 repository interfaces + extend `IContentBlogsUnitOfWork` | 1.5 | Backend Dev A |
| 7 | Application: 15 commands + validators + 8 queries + cache keys | 14 | Backend Dev A |
| 8 | Infrastructure: 5 EF configs + 5 repos + outbox writer (verify/create) + DbContext additions | 8 | Backend Dev B |
| 9 | Infrastructure: 8 integration converters + DI registrations | 3 | Backend Dev B |
| 10 | Background service: `CreatorInvitationCleanupService` | 2 | Backend Dev B |
| 11 | Presentation: `CreatorEndpoints.cs` (14 endpoints) + `AdminCreatorEndpoints.cs` (8 endpoints) + wire-up | 6 | Backend Dev A |
| 12 | SharedKernel: register 8 integration events in `IntegrationEventTypeRegistry` | 0.5 | Either |
| 13 | Cross-module handlers — Messaging × 7 (Welcome, Rejected, MoreInfoRequested, Suspended, Reinstated, InvitationDelivery, AdminNewApplicationNotification) | 7 | Backend Dev B |
| 14 | Cross-module handler — Security × 1 (role-claim assignment) | 1.5 | Backend Dev B |
| 15 | Cross-module handlers — Tracking × 3 (Approved, InvitationRedeemed, Followed) | 3 | Backend Dev A |
| 16 | Cross-module handlers — ContentBlogs internal × 2 (Hide / Unhide articles cascade) | 2 | Backend Dev A |
| 17 | Cross-module handler — ContentBlogs internal `ProviderApprovedLinkCreatorProfileHandler` | 1.5 | Backend Dev A |
| 17b | **Within-module domain event handlers × 5** (FollowerCount ±, ApprovedArticleCount +, MarkInvitationRedeemed) | 3 | Backend Dev A |
| 18 | Migration `ContentBlogsAddCreatorIdentityModule` + niche seed data | 2 | Backend Dev A |
| 19 | Unit tests: state machine, slug generator, validators, disclosure detector (Wave-7 minimal) | 6 | Both |
| 20 | Integration tests: 8 endpoint flows (apply, redeem, approve, follow, feed, suspend, reinstate, request-more-info) | 4 | Both |
| **Total** | | **~81h** | |

---

## 13. Acceptance Criteria

- [ ] All 22 missing endpoints respond per spec with correct status codes
- [ ] `CreatorApplication` state machine transitions verified end-to-end:
  - Draft → Pending → Approved (creates CreatorProfile with slug)
  - Pending → Reject → cooling period → resubmit (counter increments)
  - Pending → RequestMoreInfo → MoreInfoNeeded → Resubmit → Pending (counter does NOT increment)
- [ ] Max 3 re-applications enforced — 4th attempt returns 422 `Creator.TooManyReapplications`
- [ ] Slug generation: collision suffix (`-2`, `-3`) works; admin override validated; reserved slugs (e.g. `admin`, `api`, `me`) blocked
- [ ] Invitation expiry: `Active` invitations past `ExpiresAt` flipped to `Expired` by background service within 24h
- [ ] Invitation flows: email-token redeem works without prior auth (token validates); in-app variant works for authenticated user
- [ ] `CreatorApplicationApprovedIntegrationEvent` consumed by Security → user gets "Creator" role claim within outbox-processing window
- [ ] `CreatorApplicationApprovedIntegrationEvent` consumed by Messaging → welcome notification (in-app + email)
- [ ] Provider–Creator link: user approved as provider AFTER becoming creator → `CreatorProfile.IsProvider=true, ProviderApplicationId` populated
- [ ] Article authoring:
  - Creator creates article in Draft (`AuthoredByCreatorId=profileId`)
  - Submit-for-review transitions to PendingCreatorReview
  - Admin approve (existing endpoint, routes by `AuthoredByCreatorId`) transitions to Published + increments `ApprovedArticleCount`
- [ ] Suspension cascade: suspended creator's published articles flipped to Hidden within outbox-processing window
- [ ] Reinstatement cascade: previously hidden articles flipped back to Published
- [ ] Follow/unfollow works; duplicate follow returns 409 `Creator.AlreadyFollowing`; self-follow returns 422
- [ ] Creator feed `GET /api/v1/me/creator-feed` returns published articles from followed creators only, paginated, cached 2 min
- [ ] All endpoints use `MustHavePermissionAttribute` per non-negotiable rule #1
- [ ] All query handlers implement `ICacheableQuery`; all command handlers invalidate appropriate cache tags
- [ ] All handlers have `ILogger<THandler>`
- [ ] All domain mutations call `MarkUpdated()` (AuditableEntity)
- [ ] All domain mutations emit appropriate `DomainEventBase` via `AddDomainEvent()`
- [ ] `dotnet build` green for ContentBlogs.* + YallaJo.Api
- [ ] Unit test coverage ≥ 70% for new domain code
- [ ] Integration test green for all 8 critical flows

---

## 14. Forward Look — Wave-8

Wave-7 establishes the foundation. Wave-8 builds on it:

- `CreatorPost` aggregate (Videos, PhotoStories, LongReviews, Itineraries) — added alongside Article
- `CreatorTierPromotionService` background service uses `EligibleForTier1` flag set in Wave-7 schema
- `CreatorStatsRollupService` recomputes counts hourly
- Disclosure enforcement: domain method on `CreatorPost` (and retroactively on `Article`) checks tag overlap with `ProviderApplicationId`
- Featuring system (Tier-2 creators eligible)
- Post moderation queue (separate from blog article queue due to volume)

See `Agents/Waves/Wave-8.md` for full detail.

---

## Appendix A — Reference patterns

This wave reuses patterns established by Wave-2 verbatim:

- State machine aggregate: see `Accounts.Domain/Entities/ProviderApplication.cs`
- Integration event converter: see `Accounts.Infrastructure/EventHandlers/AccountsIntegrationConverters.cs`
- Cross-module notification handler: see `Messaging.Infrastructure/EventHandlers/ProviderApprovedNotificationHandler.cs`
- Cross-module cascade handler: see `ContentTours.Infrastructure/EventHandlers/ProviderSuspendedSuspendToursHandler.cs`
- Background service: see `Booking.Infrastructure/BackgroundServices/DocumentExpiryCheckService.cs` (if exists) or use `CreatorInvitationCleanupService.cs` template above

When in doubt, mirror Wave-2 exactly.
