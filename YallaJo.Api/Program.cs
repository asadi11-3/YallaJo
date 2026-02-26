using YallaJo.Api.ExceptionHandlers;
using Microsoft.AspNetCore.Diagnostics;
using Accounts.Application;
using Accounts.Infrastructure;
using Accounts.Presentation;
using Auth.Application;
using Auth.Infrastructure;
using Auth.Presentation;
using Security.Application;
using Security.Infrastructure;
using Security.Presentation;
using YallaJo.SharedKernel.Infrastructure;
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

// ── Authentication & Authorization ────────────────────────────────────────
// TODO: Register a concrete scheme here (e.g. AddJwtBearer) when the Auth
//       module implements credential verification and token issuance.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ── API Documentation ─────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "YallaJo API",
        Version = "v1"
    }));

// ── Exception Handlers ───────────────────────────────────────────────────
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

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
