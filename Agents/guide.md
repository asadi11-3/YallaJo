
# YallaJo Architecture — Complete Developer Guide

> **This guide is the single-source-of-truth for understanding and working with the YallaJo modular monolith.**  
> Generated from full codebase analysis on 2026-03-06.

---

## Table of Contents

### 📐 Foundations (1–3)
*Read first. Architecture, layers, and how to build features.*

1. [🏗️ Architecture Overview](#1-architecture-overview)
2. [📦 Layer-by-Layer Anatomy of a Module](#2-layer-by-layer-anatomy-of-a-module)
3. [🔧 Step-by-Step: Creating CRUD Endpoints](#3-step-by-step-creating-crud-endpoints)

### 🧩 Domain Patterns (4–7)
*Events, repositories, persistence.*

4. [📢 Domain Events — When & How](#4-domain-events--when--how)
5. [🔗 Integration Events — When & How](#5-integration-events--when--how)
6. [📖 Read Repository vs Write Repository](#6-read-repository-vs-write-repository)
7. [💾 Unit of Work — The Persistence Orchestrator](#7-unit-of-work--the-persistence-orchestrator)

### 🎯 Application Patterns (8–11)
*Result handling, queries, request flow, value objects.*

8. [✅ The Result Pattern — Error Handling Without Exceptions](#8-the-result-pattern)
9. [🔍 The Specification Pattern — Complex Queries](#9-the-specification-pattern--complex-queries)
10. [🔄 Complete Request Lifecycle](#10-complete-request-lifecycle)
11. [💎 Value Objects — When & How to Use](#11-value-objects--when--how-to-use)

### 🔩 Infrastructure & Cross-Cutting (12–16)
*EF Core, concurrency, pipeline, auth.*

12. [📚 Repository Interfaces — Complete API Reference](#12-repository-interfaces--complete-api-reference)
13. [🔒 Soft Delete & Concurrency](#13-soft-delete--concurrency)
14. [📄 PaginatedResult\<T\>](#14-paginatedresultt)
15. [⚙️ MediatR Pipeline Behaviors](#15-mediatr-pipeline-behaviors)
16. [👤 ICurrentUser & IRequestContext](#16-icurrentuser--irequestcontext)

### 🚀 Performance & Features (17–19)
*Caching, translation, quick reference.*

17. [⚡ Cheat Sheet — Quick Reference](#17-cheat-sheet--quick-reference)
18. [🌍 Translation System — Cross-Module Auto-Translation](#18-translation-system--cross-module-auto-translation)
19. [🗄️ Caching — HybridCache, Tags, Redis L2](#19-caching--hybridcache-tags-redis-l2)

---

## 🏗️ 1. Architecture Overview

*The big picture — how YallaJo is structured as a modular monolith. Read this first to understand the project layout and dependency rules before touching any code.*

YallaJo is a **Modular Monolith** built on **.NET 9** with **Clean Architecture**, **CQRS**, and **DDD** principles.

### Project Structure

Each bounded context (module) has **5 projects**:

```
{Module}.Domain/           ← Pure business logic. ZERO external dependencies.
{Module}.Application/      ← Use cases (Commands, Queries, Handlers, Validators)
{Module}.Infrastructure/   ← EF Core, repositories, DbContext, event handlers
{Module}.Presentation/     ← Minimal API endpoints
{Module}.Contracts/        ← Integration events (published for OTHER modules to consume)
```

Plus 3 shared kernel projects:

```
YallaJo.SharedKernel.Domain/          ← Base entities, value objects, interfaces
YallaJo.SharedKernel.Application/     ← CQRS interfaces, pipeline behaviors
YallaJo.SharedKernel.Infrastructure/  ← Base repositories, UoW, outbox/inbox
```

And the host:

```
YallaJo.Api/              ← Program.cs, middleware, auth, module wiring
```

### Dependency Flow (STRICT — never violate)

```
Presentation → Application → Domain ← Infrastructure
                                      ↑
                                SharedKernel
```

- Domain depends on NOTHING (except SharedKernel.Domain)
- Application depends on Domain only
- Infrastructure depends on Domain + Application
- Presentation depends on Application only
- **Infrastructure NEVER leaks into Application or Domain**

> **⚠️ Warning:** The dependency rule is strict — never add a project reference from Domain → Application, Application → Infrastructure, or Domain → Infrastructure. This is the most common architecture mistake and permanently breaks module isolation.

[↑ Back to Table of Contents](#table-of-contents)

---

## 📦 2. Layer-by-Layer Anatomy of a Module

*A tour of every layer in a module — Domain, Application, Infrastructure, Presentation, and Contracts. Use this as a reference when creating or reviewing code in any project.*

### 2.1 Domain Layer (`{Module}.Domain/`)

Contains the **heart of your business logic**. Framework-agnostic.

```
{Module}.Domain/
├── Entities/
│   ├── MyAggregate.cs          ← Aggregate root (: AuditableEntity, IAggregateRoot)
│   ├── ChildEntity.cs          ← Owned entity (: AuditableEntity)
│   └── JoinEntity.cs           ← Join table (: BaseEntity)
├── Enums/
│   └── MyStatus.cs             ← Business enumerations
├── Events/
│   └── MyAggregateCreatedEvent.cs  ← Domain events (: DomainEventBase)
├── Repositories/
│   ├── IMyAggregateRepository.cs   ← Repository interface (: IRepository<T>)
│   └── IMyModuleUnitOfWork.cs      ← Module UoW interface (: IUnitOfWork)
└── Exceptions/                      ← (optional) Module-specific domain exceptions
```

**Entity Inheritance Chain:**

```
BaseEntity<TKey>                    ← Id, CreatedAt, UpdatedAt, DomainEvents collection
  └── AuditableEntity<TKey>        ← + IsDeleted, DeletedAt, RowVersion, SoftDelete(), Restore()
       └── YourAggregate           ← + IAggregateRoot marker

BaseEntity (Guid shorthand)         ← BaseEntity<Guid> with Guid.CreateVersion7()
AuditableEntity (Guid shorthand)    ← AuditableEntity<Guid> with Guid.CreateVersion7()
```

> **💡 Tip:** Use `Guid.CreateVersion7()` instead of `Guid.NewGuid()` for time-sortable IDs — they sort in insertion order, giving better B-tree index performance. `BaseEntity` calls this automatically; never set `Id` manually.

**When to use which base class:**

| Scenario | Base Class | Interface |
|----------|-----------|-----------|
| Aggregate root (booking, user, profile) | `AuditableEntity` | `IAggregateRoot` |
| Child entity owned by aggregate (join request, email) | `AuditableEntity` | — |
| Join table (user-role, guide-language) | `BaseEntity<Guid>` or just properties | — |
| Simple reference entity (no soft-delete needed) | `BaseEntity` | — |

**Example — Aggregate Root:**

```csharp
// Booking.Domain/Entities/TourBooking.cs
public sealed class TourBooking : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid TourId { get; private set; }
    public BookingStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = null!;
    public string? ConfirmationCode { get; private set; }
    
    private readonly List<JoinRequest> _joinRequests = [];
    public IReadOnlyCollection<JoinRequest> JoinRequests => _joinRequests.AsReadOnly();

    private TourBooking() { } // EF Core constructor

    // ── Factory Method (the ONLY way to create) ──
    public static TourBooking Create(Guid userId, Guid tourId, Money price)
    {
        var booking = new TourBooking
        {
            UserId = userId,
            TourId = tourId,
            Status = BookingStatus.Pending,
            TotalPrice = price
        };
        
        // Raise domain event
        booking.AddDomainEvent(new TourBookingCreatedEvent(booking.Id, userId, tourId));
        return booking;
    }
    
    // ── Business Methods ──
    public void Confirm(string confirmationCode)
    {
        if (Status != BookingStatus.Pending)
            throw new BusinessRuleViolationException("Only pending bookings can be confirmed.");
        
        Status = BookingStatus.Confirmed;
        ConfirmationCode = confirmationCode;
        MarkUpdated();
        
        AddDomainEvent(new TourBookingConfirmedEvent(Id, confirmationCode));
    }
    
    public void Cancel(string reason)
    {
        if (Status is BookingStatus.Cancelled or BookingStatus.Completed)
            throw new BusinessRuleViolationException("Cannot cancel this booking.");
        
        Status = BookingStatus.Cancelled;
        MarkUpdated();
        
        AddDomainEvent(new TourBookingCancelledEvent(Id, reason));
    }
}
```

**Key rules for entities:**
- Private setters — state changes ONLY through methods
- Private parameterless constructor for EF Core
- Factory methods (`Create()`) for construction — raise creation events here
- Business methods for state transitions — raise change events here
- `MarkUpdated()` updates the `UpdatedAt` timestamp
- `AddDomainEvent()` queues events for dispatch (inherited from BaseEntity)

---

### 2.2 Application Layer (`{Module}.Application/`)

Contains **use cases** expressed as Commands and Queries.

```
{Module}.Application/
├── Commands/
│   ├── CreateTourBooking/
│   │   ├── CreateTourBookingCommand.cs         ← ICommand<Guid>
│   │   ├── CreateTourBookingCommandHandler.cs  ← ICommandHandler<..., Guid>
│   │   └── CreateTourBookingCommandValidator.cs ← AbstractValidator<...>
│   ├── ConfirmTourBooking/
│   │   ├── ConfirmTourBookingCommand.cs
│   │   └── ConfirmTourBookingCommandHandler.cs
│   └── CancelTourBooking/
│       └── ...
├── Queries/
│   ├── GetTourBooking/
│   │   ├── GetTourBookingQuery.cs              ← IQuery<TourBookingDto>
│   │   ├── GetTourBookingQueryHandler.cs       ← IQueryHandler<..., TourBookingDto>
│   │   └── TourBookingDto.cs                   ← Result DTO
│   └── ListTourBookings/
│       └── ...
├── EventHandlers/
│   └── SomeIntegrationEventHandler.cs          ← Consumes integration events from OTHER modules
├── DependencyInjection.cs
└── Mappings/  (optional — if using AutoMapper)
```

**Command (write operation):**

```csharp
// The command — an immutable record
public sealed record CreateTourBookingCommand(
    Guid UserId,
    Guid TourId,
    DateTime ScheduledDate,
    int ParticipantCount) : ICommand<Guid>;  // Returns Guid (the new booking ID)

// The handler
public sealed class CreateTourBookingCommandHandler(
    IBookingRepository bookingRepository,
    IBookingUnitOfWork unitOfWork)
    : ICommandHandler<CreateTourBookingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateTourBookingCommand request, CancellationToken ct)
    {
        // 1. Business validation
        var exists = await bookingRepository.AnyAsync(
            b => b.UserId == request.UserId && b.TourId == request.TourId 
                 && b.Status == BookingStatus.Pending, ct);
        
        if (exists)
            return Result.Conflict<Guid>("Duplicate booking exists.");
        
        // 2. Create aggregate via factory method
        var price = Money.Create(150m, "JOD");
        var booking = TourBooking.Create(request.UserId, request.TourId, price);
        
        // 3. Persist
        await bookingRepository.AddAsync(booking, ct);
        await unitOfWork.SaveChangesAsync(ct);  // Domain events dispatched HERE
        
        // 4. Return result
        return Result.Created(booking.Id);
    }
}

// The validator (runs BEFORE handler via ValidationBehavior pipeline)
public sealed class CreateTourBookingCommandValidator 
    : AbstractValidator<CreateTourBookingCommand>
{
    public CreateTourBookingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.ScheduledDate).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.ParticipantCount).InclusiveBetween(1, 20);
    }
}
```

**Query (read operation):**

```csharp
public sealed record GetTourBookingQuery(Guid BookingId) : IQuery<TourBookingDto>;

public sealed record TourBookingDto(
    Guid Id,
    Guid UserId,
    Guid TourId,
    string Status,
    decimal TotalPrice,
    string Currency,
    string? ConfirmationCode,
    DateTime CreatedAt);

public sealed class GetTourBookingQueryHandler(
    IBookingRepository bookingRepository)       // NO UnitOfWork — read-only!
    : IQueryHandler<GetTourBookingQuery, TourBookingDto>
{
    public async Task<Result<TourBookingDto>> Handle(
        GetTourBookingQuery request, CancellationToken ct)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, ct);
        
        if (booking is null)
            return Result.NotFound<TourBookingDto>("Booking not found.");
        
        return Result.Success(new TourBookingDto(
            booking.Id,
            booking.UserId,
            booking.TourId,
            booking.Status.ToString(),
            booking.TotalPrice.Amount,
            booking.TotalPrice.Currency,
            booking.ConfirmationCode,
            booking.CreatedAt));
    }
}
```

**Key CQRS rules:**
- Commands → `ICommand` or `ICommand<TResponse>` → returns `Result` or `Result<T>`
- Queries → `IQuery<TResponse>` → returns `Result<T>`
- Command handlers inject `IXxxRepository` + `IXxxUnitOfWork`
- Query handlers inject `IXxxRepository` ONLY (no UnitOfWork needed)
- `unitOfWork.SaveChangesAsync()` is called at handler level, NOT middleware
- Validators are auto-discovered and run via `ValidationBehavior` pipeline

> **📌 Rule:** Query handlers must NOT inject `IUnitOfWork`. They are read-only — if you find yourself needing to save inside a query handler, you have a design problem.

---

### 2.3 Infrastructure Layer (`{Module}.Infrastructure/`)

Contains **EF Core implementation** of domain abstractions.

```
{Module}.Infrastructure/
├── Persistence/
│   ├── BookingDbContext.cs                    ← DbContext with schema
│   ├── BookingUnitOfWork.cs                  ← Module-specific UoW wrapper
│   ├── Configurations/
│   │   ├── TourBookingConfiguration.cs       ← IEntityTypeConfiguration<T>
│   │   └── ...
│   └── Seeding/
│       └── BookingDbInitializer.cs           ← Seed data
├── Repositories/
│   └── BookingRepository.cs                  ← Concrete repository
├── EventHandlers/
│   └── TourBookingCreatedDomainEventHandler.cs  ← Domain event → Outbox
├── Migrations/
│   └── ...
└── DependencyInjection.cs
```

**DbContext:**

```csharp
public sealed class BookingDbContext : DbContext, IDbContext
{
    public DbSet<TourBooking> TourBookings => Set<TourBooking>();
    public DbSet<TourGuide> TourGuides => Set<TourGuide>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("booking");   // Module-isolated schema
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}
```

**Entity Configuration:**

```csharp
public sealed class TourBookingConfiguration : IEntityTypeConfiguration<TourBooking>
{
    public void Configure(EntityTypeBuilder<TourBooking> builder)
    {
        builder.HasKey(b => b.Id);
        
        // Value Object mapping (owned type)
        builder.OwnsOne(b => b.TotalPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("TotalCurrency").HasMaxLength(3);
        });
        
        // Indexes
        builder.HasIndex(b => b.ConfirmationCode).IsUnique()
               .HasFilter("[ConfirmationCode] IS NOT NULL");
        builder.HasIndex(b => new { b.UserId, b.ScheduledDate });
        
        // Enum conversion
        builder.Property(b => b.Status)
               .HasConversion<string>()
               .HasMaxLength(20);
        
        // Relationships
        builder.HasMany(b => b.JoinRequests)
               .WithOne()
               .HasForeignKey(j => j.TourBookingId)
               .OnDelete(DeleteBehavior.Restrict);
        
        // Soft delete query filter
        builder.HasQueryFilter(b => !b.IsDeleted);
        
        // Concurrency token
        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}
```

**Module-Specific UnitOfWork:**

```csharp
// Domain interface
public interface IBookingUnitOfWork : IUnitOfWork { }

// Infrastructure implementation — delegates to generic UnitOfWork<TContext>
public sealed class BookingUnitOfWork(IUnitOfWork<BookingDbContext> inner) : IBookingUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) 
        => inner.SaveChangesAsync(ct);
}
```

**Concrete Repository:**

```csharp
public sealed class BookingRepository(BookingDbContext context)
    : EfRepository<TourBooking, Guid>(context), IBookingRepository
{
    // Custom queries beyond base CRUD
    public async Task<TourBooking?> GetByIdWithJoinRequestsAsync(Guid id, CancellationToken ct)
        => await context.TourBookings
            .Include(b => b.JoinRequests)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    
    public async Task<List<TourBooking>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        => await context.TourBookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
}
```

**DependencyInjection.cs:**

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddBookingInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string.");
        
        // DbContext
        services.AddDbContext<BookingDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "booking");
                sql.EnableRetryOnFailure(3);
            }));
        
        // Unit of Work
        services.AddScoped<IUnitOfWork<BookingDbContext>, UnitOfWork<BookingDbContext>>();
        services.AddScoped<IBookingUnitOfWork, BookingUnitOfWork>();
        
        // Repositories
        services.AddScoped<IBookingRepository, BookingRepository>();
        
        // Event handlers & outbox
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<IOutboxProcessor, OutboxProcessor<BookingDbContext>>();
        
        // Inbox (for consuming integration events from other modules)
        services.AddScoped<IInboxStore, EfInboxStore<BookingDbContext>>();
        
        // Seeding
        services.AddScoped<IModuleDbInitializer, BookingDbInitializer>();
        
        return services;
    }
}
```

---

### 2.4 Presentation Layer (`{Module}.Presentation/`)

**Minimal API endpoints** — one static class per module.

```csharp
using Booking.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/bookings")
            .WithTags("Bookings");

        // ── POST /api/v1/bookings ────────────────────────────
        group.MapPost("/", CreateBooking)
            .WithName("CreateBooking")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Create))
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // ── GET /api/v1/bookings/{id} ────────────────────────
        group.MapGet("/{id:guid}", GetBooking)
            .WithName("GetBooking")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Read))
            .Produces<TourBookingDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ── GET /api/v1/bookings ─────────────────────────────
        group.MapGet("/", ListBookings)
            .WithName("ListBookings")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Read))
            .Produces<PaginatedResult<TourBookingDto>>();

        // ── PUT /api/v1/bookings/{id} ────────────────────────
        group.MapPut("/{id:guid}", UpdateBooking)
            .WithName("UpdateBooking")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Update))
            .Produces<TourBookingDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)  // concurrency
            .ProducesValidationProblem();

        // ── PATCH /api/v1/bookings/{id}/confirm ─────────────
        group.MapPatch("/{id:guid}/confirm", ConfirmBooking)
            .WithName("ConfirmBooking")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Approve))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ── DELETE /api/v1/bookings/{id} ────────────────────
        group.MapDelete("/{id:guid}", CancelBooking)
            .WithName("CancelBooking")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.Booking, AppAction.Delete))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    // ── Endpoint Implementations ────────────────────────────

    private static async Task<IResult> CreateBooking(
        CreateBookingRequest request, ISender sender, CancellationToken ct)
    {
        var command = new CreateTourBookingCommand(
            request.UserId, request.TourId, 
            request.ScheduledDate, request.ParticipantCount);
        
        var result = await sender.Send(command, ct);
        
        return result.IsSuccess
            ? Results.Created($"/api/bookings/{result.Value}", result.Value)
            : ToProblem(result);
    }

    private static async Task<IResult> GetBooking(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetTourBookingQuery(id), ct);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result);
    }

    private static async Task<IResult> ListBookings(
        [AsParameters] ListBookingsRequest request, ISender sender, CancellationToken ct)
    {
        var query = new ListTourBookingsQuery(request.Page, request.PageSize);
        var result = await sender.Send(query, ct);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result);
    }

    private static async Task<IResult> UpdateBooking(
        Guid id, UpdateBookingRequest request, ISender sender, CancellationToken ct)
    {
        var command = new UpdateTourBookingCommand(id, request.ScheduledDate, request.ParticipantCount);
        var result = await sender.Send(command, ct);
        
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result);
    }

    private static async Task<IResult> ConfirmBooking(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ConfirmTourBookingCommand(id), ct);
        
        return result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result);
    }

    private static async Task<IResult> CancelBooking(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTourBookingCommand(id), ct);
        
        return result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result);
    }

    // ── Result → HTTP Response Mapping ──────────────────────

    private static IResult ToProblem<T>(Result<T> result) =>
        Results.Problem(
            statusCode: (int)result.Outcome,
            title: result.Errors.FirstOrDefault()?.Code,
            detail: result.Errors.FirstOrDefault()?.Message);
    
    private static IResult ToProblem(Result result) =>
        Results.Problem(
            statusCode: (int)result.Outcome,
            title: result.Errors.FirstOrDefault()?.Code,
            detail: result.Errors.FirstOrDefault()?.Message);
}

// Request DTOs (separate from Commands — endpoint concerns only)
public sealed record CreateBookingRequest(
    Guid UserId, Guid TourId, DateTime ScheduledDate, int ParticipantCount);

public sealed record UpdateBookingRequest(
    DateTime? ScheduledDate, int? ParticipantCount);

