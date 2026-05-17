# Social — Entity Ownership Matrix

> Status legend: 🟢 in-scope aggregate, 🔵 owned/child entity (BaseEntity), 🟡 deferred to a future sprint, ⚪ external read-snapshot populated by inbox.

---

## 1. Aggregates In Scope (PW-2)

| File (existing) | Base class | Aggregate? | Domain events raised | Owner | Task |
|---|---|---|---|---|---|
| `Social.Domain/Entities/Review.cs` | `AuditableEntity, IAggregateRoot` | 🟢 yes | ReviewPublished, ReviewEdited, ReviewDeleted, ReviewAutoHidden, ReviewRestored, ReviewReplyAdded | Mahmoud | T1 |
| `Social.Domain/Entities/Favorite.cs` | `AuditableEntity, IAggregateRoot` | 🟢 yes | FavoriteAdded, FavoriteRemoved | Fadwa | T2 |
| `Social.Domain/Entities/Report.cs` | `AuditableEntity, IAggregateRoot` | 🟢 yes | ReportSubmitted, ReportResolved, EntityAutoActioned (raised when 5-report threshold hits) | Mohammad | T4 |

## 2. Child / Junction / Audit Entities (stay BaseEntity)

| File | Base class | Reason | Owner |
|---|---|---|---|
| `Social.Domain/Entities/ReviewReply.cs` (NEW) | BaseEntity | Owned by Review aggregate; replies don't have independent lifecycle | Mahmoud (T1) |
| `Social.Domain/Entities/ContentModerationLog.cs` | BaseEntity | Append-only audit row; no business invariants | Mohammad (T4) |
| `Social.Domain/Entities/ProfanityBlocklistEntry.cs` (NEW) | BaseEntity | Reference data (admin-managed); part of Moderation domain | Tech Lead (PW-6) |
| `Social.Domain/Entities/BookingEligibilitySnapshot.cs` (NEW) | BaseEntity | Read-snapshot ingested from booking inbox | Tech Lead (PW-5) |
| `Social.Domain/Entities/EntityRatingCache.cs` (NEW) | BaseEntity | Denormalized per-entity rating snapshot written by RatingRecalculationService | Mohammad (T5) |

## 3. Deferred (stub, do not implement this sprint)

| File | Reason |
|---|---|
| `Social.Domain/Entities/AccessibilityReview.cs` | Phase 3 feature 14. Annotate with `[Obsolete("Deferred to Phase 3 — see YallaJo.md §Wave 6 backlog")]` to prevent accidental usage. |

## 4. External Read-Snapshots Consumed (no Social ownership)

| Snapshot | Source integration event | Used by |
|---|---|---|
| `social.BookingEligibilitySnapshots` | `booking.tour-booking.completed.v1` | T1 verified-booking gate |
| `social.PlaceSnapshots` (PK = PlaceId) | `content-places.place.created.v1`, `.updated.v1`, `.deleted.v1` | T3 OrphanedFavoritesCleanupService + Favorite entity-type validation |
| `social.BusinessSnapshots` (PK = BusinessId) | `content-places.business.created.v1`, `.updated.v1`, `.deleted.v1` | Same |
| `social.TourSnapshots` (PK = TourId) | `content-tours.tour.published.v1`, `.updated.v1`, `.deleted.v1` | Same + provider lookup for reply ownership check |

---

## 5. Cross-Module Dependencies

| Module | Depends on | Reason |
|---|---|---|
| Social → Booking | `booking.tour-booking.completed.v1` integration event consumed | Populates BookingEligibilitySnapshot for verified-booking gate (S-R1) |
| Social → ContentPlaces | 6 integration events (place + business CRUD) | Maintains PlaceSnapshots + BusinessSnapshots; validates Favorite + Report target entity exists |
| Social → ContentTours | 4 integration events (tour CRUD) | Maintains TourSnapshots; resolves provider ownership for review replies |
| ContentTours → Social | `social.rating.recalculated.v1` integration event consumed | Updates Tour.AverageRating + Tour.ReviewCount denormalized columns |
| ContentPlaces → Social | `social.rating.recalculated.v1` integration event consumed | Updates Place.AverageRating + Business.AverageRating |
| Messaging → Social | `social.favorite.added.v1` integration event consumed | Subscribes user to discount notifications on the entity (PDF 2 §1.7) |
| Messaging → Social | `social.review.published.v1` integration event consumed | Sends "New review on your tour" notification to provider |
| Messaging → Social | `social.report.resolved.v1` integration event consumed | Notifies reporter of outcome |
| Analytics → Social | 3 integration events consumed | UserInteraction logging |

---

## 6. Value Objects (new in this sprint)

| File | Purpose | Validation |
|---|---|---|
| `Social.Domain/ValueObjects/ReviewRating.cs` | Wraps decimal rating | Must be in [1.0, 5.0] in 0.5 increments (1.0, 1.5, 2.0, ..., 5.0). Static factory `Create(decimal value)` returns Result. |
| `Social.Domain/ValueObjects/ReviewCursor.cs` | Cursor for review list pagination | Base64 of `{Id, CreatedAt}` |
| `Social.Domain/ValueObjects/FavoriteCursor.cs` | Cursor for favorite list pagination | Base64 of `{Id, AddedAt}` |
| `Social.Domain/ValueObjects/ReportCursor.cs` | Cursor for report admin list | Base64 of `{Id, SubmittedAt}` |
| `Social.Domain/ValueObjects/ModerationLogCursor.cs` | Cursor for moderation log list | Base64 of `{Id, ActionedAt}` |

