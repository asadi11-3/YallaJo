using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Social.Presentation.Endpoints;

namespace Social.Presentation;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reviewGroup = endpoints.MapGroup("/api/v1/social/reviews");
        reviewGroup.MapReviewEndpoints();
        reviewGroup.MapReviewAdminEndpoints();

        endpoints.MapGroup("/api/v1/social/favorites")
            .MapFavoriteEndpoints();

        endpoints.MapGroup("/api/v1/social/reports")
            .MapReportEndpoints();

        endpoints.MapGroup("/api/v1/social/moderation")
            .MapModerationEndpoints();

        return endpoints;
    }
}
