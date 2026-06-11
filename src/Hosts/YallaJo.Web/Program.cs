using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.DependencyInjection;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Security.Recaptcha;
using YallaJo.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication (cookie — MVC frontend, BFF pattern) ──────────────────────
var authBuilder = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/auth/sign-in";
        options.LogoutPath       = "/auth/sign-out";
        options.AccessDeniedPath = "/auth/sign-in";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Name       = "YallaJo.Web";
    });

// ── Server-side ticket store (Phase 2) ───────────────────────────────────────
// The full authentication ticket (incl. the JWT access token + refresh token) is
// stored server-side in IMemoryCache; the browser cookie carries only a small
// opaque session key. This keeps the admin cookie to a single chunk and well under
// Kestrel's request-header limit. The store is attached to the MAIN application
// cookie only, via IPostConfigureOptions, so external-auth/correlation cookies are
// unaffected.
// NOTE: IMemoryCache is single-instance; sessions are lost on app restart. For
// multi-instance/load-balanced hosting, swap MemoryCacheTicketStore for an
// IDistributedCache (e.g. Redis) implementation.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<YallaJo.Web.Infrastructure.Authentication.Ticket.MemoryCacheTicketStore>();
builder.Services.AddSingleton<
    Microsoft.Extensions.Options.IPostConfigureOptions<CookieAuthenticationOptions>,
    YallaJo.Web.Infrastructure.Authentication.Ticket.ConfigureCookieTicketStore>();

// External-provider infrastructure: intermediate cookie + Google/Facebook
// handlers (registered only when configured) + signed-ticket builder.
builder.Services.AddYallaJoExternalAuth(builder.Configuration, authBuilder);

// reCAPTCHA v3 client-side integration. Views pull IRecaptchaScriptService to
// render the script + hidden field centrally (see _RecaptchaField partial).
builder.Services.Configure<RecaptchaOptions>(
    builder.Configuration.GetSection(RecaptchaOptions.SectionName));
builder.Services.AddSingleton<IRecaptchaScriptService, RecaptchaScriptService>();

builder.Services.AddAuthorization();

// ── HttpClient → API (BFF pattern) ───────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtAuthHandler>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

// Primary typed client — goes through JwtAuthHandler to attach Bearer tokens.
//
// Handler pipeline (outermost → innermost):
//   JwtAuthHandler → StandardResilience → SocketsHttpHandler (primary) → network
//
// • SocketsHttpHandler recycles pooled connections every 2 min so the BFF
//   picks up API DNS / load-balancer changes without a restart, and requests
//   negotiate transparent response decompression (gzip/brotli/deflate).
// • JwtAuthHandler stays OUTERMOST so the Bearer token is attached once, before
//   any resilience retry re-sends the (already-authorized) request.
// • AddStandardResilienceHandler adds retry + circuit-breaker + total(30s)/
//   attempt(10s) timeouts. We intentionally leave HttpClient.Timeout at its
//   default so it never fights the resilience pipeline's own timeouts.
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    AutomaticDecompression   = DecompressionMethods.All,
})
.AddHttpMessageHandler<JwtAuthHandler>()
.AddStandardResilienceHandler();

// Expose the configured typed client through IApiClient so feature-level
// API clients depend on a mockable seam (unit-testable) rather than the
// concrete ApiClient. Resolves the SAME resilience-configured instance —
// no second HttpClient is created.
builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());