`ReviewRating` is the only meaningful one. The cursors are uniform record types `(Guid Id, DateTime Timestamp)`.

---

## 7. Enums (new + extended)

```csharp
// NEW
public enum ReviewDeletionSource { User = 0, Admin = 1, System = 2 }
public enum FavoriteEntityType { Tour = 0, Place = 1, Business = 2 }
public enum ReportableEntityType { Review = 0, Tour = 1, Place = 2, Business = 3, Blog = 4 }
public enum ReviewStatus { Published = 0, AwaitingModeration = 1, AutoHidden = 2, RemovedByAdmin = 3, DeletedByUser = 4 }

// EXTENDED (verify presence in current code; add if missing)
public enum ReviewTargetType { Tour = 0, Place = 1, Business = 2, TourGuide = 3 }   // Existing — verify
public enum ReportReason { Spam = 0, Inappropriate = 1, Misleading = 2, Harassment = 3, FakeReview = 4, Other = 5 }   // Per PDF 2 §1.6
public enum ReportStatus { Open = 0, UnderReview = 1, Resolved = 2, Dismissed = 3 }
public enum ModerationAction { Dismiss = 0, RemoveContent = 1, WarnUser = 2, BanUser = 3, RestoreContent = 4 }
```

---

## 8. EF Configurations (12 files in `Social.Infrastructure/Persistence/Configurations/`)

1. `ReviewConfiguration.cs` — TPH not needed (only one Review entity); table `social.Reviews`; `Rating decimal(3,2)`; unique index `(UserId, TargetType, TargetId) WHERE IsDeleted=0`; OwnsMany ReviewReply collection (table `social.ReviewReplies`); RowVersion required.
2. `ReviewReplyConfiguration.cs` — owned via Review aggregate; FK `ReviewId`; cascade delete with parent.
3. `FavoriteConfiguration.cs` — table `social.Favorites`; unique index `(UserId, EntityType, EntityId)`; AddedAt column required.
4. `ReportConfiguration.cs` — table `social.Reports`; unique index `(UserId, EntityType, EntityId) WHERE Status=0` (Open).
5. `ContentModerationLogConfiguration.cs` — table `social.ContentModerationLogs`; append-only (no UpdatedAt); index on `(AdminUserId, ActionedAt DESC)`.
6. `ProfanityBlocklistEntryConfiguration.cs` — table `social.ProfanityBlocklist`; unique index on `(Word, LanguageCode)`.
7. `BookingEligibilitySnapshotConfiguration.cs` — table `social.BookingEligibilitySnapshots`; composite PK `(UserId, TargetType, TargetId)`; index on `LastCompletedAt`.
8. `EntityRatingCacheConfiguration.cs` — table `social.EntityRatingCache`; composite PK `(TargetType, TargetId)`; index on `BayesianScore DESC` for ranking queries.
9. `PlaceSnapshotConfiguration.cs` — table `social.PlaceSnapshots`; PK `PlaceId`; tracks `Name, IsDeleted, LastEventId`.
10. `BusinessSnapshotConfiguration.cs` — same shape for businesses.
11. `TourSnapshotConfiguration.cs` — same shape for tours; adds `ProviderId` for reply ownership check.
12. `InboxMessageConfiguration.cs` + `OutboxMessageConfiguration.cs` — standard infrastructure tables.

---

## 9. Migration Sequence

Apply in this exact order (squashes forbidden):

| # | Migration | Owner | When |
|---|---|---|---|
| 1 | `SocialAddAggregateRootAndAuditMembers` | Tech Lead | PW-2 (Tue 2026-10-20) |
| 2 | `SocialAddReviewProvenanceColumns` | Mahmoud | T1 (adds Status, IsVerifiedBooking, ProfanityFlagged, AutoHiddenAt nullable; updates unique index to filter on IsDeleted) |
| 3 | `SocialAddReviewReplyTable` | Mahmoud | T1 |
| 4 | `SocialCreateFavoritesIndexes` | Fadwa | T2 (unique index + entityType filter index) |
| 5 | `SocialAddReportIndexesAndStatus` | Mohammad | T4 |
| 6 | `SocialAddContentModerationLog` | Mohammad | T4 (if table doesn't exist yet) |
| 7 | `SocialAddEntityRatingCacheTable` | Mohammad | T5 |
| 8 | `SocialAddBookingEligibilitySnapshotTable` | Tech Lead | PW-5 (or T1 first step if PW slipped) |
| 9 | `SocialAddCrossModuleSnapshotTables` | Tech Lead | PW-5 (places + businesses + tours snapshots) |
| 10 | `SocialSeedProfanityBlocklist` | Tech Lead | PW-6 (data-only, seeds ~50 EN + AR entries) |
