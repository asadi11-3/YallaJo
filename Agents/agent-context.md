# YallaJo — Agent Onboarding & Progress Context

> **Last Updated**: 2026-04-21 (ContentPlaces Place CQRS event/caching fix pass) | **Build State**: `ContentPlaces.Application` build 0 errors; `ContentPlaces.Presentation` build 0 errors; no ContentPlaces automated tests exist yet; full solution still has pre-existing analyzer warnings

> **Purpose**: Single source of truth for any AI agent working on YallaJo. **Read this entire file once at the start of every session.** Every section contains rules you must follow — do not skip any.
## 📑 Table of Contents
### [CRITICAL] — Read first, always apply
- §Reference Files · §Gotchas · §Common Mistakes · §Performance Rules · §Security Rules
- §Error Handling · §Try/Catch & Exception Rules · §Pre-flight & Completion · §Error Learning
### [REQUIRED] — Must know, apply on every task
- §Update This File · §Entity Checklist · §Scaffold & Template Rules · §DI Registration
- §Naming Conventions · §Validation Rules · §Concurrency & Data Integrity
- §DateTime & Guid Rules · §Enum Handling · §String Column Rules · §Git Conventions
### [REFERENCE] — Look up when your task involves this area
- §Caching Rules · §Polly Resilience · §Module Dependencies · §Auth & Authorization
- §Logging Rules · §Domain vs Integration Events · §Cross-Module Communication
- §Localization & RTL · §Environment Rules · §Dependency Management
- §Testing Strategy · §API Documentation · §Validation & Error Flow
### [TRACKING] — Project status (not rules)
- §Project Overview · §Module Status · §What Has Been Built · §What Needs To Be Done
- §Key File Locations · §How to Run & Verify · §Constraints · §Agent Efficiency Rules
- §Agent Decision-Making · §Session Handoff

---
## 🔖 [CRITICAL] MANDATORY: Reference Files
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
## ⚠️ [CRITICAL] Critical Discoveries & Gotchas
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
| 12 | **`Microsoft.Extensions.*` packages MUST stay on `9.x` — NEVER add `10.x`** | The project targets `net9.0`. Adding a `10.x` version of any `Microsoft.Extensions.*` package causes `NU1605: Detected package downgrade` because other projects transitively pull `9.x`. Exception: `Caching.Hybrid` is pinned at `9.3.0` (its GA version). All three `Caching.Hybrid` references (SharedKernel.Application, SharedKernel.Infrastructure, module Application) MUST be `9.3.0`. Before adding any `Microsoft.Extensions.*` package, search the solution for the existing version and match it exactly. |
| 13 | **ALL queries MUST implement `ICacheableQuery` — no uncached queries** | Every `IQuery<T>` record in every module MUST implement `ICacheableQuery` with `CacheKey`, `CacheDuration`, and `Tags`. Returning data without caching defeats the HybridCache pipeline behavior. Forgetting it means the `QueryCachingBehavior` is silently bypassed and every request hits the DB. |
| 14 | **ALL command handlers MUST inject `HybridCache` and call `RemoveByTagAsync` after save** | Writing data without invalidating the cache leaves stale data visible to users until TTL expires. `RemoveByTagAsync` is called AFTER a successful `SaveChangesAsync` — never before (a failed save would leave an empty cache). |
| 15 | **`RemoveByTagAsync` MUST use the MOST SPECIFIC tag — NEVER use a coarse tag for single-entity mutations** | `RemoveByTagAsync("attachments")` evicts ALL entities' attachment caches system-wide. Single-entity deletes/updates MUST use `$"attachments:{EntityType}:{EntityId}"` + `$"attachment:{id}"`. Coarse tags (`"categories"`, `"tags"`) are only correct for operations that affect ALL instances (schema changes, bulk deletes). ERR-010 in error-log.md. |
| 16 | **Before calling any domain method that raises an event, guard the current state** | `language.Activate()` ALWAYS raises `LanguageActivatedDomainEvent`. If the language is already active and you call `Activate()` again, you get a duplicate outbox row → duplicate integration event → duplicate downstream work (re-translating all content). Always check: `if (!entity.IsInTargetState) entity.TransitionToTargetState()`. See ERR-009 in error-log.md. |
| 17 | **Recursive tree builders MUST have cycle detection** | Any method that traverses parent-child relationships from DB data must use a `HashSet<Guid> visited` set to detect circular references. A circular parent chain in the DB causes `StackOverflowException` that crashes the process. See ERR-012 in error-log.md. |
| 18 | **`ILogger<THandler>` is mandatory in ALL handlers — commands AND queries** | Query handlers are NOT exempt. 21 ContentCore handlers were found missing ILogger during audit (2026-04-17). The rule applies to every `ICommandHandler` and `IQueryHandler` implementation in every module. See BUG-005 in ContentCore-fixes-required.md. |
| 19 | **Never mark an inbox/outbox-driven email notification as processed before the email send succeeds** | If you persist the OTP/token and mark the inbox message processed first, then swallow SMTP failure, registration returns success but no email is delivered and the event will never retry. Mark failed OTPs used and let the outbox retry instead. |
| 20 | **Google App Passwords copied from UI may include spaces — normalize before SMTP auth** | Google shows app passwords in 4-character groups for readability. Passing the spaced value directly to `NetworkCredential` causes Gmail auth failure even when the password looks correct. Strip spaces and trim before calling `SendMailAsync`. |
| 21 | **Do not run parallel `dotnet build/test` commands that share projects/output paths** | Concurrent compilation against shared `obj/bin` outputs can throw `CS2012` file-lock errors (dll in use by another process). Run build/test validations sequentially when targets overlap. |
| 22 | **Do not fix ContentPlaces event dispatch by injecting `ContentPlacesDbContext` into Application handlers** | `ContentPlaces.Application` does not reference Infrastructure by design. Restore aggregate event dispatch by making `IContentPlacesUnitOfWork` delegate to the shared `IUnitOfWork<ContentPlacesDbContext>` inside Infrastructure, or raise domain events on the aggregate and handle outbox writes in Infrastructure event handlers. |

---
## 🚨 [CRITICAL] Common Mistakes & Fixes
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
| Called `Activate()` / state-transition method without checking current state | Duplicate domain events → duplicate outbox rows → duplicate integration events → duplicate downstream processing (e.g., re-translating all content) | Before calling any method that raises a domain event, guard: `if (!entity.IsActive) entity.Activate()`. Never call state-change methods unconditionally. ERR-009. |
| Used `BaseEntity` for an entity that needs `UpdatedAt` / `RowVersion` | `UpdatedAt` manually set via `= DateTime.UtcNow` (two sources of truth), `[Timestamp]` attribute on a `BaseEntity` property bypasses interceptors | If an entity needs `UpdatedAt`, `IsDeleted`, or `RowVersion`, it MUST extend `AuditableEntity`. Use `MarkUpdated()` — never `UpdatedAt = DateTime.UtcNow`. See §guide.md §13 Entity Base Class Selection. |
| Recursive tree builder with no cycle detection | `StackOverflowException` crashes the process when DB has circular parent references | Add `HashSet<Guid> visited` to recursive method. Before recursing, call `visited.Add(id)` and return leaf if false. ERR-012. |
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
## 🏎️ [CRITICAL] Performance Rules (MANDATORY)
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
## ⚡ [REQUIRED] MANDATORY: Update This File After Every Task
After completing ANY work, do ALL of the following before ending your session:

1. **"What Has Been Built" (Work Tracker)** — Add entry for every feature/entity/fix completed. Use `🤖 Agent` for your work, `👤 User` if user tells you they built something. Status: ✅ complete / 🟡 partial / ❌ broken
2. **"Module Status Overview"** — Update module's status and notes
3. **"What Needs To Be Done Next"** — Remove completed items, add new discoveries
4. **"Gotchas"** — Add any new gotchas (number sequentially from last)
5. **"Build State"** — Update error/warning count in header
6. **`Agents/error-log.md`** — Verify all errors encountered during this session are logged with root cause and prevention rule

**Why**: Next agent reads this file + the error log FIRST. Stale data = wrong decisions. Missing error entries = repeated mistakes.

---
## [TRACKING] Project Overview
**YallaJo** — Tourism/Booking Modular Monolith, .NET 9, Clean Architecture per module, CQRS (MediatR), domain events, shared kernel.
### Tech Stack
`.NET 9` (SDK 10.0.200-preview) · `MediatR` · `EF Core` (SQL Server) · `FluentValidation` · `SixLabors.ImageSharp` · `FFMpegCore` · `Azure Translator API`
### Solution Structure
- **YallaJo.Api** (`:57065`) — REST API, JWT Bearer auth
- **YallaJo.Web** (`:57070`) — MVC admin UI, Cookie auth, HttpClient BFF (calls API)
- **YallaJo.SharedKernel** — Domain/Application/Infrastructure shared abstractions
- **14 Modules**: Auth, Security, Accounts, ContentCore, ContentPlaces, ContentTours, ContentBlogs, ContentSeo, Booking, Finance, Messaging, Social, Tracking, Analytics

