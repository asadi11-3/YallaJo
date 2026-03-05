# SQL-to-Entities Migration — Architectural Learnings

## 2026-02-26 — Session ses_365cdafb7ffehsq6wgNF9Be3ht

### BASE CLASS HIERARCHY (confirmed from source)
- `BaseEntity<TKey>` → generic base (Id, CreatedAt, UpdatedAt, DomainEvents)
- `BaseEntity` → `BaseEntity<Guid>`, constructor: `Id = Guid.CreateVersion7()`
- `AuditableEntity<TKey>` → adds IsDeleted, DeletedAt, RowVersion (byte[] [Timestamp]), SoftDelete(), Restore(), MarkUpdated()
- `AuditableEntity` → `AuditableEntity<Guid>`, constructor: `Id = Guid.CreateVersion7()`
- `IAggregateRoot` — marker interface (inherits DomainEvents, ClearDomainEvents from BaseEntity)
- `ISoftDeletable` — interface (IsDeleted, DeletedAt, SoftDelete, Restore)

### ENTITY PRIVATE CONSTRUCTOR RULE
Every entity MUST have: `private {Entity}() { } // EF Core`

### DBCONTEXT INTERFACE
`IDbContext` lives in `YallaJo.SharedKernel.Application.Abstractions.Data` (NOT Infrastructure)
Import: `using YallaJo.SharedKernel.Application.Abstractions.Data;`

### UNIT OF WORK
`IUnitOfWork<TContext>` lives in `YallaJo.SharedKernel.Infrastructure.Data`
`UnitOfWork<TContext>` is the implementation — constructor-injected, primary constructor syntax
Register: `services.AddScoped<IUnitOfWork<{Module}DbContext>, UnitOfWork<{Module}DbContext>>();`

### OUTBOX MESSAGE
Lives in `YallaJo.SharedKernel.Infrastructure.Outbox.OutboxMessage`
Import: `using YallaJo.SharedKernel.Infrastructure.Outbox;`
DO NOT register OutboxProcessor for new modules (not yet wired)

### PACKAGE VERSIONS (confirmed)
- MediatR: 14.0.0
- Microsoft.EntityFrameworkCore.SqlServer: 9.0.13
- Microsoft.Extensions.Configuration: 9.0.2
- Microsoft.Extensions.Configuration.Json: 9.0.2
- Microsoft.Extensions.Configuration.EnvironmentVariables: 9.0.2
- FluentValidation: 12.1.1
- FluentValidation.DependencyInjectionExtensions: 12.1.1

### CRITICAL EF CORE RULES
1. ALL Guid PKs: `builder.Property(x => x.Id).ValueGeneratedNever()`
2. BIGINT IDENTITY ONLY (Analytics.AuditLog, Analytics.UserInteraction): `builder.Property(x => x.Id).ValueGeneratedOnAdd()`
3. Monetary: `HasPrecision(19, 4)`  | Rating: `HasPrecision(3, 2)` | Lat: `HasPrecision(10, 8)` | Lon: `HasPrecision(11, 8)`
4. varchar: ALWAYS `.IsUnicode(false)` + HasMaxLength
5. Enums: `.HasConversion<int>()`
6. AuditableEntity MUST have: `builder.HasQueryFilter(x => !x.IsDeleted)` + `builder.Property(x => x.RowVersion).IsRowVersion()`
7. BaseEntity must NOT have HasQueryFilter or IsRowVersion
8. Financial FKs: `OnDelete(DeleteBehavior.Restrict)` | Regular FKs: `OnDelete(DeleteBehavior.Cascade)`
9. Cross-module FK: just `builder.Property(x => x.ForeignId).IsRequired()` — NO HasOne, NO navigation property
10. junction table: `builder.HasKey(x => new { x.A, x.B })` — NO base class inheritance
11. SQL float columns → C# `double` + `.HasColumnType("float")`
12. varbinary(max) → C# `byte[]` + `.HasColumnType("varbinary(max)")`
13. TimeOnly: EF9 handles natively, no special config
14. DateOnly: EF9 handles natively, no special config

