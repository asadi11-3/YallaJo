using Accounts.Application;
using Accounts.Infrastructure;
using Accounts.Presentation;
using Analytics.Application;
using Analytics.Infrastructure;
using Analytics.Presentation;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Presentation;
using Booking.Application;
using Booking.Infrastructure;
using Booking.Presentation;
using ContentBlogs.Application;
using ContentBlogs.Infrastructure;
using ContentBlogs.Presentation;
using ContentCore.Application;
using ContentCore.Infrastructure;
using ContentCore.Presentation;
using ContentPlaces.Application;
using ContentPlaces.Infrastructure;
using ContentPlaces.Presentation;
using ContentSeo.Application;
using ContentSeo.Infrastructure;
using ContentSeo.Presentation;
using ContentTours.Application;
using ContentTours.Infrastructure;
using ContentTours.Presentation;
using Finance.Application;
using Finance.Infrastructure;
using Finance.Presentation;
using Messaging.Application;
using Messaging.Infrastructure;
using Messaging.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Security.Application;
using Security.Contracts.Authorization;
using Security.Infrastructure;
using Security.Infrastructure.Seeding;
using Security.Presentation;
using Social.Application;
using Social.Infrastructure;
using Social.Presentation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracking.Application;
using Tracking.Infrastructure;
using Tracking.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.Api.Endpoints;
using YallaJo.Api.ExceptionHandlers;
using YallaJo.Api.Extensions;
using YallaJo.Api.Middleware;
using YallaJo.Api.Services;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Infrastructure;

// ── Serilog bootstrap (captures startup errors) ───────────────────────────
var builder = WebApplication.CreateBuilder(args);
builder.AddYallaJoSerilog();

// ── JSON options for minimal-API endpoints ────────────────────────────────
// Aligns the API's read/write contract with the web client (ApiClient) which
// serializes with CamelCase.  Without this, {"roleId":"..."} sent by the
// client would silently fail to bind to PascalCase record constructor params,
// leaving every Guid at Guid.Empty and triggering FluentValidation failures.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull;

    // F26 fix: accept enum *names* (e.g. "Tour", "IndependentGuide") in request
    // bodies, not just integers. Without this converter, System.Text.Json rejects
    // string enum values with a JsonException → 500/400, which broke favorites,
    // reviews, notification-template and other DTOs that carry enum fields.
    // JsonStringEnumConverter still accepts integer values on read, so existing
    // numeric callers keep working; responses now emit readable enum names.
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ── Module registrations ──────────────────────────────────────────────────
builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);

builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);

builder.Services.AddSecurityApplication();
builder.Services.AddSecurityInfrastructure(builder.Configuration);

builder.Services.AddContentCoreApplication();
builder.Services.AddContentCoreInfrastructure(builder.Configuration);

builder.Services.AddContentPlacesApplication();
builder.Services.AddContentPlacesInfrastructure(builder.Configuration);

builder.Services.AddContentToursApplication();
builder.Services.AddContentToursInfrastructure(builder.Configuration);

builder.Services.AddContentBlogsApplication();
builder.Services.AddContentBlogsInfrastructure(builder.Configuration);
builder.Services.AddContentBlogsPresentation();

builder.Services.AddContentSeoApplication();
builder.Services.AddContentSeoInfrastructure(builder.Configuration);

builder.Services.AddAnalyticsApplication();
builder.Services.AddAnalyticsInfrastructure(builder.Configuration);

builder.Services.AddBookingApplication();
builder.Services.AddBookingInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddFinanceApplication();
builder.Services.AddFinanceInfrastructure(builder.Configuration);

builder.Services.AddMessagingApplication();
builder.Services.AddMessagingInfrastructure(builder.Configuration);
builder.Services.AddSignalR(o =>
{
    // UI-PERF S7: client keep-alive 15s, server timeout 30s.
    o.KeepAliveInterval = TimeSpan.FromSeconds(15);
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    // Bounds concurrent hub-method invocations per connection (defence-in-depth).
    // NOTE: this is NOT the UI-PERF S6 "max 5 connections per user" cap — that is a
    // per-user connection limit enforced at the gateway / a hub connection registry,
    // not by SignalR options. (The public TourSlotsHub is anonymous, so S6 applies to
    // the authenticated NotificationHub; tour-hub abuse is bounded by rate-limiting.)
    o.MaximumParallelInvocationsPerClient = 5;
});

builder.Services.AddSocialApplication();
builder.Services.AddSocialInfrastructure(builder.Configuration);

builder.Services.AddTrackingApplication();
builder.Services.AddTrackingInfrastructure(builder.Configuration);

// ── Shared cross-cutting: behaviors, clock, domain event dispatcher ───────
builder.Services.AddSharedKernelInfrastructure(builder.Configuration);
builder.Services.AddDataSeeding();

