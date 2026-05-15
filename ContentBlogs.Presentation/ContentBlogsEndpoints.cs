using ContentBlogs.Presentation.Endpoints.Blog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentBlogs.Presentation;

public static class ContentBlogsEndpoints
{
    public static IEndpointRouteBuilder MapContentBlogsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/blogs")
            .WithTags("ContentBlogs");

        BlogEndpoints.MapBlogEndpoints(group);

        return endpoints;
    }
}
