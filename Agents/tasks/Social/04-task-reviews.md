# TASK 1 — Reviews

> **Owner:** Mahmoud (Intermediate) — **Hours:** 38h — **Hard deadline:** Sun **2026-11-08 17:00**
> **Earliest start:** Wed 2026-10-21 (after PW-2 migration applied)
> **Endpoints:** 8 — **Depends on:** PW-1..PW-7, BookingEligibilitySnapshot inbox (PW-5)

---

## 1. Endpoint List

| # | Method | Path | Permission | Notes |
|---|---|---|---|---|
| 1 | POST | `/api/v1/reviews` | `Review.Create` | Body: `{targetType, targetId, rating, title?, content}`. Returns 201 + Review DTO with `isVerifiedBooking`. |
| 2 | PUT | `/api/v1/reviews/{id}` | `Review.Update` (self-only) | Edit within 48h (S-R3). Rating + title + content. |
| 3 | DELETE | `/api/v1/reviews/{id}` | `Review.Delete` (self-only) | Soft-delete. Source = User. |
| 4 | POST | `/api/v1/reviews/{id}/reply` | `ReviewReply.Create` | Provider only — must own target entity (verified via TourSnapshot/PlaceSnapshot.ProviderId). |
| 5 | PUT | `/api/v1/reviews/{id}/reply/{replyId}` | `ReviewReply.Update` (self-only) | Unlimited edit window (S-R3). |
| 6 | DELETE | `/api/v1/reviews/{id}/reply/{replyId}` | `ReviewReply.Delete` (self-only) | Soft-delete. |
| 7 | GET | `/api/v1/reviews/my-reviews` | `Review.Read` (self-filter) | Cursor pagination; includes deleted=false; includes Status field. |
| 8 | GET | `/api/v1/reviews/admin/flagged` | `AdminModerationQueue.Read` | Auto-hidden reviews queue. Filter by entityType + dateRange. |

**Public GET endpoints (list reviews on a tour, see all replies, etc.)** are owned by ContentTours / ContentPlaces — they read denormalized `Review` data via integration events. NOT this sprint's scope.

---

## 2. Review Aggregate (Mahmoud builds)

```csharp
public sealed class Review : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public ReviewTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public ReviewRating Rating { get; private set; }           // VO
    public string? Title { get; private set; }                  // max 200
    public string Content { get; private set; }                 // max 2000
    public ReviewStatus Status { get; private set; }
    public bool IsVerifiedBooking { get; private set; }
    public bool ProfanityFlagged { get; private set; }
    public DateTime? AutoHiddenAt { get; private set; }
    public int CurrentReportCount { get; private set; }
    public DateTime? LastEditedAt { get; private set; }

    private readonly List<ReviewReply> _replies = new();
    public IReadOnlyList<ReviewReply> Replies => _replies;

    public static Review Create(...)               // factory; raises ReviewPublishedDomainEvent OR none if profanity → AwaitingModeration
    public Result Edit(...)                        // 48h window check; profanity re-scan; raises ReviewEditedDomainEvent
    public Result Delete(ReviewDeletionSource src) // soft delete; raises ReviewDeletedDomainEvent
    public Result AutoHide(int reportCount)        // idempotent; raises ReviewAutoHiddenDomainEvent
    public Result Restore(Guid adminUserId)        // raises ReviewRestoredDomainEvent
    public Result AddReply(Guid providerUserId, string content)   // creates ReviewReply child; raises ReviewReplyAddedDomainEvent
    public Result UpdateReply(Guid replyId, Guid providerUserId, string content)
    public Result DeleteReply(Guid replyId, Guid providerUserId)
}

public sealed class ReviewReply : BaseEntity
{
    public Guid ReviewId { get; private set; }
    public Guid ProviderUserId { get; private set; }
    public string Content { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastEditedAt { get; private set; }
    // ... factories
}
```

