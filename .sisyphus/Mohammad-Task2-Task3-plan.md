# Mohammad — Task 2 + Task 3 Implementation Plan

**Author**: planning agent (Claude / Prometheus)
**Date drafted**: 2026-04-26
**Source brief**: `Agents/ContentTours-team-tasks.md` (sections Task 2 + Task 3)
**Cross-references**: `Agents/agent-context.md` §2, `Agents/guide.md`, `Agents/templates/*.template`, `Agents/patterns/*.md`, `Agents/YallaJo Business Rules & Edge Cases.pdf`

> **Total scope**: 13 endpoints (8 Task 2 + 5 Task 3) · 52 hours combined · spans W2 W → W3 M
> **Hard deadlines**: Task 2 = Thu 2026-05-14 · 17:00 · Task 3 = Mon 2026-05-18 · 17:00

---

## 0. Executive Summary

Mohammad ships two adjacent feature blocks on the **ContentTours** module:

| Block | Endpoints | Hours | Deadline | Earliest start |
|---|---:|---:|---|---|
| **Task 2A** — TourSchedule CRUD + recurrence engine | 4 | 13 | — | W 2026-05-06 PM |
| **Task 2B** — TourPricingTier CRUD + Adult-tier guard | 4 | 15 | Thu 2026-05-14 17:00 | W 2026-05-06 PM |
| **Task 3** — Search / Suggest / Featured / MyTours / FeatureToggle | 5 | 24 | Mon 2026-05-18 17:00 | T 2026-05-12 PM |

Both tasks **depend on Task 1 (Mahmoud)** delivering the Tour aggregate with: `Pending/Approved/Rejected/Suspended/Archived` statuses, audit fields, `Tour.Submit/Approve/Reject` methods, the ContentTours `PermissionCatalog`, and Task 1 Tour endpoints (1–11) wired in `ContentToursEndpoints.cs`.

---

## 1. Pre-Flight: Existing Repository State (Snapshot 2026-04-26)

Result of inspection across `ContentTours.{Domain, Application, Infrastructure, Presentation, Contracts}`.

### 1.1 Domain — what already exists

| File | State | Mohammad's action |
|---|---|---|
| `ContentTours.Domain/Entities/Tour.cs` | Aggregate root, `AuditableEntity, IAggregateRoot`. Has `Currency`, `BasePrice` (Money), `IsFeatured`, `Status`, `IsChildFriendly`, `IsAccessible`, `IsInstantBooking`, etc. **No `SetFeatured` method.** | Add `SetFeatured(isFeatured, changedByUserId)` in Task 3 (per brief: Mahmoud may not have added it) |
| `ContentTours.Domain/Entities/TourSchedule.cs` | `BaseEntity`. Fields exist (`TourId`, `DayOfWeek`, `StartTime`, `EndTime?`, `IsActive`, nav `Tour`). **NO factory, NO methods.** Hard-delete only (no `IsDeleted`/RowVersion) — matches brief. | Add `Create / Update / Deactivate` static + instance methods (Task 2A WBS 2.1) |
| `ContentTours.Domain/Entities/TourPricingTier.cs` | `BaseEntity`. Fields exist (`TourId`, `Name`, `Description?`, `Price` (Money), `Currency`, `MinParticipants`, `MaxParticipants?`, `IsActive`, nav `Tour`). **NO factory, NO methods.** Hard-delete only — matches brief. | Add `Create / Update / Deactivate` (Task 2B WBS 2.1) |
| `ContentTours.Domain/Entities/TourTranslation.cs` | `BaseEntity`. Has `Create()` factory. | Read-only consumer in Task 3 search/suggest |
| `ContentTours.Domain/Enums/TourStatus.cs` | **MISMATCH** — currently `Draft=0, Published=1, Archived=2, Suspended=3`. Brief Task 1 expects `Draft=0, Pending=1, Approved=2, Rejected=3, Suspended=4, Archived=5`. | **BLOCKER on Mahmoud** — Task 1 PW-1 migration must rewrite enum before Task 2B's Adult-tier rule can reference `Pending/Approved`. |
| `ContentTours.Domain/Events/TourPlaceCountChangedDomainEvent.cs` | Exists. | Don't touch |
| `ContentTours.Domain/Events/TourFeaturedChangedDomainEvent.cs` | **MISSING.** | Mohammad creates in Task 3 WBS 3.1 |

### 1.2 Application — what already exists

| File | State | Mohammad's action |
|---|---|---|
| `ContentTours.Application/DependencyInjection.cs` | Registers MediatR + FluentValidation from assembly. ✅ Auto-discovers anything Mohammad adds. | No edit needed |
| `ContentTours.Application/Interfaces/IContentToursUnitOfWork.cs` | Exists (extends `IUnitOfWork`). | Use this for all Task 2 handlers (no event dispatch) |
| `Commands/` | **DOES NOT EXIST** | Mohammad creates 9 command folders |
| `Queries/` | **DOES NOT EXIST** | Mohammad creates 6 query folders |
| `EventHandlers/` | **DOES NOT EXIST** | Mohammad creates `TourFeaturedChangedDomainEventHandler` (Task 3) |
| `Interfaces/IScheduleBookingCountService.cs` | **MISSING.** | Mohammad creates in Task 2A WBS 2.3 |

### 1.3 Infrastructure — what already exists

