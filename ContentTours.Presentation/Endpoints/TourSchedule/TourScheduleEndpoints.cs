using ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;
using ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;
using ContentTours.Application.Commands.TourSchedule.UpdateTourSchedule;
using ContentTours.Application.Queries.TourSchedule.Common;
using ContentTours.Application.Queries.TourSchedule.ListTourSchedules;
using ContentTours.Contracts.Authorization;
using ContentTours.Presentation.Endpoints.TourSchedule.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentTours.Presentation.Endpoints.TourSchedule;

internal static class TourScheduleEndpoints
{
    internal static void MapTourScheduleEndpoints(RouteGroupBuilder group)
    {
        var schedules = group.MapGroup("/{id:guid}/schedules")
            .WithTags("ContentTours | Schedules");

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

        schedules.MapPost("/", async (
            Guid id,
            CreateTourScheduleRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateTourScheduleCommand(
                TourId: id,
                Pattern: request.Pattern,
                DaysOfWeek: request.DaysOfWeek,
                CustomDates: request.CustomDates,
                StartTime: request.StartTime,
                EndTime: request.EndTime,
                ValidFrom: request.ValidFrom,
                ValidTo: request.ValidTo,
                IsActive: request.IsActive);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("CreateTourSchedule")
        .WithSummary("Create tour schedules via recurrence pattern (Once/Daily/Weekly/Custom; 90-day cap; 120-row limit)")
        .Produces<CreateTourScheduleResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentToursFeatures.Tour, AppAction.Update));

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
