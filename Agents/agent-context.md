# YallaJo — Agent Onboarding & Progress Context

> **Last Updated**: 2026-03-15 | **Build State**: 0 errors, 0 warnings

> **Purpose**: Single source of truth for any AI agent working on YallaJo. **Read this entire file once at the start of every session.** Every section contains rules you must follow — do not skip any.

---

## 🔖 MANDATORY: Reference Files

Before doing ANY work, read these files from `Agents/`:

| File | Contains | When to Read |
|------|----------|-------------|
| `Agents/guide.md` | **HOW to build** — layer anatomy, CQRS patterns, events, repos, UoW, Result pattern, Specs, Value Objects, pipeline behaviors, translation system, code templates, EF config patterns, DI patterns (~2162 lines) | **ALWAYS** — this is the code pattern bible |
| `Agents/YallaJo.md` | **WHAT to build** — all ~196 endpoints across 4 phases, middleware, 18 background services, 3 SignalR hubs, business logic (~1968 lines) | When implementing new features or endpoints |
| `Agents/Endpoints.pdf` | Endpoint definitions (visual) | When implementing endpoints |
| `Agents/YallaJo Business Rules & Edge Cases.pdf` | Business rules & edge cases (63 pages, 24 sections) — state machines, validation, booking, payments, reviews, etc. | **ALWAYS when implementing any module** — this is the business logic bible |
| `Agents/error-log.md` | Mistakes previous agents made — with root cause and prevention rules | **ALWAYS** — read before writing any code |

**`guide.md` = code patterns. `Business Rules PDF` = business logic. Never guess — look it up.**

### ⛔ Business Rules PDF Rule (NON-NEGOTIABLE)

Before implementing ANY domain logic, validator, state machine, or edge case handling for ANY module, you MUST:

1. **Read the relevant section** of `Agents/YallaJo Business Rules & Edge Cases.pdf` for that module
2. **Extract every rule** that applies to the entity/feature you're building
3. **Implement every rule** — do not skip rules because they seem edge-case-y. They exist for a reason.
4. **If a rule is ambiguous**, ask the user — do not interpret it yourself

The PDF covers: provider registration flows, tour/place management rules, booking & slot locking logic, payment & refund policies, review/rating algorithms, wishlist notifications, SEO/sitemap rules, blog management, map display rules, weather integration, notification system, children-friendly/accessibility rules, packaging system, subscriptions & feature gating, referral & loyalty points, dispute center state machines, recommendation engine, AI chatbot rules, accessibility UI, discount & promotions with stacking/payout rules.

**If you implement a module without reading its section in this PDF, your work is considered incomplete and must be redone.**

---

## ⚠️ Critical Discoveries & Gotchas

These are hard-won lessons. **Read before writing any code.**

| # | Gotcha | What Happens If You Ignore It |
|---|--------|-------------------------------|
| 1 | **Domain events on non-`IAggregateRoot` entities are NEVER dispatched** | `UnitOfWork.SaveChangesAsync()` only collects events from `IAggregateRoot` entries. Your handler silently never runs. |
| 2 | **NEVER call `SaveChangesAsync` in domain event handlers** | UoW dispatches events BEFORE SaveChanges. Handler changes are persisted atomically. Calling Save in handler breaks atomicity + can double-save. |
| 3 | **No Hangfire/Quartz** — background jobs use `BackgroundService` + `Channel<T>` | Don't add Hangfire packages. Use the existing in-memory queue pattern. |
| 4 | **`MarkUpdated()` exists ONLY on `AuditableEntity`, NOT `BaseEntity`** | For BaseEntity children: `UpdatedAt = DateTime.UtcNow;` directly (protected set). |
| 5 | **`BaseEntity.UpdatedAt` has `protected set`** | Settable inside entity methods only. |
| 6 | **`EfRepository<T>` requires `IAggregateRoot` constraint** | Non-aggregate entities → `EfEntityRepository<T, TKey>`. |
| 7 | **Junction tables are plain classes** — no base class, composite keys, DbContext-direct repos | Don't extend BaseEntity for junction tables. |
| 8 | **`LoginCommand` signature**: `LoginCommand(string Email, string Password)` | Only 2 params — no username, no rememberMe. |
| 9 | **`Result<T>` errors**: `result.Errors` = `IReadOnlyList<Error>`, `result.Messages` = `IReadOnlyList<string>` | `Error` is `record Error(string Code, string Message)`. |
| 10 | **DI registration is the #1 most forgotten step** | Every repo, service, UoW MUST be registered in `DependencyInjection.cs` or you get runtime `InvalidOperationException`. |
| 11 | **Business Rules PDF is MANDATORY before implementing any module** | `Agents/YallaJo Business Rules & Edge Cases.pdf` contains every validation rule, state machine, and edge case. If you skip it, you WILL miss critical business logic and your work must be redone. |

---

## 🚨 Common Mistakes & Fixes

### DI & Configuration Bugs

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Forgot DI registration | `InvalidOperationException: No service for type 'IXxxRepository'` | Add `services.AddScoped<IXxxRepository, XxxRepository>()` in `{Module}.Infrastructure/DependencyInjection.cs` |
| Used `EfRepository<T>` for non-aggregate | Build error: type constraint `IAggregateRoot` not satisfied | Use `EfEntityRepository<T, TKey>` instead |
| Forgot `ValueGeneratedNever()` in EF config | EF tries to auto-generate GUIDs, ignoring `Guid.CreateVersion7()` | Add `builder.Property(x => x.Id).ValueGeneratedNever();` |
| Forgot `HasQueryFilter(x => !x.IsDeleted)` on AuditableEntity | Soft-deleted records appear in queries | Add the global query filter in entity configuration |
| Registered wrong lifetime (Singleton for scoped dependency) | `Cannot consume scoped service from singleton` at runtime | Repos/UoW/DbContext = always `Scoped`. Background services = `Singleton` with `IServiceScopeFactory` to resolve scoped deps. |
| Forgot to add `DbSet<T>` to module DbContext | EF doesn't know about the entity — migrations won't create the table | Add `public DbSet<{Entity}> {Entities} => Set<{Entity}>();` to the module's DbContext |
| Module wiring missing in `Program.cs` | Endpoints return 404, handlers never execute | Add all 3 lines: `Add{Module}Application()`, `Add{Module}Infrastructure()`, `Map{Module}Endpoints()` |

### Domain & Event Bugs

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Called `SaveChangesAsync()` in event handler | Duplicate saves, broken atomicity, intermittent data corruption | Remove the call — UoW saves everything atomically after all handlers run |
| Raised domain event on `BaseEntity` | Event handler never executes, no error logged | Raise through the aggregate root, or use integration events |
| Modifying a different aggregate in a domain event handler | Inconsistent state — second aggregate may not save, or may corrupt transaction | Use integration event (outbox) instead. One transaction = one aggregate. |
| Entity has public setters | Any code can modify entity state bypassing business rules | All setters must be `private` or `private set`. Expose business methods instead (`Update()`, `Activate()`). |
| Forgot to raise domain event in factory method | Downstream handlers (translation, audit) never trigger | Every `Create()` factory MUST raise `{Entity}CreatedDomainEvent` before returning. |

### EF Core & Query Bugs

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Navigation property causes JSON circular reference | `JsonException: A possible object cycle was detected` on serialization | Never return entities from endpoints. Always map to DTOs. DTOs must not have circular navigation. |
| Forgot `.Include()` — accessed null navigation | `NullReferenceException` on `entity.RelatedEntity.Property` | Add `.Include(x => x.RelatedEntity)` to the query. Or use projection (`.Select()`) which auto-resolves. |
| N+1 query inside a loop | Endpoint takes 5+ seconds, DB hammered with individual queries | Batch-fetch with `.Include()` or `GetByIdsAsync()`. See Performance Rule P11. |
| Used `.ToListAsync()` without filter on large table | Out of memory, timeout on tables with 100K+ rows | Always filter and paginate. Never load an entire table. |
| EF tracking conflict — same entity loaded twice | `InvalidOperationException: entity with same key is already being tracked` | Use `AsNoTracking()` for reads. For writes, load from the SAME DbContext instance. |
| Migration merge conflict | Migration snapshot out of sync after merging branches | Run `dotnet ef migrations remove` then re-add the migration. Never hand-edit the snapshot. |
| Decimal precision loss | Price `99.99` stored as `100.00` or `99.9900000000` | Configure: `builder.Property(x => x.Price).HasPrecision(18, 2);` — always specify precision for money. |
| Used `DateTime.Now` instead of `DateTime.UtcNow` | Timestamps shift by UTC+3 (Jordan), sorting/filtering breaks | Always `DateTime.UtcNow`. See §DateTime Rules. |

### Async & Runtime Bugs

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Dropped `CancellationToken` | Request cancellation doesn't propagate, wasted server resources | Pass `ct` through every async call chain |
| Used `.Result` or `.Wait()` on async code | Deadlock — request hangs forever, thread pool starved | Always `await`. Never block on async. If you must call async from sync, use `Task.Run(() => ...).GetAwaiter().GetResult()` as absolute last resort. |
| Fire-and-forget without error handling | `Task.Run(() => DoWork())` — exceptions silently swallowed | Queue to `Channel<T>` background service. If fire-and-forget is unavoidable, wrap in try/catch with logging. |
| Disposed `HttpClient` per request | Socket exhaustion — `SocketException: address already in use` after ~100 requests | Use `IHttpClientFactory` (already configured in Web layer). Never `new HttpClient()`. |
| Used `Results.Ok()` for create endpoints | Returns 200 instead of 201 Created | Use `result.Outcome == Outcome.Created ? Results.Created(...) : Results.Ok(...)` |