---
## [TRACKING] Module Status Overview
| Module | Status | Notes |
|--------|--------|-------|
| Auth | ✅ Fixed | Registration now creates the Accounts profile synchronously before returning success, eliminating the `GET /api/v1/accounts/profile` 404 race for newly registered users. `RegisterCommandHandler` calls the Accounts profile-creation contract directly, while the existing `Accounts.UserCreatedIntegrationEventHandler` remains as an idempotent fallback. Email-delivery fixes remain in place. Validation: `Accounts.Contracts`, `Accounts.Application`, and `Auth.Application` builds completed with 0 errors (warnings only). |
| Security | ✅ Fixed | Privilege hierarchy enforcement is now in handlers (Owner > SuperAdmin > Admin > standard roles), and seed identity profiles were aligned to canonical roles for testing (`Owner`, `SuperAdmin`, `Admin`, `TourGuide`, `User`) with one Owner only. |
| Accounts | ✅ Fixed | Profile self-service flow hardened: UpdateProfile flat-binding fix retained, avatar upload contract fixed (relative `/uploads/...` now accepted by `UpdateAvatarCommandValidator`), and API avatar endpoint now deletes freshly uploaded files when profile update fails (prevents orphan files). Added `tests/Accounts.Tests.Unit` validator regressions. |
| ContentCore | ✅ Fixed | Full audit + all 11 bugs fixed 2026-04-17, plus WS follow-up remediation: upload magic-byte signature validation (WS6), verified post-commit attachment deletion flow/no-op event handler consistency (WS4), and new migration `20260417115007_UpdateContentCoreUnicodeTranslationCacheAndStatus` for Unicode + CategoryTranslation.Status + TranslationCache hash index. Build: 0 errors. See `Agents/ContentCore-fixes-required.md`. |
| ContentPlaces | 🟡 In Progress | Tasks 2+3 complete (Business CQRS + state machine + BusinessHours). Task 1 Place CQRS has now been hardened: delete guard fixed, Place UoW now dispatches aggregate domain events through the shared UoW, Place create/update/delete publish outbox integration events, Place queries are cacheable, and Place command handlers now invalidate HybridCache tags. Remaining gap: list filtering still cannot enforce `categoryId`/`hasActiveTours` because the current Place model/query shape has no category or tour-count backing fields yet. Tasks 4–8 remain. |
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
## 📋 [TRACKING] What Has Been Built (Work Tracker)
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
| 16 | ContentPlaces pre-work | ✅ | 🤖 Agent | Schema conflict resolved: dropped unique index `(BusinessId, DayOfWeek)` from BusinessHoursConfiguration, added migration `UpdateBusinessHoursAllowSplitShifts`. Added `ContentPlacesDbContextFactory` design-time factory. Fixed truncated ContentCore `ReorderCategoriesCommand` + `ReorderCategoriesCommandHandler`. Full task plan written to `Agents/ContentPlaces-tasks.md`. Build: 0 errors. |
| 17 | ContentCore Result/Error consistency pass | ✅ | 🤖 Agent | Fixed 20+ handlers using message-only `.NotFound("string")` / `.Conflict("string")` overloads → replaced with proper `Error` records following `{Entity}.{Reason}` convention. Fixed `ToProblem` in `ContentCoreEndpoints.cs` to fall back to `Messages` when `Errors` is empty. Files fixed: all Category, Tag, EntityTag, EntityCategory, Specialization, Language handlers + GetTagByIdQueryHandler + GetCategoryByIdQueryHandler. Build: 0 errors. |
| 18 | ContentCore try/catch hardening — all 36 handlers | ✅ | 🤖 Agent | Applied 3 try/catch patterns from `Agents/patterns/error-handling-patterns.md` to all 36 handlers. Pattern A (command with SaveChangesAsync): outer `OperationCanceledException when ct.IsCancellationRequested` + inner `DbUpdateConcurrencyException` → `{Entity}.ConcurrencyConflict`. Pattern B (query handlers): outer `OperationCanceledException` only. Pattern C (TranslateText + BatchTranslate external service calls): `HttpRequestException` → `Translation.ServiceUnavailable` + `TaskCanceledException when !ct.IsCancellationRequested` → `Translation.Timeout`. Also fixed 6 remaining Error record violations in Attachment/Translation handlers (DeleteAttachment, SetPrimaryImage, ReorderAttachments, UpdateTranslation, ApproveTranslation). Added `Microsoft.EntityFrameworkCore 9.0.13` to `ContentCore.Application.csproj`. Build: 0 errors. |
| 19 | HybridCache migration — ContentCore + SharedKernel | ✅ | 🤖 Agent | Migrated from `IMemoryCache` to `HybridCache` (`Microsoft.Extensions.Caching.Hybrid`). 36 files modified. **SharedKernel**: `ICacheableQuery` gained `Tags` property (default `[]`); `QueryCachingBehavior` rewritten to `HybridCache.GetOrCreateAsync` (built-in stampede prevention + tag support); `DependencyInjection.cs` replaced `AddMemoryCache()` with `AddHybridCache()`. **ContentCore queries** (11 files): all query records now have `Tags` getter (`["categories"]`, `["tags"]`, etc.). **ContentCore commands** (17 files): all handlers replaced `IMemoryCache` + `cache.Remove(key)` with `HybridCache` + `cache.RemoveByTagAsync("tag", ct)`. `DeleteCategoryCommandHandler.InvalidateCategoryCache` static method removed — tag eviction makes it unnecessary. Packages: `Microsoft.Extensions.Caching.Hybrid` added to SharedKernel.Application + SharedKernel.Infrastructure; `Microsoft.Extensions.Caching.Memory` removed from SharedKernel.Application. Zero `IMemoryCache` references remain. Caching section in `agent-context.md` + `patterns/caching-patterns.md` fully rewritten. Build: 0 errors, 0 warnings. |
| 17 | ContentCore.Application — Error Handling Patterns | ✅ | 🤖 Agent | Added try/catch patterns to ALL 36 ContentCore.Application handlers. Pattern A (DbUpdateConcurrencyException inner + OperationCanceledException outer) applied to 25 command handlers. Pattern C (HttpRequestException + TaskCanceledException + OperationCanceledException) applied to 2 external translation handlers. Pattern B (OperationCanceledException only) applied to 11 query handlers. Fixed 6 Error record violations in Attachment/Translation handlers. Added `Microsoft.EntityFrameworkCore` package reference to ContentCore.Application.csproj. Build: 0 errors. |
| 20 | Roslyn Analyzers — Directory.Build.props | ✅ | 🤖 Agent | Created `Directory.Build.props` at solution root with 4 analyzers: StyleCop.Analyzers 1.1.118, Meziantou.Analyzer 2.0.182, Roslynator.Analyzers 4.12.10, AsyncAwaitBestPractices 8.0.0. All analyzer-only (PrivateAssets=all, zero runtime footprint). `RunAnalyzersDuringBuild=true`, `EnforceCodeStyleInBuild=true`, warnings not errors (CodeAnalysisTreatWarningsAsErrors=false). Build: 0 errors. |
| 21 | Serilog Structured Logging | ✅ | 🤖 Agent | Replaced built-in ILogger config with Serilog 10.0.0. `SerilogExtensions.cs`: `UseSerilog()` on host, ReadFrom.Configuration, enrichers (Environment, Machine, Thread, Application), console sink (human-readable in dev, JSON in prod), rolling file sink (30-day retention). `UseSerilogRequestLogging()` middleware with enriched context (host, user-agent, client IP, user ID). appsettings.json updated with Serilog MinimumLevel config. Build: 0 errors. |
| 22 | OpenTelemetry Tracing + Metrics | ✅ | 🤖 Agent | `OpenTelemetryExtensions.cs`: tracing for ASP.NET Core (filters health/swagger noise), HttpClient, EF Core, custom MediatR ActivitySource. Metrics for ASP.NET Core + HttpClient. OTLP exporter configurable via `OpenTelemetry:OtlpEndpoint` in appsettings. Console exporter in dev. Packages: OpenTelemetry.Extensions.Hosting 1.15.0, Instrumentation.AspNetCore 1.15.1, Instrumentation.Http 1.15.0, Instrumentation.EntityFrameworkCore 1.15.0-beta.1, Exporter.OTLP 1.15.0, Exporter.Console 1.15.0. Build: 0 errors. |
| 23 | API Versioning | ✅ | 🤖 Agent | `ApiVersioningExtensions.cs`: Asp.Versioning.Http 8.1.1, URL segment strategy, default v1, `AssumeDefaultVersionWhenUnspecified=true`, `ReportApiVersions=true`. All 4 active module endpoint files updated: `/api/content-core` → `/api/v1/content-core`, `/api/accounts` → `/api/v1/accounts`, `/api/auth` → `/api/v1/auth`, `/api/security` → `/api/v1/security`. 10 other modules have empty endpoint stubs (no paths to update yet). Build: 0 errors. |
| 24 | Global Exception Handler | ✅ | 🤖 Agent | `GlobalExceptionHandler.cs`: catch-all `IExceptionHandler` registered AFTER ValidationExceptionHandler + DbUpdateExceptionHandler. Maps OperationCanceledException→499, UnauthorizedAccessException→403, TimeoutException→504, default→500. Adds correlation ID (Activity.Current.Id or TraceIdentifier) to ProblemDetails. Structured Serilog logging with path/method/correlationId. Stack traces only in Development. Build: 0 errors. |
| 25 | Health Checks (Liveness + Readiness) | ✅ | 🤖 Agent | `HealthCheckExtensions.cs`: custom SQL Server check (opens connection, `SELECT 1`), Azure Translator config check. Three endpoints: `/health/live` (zero-check liveness for K8s), `/health/ready` (tagged dependency checks), `/health` (legacy, all checks). JSON response with status, duration, timestamps, per-check detail. Build: 0 errors. |
| 26 | Test Infrastructure | ✅ | 🤖 Agent | Created `tests/YallaJo.Tests.Shared/` (xunit 2.9.3, NSubstitute 5.3.0, FluentAssertions 7.0.0) and `tests/ContentCore.Tests.Unit/` with 6 passing Category domain tests: Create valid, Create raises event, Create null name throws, Update changes props, SoftDelete sets IsDeleted, Update null name throws. Both projects added to solution. `dotnet test`: 6 passed, 0 failed. |
| 27 | Program.cs Infrastructure Wiring | ✅ | 🤖 Agent | Rewired Program.cs: added `builder.AddYallaJoSerilog()`, `builder.Services.AddYallaJoApiVersioning()`, `builder.AddYallaJoOpenTelemetry()`, `builder.Services.AddYallaJoHealthChecks()`, `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()`, `app.UseYallaJoSerilogRequestLogging()`, `app.MapYallaJoHealthChecks()`. Simplified ProblemDetails registration (GlobalExceptionHandler handles dev details). Removed stale `using Microsoft.AspNetCore.Diagnostics`. Build: 0 errors. |
| 28 | ContentPlaces Task 2+3 Bug Fixes & Completions | ✅ | 🤖 Agent | Fixed 6 build errors (wrong concurrency exception type + wrong Result.Conflict overload). Added missing `ReinstateBusiness` command/handler/endpoint. Expanded `BusinessDetailDto` from 22→34 fields (LicenseNumber, TaxId, MetaTitle, MetaDescription, SubscriptionTier, ServiceItemCount, StaffCount, AmenityCount, Translations, BusinessHours, UpdatedAt). Created `BusinessTranslationDto`. Updated `GetBusinessByIdQueryHandler` to use `GetByIdWithDetailsAsync` (single query with all Includes). Fixed `BusinessHoursDto` shape (added Guid Id, changed int→string DayOfWeek). Added `GetByIdWithDetailsAsync` to `IBusinessRepository` + `BusinessRepository`. Improved `ReplaceBusinessHoursAsync` to use `ExecuteDeleteAsync` (bulk SQL delete, no load-then-delete). Added `ICurrentUser` guard to `SuspendBusinessCommandHandler` + `ReinstateBusinessCommandHandler`. All domain state-machine methods remain `void`+throw per guide.md rules. Build: 0 errors. |
| 29 | ContentCore Full Audit | ✅ | 🤖 Agent | Full read of all 140+ ContentCore files. Build: 0 errors throughout. 11 bugs found across Domain, Application, Infrastructure — 2 high, 5 medium, 4 low. Full report written to `Agents/ContentCore-fixes-required.md`. Error log updated (ERR-009 through ERR-012). Guide.md updated: state-change guard rule, fine-grained vs coarse cache tag strategy. agent-context.md updated: gotchas #15–18 added. Notable: ContentCore is architecturally the best module — IContentCoreUnitOfWork correctly wraps IUnitOfWork<TContext> and dispatches domain events (unlike ContentPlaces). |
| 30 | ContentCore All Bugs Fixed | ✅ | 🤖 Agent | Fixed all 11 bugs from ContentCore-fixes-required.md. BUG-001: UpdateLanguageCommandHandler now guards Activate/Deactivate with state checks (prevents duplicate domain events). BUG-002: DeleteAttachmentCommandHandler uses fine-grained cache tags (not coarse "attachments"). BUG-003: Tag.cs changed from BaseEntity→AuditableEntity, [Timestamp] removed, MarkUpdated() used, TagConfiguration cleaned. BUG-004: Dead coarse tags removed from GetEntityCategoriesQuery+GetEntityTagsQuery. BUG-005: ILogger added to all 21 handlers. BUG-006: Cycle detection (HashSet<Guid> visited) added to ListCategoriesQueryHandler.BuildNode. BUG-007: GetCategoryByIdQuery+Handler now support IncludeInactive param for admins. DESIGN-001: Cargo usings removed from IContentCoreUnitOfWork. DESIGN-003: TODO comment added for MediaProcessingBackgroundService. BUG-008/009: asNoTracking explicit, missing step 2 comment added. guide.md updated: BaseEntity vs AuditableEntity selection rule + IncludeInactive pattern. agent-context.md Common Mistakes updated. Build: 0 errors. |
| 31 | ContentCore WS4/WS6 + migration follow-up | ✅ | 🤖 Agent | Implemented binary file signature (magic-byte) validation in `UploadAttachmentCommandHandler` before storage upload, with strict signature↔attachment-type↔MIME↔extension compatibility checks (SVG remains blocked by validator). Verified attachment deletion flow consistency: post-commit delete stays in `DeleteAttachmentCommandHandler`, `AttachmentDeletedDomainEventHandler` remains intentional no-op, upload rollback cleanup remains best-effort. Added EF migration `20260417115007_UpdateContentCoreUnicodeTranslationCacheAndStatus` covering Unicode column updates, `CategoryTranslations.Status`, and `TranslationCaches.OriginalTextHash` unique index (also includes pending Tag audit columns from model drift). Validation: `dotnet build` (0 errors) and `dotnet test tests/ContentCore.Tests.Unit` (6/6 passed). |
| 32 | Auth registration email delivery retry fix | ✅ | 🤖 Agent | Fixed the false-success registration email path. `Auth.Application/EventHandlers/UserCreatedIntegrationEventHandler.cs` now keeps the inbox message unprocessed until `IEmailService.SendAsync` succeeds, invalidates failed OTPs, and rethrows so the outbox can retry cleanly. `ResendOtpCommandHandler.cs` now invalidates unsent codes and returns `Otp.EmailDeliveryFailed` instead of leaving a dead active OTP. Registration copy in `RegistrationEndpoints.cs` now says the code should arrive shortly instead of guaranteeing immediate delivery; `CredentialEndpoints.cs` documents the 500 resend case. Validation: `lsp_diagnostics` clean on 4 touched files; `dotnet build Auth.Application/Auth.Application.csproj` and `dotnet build Auth.Presentation/Auth.Presentation.csproj` both 0 errors. Full solution build was blocked by running `YallaJo.Api`/`YallaJo.Web` processes locking output files. |
| 33 | Gmail SMTP app-password normalization fix | ✅ | 🤖 Agent | Root cause for `SendMailAsync` failure was likely Gmail app passwords copied with spaces from Google UI. `Auth.Infrastructure/Services/GmailEmailService.cs` now trims and de-spaces `GmailOptions.AppPassword`, trims and validates sender/recipient addresses with `MailAddress`, and adds a 30s SMTP timeout before calling `SendMailAsync`. Validation: `lsp_diagnostics` clean on `GmailEmailService.cs`; `dotnet build Auth.Infrastructure/Auth.Infrastructure.csproj -clp:ErrorsOnly -v:q` succeeded with 0 errors (pre-existing analyzer warnings remain). |
| 34 | Registration profile creation made synchronous | ✅ | 🤖 Agent | Fixed the new-user `GET /api/v1/accounts/profile` 404 caused by registration returning before the Accounts profile existed. Added `ProfileCreationRequest` + `CreateForUserAsync` to `Accounts.Contracts/Abstractions/IProfileCreationService.cs`, refactored `Accounts.Application/Services/ProfileCreationService.cs` to share create logic, and updated `Auth.Application/Commands/Register/RegisterCommandHandler.cs` to create the Accounts profile immediately after Security user creation. Profile conflicts are treated as success so the existing Accounts integration-event handler can remain as a safe idempotent fallback. Validation: `lsp_diagnostics` clean on all 3 touched files; `dotnet build Accounts.Contracts/Accounts.Contracts.csproj`, `dotnet build Accounts.Application/Accounts.Application.csproj`, and `dotnet build Auth.Application/Auth.Application.csproj` all completed with 0 errors. |
| 35 | Accounts profile avatar upload contract + cleanup | ✅ | 🤖 Agent | Fixed API/Web mismatch where avatar uploads were saved but rejected because `UpdateAvatarCommandValidator` only allowed absolute URLs while local storage returns rooted relative paths (`/uploads/...`). Validator now accepts absolute or rooted relative URLs. Added compensating cleanup in `Accounts.Presentation/Endpoints/Profile/ProfileEndpoints.cs` to delete uploaded files when `UpdateAvatarCommand` fails, preventing orphaned files. Improved Web UX in `YallaJo.Web/Areas/Accounts/Features/Profile/ProfileController.cs` to show first validation error instead of generic fallback. Added `tests/Accounts.Tests.Unit` with 3 validator regressions and added the project to `YallaJo.sln`. Validation: `dotnet build Accounts.Presentation/Accounts.Presentation.csproj -clp:ErrorsOnly` (0 errors), `dotnet test tests/Accounts.Tests.Unit/Accounts.Tests.Unit.csproj` (3/3), `dotnet test tests/Web.Tests.Unit/Web.Tests.Unit.csproj` (16/16). |
| 36 | Security identity seed role alignment | ✅ | 🤖 Agent | Updated `SeedIdentityProfiles` and `SecurityDbInitializer` so seeded identities use canonical role names for hierarchy testing: exactly one `Owner`, plus `SuperAdmin`, `Admin`, `TourGuide`, and `User`. Removed legacy seed role names (`Guide`, `BusinessOwner`) and aligned role creation/claim mapping to `AppRoles` constants. Validation: `dotnet build Security.Infrastructure/Security.Infrastructure.csproj -clp:ErrorsOnly` (0 errors), `dotnet build YallaJo.SharedKernel.Infrastructure/YallaJo.SharedKernel.Infrastructure.csproj -clp:ErrorsOnly` (0 errors), `dotnet test tests/Security.Tests.Unit/Security.Tests.Unit.csproj` (64/64). |
| 37 | Security seed expansion for Admin and SuperAdmin | ✅ | 🤖 Agent | Expanded `SeedIdentityProfiles` to add three more `Admin` users and three more `SuperAdmin` users for hierarchy and authorization testing. Resulting seed mix: 1 Owner, 4 SuperAdmins, 4 Admins, 2 TourGuides, 5 Users. Validation: `dotnet build Security.Infrastructure/Security.Infrastructure.csproj -clp:ErrorsOnly` (0 errors), `dotnet build YallaJo.SharedKernel.Infrastructure/YallaJo.SharedKernel.Infrastructure.csproj -clp:ErrorsOnly` (0 errors), `dotnet test tests/Security.Tests.Unit/Security.Tests.Unit.csproj` (64/64). |
| 38 | ContentPlaces Place CQRS event/caching fix pass | 🟡 | 🤖 Agent | Fixed the Place delete guard to check `PlaceBusinesses` via `IPlaceRepository.HasActiveLinkedBusinessesAsync`, corrected create-slug uniqueness to use the normalized slug, made `IContentPlacesUnitOfWork` delegate to the shared UoW so aggregate domain events dispatch without breaking layer boundaries, added `PlaceDeletedDomainEvent` + outbox handler, added outbox writes to Place created/updated domain event handlers, made Place list/detail queries implement `ICacheableQuery`, added Place cache keys, and added HybridCache invalidation to Create/Update/Delete/Feature/Verify handlers. Validation: `lsp_diagnostics` clean for ContentPlaces.Application/Infrastructure/Domain, `dotnet build ContentPlaces.Application/ContentPlaces.Application.csproj -clp:ErrorsOnly` (0 errors), `dotnet build ContentPlaces.Presentation/ContentPlaces.Presentation.csproj -clp:ErrorsOnly` (0 errors). Remaining gap: `categoryId` and `hasActiveTours` list filtering still need underlying model/query support before they can be implemented correctly. |

