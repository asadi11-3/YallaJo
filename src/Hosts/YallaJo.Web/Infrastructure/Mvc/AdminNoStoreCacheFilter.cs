using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace YallaJo.Web.Infrastructure.Mvc;

public sealed class AdminNoStoreCacheFilter : IAsyncResultFilter
{
    // Auth-gated areas whose responses must never be stored by any cache (UI-PERF-C2).
    // Provider added by the Provider-area modernization (Phase 8): every Provider page
    // is authenticated/personalised, so browsers and proxies must revalidate always.
    private static readonly string[] NoStoreAreas = ["Admin", "Provider"];

    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var area = context.RouteData.Values.TryGetValue("area", out var value)
            ? value as string
            : null;

        if (area is not null && NoStoreAreas.Contains(area, StringComparer.OrdinalIgnoreCase))
        {
            var headers = context.HttpContext.Response.Headers;
            headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
            headers[HeaderNames.Pragma] = "no-cache";
            headers[HeaderNames.Expires] = "0";
        }

        return next();
    }
}
