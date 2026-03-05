# SQL DDL to C# Entity Models Migration

## TL;DR

> **Quick Summary**: Convert a massive SQL Server DDL script (84 tables across 8 schemas) into C# entity models organized as 11 new modules in the YallaJo Modular Monolith. Each module gets 5 Clean Architecture layers (Domain, Application, Infrastructure, Contracts, Presentation) with entities, Fluent API configurations, DbContext, and DI wiring. Existing Auth, Accounts, and Security modules remain untouched.
>
> **Deliverables**:
> - 11 new modules × 5 layers = 55 new .csproj projects
> - ~84 entity classes with proper base class inheritance
> - ~84 Fluent API configuration classes
> - 11 DbContext classes with DbSet properties
> - 11 DI registration extensions
> - Program.cs wired with all 11 new modules
> - Solution file updated with proper folder structure
>
> **Estimated Effort**: XL (84 entities, 55 projects, ~250+ files)
> **Parallel Execution**: YES — 5 waves
> **Critical Path**: Wave 1 scaffolding → Wave 2 Content.Core → Wave 3 Content.* + Booking → Wave 4 remaining modules → Wave 5 final verification

---

## Context

### Original Request
Convert a massive SQL Server V2 DDL script (~84 tables across Analytics, Booking, Content, Finance, Messaging, Social, Tracking schemas) into C# entity models. The existing Auth, Accounts, and Security modules must NOT be modified. Tables already in those modules (Security schema: Users, Roles, UserRoles, RoleClaims, UserClaims, Emails, Phones, AuditLog; Auth schema: Sessions, Devices, RefreshTokens, Otps, ExternalProviders; Accounts: Profiles) must be excluded.

### Interview Summary
**Key Discussions**:
- **AuditLog**: Security module has its own AuditLog. Analytics.AuditLog has DIFFERENT columns (UserAgent, EntityId as nvarchar, OldValues/NewValues) serving application-level events → CREATE separate Analytics.AuditLog
- **Module layers**: All 5 layers per module (Domain, Application, Infrastructure, Contracts, Presentation) — matches existing pattern
- **Cross-module FKs**: Logical references only — plain `Guid UserId` with NO navigation property to Security.User
- **ID generation**: Domain-generated — `Guid.CreateVersion7()` in constructor, `ValueGeneratedNever()` in config
- **Junction tables**: Composite PK via `HasKey(x => new { })` — these won't inherit from BaseEntity
- **Content module**: SPLIT into sub-modules with separate schemas — Content.Core, Content.Places, Content.Tours, Content.Blogs, Content.SEO
- **Test strategy**: Build verification (`dotnet build`) + LSP diagnostics only — no unit test framework

**Research Findings**:
- Codebase uses Modular Monolith with Clean Architecture (5 layers per module)
- `BaseEntity<TKey>` → `AuditableEntity<TKey>` with `ISoftDeletable`, `IAggregateRoot`
- `Guid.CreateVersion7()` for ID generation, `ValueGeneratedNever()` in config
- Soft delete with `HasQueryFilter`, optimistic concurrency with `RowVersion [Timestamp]`
- Outbox pattern: `OutboxMessage` in each DbContext, `OutboxProcessor<TContext>` hosted service
- Namespace: `{Module}.Domain.Entities`, `{Module}.Infrastructure.Persistence.Configurations`
- Each module's Infrastructure DI: `Add{Module}Infrastructure(IConfiguration)` registers DbContext, UnitOfWork, repositories
- Each module's Application DI: `Add{Module}Application()` registers MediatR + FluentValidation

### Metis Review
**Identified Gaps** (addressed):
- **UserId type mismatch**: DDL uses `nvarchar(450)` (ASP.NET Identity), codebase uses `Guid` → ALL UserId columns become `Guid` in C#
- **Composite PK strategy**: 11 junction tables have composite PKs → User chose composite PK approach (not surrogate Guid)
- **Content module size**: 35 tables → User chose to split into 5 sub-modules
- **Analytics.AuditLog conflict**: Different columns from Security.AuditLog → User chose to create separate entity
- **BIGINT IDENTITY entities**: Analytics.AuditLogs + UserInteractions use `long` PK with `ValueGeneratedOnAdd()`
- **Decimal precision varies**: `(19,4)` monetary, `(5,2)` percentage, `(3,2)` rating, `(10,8)` latitude, `(11,8)` longitude — each needs explicit `HasPrecision`
- **Enum strategy**: tinyint in DDL → C# enums with `HasConversion<int>()` in config
- **Value objects**: NO `OwnsOne` — use plain properties (decimal Price + string Currency)
- **Translation tables**: Inherit `BaseEntity` not `AuditableEntity` (no soft delete in DDL)
- **varchar vs nvarchar**: Use `IsUnicode(false)` for `varchar` columns (Currency codes, language codes)
- **Time/Date types**: `time(7)` → `TimeOnly`, `date` → `DateOnly`

---

## Work Objectives

### Core Objective
Create 11 new C# modules (55 .csproj projects) containing ~84 entity models with Fluent API configurations, translating SQL DDL schemas into the established Clean Architecture patterns of the YallaJo Modular Monolith.

### Concrete Deliverables
- 55 new .csproj projects (11 modules × 5 layers)
- ~84 entity classes in `{Module}.Domain/Entities/`
- ~30+ enum types in `{Module}.Domain/Enums/`
- ~84 entity configuration classes in `{Module}.Infrastructure/Persistence/Configurations/`
- 11 `OutboxMessageConfiguration` classes (one per module)
- 11 DbContext classes with DbSets
- 11 Infrastructure `DependencyInjection.cs` files
- 11 Application `DependencyInjection.cs` stub files
- 11 Presentation `{Module}Endpoints.cs` stub files
- Updated `YallaJo.Api.csproj` with 22 new project references (11 Presentation + 11 Infrastructure)
- Updated `Program.cs` with 11 new module registrations
- Updated `YallaJo.sln` with 55 new projects in solution folders

### Definition of Done
- [ ] `dotnet build YallaJo.sln --configuration Release` succeeds with 0 errors
- [ ] LSP diagnostics show 0 errors across all new files
- [ ] `git diff --name-only -- Auth.* Accounts.* Security.* YallaJo.SharedKernel.*` returns empty (no modifications to existing modules)
- [ ] All 55 new projects listed in `YallaJo.sln`
- [ ] Program.cs registers all 11 new modules

### Must Have
- Every entity from the SQL DDL (except those in Auth/Accounts/Security) has a corresponding C# class
- Every entity has a matching `IEntityTypeConfiguration<T>` class
- Every module has its own DbContext with proper schema
- Cross-module references are LOGICAL (plain Guid, no navigation properties, no FK constraints)
- Composite PK junction tables use `HasKey(x => new { })` without inheriting from BaseEntity
- All monetary columns use `HasPrecision(19, 4)`
- All enum columns use `HasConversion<int>()`
- Soft-deletable entities have `HasQueryFilter(x => !x.IsDeleted)`
- Auditable entities have `RowVersion` configured as `IsRowVersion()`
- All entities follow the private constructor + factory method pattern

### Must NOT Have (Guardrails)
- **DO NOT** modify ANY file in Auth.*, Accounts.*, Security.*, YallaJo.SharedKernel.* (except YallaJo.Api references)
- **DO NOT** create EF migrations (`dotnet ef migrations add`)
- **DO NOT** add domain logic beyond constructor/factory methods and soft-delete/restore
- **DO NOT** add navigation properties for cross-module FK references (e.g., UserId → Security.User)
- **DO NOT** use `OwnsOne<Money>()` or `OwnsOne<Location>()` — use plain properties
- **DO NOT** register `OutboxProcessor<TContext>` as HostedService for new modules (register only DbContext, UnitOfWork, MediatR)
- **DO NOT** create commands, queries, handlers, validators, or endpoint implementations
- **DO NOT** use TPH/TPT inheritance for polymorphic tables — use single entity class with enum discriminator
- **DO NOT** use `HasCheckConstraint()` — enforce constraints in domain factory methods
- **DO NOT** add JSDoc-style XML comments or excessive documentation
- **DO NOT** create repository interfaces (only IUnitOfWork pattern)

---

## Verification Strategy

> **ZERO HUMAN INTERVENTION** — ALL verification is agent-executed. No exceptions.

### Test Decision
- **Infrastructure exists**: NO (no test projects)
- **Automated tests**: None — build verification + LSP diagnostics only
- **Framework**: N/A

### QA Policy
Every task uses build verification + LSP diagnostics as primary QA:
- `dotnet build YallaJo.sln` after each module
- LSP diagnostics on all new `.cs` files
- File count verification per module
- Evidence saved to `.sisyphus/evidence/task-{N}-{scenario-slug}.{ext}`

---

## Execution Strategy

### Module → Schema Mapping

| Module | Schema | Entities | Key Notes |
|--------|--------|----------|-----------|
| Content.Core | `content_core` | ~12 | Languages, Categories, Tags, Attachments, EntityImages, EntityCategories, EntityTags, Specializations, Translations |
| Content.Places | `content_places` | ~6 | Places, PlaceTranslations, Businesses, BusinessTranslations, BusinessHours, PlaceBusinesses, AccessibilityFeatures |
| Content.Tours | `content_tours` | ~8 | Tours, TourTranslations, TourSchedules, TourWaypoints, TourPricingTiers, TourTourGuides, TourPackages, TourPackageInclusions |
| Content.Blogs | `content_blogs` | ~5 | Blogs, BlogTranslations, BlogTours, BlogComments, BlogCommentReactions |
| Content.SEO | `content_seo` | ~4 | SeoMetadata, WeatherCache, FaqItems, FaqItemTranslations |
| Analytics | `analytics` | 6 | AuditLogs (BIGINT PK), UserInteractions (BIGINT PK), UserPreferences, UserPreferredCategories, PopularityScores, RecommendationCache |
| Booking | `booking` | 11 | TourGuides, TourGuideLanguages, TourGuideSpecializations, AvailabilitySlots, TourBookings, PackageBookings, Reservations, JoinRequests, SlotLocks, RefundPolicies, ProviderDocuments |
| Finance | `finance` | 16 | Payments, Payouts, PayoutItems, Disputes, CommissionRules, Subscriptions, SubscriptionPlans, SubscriptionFeatures, PlanFeatures, LoyaltyPoints, LoyaltyTransactions, Referrals, Discounts, DiscountUsages, InvoiceItems, InvoiceLineItems |
| Messaging | `messaging` | 8 | Notifications, NotificationPreferences, NotificationTemplates, DeviceTokens, SupportTickets, TicketMessages, ChatBotConversations, ChatBotMessages |
| Social | `social` | 5 | Reviews, Favorites, Reports, ContentModerationLogs, AccessibilityReviews |
| Tracking | `tracking` | 3 | LiveTrackingSessions, LocationSnapshots, TourCheckpoints |

### Parallel Execution Waves

```
Wave 1 (Foundation — ALL modules scaffolded in parallel):
├── Task 1: Content.Core — 5 projects + entities + configs + DbContext [deep]
├── Task 2: Content.Places — 5 projects + entities + configs + DbContext [deep]
├── Task 3: Content.Tours — 5 projects + entities + configs + DbContext [deep]
├── Task 4: Content.Blogs — 5 projects + entities + configs + DbContext [deep]
├── Task 5: Content.SEO — 5 projects + entities + configs + DbContext [deep]
├── Task 6: Analytics — 5 projects + entities + configs + DbContext [deep]
├── Task 7: Booking — 5 projects + entities + configs + DbContext [deep]
├── Task 8: Finance — 5 projects + entities + configs + DbContext [deep]
├── Task 9: Messaging — 5 projects + entities + configs + DbContext [deep]
├── Task 10: Social — 5 projects + entities + configs + DbContext [deep]
└── Task 11: Tracking — 5 projects + entities + configs + DbContext [deep]

Wave 2 (Integration — wiring into API host):
└── Task 12: Wire all 11 modules into YallaJo.Api (Program.cs + .csproj + .sln) [quick]

Wave 3 (Verification):
└── Task 13: Full build verification + LSP diagnostics + file count audit [deep]

Wave FINAL (After ALL tasks — independent review, 4 parallel):
├── Task F1: Plan compliance audit (oracle)
├── Task F2: Code quality review (unspecified-high)
├── Task F3: Real manual QA (unspecified-high)
└── Task F4: Scope fidelity check (deep)

Critical Path: Tasks 1-11 (parallel) → Task 12 → Task 13 → F1-F4
Parallel Speedup: ~85% faster than sequential (11 modules built simultaneously)
Max Concurrent: 11 (Wave 1)
```

