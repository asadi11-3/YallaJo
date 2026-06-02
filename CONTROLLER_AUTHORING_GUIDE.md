# Building a Controller + Views — Developer Step-by-Step Guide

**Scope:** `src/Hosts/YallaJo.Web` (the Razor MVC **BFF** that consumes `YallaJo.Api`).
**Audience:** developers adding a new screen/feature to the admin/web UI.

This repo is **not** a default MVC template. It keeps the same **four-tier pipeline**
everywhere — `Controller → Facade → ApiClient → IApiClient` — and **every area uses the
same physical layout: layered by type.**

| Area | Layout | Folders |
|---|---|---|
| **All areas** (Auth, Accounts, Content, Admin) | **Layered (by type)** | `Areas/{Area}/{Controllers,Facades,ApiClients,Models/{Feature},Views/{Controller}}` |

> **Layered-by-type is the universal standard.** Files are grouped by their **role**
> (Controllers, Facades, ApiClients, Models, Views), never by feature folder. Models stay
> grouped by feature *inside* `Models/{Feature}/`. Some areas add extra role folders:
> Admin also has `Validators/` and `Helpers/`; Accounts has `Shared/` (for the
> `_AccountSidebar` partial); Auth has `Shared/` (for `_RecaptchaField`).

> TL;DR of the moving parts:
> **`XxxController`** (HTTP + view selection) → **`XxxFacade`** (API result → ViewModel)
> → **`XxxApiClient`** (endpoint URLs) → base **`IApiClient`** (HTTP + resilience + JWT).
> ViewModels/Mappers/Requests/Responses are plain classes grouped under `Models/{Feature}/`.
> Views live under `Views/{Controller}/`.

---

## 0. Mental model (read once)

```
Browser ──HTTP──▶ XxxController : BaseController
                      │  (validates ModelState, owns routing + permissions + view)
                      ▼
                  XxxFacade            ← maps ApiResult<T> → ViewModel; sets cache eviction
                      │
                      ▼
                  XxxApiClient         ← knows the API URL + request/response DTOs
                      │
                      ▼
                  IApiClient (Services/ApiClient.cs)
                      │  GetAsync<T> / PostAsync / PutAsync / PatchAsync / DeleteAsync
                      ▼  returns ApiResult / ApiResult<T>   (never throws for API errors)
                  YallaJo.Api
```

- **Controllers never call `IApiClient` directly** — always through the feature's Facade.
- **Facades never touch `HttpContext`/`TempData`** — they only translate results to view models.
- **API errors are values, not exceptions.** Everything returns `ApiResult` / `ApiResult<T>`.

---

## 1. Decide where the feature lives

Every area uses **one layout: layered by type**. The folder path **is** the namespace.
Files are grouped by their **role**, not by feature. A new screen drops files into the
area's shared layer folders:

| Layer | Folder | Namespace |
|---|---|---|
| Controllers | `Areas/{Area}/Controllers/` | `YallaJo.Web.Areas.{Area}.Controllers` |
| Facades | `Areas/{Area}/Facades/` | `YallaJo.Web.Areas.{Area}.Facades` |
| ApiClients | `Areas/{Area}/ApiClients/` | `YallaJo.Web.Areas.{Area}.ApiClients` |
| Models (DTOs + VMs + Mappers) | `Areas/{Area}/Models/{Feature}/` | `YallaJo.Web.Areas.{Area}.Models.{Feature}` |
| Views | `Areas/{Area}/Views/{Controller}/` | (view path) |

`{Area}` is one of `Auth`, `Accounts`, `Content`, `Admin`. Some areas add extra role folders:

| Extra layer | Where | Namespace | Used for |
|---|---|---|---|
| `Validators/` | `Areas/Admin/Validators/` | `...Areas.Admin.Validators` | FluentValidation validators |
| `Helpers/` | `Areas/Admin/Helpers/` | `...Areas.Admin.Helpers` | static view/format helpers (e.g. `AuditMetadataFormatter`) |
| `Shared/` | `Areas/{Area}/Shared/` | `...Areas.{Area}.Shared` | area-wide partials/VMs (`_RecaptchaField`, `_AccountSidebar`) |

> **Models stay grouped by feature** inside `Models/`. The old per-feature `Requests/`,
> `Responses/`, `ViewModels/` and `Mappers/` sub-folders are **collapsed into one
> `Models/{Feature}/` namespace** — e.g. `Models/Login/LoginVm.cs`,
> `Models/Login/LoginRequest.cs`, `Models/Login/LoginMapper.cs` all declare
> `namespace YallaJo.Web.Areas.Auth.Models.Login;`. The same applies in every area:
> `Models/Languages/LanguageListVm.cs` declares
> `namespace YallaJo.Web.Areas.Admin.Models.Languages;`.

> **Views are discovered automatically.** `Program.cs` registers the area view-location
> format `~/Areas/{2}/Views/{1}/{0}.cshtml` (additive), so a controller action just needs a
> matching `Views/{Controller}/{Action}.cshtml` — no Program.cs change per screen. Shared
> partials live in `Areas/{Area}/Shared/` (resolved by `~/Areas/{2}/Shared/{0}.cshtml`),
> e.g. `_RecaptchaField.cshtml` (Auth), `_AccountSidebar.cshtml` (Accounts). Per-feature
> partials live in `Views/{Controller}/Partials/` and are referenced by relative name
> (e.g. `<partial name="Partials/_MetadataDetails" />`).

> **Cross-tier `using`s are explicit.** Because each tier is its own namespace, a Controller
> needs `using YallaJo.Web.Areas.{Area}.Facades;` to see its Facade, a Facade needs
> `using ...Areas.{Area}.ApiClients;`, and any file touching a model needs
> `using ...Areas.{Area}.Models.{Feature};`. DI is **suffix-based**
> (`FeatureServiceRegistration` registers every `*ApiClient`/`*Facade` as Scoped regardless
> of namespace), so moving a class between folders never needs a DI change.

