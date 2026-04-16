using ContentPlaces.Presentation.Endpoints.Business;
using ContentPlaces.Presentation.Endpoints.Place;
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

        BusinessEndpoints.MapBusinessEndpoints(group);

        PlaceEndpoints.MapPlaceEndpoints(group);

        return endpoints;
    }
}
