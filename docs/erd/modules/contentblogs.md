# ContentBlogs module — ERD

**DbContext:** `ContentBlogsDbContext` · **Schema:** `content_blogs`

Blogs (with translations, comments, reactions, views, tour links) and the creator identity
sub-domain (profiles, applications, invitations, follows, niches).

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Blog` | AuditableEntity | ✅ | ✅ | ✅ |
| `BlogTranslation` | BaseEntity | ❌ | parent | ❌ |
| `BlogTour` ⊕ | plain | ❌ | parent | ❌ |
| `BlogComment` | AuditableEntity | ✅ | ✅ | ✅ |
| `BlogCommentReaction` | BaseEntity | ❌ | parent | ❌ |
| `BlogView` | plain | ❌ | ❌ | ❌ |
| `CreatorProfile` | AuditableEntity | ✅ | ✅ | ✅ |
| `CreatorNiche` | AuditableEntity | ❌ | ✅ | ✅ |
| `CreatorInvitation` | AuditableEntity | ✅ | ✅ | ✅ |
| `CreatorFollow` | BaseEntity | ❌ | ❌ | ❌ |
| `CreatorApplication` | AuditableEntity | ✅ | ✅ | ✅ |

## Diagram

```mermaid
erDiagram
    Blog {
        guid Id PK
        guid AuthorId LREF
        guid PlaceId LREF "nullable"
        guid LanguageId LREF
        guid AuthoredByCreatorId LREF "nullable, no FK"
        json DisclosedTargets "◆ OwnsMany ToJson"
    }
    BlogTranslation {
        guid Id PK
        guid BlogId FK
        guid LanguageId LREF
    }
    BlogTour {
        guid BlogId PK_FK
        guid TourId PK "LREF → ContentTours.Tour"
    }
    BlogComment {
        guid Id PK
        guid BlogId FK
        guid UserId LREF
        guid ParentCommentId FK "nullable, self"
    }
    BlogCommentReaction {
        guid Id PK
        guid CommentId FK
        guid UserId LREF
    }
    BlogView {
        guid Id PK
        guid BlogId FK
        bytes ViewerHash
    }
    CreatorProfile {
        guid Id PK
        guid UserId LREF
        guid LinkedProviderId LREF "nullable"
    }
    CreatorFollow {
        guid Id PK
        guid FollowerUserId LREF
        guid CreatorProfileId FK
    }
    CreatorNiche {
        guid Id PK
    }
    CreatorInvitation {
        guid Id PK
        guid InvitedUserId LREF "nullable"
        guid SentByAdminId LREF
    }
    CreatorApplication {
        guid Id PK
        guid ApplicantUserId LREF
        guid InvitationId LREF "nullable"
    }

    Blog ||--o{ BlogTranslation : "Cascade"
    Blog ||--o{ BlogComment : "Cascade"
    Blog ||--o{ BlogTour : "BlogId (Cascade)"
    Blog ||--o{ BlogView : "BlogId (Cascade, navigationless)"
    BlogComment ||--o{ BlogCommentReaction : "Cascade"
    BlogComment ||--o{ BlogComment : "ParentCommentId (Restrict, self)"
    CreatorProfile ||--o{ CreatorFollow : "CreatorProfileId (Cascade)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| Blog → BlogTranslation | `BlogId` | Cascade |
| Blog → BlogComment | `BlogId` | Cascade |
| Blog → BlogTour | `BlogId` | Cascade |
| Blog → BlogView | `BlogId` | Cascade (navigationless) |
| BlogComment → BlogCommentReaction | `CommentId` | Cascade |
| BlogComment → BlogComment (self) | `ParentCommentId?` | Restrict |
| CreatorProfile → CreatorFollow | `CreatorProfileId` | Cascade *(convention-inferred)* |

## Join entity (⊕)

- **`BlogTour`** (PK `BlogId, TourId`) — partial M2M: FK to `Blog` only; `TourId` is a
  **cross-module logical reference** (no FK, no nav).

## Owned types

- `Blog.DisclosedTargets` → **`OwnsMany DisclosureTarget` mapped `ToJson`** — itself a
  polymorphic ref (`EntityType` string + `EntityId` + `RelationKind`) stored as a JSON column.

## Cross-module logical references (no DB FK)

- `Blog.{AuthorId, PlaceId?, LanguageId, ReviewedByAdminId?, FeaturedByAdminId?}`,
  `BlogTour.TourId`, `BlogComment.UserId`, creator user/admin/provider ids,
  `CreatorApplication.{NicheIds[], LanguageIds[], PreferredRegionIds[]}` (JSON Guid lists).

## Notes / unclear

- **`CreatorFollow → CreatorProfile`** relationship is **not explicitly configured** — EF
  resolves it by convention (nav `CreatorProfile`/`Followers`), required, **Cascade**
  (confirmed in the model snapshot).
- `BlogComment.IsContentRedacted` is a separate "soft" comment deletion preserving the thread
  (distinct from inherited `IsDeleted`).
- **`BlogView`** config omits a schema in `ToTable`, but `HasDefaultSchema` resolves it to
  **`content_blogs`** (confirmed in the snapshot). No schema anomaly.
