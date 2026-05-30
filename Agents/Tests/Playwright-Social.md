# Playwright MCP Test Scenarios — Social Module

> **API-ONLY MODE.** This checkout has no `YallaJo.Web`. Every `browser_navigate("https://localhost:57065/swagger…")` step below is a docking step; the actual request runs through `window.__yj.apiFetch(...)` defined in [`Playwright-APIOnly-Adapter.md`](./Playwright-APIOnly-Adapter.md). Read that adapter once at the start of every Playwright session — it also lists the 8 seeded test users and their credentials.

## Source Plans / Score

| Source | Score / Status | Test impact |
|---|---:|---|
| `Agents/Plans/Social-Workflow.md` | Implemented, audited 7.8/10; W3-B notes present | Primary source for Reviews, Favorites/Wishlist, Reports, Moderation, helpful votes, cache, route prefix `/api/v1/social/*`. |
| `Agents/Plans/Social-Audit-Report.md` | 7.8/10 | Confirms 23 REST endpoints, 13 entities, review state machine, rating cache, report flow, key gaps. |
| `Agents/Plans/Social-FixPlan.md` | Fix backlog | Confirms public reviews, HybridCache, warn/ban, helpful vote query, validators as coverage targets. |
| `Agents/Plans/BlogCreatorPost-Merger.md` | Executed; merger substantially complete | Primary source for unified Blog lifecycle, creator blog routes, comments/reactions/counters, creator follows. |
| `Agents/Plans/BlogCreatorPost-Merger-Audit-Report.md` | 8.5/10 | Confirms Blog entity/status, 24 blog endpoints, 17 creator endpoints, route differences, integration events. |
| `Agents/Plans/BlogCreatorPost-Merger-FixPlan.md` | Cleanup-only | Duplicate featuring/remnant cleanup informs Known Divergence. |
| `Agents/Plans/Master-RoadmapTo10.md` | Social 7.8 → 10 target; Blog 9.3 → 10 target | Confirms Social priorities: public reviews, warn/ban, cache, validators, helpful votes. |
| `Agents/Plans/CrossDocumentAnalysisReport.md` | Contradictions resolved | Use task-level resolutions: anyone authenticated may review; verified badge only when completed booking exists; ratings use 0.5 increments; auto-hide after 5 reports; profanity filtering is redundant across modules. |

**Scenario count:** 62 total = 52 active, 6 NOT_BUILT, 4 DEFERRED.

## 0. Prerequisites

- **App state:** currently not running; SQL bug blocks execution; reCAPTCHA disabled.
- **Web URL:** `https://localhost:57065/swagger`.
- **API URL:** `https://localhost:57065`.
- **Playwright MCP:** use standard `mcp__playwright__browser_*` tools (`browser_navigate`, `browser_click`, `browser_fill_form`, `browser_wait_for`, `browser_snapshot`, `browser_network_requests`, etc.).
- **Seed users:**
  - `admin@yallajo.test` / `TestPass!23` — Administrator + Moderator.
  - `userA@yallajo.test` / `TestPass!23` — completed booking; verified reviewer on seeded listing.
  - `userB@yallajo.test` / `TestPass!23` — no booking; non-verified reviewer.
  - `guide-approved@yallajo.test` / `TestPass!23` — approved guide / creator/provider flows.
  - `business@yallajo.test` / `TestPass!23` — business/provider flows.
- **Seed content:**
  - Tour `T1`: 0 reviews; use to test no aggregate and create first review.
  - Tour `T2`: 4 reviews; average/summary should display because threshold is >=3.
  - Blog post `B1`: Published.
- **Resolved behavior for these scenarios:**
  - Review creation: **any authenticated user** may create one review per entity; completed booking controls verified badge only.
  - Rating scale: `1.0` through `5.0` in `0.5` increments.
  - Auto-hide: review/content becomes hidden on the **5th unique report**.
  - Aggregate display: no aggregate when review count is `<3`; aggregate visible when count is `>=3`.
  - Review edit window: 47h old review editable; 49h old review rejected.
