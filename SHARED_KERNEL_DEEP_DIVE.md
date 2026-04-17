# YallaJoJo: Shared Kernel & Web Infrastructure Deep Exploration

## Executive Summary

This document provides a comprehensive analysis of the **shared kernel** and **web infrastructure** that the Auth module (and all other modules) depend on. The architecture follows a **modular monolith** pattern with:

- **Shared Kernel**: Domain abstractions, application patterns, and infrastructure utilities
- **Web Project (MVC)**: BFF (Backend-for-Frontend) pattern with cookie-based authentication
- **API Project (REST)**: JWT-based authentication with permission-based authorization
- **Auth Module**: Bridges the Web and API, managing credentials, tokens, and sessions

---

## 1. SHARED KERNEL ARCHITECTURE

### 1.1 YallaJo.SharedKernel.Domain

**Purpose**: Core domain abstractions with NO external dependencies (except System.*)

#### 1.1.1 Base Entities

**BaseEntity.cs**: All entities use Guid.CreateVersion7() for IDs (time-based, sortable)
- Domain events collected in-memory and dispatched by UnitOfWork
- Equality based on ID (transient entities are never equal)

**AuditableEntity.cs**: Soft delete support, row version for optimistic concurrency control
- Audit timestamps (CreatedAt, UpdatedAt, DeletedAt)

#### 1.1.2 Result Types (Railway-Oriented Programming)

**Result.cs** and **Result<T>.cs**: All commands return Result or Result<T> (never throw exceptions for business logic)
- Outcome enum maps directly to HTTP status codes (200, 201, 400, 401, 403, 404, 409, 500, 429, 499)
- Error record with Code (for API clients) and Message (for users)
- Supports monadic operations (Map, Bind, Match) for functional composition
- JSON serializable (required for HybridCache L2 Redis serialization)

#### 1.1.3 Repository Abstractions

**IRepository<TEntity, TKey>**: Combines IReadRepository and IWriteRepository
- Expression-based queries: GetByIdAsync, GetAsync, FirstOrDefaultAsync, SingleOrDefaultAsync, GetAllAsync, GetPaginatedAsync, SelectAsync, SelectPaginatedAsync, Query, ExistsAsync, AnyAsync, CountAsync
- Specification-based queries: FirstOrDefaultAsync(ISpecification<TEntity>), ListAsync, PaginatedListAsync, etc.
- All methods support asNoTracking for read-only queries

**IWriteRepository<TEntity, TKey>**: Add/Update/Remove operations
- Add/Update/Remove: Add, AddRange, AddAsync, AddRangeAsync, Update, UpdateRange, Remove, RemoveRange
- Bulk operations: ExecuteDeleteAsync, DeleteByIdAsync, ExecuteDeleteByIdsAsync
- Concurrency: AttachAndMarkModified, AttachAndMarkModifiedWithConcurrency
- Note: EF-specific ExecuteUpdateAsync lives only on concrete EfWriteRepository (not in domain interface)

**IUnitOfWork**: Coordinates persistence
- Task<int> SaveChangesAsync(CancellationToken ct = default);

---

### 1.2 YallaJo.SharedKernel.Application

**Purpose**: Application layer abstractions (CQRS, validation, behaviors, context)

#### 1.2.1 CQRS Messaging

**ICommand**: All commands return Result or Result<TResponse>
**ICommandHandler**: Implements IRequestHandler<TCommand, Result>
**IQuery<TResponse>**: All queries return Result<TResponse>
**IQueryHandler**: Implements IRequestHandler<TQuery, Result<TResponse>>

Key Points:
- All commands return Result or Result<T> (never throw exceptions for business logic)
- All queries return Result<T>
- Built on MediatR for in-process messaging
- Handlers are registered per-module via AddMediatR()

#### 1.2.2 Pipeline Behaviors