### File & Media Bugs

| Mistake | Symptom | Fix |
|---------|---------|-----|
| File path traversal in upload filename | Attacker uploads `../../../etc/passwd` or `..\..\web.config` | Always sanitize: `Path.GetFileName(uploadedFileName)`. Never use user-provided paths directly. |
| Saved file without unique name | Files overwrite each other when two users upload `photo.jpg` | Generate unique name: `$"{Guid.CreateVersion7()}{Path.GetExtension(file.FileName)}"` |
| Image processing on request thread | Endpoint times out on large images, thread pool blocked | Queue to `Channel<T>` background service. Return 202 Accepted with a status URL. |
| No file type validation | User uploads `.exe` disguised as `.jpg` | Validate both extension AND MIME type. Check magic bytes for images (first 4-8 bytes of the file). |

---

## 🏎️ Performance Rules (MANDATORY)

Agents MUST follow these rules when writing any code. All code examples are in `guide.md`.

### P1 — Always Propagate CancellationToken

Every async method MUST accept and forward `CancellationToken ct`. Never drop it — dropped tokens = wasted server work on cancelled requests.

### P2 — DTO Size Rules

- **List endpoints** → return `SummaryDto` (5–8 key fields: Id, Name, Slug, Status, CreatedAt)
- **Detail endpoints** → return `DetailDto` (all relevant fields)
- **NEVER** return the full entity shape on list endpoints

### P3 — Pagination Defaults & Limits

Every list query MUST have pagination: default `PageSize = 20`, max `100`. Validator MUST enforce: `RuleFor(x => x.PageSize).InclusiveBetween(1, 100)`.

### P4 — Split Queries for Multiple Includes

When a query has 2+ `.Include()` calls, add `.AsSplitQuery()` to prevent cartesian row explosion.

### P5 — Projections Over Full Entity Loads

Use `.Select()` / `SelectAsync()` to load only needed columns. Never load full entity graphs then map in memory.

### P6 — Bulk Operations (3+ Records)

Use `AddRangeAsync()`, `RemoveRange()`, `ExecuteUpdateAsync()`, `ExecuteDeleteAsync()`. Never loop with individual saves (1 roundtrip vs N).

### P7 — No Lazy Loading

Never add `virtual` to navigation properties. Never install `Microsoft.EntityFrameworkCore.Proxies`. Always use explicit `.Include()`.

### P8 — AsNoTracking for Reads

All query handlers MUST use `asNoTracking: true` (repo default). Only disable when you intend to modify and save the entity.

### P9 — Index-Aware Filtering

- Every `Slug` → unique index
- Every FK → index (EF auto-handles navigation properties)
- Any column used in WHERE across 3+ queries → add explicit index
- Soft-delete filtered index: `.HasFilter("IsDeleted = 0")`

### P10 — Background Processing

Never run heavy processing (image resize, video metadata, translation API) on the request thread. Always queue to `Channel<T>` background service. Set timeouts on external calls.

### P11 — N+1 Query Prevention

Never fetch related data inside a loop. Use `.Include()` for single query, or batch-fetch with `GetByIdsAsync()`.

### P12 — Upload Size Limits

Image uploads: max 50 MB. Video uploads: max 500 MB. Always validate file type and size in the validator BEFORE processing.

---

## ⚡ MANDATORY: Update This File After Every Task

After completing ANY work, do ALL of the following before ending your session:

1. **"What Has Been Built" (Work Tracker)** — Add entry for every feature/entity/fix completed. Use `🤖 Agent` for your work, `👤 User` if user tells you they built something. Status: ✅ complete / 🟡 partial / ❌ broken
2. **"Module Status Overview"** — Update module's status and notes
3. **"What Needs To Be Done Next"** — Remove completed items, add new discoveries
4. **"Gotchas"** — Add any new gotchas (number sequentially from last)
5. **"Build State"** — Update error/warning count in header
6. **`Agents/error-log.md`** — Verify all errors encountered during this session are logged with root cause and prevention rule

**Why**: Next agent reads this file + the error log FIRST. Stale data = wrong decisions. Missing error entries = repeated mistakes.

---

## Project Overview

**YallaJo** — Tourism/Booking Modular Monolith, .NET 9, Clean Architecture per module, CQRS (MediatR), domain events, shared kernel.

### Tech Stack

`.NET 9` (SDK 10.0.200-preview) · `MediatR` · `EF Core` (SQL Server) · `FluentValidation` · `SixLabors.ImageSharp` · `FFMpegCore` · `Azure Translator API`

### Solution Structure

- **YallaJo.Api** (`:57065`) — REST API, JWT Bearer auth
- **YallaJo.Web** (`:57070`) — MVC admin UI, Cookie auth, HttpClient BFF (calls API)
- **YallaJo.SharedKernel** — Domain/Application/Infrastructure shared abstractions
- **14 Modules**: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, ContentBlogs, ContentSeo, Booking, Finance, Messaging, Social, Tracking, Analytics

---

## Module Status Overview

| Module | Status | Notes |
|--------|--------|-------|
| Auth | ✅ Complete | Pre-existing |
| Security | ✅ Complete | Pre-existing |
| Accounts | ✅ Complete | Pre-existing |
| ContentCore | ✅ Complete | Category (tree, depth, reorder, slug, translations, deactivate/activate), Specialization, Tag, EntityCategory, EntityTag, Attachment, Translation |
| ContentPlaces | ⬜ Not started | Entities exist, endpoints empty |
| ContentTours | ⬜ Not started | Entities exist, endpoints empty |
| ContentBlogs | ⬜ Not started | Entities exist, endpoints empty |
| ContentSeo | ⬜ Not started | Entities exist, endpoints empty |
| Booking | ⬜ Not started | Entities exist, endpoints empty |
| Finance | ⬜ Not started | Entities exist, endpoints empty |
| Messaging | ⬜ Not started | Entities exist, endpoints empty |
| Social | ⬜ Not started | Entities exist, endpoints empty |
| Tracking | ⬜ Not started | Entities exist, endpoints empty |
| Analytics | ⬜ Not started | Entities exist, endpoints empty |

---

## 📋 What Has Been Built (Work Tracker)

This section is the **single source of truth** for what exists in the codebase. It tracks ALL work — whether done by an agent or by the user.

### Tracking Rules (MANDATORY)

1. **After completing ANY feature, entity, endpoint, or fix** — add an entry here immediately. Do not wait until session end.
2. **User-completed work**: If the user tells you they built something, add it here with `👤 User` in the Built By column.
3. **Agent-completed work**: Add it with `🤖 Agent` in the Built By column.
4. **Partial work**: If work is incomplete, mark it 🟡 and describe exactly what's done and what remains.
5. **Broken/reverted work**: Mark it ❌ with the reason it's broken so the next agent knows not to build on it.
6. **Never remove entries** — only update their status. History matters.
7. **Number entries sequentially** — never reuse or skip numbers.

### Work Log

| # | Module / Feature | Status | Built By | Summary |
|---|-----------------|--------|----------|---------|
| 1 | Translation System | ✅ | 🤖 Agent | SharedKernel abstractions, `AzureTranslateService` (primary), `AutoSaveTranslationService` (DB cache decorator), `EntityTranslationOrchestrator`, domain + integration events via outbox/inbox |
| 2 | Category (Full CQRS) | ✅ | 🤖 Agent | `AuditableEntity` + `IAggregateRoot`, Create/Update/Delete/List/GetById, soft delete, endpoints at `/api/content-core/categories` |
| 3 | Tag (Full CQRS) | ✅ | 🤖 Agent | `BaseEntity` (NOT aggregate), `EfEntityRepository`, full REST endpoints |
| 4 | EntityCategory Junction | ✅ | 🤖 Agent | Plain class, composite key, AssignCategories/RemoveCategory/GetEntityCategories |
| 5 | EntityTag Junction | ✅ | 🤖 Agent | Plain class, composite key, AssignTags/RemoveTag/GetEntityTags |
| 6 | Attachment System | ✅ | 🤖 Agent | `Attachment.cs` + `EntityImage.cs`, `LocalFileStorageService` (→ Cloudinary later), domain events |
| 7 | Media Processing | ✅ | 🤖 Agent | ImageSharp thumbnails, FFMpegCore metadata, `BackgroundService` + `Channel<T>` queue |
| 8 | YallaJo.Web (Admin UI) | ✅ | 🤖 Agent | HttpClient BFF, `ApiClient`, `JwtAuthHandler`, `AuthController` (Login/Logout), `DashboardController` |
| 9 | Auth/Security/Accounts | ✅ | 👤 User | Pre-existing, fully implemented |
| 10 | Specialization (Full CQRS) | ✅ | 🤖 Agent | `AuditableEntity` (non-aggregate), Create/Update/List handlers + validators, endpoints at `/api/content-core/specializations` |
| 11 | Category Tree Structure | ✅ | 🤖 Agent | `ListCategories` returns nested tree (roots→children→grandchildren), `GetCategoryById` returns direct children, 3-level max depth enforced in `CreateCategory` |
| 12 | Category Slug Auto-Gen | ✅ | 🤖 Agent | `CreateCategoryCommand.Slug` is now optional (nullable); auto-generated from name via Slugify when not provided |
| 13 | Category Reorder Endpoint | ✅ | 🤖 Agent | `PUT /api/content-core/categories/reorder` — `ReorderCategoriesCommand` batch updates SortOrder for multiple categories |
| 14 | Merge Refactor: Category CQRS Optimization | ✅ | 🤖 Agent | Resolved local/remote conflict; clean `ICategoryRepository` with `GetAllWithTranslationsAsync` + `GetByIdWithTranslationsAsync` (no EF leakage into Domain); `CategoryRepository` does EF Include in Infrastructure only; `ReorderCategories` uses single batch query O(1) instead of N roundtrips; `CategoryDto` adds `Translations` collection + `WithTranslations` flag; Accept-Language header drives translation loading at endpoint layer; DELETE stays soft-delete; `PATCH /{id}/activate` + `PATCH /{id}/deactivate` added; `UpdateCategoryCommandHandler` adds depth validation on parent change + manual translation overrides; removed 6 redundant files from remote merge; 0 errors, 0 warnings |
| 15 | Caching + Polly Resilience | ✅ | 🤖 Agent | `ICacheableQuery` marker + `QueryCachingBehavior<,>` open-generic pipeline in SharedKernel; `ContentCoreCacheKeys` static key factory; all 11 query records implement ICacheableQuery (Languages 1h, Categories/Tags/Specs 30m, Attachments/Junctions 15m); 19 command handlers inject `IMemoryCache` and call `Remove()` on write; Azure Translator HttpClient: `AddStandardResilienceHandler` (Polly v8: 3x retry exp+jitter, circuit breaker, 10s timeout); packages: `Microsoft.Extensions.Http.Resilience` 9.4.0, `Microsoft.Extensions.Caching.Memory` |