- **General test setup helpers:**
  - Prefer UI routes if present under `YallaJo.Web/Areas/Social`, `Areas/Blog`, `Areas/Reviews`; otherwise use API calls through page context and assert UI-visible result when possible.
  - Use isolated test data names prefixed `pw-social-<timestamp>`.
  - For time-window tests, seed via API/DB fixture or test-only endpoint; do not depend on wall-clock waiting.
  - For 5-report auto-hide, use five distinct authenticated accounts or test-created users; duplicate user report must not count.
  - Capture network responses for create/edit/delete/report operations and assert expected status codes plus UI feedback.

## 1. Built — Active Scenarios

### Reviews & Ratings

#### S001 — Anonymous can view public approved reviews
- **Given** anonymous visitor opens tour `T2` detail or `/api/v1/social/reviews/Tour/{T2}` exposed through UI.
- **When** the reviews section loads.
- **Then** approved reviews are visible without login; create/edit/report controls prompt login.
- **MCP hints:** `browser_navigate(Web URL + tour detail)`, `browser_snapshot`, assert review cards and anonymous-safe actions.

#### S002 — Create review as authenticated user without completed booking
- **Given** `userB@yallajo.test` is logged in.
- **When** user submits review for `T1` with rating `4.5`, title, and content.
- **Then** review is created/published or queued per profanity rules; no verified badge is shown.
- **Assert:** creation is not blocked by missing booking; one-review-per-entity rule still applies.

#### S003 — Create review as verified reviewer
- **Given** `userA@yallajo.test` is logged in and has a completed booking for the listing.
- **When** user submits review for the eligible listing with rating `5.0`.
- **Then** review card displays a verified-booking badge/label.
- **Critical check:** `userA` badge visible; same flow with `userB` has no badge.

#### S004 — Reject anonymous review creation
- **Given** anonymous visitor opens review form from `T1`.
- **When** they attempt to submit.
- **Then** UI redirects to login or API returns `401`; no review appears after refresh.

#### S005 — Rating accepts 0.5 increments
- **Given** authenticated user opens review form.
- **When** selecting/submitting each valid value: `1.0`, `1.5`, `2.0`, `2.5`, `3.0`, `3.5`, `4.0`, `4.5`, `5.0` using isolated entities/users.
- **Then** each request succeeds and displayed rating matches submitted value.

#### S006 — Rating rejects invalid increments and bounds
- **Given** authenticated user uses API-backed form manipulation.
- **When** submitting `0.5`, `5.5`, `4.25`, blank rating, and non-numeric rating.
- **Then** validation errors appear; no review is created.

#### S007 — Review content max length 2000 chars
- **Given** authenticated user opens review form.
- **When** submitting exactly 2000 characters.
- **Then** review succeeds and content renders safely.
- **When** submitting 2001 characters.
- **Then** client/server validation rejects with a max-length error.

#### S008 — Review photos maximum 3 per review
- **Given** authenticated reviewer creates a review and uploads images through ContentCore attachment UI/API with `EntityType=Review`.
- **When** attaching 1, 2, then 3 accepted images (`jpg/png/webp`).
- **Then** all render in the review card/gallery.
- **When** adding a 4th image.
- **Then** upload is blocked or API returns validation error; first 3 remain intact.
- **Known divergence:** Social plan note mentions max 5 photos; this scenario follows task requirement max 3.

#### S009 — Reject unsupported review photo types
- **Given** a review exists.
- **When** uploading `.gif`, `.svg`, `.pdf`, or oversized file if a limit is exposed.
- **Then** UI/API rejects and review photo list is unchanged.

#### S010 — Edit own review within 47h
- **Given** `userA` owns a review seeded with `CreatedAt = now - 47h`.
- **When** user edits rating/content.
- **Then** update succeeds, changed values render, and cache/list refresh reflects the edit.

#### S011 — Reject edit after 49h
- **Given** `userA` owns a review seeded with `CreatedAt = now - 49h`.
- **When** user attempts edit.
- **Then** UI shows edit disabled or API returns validation/forbidden; original content remains.

#### S012 — Reject editing another user's review
- **Given** review belongs to `userA`.
- **When** `userB` attempts direct navigation/API edit.
- **Then** response is `403` or equivalent UI error; content unchanged.

#### S013 — Delete own review
- **Given** `userB` owns a review.
- **When** user deletes it and confirms.
- **Then** review is removed from public listing, counts/summary update after refresh, and owner no longer sees it in my reviews except optional deleted state.

