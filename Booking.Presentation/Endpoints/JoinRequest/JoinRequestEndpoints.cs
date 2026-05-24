using Booking.Application.Commands.JoinRequest.ApproveJoinRequest;
using Booking.Application.Queries.JoinRequest;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.JoinRequest;

internal static class JoinRequestEndpoints
{
    internal static void MapJoinRequestEndpoints(RouteGroupBuilder group)
    {
        MapCreateEndpoint(group);
        MapApproveEndpoint(group);
        MapRejectEndpoint(group);
    }

    private static void MapCreateEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/join-requests", async (
                CreateJoinRequestRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("CreateJoinRequest")
            .WithSummary("Submit a request to join a Confirmed tour booking owned by another user.")
            .WithTags("Booking")
            .Accepts<CreateJoinRequestRequest>("application/json")
            .Produces<JoinRequestDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.Create))
            .RequireAuthorization();
    }

    private static void MapApproveEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/join-requests/{id:guid}/approve", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ApproveJoinRequestCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("ApproveJoinRequest")
            .WithSummary("Approve a pending join request (booking owner or admin). Reserves slot capacity.")
            .WithTags("Booking")
            .Produces<JoinRequestDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.Approve))
            .RequireAuthorization();
    }

    private static void MapRejectEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/join-requests/{id:guid}/reject", async (
                Guid id,
                RejectJoinRequestRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("RejectJoinRequest")
            .WithSummary("Reject a pending join request (booking owner or admin).")
            .WithTags("Booking")
            .Accepts<RejectJoinRequestRequest>("application/json")
            .Produces<JoinRequestDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.Reject))
            .RequireAuthorization();
    }
}
