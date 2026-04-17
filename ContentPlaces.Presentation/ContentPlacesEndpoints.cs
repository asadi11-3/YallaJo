using ContentPlaces.Presentation.Endpoints.Place;
using ContentPlaces.Presentation.ServiceItems; 
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentPlaces.Presentation;

public static class ContentPlacesEndpoints
{
    public static IEndpointRouteBuilder MapContentPlacesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .WithTags("ContentPlaces");

        PlaceEndpoints.MapPlaceEndpoints(group);
        ServiceItemEndpoints.MapServiceItemEndpoints(group);
        
        return endpoints;
    }
}
