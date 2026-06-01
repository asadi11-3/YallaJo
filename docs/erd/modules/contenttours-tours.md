# ContentTours module — Tour aggregate (ERD 1 of 2)

**DbContext:** `ContentToursDbContext` · **Schema:** `content_tours`

Covers the `Tour` and `TourPackage` aggregates: translations, schedules, waypoints, pricing
tiers, child facilities, and packages. The guide-related entities are in
[`contenttours-guides.md`](contenttours-guides.md).

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `Tour` | AuditableEntity | ✅ | ✅ | ✅ |
| `TourTranslation` | BaseEntity | ❌ | parent | ❌ |
| `TourSchedule` | BaseEntity | ❌ | parent | ❌ |
| `TourWaypoint` | BaseEntity | ❌ | parent | ❌ |
| `TourPricingTier` | BaseEntity | ❌ | parent | ❌ |
| `TourPricingTierTranslation` | BaseEntity | ❌ | parent | ❌ |
| `TourChildFacility` ⊕ | plain | ❌ | parent | ❌ |
| `TourTourGuide` ⊕ *(legacy)* | plain | ❌ | parent | ❌ |
| `TourPackage` | AuditableEntity | ✅ | ✅ | ✅ |
| `TourPackageTour` ⊕ | plain | ❌ | parent | ❌ |
| `TourPackageInclusion` | BaseEntity | ❌ | parent | ❌ |

## Diagram

```mermaid
erDiagram
    Tour {
        guid Id PK
        guid PlaceId LREF "→ ContentPlaces.Place"
        guid CreatedByUserId LREF
        decimal BasePrice "◆ Money"
        decimal Latitude "◆ Location"
    }
    TourTranslation {
        guid Id PK
        guid TourId FK
        guid LanguageId LREF "→ ContentCore.Language"
        string Title
    }
    TourSchedule {
        guid Id PK
        guid TourId FK
        datetime StartTime
    }
    TourWaypoint {
        guid Id PK
        guid TourId FK
        decimal Latitude "◆ Location"
    }
    TourPricingTier {
        guid Id PK
        guid TourId FK
        decimal Price "◆ Money"
    }
    TourPricingTierTranslation {
        guid Id PK
        guid TourPricingTierId FK
        string LanguageCode "LREF (string!)"
    }
    TourChildFacility {
        guid TourId PK_FK
        int Facility PK "enum"
    }
    TourTourGuide {
        guid TourId PK_FK
        guid TourGuideId PK "LREF (legacy)"
    }
    TourPackage {
        guid Id PK
        guid CreatedByUserId LREF
        decimal Price "◆ Money"
    }
    TourPackageTour {
        guid TourPackageId PK_FK
        guid TourId PK_FK
    }
    TourPackageInclusion {
        guid Id PK
        guid TourPackageId FK
    }

    Tour ||--o{ TourTranslation : "Cascade"
    Tour ||--o{ TourSchedule : "Cascade"
    Tour ||--o{ TourWaypoint : "Cascade"
    Tour ||--o{ TourPricingTier : "Cascade"
    Tour ||--o{ TourChildFacility : "Cascade"
    Tour ||--o{ TourTourGuide : "Cascade (legacy)"
    TourPricingTier ||--o{ TourPricingTierTranslation : "Cascade"
    TourPackage ||--o{ TourPackageTour : "Cascade"
    TourPackage ||--o{ TourPackageInclusion : "Cascade"
    TourPackageTour }o--|| Tour : "TourId (Restrict)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| Tour → TourTranslation / TourSchedule / TourWaypoint / TourPricingTier / TourChildFacility / TourTourGuide | `TourId` | Cascade |
| TourPricingTier → TourPricingTierTranslation | `TourPricingTierId` | Cascade |
| TourPackage → TourPackageTour / TourPackageInclusion | `TourPackageId` | Cascade |
| TourPackageTour → Tour | `TourId` | **Restrict** |

## Join entities (⊕)

- `TourPackageTour` (PK `TourPackageId, TourId`) — **true M2M** (FK to both, Tour side Restrict).
- `TourTourGuide` (PK `TourId, TourGuideId`) — **legacy** join (FK to Tour only; `TourGuideId`
  is a logical ref). Superseded by `GuideTourOffering` (see guides ERD).
- `TourChildFacility` (PK `TourId, Facility-enum`) — set-replace child (not a true M2M).

## Owned types

- `Tour.BasePrice` (Money), `Tour.Location`, `Tour.MeetingPoint?`, `TourWaypoint.Location`,
  `TourPricingTier.Price` (Money), `TourPackage.Price` (Money).

## Cross-module logical references (no DB FK)

- `Tour.PlaceId` (→ Place), `*.LanguageId` (→ Language), `TourTourGuide.TourGuideId`,
  user/admin ids. `TourPricingTierTranslation.LanguageCode` is a **string** (inconsistent
  with other translations' `LanguageId`).
