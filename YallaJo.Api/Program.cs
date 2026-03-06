using Accounts.Application;
using Accounts.Infrastructure;
using Accounts.Presentation;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Security.Application;
using Security.Contracts.Authorization;
using Security.Infrastructure;
using Security.Infrastructure.Seeding;
using Security.Presentation;
using System.Text;
using YallaJo.Api.Authorization;
using YallaJo.Api.ExceptionHandlers;
using YallaJo.Api.Services;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Infrastructure;
using YallaJo.Api.Extensions;
using ContentCore.Application;
using ContentCore.Infrastructure;
using ContentCore.Presentation;
using ContentPlaces.Application;
using ContentPlaces.Infrastructure;
using ContentPlaces.Presentation;
using ContentTours.Application;
using ContentTours.Infrastructure;
using ContentTours.Presentation;
using ContentBlogs.Application;
using ContentBlogs.Infrastructure;
using ContentBlogs.Presentation;
using ContentSeo.Application;
using ContentSeo.Infrastructure;
using ContentSeo.Presentation;
using Analytics.Application;
using Analytics.Infrastructure;
using Analytics.Presentation;
using Booking.Application;
using Booking.Infrastructure;
using Booking.Presentation;
using Finance.Application;
using Finance.Infrastructure;
using Finance.Presentation;
using Messaging.Application;
using Messaging.Infrastructure;
using Messaging.Presentation;
using Social.Application;
using Social.Infrastructure;
using Social.Presentation;
using Tracking.Application;
using Tracking.Infrastructure;
using Tracking.Presentation;

var builder = WebApplication.CreateBuilder(args);

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
            ClockSkew = TimeSpan.FromSeconds(30)
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
        Description = "Enter your JWT access token."
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Exception Handlers ───────────────────────────────────────────────────
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<DbUpdateExceptionHandler>();

// ── Problem Details (RFC 7807) ────────────────────────────────────────────
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
    {
        if (ctx.HttpContext.RequestServices
                .GetService<IHostEnvironment>()?.IsDevelopment() == true)
        {
            var ex = ctx.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex is not null)
                ctx.ProblemDetails.Extensions["exception"] = ex.ToString();
        }
    });

// ── Health Checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

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

// 2. Dev tooling
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "YallaJo API v1"));
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// 3. Transport security
app.UseHttpsRedirection();

// 4. Authentication MUST come before Authorization
// 4a. Rate limiter — must precede authentication so anonymous endpoints
//     are throttled before the JWT pipeline runs
app.UseRateLimiter();

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
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
