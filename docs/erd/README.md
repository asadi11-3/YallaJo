# YallaJo — Entity Relationship Documentation (ERD)

This folder documents the complete data model of the **YallaJo** platform, derived
**directly from the EF Core model** (DbContext classes, entity classes, and Fluent API
`IEntityTypeConfiguration` classes). It is intended for both technical (developer) and
documentation (SRS/PDF) audiences.

> **Source of truth:** The live **Fluent API configurations + entity classes + DbContexts**.
> See [Limitations](#limitations) regarding the (stale) EF migration `ModelSnapshot` files.

---

## Architecture summary

YallaJo is a **modular monolith**. There are **14 modules**, each owning its **own
`DbContext`** and its **own database schema**. Crucially:

- **No database-level foreign key crosses a module boundary.**
- Inter-module links are plain `Guid` columns ("logical references") populated via
  **integration events** (Outbox → Inbox messaging).
- Some modules keep **snapshot entities** — local read-model copies of data owned by
  other modules.

---

## Document index

| File | Purpose | Audience |
|------|---------|----------|
| [`00-master.md`](00-master.md) | Module-level master ERD (modules as nodes + dominant cross-module flows) | All |
| [`00-simplified-srs.md`](00-simplified-srs.md) | Simplified ERD (core business entities only, no infra) | SRS / PDF / non-technical |
| [`00-cross-module-logical-links.md`](00-cross-module-logical-links.md) | Catalog of every cross-module logical reference; **real DB FK vs logical-only** | Architects / developers |
| [`VERIFICATION.md`](VERIFICATION.md) | PHASE 4 verification report + limitations | Reviewers |
| `modules/accounts.md` | Accounts module ERD | Developers |
| `modules/auth.md` | Auth module ERD | Developers |
| `modules/security.md` | Security (identity) module ERD | Developers |
| `modules/booking.md` | Booking module ERD | Developers |
| `modules/finance-payments.md` | Finance — Payments / Payouts / Invoices | Developers |
| `modules/finance-disputes-discounts.md` | Finance — Disputes / Discounts / Provider banking | Developers |
| `modules/analytics-personalization.md` | Analytics — personalization / recommendation | Developers |
| `modules/analytics-metrics-snapshots.md` | Analytics — metrics / snapshots / GDPR | Developers |
| `modules/messaging.md` | Messaging module ERD | Developers |
| `modules/tracking.md` | Tracking module ERD | Developers |
| `modules/social.md` | Social module ERD | Developers |
| `modules/contenttours-tours.md` | ContentTours — Tour aggregate | Developers |
| `modules/contenttours-guides.md` | ContentTours — Guide aggregate | Developers |
| `modules/contentseo.md` | ContentSeo module ERD | Developers |
| `modules/contentplaces.md` | ContentPlaces module ERD | Developers |
| `modules/contentcore.md` | ContentCore module ERD | Developers |
| `modules/contentblogs.md` | ContentBlogs module ERD | Developers |

---

## Legend

These symbols/conventions are used consistently across all diagrams.

| Notation | Meaning |
|----------|---------|
| **Solid line** (`\|\|--o{`, `\|\|--\|{`, etc.) | **Real database foreign key** (intra-module, enforced by EF) |
| **Dashed line** (`..`) or a `note` | **Logical cross-module reference** (plain `Guid`, **no DB FK**) |
| `PK` | Primary key |
| `FK` | Foreign key (real DB FK) |
| `LREF` | Logical reference column (no DB FK) |
| `◆ owned` | EF **owned type** / value object (`OwnsOne`/`OwnsMany`/`ToJson`) |
| `⊕ join` | Join / junction entity (many-to-many) |
| `🗑 soft-delete` | Has `IsDeleted`/`DeletedAt` **and** a `HasQueryFilter` |
| `🔒 rowversion` | Has a `RowVersion` concurrency token (`IsRowVersion()`) |
| `📸 snapshot` | Integration snapshot (local copy of another module's data) |

**Cardinality (Mermaid crow's foot):**

| Symbol | Meaning |
|--------|---------|
| `\|\|--\|\|` | one-to-one |
| `\|\|--o{` | one-to-(zero-or-many) |
| `\|\|--\|{` | one-to-(one-or-many) |
| `}o--o{` | many-to-many |

---

## Shared base classes (from `YallaJo.SharedKernel.Domain`)

| Base type | Key | Audit fields | Soft delete | Concurrency |
|-----------|-----|--------------|-------------|-------------|
| `BaseEntity<TKey>` / `BaseEntity` (Guid v7) | `Id` | `CreatedAt`, `UpdatedAt?` | ❌ | ❌ |
| `AuditableEntity<TKey>` / `AuditableEntity` | `Id` | `CreatedAt`, `UpdatedAt?` | `IsDeleted`, `DeletedAt?` (`ISoftDeletable`) | `byte[] RowVersion` (`[Timestamp]`) |
| `IAggregateRoot` | — | marker interface (DomainEvents) | — | — |

> `AuditableEntity` implements **`ISoftDeletable` only** — it does **not** implement
> `IAggregateRoot`. Aggregate roots opt in explicitly.

**Shared value objects:** `Money {Amount, Currency}`, `Location {Latitude, Longitude}`,
`DateRange {Start, End}`.

---

## DbContexts & schemas (14)

| DbContext | Schema | DbContext | Schema |
|-----------|--------|-----------|--------|
| `AccountsDbContext` | `accounts` | `TrackingDbContext` | `tracking` |
| `AuthDbContext` | `auth` | `SocialDbContext` | `social` |
| `SecurityDbContext` | `security` | `ContentToursDbContext` | `content_tours` |
| `BookingDbContext` | `booking` | `ContentSeoDbContext` | `content_seo` |
| `FinanceDbContext` | `finance` | `ContentPlacesDbContext` | `content_places` |
| `AnalyticsDbContext` | `analytics` | `ContentCoreDbContext` | `content_core` |
| `MessagingDbContext` | `messaging` | `ContentBlogsDbContext` | `content_blogs` |

Every schema also contains `OutboxMessage` (all 14) and `InboxMessage` (all except
Booking & Tracking, which carry Outbox only). These infra tables are **noted compactly**
in each module diagram rather than fully redrawn.

---

## Limitations

1. **Stale migration snapshots.** The committed EF `*DbContextModelSnapshot.cs` files are
   **out of date** relative to the configuration classes. The configurations define
   **106 `HasQueryFilter` calls**, but **no ModelSnapshot contains any `HasQueryFilter`**.
   Per the mission, the **Fluent API configurations are treated as the source of truth**;
   the snapshots were used only to confirm resolved schemas and convention-inferred FKs.
2. **Soft-delete columns vs. query filter.** A few `AuditableEntity` entities
   (`Messaging.Notification`, `Analytics.SuggestionBatch`, `ContentCore.Tag`) carry
   `IsDeleted`/`DeletedAt`/`RowVersion` columns but their configs do **not** declare a
   `HasQueryFilter` — so they are *physically* soft-delete-capable but not auto-filtered.
   These are marked accordingly.
3. **Check constraints & filtered indexes** that live only in migration SQL (not in
   configurations) are not exhaustively rendered; the notable XOR constraint
   (`CK_ProviderDocuments_SingleTarget`) is annotated.
4. **Cross-module relationships are logical only** (no DB FK) and are rendered as
   dashed/annotated links. See [`00-cross-module-logical-links.md`](00-cross-module-logical-links.md).
5. **Unclear items are annotated as unclear** — none were invented.
6. **Deferred entities** (present in `_Deferred/` folders, not mapped) are excluded:
   `Booking.Reservation`, `Booking.PackageBooking`, Finance subscription/loyalty/referral
   types, `Analytics.IngestDebounceMarker`, `Messaging.ChatBotConversation/ChatBotMessage`.