**ValidationBehavior**: Validates requests using FluentValidation validators
**LoggingBehavior**: Logs request start/completion with elapsed time
**PerformanceBehavior**: Warns if request takes > 500ms
**QueryCachingBehavior**: Caches queries implementing ICacheableQuery using HybridCache

Key Points:
- Behaviors are registered globally in SharedKernel.Infrastructure.DependencyInjection
- Order: Validation → Logging → Performance → QueryCaching
- FluentValidation validators are auto-discovered per module

#### 1.2.3 Context Abstractions

**ICurrentUser**: Guid? UserId, string? UserName, string? Email, bool IsAuthenticated, IEnumerable<string> Roles, IEnumerable<string> Permissions, IEnumerable<Claim> Claims
- Registered as scoped in the API host
- Extracted from ClaimsPrincipal (JWT claims)
- Used by handlers to audit/track requests

**IRequestContext**: string? IpAddress, string? UserAgent, string? DeviceName, string? AcceptLanguage, string? Platform
- Registered as scoped in the API host
- Extracted from HttpContext
- Used by Auth module to track sessions/devices

#### 1.2.4 Data Abstractions

**IDbContext**: Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
**IInboxStore**: Task<bool> HasBeenProcessedAsync(Guid messageId, CancellationToken ct = default); void MarkAsProcessed(Guid messageId);
**IDomainEventDispatcher**: Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default);

---

### 1.3 YallaJo.SharedKernel.Infrastructure

**Purpose**: Concrete implementations of domain/application abstractions

#### 1.3.1 Dependency Injection

**DependencyInjection.cs**: Called ONCE from the API host after all module registrations
- Registers global MediatR behaviors
- Sets up HybridCache for query caching
- Composite outbox processor handles all modules' outbox messages

#### 1.3.2 Unit of Work Implementation

**UnitOfWork<TContext>**: 
1. Collects domain events from all aggregate roots
2. Clears domain events from aggregates
3. Dispatches domain events BEFORE SaveChanges (handlers can write OutboxMessages to the same DbContext)
4. Persists everything atomically

Key Points:
- Domain events are dispatched before SaveChanges
- Handlers can write OutboxMessages to the same DbContext
- Single SaveChangesAsync persists both aggregate changes and outbox messages atomically
- Prevents lost events in distributed systems

#### 1.3.3 Repository Implementations

**EfRepository<TEntity, TKey>**: Composition pattern: delegates to EfReadRepository and EfWriteRepository
- Supports both generic <TEntity, TKey> and default <TEntity> (Guid key)
- EF-specific methods (ExecuteUpdateAsync) only on concrete implementations

---

### 1.4 YallaJo.SharedKernel.Presentation

**Purpose**: HTTP response mapping

**ResultExtensions.cs**: Single authoritative mapping from domain Result to HTTP responses
- Maps Result<T> to IResult (200 OK, 201 Created, or RFC 7807 Problem)
- Maps Result<T> to IResult with Location header
- Maps Result to IResult (200 OK or RFC 7807 Problem)
- Eliminates per-module ToApiResult / ToProblem copies
- RFC 7807 Problem Details format
- Imported in every Presentation project

---

## 2. WEB PROJECT (MVC + BFF PATTERN)

### 2.1 Program.cs Configuration

#### 2.1.1 Authentication (Cookie-Based)

```csharp
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/auth/login";
        options.LogoutPath       = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = SameSiteMode.Strict;
        options.Cookie.Name       = "YallaJo.Web";
    });
```

Key Points:
- HttpOnly: Cookie cannot be accessed by JavaScript (prevents XSS token theft)
- SameSite=Strict: Cookie only sent to same-site requests (prevents CSRF)
- SlidingExpiration: Extends expiry on each request (8-hour idle timeout)
- Tokens are encrypted server-side in the cookie (never exposed to browser)

#### 2.1.2 HTTP Client Configuration (BFF Pattern)

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtAuthHandler>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

// Primary typed client — goes through JwtAuthHandler to attach Bearer tokens
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
