# Social module — ERD

**DbContext:** `SocialDbContext` · **Schema:** `social`

Reviews (with replies & helpful votes), reports, favorites, rating caches, moderation, and
integration snapshots used for eligibility checks.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Review` | AuditableEntity | ✅ | ✅ | ✅ |
| `ReviewReply` ◆ | BaseEntity (owned) | ❌ | (owner) | ❌ |
| `ReviewHelpfulVote` ⊕ | plain | ❌ | ❌ | ❌ |
| `Report` | AuditableEntity | ✅ | ✅ | ✅ |
| `Favorite` | AuditableEntity | ✅ | ✅ | ✅ |
| `EntityRatingCache` | AuditableEntity | ✅ | ✅ | ✅ |
| `UserModerationRecord` | AuditableEntity | ✅ | ✅ | ✅ |
| `ContentModerationLog` | BaseEntity | ❌ | ❌ | ❌ |
| `ProfanityBlocklistEntry` | BaseEntity | ❌ | ❌ | ❌ |
| `TourSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ |
| `PlaceSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ |
| `BusinessSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ |
| `BookingEligibilitySnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ |

## Diagram

```mermaid
erDiagram
    Review {
        guid Id PK
        guid UserId LREF
        string TargetType
        guid TargetId LREF "polymorphic"
        int Rating
    }
    ReviewReply {
        guid Id PK
        guid ReviewId FK "owned"
        guid ProviderUserId LREF
        string Body
    }
    ReviewHelpfulVote {
        guid ReviewId PK "LREF, no FK"
        guid UserId PK "LREF"
    }
    Report {
        guid Id PK
        guid ReporterUserId LREF
        string EntityType
        guid EntityId LREF "polymorphic"
        string Status
    }
    Favorite {
        guid Id PK
        guid UserId LREF
        string EntityType
        guid EntityId LREF "polymorphic"
    }
    EntityRatingCache {
        guid Id PK
        string TargetType
        guid TargetId LREF "polymorphic"
        double Average
    }
    UserModerationRecord {
        guid Id PK
        guid UserId LREF
        guid IssuedByAdminId LREF
    }
    ContentModerationLog {
        guid Id PK
        guid AdminUserId LREF
        guid SourceReportId LREF "nullable"
    }
    TourSnapshot {
        guid Id PK
        guid TourId LREF
    }
    PlaceSnapshot {
        guid Id PK
        guid PlaceId LREF
    }
    BusinessSnapshot {
        guid Id PK
        guid BusinessId LREF
    }
    BookingEligibilitySnapshot {
        guid Id PK
        guid UserId LREF
        string TargetType
        guid TargetId LREF
    }

    Review ||--o{ ReviewReply : "Replies (OwnsMany)"
```

## Relationships

- **`Review` 1—* `ReviewReply`** — **owned collection** (`OwnsMany`), FK `ReviewId` → table `ReviewReplies`.
- No other EF relationships. `ReviewHelpfulVote` links to `Review` by id only (composite PK, no FK/nav).

## Owned types

- `Review.Replies` → `OwnsMany ReviewReply` (own table). `ReviewReply` has an entity-local
  `IsDeleted` flag (not `ISoftDeletable`).

## Snapshots (📸)

- `TourSnapshot`, `PlaceSnapshot`, `BusinessSnapshot` (own manual `IsDeleted` columns, no query
  filter), `BookingEligibilitySnapshot` (verifies completed-booking eligibility).

## Cross-module logical references (no DB FK)

- `UserId`, polymorphic `TargetType/EntityType + TargetId/EntityId` (three distinct enums:
  `ReviewTargetType`, `ReportableEntityType`, `FavoriteEntityType`); snapshot ids
  `TourId`/`PlaceId`/`BusinessId`/`BookingId`.

## Notes / unclear

- `ContentModerationLog` and `ProfanityBlocklistEntry` are append-only `BaseEntity` tables.