---

## What Needs To Be Done Next

### Wave 1 — ContentCore Completion
- ~~Category tree, depth validation, slug auto-gen, reorder~~ ✅ Done
- ~~Specialization CQRS~~ ✅ Done
- Verify all endpoints end-to-end (recommend running Swagger after migration)
- EF Migrations — all ContentCore entity schemas were pre-existing; no new schema changes from this session's fixes

### Wave 2 — Phase 1 MVP (~80 endpoints)
- **ContentPlaces**: Places full CQRS (~16 endpoints)
- **ContentTours**: Tours full CQRS (~34 endpoints)
- **Booking**: Core booking state machine (~32 endpoints)
- **Finance**: Payments, payouts (~52 endpoints)
- **Social**: Reviews, favorites (~22 endpoints)

### Wave 3 — Phase 2+
- ContentBlogs, ContentSeo, Messaging, Analytics, Tracking
- Middleware: CorrelationId, RequestLocalization, RateLimiting, SeoRedirect, CORS, ResponseCompression
- 18 background services, 3 SignalR hubs

### Infrastructure
- EF Migrations for all entities
- File storage: Local → Cloudinary
- User builds remaining MVC admin controllers/views

---

## 🧱 New Entity Checklist

**MUST follow this exact sequence. Do NOT skip steps. All code patterns are in `guide.md`.**

### Step 1: Domain (`{Module}.Domain`)
- [ ] Entity in `Entities/` — choose base: `AuditableEntity + IAggregateRoot` | `BaseEntity` | plain class (junction)
- [ ] Private EF constructor: `private {Entity}() { }`
- [ ] Factory: `public static {Entity} Create(...)` — `Guid.CreateVersion7()`, raise domain event inside
- [ ] Business methods: `Update()`, `Activate()`, `SoftDelete()` (if AuditableEntity)
- [ ] Domain events in `Events/`
- [ ] Repository interface in `Repositories/` (see repo selection table below)

### Step 2: Application (`{Module}.Application`)
- [ ] `Commands/{Entity}/Create{Entity}/` — Command, Handler, Validator
- [ ] `Commands/{Entity}/Update{Entity}/` — Command, Handler, Validator
- [ ] `Commands/{Entity}/Delete{Entity}/` — Command, Handler, Validator
- [ ] `Queries/{Entity}/List{Entities}/` — Query (with SummaryDto), Handler (paginated, AsNoTracking)
- [ ] `Queries/{Entity}/Get{Entity}ById/` — Query (with DetailDto), Handler
- [ ] Domain event handlers in `EventHandlers/` (if needed)

### Step 3: Infrastructure (`{Module}.Infrastructure`)
- [ ] EF config in `Persistence/Configurations/` (MUST follow `guide.md` patterns)
- [ ] Repository in `Repositories/`
- [ ] `DbSet<{Entity}>` in module's DbContext
- [ ] **⚠️ Register in `DependencyInjection.cs`** — #1 most forgotten step
- [ ] EF migration: `dotnet ef migrations add Add{Entity} --project {Module}.Infrastructure --startup-project YallaJo.Api`

### Step 4: Presentation (`{Module}.Presentation`)
- [ ] Endpoint mapping in `{Module}Endpoints.cs`
- [ ] Wire in main `Map{Module}Endpoints()` method
- [ ] Request DTOs at bottom of file

### Step 5: Verify
- [ ] `dotnet build` — 0 errors
- [ ] `lsp_diagnostics` on all changed files
- [ ] Test via Swagger

### Repository Selection

| Entity Type | Interface | Implementation |
|-------------|-----------|----------------|
| Aggregate Root (`IAggregateRoot`) | `IRepository<T, Guid>` | `EfRepository<T>` |
| Non-aggregate (`BaseEntity` / `AuditableEntity`) | `IReadRepository<T,TKey>` + `IWriteRepository<T,TKey>` | `EfEntityRepository<T, TKey>` |
| Junction table (plain class) | Custom interface | DbContext-direct |

---

## 🔧 Scaffold & Template Usage Rules

### Using the Scaffold Script

When creating a new entity, ALWAYS use the scaffold script first:

```powershell
.\Agents\scaffold.ps1 -Module {Module} -Entity {Entity} -Schema {schema}
```

### Post-Scaffold Mandatory Review (NON-NEGOTIABLE)

After running `scaffold.ps1`, you MUST review and customize EVERY generated file before building. The scaffold creates generic boilerplate - your job is to make it production-ready.

**Review Checklist - go through EVERY generated file:**

1. **Entity file** (`{Module}.Domain/Entities/{Entity}.cs`)
   - [ ] Add ALL entity-specific properties from the spec (`YallaJo.md` + Business Rules PDF)
   - [ ] Add ALL entity-specific business methods (state transitions, validation logic)
   - [ ] Add navigation properties (NO `virtual` keyword - lazy loading banned)
   - [ ] Verify guard clauses in `Create()` and `Update()` cover all required fields
   - [ ] Verify domain events carry all necessary data

2. **Domain Events** (`{Module}.Domain/Events/`)
   - [ ] Add ALL fields the event handlers will need (not just Id and Name)
   - [ ] Create additional events for entity-specific state changes (e.g., `BookingCancelledDomainEvent`)

3. **Command/Query files** (`{Module}.Application/`)
   - [ ] Add ALL entity-specific fields to Command records
   - [ ] Add uniqueness checks in handlers (slug, email, etc.)
   - [ ] Add FK validation in handlers (verify referenced entities exist)
   - [ ] Add ALL business rule validation from Business Rules PDF
   - [ ] Add ILogger to every handler constructor
   - [ ] Verify SummaryDto has only 5-8 fields (not full entity)
   - [ ] Verify DetailDto has ALL relevant fields
   - [ ] Add pagination validation to list query

4. **Validators** (`{Module}.Application/Commands/`)
   - [ ] Add validation rules for EVERY input field
   - [ ] Add `.HasMaxLength()` matching the EF config
   - [ ] Add range validation for numeric fields
   - [ ] Add `.IsInEnum()` for enum fields
   - [ ] Add `.NotEqual(Guid.Empty)` for FK references

5. **EF Configuration** (`{Module}.Infrastructure/Persistence/Configurations/`)
   - [ ] Add ALL entity-specific property configurations
   - [ ] Add `.HasPrecision(18, 2)` for decimal/money fields
   - [ ] Add `.HasConversion<int>()` for enum fields
   - [ ] Add `.IsUnicode(false)` for ASCII-only fields (slugs, codes)
   - [ ] Add ALL relationships (HasMany, HasOne, FK, delete behavior)
   - [ ] Add ALL indexes (unique, filtered, composite)
   - [ ] Verify `HasQueryFilter(!IsDeleted)` is present

6. **Endpoints** (`{Module}.Presentation/`)
   - [ ] Add ALL endpoint-specific request parameters
   - [ ] Verify EVERY endpoint has `.RequireAuthorization()` or `.AllowAnonymous()`
   - [ ] Verify EVERY endpoint has `.WithName()`, `.WithSummary()`, `.Produces<T>()`
   - [ ] Add `.ProducesValidationProblem()` on POST/PUT
   - [ ] Add `.ProducesProblem(404)` on GET/{id}, PUT, DELETE

7. **DI Registration** (`{Module}.Infrastructure/DependencyInjection.cs`)
   - [ ] Verify the new repository is registered
   - [ ] Verify ALL new services are registered

8. **DbContext**
   - [ ] Verify `DbSet<{Entity}>` is added

### After Review: Build & Verify

```bash
dotnet build  # MUST pass with 0 errors
```

Then run `lsp_diagnostics` on every changed file.

**Scaffold output is NEVER production-ready. If you skip the review, you WILL ship broken or incomplete code.**

### Using Templates Manually

If not using the scaffold script, copy templates from `Agents/templates/`. Same review rules apply - every template file MUST be reviewed and customized before use.

---

## 📦 DI Registration Rules

**Every service/repository MUST be registered or you get runtime `InvalidOperationException`.**

