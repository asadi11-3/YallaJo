using ContentPlaces.Application.Commands.BusinessAmenity.AddBusinessAmenity;
using ContentPlaces.Application.Commands.BusinessAmenity.RemoveBusinessAmenity;
using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;
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

        // GET
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("List amenities")
        .WithDescription("Returns all amenities for a given business")
        .AllowAnonymous();

        // POST
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Add amenity")
        .WithDescription("Adds a new amenity to the business if it does not already exist")
        .RequireAuthorization();

        // DELETE
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Remove amenity")
        .WithDescription("Deletes an amenity from the business")
        .RequireAuthorization();
    }
}