### SCHEMA NAMING DECISIONS
- ContentCore → schema: content_core
- ContentPlaces → schema: content_places
- ContentTours → schema: content_tours
- ContentBlogs → schema: content_blogs
- ContentSeo → schema: content_seo
- Analytics → schema: analytics
- Booking → schema: booking
- Finance → schema: finance
- Messaging → schema: messaging
- Social → schema: social
- Tracking → schema: tracking

### USEID TYPE
ALL UserId columns in ALL modules = `Guid` (not string). SQL DDL used nvarchar(450) for ASP.NET Identity but the codebase uses Guid PKs.

### SOLUTION FILE
Wave 1 tasks should NOT modify YallaJo.sln. Task 12 adds all projects to solution.
Build verification during Wave 1: `dotnet build {Module}.Infrastructure/{Module}.Infrastructure.csproj`

### NAMESPACE PATTERN
- Entities: `{Module}.Domain.Entities` (e.g., `ContentCore.Domain.Entities`)
- Enums: `{Module}.Domain.Enums`
- Configs: `{Module}.Infrastructure.Persistence.Configurations`
- DbContext: `{Module}.Infrastructure.Persistence`
- DI (Infra): `{Module}.Infrastructure`
- DI (App): `{Module}.Application`
- Endpoints: `{Module}.Presentation`

### 2026-02-26 — ContentCore module scaffolding learnings

- ContentCore schema is `content_core` across DbContext, table mappings, and migrations history table.
- Cross-module references (e.g., `LanguageId`, `UploadedByUserId`) remain plain `Guid` properties with no EF relationship.
- Junction entities (`EntityImage`, `EntityCategory`, `EntityTag`) must not inherit from base entities and must use composite keys.
- AuditableEntity configs require soft-delete query filter and row-version; BaseEntity configs must omit both.
- `OutboxMessageConfiguration` should be copied as-is except table schema/name adjustments; do not register `OutboxProcessor` in this wave.

### 2026-02-26 — ContentBlogs module scaffolding learnings

- ContentBlogs project references mirror ContentCore/Security patterns exactly: Domain->SharedKernel.Domain, Contracts->SharedKernel.Domain, Application->Domain+Contracts+SharedKernel.Application, Infrastructure->Application+Domain+Contracts+SharedKernel.Infrastructure, Presentation->Application + AspNetCore framework ref.
- `ContentBlogsDbContext` follows module convention: `HasDefaultSchema("content_blogs")`, filtered `ApplyConfigurationsFromAssembly`, migrations history table schema set in infrastructure DI.
- Cross-module references (`AuthorId`, `LanguageId`, `TourId`, `UserId`) remain required scalar Guid properties with no navigation/HasOne mapping.
- `BlogTour` is a pure junction entity (no base class) with composite key `{ BlogId, TourId }` and default `SortOrder`.
- Auditable entities (`Blog`, `BlogComment`) require both `HasQueryFilter(x => !x.IsDeleted)` and `RowVersion.IsRowVersion()`; base entities (`BlogTranslation`, `BlogCommentReaction`) intentionally omit both.

### 2026-02-26 — ContentSeo module scaffolding learnings

- `SeoEntityType` columns are configured with `.HasConversion<int>()` even though enum backing type is `byte`.
- `SeoMetadata` requires a unique composite index on `{ EntityType, EntityId }` and `SitemapPriority` precision/default via `.HasPrecision(2, 1).HasDefaultValue(0.5m)`.
- `WeatherCache` keeps cross-module `PlaceId` as required scalar Guid only; no navigation and no `HasOne` mapping.
- FAQ split follows module conventions: `FaqItem` is auditable with query filter + row version, while `FaqItemTranslation` is base entity without soft-delete concurrency settings.
- DbContext/DI conventions remain strict: `HasDefaultSchema("content_seo")`, filtered `ApplyConfigurationsFromAssembly`, migrations history in `content_seo`, and no OutboxProcessor registration.

