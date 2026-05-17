# Social — Critical Rules (S-R1..S-R10)

> Additive to `Phase1-Phase2-Completion-INDEX.md` §4 (16 universal rules). Every PR in this sprint that violates one of these is blocked at review.

---

## S-R1 — Verified-Booking Gate (Reviews)

Per **PDF 2 §1.6**: "ANYONE can leave review (no booking required), VERIFIED badge for completed bookings, REVIEW WINDOW 30 DAYS from completion."

**Implementation:** `POST /reviews` does NOT reject non-buyers. Instead:

1. Look up `IBookingEligibilitySnapshotRepository.UserHasCompletedBookingAsync(userId, targetType, targetId, ct)`.
2. If true AND `now - LastCompletedAt <= 30 days` → review.IsVerifiedBooking = true (badge shown on frontend).
3. If false OR > 30 days → review.IsVerifiedBooking = false (no badge, but submission allowed).

**Why no hard reject:** PDF 2 §1.6 explicitly allows non-verified reviews. They get weighted 0.5 in the Bayesian score (S-R6).

**Edge case:** review window starts from `LastCompletedAt`, not `FirstCompletedAt`. A user who books the same tour twice has the window reset on each completion.

---

## S-R2 — One Review Per User Per Target

Hard uniqueness. `IReviewRepository.UserHasReviewedAsync(userId, targetType, targetId, ct)` checks BEFORE create. Database also enforces:

```sql
CREATE UNIQUE INDEX UX_Reviews_UserTarget
  ON social.Reviews(UserId, TargetType, TargetId)
  WHERE IsDeleted = 0;
```

`IsDeleted = 0` clause means a soft-deleted review allows the user to submit a fresh one (PDF 2 §1.6 edge case: "deleted review allows new submission — soft deletes don't count").

Race condition: two concurrent POSTs from same user → second hits `DbUpdateException` from the unique index → catch in Infrastructure only (INDEX §4 R12) → return `Error("Review.AlreadyExists", "...") Outcome.Conflict`.

---

## S-R3 — 48-Hour Edit Window

`PUT /reviews/{id}` rejects with `Error("Review.EditWindowExpired", "Reviews can only be edited within 48 hours of posting.") Outcome.UnprocessableEntity` when `now - CreatedAt > TimeSpan.FromHours(48)`.

**Boundary handling:** `<= 48` accepted, `> 48` rejected. Use `>` not `>=`. Test cases include 47h59m59s (accept), 48h00m00s (accept, last-millisecond), 48h00m00.001s (reject).

**Window is based on `CreatedAt` NOT `UpdatedAt`**. Multiple edits within 48h all allowed, but the 49th-hour edit is rejected even if previous edit was at 47h.

Soft-deletion is NOT bound by 48h — user can delete own review anytime.

Provider replies (`PUT /reviews/{id}/reply/{replyId}`) are UNLIMITED edit window per PDF 2 §1.6 — no time check.

---

## S-R4 — Profanity Filter Lifecycle

All user-generated text (review content, review title, review reply content, report description, ticket message) goes through `IProfanityFilter.ContainsProfanityAsync` BEFORE publication.

**Behavior on profanity hit:**
- Reviews: `Status = AwaitingModeration`, NOT visible publicly. Admin appears in moderation queue.
- Review replies: same.
- Reports: never auto-flagged for profanity — they're admin-only by design.

**Editing a published review triggers a re-scan.** If the edit introduces profanity, status transitions back to `AwaitingModeration` and `ReviewEditedDomainEvent` payload includes `ProfanityFlagged = true` for downstream auditing.

**Profanity scan is `Task.Run`'d off the request thread for latency-sensitive endpoints** — NO. The scan IS in-request because BlocklistProfanityFilter is sub-millisecond. Only when we swap to a real ML classifier (Phase 4) do we move scanning to a background channel + delayed publish.

---

## S-R5 — 5-Report Auto-Hide (Reviews only)

PDF 2 §1.6: "5 unique reports → auto-hide+flag admin".

**Implementation:** `POST /reviews/{id}/report` handler:
1. Validate report (PW-6 rules + reason enum).
2. Insert Report row.
3. Re-count active reports for the review via `IReportRepository.CountActiveReportsAsync(EntityType.Review, reviewId, ct)`.
4. If count >= 5: load Review aggregate, call `review.AutoHide(reportCount)` which transitions Status → `AutoHidden` AND raises `ReviewAutoHiddenDomainEvent` (PW-3). Idempotent — if already AutoHidden, no-op.
5. SaveChanges.

Auto-hidden reviews are invisible to public, NOT counted in `RatingRecalculationService`, but ARE visible to admin queue (`GET /reviews/admin/flagged`).