---
## [TRACKING] What Needs To Be Done Next
### Wave 1 — ContentCore Completion
- ~~Category tree, depth validation, slug auto-gen, reorder~~ ✅ Done
- ~~Specialization CQRS~~ ✅ Done
- Verify all endpoints end-to-end (recommend running Swagger after migration)
- ~~EF Migrations — all ContentCore entity schemas were pre-existing; no new schema changes from this session's fixes~~ ✅ Updated: migration `20260417115007_UpdateContentCoreUnicodeTranslationCacheAndStatus` added for Unicode/Status/hash schema updates
- Apply latest ContentCore migration to the target database and smoke-test ContentCore attachment/category/language flows in Swagger
- Add WS10-focused automated tests for signature mismatch rejection, human-reviewed translation preservation, and translation-cache hash dedup race safety
### Wave 2 — ContentPlaces Full Module (34 endpoints)
- See `Agents/ContentPlaces-tasks.md` for the complete task breakdown, WBS, and implementation rules
- **Task 1**: Place CQRS + Admin Actions (8 endpoints) — Phase 1
  - Remaining Place gap: add real `categoryId` / `hasActiveTours` filtering only after Place/category and tour-count data is modeled in ContentPlaces or exposed via a proper cross-module read path
- ~~**Task 2**: Business CQRS + Full State Machine (9 endpoints) — Phase 1~~ ✅ Done (+ Reinstate = 10 actual endpoints)
- ~~**Task 3**: BusinessHours Batch Upsert (2 endpoints) — Phase 1~~ ✅ Done
- **Task 4**: ServiceItem Full CQRS (5 endpoints) — Phase 1
- **Task 5**: BusinessAmenity Management (3 endpoints) — Phase 1
- **Task 6**: BusinessStaff Management (3 endpoints) — Phase 1
- **Task 7**: Place Geo-Search — Nearby + Map Viewport (2 endpoints) — Phase 2
- **Task 8**: AccessibilityFeature Get + Update (2 endpoints) — Phase 3
### Wave 3 — Phase 1 MVP remaining (~80 endpoints)
- **ContentTours**: Tours full CQRS (~34 endpoints)
- **Booking**: Core booking state machine (~32 endpoints)
- **Finance**: Payments, payouts (~52 endpoints)
- **Social**: Reviews, favorites (~22 endpoints)
### Wave 3 — Phase 2+
- ContentBlogs, ContentSeo, Messaging, Analytics, Tracking
- Middleware: CorrelationId, RequestLocalization, RateLimiting, SeoRedirect, CORS, ResponseCompression
- 18 background services, 3 SignalR hubs
### Infrastructure
- ~~Roslyn Analyzers~~ ✅ Done — Directory.Build.props with 4 analyzers
- ~~Serilog~~ ✅ Done — structured logging with enrichers, file + console sinks
- ~~OpenTelemetry~~ ✅ Done — tracing + metrics, OTLP exporter configurable
- ~~API Versioning~~ ✅ Done — Asp.Versioning.Http, URL segment, /api/v1/
- ~~Global Exception Handler~~ ✅ Done — catch-all with correlation IDs
- ~~Health Checks~~ ✅ Done — /health/live, /health/ready, /health
- ~~Test Infrastructure~~ ✅ Done — YallaJo.Tests.Shared + ContentCore.Tests.Unit (6 tests)
- Smoke-test `/api/v1/auth/register` + `/api/v1/auth/resend-otp` against a running environment after stopping `YallaJo.Api`/`YallaJo.Web` so a full solution build can complete without file-lock errors
- Clean up StyleCop SA1200 warnings (usings outside namespace) across existing files — low priority, cosmetic
- Add integration tests with Testcontainers + Respawn (per module, as modules are implemented)
- Docker + Aspire setup (when ready for deployment)
- EF Migrations for all entities
- File storage: Local → Cloudinary
- User builds remaining MVC admin controllers/views

