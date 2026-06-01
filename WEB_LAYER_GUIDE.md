# YallaJo.Web — Web Layer Performance & DRY Guide

> **Scope:** `src/Hosts/YallaJo.Web` — the Razor **MVC** admin/auth UI acting as a **BFF** (Backend-for-Frontend) that consumes `YallaJo.Api`.
> **Target framework:** `net9.0` (Nullable + ImplicitUsings enabled).
> **Goal:** Make the web layer fast *and* keep it DRY — building on the architecture that already exists rather than reinventing it.
> **Sources:** Microsoft Learn (ASP.NET Core 9/10 — caching, views, static assets, best-practices, updated 2025-12), Duende "Need for Speed" (2025-08), Syncfusion/c-sharpcorner perf tuning (2025).

---

## 0. Current State (Baseline) — *what already exists*

`YallaJo.Web` is **not** a default template. It is a mature **feature-folder (vertical-slice)** BFF. Before recommending anything, here is what is already in place and **should not be rebuilt**:

### Architecture already established
- **Feature-folder views** via custom Razor view-location expanders (registered in `Program.cs` → `AddRazorOptions`):
  - Non-admin: `~/Areas/{area}/Features/{controller}/Views/{view}.cshtml` (+ `/Views/Shared/`)
  - Area-shared: `~/Areas/{area}/Shared/{view}.cshtml` (e.g. `Areas/Auth/Shared/_RecaptchaField.cshtml`)
  - Admin: `~/Areas/Admin/Modules/{module}/Features/{controller}/Views/{view}.cshtml`, auto-discovered by `AdminModuleViewLocationExpander` (filesystem scan at startup — no manual list).
