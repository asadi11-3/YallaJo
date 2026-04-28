using ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;
using ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;
using ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;
using ContentTours.Application.Queries.TourSchedule.Common;
using ContentTours.Application.Queries.TourSchedule.ListTourSchedules;
using ContentTours.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation;

public static class TourScheduleEndpoints
{
    public static void MapTourScheduleEndpoints(RouteGroupBuilder group)
    {
        var schedules = group.MapGroup("/{id:guid}/schedules")
            .WithTags("ContentTours | Schedules");

        // ── GET /api/v1/tours/{id}/schedules ─────────────────────────────────
        schedules.MapGet("/", async (
            Guid id,
            bool? activeOnly,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListTourSchedulesQuery(id, activeOnly ?? true), ct);
            return result.ToApiResult();
        })
        .WithName("ListTourSchedules")
        .WithSummary("List schedules for a tour")
        .Produces<IReadOnlyList<TourScheduleDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── POST /api/v1/tours/{id}/schedules ────────────────────────────────
        schedules.MapPost("/", async (
            Guid id,
            CreateTourScheduleRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourScheduleCommand(
                id,
                request.DaysOfWeek,
                request.StartTime,
                request.EndTime,
                request.IsActive);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("CreateTourSchedule")
        .WithSummary("Create tour schedules via recurrence pattern")
        .Produces<CreateTourScheduleResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update));

        // ── PUT /api/v1/tours/{id}/schedules/{scheduleId} ────────────────────
        schedules.MapPut("/{scheduleId:guid}", async (
            Guid id,
            Guid scheduleId,
            UpdateTourScheduleRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateTourScheduleCommand(
                id, scheduleId,
                request.DayOfWeek, request.StartTime, request.EndTime, request.IsActive);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateTourSchedule")
        .WithSummary("Update a tour schedule")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update));

        // ── DELETE /api/v1/tours/{id}/schedules/{scheduleId} ─────────────────
        schedules.MapDelete("/{scheduleId:guid}", async (
            Guid id,
            Guid scheduleId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteTourScheduleCommand(id, scheduleId), ct);
            return result.ToApiResult();
        })
        .WithName("DeleteTourSchedule")
        .WithSummary("Delete a tour schedule (blocked if future bookings exist)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update));
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateTourScheduleRequest(
    /// <summary>Days of week to schedule (0=Sunday, 1=Monday … 6=Saturday). At least one required.</summary>
    List<byte> DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive = true);

public sealed record UpdateTourScheduleRequest(
    byte DayOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive);
