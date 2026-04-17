using ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;
using ContentPlaces.Application.Commands.ServiceItem.UpdateServiceItem;
using ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;
using ContentPlaces.Application.Queries.ServiceItem.GetServiceItemById;
using ContentPlaces.Presentation.Endpoints.ServiceItem.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.ServiceItem;

internal static class ServiceItemEndpoints
{
    internal static void MapServiceItemEndpoints(RouteGroupBuilder group)
    {
        var services = group.MapGroup("/places/businesses/{businessId:guid}/services")
            .WithTags("ContentPlaces | ServiceItems");


        services.MapGet("/", async (Guid businessId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListServiceItemsQuery(businessId), ct);
            return result.ToApiResult();
        })
        .WithName("ListServiceItems")
        .Produces<IReadOnlyList<ServiceItemDto>>(StatusCodes.Status200OK)
        .WithSummary("List service items for a business")
        .AllowAnonymous();

        services.MapGet("/{serviceItemId:guid}", async (
            Guid businessId, Guid serviceItemId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetServiceItemByIdQuery(businessId, serviceItemId), ct);
            return result.ToApiResult();
        })
        .WithName("GetServiceItemById")
        .Produces<ServiceItemDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get service item details by ID")
        .AllowAnonymous();

        services.MapPost("/", async (
            Guid businessId, CreateServiceItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateServiceItemCommand(businessId, request.Name, request.Price,
                    request.DurationMinutes, request.MaxCapacity, request.Currency, request.SortOrder), ct);
            return result.ToApiResult();
        })
        .WithName("CreateServiceItem")
        .Produces<CreateServiceItemResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a service item for a business")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.ServiceItem, AppAction.Create))
        .RequireAuthorization();

        services.MapPut("/{serviceItemId:guid}", async (
            Guid businessId, Guid serviceItemId, UpdateServiceItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateServiceItemCommand(serviceItemId, businessId, request.Name, request.Price,
                    request.DurationMinutes, request.MaxCapacity, request.Currency, request.SortOrder), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateServiceItem")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Update a service item")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.ServiceItem, AppAction.Update))
        .RequireAuthorization();

        services.MapDelete("/{serviceItemId:guid}", async (
            Guid businessId, Guid serviceItemId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteServiceItemCommand(serviceItemId, businessId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteServiceItem")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a service item")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.ServiceItem, AppAction.SoftDelete))
        .RequireAuthorization();
    }
}
