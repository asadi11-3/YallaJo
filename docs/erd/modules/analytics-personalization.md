# Analytics module — Personalization / Recommendation (ERD 1 of 2)

**DbContext:** `AnalyticsDbContext` · **Schema:** `analytics`

Covers user preferences, recommendation caches, suggestion batches, boosts/editorial pins,
experiments, and exclusions. Metrics, snapshots and GDPR are in
[`analytics-metrics-snapshots.md`](analytics-metrics-snapshots.md).

> **Analytics has NO navigation properties / NO `HasOne`/`HasMany`.** All links are loose
> `Guid` columns. Most entities use the **polymorphic `EntityKind`/`SourceKind` + `EntityId`**
> pattern.

## Entities

| Entity | Base | Aggregate root | 🗑 | 🔒 | Key |
|--------|------|----------------|----|----|-----|
| `UserPreference` | AuditableEntity | ✅ | ✅ | ✅ | Guid |
| `UserPreferredCategory` ⊕ | plain | ❌ | ❌ | ❌ | composite (UserId,CategoryId) |
| `RecommendationCache` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `SuggestionBatch` | AuditableEntity | ✅ | ❌* | ❌* | Guid |
| `BoostPackage` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `EditorialPin` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `UserExcludedEntity` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `Experiment` | BaseEntity | ✅ | ❌ | ❌ | Guid |
| `ExperimentAssignment` ⊕ | plain | ❌ | ❌ | ❌ | composite (UserId,ExperimentId) |

\* `SuggestionBatch` has `IsDeleted`/`RowVersion` **columns** but **no query filter / no
`IsRowVersion()`** in config — see Limitations.

## Diagram

```mermaid
erDiagram
    UserPreference {
        guid Id PK
        guid UserId LREF
    }
    UserPreferredCategory {
        guid UserId PK "LREF"
        guid CategoryId PK "LREF → ContentCore.Category"
    }
    RecommendationCache {
        guid Id PK
        guid UserId LREF "nullable"
        guid BatchId LREF "no FK"
        string EntityKind
        guid EntityId LREF "polymorphic"
    }
    SuggestionBatch {
        guid Id PK
        string SourceKind
        guid SourceId LREF "polymorphic"
    }
    BoostPackage {
        guid Id PK
        guid ProviderId LREF
        string EntityKind
        guid EntityId LREF "polymorphic"
    }
    EditorialPin {
        guid Id PK
        string EntityKind
        guid EntityId LREF "polymorphic"
    }
    UserExcludedEntity {
        guid Id PK
        guid UserId LREF
        string EntityKind
        guid EntityId LREF "polymorphic"
    }
    Experiment {
        guid Id PK
        string Key
    }
    ExperimentAssignment {
        guid UserId PK "LREF"
        guid ExperimentId PK "LREF → Experiment"
    }
```

> No relationship lines: Analytics defines none. `UserPreferredCategory` and
> `ExperimentAssignment` are composite-key join-style tables with **no navigations / no FK**.

## Join tables (⊕)

- `UserPreferredCategory` (PK `UserId, CategoryId`) — both keys cross-module logical refs.
- `ExperimentAssignment` (PK `UserId, ExperimentId`) — links a user to an experiment by id only.

## Cross-module logical references (no DB FK)

- `UserId`, `CategoryId`, `ProviderId`, `BatchId`, and pervasive polymorphic
  `EntityKind/SourceKind` + `EntityId/SourceId` (target = Tour/Place/Business per discriminator).

## Notes / unclear

- `EntityRef` value object exists in the domain but is **not mapped** as an owned type.
- `SuggestionBatch` soft-delete/rowversion columns exist but are not enforced (see Limitations).