**Profanity flow in Create / Edit:**
1. Call `await _profanityFilter.ContainsProfanityAsync(content, languageCode, ct)`.
2. If true: set `Status = AwaitingModeration`, `ProfanityFlagged = true`, DON'T raise ReviewPublishedDomainEvent (frontend sees Status = AwaitingModeration in response, knows it's pending). Add to admin queue via `entity.Status` filter.
3. Else: Status = Published, raise ReviewPublishedDomainEvent.

---

## 3. Verified-Booking Check (S-R1)

In `CreateReviewCommandHandler`:
```csharp
var hasCompletedBooking = await _eligibilityRepo.UserHasCompletedBookingAsync(
    currentUser.UserId, command.TargetType, command.TargetId, ct);

var isVerified = hasCompletedBooking;  // 30-day check is inside snapshot lookup
var review = Review.Create(
    userId: currentUser.UserId,
    targetType: command.TargetType,
    targetId: command.TargetId,
    rating: ratingResult.Value,
    title: command.Title,
    content: command.Content,
    isVerifiedBooking: isVerified,
    profanityFilter: _profanityFilter,
    languageCode: command.LanguageCode ?? "en",
    now: _timeProvider.GetUtcNow().UtcDateTime,
    ct: ct);

if (review.IsFailure) return review.ToError();
_repo.Add(review.Value);
await _unitOfWork.SaveChangesAsync(ct);
await _cache.RemoveByTagAsync($"reviews:user:{currentUser.UserId}", ct);
await _cache.RemoveByTagAsync($"entity-rating:{command.TargetType}:{command.TargetId}", ct);
```

---

## 4. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Review + ReviewReply + ReviewRating VO + ReviewStatus enum + factories | 5 | 2026-10-24 |
| 2 | EF configurations + migration `SocialAddReviewProvenanceColumns` + `SocialAddReviewReplyTable` | 4 | 2026-10-26 |
| 3 | CreateReviewCommand + Validator + Handler + tests | 5 | 2026-10-28 |
| 4 | UpdateReviewCommand + 48h gate + Validator + Handler + tests | 4 | 2026-10-30 |
| 5 | DeleteReviewCommand + Handler + tests | 2 | 2026-10-30 |
| 6 | AddReviewReplyCommand + provider ownership check + Validator + Handler + tests | 4 | 2026-11-01 |
| 7 | UpdateReviewReplyCommand + DeleteReviewReplyCommand + Handlers + tests | 3 | 2026-11-02 |
| 8 | GetMyReviewsQuery (cursor) + Handler + ICacheableQuery + tests | 3 | 2026-11-04 |
| 9 | GetAdminFlaggedReviewsQuery + Handler + tests | 3 | 2026-11-05 |
| 10 | 8 endpoint wiring + Swagger XML docs | 2 | 2026-11-06 |
| 11 | Integration test: end-to-end booking-complete → review-publish → outbox round-trip | 2 | 2026-11-07 |
| 12 | PR review fixes | 1 | 2026-11-08 |
| **Total** | | **38h** | **Sun 2026-11-08** |

---

## 5. Acceptance Tests (12 cases)

1. POST review with completed booking → IsVerifiedBooking = true, Status = Published, outbox `social.review.published.v1`.
2. POST review without booking → IsVerifiedBooking = false, Status = Published.
3. POST review with profanity → Status = AwaitingModeration, ProfanityFlagged = true, NO outbox row.
4. POST second review same tour same user → 409 `Review.AlreadyExists`.
5. PUT review at 47h59m → success, updates Rating + raises ReviewEditedDomainEvent.
6. PUT review at 48h00m01s → 422 `Review.EditWindowExpired`.
7. PUT review introducing profanity → Status reverts to AwaitingModeration.
8. DELETE own review → IsDeleted=true, can submit new review on same target.
9. DELETE other user's review → 403 `Review.OwnerMismatch`.
10. POST reply by tour-owning provider → success, ReviewReplyAddedDomainEvent raised.
11. POST reply by non-owning provider → 403 `ReviewReply.NotOwnerOfTargetEntity`.
12. GET admin/flagged returns only auto-hidden + AwaitingModeration reviews, sorted by AutoHiddenAt DESC.