#### S014 — Reject deleting another user's review
- **Given** `userA` owns a review.
- **When** `userB` attempts delete by direct URL/API.
- **Then** operation fails with `403/404`; review remains public.

#### S015 — Like/helpful vote review
- **Given** authenticated user views another user's review.
- **When** clicking helpful/like.
- **Then** count increments by 1 and selected state is shown.
- **When** page reloads.
- **Then** selected state persists.

#### S016 — Unlike/remove helpful vote
- **Given** authenticated user has marked review helpful.
- **When** clicking helpful again or remove-helpful action.
- **Then** count decrements and selected state clears.

#### S017 — Prevent duplicate helpful vote
- **Given** authenticated user already voted helpful.
- **When** duplicate POST is sent or user double-clicks.
- **Then** count remains incremented only once and duplicate request is idempotent or rejected gracefully.

#### S018 — Report review once per user
- **Given** `userB` views `userA` review.
- **When** user submits report reason.
- **Then** success message appears and review remains visible until threshold.
- **When** same user reports same review again.
- **Then** duplicate is blocked and report count does not increment.

#### S019 — Auto-hide review after 5 unique reports
- **Given** a visible review exists.
- **When** five different users report it with valid reasons.
- **Then** after reports 1-4 the review remains visible/flagged; after 5th report it becomes hidden/auto-hidden.
- **Assert:** public listing no longer shows content or shows moderated placeholder; admin queue shows auto-hidden state.

#### S020 — Aggregate rating hidden below 3 reviews
- **Given** `T1` has 0 reviews.
- **When** two users create two approved reviews.
- **Then** public tour/review summary does not show aggregate average/Bayesian score.

#### S021 — Aggregate rating visible at 3+ reviews
- **Given** tour has exactly 3 approved reviews, or seeded `T2` has 4.
- **When** visitor opens tour/review summary.
- **Then** aggregate average, count, and distribution display.

#### S022 — Weighted average reflects verified and non-verified weights
- **Given** review set contains verified reviewer rating `1.0` and non-verified reviewer rating `5.0` plus enough reviews to pass threshold.
- **When** summary recalculates or background service/test trigger updates cache.
- **Then** displayed weighted average gives verified review greater weight (`1.0`) than non-verified (`0.5`) and includes recency weighting.
- **Assert:** computed UI value is closer to the verified-heavy expected result than a plain arithmetic average.

#### S023 — Public review filters and sorting
- **Given** `T2` has mixed ratings, helpful counts, verified flags.
- **When** applying `sortBy=newest/highest/lowest/most_helpful`, `verifiedOnly=true`, and `minRating=4`.
- **Then** ordering/filtering matches query criteria and pagination remains stable.

#### S024 — Profanity moderation routes dirty review to moderation
- **Given** authenticated user submits review containing a seeded blocked word.
- **When** review is created.
- **Then** it is not publicly visible as approved; admin flagged/awaiting-moderation queue contains it.
- **Known redundancy:** profanity filter exists as cross-module duplicated concern; still test current behavior.

#### S025 — Provider official reply ownership
- **Given** review exists for provider-owned tour/business.
- **When** matching provider (`business` or `guide-approved`) adds an official reply.
- **Then** reply appears under review.
- **When** unrelated provider attempts reply.
- **Then** operation is forbidden.

#### S026 — Single provider reply constraint
- **Given** a review already has provider reply.
- **When** provider attempts adding second reply.
- **Then** request is rejected; update/delete reply actions remain available to owner/admin only.

### Blog / Content

#### S027 — Anonymous can view published blog B1
- **Given** anonymous visitor opens Blog area.
- **When** navigating to seeded blog `B1` by slug/detail.
- **Then** title, summary/content, tags/categories if present, and comments area render without login; mutation controls require auth.

#### S028 — Admin creates blog draft
- **Given** admin is logged in.
- **When** creating blog with title, slug, content, optional summary, category/tags.
- **Then** blog is saved as Draft and visible in admin/my blog list but not public listing.

#### S029 — Admin publishes draft blog
- **Given** admin-created draft exists.
- **When** admin uses Publish.
- **Then** status changes to Published, public detail/listing shows it, `BlogPostPublished`/`BlogPublishedIntegrationEvent` side effect is expected.

#### S030 — Admin archives published blog
- **Given** a published blog exists.
- **When** admin archives it.
- **Then** status changes to Archived and public listing/detail no longer exposes it except admin views.