public sealed record ListBookingsRequest(int Page = 1, int PageSize = 20);
```

**Key endpoint rules:**
- Extension method on `IEndpointRouteBuilder` — `Map{Module}Endpoints()`
- Use `MapGroup()` for shared prefix + tags
- Each endpoint → `ISender.Send(Command/Query)` → `Result<T>` → `IResult`
- Request DTOs are **separate** from Commands (decoupled API shape from use case)
- `ToProblem()` / `result.ToApiResult()` converts `Result.Outcome` (enum maps to HTTP status code) to `ProblemDetails`
- Auth (protected): `.WithMetadata(new MustHavePermissionAttribute({Module}Features.X, AppAction.Y))`
- Auth (public): `.AllowAnonymous()`
- Rate limiting: `.RequireRateLimiting(RateLimitPolicies.LoginPolicy)`

> **📌 Rule 1 (non-negotiable):** Every endpoint MUST have EITHER `.WithMetadata(new MustHavePermissionAttribute(...))` OR `.AllowAnonymous()`. See `agent-context.md §2.1`.
>
> **Forbidden**:
> - `.RequireAuthorization()` alone — "any authenticated user" is almost never what you mean; use a permission.
> - `.RequireAuthorization("Permission.X.Y")` — string-based policies bypass the attribute and the `PermissionSeeder` won't find the permission. Use `MustHavePermissionAttribute` so the permission is declared in the module's `IPermissionCatalog` and auto-seeded.
> - No decoration at all — the framework will NOT reject anonymous requests by default.

---

### 2.5 Contracts Layer (`{Module}.Contracts/`)

The module's **public surface**. Contains exactly two things: integration events and the authorization catalog.

```
{Module}.Contracts/
├── IntegrationEvents/
│   ├── TourBookingCreatedIntegrationEvent.cs
│   ├── TourBookingConfirmedIntegrationEvent.cs
│   └── TourBookingCancelledIntegrationEvent.cs
└── Authorization/
    ├── {Module}Features.cs            ← feature string constants OWNED by this module
    └── {Module}PermissionCatalog.cs   ← implements IPermissionCatalog
```

#### Integration Events

```csharp
public sealed record TourBookingCreatedIntegrationEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId,
    DateTime ScheduledDate,
    int ParticipantCount) : IntegrationEventBase;
```

#### Authorization Catalog (required for every module with endpoints)

**Features constants** — one file per module. Other modules MUST NOT add constants here.

```csharp
// Booking.Contracts/Authorization/BookingFeatures.cs
namespace Booking.Contracts.Authorization;

public static class BookingFeatures
{
    public const string Booking      = nameof(Booking);
    public const string Refund       = nameof(Refund);
    public const string Availability = nameof(Availability);
}
```

**Permission catalog** — lists every permission this module contributes:

```csharp
// Booking.Contracts/Authorization/BookingPermissionCatalog.cs
using YallaJo.SharedKernel.Application.Authorization;

namespace Booking.Contracts.Authorization;

public sealed class BookingPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Booking";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(BookingFeatures.Booking, AppAction.Read,    PermissionGroup.BookingOperations, "View bookings"),
        new(BookingFeatures.Booking, AppAction.Create,  PermissionGroup.BookingOperations, "Create a booking"),
        new(BookingFeatures.Booking, AppAction.Update,  PermissionGroup.BookingOperations, "Update a booking"),
        new(BookingFeatures.Booking, AppAction.Approve, PermissionGroup.BookingOperations, "Approve a pending booking"),
        new(BookingFeatures.Booking, AppAction.Reject,  PermissionGroup.BookingOperations, "Reject a booking"),
        new(BookingFeatures.Booking, AppAction.Delete,  PermissionGroup.BookingOperations, "Cancel a booking"),
    ];
}
```

**DI registration** — goes in `{Module}.Infrastructure/DependencyInjection.cs` (NOT in Contracts, to keep Contracts DI-free):

```csharp
services.AddSingleton<IPermissionCatalog, BookingPermissionCatalog>();
```

`PermissionSeeder` in `Security.Infrastructure` auto-discovers every registered `IPermissionCatalog` and seeds the database at startup. **Adding a new module requires zero changes to Security** — it just registers its catalog and the permissions appear.

**Rules:**
- ONLY integration events + authorization catalog + shared DTOs that OTHER modules need to reference
- Other modules add a project reference to `{Module}.Contracts` — NEVER to `{Module}.Domain` or `{Module}.Application`
- Integration events inherit from `IntegrationEventBase` (which implements `IIntegrationEvent`)
- **NEVER add another module's features to your `{Module}Features.cs`** — the boundary is strict
- See `agent-context.md §4.2` for the full "How to Add a New Permission" walkthrough
- See `authorization-refactor-plan.md` for the complete authorization architecture

[↑ Back to Table of Contents](#table-of-contents)

---

## 🔧 3. Step-by-Step: Creating CRUD Endpoints

*A concrete end-to-end walkthrough for building a new feature from scratch. Follow these steps when scaffolding any new entity with full CRUD support.*

### Step 1: Define the Domain Entity

In `{Module}.Domain/Entities/`:

```csharp
public sealed class Tour : AuditableEntity, IAggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    
    private Tour() { }
    
    public static Tour Create(string title, string description, Money price)
    {
        var tour = new Tour { Title = title, Description = description, Price = price };
        tour.AddDomainEvent(new TourCreatedEvent(tour.Id, title));
        return tour;
    }
    
    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
        MarkUpdated();
    }
}
```

### Step 2: Define Repository & UoW Interfaces

In `{Module}.Domain/Repositories/`:

```csharp
public interface ITourRepository : IRepository<Tour> 
{
    // Add custom queries beyond base CRUD if needed
    Task<Tour?> GetByTitleAsync(string title, CancellationToken ct);
}

public interface ITourUnitOfWork : IUnitOfWork { }
```

### Step 3: Create Commands & Queries

In `{Module}.Application/Commands/CreateTour/`:

```csharp
// Command
public sealed record CreateTourCommand(
    string Title, string Description, decimal Price, string Currency) : ICommand<Guid>;

// Handler
public sealed class CreateTourCommandHandler(
    ITourRepository tourRepository,
    ITourUnitOfWork unitOfWork)
    : ICommandHandler<CreateTourCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTourCommand request, CancellationToken ct)
    {
        var tour = Tour.Create(request.Title, request.Description, 
            Money.Create(request.Price, request.Currency));
        
        await tourRepository.AddAsync(tour, ct);
        await unitOfWork.SaveChangesAsync(ct);
        
        return Result.Created(tour.Id);
    }
}

// Validator
public sealed class CreateTourCommandValidator : AbstractValidator<CreateTourCommand>
{
    public CreateTourCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
```

Repeat for **Update**, **Delete** (or soft-delete), **Get**, **List**.

### Step 4: Implement Infrastructure

**Repository** in `{Module}.Infrastructure/Repositories/`:

```csharp
public sealed class TourRepository(TourDbContext context)
    : EfRepository<Tour, Guid>(context), ITourRepository
{
    public async Task<Tour?> GetByTitleAsync(string title, CancellationToken ct)
        => await context.Tours.FirstOrDefaultAsync(t => t.Title == title, ct);
}
```

**UnitOfWork** in `{Module}.Infrastructure/Persistence/`:

```csharp
public sealed class TourUnitOfWork(IUnitOfWork<TourDbContext> inner) : ITourUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct) => inner.SaveChangesAsync(ct);
}
```

**DbContext + Configuration** in `{Module}.Infrastructure/Persistence/`.

**Register in DI** in `{Module}.Infrastructure/DependencyInjection.cs`:

```csharp
public static IServiceCollection AddTourInfrastructure(
    this IServiceCollection services, IConfiguration config)
{
    // DbContext, UoW, repos, event handlers, outbox/inbox, seeders...
    services.AddScoped<ITourRepository, TourRepository>();
    services.AddScoped<ITourUnitOfWork, TourUnitOfWork>();

    // ── Permission catalog (discovered by Security's PermissionSeeder) ──
    services.AddSingleton<IPermissionCatalog, TourPermissionCatalog>();

    return services;
}
```

### Step 5: Define the Permission Catalog

In `{Module}.Contracts/Authorization/`:

```csharp
// TourFeatures.cs
namespace Tour.Contracts.Authorization;

public static class TourFeatures
{
    public const string Tour    = nameof(Tour);
    public const string Pricing = nameof(Pricing);
}

// TourPermissionCatalog.cs
using YallaJo.SharedKernel.Application.Authorization;

namespace Tour.Contracts.Authorization;

public sealed class TourPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Tour";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(TourFeatures.Tour, AppAction.Read,   PermissionGroup.ContentManagement, "View tours"),
        new(TourFeatures.Tour, AppAction.Create, PermissionGroup.ContentManagement, "Create a tour"),
        new(TourFeatures.Tour, AppAction.Update, PermissionGroup.ContentManagement, "Update a tour"),
        new(TourFeatures.Tour, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a tour"),
    ];
}
```

### Step 6: Wire Endpoints

In `{Module}.Presentation/TourEndpoints.cs`:

```csharp
using Tour.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;

public static IEndpointRouteBuilder MapTourEndpoints(this IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapGroup("/api/v1/tours").WithTags("Tours");

    group.MapPost("/", CreateTour)
        .WithName("CreateTour")
        .WithMetadata(new MustHavePermissionAttribute(TourFeatures.Tour, AppAction.Create))
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

    group.MapGet("/{id:guid}", GetTour)
        .WithName("GetTour")
        .WithMetadata(new MustHavePermissionAttribute(TourFeatures.Tour, AppAction.Read))
        .Produces<TourDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

    // ... rest of CRUD endpoints, each with its own MustHavePermission attribute

    return endpoints;
}
```

> **📌 Rule 1 check**: Every endpoint must have `.WithMetadata(new MustHavePermissionAttribute(...))` OR `.AllowAnonymous()`. No exceptions. See `agent-context.md §2.1`.

### Step 7: Register in Program.cs

```csharp
// In YallaJo.Api/Program.cs
builder.Services.AddTourApplication();
builder.Services.AddTourInfrastructure(builder.Configuration);
// ...
app.MapTourEndpoints();
```

**Verify seed**: start the app and check the startup log:

```
[INFO] Seeding 64 permissions from 5 modules: Security, ContentCore, ContentPlaces, Accounts, Tour
```

If your module isn't in that list, the catalog wasn't registered in DI. Fix `DependencyInjection.cs`.

[↑ Back to Table of Contents](#table-of-contents)

---

## 📢 4. Domain Events — When & How

*In-module, in-transaction side effects. Raised by aggregates, dispatched by `UnitOfWork<TContext>` BEFORE `SaveChangesAsync`, handlers may mutate the same DbContext so the aggregate + handler changes commit atomically.*

### 4.1 What Are They?

A domain event says **"something business-significant happened inside this module"**. Another piece of the same module (translation cache, audit log, read model, outbox writer) needs to react — **but only if the main change actually succeeds**.

**Guarantees**:
- Synchronous, in-process
- Same database transaction as the aggregate that raised it
- Handler's DbContext changes commit atomically with the aggregate
- If any handler throws → the whole transaction rolls back (nothing is saved)
- Events are cleared from aggregates **before** dispatch (prevents infinite recursion)

### 4.2 When to Raise a Domain Event

| Trigger | Example event |
|---|---|
| Aggregate created via factory | `CategoryCreatedDomainEvent`, `UserCreatedEvent` |
| State transition | `BookingConfirmedDomainEvent`, `LanguageActivatedDomainEvent` |
| Business-significant change | `PriceChangedDomainEvent`, `EmailVerifiedDomainEvent` |
| Cross-module notification needed | Any event that must produce an outbox row |

**Do NOT raise a domain event for**:
- Simple field updates (use `MarkUpdated()` instead)
- Read/query operations
- Validation failures (those are `Result.Invalid`, not events)
- Purely technical concerns (logging, metrics — use pipeline behaviors)

### 4.3 The Five Moving Parts

```
┌──────────────────────────────────────────────────────────┐
│ Domain Layer (zero external deps)                        │
│                                                          │
│  IDomainEvent (marker)                                   │
│     │                                                    │
│     └── DomainEventBase (abstract record: EventId,       │
│         │                OccurredOn)                     │
│         │                                                │
│         └── TagCreatedDomainEvent (concrete)             │
│                                                          │
│  BaseEntity                                              │
│     ├── _domainEvents: List<IDomainEvent>                │
│     ├── DomainEvents: IReadOnlyCollection<IDomainEvent>  │
│     ├── protected AddDomainEvent(IDomainEvent)           │
│     └── ClearDomainEvents()                              │
└──────────────────────────────────────────────────────────┘
                          ▼
┌──────────────────────────────────────────────────────────┐
│ Application Layer                                        │
│                                                          │
│  DomainEventNotification<TEvent> : INotification         │
│    └── wraps the domain event for MediatR               │
└──────────────────────────────────────────────────────────┘
                          ▼
┌──────────────────────────────────────────────────────────┐
│ Infrastructure Layer                                     │
│                                                          │
│  UnitOfWork<TContext> ─── collects, clears, dispatches,  │
│                           saves — in that exact order    │
│                                                          │
│  MediatRDomainEventDispatcher ─── wraps each event       │
│                                    and calls IMediator   │
│                                    .Publish              │
│                                                          │
│  Handlers: INotificationHandler<DomainEventNotification<T>>│
└──────────────────────────────────────────────────────────┘
```

### 4.4 The Dispatch Pipeline (annotated)

**`UnitOfWork<TContext>.SaveChangesAsync`** — the beating heart. 35 lines. Read it twice.

```csharp
// YallaJo.SharedKernel.Infrastructure/Data/UnitOfWork.cs
public async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    // 1. Collect aggregates with pending events
    //    - ONLY IAggregateRoot entries are scanned. BaseEntity children are ignored.
    //    - This is why you MUST mark your aggregate with IAggregateRoot.
    var aggregates = context.ChangeTracker
        .Entries<IAggregateRoot>()
        .Where(e => e.Entity.DomainEvents.Count > 0)
        .Select(e => e.Entity)
        .ToList();

    // 2. Snapshot all events across all aggregates
    var domainEvents = aggregates
        .SelectMany(a => a.DomainEvents)
        .ToList();

    // 3. Clear events from aggregates BEFORE dispatch
    //    - Prevents infinite recursion if a handler triggers another SaveChanges
    //    - If a handler raises NEW events on the same aggregate, they accumulate
    //      in a fresh (empty) list and would be picked up by a subsequent save
    foreach (var aggregate in aggregates)
        aggregate.ClearDomainEvents();

    // 4. Dispatch BEFORE SaveChanges
    //    - Handlers can write OutboxMessages / translations / audit logs to the
    //      same DbContext
    //    - If a handler throws, we never reach step 5 → aggregate is not saved
    //    - If a handler succeeds, its changes piggyback on the aggregate's commit
    if (domainEvents.Count > 0)
        await dispatcher.DispatchAsync(domainEvents, ct);

    // 5. ONE commit: aggregate changes + handler changes (outbox/translations/audit)
    return await context.SaveChangesAsync(ct);
}
```

**`MediatRDomainEventDispatcher`** — 24 lines. Wraps each domain event in a `DomainEventNotification<T>` and publishes via MediatR.

```csharp
// YallaJo.SharedKernel.Infrastructure/Events/MediatRDomainEventDispatcher.cs
public sealed class MediatRDomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            // Runtime generic construction — required because the event type
            // is known only at dispatch time (not at compile time).
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)!;

            // MediatR's default ForeachAwaitPublisher runs handlers sequentially
            // and SHORT-CIRCUITS on the first exception (see §4.8).
            await mediator.Publish((INotification)notification, ct);
        }
    }
}
```

**`DomainEventNotification<T>`** — just a wrapper so MediatR sees an `INotification`:

```csharp
// YallaJo.SharedKernel.Application/Abstractions/Messaging/DomainEventNotification.cs
public sealed record DomainEventNotification<TEvent>(TEvent Event) : INotification
    where TEvent : IDomainEvent;
```

> **Why the wrapper?** MediatR requires `INotification`. `IDomainEvent` stays in the Domain layer (no MediatR dependency). The wrapper is added at the Application boundary. Handlers implement `INotificationHandler<DomainEventNotification<TourBookingCreatedEvent>>` — the generic parameter carries the strong type.

### 4.5 Writing a Domain Event Handler

**Step 1** — Define the event in `{Module}.Domain/Events/`:

```csharp
public sealed record TourBookingCreatedDomainEvent(
    Guid BookingId,
    Guid UserId,
    Guid TourId) : DomainEventBase;
```

**Step 2** — Raise it from the aggregate (factory method or state transition):

```csharp
public static TourBooking Create(Guid userId, Guid tourId, Money price)
{
    var booking = new TourBooking { /* ... */ };
    booking.AddDomainEvent(new TourBookingCreatedDomainEvent(booking.Id, userId, tourId));
    return booking;
}
```

**Step 3** — Handle it in `{Module}.Infrastructure/EventHandlers/`:

```csharp
public sealed class TourBookingCreatedDomainEventHandler(
    BookingDbContext dbContext,
    ILogger<TourBookingCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourBookingCreatedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourBookingCreatedDomainEvent> notification,
        CancellationToken ct)
    {
        var e = notification.Event;

        logger.LogInformation(
            "Handling TourBookingCreated for booking {BookingId}, writing integration event to outbox",
            e.BookingId);

        // Convert domain event → integration event → outbox row
        var integrationEvent = new TourBookingCreatedIntegrationEvent(
            e.BookingId, e.UserId, e.TourId, DateTime.UtcNow, 1);

        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        // ⚠️ NO SaveChangesAsync here. UnitOfWork will commit atomically.
        return Task.CompletedTask;
    }
}
```

### 4.6 Handler Requirements (MANDATORY)

| Rule | Why |
|---|---|
| **Never call `SaveChangesAsync`** | UoW commits atomically at step 5. Calling Save in the handler either double-saves (corrupting transaction state) or fails because your handler's changes are already buffered by EF. |
| **Only mutate the same `DbContext`** | Writing to a *different* module's DbContext breaks atomicity — that's what integration events are for. |
| **Never cross module boundaries** | Domain events are in-module. If you need to notify another module, emit an `IntegrationEvent` via the outbox (§5). |
| **Inject `ILogger<THandler>`** | Mandatory for every handler. |
| **Handle errors appropriately** | If the handler throws, the aggregate is NOT saved. That may or may not be what you want (see §4.8). |
| **Keep handlers fast** | They run synchronously inside the request path. Heavy work → offload to a background service via `Channel<T>`. |

### 4.7 The "BEFORE vs AFTER SaveChanges" Decision

YallaJo dispatches domain events **BEFORE** `SaveChangesAsync`. This matches:

| Template / Author | Timing | Reason |
|---|---|---|
| **YallaJo** | BEFORE | Atomic commit of aggregate + outbox row |
| [eShop (Microsoft)](https://github.com/dotnet/eShop/blob/main/src/Ordering.Infrastructure/OrderingContext.cs) | BEFORE | Handler DbContext mutations join the same transaction |
| [Jason Taylor CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture/blob/main/src/Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs) | BEFORE | Via `SavingChangesAsync` interceptor |
| [Kamil Grzybek modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) | BEFORE | "Command Handler defines the transaction boundary" |
| Jimmy Bogard (2014) | BEFORE | "Just before we commit our transaction" |
| [Milan Jovanović](https://www.milanjovanovic.tech/blog/how-to-use-ef-core-interceptors) | AFTER (with Outbox) | Eventual consistency — pair with Outbox pattern |

**The BEFORE choice gives us**:
- Atomic outbox (aggregate + outbox row commit together → at-least-once delivery)
- Simple failure semantics (one transaction, one rollback)
- Handler DbContext mutations persist with the aggregate

**The cost**: a handler that calls an external service (email, HTTP) synchronously will block the request AND its failure will roll back the aggregate. Don't do that. External calls go in an **integration event handler** (§5), consumed out-of-band.

### 4.8 Failure Semantics

MediatR's default publisher (`ForeachAwaitPublisher`) invokes handlers **sequentially** and **short-circuits on the first exception**. YallaJo inherits this behavior for domain events.

| Scenario | Behavior |
|---|---|
| Handler #1 throws | Handler #2 never runs. SaveChanges never runs. Aggregate NOT saved. |
| Handler raises a new domain event on the same aggregate | Event is queued in the (now empty) events list. Current `SaveChangesAsync` does NOT re-dispatch. The event is raised on the next save or lost if there isn't one. ⚠️ |
| Handler calls SaveChanges (anti-pattern) | Partial commit — aggregate saved without outbox row. Broken atomicity. **Do not do this.** |
| SaveChanges throws (concurrency, unique constraint) | All handler mutations are rolled back. Aggregate not saved. Outbox row not written. Caller receives `DbUpdateException`. |

**If you need handler failure isolation** (one handler's failure should not abort the whole save), do the work via an integration event instead — the outbox processor isolates handlers per-message.

### 4.9 Concrete End-to-End Example

Scenario: user creates a tag. Translation pipeline must translate the name. Outbox row must be written so Analytics module can increment tag-creation metric.

```csharp
// 1. Command handler
public async Task<Result<Guid>> Handle(CreateTagCommand cmd, CancellationToken ct)
{
    var tag = Tag.Create(cmd.Name, cmd.Slug);           // raises TagCreatedDomainEvent
    await tagRepository.AddAsync(tag, ct);
    await unitOfWork.SaveChangesAsync(ct);              // ← triggers the pipeline below
    return Result<Guid>.Created(tag.Id);
}

