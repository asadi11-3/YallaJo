# TASK 1 — Fix §8.1 `ICurrentUser` Violations in ContentPlaces Handlers

> **Owner:** Mahmoud (Intermediate) — **Hours:** 16h — **Hard deadline:** Sun **2027-03-07 17:00**
> **Earliest start:** Wed 2027-03-03 09:00 (after PW-1..PW-3 merged)
> **Endpoints:** 0 (handlers only). **Files touched:** 8 handlers + ~16 test files + ~8 endpoint files (forwarding `currentUser.UserId` via command property).
> **Depends on:** PW-1 (fresh violation CSV), PW-2 (permission coverage verified), PW-3 (regression tests in place — RED initially).

This task removes `ICurrentUser` injection from 8 ContentPlaces command handlers and migrates each to either: (a) pure handlers where authorization is purely the endpoint's job, or (b) handlers receiving the actor's UserId as a command property forwarded by the endpoint.

---

## 1. The 8 Handlers

All under `ContentPlaces.Application/Businesses/Commands/`:

| # | Handler | Reason it has `ICurrentUser` today | Fix pattern |
|---|---|---|---|
| 1 | `ApproveBusinessCommandHandler` | Checks `IsAuthenticated` + records `ApprovedByUserId` | Remove injection. Endpoint forwards `currentUser.UserId` via `ApprovedByUserId` command property. |
| 2 | `RejectBusinessCommandHandler` | Same pattern | Same: command property `RejectedByUserId` |
| 3 | `SuspendBusinessCommandHandler` | Same pattern | Command property `SuspendedByUserId` |
| 4 | `ReinstateBusinessCommandHandler` | Same pattern | Command property `ReinstatedByUserId` |
| 5 | `AddBusinessStaffCommandHandler` | Provider-self check + creator stamp | Remove injection. Endpoint forwards `currentUser.UserId` as `ActingUserId`. Handler does IDOR check `if (business.OwnerUserId != command.ActingUserId) Forbidden`. |
| 6 | `AddBusinessAmenityCommandHandler` | Same pattern | Same: `ActingUserId` IDOR check |
| 7 | `RemoveBusinessAmenityCommandHandler` | Same pattern | Same |
| 8 | `SetBusinessHoursCommandHandler` | Same pattern | Same |

---

## 2. Worked Example — `ApproveBusinessCommandHandler`

### Before

```csharp
// ContentPlaces.Application/Businesses/Commands/ApproveBusinessCommandHandler.cs
public sealed class ApproveBusinessCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    ICurrentUser currentUser,                 // ← VIOLATION
    HybridCache cache,
    ILogger<ApproveBusinessCommandHandler> logger)
    : IRequestHandler<ApproveBusinessCommand, Result>
{
    public async Task<Result> Handle(ApproveBusinessCommand command, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)     // ← VIOLATION
            return Result.Failure(new Error("Auth.Unauthenticated", "..."), Outcome.Forbidden);

        var business = await businessRepo.GetByIdAsync(command.BusinessId, ct);
        if (business is null) return Result.Failure(new Error("Business.NotFound", "..."), Outcome.NotFound);

        var result = business.Approve(currentUser.UserId!.Value);  // ← creator-stamp via ambient
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{command.BusinessId}", ct);
        return Result.Success();
    }
}
```

### Command (Application/Businesses/Commands/ApproveBusinessCommand.cs)

```csharp
// Before
public sealed record ApproveBusinessCommand(Guid BusinessId) : IRequest<Result>;

// After (adds ApprovedByUserId set from endpoint)
public sealed record ApproveBusinessCommand(
    Guid BusinessId,
    Guid ApprovedByUserId) : IRequest<Result>;
```

### Validator (if present)

```csharp
public sealed class ApproveBusinessCommandValidator : AbstractValidator<ApproveBusinessCommand>
{
    public ApproveBusinessCommandValidator()
    {
        RuleFor(x => x.BusinessId).NotEmpty();
        RuleFor(x => x.ApprovedByUserId).NotEmpty();  // ← new
    }
}
```

### After (handler — no `ICurrentUser`)

```csharp
public sealed class ApproveBusinessCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    HybridCache cache,
    ILogger<ApproveBusinessCommandHandler> logger)
    : IRequestHandler<ApproveBusinessCommand, Result>
{
    public async Task<Result> Handle(ApproveBusinessCommand command, CancellationToken ct)
    {
        // No auth check here — endpoint already enforced MustHavePermission(Business, Approve).
        var business = await businessRepo.GetByIdAsync(command.BusinessId, ct);
        if (business is null)
            return Result.Failure(new Error("Business.NotFound", $"Business {command.BusinessId} not found"), Outcome.NotFound);

        var result = business.Approve(command.ApprovedByUserId);  // ← from command, not ambient
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{command.BusinessId}", ct);
        logger.LogInformation("Business {BusinessId} approved by {UserId}", command.BusinessId, command.ApprovedByUserId);
        return Result.Success();
    }
}
```

### Endpoint (ContentPlaces.Presentation/Endpoints/BusinessAdminEndpoints.cs)

