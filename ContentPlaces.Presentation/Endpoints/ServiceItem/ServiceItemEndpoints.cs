using ContentPlaces.Application.Commands.ServiceItem.CreateServiceItem;
using ContentPlaces.Application.Commands.ServiceItem.UpdateServiceItem;
using ContentPlaces.Application.Commands.ServiceItem.DeleteServiceItem;
using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;
using ContentPlaces.Application.Queries.ServiceItem.GetServiceItemById;
using ContentPlaces.Domain.Repositories;
using ContentPlaces.Presentation.Endpoints.ServiceItem.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContentPlaces.Contracts.Authorization;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;

namespace ContentPlaces.Presentation.Endpoints.ServiceItem;

internal static class ServiceItemEndpoints
{
    internal static void MapServiceItemEndpoints(RouteGroupBuilder group)
    {
        // ── Group 1: business-scoped (list + create, businessId in route) ─────
        var bizServices = group.MapGroup("/places/businesses")
            .WithTags("ContentPlaces | ServiceItems");

        bizServices.MapGet("/{businessId:guid}/services", async (
            Guid businessId,
            ISender sender,
            ICurrentUser currentUser,
            IBusinessRepository businessRepository,
            CancellationToken ct) =>
        {
            // Compute IsElevated at the endpoint so it travels on the cache key.
            // Elevated = admin-tier role OR owner of the target business.
            var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
                >= RolePrivilegeLevel.Admin;
            var isOwner = false;
            if (!isAdminTier && currentUser.UserId.HasValue)
            {
                var business = await businessRepository.GetByIdAsync(businessId, ct);
                isOwner = business is not null && business.OwnerId == currentUser.UserId.Value;
            }
            var isElevated = isAdminTier || isOwner;

            var result = await sender.Send(
                new ListServiceItemsQuery(businessId, isElevated), ct);
            return result.ToApiResult();
        })
        .WithName("ListServiceItems")
        .Produces<IReadOnlyList<ServiceItemDto>>(StatusCodes.Status200OK)
        .WithSummary("List service items for a business")
        .AllowAnonymous();

        bizServices.MapPost("/{businessId:guid}/services", async (
            Guid businessId, CreateServiceItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateServiceItemCommand(
                    businessId, request.Name, request.Price,
                    request.DurationMinutes, request.MaxCapacity, request.Currency,
                    request.Category, request.Description, request.SortOrder), ct);
            return result.ToApiResult();
        })
        .WithName("CreateServiceItem")
        .Produces<CreateServiceItemResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a service item for a business")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.ServiceItem, AppAction.Create))
        .RequireAuthorization();

        // ── Group 2: item-level (get / update / delete — no businessId in route) ─
        var itemServices = group.MapGroup("/places/businesses/services")
            .WithTags("ContentPlaces | ServiceItems");

        itemServices.MapGet("/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            // BusinessId = Guid.Empty because the handler loads by ServiceItemId only.
            // The BusinessId in the query is used as a secondary validation; pass Empty for
            // item-level routes where businessId is not in the URL.
            var result = await sender.Send(new GetServiceItemByIdQuery(Guid.Empty, id), ct);
            return result.ToApiResult();
        })
        .WithName("GetServiceItemById")
        .Produces<ServiceItemDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get service item details by ID")
        .AllowAnonymous();

        itemServices.MapPut("/{id:guid}", async (
            Guid id, UpdateServiceItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateServiceItemCommand(
                    id, request.BusinessId, request.Name, request.Price,
                    request.DurationMinutes, request.MaxCapacity, request.Currency,
                    request.Category, request.Description, request.SortOrder), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateServiceItem")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Update a service item")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.ServiceItem, AppAction.Update))
        .RequireAuthorization();

        itemServices.MapDelete("/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteServiceItemCommand(id, Guid.Empty), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteServiceItem")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a service item")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.ServiceItem, AppAction.SoftDelete))
        .RequireAuthorization();
    }
}