// 2. UnitOfWork.SaveChangesAsync runs:
//    a. Collects [TagCreatedDomainEvent]
//    b. Clears tag._domainEvents
//    c. Dispatches via MediatR
//    d. MediatR finds two handlers:
//       - TagCreatedTranslationHandler  (runs first — adds TagTranslation rows)
//       - TagCreatedOutboxHandler       (runs second — adds OutboxMessage row)
//    e. Both handlers complete successfully
//    f. context.SaveChangesAsync() commits: Tag + TagTranslations + OutboxMessage
//       → ONE transaction, one commit, one rollback guarantee

// 3. (Async, later) CompositeOutboxProcessor picks up the OutboxMessage (§5)
//    → publishes TagCreatedIntegrationEvent
//    → Analytics module consumes it, increments metric
```

If any of steps a–f fails → nothing is saved → caller receives an error → safe to retry.

### 4.10 Anti-Patterns

```csharp
// ❌ Raising from outside an aggregate method
var booking = new TourBooking { UserId = userId };
booking.AddDomainEvent(new TourBookingCreatedDomainEvent(...)); // compiler error
                                                                  // AddDomainEvent is protected

// ✅ Raise inside factory / state method
booking.AddDomainEvent is called INSIDE Tour.Create(...) or Tour.Confirm(...)

// ────────────────────────────────────────────────────────────────

// ❌ Calling SaveChanges in a handler (breaks atomicity)
public async Task Handle(DomainEventNotification<TagCreatedEvent> n, CancellationToken ct)
{
    dbContext.Something.Add(...);
    await dbContext.SaveChangesAsync(ct);  // ❌ NO — UoW does this
}

// ❌ Crossing module boundaries in a domain event handler
public async Task Handle(DomainEventNotification<TagCreatedEvent> n, CancellationToken ct)
{
    accountsDbContext.Profiles.Add(...);  // ❌ different module's DbContext
    // Use an integration event instead (§5)
}

// ❌ Using a domain event for data-only transport (use the command itself)
aggregate.AddDomainEvent(new UserTypedALetterDomainEvent(letter));  // ❌ not a business fact

// ❌ Raising an event on BaseEntity (not IAggregateRoot)
public sealed class TagTranslation : BaseEntity { /* no IAggregateRoot */ }
translation.AddDomainEvent(new Something());  // ❌ UoW never scans non-aggregate entries — handler never runs, no error logged

// ❌ Calling state method without state guard (duplicate event)
language.Activate();  // If already active → raises LanguageActivatedEvent again
                      // → duplicate outbox row → duplicate integration event
                      // → re-translates ALL content (expensive)
// ✅ FIX:
if (!language.IsActive) language.Activate();
```

### 4.11 Testing Domain Events

```csharp
[Fact]
public void Create_Raises_TagCreatedDomainEvent()
{
    // Arrange & Act
    var tag = Tag.Create("Food", "food");

    // Assert — events observable via the IReadOnlyCollection exposed by BaseEntity
    var domainEvent = tag.DomainEvents
        .OfType<TagCreatedDomainEvent>()
        .SingleOrDefault();

    domainEvent.Should().NotBeNull();
    domainEvent!.Name.Should().Be("Food");
}

[Fact]
public void ClearDomainEvents_Empties_The_Collection()
{
    var tag = Tag.Create("Food", "food");
    tag.ClearDomainEvents();
    tag.DomainEvents.Should().BeEmpty();
}
```

Full UoW + handler flow can be tested via in-memory DbContext + NSubstitute-mocked `IDomainEventDispatcher`. See `tests/ContentCore.Tests.Unit` for working examples.

[↑ Back to Table of Contents](#table-of-contents)

---

## 🔗 5. Integration Events — Outbox / Inbox Pattern

*Cross-module asynchronous communication via the transactional outbox. Events are persisted as rows in the publisher's DbContext (same transaction as the aggregate change), then dispatched out-of-band by a background processor. Consumers use an inbox table for idempotency.*

### 5.1 Why the Outbox Pattern?

Without it you have the **dual-write problem**:
```
await dbContext.SaveChangesAsync(ct);  // ← commits the user row
await messageBus.PublishAsync(...);    // ← crashes: user saved but event lost
```
Or the reverse:
```
await messageBus.PublishAsync(...);    // ← event sent
await dbContext.SaveChangesAsync(ct);  // ← crashes: event sent but user NOT saved
```
Both scenarios leave the system in an inconsistent state.

**Outbox fixes this** by writing the event as a row in the **same DbContext** as the aggregate. One `SaveChangesAsync`, one transaction, atomic: either the aggregate AND the outbox row commit, or neither does. A background processor then picks up committed outbox rows and dispatches them to handlers. Loss is impossible; duplicates are possible but handled by the inbox.

### 5.2 When to Publish an Integration Event

| Scenario | Example |
|---|---|
| Other module must react | User created (Security) → Accounts creates profile |
| Cross-module data sync | Email verified (Auth) → Security updates claim |
| Audit trail | Significant action → Analytics writes audit log |
| External notification | Booking confirmed → Messaging sends email/SMS |
| Async heavy work | Tag created → Translation orchestrator translates name |

**Do NOT publish an integration event for**:
- Side effects within the same module (use a domain event)
- UI read-model projections (use EF `SavingChangesInterceptor` or a dedicated read repo)
- Validation errors / expected business failures (those are `Result.Failure`, not events)

### 5.3 The Eight Moving Parts

```
Publishing Module                     SharedKernel.Infrastructure            Consuming Module
─────────────────                     ──────────────────────────             ────────────────
1. IIntegrationEvent (marker)    ──►                                    
2. IntegrationEventBase          ──►  3. OutboxMessage (row schema)
   (EventId, OccurredOn)             
                                      4. OutboxProcessor<TContext>      
                                         - batch 20, lock 5min,
                                         - max retry 10
                                      5. CompositeOutboxProcessor
                                         (10s background loop)
                                      6. IntegrationEventNotification<T>
                                         (wrapper with MessageId)
                                                                          ◄── 7. INotificationHandler
                                                                                 <IntegrationEventNotification<T>>
                                                                          ◄── 8. IInboxStore / InboxMessage
                                                                                 (idempotency)
```

### 5.4 Outbox Schema — `OutboxMessage`

```csharp
// YallaJo.SharedKernel.Infrastructure/Outbox/OutboxMessage.cs
public sealed class OutboxMessage
{
    public Guid      Id              { get; }  // Guid.CreateVersion7() — time-sortable
    public string    Type            { get; }  // AssemblyQualifiedName of the event type
    public string    Content         { get; }  // JSON-serialized event payload
    public DateTime  OccurredOnUtc   { get; }  // Event creation timestamp
    public DateTime? ProcessedOnUtc  { get; }  // null = pending; set = success
    public string?   Error           { get; }  // last-failure aggregated error (truncated to 4000 chars)
    public int       RetryCount      { get; }  // 0..10 (10 = dead-letter)
    public DateTime? LockedUntil     { get; }  // distributed lock — prevents double-processing
}
```

**Each module's DbContext has its own `OutboxMessages` table** in its own schema (`security.OutboxMessages`, `auth.OutboxMessages`, etc.). This is deliberate — it keeps the transactional atomicity per-module.

### 5.5 Publishing an Integration Event

**Step 1** — Define the event in `{Module}.Contracts/IntegrationEvents/`:

```csharp
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : IntegrationEventBase;
```

`IntegrationEventBase` auto-populates `EventId = Guid.CreateVersion7()` and `OccurredOn = DateTime.UtcNow`.

**Step 2** — Write it to the outbox from the **domain event handler** of the publishing module:

```csharp
// Security.Infrastructure/EventHandlers/UserCreatedDomainEventHandler.cs
public sealed class UserCreatedDomainEventHandler(
    SecurityDbContext dbContext,
    ILogger<UserCreatedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserCreatedEvent>>
{
    public Task Handle(
        DomainEventNotification<UserCreatedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.Event;

        var integrationEvent = new UserCreatedIntegrationEvent(
            e.UserId, e.Email, e.FirstName, e.LastName);

        // OutboxMessage.Create serializes the event to JSON with AssemblyQualifiedName type info
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));

        // ⚠️ NO SaveChanges — UoW commits atomically with the user aggregate (§4)
        return Task.CompletedTask;
    }
}
```

### 5.6 The Background Processor — How Events Are Dispatched

`CompositeOutboxProcessor` runs as a single `BackgroundService` polling every **10 seconds**:

```csharp
// YallaJo.SharedKernel.Infrastructure/BackgroundJobs/CompositeOutboxProcessor.cs
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        using var scope = serviceProvider.CreateScope();

        // Resolves every IOutboxProcessor from DI — one per module DbContext
        var processors = scope.ServiceProvider.GetServices<IOutboxProcessor>();

        foreach (var processor in processors)
        {
            try { await processor.ProcessOutboxMessagesAsync(ct); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing failed for {Processor}", processor.GetType().Name);
                // One module's failure does NOT stop others
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(10), ct);
    }
}
```

**Per-module processor (`OutboxProcessor<TContext>`)** — 173 lines of careful logic:

```csharp
// YallaJo.SharedKernel.Infrastructure/BackgroundJobs/OutboxProcessor.cs
internal const int MaxRetryCount = 10;                                      // dead-letter threshold
private const int BatchSize = 20;                                           // messages per poll cycle
private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);    // distributed lock TTL

public async Task ProcessOutboxMessagesAsync(CancellationToken ct = default)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

    var now = DateTime.UtcNow;

    // ── Step 1: Fetch & lock unprocessed, un-dead-lettered, unlocked (or lock-expired) messages ──
    var messages = await dbContext.Set<OutboxMessage>()
        .Where(m => m.ProcessedOnUtc == null
                 && m.RetryCount < MaxRetryCount
                 && (m.LockedUntil == null || m.LockedUntil < now))
        .OrderBy(m => m.OccurredOnUtc)
        .Take(BatchSize)
        .ToListAsync(ct);

    if (messages.Count == 0) return;

    // Claim them — prevents another instance from processing the same message
    var lockUntil = now.Add(LockDuration);
    foreach (var msg in messages) msg.Lock(lockUntil);
    await dbContext.SaveChangesAsync(ct);

    // ── Step 2: Process each message ──
    foreach (var message in messages)
    {
        if (ct.IsCancellationRequested) break;

        try
        {
            // Deserialize
            var eventType = Type.GetType(message.Type);
            if (eventType is null) { message.MarkAsFailed($"Unknown event type: {message.Type}"); continue; }

            var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType) as IIntegrationEvent;
            if (integrationEvent is null) { message.MarkAsFailed("Deserialization returned null"); continue; }

            // Wrap
            var notificationType = typeof(IntegrationEventNotification<>).MakeGenericType(eventType);
            var notification = (INotification)Activator.CreateInstance(notificationType, message.Id, integrationEvent)!;

            // ── Step 3: Individual handler invocation via reflection (NOT mediator.Publish) ──
            //     See §5.7 — this is the most important design decision.
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handlers = scope.ServiceProvider.GetServices(handlerType).Where(h => h is not null).ToList();

            if (handlers.Count == 0)
            {
                // No consumers registered → nothing to do, mark processed (don't retry forever)
                message.MarkAsProcessed();
                continue;
            }

            var handlerFailures = new List<string>();
            foreach (var handler in handlers)
            {
                try
                {
                    var task = (Task?)handlerType.GetMethod("Handle")!
                        .Invoke(handler, new object[] { notification, ct });
                    if (task is not null) await task;
                }
                catch (Exception ex)
                {
                    // Unwrap TargetInvocationException for clarity
                    var actual = ex is TargetInvocationException tie && tie.InnerException is not null
                        ? tie.InnerException : ex;

                    logger.LogError(actual,
                        "Handler {Handler} failed for outbox message {MessageId}. Other handlers will still run.",
                        handler!.GetType().FullName, message.Id);

                    handlerFailures.Add($"{handler.GetType().Name}: {actual.GetType().Name} {actual.Message}");
                }
            }

            // ── Step 4: Mark processed ONLY if ALL handlers succeeded ──
            if (handlerFailures.Count > 0)
            {
                var aggregate = string.Join(" | ", handlerFailures);
                if (aggregate.Length > 4000) aggregate = aggregate[..4000];
                message.MarkAsFailed(aggregate);   // RetryCount++, will retry next cycle
            }
            else
            {
                message.MarkAsProcessed();         // ProcessedOnUtc = DateTime.UtcNow
            }
        }
        catch (Exception ex) { message.MarkAsFailed(ex.Message); }
    }

    // ── Step 5: Persist state changes (marks + retry counts) ──
    await dbContext.SaveChangesAsync(ct);
}
```

### 5.7 The Big Design Decision — Why Reflection Instead of `mediator.Publish`

MediatR's default `ForeachAwaitPublisher` **short-circuits on the first exception**. For integration events that is **wrong**, because:

1. **Multiple modules subscribe to the same event** — e.g., `UserCreatedIntegrationEvent` is consumed by Accounts (create profile), Auth (mark session-ready), Analytics (increment counter).
2. **If Accounts fails, Auth and Analytics still need to run** — otherwise one failing module starves every other consumer.

YallaJo replaces `mediator.Publish` with per-handler reflection invocation and per-handler try/catch:

```csharp
foreach (var handler in handlers)
{
    try { /* invoke handler */ }
    catch (Exception ex) { handlerFailures.Add(...); /* continue to next handler */ }
}

// Only mark processed if ALL handlers succeeded
if (handlerFailures.Count > 0) message.MarkAsFailed(aggregate);
else message.MarkAsProcessed();
```

This gives **per-handler failure isolation** + **all-or-nothing retry semantics**:
- Failed handlers cause the message to be retried next cycle
- Successful handlers short-circuit on retry via their own **inbox** (§5.8)
- Eventually all handlers succeed OR the message dead-letters after 10 retries

### 5.8 Consuming an Integration Event — The Inbox Pattern

**Step 1** — Register an `IInboxStore` for the consuming module:

```csharp
// Accounts.Infrastructure/DependencyInjection.cs
services.AddScoped<IAccountsInboxStore, EfInboxStore<AccountsDbContext>>();
```

(Each module has its own typed inbox alias — `IAccountsInboxStore`, `IAuthInboxStore`, etc. — so the DI container resolves per-module.)

**Step 2** — Write the handler in `{Module}.Application/EventHandlers/`:

```csharp
// Accounts.Application/EventHandlers/UserCreatedIntegrationEventHandler.cs
public sealed class UserCreatedIntegrationEventHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    IAccountsInboxStore inboxStore,
    ILogger<UserCreatedIntegrationEventHandler> logger)
    : INotificationHandler<IntegrationEventNotification<UserCreatedIntegrationEvent>>
{
    public async Task Handle(
        IntegrationEventNotification<UserCreatedIntegrationEvent> notification,
        CancellationToken ct)
    {
        // ── Step A: Inbox idempotency check FIRST ──
        if (await inboxStore.HasBeenProcessedAsync(notification.MessageId, ct))
        {
            logger.LogWarning(
                "Accounts: Message {MessageId} (UserCreated for {UserId}) already processed — skipping.",
                notification.MessageId, notification.Event.UserId);
            return;
        }

        var evt = notification.Event;

        // ── Step B: Defensive check for entity already existing (e.g., synchronous write path already ran) ──
        if (await profileRepository.AnyAsync(p => p.UserId == evt.UserId, ct))
        {
            inboxStore.MarkAsProcessed(notification.MessageId);
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        // ── Step C: Business logic ──
        var profile = Profile.Create(evt.UserId, evt.FirstName, evt.LastName);
        await profileRepository.AddAsync(profile, ct);

        // ── Step D: Mark inbox + SaveChanges atomically ──
        inboxStore.MarkAsProcessed(notification.MessageId);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Accounts: Profile {ProfileId} created for user {UserId}.",
            profile.Id, evt.UserId);
    }
}
```

**The inbox table** is tiny:

```csharp
// YallaJo.SharedKernel.Infrastructure/Inbox/InboxMessage.cs
public sealed class InboxMessage
{
    public Guid Id { get; }              // = OutboxMessage.Id (unique constraint prevents duplicates)
    public DateTime ProcessedAt { get; } // when this consumer handled it
}
```

The inbox row's primary key **is** the outbox message ID. On duplicate delivery, `HasBeenProcessedAsync` returns true and the handler returns immediately — that's the idempotency guarantee.

**`EfInboxStore`** defers persistence to the caller's UoW:

```csharp
public void MarkAsProcessed(Guid messageId)
    => dbContext.Set<InboxMessage>().Add(InboxMessage.Create(messageId));
    // NO SaveChanges — the handler's unitOfWork.SaveChangesAsync commits:
    //   business change + inbox row   → atomic
```

### 5.9 Consumer Handler Requirements (MANDATORY)

| Order | Rule | Why |
|---|---|---|
| 1 | Check inbox FIRST | Idempotency — prevents double-processing on retry |
| 2 | (optional) Defensive existence check | If a synchronous path also ran, don't create duplicates |
| 3 | Do the work | Business logic |
| 4 | Mark inbox LAST | Only after work succeeds |
| 5 | Single `SaveChangesAsync` | Atomic commit of work + inbox row |
| 6 | Never external side effect before inbox mark | If email fails and inbox is marked, retry is lost |
| 7 | Inject `ILogger<THandler>` | Mandatory |
| 8 | Never catch exceptions for business flow | Let them throw — outbox processor isolates and retries |

### 5.10 Concrete End-to-End Example — `UserCreatedIntegrationEvent`

```
T=0s      HTTP POST /api/v1/auth/register
            ↓
          RegisterCommandHandler
            ├─ User.Create(...)                                → raises UserCreatedDomainEvent
            └─ unitOfWork.SaveChangesAsync()
                 ├─ UoW collects UserCreatedDomainEvent
                 ├─ UoW clears events
                 ├─ UoW dispatches
                 │    └─ UserCreatedDomainEventHandler
                 │         └─ dbContext.OutboxMessages.Add(
                 │              OutboxMessage.Create(
                 │                new UserCreatedIntegrationEvent(...)))
                 └─ context.SaveChangesAsync()                  ← ONE commit: User + OutboxMessage
          ↓
          201 Created returned to caller

T≤10s    CompositeOutboxProcessor polls (every 10s)
            ↓
          OutboxProcessor<SecurityDbContext>.ProcessOutboxMessagesAsync()
            ├─ Finds UserCreatedIntegrationEvent row (ProcessedOnUtc=null, RetryCount<10)
            ├─ Locks it (LockedUntil = now + 5min)
            ├─ Deserializes JSON
            ├─ Resolves handlers:
            │    • Accounts.UserCreatedIntegrationEventHandler
            │    • Auth.UserCreatedIntegrationEventHandler
            │    • Analytics.UserCreatedIntegrationEventHandler
            │
            ├─ Accounts handler runs
            │    ├─ Inbox check → not found
            │    ├─ Profile.Create(...)
            │    ├─ profileRepository.AddAsync(profile)
            │    ├─ inboxStore.MarkAsProcessed(messageId)
            │    └─ accountsUnitOfWork.SaveChangesAsync()       ← atomic: Profile + Accounts.InboxMessage
            │
            ├─ Auth handler runs → same pattern → atomic: Session + Auth.InboxMessage
            ├─ Analytics handler runs → same pattern → atomic: MetricRow + Analytics.InboxMessage
            │
            └─ All succeeded → message.MarkAsProcessed() → saves

T=20s    Next poll → no unprocessed messages for this event → skipped
```

### 5.11 Failure Scenarios

| Failure | What Happens | Recovery |
|---|---|---|
| Handler #2 throws `DbUpdateException` | Handlers #1 and #3 still ran (inbox marked). Message marked failed with aggregated error. RetryCount=1. | Next poll: handler #2 retries. Handlers #1, #3 short-circuit via inbox (return early). All succeed → message marked processed. |
| Publisher commits aggregate but processor hasn't run yet | Outbox row exists, ProcessedOnUtc=null. Events will be delivered on next poll. | Normal operation — at-least-once delivery guarantee. |
| Processor instance A crashes mid-processing | Message has `LockedUntil > now`. Other instances wait 5 min. | After 5 min, another instance claims the message and retries. |
| Consumer succeeds but SaveChanges fails | Transaction rolled back — business change + inbox row both rolled back. Handler exception propagates to processor → RetryCount++. | Next poll: re-delivered. Inbox check fails (no row) → re-processes. |
| Message has 10 failed retries | `RetryCount = 10` — filter `RetryCount < MaxRetryCount` excludes it. Dead-lettered. | **Manual intervention required.** Ops query: `SELECT * FROM OutboxMessages WHERE RetryCount >= 10` |
| `Type.GetType(message.Type)` returns null (assembly renamed) | Message marked failed with "Unknown event type". Retried forever (actually capped at max). | Deploy with the old assembly name OR manually delete the bad rows. |

### 5.12 Multi-Instance Safety

The system is safe to run on multiple hosts because of optimistic locking:

```sql
-- Query the processor uses (reconstructed):
SELECT TOP 20 * FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL
  AND RetryCount < 10
  AND (LockedUntil IS NULL OR LockedUntil < GETUTCDATE())
ORDER BY OccurredOnUtc;