---
## 🧱 [REQUIRED] New Entity Checklist
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
- [ ] `Queries/{Entity}/List{Entities}/` — Query **with `ICacheableQuery`** (SummaryDto, paginated, AsNoTracking)
- [ ] `Queries/{Entity}/Get{Entity}ById/` — Query **with `ICacheableQuery`** (DetailDto)
- [ ] `Caching/{Module}CacheKeys.cs` — static key factory class (if not already exists for this module)
- [ ] `Microsoft.Extensions.Caching.Hybrid` **`9.3.0`** in `{Module}.Application.csproj` (if not already present)
- [ ] **All command handlers inject `HybridCache` and call `RemoveByTagAsync` after successful save**
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
## 🔧 [REQUIRED] Scaffold & Template Usage Rules
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
   - [ ] **Every `IQuery<T>` record implements `ICacheableQuery`** — `CacheKey` (from CacheKeys class), `CacheDuration` (5 min for lists/detail), `Tags` (coarse `"{entity}s"` + fine `"{entity}:{id}"` for detail)
   - [ ] **Every command handler injects `HybridCache`** and calls `await cache.RemoveByTagAsync(...)` **after** successful save
   - [ ] **`{Module}CacheKeys.cs`** exists with a method for every cached query
   - [ ] **Auth-varied queries** (admin sees different data than public) include `userId` and `isAdmin` in the `CacheKey`

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
## 📦 [REQUIRED] DI Registration Rules
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
## 🗄️ [REFERENCE] Caching Rules