> The sections below use the **`Languages`** feature in the **Admin** area as the running
> example (it shows the full list + create + edit shape): `Controllers/LanguagesController.cs`,
> `Facades/LanguagesFacade.cs`, `ApiClients/LanguagesApiClient.cs`, `Models/Languages/*`,
> `Views/Languages/{Index,Edit}.cshtml`. The same code works in any area — only the
> `{Area}` namespace segment changes.

---

## 2. Create the structure

Add files to the area's shared layer folders. The files for one feature are **spread across
the layer folders** (not gathered in one feature folder). Example — the `Languages` feature
in the **Admin** area:

```
Areas/Admin/
├── Controllers/
│   └── LanguagesController.cs       # namespace YallaJo.Web.Areas.Admin.Controllers
├── Facades/
│   └── LanguagesFacade.cs           # namespace YallaJo.Web.Areas.Admin.Facades
├── ApiClients/
│   └── LanguagesApiClient.cs        # namespace YallaJo.Web.Areas.Admin.ApiClients
├── Models/
│   └── Languages/                   # namespace YallaJo.Web.Areas.Admin.Models.Languages
│       ├── LanguageItemResponse.cs  #   (inbound DTO)
│       ├── CreateLanguageRequest.cs #   (outbound DTO)
│       ├── UpdateLanguageRequest.cs
│       ├── LanguageRowVm.cs         #   (list/row VM)
│       ├── LanguageListVm.cs
│       ├── CreateLanguageVm.cs      #   (form VM)
│       ├── UpdateLanguageVm.cs
│       └── LanguagesMapper.cs       #   (static VM↔DTO mapping)
├── Validators/                      # (Admin only) namespace ...Areas.Admin.Validators
│   └── HomeVmValidator.cs
├── Helpers/                         # (Admin only) namespace ...Areas.Admin.Helpers
│   └── AuditMetadataFormatter.cs
└── Views/
    └── Languages/
        ├── Index.cshtml
        └── Edit.cshtml
```

Every other area follows the identical shape — just swap the `{Area}` segment. For example
the Accounts `Profile` feature lives in `Areas/Accounts/Controllers/ProfileController.cs`,
`Areas/Accounts/Facades/ProfileFacade.cs`, `Areas/Accounts/Models/Profile/*`,
`Areas/Accounts/Views/Profile/*`, with the shared `Areas/Accounts/Shared/_AccountSidebar.cshtml`
partial. Auth keeps `Areas/Auth/Shared/_RecaptchaField.cshtml`.

> **Naming is a contract.** Classes ending in `ApiClient` or `Facade` are auto-registered
> in DI (see §9) **by name suffix — regardless of folder or namespace**. That is exactly why
> moving the areas to the layered layout needed no DI changes. Always keep the suffixes.

---

## 3. Step 1 — DTOs (Requests + Responses)

These mirror the API's JSON contract. `Response` = what you read, `Request` = what you send.
Use `init` setters; serialization is camelCase (handled by the base client).

`Responses/LanguageItemResponse.cs`
```csharp
namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class LanguageItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NativeName { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsRtl { get; init; }
    public bool IsActive { get; init; }
}
```