Admin actions:
- `POST /reviews/admin/{id}/approve` — restore via `review.Restore(adminUserId)` (raises `ReviewRestoredDomainEvent`). Resets report count? **No** — reports stay, but auto-hide threshold becomes inactive until ANOTHER 5 new reports arrive.
- `POST /reviews/admin/{id}/remove` — permanent removal via `review.AdminDelete(adminUserId)` (raises `ReviewDeletedDomainEvent` with `Source = Admin`). Tombstone.

**Uniqueness of reports:** `IReportRepository.UserAlreadyReportedAsync` prevents a user from reporting same review twice. Database unique index `UX_Reports_UserEntity (UserId, EntityType, EntityId) WHERE Status = Open` enforces.

---

## S-R6 — Bayesian Rating Algorithm

Per **PDF 2 §18**:

```
weight_i = VerificationWeight × RecencyWeight

VerificationWeight:
  Verified booking → 1.0
  Non-verified     → 0.5

RecencyWeight:
  < 90 days   → 1.0
  90-180 days → 0.7
  > 180 days  → 0.5

AverageRating = Σ(rating_i × weight_i) / Σ(weight_i)

BayesianScore = (ReviewCount × AvgRating + C × GlobalAverage) / (ReviewCount + C)
  where C = 10 (confidence threshold, admin-configurable)
  GlobalAverage = avg of all tour ratings across platform (refreshed daily by RatingRecalculationService)
```

**Display rules:**
- `MIN 3 REVIEWS` before AverageRating is shown publicly (PDF 2 §1.6 + §18). `EntityRatingCache.ReviewCount < 3` → API omits AverageRating field, frontend shows "Not enough reviews yet".
- Bands per PDF 2 §18: 4.5-5.0 Top Rated badge, 4.0-4.49 Popular section, 3.0-3.99 standard, 2.0-2.99 deprioritized, <2.0 admin flagged after 90 days.

**Computation:** `RatingRecalculationService` (T5) runs daily at 03:00 UTC, scans every Tour/Place/Business with ≥ 1 non-deleted review created/updated in last 24h OR not yet scored, recomputes both `AverageRating` and `BayesianScore`, persists to `EntityRatingCache`, raises `EntityRatingRecalculatedDomainEvent` → outbox `social.rating.recalculated.v1` → consumed by ContentTours / ContentPlaces inbox handlers.

**Why store BOTH AverageRating and BayesianScore:** ratings (visible to user) and ranking signal (used by search / discovery) are different. Search uses BayesianScore so brand-new entities don't dominate.

---

## S-R7 — Favorites Limit + Toggle Semantics

PDF 2 §1.7: "MAX 500 per user, toggle once (re-click removes)".

**POST /favorites** is idempotent toggle ONLY in the sense that it gracefully handles "already favorited":
- Already favorited → `Error("Favorite.AlreadyExists", ...) Outcome.Conflict` (409). Frontend should call DELETE instead.
- Over 500 limit → `Error("Favorite.LimitExceeded", "Maximum 500 favorites allowed.") Outcome.UnprocessableEntity` (422).
- Otherwise create Favorite + raise `FavoriteAddedDomainEvent` + outbox `social.favorite.added.v1`.

Limit check is racy — two parallel POSTs from same user at count=499 can both pass the count check. Acceptable: PDF 2 §1.7 says "no max limit" originally but PDF 1 Wave 6 specifies 500. We enforce best-effort.

**`DELETE /favorites/{entityType}/{entityId}`** uses entity reference (not Favorite ID) for frontend simplicity. Handler looks up via `IFavoriteRepository.FindForUserAsync` and `favorite.Remove()` raises `FavoriteRemovedDomainEvent`. Idempotent: if not found, return 204 anyway.

**No FavoriteRemovedIntegrationEvent in this sprint** — Messaging only needs additions (for discount notifications). Future: add `social.favorite.removed.v1` if needed.

---

## S-R8 — ICurrentUser Discipline

Per INDEX §4 R2 ("ICurrentUser ONLY for self/ownership comparisons"). Audit list for Social handlers:

| Handler | Why ICurrentUser is needed |
|---|---|
| CreateReviewCommandHandler | Stamps UserId on new Review |
| UpdateReviewCommandHandler | Compares review.UserId to currentUser.UserId (ownership) |
| DeleteReviewCommandHandler | Same |
| AddReviewReplyCommandHandler | Verifies provider owns the target tour (via BookingProviderSnapshot lookup OR ContentTours snapshot — TBD in T1) |
| UpdateReviewReplyCommandHandler | Owner-only |
| DeleteReviewReplyCommandHandler | Owner-only |
| AddFavoriteCommandHandler | Stamps UserId |
| RemoveFavoriteCommandHandler | Self-only |
| GetMyReviewsQueryHandler | Self-filter |
| GetMyFavoritesQueryHandler | Self-filter |
| CheckFavoriteQueryHandler | Self-only |
| SubmitReportCommandHandler | Stamps ReporterUserId |

