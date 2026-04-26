using ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;
using ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;
using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;
using ContentPlaces.Presentation.Endpoints.BusinessAmenity.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.BusinessAmenity;

internal static class BusinessAmenityEndpoints
{
    internal static void MapBusinessAmenityEndpoints(RouteGroupBuilder group)
    {
        var amenities = group.MapGroup("/places/businesses")
            .WithTags("ContentPlaces | BusinessAmenities");

        // Get amenities (public)
        amenities.MapGet("/{businessId:guid}/amenities", async (
            Guid businessId,
            ISender sender,
            CancellationToken ct,
            int page = 1,
            int pageSize = 10) =>
        {
            var result = await sender.Send(
                new ListBusinessAmenitiesQuery(businessId, page, pageSize), ct);

            return result.ToApiResult();
        })
        .WithName("ListBusinessAmenities")
        .Produces<IReadOnlyList<BusinessAmenityDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("List amenities for a business")
        .AllowAnonymous();

        // Add amenity (owner OR admin-tier role; enforced in handler)
        amenities.MapPost("/{businessId:guid}/amenities", async (
            Guid businessId,
            AddBusinessAmenityRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddBusinessAmenityCommand(
                    businessId,
                    request.Name,
                    request.Icon,
                    request.SortOrder), ct);

            return result.ToApiResult();
        })
        .WithName("AddBusinessAmenity")
        .Produces<BusinessAmenityDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add an amenity to a business (owner OR admin-tier)")
        .RequireAuthorization();

        // Remove amenity (owner OR admin-tier role; enforced in handler)
        amenities.MapDelete("/amenities/{amenityId:guid}", async (
            Guid amenityId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RemoveBusinessAmenityCommand(amenityId), ct);

            return result.ToApiResult();
        })
        .WithName("RemoveBusinessAmenity")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove an amenity from a business (owner OR admin-tier)")
        .RequireAuthorization();
    }
}
