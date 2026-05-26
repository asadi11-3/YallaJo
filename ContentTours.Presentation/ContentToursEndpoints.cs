using ContentTours.Presentation.Endpoints.ChildrenInfo;
using ContentTours.Presentation.Endpoints.Tour;
using ContentTours.Presentation.Endpoints.TourGuide;
using ContentTours.Presentation.Endpoints.TourPackage;
using ContentTours.Presentation.Endpoints.TourPricingTier;
using ContentTours.Presentation.Endpoints.TourSchedule;
using ContentTours.Presentation.Endpoints.TourSearch;
using ContentTours.Presentation.Endpoints.TourWaypoint;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#pragma warning disable SA1516

namespace ContentTours.Presentation;

public static class ContentToursEndpoints
{
    public static IEndpointRouteBuilder MapContentToursEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/tours")
            .WithTags("ContentTours");

        // Task 1 — Tour Core (endpoints 1–11)
        TourEndpoints.MapTourEndpoints(group);

        // Task 2A — TourSchedule (endpoints 12–15)
        TourScheduleEndpoints.MapTourScheduleEndpoints(group);

        // Task 2B — TourPricingTier (endpoints 16–19)
        TourPricingTierEndpoints.MapTourPricingTierEndpoints(group);

        // Task 3 — Search / Suggest / Featured / MyTours / FeatureToggle (endpoints 20–24)
        TourSearchEndpoints.MapTourSearchEndpoints(group);

        // Task 4A — TourWaypoint (endpoints 25–28)
        TourWaypointEndpoints.MapTourWaypointEndpoints(group);

        // Task 4B — TourTourGuide (endpoints 29–31)
        TourGuideEndpoints.MapTourGuideEndpoints(group);

        // Task 4C — ChildrenInfo (endpoints 32–33)
        ChildrenInfoEndpoints.MapChildrenInfoEndpoints(group);

        //  Task 5 — TourPackage 
        TourPackageEndpoints.MapTourPackageEndpoints(group);

        // Guide applications + open/close-applications (mapped onto tour group)
        GuideApplicationEndpoints.MapGuideApplicationEndpoints(group);

        // Guide offering management (schedules, pricing tiers, private tours)
        GuideOfferingEndpoints.MapGuideOfferingEndpoints(group);

        var guidesGroup = endpoints.MapGroup("/api/v1/guides")
            .WithTags("ContentTours | Guide Profiles");

        TourGuideProfileEndpoints.MapTourGuideProfileEndpoints(guidesGroup);

        // Admin guide management (suspend, reinstate)
        AdminTourGuideEndpoints.MapAdminTourGuideEndpoints(guidesGroup);

        // Tour proposals (independent group at /api/v1/tours/proposals)
        TourProposalEndpoints.MapTourProposalEndpoints(endpoints);

        return endpoints;
    }
}
