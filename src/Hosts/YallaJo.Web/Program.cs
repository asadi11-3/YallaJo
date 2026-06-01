using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.DependencyInjection;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Infrastructure.Security.Recaptcha;
using YallaJo.Web.Services;

// ── Auth feature registrations ────────────────────────────────────────────────
using YallaJo.Web.Areas.Auth.Features.Login;
using YallaJo.Web.Areas.Auth.Features.AcceptInvite;
using YallaJo.Web.Areas.Auth.Features.Register;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword;
using YallaJo.Web.Areas.Auth.Features.ResetPassword;
using YallaJo.Web.Areas.Auth.Features.Sessions;
using YallaJo.Web.Areas.Auth.Features.Devices;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders;
using YallaJo.Web.Areas.Auth.Features.Logout;
using YallaJo.Web.Areas.Auth.Features.LogoutAll;

// ── Accounts (non-admin, self-service) feature registrations ─────────────────
using YallaJo.Web.Areas.Accounts.Features.ChangePassword;
using YallaJo.Web.Areas.Accounts.Features.UpdatePhone;
using YallaJo.Web.Areas.Accounts.Features.Profile;

// ── Admin / Security feature registrations ────────────────────────────────────
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

// ── Admin / Accounts feature registrations ────────────────────────────────────
using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations;

// ── Admin / ContentCore feature registrations ────────────────────────────────
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Attachments;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations;

// ── Admin / ContentPlaces feature registrations ──────────────────────────────
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication (cookie — MVC frontend, BFF pattern) ──────────────────────
var authBuilder = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/auth/login";
        options.LogoutPath       = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Name       = "YallaJo.Web";
    });

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
});

// ── MVC + custom Razor view locations ────────────────────────────────────────
var mvcBuilder  = builder.Services.AddControllersWithViews(options =>
{
    // ForbiddenResultFilter: the ONE code path that renders AccessDenied.cshtml.
    // Branches on content negotiation: HTML page → view, AJAX/JSON → ProblemDetails.
    options.Filters.Add<ForbiddenResultFilter>();
})
    .AddRazorOptions(o =>
    {
        // Auth / Accounts / Content areas follow the flat feature-folder convention:
        //   Areas/{area}/Features/{controller}/Views/{view}.cshtml
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/{0}.cshtml");
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/Shared/{0}.cshtml");

        // Area-level shared partials live at ~/Areas/{area}/Shared/{view}.cshtml
        // (e.g. Areas/Auth/Shared/_RecaptchaField.cshtml — used by every auth form).
        // Without this entry, <partial name="_RecaptchaField" /> would not resolve
        // because the feature-folder formats above only look inside a specific
        // controller's folder.
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Shared/{0}.cshtml");

        // Admin area uses a deeper module-based convention:
        //   Areas/Admin/Modules/{module}/Features/{controller}/Views/{view}.cshtml
        // The AdminModuleViewLocationExpander auto-discovers modules by scanning the
        // filesystem at startup — no manual list to maintain.
        o.ViewLocationExpanders.Add(
            new AdminModuleViewLocationExpander(builder.Environment.ContentRootPath));
    });

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
    app.UseHsts();
}

app.UseHttpsRedirection();

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

// Root landing → Auth/Login (area-aware).
app.MapControllerRoute(
    name:    "root",
    pattern: string.Empty,
    defaults: new { area = "Auth", controller = "Login", action = "Index" })
   .WithStaticAssets();

app.MapControllerRoute(
    name:    "default",
    pattern: "{area=Auth}/{controller=Login}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