-- Immediately after: UPDATE ... SET LockedUntil = @now + 5min; SaveChangesAsync
```

If two instances poll simultaneously, only one wins the `UPDATE` (by EF's optimistic concurrency). The other sees 0 rows updated and retries next cycle.

**Lock expires after 5 minutes** — if the winning instance crashes, another can take over after that timeout. This is why handlers must be **idempotent** and the **inbox is mandatory**.

### 5.13 Operational Queries

Save these as dashboards / alerts:

```sql
-- Backlog size (should be near zero)
SELECT COUNT(*) FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL AND RetryCount < 10;

-- Currently in-flight (locked and not yet finished)
SELECT COUNT(*) FROM OutboxMessages
WHERE ProcessedOnUtc IS NULL AND LockedUntil > GETUTCDATE();

-- DEAD LETTERS — manual intervention required
SELECT Id, Type, Error, RetryCount, OccurredOnUtc
FROM OutboxMessages
WHERE RetryCount >= 10
ORDER BY OccurredOnUtc DESC;

-- Average processing latency
SELECT AVG(DATEDIFF(SECOND, OccurredOnUtc, ProcessedOnUtc)) AS AvgSeconds
FROM OutboxMessages
WHERE ProcessedOnUtc IS NOT NULL
  AND OccurredOnUtc > DATEADD(HOUR, -1, GETUTCDATE());
```

### 5.14 Industry Comparison

| Approach | Dispatch | Locking | Retry | Failure isolation |
|---|---|---|---|---|
| **YallaJo** | Reflection, individual handler invocation | Optimistic via `LockedUntil` | Max 10, passive (next cycle, no backoff) | ✅ Per-handler try/catch |
| [Milan Jovanović (simple)](https://www.milanjovanovic.tech/blog/implementing-the-outbox-pattern) | `mediator.Publish` | DB row lock | Max 3 typical | ❌ First failure aborts rest |
| [Milan Jovanović (Quartz)](https://www.milanjovanovic.tech/blog/scheduling-background-jobs-with-quartz-net) | Quartz job + Mediator | Quartz cluster | Quartz built-in | Depends on Mediator |
| [Kamil Grzybek modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) | Custom Autofac dispatcher | Row lock | Manual | ✅ Per-module isolation |
| MassTransit v8 | MassTransit pipeline | Framework-managed | Exponential backoff | ✅ Built-in |
| Wolverine | Wolverine transport | Framework-managed | Exponential backoff | ✅ Built-in |
| [eShop (Microsoft)](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/integration-event-based-microservice-communications) | Service Bus | Broker | Broker | Broker |

**YallaJo's trade-offs vs alternatives**:
- ✅ Zero dependencies beyond EF + MediatR (no Quartz, Hangfire, Service Bus)
- ✅ Strongest failure isolation (per-handler)
- ❌ No exponential backoff (retries every 10s forever until success or max)
- ❌ No dedicated dead-letter queue (dead messages stay in main table, queryable)
- ❌ `AssemblyQualifiedName` fragility (integration event type rename breaks deserialization)

### 5.15 Anti-Patterns

```csharp
// ❌ Publishing directly from the command handler (skips outbox = dual-write bug)
public async Task<Result> Handle(RegisterCommand cmd, CancellationToken ct)
{
    var user = User.Create(...);
    await userRepo.AddAsync(user, ct);
    await unitOfWork.SaveChangesAsync(ct);            // commits user
    await messageBus.PublishAsync(new UserCreated()); // ❌ second call — if this fails, user saved but event lost
}

// ✅ FIX: raise domain event inside User.Create; domain event handler writes to outbox;
//        UoW commits user + outbox atomically

// ────────────────────────────────────────────────────────────

// ❌ Consumer that does external side effect BEFORE marking inbox
public async Task Handle(...)
{
    if (await inboxStore.HasBeenProcessedAsync(id, ct)) return;

    await emailService.SendAsync(...);                 // ❌ succeeds
    inboxStore.MarkAsProcessed(id);
    await unitOfWork.SaveChangesAsync(ct);             // ❌ throws → email sent but inbox not marked → email sent AGAIN on retry
}

// ✅ FIX: if external effect has its own idempotency, fine.
//        Otherwise, queue the external work to a separate background service
//        and only mark inbox after the work is durably queued.

// ────────────────────────────────────────────────────────────

// ❌ Forgetting the inbox check
public async Task Handle(IntegrationEventNotification<UserCreatedIntegrationEvent> n, CancellationToken ct)
{
    var profile = Profile.Create(n.Event.UserId, ...);  // ❌ runs EVERY retry → creates duplicate profiles
    await profileRepo.AddAsync(profile, ct);
    await unitOfWork.SaveChangesAsync(ct);
}

// ────────────────────────────────────────────────────────────

// ❌ Catching exceptions inside the handler (hides failures from the processor)
public async Task Handle(...)
{
    try { /* work */ }
    catch { /* swallow */ }  // ❌ processor thinks it succeeded, marks message processed → consumer never retries
}

// ✅ FIX: let it throw. The processor isolates the failure and retries next cycle.

// ────────────────────────────────────────────────────────────

// ❌ Integration event referencing Domain types
public sealed record UserCreatedIntegrationEvent(
    User User,                                         // ❌ Domain entity
    Address Address) : IntegrationEventBase;           // ❌ ValueObject

// ✅ FIX: integration events are FLAT DTOs with primitives / Guids / strings only
public sealed record UserCreatedIntegrationEvent(
    Guid UserId, string Email, string FirstName, string LastName) : IntegrationEventBase;
```

### 5.16 Known Limitations (document these as you hit them)

| Limitation | Impact | Mitigation |
|---|---|---|
| `Type.GetType(AssemblyQualifiedName)` breaks on assembly rename / refactor | Messages fail to deserialize; dead-letter | Add an integration-event type registry keyed by short name. Defer renames until dead-letters drain. |
| No exponential backoff | A broken dependency is hammered every 10s | Accept — queue depth is bounded (batch 20), monitor backlog |
| No true dead-letter table | Dead messages sit in main table with `RetryCount >= 10` | Query `WHERE RetryCount >= 10` for ops dashboard |
| Lock duration 5min fixed | Slow handlers may race after 5min timeout | Keep handlers fast; offload to separate background services if needed |
| No integration-event versioning story | Schema change requires backward-compatible payloads | Add optional fields, never remove; handle defaults in consumers |
| No CAP-style outbox across DBs | All modules share one SQL Server instance | Sufficient for the monolith; not applicable until split |

### 5.17 Testing Integration Events

```csharp
// Publisher side — verify outbox row is written
[Fact]
public async Task RegisterCommand_Writes_UserCreatedIntegrationEvent_To_Outbox()
{
    // Arrange (InMemory DbContext + Substitute<IDomainEventDispatcher>)
    var handler = new RegisterCommandHandler(...);

    // Act
    await handler.Handle(new RegisterCommand("a@b.com", "John", "Doe"), default);

    // Assert
    var outboxRow = await dbContext.OutboxMessages.SingleOrDefaultAsync();
    outboxRow.Should().NotBeNull();
    outboxRow!.Type.Should().Contain(nameof(UserCreatedIntegrationEvent));
}

// Consumer side — verify idempotency
[Fact]
public async Task Handler_Is_Idempotent_On_Duplicate_Delivery()
{
    var handler = new UserCreatedIntegrationEventHandler(...);
    var notification = new IntegrationEventNotification<UserCreatedIntegrationEvent>(
        Guid.CreateVersion7(),
        new UserCreatedIntegrationEvent(userId, "a@b.com", "John", "Doe"));

    await handler.Handle(notification, default);
    await handler.Handle(notification, default);  // duplicate

    (await profileRepository.CountAsync(p => p.UserId == userId)).Should().Be(1);
}
```

### 5.18 Production Hardening

This section covers the production-readiness concerns that are typically underdocumented: retention policy, schema versioning, dead-letter strategy, multi-instance polling, observability, and testing. Backed by 15+ industry references ([Chris Richardson microservices.io](https://microservices.io/patterns/data/transactional-outbox.html), [Milan Jovanović](https://www.milanjovanovic.tech/blog/scaling-the-outbox-pattern), [Kamil Grzybek](https://www.kamilgrzybek.com/design/the-outbox-pattern/), [Wolverine](https://wolverinefx.io/guide/durability), [MassTransit](https://masstransit.io/documentation/configuration/middleware/outbox), [NServiceBus](https://docs.particular.net/nservicebus/outbox/), [Brighter](https://brightercommand.gitbook.io/paramore-brighter-documentation/outbox-and-inbox/mssqloutbox), [João Antunes OutboxKit](https://blog.codingmilitia.com/2024/12/03/introducing-outboxkit/)).

#### 5.18.1 Retention Policy

Processed outbox rows accumulate forever today. Add a cleanup job to prune them while preserving audit trail + dead-letters.

| Category | Retention | Reason |
|---|---|---|
| Successfully processed (`ProcessedOnUtc` IS NOT NULL, `RetryCount < 10`) | **30 days** default | Audit + replay window. Jovanović, Grzybek, NServiceBus converge on this. |
| Dead-lettered (`RetryCount >= 10`) | **NEVER auto-delete** | Evidence for debugging + replay after bug fix. Keep indefinitely or with separate 180-day window. |
| In-flight (`LockedUntil > now`) | N/A | Cleanup job excludes these. |
| Pending (`ProcessedOnUtc` IS NULL, not locked) | N/A | Cleanup job excludes these. |

**Recommended `OutboxCleanupBackgroundService`** (add to `YallaJo.SharedKernel.Infrastructure/BackgroundJobs/`):

```csharp
/// <summary>
/// Deletes successfully processed outbox rows older than the retention window.
/// Runs once per hour. Never deletes dead-lettered rows (RetryCount >= 10) —
/// those require manual intervention.
///
/// Per-module cleanup: resolves every IOutboxProcessor via DI to locate each
/// module's DbContext, then runs the cleanup query against that schema.
/// </summary>
public sealed class OutboxCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<OutboxCleanupBackgroundService> logger,
    IOptions<OutboxCleanupOptions> options) : BackgroundService
{
    private const int BatchSize = 1000; // delete in batches to avoid long-running transactions
    private readonly TimeSpan _retentionPeriod = options.Value.RetentionPeriod;   // default 30 days
    private readonly TimeSpan _cleanupInterval = options.Value.CleanupInterval;   // default 1 hour

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation(
            "Outbox cleanup service started — retention: {Retention}, interval: {Interval}",
            _retentionPeriod, _cleanupInterval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CleanupAllModulesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox cleanup iteration failed");
            }

            try { await Task.Delay(_cleanupInterval, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }

    private async Task CleanupAllModulesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var cutoff = DateTime.UtcNow - _retentionPeriod;
        var cleaners = scope.ServiceProvider.GetServices<IOutboxCleaner>();

        foreach (var cleaner in cleaners)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var deleted = await cleaner.DeleteProcessedBeforeAsync(cutoff, BatchSize, ct);
                if (deleted > 0)
                {
                    logger.LogInformation(
                        "Outbox cleanup: deleted {Count} rows for {Module} (cutoff {Cutoff})",
                        deleted, cleaner.ModuleName, cutoff);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox cleanup failed for {Module}", cleaner.ModuleName);
            }
        }
    }
}

/// <summary>Per-module cleanup abstraction. Each module registers its own implementation.</summary>
public interface IOutboxCleaner
{
    string ModuleName { get; }
    Task<int> DeleteProcessedBeforeAsync(DateTime cutoff, int batchSize, CancellationToken ct);
}

public sealed class OutboxCleaner<TContext>(IServiceScopeFactory scopeFactory) : IOutboxCleaner
    where TContext : DbContext
{
    public string ModuleName => typeof(TContext).Name;

    public async Task<int> DeleteProcessedBeforeAsync(
        DateTime cutoff, int batchSize, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        int totalDeleted = 0;
        int deletedInBatch;

        do
        {
            // ExecuteDeleteAsync in EF 7+ does a single bulk DELETE statement
            // Filter: only PROCESSED rows older than cutoff with RetryCount < 10 (exclude dead-letters)
            deletedInBatch = await db.Set<OutboxMessage>()
                .Where(m => m.ProcessedOnUtc != null
                         && m.ProcessedOnUtc < cutoff
                         && m.RetryCount < OutboxProcessor<TContext>.MaxRetryCount)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);

            totalDeleted += deletedInBatch;

        } while (deletedInBatch == batchSize && !ct.IsCancellationRequested);

        return totalDeleted;
    }
}

public sealed class OutboxCleanupOptions
{
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
```

**Registration in each module's DI**:

```csharp
// {Module}.Infrastructure/DependencyInjection.cs
services.AddScoped<IOutboxCleaner, OutboxCleaner<SecurityDbContext>>();
```

**Registration in `YallaJo.SharedKernel.Infrastructure/DependencyInjection.cs`**:

```csharp
services.Configure<OutboxCleanupOptions>(config.GetSection("OutboxCleanup"));
services.AddHostedService<OutboxCleanupBackgroundService>();
```

**`appsettings.json`**:

```json
{
  "OutboxCleanup": {
    "RetentionPeriod": "30.00:00:00",
    "CleanupInterval": "01:00:00"
  }
}
```

**Index for the cleanup query** (add via EF migration):

```sql
-- SQL Server — filtered index excludes dead-letters and includes only processed rows
CREATE NONCLUSTERED INDEX IX_OutboxMessages_Cleanup
ON OutboxMessages (ProcessedOnUtc)
WHERE ProcessedOnUtc IS NOT NULL AND RetryCount < 10;
```

#### 5.18.2 Schema Versioning — Integration Event Type Registry

`OutboxMessage.Type = integrationEvent.GetType().AssemblyQualifiedName!` is **fragile**. Renaming the class, namespace, or assembly breaks deserialization of outstanding outbox rows. This is a production risk.

**Fix**: decouple stored type name from CLR `AssemblyQualifiedName` using a short-name registry.

```csharp
/// <summary>
/// Maps stable logical names to CLR types.
/// Every integration event MUST be registered here.
/// NEVER remove or rename an existing key — only add new ones.
/// To rename an event type, keep the old key as a legacy alias.
/// </summary>
public static class IntegrationEventTypeRegistry
{
    private static readonly Dictionary<string, Type> _nameToType = new()
    {
        ["user.created.v1"]              = typeof(UserCreatedIntegrationEvent),
        ["user.email-verified.v1"]       = typeof(UserEmailVerifiedIntegrationEvent),
        ["tag.created.v1"]               = typeof(TagCreatedIntegrationEvent),
        ["place.created.v1"]             = typeof(PlaceCreatedIntegrationEvent),
        // ... every integration event from every module
    };

    private static readonly Dictionary<Type, string> _typeToName =
        _nameToType
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.First().Key); // canonical (first) name per type

    public static string GetName(Type type)
        => _typeToName.TryGetValue(type, out var name)
            ? name
            : throw new InvalidOperationException(
                $"Type {type.FullName} not registered in IntegrationEventTypeRegistry. " +
                "Add it before publishing.");

    public static bool TryGetType(string name, out Type? type)
        => _nameToType.TryGetValue(name, out type);
}
```

**Modify `OutboxMessage.Create`** to use the registry:

```csharp
public static OutboxMessage Create(IIntegrationEvent integrationEvent)
{
    return new OutboxMessage
    {
        Id = Guid.CreateVersion7(),
        // Was: integrationEvent.GetType().AssemblyQualifiedName!
        Type = IntegrationEventTypeRegistry.GetName(integrationEvent.GetType()),
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        OccurredOnUtc = integrationEvent.OccurredOn,
        ProcessedOnUtc = null,
        Error = null,
        RetryCount = 0,
        LockedUntil = null
    };
}
```

**Modify `OutboxProcessor.ProcessOutboxMessagesAsync`** to use the registry:

```csharp
// Was: var eventType = Type.GetType(message.Type);
if (!IntegrationEventTypeRegistry.TryGetType(message.Type, out var eventType) || eventType is null)
{
    logger.LogError(
        "Unknown integration event type {Type} (message {MessageId}) — dead-lettering",
        message.Type, message.Id);

    // Force dead-letter: set RetryCount to MaxRetryCount so filter excludes it
    // (Alternative: add a dedicated Status column — see §5.18.3)
    while (message.RetryCount < OutboxProcessor<TContext>.MaxRetryCount)
        message.MarkAsFailed($"Unknown type: {message.Type}");
    continue;
}
```

**Schema evolution rules** (from [Confluent schema evolution](https://developer.confluent.io/courses/microservices/schema-evolution)):

| Change | Safe? | Notes |
|---|---|---|
| Add optional field with default | ✅ YES | Always safe — tolerant reader pattern |
| Add required field (no default) | ❌ NO | Old consumers fail to deserialize |
| Remove optional field | ⚠️ RISKY | New consumers may expect it |
| Remove required field | ❌ NEVER | Hard break |
| Rename field | ❌ NEVER | Use add-new + deprecate-old pattern |
| Change field type | ❌ NEVER | Hard break |

**Renaming an event type safely (3-step migration)**:

1. Add new type name to registry alongside old name (both point to same CLR type): `["user.created.v2"] = typeof(UserCreatedIntegrationEvent), ["user.created.v1"] = typeof(UserCreatedIntegrationEvent),`
2. Deploy. Producer starts writing `v2`. Consumers handle both keys. Old rows with `v1` still deserialize via the legacy alias.
3. Wait for retention window + all `v1` rows processed. Remove `v1` key from registry.

#### 5.18.3 Dead-Letter Strategy

**Current YallaJo state**: dead-lettered messages (`RetryCount >= 10`) stay in the main table but are filtered out of dispatcher queries. They are queryable via SQL but there is no automated alerting.

**Recommended enhancements** (listed in priority order):

1. **Add explicit `Status` column** (`Pending` | `Processing` | `Processed` | `Failed` | `Dead`) — makes queries self-documenting:

```csharp
public sealed class OutboxMessage
{
    // ... existing fields ...
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;
}

public enum OutboxMessageStatus { Pending, Processing, Processed, Failed, Dead }
```

2. **Expose dead-letter health endpoint** — `/health/outbox-dead-letters` returns 200 if count is 0, 503 otherwise. Integrates with existing health-check infrastructure.

3. **Add replay endpoint for ops** (guarded by `Permission.Ops.ReplayDeadLetter`):

```csharp
// YallaJo.Api/Endpoints/OpsEndpoints.cs
ops.MapPost("/outbox/replay/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var result = await sender.Send(new ReplayDeadLetterCommand(id), ct);
    return result.ToApiResult();
})
.WithMetadata(new MustHavePermissionAttribute(OpsFeatures.DeadLetter, AppAction.Replay))
.WithName("ReplayDeadLetter");
```

**Replay strategy** (preferred: clone, don't mutate):

```csharp
public async Task<Result> Handle(ReplayDeadLetterCommand cmd, CancellationToken ct)
{
    var dead = await dbContext.OutboxMessages.FindAsync([cmd.MessageId], ct);
    if (dead is null || dead.RetryCount < OutboxProcessor<T>.MaxRetryCount)
        return Result.Failure(Error.NotFound("OutboxMessage.NotDead"), Outcome.NotFound);

    // Clone → new row with fresh ID, RetryCount=0. Preserves dead-letter evidence.
    dbContext.OutboxMessages.Add(new OutboxMessage {
        Id = Guid.CreateVersion7(),
        Type = dead.Type,
        Content = dead.Content,
        OccurredOnUtc = DateTime.UtcNow, // re-stamp so it's picked up on next poll
        // ProcessedOnUtc, Error, RetryCount, LockedUntil all default
    });
    await dbContext.SaveChangesAsync(ct);
    return Result.Success();
}
```

4. **Ops dashboard queries** (add to documentation):

```sql
-- Dead-letter count per module (RUN THIS AS MONITORING)
SELECT 'Security' AS Module, COUNT(*) AS DeadLetters, MIN(OccurredOnUtc) AS Oldest
FROM security.OutboxMessages WHERE RetryCount >= 10
UNION ALL
SELECT 'Accounts', COUNT(*), MIN(OccurredOnUtc)
FROM accounts.OutboxMessages WHERE RetryCount >= 10;

-- Dead-letters with error details
SELECT Id, Type, Error, RetryCount, OccurredOnUtc,
       DATEDIFF(HOUR, OccurredOnUtc, GETUTCDATE()) AS AgeHours
FROM security.OutboxMessages
WHERE RetryCount >= 10
ORDER BY OccurredOnUtc DESC;

-- Processing backlog (should be near zero in healthy system)
SELECT COUNT(*) AS Backlog
FROM security.OutboxMessages
WHERE ProcessedOnUtc IS NULL AND RetryCount < 10;