```csharp
// Before
group.MapPost("/admin/businesses/{id:guid}/approve", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var command = new ApproveBusinessCommand(id);
    var result = await mediator.Send(command, ct);
    return result.ToApiResult();
})
.RequireAuthorization();  // ← ALSO violates §8.2 — fix in TASK 3? No — Mohammad fixes Auth/Accounts/Security/ContentCore/ContentPlaces in TASK 3.
                          //    But since we're already in this endpoint, ALSO add MustHavePermission here as part of this PR to keep one cohesive change.

// After
group.MapPost("/admin/businesses/{id:guid}/approve",
    async (Guid id, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
    {
        var actingUserId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user missing UserId");
        var command = new ApproveBusinessCommand(id, ApprovedByUserId: actingUserId);
        var result = await mediator.Send(command, ct);
        return result.ToApiResult();
    })
    .WithMetadata(new MustHavePermissionAttribute(
        ContentPlacesFeatures.Business,
        AppAction.Approve))   // ← explicit policy attribute
    .WithName("ApproveBusiness");
```

The `?? throw` is intentional — if `MustHavePermission` did its job, `UserId` will be non-null. If somehow null, fail loud rather than silently insert `Guid.Empty`.

---

## 3. Pattern for Provider-Owned Handlers (#5..#8)

For handlers that need **IDOR** (caller must own the business), the pattern is:

### Command (adds `ActingUserId`)

```csharp
public sealed record AddBusinessAmenityCommand(
    Guid BusinessId,
    string AmenityType,
    string? Description,
    Guid ActingUserId) : IRequest<Result>;
```

### Handler

```csharp
public sealed class AddBusinessAmenityCommandHandler(
    IBusinessRepository businessRepo,
    IContentPlacesUnitOfWork uow,
    HybridCache cache,
    ILogger<AddBusinessAmenityCommandHandler> logger)
    : IRequestHandler<AddBusinessAmenityCommand, Result>
{
    public async Task<Result> Handle(AddBusinessAmenityCommand command, CancellationToken ct)
    {
        var business = await businessRepo.GetByIdWithOwnerAsync(command.BusinessId, ct);
        if (business is null)
            return Result.Failure(new Error("Business.NotFound", "..."), Outcome.NotFound);

        // IDOR check — caller must own the business (or admin with override permission)
        if (business.OwnerUserId != command.ActingUserId)
            return Result.Failure(new Error("Business.OwnerMismatch", $"User {command.ActingUserId} is not the owner of business {command.BusinessId}"), Outcome.Forbidden);

        var result = business.AddAmenity(command.AmenityType, command.Description);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync($"business:{command.BusinessId}:amenities", ct);
        return Result.Success();
    }
}
```

### Endpoint

```csharp
group.MapPost("/businesses/{id:guid}/amenities",
    async (Guid id, AddBusinessAmenityRequest request, ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
    {
        var actingUserId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated user missing UserId");
        var command = new AddBusinessAmenityCommand(
            BusinessId: id,
            AmenityType: request.AmenityType,
            Description: request.Description,
            ActingUserId: actingUserId);
        var result = await mediator.Send(command, ct);
        return result.ToApiResult();
    })
    .WithMetadata(new MustHavePermissionAttribute(
        ContentPlacesFeatures.Business,
        AppAction.Update));
```

**Admin override:** if admins should also be able to add amenities to any business (likely yes per PDF 2), the handler IDOR check needs to allow that. Two options:

1. **Add a Boolean `IsAdmin` to the command** (set from endpoint via `currentUser.IsInRole("Admin")`) and short-circuit IDOR if true.
2. **Use two separate endpoints**: `/admin/businesses/{id}/amenities` with `MustHavePermission(AdminBusinessManagement, AppAction.Update)` skipping IDOR + `/businesses/{id}/amenities` for owners with IDOR. Cleaner separation.

**Decision:** prefer option 2 unless it's already a single endpoint shared in current code (then option 1). PR description documents decision.

---

## 4. WBS

| # | Step | Hours | Finish-by |
|---|---|---|---|
| 1 | Update `ApproveBusinessCommand` + handler + endpoint + test fixture | 2 | 2027-03-03 |
| 2 | Update `RejectBusinessCommandHandler` (parallel pattern) | 1.5 | 2027-03-04 |
| 3 | Update `SuspendBusinessCommandHandler` + `ReinstateBusinessCommandHandler` | 2 | 2027-03-04 |
| 4 | Update `AddBusinessStaffCommandHandler` (IDOR pattern + tests) | 2 | 2027-03-05 |
| 5 | Update `AddBusinessAmenityCommandHandler` + `RemoveBusinessAmenityCommandHandler` | 2 | 2027-03-05 |
| 6 | Update `SetBusinessHoursCommandHandler` | 1.5 | 2027-03-06 |
| 7 | Per-handler unit tests (16 — one per handler covering happy + IDOR-fail) | 3 | 2027-03-06 |
| 8 | Per-endpoint integration tests (8 — `MustHavePermission` attribute verification + 401/403/200 wiring) | 1.5 | 2027-03-07 |
| 9 | PR + review fixes | 0.5 | 2027-03-07 |
| **Total** | | **16h** | **Sun 2027-03-07** |

---

## 5. Acceptance

1. **Zero `ICurrentUser` injections** in 8 listed handlers — `rg "ICurrentUser" ContentPlaces.Application/Businesses/Commands/` returns 0 lines.
2. **8 endpoints have `MustHavePermissionAttribute`** — PW-3 metadata test no longer flags them.
3. **16+ unit tests pass** — happy path + IDOR-fail per handler.
4. **8 integration tests pass** — endpoint-level auth metadata verification.
5. **No regression** — full `dotnet test ContentPlaces.Tests.Unit` green.
6. **PR description** lists each violation row from PW-1 CSV with line-number link and confirms it's resolved.
