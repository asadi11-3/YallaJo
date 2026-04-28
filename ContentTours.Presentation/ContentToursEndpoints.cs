using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentTours.Presentation;

public static class ContentToursEndpoints
{
    public static IEndpointRouteBuilder MapContentToursEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tours")
            .WithTags("ContentTours");

        // Task 2A — TourSchedule (endpoints 12–15)
        TourScheduleEndpoints.MapTourScheduleEndpoints(group);

        // Task 2B — TourPricingTier (endpoints 16–19)
        TourPricingTierEndpoints.MapTourPricingTierEndpoints(group);

        // Task 3 — Search / Suggest / Featured / MyTours / FeatureToggle (endpoints 20–24)
        TourSearchEndpoints.MapTourSearchEndpoints(group);

        return endpoints;
    }
}