### Dependency Matrix

| Task | Depends On | Blocks | Wave |
|------|-----------|--------|------|
| 1 (Content.Core) | — | 12 | 1 |
| 2 (Content.Places) | — | 12 | 1 |
| 3 (Content.Tours) | — | 12 | 1 |
| 4 (Content.Blogs) | — | 12 | 1 |
| 5 (Content.SEO) | — | 12 | 1 |
| 6 (Analytics) | — | 12 | 1 |
| 7 (Booking) | — | 12 | 1 |
| 8 (Finance) | — | 12 | 1 |
| 9 (Messaging) | — | 12 | 1 |
| 10 (Social) | — | 12 | 1 |
| 11 (Tracking) | — | 12 | 1 |
| 12 (API Wiring) | 1-11 | 13 | 2 |
| 13 (Build Verify) | 12 | F1-F4 | 3 |
| F1-F4 (Reviews) | 13 | — | FINAL |

### Agent Dispatch Summary

- **Wave 1**: **11 tasks** — All `deep` category (each creates 5 projects, multiple entities, configs, DbContext, DI)
- **Wave 2**: **1 task** — `quick` (wire Program.cs + .csproj + .sln)
- **Wave 3**: **1 task** — `deep` (full build + LSP + audit)
- **Wave FINAL**: **4 tasks** — F1 `oracle`, F2 `unspecified-high`, F3 `unspecified-high`, F4 `deep`

---

## TODOs


- [ ] 1. Content.Core Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Content.Core module with 5 .csproj projects following the established pattern. This module owns shared content building blocks: Languages, Categories, Tags, Attachments, Specializations, and the polymorphic V2 tables (EntityImages, EntityCategories, EntityTags). Also includes translation entities for Categories.

  1. Create 5 projects: `ContentCore.Domain`, `ContentCore.Application`, `ContentCore.Infrastructure`, `ContentCore.Contracts`, `ContentCore.Presentation`
  2. Add project references matching existing pattern:
     - Domain → SharedKernel.Domain
     - Application → Domain, Contracts, SharedKernel.Application (+ MediatR, FluentValidation)
     - Infrastructure → Application, Domain, Contracts, SharedKernel.Infrastructure (+ MediatR, EF Core SqlServer, Config packages)
     - Contracts → SharedKernel.Domain
     - Presentation → Application (+ FrameworkReference Microsoft.AspNetCore.App)
  3. Create enums in `ContentCore.Domain/Enums/`:
     - `EntityType` : byte { Place = 0, Tour = 1, Business = 2, Review = 3, Blog = 4, TourGuide = 5 }
     - `AttachmentType` : byte { Image = 0, Video = 1, Document = 2, Audio = 3 }
     - `ImageSize` : byte { Thumbnail = 0, Small = 1, Medium = 2, Large = 3, Original = 4 }
  4. Create entities in `ContentCore.Domain/Entities/`:
     - `Language` : AuditableEntity — Id (Guid), Code (string max 10), Name (string max 100), NativeName (string max 100), IsRtl (bool), IsActive (bool). Collections: CategoryTranslations, PlaceTranslations, TourTranslations, BlogTranslations, etc. (navigation from other modules — do NOT add these, they belong to their respective modules)
     - `Category` : AuditableEntity, IAggregateRoot — Id (Guid), ParentCategoryId (Guid?), Name (string max 200), Slug (string max 200), Icon (string? max 100), SortOrder (int), IsActive (bool). Self-referencing: ParentCategory, SubCategories collection. Child: CategoryTranslations collection
     - `CategoryTranslation` : BaseEntity — Id (Guid), CategoryId (Guid FK), LanguageId (Guid FK), Name (string max 200), Slug (string max 200). Navigation: Category, Language
     - `Tag` : BaseEntity — Id (Guid), Name (string max 100), Slug (string max 100), IsActive (bool default true)
     - `Attachment` : BaseEntity — Id (Guid), EntityType (EntityType enum), EntityId (Guid), Type (AttachmentType enum), Url (string max 2048), ThumbnailUrl (string? max 2048), OriginalFileName (string? max 500), MimeType (string? max 100), FileSize (long?), Width (int?), Height (int?), DurationSeconds (int?), SortOrder (int default 0), Iv (byte[]?), Hmac (byte[]?), UploadedAt (DateTime), UploadedByUserId (Guid)
     - `EntityImage` : no base class (junction) — EntityType (EntityType enum), EntityId (Guid), AttachmentId (Guid), ImageSize (ImageSize enum), SortOrder (int default 0), IsPrimary (bool default false). Composite PK: (EntityType, EntityId, AttachmentId, ImageSize). Navigation: Attachment
     - `EntityCategory` : no base class (junction) — EntityType (EntityType enum), EntityId (Guid), CategoryId (Guid). Composite PK: (EntityType, EntityId, CategoryId). Navigation: Category
     - `EntityTag` : no base class (junction) — EntityType (EntityType enum), EntityId (Guid), TagId (Guid). Composite PK: (EntityType, EntityId, TagId). Navigation: Tag
     - `Specialization` : AuditableEntity — Id (Guid), Name (string max 200), Description (string? max 1000), Icon (string? max 100), IsActive (bool default true)
  5. Create EF configurations in `ContentCore.Infrastructure/Persistence/Configurations/` for each entity + OutboxMessageConfiguration
  6. Create `ContentCoreDbContext` with schema `content_core`, all DbSets + OutboxMessages
  7. Create `ContentCore.Infrastructure/DependencyInjection.cs` — register DbContext, UnitOfWork, MediatR (NO OutboxProcessor hosted service)
  8. Create `ContentCore.Application/DependencyInjection.cs` — register MediatR + FluentValidation from assembly
  9. Create `ContentCore.Presentation/ContentCoreEndpoints.cs` — empty stub with MapContentCoreEndpoints extension method

  **Must NOT do**:
  - Do NOT add navigation properties to entities in other modules (Language is referenced by many, but don't add collections for cross-module entities)
  - Do NOT use OwnsOne for any property
  - Do NOT create repository interfaces
  - Do NOT register OutboxProcessor hosted service
  - Do NOT create migrations

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Creates 5 projects, 9+ entity classes, 9+ configuration classes, DbContext, DI wiring — requires deep understanding of EF Core Fluent API patterns and cross-referencing SQL DDL with C# conventions
  - **Skills**: []
  - **Skills Evaluated but Omitted**:
    - `playwright`: No browser interaction needed
    - `frontend-ui-ux`: No UI work

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 2-11)
  - **Blocks**: Task 12 (API wiring)
  - **Blocked By**: None (can start immediately)

  **References** (CRITICAL):

  **Pattern References** (existing code to follow):
  - `Security.Domain/Entities/User.cs` — Aggregate root with collections pattern (private List, public IReadOnlyCollection, private constructor)
  - `Security.Domain/Entities/Email.cs` — Child entity with FK navigation property pattern
  - `Security.Domain/Entities/AuditLog.cs` — BaseEntity (not AuditableEntity) for append-only entities
  - `Security.Infrastructure/Persistence/Configurations/UserConfiguration.cs` — Fluent API pattern: ToTable, HasKey, ValueGeneratedNever, property configs, relationships, HasQueryFilter, indexes
  - `Security.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs` — Config for non-auditable entities (no HasQueryFilter, no RowVersion)
  - `Security.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs` — Join entity config with composite unique index
  - `Security.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs` — Outbox message config pattern to copy exactly
  - `Security.Infrastructure/Persistence/SecurityDbContext.cs` — DbContext pattern: HasDefaultSchema, ApplyConfigurationsFromAssembly with namespace filter
  - `Security.Infrastructure/DependencyInjection.cs` — Infrastructure DI: AddDbContext, AddScoped UnitOfWork, AddMediatR, NO OutboxProcessor for new modules
  - `Security.Application/DependencyInjection.cs` — Application DI: AddMediatR + AddValidatorsFromAssembly
  - `Security.Presentation/SecurityEndpoints.cs` — Endpoint stub pattern: static class with Map{Module}Endpoints extension

  **API/Type References** (contracts to implement against):
  - `YallaJo.SharedKernel.Domain/Entities/BaseEntity.cs` — Base class for non-auditable entities (Guid PK via CreateVersion7)
  - `YallaJo.SharedKernel.Domain/Entities/AuditableEntity.cs` — Base class for auditable entities (adds IsDeleted, DeletedAt, RowVersion, SoftDelete/Restore)
  - `YallaJo.SharedKernel.Domain/Entities/IAggregateRoot.cs` — Interface for aggregate root entities
  - `YallaJo.SharedKernel.Domain/Entities/ISoftDeletable.cs` — Interface for soft delete
  - `YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — Outbox message entity class
  - `YallaJo.SharedKernel.Application/Abstractions/Data/IDbContext.cs` — DbContext interface
  - `YallaJo.SharedKernel.Infrastructure/Data/UnitOfWork.cs` — UnitOfWork generic class
  - `YallaJo.SharedKernel.Domain/Abstractions/Data/IUnitOfWork.cs` — UnitOfWork interface

  **External References**:
  - SQL DDL: `[Content].[Languages]`, `[Content].[Categories]`, `[Content].[CategoryTranslations]`, `[Content].[Tags]`, `[Content].[Attachments]`, `[Content].[EntityImages]`, `[Content].[EntityCategories]`, `[Content].[EntityTags]`, `[Content].[Specializations]` table definitions

  **csproj References** (project file patterns to follow):
  - `Security.Domain/Security.Domain.csproj` — Domain project pattern: net9.0, reference SharedKernel.Domain only
  - `Security.Application/Security.Application.csproj` — Application project: FluentValidation + MediatR + reference Domain, Contracts, SharedKernel.Application
  - `Security.Infrastructure/Security.Infrastructure.csproj` — Infrastructure project: MediatR + EF Core SqlServer + Config packages + reference Application, Contracts, Domain, SharedKernel.Infrastructure
  - `Security.Contracts/Security.Contracts.csproj` — Contracts project: reference SharedKernel.Domain only
  - `Security.Presentation/Security.Presentation.csproj` — Presentation project: FrameworkReference Microsoft.AspNetCore.App + reference Application

  **Acceptance Criteria**:
  - [ ] 5 .csproj files exist: ContentCore.Domain, ContentCore.Application, ContentCore.Infrastructure, ContentCore.Contracts, ContentCore.Presentation
  - [ ] 9 entity .cs files in ContentCore.Domain/Entities/
  - [ ] 3 enum .cs files in ContentCore.Domain/Enums/
  - [ ] 10 configuration .cs files in ContentCore.Infrastructure/Persistence/Configurations/ (9 entities + 1 OutboxMessage)
  - [ ] ContentCoreDbContext.cs exists with schema `content_core` and correct DbSets
  - [ ] DependencyInjection.cs in both Infrastructure and Application
  - [ ] ContentCoreEndpoints.cs stub in Presentation
  - [ ] All project references correct (Domain→SharedKernel.Domain, etc.)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles independently
    Tool: Bash
    Preconditions: All 5 projects created with correct references
    Steps:
      1. Run: dotnet build ContentCore.Domain/ContentCore.Domain.csproj
      2. Run: dotnet build ContentCore.Infrastructure/ContentCore.Infrastructure.csproj
    Expected Result: Both build with 0 errors, 0 warnings
    Failure Indicators: CS0246 (type not found), CS0234 (namespace missing), any compilation error
    Evidence: .sisyphus/evidence/task-1-build.txt

  Scenario: Entity count matches spec
    Tool: Bash (PowerShell)
    Preconditions: Entity files created
    Steps:
      1. Run: Get-ChildItem -Path ContentCore.Domain/Entities -Filter *.cs | Measure-Object
      2. Run: Get-ChildItem -Path ContentCore.Domain/Enums -Filter *.cs | Measure-Object
      3. Run: Get-ChildItem -Path ContentCore.Infrastructure/Persistence/Configurations -Filter *.cs | Measure-Object
    Expected Result: 9 entities, 3 enums, 10 configurations
    Failure Indicators: Count mismatch
    Evidence: .sisyphus/evidence/task-1-file-count.txt

  Scenario: DbContext has correct schema and DbSets
    Tool: Bash (grep)
    Preconditions: DbContext created
    Steps:
      1. grep 'HasDefaultSchema' ContentCore.Infrastructure/Persistence/ContentCoreDbContext.cs → contains 'content_core'
      2. grep -c 'DbSet<' ContentCore.Infrastructure/Persistence/ContentCoreDbContext.cs → returns count matching entity count + OutboxMessages
    Expected Result: Schema is 'content_core', DbSet count = 10 (9 entities + OutboxMessages)
    Failure Indicators: Wrong schema name, missing DbSets
    Evidence: .sisyphus/evidence/task-1-dbcontext.txt
  ```

  **Commit**: YES
  - Message: `feat(content-core): add entity models and configurations for Content.Core module`
  - Files: `ContentCore.Domain/**`, `ContentCore.Application/**`, `ContentCore.Infrastructure/**`, `ContentCore.Contracts/**`, `ContentCore.Presentation/**`
  - Pre-commit: `dotnet build ContentCore.Infrastructure/ContentCore.Infrastructure.csproj`