#### S031 — Blog status invalid transitions blocked
- **Given** Archived/Removed/Hidden blog states are seeded.
- **When** attempting invalid transitions such as Published → Draft or Archived → Publish if not supported.
- **Then** UI/API returns business error and status remains unchanged.

#### S032 — Creator Tier-0 blog requires review
- **Given** approved creator at Tier-0 is logged in.
- **When** creator submits blog.
- **Then** status becomes PendingReview; not public until admin approves.

#### S033 — Creator Tier-1+ auto-publishes
- **Given** approved creator at Tier-1+ is logged in.
- **When** creator submits blog.
- **Then** status becomes Published without admin approval.

#### S034 — Creator blog excerpt validation
- **Given** creator-authored blog form.
- **When** summary/excerpt is under 100 chars or over 500 chars.
- **Then** validation fails.
- **When** summary is 100-500 chars.
- **Then** save/submit succeeds.

#### S035 — Blog comments nested max 2 levels
- **Given** authenticated user comments on published blog `B1`.
- **When** adding root comment, reply to root, and reply to reply.
- **Then** root and level-2 reply succeed; level-3 attempt is blocked or flattened per UI; no deeper nesting appears.

#### S036 — Blog comment moderation
- **Given** user posts a clean comment and another with blocked/profane content.
- **When** admin opens moderation queue.
- **Then** clean comment is public or approved; flagged comment awaits moderation and can be approved/rejected/hidden.

#### S037 — Like/react to blog post
- **Given** authenticated user opens published blog.
- **When** clicking like/reaction.
- **Then** reaction count increments and persists after reload.
- **When** toggling off.
- **Then** count decrements.

#### S038 — Blog tags/categories filter
- **Given** published blogs with tags/categories exist.
- **When** selecting a tag/category filter.
- **Then** list shows only matching published blogs and selected filter is visible in URL/UI.

#### S039 — Blog featuring uses new endpoint only
- **Given** admin is logged in and published blog exists.
- **When** using feature/unfeature UI.
- **Then** POST feature/unfeature route succeeds and featured state changes.
- **Known divergence:** old PATCH mark-as-featured/unfeatured routes were a cleanup target; tests should fail if UI still prefers old duplicate commands.

### Wishlist / Favorites

#### S040 — Login required for wishlist/favorites
- **Given** anonymous visitor views tour/place/blog/guide card.
- **When** clicking wishlist/favorite.
- **Then** login prompt or redirect occurs; no favorite is created.

#### S041 — Toggle tour in wishlist
- **Given** authenticated user views tour `T1`.
- **When** clicking wishlist/favorite.
- **Then** item is added and icon state changes.
- **When** clicking again.
- **Then** item is removed and icon state clears.

#### S042 — View wishlist list
- **Given** authenticated user has favorited tour/place/business/blog/guide items supported by current enum.
- **When** opening My Wishlist/Favorites.
- **Then** all active favorites appear with entity type labels and removed/deleted entities do not appear after cleanup.

#### S043 — Favorite idempotency and max limit behavior
- **Given** authenticated user favorites the same entity repeatedly or via double-click.
- **When** duplicate add/remove requests occur.
- **Then** final state is correct and no duplicate rows/cards appear.
- **When** user reaches max 500 favorites in fixture.
- **Then** next add is rejected with quota message.

### Reports / Moderation

#### S044 — Report content types
- **Given** authenticated user can access report action for tour, review, comment, blog, user/guide where UI exposes it.
- **When** submitting valid reason for each type.
- **Then** report is created, content is unchanged until moderation/threshold, and report queue receives correct target type.

#### S045 — Admin report queue
- **Given** admin logs in.
- **When** opening reports/moderation queue.
- **Then** open/under-review reports are listed with target type, reporter, reason, status, timestamps, and action buttons.

#### S046 — Report state machine Open → UnderReview → Resolved
- **Given** an open report exists.
- **When** admin starts review and resolves with action.
- **Then** report transitions to UnderReview then Resolved, moderation log row appears, and resolved report leaves active queue.

#### S047 — Dismiss report
- **Given** report exists but content is valid.
- **When** admin dismisses it with reason.
- **Then** report status is Dismissed/Resolved-no-action and target content remains public.

