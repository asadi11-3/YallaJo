using ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;
using ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;
using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;
using ContentPlaces.Presentation.Endpoints.BusinessAmenity.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentPlaces.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.BusinessAmenity;

internal static class BusinessAmenityEndpoints
{
    internal static void MapBusinessAmenityEndpoints(RouteGroupBuilder group)
    {
        var amenities = group.MapGroup("/places/businesses")
            .WithTags("ContentPlaces | BusinessAmenities");


        amenities.MapGet("/{id:guid}/amenities", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListBusinessAmenitiesQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("ListBusinessAmenities")
        .Produces<IReadOnlyList<BusinessAmenityDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("List amenities for a business")
        .AllowAnonymous();

        amenities.MapPost("/{id:guid}/amenities", async (
            Guid id,
            AddBusinessAmenityRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddBusinessAmenityCommand(id, request.Name, request.Icon, request.SortOrder), ct);
            return result.ToApiResult();
        })
        .WithName("AddBusinessAmenity")
        .Produces<BusinessAmenityDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Add an amenity to a business")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.BusinessAmenity, AppAction.Create))
        .RequireAuthorization();

        amenities.MapDelete("/amenities/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveBusinessAmenityCommand(id), ct);
            return result.ToApiResult();
        })
        .WithName("RemoveBusinessAmenity")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove an amenity from a business")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.BusinessAmenity, AppAction.Delete))
        .RequireAuthorization();
    }
}
