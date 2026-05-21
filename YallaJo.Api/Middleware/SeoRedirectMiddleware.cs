using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.Api.Services;

namespace YallaJo.Api.Middleware;

public sealed class SeoRedirectMiddleware(
    RequestDelegate next,
    ISeoRedirectLookupService lookupService,
    HybridCache cache,
    ILogger<SeoRedirectMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, CancellationToken ct = default)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var redirect = await cache.GetOrCreateAsync(
            $"seo-redirect:{path.ToLowerInvariant()}",
            async _ => await lookupService.FindRedirectAsync(path, ct),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            tags: ["seo-redirects"],
            cancellationToken: ct);

        if (redirect is null)
        {
            await next(context);
            return;
        }

        _ = lookupService.RecordHitAsync(redirect.Id, ct);

        context.Response.Headers.Location = redirect.TargetPath;
        context.Response.StatusCode = redirect.IsPermanent ? StatusCodes.Status301MovedPermanently : StatusCodes.Status302Found;
    }
}
