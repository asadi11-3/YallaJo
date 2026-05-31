using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Social.Presentation.Endpoints.Favorite;
using Social.Presentation.Endpoints.Moderation;
using Social.Presentation.Endpoints.Report;
using Social.Presentation.Endpoints.Review;

namespace Social.Presentation;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reviews = endpoints.MapGroup("/api/v1/social/reviews");
        ReviewEndpoints.MapReviewEndpoints(reviews);
        ReviewEndpoints.MapReviewAdminEndpoints(reviews);

        var favorites = endpoints.MapGroup("/api/v1/social/favorites");
        FavoriteEndpoints.MapFavoriteEndpoints(favorites);

        var reports = endpoints.MapGroup("/api/v1/social/reports");
        ReportEndpoints.MapReportEndpoints(reports);

        var moderation = endpoints.MapGroup("/api/v1/social/moderation");
        ModerationEndpoints.MapModerationEndpoints(moderation);

        return endpoints;
    }
}