`Requests/CreateLanguageRequest.cs` — request DTOs are concise **positional records**:
```csharp
namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed record CreateLanguageRequest(string Code, string Name, string NativeName, bool IsRtl);
```
```csharp
// Requests/UpdateLanguageRequest.cs
public sealed record UpdateLanguageRequest(string Name, string NativeName, bool IsRtl, bool IsActive);
```
(Match the field names/shape the API's endpoints actually accept.)

---

## 4. Step 2 — ViewModels

ViewModels are what `.cshtml` binds to. **Display/list** VMs use `init`; **form** VMs use
`set` + DataAnnotations (so model binding + client validation work).

`ViewModels/LanguageRowVm.cs` (one row in a table)
```csharp
namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class LanguageRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NativeName { get; init; } = string.Empty;
    public bool IsRtl { get; init; }
    public bool IsActive { get; init; }
}
```

`ViewModels/LanguageListVm.cs` (the page model — list + inline create form + filter)
```csharp
namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class LanguageListVm
{
    public IReadOnlyList<LanguageRowVm> Languages { get; init; } = [];
    public CreateLanguageVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }
}
```

`ViewModels/CreateLanguageVm.cs` (a form → use `set` + validation attributes)
```csharp
using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class CreateLanguageVm
{
    [Required, StringLength(10, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string NativeName { get; set; } = string.Empty;

    public bool IsRtl { get; set; }
}
```
(`UpdateLanguageVm` adds `public Guid Id { get; set; }` plus `IsActive`.)

> The list page model (`LanguageListVm`) composes the row list + the inline create-form VM:
> ```csharp
> public IReadOnlyList<LanguageRowVm> Languages { get; init; } = [];
> public CreateLanguageVm Create { get; init; } = new();   // its own fields are `set`
> public bool ActiveOnly { get; init; }
> ```
> The container properties can stay `init` because the controller rebuilds the VM on each
> render; only the **inner form fields** (on `CreateLanguageVm`) need `set` for model binding.

---

## 5. Step 3 — Mapper (static, no DI)

A static class that converts **Response → ViewModel** and **ViewModel → Request**.
Keep it dumb and allocation-only — no I/O, no logic.

`Mappers/LanguagesMapper.cs`
```csharp
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Areas.Admin.Models.Languages;

namespace YallaJo.Web.Areas.Admin.Models.Languages;

public static class LanguagesMapper
{
    public static LanguageRowVm ToRowVm(LanguageItemResponse r) => new()
    {
        Id = r.Id, Code = r.Code, Name = r.Name, NativeName = r.NativeName,
        IsRtl = r.IsRtl, IsActive = r.IsActive,
    };

    // Request records are positional; trim user input here.
    public static CreateLanguageRequest ToCreateRequest(CreateLanguageVm vm) => new(
        Code:       vm.Code.Trim(),
        Name:       vm.Name.Trim(),
        NativeName: vm.NativeName.Trim(),
        IsRtl:      vm.IsRtl);

    public static UpdateLanguageRequest ToUpdateRequest(UpdateLanguageVm vm) => new(
        Name:       vm.Name.Trim(),
        NativeName: vm.NativeName.Trim(),
        IsRtl:      vm.IsRtl,
        IsActive:   vm.IsActive);
}
```

---

## 6. Step 4 — ApiClient (endpoint URLs)

A `sealed` class taking **`IApiClient`** (the seam — easy to mock in tests). Each method is a
thin one-liner binding an HTTP verb + URL + DTOs. **No mapping, no view models here.**

`LanguagesApiClient.cs`
```csharp
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class LanguagesApiClient
{
    private readonly IApiClient _api;
    public LanguagesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<LanguageItemResponse>>> GetLanguagesAsync(
        bool activeOnly, CancellationToken ct = default)
        => _api.GetAsync<List<LanguageItemResponse>>(
            $"/api/v1/content-core/languages?activeOnly={(activeOnly ? "true" : "false")}", ct);

    public Task<ApiResult> CreateAsync(CreateLanguageRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/content-core/languages", request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateLanguageRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/content-core/languages/{id}", request, ct);
}
```

**`IApiClient` verbs available** (all return `ApiResult` / `ApiResult<T>`):
`GetAsync<T>`, `PostAsync<T>` / `PostAsync`, `PutAsync<T>` / `PutAsync`, `PatchAsync`,
`DeleteAsync`, `PostFileAsync<T>`, `PutFileAsync<T>`.
Always pass the `CancellationToken` through.

---

## 7. Step 5 — Facade (ApiResult → ViewModel)

The facade is where the API result becomes a ViewModel and where **failure states are
translated** (`IsUnauthorized` → `ForceSignOut`, conflict/not-found → friendly text, etc.).

> **Output-cache eviction:** if your data is cached (lookups), inject `IOutputCacheStore`
> and call `EvictByTagAsync("lookups", ct)` after a successful write. If your feature is
> not cached, **omit the cache entirely** — just take the ApiClient in the ctor.

`LanguagesFacade.cs`
```csharp
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class LanguagesFacade
{
    private readonly LanguagesApiClient _api;
    private readonly IOutputCacheStore _cache;   // omit if the feature isn't output-cached

    public LanguagesFacade(LanguagesApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<LanguageListVm>> GetLanguagesAsync(bool activeOnly, CancellationToken ct = default)
    {
        var result = await _api.GetLanguagesAsync(activeOnly, ct);
        if (result.IsSuccess)
            return ApiResult<LanguageListVm>.CreateSuccess(new LanguageListVm
            {
                Languages = (result.Data ?? []).Select(LanguagesMapper.ToRowVm).ToList(),
            });
        if (result.IsUnauthorized) return ApiResult<LanguageListVm>.ForceSignOut();
        return ApiResult<LanguageListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> CreateAsync(CreateLanguageVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.CreateAsync(LanguagesMapper.ToCreateRequest(vm), ct),
                                "Could not create language.", ct);

    public async Task<ApiResult> UpdateAsync(UpdateLanguageVm vm, CancellationToken ct = default)
        => await NormalizeAsync(await _api.UpdateAsync(vm.Id, LanguagesMapper.ToUpdateRequest(vm), ct),
                                "Could not update language.", ct);

    // Translate a write result + evict the shared lookups cache on success.
    private async Task<ApiResult> NormalizeAsync(ApiResult result, string fallback, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync("lookups", ct);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsConflict)        return ApiResult.Fail("A language with this code already exists.");
        if (result.IsNotFound)        return ApiResult.Fail("Language not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
```

**`ApiResult` helpers you'll use:** `IsSuccess`, `Data`, `Error`, `StatusCode`,
`ValidationErrors`, and the convenience flags `IsUnauthorized` / `IsForbidden` /
`IsNotFound` / `IsConflict` / `IsValidationError` / `IsTooManyRequests`, plus
`RequireSignOut` (== `IsUnauthorized`). Factories: `CreateSuccess` / `CreateFailure` /
`Ok` / `Fail` / `Invalid` / `ForceSignOut`.

---

## 8. Step 6 — Controller (inherit `BaseController`)

**Always inherit `BaseController`** — it removes the per-action boilerplate. Decorate the
class with `[Area(...)]`, `[Authorize]`, and (admin) a default `[RequirePermission(...)]`.

`LanguagesController.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Languages;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;          // BaseController

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Language.Read)]   // default permission for all actions
public sealed class LanguagesController : BaseController
{
    private readonly LanguagesFacade _facade;
    public LanguagesController(LanguagesFacade facade) => _facade = facade;

    // ---- READ: list -------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetLanguagesAsync(activeOnly, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;   // 401 → /auth/login
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new LanguageListVm { ActiveOnly = activeOnly });
        }
        return View(result.Data);
    }

    // ---- WRITE: create (POST + antiforgery + stronger permission) ----------
    [HttpPost("admin/languages/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Language.Create)]
    public async Task<IActionResult> Create(CreateLanguageVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await ReloadIndex(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Language created.");           // TempData flash → _Alerts
            return RedirectToAction(nameof(Index));    // PRG pattern
        }

        // Server validation → re-render the inline form with errors.
        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create language.");
        return await ReloadIndex(vm, ct);
    }

    // ---- READ: edit form --------------------------------------------------
    [HttpGet("admin/languages/{id:guid}/edit")]
    [RequirePermission(WebPermission.Language.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var list = await _facade.GetLanguagesAsync(activeOnly: false, ct);
        if (GuardSignOut(list) is { } signOut) return signOut;
        if (!list.IsSuccess || list.Data is null)
        {
            SetError(list.Error ?? "Could not load languages.");
            return RedirectToAction(nameof(Index));
        }

        var row = list.Data.Languages.FirstOrDefault(l => l.Id == id);
        if (row is null)
        {
            SetError("Language not found.");
            return RedirectToAction(nameof(Index));
        }

        return View(new UpdateLanguageVm
        {
            Id = row.Id, Name = row.Name, NativeName = row.NativeName,
            IsRtl = row.IsRtl, IsActive = row.IsActive,
        });
    }

    // ---- WRITE: edit submit ----------------------------------------------
    [HttpPost("admin/languages/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Language.Update)]
    public async Task<IActionResult> Edit(Guid id, UpdateLanguageVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Language updated.");
            return RedirectToAction(nameof(Index));
        }

        if (!ApplyValidationErrors(result))
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update language.");
        return View(vm);
    }

    // ---- private view-rebuild helper --------------------------------------
    private async Task<IActionResult> ReloadIndex(CreateLanguageVm create, CancellationToken ct)
    {
        var list = await _facade.GetLanguagesAsync(activeOnly: false, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new LanguageListVm { Languages = list.Data.Languages, Create = create }
            : new LanguageListVm { Create = create };
        return View(nameof(Index), vm);
    }
}
```

### `BaseController` cheat-sheet (use these, don't re-implement)

| Member | Use it for |
|---|---|
| `GuardSignOut(result)` | `if (GuardSignOut(result) is { } r) return r;` — bounce 401s to login. Call after **every** facade call. |
| `RedirectToLogin()` | Manual redirect to `Auth/Login`. (You rarely need it directly.) |
| `SetSuccess(msg)` / `SetError(msg)` | Write a TempData flash banner (rendered by `_Alerts`). |
| `SetFlash(result, "Saved.")` | One-liner: success message on success, API error on failure. |
| `ApplyValidationErrors(result)` | Copies API field errors into `ModelState`; returns `true` if any. |

> **Never** redefine a private `RedirectToLogin()` or copy the `RequireSignOut` block —
> that's exactly what `BaseController` removed.

### Controller rules of thumb
- Every action is `async Task<IActionResult>` and takes a trailing `CancellationToken ct`.
- **Reads** = `[HttpGet]`. **Writes** = `[HttpPost]` + `[ValidateAntiForgeryToken]`.
- Put the **strictest** permission on the action; the class-level one is the baseline.
- Use **PRG** (Post → Redirect → Get) after a successful write; flash via `SetSuccess`.
- Custom routes use explicit templates (e.g. `[HttpPost("admin/languages/{id:guid}/edit")]`).
  Without a template, the default route is `{area}/{controller}/{action}/{id?}`.

---

## 9. Step 7 — DI registration: **nothing to do** ✅

There is **no manual `AddScoped`**. `Program.cs` calls `builder.Services.AddFeatureServices();`
which reflection-scans the assembly and registers every concrete class whose name ends in
**`ApiClient`** or **`Facade`** as `Scoped`. Just name your classes correctly and they're wired.

> The scan keys on the **class-name suffix only — not the folder or namespace.** That is why
> every area could move to the layered layout (`Facades/`, `ApiClients/`) without a single DI
> change. Whether `LanguagesFacade` lives in `Modules/.../Features/Languages/` or `Facades/`, it
> registers identically.

(The base `ApiClient` / `IApiClient`, `IOutputCacheStore`, etc. are already registered.)

---

## 9.5 Composite views — calling **multiple facades** from one screen

Sometimes a single screen needs data from several API endpoints (e.g. a "Security"
page showing active **sessions** + trusted **devices** + linked **external providers**).
**Do not** collapse those features into one god-controller that injects `IApiClient`.
The pipeline contract still holds — you **compose facades**, you do not bypass the tier.

> **Rule:** A controller may inject **as many `Facade`s as the screen needs**. What stays
> forbidden is a controller injecting `IApiClient`/`HttpClient` directly. Each facade keeps
> owning exactly **one** API concern; the *composition* happens above the facade layer.

Pick the pattern that matches *why* you need multiple endpoints:

### Pattern A — Controller composes sibling facades (default)
Best when the composite screen lives in one feature and isn't reused elsewhere. The owning
feature's controller injects the other features' facades and assembles one page ViewModel.
Call `GuardSignOut` after **every** facade call.

```csharp
// Areas/Auth/Controllers/SecurityController.cs   (layered Auth)
[Area("Auth")]
[Authorize]
public sealed class SecurityController : BaseController
{
    private readonly SessionsFacade          _sessions;
    private readonly ExternalProvidersFacade _providers;

    public SecurityController(SessionsFacade sessions, ExternalProvidersFacade providers)
    {
        _sessions  = sessions;
        _providers = providers;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sessions = await _sessions.GetAsync(ct);
        if (GuardSignOut(sessions) is { } r1) return r1;

        var providers = await _providers.ListAsync(ct);
        if (GuardSignOut(providers) is { } r2) return r2;

        var vm = new SecurityPageVm
        {
            Sessions  = sessions.Data!,
            Providers = providers.Data!,
        };
        return View(vm);
    }
}
```

Because facades are auto-registered `Scoped` (§9), injecting `SessionsFacade` into another
controller costs nothing and **reuses** the existing facade — no new wiring, no
duplicated API calls.

### Pattern B — A dedicated aggregate facade (reused composite)
When the **same** composite is needed by more than one controller, wrap the composition in
its own `XxxFacade` that injects the sibling facades and returns one aggregate ViewModel.
The controller then injects just that single facade and stays thin. The aggregate facade
follows all normal facade rules (sealed, no `HttpContext`/`TempData`, returns
`ApiResult<T>`); on the first failing sub-call it returns that failure so the controller's
single `GuardSignOut` still works.

```csharp
public sealed class SecurityOverviewFacade
{
    private readonly SessionsFacade          _sessions;
    private readonly ExternalProvidersFacade _providers;

    public SecurityOverviewFacade(SessionsFacade sessions, ExternalProvidersFacade providers)
    {
        _sessions  = sessions;
        _providers = providers;
    }

    public async Task<ApiResult<SecurityPageVm>> LoadAsync(CancellationToken ct = default)
    {
        var sessions = await _sessions.GetAsync(ct);
        if (!sessions.IsSuccess) return ApiResult<SecurityPageVm>.Fail(sessions.StatusCode, sessions.Error);

        var providers = await _providers.ListAsync(ct);
        if (!providers.IsSuccess) return ApiResult<SecurityPageVm>.Fail(providers.StatusCode, providers.Error);

        return ApiResult<SecurityPageVm>.Ok(new SecurityPageVm
        {
            Sessions  = sessions.Data!,
            Providers = providers.Data!,
        });
    }
}
```

### Pattern C — View Component for a reusable widget
If the "extra endpoint" is really a self-contained widget that appears on **many** pages
(e.g. a session-count badge in the navbar), build a **ViewComponent** backed by its feature
facade instead of threading the data through every controller. The component injects the
facade, renders its own partial, and is dropped into any view with `<vc:...>`.

> **Note on `AuthController`:** the real `AuthController` injects five facades
> (`LoginFacade`/`RegisterFacade`/`ForgotPasswordFacade`/`ResetPasswordFacade`/`VerifyEmailFacade`)
> — that is deliberate Pattern A: every one of its routes is part of the **same**
> unauthenticated entry experience (sign-in / sign-up / forgot / reset / two-factor), so they
> share one controller and one set of views. Composition is for assembling **related** screens
> from multiple facades. What stays forbidden is a controller injecting many facades for
> genuinely *unrelated* responsibilities just to avoid creating a second controller.

---

## 10. Step 8 — Views

Views go under `Views/{Controller}/` in every area. The view-location formats resolve them in
this order:

```
Layered:       ~/Areas/{Area}/Views/{Controller}/{View}.cshtml
               ~/Areas/{Area}/Views/Shared/{View}.cshtml
Area-shared:   ~/Areas/{Area}/Shared/{View}.cshtml   (e.g. Areas/Auth/Shared/_RecaptchaField.cshtml,
                                                            Areas/Accounts/Shared/_AccountSidebar.cshtml)
Global shared: ~/Views/Shared/{View}.cshtml          (_Layout, _Navbar, _Alerts, _ValidationScriptsPartial)
```
These are registered once in `Program.cs` (`AddRazorOptions` → `AreaViewLocationFormats`). The
generic `~/Areas/{2}/Views/{1}/{0}.cshtml` format is **area-agnostic**, so a new screen in **any**
area (Auth, Accounts, Content, Admin) at `Areas/{Area}/Views/{Controller}/{Action}.cshtml` is
picked up at startup with **no per-screen `Program.cs` change**. Per-feature partials live in
`Views/{Controller}/Partials/` and are referenced by relative name
(e.g. `<partial name="Partials/_MetadataDetails" />`).

**Conventions every view follows (matching the existing Languages views):**
- First line is the **strongly-typed model**: `@model ...ViewModels.LanguageListVm`.
- Set `ViewData["Title"]` and **`Layout = "~/Views/Shared/_Layout.cshtml";`** in the `@{ }` block.
- Forms use **tag helpers** (`asp-area`, `asp-controller`, `asp-action`, `asp-for`,
  `asp-validation-for`, `asp-route-id`) — enabled globally via `Views/_ViewImports.cshtml`.
  Because views resolve across areas, **set `asp-area`/`asp-controller` explicitly** on forms
  and action links (don't rely on ambient values).
- Add **`@Html.AntiForgeryToken()`** inside every POST `<form>` (pairs with the controller's
  `[ValidateAntiForgeryToken]`).
- To use `<permission require="...">`, add `@using YallaJo.Web.Infrastructure.Authorization`
  at the top of the view.
- Add client validation by including `_ValidationScriptsPartial` in `@section Scripts`.
- **Preferred for new views:** rely on the shared `_Alerts` partial (rendered by `_Layout`) for
  `TempData["Success"]`/`["Error"]` flash banners instead of hand-writing alert markup.
  *(Some existing views still inline these blocks — that's legacy; don't copy it into new views.)*

`Views/Index.cshtml` (list + permission-gated inline create form)
```cshtml
@using YallaJo.Web.Infrastructure.Authorization
@model YallaJo.Web.Areas.Admin.Models.Languages.LanguageListVm
@{
    ViewData["Title"] = "Languages";
    Layout = "~/Views/Shared/_Layout.cshtml";
}

<div class="container mt-4">
    <h2>Languages</h2>

    @* Flash banners render via the shared _Alerts partial in _Layout — no inline alert markup. *@
    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <permission require="@WebPermission.Language.Create">
        <div class="card mb-4 p-3">
            <h5>Create Language</h5>
            <form asp-area="Admin" asp-controller="Languages" asp-action="Create" method="post">
                @Html.AntiForgeryToken()
                <div class="row g-2">
                    <div class="col-md-3">
                        <label asp-for="Create.Code" class="form-label"></label>
                        <input asp-for="Create.Code" class="form-control" />
                        <span asp-validation-for="Create.Code" class="text-danger"></span>
                    </div>
                    <div class="col-md-3">
                        <label asp-for="Create.Name" class="form-label"></label>
                        <input asp-for="Create.Name" class="form-control" />
                        <span asp-validation-for="Create.Name" class="text-danger"></span>
                    </div>
                    <div class="col-md-3">
                        <label asp-for="Create.NativeName" class="form-label"></label>
                        <input asp-for="Create.NativeName" class="form-control" />
                        <span asp-validation-for="Create.NativeName" class="text-danger"></span>
                    </div>
                    <div class="col-md-3 d-flex align-items-end">
                        <div class="form-check me-3">
                            <input asp-for="Create.IsRtl" class="form-check-input" />
                            <label asp-for="Create.IsRtl" class="form-check-label"></label>
                        </div>
                        <button type="submit" class="btn btn-primary">Create</button>
                    </div>
                </div>
            </form>
        </div>
    </permission>

    <table class="table table-sm table-bordered">
        <thead>
            <tr><th>Code</th><th>Name</th><th>Native</th><th>RTL</th><th>Active</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var l in Model.Languages)
            {
                <tr>
                    <td>@l.Code</td>
                    <td>@l.Name</td>
                    <td>@l.NativeName</td>
                    <td>@(l.IsRtl ? "✓" : "—")</td>
                    <td>@(l.IsActive ? "✓" : "—")</td>
                    <td>
                        <permission require="@WebPermission.Language.Update">
                            <a asp-area="Admin" asp-controller="Languages" asp-action="Edit"
                               asp-route-id="@l.Id" class="btn btn-sm btn-outline-secondary">Edit</a>
                        </permission>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

`Views/Edit.cshtml` (single-record form)
```cshtml
@model YallaJo.Web.Areas.Admin.Models.Languages.UpdateLanguageVm
@{
    ViewData["Title"] = "Edit Language";
    Layout = "~/Views/Shared/_Layout.cshtml";
}

<div class="container mt-4">
    <h2>Edit Language</h2>

    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <form asp-area="Admin" asp-controller="Languages" asp-action="Edit"
          asp-route-id="@Model.Id" method="post" class="col-md-6">
        @Html.AntiForgeryToken()
        <input type="hidden" asp-for="Id" />

        <div class="mb-3">
            <label asp-for="Name" class="form-label"></label>
            <input asp-for="Name" class="form-control" />
            <span asp-validation-for="Name" class="text-danger"></span>
        </div>
        <div class="mb-3">
            <label asp-for="NativeName" class="form-label"></label>
            <input asp-for="NativeName" class="form-control" />
            <span asp-validation-for="NativeName" class="text-danger"></span>
        </div>
        <div class="form-check mb-3">
            <input asp-for="IsRtl" class="form-check-input" />
            <label asp-for="IsRtl" class="form-check-label"></label>
        </div>
        <div class="form-check mb-3">
            <input asp-for="IsActive" class="form-check-input" />
            <label asp-for="IsActive" class="form-check-label"></label>
        </div>

        <button type="submit" class="btn btn-primary">Save</button>
        <a asp-action="Index" class="btn btn-link">Cancel</a>
    </form>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### Permission-gated UI
To show/hide elements by permission inside a view, use the built-in tag helper instead of
inline `User.HasClaim` checks:
```cshtml
<permission require="@WebPermission.Language.Create">
    <a asp-action="Create" class="btn btn-primary">New language</a>
</permission>
```

### Add it to the nav (optional)
If the feature needs a menu entry, add a permission-gated link to
`Views/Shared/_Navbar.cshtml` (it already uses `<permission>` + `asp-area/asp-controller/asp-action`).
Keep new links inside the existing permission blocks.

---

## 11. Step 9 — Permissions

Permission constants live in `Infrastructure/Authorization/WebPermission.cs`, as **nested
static classes**. The string values use the format **`Permission.{Feature}.{Action}`** and must
match exactly what the backend issues as `Permission` claims (`AppPermission.NameFor()`):
```csharp
public static class WebPermission
{
    // ...existing groups: Role, RoleClaim, User, UserRole, System, Category, ...

    public static class Language
    {
        public const string Read   = "Permission.Language.Read";
        public const string Create = "Permission.Language.Create";
        public const string Update = "Permission.Language.Update";
        public const string Delete = "Permission.Language.Delete";
    }
}
```
**RULE (enforced by convention):** never build a permission string inline anywhere else.
All checks go through `WebPermission.X.Y` — via `[RequirePermission(...)]`, the `<permission
require="...">` tag helper, or `ICurrentUser.HasPermission(...)`.

If your feature is a new resource, add a new nested group here first, then reference it.

---

## 12. Verify

1. **Build:** `dotnet build src\Hosts\YallaJo.Web\YallaJo.Web.csproj`
   *(Run it alone — parallel builds can hit a VBCSCompiler DLL lock, CS2012.)*
2. **Run** and navigate to the route (e.g. `/Admin/Languages`).
3. **Check:**
   - List loads; 401 redirects to `/auth/login` (the `GuardSignOut` guard).
   - Create/Edit POSTs succeed; success banner shows (via `_Alerts`).
   - Submitting an invalid form shows field errors (server + client).
   - Permissions hide/deny the actions you gated.

---

## 13. Paginated list queries

**Page anything that can grow** — never return unbounded collections.

### The wire contract: a **per-feature** paged Response (no shared generic)
There is **no** generic `PagedResponse<T>` in this project. Each feature declares its **own**
paged Response DTO in its `Responses/` folder, all following the same shape (see
`UserListResponse`, `AuditLogListResponse`, `PaginatedPlacesResponse`):
```csharp
// Responses/UserListResponse.cs
namespace YallaJo.Web.Areas.Admin.Models.Users;

public sealed class UserListResponse
{
    public IReadOnlyList<UserItemResponse> Items { get; init; } = [];
    public int  PageNumber      { get; init; }
    public int  PageSize        { get; init; }
    public int  TotalCount      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }
}
```
> Mirror these exact property names — the API returns them as camelCase JSON and the base
> client deserializes case-insensitively. (Note: there's **no `TotalPages`** on the wire;
> derive it only if a view actually needs it.)

### Step 1 — ApiClient: request the page, deserialize into the feature's list Response
```csharp
public Task<ApiResult<UserListResponse>> GetUsersAsync(
    int page, int pageSize, CancellationToken ct = default)
    => _api.GetAsync<UserListResponse>(
        $"/api/v1/security/users?page={page}&pageSize={pageSize}", ct);
```

### Step 2 — List ViewModel carries the paging state
Keep the API's `PagedResponse` out of the view; project the bits the pager needs:
```csharp
public sealed class UserListVm
{
    public IReadOnlyList<UserRowVm> Users { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext { get; init; }
}
```

### Step 3 — Facade: map the list Response → list VM
```csharp
public async Task<ApiResult<UserListVm>> GetUsersAsync(int page, int pageSize, CancellationToken ct = default)
{
    var result = await _users.GetUsersAsync(page, pageSize, ct);
    if (result.IsSuccess)
    {
        if (result.Data is null)
            return ApiResult<UserListVm>.CreateFailure("Could not load users.");

        var d = result.Data;
        return ApiResult<UserListVm>.CreateSuccess(new UserListVm
        {
            Users       = d.Items.Select(UsersMapper.ToRowVm).ToList(),
            Page        = d.PageNumber,
            PageSize    = d.PageSize,
            TotalCount  = d.TotalCount,
            HasPrevious = d.HasPreviousPage,
            HasNext     = d.HasNextPage,
        });
    }
    if (result.IsUnauthorized) return ApiResult<UserListVm>.ForceSignOut();
    return ApiResult<UserListVm>.CreateFailure(result.Error ?? "Could not load users.");
}
```

### Step 4 — Controller: accept `page` (and filters), pick a sane `pageSize`
```csharp
[HttpGet]
public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
{
    var result = await _facade.GetUsersAsync(page, pageSize: 20, ct);
    if (GuardSignOut(result) is { } signOut) return signOut;
    if (!result.IsSuccess)
    {
        ViewBag.Error = result.Error;
        return View(new UserListVm());
    }
    return View(result.Data);
}
```
**Two accepted `pageSize` styles in this codebase:**
1. **Server-fixed** (Users = `20`, AuditLogs = `50`): don't accept `pageSize` from the query at all.
2. **Caller-supplied but clamped** (Places): take it, then bound it. This is the safe way to let
   the UI choose a page size:
   ```csharp
   private const int MinPageSize = 1;
   private const int MaxPageSize = 50;

   [HttpGet]
   public async Task<IActionResult> Index(
       int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
   {
       if (page < 1) page = 1;
       pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);   // never trust raw pageSize
       var result = await _facade.GetPlacesAsync(page, pageSize, search, ct);
       // ...
   }
   ```
> **Never** pass an unclamped query `pageSize` straight through — a caller could request
> `pageSize=100000` and hammer the API.

### Step 5 — View: Bootstrap pager using the VM flags
```cshtml
<nav aria-label="Users pagination">
    <ul class="pagination">
        <li class="page-item @(Model.HasPrevious ? "" : "disabled")">
            <a class="page-link" asp-action="Index" asp-route-page="@(Model.Page - 1)">Previous</a>
        </li>
        <li class="page-item disabled">
            <span class="page-link">Page @Model.Page</span>
        </li>
        <li class="page-item @(Model.HasNext ? "" : "disabled")">
            <a class="page-link" asp-action="Index" asp-route-page="@(Model.Page + 1)">Next</a>
        </li>
    </ul>
</nav>
```

### Filtered + paged lists (e.g. AuditLogs)
When the list also has filters, **carry every filter through `asp-route-*`** so paging preserves
them, and echo the filters back on the VM so the filter bar re-renders:
```csharp
[HttpGet]
public async Task<IActionResult> Index(
    int page = 1, Guid? userId = null, string? action = null,
    DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
{
    var result = await _facade.GetLogsAsync(page, pageSize: 50, userId, action, from, to, ct);
    if (GuardSignOut(result) is { } r) return r;
    // ...return View(result.Data) — VM includes FilterUserId/FilterAction/... for the filter bar
}
```
```cshtml
@* keep filters on the pager links *@
<a class="page-link" asp-action="Index"
   asp-route-page="@(Model.Page + 1)"
   asp-route-userId="@Model.FilterUserId"
   asp-route-action="@Model.FilterAction">Next</a>
```

**Pagination rules of thumb**
- Every list endpoint that can grow is paged — no unbounded `GetAll`.
- 1-based `page`; default `page = 1`; server owns `pageSize`.
- Drive the pager **only** from the VM flags (`HasPrevious`/`HasNext`) — never compute
  `TotalCount / PageSize` math in the view.
- Preserve filters/sort across page links via `asp-route-*`.

---

## 14. Remote (server-side) validation

> **Status:** there is **no `[Remote]` usage in the codebase today.** This is the recommended,
> architecture-fitting way to add async "is this value available?" checks (e.g. unique language
> `Code`, unique email). It plays nicely with the existing jQuery-validation-unobtrusive setup
> already pulled in by `_ValidationScriptsPartial`.

Remote validation gives the **live, per-field** check as the user types. It does **not** replace
the server-side validation you already get from the API (the facade's `ApplyValidationErrors`
remains the authoritative guard on submit) — it's a UX nicety layered on top.

### Step 1 — A lightweight validation endpoint on the controller
Add a `[HttpGet]`/`[AcceptVerbs]` action that returns `Json(true)` / `Json("message")`. It calls
the facade just like any other read. Keep it cheap and cancellable.
```csharp
// On LanguagesController
[AcceptVerbs("GET", "POST")]
[RequirePermission(WebPermission.Language.Read)]
public async Task<IActionResult> IsCodeAvailable(string code, Guid? id, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(code))
        return Json(true);                       // let [Required] handle emptiness

    var result = await _facade.GetLanguagesAsync(activeOnly: false, ct);
    if (!result.IsSuccess || result.Data is null)
        return Json(true);                       // fail open — the API re-validates on submit

    var taken = result.Data.Languages.Any(l =>
        string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase) && l.Id != id);

    // jQuery-unobtrusive convention: true = valid, string = the error message.
    return Json(taken ? $"Language code '{code}' is already in use." : (object)true);
}
```
> The `id` parameter lets the **edit** form exclude the current record from the uniqueness check.

### Step 2 — Decorate the form ViewModel field with `[Remote]`
```csharp
using Microsoft.AspNetCore.Mvc;                     // RemoteAttribute lives here in MVC

public sealed class CreateLanguageVm
{
    [Required, StringLength(10)]
    [Remote(action: "IsCodeAvailable", controller: "Languages", areaName: "Admin",
            HttpMethod = "GET", ErrorMessage = "This language code is already in use.")]
    public string Code { get; set; } = string.Empty;
    // ...
}
```
For the **edit** VM, pass the current id so it's excluded:
```csharp
[Remote(action: "IsCodeAvailable", controller: "Languages", areaName: "Admin",
        HttpMethod = "GET", AdditionalFields = nameof(Id))]
public string Code { get; set; } = string.Empty;
```

### Step 3 — Ensure the view loads validation scripts
Already covered by the standard convention — just keep:
```cshtml
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```
The `asp-for`/`asp-validation-for` tag helpers emit the `data-val-remote-*` attributes; the
unobtrusive adapter does the AJAX call to your endpoint as the field blurs.

**Remote-validation rules of thumb**
- It's **UX only** — the API + `ApplyValidationErrors` are still the source of truth on submit.
- **Fail open** (return `true`) on transient errors so a flaky check never blocks a valid submit.
- Always pass `CancellationToken`; keep the endpoint behind the appropriate `[RequirePermission]`.
- Use `AdditionalFields = nameof(Id)` on edit forms to exclude the current record.
- Don't put expensive work here — it runs on every keystroke/blur. Prefer a cached lookup
  (the ContentCore lists are already output-cached under the `lookups` tag).

---

## 15. New-feature checklist (copy this into the PR)

- [ ] Files placed per the layered layout (see §1, same for **every** area — Auth, Accounts, Content, Admin):
  - `Controllers/{Name}Controller.cs` (`...Areas.{Area}.Controllers`), `Facades/{Name}Facade.cs` (`...Areas.{Area}.Facades`), `ApiClients/{Name}ApiClient.cs` (`...Areas.{Area}.ApiClients`), DTOs/VMs/Mapper under `Models/{Feature}/` (`...Areas.{Area}.Models.{Feature}`), views under `Views/{Controller}/`
  - Extra role folders where used: `Validators/` (`...Areas.{Area}.Validators`), `Helpers/` (`...Areas.{Area}.Helpers`), `Shared/` for area-shared partials
- [ ] DTOs: Requests + Responses (init setters, camelCase)
- [ ] ViewModels (list VMs `init`; form VMs `set` + DataAnnotations)
- [ ] `{Name}Mapper.cs` (static, Response→VM and VM→Request)
- [ ] `{Name}ApiClient : ` takes `IApiClient`, one-liners per endpoint, returns `ApiResult`/`ApiResult<T>`
- [ ] `{Name}Facade` maps results → VMs, handles `IsUnauthorized`/conflict/etc., evicts cache on writes (if cached)
- [ ] `{Name}Controller : BaseController` with `[Area]` + `[Authorize]` + `[RequirePermission]` (Auth self-service screens use `[AllowAnonymous]`/`[Authorize]` only)
- [ ] Reads `[HttpGet]`; writes `[HttpPost] + [ValidateAntiForgeryToken]` + stricter `[RequirePermission]`
- [ ] Every action: `async`, trailing `CancellationToken`, `GuardSignOut`, PRG on success
- [ ] Views with `@model` first line, tag helpers, `_ValidationScriptsPartial`; no inline alert markup
- [ ] Cross-tier `using`s present: controllers `using ...Areas.{Area}.Facades;`, facades `using ...Areas.{Area}.ApiClients;`, plus the relevant `...Models.{Feature}` imports
- [ ] New permission constants added to `WebPermission` (if new resource)
- [ ] Nav link added (permission-gated) if needed
- [ ] **List endpoints paged** via a per-feature list Response (`Items` + `PageNumber`/`PageSize`/`TotalCount`/`HasPreviousPage`/`HasNextPage`); `pageSize` server-fixed **or** `Math.Clamp`ed; pager driven by `HasPrevious`/`HasNext`; filters preserved on page links
- [ ] **Remote validation** added for uniqueness-style fields (optional): `[Remote]` on the VM + cheap, fail-open validation action; API still authoritative on submit
- [ ] `dotnet build` clean; manual smoke test passed

---

## Anti-patterns (do **not** do these)

- ❌ Inject `IApiClient`/`HttpClient` straight into a controller — always go through the Facade.
- ❌ Throw/catch for API errors — use the `ApiResult` flags.
- ❌ Re-implement `RedirectToLogin()` or the `RequireSignOut` block — use `BaseController`.
- ❌ Hand-write `<div class="alert">…TempData…</div>` in views — `_Alerts` already does it.
- ❌ `Html.Partial` / `Html.RenderPartial` — use `<partial>` / `await Html.PartialAsync` (deadlock risk).
- ❌ Manual `AddScoped<...>` in `Program.cs` — the scanner handles `*ApiClient` / `*Facade`.
- ❌ Block on async (`.Result` / `.Wait()`) — `await` all the way, thread the `CancellationToken`.
- ❌ Direct `User.HasClaim(...)` in views/controllers — use `ICurrentUser` / `<permission>` / `[RequirePermission]`.
- ❌ Returning unbounded lists — page anything that grows; never compute pager math in the view.
- ❌ Treating `[Remote]` as real validation — it's UX only; the API + `ApplyValidationErrors` decide on submit.

---

### Related guides
- `WEB_LAYER_GUIDE.md` — web-layer performance & DRY decisions (output caching, static assets, DI scan, BaseController rationale).
- `PERFORMANCE_OPTIMIZATION_GUIDE.md` — backend/API performance tiers.