- **Areas:** `Auth`, `Accounts`, `Admin/Modules/{Security, Accounts, ContentCore, ContentPlaces}`.
- **DRY backbone already present:** every feature is an `XxxApiClient` + `XxxFacade` pair. Base typed client is `Services/ApiClient.cs`, abstracted behind **`IApiClient`** (`Services/IApiClient.cs`) for testability.
- **`BaseController`** (`Infrastructure/Mvc/BaseController.cs`): all controllers inherit it to remove per-action boilerplate (login redirect, 401 guard, TempData flash, validation→ModelState). See **§0.1**.
- **Services/**: `ApiClient.cs` (base typed client, `: IApiClient`), `IApiClient.cs` (consumption seam), `JwtAuthHandler.cs`, `ApiAssetUrlResolver.cs` (+interface) for converting API-relative upload paths to absolute browser URLs.
- **Infrastructure/** (26 files): `Api/Contracts/ApiResult.cs`; external auth (Google/Facebook) via `AddYallaJoExternalAuth`; `WebSignInService`; `Authorization/` (`ForbiddenResultFilter` — HTML→AccessDenied view, AJAX/JSON→ProblemDetails; `RequirePermissionAttribute`; `WebPermission`); `Identity/CurrentUser` (scoped per-request claim cache — all permission/role checks route through it); `Mvc/AdminModuleViewLocationExpander`; `Security/Recaptcha/` (reCAPTCHA v3); `TagHelpers/PermissionTagHelper`.
- **Views/**: `_ViewImports.cshtml`, `_ViewStart.cshtml`, `Shared/{AccessDenied,_Layout,_Navbar,_ValidationScriptsPartial}.cshtml`.

### Performance/resilience already done in `Program.cs`
- ✅ **Cookie-based BFF auth** (HttpOnly, `SameSite=Lax`, `SecurePolicy=Always`, 8h sliding, `LoginPath=/auth/login`, cookie `YallaJo.Web`).
- ✅ **Typed client** `AddHttpClient<ApiClient>` with `BaseAddress` from config key `ApiBaseUrl`.
- ✅ **Connection pooling**: `ConfigurePrimaryHttpMessageHandler(SocketsHttpHandler { PooledConnectionLifetime = 2min, AutomaticDecompression = All })`.
- ✅ **`JwtAuthHandler` registered OUTERMOST** (`AddHttpMessageHandler` before resilience) so the bearer token attaches before retries.
- ✅ **`AddStandardResilienceHandler()`** (retry + circuit breaker + total 30s / per-attempt 10s). `HttpClient.Timeout` intentionally left default.
- ✅ Named **`"anon"`** client for `/refresh` (avoids `JwtAuthHandler` recursion).
- ✅ **`app.MapStaticAssets()`** already replaces `UseStaticFiles()`.
- ✅ Global **`ForbiddenResultFilter`** registered in `AddControllersWithViews`.
- ✅ Correct **middleware order**: `UseExceptionHandler("/error")`+`UseHsts` (prod) → `UseHttpsRedirection` → `UseAuthentication` → `UseAuthorization` → `MapStaticAssets` → route mappings.

> **Bottom line:** The big wins (typed client, pooling, resilience, JWT handler ordering, static-asset optimization, feature-folder DRY, permission tag helper) are **already implemented**. This guide focuses only on the **genuine gaps**.

---

## PART A — Genuine PERFORMANCE gaps

### A1. Chain `.WithStaticAssets()` onto the route mappings  *(✅ DONE)*

`MapStaticAssets()` was already called, but the route mappings did **not** chain `.WithStaticAssets()`. Chaining it lets the `script`/`link`/`img` tag helpers resolve **fingerprinted** filenames (content-hashed, immutable-cached) inside MVC views. **Implemented:** all three `MapControllerRoute(...)` calls (`areas`, `root`, `default`) now chain `.WithStaticAssets()`, and `<ImportMap />` was added to the `<head>` of `Views/Shared/_Layout.cshtml`. LSP-verified clean. (Layout assets are currently CDN-based, so the immediate effect is small — the benefit applies to any local `wwwroot` assets referenced via tag helpers going forward.)

```csharp
app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
   .WithStaticAssets();   // ← add to each MapControllerRoute

app.MapControllerRoute(name: "root",    pattern: "", defaults: new { area = "Auth", controller = "Login", action = "Index" })
   .WithStaticAssets();

app.MapControllerRoute(name: "default", pattern: "{area=Auth}/{controller=Login}/{action=Index}/{id?}")
   .WithStaticAssets();
```

- Then reference assets normally; tag helpers emit fingerprinted URLs automatically.
- For JS modules, add `<ImportMap />` to the `<head>` of `Views/Shared/_Layout.cshtml`.
- `MapStaticAssets` does **not** minify — keep minification to a bundler (see A5).

### A2. Output Caching — *(✅ DONE — registration + eviction wired)*

**Implemented:** `Program.cs` now calls `AddOutputCache(...)` (opt-in `NoCache` base policy + `Lookups` policy = 10-min expiry tagged `"lookups"`, + `PublicList` policy = 30s expiry varied by `page`/`pageSize`) and `app.UseOutputCache()` (placed right after `UseAuthentication`/`UseAuthorization`, before static assets + routing). The four ContentCore lookup facades (`Categories`, `Languages`, `Tags`, `Specializations`) inject `IOutputCacheStore` and call `await _cache.EvictByTagAsync("lookups", ct)` after every successful `Create`/`Update`/`Delete`.

> **Effective scope caveat:** the ContentCore list pages are **authenticated admin GET** pages, which output caching **bypasses by default** (cookie/auth responses aren't cached). So no read action is decorated with `[OutputCache(PolicyName="Lookups")]` yet — doing so would be a no-op without a custom policy that overrides the auth bypass. The policies + eviction are in place and become effective the moment a genuinely **anonymous/public** read endpoint (a public lookups API, or `Auth` area pages) opts into `Lookups`/`PublicList`. The eviction calls are correct and harmless in the meantime.

Original analysis below for reference:



There is **no** `AddOutputCache`/`UseOutputCache`. For a UI BFF, **output caching** (server-controlled) is the right tool — response caching is defeated because browsers send `Cache-Control: max-age=0`.

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.NoCache());           // opt-in only
    options.AddPolicy("Lookups", b => b.Expire(TimeSpan.FromMinutes(10)).Tag("lookups"));
    options.AddPolicy("PublicList", b => b.Expire(TimeSpan.FromSeconds(30)).SetVaryByQuery("page", "pageSize"));
});
```
```csharp
app.UseOutputCache();   // AFTER UseRouting/route setup; AFTER UseCors if present
```

**Critical for this BFF:** output caching by default does **NOT** cache non-200, non-GET/HEAD, cookie-setting, or **authenticated** responses. Almost every admin page here is authenticated, so the ROI is narrow and specific:
- **Best targets:** the `Auth` area's anonymous pages and **shared lookup data** served to the browser (Languages, Tags, Categories, Specializations) — exactly the `ContentCore` module's read endpoints.
- Built-in **resource locking** prevents stampede.
- Multi-node: `AddStackExchangeRedisOutputCache(...)` (not `IDistributedCache` — no atomic tagging).
- Invalidate on mutation: inject `IOutputCacheStore`, call `await store.EvictByTagAsync("lookups", ct)` inside the relevant `XxxFacade` write methods.

### A3. Fragment caching — `<cache>` Tag Helper  *(✅ DONE — antiforgery-safe split)*

**Implemented in `_Navbar.cshtml`:** only the **static nav-link list** (Users/Roles/Languages — identical for every authenticated user in this app) is wrapped in `<cache expires-after="@TimeSpan.FromMinutes(30)">`. The per-user profile link (`@CurrentUser.UserId`) and the logout `<form method="post">` are deliberately left **OUTSIDE** the cache.

> **⚠️ Critical antiforgery caveat (why the whole navbar is NOT cached):** the logout form emits a hidden `__RequestVerificationToken` (the logout POST is `[ValidateAntiForgeryToken]`). Caching that fragment would serve **one user's token to other users/sessions**, breaking logout with antiforgery failures. Therefore: **never** wrap an antiforgery-bearing form in `<cache>`. Likewise, `vary-by-user` was NOT used here because the cached portion is intentionally user-agnostic (static links); if you later add permission-driven links, either add `vary-by-user="true"` OR keep permission-conditional markup outside the cache. The `PermissionTagHelper` checks are cheap (`CurrentUser` claim cache), so caching them buys little versus the correctness risk.

Original guidance below for reference (the `vary-by-user` example applies to permission-driven fragments that contain **no** antiforgery token):


```cshtml
@* per-user nav, cached 30 min *@
<cache expires-after="@TimeSpan.FromMinutes(30)" vary-by-user="true">
    @await Html.PartialAsync("_Navbar")
</cache>

@* lookup-bound fragment, varied by route + query *@
<cache expires-sliding="@TimeSpan.FromMinutes(5)" vary-by-route="page" vary-by-query="filter">
    @await Component.InvokeAsync("CategoryFilter")
</cache>
```

- Supports `vary-by`, `vary-by-route`, `vary-by-query`, `vary-by-header`, `vary-by-cookie`, `vary-by-user`, `vary-by-culture`. Default expiry 20 min; backed by `IMemoryCache`.
- Web-farm: `<distributed-cache>` (Redis/SQL/NCache).
- **Caution:** since the nav is permission-sensitive, always key it with `vary-by-user` (or a permission-hash) so users never see another user's menu. Evict by reducing expiry if roles change frequently.

### A4. Async & `HttpContext` discipline  *(✅ AUDITED & FIXED)*

**Audit result (this iteration):** two real blocking `.Result` usages were found and have now been **refactored to plain `await`**:
- ✅ **`Areas/Admin/Modules/Security/Features/Users/UsersFacade.cs` (`GetDetailsAsync`)** — was `await Task.WhenAll(userTask, rolesTask)` then `userTask.Result` / `rolesTask.Result`. Now awaits both into locals (`var userResult = await userTask; var rolesResult = await rolesTask;`) after `WhenAll`, eliminating every `.Result` read and `AggregateException` wrapping.
- ✅ **`Areas/Admin/Modules/Security/Features/Roles/RolesFacade.cs` (`CreateAsync`)** — was `.ContinueWith(t => ToVoidResult(t.Result), ct, ...)`. Now `=> ToVoidResult(await _api.CreateRoleAsync(...))` — plain `await`, no `ContinueWith`, no scheduler footgun.

Both were **pre-existing** (not introduced this iteration). Confirmed-correct: `CurrentUser` (`Infrastructure/Identity/CurrentUser.cs`) stores the **`IHttpContextAccessor`** (not `HttpContext`) and reads `HttpContext?.User` lazily with a null-check — the thread-safe pattern; `CancellationToken` is threaded through `ApiClient` + facades. Keep these invariants when adding features:

The codebase already uses `AsyncAwaitBestPractices`. Verify across facades/clients:
- Every action and the full call stack is `async Task<IActionResult>` with `CancellationToken` threaded into the `ApiClient`/facade. No `.Result` / `.Wait()` / sync-over-async.
- Lists return `await ...ToListAsync()` / paginated DTOs — never raw lazily-iterated `IEnumerable<T>`; **add pagination to every list endpoint**.
- `HttpContext` is not thread-safe and invalid after the request ends. `CurrentUser` already wraps `IHttpContextAccessor` correctly — keep that the single access point; never store `HttpContext` in a field. For fire-and-forget use `IServiceScopeFactory.CreateAsyncScope()`; never `async void`.
- Don't modify status/headers after the body starts (`Response.HasStarted` / `Response.OnStarting`).

### A5. Client assets (Duende 2025 guidance)  *(◑ PARTIALLY DONE)*

**Implemented this iteration:** `_Layout.cshtml` now serves **bootstrap-icons CSS** and the **bootstrap JS bundle** from the repo's local `wwwroot/assets/vendor/` library (`<link href="~/assets/vendor/bootstrap-icons/bootstrap-icons.css">`, `<script src="~/assets/vendor/bootstrap/dist/js/bootstrap.bundle.min.js">`). The Link/Script tag helpers (registered in `_ViewImports`) run with the already-chained `.WithStaticAssets()` (A1), so these now get **SHA-256 fingerprinting + immutable far-future caching + build-time gzip/Brotli** automatically. Icon-font `.woff/.woff2` resolve via the CSS's relative `./fonts/` path. Bootstrap **CSS stays on the jsDelivr CDN** — there is no local `bootstrap.min.css` in `wwwroot` (only the JS bundle ships locally), so a local `~/` reference would 404.

**Remaining (optional):**
- Add a local `bootstrap.min.css` and switch the CDN `<link>` to `~/...` (or keep CDN via `asp-fallback-href`) so the CSS is fingerprinted/compressed too.
- The repo ships a full vendor library (apexcharts, choices, dropzone, flatpickr, quill, splide, tiny-slider, font-awesome, AOS, glightbox, nouislider, …) + theme `assets/css/style.css` — wire these **per-page** (`@section Scripts` / page `<link>`s) only where used; don't load all globally.
- HTTP/2 multiplexing: prefer **grouping by change-frequency** over giant bundles; serve versioned 3rd-party libs separately.
- Images: WebP/AVIF, explicit `width`/`height` (avoid reflow), `loading="lazy"` below the fold; `<link rel="preload">` for critical assets (href must match exactly, incl. version query).
- Replace icon fonts with **SVG spritesheets**; **variable / self-hosted fonts** to avoid FOUT.
- Environment-split raw vs. min/bundled with the Environment Tag Helper + `asp-append-version="true"`.

---

## PART B — Genuine DRY gaps

> The `XxxApiClient` + `XxxFacade` + base `ApiClient` + `PermissionTagHelper` + view-location expanders already eliminate most repetition. Only two real smells remain.

### B1. Collapse the ~40 repeated DI registrations  *(✅ DONE — the #1 DRY smell)*

**Previously** `Program.cs` contained ~40 hand-written pairs:
```csharp
builder.Services.AddScoped<UsersApiClient>();
builder.Services.AddScoped<UsersFacade>();
builder.Services.AddScoped<RolesApiClient>();
builder.Services.AddScoped<RolesFacade>();
// ...repeated ~20 times
```

**Implemented:** a **zero-dependency reflection scanner** (no Scrutor — it isn't referenced in the csproj). New file `Infrastructure/DependencyInjection/FeatureServiceRegistration.cs`:
```csharp
public static class FeatureServiceRegistration
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection services)
    {
        var featureTypes = typeof(FeatureServiceRegistration).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t != typeof(ApiClient))   // base typed client stays on AddHttpClient<ApiClient>
            .Where(t => t.Name.EndsWith("ApiClient", StringComparison.Ordinal)
                        || t.Name.EndsWith("Facade", StringComparison.Ordinal));

        foreach (var type in featureTypes)
            services.AddScoped(type);

        return services;
    }
}
```
`Program.cs` now collapses all ~40 pairs to a single call:
```csharp
builder.Services.AddFeatureServices();
```
New features register **automatically** by naming convention (`*ApiClient` / `*Facade`) — no `Program.cs` edit per feature. Program.cs dropped from ~296 → 219 lines. **Build: 0 warnings, 0 errors.**

> **Note:** the primary `ApiClient` is intentionally excluded from the scan — it must keep its `AddHttpClient<ApiClient>(...)` registration (typed `HttpClient` + `SocketsHttpHandler` pooling + `JwtAuthHandler` + resilience) and is exposed through `IApiClient`.
>
> **Alternative considered:** Scrutor (`services.Scan(scan => scan.FromAssemblyOf<ApiClient>().AddClasses(c => c.Where(t => t.Name.EndsWith("ApiClient") || t.Name.EndsWith("Facade"))).AsSelf().WithScopedLifetime())`) is more concise, but was rejected to avoid adding a NuGet dependency for a one-method need.

### B2. Shared `_Alerts` partial for TempData messaging  *(✅ DONE)*

**Implemented:** created `Views/Shared/_Alerts.cshtml` (Bootstrap 5 dismissible banners for TempData `Success`/`Error`/`Warning`/`Info` — matching the keys `BaseController.SetSuccess`/`SetError` write), included once in `_Layout.cshtml` just before `@RenderBody()`. Removed the duplicated inline `@if (TempData[...] is string)` alert blocks from `Login/Index.cshtml`, `Profile/Index.cshtml`, and `Users/Index.cshtml`. New feature views get flash banners for free.

Original guidance:


If success/error banners are repeated across feature views, centralize once in `_Layout.cshtml`:
```cshtml
@* Views/Shared/_Alerts.cshtml *@
@if (TempData["Success"] is string s) { <div class="alert alert-success">@s</div> }
@if (TempData["Error"]   is string e) { <div class="alert alert-danger">@e</div> }
@if (TempData["Warning"] is string w) { <div class="alert alert-warning">@w</div> }
```
```cshtml
@* _Layout.cshtml *@
<partial name="_Alerts" />
@RenderBody()
```

### B3. View DRY building blocks already available — use them consistently

| Technique | Status in repo | Use it for |
|---|---|---|
| `_Layout` / `_ViewImports` / `_ViewStart` | ✅ present | shared chrome, `@addTagHelper`, default `Layout` |
| Area-shared partials (`_RecaptchaField`) | ✅ present | reusable area markup |
| `PermissionTagHelper` | ✅ present | show/hide UI by permission — prefer over inline checks |
| **View Components** | (consider) | server-driven, DI-backed fragments (nav, current-user panel, dashboard widgets) — pair with `<cache>` (A3) |
| **Display/Editor Templates** | (consider) | a model type rendered identically across pages (`Address`, `Money`, `StatusBadge`) → `@Html.DisplayFor`/`EditorFor`, change once |
| **Custom Tag Helpers** | (consider) | repeated parameterized markup (pager, alert, button) |

Partials: always `<partial name="_X" model="..."/>` or `Html.PartialAsync` — **never** `Html.Partial`/`RenderPartial` (deadlock risk).

---

## PART C — Razor compilation & build  *(✅ DONE)*

- `.cshtml` compiles at **build + publish** by the Razor SDK by default — kept.
- **Implemented:** the `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` package reference is now **`Condition="'$(Configuration)' == 'Debug'"`** (Debug-only) in the csproj, so it never ships to production. The `mvcBuilder.AddRazorRuntimeCompilation()` call in `Program.cs` is wrapped in `#if DEBUG ... #endif` (still also gated by `IsDevelopment()`), so **Release builds compile without the package**. Runtime compilation is obsoleted in .NET 10 — this keeps it strictly a dev convenience.
- **Implemented:** removed the two **stale** empty `<Folder Include>` placeholders that now contain real files (`Infrastructure\DependencyInjection\` → `FeatureServiceRegistration.cs`; `Infrastructure\Authentication\SignIn\` → `IWebSignInService.cs`/`WebSignInService.cs`). The remaining `<Folder Include>` entries point at genuinely-empty scaffolding folders and were left intentionally.

---

## Implementation Order (highest ROI first)

1. ~~**DI DRY (B1)** — replace ~40 `AddScoped` pairs with a single scan block.~~ ✅ **DONE** *(biggest cleanup, low risk)*
2. ~~**`.WithStaticAssets()` (A1)** — chain onto each `MapControllerRoute`; add `<ImportMap/>` to `_Layout`.~~ ✅ **DONE**
3. ~~**Output caching (A2)** — `AddOutputCache`/`UseOutputCache`; `EvictByTagAsync` in the matching facades on writes.~~ ✅ **DONE** (decorate a real anonymous read endpoint with `[OutputCache("Lookups")]` when one exists).
4. ~~**Fragment caching (A3)** — `<cache>` around the static nav list (antiforgery-safe).~~ ✅ **DONE**
5. ~~**`_Alerts` partial (B2)** — centralize TempData banners.~~ ✅ **DONE**
6. ~~**Runtime-compilation cleanup (C)** — package dev-only; tidy stale csproj folders.~~ ✅ **DONE**
7. ~~**Audit async (A4)**~~ ✅ **DONE** — `UsersFacade`/`RolesFacade` `.Result` removed. **Client assets (A5)** + **pagination on list endpoints** — *remaining.*
8. **Measure** — `dotnet-counters`, `dotnet-trace`, OpenTelemetry; verify cache hit rates and p95 latency.

---

## Quick Checklist

- [x] ~40 `AddScoped<XxxApiClient/Facade>` → single reflection scan (`AddFeatureServices()`, keeps `AddHttpClient<ApiClient>`)
- [x] `.WithStaticAssets()` chained on every `MapControllerRoute`; `<ImportMap/>` in `_Layout`
- [x] `AddOutputCache` + `UseOutputCache`; `Lookups`/`PublicList` policies; `EvictByTagAsync("lookups")` in ContentCore facades on mutations
- [x] `<cache>` around the static `_Navbar` list (antiforgery-safe split; permission/logout left uncached)
- [x] Shared `_Alerts` partial in `_Layout`; inline alert blocks removed from Login/Profile/Users views
- [x] `RuntimeCompilation` package made dev-only (`Condition` Debug + `#if DEBUG`); stale empty csproj folders removed
- [x] **A4 findings fixed:** `UsersFacade.GetDetailsAsync` and `RolesFacade.CreateAsync` refactored to plain `await` (no `.Result`/`ContinueWith`) — *pagination on list endpoints still recommended (A5/A4)*
- [x] `CurrentUser` is the single `HttpContext` access point (stores `IHttpContextAccessor`, lazy null-checked); no `HttpContext` in fields
- [◑] **A5 (partial):** bootstrap-icons CSS + bootstrap JS bundle now served locally from `wwwroot/assets/vendor/` (fingerprinted + immutable via `.WithStaticAssets()`); bootstrap CSS still CDN (no local copy). Remaining: local bootstrap CSS, per-page vendor wiring, WebP/AVIF + lazy images, env-split bundling
- [ ] Telemetry wired (OpenTelemetry / dotnet-counters); cache hit-rate + p95 verified *(measurement phase)*

---

### What was intentionally NOT recommended (already done)
Typed `HttpClient` + `BaseAddress` from config · `SocketsHttpHandler` pooling + `AutomaticDecompression` · `AddStandardResilienceHandler` (retry/CB/timeouts) · `JwtAuthHandler` outermost · `"anon"` refresh client · cookie BFF auth · `MapStaticAssets()` · `ForbiddenResultFilter` · `PermissionTagHelper` · feature-folder view-location expanders · `ApiClient`+`Facade` DRY pattern · `CurrentUser` claim cache · **`IApiClient` consumption seam (all 26 feature clients wired to it)** · **`BaseController` (login redirect / 401 guard / TempData flash / validation→ModelState)**.

### Already-implemented this iteration (see §0.1)
- ✅ `IApiClient` extracted; `ApiClient : IApiClient`; `AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>())`; **26 feature ApiClients** now depend on `IApiClient`. Build clean.
- ✅ `BaseController` added; `UsersController` + `ProfileController` converted as reference implementations. Build clean.
- ✅ **B1 done**: ~40 `AddScoped<XxxApiClient/Facade>` pairs collapsed into a single `builder.Services.AddFeatureServices();` (zero-dependency reflection scanner in `Infrastructure/DependencyInjection/FeatureServiceRegistration.cs`; excludes base `ApiClient`). `Program.cs` ~296 → 219 lines. Build clean.
