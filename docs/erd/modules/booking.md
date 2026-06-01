# Booking module — ERD

**DbContext:** `BookingDbContext` · **Schema:** `booking`

Tour bookings, guide availability slots, join requests, slot locks, refund policies,
provider documents, guide discounts, and integration snapshots.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 | Notes |
|--------|------|----------------|----|----|-------|
| `TourBooking` | AuditableEntity | ✅ | ✅ | ✅ | |
| `TourGuide` | AuditableEntity | ✅ | ✅ | ✅ | local guide record |
| `TourGuideLanguage` ⊕ | plain | ❌ | parent | ❌ | composite PK |
| `TourGuideSpecialization` ⊕ | plain | ❌ | parent | ❌ | composite PK |
| `AvailabilitySlot` | AuditableEntity | ✅ | ✅ | ✅ | |
| `SlotLock` | BaseEntity | ✅ | parent | ❌ | |
| `JoinRequest` | AuditableEntity | ✅ | ✅ | ✅ | |
| `RefundPolicy` | AuditableEntity | ✅ | ✅ | ✅ | |
| `ProviderDocument` | AuditableEntity | ✅ | ✅ | ✅ | XOR target |
| `GuideDiscount` | AuditableEntity | ✅ | ✅ | ✅ | |
| `TourSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ | Id = TourId |
| `ProviderSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ | Id = ProviderId |
| `PricingTierSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ | |
| `OutboxMessage` | infra | — | — | — | |

## Diagram

```mermaid
erDiagram
    TourGuide {
        guid Id PK
        guid UserId LREF "unique"
    }
    TourGuideLanguage {
        guid TourGuideId PK_FK
        guid LanguageId PK "LREF → ContentCore.Language"
    }
    TourGuideSpecialization {
        guid TourGuideId PK_FK
        guid SpecializationId PK "LREF → ContentCore.Specialization"
    }
    AvailabilitySlot {
        guid Id PK
        guid TourGuideId FK
        guid TourId LREF "nullable"
        guid BusinessId LREF "nullable"
        datetime StartUtc
    }
    SlotLock {
        guid Id PK
        guid AvailabilitySlotId FK
        guid UserId LREF
        guid BookingId LREF "nullable, no FK"
    }
    TourBooking {
        guid Id PK
        string Reference
        guid UserId LREF
        guid TourId LREF
        guid ProviderId LREF
        guid GuideId LREF
        guid AvailabilitySlotId LREF "indexed, no FK"
        guid JoinedFromBookingId "self LREF, no FK"
        decimal TotalAmount
    }
    JoinRequest {
        guid Id PK
        guid TourBookingId FK
        guid UserId LREF
        guid AvailabilitySlotId LREF "no FK"
        guid ResultingBookingId LREF "nullable, no FK"
    }
    ProviderDocument {
        guid Id PK
        guid TourGuideId FK "optional (XOR)"
        guid BusinessId LREF "optional (XOR), no FK"
    }
    RefundPolicy {
        guid Id PK
    }
    GuideDiscount {
        guid Id PK
        guid GuideUserId LREF
        guid TourId LREF "nullable"
    }
    TourSnapshot {
        guid Id PK "= TourId"
    }
    ProviderSnapshot {
        guid Id PK "= ProviderId"
    }
    PricingTierSnapshot {
        guid Id PK
        guid TourId LREF
    }

    TourGuide ||--o{ TourGuideLanguage : "Cascade"
    TourGuide ||--o{ TourGuideSpecialization : "Cascade"
    TourGuide ||--o{ AvailabilitySlot : "Restrict"
    TourGuide ||--o{ ProviderDocument : "Restrict (optional FK)"
    AvailabilitySlot ||--o{ SlotLock : "Restrict (navigationless)"
    TourBooking ||--o{ JoinRequest : "Restrict"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Required | Delete |
|-----------------------|----|----------|--------|
| TourGuide → TourGuideLanguage | `TourGuideId` | yes | Cascade |
| TourGuide → TourGuideSpecialization | `TourGuideId` | yes | Cascade |
| TourGuide → AvailabilitySlot | `TourGuideId` | yes | Restrict |
| TourGuide → ProviderDocument | `TourGuideId` | **optional** | Restrict |
| AvailabilitySlot → SlotLock | `AvailabilitySlotId` | yes | Restrict (navigationless) |
| TourBooking → JoinRequest | `TourBookingId` | yes | Restrict |

## Join entities (⊕)

- `TourGuideLanguage` (PK `TourGuideId, LanguageId`) and `TourGuideSpecialization`
  (PK `TourGuideId, SpecializationId`) are **half-joins**: FK only to `TourGuide`; the
  second key column is a **cross-module logical reference** (no FK).

## Owned types / value objects

- None mapped via `OwnsOne`/`OwnsMany`. `TourBooking` pricing is stored as flat decimal
  columns; `BookingReference` flattened to `Reference` string; `BookingLineItem[]`
  serialized to `LineItemsJson`; `RefundPolicySnapshot` serialized to a JSON column.

## Snapshots (📸)

- `TourSnapshot` (Id = source TourId), `ProviderSnapshot` (Id = source ProviderId),
  `PricingTierSnapshot` — local copies of ContentTours/Accounts data via inbox handlers.

## Cross-module logical references (no DB FK)

- `UserId`, `TourId`, `ProviderId`, `GuideId`, `BusinessId`, `LanguageId`,
  `SpecializationId` (see [`00-cross-module-logical-links.md`](../00-cross-module-logical-links.md)).

## Notes / unclear

- **`ProviderDocument` XOR target:** exactly one of `TourGuideId` (real optional FK) or
  `BusinessId` (logical, cross-module) — enforced by check constraint
  `CK_ProviderDocuments_SingleTarget`.
- `TourBooking.AvailabilitySlotId`, `JoinRequest.AvailabilitySlotId`,
  `TourBooking.JoinedFromBookingId?` are intra-module but **FK-less** (indexed only).