// Anonymous client used by JwtAuthHandler for the /refresh endpoint
// (must not go through JwtAuthHandler — that would cause infinite recursion).
builder.Services.AddHttpClient("anon", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ── Auth infrastructure ───────────────────────────────────────────────────────
builder.Services.AddScoped<IWebSignInService, WebSignInService>();

// Pending e-mail-verification state: Data-Protection-encrypted, time-limited cookie
// carrying the registered e-mail between SignUp and the OTP (TwoFactor) screen —
// replaces the old ?email= query-string / hidden-field round-trip (PII + arbitrary-
// target OTP guessing). The OTP itself is never stored here.
builder.Services.AddScoped<
    YallaJo.Web.Infrastructure.Authentication.SignIn.IPendingVerificationStore,
    YallaJo.Web.Infrastructure.Authentication.SignIn.PendingVerificationStore>();

// API-hosted asset URL resolver — turns API-relative paths like
// "/uploads/avatars/<guid>.png" into absolute URLs the browser can fetch
// from the API origin. Used for avatar rendering in the web layer.
builder.Services.AddSingleton<IApiAssetUrlResolver, ApiAssetUrlResolver>();

// ── Identity / authorization infrastructure ───────────────────────────────────
// ICurrentUser: scoped, lazy per-request claim cache.
// Every permission and role check in the project goes through this interface.
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ── Feature services (ApiClients + Facades) ──────────────────────────────────
// Convention-based registration: every concrete class whose name ends in
// "ApiClient" or "Facade" is registered as scoped self. This replaces the ~40
// hand-written AddScoped<XxxApiClient>()/AddScoped<XxxFacade>() pairs.
// The base ApiClient is excluded (registered via AddHttpClient<ApiClient> above
// and exposed through IApiClient so it keeps its resilience pipeline + JWT handler).
builder.Services.AddFeatureServices();

// GuideIdAccessor: scoped per-request memoization of GET /guides/me (guideId +
// sidebar identity) — not covered by the ApiClient/Facade suffix convention.
builder.Services.AddScoped<YallaJo.Web.Areas.Guide.Services.GuideIdAccessor>();

// ── Output caching ────────────────────────────────────────────────────────────
// Server-side output caching (NOT response caching — browsers send
// Cache-Control: max-age=0 which defeats that). By default output caching does
// NOT cache authenticated/cookie-setting/non-GET responses, so the ROI here is
// deliberately narrow: anonymous Auth pages + shared read-only lookup data
// (Languages/Tags/Categories/Specializations from the ContentCore module).
// Mutations evict by tag via IOutputCacheStore.EvictByTagAsync("lookups", ct).
builder.Services.AddOutputCache(options =>
{
    // Opt-in only: nothing is cached unless an endpoint/policy says so.
    options.AddBasePolicy(b => b.NoCache());

    // Shared read-only reference data — safe to cache briefly, tagged for eviction.
    options.AddPolicy("Lookups", b => b
        .Expire(TimeSpan.FromMinutes(10))
        .Tag("lookups"));

    // Anonymous public lists — short TTL, varied by paging query.
    options.AddPolicy("PublicList", b => b
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("page", "pageSize"));

    // ── Plan caching policies (UI-UX §5 / UI-PERF-C1) ────────────────────────
    // Public, anonymous-safe pages. VaryByQuery("*") so parameterised listings
    // (page/sort/filters) cache per distinct query while static pages (home)
    // keep a single entry. Per-entity tags (homepage, tour:{id}, place:{id},
    // business:{id}, blog:{id}, category:tree) are attached per-action via
    // [OutputCache(Tags = ...)] and evicted with IOutputCacheStore.EvictByTagAsync.
    options.AddPolicy("PublicShort",  b => b.Expire(TimeSpan.FromMinutes(5)).SetVaryByQuery("*"));
    options.AddPolicy("PublicMedium", b => b.Expire(TimeSpan.FromMinutes(30)).SetVaryByQuery("*"));
    options.AddPolicy("PublicLong",   b => b.Expire(TimeSpan.FromHours(1)).SetVaryByQuery("*"));
    options.AddPolicy("PublicDay",    b => b.Expire(TimeSpan.FromHours(24)).SetVaryByQuery("*"));

    // Authenticated pages: explicit no-store marker (base policy is already
    // NoCache; naming it documents intent at the action site).
    options.AddPolicy("NoStore", b => b.NoCache());
});

// ── Localization (UI-UX §2: en + ar, DEFAULT ar / RTL — Jordan-first) ─────────
// Provider order is the framework default: QueryString → Cookie → Accept-Language,
// matching the plan. The cookie provider lets the language switcher persist a
// choice. Resources live under /Resources (e.g. Resources/SharedResource.ar.resx).
//
// IMPORTANT: ResourcesPath is left EMPTY on purpose. The marker type is
// YallaJo.Web.Resources.SharedResource — its namespace ALREADY encodes the
// "Resources" folder. ASP.NET Core builds the resource base name by prepending
// ResourcesPath to the type's namespace-relative path, so a non-empty
// ResourcesPath="Resources" would resolve to Resources/Resources/SharedResource.*.resx
// (a path that does not exist) and EVERY key would fall back to its own name.
builder.Services.AddLocalization();

string[] supportedCultures = ["ar", "en"];
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("ar");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
    options.ApplyCurrentCultureToResponseHeaders = true;
});

