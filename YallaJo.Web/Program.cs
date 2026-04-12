using Microsoft.AspNetCore.Authentication.Cookies;
using YallaJo.Web.Infrastructure.Authentication.SignIn;
using YallaJo.Web.Services;

// ── Auth feature registrations ────────────────────────────────────────────────
using YallaJo.Web.Areas.Auth.Features.Login;
using YallaJo.Web.Areas.Auth.Features.VerifyEmail;
using YallaJo.Web.Areas.Auth.Features.ForgotPassword;
using YallaJo.Web.Areas.Auth.Features.ResetPassword;
using YallaJo.Web.Areas.Auth.Features.Sessions;
using YallaJo.Web.Areas.Auth.Features.Devices;
using YallaJo.Web.Areas.Auth.Features.ExternalProviders;
using YallaJo.Web.Areas.Auth.Features.Logout;
using YallaJo.Web.Areas.Auth.Features.LogoutAll;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication (cookie — MVC frontend, BFF pattern) ──────────────────────
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/auth/login";
        options.LogoutPath       = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = SameSiteMode.Strict;
        options.Cookie.Name       = "YallaJo.Web";
    });

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

// ── Auth feature services ─────────────────────────────────────────────────────
builder.Services.AddScoped<LoginApiClient>();
builder.Services.AddScoped<LoginFacade>();

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

// ── MVC + custom Razor view locations ────────────────────────────────────────
builder.Services.AddControllersWithViews()
    .AddRazorOptions(o =>
    {
        // Feature-folder convention: Areas/{area}/Features/{controller}/Views/{view}.cshtml
        // Used by Auth, Accounts, Content areas.
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/{0}.cshtml");
        o.AreaViewLocationFormats.Add("~/Areas/{2}/Features/{1}/Views/Shared/{0}.cshtml");
    });

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
    pattern: "",
    defaults: new { area = "Auth", controller = "Login", action = "Index" });

app.MapControllerRoute(
    name:    "default",
    pattern: "{area=Auth}/{controller=Login}/{action=Index}/{id?}");

app.Run();