| What | Where | Lifetime |
|------|-------|----------|
| Repos, UoW, DbContext, Module services | `{Module}.Infrastructure/DependencyInjection.cs` | Scoped |
| Background services | Same file | Singleton queue + `AddHostedService` |
| SharedKernel behaviors | `SharedKernel.Infrastructure/DependencyInjection.cs` | Auto (MediatR) |
| Cross-cutting (ICurrentUser, IRequestContext) | `YallaJo.Api/Program.cs` | Scoped |
| Auth policies | `YallaJo.Api/Program.cs` | Singleton |

**Reference implementation**: See `ContentCore.Infrastructure/DependencyInjection.cs`

**Wiring in Program.cs** — every module needs exactly 3 lines:
```csharp
builder.Services.Add{Module}Application();
builder.Services.Add{Module}Infrastructure(builder.Configuration);
app.Map{Module}Endpoints();
```

---

## 🗄️ Caching Rules

**Current state**: No caching infrastructure. All reads hit DB directly. Only exception: `AutoSaveTranslationService` saves translations to DB table (not in-memory).

**MUST add caching when**: Queries are frequently called with the same parameters and the data changes infrequently (categories, tags, languages).

**Required approach**: Start with `IMemoryCache` -> invalidate on writes -> migrate to Redis (`IDistributedCache`) when scaling.

**Cache key convention**: `{entity}:{scope}:{params}` — e.g., `categories:all`, `categories:{id}`, `tags:active`

---

## 🔗 Module Dependency Rules

```
SharedKernel.Domain         → (no dependencies)
SharedKernel.Application    → SharedKernel.Domain
SharedKernel.Infrastructure → SharedKernel.Application + Domain

{Module}.Domain             → SharedKernel.Domain
{Module}.Application        → {Module}.Domain + Contracts + SharedKernel.Application
{Module}.Infrastructure     → {Module}.Application + Domain + Contracts + SharedKernel.Infrastructure
{Module}.Presentation       → {Module}.Application ONLY (never Infrastructure or Domain)

YallaJo.Api                 → All Presentation + All Infrastructure + SharedKernel.Infrastructure
YallaJo.Web                 → ZERO project references (pure HttpClient)
```

**Critical**: Presentation NEVER references Infrastructure/Domain. Application NEVER references Infrastructure. Cross-module = Contracts or integration events. Circular reference = build failure.

---

## 🔐 Auth & Authorization

**JWT**: Configured in `appsettings.json` under `"Jwt"`. Claims: `sub` (User ID), `role`.

**Policies** (hierarchical — each includes higher roles):

| Policy | Roles Included |
|--------|---------------|
| `"Owner"` | Owner only |
| `"SuperAdmin"` | Owner, SuperAdmin |
| `"Admin"` | Owner, SuperAdmin, Admin |

**Roles**: Owner, SuperAdmin, Admin, User, TourGuide, Guest (from `Security.Contracts.Authorization.AppRoles`)

**Endpoint auth**: `.RequireAuthorization("Admin")` · `.AllowAnonymous()` · `.RequireAuthorization()` (any valid JWT)

**Permission-based**: `PermissionPolicyProvider` + `PermissionAuthorizationHandler` for fine-grained checks.

**Web BFF flow**: Browser → Web (:57070, cookie) → `JwtAuthHandler` extracts JWT from cookie → HttpClient → API (:57065, bearer)

---

## ✅ Validation & Error Flow

**Pipeline**: Request → `LoggingBehavior` → `ValidationBehavior` (FluentValidation) → `PerformanceBehavior` (>500ms warning) → Handler → `Result<T>`

**Validation failure**: `ValidationException` → `ValidationExceptionHandler` → RFC 7807 `ValidationProblemDetails` (400)

**Result → HTTP mapping** (via `ToApiResult()`):

| Result | HTTP |
|--------|------|
| Success | 200 OK |
| Created | 201 Created |
| NotFound | 404 |
| Conflict | 409 |
| Validation | 400 |

---

## 📁 Key File Locations

### SharedKernel
- `YallaJo.SharedKernel.Domain/Entities/` — BaseEntity, AuditableEntity, IAggregateRoot
- `YallaJo.SharedKernel.Domain/Abstractions/Results/` — Result, ResultT, Error
- `YallaJo.SharedKernel.Application/Abstractions/Messaging/` — ICommand, IQuery
- `YallaJo.SharedKernel.Infrastructure/Data/Repositories/` — EfRepository (aggregate), EfEntityRepository (non-aggregate)

### ContentCore (reference implementation)
- `ContentCore.Domain/Entities/` — Category, Tag, EntityCategory, EntityTag, Attachment, EntityImage
- `ContentCore.Domain/Enums/EntityType.cs` — Place, Tour, Business, Review, Blog, TourGuide
- `ContentCore.Application/Commands/` + `Queries/` — all CQRS
- `ContentCore.Infrastructure/DependencyInjection.cs` — reference DI registration
- `ContentCore.Presentation/ContentCoreEndpoints.cs` — reference endpoint wiring

### Web & API
- `YallaJo.Api/Program.cs` — JWT, module wiring
- `YallaJo.Web/Services/ApiClient.cs` + `JwtAuthHandler.cs` — BFF infrastructure
- `YallaJo.Web/Controllers/AuthController.cs` — reference MVC controller

---

## 🚀 How to Run & Verify

| Command | What It Does |
|---------|-------------|
| `dotnet run --project YallaJo.Api` | API on `https://localhost:57065` |
| `dotnet run --project YallaJo.Web` | Admin UI on `https://localhost:57070` |
| `dotnet build` | Build solution (expect 0 errors) |
| Swagger | `https://localhost:57065/swagger` (dev only) |
| Health | `GET https://localhost:57065/health` (anonymous) |

**Prerequisites**: .NET SDK 10.0.200-preview, SQL Server (LocalDB/SQLEXPRESS), ffmpeg 8.0.1 (PATH)

**Connection strings**: `appsettings.{Environment}.json` — Dev DB: `YallaJo_Dev`, Prod: `YallaJo`

---

## 📋 Constraints (From User)

- Translation service: API-based, auto-translate, pluggable (Azure primary)
- Events: Use outbox/inbox pattern
- File storage: Local now, Cloudinary later — pluggable via `IFileStorageService`
- YallaJo.Api = pure API. YallaJo.Web = separate UI layer via HttpClient BFF
- Web: Only AuthController built by agent — user handles rest
- MUST follow `guide.md` exactly for all code patterns
- MUST follow `YallaJo.md` + Business Rules PDF for business logic

---

## ⚙️ Agent Efficiency Rules

These rules minimize wasted work and token consumption. MUST follow them strictly.

| Rule | Detail |
|------|--------|
| **Build once at the end** | Run `dotnet build` only after completing all files for a full entity/feature. Create all files first, THEN build once. |
| **Create files in one batch** | Create ALL files in one pass: Domain -> Application -> Infrastructure -> Presentation. Do NOT build between layers. |
| **Reuse ContentCore as template** | Copy existing files from ContentCore (the reference implementation), then rename and modify. Do NOT generate from scratch. |
| **Read docs before searching code** | Check `guide.md`, `YallaJo.md`, and this file BEFORE searching the codebase. If the answer is documented, do NOT grep for it. |
| **Read each file once per session** | Read a file once per session and retain its contents. Re-reading the same file wastes tokens. |
| **Open known paths directly** | If you know the file path (from Section Key File Locations or this document), open it directly. Do NOT search for files whose locations are already documented. |

---

## 📛 Naming Conventions

Every name in this codebase follows a strict convention. Do not deviate.

| What | Convention | Example |
|------|-----------|---------|
| Error codes | `{Entity}.{Reason}` | `Category.NotFound`, `Tour.SlugConflict`, `Booking.AlreadyCancelled` |
| Commands | `{Verb}{Entity}Command` | `CreatePlaceCommand`, `UpdateTourCommand`, `DeleteBookingCommand` |
| Queries | `{Verb}{Entity}Query` | `ListToursQuery`, `GetPlaceByIdQuery` |
| DTOs (list) | `{Entity}SummaryDto` | `TourSummaryDto`, `PlaceSummaryDto` |
| DTOs (detail) | `{Entity}DetailDto` | `TourDetailDto`, `PlaceDetailDto` |
| Domain events | `{Entity}{PastTenseVerb}DomainEvent` | `PlaceCreatedDomainEvent`, `BookingCancelledDomainEvent` |
| Validators | `{CommandName}Validator` | `CreatePlaceCommandValidator` |
| EF configs | `{Entity}Configuration` | `PlaceConfiguration`, `TourConfiguration` |
| Migrations | `Add{Entity}` / `Update{Entity}{Change}` | `AddPlace`, `UpdateTourAddCapacity` |
| Repositories | `I{Entity}Repository` → `{Entity}Repository` | `IPlaceRepository` → `PlaceRepository` |
| Endpoints file | `{Module}Endpoints.cs` | `ContentPlacesEndpoints.cs` |
| DB schemas | lowercase with underscores | `content_core`, `content_places`, `booking` |

---

## 🔒 Security Rules

These are non-negotiable. Every agent MUST follow them on every task.

### Authentication & Authorization

| Rule | Detail |
|------|--------|
| **Every endpoint MUST have explicit auth** | Either `.RequireAuthorization()` or `.AllowAnonymous()`. Implicit auth = invisible bugs. |
| **Use policy-based auth, not role checks in code** | Use `.RequireAuthorization("Admin")` — not `if (user.Role == "Admin")` in handlers. Policies are centralized and auditable. |
| **IDOR protection on every data-access endpoint** | Always verify the authenticated user owns or has permission to access the requested resource. `GET /bookings/{id}` must check that the booking belongs to the current user (or user is admin). Never trust the client-provided ID alone. |
| **JWT key minimum 256 bits (32 chars)** | Shorter keys are brute-forceable. The key in `appsettings.json` must be at least 32 characters. |
| **Never expose user enumeration** | Login/register errors must not reveal whether an email exists. Use generic messages: "Invalid email or password" — not "User not found" or "Wrong password". |
| **Account lockout after failed attempts** | After 5 consecutive failed login attempts, lock the account for 15 minutes. Track via `FailedLoginCount` and `LockoutEnd` on the user entity. |

