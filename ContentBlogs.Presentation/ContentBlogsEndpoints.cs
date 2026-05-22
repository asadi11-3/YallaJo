using ContentBlogs.Presentation.Endpoints.Blog;
using ContentBlogs.Presentation.Endpoints.BlogComment;
using ContentBlogs.Presentation.Endpoints.Creator;
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
        BlogCommentEndpoints.MapBlogCommentEndpoints(group);
        CreatorEndpoints.MapCreatorEndpoints(group);
        CreatorPostEndpoints.MapCreatorPostEndpoints(group);
        AdminCreatorEndpoints.MapAdminCreatorEndpoints(group);
        AdminPostEndpoints.MapAdminPostEndpoints(group);

        return endpoints;
    }
}
