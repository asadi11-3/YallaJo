# Analytics module — Metrics / Snapshots / GDPR (ERD 2 of 2)

**DbContext:** `AnalyticsDbContext` · **Schema:** `analytics`

Covers high-volume event/metric tables, popularity scoring, integration snapshots, seasonal
reference data, the dashboard cache, and GDPR deletion requests. Personalization is in
[`analytics-personalization.md`](analytics-personalization.md).

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 | Key |
|--------|------|----------------|----|----|-----|
| `UserInteraction` | BaseEntity&lt;long&gt; | ❌ | ❌ | ❌ | long |
| `SuggestionMetric` | BaseEntity&lt;long&gt; | ❌ | ❌ | ❌ | long |
| `SponsoredClickEvent` | BaseEntity&lt;long&gt; | ❌ | ❌ | ❌ | long |
| `EntityPopularitySnapshot` 📸 | BaseEntity&lt;long&gt; | ❌ | ❌ | ❌ | long |
| `AuditLog` | BaseEntity&lt;long&gt; | ✅ | ❌ | ❌ | long |
| `PopularityScore` | AuditableEntity | ✅ | ✅ | ✅ | Guid |
| `EntityAttributeSnapshot` 📸 | AuditableEntity | ✅ | ✅ | ✅ | Guid |
| `PaymentSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ | Guid |
| `BookingSnapshot` 📸 | BaseEntity | ❌ | ❌ | ❌ | Guid |
| `SeasonalityRule` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `HolidayCalendar` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `TripArc` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `DashboardCache` | plain | ❌ | ❌ | ❌ | string `Key` |
| `GdprDeletionRequest` | BaseEntity | ✅ | ❌ | ❌ | Guid |

## Diagram

```mermaid
erDiagram
    UserInteraction {
        long Id PK
        guid UserId LREF "nullable"
        string EntityType
        guid EntityId LREF "polymorphic"
    }
    SuggestionMetric {
        long Id PK
        guid BatchId LREF "nullable"
        guid UserId LREF "nullable"
    }
    SponsoredClickEvent {
        long Id PK
        guid BidId LREF
        string SourceKind
        guid SourceId LREF "polymorphic"
    }
    PopularityScore {
        guid Id PK
        string EntityType
        guid EntityId LREF "polymorphic"
        double Score
    }
    EntityPopularitySnapshot {
        long Id PK
        string EntityType
        guid EntityId LREF "polymorphic"
    }
    EntityAttributeSnapshot {
        guid Id PK
        string EntityKind
        guid EntityId LREF "polymorphic"
        guid PlaceId LREF "nullable"
    }
    PaymentSnapshot {
        guid Id PK
        guid PaymentId LREF
        guid BookingId LREF
        guid UserId LREF
        guid ProviderId LREF
    }
    BookingSnapshot {
        guid Id PK
        guid BookingId LREF
        guid UserId LREF
        guid ProviderId LREF
        guid TourId LREF
    }
    SeasonalityRule {
        guid Id PK
        guid PlaceId LREF
    }
    HolidayCalendar {
        guid Id PK
        date Date
    }
    TripArc {
        guid Id PK
    }
    DashboardCache {
        string Key PK
        string Payload
    }
    AuditLog {
        long Id PK
        guid UserId LREF "nullable"
        string EntityType
        guid EntityId LREF
        guid RedactedByUserId LREF "nullable"
    }
    GdprDeletionRequest {
        guid Id PK
        guid UserId LREF
        string Status
    }
```

> No relationship lines: Analytics defines no EF relationships. All links are loose ids.

## Snapshots (📸)

- `PaymentSnapshot`, `BookingSnapshot` — local copies of Finance/Booking data.
- `EntityPopularitySnapshot` — point-in-time popularity score.
- `EntityAttributeSnapshot` — denormalized Tour/Place/Business attributes for recommendations.

## Cross-module logical references (no DB FK)

- `UserId`, `BookingId`, `PaymentId`, `ProviderId`, `TourId`, `PlaceId`, `BatchId`, `BidId`,
  and polymorphic `EntityType/EntityKind/SourceKind + EntityId/SourceId`. `AuditLog.EntityType`
  is a **string** discriminator.

## Notes / unclear

- High-volume event tables use **`long`** keys (`UserInteraction`, `SuggestionMetric`,
  `SponsoredClickEvent`, `EntityPopularitySnapshot`, `AuditLog`).
- `DashboardCache` is keyed by a string `Key`.
