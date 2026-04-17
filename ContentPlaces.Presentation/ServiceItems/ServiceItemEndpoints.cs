using ContentPlaces.Application.Features.ServiceItems.Commands.Create;
using ContentPlaces.Application.Features.ServiceItems.Commands.Delete;
using ContentPlaces.Application.Features.ServiceItems.Commands.Update;
using ContentPlaces.Application.Features.ServiceItems.Queries.GetById;
using ContentPlaces.Application.Features.ServiceItems.Queries.GetList;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ContentPlaces.Presentation.ServiceItems;

public static class ServiceItemEndpoints
{
    public static void MapServiceItemEndpoints(this IEndpointRouteBuilder app)
    {
        // (POST)
        app.MapPost("/api/v1/places/businesses/{businessId:guid}/services",
            async (Guid businessId, CreateServiceItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateServiceItemCommand(
                    businessId,
                    request.Name,
                    request.Price,
                    request.DurationMinutes,
                    request.MaxCapacity,
                    request.Currency,
                    request.SortOrder);

                var serviceItemId = await sender.Send(command, ct);

                return Results.Ok(new { Id = serviceItemId });
            })
        .WithName("CreateServiceItem")
        .WithTags("Service Items")
        .WithSummary("Creates a new service item for a specific business")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .RequireAuthorization();

        // ==========================================
        // 2.  (PUT)
        // ==========================================
        app.MapPut("/api/v1/places/businesses/{businessId:guid}/services/{serviceItemId:guid}",
            async (Guid businessId, Guid serviceItemId, UpdateServiceItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateServiceItemCommand(
                    serviceItemId,
                    businessId,
                    request.Name,
                    request.Price,
                    request.DurationMinutes,
                    request.MaxCapacity,
                    request.Currency,
                    request.SortOrder);

                await sender.Send(command, ct);

                return Results.NoContent();
            })
        .WithName("UpdateServiceItem")
        .WithTags("Service Items")
        .WithSummary("Updates an existing service item for a business")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .RequireAuthorization();

        // ==========================================
        // 3. (DELETE)
        // ==========================================
        app.MapDelete("/api/v1/places/businesses/{businessId:guid}/services/{serviceItemId:guid}",
            async (Guid businessId, Guid serviceItemId, ISender sender, CancellationToken ct) =>
            {
                var command = new DeleteServiceItemCommand(serviceItemId, businessId);

                await sender.Send(command, ct);

                return Results.NoContent();
            })
        .WithName("DeleteServiceItem")
        .WithTags("Service Items")
        .WithSummary("Deletes a service item from a business")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();

        // ==========================================
        // 4.  (GET List)
        // ==========================================
        app.MapGet("/api/v1/places/businesses/{businessId:guid}/services",
            async (Guid businessId, ISender sender, CancellationToken ct) =>
            {
                var query = new ListServiceItemsQuery(businessId);

                var result = await sender.Send(query, ct);

                return Results.Ok(result);
            })
        .WithName("ListServiceItems")
        .WithTags("Service Items")
        .WithSummary("Gets all service items for a specific business")
        .Produces<List<ServiceItemResponse>>(StatusCodes.Status200OK)

        ;

        // ==========================================
        // 5. (GET By Id)
        // ==========================================
        app.MapGet("/api/v1/places/businesses/{businessId:guid}/services/{serviceItemId:guid}",
            async (Guid businessId, Guid serviceItemId, ISender sender, CancellationToken ct) =>
            {
                var query = new GetServiceItemByIdQuery(businessId, serviceItemId);

                var result = await sender.Send(query, ct);

                return Results.Ok(result);
            })
        .WithName("GetServiceItemById")
        .WithTags("Service Items")
        .WithSummary("Gets details of a specific service item")
        .Produces<ServiceItemDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)

        ;
    }
}