### Input Security

| Rule | Detail |
|------|--------|
| **Always validate input via FluentValidation** | First line of defense. Never trust client data. |
| **Never use raw SQL with string interpolation** | `dbContext.Database.ExecuteSqlRaw($"SELECT * FROM Users WHERE Name = '{name}'")` = SQL injection. Use parameterized: `ExecuteSqlRaw("SELECT * FROM Users WHERE Name = {0}", name)` or LINQ queries. |
| **Sanitize HTML in user-generated content** | Tour descriptions, reviews, blog posts — any rich text must be sanitized to prevent XSS. Strip `<script>`, `onclick`, `javascript:` etc. Use a whitelist approach (allow only safe tags). |
| **Validate file uploads beyond extension** | Check MIME type AND magic bytes (file signature). Reject executables, scripts, and unexpected types. Rename files to generated names — never preserve user-provided filenames in storage. |
| **Regex DoS (ReDoS) protection** | Never use user input directly in `Regex` patterns. For slug validation or search, use pre-compiled static regex with a timeout: `new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1))`. |
| **Request size limits** | Set `[RequestSizeLimit]` on upload endpoints. Default max request body MUST be 30MB globally, with explicit overrides only on upload endpoints. |

### Data Protection

| Rule | Detail |
|------|--------|
| **Never hardcode secrets** | No API keys, JWT keys, connection strings, or passwords in code. Use `appsettings.json`, environment variables, or `dotnet user-secrets`. |
| **Never log sensitive data** | No passwords, JWT tokens, credit card numbers, PII, or full request bodies containing auth data in logs. Use structured logging with sanitized fields. |
| **Never expose internal errors to clients** | API responses must never contain stack traces, SQL errors, file paths, or server internals. The global `IExceptionHandler` returns generic ProblemDetails. |
| **Soft-delete user data, never hard-delete** | User-facing entities use `SoftDelete()`. Supports audit trails and data recovery. Only junction tables and truly internal records use hard delete. |
| **Encrypt sensitive fields at rest** | Payment-related data, personal identification numbers, and API keys stored in DB MUST use column-level encryption or at minimum, hashing for passwords (already handled by Identity). |
| **Audit trail for sensitive operations** | All admin actions (user role changes, account lockout/unlock, data deletion, payment refunds) must be logged with: who, what, when, from which IP. |

### API Security

| Rule | Detail |
|------|--------|
| **CORS policy — explicit origins only** | Never use `AllowAnyOrigin()` with `AllowCredentials()`. Whitelist specific origins: `WithOrigins("https://yallajo.com", "https://admin.yallajo.com")`. |
| **Rate limiting on sensitive endpoints** | Login: max 10 requests/minute per IP. Register: max 5/minute. Password reset: max 3/minute. API: max 100 requests/minute per user. Use `Microsoft.AspNetCore.RateLimiting`. |
| **HTTPS only — no HTTP** | `UseHttpsRedirection()` must be in the pipeline. Set HSTS headers in production. All cookies must have `Secure` flag. |
| **Security headers on every response** | Add via middleware: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `X-XSS-Protection: 0` (rely on CSP instead), `Referrer-Policy: strict-origin-when-cross-origin`, `Content-Security-Policy: default-src 'self'`. |
| **Anti-forgery on Web MVC layer** | Every POST/PUT/DELETE form in `YallaJo.Web` must include `@Html.AntiForgeryToken()`. Controllers must have `[ValidateAntiForgeryToken]` on mutating actions. API layer uses JWT instead. |
| **Prevent open redirects in Web auth** | After login, validate the return URL: `Url.IsLocalUrl(returnUrl)` before redirecting. Never redirect to user-provided external URLs. |

### Booking & Payment Security (YallaJo-Specific)

| Rule | Detail |
|------|--------|
| **Never trust client-sent prices** | When creating a booking, always recalculate the price server-side from the tour/place pricing rules. Client sends tour ID + slot + quantity — server computes total. |
| **Idempotency on payment endpoints** | Payment creation must be idempotent — use an `IdempotencyKey` (client-generated GUID) to prevent double charges. If the same key is sent twice, return the original result. |
| **Booking state machine validation** | Every state transition (Pending → Confirmed → Completed, Pending → Cancelled) must be validated in the domain entity. Reject invalid transitions: `if (Status != BookingStatus.Pending) return Result.Failure(...)`. |
| **Financial amounts: decimal(18,2) — always** | No `float`, no `double` for money. SQL Server `decimal(18,2)`. C# `decimal`. Configure in EF: `.HasPrecision(18, 2)`. |
| **Refund amount validation** | Refund amount must never exceed the original payment amount. Validate server-side: `if (refundAmount > originalPayment.Amount) return Result.Failure(...)`. |
| **Slot locking with expiration** | When a user starts a booking, lock the slot for a limited time (e.g., 15 minutes). Use `LockedUntil` column. Expired locks are released by a background service. Never trust the client to release locks. |

### Security Checklist for New Endpoints

Before marking any endpoint as complete, verify:

- [ ] Has explicit `.RequireAuthorization("Policy")` or `.AllowAnonymous()`?
- [ ] If data-access: does it verify the user owns/can access the resource (IDOR check)?
- [ ] All input validated via FluentValidation?
- [ ] No raw SQL with string concatenation?
- [ ] Response doesn't leak internal details (entity IDs of other users, stack traces)?
- [ ] File uploads (if any) validate type, size, and sanitize filename?
- [ ] Financial amounts use `decimal`, not `float`/`double`?
- [ ] Prices recalculated server-side, not trusted from client?

---

## 🔀 Cross-Module Communication Rules

Modules are isolated by design. Breaking isolation creates coupling that compounds over time.

| Rule | Why |
|------|-----|
| **Never `using` another module's Domain or Application namespace** | Breaks module isolation. |
| **Shared DTOs and interfaces go in `{Module}.Contracts`** | That's what the Contracts projects exist for. |
| **Cross-module data access = integration event via outbox/inbox** | Never query another module's DbContext directly. |
| **Cross-module references only allowed through Contracts** | `{Module}.Application` can reference `{OtherModule}.Contracts` — nothing else. |

---

## 🤔 Agent Decision-Making Rules

Decision matrix for acting vs. asking the user.

| Situation | Action |
|-----------|--------|
| Answer is in `guide.md`, `YallaJo.md`, or Business Rules PDF | **MUST act** — NEVER ask the user when the docs already define the answer |
| Business logic decision NOT covered in any doc | **MUST ask** — NEVER guess business rules |
| Multiple valid approaches, similar effort | **MUST act** — pick the simpler option and record the assumption |
| Multiple valid approaches, 2x+ effort difference | **MUST ask** — present options with effort estimate |
| Existing code contradicts `guide.md` | **MUST ask** — use this exact prompt: "I see X in code but guide says Y. Which to follow?" |
| Unsure if a feature is in scope | **MUST ask** — NEVER build unrequested scope |
| About to delete or overwrite existing working code | **MUST ask** — confirm before destructive changes |

---

## 🤝 Session Handoff Rules

Every session must leave the codebase in a clean, resumable state for the next agent.

| Rule | Why |
|------|-----|
| **If you can't complete your task**, mark it 🟡 in "What Has Been Built" with a note saying exactly what's left | Next agent knows where to pick up |
| **Never leave uncommitted broken code** — either finish the feature or revert | Next agent inherits a clean state |
| **If you discover something that changes the plan**, update "What Needs To Be Done" immediately | Plans drift — the doc must reflect reality |
| **If a module's status changed**, update the Module Status Overview table | Next agent trusts the table to be accurate |
| **Update the Build State in the header** with your final `dotnet build` result | Next agent knows if the build is clean |

---

## 🔐 Concurrency & Data Integrity Rules

| Rule | Detail |
|------|--------|
| **AuditableEntity has `RowVersion`** — always configure `.IsRowVersion()` in EF config | Enables optimistic concurrency. Prevents silent data overwrites when two users edit the same record. |
| **Soft delete for user-facing entities** | Use `SoftDelete()` method (sets `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`). User data MUST be recoverable. |
| **Hard delete for junction tables and internal records** | Junction rows (EntityCategory, EntityTag) are disposable — use real `DELETE`. |
| **Unique constraints MUST be enforced at DB level** | Code-level uniqueness checks have race conditions. Always add a unique index in the EF configuration. Code checks are an optimization on top, not a replacement. |
| **Check for existence before creating** (slug, email, etc.) | Use `ExistsAsync()` with the unique field, return `Result.Conflict()` if already taken. |

---

## 🕐 DateTime & Guid Rules

| Rule | Detail |
|------|--------|
| **Always `DateTime.UtcNow`** — never `DateTime.Now` | Store UTC everywhere. Convert to local only on the client/display layer. Jordan is UTC+3 — mixing local/UTC corrupts data. |
| **Always `Guid.CreateVersion7()`** — never `Guid.NewGuid()` | V7 GUIDs are time-sortable, which means better clustered index performance in SQL Server. `NewGuid()` is random = index fragmentation. |
| **`DateTimeOffset` for user-facing timestamps** | When an API response includes a timestamp the user will see, use `DateTimeOffset` so the timezone is explicit. Internal storage remains `DateTime` in UTC. |

---

