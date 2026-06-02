# ContentTours module — Guide aggregate (ERD 2 of 2)

**DbContext:** `ContentToursDbContext` · **Schema:** `content_tours`

Covers `TourGuide` and the guide-offering lifecycle: languages, specializations,
applications, offerings, schedules, pricing tiers, availability blocks, and proposals.
Tour-side entities are in [`contenttours-tours.md`](contenttours-tours.md).

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `TourGuide` | AuditableEntity | ✅ | ✅ | ✅ |
| `TourGuideLanguage` ⊕ | plain | ❌ | parent | ❌ |
| `TourGuideSpecialization` ⊕ | plain | ❌ | parent | ❌ |
| `GuideApplication` | AuditableEntity | ✅ | ✅ | ✅ |
| `GuideTourOffering` | AuditableEntity | ❌ | ✅ | ✅ |
| `GuideSchedule` | BaseEntity | ❌ | ❌ | ❌ |
| `GuidePricingTier` | BaseEntity | ❌ | ❌ | ❌ |
| `GuideAvailabilityBlock` | BaseEntity | ❌ | ❌ | ❌ |
| `TourProposal` | AuditableEntity | ✅ | ✅ | ✅ |

## Diagram

```mermaid
erDiagram
    TourGuide {
        guid Id PK
        guid UserId LREF "unique"
        guid LinkedProviderId LREF "nullable"
    }
    TourGuideLanguage {
        guid TourGuideId PK_FK
        guid LanguageId PK "LREF → ContentCore.Language"
    }
    TourGuideSpecialization {
        guid TourGuideId PK_FK
        guid SpecializationId PK "LREF → ContentCore.Specialization"
    }
    GuideApplication {
        guid Id PK
        guid TourId LREF "no FK"
        guid TourGuideId LREF "no FK"
        guid GuideUserId LREF
    }
    GuideTourOffering {
        guid Id PK
        guid TourId FK
        guid TourGuideId LREF "no FK"
        string Status
    }
    GuideSchedule {
        guid Id PK
        guid GuideTourOfferingId LREF "no FK"
        guid TourGuideId LREF "no FK"
    }
    GuidePricingTier {
        guid Id PK
        guid GuideTourOfferingId LREF "no FK"
        decimal Price "◆ Money"
    }
    GuideAvailabilityBlock {
        guid Id PK
        guid GuideId FK
    }
    TourProposal {
        guid Id PK
        guid TourGuideId LREF "no FK"
        guid GuideUserId LREF
        guid CreatedTourId LREF "no FK"
        guid PlaceId LREF
    }

    TourGuide ||--o{ TourGuideLanguage : "Cascade"
    TourGuide ||--o{ TourGuideSpecialization : "Cascade"
    TourGuide ||--o{ GuideAvailabilityBlock : "GuideId (Cascade)"
    Tour ||--o{ GuideTourOffering : "TourId (Cascade)"

    Tour {
        guid Id PK
    }
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| TourGuide → TourGuideLanguage | `TourGuideId` | Cascade |
| TourGuide → TourGuideSpecialization | `TourGuideId` | Cascade |
| TourGuide → GuideAvailabilityBlock | `GuideId` | Cascade |
| Tour → GuideTourOffering | `TourId` | Cascade (navigationless on offering) |

> `GuideSchedule`, `GuidePricingTier`, `GuideApplication`, `TourProposal` define their
> `TourId`/`TourGuideId`/`GuideTourOfferingId` as **indexed Guids with no FK** —
> application-managed referential integrity.

## Join entities (⊕)

- `TourGuideLanguage` / `TourGuideSpecialization` — half-joins (FK to TourGuide only; second
  key is a cross-module logical ref to ContentCore).

## Owned types

- `GuidePricingTier.Price` (`OwnsOne Money` → `PriceAmount` + `PriceCurrency`).

## Cross-module logical references (no DB FK)

- `TourGuide.UserId` (unique), `LinkedProviderId?`, `LanguageId`, `SpecializationId`, user/admin
  ids, `TourProposal.PlaceId`.

## Notes / unclear

- **`GuideTourOffering` replaces the legacy `TourTourGuide` join** (confirmed via the entity's
  XML doc: *"Replaces the simple TourTourGuide join entity with full lifecycle management."*).
  Both remain mapped. `GuideTourOffering` is `AuditableEntity` but **not** an aggregate root.
- **`GuideAvailabilityBlock`** config omits a schema in `ToTable`, but `HasDefaultSchema`
  resolves it to **`content_tours`** (confirmed in the model snapshot). No schema anomaly.