// ── HTTP Context services ────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, RequestContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<ISeoRedirectLookupService, NoopSeoRedirectLookupService>();

// ── Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddYallaJoRateLimiting();

// ── API Versioning ────────────────────────────────────────────────────────
builder.Services.AddYallaJoApiVersioning();

// ── OpenTelemetry (tracing + metrics) ─────────────────────────────────────
builder.AddYallaJoOpenTelemetry();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddYallaJoHealthChecks(builder.Configuration);

// ── Secret / transport fail-fast validation (Patch 0A) ────────────────────
// Secrets are no longer stored in appsettings; they MUST be supplied via
// user-secrets (dev) or environment variables / key vault (prod). Fail fast
// with a clear message if a required secret is missing or blank, and refuse to
// start in Production unless the SQL connection enforces transport encryption.
StartupSecretGuards.Validate(builder.Configuration, builder.Environment);

// ── Authentication & Authorization ────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt:Key is not configured. Supply it via user-secrets (dev) or the Jwt__Key environment variable (prod).");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Owner", p => p.RequireRole(AppRoles.Owner));
    opts.AddPolicy("SuperAdmin", p => p.RequireRole(AppRoles.Owner, AppRoles.SuperAdmin));
    opts.AddPolicy("Admin", p => p.RequireRole(AppRoles.Owner, AppRoles.SuperAdmin, AppRoles.Admin));
});

builder.Services.AddPermissionAuthorization();

// ── API Documentation ─────────────────────────────────────────────────────
// Dev-only CORS so Swagger UI can fetch swagger.json AND execute "Try it out"
// against /api/* without the browser aborting on OPTIONS preflight.
// Two scoped policies:
//   • SwaggerDocs   — for /swagger/* (loading the document, OPTIONS preflight).
//   • LocalDevApi   — for /api/*     (browser "Execute" requests including OPTIONS preflight,
//                                     Bearer auth, JSON bodies).
// Both are Development-only and applied scoped (UseWhen) so Production behavior is unchanged.
builder.Services.AddCors(options =>
{
    options.AddPolicy("SwaggerDocs", policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod());

    options.AddPolicy("LocalDevApi", policy => policy
        .WithOrigins(
            "https://localhost:57065",
            "http://localhost:57065",
            "https://localhost:57066",
            "http://localhost:57066")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());

    // Production CORS policy. Applied globally (see UseCors below) so the
    // YallaJo.Web frontend and the SignalR NotificationHub can call the API
    // cross-origin. AllowCredentials is REQUIRED for SignalR (it sends the
    // access token / cookies) and for authenticated fetch from the web app.
    // Origins are read from configuration (Cors:AllowedOrigins) so deployments
    // can set their own hostnames without a code change. With AllowCredentials,
    // wildcard origins are illegal, so an explicit list is mandatory.
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    options.AddPolicy("YallaJoPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// ── Response Compression ───────────────────────────────────────────────────
// Brotli + Gzip for API/JSON and SignalR payloads. Brotli first (better ratio),
// Gzip fallback for older clients. Enabled for HTTPS as well (payloads here are
// API JSON, not secrets in URLs, so BREACH risk is mitigated by anti-forgery /
// no-secret-in-body conventions).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "YallaJo API", Version = "v1" });

    // Deterministic full-name schema IDs to avoid collisions between modules that share
    // a short type name (e.g. Accounts.Domain.Enums.DocumentType vs Booking.Domain.Enums.DocumentType).
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token.",
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

// ── Exception Handlers (order matters — first match wins) ─────────────────
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateExceptionHandler>();
// Maps DomainException subtypes: EntityNotFoundException → 404,
// BusinessRuleViolationException/ConcurrencyException → 409. Must run before the
// GlobalExceptionHandler catch-all so these never fall through to a generic 500.
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ── Problem Details (RFC 7807) ────────────────────────────────────────────
builder.Services.AddProblemDetails();

// ── Forwarded headers (Patch 0A) ──────────────────────────────────────────
// The API runs behind a reverse proxy / hosting platform (runasp.net). Honour
// X-Forwarded-For / X-Forwarded-Proto so Request.Scheme reflects the original
// HTTPS request and rate-limiter IP partitioning keys on the real client IP
// (not the proxy). KnownNetworks/KnownProxies are cleared because the upstream
// proxy set is not statically known on the hosting platform; trust is bounded
// by the platform terminating TLS in front of the app.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

await app.UseDataSeedingAsync();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SecurityDataSeeder>();
    var seederLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("SecurityDataSeeder");
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        seederLogger.LogError(ex, "SecurityDataSeeder failed. Startup will continue.");
    }
}

// ── Middleware pipeline (ORDER IS MANDATORY) ──────────────────────────────

// 0. Forwarded headers (Patch 0A) — MUST be first so every downstream component
// (HTTPS redirect, HSTS, rate limiter IP partition, CORS, auth) sees the
// original client scheme/IP rather than the reverse proxy's.
app.UseForwardedHeaders();

// 1. Global exception handler — must be first so it wraps all downstream errors
app.UseExceptionHandler();
app.UseStatusCodePages();

// 1a. Response compression — early so all downstream responses (module
// endpoints, SignalR negotiate, health) are compressed.
app.UseResponseCompression();

// 1b. Security & observability headers (F114) — applied to every response,
// including error responses, via OnStarting inside the middleware.
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Serilog request logging — early to capture full request lifecycle
app.UseYallaJoSerilogRequestLogging();

// 3. Swagger — gated to non-Production (Patch 0A). Exposing the full API
// surface, schemas and operation list anonymously in production is an
// information-disclosure / attack-surface-mapping aid, so it is disabled there.
// CORS opened for /swagger/* so the JSON doc loads from any browser origin.
// SecurityHeadersMiddleware already relaxes CSP for /swagger paths.
if (!app.Environment.IsProduction())
{
    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
        branch => branch.UseCors("SwaggerDocs"));

    app.UseSwagger();
    app.UseSwaggerUI(ui =>
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "YallaJo API v1"));
}