## 📊 Enum Handling Rules

| Rule | Detail |
|------|--------|
| **Store enums as `int` in DB** | Always use `.HasConversion<int>()` in EF config. Never store as strings — ints are smaller, faster, and index-friendly. |
| **Define all enums in `{Module}.Domain/Enums/`** | One file per enum. Keep them in the Domain layer — they are part of the domain model. |
| **Shared enums go in `{Module}.Contracts`** | If another module needs to reference an enum (e.g., `EntityType`), put it in the Contracts project. |
| **Always add a `None = 0` or meaningful default** | Uninitialized enums default to `0`. Make sure `0` is either invalid (caught by validation) or a safe default. |

---

## 🔤 String Column Rules

YallaJo is a Jordanian tourism app — **Arabic content is expected**. String handling must account for multilingual data.

| Column Type | EF Config | Why |
|-------------|-----------|-----|
| Slugs, codes, status strings | `.IsUnicode(false).HasMaxLength(200)` | Always ASCII — no Arabic. Smaller storage + faster indexing. |
| Names, titles, descriptions | `.HasMaxLength(500)` (default Unicode) | May contain Arabic, English, or other scripts. Unicode is EF default. |
| Long text (body, bio, content) | `.HasMaxLength(4000)` or `.HasColumnType("nvarchar(max)")` | Large user content. Set a reasonable max or use max. |
| **Every string column MUST have `HasMaxLength()`** | No exceptions | Unbounded `nvarchar(max)` on every column is wasteful and prevents indexing. |

---

## 🚨 Error Handling Rules

Errors are handled differently at each layer. MUST follow this strictly.

### By Layer

| Layer | Rule | Example |
|-------|------|---------|
| **Domain** | Never throw exceptions. Use factory method return or domain event to signal problems. Business rules return via the entity method design (e.g., guard clauses in `Create()`). | `if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(...)` in factory is acceptable — it's a programming error, not a business error. |
| **Application** | Return `Result<T>` for ALL outcomes. Never throw for business logic failures. | `return Result<Guid>.Failure(Error.NotFound("Tour.NotFound", "Tour not found."));` |
| **Infrastructure** | Let infrastructure exceptions (DB, HTTP, file system) bubble up naturally. Do NOT catch and swallow. | EF `DbUpdateConcurrencyException` MUST propagate — the global handler will catch it. |
| **Presentation** | Never handle errors in endpoint code. Rely on the global `IExceptionHandler` pipeline. | Endpoints just call `sender.Send(cmd)` and return `result.ToApiResult()`. |

### Error Code Convention

Error codes MUST follow `{Entity}.{Reason}` format. They MUST be:
- **Unique** across the entire codebase
- **Machine-readable** (no spaces, PascalCase reason)
- **Documented** in the entity's command/query handler

| Code Pattern | When to Use | Example |
|-------------|-------------|---------|
| `{Entity}.NotFound` | Entity doesn't exist | `Error.NotFound("Category.NotFound", "Category 'abc' not found.")` |
| `{Entity}.AlreadyExists` | Duplicate detected (slug, email) | `Error.Conflict("Tour.AlreadyExists", "A tour with slug 'dead-sea' already exists.")` |
| `{Entity}.InvalidState` | Action not allowed in current state | `Error.Validation("Booking.InvalidState", "Cannot cancel a completed booking.")` |
| `{Entity}.Unauthorized` | User lacks permission for this specific action | `Error.Unauthorized("Place.Unauthorized", "Only the owner can edit this place.")` |
| `{Entity}.DependencyConflict` | Can't delete because other entities reference it | `Error.Conflict("Category.DependencyConflict", "Cannot delete category with 5 assigned places.")` |

### Global Exception Pipeline

```
Unhandled exception thrown anywhere
  → ExceptionHandler middleware catches it
  → Logs the full exception (stack trace, inner exceptions)
  → Returns RFC 7807 ProblemDetails to client (NO stack trace exposed)
  → 500 Internal Server Error for unexpected failures
```

**Never expose stack traces, internal paths, or SQL errors to the client.**

---

## 🔴 Try/Catch & Exception Rules

Exception handling is one of the most misused patterns in .NET. Follow these rules exactly — every violation either swallows errors silently or crashes the pipeline unexpectedly.

### The #1 Rule: Never Catch What You Can't Handle

```csharp
// ❌ WRONG — catching everything, doing nothing meaningful
try { await repo.AddAsync(entity, ct); }
catch (Exception) { }  // Swallowed. No one knows it failed.

// ❌ WRONG — catching and re-throwing loses the stack trace
try { await repo.AddAsync(entity, ct); }
catch (Exception ex) { throw new Exception("Failed", ex); }  // Pointless wrapper.

// ✅ CORRECT — let it propagate. Global handler catches it, logs it, returns 500.
await repo.AddAsync(entity, ct);
```

---

### Where try/catch IS Allowed (Whitelist)

Only add try/catch when you can take a MEANINGFUL action on the specific exception type.

#### 1. Infrastructure Layer — External Service Calls

Wrap calls to external APIs (Azure Translator, payment gateway, file storage) to convert infrastructure failures into `Result` failures:

```csharp
// ✅ In Infrastructure — wrapping external HTTP/API calls
public async Task<Result<TranslationResult>> TranslateAsync(string text, string targetLang, CancellationToken ct)
{
    try
    {
        var response = await _httpClient.PostAsync("/translate", content, ct);
        response.EnsureSuccessStatusCode();
        return Result<TranslationResult>.Success(await ParseResponse(response, ct));
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Translation API call failed for language {Language}", targetLang);
        return Result<TranslationResult>.Failure("Translation.ServiceUnavailable",
            "Translation service is temporarily unavailable.");
    }
    catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
    {
        _logger.LogWarning(ex, "Translation API timed out for language {Language}", targetLang);
        return Result<TranslationResult>.Failure("Translation.Timeout",
            "Translation request timed out.");
    }
    // Do NOT catch general Exception — let unexpected errors propagate
}
```

#### 2. Infrastructure Layer — Optimistic Concurrency

Catch `DbUpdateConcurrencyException` ONLY when you want to return a user-friendly conflict response instead of a 500:

```csharp
// ✅ In a command handler that explicitly handles concurrency conflicts
try
{
    await unitOfWork.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    return Result<Guid>.Conflict("Entity.ConcurrencyConflict",
        "This record was modified by another user. Please refresh and try again.");
}
```

#### 3. Background Services — Prevent Worker Crash

Background services MUST NOT crash on individual item failures — the worker loop MUST continue:

```csharp
// ✅ In BackgroundService — catch per-item to keep the worker alive
while (await _channel.Reader.WaitToReadAsync(ct))
{
    var item = await _channel.Reader.ReadAsync(ct);
    try
    {
        await ProcessItemAsync(item, ct);
    }
    catch (OperationCanceledException) when (ct.IsCancellationRequested)
    {
        break; // Graceful shutdown — stop the loop
    }
    catch (Exception ex)
    {
        // Log and continue — do NOT let one item crash the whole worker
        _logger.LogError(ex, "Failed to process media item {ItemId}", item.Id);
    }
}
```

#### 4. Application Layer — TaskCanceledException (Graceful Shutdown)

If you want to distinguish between user-cancelled requests and app shutdown:

```csharp
// ✅ Only when you need to differentiate cancellation sources
catch (OperationCanceledException) when (ct.IsCancellationRequested)
{
    // Request was cancelled by the client — ignore silently
    return Result<Guid>.Failure("Request.Cancelled", "Request was cancelled.");
}
```

---

### Where try/catch is FORBIDDEN

| Location | Why |
|----------|-----|
| **Domain entity methods** (`Create()`, `Update()`) | Domain uses `ArgumentException` for programming errors. These MUST propagate — they indicate a bug, not a user error. |
| **Application command/query handlers** (general `Exception`) | Handlers return `Result<T>`. Catching `Exception` here masks infrastructure failures that the global handler needs to see. |
| **Endpoint/Presentation layer** | Never. The global `IExceptionHandler` handles all unhandled exceptions. Adding try/catch in endpoints duplicates that responsibility. |
| **Validators** | FluentValidation rules are pure — no exception handling. |
| **Empty catch blocks** | `catch (Exception) { }` — NEVER. This is a bug, not error handling. |
| **Catching and swallowing without logging** | If you catch it, you MUST log it. Silent swallowing hides failures. |

---

### Exception Types Reference (this stack)

| Exception | Source | When It Occurs | What To Do |
|-----------|--------|----------------|------------|
| `ArgumentException` / `ArgumentNullException` | Domain `Create()` / `Update()` | Programming error — caller passed invalid args | Let propagate → 500. It's a developer bug. |
| `ValidationException` (FluentValidation) | MediatR `ValidationBehavior` | Input fails validator rules | Already handled by `ValidationExceptionHandler` → 400. Never catch this yourself. |
| `DbUpdateConcurrencyException` | EF Core `SaveChangesAsync` | RowVersion conflict — two users edited same record | Catch only if returning 409 Conflict to user. Otherwise propagate → 500. |
| `DbUpdateException` | EF Core `SaveChangesAsync` | DB constraint violation (FK, unique index) | Usually propagate → 500. Can catch to return 409 if you inspect `InnerException` for specific violation. |
| `OperationCanceledException` / `TaskCanceledException` | `CancellationToken` cancellation | Client disconnected or app shutting down | Catch in background services. In handlers: propagate — MediatR handles it cleanly. |
| `HttpRequestException` | `HttpClient` calls | External API unreachable or returns 4xx/5xx | Catch in Infrastructure layer. Convert to `Result.Failure`. Log with full context. |
| `TimeoutException` | External service timeouts | API/DB call exceeded timeout | Catch in Infrastructure layer. Convert to `Result.Failure("X.Timeout", ...)`. |
| `InvalidOperationException` | DI, EF state errors | Usually a programming error (misconfigured DI, wrong EF state) | Let propagate → 500. Fix the code, not the catch. |
| `UnauthorizedAccessException` | File system | No permission to read/write file | Catch in `IFileStorageService`. Return `Result.Failure("File.AccessDenied", ...)`. |
| `IOException` | File system | Disk full, file locked, path invalid | Catch in `IFileStorageService`. Return `Result.Failure("File.StorageFailed", ...)`. |
| `JsonException` | `System.Text.Json` | Malformed JSON from external API | Catch in Infrastructure parsing code. Log + return `Result.Failure`. |

