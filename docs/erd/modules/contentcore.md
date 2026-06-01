# ContentCore module — ERD

**DbContext:** `ContentCoreDbContext` · **Schema:** `content_core`

Shared reference data and cross-cutting content: languages, categories (hierarchical), tags,
specializations, attachments, and the polymorphic entity↔tag/category/image join tables.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Language` | AuditableEntity | ✅ | ✅ | ✅ |
| `Category` | AuditableEntity | ✅ | ✅ | ✅ |
| `CategoryTranslation` | BaseEntity | ❌ | parent | ❌ |
| `Tag` | AuditableEntity | ✅ | ❌* | ✅ |
| `TagTranslation` | BaseEntity | ❌ | parent | ❌ |
| `Specialization` | AuditableEntity | ✅ | ✅ | ✅ |
| `SpecializationTranslation` | BaseEntity | ❌ | parent | ❌ |
| `Attachment` | BaseEntity | ✅ | ❌ | ❌ |
| `EntityTag` ⊕ | plain | ❌ | via Tag | ❌ |
| `EntityCategory` ⊕ | plain | ❌ | via Category | ❌ |
| `EntityImage` ⊕ | plain | ❌ | ❌ | ❌ |
| `TranslationCache` | BaseEntity | ✅ | ❌ | ✅ |

\* `Tag` defers its audit/soft-delete/rowversion to a shared convention; columns exist
(IsDeleted/DeletedAt/RowVersion) but **no query filter** on Tag itself — see Limitations.

## Diagram

```mermaid
erDiagram
    Language {
        guid Id PK
        string Code
    }
    Category {
        guid Id PK
        string Slug
        guid ParentCategoryId FK "nullable, self"
    }
    CategoryTranslation {
        guid Id PK
        guid CategoryId FK
        guid LanguageId LREF
    }
    Tag {
        guid Id PK
        string Slug
    }
    TagTranslation {
        guid Id PK
        guid TagId FK
        guid LanguageId LREF
    }
    Specialization {
        guid Id PK
    }
    SpecializationTranslation {
        guid Id PK
        guid SpecializationId FK
        guid LanguageId LREF
    }
    Attachment {
        guid Id PK
        string EntityType "polymorphic"
        guid EntityId LREF
        guid UploadedByUserId LREF
    }
    EntityTag {
        string EntityType PK
        guid EntityId PK "LREF"
        guid TagId PK_FK
    }
    EntityCategory {
        string EntityType PK
        guid EntityId PK "LREF"
        guid CategoryId PK_FK
    }
    EntityImage {
        string EntityType PK
        guid EntityId PK "LREF"
        guid AttachmentId PK_FK
        int ImageSize PK
    }
    TranslationCache {
        guid Id PK
        string EntityType
        guid EntityId LREF "nullable"
    }

    Category ||--o{ CategoryTranslation : "Cascade"
    Category ||--o{ Category : "ParentCategoryId (Restrict, self)"
    Tag ||--o{ TagTranslation : "Cascade"
    Specialization ||--o{ SpecializationTranslation : "Cascade"
    Tag ||--o{ EntityTag : "TagId (Cascade)"
    Category ||--o{ EntityCategory : "CategoryId (Cascade)"
    Attachment ||--o{ EntityImage : "AttachmentId (Cascade)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| Category → CategoryTranslation | `CategoryId` | Cascade |
| Category → Category (self) | `ParentCategoryId?` | Restrict |
| Tag → TagTranslation | `TagId` | Cascade |
| Specialization → SpecializationTranslation | `SpecializationId` | Cascade |
| EntityTag → Tag | `TagId` | Cascade |
| EntityCategory → Category | `CategoryId` | Cascade |
| EntityImage → Attachment | `AttachmentId` | Cascade |

## Join entities (⊕, polymorphic)

- `EntityTag` / `EntityCategory` / `EntityImage` are **polymorphic** join tables keyed by
  `(EntityType, EntityId, …)`. They have a real FK only to the ContentCore side
  (Tag/Category/Attachment); `EntityId` is a **logical cross-entity reference** (no FK).

## Cross-module logical references (no DB FK)

- All `*.LanguageId` (translations → Language — note: no FK even though same module),
  `Attachment.{EntityType, EntityId, UploadedByUserId}`, and the polymorphic `EntityId` on
  the join tables.

## Notes / unclear

- Translation→Language is **never** an EF relationship (only unique composite indexes
  `(ParentId, LanguageId)`).
- `Attachment` is an aggregate root on `BaseEntity` (no soft-delete; uses an
  `IsMarkedForDeletion` flag instead).
