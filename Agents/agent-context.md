# YallaJo — Agent Context & Build Guide

> **Version**: 2.0 · **Last Updated**: 2026-04-22
> **Build State**: 0 errors · 135 tests pass · Authorization refactored to `IPermissionCatalog` (see `authorization-refactor-plan.md`).

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

## §0. Quick Start

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
- [ ] No raw SQL with string interpolation
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
| ContentPlaces | 🟡 | Tasks 2+3 complete (Business CQRS + state machine + BusinessHours). Task 1 (Place) and 4–8 remain. **Has `ICurrentUser` violations — see [§8.1](#81-icurrentuser-violations-8-handlers).** |
| ContentTours | ⬜ | Entities exist. Endpoints empty. |
| ContentBlogs | ⬜ | Entities exist. Endpoints empty. |
| ContentSeo | ⬜ | Entities exist. Endpoints empty. |
| Booking | ⬜ | Entities exist. Endpoints empty. |
| Finance | ⬜ | Entities exist. Endpoints empty. |
| Messaging | ⬜ | Entities exist. Endpoints empty. |
| Social | ⬜ | Entities exist. Endpoints empty. |
| Tracking | ⬜ | Entities exist. Endpoints empty. |
| Analytics | ⬜ | Entities exist. Endpoints empty. |

**Infrastructure status** (cross-cutting):
- ✅ Serilog structured logging
- ✅ OpenTelemetry tracing + metrics
- ✅ API Versioning (`/api/v1/`)
- ✅ Global Exception Handler + ProblemDetails
- ✅ Health Checks (`/health/live`, `/health/ready`, `/health`)
- ✅ HybridCache (L1 in-memory; L2 Redis future)
- ✅ Authorization refactored to `IPermissionCatalog` pattern (4 PRs, 2026-04-22)
- ✅ Rate limiting
- ⬜ 6 middleware (CorrelationId ✅, Localization ✅, RateLimiting ✅, SeoRedirect, CORS ✅, ResponseCompression ✅)
- ⬜ 18 background services (3 so far: MediaProcessing, AuthCleanup, CompositeOutboxProcessor)
- ⬜ 3 SignalR hubs (NotificationHub, LiveTrackingHub, ChatBotHub)

### §11.2 Work Log

History preserved from previous sessions. Add entries immediately after completing work.

| # | Module / Feature | Status | Built By | Summary |
|---|---|---|---|---|
| 1–37 | (earlier entries) | ✅ | — | See git history before 2026-04-22 |
| 38 | Authorization Refactor — 4 PRs | ✅ | 🤖 Agent | Relocated `MustHavePermissionAttribute` from `Security.Contracts` to `SharedKernel.Presentation`. Moved `PermissionRequirement`/`Handler`/`Provider` from `YallaJo.Api` to `SharedKernel.Presentation`. Moved `AppAction` to `SharedKernel.Application`. Added `IPermissionCatalog` + `PermissionDescriptor` + `PermissionGroup` abstractions. Created `SecurityFeatures` + `SecurityPermissionCatalog`, `ContentCoreFeatures` + `ContentCorePermissionCatalog`, `ContentPlacesFeatures` + `ContentPlacesPermissionCatalog`. Replaced `AppPermissions.cs` god-switch with `RolePermissionMapping` (DI discovery). Deleted `AppFeatures.cs`, `AppRoleGroup.cs`, `AppPermissions.cs`. Updated all 17 consumer endpoint files. Added scaffold templates. Full plan: `Agents/authorization-refactor-plan.md`. 4 commits: `b34ce3f`, `1b69478`, `0829aeb`, `e576f4d`. Build: 0 errors. Tests: 135/135 passed. |
| 39 | Standardized Agent Context v2 | ✅ | 🤖 Agent | Rewrote `Agents/agent-context.md` from 1402-line legacy version into standardized rules-first structure (§0–§11). Added 2 new non-negotiable rules: (1) every endpoint must have `MustHavePermission` or `AllowAnonymous`; (2) `ICurrentUser` only for ownership/self-comparison. Audited codebase: 28 endpoint violations + 8 `ICurrentUser` handler violations catalogued in §8. Updated gotchas registry (24 entries). Moved `endpoint-authorization-audit.md` + `endpoint-violations.csv` to `Agents/`. References `authorization-refactor-plan.md` + `guide.md` + `YallaJo.md` + Business Rules PDF. |

### §11.3 Next Up

#### Immediate — authorization hygiene (before any new feature)
- [ ] Fix 8 `ICurrentUser` violations per [§8.1](#81-icurrentuser-violations-8-handlers)
- [ ] Fix 28 endpoint auth violations per [§8.2](#82-endpoint-authorization-violations-28-endpoints) — 3-phase plan, ~2.5 hrs total

#### Wave 2 — ContentPlaces completion (34 endpoints total)
- See `Agents/ContentPlaces-tasks.md` for full breakdown
- Task 1: Place CQRS + Admin Actions (8 endpoints) — Phase 1
- Task 4: ServiceItem Full CQRS (5 endpoints) — Phase 1
- Task 5: BusinessAmenity Management (3 endpoints) — Phase 1
- Task 6: BusinessStaff Management (3 endpoints) — Phase 1
- Task 7: Place Geo-Search — Nearby + Map Viewport (2 endpoints) — Phase 2
- Task 8: AccessibilityFeature Get + Update (2 endpoints) — Phase 3

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

---

## End of Document

**Questions? Check**:
1. This file first (§0.4 has the doc index)
2. `Agents/error-log.md` for past mistakes
3. `Agents/guide.md` for deep code patterns
4. `Agents/YallaJo.md` for endpoint specs
5. `Agents/YallaJo Business Rules & Edge Cases.pdf` for business logic
6. ADRs in `Agents/decisions/` for architectural decisions

**Still stuck?** Ask the user. Never guess business rules. Never invent scope.