### 2026-02-26 — ContentTours module scaffolding learnings

- ContentTours follows the same 5-project scaffolding as ContentCore with identical package versions and reference topology.
- `ContentToursDbContext` uses `HasDefaultSchema("content_tours")` plus namespace-filtered configuration scanning under `ContentTours.Infrastructure.Persistence.Configurations`.
- `Difficulty` and `TourStatus` are persisted with `.HasConversion<int>()` even though enums use `byte` backing type.
- `Tour` and `TourPackage` are auditable and must include both `HasQueryFilter(x => !x.IsDeleted)` and `RowVersion.IsRowVersion()`; all other tour entities are base/no-filter.
- Cross-module identifiers (`CreatedByUserId`, `LanguageId`, `TourGuideId`) stay scalar required `Guid` properties without cross-module navigation mappings.

### 2026-02-26 — ContentPlaces module scaffolding learnings

- `ContentPlaces` follows the same 5-project reference graph as `ContentCore`, with identical package versions for MediatR, EF SqlServer, FluentValidation, and config packages.
- `Place` and `Business` are the only auditable aggregates in this module; both require `HasQueryFilter(x => !x.IsDeleted)` and `RowVersion.IsRowVersion()`.
- Translation and schedule entities (`PlaceTranslation`, `BusinessTranslation`, `BusinessHours`, `AccessibilityFeature`) inherit `BaseEntity` and should keep only `CreatedAt/UpdatedAt` mappings (no soft delete or row version).
- All cross-module ids (`LanguageId`, `OwnerId`, `CreatedByUserId`) stay scalar Guid properties with `builder.Property(...).IsRequired()` and no navigation/HasOne mapping.
- `PlaceBusiness` is a pure junction entity with composite key `{ PlaceId, BusinessId }`, and enum columns (`PlaceType`, `BusinessType`, `DayOfWeek`, `AccessibilityFeatureType`) use `.HasConversion<int>()`.

### 2026-02-26 � Analytics module scaffolding learnings

-  and  must inherit  and use  for BIGINT IDENTITY PKs; never assign Guid ids in constructors.
- Append-only analytics entities (, ) intentionally omit soft-delete query filters and row-version concurrency tokens.
-  is a pure junction entity with composite key , precision , and default score .
-  is configured with  despite enum backing type .
-  keeps a descending composite index  and remains  without soft-delete metadata.

### 2026-02-26 - Analytics module scaffolding learnings (corrected)

- AuditLog and UserInteraction inherit BaseEntity<long> and are configured with ValueGeneratedOnAdd for BIGINT IDENTITY primary keys.
- Append-only analytics entities (AuditLog, UserInteraction) intentionally do not use soft-delete query filters or row-version concurrency tokens.
- UserPreferredCategory is a pure junction entity with composite key { UserId, CategoryId }, precision (5,4), and default score 0.
- InteractionType is stored with HasConversion<int> although the enum backing type is byte.
- RecommendationCache uses descending composite index (UserId, EntityType, Score) and remains a BaseEntity type without soft-delete fields.

### 2026-02-26 - Tracking module scaffolding learnings

- Tracking follows the standard 5-project module topology with the same package versions and reference graph as ContentCore.
- TrackingDbContext uses default schema `tracking`, filtered assembly config scan for `Tracking.Infrastructure.Persistence.Configurations`, and migrations history table in `tracking`.
- `LocationSnapshot` keeps telemetry numeric fields as C# `double` and maps SQL `float` explicitly via `.HasColumnType("float")`.
- Cross-module ids (`TourBookingId`, `TourGuideId`, `WaypointId`) remain scalar required Guid properties with no cross-module navigation or HasOne mapping.
- Auditable entities (`LiveTrackingSession`, `TourCheckpoint`) require both soft-delete query filters and row-version concurrency, while `LocationSnapshot` intentionally has neither.

### 2026-02-26 - Social module scaffolding learnings

