# YallaJo — Pre-Work (PW) Templates

This document provides copy-paste code templates for executing the per-module Pre-Work (PW) items. Use these as scaffolding when implementing the 5 new modules.

## Table of Contents
- [PW-1 — Module Unit of Work](#pw-1--module-unit-of-work)
- [PW-2 — IAggregateRoot Entity Upgrade](#pw-2--iaggregateroot-entity-upgrade)
- [PW-3 — Domain Event Records](#pw-3--domain-event-records)
- [PW-4 — Integration Events](#pw-4--integration-events)
- [PW-5 — Repository Interfaces](#pw-5--repository-interfaces)
- [PW-7 — Permission Catalog](#pw-7--permission-catalog)
- [PW-8 — Test Project Scaffolding](#pw-8--test-project-scaffolding)
- [Solution File Registration](#solution-file-registration)

---

## PW-1 — Module Unit of Work

### Template A: `IXxxUnitOfWork` interface
**Location**: `{Module}.Application/Interfaces/IXxxUnitOfWork.cs`

```csharp
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace {Module}.Application.Interfaces;

public interface I{Module}UnitOfWork : IUnitOfWork
{
}
```

### Template B: `XxxUnitOfWork` implementation
**Location**: `{Module}.Infrastructure/Persistence/{Module}UnitOfWork.cs`

```csharp
using {Module}.Application.Interfaces;
using YallaJo.SharedKernel.Infrastructure.Data;

namespace {Module}.Infrastructure.Persistence;

internal sealed class {Module}UnitOfWork(IUnitOfWork<{Module}DbContext> uow) : I{Module}UnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => uow.SaveChangesAsync(ct);
}
```
> **CRITICAL**: Always delegate to `IUnitOfWork<TContext>`. Do NOT call `context.SaveChangesAsync` directly, as the typed UnitOfWork handles domain event dispatching.

### Template C: DI registration
**Location**: `{Module}.Infrastructure/DependencyInjection.cs`

```csharp
// Inside Add{Module}Infrastructure method:
services.AddScoped<IUnitOfWork<{Module}DbContext>, UnitOfWork<{Module}DbContext>>();
services.AddScoped<I{Module}UnitOfWork, {Module}UnitOfWork>();
```

---

## PW-2 — IAggregateRoot Entity Upgrade

**Template**: Marker interface addition
**Location**: `{Module}.Domain/Entities/{Entity}.cs`

```csharp
using YallaJo.SharedKernel.Domain.Entities;

// Before
public sealed class Review : AuditableEntity

// After  
public sealed class Review : AuditableEntity, IAggregateRoot
```
> **Note**: `IAggregateRoot` is a marker interface. No database migration is required.

---

## PW-3 — Domain Event Records

**Template**: Domain event record
**Location**: `{Module}.Domain/Events/{EventName}.cs`

```csharp
using YallaJo.SharedKernel.Domain.Event;

namespace {Module}.Domain.Events;

public sealed record {Aggregate}{Verb}DomainEvent(Guid {Aggregate}Id, $$$) : DomainEventBase;
```
- Inherit from `DomainEventBase`.
- Use `sealed record` for immutability.

### Naming Conventions
| Trigger | Event name pattern | Example |
|---|---|---|
| Entity created | `{Aggregate}CreatedDomainEvent` | `TourCreatedDomainEvent` |
| State changed | `{Aggregate}{NewState}DomainEvent` | `TourApprovedDomainEvent` |
| Entity deleted | `{Aggregate}DeletedDomainEvent` | `TourDeletedDomainEvent` |
| Child added | `{Child}AddedTo{Parent}DomainEvent` | `WaypointAddedToTourDomainEvent` |

---

## PW-4 — Integration Events

### Template A: Integration event record
**Location**: `{Module}.Contracts/IntegrationEvents/{EventName}.cs`

```csharp
using YallaJo.SharedKernel.Domain.Event;

namespace {Module}.Contracts.IntegrationEvents;

public sealed record {EventName}IntegrationEvent(
    Guid {Entity}Id,
    $$$
) : IntegrationEventBase;
```

### Template B: Registry entry
**Location**: `YallaJo.SharedKernel.Infrastructure/Abstractions/Integration/IntegrationEventTypeRegistry.cs`

```csharp
// Add to the NameToType dictionary:
// ── {Module} ({N} events) ──
["{module-key}.{entity}.{verb}.v1"] = typeof({EventName}IntegrationEvent),
```
- **Key naming rule**: `{module-key}.{entity}.{verb}.v1` (all lowercase, hyphens for multi-word).

---

## PW-5 — Repository Interfaces

### Template A: Repository interface
**Location**: `{Module}.Application/Abstractions/I{Aggregate}Repository.cs`

```csharp
namespace {Module}.Application.Abstractions;

public interface I{Aggregate}Repository
{
    Task<{Aggregate}?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add({Aggregate} entity);
    void Remove({Aggregate} entity);
}
```

### Template B: EF Core implementation
**Location**: `{Module}.Infrastructure/Repositories/{Aggregate}Repository.cs`

```csharp
using {Module}.Application.Abstractions;
using {Module}.Domain.Entities;
using {Module}.Infrastructure.Persistence;

namespace {Module}.Infrastructure.Repositories;

internal sealed class {Aggregate}Repository({Module}DbContext context) : I{Aggregate}Repository
{
    public Task<{Aggregate}?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => context.{Aggregates}.FindAsync([id], ct).AsTask();
    
    public void Add({Aggregate} entity) => context.{Aggregates}.Add(entity);
    public void Remove({Aggregate} entity) => context.{Aggregates}.Remove(entity);
}
```

### Template C: DI registration
**Location**: `{Module}.Infrastructure/DependencyInjection.cs`

```csharp
services.AddScoped<I{Aggregate}Repository, {Aggregate}Repository>();
```

---

## PW-7 — Permission Catalog

### Template A: Feature constants
**Location**: `{Module}.Contracts/Authorization/{Module}Features.cs`

```csharp
namespace {Module}.Contracts.Authorization;

public static class {Module}Features
{
    public const string {FeatureName} = "{module-key}.{feature-slug}";
}
```

### Template B: Permission catalog
**Location**: `{Module}.Contracts/Authorization/{Module}PermissionCatalog.cs`

```csharp
using YallaJo.SharedKernel.Application.Authorization;

namespace {Module}.Contracts.Authorization;

public sealed class {Module}PermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "{Module}";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new({Module}Features.{Feature}, AppAction.Create, PermissionGroup.{Group}, "Description"),
        new({Module}Features.{Feature}, AppAction.Read,   PermissionGroup.{Group}, "Description"),
        // ...
    ];
}
```

### Template C: DI registration
**Location**: `{Module}.Infrastructure/DependencyInjection.cs`

```csharp
services.AddSingleton<IPermissionCatalog, {Module}PermissionCatalog>();
```

---

## PW-8 — Test Project Scaffolding

### Template A: `.csproj` for unit tests
**Location**: `tests/{Module}.Tests.Unit/{Module}.Tests.Unit.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\{Module}.Domain\{Module}.Domain.csproj" />
    <ProjectReference Include="..\..\{Module}.Application\{Module}.Application.csproj" />
    <ProjectReference Include="..\..\tests\YallaJo.Tests.Shared\YallaJo.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

### Template B: InternalsVisibleTo
**Location**: `{Module}.Application.csproj` (and Infrastructure if needed)

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
    <_Parameter1>{Module}.Tests.Unit</_Parameter1>
  </AssemblyAttribute>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
    <_Parameter1>{Module}.IntegrationTests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

---

## Solution File Registration

Run these commands from the repository root to register new test projects:

```powershell
dotnet sln YallaJo.sln add tests/{Module}.Tests.Unit/{Module}.Tests.Unit.csproj
dotnet sln YallaJo.sln add tests/{Module}.IntegrationTests/{Module}.IntegrationTests.csproj
```
