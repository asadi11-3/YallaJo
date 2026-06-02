# Tracking module — ERD

**DbContext:** `TrackingDbContext` · **Schema:** `tracking`

Live GPS tracking of in-progress tours: sessions, location snapshots, and checkpoints.
This is the cleanest aggregate in the system.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 |
|--------|------|----------------|----|----|
| `LiveTrackingSession` | AuditableEntity | ✅ | ✅ | ✅ |
| `LocationSnapshot` | BaseEntity | ❌ | parent | ❌ |
| `TourCheckpoint` | AuditableEntity | ❌ | ✅ | ✅ |
| `OutboxMessage` | infra | — | — | — |

> Note: `LocationSnapshot` is a GPS-reading child entity, **not** a cross-module integration
> snapshot.

## Diagram

```mermaid
erDiagram
    LiveTrackingSession {
        guid Id PK
        guid TourBookingId LREF
        guid TourGuideId LREF
        guid UserId LREF
        string Status
    }
    LocationSnapshot {
        guid Id PK
        guid SessionId FK
        decimal Latitude "◆ Location"
        decimal Longitude "◆ Location"
        datetime RecordedAt
    }
    TourCheckpoint {
        guid Id PK
        guid SessionId FK
        guid WaypointId LREF "no FK, unique w/ SessionId"
    }

    LiveTrackingSession ||--o{ LocationSnapshot : "SessionId (Cascade)"
    LiveTrackingSession ||--o{ TourCheckpoint : "SessionId (Cascade)"
```

## Relationships (real FKs, intra-module)

| Principal → Dependent | FK | Delete |
|-----------------------|----|--------|
| LiveTrackingSession → LocationSnapshot | `SessionId` | Cascade |
| LiveTrackingSession → TourCheckpoint | `SessionId` | Cascade |

## Owned types

- `LocationSnapshot.Location` (`OwnsOne` `Location`) → `Latitude` (10,8), `Longitude` (11,8).

## Cross-module logical references (no DB FK)

- `LiveTrackingSession.{TourBookingId, TourGuideId, UserId}`; `TourCheckpoint.WaypointId`
  (→ ContentTours.TourWaypoint).