#### S048 — Admin remove/restore auto-hidden review
- **Given** review auto-hidden after 5 reports.
- **When** admin removes it.
- **Then** public listing hides it and audit log records admin action.
- **When** admin restores/approves it if UI supports restore.
- **Then** review becomes public again.

#### S049 — Warn user from moderation
- **Given** admin reviews report against a user/content author.
- **When** admin issues warning with reason.
- **Then** `UserModerationRecord`/moderation log is created and UI shows warning success.

#### S050 — Ban and unban user from moderation
- **Given** admin is logged in.
- **When** admin bans a user with reason.
- **Then** active ban record/log appears.
- **When** admin unbans/deletes ban.
- **Then** active ban clears and unban log appears.
- **Known gap:** Security-side global enforcement is deferred; do not assert every app request is blocked unless implemented.

### Follows

#### S051 — Follow/unfollow guide or creator profile
- **Given** authenticated user opens guide/creator public profile.
- **When** clicking Follow.
- **Then** follower count increments and following state persists.
- **When** clicking Unfollow.
- **Then** follower count decrements and state clears.

#### S052 — Followers/following lists
- **Given** a guide/creator has followers.
- **When** opening followers/following list endpoints/pages.
- **Then** paginated list displays expected users/profiles and respects auth/privacy rules.

## 2. NOT_BUILT

1. **NB001 — Discount on wishlisted item → notification (max 3/day).** Deferred/post-MVP in Social workflow; requires Finance `DiscountCreated` and Messaging notification pipeline.
2. **NB002 — Security-side global ban enforcement.** Social records warn/ban/unban, but strike escalation and Security ban middleware integration are deferred.
3. **NB003 — Strike escalation.** 1st warning, 2nd 7-day ban, 3rd 30-day ban, 4th permanent/SuperAdmin-only is planned but deferred in W3-B notes.
4. **NB004 — Active-ban and moderation-history query pages.** Workflow lists endpoints, but W3-B notes mark active-ban/history queries as deferred.
5. **NB005 — Outbound Social → Security warn/ban integration events.** Planned events are deferred per W3-B notes.
6. **NB006 — Public ReviewHelpfulVote query endpoint.** Helpful add/remove is wired; explicit `GET /reviews/{reviewId}/helpful` was a fix-plan item and may remain absent.

## 3. DEFERRED

1. **D001 — AI moderation / NSFW classifier real implementation.** Current workflow marks ML moderation and real classifier as post-MVP/no-op.
2. **D002 — Review gamification.** Badges for frequent reviewers are post-MVP.
3. **D003 — Community Q&A.** Separate entity questions are deferred.
4. **D004 — Generic shared UGC/profanity abstraction.** Cross-document report flags redundancy; not required for current acceptance.

## 4. Integration Events

### Publishes / expected side effects
- `ReviewCreated` / actual contracts may expose `ReviewPublishedIntegrationEvent` and `ReviewAggregateUpdatedIntegrationEvent`.
- `ReviewDeletedIntegrationEvent`.
- `ReviewReported` / actual contracts include `ReportSubmittedIntegrationEvent` and `ReportResolvedIntegrationEvent`.
- `WishlistItemAdded` / actual contracts include `FavoriteAddedIntegrationEvent`.
- `BlogPostPublished` / actual contracts include `BlogPublishedIntegrationEvent`.
- `RatingRecalculatedIntegrationEvent` after aggregate/rating cache recalculation.

### Consumes / expected inbound triggers
- `BookingCompleted` → creates/updates `BookingEligibilitySnapshot` for verified review badge.
- `DiscountCreated` → notify wishlisters: **not built/deferred**.
- Blog deletion / guide deactivation cleanup → favorite orphan cleanup where implemented.

### Event test scenarios
- **IE001:** After review create/delete, refresh public summary and assert review count/average eventually changes.
- **IE002:** After 5th report, assert report queue and public listing reflect hidden state.
- **IE003:** After adding favorite, assert wishlist list and favorite icon update; if outbox inspection is available, assert favorite event row exists.
- **IE004:** After publishing blog, assert public listing shows post and downstream SEO/notification observable UI is not broken.

## 5. Validation Matrix