-- Oldest unprocessed message age (processing lag)
SELECT DATEDIFF(SECOND, MIN(OccurredOnUtc), GETUTCDATE()) AS LagSeconds
FROM security.OutboxMessages
WHERE ProcessedOnUtc IS NULL AND RetryCount < 10;
```

#### 5.18.4 Multi-Instance Polling Deep Dive

YallaJo's current locking is optimistic — safe for multi-instance but not optimal. Industry comparison:

| Framework | Locking mechanism | Polling interval |
|---|---|---|
| **YallaJo** | Optimistic via `LockedUntil` (5min) | 10 seconds fixed |
| [Wolverine](https://wolverinefx.io/guide/durability) | Leadership election (1 instance owns outbox) | 5s default, per-queue override |
| [MassTransit](https://masstransit.io/documentation/configuration/middleware/outbox) | `FOR UPDATE SKIP LOCKED` (PostgreSQL) | `QueryDelay` — 0 when active |
| [NServiceBus](https://docs.particular.net/nservicebus/outbox/) | Depends on transport + persistence | Configurable per persistence |
| [Milan Jovanović scaling](https://www.milanjovanovic.tech/blog/scaling-the-outbox-pattern) | `SKIP LOCKED` + parallel workers | Continuous (no sleep when active) |

**Adaptive polling pattern** (optimization — poll faster when queue has work):

```csharp
// In CompositeOutboxProcessor.ExecuteAsync(), replace fixed Task.Delay with:
var processedThisCycle = false;
foreach (var processor in processors)
{
    try
    {
        var count = await processor.ProcessOutboxMessagesAsync(ct);
        if (count > 0) processedThisCycle = true;
    }
    catch { /* log, continue */ }
}

// Adapt: short delay while draining, long delay when idle
var delay = processedThisCycle
    ? TimeSpan.FromMilliseconds(200)    // keep draining
    : TimeSpan.FromSeconds(10);          // idle backoff (current default)

await Task.Delay(delay, ct);
```

This requires `ProcessOutboxMessagesAsync` to return `int` (count of messages processed). Small, non-breaking change.

#### 5.18.5 Observability — OpenTelemetry Metrics + Trace Propagation

**Problem**: the outbox pattern creates a trace discontinuity. The HTTP request span ends after commit. The outbox processor picks up the row later in a separate span. Without explicit W3C trace context propagation, traces break at the outbox boundary.

**Fix**: propagate `traceparent` through `OutboxMessage.TraceContext`.

**Schema change**:

```csharp
public sealed class OutboxMessage
{
    // ... existing fields ...
    public string? TraceContext { get; private set; } // W3C traceparent + baggage
}
```

**Capture at write time**:

```csharp
using OpenTelemetry.Context.Propagation;

public static class TraceContextHelpers
{
    public static string? Capture()
    {
        var activity = Activity.Current;
        if (activity is null) return null;

        var entries = new List<KeyValuePair<string, string>>();
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            entries,
            (carrier, key, value) => carrier.Add(new(key, value)));

        return JsonSerializer.Serialize(entries);
    }

    public static Activity? Restore(string? serialized, ActivitySource source, string spanName)
    {
        if (string.IsNullOrEmpty(serialized)) return source.StartActivity(spanName);

        var entries = JsonSerializer.Deserialize<List<KeyValuePair<string, string>>>(serialized)!;
        var parentContext = Propagators.DefaultTextMapPropagator.Extract(
            default, entries,
            (carrier, key) => carrier.Where(e => e.Key == key).Select(e => e.Value));

        Baggage.Current = parentContext.Baggage;

        return source.StartActivity(
            spanName,
            ActivityKind.Producer,
            parentContext.ActivityContext);
    }
}
```

**Modify `OutboxMessage.Create`**:

```csharp
public static OutboxMessage Create(IIntegrationEvent integrationEvent)
{
    return new OutboxMessage
    {
        Id = Guid.CreateVersion7(),
        Type = IntegrationEventTypeRegistry.GetName(integrationEvent.GetType()),
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
        OccurredOnUtc = integrationEvent.OccurredOn,
        TraceContext = TraceContextHelpers.Capture(),  // ← NEW
        // ...
    };
}
```

**Modify `OutboxProcessor.ProcessOutboxMessagesAsync`** — restore context before dispatching:

```csharp
private static readonly ActivitySource _activitySource = new("YallaJo.Outbox");

foreach (var message in messages)
{
    using var activity = TraceContextHelpers.Restore(
        message.TraceContext, _activitySource, "outbox.dispatch");
    activity?.SetTag("outbox.message.id", message.Id);
    activity?.SetTag("outbox.message.type", message.Type);
    activity?.SetTag("outbox.retry.count", message.RetryCount);

    // ... existing deserialize + handler invocation ...

    activity?.SetStatus(handlerFailures.Count == 0
        ? ActivityStatusCode.Ok
        : ActivityStatusCode.Error);
}
```

**Metrics to export** (OpenTelemetry Metrics API):

```csharp
public static class OutboxMetrics
{
    private static readonly Meter _meter = new("YallaJo.Outbox", "1.0.0");

    public static readonly Counter<long> ProcessedTotal =
        _meter.CreateCounter<long>("outbox.processed.total",
            description: "Total messages successfully dispatched");

    public static readonly Counter<long> FailedTotal =
        _meter.CreateCounter<long>("outbox.failed.total",
            description: "Total messages that failed dispatch");

    public static readonly Counter<long> DeadLetteredTotal =
        _meter.CreateCounter<long>("outbox.dead_lettered.total",
            description: "Total messages moved to dead-letter");

    public static readonly Histogram<double> DispatchLatencyMs =
        _meter.CreateHistogram<double>("outbox.dispatch.latency_ms",
            description: "Time from message creation to successful dispatch");

    public static readonly Histogram<int> RetryCountHist =
        _meter.CreateHistogram<int>("outbox.retry.count",
            description: "Retry count distribution");

    public static readonly Counter<long> HandlerSuccessTotal =
        _meter.CreateCounter<long>("outbox.handler.success.total",
            description: "Per-handler success count (tag: handler_name)");

    public static readonly Counter<long> HandlerFailureTotal =
        _meter.CreateCounter<long>("outbox.handler.failure.total",
            description: "Per-handler failure count (tag: handler_name)");
}
```

**Recording metrics in `OutboxProcessor`**:

```csharp
// After successful dispatch:
var latency = (DateTime.UtcNow - message.OccurredOnUtc).TotalMilliseconds;
OutboxMetrics.ProcessedTotal.Add(1, new KeyValuePair<string, object?>("module", typeof(TContext).Name));
OutboxMetrics.DispatchLatencyMs.Record(latency);
OutboxMetrics.RetryCountHist.Record(message.RetryCount);

// On handler success:
OutboxMetrics.HandlerSuccessTotal.Add(1,
    new KeyValuePair<string, object?>("handler", handler.GetType().Name));

// On handler failure:
OutboxMetrics.HandlerFailureTotal.Add(1,
    new KeyValuePair<string, object?>("handler", handler.GetType().Name));

// On dead-letter:
OutboxMetrics.DeadLetteredTotal.Add(1,
    new KeyValuePair<string, object?>("module", typeof(TContext).Name),
    new KeyValuePair<string, object?>("type", message.Type));
```

**Alert thresholds** (from [asadali.dev](https://asadali.dev/blog/high-throughput-background-processing-aspnet-core-azure-service-bus-ef-core-outbox/)):

| Alert | Threshold | Action |
|---|---|---|
| Dead-letter count > 0 for any module, sustained > 5 min | Critical | Page ops immediately |
| Backlog size > N (baseline × 3) for > 10 min | Warning | Investigate handler slowdown |
| `outbox.dispatch.latency_ms` p99 > SLA | Warning | Investigate DB or broker |
| Zero messages processed for > 30 min during business hours | Critical | Processor crashed or broker down |

#### 5.18.6 Payload Size Guidance

| Concern | Limit | Notes |
|---|---|---|
| Practical outbox payload (industry consensus) | **< 64 KB** | Keeps hot rows in SQL Server in-row storage (under 8060-byte row limit with room for overhead) |
| SQL Server 8060-byte row limit | NVARCHAR(MAX) values > 8000 bytes spill to LOB pages | Slower access, cannot be indexed |
| PostgreSQL covered-index row limit | 2,712 bytes per B-tree tuple | Excludes large payloads from `INCLUDE` columns |
| Azure Service Bus Standard | 256 KB | External broker if YallaJo later splits to microservices |
| Kafka (recommended) | < 1 MB | External broker limit |

**If a payload must exceed ~64 KB**: use the **Claim Check pattern** — store the large payload in blob storage, write only a reference token (URL or storage key) to the outbox row.

```csharp
// Conditional claim check (example sketch — not yet implemented in YallaJo):
public static OutboxMessage Create(IIntegrationEvent evt, IBlobStore blobStore)
{
    var json = JsonSerializer.Serialize(evt, evt.GetType());

    if (json.Length > 64 * 1024)
    {
        var blobKey = $"outbox/{Guid.NewGuid()}/{evt.GetType().Name}.json";
        blobStore.UploadAsync(blobKey, json).GetAwaiter().GetResult();

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = IntegrationEventTypeRegistry.GetName(evt.GetType()),
            Content = JsonSerializer.Serialize(new ClaimCheckReference(blobKey)),
            // ...
        };
    }

    // Small payload — inline as today
    return new OutboxMessage { /* ... inline ... */ };
}

public sealed record ClaimCheckReference(string BlobKey);
```

Reference: [Microsoft Azure Architecture Center — Claim Check pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/claim-check).

#### 5.18.7 Testing — Testcontainers + Respawn

For integration tests that exercise the full publish-consume loop, use Testcontainers (SQL Server) + Respawn (fast DB reset between tests).

```csharp
public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private Respawner _respawner = null!;
    private SqlConnection _connection = null!;

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        await db.Database.MigrateAsync();

        _connection = new SqlConnection(_sql.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
            SchemasToInclude = new[] { "security", "accounts", "auth" }
            // Do NOT ignore OutboxMessages/InboxMessages — they must reset between tests
        });
    }

    public async Task ResetAsync() => await _respawner.ResetAsync(_connection);
    public async Task DisposeAsync() { await _connection.DisposeAsync(); await _sql.DisposeAsync(); }
}

// Test pattern:
public class UserCreatedEndToEndTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    [Fact]
    public async Task Register_User_End_To_End_Creates_Profile_Via_Outbox()
    {
        await fixture.ResetAsync();

        // Act — register user (writes OutboxMessage)
        var response = await fixture.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { Email = "a@b.com", Password = "Secret1!", FirstName = "John", LastName = "Doe" });
        response.EnsureSuccessStatusCode();

        // Outbox row was written
        using var scope = fixture.Services.CreateScope();
        var secDb = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        var outboxRow = await secDb.OutboxMessages.SingleAsync();
        outboxRow.ProcessedOnUtc.Should().BeNull();

        // Run processor manually
        var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
        await processor.ProcessOutboxMessagesAsync(default);

        // Profile was created in Accounts module
        var accDb = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var profile = await accDb.Profiles.SingleAsync();
        profile.FirstName.Should().Be("John");

        // Inbox row was written (idempotency)
        var inbox = await accDb.InboxMessages.SingleAsync();
        inbox.Id.Should().Be(outboxRow.Id);

        // Outbox row is now marked processed
        await secDb.Entry(outboxRow).ReloadAsync();
        outboxRow.ProcessedOnUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Faulty_Handler_Retries_Then_Other_Handlers_Skip_Via_Inbox()
    {
        // Fault injection test — verify per-handler failure isolation
        // Register a handler that fails 2 times then succeeds
        // Verify: other handlers receive message once (idempotent skip on retry)
    }
}
```

References: [daninacan.com — Respawn with xUnit](https://daninacan.com/resetting-your-test-database-in-c-with-respawn/), [bakson.dev — EF Core + Respawn](https://bakson.dev/2023/08/17/ef-core-and-respawn.html).

#### 5.18.8 Implementation Checklist (to adopt §5.18 hardening in YallaJo)

Ordered by effort × impact:

- [ ] **P0 — Retention**: add `OutboxCleanupBackgroundService` + per-module `IOutboxCleaner<TContext>` (§5.18.1). Default 30-day retention.
- [ ] **P0 — Type registry**: add `IntegrationEventTypeRegistry` + migrate `OutboxMessage.Create` + `OutboxProcessor` (§5.18.2).
- [ ] **P1 — Dead-letter health**: expose `/health/outbox-dead-letters` endpoint with threshold alerting.
- [ ] **P1 — Dead-letter replay**: add `ReplayDeadLetterCommand` + ops endpoint (permission-guarded).
- [ ] **P1 — OpenTelemetry metrics**: add `OutboxMetrics` class with 8 core metrics (§5.18.5).
- [ ] **P2 — Trace context**: add `TraceContext` column to `OutboxMessage`, capture/restore via `TraceContextHelpers` (§5.18.5).
- [ ] **P2 — Adaptive polling**: modify `CompositeOutboxProcessor` to adapt delay based on processed count (§5.18.4).
- [ ] **P3 — Status column**: add explicit `Status` enum (`Pending`/`Processing`/`Processed`/`Failed`/`Dead`) — replaces current implicit-by-`RetryCount` scheme.
- [ ] **P3 — Cleanup index**: add EF migration for `IX_OutboxMessages_Cleanup` filtered index (§5.18.1).
- [ ] **P3 — Claim Check pattern**: implement for payloads > 64 KB (§5.18.6). Only needed when first encounter a large event.

#### 5.18.9 References

| # | Source | URL |
|---|---|---|
| 1 | Chris Richardson — Transactional Outbox canonical definition | [microservices.io/patterns/data/transactional-outbox.html](https://microservices.io/patterns/data/transactional-outbox.html) |
| 2 | Milan Jovanović — Implementing the Outbox Pattern | [milanjovanovic.tech/blog/implementing-the-outbox-pattern](https://www.milanjovanovic.tech/blog/implementing-the-outbox-pattern) |
| 3 | Milan Jovanović — Implementing the Inbox Pattern | [milanjovanovic.tech/blog/implementing-the-inbox-pattern-for-reliable-message-consumption](https://www.milanjovanovic.tech/blog/implementing-the-inbox-pattern-for-reliable-message-consumption) |
| 4 | Milan Jovanović — Scaling the Outbox Pattern (2.8B msgs/day) | [milanjovanovic.tech/blog/scaling-the-outbox-pattern](https://www.milanjovanovic.tech/blog/scaling-the-outbox-pattern) |
| 5 | Kamil Grzybek — The Outbox Pattern | [kamilgrzybek.com/design/the-outbox-pattern](https://www.kamilgrzybek.com/design/the-outbox-pattern/) |
| 6 | Kamil Grzybek — modular-monolith-with-ddd repo | [github.com/kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) |
| 7 | Wolverine — Durable Messaging guide | [wolverinefx.io/guide/durability](https://wolverinefx.io/guide/durability) |
| 8 | MassTransit — Outbox middleware configuration | [masstransit.io/documentation/configuration/middleware/outbox](https://masstransit.io/documentation/configuration/middleware/outbox) |
| 9 | NServiceBus — Outbox pattern | [docs.particular.net/nservicebus/outbox](https://docs.particular.net/nservicebus/outbox/) |
| 10 | Brighter — MSSQL Outbox | [brightercommand.gitbook.io — MsSqlOutbox](https://brightercommand.gitbook.io/paramore-brighter-documentation/outbox-and-inbox/mssqloutbox) |
| 11 | João Antunes — OutboxKit introduction + OTel integration | [blog.codingmilitia.com — OutboxKit](https://blog.codingmilitia.com/2024/12/03/introducing-outboxkit/) |
| 12 | Pat Helland — Life Beyond Distributed Transactions | [queue.acm.org/detail.cfm?id=3025012](https://queue.acm.org/detail.cfm?id=3025012) |
| 13 | Microsoft — Claim Check pattern | [learn.microsoft.com/azure/architecture/patterns/claim-check](https://learn.microsoft.com/en-us/azure/architecture/patterns/claim-check) |
| 14 | DevelopersVoice — Mastering Outbox Pattern in .NET | [developersvoice.com/blog/architecture/mastering-outbox-pattern-distributed-net](https://developersvoice.com/blog/architecture/mastering-outbox-pattern-distributed-net/) |
| 15 | Asad Ali — Production outbox monitoring + alerting | [asadali.dev/blog/high-throughput-background-processing-aspnet-core-azure-service-bus-ef-core-outbox](https://asadali.dev/blog/high-throughput-background-processing-aspnet-core-azure-service-bus-ef-core-outbox/) |

[↑ Back to Table of Contents](#table-of-contents)

---

## 📖 6. Read Repository vs Write Repository

*When to use a read-only repository versus a full write repository. Choosing the right abstraction prevents accidental writes in query handlers and clarifies intent.*

### Interface Split

```
IReadRepository<TEntity, TKey>     ← Expression-based queries + Specification-based queries
IWriteRepository<TEntity, TKey>    ← Add, Update, Remove, ExecuteDelete
IRepository<TEntity, TKey>         ← Combines both (for aggregate roots)
```

### When to Use Which

| Operation | Inject | Why |
|-----------|--------|-----|
| Query handler (GET, LIST) | `IReadRepository` or `IMyRepo` | No writes needed, emphasizes read-only intent |
| Command handler (POST) | `IMyRepo` (full) + `IUnitOfWork` | Needs Add + Save |
| Command handler (PUT/PATCH) | `IMyRepo` (full) + `IUnitOfWork` | Needs Read (to find entity) + implicit Update (EF ChangeTracker) + Save |
| Command handler (DELETE) | `IMyRepo` (full) + `IUnitOfWork` | Needs Read + Remove/SoftDelete + Save |

### Practical Examples

**Read-only (query handler):**
```csharp
// Only needs read operations
public sealed class ListToursQueryHandler(ITourRepository tourRepo)
{
    public async Task<Result<PaginatedResult<TourDto>>> Handle(...)
    {
        var tours = await tourRepo.GetPaginatedAsync(
            pageNumber: request.Page,
            pageSize: request.PageSize,
            orderBy: q => q.OrderByDescending(t => t.CreatedAt),
            asNoTracking: true,  // ← Performance: no change tracking needed
            ct: ct);
        
        // Map to DTOs...
    }
}
```

**Full access (command handler):**
```csharp
// Needs read + write
public sealed class UpdateTourCommandHandler(
    ITourRepository tourRepo,
    ITourUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(...)
    {
        // Read (tracked — AsNoTracking:false is default for GetByIdAsync with tracking)
        var tour = await tourRepo.GetByIdAsync(request.Id, ct, asNoTracking: false);
        
        if (tour is null) return Result.NotFound();
        
        // Modify (EF ChangeTracker picks up changes)
        tour.Update(request.Title, request.Description);
        
        // Save (no explicit Update call needed — ChangeTracker handles it)
        await unitOfWork.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
```

### Key Read Repository Methods

| Method | Use When |
|--------|----------|
| `GetByIdAsync(id)` | Fetch single entity by primary key |
| `GetAsync(filter, include)` | Fetch single entity with complex filter |
| `FirstOrDefaultAsync(filter, include, orderBy)` | First match with ordering |
| `GetAllAsync(filter, include, orderBy)` | Full list with filters |
| `GetPaginatedAsync(page, size, filter, include, orderBy)` | Paginated results |
| `SelectAsync(selector, filter, orderBy)` | Project to DTOs at database level |
| `SelectPaginatedAsync(...)` | Paginated DTO projections |
| `Query(filter, include)` | Raw IQueryable for complex LINQ |
| `ExistsAsync(predicate)` | Boolean existence check |
| `CountAsync(filter)` | Count matching entities |
| `ListAsync(specification)` | Specification-based query |
| `PaginatedListAsync(specification)` | Specification-based pagination |

### Key Write Repository Methods

| Method | Use When |
|--------|----------|
| `Add(entity)` / `AddAsync(entity)` | Insert new entity |
| `AddRange(entities)` | Bulk insert |
| `Update(entity)` | Explicitly mark as modified (usually NOT needed — ChangeTracker) |
| `AttachAndMarkModified(entity, props)` | Update specific properties only (disconnected entity) |
| `Remove(entity)` | Hard delete from DB |
| `ExecuteDeleteAsync(filter)` | Bulk delete without loading entities |
| `DeleteByIdAsync(id)` | Delete by ID without loading |

### EF ChangeTracker vs Explicit Update

```csharp
// ✅ PREFERRED — Let ChangeTracker detect changes
var entity = await repo.GetByIdAsync(id, ct, asNoTracking: false);
entity.Update(newTitle);          // Modify in memory
await unitOfWork.SaveChangesAsync(ct); // EF detects changes automatically

// ⚠️ ONLY when entity is detached (came from another context/request)
repo.AttachAndMarkModified(entity, 
    e => e.Title, 
    e => e.Description);
await unitOfWork.SaveChangesAsync(ct);

// ⚠️ ONLY for bulk operations (bypasses ChangeTracker entirely)
await repo.ExecuteDeleteAsync(e => e.IsExpired, ct);
```

> **📌 Rule:** For tracked entities, never call `repo.Update(entity)` explicitly — EF ChangeTracker detects property changes automatically after `GetByIdAsync(id, ct, asNoTracking: false)`. Only use `AttachAndMarkModified` for disconnected entities that were loaded in a different scope.

[↑ Back to Table of Contents](#table-of-contents)

---

## 💾 7. Unit of Work — The Persistence Orchestrator

*The single point of persistence — coordinates domain event dispatch and atomic saves. Every command handler calls this exactly once, and it must never be called from event handlers or middleware.*

### What It Does

The UnitOfWork is the **single point of persistence**. When you call `SaveChangesAsync()`, it:

1. **Collects** all domain events from aggregate roots in the ChangeTracker
2. **Clears** events from aggregates (prevents double-dispatch)
3. **Dispatches** domain events via MediatR (handlers run synchronously)
4. **Saves** everything in ONE database transaction (aggregate changes + outbox messages)

### Implementation Flow

```csharp
public sealed class UnitOfWork<TContext>(TContext context, IDomainEventDispatcher dispatcher)
{
    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        // 1. Extract domain events from tracked aggregates
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        // 2. Clear events from aggregates
        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        // 3. Dispatch events BEFORE SaveChanges
        //    → Handlers can write OutboxMessages to the same DbContext
        if (domainEvents.Count > 0)
            await dispatcher.DispatchAsync(domainEvents, ct);

        // 4. Atomic save: aggregate changes + outbox messages
        return await context.SaveChangesAsync(ct);
    }
}
```

### Rules

- **ALWAYS call `SaveChangesAsync` from the command handler**, not middleware
- **NEVER call `SaveChangesAsync` from a domain event handler** — the UoW does it after dispatch
- **NEVER call `SaveChangesAsync` in query handlers** — they're read-only
- One `SaveChangesAsync` = one database transaction = atomic consistency guarantee
- Domain event handlers CAN write to the DbContext (outbox messages) because SaveChanges hasn't happened yet

> **⚠️ Warning:** Call `SaveChangesAsync` from the command handler only — never from domain event handlers, never from middleware. One `SaveChangesAsync` = one transaction boundary. Multiple calls in a request create split transactions that break atomicity.

[↑ Back to Table of Contents](#table-of-contents)

---

## ✅ 8. The Result Pattern

*How to return success and failure from handlers without throwing exceptions. Every handler returns a `Result` — this section shows every available factory method and its corresponding HTTP status code.*

Every command/query handler returns `Result` or `Result<T>` instead of throwing exceptions.

```csharp
// Success
return Result.Success(booking.Id);
return Result.Created(booking.Id);          // For POST → 201
return Result<BookingDto>.Success(dto);     // For GET → 200

// Failure
return Result.NotFound<BookingDto>("Booking not found.");    // 404
return Result.Conflict<Guid>("Duplicate booking.");          // 409
return Result.Unauthorized<BookingDto>("Not authenticated."); // 401
return Result.Forbidden<BookingDto>("Insufficient permissions."); // 403
return Result.Invalid<BookingDto>(Error.Validation("Title", "Required.")); // 400
return Result.ServerError<BookingDto>("Something went wrong."); // 500
```

The `Outcome` enum maps directly to HTTP status codes:
- `Ok` = 200, `Created` = 201, `Invalid` = 400, `Unauthorized` = 401
- `Forbidden` = 403, `NotFound` = 404, `Conflict` = 409, `ServerError` = 500

[↑ Back to Table of Contents](#table-of-contents)

---

## 🔍 9. The Specification Pattern — Complex Queries

*Building reusable, composable query objects for complex filtering, sorting, pagination, and projection. Use specifications when expression-based queries become unwieldy or need reuse across handlers.*

### When to Use Specification vs Expression-Based Queries

| Approach | Use When | Example |
|----------|----------|---------|
| **Expression-based** (`GetAllAsync(filter, include, orderBy)`) | Simple, one-off queries | `repo.GetAllAsync(b => b.UserId == userId)` |
| **Specification** (`ListAsync(spec)`) | Complex, reusable, multi-condition queries | Search + filter + include + sort + page |
| **Raw IQueryable** (`Query()`) | Need full LINQ control | Joins, group-by, complex projections |

### The Specification<T> Base Class — Full Fluent API

Every specification inherits from `Specification<TEntity>` and chains methods in the constructor:

```csharp
public abstract class Specification<TEntity> : ISpecification<TEntity>
{
    // ── Filtering ──
    Where(Expression<Func<TEntity, bool>> criteria)          // Add a WHERE clause
    WhereIf(bool condition, Expression<...> criteria)        // Conditional WHERE (great for optional filters)
    
    // ── Eager Loading ──
    Include(Expression<Func<TEntity, object>> include)       // .Include(x => x.Nav)
    Include(string includeString)                            // .Include("Nav.SubNav")
    IncludeAction(Func<IQueryable<T>, IQueryable<T>> action) // .Include(x => x.Nav).ThenInclude(n => n.Sub)
    
    // ── Ordering ──
    OrderBy(Expression<Func<TEntity, object>> expr)          // ORDER BY ASC
    OrderByDescending(Expression<...> expr)                  // ORDER BY DESC
    ThenBy(Expression<...> expr)                             // THEN BY ASC
    ThenByDescending(Expression<...> expr)                   // THEN BY DESC
    
    // ── Pagination ──
    WithPaging(int pageNumber, int pageSize)                 // OFFSET/FETCH with page calculation
    WithSkipTake(int skip, int take)                         // Manual OFFSET/FETCH
    WithTop(int count)                                       // TOP N only
    
    // ── Projection & Grouping ──
    Select(Expression<Func<TEntity, object>> selector)       // SELECT specific columns
    GroupBy(Expression<Func<TEntity, object>> expr)          // GROUP BY
    
    // ── Query Options ──
    WithTracking()                                           // Disable AsNoTracking (default is no-track)
    UseSplitQuery()                                          // AsSplitQuery for cartesian explosion prevention
    WithIgnoreQueryFilters()                                 // Bypass soft-delete filters
    
    // ── Full-Text Search ──
    Search(Expression<Func<T, string>> prop, string term, int group) // EF.Functions.Like
    
    // ── Caching ──
    WithCache(string cacheKey, TimeSpan? duration)           // Cache key + TTL (default 5min)
}
```

> **💡 Tip:** Use `WhereIf(condition, predicate)` for optional filters — it only adds a WHERE clause when the condition is true, eliminating if/else branching in specification constructors and keeping them clean.

### Projection Specifications (`Specification<TEntity, TResult>`)

When you want to project to a DTO at the database level (SELECT only needed columns):

```csharp
public sealed class BookingSummarySpec : Specification<TourBooking, BookingSummaryDto>
{
    public BookingSummarySpec(Guid userId, int page, int pageSize)
    {
        Where(b => b.UserId == userId)
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .WithPaging(page, pageSize)
            .Select(b => new BookingSummaryDto(
                b.Id,
                b.TourId,
                b.Status.ToString(),
                b.TotalPrice.Amount,
                b.CreatedAt));
    }
}

// Usage — returns PaginatedResult<BookingSummaryDto> directly from SQL
var result = await repo.PaginatedListAsync(
    new BookingSummarySpec(userId, request.Page, request.PageSize), ct);
```

### Comprehensive Specification Examples

**Filterable, searchable, paginated list:**

```csharp
public sealed class SearchToursSpec : Specification<Tour>
{
    public SearchToursSpec(string? searchTerm, string? category, 
        decimal? minPrice, decimal? maxPrice, int page, int pageSize)
    {
        // Conditional filters — only applied when parameter is provided
        WhereIf(!string.IsNullOrEmpty(category), t => t.Category == category)
            .WhereIf(minPrice.HasValue, t => t.Price.Amount >= minPrice!.Value)
            .WhereIf(maxPrice.HasValue, t => t.Price.Amount <= maxPrice!.Value);
        
        // Full-text search across multiple columns (OR within same group)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            Search(t => t.Title, searchTerm, searchGroup: 1);
            Search(t => t.Description, searchTerm, searchGroup: 1);
        }
        
        // Eager load + ordering + pagination
        Include(t => t.TourGuide)
            .OrderByDescending(t => t.CreatedAt)
            .WithPaging(page, pageSize);
    }
}
```

**Complex include chains:**

```csharp
public sealed class UserWithFullDetailsSpec : Specification<User>
{
    public UserWithFullDetailsSpec(Guid userId)
    {
        Where(u => u.Id == userId)
            .WithTracking()        // Need tracking for updates
            .UseSplitQuery()       // Prevent cartesian explosion
            .IncludeAction(q => q  // Complex ThenInclude chains
                .Include(u => u.Emails)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RoleClaims)
                .Include(u => u.UserClaims));
    }
}
```

**Bypassing soft-delete filters:**

```csharp
public sealed class AllBookingsIncludingDeletedSpec : Specification<TourBooking>
{
    public AllBookingsIncludingDeletedSpec()
    {
        WithIgnoreQueryFilters()  // Sees soft-deleted rows too
            .OrderByDescending(b => b.CreatedAt)
            .WithTop(100);
    }
}
```

### How It Works Under the Hood

The `SpecificationEvaluator` translates your specification into an EF Core IQueryable in this order:

```
1. IgnoreQueryFilters (if set)
2. Criteria (WHERE clauses)
3. AdditionalCriteria (extra WHERE clauses)
4. Search (EF.Functions.Like with OR within groups, AND between groups)
5. Includes (expression-based, string-based, and action-based)
6. Ordering (OrderBy/OrderByDescending + ThenBy chain)
7. GroupBy (if set)
8. Pagination (Skip/Take)
9. AsNoTracking (default: true)
10. AsSplitQuery (if set)
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 🔄 10. Complete Request Lifecycle