// ── MVC + custom Razor view locations ────────────────────────────────────────
var mvcBuilder  = builder.Services.AddControllersWithViews(options =>
{
    // ForbiddenResultFilter: the ONE code path that renders AccessDenied.cshtml.
    // Branches on content negotiation: HTML page → view, AJAX/JSON → ProblemDetails.
    options.Filters.Add<ForbiddenResultFilter>();

    options.Filters.Add<YallaJo.Web.Infrastructure.Mvc.AdminNoStoreCacheFilter>();
})
    .AddRazorOptions(o =>
    {
        // Auth / Accounts / Content areas follow the flat feature-folder convention:
        //   Areas/{area}/Features/{controller}/Views/{view}.cshtml
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/{0}.cshtml");
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/Shared/{0}.cshtml");

        // The Auth area follows the classic layer-by-type convention instead of
        // feature folders:
        //   Areas/{area}/Views/{controller}/{view}.cshtml
        // This entry is additive — areas that still use the feature-folder formats
        // above (Accounts, Content, Admin) are unaffected.
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Views/{1}/{0}.cshtml");
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Views/Shared/{0}.cshtml");

        // Area-level shared partials live at ~/Areas/{area}/Shared/{view}.cshtml
        // (e.g. Areas/Auth/Shared/_RecaptchaField.cshtml — used by every auth form).
        // Without this entry, <partial name="_RecaptchaField" /> would not resolve
        // because the feature-folder formats above only look inside a specific
        // controller's folder.
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Shared/{0}.cshtml");

    })
    .AddViewLocalization()
    // DataAnnotations messages resolve against the single SharedResource (CON1): VM
    // attributes carry resx KEYS (e.g. ErrorMessage = "Auth.Validation.EmailRequired")
    // and this provider looks them up in Resources/SharedResource.{culture}.resx.
    // Unknown keys fall back to the literal string, so legacy English messages keep
    // rendering unchanged until they are migrated to keys.
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(YallaJo.Web.Resources.SharedResource)));

#if DEBUG
// Razor runtime compilation watches the filesystem and recompiles views on
// change. The package is referenced for Debug builds only (see csproj), so this
// call is compiled out of Release builds — guarding it with #if DEBUG keeps
// Release compiling without the (obsoleted-in-.NET-10) package.
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
#endif

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();

// GAP-7 (rule SEC2): baseline hardening headers (CSP, X-Frame-Options,
// X-Content-Type-Options, Referrer-Policy, Permissions-Policy, X-Correlation-ID)
// on every response. Registered early so it covers static assets, error pages,
// and every MVC response including the §9 RBAC console.
app.UseMiddleware<YallaJo.Web.Infrastructure.Middleware.SecurityHeadersMiddleware>();

// Apply request culture (query string → cookie → Accept-Language) before the
// MVC pipeline renders any view, so dir/lang and localized strings are correct.
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

// Output caching must run after routing/auth so policies can vary correctly and
// authenticated responses are excluded by default. Built-in resource locking
// prevents cache stampede on concurrent misses.
app.UseOutputCache();

// ── Static assets (.NET 9) ────────────────────────────────────────────────────
// MapStaticAssets replaces UseStaticFiles: build-time gzip/brotli precompression,
// content-based ETags, and fingerprinted immutable (1-year) caching for the
// files under wwwroot. Assets are still served at their original request paths,
// so existing ~/css, ~/js, ~/img links keep working.
app.MapStaticAssets();

// ── MVC routes ────────────────────────────────────────────────────────────────
// .WithStaticAssets() lets the script/link/img tag helpers resolve the
// fingerprinted (content-hashed, immutable-cached) asset URLs produced by
// MapStaticAssets() above, so local wwwroot assets get long-term caching
// automatically when referenced via asp-href/asp-src in views.
app.MapControllerRoute(
    name:    "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
   .WithStaticAssets();

// Root "/" is served by HomeController.Index ([Area("Public")] + [HttpGet("")]),
// reconciling the storefront home to the plan's canonical route (§2.1). The
// former "/" → /explore redirect has been removed; /explore and /home remain as
// secondary aliases on the same action.
app.Run();