**Current state**: ContentCore fully migrated to `HybridCache` (`Microsoft.Extensions.Caching.Hybrid` — GA March 2025). `QueryCachingBehavior` uses `HybridCache.GetOrCreateAsync` with built-in stampede prevention + tag-based eviction. 11 cached queries with `ICacheableQuery` (Tags property for group invalidation). 17 command handlers use `RemoveByTagAsync`. `Result<T>` and `Result` classes have `[JsonConstructor]` for future Redis L2 serialization. All new modules MUST use `HybridCache` from day one.

---
### Architecture Decision: HybridCache (Primary) + Output Caching (Selective)

| Layer | Technology | When to Use |
|-------|-----------|-------------|
| **Data caching** (primary) | `HybridCache` | All CQRS query handlers, all modules — replaces `IMemoryCache` |
| **Response caching** (selective) | Output Caching middleware | Read-only anonymous endpoints with identical responses for all users (e.g., `GET /api/places/nearby`, `GET /api/places/map/viewport`) |
| **Distributed L2** (future) | Redis via `IDistributedCache` | Add when scaling to multiple servers — zero code changes to handlers |

**Why HybridCache over raw IMemoryCache:**
- **Stampede prevention built-in**: `GetOrCreateAsync` coalesces concurrent requests for the same key — only one DB call fires, all waiters get the same result. No manual `SemaphoreSlim`.
- **Tag-based eviction**: `RemoveByTagAsync("categories")` removes all category-related entries in one call. Eliminates the fragile pattern of 6+ individual `cache.Remove()` calls per handler.
- **Negative caching**: `GetOrCreateAsync` naturally caches the factory result even when it returns empty/default — protects the DB from repeated misses on non-existent entities.
- **L1/L2 architecture**: Uses in-memory (L1) by default. Add `IDistributedCache` registration later for L2 (Redis) — handlers remain unchanged.
- **Serialization built-in**: JSON by default. Configurable for Protobuf. No manual `Serialize`/`Deserialize` code.

**Why NOT Output Caching for most endpoints:**
- YallaJo uses CQRS with `Result<T>`. Endpoints return different data based on auth context (admin vs anonymous, owner vs visitor).
- Output Caching caches the **full HTTP response** including headers — it would serve admin data to anonymous users or vice versa.
- **Only use Output Caching** on truly anonymous, read-only, identical-for-all-users endpoints with explicit `VaryByQueryKeys`.

---
### The "To Cache or Not to Cache" Decision

#### Read-to-Write Ratio Rule
Calculate: **reads per minute / writes per minute** for each entity.

| Ratio | Decision | Example |
|-------|----------|---------|
| **> 100:1** | MUST cache | Languages (read on every API call, written once a month) |
| **10:1 to 100:1** | SHOULD cache | Categories, Tags (read every page load, written by admin weekly) |
| **1:1 to 10:1** | MAY cache with short TTL (1–5 min) | Paginated place lists (read often, new places added daily) |
| **< 1:1** | MUST NOT cache | Bookings (written as often as read — stale data = double bookings) |

#### What MUST Be Cached
| Entity/Data | Why | TTL (Absolute) | TTL (Local) | Tags |
|---|---|---|---|---|
| Languages | Read every request (Accept-Language), written once a month | 60 min | 30 min | `languages` |
| Categories (tree) | Read every page, written by admin rarely | 30 min | 15 min | `categories` |
| Tags | Read on every listing page | 30 min | 15 min | `tags` |
| Specializations | Reference data, rarely changes | 30 min | 15 min | `specializations` |
| Entity attachments | Media URLs, rarely change after upload | 15 min | 10 min | `attachments:{entityType}:{entityId}` |
| Entity categories/tags junctions | Read on every detail page | 15 min | 10 min | `entity-junctions:{entityType}:{entityId}` |
| Translated content | Expensive to regenerate (Azure Translator API) | 120 min | 30 min | `translations:{entityType}:{entityId}` |
| Place/Business detail by slug | High-traffic SEO pages | 10 min | 5 min | `places`, `place:{id}` |

#### What MUST NEVER Be Cached
| Data | Why |
|------|-----|
| Booking state / slot availability | Real-time — stale data = double bookings, overselling |
| Payment status / transaction state | Financial accuracy is critical — always read from DB |
| User authentication tokens / sessions | Security — cached tokens can't be revoked |
| User permissions / roles | Security — must reflect DB state at all times |
| Any entity with `LockedUntil` or time-sensitive state | Race condition risk |
| Stock/inventory counts during checkout | Real-time accuracy required at point of sale |
| Currency exchange rates (if < 1h old) | Financial data — cache only with exact absolute TTL matching data freshness |

#### When to Intentionally Bypass the Cache
- **Admin write-then-read flow**: After admin creates/updates an entity, the immediately following GET must see fresh data. Use `HybridCacheEntryFlags.None` or add a `?nocache=1` query param that the handler respects.
- **Debug/troubleshooting**: Support a `Cache-Control: no-cache` header check in the pipeline behavior for admin users only.
- **Data migration/import**: Bulk import operations should bypass cache entirely and invalidate by tag after completion.

---
### HybridCache — API & Patterns

#### Registration
**Code**: See [`Agents/patterns/caching-patterns.md`](patterns/caching-patterns.md) → §HybridCache Registration

#### GetOrCreateAsync — Cache-Aside in One Call (MANDATORY pattern)
**Code**: See [`Agents/patterns/caching-patterns.md`](patterns/caching-patterns.md) → §HybridCache GetOrCreateAsync Pattern

**Key behavior**: If 50 concurrent requests arrive for the same key and the cache is empty, `GetOrCreateAsync` executes the factory callback ONCE and returns the same result to all 50 callers. This is stampede prevention built into the API.

#### Tag-Based Eviction (MANDATORY for command handlers)
**Code**: See [`Agents/patterns/caching-patterns.md`](patterns/caching-patterns.md) → §HybridCache Tag-Based Eviction

**Rules**:
- Every `ICacheableQuery` MUST declare `Tags` (one or more strings)
- Command handlers call `cache.RemoveByTagAsync("tag")` instead of individual `cache.Remove(key)` calls
- Tag convention: `{module}:{entity}` for entity-level tags, `{module}:{entity}:{id}` for instance-level tags

#### Negative Caching (Cache Penetration Defense)
**Code**: See [`Agents/patterns/caching-patterns.md`](patterns/caching-patterns.md) → §HybridCache Negative Caching

When `GetOrCreateAsync` factory returns `null` or a sentinel "not found" value, cache it with a **short TTL (30–60 seconds)**. This prevents an attacker from hammering `GET /api/tags/{random-guid}` and bypassing the cache on every request.

#### ICacheableQuery Interface (Updated for HybridCache)
**Code**: See [`Agents/patterns/caching-patterns.md`](patterns/caching-patterns.md) → §Updated ICacheableQuery Interface

---
### Cache Key Convention (MANDATORY format)
**Format**: `{module}:{entity}:{scope}:{params}` — all lowercase, colon-separated

| Key Pattern | Example | What It Caches |
|---|---|---|
| `cc:cats:{activeOnly}:{parentId}:{withTranslations}` | `cc:cats:true:root:false` | Category list |
| `cc:cat:{id}:{withTranslations}` | `cc:cat:abc123:true` | Single category |
| `cc:tags:{activeOnly}` | `cc:tags:false` | Tag list |
| `cc:langs:{activeOnly}` | `cc:langs:true` | Language list |
| `cp:place:{id}` | `cp:place:abc123` | Place detail |
| `cp:places:list:p{page}:s{size}` | `cp:places:list:p1:s20` | Paginated place list |
| `{module}:{entity}:{id}:detail` | `cp:biz:abc123:detail` | Business detail |

**Module prefixes**: `cc` = ContentCore, `cp` = ContentPlaces, `ct` = ContentTours, `bk` = Booking, `fn` = Finance

**Rules**:
- NEVER use GUIDs directly as keys without a prefix (`cc:tag:{id}` not just `{id}`)
- NEVER cache user-specific data with a shared key — include `user:{userId}` in key
- NEVER use spaces in cache keys
- Always use the module's static `CacheKeys` class — never hand-write key strings

---
### Expiration Strategy