*The complete journey from HTTP request to database save and back. Useful for debugging or onboarding — shows every layer in sequence including middleware, MediatR pipeline, UoW, and background outbox.*

```
                         HTTP POST /api/bookings
                                ↓
                    ┌─── Middleware Pipeline ───┐
                    │  ExceptionHandler         │
                    │  RateLimiter              │
                    │  Authentication (JWT)     │
                    │  Authorization (Perms)    │
                    └───────────┬───────────────┘
                                ↓
              BookingEndpoints.CreateBooking()
              → new CreateTourBookingCommand(...)
              → ISender.Send(command)
                                ↓
                    ┌─── MediatR Pipeline ─────┐
                    │  ValidationBehavior       │ → Runs FluentValidation
                    │  LoggingBehavior          │ → Logs request start
                    │  PerformanceBehavior      │ → Starts timer
                    └───────────┬───────────────┘
                                ↓
            CreateTourBookingCommandHandler.Handle()
            → bookingRepo.AddAsync(booking)
            → unitOfWork.SaveChangesAsync()
                                ↓
                    ┌─── UnitOfWork ───────────┐
                    │  Collect domain events    │
                    │  Clear events             │
                    │  Dispatch via MediatR     │
                    │    ↓                      │
                    │  DomainEventHandler       │
                    │  → Write OutboxMessage    │
                    │                           │
                    │  context.SaveChangesAsync  │ ← SINGLE atomic save
                    └───────────┬───────────────┘
                                ↓
              Result.Created(bookingId) returned
                                ↓
              Results.Created("/api/bookings/{id}", id)
                                ↓
                         HTTP 201 Created

                    ┌─── Background (10s later) ─┐
                    │  CompositeOutboxProcessor   │
                    │  → Deserialize OutboxMessage │
                    │  → Publish IntegrationEvent  │
                    │  → Handlers in other modules │
                    │    (with inbox idempotency)  │
                     └──────────────────────────────┘
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 💎 11. Value Objects — When & How to Use

*Domain-primitive types with structural equality and built-in validation. Use value objects instead of primitives when a concept has validation rules, operations, or is shared across entities.*

### What Is a Value Object?

A Value Object has **no identity** — two value objects are equal if their properties are equal. Unlike entities (which have an `Id`), value objects describe **characteristics** of things.

### The Base Class

```csharp
// YallaJo.SharedKernel.Domain/ValueObjects/ValueObject.cs
public abstract class ValueObject : IEquatable<ValueObject>
{
    // YOU override this — list every property that defines equality
    protected abstract IEnumerable<object?> GetEqualityComponents();
    
    // Structural equality: two Money(10, "JOD") are always ==
    public bool Equals(ValueObject? other) { ... }
    public override bool Equals(object? obj) => Equals(obj as ValueObject);
    public override int GetHashCode() { /* combines all components */ }
    public static bool operator ==(ValueObject? left, ValueObject? right);
    public static bool operator !=(ValueObject? left, ValueObject? right);
}
```

### How to Create a Custom Value Object

**Step 1 — Inherit from `ValueObject`:**

```csharp
public sealed class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }
    public string PostalCode { get; private set; }
    
    private Address() { }  // EF Core constructor
    
    public Address(string street, string city, string country, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street required.");
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City required.");
        Street = street;
        City = city;
        Country = country.ToUpperInvariant();
        PostalCode = postalCode;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
}
```

**Key rules for Value Objects:**
- **Immutable** — Private setters only. To change, create a NEW instance.
- **Self-validating** — Constructor throws if invalid. A VO is always in a valid state.
- **No identity** — No `Id` property. Equality by component values.
- **Private parameterless constructor** — Required for EF Core materialization.
- **Override `GetEqualityComponents()`** — Yield every property that defines equality.

> **📌 Rule:** Value objects are immutable — never add public setters or mutation methods. To "change" a value object, replace the entire instance (e.g., `entity.TotalPrice = TotalPrice.ApplyDiscount(pct)` creates a new `Money` — the old one is discarded).

### Existing Value Objects in SharedKernel

**Money** — Monetary amounts with currency:

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }  // 3-letter ISO ("JOD", "USD")
    
    public Money(decimal amount, string currency = "JOD");  // Validates: amount >= 0, currency = 3 chars
    public static Money Zero(string currency = "JOD");      // Factory for zero amount
    
    // Arithmetic (returns NEW Money, never mutates)
    public Money Add(Money other);              // Same currency enforced
    public Money Subtract(Money other);         // Same currency enforced
    public Money MultiplyBy(decimal factor);    // Quantity × unit price
    public Money ApplyDiscount(decimal percent); // 0-100 percentage
    
    // Components: Amount + Currency
}
```

**Location** — GPS coordinates:

```csharp
public sealed class Location : ValueObject
{
    public decimal Latitude { get; private set; }   // -90 to 90
    public decimal Longitude { get; private set; }  // -180 to 180
    
    public Location(decimal latitude, decimal longitude);  // Validates ranges
    public double DistanceTo(Location other);              // Haversine formula (km)
    
    // Components: Latitude + Longitude
}
```

**DateRange** — Time period:

```csharp
public sealed class DateRange : ValueObject
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    
    public DateRange(DateTime start, DateTime end);  // Validates end >= start
    public int DurationInDays { get; }                // Computed
    public bool Contains(DateTime date);              // Is date within range?
    public bool Overlaps(DateRange other);             // Do ranges overlap?
    
    // Components: Start + End
}
```

### How to Map Value Objects in EF Core (OwnsOne)

Value Objects are mapped as **owned types** — their properties become columns in the parent table:

```csharp
// In your IEntityTypeConfiguration<T>
public void Configure(EntityTypeBuilder<TourBooking> builder)
{
    // Money value object → two columns: TotalPrice + TotalPriceCurrency
    builder.OwnsOne(e => e.TotalPrice, money =>
    {
        money.Property(m => m.Amount)
            .HasColumnName("TotalPrice")     // Rename column
            .HasPrecision(19, 4);            // Decimal precision
        money.Property(m => m.Currency)
            .HasColumnName("TotalPriceCurrency")
            .HasMaxLength(3)
            .HasDefaultValue("JOD");
    });
}
```

**Multiple Money properties on same entity:**

```csharp
// Finance.Infrastructure — PayoutItem has 3 Money value objects
builder.OwnsOne(x => x.Amount, money =>
{
    money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(19, 4);
    money.Property(m => m.Currency).HasColumnName("AmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
});
builder.OwnsOne(x => x.Commission, money =>
{
    money.Property(m => m.Amount).HasColumnName("Commission").HasPrecision(19, 4);
    money.Property(m => m.Currency).HasColumnName("CommissionCurrency").HasMaxLength(3).HasDefaultValue("JOD");
});
builder.OwnsOne(x => x.NetAmount, money =>
{
    money.Property(m => m.Amount).HasColumnName("NetAmount").HasPrecision(19, 4);
    money.Property(m => m.Currency).HasColumnName("NetAmountCurrency").HasMaxLength(3).HasDefaultValue("JOD");
});
```

**Location value object:**

```csharp
// Analytics.Infrastructure — UserInteraction has Location
builder.OwnsOne(e => e.Location, loc =>
{
    loc.Property(l => l.Latitude).HasColumnName("Latitude").HasPrecision(10, 8);
    loc.Property(l => l.Longitude).HasColumnName("Longitude").HasPrecision(11, 8);
});
```

### When to Use Value Object vs Primitive

| Use Value Object When... | Use Primitive When... |
|--------------------------|----------------------|
| Combination of related values (amount + currency) | Single simple value (string name) |
| Business rules govern validity (lat must be -90..90) | No special validation needed |
| Operations belong to the concept (Money.Add()) | No operations beyond get/set |
| Multiple entities use the same concept | Only used in one place |
| Equality is by value, not identity | N/A |

### Using Value Objects in Domain Entities

```csharp
public sealed class TourBooking : AuditableEntity, IAggregateRoot
{
    public Money TotalPrice { get; private set; } = null!;
    
    public static TourBooking Create(Guid userId, Guid tourId, Money price)
    {
        return new TourBooking { TotalPrice = price };  // Passed in as VO
    }
    
    public void ApplyDiscount(decimal percentage)
    {
        TotalPrice = TotalPrice.ApplyDiscount(percentage);  // Creates NEW Money
        MarkUpdated();
    }
}
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 📚 12. Repository Interfaces — Complete API Reference

*The complete API surface for all repository methods — read, write, and specification-based. Use this as a lookup reference when building query and command handlers.*

### IReadRepository<TEntity, TKey> — Every Read Method

```csharp
public interface IReadRepository<TEntity, in TKey>
{
    // ── Single Entity by ID ──
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default, bool asNoTracking = true);
    //   Usage: var booking = await repo.GetByIdAsync(bookingId, ct);
    //   For updates, pass asNoTracking: false to enable ChangeTracker
    
    // ── Single Entity by Filter ──
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> filter,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool asNoTracking = true, CancellationToken ct = default);
    //   Usage: var booking = await repo.GetAsync(
    //       b => b.ConfirmationCode == code,
    //       include: q => q.Include(b => b.JoinRequests));
    
    // ── First Match with Ordering ──
    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true, CancellationToken ct = default);
    //   Usage: var latest = await repo.FirstOrDefaultAsync(
    //       filter: b => b.UserId == userId,
    //       orderBy: q => q.OrderByDescending(b => b.CreatedAt));
    
    // ── All Matching Entities ──
    Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true, CancellationToken ct = default);
    //   Usage: var bookings = await repo.GetAllAsync(
    //       filter: b => b.Status == BookingStatus.Confirmed,
    //       orderBy: q => q.OrderBy(b => b.ScheduledDate),
    //       ct: ct);
    
    // ── Paginated Results ──
    Task<PaginatedResult<TEntity>> GetPaginatedAsync(
        int pageNumber, int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool asNoTracking = true, CancellationToken ct = default);
    //   Usage: var page = await repo.GetPaginatedAsync(1, 20,
    //       filter: b => b.UserId == userId,
    //       include: q => q.Include(b => b.JoinRequests),
    //       orderBy: q => q.OrderByDescending(b => b.CreatedAt));
    
    // ── Projections (SELECT only needed columns) ──
    Task<List<TResult>> SelectAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default);
    //   Usage: var ids = await repo.SelectAsync(
    //       selector: b => b.Id,
    //       filter: b => b.UserId == userId);
    //   Usage: var dtos = await repo.SelectAsync(
    //       selector: b => new BookingListItem(b.Id, b.Status.ToString(), b.CreatedAt),
    //       filter: b => b.Status == BookingStatus.Pending);
    
    // ── Paginated Projections ──
    Task<PaginatedResult<TResult>> SelectPaginatedAsync<TResult>(...);
    
    // ── Raw IQueryable (for complex LINQ) ──
    IQueryable<TEntity> Query(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
        bool asNoTracking = true);
    //   Usage: var query = repo.Query(b => b.Status == BookingStatus.Confirmed)
    //       .GroupBy(b => b.TourId)
    //       .Select(g => new { TourId = g.Key, Count = g.Count() });
    
    // ── Existence & Counting ──
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct);
    //   Usage: var exists = await repo.ExistsAsync(b => b.ConfirmationCode == code, ct);
    //   Usage: var count = await repo.CountAsync(b => b.UserId == userId, ct);
    
    // ── Specification-Based (alternative to expression-based) ──
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct);
    Task<List<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct);
    Task<PaginatedResult<TEntity>> PaginatedListAsync(ISpecification<TEntity> spec, CancellationToken ct);
    Task<PaginatedResult<TResult>> PaginatedListAsync<TResult>(ISpecification<TEntity, TResult> spec, CancellationToken ct);
    Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct);
    Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct);
}
```

### IWriteRepository<TEntity, TKey> — Every Write Method

```csharp
public interface IWriteRepository<TEntity, in TKey>
{
    // ── Insert ──
    void Add(TEntity entity);                              // Sync add to ChangeTracker
    void AddRange(IEnumerable<TEntity> entities);           // Batch insert
    Task AddAsync(TEntity entity, CancellationToken ct);    // Async add (preferred)
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct);
    
    // ── Update ──
    void Update(TEntity entity);                            // Mark entire entity as modified
    void UpdateRange(IEnumerable<TEntity> entities);        // Batch update
    void AttachAndMarkModified(TEntity entity,              // Update SPECIFIC properties only
        params Expression<Func<TEntity, object>>[] modifiedProperties);
    void AttachAndMarkModifiedWithConcurrency(TEntity entity,  // + optimistic concurrency check
        string concurrencyPropertyName,
        object originalConcurrencyValue,
        params Expression<Func<TEntity, object>>[] modifiedProperties);
    
    // ── Delete ──
    void Remove(TEntity entity);                            // Hard delete single entity
    void RemoveRange(IEnumerable<TEntity> entities);        // Hard delete batch
    Task<int> ExecuteDeleteAsync(Expression<...> filter, CancellationToken ct); // Bulk hard delete (no loading)
    Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct);                 // Hard delete by ID
    Task<int> ExecuteDeleteByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct); // Bulk by IDs
}
```

### IRepository<TEntity> — Full Access for Aggregate Roots

```csharp
// Combines BOTH read and write — ONLY for aggregate roots
public interface IRepository<TEntity, in TKey> 
    : IReadRepository<TEntity, TKey>, IWriteRepository<TEntity, TKey>
    where TEntity : class, IAggregateRoot  // ← aggregate root required!
    where TKey : notnull;

