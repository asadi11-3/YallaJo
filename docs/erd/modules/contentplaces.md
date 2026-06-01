# ContentPlaces module — ERD

**DbContext:** `ContentPlacesDbContext` *(public, not sealed; does not implement IDbContext)*
· **Schema:** `content_places`

Places and businesses with translations, hours, service items, staff, amenities, and the
`PlaceBusiness` association.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Place` | AuditableEntity | ✅ | ✅ | ✅ |
| `PlaceTranslation` | BaseEntity | ❌ | parent | ❌ |
| `Business` | AuditableEntity | ✅ | ✅ | ✅ |
| `BusinessTranslation` | BaseEntity | ❌ | parent | ❌ |
| `BusinessHours` | BaseEntity | ❌ | parent | ❌ |
| `ServiceItem` | AuditableEntity | ❌ | ✅ | ✅ |
| `BusinessStaff` | AuditableEntity | ❌ | ✅ | ✅ |
| `BusinessAmenity` | BaseEntity | ❌ | parent | ❌ |
| `AccessibilityFeature` | BaseEntity | ❌ | ❌ | ❌ |
| `PlaceBusiness` ⊕ | plain | ❌ | both parents | ❌ |

## Diagram

```mermaid
erDiagram
    Place {
        guid Id PK
        string Name
        guid CategoryId LREF "nullable → ContentCore.Category"
        decimal Latitude "◆ Location"
    }
    PlaceTranslation {
        guid Id PK
        guid PlaceId FK
        guid LanguageId LREF
    }
    Business {
        guid Id PK
        string Name
        guid PlaceId LREF "scalar+index, no FK"
        guid OwnerId LREF
        decimal Latitude "◆ Location"
    }
    BusinessTranslation {
        guid Id PK
        guid BusinessId FK
        guid LanguageId LREF
    }
    BusinessHours {
        guid Id PK
        guid BusinessId FK
        int DayOfWeek
    }
    ServiceItem {
        guid Id PK
        guid BusinessId FK
        decimal Price
    }
    BusinessStaff {
        guid Id PK
        guid BusinessId FK
        guid UserId LREF
    }
    BusinessAmenity {
        guid Id PK
        guid BusinessId FK
    }
    AccessibilityFeature {
        guid Id PK
        int EntityType "byte enum"
        guid EntityId LREF "polymorphic"
    }
    PlaceBusiness {
        guid PlaceId PK_FK
        guid BusinessId PK_FK
    }

    Place ||--o{ PlaceTranslation : "Cascade"
    Business ||--o{ BusinessTranslation : "Cascade"
    Business ||--o{ BusinessHours : "Cascade"
    Business ||--o{ ServiceItem : "Cascade"
    Business ||--o{ BusinessStaff : "Cascade"
    Business ||--o{ BusinessAmenity : "Cascade"
    Place ||--o{ PlaceBusiness : "PlaceId (Cascade)"
    Business ||--o{ PlaceBusiness : "BusinessId (Cascade)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| Place → PlaceTranslation | `PlaceId` | Cascade |
| Business → BusinessTranslation / BusinessHours / ServiceItem / BusinessStaff / BusinessAmenity | `BusinessId` | Cascade |
| PlaceBusiness → Place | `PlaceId` | Cascade |
| PlaceBusiness → Business | `BusinessId` | Cascade |

## Join entity (⊕)

- **`PlaceBusiness`** (PK `PlaceId, BusinessId`) — a **real many-to-many** join with FKs to
  both sides (no inverse navigations on Place/Business).

## Owned types

- `Place.Location`, `Business.Location` (`OwnsOne Location`).
- `ServiceItem.Price` is a plain decimal + `Currency` string (NOT a Money value object).

## Cross-module logical references (no DB FK)

- `Place.CategoryId?` (→ Category), `*.LanguageId` (→ Language), `Business.OwnerId`,
  `BusinessStaff.UserId`, polymorphic `AccessibilityFeature.EntityType + EntityId`.

## Notes / unclear

- **Dual Place↔Business linkage:** `Business.PlaceId` is a **scalar + index (no FK)** —
  a "one owning place" reference — while `PlaceBusiness` is a separate **real M2M** join.
  Both are authoritative (different semantics); neither is marked legacy in code.
