using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace YallaJo.Web.Areas.Public.Caching;

public static class PublicOutputCacheTagger
{
    public static void AddTag(HttpContext httpContext, string tag)
        => httpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add(tag);
}