#### Absolute vs Local (Sliding) Expiration — When to Use Each
| Expiration | What It Does | Use When |
|---|---|---|
| **Absolute** (`Expiration`) | Hard cap — entry evicted after this duration no matter what | ALL cached data — prevents serving infinitely stale content |
| **Local** (`LocalCacheExpiration`) | L1 in-memory expiration (shorter than absolute) — keeps L1 fresh relative to L2 | Multi-tier cache (L1 memory + L2 Redis). Set L1 shorter to re-sync from L2 periodically |

#### TTL Policy by Data Type
| Data Type | Absolute TTL | Local L1 TTL | Tags | Reason |
|---|---|---|---|---|
| Reference data (categories, tags, languages, specializations) | 30–60 min | 15 min | `{entity}` | Rarely changes, invalidated on write |
| Translated content | 120 min | 30 min | `translations:{entityType}:{entityId}` | Expensive to regenerate |
| Single entity by ID (detail) | 5 min | 2 min | `{entity}`, `{entity}:{id}` | Short TTL protects against cached not-found (negative cache). Tag eviction handles write-then-read freshness. |
| Paginated list | 5 min | 2 min | `{entity}:list` | Changes frequently with new data |
| Negative cache (not-found sentinel) | 30–60 sec | 30 sec | same as positive entry | Short — entity may be created soon after |
| Config / feature flags | 60 min | 15 min | `config` | Changes require app reaction |

**Rule**: ALWAYS set `Expiration` (absolute). Set `LocalCacheExpiration` when using L2 (Redis). For single-server (current state), `Expiration` alone is sufficient.

---
### Defensive Caching — Pitfalls & Prevention

#### Cache Stampede (Thundering Herd)
**Problem**: Cache expires → 100 concurrent requests all miss → 100 DB queries fire simultaneously.
**Solution**: `HybridCache.GetOrCreateAsync` prevents this internally. It detects concurrent requests for the same key, executes the factory ONCE, and returns the result to all waiters. No manual locking needed.
**Old pattern** (IMemoryCache): Required `SemaphoreSlim` + double-check. Error-prone and doesn't compose. **Avoid.**

#### Cache Penetration (Non-Existent Entity Attacks)
**Problem**: Attacker requests `GET /api/tags/{random-guid}` → always misses cache → always hits DB.
**Solution**: Cache the "not found" result with a short TTL (30–60 seconds). `GetOrCreateAsync` makes this natural — the factory returns a sentinel or `default(T)`, and it gets cached like any other value.
**Rule**: Query handlers MUST cache miss results. Use a wrapper DTO or `Result<T>` that distinguishes "cached not-found" from "never queried".

#### Cache Avalanche (Mass Expiration)
**Problem**: All cache entries expire at the same time → sudden spike of DB queries.
**Solution**: Add jitter to TTLs. Instead of `30 minutes` for all categories, use `30 + Random(0, 5) minutes`. HybridCache does NOT do this automatically — add jitter in the `ICacheableQuery.CacheDuration` getter.

---
### Output Caching — Selective Use Only

**When to use**: Anonymous, read-only, identical-for-all-users Minimal API endpoints where the full HTTP response is the same for every caller.

**Good candidates in YallaJo**:
- `GET /api/places/nearby` (anonymous, geo-only, no auth context)
- `GET /api/places/map/viewport` (anonymous, bounding-box only)
- `GET /api/content-core/languages` (anonymous, same for everyone)

**How to apply**: `.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)).Tag("places"))` on the endpoint, plus `IOutputCacheStore.EvictByTagAsync("places")` in the corresponding command handler.

**NEVER use Output Caching on**: Any endpoint where the response varies by auth (admin sees pending businesses, anonymous sees only approved). Any endpoint with pagination that changes per request. Any POST/PUT/DELETE endpoint.

---
## 🔗 [REFERENCE] Module Dependency Rules
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
## 🔐 [REFERENCE] Auth & Authorization
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
## ✅ [REFERENCE] Validation & Error Flow
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
## 📁 [TRACKING] Key File Locations
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
## 🚀 [TRACKING] How to Run & Verify
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
## 📋 [TRACKING] Constraints (From User)
- Translation service: API-based, auto-translate, pluggable (Azure primary)
- Events: Use outbox/inbox pattern
- File storage: Local now, Cloudinary later — pluggable via `IFileStorageService`
- YallaJo.Api = pure API. YallaJo.Web = separate UI layer via HttpClient BFF
- Web: Only AuthController built by agent — user handles rest
- MUST follow `guide.md` exactly for all code patterns
- MUST follow `YallaJo.md` + Business Rules PDF for business logic

---
## ⚙️ [TRACKING] Agent Efficiency Rules
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
## 📛 [REQUIRED] Naming Conventions
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
## 🔒 [CRITICAL] Security Rules
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
## 🔀 [REFERENCE] Cross-Module Communication Rules
Modules are isolated by design. Breaking isolation creates coupling that compounds over time.

| Rule | Why |
|------|-----|
| **Never `using` another module's Domain or Application namespace** | Breaks module isolation. |
| **Shared DTOs and interfaces go in `{Module}.Contracts`** | That's what the Contracts projects exist for. |
| **Cross-module data access = integration event via outbox/inbox** | Never query another module's DbContext directly. |
| **Cross-module references only allowed through Contracts** | `{Module}.Application` can reference `{OtherModule}.Contracts` — nothing else. |

---
## 🤔 [TRACKING] Agent Decision-Making Rules
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
## 🤝 [TRACKING] Session Handoff Rules
Every session must leave the codebase in a clean, resumable state for the next agent.

| Rule | Why |
|------|-----|
| **If you can't complete your task**, mark it 🟡 in "What Has Been Built" with a note saying exactly what's left | Next agent knows where to pick up |
| **Never leave uncommitted broken code** — either finish the feature or revert | Next agent inherits a clean state |
| **If you discover something that changes the plan**, update "What Needs To Be Done" immediately | Plans drift — the doc must reflect reality |
| **If a module's status changed**, update the Module Status Overview table | Next agent trusts the table to be accurate |
| **Update the Build State in the header** with your final `dotnet build` result | Next agent knows if the build is clean |

---
## 🔐 [REQUIRED] Concurrency & Data Integrity Rules
| Rule | Detail |
|------|--------|
| **AuditableEntity has `RowVersion`** — always configure `.IsRowVersion()` in EF config | Enables optimistic concurrency. Prevents silent data overwrites when two users edit the same record. |
| **Soft delete for user-facing entities** | Use `SoftDelete()` method (sets `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`). User data MUST be recoverable. |
| **Hard delete for junction tables and internal records** | Junction rows (EntityCategory, EntityTag) are disposable — use real `DELETE`. |
| **Unique constraints MUST be enforced at DB level** | Code-level uniqueness checks have race conditions. Always add a unique index in the EF configuration. Code checks are an optimization on top, not a replacement. |
| **Check for existence before creating** (slug, email, etc.) | Use `ExistsAsync()` with the unique field, return `Result.Conflict()` if already taken. |

---
## 🕐 [REQUIRED] DateTime & Guid Rules
| Rule | Detail |
|------|--------|
| **Always `DateTime.UtcNow`** — never `DateTime.Now` | Store UTC everywhere. Convert to local only on the client/display layer. Jordan is UTC+3 — mixing local/UTC corrupts data. |
| **Always `Guid.CreateVersion7()`** — never `Guid.NewGuid()` | V7 GUIDs are time-sortable, which means better clustered index performance in SQL Server. `NewGuid()` is random = index fragmentation. |
| **`DateTimeOffset` for user-facing timestamps** | When an API response includes a timestamp the user will see, use `DateTimeOffset` so the timezone is explicit. Internal storage remains `DateTime` in UTC. |

---
## 📊 [REQUIRED] Enum Handling Rules
| Rule | Detail |
|------|--------|
| **Store enums as `int` in DB** | Always use `.HasConversion<int>()` in EF config. Never store as strings — ints are smaller, faster, and index-friendly. |
| **Define all enums in `{Module}.Domain/Enums/`** | One file per enum. Keep them in the Domain layer — they are part of the domain model. |
| **Shared enums go in `{Module}.Contracts`** | If another module needs to reference an enum (e.g., `EntityType`), put it in the Contracts project. |
| **Always add a `None = 0` or meaningful default** | Uninitialized enums default to `0`. Make sure `0` is either invalid (caught by validation) or a safe default. |

---
## 🔤 [REQUIRED] String Column Rules
YallaJo is a Jordanian tourism app — **Arabic content is expected**. String handling must account for multilingual data.

| Column Type | EF Config | Why |
|-------------|-----------|-----|
| Slugs, codes, status strings | `.IsUnicode(false).HasMaxLength(200)` | Always ASCII — no Arabic. Smaller storage + faster indexing. |
| Names, titles, descriptions | `.HasMaxLength(500)` (default Unicode) | May contain Arabic, English, or other scripts. Unicode is EF default. |
| Long text (body, bio, content) | `.HasMaxLength(4000)` or `.HasColumnType("nvarchar(max)")` | Large user content. Set a reasonable max or use max. |
| **Every string column MUST have `HasMaxLength()`** | No exceptions | Unbounded `nvarchar(max)` on every column is wasteful and prevents indexing. |

