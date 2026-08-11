using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tracking.Application.Commands.AddLocationSnapshot;
using Tracking.Application.Commands.EndLiveTrackingSession;
using Tracking.Application.Commands.PauseLiveTrackingSession;
using Tracking.Application.Commands.ReachCheckpoint;
using Tracking.Application.Commands.ResumeLiveTrackingSession;
using Tracking.Application.Commands.SkipCheckpoint;
using Tracking.Application.Commands.StartLiveTrackingSession;
using Tracking.Application.Common;
using Tracking.Application.Queries.GetActiveTrackingSessionByBookingId;
using Tracking.Application.Queries.GetLatestLocationByBookingId;
using Tracking.Application.Queries.GetTourGuideActiveSessions;
using Tracking.Application.Queries.GetTrackingHistory;
using Tracking.Application.Queries.GetTrackingSessionDetails;
using Tracking.Contracts.Authorization;
using Tracking.Domain.Enums;
using Tracking.Presentation.Endpoints.TrakcingSession.Models;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Tracking.Presentation.Endpoints.Trakcing;

internal static class TrackingSessionEndpoints
{
    internal static void MapTrackingSessionEndpoints(RouteGroupBuilder group)
    {
        MapStartSessionEndpoint(group);
        MapAddLocationEndpoint(group);
        MapEndSessionEndpoint(group);
        MapPauseSessionEndpoint(group);
        MapResumeSessionEndpoint(group);
        MapReachCheckpointEndpoint(group);
        MapSkipCheckpointEndpoint(group);
        MapGetActiveSessionByBookingEndpoint(group);
        MapGetLatestLocationByBookingEndpoint(group);
        MapGetSessionDetailsEndpoint(group);
        MapGetGuideActiveSessionsEndpoint(group);
        MapGetTrackingHistoryEndpoint(group);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>POST /api/v1/tracking/sessions/start</summary>
    private static void MapStartSessionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/start", async (
                StartTrackingSessionRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("StartLiveTrackingSession")
            .WithSummary("Start a new live tracking session for a booking.")
            .WithDescription(
                "Creates an Active session for the assigned tour guide. " +
                "Optional WaypointIds pre-register checkpoints for the tour. " +
                "Only one active/paused session is allowed per booking at a time.")
            .Accepts<StartTrackingSessionRequest>("application/json")
            .Produces<TrackingSessionSummaryDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Create))
            .RequireAuthorization();
    }
    /// <summary>POST /api/v1/tracking/sessions/{id}/locations</summary>
    private static void MapAddLocationEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id:guid}/locations", async (
                Guid id,
                AddLocationSnapshotRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("AddLocationSnapshot")
            .WithSummary("Record a GPS location snapshot for an active session.")
            .WithDescription(
                "Requires session to be Active (not Paused, Completed, or Expired). " +
                "Caller must be the assigned tour guide or an admin.")
            .Accepts<AddLocationSnapshotRequest>("application/json")
            .Produces<TrackingLocationSnapshotDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Update))
            .RequireAuthorization();
    }

    /// <summary>POST /api/v1/tracking/sessions/{id}/end</summary>
    private static void MapEndSessionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id:guid}/end", async (
                Guid id,
                EndTrackingSessionRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("EndLiveTrackingSession")
            .WithSummary("End an active or paused tracking session.")
            .WithDescription(
                "Transitions Active or Paused session → Completed. " +
                "Idempotent: calling on an already Completed session returns the current summary. " +
                "Cannot end an Expired session.")
            .Accepts<EndTrackingSessionRequest>("application/json")
            .Produces<TrackingSessionSummaryDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Update))
            .RequireAuthorization();
    }

    /// <summary>POST /api/v1/tracking/sessions/{id}/pause</summary>
    private static void MapPauseSessionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id:guid}/pause", async (
                Guid id,
                PauseTrackingSessionRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("PauseLiveTrackingSession")
            .WithSummary("Pause an active tracking session.")
            .WithDescription(
                "Transitions Active → Paused. Location snapshots are rejected while paused. " +
                "Caller must be the assigned tour guide or an admin.")
            .Accepts<PauseTrackingSessionRequest>("application/json")
            .Produces<TrackingSessionSummaryDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Update))
            .RequireAuthorization();
    }

    /// <summary>POST /api/v1/tracking/sessions/{id}/resume</summary>
    private static void MapResumeSessionEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id:guid}/resume", async (
                Guid id,
                ResumeTrackingSessionRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("ResumeLiveTrackingSession")
            .WithSummary("Resume a paused tracking session.")
            .WithDescription(
                "Transitions Paused → Active. " +
                "Caller must be the assigned tour guide or an admin.")
            .Accepts<ResumeTrackingSessionRequest>("application/json")
            .Produces<TrackingSessionSummaryDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Update))
            .RequireAuthorization();
    }

    /// <summary>POST /api/v1/tracking/sessions/{id}/checkpoints/{checkpointId}/reach</summary>
    private static void MapReachCheckpointEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id:guid}/checkpoints/{checkpointId:guid}/reach", async (
                Guid id,
                Guid checkpointId,
                ReachCheckpointRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id, checkpointId), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("ReachCheckpoint")
            .WithSummary("Mark a checkpoint as reached.")
            .WithDescription(
                "Transitions the checkpoint from NotReached → Reached. " +
                "Session must be Active. Caller must be the assigned tour guide or an admin.")
            .Accepts<ReachCheckpointRequest>("application/json")
            .Produces<TrackingCheckpointDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Update))
            .RequireAuthorization();
    }

    /// <summary>POST /api/v1/tracking/sessions/{id}/checkpoints/{checkpointId}/skip</summary>
    private static void MapSkipCheckpointEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id:guid}/checkpoints/{checkpointId:guid}/skip", async (
                Guid id,
                Guid checkpointId,
                SkipCheckpointRequest request,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(request.ToCommand(id, checkpointId), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("SkipCheckpoint")
            .WithSummary("Mark a checkpoint as skipped.")
            .WithDescription(
                "Transitions the checkpoint from NotReached → Skipped. " +
                "Session must be Active. Caller must be the assigned tour guide or an admin.")
            .Accepts<SkipCheckpointRequest>("application/json")
            .Produces<TrackingCheckpointDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.Update))
            .RequireAuthorization();
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/tracking/bookings/{bookingId}/active</summary>
    private static void MapGetActiveSessionByBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/bookings/{bookingId:guid}/active", async (
                Guid bookingId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetActiveTrackingSessionByBookingIdQuery(bookingId), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetActiveTrackingSessionByBookingId")
            .WithSummary("Get the active or paused live tracking session for a booking.")
            .WithDescription(
                "Returns full session details including all checkpoints and location snapshots. " +
                "Returns 404 if no active/paused session exists for the booking. " +
                "Customer can view own booking's session; guide/admin can view any.")
            .Produces<TrackingSessionDetailsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    /// <summary>GET /api/v1/tracking/bookings/{bookingId}/latest-location</summary>
    private static void MapGetLatestLocationByBookingEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/bookings/{bookingId:guid}/latest-location", async (
                Guid bookingId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetLatestLocationByBookingIdQuery(bookingId), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetLatestLocationByBookingId")
            .WithSummary("Get the most recent GPS location snapshot for an active booking session.")
            .WithDescription(
                "Returns 404 if no active session exists or no snapshots have been recorded yet. " +
                "Customer can view own booking's location; guide/admin can view any.")
            .Produces<TrackingLocationSnapshotDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    /// <summary>GET /api/v1/tracking/sessions/{id}</summary>
    private static void MapGetSessionDetailsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/sessions/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetTrackingSessionDetailsQuery(id), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetTrackingSessionDetails")
            .WithSummary("Get full tracking session details by session id.")
            .WithDescription(
                "Returns all checkpoints and location snapshots for the session. " +
                "Customer can view own session; guide assigned to the booking can view it; admin can view any.")
            .Produces<TrackingSessionDetailsDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    /// <summary>GET /api/v1/tracking/guides/{guideId}/active-sessions</summary>
    private static void MapGetGuideActiveSessionsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/guides/{guideId:guid}/active-sessions", async (
                Guid guideId,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(
                    new GetTourGuideActiveSessionsQuery(guideId), cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetTourGuideActiveSessions")
            .WithSummary("Get all active or paused sessions for a tour guide.")
            .WithDescription(
                "Returns a list of session summaries. Empty list if the guide has no active sessions. " +
                "Caller must be the guide themselves or an admin.")
            .Produces<IReadOnlyList<TrackingSessionSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.ReadOwn))
            .RequireAuthorization();
    }

    /// <summary>GET /api/v1/tracking/sessions/history</summary>
    private static void MapGetTrackingHistoryEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/sessions/history", async (
                ICurrentUser currentUser,
                ISender sender,
                CancellationToken cancellationToken,
                Guid? userId = null,
                string? status = null,
                string? cursor = null,
                int pageSize = GetTrackingHistoryQuery.DefaultPageSize) =>
            {
                if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                {
                    return Result.Failure<TrackingHistoryPage>(
                            new Error("Tracking.Unauthorized", "Authentication is required."),
                            Outcome.Unauthorized)
                        .ToApiResult();
                }

                // If no userId param provided, default to caller's own id.
                var resolvedUserId = userId ?? currentUser.UserId.Value;

                SessionStatus[]? statuses = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    var parts = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var parsed = new List<SessionStatus>(parts.Length);
                    foreach (var part in parts)
                    {
                        if (!Enum.TryParse<SessionStatus>(part, ignoreCase: true, out var s) || !Enum.IsDefined(s))
                        {
                            return Result.Failure<TrackingHistoryPage>(
                                    new Error("Tracking.InvalidStatusFilter", $"Invalid session status: '{part}'."),
                                    Outcome.Invalid)
                                .ToApiResult();
                        }
                        parsed.Add(s);
                    }
                    statuses = parsed.ToArray();
                }

                var query = new GetTrackingHistoryQuery(resolvedUserId, statuses, cursor, pageSize);
                var result = await sender.Send(query, cancellationToken);
                return result.ToApiResult();
            })
            .WithName("GetTrackingHistory")
            .WithSummary("Get cursor-paginated tracking session history for a user.")
            .WithDescription(
                "Returns sessions ordered by StartedAt DESC. " +
                "Defaults to the caller's own sessions; admins may pass ?userId= to query any user. " +
                "Optional ?status= filter accepts comma-separated SessionStatus values. " +
                "Pass ?cursor= from the previous page's NextCursor to page forward. " +
                "Page size clamps to [1, 50] (default 20).")
            .Produces<TrackingHistoryPage>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithMetadata(new MustHavePermissionAttribute(TrackingFeatures.TrackingSession, AppAction.ReadOwn))
            .RequireAuthorization();
    }
}
