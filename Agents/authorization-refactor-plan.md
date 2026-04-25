# Authorization Subsystem Refactor — Architecture & Implementation Plan

> **Status**: Proposed
> **Scope**: Relocate `MustHavePermissionAttribute` + related primitives to the correct architectural layers, introduce per-module permission ownership via `IPermissionCatalog`, and aggregate permissions via DI discovery.
> **Risk**: Low overall. Each PR is independently revertible. Zero runtime behavior change (the emitted policy name `Permission.{Feature}.{Action}` and the `Permission` claim type are preserved identically).

---

## Table of Contents

1. [Problem Statement](#1-problem-statement)
2. [Current State Audit](#2-current-state-audit)
3. [Target Architecture](#3-target-architecture)
4. [Design Principles](#4-design-principles)
5. [Dependency Graph](#5-dependency-graph)
6. [Project-Level Layout (After)](#6-project-level-layout-after)
7. [Core Abstractions (Complete Source)](#7-core-abstractions-complete-source)
8. [The Aggregation Pattern](#8-the-aggregation-pattern)
9. [Runtime Flow](#9-runtime-flow)
10. [Implementation Plan — 4 PRs](#10-implementation-plan--4-prs)
11. [Pre-Work: Baseline Verification](#11-pre-work-baseline-verification)
12. [PR 1 — Relocate Attribute & Runtime](#12-pr-1--relocate-attribute--runtime)
13. [PR 2 — Introduce `IPermissionCatalog` Abstractions](#13-pr-2--introduce-ipermissioncatalog-abstractions)
14. [PR 3 — Per-Module Catalogs & Seeder Migration](#14-pr-3--per-module-catalogs--seeder-migration)
15. [PR 4 — Cleanup & Documentation](#15-pr-4--cleanup--documentation)
16. [Post-Migration Verification Checklist](#16-post-migration-verification-checklist)
17. [Rollback Strategy](#17-rollback-strategy)
18. [Timeline](#18-timeline)
19. [Success Criteria](#19-success-criteria)

---

## 1. Problem Statement

`MustHavePermissionAttribute` and its supporting constants (`AppAction`, `AppFeatures`, `AppRoleGroup`) currently live in `Security.Contracts/Authorization/`. This is architecturally wrong because:

- `Security.Contracts` is documented to hold **only** integration event DTOs. The attribute is not a DTO — it is an ASP.NET Core authorization primitive.
- The attribute forces `Security.Contracts` to reference `Microsoft.AspNetCore.Authorization`, polluting a project that should be framework-agnostic.
- Every Presentation project (`Auth`, `Security`, `ContentCore`, `ContentPlaces`) must `ProjectReference Security.Contracts` just to get the attribute — they transitively pull in Security's integration events as a side effect.
- `AppFeatures` is a god-class listing every module's feature names (Role, Category, Place, BusinessStaff, etc.), creating a massive fan-in to a Security-owned file whenever any module adds a permission.
- `AppPermissions.cs` (the seeder) hard-codes all permissions from all modules in one Security file, so adding a module means editing Security — violating modular monolith independence.

**Goal**: permission authoring becomes a local, per-module concern. Security owns only enforcement + role hierarchy. SharedKernel owns cross-cutting primitives. Adding a new module never requires editing another module.

---

## 2. Current State Audit

### 2.1 Files in `Security.Contracts/Authorization/`

| File | Belongs Where | Reason |
|---|---|---|
| `MustHavePermissionAttribute.cs` | ❌ Wrong | Should be in `SharedKernel.Presentation` |
| `AppAction.cs` | ❌ Wrong | Generic CRUD verbs used by every module — should be in `SharedKernel.Application` |
| `AppFeatures.cs` | ❌ Wrong | God-class — should split per-module into `{Module}.Contracts/Authorization/{Module}Features.cs` |
| `AppRoleGroup.cs` | ❌ Wrong | Cross-cutting group constants — should be in `SharedKernel.Application` as `PermissionGroup` |
| `AppRoles.cs` | ✅ Stays | Role names + privilege math are genuine Security domain concepts |
| `RolePrivilegeLevel` enum | ✅ Stays | Security domain concept |

### 2.2 Files in `YallaJo.Api/Authorization/`

| File | Belongs Where | Reason |
|---|---|---|
| `PermissionRequirement.cs` | ❌ Wrong | Should be in `SharedKernel.Presentation` so any host can wire it |
| `PermissionAuthorizationHandler.cs` | ❌ Wrong | Same — cross-cutting runtime machinery |
| `PermissionPolicyProvider.cs` | ❌ Wrong | Same |

### 2.3 Files in `Security.Infrastructure/Seeding/`

| File | Belongs Where | Reason |
|---|---|---|
| `AppPermissions.cs` (god-list + role switch) | ❌ Replace | Replace with `PermissionSeeder` + `RolePermissionMapping` (aggregation pattern) |
| `SecurityDataSeeder.cs` | ✅ Refactor | Delegates to `PermissionSeeder` + `RolePermissionMapping` |

### 2.4 Consumer files (17 Presentation endpoints)

All 17 endpoint files use `using Security.Contracts.Authorization;` for the attribute. Must be updated to use the new SharedKernel namespaces.

```
Auth.Presentation/Endpoints/Session/SessionEndpoints.cs
Auth.Presentation/Endpoints/Invitation/InvitationEndpoints.cs
Security.Presentation/Endpoints/User/UserEndpoints.cs
Security.Presentation/Endpoints/Role/RoleEndpoints.cs
Security.Presentation/Endpoints/AuditLog/AuditLogEndpoints.cs
ContentCore.Presentation/Endpoints/Translation/TranslationEndpoints.cs
ContentCore.Presentation/Endpoints/Tag/TagEndpoints.cs
ContentCore.Presentation/Endpoints/Specialization/SpecializationEndpoints.cs
ContentCore.Presentation/Endpoints/Language/LanguageEndpoints.cs
ContentCore.Presentation/Endpoints/EntityTag/EntityTagEndpoints.cs
ContentCore.Presentation/Endpoints/EntityCategory/EntityCategoryEndpoints.cs
ContentCore.Presentation/Endpoints/Attachment/AttachmentEndpoints.cs
ContentPlaces.Presentation/Endpoints/ServiceItem/ServiceItemEndpoints.cs
ContentPlaces.Presentation/Endpoints/Place/PlaceEndpoints.cs
ContentPlaces.Presentation/Endpoints/BusinessStaff/BusinessStaffEndpoints.cs
ContentPlaces.Presentation/Endpoints/BusinessAmenity/BusinessAmenityEndpoints.cs
ContentPlaces.Presentation/Endpoints/AccessibilityFeature/AccessibilityFeatureEndpoints.cs
```

---

## 3. Target Architecture

**One-line summary**: SharedKernel owns cross-cutting auth primitives. Each module owns its own feature catalog via `IPermissionCatalog`. Security discovers catalogs at seed time and enforces the role-to-permission mapping. No module ever edits another module's permission list.

---

## 4. Design Principles

| # | Principle | Why |
|---|---|---|
| P1 | **Contracts are framework-agnostic DTOs only** | Any consumer (web, worker, CLI, test) can reference them cleanly |
| P2 | **Cross-cutting primitives live in SharedKernel** | The attribute used by all Presentation projects is cross-cutting |
| P3 | **Each module owns its feature & permission catalog** | Adding a new module means editing only that module |
| P4 | **Security owns enforcement + role hierarchy** | Single source of truth for who outranks whom |
| P5 | **Permission aggregation uses a discovery pattern** | Security doesn't hardcode other modules' features |
| P6 | **No circular dependencies** | SharedKernel → Modules = forbidden. Modules → SharedKernel = required |
| P7 | **Policy names are deterministic strings** | Attributes + runtime handlers stay decoupled via the `Permission.{Feature}.{Action}` contract |

---

## 5. Dependency Graph

```
                          ┌─────────────────────────┐
                          │  YallaJo.SharedKernel   │
                          │        .Domain          │
                          └─────────────────────────┘
                                      ▲
                    ┌─────────────────┼─────────────────┐
                    │                 │                 │
     ┌──────────────────────┐ ┌──────────────────┐ ┌────────────────────────┐
     │  SharedKernel        │ │  SharedKernel    │ │  SharedKernel          │
     │  .Application        │ │  .Infrastructure │ │  .Presentation         │
     │  (CQRS, MediatR)     │ │  (Outbox, UoW)   │ │  (Auth primitives,     │
     │  + AppAction         │ │                  │ │   ResultExtensions)    │
     │  + IPermissionCatalog│ │                  │ │  + MustHavePermission  │
     │  + PermissionGroup   │ │                  │ │  + Handler + Provider  │
     │  + PermissionDescr.  │ │                  │ │  + PolicyNames         │
     └──────────────────────┘ └──────────────────┘ └────────────────────────┘
              ▲                       ▲                        ▲
              │                       │                        │
   ┌──────────┴────────────┐          │              ┌─────────┴──────────┐
   │ {Module}.Application  │          │              │ {Module}.         │
   │ {Module}.Contracts ◄──┼──────────┘              │ Presentation      │
   │ (IPermissionCatalog   │                         │ (Endpoints use    │
   │  impl + Features)     │                         │  attribute +      │
   └───────────────────────┘                         │  feature consts)  │
              ▲                                      └───────────────────┘
              │
   ┌──────────┴────────────┐
   │ {Module}.Domain       │
   │ {Module}.Infrastructure│
   └───────────────────────┘

   Security module = just another module. No privileged position.
   Security.Infrastructure discovers IPermissionCatalog via DI, seeds.
```

**Key insight**: Security becomes a *consumer* of what other modules *publish* via a shared interface — not the owner of everyone's feature list.

---

## 6. Project-Level Layout (After)

### 6.1 `YallaJo.SharedKernel.Presentation/Authorization/`

```
YallaJo.SharedKernel.Presentation/
└── Authorization/
    ├── MustHavePermissionAttribute.cs              ← moved from Security.Contracts
    ├── PermissionPolicyNames.cs                    ← NEW: single source for policy string format
    ├── PermissionRequirement.cs                    ← moved from YallaJo.Api
    ├── PermissionAuthorizationHandler.cs           ← moved from YallaJo.Api
    ├── PermissionPolicyProvider.cs                 ← moved from YallaJo.Api
    └── AuthorizationServiceCollectionExtensions.cs ← NEW: AddPermissionAuthorization()
```

### 6.2 `YallaJo.SharedKernel.Application/Authorization/`

```
YallaJo.SharedKernel.Application/
└── Authorization/
    ├── AppAction.cs              ← moved from Security.Contracts (framework-agnostic)
    ├── IPermissionCatalog.cs     ← NEW: marker interface each module implements
    ├── PermissionDescriptor.cs   ← NEW: record DTO
    └── PermissionGroup.cs        ← NEW: replaces AppRoleGroup
```

### 6.3 Per-module layout

```
ContentCore.Contracts/
├── IntegrationEvents/...
└── Authorization/
    ├── ContentCoreFeatures.cs            ← feature constants OWNED by ContentCore
    └── ContentCorePermissionCatalog.cs   ← implements IPermissionCatalog

ContentPlaces.Contracts/
├── IntegrationEvents/...
└── Authorization/
    ├── ContentPlacesFeatures.cs
    └── ContentPlacesPermissionCatalog.cs

Security.Contracts/
├── IntegrationEvents/...
└── Authorization/
    ├── AppRoles.cs                       ← stays: role names + privilege enum
    ├── RolePrivilegeLevel.cs             ← stays: security domain concept
    ├── SecurityFeatures.cs               ← NEW: Security's OWN features (Role, User, RoleClaim, System)
    └── SecurityPermissionCatalog.cs      ← implements IPermissionCatalog for Security only

... same pattern for every future module
```

### 6.4 Security module (reshaped)

```
Security.Infrastructure/
└── Seeding/
    ├── PermissionSeeder.cs        ← NEW: scans ALL IPermissionCatalog impls via DI
    ├── RolePermissionMapping.cs   ← NEW: role-to-permission assignment policy
    └── SecurityDataSeeder.cs      ← modified: delegates to the two above
    (AppPermissions.cs deleted in PR 4)
```

---

## 7. Core Abstractions (Complete Source)

### 7.1 `MustHavePermissionAttribute`

```csharp
// YallaJo.SharedKernel.Presentation/Authorization/MustHavePermissionAttribute.cs
using Microsoft.AspNetCore.Authorization;

namespace YallaJo.SharedKernel.Presentation.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class MustHavePermissionAttribute : AuthorizeAttribute
{
    public string Feature { get; }
    public string Action { get; }

    public MustHavePermissionAttribute(string feature, string action)
        : base(PermissionPolicyNames.Build(feature, action))
    {
        Feature = feature;
        Action = action;
    }
}
```

### 7.2 `PermissionPolicyNames`

```csharp
// YallaJo.SharedKernel.Presentation/Authorization/PermissionPolicyNames.cs
namespace YallaJo.SharedKernel.Presentation.Authorization;

public static class PermissionPolicyNames
{
    public const string Prefix = "Permission.";

    public static string Build(string feature, string action)
        => $"{Prefix}{feature}.{action}";

    public static bool IsPermissionPolicy(string policyName)
        => policyName is not null
           && policyName.StartsWith(Prefix, StringComparison.Ordinal);
}
```

### 7.3 `AppAction` (framework-agnostic verbs)

```csharp
// YallaJo.SharedKernel.Application/Authorization/AppAction.cs
namespace YallaJo.SharedKernel.Application.Authorization;

public static class AppAction
{
    public const string Read       = nameof(Read);
    public const string Create     = nameof(Create);
    public const string Update     = nameof(Update);
    public const string Delete     = nameof(Delete);
    public const string UpdateSelf = nameof(UpdateSelf);
    public const string UpdateAny  = nameof(UpdateAny);
    public const string DeleteAny  = nameof(DeleteAny);
    public const string SoftDelete = nameof(SoftDelete);
    public const string Approve    = nameof(Approve);
    public const string Reject     = nameof(Reject);
    public const string Suspend    = nameof(Suspend);
    public const string Reinstate  = nameof(Reinstate);
}
```

### 7.4 `IPermissionCatalog` + `PermissionDescriptor` + `PermissionGroup`

```csharp
// YallaJo.SharedKernel.Application/Authorization/IPermissionCatalog.cs
namespace YallaJo.SharedKernel.Application.Authorization;

/// <summary>
/// Implemented by each module's Contracts project to publish its permission
/// surface. Security.Infrastructure discovers all implementations via DI
/// and seeds the database. No module references another module's catalog.
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>Stable module identifier — used for grouping in UI + logs.</summary>
    string ModuleName { get; }

    /// <summary>All permissions this module contributes.</summary>
    IReadOnlyList<PermissionDescriptor> Permissions { get; }
}

// YallaJo.SharedKernel.Application/Authorization/PermissionDescriptor.cs
public sealed record PermissionDescriptor(
    string Feature,
    string Action,
    string Group,
    string Description,
    bool IsGuestAccessible = false)
{
    /// <summary>Deterministic claim value: "Permission.{Feature}.{Action}"</summary>
    public string Name => $"Permission.{Feature}.{Action}";
}

// YallaJo.SharedKernel.Application/Authorization/PermissionGroup.cs
public static class PermissionGroup
{
    public const string SystemAccess      = nameof(SystemAccess);
    public const string ContentManagement = nameof(ContentManagement);
    public const string BookingOperations = nameof(BookingOperations);
    public const string FinanceOperations = nameof(FinanceOperations);
    public const string ModerationTools   = nameof(ModerationTools);
    public const string SupportOperations = nameof(SupportOperations);
    public const string AnalyticsAccess   = nameof(AnalyticsAccess);
}
```

### 7.5 Runtime trio

```csharp
// YallaJo.SharedKernel.Presentation/Authorization/PermissionRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

// YallaJo.SharedKernel.Presentation/Authorization/PermissionAuthorizationHandler.cs
using Microsoft.AspNetCore.Authorization;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "Permission";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(c =>
                c.Type == PermissionClaimType && c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

// YallaJo.SharedKernel.Presentation/Authorization/PermissionPolicyProvider.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (PermissionPolicyNames.IsPermissionPolicy(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}
```

### 7.6 One-line DI helper

```csharp
// YallaJo.SharedKernel.Presentation/Authorization/AuthorizationServiceCollectionExtensions.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace YallaJo.SharedKernel.Presentation.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Wires the permission-based authorization pipeline:
    ///   - PermissionPolicyProvider dynamically builds policies
    ///     for any "Permission.{Feature}.{Action}" policy name.
    ///   - PermissionAuthorizationHandler checks the user's
    ///     "Permission" claims for a match.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
```

`Program.cs` collapses from ~3 lines of auth wiring to `services.AddPermissionAuthorization();`.

---

## 8. The Aggregation Pattern

### 8.1 Example module catalog (ContentCore)

```csharp
// ContentCore.Contracts/Authorization/ContentCoreFeatures.cs
namespace ContentCore.Contracts.Authorization;

public static class ContentCoreFeatures
{
    public const string Category            = nameof(Category);
    public const string CategoryTranslation = nameof(CategoryTranslation);
    public const string Tag                 = nameof(Tag);
    public const string Specialization      = nameof(Specialization);
    public const string Language            = nameof(Language);
    public const string Attachment          = nameof(Attachment);
    public const string EntityCategory      = nameof(EntityCategory);
    public const string EntityImage         = nameof(EntityImage);
    public const string EntityTag           = nameof(EntityTag);
    public const string TranslationCache    = nameof(TranslationCache);
}

// ContentCore.Contracts/Authorization/ContentCorePermissionCatalog.cs
using YallaJo.SharedKernel.Application.Authorization;

namespace ContentCore.Contracts.Authorization;

public sealed class ContentCorePermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "ContentCore";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(ContentCoreFeatures.Category, AppAction.Read,   PermissionGroup.ContentManagement, "View categories"),
        new(ContentCoreFeatures.Category, AppAction.Create, PermissionGroup.ContentManagement, "Create a category"),
        new(ContentCoreFeatures.Category, AppAction.Update, PermissionGroup.ContentManagement, "Update a category"),
        new(ContentCoreFeatures.Category, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a category"),

        new(ContentCoreFeatures.Tag, AppAction.Read,   PermissionGroup.ContentManagement, "View tags"),
        new(ContentCoreFeatures.Tag, AppAction.Create, PermissionGroup.ContentManagement, "Create a tag"),
        new(ContentCoreFeatures.Tag, AppAction.Update, PermissionGroup.ContentManagement, "Update a tag"),
        new(ContentCoreFeatures.Tag, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a tag"),
        // ... rest
    ];
}
```

### 8.2 Registration in module DI

```csharp
// ContentCore.Infrastructure/DependencyInjection.cs (example)
public static IServiceCollection AddContentCoreModule(
    this IServiceCollection services, IConfiguration config)
{
    // ... existing wiring ...
    services.AddSingleton<IPermissionCatalog, ContentCorePermissionCatalog>(); // NEW
    return services;
}
```

> **NOTE**: Register the catalog in `{Module}.Infrastructure/DependencyInjection.cs` — NOT in the Contracts project — so `{Module}.Contracts` remains free of DI container dependencies.

### 8.3 Security discovers all catalogs at seed time

```csharp
// Security.Infrastructure/Seeding/PermissionSeeder.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Security.Domain.Entities;
using Security.Infrastructure.Persistence;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Infrastructure.Seeding;

public sealed class PermissionSeeder(
    IEnumerable<IPermissionCatalog> catalogs,       // ← all modules' catalogs injected
    SecurityDbContext dbContext,
    ILogger<PermissionSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var allDescriptors = catalogs
            .SelectMany(c => c.Permissions.Select(p => (c.ModuleName, Permission: p)))
            .ToList();

        logger.LogInformation(
            "Seeding {Count} permissions from {ModuleCount} modules: {Modules}",
            allDescriptors.Count,
            catalogs.Count(),
            string.Join(", ", catalogs.Select(c => c.ModuleName)));

        foreach (var (moduleName, descriptor) in allDescriptors)
        {
            var exists = await dbContext.Permissions
                .AnyAsync(p => p.Name == descriptor.Name, ct);

            if (!exists)
            {
                // ⚠️ Adjust factory call to match your Permission entity's constructor/factory
                var permission = Permission.Create(
                    name:        descriptor.Name,
                    feature:     descriptor.Feature,
                    action:      descriptor.Action,
                    group:       descriptor.Group,
                    description: descriptor.Description);

                dbContext.Permissions.Add(permission);
                logger.LogDebug("+ {Permission}", descriptor.Name);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
```

### 8.4 Role-to-permission mapping (extensible policy)

```csharp
// Security.Infrastructure/Seeding/RolePermissionMapping.cs
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Infrastructure.Seeding;

public sealed class RolePermissionMapping(IEnumerable<IPermissionCatalog> catalogs)
{
    private readonly IReadOnlyList<PermissionDescriptor> _all =
        catalogs.SelectMany(c => c.Permissions).ToList();

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) =>
        roleName switch
        {
            AppRoles.Owner =>
                _all.Select(p => p.Name).ToList(),

            AppRoles.SuperAdmin =>
                _all.Where(p => !IsOwnerOnly(p)).Select(p => p.Name).ToList(),

            AppRoles.Admin =>
                _all.Where(p => !IsOwnerOnly(p) && !IsSuperAdminOnly(p))
                    .Select(p => p.Name).ToList(),

            AppRoles.User =>
                _all.Where(p => (p.Feature == SecurityFeatures.User && p.Action == AppAction.UpdateSelf)
                             || p.IsGuestAccessible)
                    .Select(p => p.Name).ToList(),

            AppRoles.TourGuide =>
                _all.Where(p => p.Group == PermissionGroup.ContentManagement
                             && p.Action is AppAction.Read or AppAction.Create)
                    .Select(p => p.Name).ToList(),

            AppRoles.Guest =>
                _all.Where(p => p.IsGuestAccessible).Select(p => p.Name).ToList(),

            _ => []
        };

    private static bool IsOwnerOnly(PermissionDescriptor p) =>
        p.Feature == SecurityFeatures.System && p.Action == AppAction.Update;

    private static bool IsSuperAdminOnly(PermissionDescriptor p) =>
        p.Feature == SecurityFeatures.User && p.Action == AppAction.DeleteAny;
}
```

---

## 9. Runtime Flow

```
HTTP POST /api/content-core/tags
  ├─ Presentation: [MustHavePermission(ContentCoreFeatures.Tag, AppAction.Create)]
  │    └─ builds policy name "Permission.Tag.Create"
  │
  ├─ ASP.NET Core Authorization Middleware
  │    └─ PermissionPolicyProvider.GetPolicyAsync("Permission.Tag.Create")
  │         └─ synthesizes policy with PermissionRequirement("Permission.Tag.Create")
  │
  ├─ PermissionAuthorizationHandler.HandleRequirementAsync()
  │    └─ checks User.HasClaim(type: "Permission", value: "Permission.Tag.Create")
  │         ├─ Yes → context.Succeed() → endpoint executes
  │         └─ No  → 403 Forbidden (via global ProblemDetails)
  │
  └─ Endpoint handler (if authorized) → MediatR → handler → Result<T>
```

The attribute and handler talk through an **immutable string contract** (`Permission.{Feature}.{Action}`). Moving the attribute between assemblies, renaming namespaces, even splitting into a separate service later — none of it affects the string contract. **Zero runtime risk.**

---

## 10. Implementation Plan — 4 PRs

The migration executes as **four small, independently-shippable PRs**. Each PR leaves `main` green, buildable, and deployable. Each is revertable.

| PR | Scope | Risk | Effort |
|---|---|---|---|
| PR 1 | Relocate attribute + runtime | 🟢 Very low | 2 hours |
| PR 2 | Introduce `IPermissionCatalog` abstractions | 🟢 Low | 2 hours |
| PR 3 | Per-module catalogs + seeder migration | 🟡 Medium | 4–6 hours |
| PR 4 | Cleanup + docs + templates | 🟢 Very low | 1–2 hours |

**Total effort**: ~8–12 hours of focused work. Executes over 3–5 days with review cycles.

---

## 11. Pre-Work: Baseline Verification

Before starting, establish a known-good baseline (30 min, do once).

### Tasks

- [ ] Create feature branch: `git checkout -b refactor/authorization-layering`
- [ ] Build the full solution: `dotnet build YallaJo.sln -c Debug --nologo`. Record warnings count.
- [ ] Run all tests: `dotnet test --nologo --verbosity minimal`. Record pass count.
- [ ] Start `YallaJo.Api`, hit `GET /health`, verify 200 OK.
- [ ] **Smoke-test one protected endpoint** with + without required permission. Record the exact policy name emitted (should be `Permission.Tag.Create` format).
- [ ] Run a SQL snapshot against the seeded DB:
  ```sql
  SELECT Name FROM security.Permissions ORDER BY Name
  ```
  Save output as `baseline-permissions.txt`. Used for parity check in PR 3.

### Verification

Golden baseline recorded. Any deviation from this after each PR = investigate immediately.

---

## 12. PR 1 — Relocate Attribute & Runtime

**Branch**: `refactor/auth-pr1-relocate`
**Risk**: 🟢 Very low — pure file move + namespace update. Policy string `Permission.{Feature}.{Action}` unchanged.

### 12.1 Create new files in SharedKernel

Create the 7 files listed in [Section 7 — Core Abstractions](#7-core-abstractions-complete-source).

- `YallaJo.SharedKernel.Application/Authorization/AppAction.cs`
- `YallaJo.SharedKernel.Presentation/Authorization/PermissionPolicyNames.cs`
- `YallaJo.SharedKernel.Presentation/Authorization/MustHavePermissionAttribute.cs`
- `YallaJo.SharedKernel.Presentation/Authorization/PermissionRequirement.cs`
- `YallaJo.SharedKernel.Presentation/Authorization/PermissionAuthorizationHandler.cs`
- `YallaJo.SharedKernel.Presentation/Authorization/PermissionPolicyProvider.cs`
- `YallaJo.SharedKernel.Presentation/Authorization/AuthorizationServiceCollectionExtensions.cs`

### 12.2 Delete old files

```bash
rm Security.Contracts/Authorization/MustHavePermissionAttribute.cs
rm Security.Contracts/Authorization/AppAction.cs
rm YallaJo.Api/Authorization/PermissionRequirement.cs
rm YallaJo.Api/Authorization/PermissionAuthorizationHandler.cs
rm YallaJo.Api/Authorization/PermissionPolicyProvider.cs
```

### 12.3 Update `Security.Contracts.csproj`

Remove the ASP.NET package — no longer needed:
```xml
<!-- DELETE THIS LINE: -->
<PackageReference Include="Microsoft.AspNetCore.Authorization" Version="9.0.13" />
```

### 12.4 Update consumers (17 endpoint files)

For each file in the list in [Section 2.4](#24-consumer-files-17-presentation-endpoints), update the usings:

```csharp
// BEFORE:
using Security.Contracts.Authorization;

// AFTER (if file also uses AppFeatures / AppRoles — the common case):
using Security.Contracts.Authorization;                  // keep — for AppFeatures, AppRoles (still in old location in PR 1)
using YallaJo.SharedKernel.Presentation.Authorization;   // add — for MustHavePermission
using YallaJo.SharedKernel.Application.Authorization;    // add — for AppAction
```

### 12.5 Update internal references in `Security.Infrastructure`

`Security.Infrastructure/Seeding/AppPermissions.cs` references `AppAction`. Add:
```csharp
using YallaJo.SharedKernel.Application.Authorization; // for AppAction
```

### 12.6 Simplify `Program.cs`

```csharp
// REMOVE these lines:
// builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
// builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// REPLACE WITH:
using YallaJo.SharedKernel.Presentation.Authorization;
// ...
builder.Services.AddPermissionAuthorization();
```

Remove the old `using YallaJo.Api.Authorization;` from `Program.cs`.

### 12.7 Verify

- [ ] `dotnet build YallaJo.sln -c Debug --nologo` — zero errors, warning count ≤ baseline.
- [ ] `dotnet test --nologo` — all tests pass (same count as baseline).
- [ ] Start API. `GET /health` → 200.
- [ ] Smoke-test the same protected endpoint from Pre-Work. Same 401/403/200 behavior.
- [ ] `grep -r "YallaJo.Api.Authorization" --include="*.cs"` → **zero results**.
- [ ] `grep -r "Security.Contracts.Authorization.MustHavePermission" --include="*.cs"` → **zero results**.

### 12.8 Commit & Ship

```bash
git add -A
git commit -m "refactor(auth): relocate MustHavePermissionAttribute + runtime to SharedKernel

- Move MustHavePermissionAttribute from Security.Contracts to SharedKernel.Presentation
- Move AppAction from Security.Contracts to SharedKernel.Application (framework-agnostic verbs)
- Move PermissionRequirement/Handler/PolicyProvider from YallaJo.Api to SharedKernel.Presentation
- Introduce PermissionPolicyNames single-source-of-truth for policy string format
- Introduce AddPermissionAuthorization() DI extension
- Drop Microsoft.AspNetCore.Authorization package from Security.Contracts
- Update 17 consumer files with new usings

Zero runtime behavior change — the emitted policy name 'Permission.{Feature}.{Action}'
and the 'Permission' claim type are preserved identically."

git push -u origin refactor/auth-pr1-relocate
# Open PR, merge after review
```

---

## 13. PR 2 — Introduce `IPermissionCatalog` Abstractions

**Branch**: `refactor/auth-pr2-catalog-abstractions`
**Risk**: 🟢 Low — new types only, no deletions. Old `AppFeatures` and seeder stay intact.

### 13.1 Add abstractions to `SharedKernel.Application`

Create the three files shown in [Section 7.4](#74-ipermissioncatalog--permissiondescriptor--permissiongroup):

- `YallaJo.SharedKernel.Application/Authorization/PermissionGroup.cs`
- `YallaJo.SharedKernel.Application/Authorization/PermissionDescriptor.cs`
- `YallaJo.SharedKernel.Application/Authorization/IPermissionCatalog.cs`

### 13.2 Verify

- [ ] `dotnet build` green.
- [ ] No existing code is modified yet — these are pure additions.

### 13.3 Commit & Ship

```bash
git commit -m "feat(auth): add IPermissionCatalog abstractions in SharedKernel.Application

Introduces the contract each module will implement in PR 3 to publish its
permission surface:
  - IPermissionCatalog    (marker interface, ModuleName + Permissions)
  - PermissionDescriptor  (Feature, Action, Group, Description, IsGuestAccessible)
  - PermissionGroup       (cross-cutting group constants)

No existing code modified. Prepares for per-module catalog split in PR 3."
```

---

## 14. PR 3 — Per-Module Catalogs & Seeder Migration

**Branch**: `refactor/auth-pr3-module-catalogs`
**Risk**: 🟡 Medium — touches seeding logic. Mitigation: DB parity check against baseline snapshot.

This is the largest PR. Execute in **sub-steps**, building after each.

### 14.1 Create module catalogs

For each module that has `MustHavePermission` usages, create:
1. `{Module}.Contracts/Authorization/{Module}Features.cs` — feature string constants
2. `{Module}.Contracts/Authorization/{Module}PermissionCatalog.cs` — `IPermissionCatalog` impl

**Modules to cover in this PR** (based on current endpoint usage):

| Module | Features owned |
|---|---|
| **Security** | `Role`, `UserRole`, `RoleClaim`, `User`, `System` |
| **ContentCore** | `Category`, `CategoryTranslation`, `Specialization`, `Tag`, `EntityCategory`, `EntityImage`, `EntityTag`, `TranslationCache`, `Language`, `Attachment` |
| **ContentPlaces** | `Place`, `BusinessStaff`, `BusinessAmenity`, `AccessibilityFeature`, `ServiceItem` |
| **Auth** (if applicable) | Any auth-specific features currently in `AppFeatures` |

Example for **Security**:

```csharp
// Security.Contracts/Authorization/SecurityFeatures.cs
namespace Security.Contracts.Authorization;

public static class SecurityFeatures
{
    public const string Role      = nameof(Role);
    public const string UserRole  = nameof(UserRole);
    public const string RoleClaim = nameof(RoleClaim);
    public const string User      = nameof(User);
    public const string System    = nameof(System);
}
```

```csharp
// Security.Contracts/Authorization/SecurityPermissionCatalog.cs
using YallaJo.SharedKernel.Application.Authorization;

namespace Security.Contracts.Authorization;

public sealed class SecurityPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "Security";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new(SecurityFeatures.Role, AppAction.Read,   PermissionGroup.SystemAccess, "View roles"),
        new(SecurityFeatures.Role, AppAction.Create, PermissionGroup.SystemAccess, "Create a new role"),
        new(SecurityFeatures.Role, AppAction.Update, PermissionGroup.SystemAccess, "Edit a role"),
        new(SecurityFeatures.Role, AppAction.Delete, PermissionGroup.SystemAccess, "Delete a role"),

        new(SecurityFeatures.UserRole, AppAction.Create, PermissionGroup.SystemAccess, "Assign a role to a user"),
        new(SecurityFeatures.UserRole, AppAction.Delete, PermissionGroup.SystemAccess, "Remove a role from a user"),

        new(SecurityFeatures.RoleClaim, AppAction.Read,   PermissionGroup.SystemAccess, "View role claims"),
        new(SecurityFeatures.RoleClaim, AppAction.Create, PermissionGroup.SystemAccess, "Add a claim to a role"),
        new(SecurityFeatures.RoleClaim, AppAction.Delete, PermissionGroup.SystemAccess, "Remove a claim from a role"),

        new(SecurityFeatures.User, AppAction.Read,       PermissionGroup.SystemAccess, "View any user"),
        new(SecurityFeatures.User, AppAction.Create,     PermissionGroup.SystemAccess, "Create a user profile"),
        new(SecurityFeatures.User, AppAction.UpdateAny,  PermissionGroup.SystemAccess, "Update any user"),
        new(SecurityFeatures.User, AppAction.DeleteAny,  PermissionGroup.SystemAccess, "Hard-delete any user"),
        new(SecurityFeatures.User, AppAction.SoftDelete, PermissionGroup.SystemAccess, "Soft-delete any user"),
        new(SecurityFeatures.User, AppAction.UpdateSelf, PermissionGroup.SystemAccess, "Update own profile"),

        new(SecurityFeatures.System, AppAction.Update, PermissionGroup.SystemAccess, "Manage system settings"),
    ];
}
```

Repeat the pattern for **ContentCore**, **ContentPlaces**, and **Auth**.

### 14.2 Ensure Contracts projects can use the abstractions

Each `{Module}.Contracts.csproj` must reference `SharedKernel.Application` to use `IPermissionCatalog`, `PermissionDescriptor`, `AppAction`, `PermissionGroup`:

```xml
<ProjectReference Include="..\YallaJo.SharedKernel.Application\YallaJo.SharedKernel.Application.csproj" />
```

> **IMPORTANT**: This brings MediatR/FluentValidation/HybridCache packages into Contracts transitively. That's acceptable — they're NuGet packages, not bounded-context code. But if you want zero transitive bleed, an alternative is to create a minimal `YallaJo.SharedKernel.Authorization` project that contains only `IPermissionCatalog` + `PermissionDescriptor` + `AppAction` + `PermissionGroup`, and have `SharedKernel.Application` reference *that*. This decision is optional — not required for the refactor to work.

### 14.3 Register catalogs in each module's DI

```csharp
// Security.Infrastructure/DependencyInjection.cs (example)
services.AddSingleton<IPermissionCatalog, SecurityPermissionCatalog>();

// ContentCore.Infrastructure/DependencyInjection.cs
services.AddSingleton<IPermissionCatalog, ContentCorePermissionCatalog>();

// ContentPlaces.Infrastructure/DependencyInjection.cs
services.AddSingleton<IPermissionCatalog, ContentPlacesPermissionCatalog>();
```

Register in `{Module}.Infrastructure/DependencyInjection.cs` — NOT in Contracts — so `{Module}.Contracts` stays free of DI container dependencies.

### 14.4 Create the aggregating seeder

Create `Security.Infrastructure/Seeding/PermissionSeeder.cs` as shown in [Section 8.3](#83-security-discovers-all-catalogs-at-seed-time).

### 14.5 Create role-to-permission mapping

Create `Security.Infrastructure/Seeding/RolePermissionMapping.cs` as shown in [Section 8.4](#84-role-to-permission-mapping-extensible-policy).

### 14.6 Register seeders in Security DI

```csharp
// Security.Infrastructure/DependencyInjection.cs
services.AddScoped<PermissionSeeder>();
services.AddScoped<RolePermissionMapping>();
```

### 14.7 Update `SecurityDataSeeder` to use new seeder

```csharp
// Security.Infrastructure/Seeding/SecurityDataSeeder.cs (refactored)
public sealed class SecurityDataSeeder(
    SecurityDbContext db,
    PermissionSeeder permissionSeeder,
    RolePermissionMapping roleMapping,
    ILogger<SecurityDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        await permissionSeeder.SeedAsync(ct);
        await SeedRolesAsync(ct);
        await SeedRolePermissionsAsync(ct);
    }

    private async Task SeedRolePermissionsAsync(CancellationToken ct)
    {
        foreach (var roleName in AppRoles.AllRoles)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName, ct);
            if (role is null) continue;

            var permissionNames = roleMapping.GetPermissionsForRole(roleName);
            // ... existing logic to sync role claims ...
        }

        await db.SaveChangesAsync(ct);
    }
    // ...
}
```

### 14.8 Update endpoint files to use new feature classes

Replace every `AppFeatures.X` reference with the corresponding `{Module}Features.X`:

| Old | New |
|---|---|
| `AppFeatures.Role` | `SecurityFeatures.Role` |
| `AppFeatures.UserRole` | `SecurityFeatures.UserRole` |
| `AppFeatures.User` | `SecurityFeatures.User` |
| `AppFeatures.Category` | `ContentCoreFeatures.Category` |
| `AppFeatures.Tag` | `ContentCoreFeatures.Tag` |
| `AppFeatures.Place` | `ContentPlacesFeatures.Place` |
| `AppFeatures.BusinessStaff` | `ContentPlacesFeatures.BusinessStaff` |
| `AppFeatures.AccessibilityFeature` | `ContentPlacesFeatures.AccessibilityFeature` |
| `AppFeatures.ServiceItem` | `ContentPlacesFeatures.ServiceItem` |
| ...etc | ...etc |

### 14.9 DB parity check (CRITICAL)

- [ ] Drop & recreate the DB in a test environment: `dotnet ef database drop` → app startup seeds.
- [ ] Run: `SELECT Name FROM security.Permissions ORDER BY Name` → save as `new-permissions.txt`.
- [ ] Diff: `diff baseline-permissions.txt new-permissions.txt` → **must be empty**.
- [ ] If diff non-empty: identify missing/extra permissions, reconcile the catalog, rebuild, retest.

### 14.10 Keep old `AppPermissions.cs` & `AppFeatures.cs` alive temporarily

At this point, **both paths coexist**. Old `Security.Infrastructure/Seeding/AppPermissions.cs` still compiles but is **no longer called**. Leave it in the repo for one more PR cycle in case of rollback need.

### 14.11 Verify

- [ ] `dotnet build` green.
- [ ] `dotnet test` — all tests pass.
- [ ] DB parity check passes (14.9).
- [ ] Smoke-test: login → receive JWT → hit protected endpoint → 200.
- [ ] Log verification: at app startup, look for:
  ```
  Seeding N permissions from M modules: Security, ContentCore, ContentPlaces, ...
  ```

### 14.12 Commit & Ship

```bash
git commit -m "refactor(auth): split permission catalog per-module via IPermissionCatalog

Each module now owns its own permission surface:
  - Security:      Role, UserRole, RoleClaim, User, System
  - ContentCore:   Category, Tag, Language, Attachment, + 6 others
  - ContentPlaces: Place, BusinessStaff, BusinessAmenity, AccessibilityFeature, ServiceItem

Introduces:
  - {Module}Features.cs             (string constants owned by the module)
  - {Module}PermissionCatalog.cs    (IPermissionCatalog impl)
  - PermissionSeeder                (aggregates all catalogs via DI discovery)
  - RolePermissionMapping           (policy for which role gets which permissions)

SecurityDataSeeder delegates permission seeding to PermissionSeeder.
Old AppPermissions.cs temporarily kept (unreferenced) for PR-4 cleanup.

DB parity verified: seeded permissions match baseline row-for-row."
```

---

## 15. PR 4 — Cleanup & Documentation

**Branch**: `refactor/auth-pr4-cleanup`
**Risk**: 🟢 Very low — deletion of dead code + doc updates.

### 15.1 Delete dead code

```bash
rm Security.Infrastructure/Seeding/AppPermissions.cs
rm Security.Contracts/Authorization/AppFeatures.cs    # split into {Module}Features
rm Security.Contracts/Authorization/AppRoleGroup.cs   # replaced by PermissionGroup
```

After deletion, build will fail anywhere that still references `AppFeatures.X`. These are bugs left from PR 3. Fix by pointing each to the correct `{Module}Features.X`.

Use `grep -rn "AppFeatures\." --include="*.cs"` to find remainders.

### 15.2 Update documentation

Update `Agents/agent-context.md` — add/replace the "Authorization Pattern" section:

````markdown
### Authorization Pattern

YallaJo uses permission-based authorization with per-module ownership.

**Where things live:**
- `YallaJo.SharedKernel.Presentation/Authorization/` — cross-cutting primitives
  - `MustHavePermissionAttribute` — applied to endpoints
  - `PermissionPolicyNames` — single source for policy string format
  - `PermissionRequirement`, `PermissionAuthorizationHandler`, `PermissionPolicyProvider` — runtime
  - `AddPermissionAuthorization()` — one-line DI wiring
- `YallaJo.SharedKernel.Application/Authorization/` — framework-agnostic contracts
  - `AppAction` — CRUD verb constants
  - `IPermissionCatalog` — interface each module implements
  - `PermissionDescriptor`, `PermissionGroup`
- `{Module}.Contracts/Authorization/` — module-owned
  - `{Module}Features.cs` — feature constants this module owns
  - `{Module}PermissionCatalog.cs` — implements `IPermissionCatalog`

**Adding a new module:**
1. Create `{Module}.Contracts/Authorization/{Module}Features.cs` with string constants
2. Create `{Module}PermissionCatalog : IPermissionCatalog` listing all permissions
3. Register in `{Module}.Infrastructure/DependencyInjection.cs`:
   ```csharp
   services.AddSingleton<IPermissionCatalog, {Module}PermissionCatalog>();
   ```
4. Security's `PermissionSeeder` will auto-discover on next startup

**NEVER** add another module's features to your module's catalog. The boundary is strict.
````

### 15.3 Add scaffold templates

**File**: `Agents/templates/PermissionCatalog.cs.template`
```csharp
using YallaJo.SharedKernel.Application.Authorization;

namespace {{Module}}.Contracts.Authorization;

public sealed class {{Module}}PermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "{{Module}}";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        new({{Module}}Features.{{Entity}}, AppAction.Read,   PermissionGroup.ContentManagement, "View {{entity}}"),
        new({{Module}}Features.{{Entity}}, AppAction.Create, PermissionGroup.ContentManagement, "Create a {{entity}}"),
        new({{Module}}Features.{{Entity}}, AppAction.Update, PermissionGroup.ContentManagement, "Update a {{entity}}"),
        new({{Module}}Features.{{Entity}}, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a {{entity}}"),
    ];
}
```

**File**: `Agents/templates/ModuleFeatures.cs.template`
```csharp
namespace {{Module}}.Contracts.Authorization;

public static class {{Module}}Features
{
    public const string {{Entity}} = nameof({{Entity}});
    // Add one constant per aggregate exposed through endpoints
}
```

### 15.4 Update `scaffold.ps1`

Add steps to generate the two new files when scaffolding a module. Also inject `services.AddSingleton<IPermissionCatalog, ...>();` into the generated `DependencyInjection.cs`.

### 15.5 Add new gotcha to `agent-context.md`

```markdown
### Gotcha: Forgetting to register IPermissionCatalog

Every new module MUST register its catalog in DI:
\`\`\`csharp
services.AddSingleton<IPermissionCatalog, {Module}PermissionCatalog>();
\`\`\`
Symptom: permissions don't appear in DB after seed. Endpoints return 403 for all users.
Fix: check Security.Infrastructure startup log for "Seeding N permissions from M modules".
```

### 15.6 Verify

- [ ] `dotnet build` green.
- [ ] `dotnet test` green.
- [ ] `grep -r "AppFeatures\." --include="*.cs"` → **zero results** (all migrated).
- [ ] `grep -r "AppRoleGroup\." --include="*.cs"` → **zero results** (all use `PermissionGroup`).
- [ ] Full smoke test: login → hit 3 protected endpoints from different modules → all 200.

### 15.7 Commit & Ship

```bash
git commit -m "refactor(auth): remove deprecated AppFeatures/AppPermissions god-classes

Final cleanup after PR 3 migration:
  - Delete Security.Infrastructure/Seeding/AppPermissions.cs (replaced by PermissionSeeder)
  - Delete Security.Contracts/Authorization/AppFeatures.cs (split per-module)
  - Delete Security.Contracts/Authorization/AppRoleGroup.cs (replaced by PermissionGroup)
  - Update Agents/agent-context.md with new authorization pattern docs
  - Add scaffold templates for PermissionCatalog and ModuleFeatures
  - Update scaffold.ps1 to generate catalogs for new modules

The authorization subsystem now strictly follows:
  - SharedKernel owns cross-cutting primitives
  - Each module owns its own feature catalog
  - Security aggregates catalogs via DI discovery
  - Adding a module never modifies another module"
```

---

## 16. Post-Migration Verification Checklist

After all 4 PRs merged to main:

- [ ] `grep -r "YallaJo.Api.Authorization" --include="*.cs"` → empty
- [ ] `grep -r "Security.Contracts.Authorization.MustHavePermission" --include="*.cs"` → empty
- [ ] `grep -r "AppFeatures\." --include="*.cs"` → empty
- [ ] `grep -r "AppPermissions\." --include="*.cs"` → empty
- [ ] Fresh DB seed produces exact same `security.Permissions` rows as pre-migration baseline
- [ ] All 17 endpoint files build and serve correctly
- [ ] Startup log shows `"Seeding N permissions from M modules: Security, ContentCore, ContentPlaces, ..."`
- [ ] Integration test suite passes
- [ ] Manual smoke test: Admin role can hit all admin endpoints; User role cannot
- [ ] `Agents/agent-context.md` reflects the new architecture

---

## 17. Rollback Strategy

Each PR is revertible with `git revert`. Because permissions seed idempotently by `Name`, reverting produces no orphaned claim rows — roles retain their claims, newly-seeded permissions stay but are unreferenced.

**Emergency rollback of PR 3** (the risky one):

1. `git revert <PR3-merge-commit>`
2. Deploy immediately.
3. Old `AppPermissions.cs` still exists (kept alive intentionally in PR 3 step 14.10).
4. App re-wires to old seeder. No data loss.

---

## 18. Timeline

| Day | Work |
|---|---|
| Day 1 AM | Pre-work baseline + PR 1 (relocate) — ship by EOD |
| Day 2 | Review feedback on PR 1, merge. Start PR 2 (abstractions) — ship same day |
| Day 3 | PR 3 (catalogs + seeder) — largest, expect review cycles |
| Day 4 | Address PR 3 review, merge. DB parity re-verified in staging |
| Day 5 | PR 4 (cleanup + docs) — ship |

**Total**: 5 working days for a careful, review-driven execution. **2 days** if working solo with self-review only.

---

## 19. Success Criteria

1. ✅ Adding a new module requires zero edits to any other module's code
2. ✅ `Security.Contracts.csproj` no longer references `Microsoft.AspNetCore.Authorization`
3. ✅ No presentation project references `Security.Contracts` solely for authorization — only for genuine Security integration events
4. ✅ `PermissionSeeder` logs the module list at startup, proving DI discovery works
5. ✅ DB seeded permission set is identical to pre-migration baseline
6. ✅ Agent documentation + scaffold templates teach the new pattern automatically

---

## Appendix A — Files Changed Summary

### Files deleted permanently

```
Security.Contracts/Authorization/MustHavePermissionAttribute.cs  ← moved (PR 1)
Security.Contracts/Authorization/AppAction.cs                     ← moved (PR 1)
Security.Contracts/Authorization/AppFeatures.cs                   ← split per-module (PR 4)
Security.Contracts/Authorization/AppRoleGroup.cs                  ← replaced by PermissionGroup (PR 4)
Security.Infrastructure/Seeding/AppPermissions.cs                 ← replaced by aggregation (PR 4)
YallaJo.Api/Authorization/PermissionRequirement.cs                ← moved (PR 1)
YallaJo.Api/Authorization/PermissionAuthorizationHandler.cs       ← moved (PR 1)
YallaJo.Api/Authorization/PermissionPolicyProvider.cs             ← moved (PR 1)
```

### Files that stay in Security

```
Security.Contracts/Authorization/AppRoles.cs              ← role names, IsValidRole, privilege math
Security.Contracts/Authorization/RolePrivilegeLevel.cs    ← enum (if extracted)
Security.Contracts/Authorization/SecurityFeatures.cs      ← NEW: Security's OWN features
Security.Contracts/Authorization/SecurityPermissionCatalog.cs ← NEW: IPermissionCatalog for Security
Security.Infrastructure/Seeding/PermissionSeeder.cs       ← NEW
Security.Infrastructure/Seeding/RolePermissionMapping.cs  ← NEW
Security.Infrastructure/Seeding/SecurityDataSeeder.cs     ← modified: uses the two above
```

### New files created

```
SharedKernel.Presentation/Authorization/MustHavePermissionAttribute.cs
SharedKernel.Presentation/Authorization/PermissionPolicyNames.cs
SharedKernel.Presentation/Authorization/PermissionRequirement.cs
SharedKernel.Presentation/Authorization/PermissionAuthorizationHandler.cs
SharedKernel.Presentation/Authorization/PermissionPolicyProvider.cs
SharedKernel.Presentation/Authorization/AuthorizationServiceCollectionExtensions.cs

SharedKernel.Application/Authorization/IPermissionCatalog.cs
SharedKernel.Application/Authorization/PermissionDescriptor.cs
SharedKernel.Application/Authorization/PermissionGroup.cs
SharedKernel.Application/Authorization/AppAction.cs

ContentCore.Contracts/Authorization/ContentCoreFeatures.cs
ContentCore.Contracts/Authorization/ContentCorePermissionCatalog.cs
ContentPlaces.Contracts/Authorization/ContentPlacesFeatures.cs
ContentPlaces.Contracts/Authorization/ContentPlacesPermissionCatalog.cs
Auth.Contracts/Authorization/AuthFeatures.cs              (if applicable)
Auth.Contracts/Authorization/AuthPermissionCatalog.cs     (if applicable)
... (same pattern for every future module)
```

---

## Appendix B — Why This is "Best Case"

| Quality | How this design achieves it |
|---|---|
| **Follows Clean Architecture** | Domain → Application → Infrastructure/Presentation directions preserved. No backward refs. |
| **Follows DDD** | Each bounded context owns its own features. Security owns only its own — not everyone's. |
| **Follows Modular Monolith** | Adding a module touches zero other modules. Security discovers, doesn't dictate. |
| **Extensible** | New permission group? Add one constant. New action verb? Add one constant. New module? Scaffold generates everything. |
| **Testable** | `IPermissionCatalog` is easy to mock. `RolePermissionMapping` is a pure function over catalogs. `PermissionSeeder` is integration-testable against an in-memory DbContext. |
| **Migrates safely** | Four independently-shippable PRs. Each PR leaves the system green. Each revertible. |
| **Matches existing contracts** | Keeps the deterministic `Permission.{Feature}.{Action}` claim value that admin UIs, JWT claim generators, and audit logs already depend on. No data migration needed. |
| **Zero ambiguity for future agents** | `agent-context.md` + scaffold templates tell every future contributor exactly where permission declarations go. |

---

**End of Plan.**
