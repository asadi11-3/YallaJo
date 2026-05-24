using Booking.Application.Queries.GetRefundPolicyByTour;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.RefundPolicy;

internal static class RefundPolicyEndpoints
{
    internal static void MapRefundPolicyEndpoints(RouteGroupBuilder group)
    {
        MapUpsertEndpoint(group);
        MapUpdateEndpoint(group);
        MapGetByTourEndpoint(group);
    }

    private static void MapUpsertEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/refund-policies", async (
                UpsertRefundPolicyRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("UpsertRefundPolicy")
            .WithSummary("Create or update a tour-scoped refund policy.")
            .WithTags("Booking")
            .Accepts<UpsertRefundPolicyRequest>("application/json")
            .Produces<RefundPolicyDto>(StatusCodes.Status200OK)
            .Produces<RefundPolicyDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.RefundPolicy, AppAction.Create))
            .RequireAuthorization();
    }

    private static void MapUpdateEndpoint(RouteGroupBuilder group)
    {
        group.MapPut("/refund-policies/{id:guid}", async (
                Guid id,
                UpdateRefundPolicyRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("UpdateRefundPolicy")
            .WithSummary("Update the tiers of an existing refund policy by id.")
            .WithTags("Booking")
            .Accepts<UpdateRefundPolicyRequest>("application/json")
            .Produces<RefundPolicyDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.RefundPolicy, AppAction.Update))
            .RequireAuthorization();
    }

    private static void MapGetByTourEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/refund-policies/{tourId:guid}", async (
                Guid tourId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetRefundPolicyByTourQuery(tourId), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetRefundPolicyByTour")
            .WithSummary("Get the refund policy for a tour. Returns a public default (100/0@24h) when none is configured.")
            .WithTags("Booking")
            .Produces<RefundPolicyDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AllowAnonymous();
    }
}
