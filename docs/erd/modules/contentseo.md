# ContentSeo module — ERD

**DbContext:** `ContentSeoDbContext` · **Schema:** `content_seo`

SEO metadata, redirects, sitemap entries, FAQs (with translations), and weather caching.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `SeoMetadata` | AuditableEntity | ✅ | ✅ | ✅ |
| `Redirect` | AuditableEntity | ✅ | ✅ | ✅ |
| `SitemapEntry` | AuditableEntity | ✅ | ✅ | ✅ |
| `FaqItem` | AuditableEntity | ✅ | ✅ | ✅ |
| `FaqItemTranslation` | BaseEntity | ❌ | parent | ❌ |
| `WeatherCache` | AuditableEntity | ✅ | ✅ | ✅ |
| `WeatherDailyBudget` | BaseEntity | ✅ | ❌ | ❌ |

## Diagram

```mermaid
erDiagram
    SeoMetadata {
        guid Id PK
        string EntityType "enum"
        guid EntityId LREF "polymorphic"
    }
    Redirect {
        guid Id PK
        string FromPath
        string ToPath
    }
    SitemapEntry {
        guid Id PK
        string EntityType "string!"
        guid EntityId LREF "nullable, polymorphic"
    }
    FaqItem {
        guid Id PK
        string EntityType "enum"
        guid EntityId LREF "polymorphic"
    }
    FaqItemTranslation {
        guid Id PK
        guid FaqItemId FK
        guid LanguageId LREF
        string Question
    }
    WeatherCache {
        guid Id PK
        guid PlaceId LREF "nullable"
        date ForecastDate
    }
    WeatherDailyBudget {
        guid Id PK
        date Date
        int Count
    }

    FaqItem ||--o{ FaqItemTranslation : "FaqItemId (Cascade)"
```

## Relationships (real FK, intra-module)

- **`FaqItem` 1—* `FaqItemTranslation`** — FK `FaqItemId`, required, Cascade.

## Cross-module logical references (no DB FK)

- Polymorphic `EntityType + EntityId` on `SeoMetadata`/`FaqItem` (enum) and `SitemapEntry`
  (**string** discriminator); `FaqItemTranslation.LanguageId`; `WeatherCache.PlaceId?`.

## Notes / unclear

- `SitemapEntry.EntityType` is a `string` while `SeoMetadata`/`FaqItem` use the
  `SeoEntityType` enum — inconsistency noted.
- `WeatherDailyBudget` is an aggregate root on `BaseEntity` (no soft-delete / rowversion).
