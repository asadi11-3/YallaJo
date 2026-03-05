# SQL-to-Entities Migration — Decisions Log

## 2026-02-26 — ContentSeo module (Task 5)

- Matched ContentCore/Security scaffolding exactly for all five ContentSeo project files and DI wiring patterns.
- Kept cross-module references (`PlaceId`, `LanguageId`) as scalar Guid properties with no navigation/relationship mapping.
- Applied soft-delete/query-filter/row-version only on auditable entities (`SeoMetadata`, `FaqItem`), not base entities.

## 2026-02-26 — ContentTours module (Task 3)

- Kept `TourTourGuide` as a pure junction entity (no base class) with composite key `{ TourId, TourGuideId }` and no navigation to external Booking aggregate.
- Configured cross-module fields (`CreatedByUserId`, `LanguageId`, `TourGuideId`) as required scalar properties only, avoiding `HasOne` relationships.
- Mirrored ContentCore DI conventions exactly: register DbContext, `IUnitOfWork<ContentToursDbContext>`, and MediatR, with no OutboxProcessor hosted service registration.

## 2026-02-26 � Analytics module decisions

- Adopted schema name  consistently in table mappings, DbContext default schema, and migrations history table.
- Kept cross-module references (, , ) as scalar Guid properties only; no navigation mappings or  relationships.
- Registered only DbContext, , and MediatR in infrastructure DI; intentionally excluded OutboxProcessor hosted service for Wave 1.

## 2026-02-26 - Analytics module decisions (corrected)

- Adopted analytics as the schema name consistently in table mappings, DbContext default schema, and migrations history table.
- Kept cross-module references (UserId, CategoryId, EntityId) as scalar Guid properties only, with no relationship navigation mapping.
- Registered DbContext, IUnitOfWork<AnalyticsDbContext>, and MediatR in infrastructure DI, and excluded OutboxProcessor hosted service in this wave.

## 2026-02-26 - Social module (Task 10)

- Used Social aggregate factory methods on `Review` for target selection (place/tour/tour-guide/business) and avoided DB-level XOR check constraints per task guidance.
- Stored all Social enum columns with `.HasConversion<int>()` (including default `ReportStatus.Pending`) to match established module conventions.
- Registered Social infrastructure with DbContext + `IUnitOfWork<SocialDbContext>` + MediatR only; intentionally excluded OutboxProcessor hosted service.

## 2026-02-26 - Finance module (Task 8)

- Adopted `finance` schema consistently across DbContext default schema, all table mappings, and migrations history table configuration.
- Kept all cross-module foreign references (`UserId`, `BookingId`, `ReservationId`, etc.) as scalar Guid properties only with no cross-module navigation mapping.
- Enforced GAAP delete behavior by configuring every Finance FK relationship with `OnDelete(DeleteBehavior.Restrict)` and excluded OutboxProcessor registration from infrastructure DI.

## 2026-02-26 - Messaging module (Task 9)

- Kept messaging schema naming consistent across table mappings, `MessagingDbContext.HasDefaultSchema("messaging")`, and migrations history table schema.
- Implemented Notification as BaseEntity to preserve append-only behavior; reserved soft-delete/query-filter/row-version for auditable entities only.
- Modeled all external user references as logical Guid fields without cross-module foreign key navigation mappings.