// Guid shorthand
public interface IRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : class, IAggregateRoot;
```

### EfRepository vs EfEntityRepository — When to Use Which

| Class | Constraint | Use For | Example |
|-------|-----------|---------|---------|
| `EfRepository<TEntity>` | Requires `IAggregateRoot` | Aggregate roots | User, TourBooking, Profile |
| `EfEntityRepository<TEntity, TKey>` | No `IAggregateRoot` required | Child/supporting entities | Role, RoleClaim, JoinRequest |

```csharp
// Aggregate root repository
public sealed class BookingRepository(BookingDbContext context)
    : EfRepository<TourBooking, Guid>(context), IBookingRepository { }

// Non-aggregate entity repository
public sealed class RoleRepository(SecurityDbContext context)
    : EfEntityRepository<Role, Guid>(context), IRoleRepository { }
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 🔒 13. Soft Delete & Concurrency

*How soft delete protects data and how optimistic concurrency prevents lost updates. Both mechanisms are built into `AuditableEntity` and require minimal configuration.*

### Entity Base Class Selection — `BaseEntity` vs `AuditableEntity`

Choose the base class carefully. The choice affects EF configuration and available methods:

| Needs | Use |
|-------|-----|
| Simple identity only (`Id`) — no tracking, no concurrency | `BaseEntity` |
| Full audit trail (`CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`, `RowVersion`) | `AuditableEntity` |
| Aggregate root (can raise domain events, dispatched by UoW) | `AuditableEntity` + `IAggregateRoot` |
| Join table / value object with no lifecycle | `BaseEntity` |

**The rule**: If an entity has ANY of — `UpdatedAt`, `RowVersion`, `IsDeleted` — it MUST extend `AuditableEntity`, not `BaseEntity`. Never manually add `[Timestamp] byte[] RowVersion` or `DateTime? UpdatedAt` to a `BaseEntity` subclass. That's a sign it should be `AuditableEntity`. (`Tag` was found doing this in ContentCore — fixed.)

```csharp
// ❌ WRONG — manually managing what AuditableEntity provides for free
public sealed class Tag : BaseEntity
{
    [Timestamp]
    public byte[] RowVersion { get; private set; } = [];  // ← belongs in AuditableEntity
    public DateTime? UpdatedAt { get; private set; }       // ← belongs in AuditableEntity

    public void Update(...)
    {
        UpdatedAt = DateTime.UtcNow;  // ← should be MarkUpdated()
    }
}

// ✅ CORRECT
public sealed class Tag : AuditableEntity
{
    public void Update(...)
    {
        MarkUpdated();  // AuditableEntity handles UpdatedAt, RowVersion via interceptors
    }
}
```

**EF configuration**: When switching from `BaseEntity` to `AuditableEntity`, remove any explicit `CreatedAt`, `UpdatedAt`, `RowVersion` config from the `IEntityTypeConfiguration` — `AuditableEntity`'s shared base configuration in SharedKernel already handles them.

### IncludeInactive pattern for admin queries

When a query hides inactive/deactivated entities (e.g., `!category.IsActive`), provide an `IncludeInactive` parameter for admin callers who need to view and manage deactivated records:

```csharp
// Query record:
public sealed record GetCategoryByIdQuery(
    Guid Id,
    bool WithTranslations = false,
    bool IncludeInactive = false)    // ← admin pass true
    : IQuery<CategoryDto>, ICacheableQuery
{
    // Cache key MUST include IncludeInactive to avoid mixing admin and public cached views
    public string CacheKey => ContentCoreCacheKeys.Category(Id, WithTranslations, IncludeInactive);
}

// Handler:
if (category is null || (!category.IsActive && !request.IncludeInactive))
    return Result<CategoryDto>.Failure(new Error("Category.NotFound", ...), Outcome.NotFound);
```

**Never** return the same cached entry for admin and public callers when the visibility differs.

### Soft Delete

All entities inheriting from `AuditableEntity` support soft delete:

```csharp
// In a command handler — soft delete (PREFERRED over hard delete)
var booking = await repo.GetByIdAsync(id, ct, asNoTracking: false);
booking.SoftDelete();  // Sets IsDeleted = true, DeletedAt = now
await unitOfWork.SaveChangesAsync(ct);

// Restore a soft-deleted entity
booking.Restore();     // Sets IsDeleted = false, DeletedAt = null
await unitOfWork.SaveChangesAsync(ct);
```

> **📌 Rule:** Always prefer `SoftDelete()` over `Remove()` for aggregate roots. Hard delete (`Remove()`) is only appropriate for join tables or reference data that has no audit requirements.

```csharp
```

**Automatic Query Filtering** — soft-deleted rows are invisible by default:

```csharp
// In entity configuration
builder.HasQueryFilter(b => !b.IsDeleted);  // Global filter

// All repo queries AUTOMATICALLY skip deleted rows
var bookings = await repo.GetAllAsync();  // Only returns IsDeleted == false

// To see deleted rows, use specification:
new MySpec().WithIgnoreQueryFilters()  // Bypasses the filter
```

### Optimistic Concurrency (RowVersion)

`AuditableEntity` includes `byte[] RowVersion` for conflict detection:

```csharp
// In entity configuration
builder.Property(b => b.RowVersion).IsRowVersion();

// What happens: if two users load the same entity and both try to save,
// the second save throws DbUpdateConcurrencyException because RowVersion changed.

// For disconnected updates (entity came from a different context/request):
repo.AttachAndMarkModifiedWithConcurrency(
    entity: booking,
    concurrencyPropertyName: nameof(TourBooking.RowVersion),
    originalConcurrencyValue: request.RowVersion,  // From client
    e => e.Status,          // Properties to update
    e => e.ConfirmationCode
);
await unitOfWork.SaveChangesAsync(ct);  // Throws if RowVersion mismatch
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 📄 14. PaginatedResult<T>

*The standard pagination wrapper returned by all list queries. Understand its properties so your responses are consistent across the entire API.*

Standard pagination wrapper used across all query results:

```csharp
public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; }     // Current page items
    public int PageNumber { get; }             // 1-based page index
    public int PageSize { get; }               // Items per page
    public int TotalCount { get; }             // Total items across all pages
    public int TotalPages { get; }             // Computed: ceil(TotalCount / PageSize)
    public bool HasPreviousPage { get; }       // PageNumber > 1
    public bool HasNextPage { get; }           // PageNumber < TotalPages
    
    public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 10);
}

// Returned by:
//   repo.GetPaginatedAsync(page, size, filter, include, orderBy)
//   repo.SelectPaginatedAsync(page, size, selector, filter, orderBy)
//   repo.PaginatedListAsync(specification)
```

[↑ Back to Table of Contents](#table-of-contents)

---

## ⚙️ 15. MediatR Pipeline Behaviors

*Three automatic pipeline behaviors that run before every handler — validation, logging, and performance monitoring. You don't wire these up manually; they just work.*

Every command/query passes through 3 behaviors (in order) BEFORE reaching the handler:

```
Request → ValidationBehavior → LoggingBehavior → PerformanceBehavior → Handler
```

### ValidationBehavior

Runs ALL registered `IValidator<TRequest>` (FluentValidation) before the handler:

```csharp
// If validation fails: throws ValidationException
// Caught by ValidationExceptionHandler → HTTP 400 with RFC-7807 ProblemDetails
// If no validators registered for this request: passes through silently
```

**You don't call validators manually.** Just create a validator class, and it auto-runs:

```csharp
public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.ParticipantCount).InclusiveBetween(1, 20);
    }
}
// Automatically discovered and executed before CreateBookingCommandHandler.Handle()
```

> **💡 Tip:** Never call validators manually from command handlers. Just create the validator class and `ValidationBehavior` auto-discovers and runs it before every handler invocation — zero wiring required.

### LoggingBehavior

Logs request start, completion (with elapsed time), and failures:

```
DEBUG: Starting CreateTourBookingCommand
DEBUG: Completed CreateTourBookingCommand in 42ms
  -or-
ERROR: Failed CreateTourBookingCommand in 150ms [exception details]
```

### PerformanceBehavior

Warns if any request takes longer than **500ms**:

```
WARN: Slow request detected: ListTourBookingsQuery took 1200ms (threshold: 500ms)
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 👤 16. ICurrentUser & IRequestContext

*Interfaces for accessing the current user's identity and HTTP request metadata. Inject these instead of reading from `HttpContext` directly — they're mockable and properly abstracted.*

### ICurrentUser — The Ownership Rule (MANDATORY)

> **📌 Rule 2 (non-negotiable):** Inject `ICurrentUser` into a handler **ONLY when comparing `ICurrentUser.UserId` against a resource's owner/creator/target field**. See `agent-context.md §2.2`.

`ICurrentUser` is NOT a general-purpose "authenticated user" gate. Authorization is done at the endpoint with `MustHavePermissionAttribute` (see §2.4). A handler that only calls `currentUser.IsAuthenticated` is duplicating the job — remove the injection.

#### The Interface

```csharp
public interface ICurrentUser
{
    Guid? UserId { get; }                    // From "sub" claim
    string? UserName { get; }                // From "name" claim
    string? Email { get; }                   // From "email" claim
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }       // From "role" claims
    IEnumerable<string> Permissions { get; } // From "Permission" claims
    bool HasPermission(string permission);
    bool IsInRole(string role);
}
```

#### Decision Table — Should I Inject `ICurrentUser`?

| Scenario | Inject? | Why |
|---|---|---|
| Query: "List MY bookings" (filter by userId) | ✅ YES | Compares `booking.UserId == currentUser.UserId` |
| Command: "Update MY profile" (self-edit) | ✅ YES | Loads profile where `p.UserId == currentUser.UserId` |
| Command: "Revoke MY session" (IDOR prevention) | ✅ YES | Guards `session.UserId != currentUser.UserId → Forbidden` |
| Command: "Update ANY business" (admin can, owner can) | ✅ YES | Compares `business.OwnerId != currentUser.UserId && !isAdmin` |
| Command: "Create business" (stamp owner) | ✅ YES | `ownerId: currentUser.UserId.Value` is a valid creator stamp |
| Command: "Approve business" (admin-only action) | ❌ NO | Use `MustHavePermission(Business, Approve)` on endpoint. Don't gate inside the handler. |
| Command: "Suspend business" (moderator action) | ❌ NO | Same — permission belongs on the endpoint |
| Query: "List all users" (admin dashboard) | ❌ NO | Permission gates access; handler has no ownership concern |
| Any handler that only checks `IsAuthenticated` | ❌ NO | `MustHavePermission` on endpoint already enforced authentication |

#### ✅ Valid Patterns

```csharp
// ✅ Self-edit (list/update only the current user's resource)
public sealed class UpdateProfileCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork uow,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateProfileCommandHandler> logger)
    : ICommandHandler<UpdateProfileCommand>
{
    public async Task<Result> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Unauthorized("Authentication required.");

        // ownership = self: load ONLY the current user's profile
        var profile = await profileRepository.FirstOrDefaultAsync(
            p => p.UserId == currentUser.UserId.Value, ct);

        if (profile is null)
            return Result.Failure(Error.NotFound("Profile.NotFound"), Outcome.NotFound);

        profile.Update(cmd.FirstName, cmd.LastName);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"profile:{currentUser.UserId.Value}", ct);
        return Result.Success();
    }
}

// ✅ IDOR prevention (owner-or-admin check)
public sealed class UpdateBusinessCommandHandler(
    IBusinessRepository repo,
    IContentPlacesUnitOfWork uow,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateBusinessCommandHandler> logger)
    : ICommandHandler<UpdateBusinessCommand>
{
    public async Task<Result> Handle(UpdateBusinessCommand cmd, CancellationToken ct)
    {
        // endpoint already verified: MustHavePermission(Business, Update)
        var business = await repo.GetByIdAsync(cmd.Id, ct);
        if (business is null)
            return Result.Failure(Error.NotFound("Business.NotFound"), Outcome.NotFound);

        // row-level ownership check
        var isAdmin = currentUser.IsInRole("Admin");
        if (!isAdmin && business.OwnerId != currentUser.UserId!.Value)
            return Result.Failure(Error.Forbidden("Business.NotOwner"), Outcome.Forbidden);

        business.Update(cmd.Name, cmd.Description);
        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{cmd.Id}", ct);
        return Result.Success();
    }
}

// ✅ Creator stamp at aggregate creation
public sealed class CreateBusinessCommandHandler(
    IBusinessRepository repo,
    ICurrentUser currentUser,
    ...)
{
    public async Task<Result<CreateBusinessResult>> Handle(CreateBusinessCommand cmd, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return Result.Unauthorized("Authentication required.");

        // stamping the creator is a legitimate domain concept
        var business = BusinessEntity.Create(
            name: cmd.Name,
            slug: slug,
            ownerId: currentUser.UserId.Value);
        // ...
    }
}
```

#### ❌ Anti-Patterns (REJECT in code review)

```csharp
// ❌ Gratuitous — only checks auth, no ownership comparison
public sealed class SuspendBusinessCommandHandler(
    IBusinessRepository repo,
    ICurrentUser currentUser,  // ← REMOVE THIS
    ...)
{
    public async Task<Result> Handle(SuspendBusinessCommand cmd, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)  // ← permission on endpoint already enforced this
            return Result.Unauthorized();

        var business = await repo.GetByIdAsync(cmd.Id, ct);
        business.Suspend(cmd.Reason);  // ← no ownership check anywhere
        // ...
    }
}

// ✅ FIX — remove ICurrentUser. Guard via MustHavePermission on endpoint:
//    .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Suspend))
public sealed class SuspendBusinessCommandHandler(
    IBusinessRepository repo,
    IContentPlacesUnitOfWork uow,
    ILogger<SuspendBusinessCommandHandler> logger)
{
    public async Task<Result> Handle(SuspendBusinessCommand cmd, CancellationToken ct)
    {
        var business = await repo.GetByIdAsync(cmd.Id, ct);
        if (business is null)
            return Result.Failure(Error.NotFound("Business.NotFound"), Outcome.NotFound);

        business.Suspend(cmd.Reason);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

> **Current violations in the codebase**: 8 handlers in `ContentPlaces.Application` violate this rule. See `agent-context.md §8.1` for the exact list + fixes.

### IRequestContext — HTTP Request Metadata

```csharp
public interface IRequestContext
{
    string? IpAddress { get; }        // Client IP
    string? UserAgent { get; }        // Browser/app user agent
    string? DeviceName { get; }       // Device identifier
    string? AcceptLanguage { get; }   // Preferred language
    string? Platform { get; }         // OS/platform
}

// Usage: audit logging, analytics, rate limiting decisions
```

### IDateTimeProvider — Testable Clock

```csharp
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }      // Current UTC time
    DateTime Today => UtcNow.Date; // Today's date
}

// Usage: instead of DateTime.UtcNow (untestable), inject this:
public sealed class ExpireOldLocksCommandHandler(
    IDateTimeProvider clock)
{
    // clock.UtcNow is mockable in tests
}
```

[↑ Back to Table of Contents](#table-of-contents)

---

## ⚡ 17. Cheat Sheet — Quick Reference

*Quick reference tables for HTTP verb patterns, file checklists, and decision trees. Bookmark this section for day-to-day development.*

### HTTP Verb → Pattern Mapping

| HTTP | Endpoint Pattern | App Layer | Result |
|------|-----------------|-----------|--------|
| **POST** | `MapPost("/")` | `ICommand<Guid>` → `Result.Created(id)` | `201 Created` |
| **GET one** | `MapGet("/{id:guid}")` | `IQuery<Dto>` → `Result.Success(dto)` | `200 OK` |
| **GET list** | `MapGet("/")` | `IQuery<PaginatedResult<Dto>>` | `200 OK` |
| **PUT** | `MapPut("/{id:guid}")` | `ICommand<Dto>` → `Result.Success(dto)` | `200 OK` |
| **PATCH** | `MapPatch("/{id:guid}/action")` | `ICommand` → `Result.Success()` | `204 No Content` |
| **DELETE** | `MapDelete("/{id:guid}")` | `ICommand` → `Result.Success()` | `204 No Content` |

### File Creation Checklist (New Feature)

```
Domain
□ Domain Entity              → {Module}.Domain/Entities/
□ Domain Enums               → {Module}.Domain/Enums/
□ Domain Events              → {Module}.Domain/Events/
□ Repo Interface             → {Module}.Domain/Repositories/
□ UoW Interface              → {Module}.Domain/Repositories/

Contracts (public surface)
□ Integration Events         → {Module}.Contracts/IntegrationEvents/
□ Feature Constants          → {Module}.Contracts/Authorization/{Module}Features.cs
□ Permission Catalog         → {Module}.Contracts/Authorization/{Module}PermissionCatalog.cs

Application
□ Commands + Handlers        → {Module}.Application/Commands/
□ Queries + Handlers         → {Module}.Application/Queries/  (MUST implement ICacheableQuery)
□ Validators                 → With each command
□ Cache Keys                 → {Module}.Application/Caching/{Module}CacheKeys.cs

Infrastructure
□ DbContext + Config         → {Module}.Infrastructure/Persistence/
□ Repository Impl            → {Module}.Infrastructure/Repositories/
□ UoW Impl                   → {Module}.Infrastructure/Persistence/
□ Domain Event Handlers      → {Module}.Infrastructure/EventHandlers/
□ DI Registration            → {Module}.Infrastructure/DependencyInjection.cs
  └─ services.AddSingleton<IPermissionCatalog, {Module}PermissionCatalog>();

Presentation
□ Endpoints                  → {Module}.Presentation/
  └─ Every endpoint: .WithMetadata(new MustHavePermissionAttribute(...)) OR .AllowAnonymous()

Host
□ Register in Program.cs     → Add{Module}Application() + Add{Module}Infrastructure() + Map{Module}Endpoints()
□ EF Migration               → dotnet ef migrations add Add{Entity} --project {Module}.Infrastructure --startup-project YallaJo.Api
```

> **Authorization catalog is a new mandatory artifact as of the 2026-04-22 refactor.** Without it, your module's permissions never appear in the database and every endpoint returns 403. See `agent-context.md §4` and `authorization-refactor-plan.md` for the full architecture.

### Domain Event vs Integration Event Decision

```
"Does ONLY this module care?"
  → YES → Domain Event only (no outbox)
  → NO  → Domain Event + Integration Event via Outbox

"Is there a side effect within the same transaction?"
  → YES → Domain Event handler (runs before SaveChanges)

"Does another module need to react?"
  → YES → Integration Event in Contracts + Handler in consuming module

"Does an external system need to know?"
  → YES → Integration Event (outbox ensures delivery)
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 🌍 18. Translation System — Cross-Module Auto-Translation

*Automatic multi-language translation using Azure Translator with a DB cache decorator and event-driven orchestration. Add translation to new entities by following the checklist at the end of this section.*

### Overview

The Translation System provides **automatic multi-language content translation** across all content modules. It uses a **pluggable provider pattern** (Azure Translator as primary), a **decorator for caching**, and **event-driven orchestration** to keep translations in sync.

### Architecture Layers

```
SharedKernel.Application (abstractions)
├── ITranslationService          ← Pluggable provider interface
├── IActiveLanguageProvider       ← Returns active language Id+Code pairs
└── IEntityTranslationOrchestrator ← Translates entity field maps to all active languages

ContentCore.Domain
├── TranslationCache             ← DB-cached translations entity
├── TranslationStatus            ← Pending | AutoTranslated | HumanReviewed
├── CategoryCreatedDomainEvent   ← Triggers intra-module translation
└── LanguageActivatedDomainEvent ← Triggers cross-module bulk translation

ContentCore.Infrastructure (implementations)
├── AzureTranslateService         ← Azure Translator REST API client
├── AutoSaveTranslationService    ← Decorator: checks DB cache → falls back to API
├── ActiveLanguageProvider        ← Queries Language table for active languages
├── EntityTranslationOrchestrator ← Batch-translates field maps per language
├── CategoryCreatedDomainEventHandler    ← Domain event → translate category
└── LanguageActivatedDomainEventHandler  ← Domain event → outbox integration event

ContentCore.Contracts
└── LanguageActivatedIntegrationEvent    ← Cross-module event for bulk translation

ContentPlaces/Tours/Blogs/Seo.Infrastructure
└── LanguageActivatedIntegrationEventHandler ← Inbox consumer → translate all entities
```