| File | State | Mohammad's action |
|---|---|---|
| `ContentTours.Infrastructure/Persistence/ContentToursDbContext.cs` | DbSets exist for `Tours, TourSchedules, TourPricingTiers, TourTranslations, OutboxMessages, InboxMessages, …` | Use directly for outbox writes |
| `Persistence/ContentToursUnitOfWork.cs` | Wraps `IUnitOfWork<ContentToursDbContext>`. | Inject in command handlers |
| `Persistence/Configurations/TourScheduleConfiguration.cs` | Exists. ✅ | Likely complete; Mohammad re-reads to confirm `TimeOnly` conversion + index on `(TourId, DayOfWeek, StartTime)` |
| `Persistence/Configurations/TourPricingTierConfiguration.cs` | Exists. ✅ | Re-read to confirm `Money` owned-type mapping + filtered unique index on `(TourId, LOWER(Name))` |
| `Migrations/20260421215356_CreateModel.cs` | Initial migration done. | If config changes needed in WBS 2.5, add a new migration |
| `EventHandlers/TourPlaceCountChangedDomainEventHandler.cs` | Exists. | Don't touch; Mohammad adds `TourFeaturedChangedDomainEventHandler` in same folder |
| `Services/NoOpScheduleBookingCountService.cs` | **MISSING.** | Mohammad creates in Task 2A WBS 2.3 |
| `DependencyInjection.cs` | Registers DbContext, UoW, MediatR, OutboxProcessor. | Mohammad appends: `IScheduleBookingCountService` registration + `ContentToursPermissionCatalog` (if Mahmoud hasn't) |

### 1.4 Presentation — what already exists

`ContentTours.Presentation/ContentToursEndpoints.cs` is **EMPTY** (returns `endpoints` unchanged). Mohammad adds 13 endpoints; Mahmoud adds 11 endpoints. Coordinate the file ownership — recommended split:

```
ContentTours.Presentation/
├── ContentToursEndpoints.cs          (Mahmoud — Task 1 entry point, calls partials)
├── TourEndpoints.cs                  (Mahmoud — endpoints 1–11)
├── TourScheduleEndpoints.cs          (Mohammad — endpoints 12–15)
├── TourPricingTierEndpoints.cs       (Mohammad — endpoints 16–19)
└── TourSearchEndpoints.cs            (Mohammad — endpoints 20–24)
```

`ContentToursEndpoints.MapContentToursEndpoints` chains: `endpoints.MapTourEndpoints().MapTourScheduleEndpoints().MapTourPricingTierEndpoints().MapTourSearchEndpoints()`.

### 1.5 Contracts — what already exists

| File | State | Mohammad's action |
|---|---|---|
| `ContentTours.Contracts/IntegrationEvents/PlaceTourCountUpdatedIntegrationEvent.cs` | Exists. | Don't touch |
| `IntegrationEvents/TourPricingTierChangedIntegrationEvent.cs` | **MISSING.** | Create (Task 2B WBS 2.4) |
| `IntegrationEvents/TourScheduleChangedIntegrationEvent.cs` | **MISSING.** | Create only if Booking team requests (Task 2A WBS 2.2 — optional) |
| `IntegrationEvents/TourFeaturedChangedIntegrationEvent.cs` | **MISSING.** | Create (Task 3 WBS 3.1) |
| `Authorization/ContentToursFeatures.cs` | **MISSING.** | Coordinate with Mahmoud — owner is Task 1 but Mohammad adds entries if not yet done |
| `Authorization/ContentToursPermissionCatalog.cs` | **MISSING.** | Same as above |

### 1.6 Shared kernel signatures (verified, do not deviate)

```csharp
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    IEnumerable<Claim> Claims { get; }
    string? GetClaim(string claimType);
    bool HasPermission(string permission);
    bool IsInRole(string role);
}

public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
public interface ICommandHandler<in TCommand>            : IRequestHandler<TCommand, Result>            where TCommand : ICommand;
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse>;

public interface IQuery<TResponse>                       : IRequest<Result<TResponse>> { }
public interface IQueryHandler<in TQuery, TResponse>     : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? CacheDuration { get; }
    IReadOnlyList<string> Tags => [];
}
```

`HybridCache` (Microsoft.Extensions.Caching.Hybrid v9.3.x) is the cache primitive — use `RemoveByTagAsync(tag, ct)` after every successful mutation.

---

## 2. Cross-Team Coordination Items (BEFORE Mohammad starts)

### 2.1 Hard blockers on Mahmoud (Task 1)

Resolution required by **Tue 2026-05-05 EOD** to keep Mohammad on track.

| # | Item | Why it blocks Mohammad |
|---|---|---|
| C1 | Rewrite `TourStatus` enum to `Draft=0, Pending=1, Approved=2, Rejected=3, Suspended=4, Archived=5` (PW-1 migration) | Task 2B Adult-tier guard checks `tour.Status is Pending or Approved`. Won't compile against current enum. |
| C2 | Add audit fields to `Tour` (`SubmittedAt`, `ApprovedAt`, `ApprovedByUserId`, `RejectedAt`, `RejectedByUserId`, `RejectionReason`, `SuspendedAt`, `SuspensionReason`, `ReinstatedAt`) via migration `AddTourApprovalAuditFields` | Task 1 Submit/Approve/Reject must compile before Mohammad can implement Task 2/3 dependencies on Pending/Approved status. |
| C3 | Implement `Tour.Submit()` with the 8-gate validation (per brief) | Task 2 ships `TourSchedule` and `TourPricingTier`; Task 1 submit-gate calls `schedules.Any(s => s.IsActive)` + `tiers.Any(t => t.IsActive && t.Name eq "Adult")`. If Submit doesn't exist, the system is incomplete. |
| C4 | Create `ContentTours.Contracts/Authorization/ContentToursFeatures.cs` + `ContentToursPermissionCatalog.cs` with: `Tour.Create / Tour.Update / Tour.Delete / Tour.Submit / Tour.Approve / Tour.Reject / Tour.Suspend / Tour.Reinstate / Tour.ReadOwn / Tour.ReadAny / Tour.Feature` | Mohammad's 8 write endpoints + ToggleTourFeatured + MyTours all reference these constants. Without them, endpoints can't compile. |
| C5 | Register `ContentToursPermissionCatalog` in `ContentTours.Infrastructure/DependencyInjection.cs` (`services.AddSingleton<IPermissionCatalog, ContentToursPermissionCatalog>();`) | PermissionSeeder discovery. |

**Fallback if Mahmoud delays past Tue 2026-05-05**: Mohammad creates `ContentToursFeatures` + `ContentToursPermissionCatalog` himself with **all** Tour permissions (not just his own three) and lets Mahmoud rebase later. Email lead/Mahmoud explicitly when this happens.

### 2.2 Coordination items (non-blocking)

| # | Item | Owner |
|---|---|---|
| N1 | Add `Tour.SetFeatured(bool isFeatured, Guid changedByUserId)` to Tour aggregate raising `TourFeaturedChangedDomainEvent` only when value changes | Mohammad (Task 3 WBS 3.1) — brief permits |
| N2 | Confirm Task 1 list/detail DTOs (`TourSummaryDto`, `TourDetailDto`) live in `ContentTours.Application/Queries/Tour/Common/` so Mohammad can reuse for Search / Featured / MyTours | Mahmoud + Mohammad sync |
| N3 | Confirm `TourSchedule` + `TourPricingTier` are listed in `Tour.Submit`'s gate check (existing schedules collection + pricing tiers collection navigations on Tour) | Mahmoud — ensure nav properties wired |
| N4 | Confirm endpoint registration entry-point in `Program.cs`: `app.MapContentToursEndpoints()` is called | Tech lead — already registered for Task 1 |

---

## 3. Architecture Decisions (locked)

These are non-negotiable per `agent-context.md` rules:

1. **`TourSchedule` + `TourPricingTier` raise NO domain events.** They are `BaseEntity`, not `IAggregateRoot`. The UoW only dispatches events from aggregate-root entries. Any `AddDomainEvent` on these entities is silently dropped. Use **direct outbox writes** (see §3.4).

2. **All write handlers use `IContentToursUnitOfWork`** (plain `SaveChangesAsync` — no event dispatch). The aggregate-event-dispatching `IUnitOfWork<ContentToursDbContext>` is only used for `ToggleTourFeatured` (Task 3) which mutates the Tour aggregate.

3. **All queries implement `ICacheableQuery`** with the keys/tags/TTLs defined in the brief's `B7/B4/B10` cache policy tables.

4. **Cache invalidation goes after `SaveChangesAsync` succeeds** — never before. If save fails the cache stays warm but consistent.

5. **Endpoints decorated with `[MustHavePermission(ContentToursFeatures.Tour, AppAction.X)]` or `.AllowAnonymous()`** — never `.RequireAuthorization()` alone.

6. **Owner check pattern** (run AFTER permission check, BEFORE mutation):
   ```csharp
   if (tour.CreatedByUserId != currentUser.UserId!.Value && !currentUser.IsInRole("Admin"))
       return Result.Forbidden<T>("Tour.NotOwner", "You do not own this tour.");
   ```

7. **Soft-deleted Tour returns `Tour.NotFound 404`** to non-admins — do NOT leak existence.

8. **Currency comparisons** always `StringComparison.OrdinalIgnoreCase` (parent Tour stores upper, requests may arrive lower).

### 3.1 Direct outbox-write pattern (Task 2 only)

```csharp
// In each Task 2 command handler, BEFORE SaveChangesAsync:
var integrationEvent = new TourPricingTierChangedIntegrationEvent(
    TierId: tier.Id,
    TourId: tour.Id,
    NewPrice: tier.Price.Amount,
    Currency: tier.Currency,
    ChangeType: "Created");

dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
await unitOfWork.SaveChangesAsync(ct);    // atomic: tier insert + outbox row commit together

// AFTER successful save — cache invalidation:
await cache.RemoveByTagAsync($"tour-pricing:{tour.Id}", ct);
await cache.RemoveByTagAsync($"tour:{tour.Id}", ct);
```

`OutboxMessage.Create<T>(T integrationEvent)` is in `YallaJo.SharedKernel.Infrastructure.Outbox`. Confirm import.

### 3.2 Domain event pattern (Task 3 only — ToggleTourFeatured)

```csharp
// Tour.SetFeatured (added by Mohammad if Mahmoud didn't):
public void SetFeatured(bool isFeatured, Guid changedByUserId)
{
    if (IsFeatured == isFeatured) return;        // ERR-009 idempotency: NO event when value unchanged
    IsFeatured = isFeatured;
    MarkUpdated();
    AddDomainEvent(new TourFeaturedChangedDomainEvent(
        TourId: Id, IsFeatured: isFeatured, ChangedByUserId: changedByUserId, ChangedAt: DateTime.UtcNow));
}

// In ToggleTourFeaturedCommandHandler, use IUnitOfWork<ContentToursDbContext> (the EVENT-DISPATCHING UoW).
// The handler `TourFeaturedChangedDomainEventHandler` writes the integration event to outbox.
```

---

## 4. Task 2A — TourSchedule (4 endpoints · 13 hrs)

### 4.1 File deliverables

```
ContentTours.Domain/
└── Entities/
    └── TourSchedule.cs                                     [EDIT — add Create/Update/Deactivate]

ContentTours.Application/
├── Commands/TourSchedule/
│   ├── CreateTourSchedule/
│   │   ├── CreateTourScheduleCommand.cs                    [NEW]
│   │   ├── CreateTourScheduleCommandHandler.cs             [NEW]
│   │   ├── CreateTourScheduleCommandValidator.cs           [NEW]
│   │   ├── CreateTourScheduleResult.cs                     [NEW]  (record { int Created, int Skipped })
│   │   └── RecurrencePattern.cs                            [NEW]  (enum Once/Daily/Weekly/Custom)
│   ├── UpdateTourSchedule/
│   │   ├── UpdateTourScheduleCommand.cs                    [NEW]
│   │   ├── UpdateTourScheduleCommandHandler.cs             [NEW]
│   │   └── UpdateTourScheduleCommandValidator.cs           [NEW]
│   └── DeleteTourSchedule/
│       ├── DeleteTourScheduleCommand.cs                    [NEW]
│       ├── DeleteTourScheduleCommandHandler.cs             [NEW]
│       └── DeleteTourScheduleCommandValidator.cs           [NEW]
├── Queries/TourSchedule/
│   ├── ListTourSchedules/
│   │   ├── ListTourSchedulesQuery.cs                       [NEW]
│   │   ├── ListTourSchedulesQueryHandler.cs                [NEW]
│   │   └── ListTourSchedulesQueryValidator.cs              [NEW]
│   └── Common/
│       └── TourScheduleDto.cs                              [NEW]
├── Interfaces/
│   └── IScheduleBookingCountService.cs                     [NEW]
└── Caching/
    └── TourScheduleCacheKeys.cs                            [NEW]

ContentTours.Infrastructure/
├── Services/
│   └── NoOpScheduleBookingCountService.cs                  [NEW]
└── DependencyInjection.cs                                  [EDIT — register service]

ContentTours.Contracts/
└── IntegrationEvents/
    └── TourScheduleChangedIntegrationEvent.cs              [NEW — optional, only if Booking team requests]

ContentTours.Presentation/
└── TourScheduleEndpoints.cs                                [NEW — endpoints 12–15]
```

### 4.2 Domain methods on `TourSchedule`

```csharp
public sealed class TourSchedule : BaseEntity
{
    private TourSchedule() { } // EF

    public Guid TourId { get; private set; }
    public byte DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Tour Tour { get; private set; } = default!;

    public static TourSchedule Create(
        Guid tourId, byte dayOfWeek, TimeOnly startTime, TimeOnly? endTime, bool isActive)
    {
        if (tourId == Guid.Empty) throw new ArgumentException("TourId required.", nameof(tourId));
        if (dayOfWeek > 6)        throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "0..6 only.");
        if (endTime is { } e && e <= startTime)
            throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

        return new TourSchedule
        {
            // Id auto-set by BaseEntity (Guid.CreateVersion7)
            TourId = tourId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = isActive
        };
    }

    public void Update(byte dayOfWeek, TimeOnly startTime, TimeOnly? endTime, bool isActive)
    {
        if (dayOfWeek > 6) throw new ArgumentOutOfRangeException(nameof(dayOfWeek));
        if (endTime is { } e && e <= startTime)
            throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
        // No MarkUpdated — BaseEntity has no UpdatedAt; brief says no RowVersion either
    }

    public void Deactivate() => IsActive = false;
}
```

### 4.3 Endpoint 12 — `GET /api/v1/tours/{id}/schedules` (anonymous, list)

**Query**:
```csharp
public sealed record ListTourSchedulesQuery(Guid TourId, bool ActiveOnly = true)
    : IQuery<IReadOnlyList<TourScheduleDto>>, ICacheableQuery
{
    public string CacheKey => TourScheduleCacheKeys.List(TourId, ActiveOnly);
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
    public IReadOnlyList<string> Tags =>
        [TourScheduleCacheKeys.TagForTour(TourId), TourCacheKeys.TagForTour(TourId)];
}
```

**Handler**: confirms `tour exists && !IsDeleted`; otherwise return `Tour.NotFound 404`. Then projects `dbContext.TourSchedules.Where(s => s.TourId == TourId && (!ActiveOnly || s.IsActive)).OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime).Select(...)`.

**Cache keys** (`TourScheduleCacheKeys`):
```csharp
public static class TourScheduleCacheKeys
{
    public static string List(Guid tourId, bool activeOnly) =>
        $"ct:tour-schedules:{tourId}:active={activeOnly}";
    public static string TagForTour(Guid tourId) => $"tour-schedules:{tourId}";
}
```

### 4.4 Endpoint 13 — `POST /api/v1/tours/{id}/schedules` (recurrence engine)

The hard part. Algorithm in handler (NOT in entity — handler orchestrates):

```text
1. Load tour. If null/IsDeleted → Tour.NotFound.
   If status in {Suspended, Archived} → log Warning but proceed (per B1.1).

2. Owner-or-admin check.

3. Validate request shape via FluentValidation (covers Pattern in enum,
   DaysOfWeek non-empty when Weekly, CustomDates non-empty when Custom,
   each CustomDate in [today, today+90], EndTime > StartTime).

4. Compute date window:
       validFrom := request.ValidFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
       validTo   := request.ValidTo   ?? validFrom.AddDays(90);
       cap       := DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);
       validTo   := MIN(validTo, cap);

5. Build candidate list per pattern (same algorithm as brief's pseudocode).
   If candidates.Count > 120 → TourSchedule.ExpansionTooLarge 422.

6. Load existing schedules for this tour grouped by DayOfWeek.

7. For each DayOfWeek in (existingActive ∪ candidates), run overlap check:
       sorted := combined.Where(IsActive).OrderBy(StartTime).ThenBy(EndTime ?? TimeOnly.MaxValue);
       for each consecutive pair (a, b):
           endA := a.EndTime ?? TimeOnly.MaxValue;
           if (b.StartTime < endA) → TourSchedule.OverlapDetected 422
                                     payload { dayOfWeek, a, b }
   (Note: open-ended schedules block the rest of the day — covered by TimeOnly.MaxValue.)

8. Idempotency filter: emit only candidates where
       !await db.TourSchedules.AnyAsync(s =>
            s.TourId == tourId && s.DayOfWeek == day && s.StartTime == startTime, ct)

9. For each emitted candidate:
       var schedule = TourSchedule.Create(tourId, day, startTime, endTime, isActive: request.IsActive);
       dbContext.TourSchedules.Add(schedule);
       (optional) dbContext.OutboxMessages.Add(OutboxMessage.Create(
            new TourScheduleChangedIntegrationEvent(schedule.Id, tourId, day, startTime, endTime, "Created")));

10. await unitOfWork.SaveChangesAsync(ct);

11. Cache invalidation:
       await cache.RemoveByTagAsync($"tour-schedules:{tourId}", ct);
       await cache.RemoveByTagAsync($"tour:{tourId}", ct);

12. Return Result.Created(new CreateTourScheduleResult(Created: emitted.Count, Skipped: dedup.Count));
```

**Error mapping**:

| Code | Outcome | Trigger |
|---|---|---|
| `Tour.NotFound` | NotFound | tour null or IsDeleted |
| `Tour.NotOwner` | Forbidden | owner check failed |
| `TourSchedule.PatternParamsInvalid` | Invalid | (caught by validator) |
| `TourSchedule.CustomDateOutOfRange` | UnprocessableEntity | post-validation guard |
| `TourSchedule.ExpansionTooLarge` | UnprocessableEntity | candidates.Count > 120 |
| `TourSchedule.OverlapDetected` | UnprocessableEntity | algorithm step 7 |

> **Note on 422**: `Result.Failure(error, Outcome.UnprocessableEntity)` — confirm the `Outcome` enum has `UnprocessableEntity = 422` (the templates list it). If the codebase uses `Outcome.Conflict` for semantic validation, fall back to that and log the deviation.

### 4.5 Endpoint 14 — `PUT /api/v1/tours/{id}/schedules/{scheduleId}`

Update single row:
1. Load tour + schedule (`schedule.TourId == tourId`). Either missing → `Tour.NotFound` / `TourSchedule.NotFound`.
2. Owner check.
3. Run overlap check on the proposed (existing rows minus this one + the proposed change).
4. `schedule.Update(dayOfWeek, startTime, endTime, isActive)`.
5. (optional) Outbox `TourScheduleChangedIntegrationEvent ChangeType="Updated"`.
6. SaveChanges + cache bust.

### 4.6 Endpoint 15 — `DELETE /api/v1/tours/{id}/schedules/{scheduleId}` (with deletion guard)

```csharp
public interface IScheduleBookingCountService
{
    Task<int> GetFutureBookingCountForScheduleAsync(Guid scheduleId, CancellationToken ct);
}

internal sealed class NoOpScheduleBookingCountService(
    ILogger<NoOpScheduleBookingCountService> logger) : IScheduleBookingCountService
{
    public Task<int> GetFutureBookingCountForScheduleAsync(Guid scheduleId, CancellationToken ct)
    {
        logger.LogDebug("No booking module; returning 0 for scheduleId={ScheduleId}", scheduleId);
        return Task.FromResult(0);
    }
}

// Register in Infrastructure DI:
services.AddScoped<IScheduleBookingCountService, NoOpScheduleBookingCountService>();
```

Handler calls `await bookingCountService.GetFutureBookingCountForScheduleAsync(scheduleId, ct)`. If `> 0` → `Result.Failure<Guid>(new Error("TourSchedule.DeleteBlocked", $"{count} future bookings reference this schedule."), Outcome.Conflict)` with payload visible in the Error message — production payload format TBD with tech lead but minimum: include `futureBookings` count.

Then `dbContext.TourSchedules.Remove(schedule)` (HARD delete), optional outbox `ChangeType="Deleted"`, save, cache-bust.

### 4.7 Validators

```csharp
public sealed class CreateTourScheduleCommandValidator : AbstractValidator<CreateTourScheduleCommand>
{
    public CreateTourScheduleCommandValidator()
    {
        RuleFor(x => x.Pattern).IsInEnum();

        RuleFor(x => x.DaysOfWeek)
            .NotEmpty().WithMessage("DaysOfWeek required for Weekly pattern.")
            .When(x => x.Pattern == RecurrencePattern.Weekly);
        RuleForEach(x => x.DaysOfWeek)
            .InclusiveBetween((byte)0, (byte)6)
            .When(x => x.DaysOfWeek is not null);

        RuleFor(x => x.CustomDates)
            .NotEmpty().WithMessage("CustomDates required for Custom pattern.")
            .When(x => x.Pattern == RecurrencePattern.Custom);
        RuleForEach(x => x.CustomDates!)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow)
                    && d <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90))
            .WithMessage("Custom date must be in [today, today + 90 days].")
            .When(x => x.CustomDates is not null);

        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .When(x => x.EndTime.HasValue);
        RuleFor(x => x.ValidTo)
            .GreaterThan(x => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.ValidTo.HasValue);
    }
}

public sealed class UpdateTourScheduleCommandValidator : AbstractValidator<UpdateTourScheduleCommand>
{
    public UpdateTourScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.ScheduleId).NotEqual(Guid.Empty);
        RuleFor(x => x.DayOfWeek).InclusiveBetween((byte)0, (byte)6);
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).When(x => x.EndTime.HasValue);
    }
}

public sealed class DeleteTourScheduleCommandValidator : AbstractValidator<DeleteTourScheduleCommand>
{
    public DeleteTourScheduleCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.ScheduleId).NotEqual(Guid.Empty);
    }
}
```

### 4.8 Endpoint registration (`TourScheduleEndpoints.cs`)

```csharp
public static IEndpointRouteBuilder MapTourScheduleEndpoints(this IEndpointRouteBuilder endpoints)
{
    var group = endpoints.MapGroup("/api/v1/tours/{id:guid}/schedules").WithTags("Tour Schedules");

    group.MapGet("/", ListTourSchedules)
        .WithName("ListTourSchedules").AllowAnonymous()
        .Produces<IReadOnlyList<TourScheduleDto>>(200)
        .ProducesProblem(404);

    group.MapPost("/", CreateTourSchedule)
        .WithName("CreateTourSchedule")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update))
        .Produces<CreateTourScheduleResult>(201).ProducesValidationProblem()
        .ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

    group.MapPut("/{scheduleId:guid}", UpdateTourSchedule)
        .WithName("UpdateTourSchedule")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update))
        .Produces(204).ProducesValidationProblem()
        .ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

    group.MapDelete("/{scheduleId:guid}", DeleteTourSchedule)
        .WithName("DeleteTourSchedule")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update))
        .Produces(204).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

    return endpoints;
}
```

---

## 5. Task 2B — TourPricingTier (4 endpoints · 15 hrs)

### 5.1 File deliverables

```
ContentTours.Domain/
└── Entities/
    └── TourPricingTier.cs                                  [EDIT — add Create/Update/Deactivate]

ContentTours.Application/
├── Commands/TourPricingTier/
│   ├── CreateTourPricingTier/                              [NEW Cmd/Handler/Validator/Result]
│   ├── UpdateTourPricingTier/                              [NEW Cmd/Handler/Validator]
│   └── DeleteTourPricingTier/                              [NEW Cmd/Handler/Validator]
├── Queries/TourPricingTier/
│   ├── ListTourPricingTiers/                               [NEW Query/Handler/Validator]
│   └── Common/
│       └── TourPricingTierDto.cs                           [NEW]
└── Caching/
    └── TourPricingTierCacheKeys.cs                         [NEW]

ContentTours.Contracts/
└── IntegrationEvents/
    └── TourPricingTierChangedIntegrationEvent.cs           [NEW]

ContentTours.Presentation/
└── TourPricingTierEndpoints.cs                             [NEW — endpoints 16–19]
```

### 5.2 Domain methods on `TourPricingTier`

```csharp
public sealed class TourPricingTier : BaseEntity
{
    private TourPricingTier() { } // EF

    public Guid TourId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;
    public int MinParticipants { get; private set; } = 1;
    public int? MaxParticipants { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Tour Tour { get; private set; } = default!;

    public static TourPricingTier Create(
        Guid tourId, string name, string? description,
        Money price, string currency,
        int minParticipants, int? maxParticipants)
    {
        if (tourId == Guid.Empty) throw new ArgumentException("TourId required.", nameof(tourId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name required.", nameof(name));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be 3-letter ISO code.", nameof(currency));
        if (minParticipants < 1)
            throw new ArgumentOutOfRangeException(nameof(minParticipants));
        if (maxParticipants is { } m && m <= minParticipants)
            throw new ArgumentException("MaxParticipants must exceed MinParticipants.", nameof(maxParticipants));

        return new TourPricingTier
        {
            TourId = tourId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Price = price,
            Currency = currency.ToUpperInvariant(),
            MinParticipants = minParticipants,
            MaxParticipants = maxParticipants,
            IsActive = true
        };
    }

    public void Update(string name, string? description, Money price, string currency,
        int minParticipants, int? maxParticipants, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException(nameof(currency));
        if (minParticipants < 1) throw new ArgumentOutOfRangeException(nameof(minParticipants));
        if (maxParticipants is { } m && m <= minParticipants)
            throw new ArgumentException(nameof(maxParticipants));

        Name = name.Trim();
        Description = description?.Trim();
        Price = price;
        Currency = currency.ToUpperInvariant();
        MinParticipants = minParticipants;
        MaxParticipants = maxParticipants;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public bool IsAdult => Name.Equals("Adult", StringComparison.OrdinalIgnoreCase);
}
```

### 5.3 Adult-tier guard helper

Encapsulate the rule once, call from Update + Delete handlers:

```csharp
// In CreateTourPricingTier/Common/AdultTierGuard.cs
internal static class AdultTierGuard
{
    /// Returns Conflict when removing/deactivating the LAST active Adult tier
    /// on a Pending or Approved tour. Returns Success for Draft.
    public static Result EnsureCanRemoveOrDeactivateAdult(
        Tour tour,
        TourPricingTier targetTier,
        IReadOnlyCollection<TourPricingTier> allTiersInTour)
    {
        if (!targetTier.IsAdult) return Result.Success();
        if (tour.Status == TourStatus.Draft) return Result.Success();   // brief B1.3

        var remainingActiveAdult = allTiersInTour
            .Where(t => t.Id != targetTier.Id && t.IsActive && t.IsAdult)
            .Any();
        if (remainingActiveAdult) return Result.Success();

        return Result.Failure(
            new Error("TourPricingTier.AdultTierRequired",
                "Cannot delete or deactivate the last active Adult tier on a Pending or Approved tour."),
            Outcome.Conflict);
    }
}
```

### 5.4 Handler logic — Create/Update/Delete

**Create (Endpoint 17)**:
```text
1. Load Tour. If null/IsDeleted → Tour.NotFound 404.
2. Owner-or-admin check → Tour.NotOwner 403.
3. Validate currency match (case-insensitive) → TourPricingTier.CurrencyMismatch 400.
4. Check name uniqueness (case-insensitive) within tour → TourPricingTier.NameConflict 409.
5. tier := TourPricingTier.Create(...);
6. tierRepo.Add(tier) OR dbContext.TourPricingTiers.Add(tier);
7. dbContext.OutboxMessages.Add(OutboxMessage.Create(
       new TourPricingTierChangedIntegrationEvent(tier.Id, tour.Id, tier.Price.Amount, tier.Currency, "Created")));
8. await unitOfWork.SaveChangesAsync(ct);
9. await cache.RemoveByTagAsync($"tour-pricing:{tour.Id}", ct);
   await cache.RemoveByTagAsync($"tour:{tour.Id}", ct);
10. Return Result.Created<CreateTourPricingTierResult>(...).
```

**Update (Endpoint 18)**:
- Same prelude (tour load, owner check, currency match).
- Load tier (`tier.TourId == tourId`). If missing → `TourPricingTier.NotFound 404`.
- If name changed, re-check uniqueness.
- If `request.IsActive == false` AND `tier.IsAdult` → run `AdultTierGuard.EnsureCanRemoveOrDeactivateAdult`.
- `tier.Update(...)`.
- Outbox `ChangeType = "Updated"` (or `"Deactivated"` if `IsActive` flipped to false).
- Save + cache-bust.

**Delete (Endpoint 19)**:
- Load tour + tier.
- Owner check.
- Adult-tier guard.
- `dbContext.TourPricingTiers.Remove(tier);` (HARD delete).
- Outbox `ChangeType = "Deleted"`.
- Save + cache-bust.

### 5.5 List query (Endpoint 16)

```csharp
public sealed record ListTourPricingTiersQuery(
    Guid TourId,
    bool ActiveOnly,
    string LanguageCode)
    : IQuery<IReadOnlyList<TourPricingTierDto>>, ICacheableQuery
{
    public string CacheKey => $"ct:tour-pricing:{TourId}:active={ActiveOnly}:lang:{LanguageCode}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags =>
        [TourPricingTierCacheKeys.TagForTour(TourId), TourCacheKeys.TagForTour(TourId)];
}
```

Handler: anonymous caller → force `ActiveOnly = true`. Owner / admin → respect request. Detect via `currentUser.IsAuthenticated && (tour.CreatedByUserId == currentUser.UserId || currentUser.IsInRole("Admin"))`.

`Accept-Language` is read from `IHttpContextAccessor` (or via a shared kernel `ILanguageContext` if defined) at the endpoint level and passed into the query.

### 5.6 Validators

```csharp
public sealed class CreateTourPricingTierCommandValidator : AbstractValidator<CreateTourPricingTierCommand>
{
    public CreateTourPricingTierCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.MinParticipants).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaxParticipants)
            .GreaterThan(x => x.MinParticipants)
            .When(x => x.MaxParticipants.HasValue);
    }
}
```

(Update validator mirrors Create; Delete validator just checks GUIDs.)

### 5.7 Endpoint registration (`TourPricingTierEndpoints.cs`)

Same shape as schedules: `GET` anonymous, `POST/PUT/DELETE` with `MustHavePermission(Tour, Update)`.

---

## 6. Task 3 — Search / Suggest / Featured / MyTours / FeatureToggle (5 endpoints · 24 hrs)

### 6.1 File deliverables

```
ContentTours.Domain/
├── Entities/
│   └── Tour.cs                                             [EDIT — add SetFeatured if missing]
└── Events/
    └── TourFeaturedChangedDomainEvent.cs                   [NEW]

ContentTours.Application/
├── Commands/Tour/
│   └── ToggleTourFeatured/                                 [NEW Cmd/Handler/Validator]
├── Queries/Tour/
│   ├── SearchTours/
│   │   ├── SearchToursQuery.cs                             [NEW]
│   │   ├── SearchToursQueryHandler.cs                      [NEW]
│   │   ├── SearchToursQueryValidator.cs                    [NEW]
│   │   ├── SearchToursResult.cs                            [NEW — items, total, page, facets, appliedFilters]
│   │   ├── SearchSort.cs                                   [NEW — enum]
│   │   ├── SearchTokenizer.cs                              [NEW — pure helper]
│   │   ├── FacetComputer.cs                                [NEW — pure helper]
│   │   └── FacetRow.cs                                     [NEW — internal record]
│   ├── SuggestTours/                                       [NEW Query/Handler/Validator + DTO]
│   ├── ListFeaturedTours/                                  [NEW Query/Handler]
│   ├── ListMyTours/                                        [NEW Query/Handler/Validator]
│   └── Common/
│       ├── TourSummaryDto.cs                               [REUSE Mahmoud's if shipped, else NEW]
│       └── PaginatedResult.cs                              [REUSE shared kernel]
└── Caching/
    └── TourSearchCacheKeys.cs                              [NEW]

ContentTours.Infrastructure/
└── EventHandlers/
    └── TourFeaturedChangedDomainEventHandler.cs            [NEW]

ContentTours.Contracts/
└── IntegrationEvents/
    └── TourFeaturedChangedIntegrationEvent.cs              [NEW]

ContentTours.Presentation/
└── TourSearchEndpoints.cs                                  [NEW — endpoints 20–24]
```

### 6.2 `Tour.SetFeatured` (idempotent)

```csharp
// Add to Tour.cs in Domain/Entities (only if Mahmoud hasn't added it):
public void SetFeatured(bool isFeatured, Guid changedByUserId)
{
    if (IsFeatured == isFeatured) return;     // ERR-009 idempotency
    IsFeatured = isFeatured;
    MarkUpdated();
    AddDomainEvent(new TourFeaturedChangedDomainEvent(
        TourId: Id,
        IsFeatured: isFeatured,
        ChangedByUserId: changedByUserId,
        ChangedAt: DateTime.UtcNow));
}
```

### 6.3 Endpoint 20 — `SearchTours` (the 10-hour beast, WBS 3.3)

**Pipeline** (split into 5 phases for readability):

1. **Tokenize** (in handler, before query):
   - Trim + ToLowerInvariant.
   - Split on `\s+`.
   - Drop tokens with length < 2.
   - HashSet for dedup.
   - Cap at 10.
   - If zero surviving tokens AND no filters → `Tour.SearchQueryRequired 400`.

2. **Build filtered IQueryable**:
   ```csharp
   var filtered = dbContext.Tours
       .AsNoTracking()
       .Where(t => t.Status == TourStatus.Approved && !t.IsDeleted);
   filtered = ApplyFilters(filtered, request);   // categoryId, placeId, priceMin/Max, difficulty,
                                                  // durationMinutesMin/Max, isChildFriendly, isAccessible,
                                                  // isInstantBooking, hasDiscount, minRating, languageCode
   ```

3. **Compute facets** (capped at 5000 rows — see brief B3 code sample). Build `FacetRow` projection (pure SQL select), materialise, run `FacetComputer.Compute(...)` in memory.

4. **Apply sort + paging**:
   - `relevance` (when `q` set): use `.Select(t => new { t, score = ... }).Where(x => x.score > 0).OrderByDescending(x => x.score).ThenByDescending(t => t.CreatedAt)`. Score formula per brief B2 — translate token matches to `(EF.Functions.Like(t.Name, $"%{token}%") ? 1 : 0)` summed over the (≤10) tokens. Use `Math.Log10(t.BookingCount + 1)` if EF/SQL supports it; otherwise compute in memory after a `.Take(N*PageSize)` cap.
   - Other sorts: pure SQL `OrderBy`.
   - `Skip(skip).Take(pageSize).Select(SummaryProjection).ToListAsync(ct)`.

5. **Return** `SearchToursResult { Items, Total, Page, PageSize, TotalPages, Facets, AppliedFilters }`.

**Implementation note**: The token-match expression in the score formula must be EF-translatable. Build the score expression dynamically:

```csharp
private static IQueryable<TourScoreRow> ApplyRelevanceScore(
    IQueryable<Tour> source, IReadOnlyList<string> tokens, string lang)
{
    return source.Select(t => new TourScoreRow
    {
        Tour = t,
        NameMatches =
            (tokens.Count > 0 && EF.Functions.Like(t.Name, $"%{tokens[0]}%") ? 1 : 0) +
            (tokens.Count > 1 && EF.Functions.Like(t.Name, $"%{tokens[1]}%") ? 1 : 0) +
            // … unrolled to 10 tokens; pad with 0 when fewer than 10
            0,
        DescMatches =
            (tokens.Count > 0 && EF.Functions.Like(t.Description, $"%{tokens[0]}%") ? 1 : 0) +
            // … unrolled likewise
            0,
        Score =
            3 * NameMatches + 1 * DescMatches +
            0.3 * (t.AverageRating ?? 0) +
            (decimal)Math.Log10((double)(t.BookingCount + 1)) +
            1.0m / (1 + (decimal)(DateTime.UtcNow - t.CreatedAt).TotalDays / 30m)
    }).Where(r => r.NameMatches + r.DescMatches > 0);   // AND semantics on at-least-one match
}
```

> **AND-semantic note**: Brief says "If any token is NOT matched in any field → exclude row". Strict AND requires `NameMatches + DescMatches >= tokens.Count`. Verify with tech lead — the brief is internally inconsistent. Recommended: `>= tokens.Count` (true AND) for v1; if business prefers softer match, switch to `> 0` (any-match).

**Cache**: `ct:tours:search:{Sha1(q + filters JSON + sort + page + size + lang)}` 2 min, tags `tours:search`, `tours:list`.

### 6.4 Endpoint 21 — `SuggestTours` (3 hrs, WBS 3.2)

```csharp
public sealed record SuggestToursQuery(string Q, string LanguageCode)
    : IQuery<IReadOnlyList<TourSuggestDto>>, ICacheableQuery
{
    public string CacheKey => TourSearchCacheKeys.Suggest(Q, LanguageCode);
    public TimeSpan? CacheDuration => TimeSpan.FromSeconds(30);
    public IReadOnlyList<string> Tags => ["tours:suggest", "tours:list"];
}

public sealed record TourSuggestDto(Guid Id, string Name, string Slug, string? ThumbnailUrl);
```

Handler:
```csharp
var ql = request.Q.Trim().ToLowerInvariant();
return await dbContext.Tours.AsNoTracking()
    .Where(t => t.Status == TourStatus.Approved && !t.IsDeleted)
    .Where(t => EF.Functions.Like(t.Name, $"{ql}%")
             || t.Translations.Any(tr => tr.LanguageCode == request.LanguageCode
                                      && EF.Functions.Like(tr.Name, $"{ql}%")))
    .OrderByDescending(t => t.BookingCount).ThenBy(t => t.Name)
    .Take(10)
    .Select(t => new TourSuggestDto(t.Id, t.Name, t.Slug, /* thumbnail TBD */ null))
    .ToListAsync(ct);
```

**Index suggestion**: coordinate with tech lead on `IX_Tours_Status_IsDeleted_Name` (filtered: `Status=Approved AND IsDeleted=0`). Add via migration if p95 > 100ms.

### 6.5 Endpoint 22 — `ListFeaturedTours` (WBS 3.1)

Hard-coded 20 results, `IsFeatured == true && Status == Approved && !IsDeleted`, ordered by `BookingCount DESC, AverageRating DESC`, projected to `TourSummaryDto`. Cache 10 min, tags `tours:featured`, `tours:list`.

### 6.6 Endpoint 23 — `ListMyTours` (WBS 3.1)

```csharp
public sealed record ListMyToursQuery(
    int Page, int PageSize, TourStatus? Status, string? Sort,
    Guid? ProviderUserId,    // honoured ONLY if caller is admin
    bool IncludeDeleted)     // honoured ONLY if caller is admin
    : IQuery<PaginatedResult<TourSummaryDto>>, ICacheableQuery
{
    public string CacheKey => $"ct:my-tours:{ProviderUserId}:p{Page}:s{PageSize}:status:{Status}:sort:{Sort}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
    public IReadOnlyList<string> Tags => [$"my-tours:{ProviderUserId}"];
}
```

Endpoint constructs the query — the **endpoint** is responsible for IDOR scrubbing:

```csharp
private static async Task<IResult> ListMyTours(
    [AsParameters] ListMyToursRequest req, ISender sender, ICurrentUser user, CancellationToken ct)
{
    var isAdmin = user.IsInRole("Admin") || user.HasPermission("ContentTours.Tour.ReadAny");
    var effectiveUserId = isAdmin && req.ProviderUserId is { } pid ? pid : user.UserId!.Value;
    var includeDeleted = isAdmin && req.IncludeDeleted;
    var query = new ListMyToursQuery(req.Page, req.PageSize, req.Status, req.Sort,
                                     effectiveUserId, includeDeleted);
    var result = await sender.Send(query, ct);
    return result.ToHttp();
}
```

Handler builds the IQueryable, applies status filter (keeps ALL statuses if `Status == null`), applies sort (default `status_priority` then `UpdatedAt DESC`), paginates, projects to `TourSummaryDto`. Pagination max = 100 (`PageSize.InclusiveBetween(1,100)`).

### 6.7 Endpoint 24 — `ToggleTourFeatured` (WBS 3.1)

```csharp
public sealed record ToggleTourFeaturedCommand(Guid TourId, bool IsFeatured) : ICommand;

public sealed class ToggleTourFeaturedCommandHandler(
    ITourRepository tourRepo,
    IUnitOfWork<ContentToursDbContext> unitOfWork,    // event-dispatching UoW (Tour aggregate raises event)
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ToggleTourFeaturedCommandHandler> logger)
    : ICommandHandler<ToggleTourFeaturedCommand>
{
    public async Task<Result> Handle(ToggleTourFeaturedCommand cmd, CancellationToken ct)
    {
        var tour = await tourRepo.GetByIdAsync(cmd.TourId, ct);
        if (tour is null || tour.IsDeleted)
            return Result.NotFound("Tour.NotFound", $"Tour {cmd.TourId} not found.");
        if (tour.Status != TourStatus.Approved)
            return Result.Failure(
                new Error("Tour.CannotFeatureNonApproved", "Only Approved tours can be featured."),
                Outcome.Conflict);

        var willChange = tour.IsFeatured != cmd.IsFeatured;
        tour.SetFeatured(cmd.IsFeatured, currentUser.UserId!.Value);   // no-op if already that value
        await unitOfWork.SaveChangesAsync(ct);

        if (willChange)
        {
            await cache.RemoveByTagAsync("tours:featured", ct);
            await cache.RemoveByTagAsync($"tour:{tour.Id}", ct);
            await cache.RemoveByTagAsync("tours:list", ct);
            await cache.RemoveByTagAsync("tours:search", ct);
            logger.LogInformation("Tour {TourId} featured set to {IsFeatured} by {UserId}",
                tour.Id, cmd.IsFeatured, currentUser.UserId);
        }

        return Result.Success();   // 204 No Content
    }
}
```

### 6.8 Domain event handler

```csharp
// ContentTours.Infrastructure/EventHandlers/TourFeaturedChangedDomainEventHandler.cs
public sealed class TourFeaturedChangedDomainEventHandler(
    ContentToursDbContext dbContext,
    ILogger<TourFeaturedChangedDomainEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TourFeaturedChangedDomainEvent>>
{
    public Task Handle(
        DomainEventNotification<TourFeaturedChangedDomainEvent> note, CancellationToken ct)
    {
        var evt = note.DomainEvent;
        var integrationEvent = new TourFeaturedChangedIntegrationEvent(
            TourId: evt.TourId,
            IsFeatured: evt.IsFeatured,
            ChangedByUserId: evt.ChangedByUserId,
            ChangedAt: evt.ChangedAt);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(integrationEvent));
        logger.LogInformation("Tour {TourId} featured flag set to {IsFeatured} by {UserId}",
            evt.TourId, evt.IsFeatured, evt.ChangedByUserId);
        return Task.CompletedTask;
    }
}
```

### 6.9 Integration event registry

After creating the three new events, append to `IntegrationEventTypeRegistry` (location TBD — search the SharedKernel.Infrastructure for the type):

```
content-tours.pricing-tier.changed.v1   → TourPricingTierChangedIntegrationEvent
content-tours.schedule.changed.v1       → TourScheduleChangedIntegrationEvent  (only if shipped)
content-tours.tour.featured-changed.v1  → TourFeaturedChangedIntegrationEvent
```

### 6.10 Endpoint registration (`TourSearchEndpoints.cs`)

```csharp
public static IEndpointRouteBuilder MapTourSearchEndpoints(this IEndpointRouteBuilder endpoints)
{
    var pub = endpoints.MapGroup("/api/v1/tours").WithTags("Tour Search");

    pub.MapGet("/search", SearchTours).WithName("SearchTours").AllowAnonymous()
        .Produces<SearchToursResult>(200).ProducesValidationProblem().ProducesProblem(400);

    pub.MapGet("/search/suggest", SuggestTours).WithName("SuggestTours").AllowAnonymous()
        .Produces<IReadOnlyList<TourSuggestDto>>(200).ProducesValidationProblem();

    pub.MapGet("/featured", ListFeaturedTours).WithName("ListFeaturedTours").AllowAnonymous()
        .Produces<IReadOnlyList<TourSummaryDto>>(200);

    pub.MapGet("/provider/my-tours", ListMyTours).WithName("ListMyTours")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.ReadOwn))
        .Produces<PaginatedResult<TourSummaryDto>>(200).ProducesProblem(401).ProducesProblem(403);

    pub.MapPatch("/admin/{id:guid}/feature", ToggleTourFeatured).WithName("ToggleTourFeatured")
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Feature))
        .Produces(204).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

    return endpoints;
}
```

> **AppAction enum extension**: `AppAction.Feature` and `AppAction.ReadOwn` / `AppAction.ReadAny` may need adding to the shared kernel enum if not already present. Coordinate with Mahmoud / tech lead — this is part of cross-team item C4.

---

## 7. Daily Schedule (mapped to Mohammad's WBS)

| Date | Hours | Activity | Deliverable |
|---|---:|---|---|
| **W 2026-05-06** PM | 3 | WBS 2.1 — Domain methods on `TourSchedule` + `TourPricingTier` (Create / Update / Deactivate) | Compiles. Unit-test the Adult-tier guard helper if time permits. |
| **R 2026-05-07** | 6 | WBS 2.2 — `ListTourSchedules` query + `CreateTourSchedule` command (recurrence + overlap engines) | Endpoints 12 + 13 working in Swagger. |
| **F 2026-05-08** | 0 | Buffer / blocked on Mahmoud (Task 1 must finish today) | Verify Mahmoud landed C1–C5 from §2.1. |
| **S 2026-05-09** | rest | — | — |
| **S 2026-05-10** | 4 | WBS 2.3 — `UpdateTourSchedule` + `DeleteTourSchedule` (with `IScheduleBookingCountService` stub) | Endpoints 14 + 15. |
| **S 2026-05-10** | 6 | WBS 2.4 — All 4 TourPricingTier endpoints with Adult-tier guard | Endpoints 16–19. |
| **M 2026-05-11** AM | 2 | WBS 2.5 — Infrastructure DI wiring + integration event registry entries + cache key audit | DI green; events deserializable. |
| **M 2026-05-11** PM-1 | 3 | WBS 2.6 — Presentation: 8 endpoint files (`TourScheduleEndpoints.cs`, `TourPricingTierEndpoints.cs`) | All 8 endpoints reachable via Swagger. |
| **M 2026-05-11** PM-2 | 2 | WBS 2.7 — Validator dossier (final read-through; cover overlap + Adult-tier scenarios) | Validators ship. |
| **T 2026-05-12** AM | 2 | WBS 2.8 — Self-review (`dotnet build`, smoke test 8 endpoints, README update if needed) + open Task 2 PR | **Task 2 complete · 28 hr** |
| **T 2026-05-12** PM | 6 | WBS 3.1 — `ListMyTours` + `ListFeaturedTours` + `Tour.SetFeatured` + `ToggleTourFeatured` + domain event handler | Endpoints 22, 23, 24. |
| **W 2026-05-13** AM | 3 | WBS 3.2 — `SuggestTours` + (optional) prefix-index migration | Endpoint 21. |
| **W 2026-05-13** PM → **R 2026-05-14** PM | 10 | WBS 3.3 — `SearchTours` tokenizer + ranking projection + filters + sort composition | Endpoint 20 partial (no facets yet). |
| **F 2026-05-15** + **S 2026-05-16** | rest | — | — |
| **S 2026-05-17** | 3 | WBS 3.4 — Facet computation (5000-row cap, in-memory aggregation) | Endpoint 20 facets working. |
| **M 2026-05-18** | 2 | WBS 3.5 — Presentation cleanup, Swagger smoke, self-review, PR | **Task 3 complete · 24 hr** |

> **Critical-path warning**: Task 2 deadline (Thu 2026-05-14 17:00) is the day Task 3.3 must START. WBS 2.8 ends T 2026-05-12 12:00 → Task 2 PR review window R/F. If review pushes back, Task 3.3 (10 hr) compresses. Plan a 2-hr buffer **inside** WBS 3.3 by trimming the relevance-score complexity if needed (drop recency or rating boost for v1, ship later).

---

## 8. File-Creation Checklist (count = 47 new + 4 edits)

### 8.1 Domain (3 new + 3 edits)
- [ ] `Tour.cs` — EDIT: add `SetFeatured` (Task 3.1)
- [ ] `TourSchedule.cs` — EDIT: add `Create / Update / Deactivate` (Task 2.1)
- [ ] `TourPricingTier.cs` — EDIT: add `Create / Update / Deactivate` (Task 2.1)
- [ ] `Events/TourFeaturedChangedDomainEvent.cs` — NEW (Task 3.1)

### 8.2 Application — Commands (Task 2: 18 + Task 3: 3 = 21 files)
**TourSchedule** (9):
- [ ] `Commands/TourSchedule/CreateTourSchedule/CreateTourScheduleCommand.cs`
- [ ] `Commands/TourSchedule/CreateTourSchedule/CreateTourScheduleCommandHandler.cs`
- [ ] `Commands/TourSchedule/CreateTourSchedule/CreateTourScheduleCommandValidator.cs`
- [ ] `Commands/TourSchedule/CreateTourSchedule/CreateTourScheduleResult.cs`
- [ ] `Commands/TourSchedule/CreateTourSchedule/RecurrencePattern.cs`
- [ ] `Commands/TourSchedule/UpdateTourSchedule/UpdateTourScheduleCommand.cs`
- [ ] `Commands/TourSchedule/UpdateTourSchedule/UpdateTourScheduleCommandHandler.cs`
- [ ] `Commands/TourSchedule/UpdateTourSchedule/UpdateTourScheduleCommandValidator.cs`
- [ ] `Commands/TourSchedule/DeleteTourSchedule/DeleteTourScheduleCommand.cs`
- [ ] `Commands/TourSchedule/DeleteTourSchedule/DeleteTourScheduleCommandHandler.cs`
- [ ] `Commands/TourSchedule/DeleteTourSchedule/DeleteTourScheduleCommandValidator.cs`

**TourPricingTier** (10):
- [ ] `Commands/TourPricingTier/CreateTourPricingTier/CreateTourPricingTierCommand.cs`
- [ ] `Commands/TourPricingTier/CreateTourPricingTier/CreateTourPricingTierCommandHandler.cs`
- [ ] `Commands/TourPricingTier/CreateTourPricingTier/CreateTourPricingTierCommandValidator.cs`
- [ ] `Commands/TourPricingTier/CreateTourPricingTier/CreateTourPricingTierResult.cs`
- [ ] `Commands/TourPricingTier/UpdateTourPricingTier/UpdateTourPricingTierCommand.cs`
- [ ] `Commands/TourPricingTier/UpdateTourPricingTier/UpdateTourPricingTierCommandHandler.cs`
- [ ] `Commands/TourPricingTier/UpdateTourPricingTier/UpdateTourPricingTierCommandValidator.cs`
- [ ] `Commands/TourPricingTier/DeleteTourPricingTier/DeleteTourPricingTierCommand.cs`
- [ ] `Commands/TourPricingTier/DeleteTourPricingTier/DeleteTourPricingTierCommandHandler.cs`
- [ ] `Commands/TourPricingTier/DeleteTourPricingTier/DeleteTourPricingTierCommandValidator.cs`
- [ ] `Commands/TourPricingTier/Common/AdultTierGuard.cs`

**Tour (Task 3)** (3):
- [ ] `Commands/Tour/ToggleTourFeatured/ToggleTourFeaturedCommand.cs`
- [ ] `Commands/Tour/ToggleTourFeatured/ToggleTourFeaturedCommandHandler.cs`
- [ ] `Commands/Tour/ToggleTourFeatured/ToggleTourFeaturedCommandValidator.cs`

### 8.3 Application — Queries (Task 2: 8 + Task 3: ~17 = 25 files)
**TourSchedule** (4):
- [ ] `Queries/TourSchedule/ListTourSchedules/ListTourSchedulesQuery.cs`
- [ ] `Queries/TourSchedule/ListTourSchedules/ListTourSchedulesQueryHandler.cs`
- [ ] `Queries/TourSchedule/ListTourSchedules/ListTourSchedulesQueryValidator.cs`
- [ ] `Queries/TourSchedule/Common/TourScheduleDto.cs`

**TourPricingTier** (4):
- [ ] `Queries/TourPricingTier/ListTourPricingTiers/ListTourPricingTiersQuery.cs`
- [ ] `Queries/TourPricingTier/ListTourPricingTiers/ListTourPricingTiersQueryHandler.cs`
- [ ] `Queries/TourPricingTier/ListTourPricingTiers/ListTourPricingTiersQueryValidator.cs`
- [ ] `Queries/TourPricingTier/Common/TourPricingTierDto.cs`

**Tour (Task 3)** (~17):
- [ ] `Queries/Tour/SearchTours/SearchToursQuery.cs`
- [ ] `Queries/Tour/SearchTours/SearchToursQueryHandler.cs`
- [ ] `Queries/Tour/SearchTours/SearchToursQueryValidator.cs`
- [ ] `Queries/Tour/SearchTours/SearchToursResult.cs`
- [ ] `Queries/Tour/SearchTours/SearchSort.cs`
- [ ] `Queries/Tour/SearchTours/SearchTokenizer.cs`
- [ ] `Queries/Tour/SearchTours/FacetComputer.cs`
- [ ] `Queries/Tour/SearchTours/FacetRow.cs`
- [ ] `Queries/Tour/SuggestTours/SuggestToursQuery.cs`
- [ ] `Queries/Tour/SuggestTours/SuggestToursQueryHandler.cs`
- [ ] `Queries/Tour/SuggestTours/SuggestToursQueryValidator.cs`
- [ ] `Queries/Tour/SuggestTours/TourSuggestDto.cs`
- [ ] `Queries/Tour/ListFeaturedTours/ListFeaturedToursQuery.cs`
- [ ] `Queries/Tour/ListFeaturedTours/ListFeaturedToursQueryHandler.cs`
- [ ] `Queries/Tour/ListMyTours/ListMyToursQuery.cs`
- [ ] `Queries/Tour/ListMyTours/ListMyToursQueryHandler.cs`
- [ ] `Queries/Tour/ListMyTours/ListMyToursQueryValidator.cs`
- [ ] `Queries/Tour/Common/TourSummaryDto.cs`  (only if Mahmoud hasn't shipped)

### 8.4 Application — supporting (4 files)
- [ ] `Interfaces/IScheduleBookingCountService.cs`
- [ ] `Caching/TourScheduleCacheKeys.cs`
- [ ] `Caching/TourPricingTierCacheKeys.cs`
- [ ] `Caching/TourSearchCacheKeys.cs`

### 8.5 Infrastructure (2 new + 1 edit + 1 review)
- [ ] `Services/NoOpScheduleBookingCountService.cs`
- [ ] `EventHandlers/TourFeaturedChangedDomainEventHandler.cs`
- [ ] `Persistence/Configurations/TourPricingTierConfiguration.cs` — REVIEW: confirm filtered unique index `(TourId, LOWER(Name)) WHERE IsActive = 1` is present; add migration if not.
- [ ] `DependencyInjection.cs` — EDIT: register `IScheduleBookingCountService`; register `ContentToursPermissionCatalog` if Mahmoud hasn't.

### 8.6 Contracts (3 files + maybe 2 from Mahmoud)
- [ ] `IntegrationEvents/TourPricingTierChangedIntegrationEvent.cs`
- [ ] `IntegrationEvents/TourScheduleChangedIntegrationEvent.cs`  (optional — only if Booking team requests)
- [ ] `IntegrationEvents/TourFeaturedChangedIntegrationEvent.cs`
- [ ] `Authorization/ContentToursFeatures.cs` — coordinate with Mahmoud (cross-team C4)
- [ ] `Authorization/ContentToursPermissionCatalog.cs` — coordinate with Mahmoud (cross-team C4)

### 8.7 Presentation (3 new + 1 edit)
- [ ] `TourScheduleEndpoints.cs`
- [ ] `TourPricingTierEndpoints.cs`
- [ ] `TourSearchEndpoints.cs`
- [ ] `ContentToursEndpoints.cs` — EDIT to chain `.MapTourScheduleEndpoints().MapTourPricingTierEndpoints().MapTourSearchEndpoints()`

### 8.8 Migrations (potentially 2)
- [ ] `Migrations/yyyymmddhhmmss_AddTourPricingTierUniqueNameIndex.cs` — only if EF config doesn't already enforce case-insensitive unique name per tour
- [ ] `Migrations/yyyymmddhhmmss_AddTourNamePrefixIndex.cs` — only if Suggest p95 > 100ms in load test

---

## 9. Risk Register

| # | Risk | Probability | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Mahmoud (Task 1) misses 2026-05-08 deadline | Medium | High — Task 2/3 cannot ship without enum/audit fields/permission catalog | Mohammad volunteers to ship `ContentToursFeatures` + `ContentToursPermissionCatalog` himself with full Tour permissions on 2026-05-06. Pair on `TourStatus` enum migration if needed. |
| R2 | EF cannot translate `Math.Log10` in score formula | Medium | Medium — relevance ranking degraded | Compute partial score in SQL (token matches + rating + recency), then a top-N (`pageSize × 5`) materialised set, and finalise score (incl. Log10 of BookingCount) in memory. |
| R3 | Recurrence expansion 120-row hard limit causes false positives in legitimate use cases | Low | Medium | Document the cap in the OpenAPI summary + 422 response message. Provide hint: "Narrow validTo to reduce row count". Consider raising to 365 if business team pushes back. |
| R4 | Search facets degrade with 5001+ Approved tours (cap silently drops rows) | Low (today) → High (1y) | Medium | Document cap in code comments; add a metric (`tour_search.facet_rows_capped_total`) to alert when hit. Plan v2 to use SQL aggregation directly. |
| R5 | Adult-tier name match is locale-sensitive (e.g., Turkish 'I' / 'i') | Low | Low | Use `StringComparison.OrdinalIgnoreCase` everywhere — confirmed in entity helper and in handlers. |
| R6 | TourSchedule `TimeOnly` not configured in EF (no `HasConversion`) → silent storage as ticks | Medium | High | First action in WBS 2.1: open `TourScheduleConfiguration.cs` and verify `Property(x => x.StartTime).HasConversion(...)` exists. If missing, add it + create migration. |
| R7 | `OutboxMessage.Create` API differs from brief (e.g., needs explicit type-name string) | Low | Low | Verify on day 1 in Infrastructure project; adjust direct outbox writes if needed. |
| R8 | Task 1 `Tour.IsFeatured` setter doesn't exist as `private set` (might be inherited differently) | Low | Low | Already verified — `public bool IsFeatured { get; private set; }` exists in `Tour.cs`. Mohammad's `SetFeatured` will compile. |
| R9 | `MustHavePermissionAttribute` ctor expects different params | Low | Low | Pattern verified in `authorization-refactor-plan.md` — ctor `(string feature, AppAction action)`. Aligned. |
| R10 | `AppAction.Feature` and `AppAction.ReadOwn` not in enum | Medium | Medium | Verify on day 1 in `YallaJo.SharedKernel.Application/Authorization/AppAction.cs`. If missing, coordinate with Mahmoud — this is part of C4. |
| R11 | Per-user MyTours cache busts not wired by Task 1 handlers | Medium | Low (stale-only) | Add explicit `cache.RemoveByTagAsync($"my-tours:{userId}")` in Mohammad's three command handlers (CreateTourPricingTier / CreateTourSchedule / ToggleTourFeatured) where applicable. Coordinate with Mahmoud to add the same in his Tour CRUD handlers. |

---

## 10. Self-review checklist (run before each PR)

For each new endpoint:
- [ ] `.WithMetadata(new MustHavePermissionAttribute(...))` OR `.AllowAnonymous()` is present.
- [ ] `.Produces<T>(...)` and `.ProducesProblem(...)` cover all expected status codes.
- [ ] Handler returns `Result.NotFound / Forbidden / Conflict / Failure(..., Outcome.UnprocessableEntity)` — never throws for business errors.
- [ ] `CancellationToken` flows from endpoint → handler → repo / DbContext / cache.
- [ ] Owner check (`tour.CreatedByUserId == currentUser.UserId`) runs after permission check.
- [ ] Soft-deleted Tour → `Tour.NotFound 404` (not 410).
- [ ] Cache invalidation runs **after** `SaveChangesAsync` succeeds.
- [ ] Direct outbox write in non-aggregate handlers happens **before** `SaveChangesAsync`.
- [ ] Logger calls use structured arguments (not `$"..."` interpolation), never log secrets.
- [ ] FluentValidation validator covers EVERY field's brief-defined rule.
- [ ] No `dotnet build` warnings from new files.
- [ ] Swagger smoke: hit each endpoint with happy + error paths.

For each new query:
- [ ] Implements `ICacheableQuery` with `CacheKey`, `CacheDuration`, `Tags`.
- [ ] Cache key matches the brief's table verbatim.
- [ ] Tags include both fine-grained (`tour-pricing:{tourId}`) and coarse (`tour:{tourId}`) where the brief specifies.
- [ ] Uses `AsNoTracking()` (default in `EfRepository`'s read paths, but verify on direct `dbContext` queries).

For each new domain method:
- [ ] Idempotent when value unchanged (e.g., `SetFeatured` no-op).
- [ ] Validates inputs with `ArgumentException` / `ArgumentOutOfRangeException` (not `Result.Failure` — domain layer doesn't depend on Result).
- [ ] Raises domain event only on real state change (aggregates only).
- [ ] Calls `MarkUpdated()` on aggregates (Tour) — non-aggregates skip this.

---

## 11. Open questions for Tech Lead / PO

1. **Search AND-vs-OR semantics** (§6.3): brief says "any token NOT matched in any field → exclude" (strict AND), but the score formula `> 0` allows partial match. Confirm: AND or OR?
2. **`Outcome.UnprocessableEntity = 422`**: confirm enum value exists in `YallaJo.SharedKernel.Domain.Abstractions.Results`. If only `Conflict (409)` and `Invalid (400)` exist, where do brief's 422 codes (`OverlapDetected`, `ExpansionTooLarge`, `CustomDateOutOfRange`) map?
3. **`AppAction.Feature` / `AppAction.ReadOwn` / `AppAction.ReadAny`**: confirm presence in enum. If missing, who owns adding them — SharedKernel maintainer?
4. **`TourScheduleChangedIntegrationEvent`**: ship it or defer? Brief says "only if Booking team requests" — Booking team is Task 6 owner; ask whether they want it now or in their sprint.
5. **`MarkUpdated`-equivalent on `BaseEntity`**: confirm `BaseEntity` does NOT have an `UpdatedAt` field. If it does, Mohammad updates the brief's "no `RowVersion`, no soft delete" interpretation.
6. **Translation join in Search ranking**: brief says "tokens matched in Name OR Translation.Name". For perf, should the handler load translations as `.Include` (single query, more rows) or `.Select(t => new { t, translatedName = t.Translations.FirstOrDefault(...).Name })`? Recommend the latter.
7. **`Featured` cache key includes `Accept-Language`** in brief — but `ListFeaturedTours` returns `TourSummaryDto` (just Name/Slug/etc., locale-agnostic). Either drop the language dimension or load translations into the DTO.

---

## 12. References

- **Brief**: `Agents/ContentTours-team-tasks.md` Task 2 + Task 3 sections
- **Architecture rules**: `Agents/agent-context.md` §2 (Authorization, Result Pattern, MediatR, Cache, CurrentUser, Outbox)
- **Implementation recipe**: `Agents/guide.md` (full step-by-step)
- **Templates**: `Agents/templates/{CreateCommand,UpdateCommand,DeleteCommand,ListQuery,GetByIdQuery,EfConfiguration,Endpoints,DependencyInjection,PermissionCatalog,DomainEvent}.cs.template`
- **Patterns**: `Agents/patterns/{caching,error-handling,polly}-patterns.md`
- **Business rules PDF**: `Agents/YallaJo Business Rules & Edge Cases.pdf` (consult §"Tours / Schedules / Pricing / Search" for cross-cutting edge cases not in the brief — read before hitting any ambiguous spec gap)
- **Reference implementation**: `ContentPlaces.Contracts/Authorization/ContentPlacesPermissionCatalog.cs` (mimic structure for `ContentTours`)
- **Reference handler**: `ContentTours.Infrastructure/EventHandlers/TourPlaceCountChangedDomainEventHandler.cs` (mimic structure for `TourFeaturedChangedDomainEventHandler`)

---

**End of plan. Mohammad: read §1, §2, and §11 first; raise blockers in standup before writing code.**
