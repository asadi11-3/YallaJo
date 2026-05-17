# ADR-006 — Per-Module IUnitOfWork Delegate Pattern

- **Status:** Accepted
- **Date:** 2026-06-01
- **Context:** Pre-Work execution for Wave 5/6 modules (Booking, Finance, Social, Messaging, Analytics)

## Context

`YallaJo.SharedKernel.Infrastructure/Data/UnitOfWork<TContext>` is the canonical implementation: it dispatches domain events on aggregate roots **before** `SaveChangesAsync`, then clears the events. Modules previously inconsistent:

- Some modules had no module-specific UoW interface — handlers injected `IUnitOfWork<XxxDbContext>` directly, exposing the generic to Application code.
- `MessagingUnitOfWork` was buggy: it called `context.SaveChangesAsync(ct)` directly, **bypassing the domain-event dispatcher entirely**. Any event raised on a Messaging aggregate would never have been published.

## Decision

Every module exposes its own `I{Module}UnitOfWork` interface in `{Module}.Application/Interfaces/` and a sealed delegating implementation in `{Module}.Infrastructure/Persistence/`:

```csharp
// {Module}.Application/Interfaces/I{Module}UnitOfWork.cs
public interface I{Module}UnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// {Module}.Infrastructure/Persistence/{Module}UnitOfWork.cs
internal sealed class {Module}UnitOfWork(IUnitOfWork<{Module}DbContext> inner) : I{Module}UnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => inner.SaveChangesAsync(ct);
}
```

DI: `services.AddScoped<IUnitOfWork<{Module}DbContext>, UnitOfWork<{Module}DbContext>>();` **then** `services.AddScoped<I{Module}UnitOfWork, {Module}UnitOfWork>();`. The delegate is registered as a separate Scoped binding — both interfaces resolve to the same underlying generic UoW in a given request scope (chain of `IUnitOfWork<TContext>` → `UnitOfWork<TContext>` instance → wrapped by module delegate).

## Consequences

✅ Application handlers depend only on the narrow, module-typed interface — no leakage of `DbContext` generics.
✅ Future cross-cutting interceptors (audit, metrics, retry) can be inserted in the delegate without touching the generic.
✅ The Messaging bug class is structurally eliminated: every module's delegate physically cannot bypass dispatch because it can only call the generic UoW.

## Verification

`tests/SharedKernel.Tests.Unit/Data/UnitOfWorkTests.cs` covers the generic dispatch chain. Each module's test project will add one delegate test ("module UoW calls generic UoW exactly once") during sprint kickoff.
