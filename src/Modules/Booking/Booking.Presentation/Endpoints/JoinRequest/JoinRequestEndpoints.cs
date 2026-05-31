using Booking.Application.Commands.ApproveJoinRequest;
using Booking.Application.Commands.RejectJoinRequest;
using Booking.Application.Commands.SubmitJoinRequest;
using Booking.Application.Queries.GetJoinRequests;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.JoinRequest;

/// <summary>
/// Join request endpoints mounted under <c>/api/v1/booking/join-requests</c>.
/// </summary>
internal static class JoinRequestEndpoints
{
    internal static void MapJoinRequestEndpoints(RouteGroupBuilder group)
    {
        // GET /join-requests?tourBookingId=&myRequestsOnly=
        group.MapGet("/", async (
                ISender sender,
                CancellationToken ct,
                Guid? tourBookingId = null,
                bool myRequestsOnly = false) =>
            {
                var result = await sender.Send(new GetJoinRequestsQuery(tourBookingId, myRequestsOnly), ct);
                return result.ToApiResult();
            })
            .WithName("GetJoinRequests")
            .WithSummary("List join requests. Filter by booking or own requests.")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.ReadOwn))
            .RequireAuthorization();

        // POST /join-requests — user submits join request
        group.MapPost("/", async (
                SubmitJoinRequestRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(request.ToCommand(), ct);
                return result.ToApiResult();
            })
            .WithName("SubmitJoinRequest")
            .WithSummary("Submit a join request to join an existing confirmed booking.")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.Create))
            .RequireAuthorization();

        // POST /join-requests/{id}/approve — guide approves
        group.MapPost("/{id:guid}/approve", async (
                Guid id,
                ApproveJoinRequestRequest? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ApproveJoinRequestCommand(id, request?.ResponseMessage), ct);
                return result.ToApiResult();
            })
            .WithName("ApproveJoinRequest")
            .WithSummary("Guide approves a join request.")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.Approve))
            .RequireAuthorization();

        // POST /join-requests/{id}/reject — guide rejects
        group.MapPost("/{id:guid}/reject", async (
                Guid id,
                RejectJoinRequestRequest? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RejectJoinRequestCommand(id, request?.ResponseMessage), ct);
                return result.ToApiResult();
            })
            .WithName("RejectJoinRequest")
            .WithSummary("Guide rejects a join request.")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.JoinRequest, AppAction.Reject))
            .RequireAuthorization();
    }
}

public sealed record SubmitJoinRequestRequest(
    Guid TourBookingId,
    Guid AvailabilitySlotId,
    int ParticipantCount,
    string? Message)
{
    public SubmitJoinRequestCommand ToCommand()
        => new(TourBookingId, AvailabilitySlotId, ParticipantCount, Message);
}

public sealed record ApproveJoinRequestRequest(string? ResponseMessage);
public sealed record RejectJoinRequestRequest(string? ResponseMessage);
