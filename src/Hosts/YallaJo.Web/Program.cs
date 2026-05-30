using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.ExternalAuth;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Infrastructure.Authorization;
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
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddHttpMessageHandler<JwtAuthHandler>();

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

// ── Auth feature services ─────────────────────────────────────────────────────
builder.Services.AddScoped<LoginApiClient>();
builder.Services.AddScoped<LoginFacade>();

builder.Services.AddScoped<AcceptInviteApiClient>();
builder.Services.AddScoped<AcceptInviteFacade>();

builder.Services.AddScoped<RegisterApiClient>();
builder.Services.AddScoped<RegisterFacade>();

builder.Services.AddScoped<VerifyEmailApiClient>();
builder.Services.AddScoped<VerifyEmailFacade>();

builder.Services.AddScoped<ForgotPasswordApiClient>();
builder.Services.AddScoped<ForgotPasswordFacade>();

builder.Services.AddScoped<ResetPasswordApiClient>();
builder.Services.AddScoped<ResetPasswordFacade>();

builder.Services.AddScoped<SessionsApiClient>();
builder.Services.AddScoped<SessionsFacade>();

builder.Services.AddScoped<DevicesApiClient>();
builder.Services.AddScoped<DevicesFacade>();

builder.Services.AddScoped<ExternalProvidersApiClient>();
builder.Services.AddScoped<ExternalProvidersFacade>();

builder.Services.AddScoped<LogoutApiClient>();
builder.Services.AddScoped<LogoutFacade>();

builder.Services.AddScoped<LogoutAllApiClient>();
builder.Services.AddScoped<LogoutAllFacade>();

// ── Accounts (non-admin, self-service) services ──────────────────────────────
builder.Services.AddScoped<ChangePasswordApiClient>();
builder.Services.AddScoped<ChangePasswordFacade>();

builder.Services.AddScoped<UpdatePhoneApiClient>();
builder.Services.AddScoped<UpdatePhoneFacade>();

builder.Services.AddScoped<ProfileApiClient>();
builder.Services.AddScoped<ProfileFacade>();

builder.Services.AddScoped<InvitationsApiClient>();
builder.Services.AddScoped<InvitationsFacade>();

// ── Admin / Security services ─────────────────────────────────────────────────
builder.Services.AddScoped<UsersApiClient>();
builder.Services.AddScoped<UsersFacade>();

builder.Services.AddScoped<RolesApiClient>();
builder.Services.AddScoped<RolesFacade>();

builder.Services.AddScoped<AuditLogsApiClient>();
builder.Services.AddScoped<AuditLogsFacade>();

// Phase 5B — admin lifecycle wiring (Suspend/Reactivate/Archive/
// Reset Password/Reassign). Lives in Users/Lifecycle/ to keep the
// existing UsersController/Facade lean.
builder.Services.AddScoped<YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.LifecycleApiClient>();
builder.Services.AddScoped<YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.LifecycleFacade>();

// ── Admin / ContentCore services ─────────────────────────────────────────────
builder.Services.AddScoped<LanguagesApiClient>();
builder.Services.AddScoped<LanguagesFacade>();

builder.Services.AddScoped<TagsApiClient>();
builder.Services.AddScoped<TagsFacade>();

builder.Services.AddScoped<SpecializationsApiClient>();
builder.Services.AddScoped<SpecializationsFacade>();

builder.Services.AddScoped<CategoriesApiClient>();
builder.Services.AddScoped<CategoriesFacade>();

builder.Services.AddScoped<AttachmentsApiClient>();
builder.Services.AddScoped<AttachmentsFacade>();

builder.Services.AddScoped<TranslationsApiClient>();
builder.Services.AddScoped<TranslationsFacade>();

// ── Admin / ContentPlaces services ───────────────────────────────────────────
builder.Services.AddScoped<PlacesApiClient>();
builder.Services.AddScoped<PlacesFacade>();

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

if(builder.Environment.IsDevelopment())
{
    // In development, use the default Razor runtime compilation setup which
    // watches the filesystem for changes and automatically recompiles views.
    mvcBuilder.AddRazorRuntimeCompilation();
}

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// ── MVC routes ────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name:    "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Root landing → Auth/Login (area-aware).
app.MapControllerRoute(
    name:    "root",
    pattern: string.Empty,
    defaults: new { area = "Auth", controller = "Login", action = "Index" });

app.MapControllerRoute(
    name:    "default",
    pattern: "{area=Auth}/{controller=Login}/{action=Index}/{id?}");

app.Run();
