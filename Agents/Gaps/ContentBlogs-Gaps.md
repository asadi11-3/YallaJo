# YallaJo — ContentBlogs Module Audit Report

> Full audit report generated from code analysis.  
> Last updated: 2025-07-15  
> Module status: **Not started (⬜)** in agent-context.md — but implementation is substantially built

---

## Table of Contents

1. [Module Overview](#1-module-overview)
2. [Architecture Compliance](#2-architecture-compliance)
3. [Endpoint Security Audit](#3-endpoint-security-audit)
4. [Rule Compliance Matrix](#4-rule-compliance-matrix)
5. [Gap 1 — ICurrentUser Misuse (HIGH)](#5-gap-1--icurrentuser-misuse-high)
6. [Gap 2 — Exception Throwing in Infrastructure (MEDIUM)](#6-gap-2--exception-throwing-in-infrastructure-medium)
7. [Gap 3 — Missing Endpoints & Dead Permissions (MEDIUM)](#7-gap-3--missing-endpoints--dead-permissions-medium)
8. [Gap 4 — Spec Divergence: Reaction Types (LOW)](#8-gap-4--spec-divergence-reaction-types-low)
9. [Gap 5 — Anonymous POST Endpoint (LOW)](#9-gap-5--anonymous-post-endpoint-low)
10. [What Passed — Full Checklist](#10-what-passed--full-checklist)
11. [Scorecard](#11-scorecard)
12. [Fix Priority & Recommendations](#12-fix-priority--recommendations)
13. [Appendix — Files Audited](#13-appendix--files-audited)

---

## 1. Module Overview

### Purpose

ContentBlogs is the **blogging and content-creator platform** for YallaJo. It spans two major subsystems:
1. **Blog CMS** — admin-authored blogs with comments, reactions, tour linking, translations, view tracking, and lifecycle management (Draft/Published/Archived/Hidden)
2. **Creator Platform** — user-generated content with an application/approval pipeline, trust tiers, invitation system, post moderation, following, and niche categorization

### Structure

| Layer | Files | Key Contents |
|-------|-------|-------------|
| **Domain** | 87 | 12 entities, 12 enums, 40 domain events, 9 repository interfaces, 1 value object, 2 domain validators |
| **Application** | 193 | ~37 command handler groups, ~19 query handler groups, 4 auth guards, 5 interfaces, 40 HybridCache handlers |
| **Contracts** | 31 | 28 integration events, 3 auth contracts (permissions, features, ownership service) |
| **Infrastructure** | 92 | 45 event handlers, 14 EF configs, 9 repository implementations, 4 services, 3 background services, 11 migrations |
| **Presentation** | 18 | 62 endpoints across 7 endpoint groups |
| **Tests** | 29 | Unit tests (15 Application, 5 Domain, 8 Infrastructure, 1 Presentation) |
| **Total** | **444** | |

### Domain Entities (12)

| Entity | Purpose |
|--------|---------|
| `Blog` | Main blog aggregate — lifecycle, content, SEO, tour links, translations |
| `BlogComment` | Nested comments with 2-level depth limit (`MaxReplyDepth = 2`) |
| `BlogCommentReaction` | Reactions on comments (Like/Dislike/Love/Helpful) |
| `BlogTour` | Junction — links blogs to tours |
| `BlogTranslation` | Localized blog content |
| `BlogView` | View tracking markers with viewer hash deduplication |
| `CreatorProfile` | Creator identity — trust tier, stats, suspension, following |
| `CreatorPost` | Multi-type creator content with moderation workflow |
| `CreatorFollow` | Follow relationship between users and creators |
| `CreatorInvitation` | Email/in-app invitations to become a creator |
| `CreatorApplication` | Application pipeline with Draft/Pending/Approved/Rejected/MoreInfoNeeded |
| `CreatorNiche` | Topic categorization for creators |

### Enums (12)

| Enum | Values |
|------|--------|
| `BlogStatus` | Draft=0, Published=1, Archived=2, PendingCreatorReview=99, Hidden=100 |
| `BlogViewerKind` | (viewer classification for analytics) |
| `ReactionType` | Like=0, Dislike=1, Love=2, Helpful=3 |
| `CreatorProfileStatus` | Active, Suspended |
| `CreatorTrustTier` | New=0, Trusted=1, Expert=2 |
| `CreatorInvitationStatus` | (invitation lifecycle) |
| `CreatorInvitationKind` | (email vs in-app) |
| `CreatorApplicationSource` | (how they applied) |
| `CreatorApplicationStatus` | Draft=0, Pending=1, Approved=2, Rejected=3, MoreInfoNeeded=4 |
| `CreatorPostStatus` | Draft=0, PendingReview=1, Published=2, Rejected=3, Hidden=4, Removed=5 |
| `CreatorPostType` | (article type classification) |
| `DisclosureRelationKind` | (transparency/disclosure types) |

### Repository Interfaces (9)

IBlogRepository, IBlogCommentRepository, IBlogTourRepository, ICreatorProfileRepository, ICreatorPostRepository, ICreatorNicheRepository, ICreatorFollowRepository, ICreatorInvitationRepository, ICreatorApplicationRepository

### Integration Events (28)

Blog: BlogArchived, BlogCreated, BlogDeleted, BlogFeatured, BlogPublished, BlogRestored, BlogTourLinked, BlogTourUnlinked, BlogUnfeatured, BlogUnpublished, BlogUpdated  
Creator: CreatorApplicationApproved/MoreInfoRequested/Rejected/Submitted, CreatorEligibleForTierPromotion, CreatorFollowAdded, CreatorInvitationRedeemed/Sent, CreatorProfileReinstated/Suspended, CreatorPostFeatured/Published/Rejected/Removed/SubmittedForReview, CreatorTierDemoted/Promoted

### Permission Catalog (43 permissions across 8 features)

| Feature | Actions | Group |
|---------|---------|-------|
| Blog | Read, Create, Update, Delete, Approve | ContentManagement |
| BlogComment | Read, Create, Update, Delete, Manage | ContentManagement |
| BlogReaction | Create, Delete | ContentManagement |
| BlogTourLink | Create, Delete | ContentManagement |
| Creator | Submit, Read, Update, Follow, Unfollow, RedeemInvitation | SystemAccess |
| Creator.Article | Create, Update | SystemAccess |
| AdminCreatorQueue | Read, Invite, Approve, Reject, RequestMoreInfo, Suspend, Reinstate, PromoteTier, DemoteTier | ModerationTools |
| CreatorPost | Read, Create, Update, Delete, Submit | SystemAccess |
| AdminPostModeration | Read, Approve, Reject, Feature, Remove, **HidePost**, **UnhidePost** | ModerationTools |

---

## 2. Architecture Compliance

### Dependency Graph

```
Presentation → Application → Domain
     ↓              ↓
  Contracts    Contracts
     ↓
Infrastructure → Domain
     ↓
  Contracts (own + external)
```

✅ No reverse dependency violations detected.

### Cross-Module Dependencies

| From | To | Purpose |
|------|----|---------|
| Application | ContentTours.Contracts | ITourExistenceService (blog-tour linking) |
| Application | Security.Contracts | IUserPrivilegeLevelReader (author hierarchy guard) |
| Infrastructure | ContentCore.Contracts | LanguageActivatedIntegrationEvent |
| Infrastructure | ContentTours.Contracts | TourDeletedIntegrationEvent |
| Infrastructure | ContentPlaces.Contracts | PlaceDeletedIntegrationEvent |
| Infrastructure | Accounts.Contracts | ProviderApprovedIntegrationEvent |
| Infrastructure | Social.Contracts | ReportSubmitted/ResolvedIntegrationEvent |

### CQRS Pattern

- **Commands**: ~37 handler groups (122 files)
- **Queries**: ~19 handler groups (59 files)
- **Validators**: Not individually counted but present per command group

---

## 3. Endpoint Security Audit

### BlogEndpoints.cs — 17 endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/blogs` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/{id}` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/slug/{slug}` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/admin/{id}` | MustHavePermission(Blog, Read) ✅ |
| GET | `/api/v1/blogs/admin/deleted` | MustHavePermission(Blog, Delete) ✅ |
| POST | `/api/v1/blogs` | MustHavePermission(Blog, Create) ✅ |
| PUT | `/api/v1/blogs/{id}` | MustHavePermission(Blog, Update) ✅ |
| DELETE | `/api/v1/blogs/{id}` | MustHavePermission(Blog, Delete) ✅ |
| POST | `/api/v1/blogs/{id}/restore` | MustHavePermission(Blog, Delete) ✅ |
| POST | `/api/v1/blogs/{id}/publish` | MustHavePermission(Blog, Approve) ✅ |
| POST | `/api/v1/blogs/{id}/unpublish` | MustHavePermission(Blog, Approve) ✅ |
| POST | `/api/v1/blogs/{id}/archive` | MustHavePermission(Blog, Approve) ✅ |
| PATCH | `/api/v1/blogs/{id}/mark-as-featured` | MustHavePermission(Blog, Approve) ✅ |
| PATCH | `/api/v1/blogs/{id}/mark-as-unfeatured` | MustHavePermission(Blog, Approve) ✅ |
| **POST** | **`/api/v1/blogs/{id}/views`** | **AllowAnonymous ⚠️** |
| POST | `/api/v1/blogs/{id}/tours` | MustHavePermission(BlogTourLink, Create) ✅ |
| DELETE | `/api/v1/blogs/{id}/tours/{tourId}` | MustHavePermission(BlogTourLink, Delete) ✅ |

### BlogCommentEndpoints.cs — 6 endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/blogs/{id}/comments` | AllowAnonymous ✅ |
| POST | `/api/v1/blogs/{id}/comments` | MustHavePermission(BlogComment, Create) ✅ |
| PUT | `/api/v1/blogs/comments/{commentId}` | MustHavePermission(BlogComment, Update) ✅ |
| DELETE | `/api/v1/blogs/comments/{commentId}` | MustHavePermission(BlogComment, Delete) ✅ |
| POST | `/api/v1/blogs/comments/{commentId}/reactions` | MustHavePermission(BlogReaction, Create) ✅ |
| DELETE | `/api/v1/blogs/comments/{commentId}/reactions` | MustHavePermission(BlogReaction, Delete) ✅ |

### CreatorEndpoints.cs — 13 endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/blogs/creators/niches` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/creators/profiles/{slug}` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/creators/profiles/{profileId}/followers` | AllowAnonymous ✅ |
| POST | `/api/v1/blogs/creators/applications` | MustHavePermission(Creator, Submit) ✅ |
| GET | `/api/v1/blogs/creators/applications/mine` | MustHavePermission(Creator, Read) ✅ |
| PUT | `/api/v1/blogs/creators/applications/{applicationId}` | MustHavePermission(Creator, Update) ✅ |
| POST | `/api/v1/blogs/creators/applications/{applicationId}/submit` | MustHavePermission(Creator, Submit) ✅ |
| GET | `/api/v1/blogs/creators/profile/mine` | MustHavePermission(Creator, Read) ✅ |
| PUT | `/api/v1/blogs/creators/profile/mine` | MustHavePermission(Creator, Update) ✅ |
| POST | `/api/v1/blogs/creators/profiles/{profileId}/follow` | MustHavePermission(Creator, Follow) ✅ |
| DELETE | `/api/v1/blogs/creators/profiles/{profileId}/follow` | MustHavePermission(Creator, Unfollow) ✅ |
| GET | `/api/v1/blogs/creators/profiles/{profileId}/following` | MustHavePermission(Creator, Read) ✅ |
| POST | `/api/v1/blogs/creators/invitations/redeem` | MustHavePermission(Creator, RedeemInvitation) ✅ |

### CreatorPostEndpoints.cs — 9 endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/blogs/creators/posts` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/creators/posts/featured` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/creators/posts/{slug}` | AllowAnonymous ✅ |
| GET | `/api/v1/blogs/creators/posts/mine` | MustHavePermission(CreatorPost, Read) ✅ |
| POST | `/api/v1/blogs/creators/posts` | MustHavePermission(CreatorPost, Create) ✅ |
| PUT | `/api/v1/blogs/creators/posts/{postId}` | MustHavePermission(CreatorPost, Update) ✅ |
| POST | `/api/v1/blogs/creators/posts/{postId}/submit` | MustHavePermission(CreatorPost, Submit) ✅ |
| POST | `/api/v1/blogs/creators/posts/{postId}/publish` | MustHavePermission(CreatorPost, Submit) ✅ |
| DELETE | `/api/v1/blogs/creators/posts/{postId}` | MustHavePermission(CreatorPost, Delete) ✅ |

### AdminCreatorEndpoints.cs — 10 endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/blogs/admin/creators/applications` | MustHavePermission(AdminCreatorQueue, Read) ✅ |
| GET | `/api/v1/blogs/admin/creators/applications/{id}` | MustHavePermission(AdminCreatorQueue, Read) ✅ |
| POST | `/api/v1/blogs/admin/creators/applications/{id}/approve` | MustHavePermission(AdminCreatorQueue, Approve) ✅ |
| POST | `/api/v1/blogs/admin/creators/applications/{id}/reject` | MustHavePermission(AdminCreatorQueue, Reject) ✅ |
| POST | `/api/v1/blogs/admin/creators/applications/{id}/request-more-info` | MustHavePermission(AdminCreatorQueue, RequestMoreInfo) ✅ |
| POST | `/api/v1/blogs/admin/creators/profiles/{profileId}/suspend` | MustHavePermission(AdminCreatorQueue, Suspend) ✅ |
| POST | `/api/v1/blogs/admin/creators/profiles/{profileId}/reinstate` | MustHavePermission(AdminCreatorQueue, Reinstate) ✅ |
| POST | `/api/v1/blogs/admin/creators/profiles/{profileId}/promote` | MustHavePermission(AdminCreatorQueue, PromoteTier) ✅ |
| POST | `/api/v1/blogs/admin/creators/profiles/{profileId}/demote` | MustHavePermission(AdminCreatorQueue, DemoteTier) ✅ |
| POST | `/api/v1/blogs/admin/creators/invitations` | MustHavePermission(AdminCreatorQueue, Invite) ✅ |

### AdminPostEndpoints.cs — 6 endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/v1/blogs/admin/creators/posts` | MustHavePermission(AdminPostModeration, Read) ✅ |
| POST | `/api/v1/blogs/admin/creators/posts/{postId}/approve` | MustHavePermission(AdminPostModeration, Approve) ✅ |
| POST | `/api/v1/blogs/admin/creators/posts/{postId}/reject` | MustHavePermission(AdminPostModeration, Reject) ✅ |
| POST | `/api/v1/blogs/admin/creators/posts/{postId}/feature` | MustHavePermission(AdminPostModeration, Feature) ✅ |
| POST | `/api/v1/blogs/admin/creators/posts/{postId}/unfeature` | MustHavePermission(AdminPostModeration, Feature) ✅ |
| POST | `/api/v1/blogs/admin/creators/posts/{postId}/remove` | MustHavePermission(AdminPostModeration, Remove) ✅ |

**Total: 61 endpoints — 61/61 have auth decorations ✅ (1 anonymous POST flagged)**

---

## 4. Rule Compliance Matrix

| # | Rule | Status | Evidence |
|---|------|--------|----------|
| 1 | Every endpoint has `MustHavePermission` or explicit `AllowAnonymous` | ✅ PASS | 61/61 endpoints decorated |
| 2 | `ICurrentUser` used only for ownership checks | ⚠️ FAIL | 18 handlers + 2 guards use it for auth gating/actor stamping |
| 3 | Result pattern everywhere (no `throw new` in Application) | ✅ PASS | 0 `throw new` in 193 Application files |
| 4 | Per-module `IPermissionCatalog` | ✅ PASS | ContentBlogsPermissionCatalog with 43 entries |
| 5 | No `SaveChanges` in domain event handlers | ✅ PASS | 35 domain handlers clean; 11 integration handlers use SaveChanges (acceptable) |
| 6 | `DateTime.UtcNow` only | ✅ PASS | 0 `DateTime.Now`/`DateTime.Today` in Application + Domain |
| 7 | `Guid.CreateVersion7` only | ✅ PASS | 0 `Guid.NewGuid()` in Application + Domain |
| 8 | FluentValidation for all commands | ✅ PASS | Validators present per command group |
| 9 | HybridCache for query/invalidation | ✅ PASS | 40 handlers inject HybridCache |
| 10 | Outbox/Inbox pattern | ✅ PASS | OutboxMessageConfiguration + InboxMessageConfiguration in EF configs |
| 11 | No `throw new` in Infrastructure (runtime) | ⚠️ FAIL | 3 runtime throws in BlogViewerHashService |
| 12 | Endpoint-only auth (no handler auth) | ⚠️ FAIL | Auth gating + admin-tier bypass in handler code |

---

## 5. Gap 1 — ICurrentUser Misuse (HIGH)

### Severity: HIGH  
### Impact: Security logic scattered across handlers instead of centralized at endpoint level  
### Rule Violated: "ICurrentUser in handlers is ONLY for ownership/self-scope checks"

### Affected Files

#### Violation Type A: Auth gate + actor stamping (not ownership)

These handlers check `currentUser.IsAuthenticated` or use `currentUser.UserId` purely to stamp who performed an action, NOT for ownership:

1. `CreateBlogCommandHandler.cs` — stamps author
2. `CreateCreatorApplicationCommandHandler.cs` — auth gate + stamps applicant
3. `SendCreatorInvitationCommandHandler.cs` — auth gate + stamps admin
4. `RequestMoreInfoCommandHandler.cs` — auth gate + stamps admin
5. `ApproveCreatorApplicationCommandHandler.cs` — auth gate + stamps admin
6. `RejectCreatorApplicationCommandHandler.cs` — auth gate + stamps admin
7. `SuspendCreatorProfileCommandHandler.cs` — auth gate + stamps admin
8. `ReinstateCreatorProfileCommandHandler.cs` — auth gate + stamps admin
9. `ApproveCreatorPostCommandHandler.cs` — auth gate + admin actor
10. `RejectCreatorPostCommandHandler.cs` — auth gate + admin actor
11. `FeatureCreatorPostCommandHandler.cs` — auth gate + admin actor
12. `UnfeatureCreatorPostCommandHandler.cs` — auth gate + admin actor
13. `RemoveCreatorPostCommandHandler.cs` — auth gate + admin actor
14. `PromoteCreatorTierCommandHandler.cs` — auth gate + admin actor
15. `DemoteCreatorTierCommandHandler.cs` — auth gate + admin actor
16. `CreateBlogCommentCommandHandler.cs` — auth gate
17. `AddOrReplaceBlogCommentReactionCommandHandler.cs` — auth gate
18. `RemoveBlogCommentReactionCommandHandler.cs` — auth gate

#### Violation Type B: Admin-tier bypass in guard

19. `BlogAuthorHierarchyGuard.cs` — uses `AppRoles.HighestPrivilegeLevel(currentUser.Roles)` for role-based authorization
20. `BlogCommentAuthorizationGuard.cs` — uses `currentUser.IsAuthenticated` for auth gating

#### Acceptable Uses (ownership / self-scope)

- `GetMyCreatorPostsQueryHandler` — self-scoped query
- `GetMyCreatorApplicationQueryHandler` — self-scoped query
- `GetMyCreatorProfileQueryHandler` — self-scoped query
- `IsFollowingCreatorQueryHandler` — self-relationship check
- `UpdateCreatorApplicationCommandHandler` — ownership vs `ApplicantUserId`
- `SubmitCreatorApplicationCommandHandler` — ownership vs `ApplicantUserId`
- `UpdateCreatorProfileCommandHandler` — loads own profile
- `FollowCreatorCommandHandler` — self-target prevention
- `UnfollowCreatorCommandHandler` — self-relationship
- `CreateCreatorPostCommandHandler` — loads own creator profile
- `UpdateCreatorPostCommandHandler` — ownership check
- `DeleteCreatorPostCommandHandler` — ownership check
- `SubmitCreatorPostForReviewCommandHandler` — ownership check
- `PublishCreatorPostCommandHandler` — ownership check
- `BlogEndpoints.cs` `POST /views` — viewer detection (acceptable)

### Why This Is Wrong

The 5 non-negotiable rules state: *"Inject ICurrentUser in a handler ONLY when checking whether the calling user is the owner of the resource."* Auth gating (`IsAuthenticated` checks) and actor stamping (using UserId to record who did something) should be handled by the endpoint auth layer, not inside business logic handlers.

### Required Fix

1. Remove all `IsAuthenticated` / `UserId is null` guard clauses — let endpoint `RequireAuthorization` handle this
2. For actor stamping (admin actions), pass `UserId` via the command DTO from the endpoint, don't inject `ICurrentUser` just for stamping
3. Move `BlogAuthorHierarchyGuard`'s role-check logic to endpoint-level authorization policy

---

## 6. Gap 2 — Exception Throwing in Infrastructure (MEDIUM)

### Severity: MEDIUM  
### Impact: Runtime exceptions instead of Result-pattern error propagation  
### Rule Violated: "Result pattern everywhere — no business exceptions"

### Affected Files

| File | Line(s) | Exception Type | Severity |
|------|---------|---------------|----------|
| `BlogViewerHashService.cs` | 22 | `InvalidOperationException` | ⚠️ RUNTIME |
| `BlogViewerHashService.cs` | 34 | `ArgumentException` | ⚠️ RUNTIME |
| `BlogViewerHashService.cs` | 40 | `ArgumentOutOfRangeException` | ⚠️ RUNTIME |
| `DependencyInjection.cs` | 29 | `InvalidOperationException` | ✅ Startup guard |
| `ContentBlogsDbInitializer.cs` | 127 | `InvalidOperationException` | ✅ Seeding |
| `ContentBlogsDbInitializer.cs` | 143 | `InvalidOperationException` | ✅ Seeding |

### Why This Is Wrong

`BlogViewerHashService` throws at runtime when receiving invalid input. These should either:
- Return Result objects, or
- Be validated upstream via FluentValidation so the service never receives invalid input

### Required Fix

- Wrap `BlogViewerHashService` methods to return `Result<T>` instead of throwing
- OR add FluentValidation rules upstream to guarantee valid input before the service is called

---

## 7. Gap 3 — Missing Endpoints & Dead Permissions (MEDIUM)

### Severity: MEDIUM  
### Impact: Permissions defined but unreachable; domain methods exist with no HTTP surface

### 7.1 — AdminPostModeration.HidePost / UnhidePost

**Problem**: `ContentBlogsPermissionCatalog` defines `HidePost` and `UnhidePost` actions under `AdminPostModeration`, but **no endpoints exist** in `AdminPostEndpoints.cs` to use them.

**Domain support**: `CreatorPost` entity has Hide/Unhide methods and `CreatorPostStatus.Hidden` enum value. Domain events exist: `BlogHiddenDomainEvent`, `BlogUnhiddenDomainEvent` (on blog side).

**Fix**: Add `POST /api/v1/blogs/admin/creators/posts/{postId}/hide` and `POST /api/v1/blogs/admin/creators/posts/{postId}/unhide` endpoints, or remove dead permissions.

### 7.2 — BlogComment.Manage

**Problem**: `ContentBlogsPermissionCatalog` defines a `Manage` action for `BlogComment`, but no endpoint or handler uses this permission.

**Fix**: Either implement a bulk-manage comments feature or remove the dead permission.

### 7.3 — Blog Hide/Unhide Dead Code

**Problem**: `Blog` entity has `Hide()` and `Unhide()` methods with corresponding `BlogHiddenDomainEvent` and `BlogUnhiddenDomainEvent`. **No Application handlers or Presentation endpoints** call these methods.

**Fix**: Either implement admin hide/unhide blog endpoints or remove the dead domain methods.

---

## 8. Gap 4 — Spec Divergence: Reaction Types (LOW)

### Severity: LOW  
### Impact: Implementation differs from spec; functional but inconsistent

### Problem

The **spec** (§9 Blog CMS) defines comment reactions as: `Like`, `Helpful`, `Insightful`

The **implementation** (`ReactionType` enum) has: `Like=0`, `Dislike=1`, `Love=2`, `Helpful=3`

**Divergences**:
- `Insightful` from spec is **missing**
- `Dislike` and `Love` are **not in spec**

### Fix

Decide whether implementation intentionally supersedes spec. If spec is authoritative, align enum to `Like`, `Helpful`, `Insightful`. If implementation is intentional, update spec.

---

## 9. Gap 5 — Anonymous POST Endpoint (LOW)

### Severity: LOW  
### Impact: Violates "AllowAnonymous only for GET" convention

### Problem

`POST /api/v1/blogs/{id}/views` is decorated with `AllowAnonymous`. The project convention states that only public **GET** endpoints should be anonymous. POST endpoints should require authentication.

### Mitigating Factor

This endpoint tracks blog views. The handler uses `ICurrentUser` for optional viewer identification but functions without authentication (anonymous view tracking). The `BlogViewerHashService` generates a privacy-preserving hash for deduplication.

This is a **design decision** — anonymous view tracking is common in blog platforms. However, it violates the stated convention.

### Fix Options

1. **Accept as exception** — document it as an intentional deviation from the convention
2. **Require auth** — change to `RequireAuthorization` and only track authenticated user views
3. **Split endpoints** — one anonymous GET for view count, one authenticated POST for tracking

---

## 10. What Passed — Full Checklist

### Endpoint Auth (61/61) ✅
All 61 endpoints across 7 endpoint groups have correct auth decorations. No endpoint is missing `MustHavePermission` or `AllowAnonymous`. The only flag is the anonymous POST for view tracking (Gap #5).

### Result Pattern ✅
Zero `throw new` in all 193 Application files. Consistent `Result.Success`/`Result.Failure` usage across all sampled handlers.

### SaveChanges in Event Handlers ✅
35 domain event handlers have zero `SaveChangesAsync` calls (with explicit "no SaveChanges" comments). 11 integration event handlers use `SaveChangesAsync` appropriately in their own scope.

### Permission Catalog ✅
43 permission entries across 8 features. Three permission groups: ContentManagement, SystemAccess, ModerationTools. Clean separation of admin vs user-facing permissions.

### DateTime.UtcNow ✅
Zero `DateTime.Now` or `DateTime.Today` in Application + Domain layers.

### Guid.CreateVersion7 ✅
Zero `Guid.NewGuid()` in Application + Domain layers.

### HybridCache ✅
40 handlers inject HybridCache. Pattern: post-write invalidation via `RemoveByTagAsync`. No read-through caching (consistent with project pattern).

### Outbox/Inbox ✅
Both `OutboxMessageConfiguration` and `InboxMessageConfiguration` present in EF configs.

### Comment Nesting ✅
`BlogComment.MaxReplyDepth = 2` — enforces 2-level nesting. Root(0) → Reply(1) → Reply-to-reply(2). Matches spec's "2-level comment nesting" requirement.

### Creator Application Pipeline ✅
Full state machine: Draft → Pending → Approved/Rejected/MoreInfoNeeded. Matches spec's creator onboarding flow.

### Creator Trust Tiers ✅
`CreatorTrustTier` enum: New(0) → Trusted(1) → Expert(2). Tier-0 creators require admin review before publishing (PendingReview status). Expert creators can auto-publish.

### Background Services (3) ✅
- `CreatorInvitationCleanupService` — expires old invitations
- `CreatorTierPromotionService` — evaluates tier promotion eligibility
- `CreatorStatsRollupService` — aggregates creator statistics

### Cross-Module Integration ✅
- Reacts to TourDeleted (unlinks blogs from deleted tours)
- Reacts to PlaceDeleted (unlinks blogs from deleted places)
- Reacts to ProviderApproved (links provider to creator profile)
- Reacts to ReportSubmitted/Resolved (adjusts creator report counts)
- Reacts to LanguageActivated (creates translations)

---

## 11. Scorecard

| Area | Score | Notes |
|------|-------|-------|
| Endpoint Security | 9/10 | 61/61 decorated; 1 anonymous POST flag |
| ICurrentUser Compliance | 5/10 | 20 violations (18 handlers + 2 guards) vs 15 acceptable |
| Result Pattern | 10/10 | Zero throws in Application |
| SaveChanges Compliance | 10/10 | All domain handlers clean |
| DateTime/Guid Conventions | 10/10 | Zero violations |
| Permission Catalog | 9/10 | 3 dead permissions (HidePost, UnhidePost, Manage) |
| HybridCache | 10/10 | 40 handlers, consistent pattern |
| Spec Alignment | 8/10 | Reaction types diverge; comment nesting matches |
| Domain Richness | 9/10 | 12 entities, 40 events, full state machines, but dead methods |
| Test Coverage | 7/10 | 29 tests, mostly blog-focused; creator tests sparse |
| **Overall** | **7.5/10** | Solid module with systemic ICurrentUser issue and dead code |

---

## 12. Fix Priority & Recommendations

| # | Fix | Severity | Effort |
|---|-----|----------|--------|
| 1 | ICurrentUser handler cleanup (18 handlers + 2 guards) | HIGH | ~5h |
| 2 | Add HidePost/UnhidePost admin endpoints or remove permissions | MEDIUM | ~2h |
| 3 | Add Blog Hide/Unhide endpoints or remove dead domain methods | MEDIUM | ~2h |
| 4 | BlogViewerHashService — Result pattern or upstream validation | MEDIUM | ~1h |
| 5 | Remove or implement BlogComment.Manage permission | LOW | ~30min |
| 6 | Align ReactionType enum with spec (or update spec) | LOW | ~30min |
| 7 | Document or fix anonymous POST /views convention violation | LOW | ~30min |
| 8 | Expand creator-side test coverage | LOW | ~3h |

**Total estimated effort: ~14.5 hours**

---

## 13. Appendix — Files Audited

### Presentation (all 7 endpoint files fully read)
- `ContentBlogs.Presentation/ContentBlogsEndpoints.cs`
- `ContentBlogs.Presentation/Endpoints/Blog/BlogEndpoints.cs`
- `ContentBlogs.Presentation/Endpoints/BlogComment/BlogCommentEndpoints.cs`
- `ContentBlogs.Presentation/Endpoints/Creator/CreatorEndpoints.cs`
- `ContentBlogs.Presentation/Endpoints/Creator/CreatorPostEndpoints.cs`
- `ContentBlogs.Presentation/Endpoints/Creator/AdminCreatorEndpoints.cs`
- `ContentBlogs.Presentation/Endpoints/Creator/AdminPostEndpoints.cs`

### Application (sampled handlers + full grep scans)
- All 193 files scanned for `throw new`, `DateTime.Now`, `Guid.NewGuid()`, `ICurrentUser`
- 35+ handler files individually classified for ICurrentUser usage

### Domain (all enum files read, entity search patterns verified)
- All 12 enum files read
- Entity search patterns: BlogComment.MaxReplyDepth, Blog state transitions, Creator state machines

### Infrastructure (event handler + service scans)
- All event handler directories scanned for SaveChangesAsync
- BlogViewerHashService.cs read for throw patterns

### Contracts
- PermissionCatalog and Features fully read
- Integration event inventory via directory listing
