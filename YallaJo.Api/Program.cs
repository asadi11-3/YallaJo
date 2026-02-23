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

var builder = WebApplication.CreateBuilder(args);

// ── Module registrations ──────────────────────────────────────────────────
builder.Services.AddAccountsApplication();
builder.Services.AddAccountsInfrastructure(builder.Configuration);

builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration);

builder.Services.AddSecurityApplication();
builder.Services.AddSecurityInfrastructure(builder.Configuration);

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

// ── Infrastructure endpoints ──────────────────────────────────────────────
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