- Social follows the same 5-project module topology and package/reference graph as ContentCore/Security.
- `SocialDbContext` uses `HasDefaultSchema("social")`, filtered `ApplyConfigurationsFromAssembly`, and social-scoped migrations history table.
- Auditable entities in Social (`Review`, `Report`, `AccessibilityReview`) require both `HasQueryFilter(x => !x.IsDeleted)` and `builder.Property(x => x.RowVersion).IsRowVersion()`.
- Base entities (`Favorite`, `ContentModerationLog`) intentionally omit soft-delete query filters and row-version settings.
- Cross-module identifiers (`UserId`, `PlaceId`, `TourId`, `TourGuideId`, `BusinessId`) remain scalar Guid properties only with no cross-module `HasOne` navigation mapping.

### 2026-02-26 - Finance module scaffolding learnings

- Finance follows the same five-project scaffold and package/reference graph as ContentCore/Security, with no solution-file updates in this wave.
- Financial FK policy is strict: all Finance relationships use `OnDelete(DeleteBehavior.Restrict)` (including junction and line-item tables) for GAAP compliance.
- Every Finance enum column is persisted with `.HasConversion<int>()` even when enum backing type is `byte`.
- Auditable Finance entities require both `HasQueryFilter(x => !x.IsDeleted)` and `RowVersion.IsRowVersion()`, while `BaseEntity`/junction entities intentionally omit both.
- `FinanceDbContext` must use schema `finance`, filtered configuration assembly scanning, and finance-scoped migrations history table.

### 2026-02-26 - Messaging module scaffolding learnings

- Messaging follows the standard 5-project module reference graph and package versions used by ContentCore/Security.
- Notification is append-only and correctly modeled on BaseEntity (no soft-delete filter, no row-version), while NotificationPreference, NotificationTemplate, DeviceToken, SupportTicket, and ChatBotConversation are auditable with query filter and row version.
- All cross-module user references (UserId, AssignedToUserId, SenderUserId) remain scalar Guid properties with no HasOne navigation to Security.User.
- Enum columns are consistently persisted with HasConversion<int>() even when enums use byte backing type.
- Messaging DbContext and migrations history use schema messaging, and infrastructure DI registers DbContext + IUnitOfWork<MessagingDbContext> + MediatR only (no OutboxProcessor).


### 2026-02-26 - Code Quality Review (Task F2) findings

- Full solution build PASSES with 0 errors, 0 warnings.
- All entities sampled (~50): private EF Core constructor present on 100% of reviewed entities.
- AuditableEntity vs BaseEntity inheritance correctly applied across all modules.
- Junction tables (TourTourGuide, EntityTag, EntityCategory, UserPreferredCategory, TourGuideLanguage, TourGuideSpecialization) have NO base class - correct.
- BIGINT PK entities (Analytics.AuditLog, Analytics.UserInteraction): ValueGeneratedOnAdd(), no HasQueryFilter, no IsRowVersion - correct.
- Finance module: ALL 15 FK relationships use DeleteBehavior.Restrict.
- Tracking.LocationSnapshot: double fields (Accuracy, Speed, Heading, Altitude) use HasColumnType(float). Lat/Lon use decimal with HasPrecision.
- Social.Review: all 4 factory methods present (CreateForPlace, CreateForTour, CreateForTourGuide, CreateForBusiness).
- Booking.AvailabilitySlot: both factory methods present (CreateForTour, CreateForBusiness).
- HasQueryFilter: 42 configs with it - all correspond to AuditableEntity types. BaseEntity configs correctly omit it.
- IsRowVersion: 55 configs with it - all correspond to AuditableEntity types.
- All Guid PKs use ValueGeneratedNever() consistently.
- Naming conventions (DbContext, Add*Application, Add*Infrastructure, Map*Endpoints) 100% correct.
- Namespace conventions correct throughout.
- Security.AuditLog is BaseEntity (Guid PK), distinct from Analytics.AuditLog (BaseEntity<long>) - both correct.