using ContentTours.Presentation.Endpoints; // عشان يشوف المجلد الداخلي
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentTours.Presentation;

public static class ContentToursEndpoints
{
    public static IEndpointRouteBuilder MapContentToursEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
                             .WithTags("ContentTours");

        TourWaypointsEndpoints.MapTourWaypointsEndpoints(group);
        TourGuidesEndpoints.MapTourGuidesEndpoints(group);
        ChildrenInfoEndpoints.MapChildrenInfoEndpoints(group);

        return endpoints;
    }
}
