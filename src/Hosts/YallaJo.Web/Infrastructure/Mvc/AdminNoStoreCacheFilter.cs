using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace YallaJo.Web.Infrastructure.Mvc;

public sealed class AdminNoStoreCacheFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var area = context.RouteData.Values.TryGetValue("area", out var value)
            ? value as string
            : null;

        if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var headers = context.HttpContext.Response.Headers;
            headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
            headers[HeaderNames.Pragma] = "no-cache";
            headers[HeaderNames.Expires] = "0";
        }

        return next();
    }
}