---

### Logging Requirements Inside catch Blocks

Every catch block that doesn't re-throw MUST log before returning:

```csharp
// ✅ CORRECT — logs with full context before converting to Result
catch (HttpRequestException ex)
{
    _logger.LogError(ex,                    // ← exception object FIRST (captures stack trace)
        "External API call failed. Url={Url} StatusCode={StatusCode}",
        requestUrl, ex.StatusCode);         // ← structured log properties
    return Result<T>.Failure("Service.Unavailable", "External service is unavailable.");
}

// ❌ WRONG — no logging
catch (HttpRequestException)
{
    return Result<T>.Failure("Service.Unavailable", "External service is unavailable.");
}

// ❌ WRONG — string interpolation instead of structured logging
catch (HttpRequestException ex)
{
    _logger.LogError($"Call to {url} failed: {ex.Message}"); // Not searchable in prod
    return Result<T>.Failure(...);
}
```

---

### The finally Block Rule

Use `finally` ONLY for resource cleanup — never for business logic:

```csharp
// ✅ CORRECT — cleanup in finally
var stream = File.OpenRead(path);
try
{
    await ProcessAsync(stream, ct);
}
finally
{
    await stream.DisposeAsync(); // Always runs, even if exception thrown
}

// ✅ BETTER — use 'using' instead of try/finally for IDisposable
await using var stream = File.OpenRead(path);
await ProcessAsync(stream, ct);

// ❌ WRONG — business logic in finally
try { ... }
finally
{
    await unitOfWork.SaveChangesAsync(ct); // Don't do this — may run after an exception
}
```

---

### Exception Wrapping Rule

When you MUST wrap an exception (rare), ALWAYS include the original as `innerException` and log before wrapping:

```csharp
// ✅ CORRECT — preserves original stack trace
catch (Exception ex)
{
    _logger.LogError(ex, "Media processing failed for attachment {AttachmentId}", attachmentId);
    throw new MediaProcessingException("Failed to process media file.", ex); // ex as inner
}

// ❌ WRONG — loses original stack trace
catch (Exception)
{
    throw new MediaProcessingException("Failed to process media file."); // Where did it fail?
}
```

---

## ✅ Validation Rules

Validation happens at multiple levels. Each level has a specific responsibility.

### Validation Layers

| Layer | What to Validate | How |
|-------|-----------------|-----|
| **FluentValidation (Application)** | Input shape: required fields, string lengths, format (email, slug regex), numeric ranges, enum values | `AbstractValidator<TCommand>` — runs in MediatR `ValidationBehavior` before handler |
| **Domain (Entity methods)** | Business invariants: state transitions, business rules, cross-field logic | Guard clauses inside `Create()`, `Update()`, `Activate()` etc. |
| **Database (EF Config)** | Data integrity: unique constraints, foreign keys, check constraints | `HasIndex().IsUnique()`, `HasForeignKey()`, `HasCheckConstraint()` |

### FluentValidation Rules

| What | Validator Rule | Example |
|------|---------------|---------|
| Required string | `.NotEmpty().MaximumLength(N)` | `RuleFor(x => x.Name).NotEmpty().MaximumLength(200);` |
| Optional string | `.MaximumLength(N).When(x => x.Field != null)` | Only validate length when provided |
| Slug format | `.Matches(@"^[a-z0-9\-]+$")` | Lowercase, digits, hyphens only |
| Email format | `.EmailAddress()` | Built-in FluentValidation rule |
| Enum value | `.IsInEnum()` | Rejects values not defined in the enum |
| Numeric range | `.InclusiveBetween(min, max)` | `RuleFor(x => x.Price).GreaterThan(0);` |
| Pagination | `.InclusiveBetween(1, 100)` | On `PageSize`. Always enforce max. |
| GUID (non-empty) | `.NotEqual(Guid.Empty)` | For ID references in commands |
| Collection | `.NotEmpty().ForEach(x => x.NotEqual(Guid.Empty))` | For batch assignment commands |

### Async Validation (Uniqueness Checks)

For uniqueness validation that requires a DB call, do it in the **command handler**, NOT in the validator:

```csharp
// ✅ In the handler — has access to repository
public async Task<Result<Guid>> Handle(CreatePlaceCommand cmd, CancellationToken ct)
{
    if (await repo.ExistsBySlugAsync(cmd.Slug, ct))
        return Result<Guid>.Failure(Error.Conflict("Place.AlreadyExists", $"Slug '{cmd.Slug}' is taken."));

    var place = Place.Create(cmd.Name, cmd.Slug, ...);
    // ...
}

// ❌ Don't inject repositories into validators — validators should be pure and fast
```

### Validation Error Messages

- Use clear, user-friendly language
- Include the field name and the constraint that was violated
- For business rules, explain what the user needs to do differently
- Error messages will be shown to end users — write them accordingly

---

## 📝 Logging Rules

| Rule | Detail |
|------|--------|
| **Use structured logging** | `_logger.LogInformation("Created {EntityType} with {EntityId}", entityType, entityId)` — NOT `$"Created {entityType} with {entityId}"` |
| **Never log sensitive data** | No passwords, JWT tokens, credit card numbers, or PII in logs |
| **Log levels** | `Trace`: verbose debug. `Debug`: dev-only detail. `Information`: normal operations (entity created/updated/deleted). `Warning`: recoverable issues (retry, slow query). `Error`: failures requiring attention. `Critical`: app-stopping failures. |
| **Log at handler boundaries** | Log at the start and end of command/query handlers. The `LoggingBehavior` does this automatically — don't duplicate. |
| **Log external service calls** | Always log before/after calling external APIs (Azure Translator, payment gateway, etc.) with correlation IDs |

---

## 🔁 Domain Events vs Integration Events

| Aspect | Domain Event | Integration Event |
|--------|-------------|-------------------|
| **Scope** | Same module, same transaction | Cross-module, different transactions |
| **Delivery** | Synchronous via MediatR (dispatched by UoW before SaveChanges) | Asynchronous via outbox/inbox pattern |
| **When to use** | Side effects within the same aggregate/module: update translation cache, create audit log, cascade state change | Notify other modules: booking created → finance creates invoice, place updated → SEO regenerates sitemap |
| **Handler rule** | NEVER call `SaveChangesAsync()` — changes piggyback on the aggregate's save | Writes to outbox table in handler — separate process picks up and delivers |
| **Naming** | `{Entity}{PastTenseVerb}DomainEvent` | `{Entity}{PastTenseVerb}IntegrationEvent` |
| **Never** cross module boundaries | ✅ Correct | If you need cross-module → use integration event |

---

## 🛡️ Pre-flight & Completion Verification

### Pre-flight (Session Start)

Before writing ANY code at the start of a session:

**FAILURE TO COMPLETE ANY ITEM = WORK NOT ACCEPTED.**

1. Read `Agents/agent-context.md` (this file) in full
2. Read `Agents/error-log.md` in full
3. Run `dotnet build` — verify 0 errors before starting new work
4. If build fails: fix or report the failure BEFORE starting new work. Never build on top of a broken foundation.

### Completion Checklist (Before Marking ✅)

Before marking ANY feature as ✅ complete in the Work Tracker, verify ALL of these:

**FAILURE TO COMPLETE ANY ITEM = WORK NOT ACCEPTED.**

- [ ] DI registration added in `DependencyInjection.cs`?
- [ ] EF configuration created with all property configs?
- [ ] `HasQueryFilter(x => !x.IsDeleted)` added (if AuditableEntity)?
- [ ] `ValueGeneratedNever()` on Id property?
- [ ] `.IsRowVersion()` on RowVersion (if AuditableEntity)?
- [ ] FluentValidation validator covers ALL input fields?
- [ ] Every endpoint has explicit `.RequireAuthorization()` or `.AllowAnonymous()`?
- [ ] `CancellationToken` passed through entire call chain?
- [ ] List endpoints use SummaryDto (not full entity)?
- [ ] List endpoints have pagination with max PageSize=100?
- [ ] `dotnet build` passes with 0 errors?
- [ ] `lsp_diagnostics` clean on all changed files?

### Rollback Strategy

If your changes break the build after 3 consecutive fix attempts:

1. **Stop** — do not make more changes
2. **`git stash`** your work to preserve it without polluting main
3. **Report** what went wrong in the error log and in "What Has Been Built" (mark 🟡)
4. **Let the next agent** try fresh with the documented context of what failed

---

## 🧪 Testing Strategy & Rules

No tests exist yet, but all code MUST be written to be testable. When tests are added, they MUST follow these rules.

### Testability Rules (Apply NOW)

