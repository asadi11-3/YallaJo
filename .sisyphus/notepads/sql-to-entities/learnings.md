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