// Dev-only: CORS for /api so Swagger "Try it out" works cross-port locally.
// Not needed in Production (same-origin). Must run before rate limiter /
// authentication so OPTIONS preflight is never throttled or 401'd.
if (app.Environment.IsDevelopment())
{
    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseCors("LocalDevApi"));
}

// 4. Transport security
app.UseHttpsRedirection();

// 5. Static files — serve uploaded files from wwwroot/uploads
//
// SECURITY: provider documents (national IDs, business/tourism licenses, insurance
// certificates, etc.) are private PII. They must NEVER be served anonymously via the
// static-file middleware. This short-circuit runs BEFORE UseStaticFiles and returns 404
// for any request under the protected provider-document folders, so the bytes are
// unreachable by URL:
//   • /uploads/provider-application-documents — Accounts provider-application documents
//     (authorized download: GET /api/v1/provider/documents/{documentId}/download).
//   • /uploads/provider-documents — Booking provider documents (authorized download:
//     GET /api/v1/booking/provider/documents/{id}/download).
// StartsWithSegments matches whole path segments, so e.g. "/uploads/provider-documents-x"
// is unaffected. Public images such as avatars and tour/blog media (other /uploads
// subfolders) remain served normally.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments(
            "/uploads/provider-application-documents", StringComparison.OrdinalIgnoreCase)
        || context.Request.Path.StartsWithSegments(
            "/uploads/provider-documents", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseStaticFiles();

// 6. Request localization — parse Accept-Language, set CultureInfo
app.UseMiddleware<RequestLocalizationMiddleware>();

// 6b. CORS — production policy for the YallaJo.Web frontend + SignalR hub.
// Must run before rate limiter / authentication so the browser's OPTIONS
// preflight is never throttled or 401'd. Dev-only scoped policies (SwaggerDocs,
// LocalDevApi) are applied above via UseWhen and are unaffected by this.
app.UseCors("YallaJoPolicy");

// 7. Rate limiter — must precede authentication
app.UseRateLimiter();

// 8. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 9. SEO redirect middleware — intercepts 404s and issues 301/302 before error page
app.UseMiddleware<SeoRedirectMiddleware>();

// ── Module endpoints ──────────────────────────────────────────────────────
app.MapAccountsEndpoints();
app.MapAuthEndpoints();
app.MapSecurityEndpoints();
app.MapContentCoreEndpoints();
app.MapContentPlacesEndpoints();
app.MapContentToursEndpoints();
app.MapContentBlogsEndpoints();
app.MapContentSeoEndpoints();
app.MapAnalyticsEndpoints();
app.MapBookingEndpoints();
app.MapFinanceEndpoints();
app.MapMessagingEndpoints();
app.MapSocialEndpoints();
app.MapTrackingEndpoints();

// ── Ops endpoints ─────────────────────────────────────────────────────────
app.MapOpsEndpoints();

// ── Infrastructure endpoints ──────────────────────────────────────────────
// Swagger is only mounted outside Production (see step 3), so the docs link is
// advertised only when it actually exists.
var swaggerDocsLink = app.Environment.IsProduction() ? null : "/swagger";
app.MapGet("/", () => Results.Ok(new
{
    service = "YallaJo API",
    status = "running",
    health = "/health",
    docs = swaggerDocsLink,
}))
    .AllowAnonymous()
    .WithTags("Infrastructure")
    .WithName("ApiStatus")
    .WithSummary("Returns API status and useful links.");

// ── Health check endpoints ────────────────────────────────────────────────
app.MapYallaJoHealthChecks();

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
