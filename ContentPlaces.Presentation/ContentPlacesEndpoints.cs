using ContentPlaces.Presentation.Endpoints.Business;
using ContentPlaces.Presentation.Endpoints.Place;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentPlaces.Presentation.Endpoints.BusinessAmenity;
using ContentPlaces.Presentation.Endpoints.BusinessStaff;
using ContentPlaces.Presentation.Endpoints.AccessibilityFeature;

namespace ContentPlaces.Presentation;

public static class ContentPlacesEndpoints
{
    public static IEndpointRouteBuilder MapContentPlacesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .WithTags("ContentPlaces");

        BusinessEndpoints.MapBusinessEndpoints(group);

        PlaceEndpoints.MapPlaceEndpoints(group);
        BusinessAmenityEndpoints.MapBusinessAmenityEndpoints(group);
        BusinessStaffEndpoints.MapBusinessStaffEndpoints(group);
        AccessibilityFeatureEndpoints.MapAccessibilityFeatureEndpoints(group);
        return endpoints;
    }
}