---
## 🚨 [CRITICAL] Error Handling Rules
Errors are handled differently at each layer. MUST follow every rule in this section.

---
### Rule: Two Categories of Error — Know the Difference
| Category | Definition | How to Handle |
|----------|-----------|---------------|
| **Business error** | Expected, valid failure path. User did something invalid or entity is in wrong state. | Return `Result<T>.Failure(...)` — NEVER throw. |
| **Programming error** | Bug in code. Null where not expected, wrong type, violated invariant. | Throw `ArgumentException` / `InvalidOperationException` — these are bugs to fix, not handle. |
| **Infrastructure error** | External system failed (DB down, API 500, disk full). | Propagate — global handler catches and returns 500. OR catch and convert to `Result.Failure` in Infrastructure layer only. |

---
### By Layer (STRICT — no exceptions to these rules)
#### Domain Layer
- MUST use `ArgumentException` / `ArgumentNullException` for guard clauses in `Create()` and `Update()` — these are programming errors, not business errors.
- MUST use domain events for cross-aggregate side effects — never throw across aggregates.
- MUST NOT return `Result<T>` from entity methods — entities return `void` or the entity itself.
- MUST NOT reference `Result<T>` — Domain has no dependency on Application abstractions.

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Domain Guard Clauses

#### Application Layer
- MUST return `Result<T>` for ALL handler outcomes — success AND failure.
- MUST NOT throw for business logic failures.
- MUST catch domain `ArgumentException` that indicates a programmer called Create() wrong — this is a 500, let it propagate.
- MUST use specific error codes (`{Entity}.{Reason}`) — never generic strings.

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Application Result Handler Example

#### Infrastructure Layer
- MUST NOT catch general `Exception` — only catch specific, known exception types.
- MUST wrap external API calls (HTTP, file storage, third-party SDKs) in try/catch and return `Result.Failure`.
- MUST propagate EF exceptions unless converting a specific type to a user-friendly error.
- See §Try/Catch & Exception Rules for exact catch patterns.
#### Presentation Layer
- MUST NOT contain any try/catch.
- MUST rely entirely on `result.ToApiResult()` for business errors.
- MUST rely entirely on global `IExceptionHandler` for infrastructure errors.

---
### Result<T> Usage Patterns
#### All Result Factory Methods
**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Result Factory Methods

#### Checking Results (in orchestration code)
**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Safe Result Checking Pattern

#### Never Access .Value Without Checking IsSuccess
**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Safe .Value Access Pattern

---
### Error Code Convention
Error codes MUST follow `{Entity}.{Reason}` format. They MUST be:
- **Unique** across the entire codebase — search before adding a new one
- **Machine-readable** — PascalCase, no spaces, no punctuation except the dot
- **Stable** — once a code is used by clients, NEVER rename it (it breaks client error handling)
- **Documented** inline in the handler where they're used

| Code Pattern | HTTP | When to Use | Example |
|-------------|------|-------------|---------|
| `{Entity}.NotFound` | 404 | Entity doesn't exist | `"Category.NotFound"` |
| `{Entity}.AlreadyExists` | 409 | Duplicate slug, email, etc. | `"Tour.AlreadyExists"` |
| `{Entity}.InvalidState` | 400 | Action not allowed in current state | `"Booking.InvalidState"` |
| `{Entity}.InvalidTransition` | 400 | State machine — invalid move | `"Booking.InvalidTransition"` |
| `{Entity}.Unauthorized` | 403 | User lacks permission | `"Place.Unauthorized"` |
| `{Entity}.DependencyConflict` | 409 | Can't delete — other entities reference it | `"Category.DependencyConflict"` |
| `{Entity}.ConcurrencyConflict` | 409 | RowVersion conflict — edited by another user | `"Tour.ConcurrencyConflict"` |
| `{Entity}.QuotaExceeded` | 429 | User exceeded allowed limit | `"Booking.QuotaExceeded"` |
| `Service.Unavailable` | 503 | External API down | `"Translation.ServiceUnavailable"` |
| `Service.Timeout` | 504 | External API timed out | `"Payment.Timeout"` |
| `Request.Cancelled` | 499 | Client disconnected | `"Request.Cancelled"` |

---
### Global Exception Pipeline
**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Global Exception Pipeline Flow

#### Global Exception Handler Shape (what the client receives)
**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §ProblemDetails JSON Shapes

---
### Custom Exception Types (When to Create)
ONLY create a custom exception when:
1. You need to carry additional context that `Exception.Message` cannot express
2. A specific layer needs to react to it differently than a generic `Exception`
3. It maps to a specific HTTP status code in the global handler

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Custom Exception Example

**NEVER create custom exceptions for business errors** — those belong in `Result<T>` as error codes.
**NEVER create custom exceptions as wrappers** — they hide the real exception type.

---
## 🔁 [REFERENCE] Polly Resilience & Retry Policy Rules
Polly handles transient failures from external services (Azure Translator, payment APIs, HTTP calls). It MUST be used on every `HttpClient` that calls an external service. It MUST NOT be used on DB calls — EF's `EnableRetryOnFailure` already handles that.

---
### Package & Registration
**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §HttpClient Registration with Standard Resilience

---
### Decision: Which Policy to Use
| Scenario | Policy | Use When |
|----------|--------|----------|
| External API flaky (429, 503) | **Retry** | Transient errors expected to self-heal |
| External API consistently failing | **Circuit Breaker** | Stop hammering a broken service |
| External API slow | **Timeout** | Prevent blocking threads on slow responses |
| Limit concurrent calls | **Bulkhead** | Isolate one service from starving others |
| External API permanently down | **Fallback** | Return degraded response instead of error |
| All of the above | **Resilience Pipeline** | Compose: Timeout → Retry → Circuit Breaker |

**Rule**: ALWAYS compose policies in this order: **Timeout → Retry → Circuit Breaker → Fallback**. Inner policies execute first.

---
### Retry Policy
Use for transient HTTP errors: `408 Request Timeout`, `429 Too Many Requests`, `500`, `502`, `503`, `504`.

**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Retry Policy Configuration

#### What MUST NOT be Retried
**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Non-Retryable Statuses Reference

#### Retry with Respect-Retry-After Header (for 429 Rate Limits)
**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Retry-After Header Support

---
### Circuit Breaker Policy
Stops sending requests when a service is failing — gives it time to recover. MUST be combined with retry.

**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Circuit Breaker Configuration

#### When Circuit Is Open — Handle BrokenCircuitException
**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §BrokenCircuitException Handling

---
### Timeout Policy
Prevents requests from hanging indefinitely. MUST be the OUTERMOST policy (executes first).

**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Timeout Policy Configuration

| External Service | Recommended Total Timeout | Per-Attempt Timeout |
|-----------------|--------------------------|---------------------|
| Azure Translator | 15s | 5s |
| Payment Gateway | 30s | 10s |
| File Upload (Cloudinary) | 60s | 30s |
| Internal services | 5s | 2s |

---
### Full Resilience Pipeline (Recommended Composition)
**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Full Resilience Pipeline Composition

---
### EF Core — Already Has Retry (Do NOT add Polly on top)
**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §EF Core Retry Note

---
### Idempotency Rule for Retries
**CRITICAL**: Only retry IDEMPOTENT operations.

| Operation | Idempotent? | Safe to Retry? |
|-----------|------------|----------------|
| `GET` requests | ✅ Yes | ✅ Always |
| `PUT` (full replace) | ✅ Yes | ✅ Always |
| `DELETE` | ✅ Yes (second delete = 404, acceptable) | ✅ Yes |
| `POST` (create) | ❌ No | ⚠️ Only with idempotency key |
| Payment charge | ❌ No | ⚠️ Only with `IdempotencyKey` header |
| Email/SMS send | ❌ No | ❌ NEVER — user gets duplicate |

**Rule**: NEVER configure retry on payment creation, email sending, or SMS sending without an idempotency key mechanism in place.

---
### Polly Logging (MANDATORY)
Every resilience handler MUST log retry attempts:

**Code**: See [`Agents/patterns/polly-patterns.md`](patterns/polly-patterns.md) → §Polly Logging Callback

## 🔴 [CRITICAL] Try/Catch & Exception Rules
Exception handling is one of the most misused patterns in .NET. Follow these rules exactly — every violation either swallows errors silently or crashes the pipeline unexpectedly.
### The #1 Rule: Never Catch What You Can't Handle
**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Try/Catch Golden Rule

---
### Where try/catch IS Allowed (Whitelist)
Only add try/catch when you can take a MEANINGFUL action on the specific exception type.
#### 1. Infrastructure Layer — External Service Calls
Wrap calls to external APIs (Azure Translator, payment gateway, file storage) to convert infrastructure failures into `Result` failures:

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Whitelist: External Service try/catch

