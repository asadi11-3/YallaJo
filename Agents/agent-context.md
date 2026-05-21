# YallaJo — Agent Context & Build Guide

> **Last Updated**: 2026-05-21 (Social sprint audit fixes) | **Build State**: Social Domain/Application/Infrastructure/Presentation + YallaJo.Api builds PASS (0 errors)

> **Purpose**: The single source of truth for any AI agent working on YallaJo.
> **Read every section before writing code.** Every section is a rule you must follow.

---

## 📑 Table of Contents

### §0. Quick Start (2-minute read)
- [§0.1 What YallaJo Is](#01-what-yallajo-is)
- [§0.2 Tech Stack](#02-tech-stack)
- [§0.3 The Five Non-Negotiable Rules](#03-the-five-non-negotiable-rules)
- [§0.4 Reference Documents](#04-reference-documents)

### §1. Architecture Foundations
- [§1.1 Modular Monolith + Clean + DDD](#11-modular-monolith--clean--ddd)
- [§1.2 Project Structure](#12-project-structure)
- [§1.3 Dependency Graph (Strict)](#13-dependency-graph-strict)
- [§1.4 Request Lifecycle](#14-request-lifecycle)

### §2. The Hard Rules (non-negotiable)
- [§2.1 Rule — Endpoint Authorization](#21-rule--endpoint-authorization-mandatory)
- [§2.2 Rule — ICurrentUser Usage Policy](#22-rule--icurrentuser-usage-policy-mandatory)
- [§2.3 Rule — Result Pattern](#23-rule--result-pattern-no-business-exceptions)
- [§2.4 Rule — Per-Module Permission Catalog](#24-rule--per-module-permission-catalog)
- [§2.5 Rule — Transaction Boundaries (UoW)](#25-rule--transaction-boundaries-uow)
- [§2.6 Rule — Outbox/Inbox Atomicity](#26-rule--outboxinbox-atomicity)

### §3. CQRS, MediatR, Events
- [§3.1 Commands vs Queries](#31-commands-vs-queries)
- [§3.2 MediatR Pipeline](#32-mediatr-pipeline)
- [§3.3 Domain Events](#33-domain-events-same-module-same-transaction)
- [§3.4 Integration Events (Outbox/Inbox)](#34-integration-events-outboxinbox)

### §4. Authorization Architecture
- [§4.1 Layered Authorization Model](#41-layered-authorization-model)
- [§4.2 How to Add a New Permission](#42-how-to-add-a-new-permission)
- [§4.3 Endpoint Decoration Cookbook](#43-endpoint-decoration-cookbook)
- [§4.4 Ownership Checks in Handlers](#44-ownership-checks-in-handlers)

### §5. Cross-Cutting Patterns
- [§5.1 Caching (HybridCache)](#51-caching-hybridcache)
- [§5.2 Validation (FluentValidation)](#52-validation-fluentvalidation)
- [§5.3 Error Handling](#53-error-handling)
- [§5.4 Concurrency & Data Integrity](#54-concurrency--data-integrity)
- [§5.5 Try/Catch Policy](#55-trycatch-policy)
- [§5.6 Logging & Observability](#56-logging--observability)

### §6. Conventions
- [§6.1 Naming](#61-naming)
- [§6.2 DateTime & Guid](#62-datetime--guid)
- [§6.3 Enums & Strings](#63-enums--strings)
- [§6.4 Git](#64-git)

### §7. Checklists
- [§7.1 New Entity Checklist](#71-new-entity-checklist)
- [§7.2 New Endpoint Checklist](#72-new-endpoint-checklist)
- [§7.3 PR Review Checklist](#73-pr-review-checklist)
- [§7.4 Pre-flight & Completion](#74-pre-flight--completion)

### §8. Known Issues (from audits)
- [§8.1 ICurrentUser Violations](#81-icurrentuser-violations-8-handlers)
- [§8.2 Endpoint Authorization Violations](#82-endpoint-authorization-violations-28-endpoints)

### §9. Gotchas (hard-won lessons)
- [§9.1 Gotchas Registry](#91-gotchas-registry)

### §10. Session Protocol
- [§10.1 Session Start](#101-session-start)
- [§10.2 Session End](#102-session-end)
- [§10.3 Tracking](#103-tracking)

### §11. Module Status (tracking)
- [§11.1 Module Status Overview](#111-module-status-overview)
- [§11.2 Work Log](#112-work-log)
- [§11.3 Next Up](#113-next-up)

---

| File | Contains | When to Read |
|------|----------|-------------|
| `Agents/guide.md` | **HOW to build** — layer anatomy, CQRS patterns, events, repos, UoW, Result pattern, Specs, Value Objects, pipeline behaviors, translation system, code templates, EF config patterns, DI patterns (~2162 lines) | **ALWAYS** — this is the code pattern bible |
| `Agents/YallaJo.md` | **WHAT to build** — all ~196 endpoints across 4 phases, middleware, 18 background services, 3 SignalR hubs, business logic (~1968 lines) | When implementing new features or endpoints |
| `Agents/Endpoints.pdf` | Endpoint definitions (visual) | When implementing endpoints |
| `Agents/YallaJo Business Rules & Edge Cases.pdf` | Business rules & edge cases (63 pages, 24 sections) — state machines, validation, booking, payments, reviews, etc. | **ALWAYS when implementing any module** — this is the business logic bible |
| `Agents/error-log.md` | Mistakes previous agents made — with root cause and prevention rules | **ALWAYS** — read before writing any code |

### §0.1 What YallaJo Is

**YallaJo** is a tourism + booking platform for Jordan. Architecture: **.NET 9 modular monolith** with **Clean Architecture per module**, **CQRS via MediatR**, **Domain-Driven Design**, **Outbox/Inbox for cross-module events**. Single deployable process, 14 modules, ~196 planned endpoints across 4 phases.

### §0.2 Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 (SDK 10.0.200-preview) |
| Web | ASP.NET Core Minimal APIs |
| Mediator | MediatR 14.0.0 |
| ORM | EF Core 9.0.13 (SQL Server) |
| Validation | FluentValidation 12.1.1 |
| Caching | `Microsoft.Extensions.Caching.Hybrid` **9.3.0** (pinned) |
| Resilience | `Microsoft.Extensions.Http.Resilience` 9.4.0 (Polly v8) |
| Auth | JWT Bearer + permission-based policies (`IPermissionCatalog`) |
| Logging | Serilog 10.0.0 (console + rolling file) |
| Tracing | OpenTelemetry 1.15 (OTLP exporter) |
| Versioning | `Asp.Versioning.Http` 8.1.1 (URL segment `/api/v1/`) |
| Images | SixLabors.ImageSharp 3.1.12 |
| Video | FFMpegCore 5.4.0 |
| Translation | Azure Translator API |

### §0.3 The Five Non-Negotiable Rules

These rules are the **foundation of every code change**. Violate any of them and your PR is rejected.

#### 🛑 Rule 1 — Every endpoint must have explicit authorization
Every `MapGet` / `MapPost` / `MapPut` / `MapDelete` MUST be decorated with ONE of:
- `.WithMetadata(new MustHavePermissionAttribute({Module}Features.X, AppAction.Y))` — for protected endpoints
- `.AllowAnonymous()` — for public endpoints (login, register, public reads)

**Never** use `.RequireAuthorization()` without a permission (that's just "authenticated, but anyone").
**Never** use `.RequireAuthorization("Permission.X.Y")` string-based policies — use the attribute.

Detail: [§2.1](#21-rule--endpoint-authorization-mandatory) · Violations list: [§8.2](#82-endpoint-authorization-violations-28-endpoints)

#### 🛑 Rule 2 — `ICurrentUser` is ONLY for self/ownership comparisons
Inject `ICurrentUser` in a handler **only when** you compare `ICurrentUser.UserId` against the resource's owner/creator/target to gate access (IDOR prevention, self-edit checks).

**Never** inject `ICurrentUser` just to stamp a field or to check `IsAuthenticated` — that's what `MustHavePermission` is for. Authorization belongs on the endpoint, not inside the handler.

Detail: [§2.2](#22-rule--icurrentuser-usage-policy-mandatory) · Violations list: [§8.1](#81-icurrentuser-violations-8-handlers)

#### 🛑 Rule 3 — Commands return `Result<T>`. Never throw for business failures.
Programming errors (null where not expected, violated invariants) → `ArgumentException` / `InvalidOperationException` → 500.
Business errors (not found, conflict, forbidden, validation) → `Result<T>.Failure(Error.X, Outcome.Y)` → mapped HTTP status.
Endpoints: `result.ToApiResult()`. **Never** try/catch in endpoints or command handlers for business errors.

Detail: [§5.3](#53-error-handling)

#### 🛑 Rule 4 — Every module owns its permission catalog
Each module publishes `{Module}Features` + `{Module}PermissionCatalog : IPermissionCatalog` in its **Contracts** project.
Security's `PermissionSeeder` auto-discovers all registered catalogs. **Never** add another module's features to your module's catalog.

Detail: [§4](#4-authorization-architecture) · Background: `authorization-refactor-plan.md`

#### 🛑 Rule 5 — Domain event handlers never call `SaveChangesAsync`
`UnitOfWork.SaveChangesAsync()` dispatches domain events **before** SaveChanges. Your handler's changes piggyback on the aggregate's single commit. Calling Save in a handler breaks atomicity and can double-save.

Detail: [§2.5](#25-rule--transaction-boundaries-uow) · [§3.3](#33-domain-events-same-module-same-transaction)

### §0.4 Reference Documents

| File | Purpose | When to Read |
|---|---|---|
| `Agents/agent-context.md` (this file) | Rules, architecture, checklists, gotchas | **Always, every session** |
| `Agents/guide.md` | Deep code patterns — entity anatomy, CQRS templates, EF configs, pipeline internals | When implementing a new feature |
| `Agents/YallaJo.md` | Product spec — 196 endpoints, middleware, background services, business logic | When implementing a specific endpoint |
| `Agents/YallaJo Business Rules & Edge Cases.pdf` | Business rule bible — 63 pages, 24 sections, state machines, validation, edge cases | **Before implementing any module's business logic** |
| `Agents/Endpoints.pdf` | Visual endpoint tier breakdown (Wave 1–6) | When planning an implementation order |
| `Agents/error-log.md` | Every past mistake with root cause + prevention rule | **Always, session start** |
| `Agents/authorization-refactor-plan.md` | The full authorization subsystem architecture + migration history | When touching authorization code |
| `Agents/endpoint-authorization-audit.md` | Current endpoint authorization coverage (127 endpoints audited) | When adding/reviewing endpoints |
| `Agents/decisions/ADR-001..004.md` | Architecture decisions (Modular Monolith, CQRS, No Hangfire, Result Pattern) | When an architectural choice seems wrong |
| `Agents/patterns/` | Copy-paste code for caching, error handling, Polly | When you need a specific pattern |
| `Agents/templates/` | Scaffold templates for all CQRS artifacts | When creating a new entity |

---

## §1. Architecture Foundations

### §1.1 Modular Monolith + Clean + DDD

YallaJo combines three complementary architectures:

**Modular Monolith** — 14 bounded-context modules inside one deployable process. Each module owns its own schema, DbContext, migrations, outbox, inbox. Modules communicate **only** via integration events (no cross-module queries, no direct DbContext access across modules).

**Clean Architecture per module** — Domain has zero dependencies. Application depends on Domain only. Infrastructure implements interfaces defined in Domain + Application. Presentation depends on Application only (via MediatR).

**Domain-Driven Design** — Aggregates marked with `IAggregateRoot`. Private setters; state changes via business methods. Factory methods raise domain events. Repositories are per-aggregate. Unit of Work dispatches events before commit.

### §1.2 Project Structure

Every module has **exactly 5 projects**:

```
{Module}.Domain/           ← Pure business logic. Zero external deps.
{Module}.Application/      ← Commands, Queries, Handlers, Validators
{Module}.Infrastructure/   ← EF Core, repositories, DbContext, event handlers
{Module}.Presentation/     ← Minimal API endpoints
{Module}.Contracts/        ← Integration events + permission catalog (public surface)
```

Plus 3 SharedKernel projects:

```
YallaJo.SharedKernel.Domain/          ← Base entities, value objects, interfaces
YallaJo.SharedKernel.Application/     ← CQRS interfaces, pipeline behaviors, IPermissionCatalog
YallaJo.SharedKernel.Infrastructure/  ← UoW, outbox/inbox, domain event dispatcher
YallaJo.SharedKernel.Presentation/    ← MustHavePermissionAttribute, ResultExtensions, auth runtime
```

And the host:

```
YallaJo.Api/   ← Program.cs, middleware, JWT config, module wiring
```

### §1.3 Dependency Graph (Strict)

```
                    ┌─────────────────────────┐
                    │  SharedKernel.Domain    │
                    └─────────────────────────┘
                              ▲
              ┌───────────────┼───────────────┐
              │               │               │
   ┌──────────────────┐ ┌─────────────┐ ┌────────────────────┐
   │ SharedKernel     │ │ SharedKernel│ │ SharedKernel       │
   │ .Application     │ │ .Infra      │ │ .Presentation      │
   └──────────────────┘ └─────────────┘ └────────────────────┘
              ▲               ▲               ▲
   ┌──────────┴───────────────┴──────┐        │
   │ {Module}.Contracts              │◄───────┤
   │ (IntegrationEvents, Features,   │        │
   │  PermissionCatalog)             │        │
   └─────────────────────────────────┘        │
              ▲                                │
              │                                │
   ┌──────────┴───┐  ┌────────────┐  ┌────────┴────────┐
   │ {Module}.    │  │ {Module}.  │  │ {Module}.        │
   │ Domain       │◄─│ Application│  │ Presentation     │
   └──────────────┘  └────────────┘  └──────────────────┘
                            ▲                ▲
                            └───── YallaJo.Api (host)
```

**Rules**:
- Domain depends on NOTHING except `SharedKernel.Domain`
- Application depends on Domain + Contracts + `SharedKernel.Application`
- Infrastructure depends on Application + Domain + `SharedKernel.Infrastructure`
- Presentation depends on Application + `SharedKernel.Presentation`
- Contracts depends on `SharedKernel.Domain` + `SharedKernel.Application` (for `IPermissionCatalog`)
- **Infrastructure NEVER leaks into Application or Domain**
- **Cross-module references only via Contracts or integration events**

### §1.4 Request Lifecycle

```
HTTP POST /api/v1/content-core/tags
  │
  ├─ ASP.NET Core Pipeline
  │    ├─ Serilog Request Logging
  │    ├─ CorrelationId Middleware
  │    ├─ Authentication (JWT)
  │    ├─ Authorization (PermissionPolicyProvider synthesizes "Permission.Tag.Create")
  │    │    └─ PermissionAuthorizationHandler checks User.HasClaim("Permission", "Permission.Tag.Create")
  │    │         ├─ has claim → continue
  │    │         └─ missing   → 403 Forbidden
  │    ├─ RequestLocalization (Accept-Language → CultureInfo)
  │    └─ RateLimiter
  │
  ├─ Endpoint Handler (Presentation)
  │    └─ ISender.Send(CreateTagCommand)
  │
  ├─ MediatR Pipeline (SharedKernel.Application)
  │    ├─ ValidationBehavior      → FluentValidation
  │    ├─ LoggingBehavior         → stopwatch, structured log
  │    ├─ PerformanceBehavior     → warn on >500ms handlers
  │    ├─ QueryCachingBehavior    → HybridCache.GetOrCreateAsync (queries only)
  │    └─ Command/Query Handler
  │         ├─ Loads aggregate via repository
  │         ├─ Calls business method (e.g., tag.Update(...))
  │         ├─ Aggregate raises domain event (added to DomainEvents collection)
  │         ├─ unitOfWork.SaveChangesAsync()
  │         │    ├─ UoW collects domain events from all IAggregateRoot entries
  │         │    ├─ Clears events from aggregates
  │         │    ├─ Dispatches events via MediatR
  │         │    │    └─ Domain event handlers may ADD outbox messages to DbContext
  │         │    └─ context.SaveChangesAsync() (ONE atomic commit: aggregate + outbox)
  │         ├─ cache.RemoveByTagAsync("tag:{id}", "tags") — AFTER save
  │         └─ returns Result<T>
  │
  ├─ Endpoint maps Result<T> → HTTP via result.ToApiResult()
  │
  └─ Response (RFC 7807 ProblemDetails for failures)

(Async, out-of-band)
CompositeOutboxProcessor (BackgroundService, every 10s):
  ├─ For each module DbContext: pick up unprocessed OutboxMessages
  ├─ Lock message (LockedUntil = now + 5min) — prevents double-processing
  ├─ Deserialize JSON → IntegrationEventNotification<T>
  ├─ For each registered INotificationHandler<IntegrationEventNotification<T>>:
  │    └─ Invoke individually (not via mediator.Publish — failure isolation)
  │         └─ Handler checks inbox idempotency, does work, marks inbox, SaveChanges
  ├─ If ALL handlers succeed: mark OutboxMessage.ProcessedOnUtc
  └─ If any fails: RetryCount++, retry next cycle (max 10 retries → dead-letter)
```

### §1.5 Critical Gotchas (22 hard-earned rules)

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

## §2. The Hard Rules (non-negotiable)

### §2.1 Rule — Endpoint Authorization (MANDATORY)

**Every Minimal API endpoint MUST have ONE of the following two decorators. No exceptions.**

#### ✅ Option A — Permission-guarded (default for authenticated endpoints)

```csharp
tags.MapPost("/", async (CreateTagRequest req, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(req.ToCommand(), ct);
    return result.ToApiResult();
})
.WithName("CreateTag")
.WithSummary("Create a new tag")
.WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Create))
.Produces<Guid>(StatusCodes.Status201Created)
.ProducesValidationProblem();
```

#### ✅ Option B — Anonymous (for truly public endpoints)

```csharp
auth.MapPost("/login", async (LoginRequest req, ISender sender, CancellationToken ct) =>
{
    ...
})
.WithName("Login")
.AllowAnonymous();
```

#### ❌ Forbidden patterns

```csharp
// ❌ Authenticated but no permission — any logged-in user can hit it
endpoint.RequireAuthorization();

// ❌ String-based policy — bypasses the MustHavePermission attribute
endpoint.RequireAuthorization("Permission.Tag.Create");

// ❌ Role-based in code — hardcoded role list
if (!user.IsInRole("Admin")) return Forbid();

// ❌ No decoration at all — implicit auth is invisible
endpoint.MapPost("/tags", ...);
```

#### Why this rule exists

- **Explicit**: every endpoint declares intent. Grep finds every permission usage.
- **Auditable**: `Security.Infrastructure/Seeding/PermissionSeeder` aggregates all permissions at startup. If the attribute isn't there, it's not in the DB.
- **Refactor-safe**: renaming `AppAction.Create` → `AppAction.Add` updates all call sites via compile errors. Strings can't.
- **Consistent**: one path for authorization → one security model → one audit log.

### §2.2 Rule — `ICurrentUser` Usage Policy (MANDATORY)

**Inject `ICurrentUser` in a handler ONLY when comparing the current user's ID against a resource's owner/creator/target field.**

#### ✅ Valid uses (ownership / self-check / IDOR prevention)

```csharp
// ✅ VALID — updates ONLY the current user's profile (self-edit)
public async Task<Result<UpdateProfileResult>> Handle(UpdateProfileCommand cmd, CancellationToken ct)
{
    if (currentUser.UserId is null)
        return Result.Unauthorized("Authentication required.");

    var profile = await repo.FirstOrDefaultAsync(p => p.UserId == currentUser.UserId.Value, ct);
    // ... update profile ...
}

// ✅ VALID — verifies the booking belongs to the current user before revoking (IDOR prevention)
if (session.UserId != currentUser.UserId.Value)
    return Result.Forbidden("You cannot revoke another user's session.");

// ✅ VALID — stamps the creator during aggregate construction
var business = BusinessEntity.Create(
    name: cmd.Name,
    ownerId: currentUser.UserId.Value);  // ownerId is a domain concept; stamping is correct
```

#### ❌ Invalid uses (gratuitous injection)

```csharp
// ❌ INVALID — handler injects ICurrentUser but only checks auth, no ownership
public sealed class SuspendBusinessCommandHandler(ICurrentUser currentUser, ...)
{
    public async Task<Result> Handle(SuspendBusinessCommand cmd, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)  // ❌ "authentication check" — that's what MustHavePermission is for
            return Result.Unauthorized();

        var business = await repo.GetByIdAsync(cmd.Id, ct);
        business.Suspend(cmd.Reason);  // no ownership check
        // ...
    }
}

// ✅ FIX — remove ICurrentUser entirely; guard via permission on the endpoint
[endpoint] .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Suspend))

public sealed class SuspendBusinessCommandHandler(IBusinessRepository repo, ...)
{
    public async Task<Result> Handle(SuspendBusinessCommand cmd, CancellationToken ct)
    {
        var business = await repo.GetByIdAsync(cmd.Id, ct);
        business.Suspend(cmd.Reason);
        // ...
    }
}
```

#### Decision table

| Question | If "Yes" | If "No" |
|---|---|---|
| Does the handler compare `currentUser.UserId` against a resource field (owner, creator, target)? | ✅ Inject `ICurrentUser` | ❌ Do NOT inject |
| Does the handler just check `IsAuthenticated`? | ❌ Remove `ICurrentUser` — use `MustHavePermission` on endpoint | |
| Does the handler stamp `currentUser.UserId` into a new aggregate's `CreatedByUserId` / `OwnerId`? | ✅ Valid stamping — OK to inject | |
| Does the handler need the user ID for a permission check? | ❌ Move the check to `MustHavePermission` on the endpoint | |

**Why this rule exists**:
- Authorization belongs at the perimeter (endpoint), not deep in the use case.
- Handlers that inject `ICurrentUser` gratuitously are untestable — every unit test must mock ICurrentUser even when no ownership is checked.
- Permission changes then require finding every handler that reads `currentUser.Permissions` — an n² audit. With `MustHavePermission` it's a single-file edit.

Violations currently in the codebase: [§8.1](#81-icurrentuser-violations-8-handlers).

### §2.3 Rule — Result Pattern (no business exceptions)

Two categories of error:

| Category | Source | How to handle |
|---|---|---|
| **Programming error** | Bug — null where not expected, broken invariant | `throw new ArgumentException(...)` → caught by global handler → 500. **Never catch.** |
| **Business error** | User did something invalid, entity in wrong state | `return Result<T>.Failure(Error.X, Outcome.Y)` → endpoint maps to HTTP |
| **Infrastructure error** | External API failed, DB down | Caught in Infrastructure layer only (see [§5.5](#55-trycatch-policy)) |

**Every command handler** returns `Result<T>` — success AND failure.
**Every query handler** returns `Result<T>` — success AND failure.
**Endpoints**: `result.ToApiResult()` — never try/catch.

Factory methods:

```csharp
Result<T>.Success(value)                               // 200
Result<T>.Created(value)                               // 201
Result<T>.Failure(Error.NotFound("X"), Outcome.NotFound)   // 404
Result<T>.Failure(Error.Conflict("X"), Outcome.Conflict)   // 409
Result<T>.Failure(Error.Forbidden("X"), Outcome.Forbidden) // 403
Result<T>.Failure(Error.Invalid("X"), Outcome.Invalid)     // 400
```

Full patterns: `Agents/patterns/error-handling-patterns.md` · ADR-004 in `Agents/decisions/`.

### §2.4 Rule — Per-Module Permission Catalog

Every module owns its permission surface. Security is just another module — it only **aggregates** catalogs via DI discovery.

```
{Module}.Contracts/Authorization/
├── {Module}Features.cs           ← feature string constants (e.g., Tag, Place, Booking)
└── {Module}PermissionCatalog.cs  ← implements IPermissionCatalog
```

Registration in `{Module}.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddSingleton<IPermissionCatalog, {Module}PermissionCatalog>();
```

`PermissionSeeder` (Security.Infrastructure) auto-discovers all `IPermissionCatalog` implementations at app startup and seeds the database. **Adding a new module never modifies another module.**

Full architecture: `Agents/authorization-refactor-plan.md`.

### §2.5 Rule — Transaction Boundaries (UoW)

The Unit of Work is the ONLY place `SaveChangesAsync` is called. Never call it from:
- Domain event handlers
- Integration event handlers that don't have their own transaction
- Validators
- Mappers

#### Why

`UnitOfWork.SaveChangesAsync` does this atomically:
1. Collect domain events from all `IAggregateRoot` entries being saved
2. Clear events from aggregates (so they're not re-dispatched on next save)
3. Dispatch events via MediatR (**before** SaveChanges)
4. Domain event handlers may add outbox rows to the DbContext
5. `context.SaveChangesAsync()` — ONE commit for aggregate + outbox

If a domain event handler calls SaveChanges itself:
- First SaveChanges commits aggregate changes before outbox is written → partial commit
- Or it triggers re-dispatching events → infinite loop / duplicate outbox rows
- Or it creates a second transaction → loses atomicity guarantee

#### The aggregate-root rule

`UnitOfWork` only collects events from `IAggregateRoot` entries. If you add a domain event to a `BaseEntity` (non-aggregate), your handler **never runs** and no error is logged. This is a silent bug.

**Check**: Can this entity be reached as a root of an aggregate boundary? If yes, mark `IAggregateRoot`. If no, raise events on the parent aggregate instead.

### §2.6 Rule — Outbox/Inbox Atomicity

**Outbox side (publisher)**:
- Domain event handler writes `OutboxMessage` to the DbContext — **never calls SaveChanges**
- UoW commits aggregate + outbox row in one transaction
- `CompositeOutboxProcessor` picks up the row, deserializes, invokes integration event handlers out-of-band

**Inbox side (consumer)**:
- Every integration event handler MUST check inbox first: `if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct)) return;`
- Do the work (create profile, send email, etc.)
- Mark inbox: `inboxStore.MarkAsProcessed(notification.MessageId);`
- Call `unitOfWork.SaveChangesAsync(ct);` — ONE transaction: business changes + inbox row

**Why handlers are invoked individually** (not via `mediator.Publish`):
`CompositeOutboxProcessor` uses reflection to invoke each `INotificationHandler<IntegrationEventNotification<T>>` individually. If one handler fails, others still run. The message is only marked Processed when ALL handlers succeed. On retry, successful handlers short-circuit via inbox idempotency check. This gives per-handler failure isolation — a single buggy module can't block the entire integration event.

**Never**:
- Do external side effects (email, SMS, HTTP call) **before** marking inbox processed — if it fails, you retry forever. Order: check inbox → do work → mark inbox → SaveChanges.
- Actually, do external effects **last** and mark inbox only after they succeed. If email fails, throw — outbox retries.
- **Exception**: if the external effect itself has idempotency (e.g., `SendGrid Message-Id`), you can mark inbox first. Default assumption: external effects are NOT idempotent.

---

## §3. CQRS, MediatR, Events

### §3.1 Commands vs Queries

| Aspect | Command | Query |
|---|---|---|
| Purpose | Change state | Return data |
| Interface | `ICommand` / `ICommand<TResponse>` | `IQuery<TResponse>` |
| Handler | `ICommandHandler<TCmd>` / `ICommandHandler<TCmd, TResp>` | `IQueryHandler<TQuery, TResp>` |
| Return | `Result` / `Result<T>` | `Result<T>` |
| Cacheable | ❌ Never | ✅ Must implement `ICacheableQuery` |
| Side effects | Yes — domain events, outbox rows, cache eviction | None — read-only |
| Validation | FluentValidation required | FluentValidation optional (rare) |
| HybridCache | `RemoveByTagAsync` after save | `GetOrCreateAsync` via `QueryCachingBehavior` |

All interfaces defined in `YallaJo.SharedKernel.Application/Abstractions/Messaging/`.

### §3.2 MediatR Pipeline

Order (SharedKernel.Application):
1. `ValidationBehavior` — FluentValidation, throws `ValidationException` → 400
2. `LoggingBehavior` — stopwatch, structured log with handler name + duration
3. `PerformanceBehavior` — warn on >500ms handlers
4. `QueryCachingBehavior` — queries implementing `ICacheableQuery` only
5. Actual handler

Registration: `Program.cs` → `AddSharedKernelInfrastructure()`.

### §3.3 Domain Events (same module, same transaction)

**Purpose**: side effects within the same module, same DB transaction — update translation cache, create audit log, write outbox row for cross-module notification.

**Rules**:
- Raised inside aggregate methods (factory / state-change methods)
- Added via `AddDomainEvent(new XDomainEvent(...))` (inherited from `BaseEntity`)
- Dispatched by UoW BEFORE `SaveChangesAsync`
- Handlers: `INotificationHandler<DomainEventNotification<TEvent>>`
- Handlers may mutate the DbContext (add outbox rows, update read models) — UoW commits everything atomically
- **Handlers NEVER call `SaveChangesAsync`** (Rule 5)
- **Never cross module boundaries** — domain events are in-module only

**Naming**: `{Entity}{PastTenseVerb}DomainEvent` — e.g., `TagCreatedDomainEvent`, `BookingCancelledDomainEvent`.

### §3.4 Integration Events (Outbox/Inbox)

**Purpose**: cross-module asynchronous communication.

**Publishing flow**:
1. Domain event handler serializes integration event, writes `OutboxMessage` to DbContext
2. UoW commits (aggregate + outbox atomic)
3. `CompositeOutboxProcessor` polls every 10s across all module DbContexts
4. Picks up unprocessed message, locks it, deserializes
5. Invokes each `INotificationHandler<IntegrationEventNotification<T>>` individually
6. Marks processed only if ALL handlers succeed

**Consumer flow** (in the receiving module):
```csharp
public async Task Handle(IntegrationEventNotification<UserCreatedIntegrationEvent> notification, CancellationToken ct)
{
    // 1. Idempotency check FIRST
    if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        return;

    // 2. Business logic
    var profile = Profile.Create(evt.UserId, evt.FirstName, evt.LastName);
    await profileRepository.AddAsync(profile, ct);

    // 3. Mark inbox LAST (after all side effects succeed)
    inboxStore.MarkAsProcessed(notification.MessageId);

    // 4. ONE atomic save: business change + inbox row
    await unitOfWork.SaveChangesAsync(ct);
}
```

**Naming**: `{Entity}{PastTenseVerb}IntegrationEvent` — e.g., `UserCreatedIntegrationEvent`, `BookingConfirmedIntegrationEvent`.

**Placement**: `{Module}.Contracts/IntegrationEvents/` (public surface consumed by other modules).

---

## §4. Authorization Architecture

### §4.1 Layered Authorization Model

```
Caller's JWT with "Permission" claims
              │
              ▼
┌─────────────────────────────────────────────────────┐
│ ASP.NET Core Authorization Middleware               │
│   ├─ Sees [MustHavePermission(X.Feature, X.Action)] │
│   ├─ Policy name: "Permission.{Feature}.{Action}"   │
│   ├─ PermissionPolicyProvider synthesizes policy    │
│   │   with PermissionRequirement("Permission.X.Y")  │
│   └─ PermissionAuthorizationHandler checks claim    │
│       ├─ has → 200 OK → endpoint runs               │
│       └─ missing → 403 Forbidden                    │
└─────────────────────────────────────────────────────┘
              │ (if authorized)
              ▼
┌─────────────────────────────────────────────────────┐
│ Handler (no ICurrentUser unless ownership check)    │
│   └─ Business logic runs                            │
└─────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────┐
│ (Optional, for ownership)                           │
│   Compare ICurrentUser.UserId against resource's    │
│   owner/creator/target → Forbidden if mismatch     │
└─────────────────────────────────────────────────────┘
```

### §4.2 How to Add a New Permission

**Scenario**: You're adding a `Booking` module and need `Create`/`Approve`/`Cancel` permissions.

**Step 1**: Declare features in `Booking.Contracts/Authorization/BookingFeatures.cs`
```csharp
namespace Booking.Contracts.Authorization;

public static class BookingFeatures
{
    public const string Booking     = nameof(Booking);
    public const string Refund      = nameof(Refund);
    public const string Availability = nameof(Availability);
}
```

**Step 2**: Declare catalog in `Booking.Contracts/Authorization/BookingPermissionCatalog.cs`
```csharp
using YallaJo.SharedKernel.Application.Authorization;

namespace Booking.Contracts.Authorization;

public sealed class BookingPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Booking";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(BookingFeatures.Booking, AppAction.Read,    PermissionGroup.BookingOperations, "View bookings"),
        new(BookingFeatures.Booking, AppAction.Create,  PermissionGroup.BookingOperations, "Create a booking"),
        new(BookingFeatures.Booking, AppAction.Approve, PermissionGroup.BookingOperations, "Approve a pending booking"),
        new(BookingFeatures.Booking, AppAction.Reject,  PermissionGroup.BookingOperations, "Reject a booking"),
        new(BookingFeatures.Booking, AppAction.Delete,  PermissionGroup.BookingOperations, "Cancel a booking"),
        // ...
    ];
}
```

**Step 3**: Register catalog in `Booking.Infrastructure/DependencyInjection.cs`
```csharp
services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();
```

**Step 4**: Update `Security.Infrastructure/Seeding/RolePermissionMapping.cs` if the new permission needs to go to specific roles beyond defaults. Most permissions flow automatically via the existing role rules (Owner, SuperAdmin, Admin).

**Step 5**: Restart app → `PermissionSeeder` logs `"Seeding N permissions from M modules: Security, ContentCore, ContentPlaces, Booking, ..."`. Database now has the new `Permission.Booking.Create` claim available for role assignment.

**Step 6**: Use in endpoint:
```csharp
booking.MapPost("/", async (...) => ...)
    .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Create))
    .WithName("CreateBooking");
```

**Never**:
- Add `Booking.Features` constants to `Security.Contracts` or another module's Features class
- Hardcode permission strings in endpoints (`.RequireAuthorization("Permission.Booking.Create")`)
- Add role names to handlers (`if (user.IsInRole("Admin"))`)

### §4.3 Endpoint Decoration Cookbook

```csharp
// ── Public read (no auth) ───────────────────────────────────────────
group.MapGet("/", async (ISender sender, CancellationToken ct) => ...)
    .AllowAnonymous()
    .WithName("ListTags")
    .Produces<IReadOnlyList<TagDto>>();

// ── Protected read (authenticated + specific permission) ────────────
group.MapGet("/admin", async (ISender sender, CancellationToken ct) => ...)
    .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Read))
    .WithName("ListTagsAdmin")
    .Produces<IReadOnlyList<TagDto>>();

// ── Protected create ────────────────────────────────────────────────
group.MapPost("/", async (CreateTagRequest req, ISender sender, CancellationToken ct) => ...)
    .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Create))
    .WithName("CreateTag")
    .Produces<Guid>(StatusCodes.Status201Created)
    .ProducesValidationProblem();

// ── Protected update ────────────────────────────────────────────────
group.MapPut("/{id:guid}", async (Guid id, UpdateTagRequest req, ISender sender, CancellationToken ct) => ...)
    .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Update))
    .WithName("UpdateTag")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesValidationProblem()
    .ProducesProblem(StatusCodes.Status404NotFound);

// ── Protected delete ────────────────────────────────────────────────
group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) => ...)
    .WithMetadata(new MustHavePermissionAttribute(ContentCoreFeatures.Tag, AppAction.Delete))
    .WithName("DeleteTag")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound);
```

### §4.4 Ownership Checks in Handlers

When `MustHavePermission` is not enough (user can access the feature but only for their OWN resources), inject `ICurrentUser` and add an ownership check.

```csharp
public sealed class UpdateBusinessCommandHandler(
    IBusinessRepository repo,
    IContentPlacesUnitOfWork uow,
    HybridCache cache,
    ICurrentUser currentUser,   // ✅ injected ONLY for the ownership check below
    ILogger<UpdateBusinessCommandHandler> logger)
    : ICommandHandler<UpdateBusinessCommand>
{
    public async Task<Result> Handle(UpdateBusinessCommand cmd, CancellationToken ct)
    {
        var business = await repo.GetByIdAsync(cmd.Id, ct);
        if (business is null)
            return Result.Failure(Error.NotFound("Business.NotFound"), Outcome.NotFound);

        // ── Ownership check (IDOR prevention) ──
        // Endpoint already verified user has Business.Update permission.
        // Now verify this specific business belongs to them (or they're admin).
        var isAdmin = currentUser.IsInRole("Admin");
        if (!isAdmin && business.OwnerId != currentUser.UserId!.Value)
            return Result.Failure(Error.Forbidden("Business.NotOwner"), Outcome.Forbidden);

        business.Update(cmd.Name, cmd.Description);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{cmd.Id}", ct);
        return Result.Success();
    }
}
```

**Decision**: if the ownership check can be expressed via a permission (`Business.UpdateAny` vs `Business.UpdateSelf`), prefer the permission. Use `ICurrentUser` only when ownership is a per-row check rather than a feature-level gate.

---

## §5. Cross-Cutting Patterns

### §5.1 Caching (HybridCache)

**Current state**: All ContentCore queries + most commands use HybridCache. All new modules MUST follow the same pattern.

#### Required for every query
- Implements `ICacheableQuery` (`CacheKey`, `CacheDuration`, `Tags`)
- Tags must be **specific**: `attachment:{id}` not `attachments`
- Include `userId` / `isAdmin` in the cache key for auth-varied queries

#### Required for every command handler
- Inject `HybridCache`
- `await cache.RemoveByTagAsync(tag, ct)` **after** a successful `SaveChangesAsync`

#### Policy
| Entity type | Absolute TTL | Local L1 TTL | Tag pattern |
|---|---|---|---|
| Reference data (categories, tags, languages) | 30–60 min | 15 min | `{entity}s` |
| Entity detail | 5 min | 2 min | `{entity}`, `{entity}:{id}` |
| Paginated list | 5 min | 2 min | `{entity}:list` |
| Translations | 120 min | 30 min | `translations:{entityType}:{entityId}` |
| Negative (not-found) cache | 30–60 sec | 30 sec | same as positive |

Full reference: `Agents/patterns/caching-patterns.md`.

#### MUST NEVER be cached
- Booking state / slot availability (real-time)
- Payment status / transaction state
- User auth tokens / sessions
- User permissions / roles
- Any entity with `LockedUntil` or time-sensitive state

### §5.2 Validation (FluentValidation)

Three layers:
1. **FluentValidation** (Application) — input shape: required fields, string lengths, format, enum, ranges
2. **Domain guards** — invariants in aggregate `Create()`/`Update()`/state methods
3. **Database constraints** — unique indexes, FKs, check constraints

Validator rules:
```csharp
RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
RuleFor(x => x.Slug).Matches(@"^[a-z0-9\-]+$");
RuleFor(x => x.Email).EmailAddress();
RuleFor(x => x.Status).IsInEnum();
RuleFor(x => x.Price).GreaterThan(0);
RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
RuleFor(x => x.CategoryId).NotEqual(Guid.Empty);
```

**Uniqueness checks** (require DB call) → **in the handler**, not the validator. Return `Result.Failure(Error.Conflict("Slug.Taken"), Outcome.Conflict)`.

### §5.3 Error Handling

Error categories (from [§2.3](#23-rule--result-pattern-no-business-exceptions)):

```csharp
// Programming error (bug)
throw new ArgumentException(nameof(name), "Name cannot be empty");  // caught globally → 500

// Business error
return Result<Guid>.Failure(Error.NotFound("Tag.NotFound"), Outcome.NotFound);  // → 404

// Infrastructure error (wrap in Infra layer only)
try { await azureTranslator.TranslateAsync(...); }
catch (HttpRequestException ex)
{
    logger.LogError(ex, "Azure Translator failed");
    return Result<string>.Failure(Error.ServiceUnavailable("Translation.Unavailable"), Outcome.ServerError);
}
```

Error code convention: `{Entity}.{Reason}` — PascalCase, no spaces, stable (never rename after clients use it).

| Code | HTTP | When |
|---|---|---|
| `{Entity}.NotFound` | 404 | Entity doesn't exist |
| `{Entity}.AlreadyExists` | 409 | Duplicate |
| `{Entity}.InvalidState` | 400 | Action not allowed in current state |
| `{Entity}.ConcurrencyConflict` | 409 | RowVersion conflict |
| `{Entity}.Unauthorized` | 401 | Missing/invalid auth |
| `{Entity}.Forbidden` | 403 | Authenticated but no permission or ownership |
| `{Entity}.QuotaExceeded` | 429 | Rate limit or quota hit |
| `Service.Unavailable` | 503 | External API down |
| `Service.Timeout` | 504 | External API timed out |

Full reference: `Agents/patterns/error-handling-patterns.md`.

### §5.4 Concurrency & Data Integrity

| Rule | Detail |
|---|---|
| `AuditableEntity.RowVersion` + `.IsRowVersion()` in EF config | Optimistic concurrency — prevents silent overwrites |
| Soft delete via `SoftDelete()` | User-facing entities. Junction tables use hard delete. |
| Unique constraints at DB level | Code-level checks have race conditions. Always add unique index. |
| `ExistsAsync()` in handler before `Create` | Optimization on top of DB constraint; return `Result.Conflict`. |
| `DbUpdateConcurrencyException` | Catch in Infrastructure only. Convert to `Result.Failure("X.ConcurrencyConflict")`. |

### §5.5 Try/Catch Policy

**Whitelist** (only these catches are allowed):

1. **Infrastructure layer, external service call** — `HttpRequestException`, `TaskCanceledException` → `Result.Failure`
2. **Infrastructure layer, optimistic concurrency** — `DbUpdateConcurrencyException` → `Result.Failure("X.ConcurrencyConflict")`
3. **Background services, per-item loop** — generic `Exception` to prevent worker crash — MUST log before continuing
4. **Application layer, cancellation differentiation** — `OperationCanceledException when ct.IsCancellationRequested` to distinguish cancel vs timeout

**Forbidden**:
- Try/catch in endpoints
- Try/catch in command/query handlers for general `Exception`
- Empty catch blocks
- Catching and swallowing without logging
- Catching for "defensive programming" — let programming errors propagate to the global handler

Full reference: `Agents/patterns/error-handling-patterns.md` → §Try/Catch Golden Rule.

### §5.6 Logging & Observability

| Layer | Tool |
|---|---|
| Application log (structured) | Serilog → console (dev) + rolling file (prod) + (future) external sink |
| Per-request log | `UseSerilogRequestLogging()` — host, user-agent, client IP, user ID |
| Per-handler log | `LoggingBehavior` — start + duration |
| Traces | OpenTelemetry — ASP.NET Core, EF Core, HttpClient, custom MediatR activity source |
| Metrics | OpenTelemetry — ASP.NET Core + HttpClient |
| Exporter | OTLP (configurable via `OpenTelemetry:OtlpEndpoint`) + Console in dev |
| Correlation | `X-Correlation-Id` header (set in `CorrelationIdMiddleware`) |
| Exception handler | `GlobalExceptionHandler` → RFC 7807 ProblemDetails with correlation ID. Stack trace only in Development. |

**Rules**:
- **Every handler** injects `ILogger<THandler>` (commands AND queries)
- Structured logging: `logger.LogInformation("Created {Entity} {Id}", entity, id)` — never `$"Created {entity}"`
- **Never log** passwords, JWT tokens, credit card numbers, PII, full request bodies with auth data
- **Log at module boundaries** — before/after external API calls, domain events raised, outbox messages written

Health endpoints:
- `/health/live` (liveness — zero checks)
- `/health/ready` (readiness — DB + external services)
- `/health` (all checks, legacy)

---

## §6. Conventions

### §6.1 Naming

| What | Pattern | Example |
|---|---|---|
| Command | `{Verb}{Entity}Command` | `CreatePlaceCommand`, `CancelBookingCommand` |
| Query | `{Verb}{Entity}Query` | `ListToursQuery`, `GetTagByIdQuery` |
| Handler | `{CommandOrQuery}Handler` | `CreatePlaceCommandHandler` |
| Validator | `{CommandOrQuery}Validator` | `CreatePlaceCommandValidator` |
| Domain event | `{Entity}{PastTenseVerb}DomainEvent` | `TagCreatedDomainEvent` |
| Integration event | `{Entity}{PastTenseVerb}IntegrationEvent` | `UserCreatedIntegrationEvent` |
| List DTO | `{Entity}SummaryDto` | `TourSummaryDto` (5–8 key fields) |
| Detail DTO | `{Entity}DetailDto` | `TourDetailDto` (full shape) |
| Repository | `I{Entity}Repository` → `{Entity}Repository` | `IPlaceRepository` → `PlaceRepository` |
| EF Config | `{Entity}Configuration` | `PlaceConfiguration` |
| Migration | `Add{Entity}` / `Update{Entity}{Change}` | `AddPlace`, `UpdateTourAddCapacity` |
| Endpoint group | `{Module}Endpoints.cs` | `ContentPlacesEndpoints.cs` |
| Feature class | `{Module}Features` | `ContentCoreFeatures`, `SecurityFeatures` |
| Permission catalog | `{Module}PermissionCatalog` | `ContentCorePermissionCatalog` |
| Error code | `{Entity}.{Reason}` | `Category.NotFound`, `Tour.SlugConflict` |
| DB schema | lowercase snake_case | `content_core`, `content_places`, `booking` |

### §6.2 DateTime & Guid

- **Always `DateTime.UtcNow`** — never `DateTime.Now`. Jordan is UTC+3; mixing local/UTC corrupts data.
- **Always `Guid.CreateVersion7()`** — never `Guid.NewGuid()`. V7 is time-sortable → better clustered index perf.
- **`DateTimeOffset`** for user-facing timestamps in DTOs — timezone is explicit. Storage stays `DateTime` UTC.

### §6.3 Enums & Strings

**Enums**:
- Store as `int` via `.HasConversion<int>()`
- Define in `{Module}.Domain/Enums/`
- Shared (cross-module) → `{Module}.Contracts`
- Always include a safe default at `0`

**Strings**:
- Slugs, codes, status: `.IsUnicode(false).HasMaxLength(200)`
- Names, titles: `.HasMaxLength(500)` (Unicode default — for Arabic)
- Bodies, bios: `.HasMaxLength(4000)` or `.HasColumnType("nvarchar(max)")`
- **Every string column MUST have `HasMaxLength()`** — unbounded `nvarchar(max)` is wasteful and unindexable

### §6.4 Git

| Rule | Detail |
|---|---|
| **NEVER push to remote** | You commit locally only. User handles all pushes. Hard block, no exceptions. |
| **Never commit without being asked** | User will say when. |
| Branch naming | `feature/{module}/{entity}` · `fix/{module}/{bug}` · `refactor/{scope}` |
| Commit message | `{type}({module}): {description}` — e.g., `feat(Booking): add reservation state machine` |
| Types | `feat` · `fix` · `refactor` · `docs` · `chore` · `test` |
| One logical change per commit | Don't mix unrelated work |
| Never commit secrets | Check `.gitignore` first |
| Never force push | If history rewrite needed, ask user |

---

## §7. Checklists

## §7.0 Tracking — Module Status Overview
| Module | Status | Notes |
|--------|--------|-------|
| Auth | ✅ Fixed | Phase 4 — every admin lifecycle handler (`AdminSuspendUser`, `AdminReactivateUser`, `AdminArchiveUser`, `AdminResetPassword`, `AdminReassignAccount`) now calls `IAdminAuditWriter.RecordAsync` on the success path with action verbs from `AuditActions`. Reassignment emits exactly ONE row per Option C; `profileScrubbed: true|false` lives inside the row's metadata JSON. Failures (authn/authz/lifecycle/profile-scrub) write nothing. Phase 3D + 3C + 3B verbs unchanged. Validation: `Auth.Application` + `Auth.Presentation` build 0 errors; `tests/Auth.Tests.Unit` 258/258 passing. |
| Security | ✅ Fixed | Phase 4 — `AuditLog` extended with three nullable columns (`ActorUserId`, `Reason`, `Metadata`) + factory `AuditLog.CreateAdmin` + composite index `IX_AuditLogs_ActorUserId_OccurredAt`. Migration `AddAdminAuditColumns` applied. New cross-module contract `IAdminAuditWriter` + `AdminAuditEntry` + `AuditActions` constants in `Security.Contracts`. Internal `AdminAuditWriter` registered in DI. `GetAuditLogsQuery` + endpoint extended with optional `actorUserId`/`action`/`from`/`to` filters; `AuditLogDto` exposes Phase 4 fields. Phase 3C reassignment primitives + 3B lifecycle verbs unchanged. Validation: `Security.Infrastructure` build 0 errors; `tests/Security.Tests.Unit` 119/119 passing. |
| Accounts | ✅ Fixed | Phase 4 — `IProfileReassignmentService.ResetForReassignmentAsync` return type widened from `Result` to `Result<ProfileReassignmentOutcome>` carrying a `Scrubbed` flag. The flag flows back into the `ADMIN_REASSIGN_ACCOUNT` audit row's `Metadata.profileScrubbed`, keeping the audit timeline truthful about whether a profile row was actually mutated. Phase 3D scrub semantics unchanged. Validation: `Accounts.Application` build 0 errors; `tests/Accounts.Tests.Unit` 16/16 passing. |
| Accounts | ✅ Fixed | Profile self-service flow hardened: UpdateProfile flat-binding fix retained, avatar upload contract fixed (relative `/uploads/...` now accepted by `UpdateAvatarCommandValidator`), and API avatar endpoint now deletes freshly uploaded files when profile update fails (prevents orphan files). Added `tests/Accounts.Tests.Unit` validator regressions. |
| ContentCore | ✅ Fixed | Full audit + all 11 bugs fixed 2026-04-17, plus WS follow-up remediation: upload magic-byte signature validation (WS6), verified post-commit attachment deletion flow/no-op event handler consistency (WS4), and new migration `20260417115007_UpdateContentCoreUnicodeTranslationCacheAndStatus` for Unicode + CategoryTranslation.Status + TranslationCache hash index. Build: 0 errors. See `Agents/ContentCore-fixes-required.md`. |
| ContentPlaces | 🟡 In Progress | Major fix pass complete 2026-04-23. Place module fully wired (events, cache, filters including `categoryId`+`hasActiveTours` backed by real entity fields). Business module: 5 integration events + 5 domain event handlers. ServiceItem: full rewrite (IDOR, outbox, correct entity, ICacheableQuery, route split). Geo-search: Haversine single-compute + bounding-box pre-filter + composite index. TourCount+CategoryId on Place entity with migrations. Remaining: Fadwa tasks (staff/amenity auth holes, accessibility guard, integration events, ICacheableQuery on 3 queries, DTO fixes). See `Agents/ContentPlaces-remaining-fix-plan.md`. |
| ContentTours | 🟡 Partial | Tour entity has business methods (Publish/Archive/Suspend/AssignToPlace/RemoveFromPlace/Delete) + domain event + handler that publishes `PlaceTourCountUpdatedIntegrationEvent` to outbox. Full CQRS + endpoints not started. |
| ContentBlogs | ⬜ Not started | Entities exist, endpoints empty |
| ContentSeo | 🟡 Partial | 4 handlers consuming ContentPlaces events: PlaceCreated→SeoMetadata+SitemapEntry(active); PlaceUpdated→Touch; PlaceDeleted→Deactivate; BusinessCreated→SeoMetadata+SitemapEntry(inactive). SeoMetadata.Create + SitemapEntry.Create factories added. ContentPlaces.Contracts ref added. Endpoints empty. |
| Booking | ⬜ Not started | Entities exist, endpoints empty |
| Finance | ⬜ Not started | Entities exist, endpoints empty |
| Messaging | 🟡 Partial | InboxMessages table + IMessagingInboxStore + IMessagingUnitOfWork added. Notification.Create factory + NotificationType.Business(7). 4 handlers: BusinessApproved(InApp+Email opt-in), BusinessRejected(InApp+Email unconditional), BusinessSuspended(InApp+Email Critical unconditional), BusinessReinstated(InApp+Email opt-in). ContentPlaces.Contracts ref added. Email/Push dispatch not started. Endpoints empty. |
| Social | 🟡 Partial | Sprint audit fixes complete: 7 inbox handlers added for existing ContentPlaces/ContentTours contracts, snapshot tables/repos wired, orphaned favorites cleanup uses deleted snapshots, and 2 background services refactored to PeriodicTimer. Endpoints still empty. |
| Tracking | ⬜ Not started | Entities exist, endpoints empty |
| Analytics | 🟡 Partial | Sprint cleanup implemented: interaction POST returns 202, HybridCache user dedupe, decayed popularity scoring with rating snapshot bonus, popularity recalculation hosted service, Analytics inbox handlers for available contracts, dashboard query data wiring, migration `AnalyticsPopularityRatingSnapshot`. Remaining: Accounts provider status contract and ContentTours published contract do not exist; provider tour titles require ContentTours read model contract. |

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
| 57 | Analytics sprint cleanup | 🟡 | 🤖 Agent | Fixed A-R1..A-R4: `/api/v1/interactions` now returns 202, RecordInteraction uses 5-minute HybridCache user dedupe, scoring weights match spec with 30-day half-life decay and persisted rating snapshot bonus, `PopularityScoreCalculationService` registered with PeriodicTimer and outbox events, 15 available Analytics integration handlers added, dashboard query handlers wired to snapshots/interactions/popularity data, migration `AnalyticsPopularityRatingSnapshot` generated. Skipped missing contracts: `AccountsProviderStatusChangedIntegrationEvent`; exact `ContentToursPublishedIntegrationEvent` missing so `TourCreatedIntegrationEvent` initializes Tour scores instead. |
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
| 39 | Auth/Security Phase 3B — Admin lifecycle ops | ✅ | 🤖 Agent | Implemented admin lifecycle command flow for users: added `AdminSuspendUser`, `AdminReactivateUser`, and `AdminArchiveUser` handlers/validators in Auth, with endpoints at `/api/v1/auth/admin/users/{userId}/suspend|reactivate|archive`. Added new revocation reasons `AccountSuspended` and `AccountArchived` in `ISessionRevocationService`. Extended `ISecurityService` + `SecurityService` with hierarchy-enforced lifecycle transitions and state gating (`Suspend`: Active/PendingPasswordReset + idempotent Suspended; `Reactivate`: Suspended only; `Archive`: non-Archived only), including Security cache-tag invalidation. Validation: `dotnet build Security.Infrastructure`, `dotnet build Auth.Application`, `dotnet build Auth.Presentation` (all 0 errors), `dotnet test tests/Auth.Tests.Unit/Auth.Tests.Unit.csproj` (240/240 passed). |
| 40 | Auth/Security Phase 3C — Admin account reassignment | ✅ | 🤖 Agent | Implemented admin-initiated account reassignment per the approved plan. **Security domain**: added `User.ReassignToPendingActivation(newEmail, replacementPasswordHash)` which chains `Email.ChangeAddress` (retarget + verification reset) + `User.ResetPassword` (placeholder hash) + `TransitionTo(PendingActivation)`; added `Email.ChangeAddress` and `Email.ResetVerification`; extended `IsTransitionAllowed` with `Active|Suspended|PendingPasswordReset → PendingActivation` edges reserved for the reassignment verb and documented the expanded transition matrix. **Security contracts/service**: added `ISecurityService.ReassignUserByAdminAsync(targetId, actorId, newEmail)` returning `ReassignmentCompleted` (old/new email + lifecycle snapshot); enforces hierarchy/self check via `RoleHierarchyService`, lifecycle eligibility gate (rejects `Provisioned`/`PendingActivation`/`Archived`), email-uniqueness pre-check, and uses a `REASSIGNED:<guid>` fail-closed password placeholder. **Auth**: added `AdminReassignAccountCommand` + validator + result + handler at `Auth.Application/Commands/AdminReassignAccount/`. Handler runs Security mutation + Auth teardown in a single `ITransactionalExecutor` scope: session+refresh-token revocation with new `SessionRevocationReason.AccountReassigned`, supersede all non-terminal `ActivationToken` + `PasswordResetToken` rows, deactivate every active `ExternalProvider` link, issue a fresh `ActivationToken` for the new email and attach `ActivationTokenIssuedEvent` for outbox dispatch, single Auth `SaveChangesAsync`. Added endpoint `POST /api/v1/auth/admin/users/{userId}/reassign` with `UpdateAny` permission. **Tests**: added `tests/Security.Tests.Unit/UserReassignmentTests.cs` (11 tests covering eligible/ineligible source states, email normalization, verification reset, password invalidation, event emission, and `Email.ChangeAddress`/`ResetVerification` in isolation) and `tests/Auth.Tests.Unit/AdminReassignAccountCommandHandlerTests.cs` (10 tests covering unauthenticated actor, Security-failure propagation for NotFound/Forbidden/Conflict/email-in-use, happy-path activation-token issuance, session revocation reason, supersede sweeps, external-provider deactivation, and plain-token-never-logged invariant). **Profile handling deferred**: Accounts profile reassignment/scrub is out of scope for Phase 3C (no contract exists); documented as a known limitation. Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test YallaJo.sln --no-build` all green — Security.Tests.Unit 108/108, Auth.Tests.Unit 250/250, Accounts.Tests.Unit 3/3, Web.Tests.Unit 31/31, SharedKernel.Tests.Unit 5/5, ContentCore.Tests.Unit 11/11. |
| 48 | Bug-fix — Local login crashed for external-only / reassigned users | ✅ | 🤖 Agent | **Root cause**: `Microsoft.AspNetCore.Identity.PasswordHasher<>.VerifyHashedPassword` calls `Convert.FromBase64String` on the stored hash *before* any format-version check. For users whose `PasswordHash` is the intentional non-Base64 placeholder set by external auto-create (`EXTERNAL-ONLY:<guid>` from `Security.Application.Services.UserRegistrationService.RegisterExternalAsync`) or by Phase 3C admin reassignment (`REASSIGNED:<guid>` from `Security.Infrastructure.Services.SecurityService.ReassignUserByAdminAsync`), that decode threw `System.FormatException` — bubbling up as an unhandled 500 instead of the intended 401 "Invalid email or password.". Reproduces when a Google-created user (or a freshly-reassigned account) types email + any password into the local login form. **Fix** (single-boundary, in `Security.Infrastructure/Services/PasswordHasher.cs`): added a null/empty/whitespace guard returning `false`, and wrapped the framework `VerifyHashedPassword` call in `try { … } catch (FormatException) { return false; }`. Real Identity v2/v3 hashes flow through unchanged; the change converts a former throw into the documented "Failed" semantics the wrapper always promised. **NOT changed** per scope: `LoginCommandHandler` (already returns the right shape once `Verify` returns `false`), `SecurityService.VerifyCredentialsAsync` (already returns null = invalid credentials), `UserRegistrationService` placeholder strategy (correct and intentional), Google `ExternalLoginCommandHandler` flow (untouched — never goes through the local hasher), `IPasswordHasher` signature (unchanged). Per-scope generic message preserved (`"Invalid email or password."`) — anti-enumeration convention respected. **Tests added** (`tests/Security.Tests.Unit/PasswordHasherTests.cs`, 11 cases via `[Theory]` rows): EXTERNAL-ONLY placeholder → false (no throw); REASSIGNED placeholder → false (no throw); 4 arbitrary non-Base64 inputs → false; null/empty/whitespace → false; round-trip Hash→Verify with correct password → true; valid hash + wrong password → false. The test fixture instantiates `PasswordHasher` (internal sealed) via reflection (same pattern as `UserRegistrationServiceExternalTests`); a new project reference from `tests/Security.Tests.Unit/Security.Tests.Unit.csproj` to `Security.Infrastructure` was added so the assembly is loadable at runtime. **Behavior**: BEFORE — external-only / reassigned user posting to `/login` → 500 Internal Server Error (unhandled `FormatException`). AFTER — same request → 401 "Invalid email or password.". Google login, normal-user login, registration, and reassignment flows are unaffected. Validation: `dotnet build Security.Infrastructure/Security.Infrastructure.csproj` 0 errors; `dotnet test tests/Security.Tests.Unit/Security.Tests.Unit.csproj` 130/130 passing (was 119; +11); `dotnet test tests/Auth.Tests.Unit/Auth.Tests.Unit.csproj --no-build` 258/258 unchanged; full-solution `dotnet test YallaJo.sln --no-build` all green — Security 130/130, Auth 258/258, Accounts 16/16, Web 85/85, SharedKernel 5/5, ContentCore 11/11 (505 total). |
| 50 | Security audit paging refactor — use base SelectPaginatedAsync | ✅ | 🤖 Agent | Removed the custom audit paging method and switched the query path to repository projection APIs. `IAuditLogRepository` no longer declares `GetPagedAsync(...)`; `AuditLogRepository` no longer implements it and now relies on inherited `EfEntityRepository` read methods. `GetAuditLogsQueryHandler` now builds the same optional filter set (`userId`, `actorUserId`, exact `action`, inclusive `from/to`) as a single expression, keeps `OccurredAt` descending ordering, and projects directly to `AuditLogDto` through `SelectPaginatedAsync(...)`. Response shape and semantics remain unchanged (same pagination envelope and fields). Validation: `dotnet build Security.Domain/Security.Domain.csproj -clp:ErrorsOnly` (0 errors), `dotnet build Security.Infrastructure/Security.Infrastructure.csproj --no-dependencies -clp:ErrorsOnly` (0 errors), while full Security/Application + solution/test runs remain blocked by unrelated pre-existing duplicate type files (`CS0101`). |
| 51 | Security.Contracts abstractions split by type | 🟡 | 🤖 Agent | Refactored `Security.Contracts/Abstractions` so each interface stays in its original file and each public request/result/DTO/enum now lives in its own peer file in the same folder. Removed redundant placeholder internal stubs and kept namespaces on moved types as `Security.Contracts.Abstractions`. `dotnet build Security.Contracts/Security.Contracts.csproj` succeeded (warnings only). `dotnet build YallaJo.sln` currently fails: many consumers still import `Security.Contracts.Abstractions.SecurityService|UserRegistrationService|AdminAuditWriter` sub-namespaces, but contract types now live under `Security.Contracts.Abstractions` only; follow-up needed to align consumer imports or provide namespace-compat shims. `dotnet test YallaJo.sln --no-build` ran partially (Web/ContentCore/SharedKernel passing; several test DLLs missing due failed prior build). |
| 52 | ContentTours Phase 3 cache-language key isolation fix | ✅ | 🤖 Agent | Completed Task-1 Phase 3 cleanup for language-sensitive tour queries. `GetTourByIdQueryHandler` and `GetTourBySlugQueryHandler` no longer depend on `IRequestContext`; both now resolve translations from `request.AcceptLanguage` only, aligning runtime projection language with `ICacheableQuery.CacheKey` language segment. Cache key helper already normalizes language (`default`, first token, lowercase) and query records now carry `AcceptLanguage` for list/detail/slug keys. Validation: `dotnet build YallaJo.sln` succeeded (warnings only, no errors). |
| 53 | ContentTours Phase 6 — Tour core endpoints + permission wiring | ✅ | 🤖 Agent | Added the 11 Task-1 Tour endpoints under `/api/v1/tours` via new `TourCoreEndpoints` and wired it from `ContentToursEndpoints`. Public list/detail/slug endpoints now explicitly `AllowAnonymous()` and forward HTTP `Accept-Language` header into `ListToursQuery` / `GetTourByIdQuery` / `GetTourBySlugQuery`. Added protected lifecycle endpoints using `MustHavePermissionAttribute` only (no string policies): create/update/delete/submit/approve/reject/suspend/reinstate. Added endpoint request models (`CreateTourRequest`, `UpdateTourRequest`, `RejectTourRequest`, `SuspendTourRequest`, `RowVersionRequest`) with row-version bodies for state-machine commands. Authorization wiring: added `AppAction.Submit` in SharedKernel and added `Tour.Submit` permission descriptor in `ContentToursPermissionCatalog`. Validation: `dotnet build ContentTours.Presentation/ContentTours.Presentation.csproj` and `dotnet build YallaJo.sln` both succeeded with 0 errors. |
| 56 | ContentTours Task-3 SearchTours compound relevance score — Tech-Lead-approved scope cut | ✅ | 🤖 Agent | Documentation-only resolution of audit P1 #4. Wrote `Agents/decisions/ADR-005-search-relevance-score-deferred.md` capturing the decision to defer the PDF Task-3 §B2 compound relevance formula (name×3 + description×1 + rating×0.3 + log popularity + recency + tie-breakers) to a dedicated future search-quality task. Current `SearchToursQueryHandler` keeps its v1 fallback for `Sort == Relevance` (`OrderByDescending(BookingCount).ThenByDescending(CreatedAt)`) — explicitly accepted by PDF R-3 mitigation. **No production code changed**: filters, facets, pagination, tokenization, "no q + no filters → 400" guard, and the 4 explicit non-default sorts remain functionally complete per PDF B1/B3/B4. The PDF B2 relevance formula is **NOT** marked as implemented — it is marked as deferred with a clear scope-of-future-task list (8 components incl. translation-aware scoring coordination with P1 #5). ADR includes review triggers (search-quality KPI, relevance complaints, recommendations engine integration, scheduled v2 sprint) so the deferral cannot rot. Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test tests/ContentTours.Tests.Unit` 50/50 passed. |
| 55 | ContentTours Task-1 closing — PlaceExistence cross-module contract refactor | ✅ | 🤖 Agent | **Post-audit follow-up that resolves the previous "raw-SQL `PlaceExistsService` is schema-coupled" concern from the final audit.** Replaced the parameterized-but-schema-coupled raw-SQL service in ContentTours.Infrastructure with a proper cross-module contract owned by ContentPlaces: **new** `ContentPlaces.Contracts/Places/IPlaceExistenceService.cs` + `PlaceExistenceStatus.cs` (NotChecked/Active/NotFound/Deleted); **new** `ContentPlaces.Infrastructure/Services/PlaceExistenceService.cs` (EF-backed via `ContentPlacesDbContext`, `IgnoreQueryFilters().AsNoTracking().Select(p => new { p.IsDeleted }).FirstOrDefaultAsync(ct)` so it can distinguish Deleted from NotFound past the soft-delete query filter); **DI** registered in `ContentPlaces.Infrastructure.DependencyInjection`. **Removed** the old raw-SQL `ContentTours.Infrastructure/Services/PlaceExistsService.cs` and the local `ContentTours.Application/Interfaces/IPlaceExistsService.cs` + `PlaceExistenceStatus` enum (zero remaining references in solution). **Updated** `CreateTourCommandHandler`, `UpdateTourCommandHandler`, `SubmitTourCommandHandler` and the three test files in `tests/ContentTours.Tests.Unit/` to consume `ContentPlaces.Contracts.Places.IPlaceExistenceService`; behavior preserved exactly (`null → NotChecked`, `NotFound → Tour.PlaceNotFound/422`, `Deleted → Tour.PlaceDeleted/422`, `Active → continue`). **Layering preserved**: `ContentTours.Application → ContentPlaces.Contracts` (added `<ProjectReference>` in `ContentTours.Application.csproj`); `ContentPlaces.Infrastructure → ContentPlaces.Contracts`; `ContentTours.Infrastructure` does **not** reference `ContentPlaces.Infrastructure` (verified by `grep ProjectReference ContentTours.Infrastructure/ContentTours.Infrastructure.csproj`). Future renames of `[content_places].[Places]` or `[IsDeleted]` are now absorbed inside ContentPlaces.Infrastructure mapping only — consumers don't move. Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test tests/ContentTours.Tests.Unit` 50/50 passed; `dotnet test tests/SharedKernel.Tests.Unit` 91/91 passed. |
| 54 | ContentTours Phase 7 — Tour Task-1 unit tests | ✅ | 🤖 Agent | Created `tests/ContentTours.Tests.Unit` (xunit 2.9.3 + NSubstitute 5.3.0 + FluentAssertions 7.0.0 + EF Core InMemory 9.0.15) following `ContentPlaces.Tests.Unit` style; registered in `YallaJo.sln`. **45 tests added, 45 passing.** Coverage: domain state-machine transitions (Submit/Approve/Reject/Update-resets-Rejected/Suspend/Reinstate/SoftDelete), command-handler unit tests for Create (auth, slug conflict + soft-delete reservation, place not-found / deleted, success), Update (non-owner 403, rejected→draft auto-reset on update, RowVersion mismatch 409), Delete (BookingCount block, future-schedules block, success + outbox staging), Submit (aggregated 422 umbrella with all 6 child errors, valid draft → Pending), Admin lifecycle (Approve/Reject/Suspend/Reinstate happy paths, blank reason 400 fallback, RowVersion concurrency conflict via DbUpdateConcurrencyException). Cache-key tests pin Phase-3 language partitioning. Phase-0-deferred sanity test exercises real `TourCreatedDomainEventHandler` against `ContentToursDbContext` (InMemory) and asserts 1 outbox row of type `content-tours.tour.created.v1` with deserialisable JSON; translation-failure does NOT prevent outbox staging. **No production code changes were needed.** Endpoint smoke project skipped: solution has no WebApplicationFactory pattern; static endpoint structure already verified in Phase 6 (route counts, MustHavePermission attributes, RowVersion bindings, AllowAnonymous markers, Accept-Language pass-through). Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test tests/ContentTours.Tests.Unit` 45/45 passed. **Pre-existing solution-level test failures unrelated to Task 1**: `SharedKernel.Tests.Unit.IntegrationEventTypeRegistryTests.AllRegisteredTypes_Contains_All26Events` (brittle hardcoded count = 26 vs. 37 registered since prior phases — Task-1 did not regress it; out of scope per Phase 7 "do not refactor old modules"); `ContentPlaces.Tests.Unit` 8 unrelated failures from prior staff/amenity/accessibility work; `Auth.Tests.Unit.AuthRetentionWorkerTests` 5 unrelated failures using an unregistered `ProbeIntegrationEvent`. |
| 49 | Security repository abstraction alignment — audit log repo | ✅ | 🤖 Agent | Updated `Security.Domain/Repositories/IAuditLogRepository.cs` to inherit `IReadRepository<AuditLog, Guid>` + `IWriteRepository<AuditLog, Guid>` so the interface matches shared repository abstractions. Updated `Security.Infrastructure/Repositories/AuditLogRepository.cs` to inherit `EfEntityRepository<AuditLog, Guid>` while preserving the existing filtered paging API `GetPagedAsync(...)` used by audit timeline queries. Validation: `dotnet build Security.Domain/Security.Domain.csproj -clp:ErrorsOnly` (0 errors), `dotnet build Security.Infrastructure/Security.Infrastructure.csproj --no-dependencies -clp:ErrorsOnly` (0 errors). Full solution build is currently blocked by unrelated pre-existing duplicate type errors in Accounts/Security Application projects. |
| 47 | Web Phase 5E — Final IAM admin UI cleanup & polish | ✅ | 🤖 Agent | Concluded the Phase 5 frontend slice for the IAM lifecycle. **Pure view cleanup; no backend, controller, facade, API-client, VM, or test code touched.** **Modified** (1 file): `YallaJo.Web/Areas/Admin/Modules/Security/Features/Users/Views/Details.cshtml` — three surgical changes: (1) **Removed legacy Activate/Deactivate UI block** (the only remaining `confirm()` prompts in admin-lifecycle UI); the backend `UsersController.Activate`/`Deactivate` actions and the `/api/v1/security/users/{id}/activate|deactivate` endpoints **remain callable for non-UI clients per scope** — only the Razor view stops surfacing them. A scoped comment in the gap explains the deferral so the block is not reintroduced by accident. (2) **Replaced the inline Status `<span class="badge…">` with the `_LifecycleBadge` partial** introduced in Phase 5D, so binary status rendering has a single source of truth across Users list + Details. The `LifecycleState` parameter passes `null` until the backend exposes granular states. (3) **Upgraded TempData success/error alerts** to `alert alert-success/danger alert-dismissible fade show` with `role="alert"` and a Bootstrap `btn-close` so screen readers announce them and admins can dismiss long error messages. Added one `@using` for `Users.ViewModels`. **NOT modified** per scope: Users list page, Audit Timeline page (alerts there will be polished in a future scoped slice if requested), backend, API contracts, all controllers/facades/API clients/VMs, all five lifecycle modal partials (already correct from 5C), `_LifecycleBadge` helper/VM/partial (correct from 5D), legacy `Activate`/`Deactivate` controller actions and API endpoints (kept callable for non-UI clients). **Tests**: none added — pure view cleanup, no helper or VM changes. All 85 existing Web tests remain green. **Final consolidated deferred items** (post-Phase 5): granular lifecycle palette beyond Active/Inactive (needs `UserDto.LifecycleState` extension); `Created`/`Updated` columns (not on `UserDto`); server-side search/sort/bulk actions (`ListUsersQuery` is page/pageSize only); typed modal input not preserved across redirects (TempData-only error path); per-user audit timeline section on Details (deferred/optional, possibly Phase 6A); legacy `Activate`/`Deactivate` controller actions + API endpoints (UI-removed, kept callable; dedicated removal phase if/when product confirms no external callers); live char counter on Reason fields; action verb dropdown on audit-timeline filter; cross-module `ProfileReassignedIntegrationEvent` for downstream analytics (Phase 3D limitation); MSDTC posture when 3 DbContexts enlist (operational). Validation: `dotnet build YallaJo.Web/YallaJo.Web.csproj` 0 errors; `dotnet test tests/Web.Tests.Unit/Web.Tests.Unit.csproj --no-build` 85/85 passing; full-solution `dotnet test YallaJo.sln --no-build` all green — Security 119/119, Auth 258/258, Accounts 16/16, Web 85/85, SharedKernel 5/5, ContentCore 11/11. |
| 46 | Web Phase 5D — Users list polish | ✅ | 🤖 Agent | Polished `/admin/users` using only data already on the page. **No backend / API / DTO changes.** **New** (`Areas/Admin/Modules/Security/Features/Users/`): `Helpers/LifecycleBadge.cs` (`internal static`, `GetCssClass` + `GetLabel`, today short-circuits to the binary `IsActive` mapping, accepts forward-compat `lifecycleState` parameter that is ignored until the backend exposes granular states); `ViewModels/LifecycleBadgeVm.cs` (record `(bool IsActive, string? LifecycleState = null)`); `Views/Partials/_LifecycleBadge.cshtml` (single source for the Active/Inactive pill — green `bg-success` or gray `bg-secondary` with ARIA label). **Index.cshtml redesigned**: header strip with title + total-count chip + page indicator; page-local search input (`<input type="search">` + ~12-line inline JS that toggles row visibility by case-insensitive substring match against `data-filter-text` = Email + Roles, clearly labeled "Filter rows on this page" so admins know it does NOT page through the server); table density bumped (`table-sm table-hover align-middle`); Email cell now shows email + muted-small monospaced GUID for copy/paste into audit URLs; Roles rendered as `<span class="badge bg-light text-dark border">` chips instead of comma-joined plaintext; Status cell delegates to the new `_LifecycleBadge` partial; Actions cell is a Bootstrap `btn-group` with **Manage** (existing) + new **Audit** quick-link → `/admin/audit-logs?userId={id}` gated by `<permission require="@WebPermission.System.Read">`; empty-state row when `Model.Users.Count == 0`. **Tests added** (`tests/Web.Tests.Unit/`): `LifecycleBadgeTests` (4 explicit + 8-row `[Theory]` + 1 determinism = 13 cases) — pins the binary mapping, verifies the helper currently ignores `lifecycleState` (forward-compat sanity), pins determinism; `UsersMapperTests` (3 cases) — round-trips Id/Email/IsActive/Roles, preserves empty roles as empty-not-null, flows `IsActive=false`. **NOT touched** per scope: backend; `UserItemResponse`/`UserRowVm`/`UsersMapper` field set; `UsersController`/`UsersFacade`/`UsersApiClient`; Details page; legacy Activate/Deactivate; audit timeline page; modal partials; lifecycle controller. **Deferred items** (no backend support today): granular lifecycle palette beyond Active/Inactive (helper accepts the field; partial wires through; needs `UserDto.LifecycleState` extension to light up); `Created`/`Updated` columns (not on `UserDto`); server-side search/sort (`ListUsersQuery` accepts only `page`/`pageSize`); bulk actions; sortable columns; inline lifecycle action buttons (kept on Details per scope). Validation: `dotnet build YallaJo.Web/YallaJo.Web.csproj` 0 errors; `dotnet test tests/Web.Tests.Unit/Web.Tests.Unit.csproj` 85/85 passing; full-solution `dotnet test YallaJo.sln --no-build` all green — Security 119/119, Auth 258/258, Accounts 16/16, Web 85/85 (was 69; +16), SharedKernel 5/5, ContentCore 11/11. |
| 45 | Web Phase 5C — Lifecycle Bootstrap modals | ✅ | 🤖 Agent | Replaced browser `confirm()` prompts on `/admin/users/details/{id}` with production-quality Bootstrap 5 modals for the five admin lifecycle verbs. **Backend, API endpoints, `LifecycleApiClient`, and `LifecycleFacade` are unchanged.** **New partials** (`Areas/Admin/Modules/Security/Features/Users/Views/Partials/`): `_SuspendModal.cshtml`, `_ReactivateModal.cshtml`, `_ArchiveModal.cshtml`, `_AdminResetPasswordModal.cshtml`, `_ReassignModal.cshtml`. Each modal renders the existing form posting to `LifecycleController` with `@Html.AntiForgeryToken()`; cancel button dismisses; destructive modals (Archive, Reassign) use `data-bs-backdrop="static" data-bs-keyboard="false"` so accidental backdrop/Esc cannot dismiss mid-typing. ARIA: `role="dialog"`, `aria-labelledby`, `aria-describedby` on every modal. **VM additions**: new `AdminArchiveVm` with `[Required]+[RegularExpression("^ARCHIVE$")]` `ConfirmText`; `AdminReassignAccountVm` gained `IUnderstand` (`[Range(typeof(bool),"true","true")]`) so the modal's required confirmation checkbox is server-authoritative — bypassing the JS gate still produces a 4xx-class server-side rejection. **Controller change**: `LifecycleController.Archive(Guid userId, AdminArchiveVm vm, ct)` — now validates `ConfirmText` before calling the facade; same TempData-error pattern as the other VM-bound actions. Suspend / Reactivate / ResetPassword / Reassign signatures are unchanged. **Details.cshtml**: replaced the five inline confirm() forms in the "Admin actions" card with five `data-bs-toggle="modal"` trigger buttons plus the five partial renders (passing `Model.UserId` as model and `Model.Email` via `ViewDataDictionary["TargetEmail"]` so each modal title personalizes the action). Legacy Activate/Deactivate forms (lines 26-47) untouched per scope. **Inline JS**: ~30 lines added in `@section Scripts` for: typed-ARCHIVE enable/disable on `#archive-form`, reassign checkbox+email enable/disable on `#reassign-form`, and disable-submit-on-submit on every `.admin-action-modal form` to block double-fire during the redirect. No third-party deps; Bootstrap is already loaded by `_Layout.cshtml`. **Tests added** (`tests/Web.Tests.Unit/`): `AdminReassignAccountVmValidationTests` (6 cases: valid happy path, IUnderstand=false rejected, missing email, malformed email, email >320 chars, reason >500 chars); `AdminArchiveVmValidationTests` (10 cases via `[Theory]`: ARCHIVE accepted; null/empty rejected; archive/Archive/ARCHIV/ARCHIVED/leading-or-trailing-space/DELETE rejected). Existing `LifecycleApiClientTests` (6) untouched and green. **NOT touched** per scope: backend, `LifecycleApiClient`, `LifecycleFacade`, legacy Activate/Deactivate, Users list, granular lifecycle badge, per-user audit timeline section on Details (deferred). **Known limitations**: typed input is not preserved across redirects (TempData-only error path — accepted trade-off; modal reopen + retype on validation failure); legacy Activate/Deactivate keeps `confirm()` (Phase 5E cleanup); no live char counter on Reason fields (browser `maxlength` truncates, server `[StringLength]` enforces); no client-side hierarchy preview — backend 403/409 still drives copy. Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test YallaJo.sln --no-build` all green — Security 119/119, Auth 258/258, Accounts 16/16, Web 69/69 (was 53; +6 reassign-VM, +10 archive-VM via `[Theory]` rows), SharedKernel 5/5, ContentCore 11/11. |
| 44 | Web Phase 5B — Admin lifecycle action wiring | ✅ | 🤖 Agent | Wired the five admin lifecycle verbs into the User Details page so admins can trigger them from UI without any backend change. **New components** (`YallaJo.Web/Areas/Admin/Modules/Security/Features/Users/Lifecycle/`): `LifecycleApiClient` (typed wrappers for `PATCH …/suspend|reactivate|archive`, `POST …/reset-password`, `POST …/reassign`); `LifecycleFacade` (single shared error mapper — 401 → ForceSignOut, 403 → "Not allowed.", 404 → "User not found.", 409 → API conflict message verbatim, 400/422 → ValidationErrors propagated, 429 → "Too many requests.", other → API message or fallback); `LifecycleController` (5 actions, all `[ValidateAntiForgeryToken]`, all gated by `User.UpdateAny`, all redirect back to `Users/Details/{id}` with TempData success/error); `Requests/AdminResetPasswordRequest` + `Requests/AdminReassignAccountRequest` (wire DTOs); `ViewModels/AdminResetPasswordVm` (Reason ≤500) + `ViewModels/AdminReassignAccountVm` (NewEmail required+email+≤320, Reason ≤500). DI registrations added in `Program.cs`. **Details view updated**: legacy Activate/Deactivate kept untouched per Phase 5B scope (deferred to Phase 5C); a new "Admin actions" card added below them with five forms — three plain `confirm()`-guarded buttons (Suspend/Reactivate/Archive) plus two inline forms (Reset password with optional reason; Reassign with required new email + optional reason). Each form uses `onsubmit="return confirm(...)"` with verb-specific copy (Suspend: signs-out warning; Archive: irreversible warning; Reassign: full credential-teardown summary). No modals (deferred to Phase 5C). **Tests added** (`tests/Web.Tests.Unit/LifecycleApiClientTests.cs`): 6 cases verifying HTTP method + URL + body shape for every endpoint — Suspend/Reactivate/Archive use PATCH with no body; ResetPassword uses POST with `{ reason }` (null reason omitted from JSON per `WhenWritingNull` policy); Reassign uses POST with `{ newEmail, reason }`. Each test uses a `CapturingHandler` injected into `ApiClient` so URL drift would fail loudly. **NOT touched** per scope: legacy `Activate`/`Deactivate` controller actions and forms; modals; backend; Users list (Phase 5D); lifecycle badge granularity. **Known limitations**: `confirm()` uses generic browser dialogs (modals in Phase 5C); reset-password reason input is not preserved on validation error (modal will preserve typed values); reassign form does not include the explicit "I understand" checkbox yet (deferred to modal in Phase 5C); no client-side hierarchy preview — backend 403/409 messages drive the UX. Validation: `dotnet build YallaJo.Web/YallaJo.Web.csproj` 0 errors; `dotnet test tests/Web.Tests.Unit/Web.Tests.Unit.csproj` 53/53 passing; full-solution `dotnet test YallaJo.sln --no-build` all green — Security 119/119, Auth 258/258, Accounts 16/16, Web 53/53, SharedKernel 5/5, ContentCore 11/11. |
| 43 | Web Phase 5A — Audit timeline UI plumbing | ✅ | 🤖 Agent | Surfaced Phase 4 audit fields in the Admin Web UI without any backend change. **Web responses/VMs widened**: `AuditLogItemResponse` and `AuditLogRowVm` gained `ActorUserId`, `ResourceType`, `ResourceId`, `Reason`, `Metadata`. `AuditLogListVm` gained `FilterActorUserId`, `FilterAction`, `FilterFrom`, `FilterTo` so filters survive page navigation. **API client**: `AuditLogsApiClient` accepts `actorUserId` / `action` / `from` / `to`; introduced `internal static BuildUrl(...)` (with `InternalsVisibleTo("Web.Tests.Unit")`) so URL composition is unit-testable. **Facade + controller**: pass-through filter forwarding; controller defaults pageSize=50; legacy single-filter callers behave identically (`page` / `userId` only). **Audit Index view rebuilt**: filter bar (Subject UserId, Actor UserId, Action, From, To with `datetime-local` inputs); columns Occurred (UTC) · Actor · Action · Target · Reason · IP · Details; Actor + Target cells link to `/admin/users/details/{id}`. **Metadata rendering**: new `AuditMetadataFormatter` helper (null/empty → "—"; valid JSON → indented via `JsonSerializer.Serialize(JsonDocument, WriteIndented=true)`; invalid JSON → raw string Razor-encoded). New partial `_MetadataDetails.cshtml` wraps the formatter output in a Bootstrap collapse with a small "Details"/"Raw" toggle. Empty result row added. Pagination preserves every filter. Navbar label changed "Audit Logs" → "Audit Timeline". **Backward-compat preserved**: `AuditLogsController.Index(page, userId)` legacy-only invocations still work; new params are all optional with default null. **Tests added** (`tests/Web.Tests.Unit/`): `AuditLogsApiClientUrlTests` (7 cases) — backward-compat pagination-only URL, single subject/actor filters, action escaping, blank-action skipped, ISO-8601 round-trippable date encoding, stable param order; `AuditMetadataFormatterTests` (5 + 3 inline cases) — null/empty/whitespace returns "—", valid JSON pretty-prints, invalid/truncated JSON falls back to raw, JSON arrays accepted; `AuditLogsMapperTests` (2 cases) — Phase 4 fields mapped, legacy nulls preserved. **NOT touched** per scope: legacy Activate/Deactivate, lifecycle command UI, modals, backend, role hierarchy preview, granular lifecycle states. **Known limitations**: action filter is currently a free-text input (dropdown of `AuditActions` constants deferred); `from`/`to` use the browser's local-time `datetime-local` widget but are sent as ISO-8601 — operators in non-UTC timezones see a small offset until Phase 5E adds explicit timezone handling. Validation: `dotnet build YallaJo.Web/YallaJo.Web.csproj` 0 errors; `dotnet test tests/Web.Tests.Unit/Web.Tests.Unit.csproj` 47/47 passing; full-solution `dotnet test YallaJo.sln --no-build` all green — Security 119/119, Auth 258/258, Accounts 16/16, Web 47/47, SharedKernel 5/5, ContentCore 11/11. |
| 42 | Security/Auth Phase 4 — Admin audit timeline | ✅ | 🤖 Agent | Added a durable, append-only audit trail for the five admin lifecycle verbs (suspend / reactivate / archive / reset-password / reassign). Reused the existing `Security.AuditLog` aggregate per the approved plan; extended it with three nullable columns: `ActorUserId` (admin who performed the action), `Reason` (≤500 chars), `Metadata` (`nvarchar(max)` JSON). Added `AuditLog.CreateAdmin(...)` factory + EF migration `AddAdminAuditColumns` (with composite index `IX_AuditLogs_ActorUserId_OccurredAt`). New cross-module contract `IAdminAuditWriter` + `AdminAuditEntry` + `AuditActions` constants in `Security.Contracts`. Internal `AdminAuditWriter` (Security.Infrastructure) registered in DI; appends rows on the Security UoW, enlisting in any ambient `TransactionScope`. Five admin handlers (Auth.Application) inject `IAdminAuditWriter` + `IRequestContext` and emit a row only on the success path; failures (authn / authz / lifecycle / profile-scrub) write nothing. **Reassignment per Option C**: emits exactly ONE `ADMIN_REASSIGN_ACCOUNT` row whose metadata JSON carries `oldEmail`, `newEmail`, `lifecycleFrom`, `lifecycleTo`, `activationsSuperseded`, `resetsSuperseded`, `providersDeactivated`, and `profileScrubbed: true|false` — no second audit row for the scrub. **Phase 3D contract tweak** (Option C requirement): `IProfileReassignmentService.ResetForReassignmentAsync` return type widened from `Result` to `Result<ProfileReassignmentOutcome>` carrying a `Scrubbed` flag (`true` when an existing profile row was mutated, `false` for the missing-profile no-op path). **AdminResetPassword metadata** carries `tokenId` + `origin` only — never the plain reset code. **Read-side**: `GetAuditLogsQuery` + endpoint extended with optional `actorUserId`/`action`/`from`/`to` filters; `AuditLogDto` now projects `ActorUserId`, `ResourceType`, `ResourceId`, `Reason`, `Metadata`. Existing audit handlers (REGISTER, LOGIN, LOGOUT, PASSWORD_CHANGED, PASSWORD_RESET) continue to work unchanged via the original `AuditLog.Create` factory. **Tests added**: `tests/Security.Tests.Unit/AuditLogAdminFactoryTests.cs` (11 cases counting `[Theory]` rows: factory population, reason trim/null, target → UserId, UTC timestamp, argument guards, legacy factory leaves admin columns null); 6 new tests in `tests/Auth.Tests.Unit/AdminReassignAccountCommandHandlerTests.cs` (single-row invariant, `profileScrubbed=true` and `profileScrubbed=false` metadata branches, no audit on Security/profile failure, no plain activation token in metadata); audit assertions added to existing `AdminUserLifecycleCommandHandlerTests` and `AdminResetPasswordCommandHandlerTests` (success-path emission with action/actor/target/reason matching, no audit on early-return failure, no plain code in metadata). **Known limitations**: two-step commit on suspend / reactivate / archive / admin-reset (Auth UoW saves before Security audit save — same posture as the Phase 3A `MarkPendingPasswordResetAsync` two-step commit; reassignment is unaffected because everything runs inside `ITransactionalExecutor`); audit rows are immutable / no compensating reverts; metadata schema is conventional, not enforced; no cross-module integration event for downstream analytics. Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test YallaJo.sln --no-build` all green — Security.Tests.Unit 119/119, Auth.Tests.Unit 258/258, Accounts.Tests.Unit 16/16, Web.Tests.Unit 31/31, SharedKernel.Tests.Unit 5/5, ContentCore.Tests.Unit 11/11. |
| 41 | Accounts/Auth Phase 3D — Profile reassignment scrub | ✅ | 🤖 Agent | Closed the Phase 3C limitation by scrubbing the Accounts profile when an admin reassigns an account. **Accounts domain**: added `Profile.ResetForReassignment(newEmailLocalPart)` — sets `FirstName="Pending"`, `LastName="Activation"` (required fields stay valid), derives `DisplayName` from the trimmed email local-part (null when blank), nulls `AvatarUrl`/`DateOfBirth`/`Gender`/`Country`/`City`/`AddressLine`, keeps `UserId` stable, bumps `UpdatedAt`, does not soft-delete. **Accounts contracts**: new `IProfileReassignmentService` + `ProfileReassignmentRequest(UserId, NewEmail)` in `Accounts.Contracts/Abstractions`. **Accounts application**: internal `ProfileReassignmentService` loads the tracked profile via `FirstOrDefaultAsync(p => p.UserId == userId, asNoTracking: false)`, extracts the local-part of `NewEmail`, invokes the domain method, flushes via `IAccountsUnitOfWork`, and evicts `AccountsCacheKeys.UserProfileTag(userId)`. Missing profile → logged no-op returning `Result.Success` (idempotent vs. outbox lag). Persistence exceptions propagate so the outer `TransactionScope` rolls back. Registered in `Accounts.Application.DependencyInjection`. **Auth handler**: `AdminReassignAccountCommandHandler` now depends on `IProfileReassignmentService` and calls `ResetForReassignmentAsync` inside the transactional delegate after the fresh activation token is added and before the single Auth `SaveChangesAsync`. Failure propagates via `ReassignOutcome.Failed`; audit log line extended with `ProfileReset=true` marker. Endpoint / command / validator / result unchanged. **Tests**: added `tests/Accounts.Tests.Unit/ProfileReassignmentTests.cs` (6 domain tests, one `[Theory]` with 3 inline cases covering the null/empty/whitespace local-part branch) and `tests/Accounts.Tests.Unit/ProfileReassignmentServiceTests.cs` (5 service tests covering happy-path reset + save + cache eviction, missing-profile idempotent success, cache-tag targeting, persistence-exception propagation, and empty-UserId validation). Added NSubstitute + Microsoft.Extensions.Logging.Abstractions + Microsoft.Extensions.Caching.Hybrid package refs to `tests/Accounts.Tests.Unit/Accounts.Tests.Unit.csproj`. Updated `tests/Auth.Tests.Unit/AdminReassignAccountCommandHandlerTests.cs` with 2 new tests: handler forwards `ProfileReassignmentRequest(UserId, NewEmail)` to the service on happy path, and propagates a `Result.Failure` from the service (no silent success, no Auth `SaveChanges`). **Known limitations**: MSDTC required when Auth + Security + Accounts enlist in the same `TransactionScope` (existing cross-module posture); no `ProfileReassignedEvent`/audit timeline in Phase 3D (deferred); missing-profile is a logged no-op (idempotent); no cross-module integration event for downstream analytics (deferred). Validation: `dotnet build YallaJo.sln` 0 errors; `dotnet test YallaJo.sln --no-build` all green — Security.Tests.Unit 108/108, Auth.Tests.Unit 252/252, Accounts.Tests.Unit 16/16, Web.Tests.Unit 31/31, SharedKernel.Tests.Unit 5/5, ContentCore.Tests.Unit 11/11. |

---
## [TRACKING] What Needs To Be Done Next
### Wave 1 — ContentCore Completion
- Auth/Security/Accounts lifecycle roadmap: **Phase 3 + Phase 4 + Phase 5 (5A–5E) all complete.** Web admins can read the Phase 4 audit timeline at `/admin/audit-logs` with the full filter surface; the `/admin/users` list shows binary lifecycle badges, role chips, page-local filter, and per-row Audit quick-links; and lifecycle actions (Suspend/Reactivate/Archive/Reset Password/Reassign) are triggered from the User Details page via Bootstrap 5 modals with verb-specific consequence copy, typed-ARCHIVE confirmation, and a required "I understand…" checkbox on Reassign. Phase 5E removed the last legacy Activate/Deactivate UI from User Details (backend remains callable for non-UI clients per scope), unified the Status badge through `_LifecycleBadge`, and upgraded TempData alerts to dismissible `role="alert"` boxes. Backend 403/409 messages flow through TempData on redirect. **Final consolidated deferred items**: granular lifecycle palette beyond Active/Inactive (needs `UserDto.LifecycleState` extension); `Created`/`Updated` columns (not on `UserDto`); server-side search/sort/bulk actions (`ListUsersQuery` is page/pageSize only); typed modal input not preserved across redirects; per-user audit timeline section on Details (Phase 6A candidate); removal of legacy `Activate`/`Deactivate` controller actions + API endpoints (UI-removed, kept callable); live char counter on Reason fields; action-verb dropdown on audit filter; cross-module `ProfileReassignedIntegrationEvent`; immutable audit rows; conventional metadata schema; MSDTC posture when 3 DbContexts enlist.
- Resolve current compile blockers before full-solution validation: duplicate type definitions in `Accounts.Application/Commands/DeleteAvatar/DeleteAvatarResult.cs`, `Accounts.Application/Commands/UpdateAvatar/UpdateAvatarResult.cs`, `Accounts.Application/Commands/UpdateProfile/UpdateProfileResult.cs`, `Security.Application/Commands/CreateRole/CreateRoleResult.cs`, and `Security.Application/Commands/UpdatePhone/UpdatePrimaryPhoneResult.cs` (`CS0101`).
- ~~Category tree, depth validation, slug auto-gen, reorder~~ ✅ Done
- ~~Specialization CQRS~~ ✅ Done
- Verify all endpoints end-to-end (recommend running Swagger after migration)
- ~~EF Migrations — all ContentCore entity schemas were pre-existing; no new schema changes from this session's fixes~~ ✅ Updated: migration `20260417115007_UpdateContentCoreUnicodeTranslationCacheAndStatus` added for Unicode/Status/hash schema updates
- Apply latest ContentCore migration to the target database and smoke-test ContentCore attachment/category/language flows in Swagger
- Add WS10-focused automated tests for signature mismatch rejection, human-reviewed translation preservation, and translation-cache hash dedup race safety
### Wave 2 — ContentPlaces remaining
- ~~**Task 1**: Place CQRS + Admin Actions~~ ✅ Done (CategoryId, TourCount, HasActiveTours, Haversine optimized)
- ~~**Task 2**: Business CQRS + Full State Machine~~ ✅ Done
- ~~**Task 3**: BusinessHours Batch Upsert~~ ✅ Done
- ~~**Task 4**: ServiceItem Full CQRS~~ ✅ Done (IDOR, outbox, Category/Description, route split)
- **Task 5**: BusinessAmenity Management — Fadwa fixes remaining (IDOR, ICacheableQuery, pagination)
- **Task 6**: BusinessStaff Management — Fadwa fixes remaining (auth hole, IDOR, endpoint auth, ICacheableQuery)
- ~~**Task 7**: Place Geo-Search~~ ✅ Done (Haversine optimized, bounding-box, composite index)
- **Task 8**: AccessibilityFeature — Fadwa fixes remaining (admin guard, AccessibilityFeatureDto.Id, ICacheableQuery)
- See `Agents/ContentPlaces-remaining-fix-plan.md` and `Agents/ContentPlaces-fixes-required.md` for Fadwa's 13 open items
### Wave 3 — Phase 1 MVP remaining (~80 endpoints)
- **ContentTours Task 1 — Tour Core CQRS + Approval State Machine**: ✅ **Complete and closed.** All 8 phases (0-7) shipped: schema/audit columns + filtered slug index + translation unique key (Phase 0); Tour aggregate state machine + 7 domain events (Phase 1); commands/queries/validators/`ITourRepository`/`ContentToursCacheKeys` (Phase 2); 8 command + 3 query handlers with cancellation/concurrency guards, language-partitioned cache keys, role-tier ownership checks (Phase 3); 7 outbox-only Infrastructure domain-event handlers + best-effort translation (Phase 4); 8 integration events + registry mappings (Phase 5); 11 endpoints under `/api/v1/tours` with `MustHavePermissionAttribute` + `Accept-Language` pass-through + `RowVersion` bodies (Phase 6); 50 unit tests, all green (Phase 7).
  - **Post-audit closing follow-up (resolved):** the previous "raw-SQL `PlaceExistsService` is schema-coupled to `[content_places].[Places]`/`[IsDeleted]`" concern is no longer applicable. ContentTours now depends on `ContentPlaces.Contracts.Places.IPlaceExistenceService`; `ContentPlaces.Infrastructure` owns the EF implementation through `ContentPlacesDbContext` (uses `IgnoreQueryFilters().AsNoTracking()` to distinguish Deleted from NotFound). The old raw-SQL service and local `IPlaceExistsService` interface were removed. Build 0 errors; ContentTours.Tests.Unit 50/50 and SharedKernel.Tests.Unit 91/91 remain green. Layering verified: `ContentTours.Infrastructure` has no project reference to `ContentPlaces.Infrastructure`.
  - **Remaining deferred items for Task 1** (intentionally not part of Task 1 scope):
    1. **Endpoint runtime smoke tests** — deferred until a shared `WebApplicationFactory`/Testcontainers harness exists at solution level. Phase-6 verification was static (route count, attribute presence, Accept-Language pass-through, RowVersion binding).
    2. **Live-DB migration smoke** — initial-create migration `20260429210248_CreateModel` was statically verified; not exercised against a running SQL Server instance during the audit.
    3. **Sibling tour-feature handlers style alignment** (`TourPricingTier/*`, `TourSchedule/*`, `ListTourPricingTiers`) — 7 occurrences still use hardcoded `currentUser.IsInRole("Admin")` instead of the canonical `AppRoles.HighestPrivilegeLevel(...) >= RolePrivilegeLevel.Admin`. Out of Task-1 scope; Wave-3 sibling cleanup.
    4. **Pre-existing solution-level test failures unrelated to Task 1**: `Auth.Tests.Unit.AuthRetentionWorkerTests` (5 failures using an unregistered `ProbeIntegrationEvent` test fixture) and `ContentPlaces.Tests.Unit` (8 failures from prior staff/amenity/accessibility Wave-2 work).
- **ContentTours**: Tours full CQRS (~34 endpoints) — Task 1 (Tour Core, 11 endpoints) complete; remaining ~23 endpoints across TourSchedule/TourPricingTier/TourSearch/TourPackage/TourGuide tasks.
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

### §7.1 New Entity Checklist

Do ALL steps in order. Skip one → your work is rejected.

**Domain** (`{Module}.Domain/`)
- [ ] Entity class: choose base (`AuditableEntity + IAggregateRoot` / `BaseEntity` / plain class for junction)
- [ ] Private EF constructor: `private {Entity}() { }`
- [ ] Static factory: `public static {Entity} Create(...)` — uses `Guid.CreateVersion7()`, raises domain event
- [ ] Business methods: `Update()`, `Activate()`, `SoftDelete()` (if AuditableEntity)
- [ ] Private setters — no public setters
- [ ] Domain events in `Events/`
- [ ] Repository interface in `Repositories/` (see selection table below)

**Contracts** (`{Module}.Contracts/`)
- [ ] Add feature name to `{Module}Features.cs`
- [ ] Add permissions to `{Module}PermissionCatalog.cs` (Read/Create/Update/Delete as needed)
- [ ] Integration event records in `IntegrationEvents/` (if cross-module)

**Application** (`{Module}.Application/`)
- [ ] Commands: `Commands/{Entity}/Create{Entity}/` — Command + Handler + Validator
- [ ] Commands: `Commands/{Entity}/Update{Entity}/` — Command + Handler + Validator
- [ ] Commands: `Commands/{Entity}/Delete{Entity}/` — Command + Handler + Validator
- [ ] Query: `Queries/{Entity}/List{Entities}/` — Query **implements `ICacheableQuery`**, returns `SummaryDto`, paginated, `asNoTracking: true`
- [ ] Query: `Queries/{Entity}/Get{Entity}ById/` — Query **implements `ICacheableQuery`**, returns `DetailDto`
- [ ] `Caching/{Module}CacheKeys.cs` — static key factory
- [ ] **All command handlers inject `HybridCache` + call `RemoveByTagAsync` after save**
- [ ] **Every handler injects `ILogger<THandler>`**
- [ ] **`ICurrentUser` ONLY if comparing userId against entity field** (see [§2.2](#22-rule--icurrentuser-usage-policy-mandatory))
- [ ] Domain event handlers in `EventHandlers/` (if needed)

**Infrastructure** (`{Module}.Infrastructure/`)
- [ ] EF configuration in `Persistence/Configurations/`
- [ ] Add `DbSet<{Entity}>` to DbContext
- [ ] Repository implementation in `Repositories/`
- [ ] **DI registration in `DependencyInjection.cs`** (most forgotten step)
- [ ] EF migration: `dotnet ef migrations add Add{Entity} --project {Module}.Infrastructure --startup-project YallaJo.Api`

**Presentation** (`{Module}.Presentation/`)
- [ ] Endpoint group in `{Module}Endpoints.cs`
- [ ] **Every endpoint: `.WithMetadata(new MustHavePermissionAttribute(...))` OR `.AllowAnonymous()`** — no exceptions
- [ ] `.WithName()`, `.WithSummary()`, `.Produces<T>()`, `.ProducesValidationProblem()` on each endpoint
- [ ] Request DTOs in `Endpoints/{Entity}/Models/`

**Verify**
- [ ] `dotnet build YallaJo.sln -c Debug --nologo` — 0 errors
- [ ] `dotnet test YallaJo.sln --nologo` — all pass
- [ ] Swagger loads, new endpoints listed with correct auth indicators
- [ ] Startup log: `"Seeding N permissions from M modules: ..., {Module}"` shows new permissions seeded

**Repository selection**:
| Entity | Interface | Impl |
|---|---|---|
| Aggregate root (`IAggregateRoot`) | `IRepository<T, Guid>` | `EfRepository<T>` |
| Non-aggregate (`BaseEntity` / `AuditableEntity`) | `IReadRepository<T,TKey>` + `IWriteRepository<T,TKey>` | `EfEntityRepository<T, TKey>` |
| Junction table (plain class) | Custom interface | DbContext-direct |

### §7.2 New Endpoint Checklist

- [ ] HTTP verb + route decided (follow `YallaJo.md` spec)
- [ ] Auth decoration: `.WithMetadata(new MustHavePermissionAttribute(...))` OR `.AllowAnonymous()` — MUST have one
- [ ] Command/Query created in `{Module}.Application`
- [ ] Validator created (if input has any fields)
- [ ] Request DTO + mapping method (`ToCommand()`)
- [ ] Response: `result.ToApiResult()` — never try/catch
- [ ] OpenAPI: `.WithName()`, `.WithSummary()`, `.Produces<T>()`, `.ProducesValidationProblem()`
- [ ] `.ProducesProblem(404)` on GET/{id}, PUT, DELETE
- [ ] `.ProducesProblem(409)` on PUT (concurrency) and POST (duplicate)
- [ ] If handler needs the current user's ID for ownership check: inject `ICurrentUser` and compare → `Result.Forbidden` if mismatch
- [ ] If handler doesn't need ownership check: do NOT inject `ICurrentUser`

### §7.3 PR Review Checklist

Before marking a feature complete, verify every item:

**Architecture**
- [ ] Domain has zero dependencies on Infrastructure/EF/MediatR
- [ ] Application references only Domain + Contracts + SharedKernel.Application
- [ ] Presentation references only Application + SharedKernel.Presentation
- [ ] No cross-module `using` statements (only `{OtherModule}.Contracts`)

**Authorization**
- [ ] Every new endpoint has `MustHavePermission` OR `AllowAnonymous` — no bare `RequireAuthorization`
- [ ] Permission declared in the module's `Features.cs` AND `PermissionCatalog.cs`
- [ ] Catalog registered in module DI
- [ ] Startup log shows new permissions seeded

**Handlers**
- [ ] Every handler injects `ILogger<THandler>`
- [ ] Commands return `Result` / `Result<T>` (never throw for business failures)
- [ ] Command handlers inject `HybridCache` and call `RemoveByTagAsync` after save
- [ ] `ICurrentUser` injected ONLY when comparing `UserId` against a resource field (ownership/IDOR)
- [ ] No `if (currentUser.IsAuthenticated)` checks — that's `MustHavePermission`'s job

**Transactions**
- [ ] Domain events on `IAggregateRoot` entities only (else handler never runs)
- [ ] Domain event handlers never call `SaveChangesAsync`
- [ ] Integration event handlers check inbox idempotency first, mark processed last
- [ ] State-change methods guarded: `if (!entity.IsActive) entity.Activate()` — prevents duplicate events

**Data**
- [ ] `Guid.CreateVersion7()` — never `Guid.NewGuid()`
- [ ] `DateTime.UtcNow` — never `DateTime.Now`
- [ ] All string columns have `HasMaxLength()`
- [ ] Money uses `decimal` with `HasPrecision(18, 2)`
- [ ] Enums stored as `int` via `HasConversion<int>()`
- [ ] Soft-delete filter `.HasQueryFilter(x => !x.IsDeleted)` on `AuditableEntity`

**Performance**
- [ ] `CancellationToken` propagated through every async call
- [ ] `.AsNoTracking()` on read queries (default in repo)
- [ ] `.AsSplitQuery()` when 2+ `.Include()`
- [ ] `.Select()` projections over entity loads for large reads
- [ ] Pagination max `PageSize` = 100
- [ ] No lazy loading (`virtual` navigation properties banned)
- [ ] Every query implements `ICacheableQuery`

**Security**
- [ ] Raw SQL: use `SqlQuery<T>(FormattableString)` or `FromSqlInterpolated` — NOT `SqlQueryRaw`/`FromSqlRaw` with interpolation (injection risk). `SqlQuery<T>($"...")` IS safe — EF converts holes to DbParameter.
- [ ] File uploads validate MIME + magic bytes, sanitize filename
- [ ] Prices recalculated server-side (never trust client)
- [ ] Payment endpoints have idempotency key
- [ ] No stack traces / SQL errors / internal paths in response DTOs
- [ ] Audit trail logged for sensitive operations (role changes, deletions, refunds)

**Error handling**
- [ ] No try/catch in endpoints
- [ ] No try/catch in command/query handlers for general `Exception`
- [ ] Infrastructure try/catch only for allowed types (HttpRequestException, DbUpdateConcurrencyException, etc.)
- [ ] Every catch block logs before continuing
- [ ] Error codes follow `{Entity}.{Reason}` convention

**Testing**
- [ ] `dotnet build` — 0 errors
- [ ] `dotnet test` — all pass
- [ ] No new StyleCop/analyzer warnings beyond baseline
- [ ] Swagger UI shows new endpoints with correct auth lock icons

### §7.4 Pre-flight & Completion

**Session start** (before writing any code)
1. Read this file (`agent-context.md`) in full
2. Read `Agents/error-log.md` in full
3. Run `dotnet build YallaJo.sln -c Debug --nologo` → verify 0 errors
4. If baseline is broken: fix or report BEFORE starting new work. Never build on broken foundation.

**Completion** (before marking ✅)
- All items in [§7.1](#71-new-entity-checklist) or [§7.2](#72-new-endpoint-checklist)
- All items in [§7.3](#73-pr-review-checklist)
- Updated `§11.2 Work Log` in this file
- Updated `§11.1 Module Status Overview` if applicable
- Updated `Agents/error-log.md` if any mistakes were made
- `dotnet build` passes with 0 errors
- `dotnet test` passes all
- `lsp_diagnostics` clean on all changed files

---

## §8. Known Issues (from audits)

### §8.1 `ICurrentUser` Violations (8 handlers)

**Audited**: 2026-04-22. **Source**: `bg_a8b53a93`.

Out of 26 handlers injecting `ICurrentUser`, 18 are valid (ownership/IDOR checks) and 8 are violations. Fix each per [§2.2](#22-rule--icurrentuser-usage-policy-mandatory).

| # | File | Issue | Fix |
|---|---|---|---|
| 1 | `ContentPlaces.Application/Commands/Business/ApproveBusiness/ApproveBusinessCommandHandler.cs` | Stamps approver ID but no permission check | Endpoint `MustHavePermission(Business, Approve)`. Keep `ICurrentUser` only for the ID stamp. |
| 2 | `ContentPlaces.Application/Commands/Business/RejectBusiness/RejectBusinessCommandHandler.cs` | Stamps rejector ID but no permission check | Same — `MustHavePermission(Business, Reject)` on endpoint. |
| 3 | `ContentPlaces.Application/Commands/Business/SuspendBusiness/SuspendBusinessCommandHandler.cs` | Injects `ICurrentUser` but never uses it for ownership | **Remove `ICurrentUser` injection entirely**. Use `MustHavePermission(Business, Suspend)` on endpoint. |
| 4 | `ContentPlaces.Application/Commands/Business/ReinstateBusiness/ReinstateBusinessCommandHandler.cs` | Same as #3 | **Remove `ICurrentUser`**. Use `MustHavePermission(Business, Reinstate)`. |
| 5 | `ContentPlaces.Application/Commands/BusinessStaff/AddBusinessStaff/AddBusinessStaffCommandHandler.cs` | Only checks `IsAuthenticated`, no ownership check | Add ownership check: load `business`, verify `business.OwnerId == currentUser.UserId.Value` OR `currentUser.IsInRole("Admin")` → `Result.Forbidden` otherwise. |
| 6 | `ContentPlaces.Application/Commands/BusinessAmenity/AddBusinessAmenity/AddBusinessAmenityCommandHandler.cs` | Type mismatch bug: `business.OwnerId != currentUser.UserId` (comparing `Guid` vs `Guid?`) | Fix: `!= currentUser.UserId.Value`. Check is correct in intent. |
| 7 | `ContentPlaces.Application/Commands/BusinessAmenity/RemoveBusinessAmenity/RemoveBusinessAmenityCommandHandler.cs` | Only checks `IsAuthenticated` | Add ownership check: load business from amenity.BusinessId, verify owner. |
| 8 | `ContentPlaces.Application/Commands/BusinessHours/SetBusinessHours/SetBusinessHoursCommandHandler.cs` | Ownership check is correct but loaded with `asNoTracking: true` while modifying | Change to `asNoTracking: false` if the business entity itself is modified; otherwise current pattern is OK. |

### §8.2 Endpoint Authorization Violations (28 endpoints)

**Audited**: 2026-04-22. **Source**: `bg_a22d1b43`. **Full report**: `Agents/endpoint-authorization-audit.md` + `Agents/endpoint-violations.csv`.

**Summary**:

| Classification | Count | % |
|---|---|---|
| `PERMISSION_GUARDED` (correct) | 89 | 70% |
| `ANONYMOUS` (correct — public endpoints) | 25 | 20% |
| `AUTH_ONLY` (violation — authenticated but no permission) | 11 | 9% |
| `UNPROTECTED` (violation — no auth at all) | 0 | 0% |
| `STRING_POLICY` (violation — `.RequireAuthorization("Permission.X.Y")` instead of attribute) | 9 | 7% |
| `MISSING_METADATA` (violation — no auth metadata) | 4 | 3% |

**Total violations**: 28 endpoints across 5 Presentation projects.

**Action plan** (3-phase, ~2.5 hrs):
1. **Phase 1** (11 endpoints, ~1 hr): Add `MustHavePermissionAttribute` to auth-only endpoints in Auth.Presentation (7), Accounts.Presentation (5), Security.Presentation (2).
2. **Phase 2** (9 endpoints, ~1 hr): Replace string-based `.RequireAuthorization("Permission.X.Y")` with `.WithMetadata(new MustHavePermissionAttribute(...))` in ContentCore.Presentation (9), ContentPlaces.Presentation (5).
3. **Phase 3** (4 endpoints, ~30 min): Add missing permission metadata in ContentPlaces.Presentation (4).

Full per-endpoint details: `Agents/endpoint-violations.csv`.

---

## §9. Gotchas (hard-won lessons)

### §9.1 Gotchas Registry

| # | Gotcha | What happens if you ignore |
|---|---|---|
| 1 | Domain events on non-`IAggregateRoot` entities are NEVER dispatched | `UnitOfWork.SaveChangesAsync` only collects events from `IAggregateRoot` entries. Handler silently doesn't run. |
| 2 | **NEVER call `SaveChangesAsync` in domain event handlers** | UoW dispatches events BEFORE SaveChanges. Double-save breaks atomicity. |
| 3 | No Hangfire/Quartz — use `BackgroundService` + `Channel<T>` | ADR-003. Don't add those packages. |
| 4 | `MarkUpdated()` exists only on `AuditableEntity`, not `BaseEntity` | For BaseEntity children: set `UpdatedAt = DateTime.UtcNow` directly (protected set). |
| 5 | `BaseEntity.UpdatedAt` has `protected set` | Settable inside entity methods only. |
| 6 | `EfRepository<T>` requires `IAggregateRoot` — non-aggregates use `EfEntityRepository<T, TKey>` | Build error if misused. |
| 7 | Junction tables are plain classes — composite keys, DbContext-direct | Don't extend BaseEntity for junctions. |
| 8 | **DI registration is the #1 most forgotten step** | Every repo, service, UoW MUST be in `DependencyInjection.cs`. Runtime `InvalidOperationException` otherwise. |
| 9 | Business Rules PDF is MANDATORY before implementing any module | `Agents/YallaJo Business Rules & Edge Cases.pdf`. Skip = redo. |
| 10 | `Microsoft.Extensions.*` packages MUST stay on `9.x` — NEVER `10.x` | `NU1605: Detected package downgrade`. Exception: `Caching.Hybrid` pinned at exactly `9.3.0`. |
| 11 | ALL queries MUST implement `ICacheableQuery` | Forgetting = `QueryCachingBehavior` bypassed, every request hits DB. |
| 12 | ALL command handlers MUST `RemoveByTagAsync` after save | Writing without invalidating = stale data until TTL. |
| 13 | `RemoveByTagAsync` MUST use the MOST specific tag | `RemoveByTagAsync("attachments")` evicts ALL attachment caches. Single-entity mutations MUST use `$"attachments:{EntityType}:{EntityId}"`. |
| 14 | Guard state before calling any method that raises a domain event | `if (!entity.IsActive) entity.Activate()` — unconditional calls produce duplicate outbox rows → duplicate downstream work. |
| 15 | Recursive tree builders NEED `HashSet<Guid> visited` | Circular parent ref in DB → `StackOverflowException`. |
| 16 | `ILogger<THandler>` mandatory in ALL handlers (cmd + query) | No exceptions. 21 ContentCore handlers were missing it at audit. |
| 17 | Never mark inbox processed before external side effect succeeds | OTP email example — save OTP + mark processed + SMTP fails = dead event. Mark processed only after SMTP success. |
| 18 | Gmail app passwords copied with spaces → SMTP auth fails | Strip spaces before `NetworkCredential`. |
| 19 | No parallel `dotnet build/test` on shared projects → `CS2012` file lock | Run build/test sequentially. |
| 20 | **Every endpoint MUST have explicit `MustHavePermission` OR `AllowAnonymous`** | Bare `.RequireAuthorization()` = "any authenticated user" — usually wrong. String-based policies = unauditable. |
| 21 | **`ICurrentUser` ONLY for ownership/self-comparison** | Gratuitous injection for `IsAuthenticated` checks or stamping = rejected in review. That's `MustHavePermission`'s job. |
| 22 | Each module owns its own `IPermissionCatalog` | Adding `Booking` features to `SecurityFeatures` or `AppPermissions.cs` breaks the per-module boundary. Use `BookingFeatures` + `BookingPermissionCatalog`. |
| 23 | `PermissionPolicyNames.Build()` is the ONLY place the policy name format lives | Don't hard-code `"Permission." + feature + "." + action` anywhere. |
| 24 | Forgetting to register `IPermissionCatalog` in module DI | Symptom: permissions don't appear in DB after seed. All endpoints return 403. Check startup log for `"Seeding N permissions from M modules: ..."` — your module should be listed. |
| 25 | **Non-aggregate handlers CANNOT use domain events for outbox** | `ServiceItem`, `BusinessStaff`, `BusinessAmenity`, `BusinessHours`, `AccessibilityFeature` are NOT `IAggregateRoot`. UoW never collects their events. For these entities, inject `IContentPlacesOutboxWriter` (Application interface, Infrastructure impl) and call `outbox.Enqueue(event)` BEFORE `SaveChangesAsync`. Never inject `ContentPlacesDbContext` directly into Application handlers — violates gotcha #22 / Clean Architecture §1.3. |
| 26 | **`IPublisher.Publish()` is NOT durable — always use the outbox** | `IPublisher.Publish()` is in-process MediatR. App restart between `SaveChanges` and `Publish` = event permanently lost. Every integration event MUST go through `OutboxMessage.Create()` staged in the DbContext and committed atomically with the entity row. See `ServiceItemCreatedIntegrationEvent` for the correct pattern. |
| 27 | **`EfRepository<T>` requires `IAggregateRoot` — `ServiceItem` removed `IAggregateRoot` so `IServiceItemRepository` must use `IReadRepository + IWriteRepository`** | Changing `ServiceItem` from aggregate to non-aggregate also requires changing the repository interface from `IRepository<T, Guid>` (which has `IAggregateRoot` constraint) to `IReadRepository<T, Guid>, IWriteRepository<T, Guid>`. And the impl from `EfRepository<T, Guid>` to `EfEntityRepository<T, Guid>`. Both changes must be made together or the build breaks. |
| 28 | **Every new integration event MUST be registered in `IntegrationEventTypeRegistry`** | `OutboxMessage.Create()` calls `IntegrationEventTypeRegistry.GetName()`. Publishing an unregistered event throws `InvalidOperationException` at runtime. Add the stable logical name (e.g. `"content-places.business.approved.v1"`) to `IntegrationEventTypeRegistry.cs` AND update `IntegrationEventTypeRegistryTests.cs` with the new count. Registry lives in `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/`. |
| 29 | **Haversine double-computation anti-pattern** | Writing `6371 * ACOS(...)` in both `SELECT` (for alias) and `WHERE` (for filter) runs the formula TWICE per row. SQL Server does not CSE this. Fix: wrap in a derived table — compute once in inner query, filter on the alias in outer query. See `PlaceRepository.GetNearbyAsync` for the correct pattern. |
| 30 | **`SqlQuery<T>($"...")` FormattableString IS injection-safe — do not confuse with `SqlQueryRaw`** | `context.Database.SqlQuery<T>(FormattableString)` (used with `$"""..."""` verbatim + interpolation) converts every `{hole}` to a `DbParameter`. This is identical safety to `FromSqlInterpolated`. `SqlQueryRaw(string)` is the dangerous overload. Never use `FromSqlRaw($"... {userInput} ...")` — that is a SQL injection vulnerability. |
| 31 | **Add bounding-box pre-filter before Haversine for geo queries** | Running `ACOS/COS/SIN/RADIANS` on every row is expensive. Add a cheap arithmetic bounding-box (`Latitude BETWEEN minLat AND maxLat AND Longitude BETWEEN minLng AND maxLng`) before the Haversine formula. This eliminates ~99% of rows using the `IX_Places_IsDeleted_Latitude_Longitude` composite index before any trig runs. See `PlaceRepository.GetNearbyAsync`. |
| 32 | **`TourCount` on Place is denormalized — update via `PlaceTourCountUpdatedIntegrationEvent` inbox** | `Place.TourCount` is owned by ContentPlaces but the authoritative count lives in ContentTours. ContentTours publishes `PlaceTourCountUpdatedIntegrationEvent(PlaceId, ActiveTourCount)` via outbox whenever Tour status or PlaceId changes. ContentPlaces handles it via `PlaceTourCountUpdatedIntegrationEventHandler` inbox handler calling `place.UpdateTourCount(count)`. The count is always a fresh re-query from ContentToursDbContext — never increment/decrement (avoids drift). |
| 33 | **`HasActiveTours` filter on `ListPlacesQuery` requires `Place.TourCount > 0`** | The `PlaceFilterSpecification` uses `WhereIf(hasActiveTours == true, p => p.TourCount > 0)`. This field is denormalized and kept in sync by the TourCount event flow (gotcha #32). If TourCount is always 0 (e.g. no tours published), the filter will correctly return nothing. Do not try to cross-join ContentTours from ContentPlaces to compute this. |

---

## §10. Session Protocol

### §10.1 Session Start

1. Read `Agents/agent-context.md` (this file) in full
2. Read `Agents/error-log.md` in full
3. `dotnet build YallaJo.sln -c Debug --nologo` — verify 0 errors
4. If build fails: fix OR report before starting new work

### §10.2 Session End

1. Update `§11.2 Work Log` — add entry for every feature/entity/fix completed
2. Update `§11.1 Module Status Overview` — reflect reality
3. Update `§11.3 Next Up` — remove completed items, add new discoveries
4. Update `Agents/error-log.md` — log every mistake with root cause + prevention rule
5. Update `§9.1 Gotchas` — add new gotchas (number sequentially)
6. Update header `Build State` — your final `dotnet build` result

### §10.3 Tracking

| Status | Meaning |
|---|---|
| ✅ | Complete, tested, shipped |
| 🟡 | Partial — document exactly what's done and what remains |
| ❌ | Broken / reverted — explain why, so next agent doesn't rebuild on it |
| ⬜ | Not started |

**Never remove entries** — only update their status. History matters.
**Number entries sequentially** — never reuse or skip numbers.

---

## §11. Module Status (tracking)

### §11.1 Module Status Overview

| Module | Status | Notes |
|---|---|---|
| Auth | ✅ | Registration synchronously creates Accounts profile; email retry hardened; Gmail app-password normalization; JWT + refresh token rotation. |
| Security | ✅ | Privilege hierarchy enforced. Seed identities cover all roles. `IPermissionCatalog` pattern (PR 3 of auth refactor). |
| Accounts | ✅ | Profile + avatar self-service with validator + orphan-file cleanup. 3/3 unit tests. |
| ContentCore | ✅ | Full audit — 11 bugs fixed. All queries cached, all commands evict by tag. 11/11 unit tests. |
| ContentPlaces | 🟡 | Major fix pass complete (2026-04-23). Place module: all 8 fixes done. Business module: 5 integration events + 5 domain event handlers wired, duplicate DI fixed. ServiceItem: IAggregateRoot removed, Category/Description added, PriceCurrency/SalePriceCurrency columns dropped (migration), outbox pattern, IDOR checks, ICacheableQuery. Geo-search: Haversine single-compute + bounding-box. TourCount + CategoryId fields added to Place with migrations. **2026-05-20 Authorization-Cleanup:** removed the 8 `ICurrentUser` violations in Business admin/staff/amenity/hours handlers by passing actor IDs from endpoints; added typed auth metadata fixes in ContentPlaces endpoints. Remaining open: accessibility/admin polish, BusinessStaff integration events, query cache coverage, AccessibilityFeatureDto.Id, amenity pagination. |
| ContentTours | 🟡 | Tour entity has business methods (`Publish`, `Archive`, `Suspend`, `AssignToPlace`, `RemoveFromPlace`, `Delete`) + `TourPlaceCountChangedDomainEvent` + `TourPlaceCountChangedDomainEventHandler` that publishes `PlaceTourCountUpdatedIntegrationEvent` to outbox. `PlaceTourCountUpdatedIntegrationEvent` in Contracts. ContentPlaces has inbox handler. All wired end-to-end. **2026-04-28:** suppressed EF Core 9 `PendingModelChangesWarning` for `ContentToursDbContext` in Development only after repo inspection showed model snapshot and configuration are aligned; prevents false-positive startup migration failure noise while preserving non-development strictness. Also fixed `ContentToursDbInitializer` to stop reflection-setting computed getter-only `TourPricingTier.Currency`; seed tiers now rely on `Price.Currency`, removing startup seeding failure. Added explicit seed `ParticipantType` values (`Standard=Adult`, `VIP=Other`) so submit-time Adult-tier guard has valid seed data. Endpoints still empty. Full CQRS not started. |
| ContentBlogs | ⬜ | Entities exist. Endpoints empty. |
| ContentSeo | 🟡 | 4 integration event handlers wired (Place Created/Updated/Deleted + Business Created). SeoMetadata + SitemapEntry factories added. Weather PDF §11 compliance wired: coordinate/date weather cache key, 7-day forecast cache, persistent daily budget gate, budget-exhausted outbox event, and location endpoint. Business SEO created inactive until approval (Phase 2 handler pending). |
| Booking | 🟡 | **Pre-Work complete (2026-06-01):** `IBookingUnitOfWork` delegate, 6 aggregates marked `IAggregateRoot`, 14 domain events, 12 integration events (registered `booking.*.v1`), 7 repository interfaces + EF stubs, `ICommissionLookupService` stub in Finance.Contracts, `BookingPermissionCatalog` (26 perms / 8 features), test projects scaffolded. Build 0 errors. **Endpoints empty** — sprint kickoff 2026-06-15. See `Agents/tasks/Booking/PRE-WORK-KICKOFF.md`. |
| Finance | 🟡 | **Pre-Work complete (2026-06-01):** `IFinanceUnitOfWork` delegate, 11 aggregates marked, 14 domain events, 10 integration events, 6 repository interfaces, `IPaymentGateway` + `FakePaymentGateway` stub, PCI baseline (`PaymentRedactor`), `FinancePermissionCatalog` (22 perms / 12 features), test projects. Build 0 errors. **2026-05-21 Cleanup:** `SqlInvoiceNumberGenerator` now uses EF Core tracked `InvoiceNumberCounter` rows with `RowVersion` optimistic concurrency/retry and migration `FinanceAddInvoiceNumberCounterRowVersion`; `CommissionLookupService` now resolves active Free/JOD rules via `ICommissionRuleRepository` with 15% fallback; Finance cursor `pageSize` clamping moved from repositories into query handlers. **Endpoints empty** — sprint kickoff 2026-08-17. See `Agents/tasks/Finance/PRE-WORK-KICKOFF.md`. |
| Messaging | 🟡 | **Messaging sprint pass (2026-05-20):** added domain handlers for ticket auto-assignment, SignalR notification broadcast, and support-ticket created user notifications; added support-ticket resolved inbox handler; added Booking/Finance/Payout inbox handlers (skipping PaymentFailed/RefundInitiated notification creation because current events lack a target UserId); added `EmailNotificationSenderService` and `ReadNotificationCleanupService`; registered hosted services/options and SignalR in API; added Booking/Finance contract refs. `AuthUserRegisteredIntegrationEvent` is absent from current `Auth.Contracts`, so Auth registration notification/snapshot handlers were not created to avoid uncompilable code. Validation: Messaging Application/Infrastructure/Presentation + YallaJo.Api builds PASS (0 errors). **Endpoints still mostly empty.** |
| Social | 🟡 | **Pre-Work complete (2026-06-01):** `ISocialUnitOfWork` delegate, 3 aggregates marked (Review/Favorite/Report), 12 domain events, 5 integration events, 4 repository interfaces, `IProfanityFilter` + `INsfwClassifier` + Noop stubs, `SocialPermissionCatalog` (19 perms / 6 features), test projects. Build 0 errors. **Endpoints empty** — sprint kickoff 2026-10-19. See `Agents/tasks/Social/PRE-WORK-KICKOFF.md`. |
| Tracking | ⬜ | Entities exist. Endpoints empty. |
| Analytics | 🟡 | **Pre-Work complete (2026-06-01):** `IAnalyticsUnitOfWork` delegate, 5 aggregates marked (incl. `UserInteraction`/`AuditLog` with BIGINT `BaseEntity<long>` keys), 10 domain events, 3 integration events, 5 repository interfaces, `IClientContextProvider` + Noop stub, `AnalyticsPermissionCatalog` (14 perms / 6 features), test projects. Build 0 errors. **Endpoints empty** — sprint kickoff 2027-01-18. See `Agents/tasks/Analytics/PRE-WORK-KICKOFF.md`. |
| Auth-Cleanup | 🟡 | **Sprint pass 2026-05-20:** fixed targeted bare `.RequireAuthorization()` endpoints with typed `MustHavePermissionAttribute`; added Auth/Accounts feature constants + permission catalogs and DI registration; fixed ContentPlaces handler actor-ID cleanup; added SeoRedirect middleware stub/service wiring. Validation builds PASS for requested affected projects. Older pre-work debt catalogs may still include broader historical AUTH_AND_PERM_MIX/string-policy cleanup not covered by this sprint slice. |

**Infrastructure status** (cross-cutting):
- ✅ Serilog structured logging
- ✅ OpenTelemetry tracing + metrics
- ✅ API Versioning (`/api/v1/`)
- ✅ Global Exception Handler + ProblemDetails
- ✅ Health Checks (`/health/live`, `/health/ready`, `/health`)
- ✅ HybridCache (L1 in-memory; L2 Redis future)
- ✅ Authorization refactored to `IPermissionCatalog` pattern (4 PRs, 2026-04-22)
- ✅ Rate limiting
- ✅ 6 middleware (CorrelationId ✅, Localization ✅, RateLimiting ✅, SeoRedirect ✅ noop-backed, CORS ✅, ResponseCompression ✅)
- ⬜ 18 background services (3 so far: MediaProcessing, AuthCleanup, CompositeOutboxProcessor)
- ⬜ 3 SignalR hubs (NotificationHub, LiveTrackingHub, ChatBotHub)

### §11.2 Work Log

History preserved from previous sessions. Add entries immediately after completing work.

| # | Module / Feature | Status | Built By | Summary |
|---|---|---|---|---|
| 1–37 | (earlier entries) | ✅ | — | See git history before 2026-04-22 |
| 46 | ContentTours — TourPricingTierTranslation + localized pricing tier list | ✅ | 🤖 Agent | Added `TourPricingTierTranslation` domain entity (`TourPricingTierId`, `LanguageCode`, `Name`, `Description`) with create/update methods; added `Translations` navigation to `TourPricingTier`; added `ITourPricingTierTranslationRepository` + EF repo + DI registration; added `TourPricingTierTranslations` DbSet and EF config (unique `(TourPricingTierId, LanguageCode)`, name 200, description 500). Updated `LanguageActivatedIntegrationEventHandler` to backfill stub pricing-tier translations for newly activated languages using tier name/description fallback text. Updated `ListTourPricingTiersQuery` to accept nullable language, normalized cache key language handling, resolved `Accept-Language` from endpoint/request context, and returned localized pricing tier `Name`/`Description` with neutral-language fallback before defaulting to base tier fields. **Migration note**: attempted new EF migration, but existing prior migration `20260426203753_DropTierCurrencyAddParticipantTypeAndTourNameIndex` already contained `TourPricingTierTranslations`; removed the bogus generated follow-up migration after verification. Build: `dotnet build YallaJo.sln --no-incremental -v q` → 0 errors, warnings only. |
| 47 | ContentPlaces + ContentTours — startup EF fixes | ✅ | 🤖 Agent | Fixed `ContentPlaces.Infrastructure/Persistence/Configurations/PlaceConfiguration.cs` composite geo index to reference owned `Location` members via lambda (`IsDeleted`, `Location.Latitude`, `Location.Longitude`) instead of stale string property names, eliminating runtime model-build failure `The property 'Latitude' cannot be added to the type 'Place'`. In `ContentTours.Infrastructure/DependencyInjection.cs`, added Development-only suppression for EF Core 9 `RelationalEventId.PendingModelChangesWarning` after verifying `ContentToursDbContext` model and snapshot are aligned, preventing false-positive `MigrateAsync` startup noise in local dev while keeping non-development behavior strict. Validation: `lsp_diagnostics` clean on changed files; `dotnet build ContentPlaces.Infrastructure --no-dependencies` ✅; `dotnet build ContentTours.Infrastructure --no-dependencies` ✅; full solution build blocked by pre-existing `YallaJo.Web.exe` lock (`MSB3021`/`MSB3027`). |
| 48 | ContentPlaces + Auth — migration recovery pass | ✅ | 🤖 Agent | **ContentPlaces**: replaced invalid owner-level geo index with owned-builder index `loc.HasIndex(l => new { l.Latitude, l.Longitude }).HasDatabaseName("IX_Places_Latitude_Longitude")`; generated migration `20260428134344_Place_AddGeoBoundingBoxIndex`; applied ContentPlaces DB updates successfully (`CreateModel`, `ServiceItem_RemoveDuplicateCurrencyColumns`, `Place_AddCategoryIdAndTourCount`, `Place_AddGeoBoundingBoxIndex`). **Auth**: hardened migration `20260425201632_FixAuthDevicesTable` against local schema drift by replacing hard `DropIndex`/`CreateIndex` calls with guarded `IF EXISTS` / `IF NOT EXISTS` SQL for legacy OTP and ExternalProviders indexes; `dotnet ef database update --project Auth.Infrastructure --startup-project YallaJo.Api --context AuthDbContext` then completed successfully. Validation: `lsp_diagnostics` clean on touched files; `dotnet build ContentPlaces.Infrastructure --no-dependencies` ✅; `dotnet build Auth.Infrastructure --no-dependencies` ✅. |
| 49 | ContentTours — seeding fix for computed pricing-tier currency | ✅ | 🤖 Agent | `ContentToursDbInitializer.CreatePricingTiers()` was still reflection-setting `TourPricingTier.Currency`, but `TourPricingTier.Currency` is now a getter-only computed property derived from `Price.Currency` and ignored by EF mapping. This caused startup failure `Property set method not found` from `SetProperty<TValue>`. Removed the two stale `SetProperty(... nameof(TourPricingTier.Currency), "JOD")` calls for Standard and VIP tiers; left `Price = new Money(..., "JOD")` as source of truth. Validation: `lsp_diagnostics` clean on `ContentToursDbInitializer.cs`; `dotnet build ContentTours.Infrastructure --no-dependencies` ✅. |
| 50 | Seeder/model audit + ContentTours participant-type seed values | ✅ | 🤖 Agent | Added explicit `ParticipantType` seed assignments in `ContentToursDbInitializer`: `Standard -> ParticipantType.Adult`, `VIP -> ParticipantType.Other`, ensuring at least one active Adult tier exists for submit/approval guards after the recent typed pricing-tier refactor. Performed broad reflection-seeder audit across `Analytics`, `Booking`, `ContentBlogs`, `ContentPlaces`, `ContentSeo`, `ContentTours`, `Finance`, `Messaging`, `Security`, `Social`, and `Tracking` by scanning `SetProperty(... nameof(...))` usages against current entity/property definitions. Confirmed concrete broken mismatch was `TourPricingTier.Currency` getter-only computed property; no additional obvious computed/getter-only seeded properties were found in the audited modules from the current direct scan. Validation: `lsp_diagnostics` clean on `ContentToursDbInitializer.cs`; `dotnet build ContentTours.Infrastructure --no-dependencies` ✅. |
| 51 | ContentTours — TourGuide unit tests (Phase C.8) | ✅ | 🤖 Agent | Completed `tests/ContentTours.Tests.Unit/Ezz/TourGuideAssignTests.cs` and added `tests/ContentTours.Tests.Unit/Ezz/TourGuideUnassignTests.cs` + `tests/ContentTours.Tests.Unit/Ezz/TourGuideListTests.cs` for assign/unassign/list flows. Coverage includes ownership/admin gate, not-found/conflict/invalid outcomes, primary demotion/promotion rules, outbox enqueue assertions, profile enrichment fallback, and concurrency conflict behavior. Updated `tests/ContentTours.Tests.Unit/Ezz/TestAsyncQueryable.cs` to implement `IOrderedQueryable<T>` so ordered LINQ operators used by query handlers execute correctly in async unit tests. Validation: `dotnet test tests/ContentTours.Tests.Unit/ContentTours.Tests.Unit.csproj` → Passed (190/190). |
| 44 | Messaging — Inbox foundation + 4 Business notification handlers | ✅ | 🤖 Agent | **Foundation**: `IMessagingInboxStore` + `IMessagingUnitOfWork` interfaces (Application), `MessagingInboxStore` + `MessagingUnitOfWork` impls (Infrastructure), `InboxMessageConfiguration` EF config, `InboxMessages` DbSet added to `MessagingDbContext`, DI registered, migration `Messaging_AddInboxMessagesTable` generated. **Entities**: `Notification.Create()` factory + `MarkSent()` + `MarkRead()` methods. `NotificationType.Business = 7` added. **Handlers** (4): `BusinessApprovedIntegrationEventHandler` (InApp always + Email opt-in via `NotificationPreference`); `BusinessRejectedIntegrationEventHandler` (InApp + Email both unconditional — preference override, plan Rule C); `BusinessSuspendedIntegrationEventHandler` (InApp + Email Critical both unconditional — revenue-blocking, plan Rule C); `BusinessReinstatedIntegrationEventHandler` (InApp always + Email opt-in). **Project ref**: `ContentPlaces.Contracts` added to `Messaging.Infrastructure.csproj`. Build: 0 errors. Tests: 183/183. |
| 45 | ContentSeo — 4 Place+Business integration event handlers | ✅ | 🤖 Agent | **Entities**: `SeoMetadata.Create()` + `UpdateMeta()` (preserves editor ownership). `SitemapEntry.Create()` + `Touch()` + `ChangeUrl()` + `Deactivate()` + `Reactivate()`. **Handlers** (4): `PlaceCreatedIntegrationEventHandler` → creates `SeoMetadata(Place, priority=0.6)` + `SitemapEntry(/places/{slug}, active=true)`; `PlaceUpdatedIntegrationEventHandler` → `Touch()` SitemapEntry, self-heals missing records, logs TODO for slug URL update (event lacks OldSlug); `PlaceDeletedIntegrationEventHandler` → `SitemapEntry.Deactivate()`, SeoMetadata preserved for audit; `BusinessCreatedIntegrationEventHandler` → creates `SeoMetadata(Business, priority=0.5)` + `SitemapEntry(/businesses/{slug}, active=false)` — inactive until admin approves business. All handlers: idempotency guard + single `SaveChangesAsync` per §2.6. **Project ref**: `ContentPlaces.Contracts` added to `ContentSeo.Infrastructure.csproj`. Build: 0 errors. Tests: 183/183. |
| 40 | ContentPlaces — Full Fix Pass (Mahmoud + Mohammad + Ezz) | ✅ | 🤖 Agent | **Phase 0**: `IContentPlacesOutboxWriter` interface (Application) + `ContentPlacesOutboxWriter` impl (Infrastructure) — clean abstraction for non-aggregate outbox writes, respects gotcha #22. **Phase 1**: `HasActiveTours` + `CategoryId` filters wired into `ListPlacesQuery` → `PlaceFilterSpecification` → `ContentPlacesCacheKeys`. **Phase 2 (Mohammad)**: Duplicate `IBusinessRepository` DI removed; 5 Business integration event records in Contracts (`BusinessCreated/Approved/Rejected/Suspended/Reinstated`); 5 Business domain event handlers in Infrastructure (load business for OwnerId, translate name for Created, write to outbox); `Business.AddOrUpdateTranslation()` method added. **Phase 3 (Ezz)**: `ServiceItem` entity rewritten — `IAggregateRoot` removed, `Category`+`Description` added to `Create`/`Update`, `PriceCurrency`+`SalePriceCurrency` dropped; `ServiceItemConfiguration` cleaned; EF migration `ServiceItem_RemoveDuplicateCurrencyColumns`; `ServiceItemCreate...Event` renamed → `ServiceItemCreated...Event`; `CreateServiceItemResult.cs` namespace cleaned; 3 write handlers rewritten (IDOR + `IContentPlacesOutboxWriter` + `HybridCache`); `ListServiceItemsQueryHandler` filters by `IsAvailable` for public, shows all for owner/admin; `ListServiceItemsQuery` + `GetServiceItemByIdQuery` implement `ICacheableQuery`; `GetNearbyPlacesQuery` + `GetMapViewportQuery` implement `ICacheableQuery`; `GetNearbyPlacesQueryHandler` replaced in-memory Haversine with `IPlaceRepository.GetNearbyAsync` SQL; `NearbyPlaceSummaryDto` corrected shape (Slug, Latitude, Longitude, AverageRating, DistanceKm); ServiceItem routes split (List+Create under `/{businessId}/services`, Get+Update+Delete under `/services/{id}`); `CreateServiceItemRequest`+`UpdateServiceItemRequest` updated with Category+Description. **IntegrationEventTypeRegistry** updated to 19 events (was 13); `IServiceItemRepository` fixed to use `IReadRepository+IWriteRepository` + `EfEntityRepository`. Build: 0 errors. Tests: 183/183. |
| 41 | Place — CategoryId + TourCount fields | ✅ | 🤖 Agent | Added `Guid? CategoryId` and `int TourCount` (default 0) to `Place` entity with `SetCategory(Guid?)` and `UpdateTourCount(int)` business methods. EF config: nullable `CategoryId` column + `TourCount` column (default 0) + indexes `IX_Places_CategoryId` + `IX_Places_TourCount`. Migration `Place_AddCategoryIdAndTourCount` generated. `PlaceFilterSpecification` now uses real `WhereIf` expressions for both fields (TODOs resolved). Build: 0 errors. Tests: 183/183. |
| 42 | ContentTours → ContentPlaces TourCount sync (full event flow) | ✅ | 🤖 Agent | **ContentTours.Domain**: `TourPlaceCountChangedDomainEvent(TourId, PlaceId?)` added; Tour entity gained business methods `Publish()`, `Archive()`, `Suspend()`, `AssignToPlace(Guid)`, `RemoveFromPlace()`, `Delete()` — each raises the domain event when a PlaceId is affected. **ContentTours.Contracts**: `PlaceTourCountUpdatedIntegrationEvent(PlaceId, ActiveTourCount)`. **ContentTours.Infrastructure**: `TourPlaceCountChangedDomainEventHandler` — re-queries `ContentToursDbContext.Tours.CountAsync(Published + non-deleted + PlaceId)` to get authoritative count, writes outbox row. **ContentPlaces.Infrastructure**: `PlaceTourCountUpdatedIntegrationEventHandler` inbox handler — idempotency check, `place.UpdateTourCount(count)`, mark processed, `SaveChangesAsync`. `ContentPlaces.Infrastructure.csproj` references `ContentTours.Contracts`. `IntegrationEventTypeRegistry` + test updated. Design principle: ContentTours sends authoritative count (fresh re-query), not delta — idempotent and drift-proof. Build: 0 errors. Tests: 183/183. |
| 43 | PlaceRepository Haversine optimization | ✅ | 🤖 Agent | Fixed 3 issues in `GetNearbyAsync`: (1) **Double-computation** — Haversine was computed in both SELECT and WHERE (2× per row). Fixed by wrapping in derived table: compute once in inner query, filter on alias in outer query. (2) **No bounding-box pre-filter** — added cheap `Latitude/Longitude BETWEEN` filter (arithmetic, index-scannable) before trig functions, eliminating ~99% of rows early. (3) **No composite index for bounding-box** — added `IX_Places_IsDeleted_Latitude_Longitude` composite index to `PlaceConfiguration` + migration `Place_AddGeoBoundingBoxIndex`. Also confirmed: `SqlQuery<T>($"")` FormattableString IS injection-safe (EF Core converts holes to DbParameter). TODO logged: NetTopologySuite + `geography` column + SPATIAL INDEX when dataset > ~50k places. Build: 0 errors. Tests: 183/183. |
| 38 | Authorization Refactor — 4 PRs | ✅ | 🤖 Agent | Relocated `MustHavePermissionAttribute` from `Security.Contracts` to `SharedKernel.Presentation`. Moved `PermissionRequirement`/`Handler`/`Provider` from `YallaJo.Api` to `SharedKernel.Presentation`. Moved `AppAction` to `SharedKernel.Application`. Added `IPermissionCatalog` + `PermissionDescriptor` + `PermissionGroup` abstractions. Created `SecurityFeatures` + `SecurityPermissionCatalog`, `ContentCoreFeatures` + `ContentCorePermissionCatalog`, `ContentPlacesFeatures` + `ContentPlacesPermissionCatalog`. Replaced `AppPermissions.cs` god-switch with `RolePermissionMapping` (DI discovery). Deleted `AppFeatures.cs`, `AppRoleGroup.cs`, `AppPermissions.cs`. Updated all 17 consumer endpoint files. Added scaffold templates. Full plan: `Agents/authorization-refactor-plan.md`. 4 commits: `b34ce3f`, `1b69478`, `0829aeb`, `e576f4d`. Build: 0 errors. Tests: 135/135 passed. |
| 39 | Standardized Agent Context v2 | ✅ | 🤖 Agent | Rewrote `Agents/agent-context.md` from 1402-line legacy version into standardized rules-first structure (§0–§11). Added 2 new non-negotiable rules: (1) every endpoint must have `MustHavePermission` or `AllowAnonymous`; (2) `ICurrentUser` only for ownership/self-comparison. Audited codebase: 28 endpoint violations + 8 `ICurrentUser` handler violations catalogued in §8. Updated gotchas registry (24 entries). Moved `endpoint-authorization-audit.md` + `endpoint-violations.csv` to `Agents/`. References `authorization-refactor-plan.md` + `guide.md` + `YallaJo.md` + Business Rules PDF. |
| 52 | ContentCore — `CONTENTCORE-STD-P1-002` EntityTag query filter parity | ✅ | 🤖 Agent | Added missing soft-delete parent filter in `ContentCore.Infrastructure/Persistence/Configurations/EntityTagConfiguration.cs` via `builder.HasQueryFilter(x => !x.Tag.IsDeleted);` to align with existing `EntityCategory`/`TagTranslation` defensive filtering pattern. Scope kept intentionally minimal (single file, no handler/repository/query/endpoint changes, no migration generation). Validation (sequential to avoid `CS2012` file locks): `dotnet build ContentCore.Infrastructure/ContentCore.Infrastructure.csproj --nologo` ✅, `dotnet build ContentCore.Application/ContentCore.Application.csproj --nologo` ✅, `dotnet build ContentCore.Presentation/ContentCore.Presentation.csproj --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit/ContentCore.Tests.Unit.csproj --nologo` ✅ (11/11). Accepted existing model snapshot drift for this pass; no migration added by design. |
| 53 | ContentCore — `CONTENTCORE-STD-P2-001` TriggerTranslationBackfill concurrency guard | ✅ | 🤖 Agent | Added minimal `DbUpdateConcurrencyException` handling around both existing `unitOfWork.SaveChangesAsync(ct)` calls in `ContentCore.Application/Commands/Translation/TriggerTranslationBackfill/TriggerTranslationBackfillCommandHandler.cs` (tag backfill + specialization backfill paths). Both now return `Result<TriggerTranslationBackfillResult>.Conflict(new Error("Translation.ConcurrencyConflict", "One or more records were modified by another user. Please retry."))`. No changes to loops/batching/orchestrator/cancellation flow/endpoints/DI and no migrations/tests added. Validation (sequential): `dotnet build ContentCore.Application/ContentCore.Application.csproj --nologo` ✅, `dotnet build ContentCore.Presentation/ContentCore.Presentation.csproj --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit/ContentCore.Tests.Unit.csproj --nologo` ✅ (11/11). |
| 54 | ContentCore.Tests.Unit — Attachment owner/admin authorization backfill (T2) | ✅ | 🤖 Agent | Added T2 regression tests for attachment handlers to enforce owner-or-admin authorization and failure side-effect guarantees. Extended `tests/ContentCore.Tests.Unit/UploadAttachmentCommandHandlerTests.cs` with owner-allowed + unsupported-entity scenarios while preserving anti-spoofing coverage. Added new suites: `tests/ContentCore.Tests.Unit/DeleteAttachmentCommandHandlerTests.cs`, `tests/ContentCore.Tests.Unit/SetPrimaryImageCommandHandlerTests.cs`, and `tests/ContentCore.Tests.Unit/ReorderAttachmentsCommandHandlerTests.cs` covering admin-tier resolver bypass, owner allowed, uploader-not-owner forbidden, wrong-entity guard (SetPrimaryImage), and not-found/deleted/unsupported mappings with assertions that failed auth paths do not mutate/save/cache/storage/queue. Validation (sequential): `dotnet build ContentCore.Application/ContentCore.Application.csproj --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit/ContentCore.Tests.Unit.csproj --nologo` ✅ (68/68), `dotnet build YallaJo.sln --nologo` ✅. |
| 55 | ContentCore.Tests.Unit — DeleteAttachment domain-event / outbox regression (T3) | ✅ | 🤖 Agent | Added T3 regression for CONTENTCORE-STD-P0-001. New `tests/ContentCore.Tests.Unit/AttachmentDomainEventTests.cs` (6 domain tests): MarkForDeletion raises AttachmentDeletedDomainEvent; event carries correct AttachmentId/EntityType/EntityId/AttachmentType/Url; method is not idempotent; Create + MarkForDeletion emit both events cumulatively. New class `DeleteAttachmentEventRegressionTests` in existing handler test file (9 tests): owner delete raises event; admin delete raises event; event payload fields correct; entity-attachments cache tag evicted; single-attachment cache tag evicted; forbidden path emits no event / no remove/save/storage/cache; attachment-not-found emits no event; target-entity-not-found emits no event; target-entity-deleted emits no event. Outbox handler (AttachmentDeletedDomainEventHandler) deferred — requires EF InMemory + ContentCore.Infrastructure project ref (integration-test scope). No production code changed. Validation (sequential): `dotnet build ContentCore.Application/ContentCore.Application.csproj --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit/ContentCore.Tests.Unit.csproj --nologo` ✅ (83/83, was 68), `dotnet build YallaJo.sln --nologo` ✅. |
| 58 | ContentCore.Tests.Unit — TriggerTranslationBackfill concurrency conflict regression (T6) | ✅ | 🤖 Agent | Added T6 regression for CONTENTCORE-STD-P2-001. New `tests/ContentCore.Tests.Unit/TriggerTranslationBackfillCommandHandlerTests.cs` with file-local `TranslationBackfillHandlerBuilder` (including `NoOpOrchestrator`, `TagRepoReturning`, `SpecRepoReturning`, `ConcurrentUnitOfWork` helpers) and two test classes (9 tests total). Class `TriggerTranslationBackfillConcurrencyTests` (5 tests): tag path concurrency conflict asserts Outcome.Conflict + Error.Code=="Translation.ConcurrencyConflict" + Error.Message=="One or more records were modified by another user. Please retry."; same for empty tag list; specialization path same assertions; same for empty spec list; not-throw guard confirms exception never escapes. Class `TriggerTranslationBackfillSuccessTests` (4 tests): tag success path asserts Outcome.Ok + correct TriggerTranslationBackfillResult fields; specialization success path; invalid entity kind returns Outcome.Invalid + Error.Code=="Backfill.InvalidKind"; case-insensitive EntityKind routing ("TAG", "Tag" both succeed). SaveChangesAsync faulted via `Task.FromException<int>(new DbUpdateConcurrencyException())`. No production code changed. Validation (sequential): `dotnet build ContentCore.Application --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit --nologo` ✅ (110/110, was 101), `dotnet build YallaJo.sln --nologo` ✅. |
| 57 | ContentCore.Tests.Unit — EntityTag soft-deleted Tag query filter regression (T5) | ✅ | 🤖 Agent | Added T5 regression for CONTENTCORE-STD-P1-002. New `tests/ContentCore.Tests.Unit/EntityTagQueryFilterTests.cs` with 3 test classes (5 tests total). Infrastructure: added `Microsoft.EntityFrameworkCore.Sqlite` 9.0.15 + `ContentCore.Infrastructure` project reference to test csproj. `ContentCoreDbContext` could not be used for full SQLite schema creation because `OutboxMessageConfiguration.HasColumnType("nvarchar(max)")` fails SQLite DDL; instead created `internal sealed class TagFilterTestContext` that applies only production `TagConfiguration` + `EntityTagConfiguration` (real artifact, not copy). Additional SQLite workaround: `[Timestamp]` on `AuditableEntity.RowVersion` sets `ValueGeneratedOnAddOrUpdate()` which causes EF Core to omit the column from INSERT, triggering SQLite NOT NULL error; fixed with `modelBuilder.Entity<Tag>().Property(x => x.RowVersion).ValueGeneratedNever()`. SQL-level tests (class `EntityTagQueryFilterTests`, 3 tests): active-tag EntityTag returned; soft-deleted-tag EntityTag excluded; mixed active+deleted returns only active. Model-metadata tests (class `EntityTagAndCategoryFilterModelTests`, 2 tests): verify production `ContentCoreDbContext` model registers `HasQueryFilter(x => !x.Tag.IsDeleted)` on EntityTag and `HasQueryFilter(x => !x.Category.IsDeleted)` on EntityCategory (model-only inspection, no EnsureCreated). No production code changed. Validation (sequential): `dotnet build ContentCore.Infrastructure --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit --nologo` ✅ (101/101, was 96), `dotnet build YallaJo.sln --nologo` ✅. |
| 60 | ContentCore.Tests.Unit — Cache tag golden-string regression (T8) + production bug fix | ✅ | 🤖 Agent | Added T8 golden-string regression for CONTENTCORE-STD-P2-002. New `tests/ContentCore.Tests.Unit/ContentCoreCacheKeysGoldenStringTests.cs` (17 tests, no mocks, no DB): 6 global tag constant assertions (CategoriesTag, TagsTag, LanguagesTag, SpecializationsTag, AttachmentsTag, TranslationsTag); 5 single-resource tag helper assertions with fixed GUIDs (CategoryTag, TagTag, LanguageTag, SpecializationTag, AttachmentTag); 4 entity-scoped tag helper assertions with "Tour" entityType and fixed entityId (EntityAttachmentsTag, EntityCategoriesTag, EntityTagsTag, EntityTranslationsTag); 2 EntityType enum parity assertions (Tour.ToString()=="Tour", enum path matches direct literal path). ALSO fixed pre-existing production bug discovered when T8 validation ran: `ActivateSpecializationCommandHandler.cs` had (1) missing opening brace on `if (spec is null) {` → syntax error CS1524; (2) wrong capture variable `cancel` instead of `cancellationToken` in outer catch → CS0103. Both are confirmed compile-breaking bugs unrelated to T8 scope; fixed under the "real confirmed bug" exception. Validation (sequential): `dotnet build ContentCore.Application --nologo` ✅ (was 5 errors, now 0), `dotnet test tests/ContentCore.Tests.Unit --nologo` ✅ (133/133, was 116), `dotnet build YallaJo.sln --nologo` ✅. |
| 61 | ContentSeo — Weather PDF §11 persistent budget gate | ✅ | 🤖 Agent | Completed Phase 4 weather compliance: added `WeatherDailyBudgetRepository`, EF configuration, `WeatherDailyBudgets` DbSet, `IWeatherBudgetGate`, scoped `WeatherBudgetGate`, and `WeatherBudgetExhaustedIntegrationEvent` registered in `IntegrationEventTypeRegistry`. `RefreshWeatherCommandHandler` now consumes budget before upstream provider calls and returns `Weather.BudgetExhausted` with `Outcome.TooManyRequests` when exhausted. `WeatherPreFetchService` resolves the scoped gate in its daily run placeholder and logs readiness for future per-provider-call checks. Generated migration `20260518223144_WeatherDailyBudgetAndCoordinateKey` for coordinate/date weather cache key + `WeatherDailyBudget` table (also captured existing FaqItems.Question length drift from current model). Fixed `ContentSeoDbInitializer` seed data to use `ForecastJson` plus rounded coordinates/date after the weather cache rename. Validation: `lsp_diagnostics` clean on changed files; `dotnet build ContentSeo.Domain`, `.Application`, `.Infrastructure`, `.Presentation`, and `YallaJo.Api` all PASS with 0 errors. |
| 62 | Messaging — sprint handlers + background services | ✅ | 🤖 Agent | Added `TicketCreatedAutoAssignHandler` (round-robin active admin assignment), `NotificationCreatedSignalRBroadcastHandler` (optional SignalR broadcast via `IHubContext<Hub>`), `SupportTicketCreatedForUserHandler`, and `SupportTicketResolvedForUserHandler`. Added inbox handlers for `TourBookingConfirmed/Cancelled/Completed`, `PaymentCompleted`, `PaymentFailed` (logs/marks processed because event lacks UserId), `RefundInitiated` (logs/marks processed because event lacks UserId), and `PayoutScheduled`. Added `EmailNotificationSenderService` (30s timer, 2m initial delay, batch 50, retry backoff 1/5/15m, user snapshot email resolution, max 3 retries) and `ReadNotificationCleanupService` (weekly Sunday 02:00 UTC, read-retention cleanup, per-user cap, stale device token delete). Updated `Messaging.Infrastructure` DI with manually bound options + hosted services, added Booking/Finance contract refs, and registered SignalR in `YallaJo.Api/Program.cs`. `AuthUserRegisteredIntegrationEvent` does not exist in current `Auth.Contracts`; no uncompilable Auth registration/snapshot handlers were added. Validation (sequential): `dotnet build Messaging.Infrastructure`, `Messaging.Application`, `Messaging.Presentation`, and `YallaJo.Api` all PASS with 0 errors. |
| 63 | Finance — sprint cleanup fixes | ✅ | 🤖 Agent | Removed raw SQL from `SqlInvoiceNumberGenerator`; invoice numbers now use EF Core tracked `InvoiceNumberCounter` rows, `RowVersion` optimistic concurrency, bounded retry/detach handling, and migration `20260521030854_FinanceAddInvoiceNumberCounterRowVersion`. `CommissionLookupService` now queries `ICommissionRuleRepository` for Free/JOD rules and falls back to 15%. Moved cursor `pageSize` clamping out of Finance repositories and into the six paged query handlers. Validation: Finance Domain/Contracts/Application/Infrastructure/Presentation and YallaJo.Api builds PASS (0 errors); Finance.Tests.Unit 3/3 PASS; Finance.IntegrationTests 4/4 PASS; raw SQL and repository clamp greps clean. |
| 64 | Social — sprint audit inbox + timer fixes | ✅ | 🤖 Agent | Added Social snapshot entities/configs/repos for `PlaceSnapshot`, `BusinessSnapshot`, and `TourSnapshot`; wired DbSets and DI; generated migration `20260521033426_SocialAddContentSnapshots`. Added 7 inbox-idempotent handlers for existing contracts: `PlaceCreated/Updated/Deleted`, `BusinessCreated`, `TourCreated/Updated/Deleted`. Added ContentPlaces.Contracts and ContentTours.Contracts refs to Social.Application. Skipped `BusinessUpdatedIntegrationEvent` and `BusinessDeletedIntegrationEvent` because those contract files do not exist. Refactored `OrphanedFavoritesCleanupService` and `RatingRecalculationService` from repeated `Task.Delay` loops to initial-delay + `PeriodicTimer` cadence; cleanup now queries deleted snapshot IDs. Validation: Social.Domain, Social.Application, Social.Infrastructure, Social.Presentation, and YallaJo.Api builds PASS (0 errors). |
| 59 | ContentCore.Tests.Unit — Invalid EntityType regression (T7) | ✅ | 🤖 Agent | Added T7 regression for CONTENTCORE-STD-P2-004 and P2-004b. New `tests/ContentCore.Tests.Unit/EntityQueryHandlerInvalidEntityTypeTests.cs` (6 tests) with file-local `CategoryQueryHandlerBuilder` and `TagQueryHandlerBuilder`: GetEntityCategoriesQueryHandler invalid EntityType → Outcome.Invalid + Code=="EntityCategory.InvalidEntityType" + Message=="Invalid entity type." + Value==null (no Success(empty)) + repository not called + valid EntityType sanity; GetEntityTagsQueryHandler same matrix with Code=="EntityTag.InvalidEntityType". Strengthened 4 existing T1 command-side invalid-EntityType tests by merging the Code-only `ContainSingle` into a compound predicate that also checks Message=="Invalid entity type." — AssignCategoriesToEntity, RemoveCategoryFromEntity, AssignTagsToEntity, RemoveTagFromEntity. No production code changed. Validation (sequential): `dotnet build ContentCore.Application --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit --nologo` ✅ (116/116, was 110), `dotnet build YallaJo.sln --nologo` ✅. |
| 56 | ContentCore.Tests.Unit — Category SoftDelete/Restore domain-event regression (T4) | ✅ | 🤖 Agent | Added T4 regression for CONTENTCORE-STD-P1-001. Extended `tests/ContentCore.Tests.Unit/CategoryTests.cs` with 7 domain-level tests: SoftDelete raises CategoryDeletedDomainEvent; event carries CategoryId+Slug; SoftDelete is idempotent (guard `if (IsDeleted) return` prevents second event); Restore raises CategoryRestoredDomainEvent after SoftDelete; event carries CategoryId+Slug and IsDeleted flips to false; Restore on live category raises no event (guard `if (!IsDeleted) return`); Restore is idempotent (second call after first restore raises no additional event). New `tests/ContentCore.Tests.Unit/CategorySoftDeleteRestoreHandlerTests.cs` (6 handler-level tests) with shared file-local `CategoryHandlerBuilder`: DeleteCategory success path raises exactly one CategoryDeletedDomainEvent via SoftDelete() delegation; not-found path raises no event; has-children path raises no event + no save; RestoreCategory success path raises exactly one CategoryRestoredDomainEvent via Restore() delegation; already-live category returns Invalid + no event + no save; not-found returns NotFound + no save. All tests use `DomainEventAssertions` and `.OfType<>().Should().ContainSingle/BeEmpty()` for strong no-duplicate guarantees. No production code changed. Validation (sequential): `dotnet build ContentCore.Domain --nologo` ✅, `dotnet build ContentCore.Application --nologo` ✅, `dotnet test tests/ContentCore.Tests.Unit --nologo` ✅ (96/96, was 83), `dotnet build YallaJo.sln --nologo` ✅. |

### §11.3 Next Up

#### Immediate — authorization hygiene (before any new feature)
- [ ] Fix 8 `ICurrentUser` violations per [§8.1](#81-icurrentuser-violations-8-handlers)
- [ ] Fix 28 endpoint auth violations per [§8.2](#82-endpoint-authorization-violations-28-endpoints) — 3-phase plan, ~2.5 hrs total

#### Wave 2 — ContentPlaces remaining (Fadwa tasks)
See `Agents/ContentPlaces-fixes-required.md` for Fadwa's 13 open items. Summary:
- [ ] `RemoveBusinessStaff` handler: no auth at all — add `ICurrentUser` + ownership check
- [ ] `ListBusinessStaff` endpoint: still `.AllowAnonymous()` — must be `MustHavePermission(BusinessStaff, Read)`
- [ ] `ListBusinessStaffQueryHandler`: no IDOR filtering — inject `ICurrentUser` + `IBusinessRepository`, verify ownership
- [ ] `UpdateAccessibilityFeatures` handler: no admin guard — add `ICurrentUser` + `IsInRole("Admin")` check
- [ ] Create `BusinessStaffAddedIntegrationEvent` + `BusinessStaffRemovedIntegrationEvent` in Contracts
- [ ] 3 query records missing `ICacheableQuery`: `ListBusinessAmenitiesQuery`, `ListBusinessStaffQuery`, `GetAccessibilityFeaturesQuery`
- [ ] `AccessibilityFeatureDto` missing `Guid Id` field
- [ ] `ListAmenities` endpoint: `Page`/`PageSize` not bound from query string
- [ ] Remove manual `CreatedAt = DateTime.UtcNow` from `BusinessAmenity.Create()` and `BusinessStaff.Create()` factories
- [ ] Fix error codes in amenity/staff handlers (`"Auth.Unauthorized"` → `Error.Unauthorized(msg)`)
- See `Agents/ContentPlaces-remaining-fix-plan.md` §"What's NOT in this plan" for full list

#### Wave 3 — MVP remaining (~80 endpoints)
- ContentTours: full CQRS (~34 endpoints)
- Booking: core booking state machine (~32 endpoints)
- Finance: payments + payouts (~52 endpoints)
- Social: reviews + favorites (~22 endpoints)

#### Infrastructure
- [ ] StyleCop SA1200 cleanup (low priority, cosmetic)
- [ ] Integration tests with Testcontainers + Respawn (per module)
- [ ] Docker + Aspire setup
- [ ] Cloudinary swap for `IFileStorageService` (when ready for production files)
- [ ] Run remaining EF migrations manually (Finance, Messaging, Security, Social, Tracking for AddOutboxTraceContext; all 14 for AddOutboxStatusColumn)

---

## §N. Outbox/Inbox Production Hardening — Session 2026-04-22

**Plan source**: `Agents/outbox-hardening-implementation-plan.md`
**Status**: All 5 PRs implemented. 0 build errors. 171 tests pass (was 135).
**No commits made** (user preference).

### PR 1 — Outbox Retention Cleanup ✅

**New files**:
- `SharedKernel.Infrastructure/Outbox/OutboxCleanupOptions.cs` — config POCO (Enabled, RetentionPeriod, CleanupInterval, BatchSize)
- `SharedKernel.Infrastructure/Outbox/IOutboxCleaner.cs` — per-module interface (also has `CountDeadLetteredAsync`, `ListDeadLetteredAsync`, `ReplayDeadLetterAsync` added in PR 3)
- `SharedKernel.Infrastructure/Outbox/OutboxCleaner<TContext>.cs` — EF impl; uses `ExecuteDeleteAsync` for SQL Server/Postgres, falls back to load-and-remove for InMemory
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxCleanupBackgroundService.cs` — hosted service, ticks every `CleanupInterval`, skips dead-lettered rows
- `tests/SharedKernel.Tests.Unit/OutboxCleanerTests.cs` — 6 tests

**Modified files**:
- All 14 `{Module}.Infrastructure/DependencyInjection.cs` — added `services.AddScoped<IOutboxCleaner, OutboxCleaner<{Module}DbContext>>()`
- `SharedKernel.Infrastructure/DependencyInjection.cs` — registers `OutboxCleanupBackgroundService` + binds `OutboxCleanupOptions`
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs` — `MaxRetryCount` promoted to `public const`
- `SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — added `MarkAsProcessedAt(DateTime)` for test-friendly state setting
- `YallaJo.Api/appsettings.json` + `appsettings.Development.json` — `OutboxCleanup` section
- `YallaJo.Api/Program.cs` — passes `builder.Configuration` to `AddSharedKernelInfrastructure`

**Key gotchas**:
- `InMemoryDatabaseRoot` must be shared across all scopes in tests or each scope sees empty DB
- `ExecuteDeleteAsync` is NOT supported by InMemory provider — detect by `db.Database.ProviderName`

---

### PR 2 — Integration Event Type Registry ✅

**New files**:
- `SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs` — maps **19** stable logical names (e.g. `"security.user.created.v1"`) to CLR types; throws on unregistered publish attempts. Started at 13; grew to 19 after ContentPlaces fix pass + ContentTours TourCount event.
- `tests/SharedKernel.Tests.Unit/IntegrationEventTypeRegistryTests.cs` — 5 tests (GetName, TryGetType, roundtrip, unregistered throws). Count assertion updated to 19.

**Modified files**:
- `SharedKernel.Infrastructure/YallaJo.SharedKernel.Infrastructure.csproj` — added `<ProjectReference>` to `Auth.Contracts`, `ContentCore.Contracts`, `ContentPlaces.Contracts`, `Security.Contracts`
- `SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — `Create()` now calls `IntegrationEventTypeRegistry.GetName()` instead of `AssemblyQualifiedName`
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs` — dual-read: tries registry first, falls back to `Type.GetType()` for legacy AQN rows
- `tests/SharedKernel.Tests.Unit/OutboxProcessorTests.cs` — `SeedOutboxMessageAsync` now bypasses `Create()` via reflection (stores AQN) to exercise dual-read fallback; added `LegacyAssemblyQualifiedNameRow_ShouldProcessViaFallbackPath` test

**Key gotchas**:
- Tests that use `OutboxMessage.Create()` with stub events not in the registry will throw. Use `Activator.CreateInstance(typeof(OutboxMessage), nonPublic: true)` + reflection to set properties directly in tests.
- Now **19 events** across 5 modules. Security (5), Auth (2), ContentCore (1), ContentPlaces (10), ContentTours (1).
- `ServiceItemCreateIntegrationEvent` was **renamed** to `ServiceItemCreatedIntegrationEvent` (missing 'd' fixed). Old name no longer exists.

**Registered events (short keys)** — current 19 total:
```
security.user.created.v1, security.user.email-verified.v1,
security.user.password-changed.v1, security.user.password-reset.v1,
security.user.phone-updated.v1, auth.user.logged-in.v1,
auth.session.revoked.v1, content-core.language.activated.v1,
content-places.place.created.v1, content-places.place.updated.v1,
content-places.place.deleted.v1,
content-places.business.created.v1, content-places.business.approved.v1,
content-places.business.rejected.v1, content-places.business.suspended.v1,
content-places.business.reinstated.v1,
content-places.service-item.created.v1, content-places.service-item.deleted.v1,
content-tours.place.tour-count-updated.v1
```

---

### PR 3 — Dead-Letter Ops + OpenTelemetry Metrics ✅

**New files**:
- `SharedKernel.Infrastructure/Outbox/OutboxMetrics.cs` — 8 OTel instruments (Meter: `YallaJo.Outbox`)
- `SharedKernel.Infrastructure/Outbox/OutboxDeadLetterDto.cs` — read model for dead-letter ops
- `SharedKernel.Infrastructure/Handlers/Outbox/ReplayDeadLetterCommandHandler.cs` — clones dead-lettered row, zeroes RetryCount, preserves original
- `SharedKernel.Infrastructure/Handlers/Outbox/ListDeadLettersQueryHandler.cs` — aggregates dead-letters across all modules
- `SharedKernel.Application/Abstractions/Outbox/ReplayDeadLetterCommand.cs`
- `SharedKernel.Application/Abstractions/Outbox/ListDeadLettersQuery.cs` + result records
- `SharedKernel.Application/Authorization/OpsFeatures.cs` — `OpsFeatures.Outbox` constant
- `YallaJo.Api/HealthChecks/OutboxDeadLetterHealthCheck.cs` — returns Degraded if any module has dead-lettered messages
- `YallaJo.Api/Endpoints/OpsEndpoints.cs` — `GET /api/v1/ops/outbox/dead-letters`, `POST /api/v1/ops/outbox/dead-letters/{module}/{id}/replay`

**Modified files**:
- `SharedKernel.Application/Authorization/AppAction.cs` — added `Replay` constant
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs` — records 6 OTel metrics
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxCleanupBackgroundService.cs` — records `CleanupDeletedTotal`
- `SharedKernel.Infrastructure/Outbox/IOutboxCleaner.cs` — added `CountDeadLetteredAsync`, `ListDeadLetteredAsync`, `ReplayDeadLetterAsync`
- `SharedKernel.Infrastructure/Outbox/OutboxCleaner.cs` — implements new interface methods
- `SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — added `CreateReplayCopy()`
- `YallaJo.Api/Extensions/HealthCheckExtensions.cs` — registers `OutboxDeadLetterHealthCheck`
- `YallaJo.Api/Extensions/OpenTelemetryExtensions.cs` — `.AddMeter("YallaJo.Outbox")`, `.AddSource("YallaJo.Outbox")`
- `YallaJo.Api/Program.cs` — `app.MapOpsEndpoints()`

**OTel metrics emitted** (all tagged with `module` and/or `type`/`handler`):
- `outbox.processed.total`, `outbox.failed.total`, `outbox.dead_lettered.total`
- `outbox.dispatch.latency_ms`, `outbox.retry.count`
- `outbox.handler.success.total`, `outbox.handler.failure.total`
- `outbox.cleanup.deleted.total`

**Ops endpoints** (both require `Permission.Outbox.{Read|Replay}`):
- `GET  /api/v1/ops/outbox/dead-letters?module=&limit=50`
- `POST /api/v1/ops/outbox/dead-letters/{module}/{id}/replay`

---

### PR 4 — W3C Trace Context + Adaptive Polling ✅

**New files**:
- `SharedKernel.Infrastructure/Outbox/TraceContextHelpers.cs` — BCL-only (no OTel API dep); `Capture()` stores `Activity.Id` + `TraceStateString` as JSON; `TryRestoreContext()` parses back to `ActivityContext`
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxActivitySource.cs` — `ActivitySource("YallaJo.Outbox", "1.0.0")`

**Modified files**:
- `SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — added `TraceContext` property; `Create()` calls `TraceContextHelpers.Capture()`
- `SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs` — restores trace context before each message; sets activity tags + status; returns `int` (messages processed count)
- `SharedKernel.Infrastructure/BackgroundJobs/IOutboxProcessor.cs` — `Task<int>` (was `Task`)
- `SharedKernel.Infrastructure/BackgroundJobs/CompositeOutboxProcessor.cs` — **adaptive polling**: 200 ms drain delay when any processor returned > 0, 10 s idle delay when all returned 0
- All 14 `OutboxMessageConfiguration.cs` — added `builder.Property(o => o.TraceContext).IsRequired(false).HasMaxLength(500)`

**EF migrations** (`AddOutboxTraceContext`): Created for Accounts, Analytics, Auth, Booking, ContentBlogs, ContentCore, ContentPlaces, ContentSeo, ContentTours. **Still needed (manual)**: Finance, Messaging, Security, Social, Tracking.

**Additional ContentPlaces migrations created in 2026-04-23 fix pass** (apply after AddOutboxTraceContext):
- `ServiceItem_RemoveDuplicateCurrencyColumns` — drops `PriceCurrency` + `SalePriceCurrency` columns from `ServiceItems`
- `Place_AddCategoryIdAndTourCount` — adds `CategoryId` (nullable Guid) + `TourCount` (int, default 0) + indexes `IX_Places_CategoryId` + `IX_Places_TourCount`
- `Place_AddGeoBoundingBoxIndex` — adds composite index `IX_Places_IsDeleted_Latitude_Longitude` for geo bounding-box pre-filter

**Migration command template**:
```
dotnet ef migrations add AddOutboxTraceContext \
  --project {Module}.Infrastructure \
  --startup-project YallaJo.Api \
  --context {Module}DbContext
```

---

### PR 5 — Explicit Status Column ✅

**New files**:
- `SharedKernel.Infrastructure/Outbox/OutboxMessageStatus.cs` — `enum { Pending=0, Processing=1, Processed=2, Failed=3, Dead=4 }`
- `SharedKernel.Infrastructure/Outbox/OutboxConstants.cs` — `MaxRetryCount = 10` (single source of truth; `OutboxProcessor<T>.MaxRetryCount` delegates to this)

**Modified files**:
- `SharedKernel.Infrastructure/Outbox/OutboxMessage.cs` — added `Status` property; `Lock()`, `MarkAsProcessed()`, `MarkAsProcessedAt()`, `MarkAsFailed()`, `Create()`, `CreateReplayCopy()` all set `Status` correctly
- All 14 `OutboxMessageConfiguration.cs` — added `builder.Property(o => o.Status).IsRequired().HasDefaultValue(OutboxMessageStatus.Pending).HasConversion<int>()`

**EF migrations** (`AddOutboxStatusColumn`): **ALL 14 still needed (manual)**.

**Migration command template**:
```
dotnet ef migrations add AddOutboxStatusColumn \
  --project {Module}.Infrastructure \
  --startup-project YallaJo.Api \
  --context {Module}DbContext
```

**Key gotcha**: Use `HasDefaultValue(OutboxMessageStatus.Pending)` NOT `HasDefaultValue(0)`. EF validates that the default value type matches the CLR property type after conversion — `int` vs `OutboxMessageStatus` mismatch causes `DbContext.get_ContextServices()` to throw in tests using the InMemory provider.

---

### Remaining Manual Steps

Run these commands to complete the database schema:

```powershell
# ── AddOutboxTraceContext (5 remaining) ──────────────────────────────────────
$root = "C:\Users\admin1\source\repos\YallaJo"; $api = "$root\YallaJo.Api"
foreach ($m in @("Finance","Messaging","Security","Social","Tracking")) {
  dotnet ef migrations add AddOutboxTraceContext `
    --project "$root\$m.Infrastructure" `
    --startup-project $api `
    --context "${m}DbContext"
}

# ── AddOutboxStatusColumn (all 14) ───────────────────────────────────────────
foreach ($m in @("Accounts","Analytics","Auth","Booking","ContentBlogs","ContentCore",
                  "ContentPlaces","ContentSeo","ContentTours","Finance","Messaging",
                  "Security","Social","Tracking")) {
  dotnet ef migrations add AddOutboxStatusColumn `
    --project "$root\$m.Infrastructure" `
    --startup-project $api `
    --context "${m}DbContext"
}

# ── Apply all migrations ─────────────────────────────────────────────────────
dotnet ef database update --project "$root\{Module}.Infrastructure" `
  --startup-project $api --context {Module}DbContext
# (repeat per module or run via the API startup auto-migration if configured)
```

---

## End of Document

---

## §N. ContentTours Presentation Endpoint Organization — Session 2026-05-01

**Status**: Completed (file move + model extraction only; no route/behavior changes).

**Updated structure**:
- Added `ContentTours.Presentation/Endpoints/TourSchedule/TourScheduleEndpoints.cs`
- Added `ContentTours.Presentation/Endpoints/TourSchedule/Models/CreateTourScheduleRequest.cs`
- Added `ContentTours.Presentation/Endpoints/TourSchedule/Models/UpdateTourScheduleRequest.cs`
- Added `ContentTours.Presentation/Endpoints/TourPricingTier/TourPricingTierEndpoints.cs`
- Added `ContentTours.Presentation/Endpoints/TourPricingTier/Models/CreateTourPricingTierRequest.cs`
- Added `ContentTours.Presentation/Endpoints/TourPricingTier/Models/UpdateTourPricingTierRequest.cs`
- Added `ContentTours.Presentation/Endpoints/TourSearch/TourSearchEndpoints.cs`
- Added `ContentTours.Presentation/Endpoints/TourSearch/Models/ToggleTourFeaturedRequest.cs`
- Updated `ContentTours.Presentation/ContentToursEndpoints.cs` imports to map new endpoint namespaces
- Removed legacy root-level files: `TourScheduleEndpoints.cs`, `TourPricingTierEndpoints.cs`, `TourSearchEndpoints.cs`

**Validation**:
- `dotnet build YallaJo.sln` ✅
- `dotnet test tests/ContentTours.Tests.Unit/ContentTours.Tests.Unit.csproj --no-build` ✅ (115/115)
- `dotnet test tests/SharedKernel.Tests.Unit/SharedKernel.Tests.Unit.csproj --no-build` ✅ (91/91)

**Questions? Check**:
1. This file first (§0.4 has the doc index)
2. `Agents/error-log.md` for past mistakes
3. `Agents/guide.md` for deep code patterns
4. `Agents/YallaJo.md` for endpoint specs
5. `Agents/YallaJo Business Rules & Edge Cases.pdf` for business logic
6. ADRs in `Agents/decisions/` for architectural decisions

**Still stuck?** Ask the user. Never guess business rules. Never invent scope.

- 2026-05-20: Analytics sprint implementation pass: rewrote Analytics features/permission catalog (14 permissions), domain/integration events, Phase 1 entities/enums, Application repository/queue/redaction interfaces, EF repositories/configurations/DbContext/DI, minimal interaction/popular/trending/dashboard/provider/audit handlers/endpoints, generated AnalyticsSprintSchema migration. Validation: Analytics.Domain/Contracts/Application/Infrastructure/Presentation and YallaJo.Api build PASS (0 errors). Note: Messaging.Application had pre-existing compile drift fixed during Api validation (stale using directives, DeviceToken.Register arg order, SupportTicket.TicketMessages property).
- 2026-05-20: Authorization-Cleanup sprint pass: removed `ICurrentUser` from 8 ContentPlaces.Application handlers by adding actor IDs to commands and forwarding `currentUser.UserId!.Value` from endpoints; `Business.Suspend/Reinstate` now stamp the reviewing actor like approve/reject. Added typed permission metadata to Auth/Accounts/Security self-service endpoints; added Auth/Accounts feature constants + permission catalogs and registered them in Infrastructure DI; aligned ContentPlaces place delete/staff read metadata; added `SeoRedirectMiddleware` + noop lookup service and registered it after authorization. Validation: requested Auth/Accounts/Security/ContentCore/ContentPlaces Presentation, ContentPlaces.Application, and YallaJo.Api builds PASS (0 errors); changed-file LSP diagnostics clean (repo-level LSP still reports stale `obj/Release` duplicate-attribute noise unrelated to changed source).
- 2026-05-21: Booking sprint audit fix pass: registered 12 existing Booking integration events in `IntegrationEventTypeRegistry`; added SlotLock active unique filtered index; added `SlotLockCleanupService`, `DocumentExpiryCheckService`, and `ProviderAutoAcceptService` using `PeriodicTimer` + scoped ticks; added repo methods for expired slot locks, provider document expiry windows, and stale pending provider confirmations; added provider document expiry notification tracking fields/methods and BG-service lookup indexes; generated `BookingAddSlotLockFilteredIndex` migration (contains SlotLock filtered index plus provider document tracking/indexes and TourBooking status/UpdatedAt index). Validation: Booking.Domain/Application/Infrastructure/Presentation, YallaJo.SharedKernel.Infrastructure, and YallaJo.Api builds PASS (0 errors); changed-source LSP diagnostics clean. Repo-level LSP still reports unrelated generated `obj/Release` duplicate-attribute noise.
- 2026-05-21: Finance sprint audit fix pass: fixed shared `Money` construction to preserve 4dp precision (`Math.Round(..., 4, MidpointRounding.ToEven)`) while keeping display formatting at 2dp; verified Finance Payment/Payout/Invoice money EF columns use `HasPrecision(19, 4)`; registered 3 missing Finance integration events (`finance.dispute.opened.v1`, `finance.subscription.activated.v1`, `finance.subscription.cancelled.v1`) for all 13 Finance event contracts; replaced `SqlInvoiceNumberGenerator` semaphore/MAX scan with atomic SQL Server monthly `InvoiceNumberCounters` table via `MERGE ... WITH (HOLDLOCK)` and generated `20260521024619_FinanceAddInvoiceNumberCounter`; refactored `PayoutBatchingService` to initial schedule delay + `PeriodicTimer(TimeSpan.FromDays(7))` with scoped ticks. Validation: Finance.Domain/Contracts/Application/Infrastructure/Presentation, YallaJo.SharedKernel.Infrastructure, and YallaJo.Api builds PASS (0 errors); `tests/Finance.Tests.Unit` PASS 3/3; `tests/Finance.IntegrationTests` PASS 4/4; changed-source LSP diagnostics clean. No new error-log entry: no implementation/build errors encountered.

