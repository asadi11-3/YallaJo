// <copyright file="SeoRedirectMiddleware.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Api.Middleware;

using ContentSeo.Domain.Repositories;
using ContentSeo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Intercepts 404 responses and checks the <c>Redirects</c> table for a matching
/// <c>OldUrl</c>. If found, returns a 301 or 302 with the <c>Location</c> header
/// set to <c>NewUrl</c> and increments <c>HitCount</c>.
///
/// Must be registered AFTER <c>UseAuthentication</c>/<c>UseAuthorization</c> but
/// BEFORE <c>MapEndpoints</c> — or more precisely, it wraps the endpoint pipeline
/// by calling <c>_next</c> first and then inspecting the status code.
///
/// PDF §8 requirement: "SeoRedirectMiddleware checks on every 404 before returning
/// error page."
/// </summary>
public sealed class SeoRedirectMiddleware(RequestDelegate next)
{
    // Cache TTL for redirect lookups — short enough to pick up new redirects quickly.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
    };

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Only intercept 404s — let everything else pass through.
        if (context.Response.StatusCode != StatusCodes.Status404NotFound)
            return;

        // Avoid intercepting API routes — only handle path-based redirects.
        var path = context.Request.Path.Value ?? string.Empty;

        await using var scope = context.RequestServices.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<HybridCache>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ContentSeoDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeoRedirectMiddleware>>();

        // ── Cache lookup ──────────────────────────────────────────────────────
        var cacheKey = $"seo:redirect:{path}";
        var redirectInfo = await cache.GetOrCreateAsync<RedirectInfo?>(
            cacheKey,
            async cancellation =>
            {
                var redirect = await dbContext.Redirects
                    .AsNoTracking()
                    .Where(r => r.OldUrl == path && r.IsActive && !r.IsDeleted)
                    .Select(r => new RedirectInfo(r.Id, r.NewUrl, r.StatusCode))
                    .FirstOrDefaultAsync(cancellation);

                return redirect;
            },
            CacheOptions,
            tags: ["seo:redirects:all"],
            cancellationToken: context.RequestAborted);

        if (redirectInfo is null)
            return;

        // ── Increment HitCount (fire-and-forget, non-blocking) ────────────────
        _ = Task.Run(async () =>
        {
            try
            {
                await using var hitScope = context.RequestServices.CreateAsyncScope();
                var hitDb = hitScope.ServiceProvider.GetRequiredService<ContentSeoDbContext>();
                var redirect = await hitDb.Redirects.FindAsync([redirectInfo.Id]);
                if (redirect is not null)
                {
                    redirect.IncrementHit();
                    await hitDb.SaveChangesAsync();
                }
            }
#pragma warning disable CA1031 // Best-effort hit counter — must not fail the redirect
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(ex, "SeoRedirectMiddleware: Failed to increment HitCount for redirect {RedirectId}.", redirectInfo.Id);
            }
        });

        // ── Issue redirect ────────────────────────────────────────────────────
        context.Response.Clear();
        context.Response.StatusCode = redirectInfo.StatusCode;
        context.Response.Headers.Location = redirectInfo.NewUrl;

        logger.LogInformation(
            "SeoRedirectMiddleware: {OldUrl} → {NewUrl} ({StatusCode})",
            path, redirectInfo.NewUrl, redirectInfo.StatusCode);
    }

    private sealed record RedirectInfo(Guid Id, string NewUrl, int StatusCode);
}