- [ ] 2. Content.Places Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Content.Places module with 5 .csproj projects. This module owns place and business entities: Places, PlaceTranslations, Businesses, BusinessTranslations, BusinessHours, PlaceBusinesses, and AccessibilityFeatures.

  1. Create 5 projects following same pattern as Task 1: `ContentPlaces.Domain`, `ContentPlaces.Application`, `ContentPlaces.Infrastructure`, `ContentPlaces.Contracts`, `ContentPlaces.Presentation`
  2. Create enums in `ContentPlaces.Domain/Enums/`:
     - `PlaceType` : byte { Attraction = 0, Restaurant = 1, Hotel = 2, Shopping = 3, Nature = 4, Historical = 5, Religious = 6, Entertainment = 7 }
     - `BusinessType` : byte { Restaurant = 0, Hotel = 1, Shop = 2, Agency = 3, Transport = 4, Guide = 5, Other = 6 }
     - `DayOfWeek` : byte { Sunday = 0, Monday = 1, Tuesday = 2, Wednesday = 3, Thursday = 4, Friday = 5, Saturday = 6 }
     - `AccessibilityFeatureType` : byte { Wheelchair = 0, Visual = 1, Hearing = 2, Cognitive = 3, Mobility = 4, Other = 5 }
  3. Create entities in `ContentPlaces.Domain/Entities/`:
     - `Place` : AuditableEntity, IAggregateRoot — Id (Guid), Name (string max 300), Slug (string max 300), Description (string? nvarchar max), PlaceType (PlaceType enum), Latitude (decimal 10,8), Longitude (decimal 11,8), Address (string? max 500), City (string? max 200), Country (string? max 200), PostalCode (string? max 20), Phone (string? max 50), Email (string? max 200), Website (string? max 500), AverageRating (decimal 3,2 default 0), ReviewCount (int default 0), IsFeatured (bool default false), IsVerified (bool default false), MetaTitle (string? max 200), MetaDescription (string? max 500), CreatedByUserId (Guid). Collections: PlaceTranslations
     - `PlaceTranslation` : BaseEntity — Id (Guid), PlaceId (Guid FK), LanguageId (Guid logical ref to ContentCore.Language), Name (string max 300), Description (string? nvarchar max), Address (string? max 500). Navigation: Place
     - `Business` : AuditableEntity, IAggregateRoot — Id (Guid), Name (string max 300), Slug (string max 300), Description (string? nvarchar max), BusinessType (BusinessType enum), PlaceId (Guid? logical ref), Latitude (decimal 10,8), Longitude (decimal 11,8), Address (string? max 500), City (string? max 200), Country (string? max 200), PostalCode (string? max 20), Phone (string? max 50), Email (string? max 200), Website (string? max 500), AverageRating (decimal 3,2 default 0), ReviewCount (int default 0), IsVerified (bool default false), IsFeatured (bool default false), OwnerId (Guid logical ref to Security.User), MetaTitle (string? max 200), MetaDescription (string? max 500), LicenseNumber (string? max 100), TaxId (string? max 100). Collections: BusinessTranslations, BusinessHours
     - `BusinessTranslation` : BaseEntity — Id (Guid), BusinessId (Guid FK), LanguageId (Guid logical ref), Name (string max 300), Description (string? nvarchar max), Address (string? max 500). Navigation: Business
     - `BusinessHours` : BaseEntity — Id (Guid), BusinessId (Guid FK), DayOfWeek (DayOfWeek enum), OpenTime (TimeOnly), CloseTime (TimeOnly), IsClosed (bool default false). Navigation: Business
     - `PlaceBusiness` : no base class (junction) — PlaceId (Guid), BusinessId (Guid). Composite PK: (PlaceId, BusinessId)
     - `AccessibilityFeature` : BaseEntity — Id (Guid), EntityType (byte — use ContentCore EntityType enum via reference), EntityId (Guid), FeatureType (AccessibilityFeatureType enum), Name (string max 200), Description (string? max 1000), IsAvailable (bool default true)
  4. Create EF configurations for each entity + OutboxMessageConfiguration
  5. Create `ContentPlacesDbContext` with schema `content_places`
  6. Create DI registrations (Infrastructure + Application) and endpoint stub

  **Must NOT do**:
  - Do NOT add FK navigation to ContentCore.Language (cross-module — use plain Guid LanguageId)
  - Do NOT add FK navigation to Security.User for OwnerId/CreatedByUserId
  - Do NOT use OwnsOne for Location (Latitude/Longitude)

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: 7 entities with complex properties (coordinates, time, translations), multiple enums, cross-module references
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1, 3-11)
  - **Blocks**: Task 12 (API wiring)
  - **Blocked By**: None

  **References** (CRITICAL):

  **Pattern References**:
  - ALL references from Task 1 apply (same project structure, entity patterns, config patterns)
  - `Security.Domain/Entities/User.cs` — Aggregate root with collections (Place has PlaceTranslations collection)
  - `Security.Infrastructure/Persistence/Configurations/UserConfiguration.cs` — HasMany/WithOne/HasForeignKey for parent-child relationships

  **API/Type References**:
  - Same SharedKernel references as Task 1
  - SQL DDL: `[Content].[Places]`, `[Content].[PlaceTranslations]`, `[Content].[Businesses]`, `[Content].[BusinessTranslations]`, `[Content].[BusinessHours]`, `[Content].[PlaceBusinesses]`, `[Content].[AccessibilityFeatures]`

  **csproj References**:
  - Same patterns as Task 1

  **Acceptance Criteria**:
  - [ ] 5 .csproj files exist for ContentPlaces module
  - [ ] 7 entity .cs files in ContentPlaces.Domain/Entities/
  - [ ] 4 enum .cs files in ContentPlaces.Domain/Enums/
  - [ ] 8 configuration .cs files (7 entities + OutboxMessage)
  - [ ] ContentPlacesDbContext.cs with schema `content_places`
  - [ ] Latitude uses HasPrecision(10, 8), Longitude uses HasPrecision(11, 8)
  - [ ] PlaceBusiness uses composite PK HasKey(x => new { x.PlaceId, x.BusinessId })

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles independently
    Tool: Bash
    Preconditions: All 5 projects created
    Steps:
      1. Run: dotnet build ContentPlaces.Infrastructure/ContentPlaces.Infrastructure.csproj
    Expected Result: Build succeeded, 0 errors
    Evidence: .sisyphus/evidence/task-2-build.txt

  Scenario: Decimal precision configured correctly
    Tool: Bash (grep)
    Steps:
      1. grep 'HasPrecision(10, 8)' ContentPlaces.Infrastructure/Persistence/Configurations/PlaceConfiguration.cs
      2. grep 'HasPrecision(11, 8)' ContentPlaces.Infrastructure/Persistence/Configurations/PlaceConfiguration.cs
    Expected Result: Both lines found (Latitude 10,8 and Longitude 11,8)
    Evidence: .sisyphus/evidence/task-2-precision.txt

  Scenario: Composite PK on junction table
    Tool: Bash (grep)
    Steps:
      1. grep 'HasKey' ContentPlaces.Infrastructure/Persistence/Configurations/PlaceBusinessConfiguration.cs
    Expected Result: Contains 'new { x.PlaceId, x.BusinessId }' or equivalent composite key
    Evidence: .sisyphus/evidence/task-2-composite-pk.txt
  ```

  **Commit**: YES
  - Message: `feat(content-places): add entity models and configurations for Content.Places module`
  - Files: `ContentPlaces.Domain/**`, `ContentPlaces.Application/**`, `ContentPlaces.Infrastructure/**`, `ContentPlaces.Contracts/**`, `ContentPlaces.Presentation/**`
  - Pre-commit: `dotnet build ContentPlaces.Infrastructure/ContentPlaces.Infrastructure.csproj`

- [ ] 3. Content.Tours Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Content.Tours module with 5 .csproj projects. This module owns tour-related content: Tours, TourTranslations, TourSchedules, TourWaypoints, TourPricingTiers, TourTourGuides, TourPackages, TourPackageInclusions.

  1. Create 5 projects: `ContentTours.Domain`, `ContentTours.Application`, `ContentTours.Infrastructure`, `ContentTours.Contracts`, `ContentTours.Presentation`
  2. Create enums in `ContentTours.Domain/Enums/`:
     - `Difficulty` : byte { Easy = 0, Moderate = 1, Hard = 2, Expert = 3 }
     - `TourStatus` : byte { Draft = 0, Published = 1, Archived = 2, Suspended = 3 }
  3. Create entities in `ContentTours.Domain/Entities/`:
     - `Tour` : AuditableEntity, IAggregateRoot — Id (Guid), Name (string max 300), Slug (string max 300), Description (string? nvarchar max), ShortDescription (string? max 1000), Difficulty (Difficulty enum), DurationMinutes (int), MaxGroupSize (int), MinAge (int?), BasePrice (decimal 19,4), Currency (varchar 3 IsUnicode false), Latitude (decimal 10,8), Longitude (decimal 11,8), MeetingPoint (string? max 500), MeetingPointLatitude (decimal? 10,8), MeetingPointLongitude (decimal? 11,8), Status (TourStatus enum default Draft), AverageRating (decimal 3,2 default 0), ReviewCount (int default 0), BookingCount (int default 0), IsFeatured (bool default false), IsInstantBooking (bool default false), CancellationPolicyHours (int default 24), MetaTitle (string? max 200), MetaDescription (string? max 500), CreatedByUserId (Guid). Collections: TourTranslations, TourSchedules, TourWaypoints, TourPricingTiers, TourPackages
     - `TourTranslation` : BaseEntity — Id (Guid), TourId (Guid FK), LanguageId (Guid logical ref), Name (string max 300), Description (string? nvarchar max), ShortDescription (string? max 1000), MeetingPoint (string? max 500). Navigation: Tour
     - `TourSchedule` : BaseEntity — Id (Guid), TourId (Guid FK), DayOfWeek (byte), StartTime (TimeOnly), EndTime (TimeOnly?), IsActive (bool default true). Navigation: Tour
     - `TourWaypoint` : BaseEntity — Id (Guid), TourId (Guid FK), Name (string max 200), Description (string? max 1000), Latitude (decimal 10,8), Longitude (decimal 11,8), SortOrder (int), DurationMinutes (int?), WaypointType (byte default 0). Navigation: Tour
     - `TourPricingTier` : BaseEntity — Id (Guid), TourId (Guid FK), Name (string max 200), Description (string? max 500), Price (decimal 19,4), Currency (varchar 3 IsUnicode false), MinParticipants (int default 1), MaxParticipants (int?), IsActive (bool default true). Navigation: Tour
     - `TourTourGuide` : no base class (junction) — TourId (Guid), TourGuideId (Guid logical ref to Booking.TourGuides). Composite PK: (TourId, TourGuideId). IsPrimary (bool default false)
     - `TourPackage` : AuditableEntity — Id (Guid), TourId (Guid FK), Name (string max 200), Description (string? max 1000), Price (decimal 19,4), Currency (varchar 3 IsUnicode false), MaxParticipants (int?), ValidFrom (DateTime?), ValidTo (DateTime?), IsActive (bool default true). Collections: TourPackageInclusions. Navigation: Tour
     - `TourPackageInclusion` : BaseEntity — Id (Guid), TourPackageId (Guid FK), Description (string max 500), SortOrder (int default 0). Navigation: TourPackage
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `ContentToursDbContext` with schema `content_tours`
  6. Create DI registrations and endpoint stub

  **Must NOT do**:
  - Do NOT add FK navigation to Booking.TourGuides for TourTourGuide.TourGuideId (cross-module)
  - Do NOT add FK navigation to ContentCore.Language for LanguageId
  - Do NOT use OwnsOne for Price/Currency or coordinates

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: 8 entities with monetary values, coordinates, time types, and cross-module junction table
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1-2, 4-11)
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References** (CRITICAL):

  **Pattern References**:
  - ALL references from Task 1 apply
  - `Security.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs` — Join entity with unique composite index (for TourTourGuide)

  **API/Type References**:
  - Same SharedKernel references as Task 1
  - SQL DDL: `[Content].[Tours]`, `[Content].[TourTranslations]`, `[Content].[TourSchedules]`, `[Content].[TourWaypoints]`, `[Content].[TourPricingTiers]`, `[Content].[TourTourGuides]`, `[Content].[TourPackages]`, `[Content].[TourPackageInclusions]`

  **Acceptance Criteria**:
  - [ ] 5 .csproj files exist for ContentTours module
  - [ ] 8 entity .cs files in ContentTours.Domain/Entities/
  - [ ] 2 enum .cs files in ContentTours.Domain/Enums/
  - [ ] 9 configuration .cs files (8 entities + OutboxMessage)
  - [ ] TourTourGuide uses composite PK and has NO navigation to Booking.TourGuides
  - [ ] All Price columns use HasPrecision(19, 4)
  - [ ] Currency columns use HasMaxLength(3) and IsUnicode(false)
  - [ ] TimeOnly used for TourSchedule StartTime/EndTime

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles independently
    Tool: Bash
    Steps:
      1. Run: dotnet build ContentTours.Infrastructure/ContentTours.Infrastructure.csproj
    Expected Result: Build succeeded, 0 errors
    Evidence: .sisyphus/evidence/task-3-build.txt

  Scenario: Monetary precision and currency config
    Tool: Bash (grep)
    Steps:
      1. grep 'HasPrecision(19, 4)' ContentTours.Infrastructure/Persistence/Configurations/TourConfiguration.cs
      2. grep 'IsUnicode(false)' ContentTours.Infrastructure/Persistence/Configurations/TourConfiguration.cs
    Expected Result: Both patterns found for Price/Currency columns
    Evidence: .sisyphus/evidence/task-3-monetary.txt
  ```

  **Commit**: YES
  - Message: `feat(content-tours): add entity models and configurations for Content.Tours module`
  - Pre-commit: `dotnet build ContentTours.Infrastructure/ContentTours.Infrastructure.csproj`


- [ ] 4. Content.Blogs Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Content.Blogs module with 5 .csproj projects. This module owns blog content: Blogs, BlogTranslations, BlogTours (junction), BlogComments, BlogCommentReactions.

  1. Create 5 projects: `ContentBlogs.Domain`, `ContentBlogs.Application`, `ContentBlogs.Infrastructure`, `ContentBlogs.Contracts`, `ContentBlogs.Presentation`
  2. Create enums in `ContentBlogs.Domain/Enums/`:
     - `BlogStatus` : byte { Draft = 0, Published = 1, Archived = 2 }
     - `ReactionType` : byte { Like = 0, Dislike = 1, Love = 2, Helpful = 3 }
  3. Create entities:
     - `Blog` : AuditableEntity, IAggregateRoot — Id (Guid), Title (string max 500), Slug (string max 500), Content (nvarchar max), Summary (string? max 1000), AuthorId (Guid logical ref to Security.User), Status (BlogStatus enum default Draft), IsFeatured (bool default false), ViewCount (int default 0), ReadTimeMinutes (int?), MetaTitle (string? max 200), MetaDescription (string? max 500), PublishedAt (DateTime?). Collections: BlogTranslations, BlogComments
     - `BlogTranslation` : BaseEntity — Id (Guid), BlogId (Guid FK), LanguageId (Guid logical ref), Title (string max 500), Content (nvarchar max), Summary (string? max 1000). Navigation: Blog
     - `BlogTour` : no base class (junction) — BlogId (Guid), TourId (Guid logical ref to ContentTours). Composite PK: (BlogId, TourId). SortOrder (int default 0)
     - `BlogComment` : AuditableEntity — Id (Guid), BlogId (Guid FK), ParentCommentId (Guid? self-referencing FK), UserId (Guid logical ref), Content (nvarchar max), LikeCount (int default 0). Self-referencing: ParentComment, Replies collection. Navigation: Blog. Collections: Reactions
     - `BlogCommentReaction` : BaseEntity — Id (Guid), CommentId (Guid FK), UserId (Guid logical ref), ReactionType (ReactionType enum). Navigation: BlogComment. Unique index: (CommentId, UserId)
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `ContentBlogsDbContext` with schema `content_blogs`
  6. Create DI registrations and endpoint stub

  **Must NOT do**:
  - Do NOT add FK navigation to Security.User for AuthorId/UserId
  - Do NOT add FK navigation to ContentTours.Tour for BlogTour.TourId (cross-module)

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Self-referencing FK (BlogComments), composite PK junction, cross-module references
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with Tasks 1-3, 5-11)
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - `Security.Domain/Entities/User.cs` — Self-referencing pattern: Category has ParentCategory/SubCategories — use same pattern for BlogComment ParentComment/Replies
  - SQL DDL: `[Content].[Blogs]`, `[Content].[BlogTranslations]`, `[Content].[BlogTours]`, `[Content].[BlogComments]`, `[Content].[BlogCommentReactions]`

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 5 entities, 2 enums, 6 configs (5 entities + OutboxMessage)
  - [ ] BlogComment has self-referencing FK: ParentCommentId, ParentComment navigation, Replies collection
  - [ ] BlogTour uses composite PK (BlogId, TourId)
  - [ ] BlogCommentReaction has unique index on (CommentId, UserId)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + self-referencing FK
    Tool: Bash
    Steps:
      1. dotnet build ContentBlogs.Infrastructure/ContentBlogs.Infrastructure.csproj
      2. grep 'ParentCommentId' ContentBlogs.Domain/Entities/BlogComment.cs
      3. grep 'HasForeignKey.*ParentCommentId' ContentBlogs.Infrastructure/Persistence/Configurations/BlogCommentConfiguration.cs
    Expected Result: Build succeeds, self-referencing FK exists in entity and config
    Evidence: .sisyphus/evidence/task-4-build.txt
  ```

  **Commit**: YES
  - Message: `feat(content-blogs): add entity models and configurations for Content.Blogs module`
  - Pre-commit: `dotnet build ContentBlogs.Infrastructure/ContentBlogs.Infrastructure.csproj`

- [ ] 5. Content.SEO Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Content.SEO module with 5 .csproj projects. This module owns SEO metadata, weather cache, FAQs: SeoMetadata, WeatherCache, FaqItems, FaqItemTranslations.

  1. Create 5 projects: `ContentSeo.Domain`, `ContentSeo.Application`, `ContentSeo.Infrastructure`, `ContentSeo.Contracts`, `ContentSeo.Presentation`
  2. Create enums in `ContentSeo.Domain/Enums/`:
     - `SeoEntityType` : byte { Place = 0, Tour = 1, Business = 2, Blog = 3 }
  3. Create entities:
     - `SeoMetadata` : AuditableEntity — Id (Guid), EntityType (SeoEntityType enum), EntityId (Guid), MetaTitle (string? max 200), MetaDescription (string? max 500), CanonicalUrl (string? max 2048), OgTitle (string? max 200), OgDescription (string? max 500), OgImageUrl (string? max 2048), SchemaMarkup (nvarchar max), SitemapPriority (decimal 2,1 default 0.5), SitemapChangeFrequency (string? max 20 IsUnicode false). Unique index: (EntityType, EntityId)
     - `WeatherCache` : BaseEntity — Id (Guid), PlaceId (Guid logical ref to ContentPlaces), Temperature (decimal 5,2?), FeelsLike (decimal 5,2?), Humidity (int?), WindSpeed (decimal 5,2?), WindDirection (int?), Condition (string? max 100), Icon (string? max 100), UvIndex (decimal 4,2?), Forecast (nvarchar max), FetchedAt (DateTime), ExpiresAt (DateTime). Index: (PlaceId, FetchedAt)
     - `FaqItem` : AuditableEntity — Id (Guid), EntityType (SeoEntityType enum), EntityId (Guid), Question (string max 1000), Answer (nvarchar max), SortOrder (int default 0), IsActive (bool default true). Collections: FaqItemTranslations. Index: (EntityType, EntityId)
     - `FaqItemTranslation` : BaseEntity — Id (Guid), FaqItemId (Guid FK), LanguageId (Guid logical ref), Question (string max 1000), Answer (nvarchar max). Navigation: FaqItem
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `ContentSeoDbContext` with schema `content_seo`
  6. Create DI registrations and endpoint stub

  **Must NOT do**:
  - Do NOT add FK navigation to ContentPlaces.Place for WeatherCache.PlaceId

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Multiple decimal precisions (2,1 for sitemap priority; 5,2 for temperature; 4,2 for UV), nvarchar(max) columns, unique composite indexes
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - SQL DDL: `[Content].[SeoMetadata]`, `[Content].[WeatherCache]`, `[Content].[FaqItems]`, `[Content].[FaqItemTranslations]`

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 4 entities, 1 enum, 5 configs (4 entities + OutboxMessage)
  - [ ] SeoMetadata has unique index on (EntityType, EntityId)
  - [ ] SitemapPriority uses HasPrecision(2, 1) with default 0.5
  - [ ] Temperature uses HasPrecision(5, 2)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + decimal precisions
    Tool: Bash
    Steps:
      1. dotnet build ContentSeo.Infrastructure/ContentSeo.Infrastructure.csproj
      2. grep 'HasPrecision(2, 1)' ContentSeo.Infrastructure/Persistence/Configurations/SeoMetadataConfiguration.cs
      3. grep 'HasPrecision(5, 2)' ContentSeo.Infrastructure/Persistence/Configurations/WeatherCacheConfiguration.cs
    Expected Result: Build succeeds, correct decimal precisions configured
    Evidence: .sisyphus/evidence/task-5-build.txt
  ```

  **Commit**: YES
  - Message: `feat(content-seo): add entity models and configurations for Content.SEO module`
  - Pre-commit: `dotnet build ContentSeo.Infrastructure/ContentSeo.Infrastructure.csproj`

- [ ] 6. Analytics Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Analytics module with 5 .csproj projects. This module has a KEY EDGE CASE: AuditLogs and UserInteractions use `BIGINT IDENTITY` (long) PKs instead of Guid. These entities inherit from `BaseEntity<long>` with `ValueGeneratedOnAdd()` instead of `ValueGeneratedNever()`.

  1. Create 5 projects: `Analytics.Domain`, `Analytics.Application`, `Analytics.Infrastructure`, `Analytics.Contracts`, `Analytics.Presentation`
  2. Create enums in `Analytics.Domain/Enums/`:
     - `InteractionType` : byte { View = 0, Click = 1, Share = 2, Bookmark = 3, Search = 4, Review = 5, Booking = 6 }
  3. Create entities:
     - `AuditLog` : BaseEntity<long> (NOT AuditableEntity — append-only, BIGINT PK) — Id (long, ValueGeneratedOnAdd), UserId (Guid? logical ref), UserAgent (string? max 500), Action (string max 200), EntityType (string max 200), EntityId (string? max 450 — nvarchar, NOT Guid), IpAddress (string? max 45), OldValues (nvarchar max), NewValues (nvarchar max), OccurredAt (DateTime). Config: ValueGeneratedOnAdd(), do NOT call CreateVersion7() in constructor
     - `UserInteraction` : BaseEntity<long> (BIGINT PK) — Id (long, ValueGeneratedOnAdd), UserId (Guid logical ref), InteractionType (InteractionType enum), EntityType (string max 200), EntityId (Guid), Latitude (decimal? 10,8), Longitude (decimal? 11,8), SessionId (string? max 100 IsUnicode false), DeviceType (string? max 50), DurationSeconds (int?), OccurredAt (DateTime). Config: ValueGeneratedOnAdd()
     - `UserPreference` : AuditableEntity — Id (Guid), UserId (Guid logical ref), PreferenceKey (string max 200), PreferenceValue (nvarchar max). Unique index: (UserId, PreferenceKey)
     - `UserPreferredCategory` : no base class (junction) — UserId (Guid logical ref), CategoryId (Guid logical ref to ContentCore.Category). Composite PK: (UserId, CategoryId). PreferenceScore (decimal 5,4 default 0)
     - `PopularityScore` : AuditableEntity — Id (Guid), EntityType (string max 200), EntityId (Guid), TrendingScore (decimal 10,4 default 0), ViewCount (int default 0), BookmarkCount (int default 0), ShareCount (int default 0), BookingCount (int default 0), ReviewScore (decimal 3,2 default 0), LastCalculatedAt (DateTime). Unique index: (EntityType, EntityId)
     - `RecommendationCache` : BaseEntity — Id (Guid), UserId (Guid logical ref), EntityType (string max 200), EntityId (Guid), Score (decimal 10,4), Reason (string? max 500), GeneratedAt (DateTime), ExpiresAt (DateTime). Index: (UserId, EntityType, Score descending)
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `AnalyticsDbContext` with schema `analytics`
  6. Create DI registrations and endpoint stub

  **CRITICAL EDGE CASE**: AuditLog and UserInteraction use `BaseEntity<long>` with `ValueGeneratedOnAdd()`. Their constructors must NOT set Id (long defaults to 0, which is correct for identity columns). The configuration must use `builder.Property(x => x.Id).ValueGeneratedOnAdd()` instead of `ValueGeneratedNever()`.

  **Must NOT do**:
  - Do NOT use ValueGeneratedNever() on AuditLog or UserInteraction — they use database-generated BIGINT IDENTITY
  - Do NOT add Guid.CreateVersion7() in AuditLog/UserInteraction constructors
  - Do NOT add soft delete to AuditLog or UserInteraction (they're append-only)
  - Do NOT add FK navigation to ContentCore.Category for UserPreferredCategory

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Critical BIGINT IDENTITY edge case, mixed PK strategies (long vs Guid), multiple decimal precisions
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - `Security.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs` — Append-only entity config pattern (no HasQueryFilter, no RowVersion)
  - `YallaJo.SharedKernel.Domain/Entities/BaseEntity.cs` — Note the `BaseEntity<TKey>` generic variant for long PKs
  - SQL DDL: `[Analytics].[AuditLogs]` (BIGINT IDENTITY), `[Analytics].[UserInteractions]` (BIGINT IDENTITY), `[Analytics].[UserPreferences]`, `[Analytics].[UserPreferredCategories]`, `[Analytics].[PopularityScores]`, `[Analytics].[RecommendationCache]`

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 6 entities, 1 enum, 7 configs (6 entities + OutboxMessage)
  - [ ] AuditLog inherits BaseEntity<long>, NOT BaseEntity or AuditableEntity
  - [ ] UserInteraction inherits BaseEntity<long>
  - [ ] AuditLog config uses ValueGeneratedOnAdd(), NOT ValueGeneratedNever()
  - [ ] UserInteraction config uses ValueGeneratedOnAdd()
  - [ ] UserPreferredCategory uses composite PK (UserId, CategoryId)
  - [ ] PopularityScore has unique index on (EntityType, EntityId)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + BIGINT PK strategy
    Tool: Bash
    Steps:
      1. dotnet build Analytics.Infrastructure/Analytics.Infrastructure.csproj
      2. grep 'BaseEntity<long>' Analytics.Domain/Entities/AuditLog.cs
      3. grep 'ValueGeneratedOnAdd' Analytics.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
      4. Ensure NO 'ValueGeneratedNever' in AuditLog or UserInteraction configs
    Expected Result: Build succeeds, long PK with ValueGeneratedOnAdd confirmed, no ValueGeneratedNever
    Evidence: .sisyphus/evidence/task-6-build.txt

  Scenario: No soft delete on append-only entities
    Tool: Bash (grep)
    Steps:
      1. grep 'HasQueryFilter' Analytics.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
      2. grep 'HasQueryFilter' Analytics.Infrastructure/Persistence/Configurations/UserInteractionConfiguration.cs
    Expected Result: Neither file contains HasQueryFilter (append-only, no soft delete)
    Evidence: .sisyphus/evidence/task-6-no-softdelete.txt
  ```

  **Commit**: YES
  - Message: `feat(analytics): add entity models and configurations for Analytics module`
  - Pre-commit: `dotnet build Analytics.Infrastructure/Analytics.Infrastructure.csproj`


- [ ] 7. Booking Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Booking module with 5 .csproj projects. This module owns all booking-related entities: TourGuides, TourGuideLanguages, TourGuideSpecializations, AvailabilitySlots, TourBookings, PackageBookings, Reservations, JoinRequests, SlotLocks, RefundPolicies, ProviderDocuments.

  1. Create 5 projects: `Booking.Domain`, `Booking.Application`, `Booking.Infrastructure`, `Booking.Contracts`, `Booking.Presentation`
  2. Create enums in `Booking.Domain/Enums/`:
     - `BookingStatus` : byte { Pending = 0, Confirmed = 1, InProgress = 2, Completed = 3, Cancelled = 4, Refunded = 5, NoShow = 6 }
     - `SlotType` : byte { Tour = 0, Business = 1 }
     - `DocumentType` : byte { License = 0, Insurance = 1, Certificate = 2, Identity = 3, Other = 4 }
     - `DocumentStatus` : byte { Pending = 0, Approved = 1, Rejected = 2, Expired = 3 }
     - `JoinRequestStatus` : byte { Pending = 0, Approved = 1, Rejected = 2, Cancelled = 3 }
  3. Create entities:
     - `TourGuide` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), Bio (string? nvarchar max), YearsOfExperience (int default 0), AverageRating (decimal 3,2 default 0), ReviewCount (int default 0), CompletedTourCount (int default 0), IsVerified (bool default false), IsActive (bool default true), HourlyRate (decimal? 19,4), Currency (varchar? 3 IsUnicode false), ResponseTimeMinutes (int?). Collections: TourGuideLanguages, TourGuideSpecializations, AvailabilitySlots, ProviderDocuments
     - `TourGuideLanguage` : no base class (junction) — TourGuideId (Guid FK), LanguageId (Guid logical ref to ContentCore.Language). Composite PK: (TourGuideId, LanguageId). ProficiencyLevel (byte default 0). Navigation: TourGuide
     - `TourGuideSpecialization` : no base class (junction) — TourGuideId (Guid FK), SpecializationId (Guid logical ref to ContentCore.Specialization). Composite PK: (TourGuideId, SpecializationId). Navigation: TourGuide
     - `AvailabilitySlot` : AuditableEntity — Id (Guid), TourGuideId (Guid FK), SlotType (SlotType enum), TourId (Guid? logical ref), BusinessId (Guid? logical ref), Date (DateOnly), StartTime (TimeOnly), EndTime (TimeOnly), MaxCapacity (int default 1), BookedCount (int default 0), IsActive (bool default true). Factory methods: CreateForTour(...), CreateForBusiness(...) to enforce XOR constraint. Navigation: TourGuide
     - `TourBooking` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), TourId (Guid logical ref), TourGuideId (Guid? logical ref), ScheduledDate (DateOnly), StartTime (TimeOnly?), ParticipantCount (int default 1), TotalPrice (decimal 19,4), Currency (varchar 3 IsUnicode false), Status (BookingStatus enum default Pending), SpecialRequests (string? max 2000), CancellationReason (string? max 1000), CancelledAt (DateTime?), CompletedAt (DateTime?), ConfirmedAt (DateTime?). Collections: JoinRequests
     - `PackageBooking` : AuditableEntity — Id (Guid), UserId (Guid logical ref), TourPackageId (Guid logical ref to ContentTours.TourPackage), BookingDate (DateOnly), ParticipantCount (int default 1), TotalPrice (decimal 19,4), Currency (varchar 3 IsUnicode false), Status (BookingStatus enum default Pending), SpecialRequests (string? max 2000), CancellationReason (string? max 1000), CancelledAt (DateTime?)
     - `Reservation` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), BusinessId (Guid logical ref), ReservationDate (DateOnly), ReservationTime (TimeOnly), PartySize (int default 1), Status (BookingStatus enum default Pending), SpecialRequests (string? max 2000), ConfirmedAt (DateTime?), CancellationReason (string? max 1000), CancelledAt (DateTime?)
     - `JoinRequest` : AuditableEntity — Id (Guid), TourBookingId (Guid FK), UserId (Guid logical ref), Status (JoinRequestStatus enum default Pending), Message (string? max 1000), ParticipantCount (int default 1), RespondedAt (DateTime?), ResponseMessage (string? max 1000). Navigation: TourBooking
     - `SlotLock` : BaseEntity — Id (Guid), AvailabilitySlotId (Guid FK), UserId (Guid logical ref), LockedAt (DateTime), ExpiresAt (DateTime), IsReleased (bool default false), ReleasedAt (DateTime?). Navigation: AvailabilitySlot. Index: (AvailabilitySlotId, IsReleased)
     - `RefundPolicy` : AuditableEntity — Id (Guid), Name (string max 200), Description (string? max 1000), FullRefundHours (int), PartialRefundHours (int), PartialRefundPercent (decimal 5,2), IsDefault (bool default false), IsActive (bool default true)
     - `ProviderDocument` : AuditableEntity — Id (Guid), TourGuideId (Guid FK), DocumentType (DocumentType enum), DocumentUrl (string max 2048), OriginalFileName (string? max 500), ExpiresAt (DateTime?), Status (DocumentStatus enum default Pending), ReviewedAt (DateTime?), ReviewedByUserId (Guid? logical ref), RejectionReason (string? max 1000). Navigation: TourGuide
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `BookingDbContext` with schema `booking`
  6. Create DI registrations and endpoint stub

  **IMPORTANT**: Financial FK behavior — TourBookings and PackageBookings FK delete behavior should be NO ACTION (Restrict) to comply with GAAP. Use `.OnDelete(DeleteBehavior.Restrict)` for all FKs on these entities.

  **Must NOT do**:
  - Do NOT add FK navigation to Security.User for UserId
  - Do NOT add FK navigation to ContentTours.Tour/TourPackage or ContentPlaces.Business
  - Do NOT add FK navigation to ContentCore.Language/Specialization for junction tables
  - Do NOT use HasCheckConstraint for AvailabilitySlot XOR — use factory methods only

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: 11 entities (largest non-Content module), multiple enums, composite PK junctions, GAAP-compliant delete behavior, XOR constraint factory methods, DateOnly/TimeOnly, monetary precision
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - `Security.Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs` — Join entity config with composite index (for TourGuideLanguage, TourGuideSpecialization)
  - SQL DDL: `[Booking].*` all 11 table definitions
  - GAAP compliance note: Financial entities (TourBookings, PackageBookings, Reservations) must use DeleteBehavior.Restrict

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 11 entities, 5 enums, 12 configs (11 entities + OutboxMessage)
  - [ ] TourGuideLanguage and TourGuideSpecialization use composite PKs
  - [ ] TourBooking/PackageBooking FKs use DeleteBehavior.Restrict
  - [ ] AvailabilitySlot has two static factory methods (CreateForTour, CreateForBusiness)
  - [ ] All monetary columns (TotalPrice, HourlyRate) use HasPrecision(19, 4)
  - [ ] DateOnly for dates, TimeOnly for times

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + GAAP delete behavior
    Tool: Bash
    Steps:
      1. dotnet build Booking.Infrastructure/Booking.Infrastructure.csproj
      2. grep 'DeleteBehavior.Restrict' Booking.Infrastructure/Persistence/Configurations/TourBookingConfiguration.cs
      3. grep -c 'HasKey.*new' Booking.Infrastructure/Persistence/Configurations/TourGuideLanguageConfiguration.cs
    Expected Result: Build succeeds, Restrict delete on bookings, composite key on junction
    Evidence: .sisyphus/evidence/task-7-build.txt

  Scenario: XOR factory methods on AvailabilitySlot
    Tool: Bash (grep)
    Steps:
      1. grep 'CreateForTour' Booking.Domain/Entities/AvailabilitySlot.cs
      2. grep 'CreateForBusiness' Booking.Domain/Entities/AvailabilitySlot.cs
    Expected Result: Both factory methods exist
    Evidence: .sisyphus/evidence/task-7-factory.txt
  ```

  **Commit**: YES
  - Message: `feat(booking): add entity models and configurations for Booking module`
  - Pre-commit: `dotnet build Booking.Infrastructure/Booking.Infrastructure.csproj`

- [ ] 8. Finance Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Finance module with 5 .csproj projects. This module owns all financial entities: Payments, Payouts, PayoutItems, Disputes, CommissionRules, Subscriptions, SubscriptionPlans, SubscriptionFeatures, PlanFeatures, LoyaltyPoints, LoyaltyTransactions, Referrals, Discounts, DiscountUsages, InvoiceItems, InvoiceLineItems.

  1. Create 5 projects: `Finance.Domain`, `Finance.Application`, `Finance.Infrastructure`, `Finance.Contracts`, `Finance.Presentation`
  2. Create enums in `Finance.Domain/Enums/`:
     - `PaymentStatus` : byte { Pending = 0, Processing = 1, Completed = 2, Failed = 3, Refunded = 4, PartiallyRefunded = 5 }
     - `PaymentMethod` : byte { CreditCard = 0, DebitCard = 1, BankTransfer = 2, Wallet = 3, Cash = 4 }
     - `PayoutStatus` : byte { Pending = 0, Processing = 1, Completed = 2, Failed = 3 }
     - `DisputeStatus` : byte { Open = 0, UnderReview = 1, Resolved = 2, Escalated = 3, Closed = 4 }
     - `DisputeResolution` : byte { RefundFull = 0, RefundPartial = 1, NoRefund = 2, Compromise = 3 }
     - `SubscriptionStatus` : byte { Active = 0, Paused = 1, Cancelled = 2, Expired = 3 }
     - `BillingCycle` : byte { Monthly = 0, Quarterly = 1, Annual = 2 }
     - `DiscountType` : byte { Percentage = 0, FixedAmount = 1 }
     - `InvoiceStatus` : byte { Draft = 0, Sent = 1, Paid = 2, Overdue = 3, Cancelled = 4 }
     - `TransactionType` : byte { Earned = 0, Redeemed = 1, Expired = 2, Adjusted = 3 }
  3. Create entities (16 total):
     - `Payment` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), BookingId (Guid? logical ref), ReservationId (Guid? logical ref), Amount (decimal 19,4), Currency (varchar 3 IsUnicode false), PaymentMethod (PaymentMethod enum), Status (PaymentStatus enum default Pending), TransactionId (string? max 200 IsUnicode false), GatewayResponse (nvarchar max), PaidAt (DateTime?), RefundedAmount (decimal 19,4 default 0), RefundedAt (DateTime?). Index: (UserId, Status)
     - `Payout` : AuditableEntity, IAggregateRoot — Id (Guid), RecipientUserId (Guid logical ref), Status (PayoutStatus enum default Pending), TotalAmount (decimal 19,4), Currency (varchar 3 IsUnicode false), ProcessedAt (DateTime?), BankAccountInfo (string? max 500), TransactionId (string? max 200 IsUnicode false), Notes (string? max 1000). Collections: PayoutItems
     - `PayoutItem` : BaseEntity — Id (Guid), PayoutId (Guid FK), BookingId (Guid logical ref), Amount (decimal 19,4), Commission (decimal 19,4), NetAmount (decimal 19,4). Navigation: Payout
     - `Dispute` : AuditableEntity — Id (Guid), PaymentId (Guid FK), UserId (Guid logical ref), Reason (string max 1000), Description (nvarchar max), Status (DisputeStatus enum default Open), Resolution (DisputeResolution? enum), ResolvedAt (DateTime?), ResolvedByUserId (Guid? logical ref), ResolutionNotes (nvarchar max). Navigation: Payment
     - `CommissionRule` : AuditableEntity — Id (Guid), Name (string max 200), Description (string? max 1000), CommissionPercentage (decimal 5,2), MinAmount (decimal? 19,4), MaxAmount (decimal? 19,4), EntityType (string max 200), IsActive (bool default true), Priority (int default 0), ValidFrom (DateTime?), ValidTo (DateTime?)
     - `SubscriptionPlan` : AuditableEntity — Id (Guid), Name (string max 200), Description (string? max 1000), Price (decimal 19,4), Currency (varchar 3 IsUnicode false), BillingCycle (BillingCycle enum), TrialDays (int default 0), IsActive (bool default true), SortOrder (int default 0). Collections: PlanFeatures, Subscriptions
     - `SubscriptionFeature` : AuditableEntity — Id (Guid), Name (string max 200), Description (string? max 500), Code (string max 100 IsUnicode false)
     - `PlanFeature` : no base class (junction) — PlanId (Guid FK), FeatureId (Guid FK). Composite PK: (PlanId, FeatureId). Value (string? max 200). Navigation: SubscriptionPlan, SubscriptionFeature
     - `Subscription` : AuditableEntity — Id (Guid), UserId (Guid logical ref), PlanId (Guid FK), Status (SubscriptionStatus enum default Active), StartDate (DateTime), EndDate (DateTime?), CancelledAt (DateTime?), TrialEndsAt (DateTime?). Navigation: SubscriptionPlan
     - `LoyaltyPoints` : AuditableEntity — Id (Guid), UserId (Guid logical ref), TotalPoints (int default 0), AvailablePoints (int default 0), LifetimePoints (int default 0). Unique index: UserId. Collections: Transactions
     - `LoyaltyTransaction` : BaseEntity — Id (Guid), LoyaltyPointsId (Guid FK), Points (int), TransactionType (TransactionType enum), Description (string? max 500), ReferenceId (Guid?), ExpiresAt (DateTime?). Navigation: LoyaltyPoints
     - `Referral` : AuditableEntity — Id (Guid), ReferrerUserId (Guid logical ref), ReferredUserId (Guid? logical ref), ReferralCode (string max 50 IsUnicode false), Status (byte default 0), RewardAmount (decimal? 19,4), Currency (varchar? 3 IsUnicode false), CompletedAt (DateTime?). Unique index: ReferralCode. Unique index: ReferredUserId (filtered where not null)
     - `Discount` : AuditableEntity — Id (Guid), Code (string max 50 IsUnicode false), Name (string max 200), Description (string? max 1000), DiscountType (DiscountType enum), DiscountValue (decimal 19,4), MinOrderAmount (decimal? 19,4), MaxDiscountAmount (decimal? 19,4), MaxUsageCount (int?), CurrentUsageCount (int default 0), ValidFrom (DateTime), ValidTo (DateTime), IsActive (bool default true), EntityType (string? max 200), EntityId (Guid?). Unique index: Code. Collections: DiscountUsages
     - `DiscountUsage` : BaseEntity — Id (Guid), DiscountId (Guid FK), UserId (Guid logical ref), BookingId (Guid? logical ref), Amount (decimal 19,4), UsedAt (DateTime). Navigation: Discount
     - `InvoiceItem` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), InvoiceNumber (string max 50 IsUnicode false), Status (InvoiceStatus enum default Draft), SubTotal (decimal 19,4), TaxAmount (decimal 19,4 default 0), TotalAmount (decimal 19,4), Currency (varchar 3 IsUnicode false), DueDate (DateOnly), PaidAt (DateTime?), Notes (nvarchar max). Unique index: InvoiceNumber. Collections: InvoiceLineItems
     - `InvoiceLineItem` : BaseEntity — Id (Guid), InvoiceId (Guid FK), Description (string max 500), Quantity (int default 1), UnitPrice (decimal 19,4), Amount (decimal 19,4), EntityType (string? max 200), EntityId (Guid?). Navigation: InvoiceItem
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `FinanceDbContext` with schema `finance`
  6. Create DI registrations and endpoint stub

  **CRITICAL**: ALL monetary columns use HasPrecision(19, 4). ALL FK delete behaviors on financial entities use DeleteBehavior.Restrict (GAAP compliance — financial records must never cascade-delete).

  **Must NOT do**:
  - Do NOT cascade delete on any financial FK
  - Do NOT add FK navigation to Security.User or Booking entities (cross-module)

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: 16 entities (largest module), 10 enums, complex financial constraints, multiple monetary columns requiring HasPrecision(19,4), GAAP-compliant delete behavior throughout
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - GAAP compliance: ALL FKs use DeleteBehavior.Restrict
  - SQL DDL: `[Finance].*` all 16 table definitions

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 16 entities, 10 enums, 17 configs (16 entities + OutboxMessage)
  - [ ] ALL monetary decimal columns use HasPrecision(19, 4)
  - [ ] ALL FK delete behaviors use DeleteBehavior.Restrict
  - [ ] PlanFeature uses composite PK (PlanId, FeatureId)
  - [ ] InvoiceNumber has unique index
  - [ ] ReferralCode has unique index
  - [ ] Currency columns use IsUnicode(false) and HasMaxLength(3)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + financial constraints
    Tool: Bash
    Steps:
      1. dotnet build Finance.Infrastructure/Finance.Infrastructure.csproj
      2. grep -c 'HasPrecision(19, 4)' Finance.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs
      3. grep -c 'DeleteBehavior.Restrict' Finance.Infrastructure/Persistence/Configurations/PaymentConfiguration.cs
    Expected Result: Build succeeds, multiple HasPrecision(19,4) calls, Restrict delete on all FKs
    Evidence: .sisyphus/evidence/task-8-build.txt

  Scenario: Entity and enum count
    Tool: Bash (PowerShell)
    Steps:
      1. Get-ChildItem -Path Finance.Domain/Entities -Filter *.cs | Measure-Object
      2. Get-ChildItem -Path Finance.Domain/Enums -Filter *.cs | Measure-Object
    Expected Result: 16 entities, 10 enums
    Evidence: .sisyphus/evidence/task-8-count.txt
  ```

  **Commit**: YES
  - Message: `feat(finance): add entity models and configurations for Finance module`
  - Pre-commit: `dotnet build Finance.Infrastructure/Finance.Infrastructure.csproj`

- [ ] 9. Messaging Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Messaging module with 5 .csproj projects. This module owns: Notifications, NotificationPreferences, NotificationTemplates, DeviceTokens, SupportTickets, TicketMessages, ChatBotConversations, ChatBotMessages.

  1. Create 5 projects: `Messaging.Domain`, `Messaging.Application`, `Messaging.Infrastructure`, `Messaging.Contracts`, `Messaging.Presentation`
  2. Create enums in `Messaging.Domain/Enums/`:
     - `NotificationType` : byte { System = 0, Booking = 1, Payment = 2, Review = 3, Promotion = 4, Social = 5, Chat = 6 }
     - `NotificationChannel` : byte { InApp = 0, Push = 1, Email = 2, Sms = 3 }
     - `NotificationPriority` : byte { Low = 0, Medium = 1, High = 2, Critical = 3 }
     - `TicketStatus` : byte { Open = 0, InProgress = 1, WaitingOnCustomer = 2, Resolved = 3, Closed = 4 }
     - `TicketPriority` : byte { Low = 0, Medium = 1, High = 2, Urgent = 3 }
     - `DevicePlatform` : byte { iOS = 0, Android = 1, Web = 2 }
  3. Create entities:
     - `Notification` : BaseEntity — Id (Guid), UserId (Guid logical ref), Type (NotificationType enum), Channel (NotificationChannel enum), Priority (NotificationPriority enum default Low), Title (string max 200), Body (string max 2000), Data (nvarchar max), IsRead (bool default false), ReadAt (DateTime?), SentAt (DateTime?), EntityType (string? max 200), EntityId (Guid?). Index: (UserId, IsRead, CreatedAt desc)
     - `NotificationPreference` : AuditableEntity — Id (Guid), UserId (Guid logical ref), NotificationType (NotificationType enum), Channel (NotificationChannel enum), IsEnabled (bool default true). Unique index: (UserId, NotificationType, Channel)
     - `NotificationTemplate` : AuditableEntity — Id (Guid), Name (string max 200), Code (string max 100 IsUnicode false), Type (NotificationType enum), Channel (NotificationChannel enum), Subject (string? max 500), BodyTemplate (nvarchar max), IsActive (bool default true). Unique index: Code
     - `DeviceToken` : AuditableEntity — Id (Guid), UserId (Guid logical ref), Token (string max 500), Platform (DevicePlatform enum), DeviceName (string? max 200), IsActive (bool default true), LastUsedAt (DateTime?). Index: (UserId, IsActive)
     - `SupportTicket` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), Subject (string max 500), Description (nvarchar max), Status (TicketStatus enum default Open), Priority (TicketPriority enum default Medium), AssignedToUserId (Guid? logical ref), Category (string? max 100), ResolvedAt (DateTime?), ClosedAt (DateTime?). Collections: TicketMessages. Index: (UserId, Status)
     - `TicketMessage` : BaseEntity — Id (Guid), TicketId (Guid FK), SenderUserId (Guid logical ref), Message (nvarchar max), IsStaffReply (bool default false). Navigation: SupportTicket
     - `ChatBotConversation` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), Title (string? max 200), IsActive (bool default true), LastMessageAt (DateTime?). Collections: ChatBotMessages. Index: (UserId, IsActive)
     - `ChatBotMessage` : BaseEntity — Id (Guid), ConversationId (Guid FK), IsFromBot (bool), Message (nvarchar max), Confidence (decimal? 5,4), Intent (string? max 200). Navigation: ChatBotConversation
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `MessagingDbContext` with schema `messaging`
  6. Create DI registrations and endpoint stub

  **Must NOT do**:
  - Do NOT add FK navigation to Security.User

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: 8 entities, 6 enums, parent-child relationships (Ticket/Messages, Conversation/Messages), multiple indexes
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - SQL DDL: `[Messaging].*` all 8 table definitions

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 8 entities, 6 enums, 9 configs (8 entities + OutboxMessage)
  - [ ] Notification inherits BaseEntity (not AuditableEntity — notifications are append-only)
  - [ ] NotificationPreference has unique index on (UserId, NotificationType, Channel)
  - [ ] ChatBotMessage.Confidence uses HasPrecision(5, 4)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + notification indexes
    Tool: Bash
    Steps:
      1. dotnet build Messaging.Infrastructure/Messaging.Infrastructure.csproj
      2. grep 'IsUnique' Messaging.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs
      3. grep 'HasPrecision(5, 4)' Messaging.Infrastructure/Persistence/Configurations/ChatBotMessageConfiguration.cs
    Expected Result: Build succeeds, unique composite index on preferences, correct decimal precision
    Evidence: .sisyphus/evidence/task-9-build.txt
  ```

  **Commit**: YES
  - Message: `feat(messaging): add entity models and configurations for Messaging module`
  - Pre-commit: `dotnet build Messaging.Infrastructure/Messaging.Infrastructure.csproj`


- [ ] 10. Social Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Social module with 5 .csproj projects. This module owns: Reviews, Favorites, Reports, ContentModerationLogs, AccessibilityReviews.

  1. Create 5 projects: `Social.Domain`, `Social.Application`, `Social.Infrastructure`, `Social.Contracts`, `Social.Presentation`
  2. Create enums in `Social.Domain/Enums/`:
     - `ReviewTargetType` : byte { Place = 0, Tour = 1, TourGuide = 2, Business = 3 }
     - `ReportStatus` : byte { Pending = 0, UnderReview = 1, ActionTaken = 2, Dismissed = 3 }
     - `ReportReason` : byte { Spam = 0, Inappropriate = 1, Harassment = 2, FalseInfo = 3, Copyright = 4, Other = 5 }
     - `ModerationAction` : byte { Approved = 0, Rejected = 1, Edited = 2, Flagged = 3, Removed = 4 }
  3. Create entities:
     - `Review` : AuditableEntity, IAggregateRoot — Id (Guid), UserId (Guid logical ref), PlaceId (Guid? logical ref), TourId (Guid? logical ref), TourGuideId (Guid? logical ref), BusinessId (Guid? logical ref), Rating (decimal 3,2), Title (string? max 200), Content (nvarchar max), VisitDate (DateOnly?), IsVerified (bool default false), IsReported (bool default false), HelpfulCount (int default 0). Factory methods: CreateForPlace(...), CreateForTour(...), CreateForTourGuide(...), CreateForBusiness(...) to enforce XOR (only one target ID set). Indexes: (PlaceId), (TourId), (TourGuideId), (BusinessId), (UserId)
     - `Favorite` : BaseEntity — Id (Guid), UserId (Guid logical ref), EntityType (string max 200), EntityId (Guid). Unique index: (UserId, EntityType, EntityId). Index: (UserId)
     - `Report` : AuditableEntity — Id (Guid), ReporterUserId (Guid logical ref), EntityType (string max 200), EntityId (Guid), Reason (ReportReason enum), Description (nvarchar max), Status (ReportStatus enum default Pending), ResolvedAt (DateTime?), ResolvedByUserId (Guid? logical ref), ResolutionNotes (string? max 1000). Index: (EntityType, EntityId, Status)
     - `ContentModerationLog` : BaseEntity — Id (Guid), EntityType (string max 200), EntityId (Guid), ModeratorUserId (Guid logical ref), Action (ModerationAction enum), Reason (string? max 1000), OccurredAt (DateTime). Index: (EntityType, EntityId)
     - `AccessibilityReview` : AuditableEntity — Id (Guid), UserId (Guid logical ref), EntityType (string max 200), EntityId (Guid), WheelchairAccessible (bool?), VisualAidAvailable (bool?), HearingAidAvailable (bool?), AccessibilityRating (decimal 3,2?), Comments (nvarchar max), VisitDate (DateOnly?). Index: (EntityType, EntityId)
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `SocialDbContext` with schema `social`
  6. Create DI registrations and endpoint stub

  **Must NOT do**:
  - Do NOT add FK navigation to ContentPlaces.Place, ContentTours.Tour, Booking.TourGuide, ContentPlaces.Business (all cross-module)
  - Do NOT use HasCheckConstraint for Review XOR — use factory methods only

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Polymorphic XOR pattern (Review targets), multiple cross-module logical references, factory methods for constraint enforcement
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - SQL DDL: `[Social].*` all 5 table definitions

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 5 entities, 4 enums, 6 configs (5 entities + OutboxMessage)
  - [ ] Review has 4 factory methods (CreateForPlace, CreateForTour, CreateForTourGuide, CreateForBusiness)
  - [ ] Review has nullable PlaceId, TourId, TourGuideId, BusinessId — NO navigation properties
  - [ ] Favorite has unique index on (UserId, EntityType, EntityId)
  - [ ] Rating uses HasPrecision(3, 2)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + XOR factory methods
    Tool: Bash
    Steps:
      1. dotnet build Social.Infrastructure/Social.Infrastructure.csproj
      2. grep 'CreateForPlace' Social.Domain/Entities/Review.cs
      3. grep 'CreateForTour' Social.Domain/Entities/Review.cs
      4. grep 'HasPrecision(3, 2)' Social.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs
    Expected Result: Build succeeds, factory methods exist, decimal precision configured
    Evidence: .sisyphus/evidence/task-10-build.txt
  ```

  **Commit**: YES
  - Message: `feat(social): add entity models and configurations for Social module`
  - Pre-commit: `dotnet build Social.Infrastructure/Social.Infrastructure.csproj`

- [ ] 11. Tracking Module — Entity Models, Configurations, and Infrastructure

  **What to do**:
  Create the Tracking module with 5 .csproj projects. This is the smallest module with 3 entities: LiveTrackingSessions, LocationSnapshots, TourCheckpoints.

  1. Create 5 projects: `Tracking.Domain`, `Tracking.Application`, `Tracking.Infrastructure`, `Tracking.Contracts`, `Tracking.Presentation`
  2. Create enums in `Tracking.Domain/Enums/`:
     - `SessionStatus` : byte { Active = 0, Paused = 1, Completed = 2, Expired = 3 }
     - `CheckpointStatus` : byte { NotReached = 0, Reached = 1, Skipped = 2 }
  3. Create entities:
     - `LiveTrackingSession` : AuditableEntity, IAggregateRoot — Id (Guid), TourBookingId (Guid logical ref to Booking), TourGuideId (Guid logical ref), Status (SessionStatus enum default Active), StartedAt (DateTime), EndedAt (DateTime?), LastLocationUpdate (DateTime?). Collections: LocationSnapshots, TourCheckpoints. Index: (TourBookingId), (TourGuideId, Status)
     - `LocationSnapshot` : BaseEntity — Id (Guid), SessionId (Guid FK), Latitude (decimal 10,8), Longitude (decimal 11,8), Accuracy (double — SQL float), Speed (double? — SQL float), Heading (double? — SQL float), Altitude (double? — SQL float), CapturedAt (DateTime). Navigation: LiveTrackingSession. Index: (SessionId, CapturedAt)
     - `TourCheckpoint` : AuditableEntity — Id (Guid), SessionId (Guid FK), WaypointId (Guid logical ref to ContentTours.TourWaypoint), Status (CheckpointStatus enum default NotReached), ReachedAt (DateTime?), Notes (string? max 500). Navigation: LiveTrackingSession. Unique index: (SessionId, WaypointId)
  4. Create EF configurations + OutboxMessageConfiguration
  5. Create `TrackingDbContext` with schema `tracking`
  6. Create DI registrations and endpoint stub

  **IMPORTANT**: LocationSnapshot uses SQL `float` columns — map to C# `double` with `.HasColumnType("float")` in config.

  **Must NOT do**:
  - Do NOT add FK navigation to Booking.TourBooking or Booking.TourGuide (cross-module)
  - Do NOT add FK navigation to ContentTours.TourWaypoint (cross-module)

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: SQL float to double mapping, GPS coordinate precision, cross-module logical references
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1
  - **Blocks**: Task 12
  - **Blocked By**: None

  **References**:
  - ALL references from Task 1 apply
  - SQL DDL: `[Tracking].*` all 3 table definitions

  **Acceptance Criteria**:
  - [ ] 5 .csproj files, 3 entities, 2 enums, 4 configs (3 entities + OutboxMessage)
  - [ ] LocationSnapshot uses double (not float) for Accuracy, Speed, Heading, Altitude
  - [ ] LocationSnapshot config uses HasColumnType("float") for double columns
  - [ ] Coordinates use HasPrecision(10, 8) and HasPrecision(11, 8)
  - [ ] TourCheckpoint has unique index on (SessionId, WaypointId)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Module compiles + float mapping
    Tool: Bash
    Steps:
      1. dotnet build Tracking.Infrastructure/Tracking.Infrastructure.csproj
      2. grep 'double' Tracking.Domain/Entities/LocationSnapshot.cs
      3. grep 'HasColumnType.*float' Tracking.Infrastructure/Persistence/Configurations/LocationSnapshotConfiguration.cs
    Expected Result: Build succeeds, double properties exist, float column type configured
    Evidence: .sisyphus/evidence/task-11-build.txt
  ```

  **Commit**: YES
  - Message: `feat(tracking): add entity models and configurations for Tracking module`
  - Pre-commit: `dotnet build Tracking.Infrastructure/Tracking.Infrastructure.csproj`

- [ ] 12. Wire All Modules into YallaJo.Api

  **What to do**:
  Wire all 11 new modules into the API host project. This is a pure integration task — no new entities.

  1. Update `YallaJo.Api/YallaJo.Api.csproj` — add 22 new ProjectReferences:
     - 11 Presentation projects: ContentCore.Presentation, ContentPlaces.Presentation, ContentTours.Presentation, ContentBlogs.Presentation, ContentSeo.Presentation, Analytics.Presentation, Booking.Presentation, Finance.Presentation, Messaging.Presentation, Social.Presentation, Tracking.Presentation
     - 11 Infrastructure projects: ContentCore.Infrastructure, ContentPlaces.Infrastructure, ContentTours.Infrastructure, ContentBlogs.Infrastructure, ContentSeo.Infrastructure, Analytics.Infrastructure, Booking.Infrastructure, Finance.Infrastructure, Messaging.Infrastructure, Social.Infrastructure, Tracking.Infrastructure
  2. Update `YallaJo.Api/Program.cs` — add 22 new service registrations + 11 endpoint mappings:
     ```csharp
     // Add using statements for all 11 modules
     // Under Module registrations section:
     builder.Services.AddContentCoreApplication();
     builder.Services.AddContentCoreInfrastructure(builder.Configuration);
     // ... repeat for all 11 modules
     // Under Module endpoints section:
     app.MapContentCoreEndpoints();
     // ... repeat for all 11 modules
     ```
  3. Update `YallaJo.sln` — add all 55 new projects with proper solution folders:
     - Use `dotnet sln add --solution-folder "src/Modules/ContentCore"` pattern
     - Solution folders: ContentCore, ContentPlaces, ContentTours, ContentBlogs, ContentSeo, Analytics, Booking, Finance, Messaging, Social, Tracking

  **Must NOT do**:
  - Do NOT modify existing module registrations (Auth, Accounts, Security)
  - Do NOT change the order of existing middleware or service registrations
  - Do NOT register OutboxProcessor for new modules

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Mechanical wiring task — no complex logic, just adding references and registration calls following the exact existing pattern
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 2 (sequential, after all Wave 1 tasks)
  - **Blocks**: Task 13
  - **Blocked By**: Tasks 1-11

  **References**:
  - `YallaJo.Api/Program.cs` — Existing registration pattern to follow exactly (lines 17-24 for services, lines 88-90 for endpoints)
  - `YallaJo.Api/YallaJo.Api.csproj` — Existing ProjectReference pattern (lines 20-27)

  **Acceptance Criteria**:
  - [ ] YallaJo.Api.csproj has 22 new ProjectReferences (11 Presentation + 11 Infrastructure)
  - [ ] Program.cs has 11 new Add{Module}Application() calls
  - [ ] Program.cs has 11 new Add{Module}Infrastructure() calls
  - [ ] Program.cs has 11 new Map{Module}Endpoints() calls
  - [ ] YallaJo.sln lists all 74 projects (19 + 55)
  - [ ] Existing registrations unchanged (Auth, Accounts, Security still present)

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Full solution builds
    Tool: Bash
    Steps:
      1. dotnet build YallaJo.sln --configuration Release
      2. dotnet sln YallaJo.sln list | wc -l (or Measure-Object on PowerShell)
    Expected Result: Build succeeded with 0 errors, 74 projects listed
    Failure Indicators: Any CS* error, missing project references, project count != 74
    Evidence: .sisyphus/evidence/task-12-build.txt

  Scenario: Existing modules untouched
    Tool: Bash (git)
    Steps:
      1. git diff --name-only -- "Auth.*" "Accounts.*" "Security.*" "YallaJo.SharedKernel.*"
    Expected Result: Empty output (no modifications to existing module code)
    Evidence: .sisyphus/evidence/task-12-unchanged.txt

  Scenario: All registrations present
    Tool: Bash (grep)
    Steps:
      1. grep -c 'AddContent' YallaJo.Api/Program.cs → expect 10 (5 modules × 2: App + Infra)
      2. grep -c 'MapContent' YallaJo.Api/Program.cs → expect 5
      3. grep -c 'Add.*Application' YallaJo.Api/Program.cs → expect 14 (3 existing + 11 new)
      4. grep -c 'Map.*Endpoints' YallaJo.Api/Program.cs → expect 14
    Expected Result: All counts match
    Evidence: .sisyphus/evidence/task-12-registrations.txt
  ```

  **Commit**: YES
  - Message: `feat(api): wire all 11 new modules into API host`
  - Files: `YallaJo.Api/Program.cs`, `YallaJo.Api/YallaJo.Api.csproj`, `YallaJo.sln`
  - Pre-commit: `dotnet build YallaJo.sln --configuration Release`

- [ ] 13. Full Build Verification + LSP Diagnostics + File Count Audit

  **What to do**:
  Comprehensive verification that the entire migration is correct and complete.

  1. Run `dotnet build YallaJo.sln --configuration Release` — must succeed with 0 errors
  2. Run LSP diagnostics on ALL new .cs files — must show 0 errors
  3. Verify file counts per module:
     - Content.Core: 9 entities, 3 enums, 10 configs
     - Content.Places: 7 entities, 4 enums, 8 configs
     - Content.Tours: 8 entities, 2 enums, 9 configs
     - Content.Blogs: 5 entities, 2 enums, 6 configs
     - Content.SEO: 4 entities, 1 enum, 5 configs
     - Analytics: 6 entities, 1 enum, 7 configs
     - Booking: 11 entities, 5 enums, 12 configs
     - Finance: 16 entities, 10 enums, 17 configs
     - Messaging: 8 entities, 6 enums, 9 configs
     - Social: 5 entities, 4 enums, 6 configs
     - Tracking: 3 entities, 2 enums, 4 configs
  4. Verify all DbContexts have correct schema names
  5. Verify git diff shows NO changes to existing modules
  6. Count total entity files across all modules — expect ~82
  7. Spot-check 3-5 entities against SQL DDL for property completeness

  **Must NOT do**:
  - Do NOT modify any files — this is verification only
  - Do NOT create migrations

  **Recommended Agent Profile**:
  - **Category**: `deep`
    - Reason: Comprehensive audit requiring reading many files, running build, checking counts, and cross-referencing against plan
  - **Skills**: []

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (sequential, after Task 12)
  - **Blocks**: F1-F4
  - **Blocked By**: Task 12

  **References**:
  - This plan document (file counts, module names, schema names)
  - All SQL DDL table definitions for spot-checking

  **Acceptance Criteria**:
  - [ ] dotnet build succeeds with 0 errors
  - [ ] LSP diagnostics: 0 errors on all new files
  - [ ] File counts match plan per module
  - [ ] All 11 DbContexts have correct schema names
  - [ ] Existing modules unmodified

  **QA Scenarios (MANDATORY):**

  ```
  Scenario: Complete solution health check
    Tool: Bash
    Preconditions: All 12 previous tasks completed
    Steps:
      1. dotnet build YallaJo.sln --configuration Release 2>&1 | tee build-output.txt
      2. grep 'error' build-output.txt | wc -l → expect 0
      3. dotnet sln YallaJo.sln list | Measure-Object → expect 74 projects
      4. git diff --name-only -- "Auth.*" "Accounts.*" "Security.*" "YallaJo.SharedKernel.*" → expect empty
    Expected Result: 0 build errors, 74 projects, 0 changes to existing modules
    Evidence: .sisyphus/evidence/task-13-health.txt

  Scenario: DbContext schema audit
    Tool: Bash (grep)
    Steps:
      1. For each module, grep 'HasDefaultSchema' in DbContext file
      2. Verify schema names: content_core, content_places, content_tours, content_blogs, content_seo, analytics, booking, finance, messaging, social, tracking
    Expected Result: All 11 schemas correct
    Evidence: .sisyphus/evidence/task-13-schemas.txt
  ```

  **Commit**: NO (verification only)

---

## Final Verification Wave (MANDATORY — after ALL implementation tasks)

> 4 review agents run in PARALLEL. ALL must APPROVE. Rejection → fix → re-run.

- [ ] F1. **Plan Compliance Audit** — `oracle`
  Read the plan end-to-end. For each "Must Have": verify implementation exists (read file, check entity, check config). For each "Must NOT Have": search codebase for forbidden patterns — reject with file:line if found. Check evidence files exist in .sisyphus/evidence/. Compare deliverables against plan.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [ ] F2. **Code Quality Review** — `unspecified-high`
  Run `dotnet build YallaJo.sln --configuration Release`. Review all new entity files for: missing `HasQueryFilter` on AuditableEntity configs, missing `ValueGeneratedNever()` on Guid PKs, missing `IsRowVersion()` on RowVersion, `as any` equivalents, empty catches, unused usings. Check naming conventions match existing modules.
  Output: `Build [PASS/FAIL] | Entities [N reviewed/N issues] | Configs [N reviewed/N issues] | VERDICT`

- [ ] F3. **Real Manual QA** — `unspecified-high`
  Start from clean state. Verify `dotnet build` succeeds. Count projects in solution (expect 74). Verify each DbContext has correct schema name. Verify each module's DI extension exists and follows the pattern. Spot-check 3 entities per module against SQL DDL for property completeness. Save evidence.
  Output: `Build [PASS/FAIL] | Projects [N/74] | DbContexts [N/11] | Spot-checks [N/N pass] | VERDICT`

- [ ] F4. **Scope Fidelity Check** — `deep`
  For each task: read "What to do", read actual diff (git log/diff). Verify 1:1 — everything in spec was built (no missing), nothing beyond spec was built (no creep). Check "Must NOT do" compliance — verify Auth.*, Accounts.*, Security.*, SharedKernel.* are UNMODIFIED. Flag unaccounted changes.
  Output: `Tasks [N/N compliant] | Existing modules [CLEAN/MODIFIED] | Unaccounted [CLEAN/N files] | VERDICT`

---

## Commit Strategy

Each module task (Tasks 1-11) should commit independently:
- **Task 1**: `feat(content-core): add entity models and configurations for Content.Core module`
- **Task 2**: `feat(content-places): add entity models and configurations for Content.Places module`
- **Task 3**: `feat(content-tours): add entity models and configurations for Content.Tours module`
- **Task 4**: `feat(content-blogs): add entity models and configurations for Content.Blogs module`
- **Task 5**: `feat(content-seo): add entity models and configurations for Content.SEO module`
- **Task 6**: `feat(analytics): add entity models and configurations for Analytics module`
- **Task 7**: `feat(booking): add entity models and configurations for Booking module`
- **Task 8**: `feat(finance): add entity models and configurations for Finance module`
- **Task 9**: `feat(messaging): add entity models and configurations for Messaging module`
- **Task 10**: `feat(social): add entity models and configurations for Social module`
- **Task 11**: `feat(tracking): add entity models and configurations for Tracking module`
- **Task 12**: `feat(api): wire all 11 new modules into API host`
- **Task 13**: No commit (verification only)

---

## Success Criteria

### Verification Commands
```bash
dotnet build YallaJo.sln --configuration Release  # Expected: Build succeeded, 0 errors
dotnet sln YallaJo.sln list | wc -l              # Expected: 74 (19 existing + 55 new)
git diff --name-only -- "Auth.*" "Accounts.*" "Security.*" "YallaJo.SharedKernel.*"  # Expected: empty
```

### Final Checklist
- [ ] All 84 entities from SQL DDL have corresponding C# classes (excluding Security schema tables already in existing modules)
- [ ] All entities have `IEntityTypeConfiguration<T>` classes
- [ ] All 11 modules have DbContext with correct schema
- [ ] All cross-module references are logical (plain Guid, no navigation)
- [ ] All monetary columns use `HasPrecision(19, 4)`
- [ ] All composite PK tables use `HasKey(x => new { })`
- [ ] Program.cs has all 11 new module registrations
- [ ] Solution file has all 55 new projects
- [ ] Existing modules (Auth, Accounts, Security, SharedKernel) are UNMODIFIED
- [ ] `dotnet build` passes with 0 errors