| Rule | Detail |
|------|--------|
| **No static coupling** | Never use static methods for business logic. Always inject dependencies via constructor. |
| **No `new` for services** | Never `new SomeService()` inside a handler. Inject via DI. |
| **No hidden dependencies** | If a handler needs something, it must be in the constructor parameters. No `ServiceLocator`, no `HttpContext.RequestServices`. |
| **Pure domain logic** | Entity methods (`Create()`, `Update()`, `SoftDelete()`) MUST have zero dependencies on infrastructure — they take primitives and return void or the entity. |
| **Result pattern enables assertions** | `Result<T>.IsSuccess`, `result.Outcome`, `result.Errors` are all testable without HTTP. |

### Test Naming Convention (For Future)

`{MethodUnderTest}_Should{ExpectedBehavior}_When{Condition}`

Examples:
- `Create_ShouldRaiseDomainEvent_WhenCalledWithValidArgs`
- `Handle_ShouldReturnNotFound_WhenEntityDoesNotExist`
- `Handle_ShouldReturnConflict_WhenSlugAlreadyExists`

### What to Test (Priority Order)

1. **Domain entity methods** — factory methods, business rules, state transitions
2. **Command/Query handlers** — mock the repository, verify correct Result
3. **Validators** — verify all rules fire correctly for valid/invalid input
4. **Do NOT test**: EF configurations, DI registration, endpoint routing (these are infrastructure concerns tested by integration tests)

---

## 🌿 Git Conventions

| Rule | Detail |
|------|--------|
| **NEVER push to remote** | You MUST only commit locally. NEVER run `git push` under any circumstances. The user handles all pushes manually. This is a hard block — no exceptions, no "just this once." |
| **Never commit without being asked** | The user will tell you when to commit. Do not auto-commit after completing work. |
| **Commit only, never push** | When the user asks you to commit: `git add` + `git commit` ONLY. Stop there. Do NOT follow up with `git push`. |
| **Branch naming** | `feature/{module}/{entity-or-feature}` — e.g., `feature/content-places/place-entity`, `fix/booking/cancel-state-bug` |
| **Commit message format** | `{type}({module}): {description}` — e.g., `feat(ContentPlaces): add Place entity with full CQRS`, `fix(Booking): prevent double cancellation` |
| **Commit types** | `feat` (new feature), `fix` (bug fix), `refactor` (no behavior change), `docs` (documentation), `chore` (build/config) |
| **One logical change per commit** | Don't mix a new entity with a bug fix in the same commit. |
| **Never commit secrets** | No `appsettings.Development.json` with real keys, no `.env` files, no credential files. Check `.gitignore` first. |
| **Never force push** | NEVER run `git push --force` or `git push --force-with-lease`. If you need to rewrite history, ask the user first. |

---

## 📖 API Documentation Rules

Every endpoint must be self-documenting via Swagger/OpenAPI annotations.

| Annotation | When to Use | Example |
|------------|-------------|---------|
| `.WithName("OperationId")` | Every endpoint — unique operation ID | `.WithName("CreatePlace")` |
| `.WithSummary("...")` | Every endpoint — one-line description | `.WithSummary("Create a new place")` |
| `.WithDescription("...")` | Complex endpoints — detailed explanation | `.WithDescription("Creates a place and triggers auto-translation...")` |
| `.Produces<T>(200)` | Every endpoint — success response type | `.Produces<PlaceDetailDto>(StatusCodes.Status200OK)` |
| `.ProducesValidationProblem()` | Endpoints with input validation | On all POST/PUT endpoints |
| `.ProducesProblem(404)` | Endpoints that can return NotFound | On GET-by-ID, PUT, DELETE |
| `.WithTags("Group")` | Group-level — already set on MapGroup | `.WithTags("ContentPlaces")` |

---

## 🌍 Localization & RTL Rules

YallaJo is a Jordanian tourism platform. Arabic (RTL) and English (LTR) are the primary languages.

### Content Rules

| Rule | Detail |
|------|--------|
| **Translatable fields** | `Name`, `Description`, `Title`, `Body` — any user-facing text. Identified by having a corresponding `{Entity}Translation` entity. |
| **Non-translatable fields** | `Slug` (always ASCII), `Icon`, `SortOrder`, `Price`, `Coordinates`, `Status`, `Email` — data that doesn't change by language. |
| **Translation trigger** | When a translatable entity is created/updated, a domain event triggers `EntityTranslationOrchestrator` which calls Azure Translator for all active languages. |
| **Source language** | Every create/update command includes `SourceLanguageCode` (defaults to `"en"`). This tells the translator what language the input is in. |
| **Slug handling** | Slugs are ALWAYS lowercase ASCII (`^[a-z0-9\-]+$`). Arabic content gets an English slug. Slugs are NOT translated. |

### API Response Localization

| Rule | Detail |
|------|--------|
| **`Accept-Language` header** | API consumers send `Accept-Language: ar` or `Accept-Language: en` to get localized responses. |
| **Fallback** | If translation doesn't exist for requested language, return the original (source) language content. Never return empty. |
| **Direction hint** | When relevant, include `"direction": "rtl"` or `"direction": "ltr"` in response DTOs for Arabic and English respectively. |

---

## 🌐 Environment Rules

| Environment | DB | Logging | External Services | Swagger |
|-------------|----|---------|--------------------|---------|
| **Development** | `YallaJo_Dev` (LocalDB/SQLEXPRESS) | Debug level, console output | Azure Translator (dev key), Local file storage | Enabled |
| **Staging** | `YallaJo_Staging` | Information level, structured JSON | Azure Translator (staging key), Cloudinary (staging) | Enabled |
| **Production** | `YallaJo` | Warning level, structured JSON, external sink | Azure Translator (prod key), Cloudinary (prod) | Disabled |

### Environment-Specific Rules

| Rule | Detail |
|------|--------|
| **Never use Development config in Production** | JWT keys, connection strings, and API keys MUST be different per environment. |
| **Seed data is Development-only** | `IModuleDbInitializer` seeds sample data only when `ASPNETCORE_ENVIRONMENT=Development`. |
| **Feature flags** | Use `IConfiguration` sections to toggle features per environment — not `#if DEBUG`. |
| **Error detail** | Development: include exception details in ProblemDetails. Production: generic error messages only. |

---

## 📦 Dependency Management Rules

| Rule | Detail |
|------|--------|
| **Never add a NuGet package without asking the user first** | New dependencies have long-term maintenance costs. Always propose before adding. |
| **MUST use existing packages first** | If a capability is already covered by an installed package, use it. Do NOT add a second package for the same purpose. |
| **No preview packages in production code** | Unless the project explicitly uses a preview SDK (YallaJo uses .NET SDK 10.0.200-preview, targeting net9.0 — this is fine). |
| **Pin versions** | Always specify exact version in `.csproj` — no floating versions (`*`). |

### Approved Packages (already in use)

| Package | Purpose | DO NOT replace with |
|---------|---------|-------------------|
| MediatR | CQRS pipeline | Wolverine, raw DI |
| FluentValidation | Input validation | DataAnnotations |
| EF Core (SqlServer) | ORM | Dapper (for CQRS queries it's OK to add later) |
| SixLabors.ImageSharp | Image processing | System.Drawing, SkiaSharp |
| FFMpegCore | Video/audio metadata | MediaToolkit |
| Azure.AI.Translation.Text | Translation API | Google Translate SDK |

### Banned Packages

| Package | Why |
|---------|-----|
| Hangfire / Quartz | ADR-003: We use BackgroundService + Channel\<T\> |
| AutoMapper | We use manual DTO mapping for explicitness and performance |
| MediatR.Extensions.* | Unnecessary — pipeline behaviors are in SharedKernel |
| EntityFramework.Proxies | No lazy loading — see Performance Rule P7 |

---

## 🧠 Error Learning System (MANDATORY)

Agents make mistakes. The same mistakes get repeated across sessions. This system ensures every error is captured and never repeated.

### Rule: Read Before Work, Write Before Leaving

1. **At session start**: Read `Agents/error-log.md` in full. These are mistakes previous agents made — do NOT repeat them.
2. **During work**: When you encounter ANY error (build failure, runtime exception, wrong assumption, logic bug, broken test, misused API), **log it immediately** to `Agents/error-log.md` before continuing your fix.
3. **At session end**: Review your error log entries for completeness. Every entry must have a root cause and a prevention rule.

### What Counts as a Loggable Error

- Build errors caused by your code
- Runtime exceptions from incorrect DI, wrong type usage, missing config
- Incorrect assumptions about existing code (e.g., assumed a method existed, wrong signature)
- Logic errors caught during testing
- Patterns you tried that didn't work in this codebase
- EF migration issues
- Any fix that took you more than one attempt

### What Does NOT Count

- Pre-existing errors you didn't cause
- Typos caught and fixed immediately (< 30 seconds to fix)
- User-requested changes to your work

### Entry Format

Every entry in `Agents/error-log.md` MUST follow this exact format:

```markdown
### ERR-{number}: {Short descriptive title}
- **Date**: {YYYY-MM-DD}
- **Module**: {Which module were you working on}
- **What Happened**: {What you did that caused the error — be specific}
- **Error Message**: {Exact error message or symptom}
- **Root Cause**: {WHY it happened — the actual underlying reason}
- **Fix Applied**: {What you did to fix it}
- **Prevention Rule**: {A concrete rule future agents must follow to avoid this}
```

### How This Improves Future Agents

- The **Prevention Rule** field is the most important — it becomes a searchable rule that prevents the same class of error
- Patterns in the error log reveal systemic issues (e.g., if 5 entries are about DI registration, the checklist needs strengthening)
- The error log is append-only — never delete entries, even if they seem obvious. What's obvious to you may not be obvious to the next agent.

### Error Log Location

`Agents/error-log.md` — read at session start, append during work, never delete entries.