| Feature | Field/rule | Valid | Invalid / expected rejection |
|---|---|---|---|
| Review rating | 1.0-5.0, 0.5 increments | 1.0, 1.5, 5.0 | 0.5, 5.5, 4.25, blank |
| Review content | Max 2000 chars | 2000 chars | 2001 chars, unsafe script rendered as HTML |
| Review title | Required if UI requires it; max per current form | Normal text | Empty/over max |
| Review photos | Max 3; image/jpeg/png/webp | 1-3 images | 4th image, invalid type |
| Review duplicate | One review per entity per user | First review | Second review by same user/entity |
| Review edit | 48h window | 47h | 49h |
| Report reason | Required; max length | Meaningful reason | Empty, over max, duplicate same user/content |
| Helpful vote | One per review/user | First vote | Duplicate vote changes count >1 |
| Favorite | Max 500/user; unique entity | First add | Duplicate add, 501st favorite |
| Blog summary/excerpt | Creator blogs 100-500 chars | 100-500 | <100 or >500 for creator blog |
| Blog status | Draft→Published→Archived; creator PendingReview as applicable | Allowed transitions | Invalid back transitions |
| Comments | Max nesting 2 levels | Root + one reply | Reply-to-reply beyond level 2 |

## 6. Auth Matrix

| Capability | Anonymous | Authenticated user | Owner | Provider/Guide | Moderator/Admin |
|---|---:|---:|---:|---:|---:|
| View public reviews/rating summary | Yes | Yes | Yes | Yes | Yes |
| View published blogs | Yes | Yes | Yes | Yes | Yes |
| Create review | No | Yes | Yes | Yes | Yes |
| Verified review badge | No | Only if completed booking | Only if completed booking | N/A | N/A |
| Edit/delete review | No | No | Yes within rules | No unless owner | Admin remove/restore only |
| Helpful vote/like review | No | Yes | Yes except own if restricted by UI | Yes | Yes |
| Report content | No | Yes | Yes | Yes | Yes |
| Add provider reply | No | No | No | Yes for owned entity | Yes |
| Wishlist/favorite | No | Yes | Yes | Yes | Yes |
| Create/admin blog | No | No | Creator/admin only | Creator/provider as authorized | Yes |
| Comment on blog | No | Yes | Yes | Yes | Yes |
| Moderate reports/comments/reviews | No | No | No | No | Yes |
| Warn/ban user | No | No | No | No | Yes |
| Follow guide/creator | No | Yes | Yes | Yes | Yes |

## 7. State Machines

### Blog post
```text
Draft → Published → Archived
Draft → PendingReview → Published
PendingReview → Rejected
Published → Hidden → Published
Published → Removed
Any allowed soft-delete path → Deleted/Removed per UI/API
```

### Report
```text
Open → UnderReview → Resolved
Open → UnderReview → Dismissed
Open/UnderReview → Resolved with action: Hide/Remove content, WarnUser, BanUser
```

### Review
```text
Create → Published
Create dirty/profane → AwaitingModeration → Approved/Published
Published → AutoHidden after 5 unique reports
Published → RemovedByAdmin
Published → DeletedByUser
Published → Edited (only within 48h)
```

### Favorite / Wishlist
```text
Absent → Added/Active → Removed/SoftDeleted → Re-added/Active
```

## 8. Known Divergence

1. **Review eligibility:** Some older plan text says booking-verified reviews only. These scenarios follow the task-level resolved rule: anyone authenticated can review; booking controls verified badge.
2. **Report threshold:** Some cross-doc text says 3 reports; Social notes and task require 5 unique reports for auto-hide.
3. **Review photo max:** Social plan mentions max 5 via ContentCore; task requires max 3 per review.
4. **Social routes:** Implementation notes say routes live under `/api/v1/social/*`; older plan tables omit the prefix.
5. **Blog routes:** Merger audit reports actual routes differ from plan (`/blogs/creators/profile/mine/*`, `/blogs/admin/creators/profiles/{id}`, same `POST /blogs` for admin/creator) — tests should discover/use actual UI links where possible.
6. **Blog featuring:** Duplicate old PATCH mark-as-featured endpoints were cleanup debt; test should prefer new POST feature/unfeature flow.
7. **Warn/ban:** Social-side records/endpoints exist; strike escalation, Security integration events, active-ban/history queries, and global enforcement are deferred.
8. **App execution blocker:** app is not running and SQL bug exists; this document is scenario design, not executed results.