### Pluggable Provider Pattern

The translation service is abstracted behind `ITranslationService` — swap providers without touching business logic:

```csharp
// SharedKernel.Application/Abstractions/Translation/ITranslationService.cs
public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(
        string text, string sourceLanguage, string targetLanguage, CancellationToken ct);
    Task<List<TranslationResult>> BatchTranslateAsync(
        List<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct);
    Task<string> DetectLanguageAsync(string text, CancellationToken ct);
    Task<List<SupportedLanguage>> GetSupportedLanguagesAsync(CancellationToken ct);
}
```

The primary implementation is `AzureTranslateService`, configured in `appsettings.json`:

```json
"AzureTranslator": {
  "SubscriptionKey": "<your-key>",
  "Region": "<your-region>",
  "Endpoint": "https://api.cognitive.microsofttranslator.com"
}
```

### Decorator Pattern — Caching Layer

The `AutoSaveTranslationService` wraps any `ITranslationService` to add DB-level caching. It checks `TranslationCache` before hitting the external API:

```csharp
// DI registration — decorator pattern
services.AddHttpClient<AzureTranslateService>();
services.AddScoped<ITranslationService>(sp => new AutoSaveTranslationService(
    inner: sp.GetRequiredService<AzureTranslateService>(),
    cacheRepository: sp.GetRequiredService<ITranslationCacheRepository>(),
    unitOfWork: sp.GetRequiredService<IContentCoreUnitOfWork>()));
```

**Flow:** Request → Check DB cache → Cache hit? Return cached → Cache miss? Call Azure API → Save to cache → Return

### Entity Translation Orchestrator

The orchestrator translates a map of entity fields into all active languages at once:

```csharp
// SharedKernel.Application/Abstractions/Translation/IEntityTranslationOrchestrator.cs
public interface IEntityTranslationOrchestrator
{
    Task<List<EntityFieldTranslationSet>> TranslateEntityFieldsAsync(
        Dictionary<string, string> sourceFields,  // e.g., { "Name": "Dead Sea", "Description": "..." }
        string sourceLanguageCode,
        CancellationToken ct);
    // Returns one EntityFieldTranslationSet PER active language:
    //   { LanguageId, LanguageCode, TranslatedFields: { "Name": "البحر الميت", "Description": "..." } }
}
```

### Event-Driven Translation Flow

Translation is triggered by **events**, not inline code. This keeps command handlers clean and decoupled.

#### Intra-Module: Domain Event (same transaction)

When a Category is created, a domain event triggers translation within ContentCore:

```
CreateCategoryCommandHandler
  → Category.Create(name, sourceLanguageCode)
    → AddDomainEvent(new CategoryCreatedDomainEvent(Id, Name, SourceLanguageCode))
  → unitOfWork.SaveChangesAsync()
    → CategoryCreatedDomainEventHandler
      → IEntityTranslationOrchestrator.TranslateEntityFieldsAsync({ "Name": name })
      → category.AddTranslation() for each language
    → Single atomic SaveChanges (category + all translations)
```

#### Cross-Module: Integration Event (via outbox/inbox)

When a Language is activated, ALL content modules translate their entities:

```
Language.Create("ar") or Language.Activate()
  → LanguageActivatedDomainEvent raised
  → unitOfWork.SaveChangesAsync()
    → LanguageActivatedDomainEventHandler
      → Writes LanguageActivatedIntegrationEvent to ContentCore outbox
    → Atomic save (language + outbox message)

CompositeOutboxProcessor (background, every 10s)
  → Picks up outbox message → publishes IntegrationEventNotification

ContentPlaces handler receives → inbox check → translate all Places + Businesses
ContentTours handler receives  → inbox check → translate all Tours
ContentBlogs handler receives  → inbox check → translate all Blogs
ContentSeo handler receives    → inbox check → translate all FaqItems
```

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Domain event for CategoryCreated (not inline) | Keeps handler clean; translation is a side effect, not core logic |
| Integration event for LanguageActivated | Multiple modules need to react; outbox ensures delivery |
| Decorator for caching | Same translation text is never sent to Azure twice |
| Orchestrator per-language batching | One API call per language, not per field |
| `TranslationStatus` enum | Distinguishes auto-translated vs human-reviewed content |

### Adding Translation to a New Entity

To make a new entity translatable:

```
1. Add a {Entity}Translation entity with Create() factory method
2. Add a {Entity}CreatedDomainEvent in the module's Domain/Events/
3. Raise the event from {Entity}.Create()
4. Create {Entity}CreatedDomainEventHandler in Infrastructure/EventHandlers/
   → Use IEntityTranslationOrchestrator to translate
   → Add translations to the entity
5. Add a handler for LanguageActivatedIntegrationEvent (if cross-module)
   → Query all entities, translate missing language, save with inbox idempotency
```

[↑ Back to Table of Contents](#table-of-contents)

---

## 🗄️ 19. Caching — HybridCache, Tags, Redis L2

*How to cache query results, prevent stampedes, and invalidate stale data. Every new module must follow these patterns.*

YallaJo uses `HybridCache` (`Microsoft.Extensions.Caching.Hybrid`) for all data-level caching. This section covers the exact patterns to follow when adding caching to any module.

### Technology Stack

| Layer | Technology | Purpose |
|---|---|---|
| Data caching | `HybridCache` via `ICacheableQuery` + `QueryCachingBehavior` MediatR pipeline | Cache query handler results with stampede prevention + tag-based eviction |
| L1 cache | In-memory (built into HybridCache) | Per-process, zero serialization, fastest access |
| L2 cache (future) | Redis via `IDistributedCache` | Shared across server instances — add when scaling |
| Response caching (selective) | Output Caching middleware | Full HTTP response cache for anonymous-only endpoints |

### The Decision: Cache or Not Cache

**Rule of thumb**: If the read-to-write ratio is > 10:1 AND the data is not real-time critical, cache it.

**MUST cache**: Reference data (categories, tags, languages, specializations), translated content, entity detail by slug (SEO pages).

**MUST NOT cache**: Bookings, payments, slot availability, user sessions, authentication tokens, any entity with `LockedUntil`.

### How to Add Caching to a Query

#### Step 1: Implement `ICacheableQuery` on the query record

```csharp
public sealed record ListPlacesQuery(bool ActiveOnly, int Page, int PageSize)
    : IQuery<IReadOnlyList<PlaceSummaryDto>>, ICacheableQuery
{
    // 1. Deterministic key from parameters
    public string CacheKey => ContentPlacesCacheKeys.PlaceList(ActiveOnly, Page, PageSize);

    // 2. TTL — list queries get longer, get-by-id queries get shorter
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);

    // 3. Tags for group invalidation
    public IReadOnlyList<string> Tags => ["places"];
}

public sealed record GetPlaceByIdQuery(Guid Id)
    : IQuery<PlaceDetailDto>, ICacheableQuery
{
    public string CacheKey => ContentPlacesCacheKeys.Place(Id);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);  // Short — negative cache protection
    public IReadOnlyList<string> Tags => ["places", $"place:{Id}"];
}
```

The `QueryCachingBehavior` pipeline behavior handles the rest automatically:
- On cache HIT: returns cached result, handler is NOT called
- On cache MISS: calls handler, caches result with tags, returns to caller
- On STAMPEDE: coalesces concurrent misses into one handler call

#### Step 2: Add tag-based eviction in command handlers

```csharp
public sealed class CreatePlaceCommandHandler(
    IPlaceRepository placeRepository,
    IContentPlacesUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<CreatePlaceCommand, CreatePlaceResult>
{
    public async Task<Result<CreatePlaceResult>> Handle(
        CreatePlaceCommand request, CancellationToken ct)
    {
        try
        {
            // ... business logic ...

            try { await unitOfWork.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreatePlaceResult>.Failure(
                    new Error("Place.ConcurrencyConflict", "..."), Outcome.Conflict);
            }

            // Invalidate ALL place-related cache entries with one call
            await cache.RemoveByTagAsync("places", ct);

            return Result<CreatePlaceResult>.Created(new CreatePlaceResult(place.Id));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<CreatePlaceResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
```

#### Step 3: Create a static CacheKeys class in each module

```csharp
// ContentPlaces.Application/Caching/ContentPlacesCacheKeys.cs
public static class ContentPlacesCacheKeys
{
    public static string PlaceList(bool activeOnly, int page, int pageSize) =>
        $"cp:places:{activeOnly}:p{page}:s{pageSize}";

    public static string Place(Guid id) => $"cp:place:{id}";

    public static string PlaceBySlug(string slug) => $"cp:place:slug:{slug}";

    public static string BusinessList(Guid placeId, int page, int pageSize) =>
        $"cp:biz:{placeId}:p{page}:s{pageSize}";

    public static string Business(Guid id) => $"cp:biz:{id}";
}
```

### TTL Guidelines

| Query type | Absolute TTL | Why |
|---|---|---|
| List queries (paginated) | 5 min | Frequently changes with new data |
| Get-by-ID / get-by-slug | 5 min | Short — protects against cached not-found (negative cache). Tag eviction handles write-then-read. |
| Reference data lists (categories, tags, languages) | 30 min | Rarely changes, invalidated on write via tags |
| Translated content | 120 min | Expensive to regenerate (external API call) |

### Tag Convention

| Tag pattern | Scope | Example |
|---|---|---|
| `"{entity}"` | All entries for this entity type | `"places"`, `"categories"`, `"tags"` |
| `"{entity}:{id}"` | One specific entity instance | `"place:abc123"`, `"category:def456"` |
| `"attachments:{entityType}:{entityId}"` | Attachments scoped to a parent | `"attachments:Place:abc123"` |
| `"translations:{entityType}:{entityId}"` | Translations scoped to a parent | `"translations:Place:abc123"` |

**Command handler rule — use the RIGHT granularity**:

| Operation type | Tags to evict | Why |
|---|---|---|
| Create new entity | Coarse only (`"places"`) | All lists become stale, no specific instance to target |
| Update single entity | Fine + coarse (`"place:{id}"` + `"places"`) | Detail cache stale + list summaries stale |
| Delete single entity | Fine + coarse (`"place:{id}"` + `"places"`) | Same as update |
| Delete attachment | Fine only (`"attachments:{Type}:{EntityId}"` + `"attachment:{id}"`) | Only THIS entity's attachments are affected — do NOT use coarse `"attachments"` which evicts ALL entities' attachment caches system-wide |
| Assign/remove category from entity | Fine only (`"entity-categories:{Type}:{EntityId}"`) | Only THIS entity's category assignments are affected |

⚠️ **Critical rule**: Using `RemoveByTagAsync("coarse-tag")` on single-entity mutations causes a cache stampede — ALL entries tagged with that tag are evicted system-wide, forcing every entity to hit the DB on next read. Always prefer `RemoveByTagAsync($"fine-grained:{id}")` for single-entity mutations.

**State-change guard rule** (prevents duplicate domain events → duplicate outbox writes):
Before calling any domain method that raises a domain event (`Activate()`, `Approve()`, `Suspend()`, etc.), ALWAYS check that the entity is NOT already in the target state:

```csharp
// WRONG — fires event even if already active:
language.Activate();

// CORRECT — only fires if state actually changes:
if (!language.IsActive)
    language.Activate();

// For toggle commands:
if (request.IsActive && !language.IsActive)
    language.Activate();
else if (!request.IsActive && language.IsActive)
    language.Deactivate();
```

Calling state-change methods without guards causes duplicate domain events → duplicate outbox rows → duplicate integration events → duplicate downstream work (e.g., re-translating all content when a language was already active).

### Cache Key Convention

**Format**: `{module}:{entity}:{scope}:{params}` — all lowercase, colon-separated.

**Module prefixes**: `cc` = ContentCore, `cp` = ContentPlaces, `ct` = ContentTours, `bk` = Booking, `fn` = Finance.

Rules:
- NEVER use GUIDs as keys without an entity prefix
- NEVER cache user-specific data with a shared key — include `user:{userId}`
- NEVER hand-write key strings — always use the module's static `CacheKeys` class
- NEVER use spaces in cache keys

### New Module Caching Setup Checklist

Every new module that adds caching MUST follow ALL these steps. Do NOT skip any. Each step is a mandatory requirement.

#### Step A: Add the package to `{Module}.Application.csproj`

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="9.3.0" />
```

> ⚠️ **LOCKED VERSION**: Always use `9.3.0`. This is the canonical version for this project.
> Do NOT use `10.x` — see §Version Consistency Rule in `agent-context.md`.
> The version in `SharedKernel.Application` and `SharedKernel.Infrastructure` must match.
> If you see a mismatch, downgrade all references to `9.3.0` before proceeding.

#### Step B: Create `{Module}.Application/Caching/{Module}CacheKeys.cs`

One static class per module. All key-building logic lives here — never hand-write key strings in handlers.

```csharp
namespace {Module}.Application.Caching;

public static class {Module}CacheKeys
{
    // Pattern: {prefix}:{entity}:{scope}:{params}
    // Module prefixes: cc=ContentCore, cp=ContentPlaces, ct=ContentTours, bk=Booking, fn=Finance

    public static string EntityList(bool activeOnly, int page, int pageSize) =>
        $"{prefix}:{entity}:{activeOnly}:p{page}:s{pageSize}";

    public static string Entity(Guid id) =>
        $"{prefix}:{entity}:{id}";

    // If the query varies by caller identity (admin vs public), include user context:
    public static string EntityForUser(Guid id, Guid? userId, bool isAdmin) =>
        $"{prefix}:{entity}:{id}:u:{userId}:a:{isAdmin}";
}
```

#### Step C: Add `ICacheableQuery` to every query record

Every query that returns data (list or single-entity) MUST implement `ICacheableQuery`. No exceptions.

```csharp
public sealed record ListXxxQuery(int Page, int PageSize)
    : IQuery<PaginatedResult<XxxSummaryDto>>, ICacheableQuery
{
    public string CacheKey => {Module}CacheKeys.XxxList(Page, PageSize);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["xxxs"];          // coarse tag for the entity type
}

public sealed record GetXxxByIdQuery(Guid Id, Guid? UserId, bool IsAdmin)
    : IQuery<XxxDetailDto>, ICacheableQuery
{
    public string CacheKey => {Module}CacheKeys.Xxx(Id, UserId, IsAdmin);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags => ["xxxs", $"xxx:{Id}"];  // coarse + fine-grained
}
```

#### Step D: Inject `HybridCache` and call `RemoveByTagAsync` in every command handler

Every command that mutates state MUST invalidate the cache after a successful save.

```csharp
public sealed class CreateXxxCommandHandler(
    IXxxRepository xxxRepository,
    I{Module}UnitOfWork unitOfWork,
    HybridCache cache,                   // ← inject
    ILogger<CreateXxxCommandHandler> logger)
    : ICommandHandler<CreateXxxCommand, CreateXxxResult>
{
    public async Task<Result<CreateXxxResult>> Handle(CreateXxxCommand request, CancellationToken ct)
    {
        // ... business logic + save ...

        var saveResult = await SaveAsync(..., ct);
        if (saveResult is not null) return saveResult;

        await cache.RemoveByTagAsync("xxxs", ct);    // ← evict after save, not before

        return Result<CreateXxxResult>.Created(new CreateXxxResult(entity.Id));
    }
}
```

**Tag eviction rule**: Evict AFTER a successful save. Never evict before save — a failed save would leave an empty cache backed by nothing.

#### Step E: Verify — Caching Checklist Before Marking Feature Complete

- [ ] `Microsoft.Extensions.Caching.Hybrid` `9.3.0` added to `{Module}.Application.csproj`?
- [ ] `{Module}CacheKeys.cs` static class created in `{Module}.Application/Caching/`?
- [ ] Every query record implements `ICacheableQuery` (CacheKey + CacheDuration + Tags)?
- [ ] Every command handler injects `HybridCache` and calls `RemoveByTagAsync` after successful save?
- [ ] Tags follow convention: `"{entity}s"` for lists, `"{entity}s"` + `"{entity}:{id}"` for detail?
- [ ] Keys follow format `{prefix}:{entity}:{scope}:{params}` — all lowercase, colon-separated?
- [ ] Auth-varied queries (admin vs public) include `userId` and `isAdmin` in the cache key?
- [ ] `RemoveByTagAsync` called AFTER save (not before)?

### Serialization (Result<T> Redis-Ready)

`Result<T>` and `Result` classes have `[JsonConstructor]` attributes for System.Text.Json deserialization. This is required for HybridCache L2 (Redis) serialization.

**When adding new types to cache**: Ensure all cached types (DTOs, Result wrappers) are JSON-serializable:
- All properties must have public getters
- Classes need a `[JsonConstructor]` public constructor OR be records (records serialize natively)
- Records with `init` properties work out of the box
- `IReadOnlyList<T>` deserializes to `List<T>` by default — this is fine

### What NOT to Do

| Anti-pattern | Why it's wrong | Correct approach |
|---|---|---|
| `cache.Remove(key1); cache.Remove(key2); ...` | Fragile — miss a key = stale data | `cache.RemoveByTagAsync("tag", ct)` |
| `IMemoryCache` in new code | No stampede prevention, no tags | `HybridCache` only |
| 30-min TTL on get-by-id | Cached not-found persists too long | 5-min TTL + tag eviction |
| Caching without tags | Impossible to invalidate in bulk | Always set `Tags` on `ICacheableQuery` |
| Output Caching on auth-varied endpoints | Wrong response served to wrong user | Data-level `HybridCache` only |
| `SemaphoreSlim` for stampede prevention | Doesn't compose, error-prone | `GetOrCreateAsync` handles it internally |

### Adding Redis L2 (Scaling to Multiple Servers)

When the app runs on more than one server instance, add Redis as the L2 distributed cache. **Zero handler code changes required** — HybridCache auto-detects the registered `IDistributedCache` and uses it as L2.

#### Step 1: Install the package

```bash
dotnet add YallaJo.Api package Microsoft.Extensions.Caching.StackExchangeRedis
```

#### Step 2: Add connection string

In `appsettings.json` (or `appsettings.Production.json`):

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,abortConnect=false,connectTimeout=5000,syncTimeout=3000"
  }
}
```

Production Redis connection string should come from environment variables or Azure Key Vault — never hardcode credentials.

#### Step 3: Register in Program.cs

Add this **before** `AddSharedKernelInfrastructure()`:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "YallaJo:";  // Prefix all keys to avoid collisions with other apps
});
```

That's it. HybridCache now uses:
- **L1 (in-memory)**: Fast, per-process — serves hot data
- **L2 (Redis)**: Shared across all server instances — backs up L1 and syncs eviction

#### How L1/L2 Interaction Works

```
Request → L1 hit? → Return (fastest)
        → L1 miss → L2 hit? → Store in L1 → Return
                   → L2 miss → Execute handler → Store in L1 + L2 → Return

Eviction (RemoveByTagAsync):
  → Removes from L1 (this process)
  → Removes from L2 (Redis — shared)
  → Other server instances will miss L1, re-fetch from L2 (which is also evicted)
  → Next request triggers handler → repopulates L1 + L2
```

#### Redis Resilience — What Happens When Redis Goes Down

HybridCache degrades gracefully:
- **Redis down**: L1 still works for the current process. Cache misses fall through to the handler (DB). No exceptions thrown to the client.
- **Redis comes back**: L2 re-populates on the next cache miss. No manual intervention needed.
- **No try/catch needed in handler code** — HybridCache handles Redis failures internally.

#### LocalCacheExpiration (L1 ↔ L2 Sync)

When Redis is active, `LocalCacheExpiration` controls how long L1 keeps entries before re-checking L2. This prevents L1 from serving stale data after another server instance evicts L2.

```csharp
// Already configured in SharedKernel DI:
services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(15),           // L2 (Redis) max lifetime
        LocalCacheExpiration = TimeSpan.FromMinutes(5),  // L1 re-syncs from L2 every 5 min
    };
});
```

**Rule**: `LocalCacheExpiration` should always be **shorter** than `Expiration`. Recommended ratio: L1 = 1/3 of L2.

#### What Changes in Behavior After Adding Redis

| Aspect | Before (L1 only) | After (L1 + Redis L2) |
|---|---|---|
| Cache scope | Per-process only | Shared across all servers |
| Serialization | None (objects by reference) | JSON via System.Text.Json |
| Stampede prevention | Per-process | Per-process (not cross-server — no distributed lock) |
| Eviction | Instant (same process) | Instant locally, propagates to L2 |
| Cold start | Full cache miss | L2 (Redis) has warm data from other instances |
| Handler code changes | None | None |

#### Serialization Checklist (Before Enabling Redis)

When Redis L2 is added, every cached type must be JSON-serializable. Verify:

- [ ] `Result<T>` and `Result` have `[JsonConstructor]` — ✅ already done
- [ ] `Error` record — ✅ records serialize natively
- [ ] `Outcome` enum — ✅ enums serialize as ints
- [ ] All DTOs (records with `init` properties) — ✅ records serialize natively
- [ ] No `IReadOnlyList<T>` properties with custom implementations — verify they deserialize to `List<T>`
- [ ] No circular references in any cached DTO
- [ ] No `Func<>`, `Action<>`, or delegate properties on cached types