#### 2. Infrastructure Layer — Optimistic Concurrency
Catch `DbUpdateConcurrencyException` ONLY when you want to return a user-friendly conflict response instead of a 500:

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Whitelist: Optimistic Concurrency try/catch

#### 3. Background Services — Prevent Worker Crash
Background services MUST NOT crash on individual item failures — the worker loop MUST continue:

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Whitelist: Background Worker try/catch

#### 4. Application Layer — TaskCanceledException (Graceful Shutdown)
If you want to distinguish between user-cancelled requests and app shutdown:

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Whitelist: Cancellation Handling

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

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Logging in catch Blocks

---
### The finally Block Rule
Use `finally` ONLY for resource cleanup — never for business logic:

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §finally Block Usage

---
### Exception Wrapping Rule
When you MUST wrap an exception (rare), ALWAYS include the original as `innerException` and log before wrapping:

**Code**: See [`Agents/patterns/error-handling-patterns.md`](patterns/error-handling-patterns.md) → §Exception Wrapping Pattern

---
## ✅ [REQUIRED] Validation Rules
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
## 📝 [REFERENCE] Logging Rules
| Rule | Detail |
|------|--------|
| **Use structured logging** | `_logger.LogInformation("Created {EntityType} with {EntityId}", entityType, entityId)` — NOT `$"Created {entityType} with {entityId}"` |
| **Never log sensitive data** | No passwords, JWT tokens, credit card numbers, or PII in logs |
| **Log levels** | `Trace`: verbose debug. `Debug`: dev-only detail. `Information`: normal operations (entity created/updated/deleted). `Warning`: recoverable issues (retry, slow query). `Error`: failures requiring attention. `Critical`: app-stopping failures. |
| **Log at handler boundaries** | Log at the start and end of command/query handlers. The `LoggingBehavior` does this automatically — don't duplicate. |
| **Log external service calls** | Always log before/after calling external APIs (Azure Translator, payment gateway, etc.) with correlation IDs |

---
## 🔁 [REFERENCE] Domain Events vs Integration Events
| Aspect | Domain Event | Integration Event |
|--------|-------------|-------------------|
| **Scope** | Same module, same transaction | Cross-module, different transactions |
| **Delivery** | Synchronous via MediatR (dispatched by UoW before SaveChanges) | Asynchronous via outbox/inbox pattern |
| **When to use** | Side effects within the same aggregate/module: update translation cache, create audit log, cascade state change | Notify other modules: booking created → finance creates invoice, place updated → SEO regenerates sitemap |
| **Handler rule** | NEVER call `SaveChangesAsync()` — changes piggyback on the aggregate's save | Writes to outbox table in handler — separate process picks up and delivers |
| **Naming** | `{Entity}{PastTenseVerb}DomainEvent` | `{Entity}{PastTenseVerb}IntegrationEvent` |
| **Never** cross module boundaries | ✅ Correct | If you need cross-module → use integration event |

---
## 🛡️ [CRITICAL] Pre-flight & Completion Verification
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
- [ ] **Every query record implements `ICacheableQuery` (CacheKey + CacheDuration + Tags)?**
- [ ] **Every command handler injects `HybridCache` and calls `RemoveByTagAsync` after successful save?**
- [ ] **`Microsoft.Extensions.Caching.Hybrid` `9.3.0` added to `{Module}.Application.csproj`?**
- [ ] **`{Module}CacheKeys.cs` static class exists with all key methods?**
- [ ] **Auth-varied queries include `userId`/`isAdmin` in cache key?**
- [ ] `dotnet build` passes with 0 errors?
- [ ] `lsp_diagnostics` clean on all changed files?
### Rollback Strategy
If your changes break the build after 3 consecutive fix attempts:

1. **Stop** — do not make more changes
2. **`git stash`** your work to preserve it without polluting main
3. **Report** what went wrong in the error log and in "What Has Been Built" (mark 🟡)
4. **Let the next agent** try fresh with the documented context of what failed

---
## 🧪 [REFERENCE] Testing Strategy & Rules
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
## 🌿 [REQUIRED] Git Conventions
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
## 📖 [REFERENCE] API Documentation Rules
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
## 🌍 [REFERENCE] Localization & RTL Rules
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
## 🌐 [REFERENCE] Environment Rules
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
## 📦 [REFERENCE] Dependency Management Rules
| Rule | Detail |
|------|--------|
| **Never add a NuGet package without asking the user first** | New dependencies have long-term maintenance costs. Always propose before adding. |
| **MUST use existing packages first** | If a capability is already covered by an installed package, use it. Do NOT add a second package for the same purpose. |
| **No preview packages in production code** | Unless the project explicitly uses a preview SDK (YallaJo uses .NET SDK 10.0.200-preview, targeting net9.0 — this is fine). |
| **Pin versions** | Always specify exact version in `.csproj` — no floating versions (`*`). |
| **Version consistency is MANDATORY** | Every `Microsoft.Extensions.*` package MUST use the same major.minor version across ALL projects. The canonical version band for this project is **`9.x`** (e.g., `9.0.x`, `9.3.0`, `9.4.0`). NEVER add a `10.x` version of any `Microsoft.Extensions.*` package — even if the SDK is .NET 10 preview and NuGet resolves it. The target framework is `net9.0` and the extensions ecosystem must stay on `9.x`. |
| **Before adding any `Microsoft.Extensions.*` package** | Search the solution for the same package family (`grep -r "Microsoft.Extensions" *.csproj`). Use the exact version already present. If not present, use the latest `9.x` stable. |

### ⚠️ Version Consistency Rule (STRICT — violations cause NU1605 build errors)

This project targets `net9.0` with SDK `10.0.x-preview`. This combination is valid.
However, **NuGet package versions must stay internally consistent**:

| Package Family | Canonical Version | Rule |
|---|---|---|
| `Microsoft.Extensions.*` (Configuration, Hosting, DI, etc.) | `9.0.x` – `9.4.x` | Never use `10.x` |
| `Microsoft.EntityFrameworkCore.*` | `9.0.x` | Never use `10.x` |
| `Microsoft.AspNetCore.*` | `9.0.x` | Never use `10.x` |
| `Microsoft.Extensions.Caching.Hybrid` | **`9.3.0`** (pinned) | This is the ONLY exception to automatic `9.0.x` — HybridCache reached GA at `9.3.0`. All three references (SharedKernel.Application, SharedKernel.Infrastructure, any module Application) MUST use exactly `9.3.0`. |

**Why this matters**: Using `10.x` in one project while another project transitively pulls `9.x` of the same package causes `NU1605: Detected package downgrade`. This is treated as a build error in this solution (`TreatWarningsAsErrors` is set for analyzers).

**How to verify before adding a package**:
```powershell
# Check what version is already used across the solution
Select-String -Path "**/*.csproj" -Pattern "PackageName" -Recurse
```
Always match the version already in use. If no version exists yet, use the latest `9.x` stable.

### Approved Packages (already in use)
| Package | Pinned Version | Purpose | DO NOT replace with |
|---------|---------------|---------|-------------------|
| MediatR | `14.0.0` | CQRS pipeline | Wolverine, raw DI |
| FluentValidation | `12.1.1` | Input validation | DataAnnotations |
| EF Core (SqlServer) | `9.0.13` | ORM | Dapper (for CQRS queries it's OK to add later) |
| **Microsoft.Extensions.Caching.Hybrid** | **`9.3.0`** | HybridCache — data caching with stampede prevention + tag eviction | IMemoryCache, IDistributedCache directly |
| Microsoft.Extensions.Http.Resilience | `9.4.0` | Polly v8 resilience for HttpClient | Raw Polly setup |
| SixLabors.ImageSharp | `3.1.12` | Image processing | System.Drawing, SkiaSharp |
| FFMpegCore | `5.4.0` | Video/audio metadata | MediaToolkit |
| Azure.AI.Translation.Text | (current) | Translation API | Google Translate SDK |
| Serilog.AspNetCore | `10.0.0` | Structured logging | Microsoft.Extensions.Logging direct |
| Asp.Versioning.Http | `8.1.1` | API versioning | Manual route strings |

### Banned Packages
| Package | Why |
|---------|-----|
| Hangfire / Quartz | ADR-003: We use BackgroundService + Channel\<T\> |
| AutoMapper | We use manual DTO mapping for explicitness and performance |
| MediatR.Extensions.* | Unnecessary — pipeline behaviors are in SharedKernel |
| EntityFramework.Proxies | No lazy loading — see Performance Rule P7 |
| IMemoryCache (standalone) | Replaced by HybridCache. Zero new code should use `IMemoryCache`. |

---
## 🧠 [CRITICAL] Error Learning System (MANDATORY)
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