All OTHER handlers (admin queries, RatingRecalculationService, etc.) should NOT inject `ICurrentUser`. Admin endpoints are protected by `MustHavePermission(AdminModerationQueue.Approve)` and the admin user identity comes from `HttpContext.User` in the endpoint, passed explicitly as `adminUserId` into the command.

---

## S-R9 — Cursor Pagination + Cache Strategy

Same cursor envelope as Booking/Finance: `{ items, nextCursor (base64 opaque), totalCount? }`. PageSize clamped [1, 50], default 20.

**Cache tags (per INDEX §4 R10 — most specific):**

| Endpoint | Cache key | Tag(s) | TTL |
|---|---|---|---|
| GET /favorites | `favorites:user:{userId}:filter:{entityTypeOrAll}:cursor:{cursor}:take:{n}` | `favorites:user:{userId}` | 1 min |
| GET /favorites/check/{type}/{id} | `favorite-check:user:{userId}:type:{type}:id:{id}` | `favorites:user:{userId}` | 30 sec |
| GET /my-reviews | `reviews:user:{userId}:cursor:{cursor}:take:{n}` | `reviews:user:{userId}` | 1 min |
| GET /reviews/admin/flagged | `reviews:admin:flagged:cursor:{cursor}:take:{n}` | `reviews:admin` | 30 sec |
| GET /reports/admin | `reports:admin:filter:{hash}:cursor:{cursor}:take:{n}` | `reports:admin` | 30 sec |
| GET /moderation/logs | `moderation-logs:admin:filter:{hash}:cursor:{cursor}:take:{n}` | `moderation-logs` | 1 min |

**Invalidation rules** (every command MUST call `RemoveByTagAsync(tag, ct)` AFTER `SaveChangesAsync`):

| Command | Tags evicted |
|---|---|
| Create/Update/Delete Review | `reviews:user:{userId}`, `reviews:admin`, `entity-rating:{targetType}:{targetId}` |
| Auto-hide review | `reviews:admin`, `entity-rating:{type}:{id}` |
| Add/Remove Favorite | `favorites:user:{userId}` |
| Submit Report | `reports:admin`, `moderation-logs` |
| Resolve Report | `reports:admin`, `moderation-logs`, `reviews:admin` |

---

## S-R10 — Error Code Registry

Domain errors use `{Entity}.{Reason}` PascalCase (INDEX §4 R13). Outcome mapping:

```
Review.NotFound                   → NotFound (404)
Review.AlreadyExists              → Conflict (409)
Review.OwnerMismatch              → Forbidden (403)
Review.EditWindowExpired          → UnprocessableEntity (422)
Review.InvalidRating              → ValidationError (400)
Review.ContentTooLong             → ValidationError (400)
Review.ContentTooShort            → ValidationError (400)
Review.AutoHidden                 → Forbidden (403) (cannot edit auto-hidden)
Review.ProfanityDetected          → UnprocessableEntity (422) — but the review IS submitted (Status=AwaitingModeration), this code is returned for transparency
ReviewReply.NotFound              → NotFound (404)
ReviewReply.OwnerMismatch         → Forbidden (403)
ReviewReply.NotOwnerOfTargetEntity → Forbidden (403) (provider tried to reply to review on a tour they don't own)
ReviewReply.ContentTooLong        → ValidationError (400)

Favorite.AlreadyExists            → Conflict (409)
Favorite.NotFound                 → NotFound (404)
Favorite.LimitExceeded            → UnprocessableEntity (422)
Favorite.InvalidEntityType        → ValidationError (400)
Favorite.OwnerMismatch            → Forbidden (403)

Report.NotFound                   → NotFound (404)
Report.AlreadyReported            → Conflict (409)
Report.InvalidReason              → ValidationError (400)
Report.DescriptionTooLong         → ValidationError (400)
Report.TargetNotReportable        → ValidationError (400)
Report.AlreadyResolved            → Conflict (409)

Moderation.ActionNotAllowed       → UnprocessableEntity (422) (e.g. trying to Ban for a Spam report — policy)
Moderation.ReviewNotAutoHidden    → UnprocessableEntity (422) (approve called on a non-hidden review)
Moderation.InvalidAction          → ValidationError (400)

RatingRecalc.NoReviewsFound       → NotFound (404) — only used by manual admin trigger

BookingEligibility.NotFound       → NotFound (404) — internal, never exposed to user; verified flag just becomes false
```

Total ≈ 27 codes. Final list locked at T1 PR review.
