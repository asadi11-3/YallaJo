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
using Tracking.Application;
using Tracking.Infrastructure;
using Tracking.Presentation;
using YallaJo.Api.Authorization;
using YallaJo.Api.ExceptionHandlers;
using YallaJo.Api.Extensions;
using YallaJo.Api.Middleware;
using YallaJo.Api.Services;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Infrastructure;

// ── Serilog bootstrap (captures startup errors) ───────────────────────────
var builder = WebApplication.CreateBuilder(args);
builder.AddYallaJoSerilog();

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

builder.Services.AddContentSeoApplication();
builder.Services.AddContentSeoInfrastructure(builder.Configuration);

builder.Services.AddAnalyticsApplication();
builder.Services.AddAnalyticsInfrastructure(builder.Configuration);

builder.Services.AddBookingApplication();
builder.Services.AddBookingInfrastructure(builder.Configuration);

builder.Services.AddFinanceApplication();
builder.Services.AddFinanceInfrastructure(builder.Configuration);

builder.Services.AddMessagingApplication();
builder.Services.AddMessagingInfrastructure(builder.Configuration);

builder.Services.AddSocialApplication();
builder.Services.AddSocialInfrastructure(builder.Configuration);

builder.Services.AddTrackingApplication();
builder.Services.AddTrackingInfrastructure(builder.Configuration);

// ── Shared cross-cutting: behaviors, clock, domain event dispatcher ───────
builder.Services.AddSharedKernelInfrastructure();
builder.Services.AddDataSeeding();

// ── HTTP Context services ────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, RequestContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ── Rate Limiting ─────────────────────────────────────────────────────────
builder.Services.AddYallaJoRateLimiting();

// ── API Versioning ────────────────────────────────────────────────────────
builder.Services.AddYallaJoApiVersioning();

// ── OpenTelemetry (tracing + metrics) ─────────────────────────────────────
builder.AddYallaJoOpenTelemetry();

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddYallaJoHealthChecks(builder.Configuration);

// ── Authentication & Authorization ────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
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

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ── API Documentation ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "YallaJo API", Version = "v1" });

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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ── Problem Details (RFC 7807) ────────────────────────────────────────────
builder.Services.AddProblemDetails();

var app = builder.Build();

await app.UseDataSeedingAsync();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SecurityDataSeeder>();
    await seeder.SeedAsync();
}

// ── Middleware pipeline (ORDER IS MANDATORY) ──────────────────────────────

// 1. Global exception handler — must be first so it wraps all downstream errors
app.UseExceptionHandler();
app.UseStatusCodePages();

// 2. Serilog request logging — early to capture full request lifecycle
app.UseYallaJoSerilogRequestLogging();

// 3. Dev tooling
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "YallaJo API v1"));
}

// 4. Transport security
app.UseHttpsRedirection();

// 5. Static files — serve uploaded files from wwwroot/uploads
app.UseStaticFiles();

// 6. Request localization — parse Accept-Language, set CultureInfo
app.UseMiddleware<RequestLocalizationMiddleware>();

// 7. Rate limiter — must precede authentication
app.UseRateLimiter();

// 8. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

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

// ── Infrastructure endpoints ──────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new
{
    service = "YallaJo API",
    status = "running",
    health = "/health",
    docs = "/swagger",
}))
    .AllowAnonymous()
    .WithTags("Infrastructure")
    .WithName("ApiStatus")
    .WithSummary("Returns API status and useful links.");

// ── Health check endpoints ────────────────────────────────────────────────
app.MapYallaJoHealthChecks();

app.Run();
