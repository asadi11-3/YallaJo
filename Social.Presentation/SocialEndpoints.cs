using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Social.Presentation.Endpoints;

namespace Social.Presentation;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reviewGroup = endpoints.MapGroup("/api/v1/reviews");
        reviewGroup.MapReviewEndpoints();
        reviewGroup.MapReviewAdminEndpoints();

        endpoints.MapGroup("/api/v1/favorites")
            .MapFavoriteEndpoints();

        endpoints.MapGroup("/api/v1/reports")
            .MapReportEndpoints();

        endpoints.MapGroup("/api/v1/moderation")
            .MapModerationEndpoints();

        return endpoints;
    }
}
