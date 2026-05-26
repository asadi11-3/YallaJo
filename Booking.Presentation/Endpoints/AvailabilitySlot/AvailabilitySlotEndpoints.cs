using Booking.Application.Commands.CreateAvailabilitySlot;
using Booking.Application.Commands.DeactivateAvailabilitySlot;
using Booking.Application.Commands.UpdateAvailabilitySlot;
using Booking.Application.Queries.GetAvailabilitySlots;
using Booking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Booking.Presentation.Endpoints.AvailabilitySlot;

/// <summary>
/// Availability slot endpoints mounted under <c>/api/v1/booking/slots</c>.
/// </summary>
internal static class AvailabilitySlotEndpoints
{
    internal static void MapAvailabilitySlotEndpoints(RouteGroupBuilder group)
    {
        // GET /api/v1/booking/slots — list slots (filter by tourId or guideId)
        group.MapGet("/", async (
                ISender sender,
                CancellationToken ct,
                Guid? tourId = null,
                Guid? tourGuideId = null,
                string? fromDate = null,
                string? toDate = null) =>
            {
                DateOnly? parsedFrom = null;
                DateOnly? parsedTo = null;

                if (fromDate is not null && !DateOnly.TryParse(fromDate, out var fd))
                {
                    return Results.BadRequest("Invalid fromDate format.");
                }
                else if (fromDate is not null)
                {
                    parsedFrom = DateOnly.Parse(fromDate);
                }

                if (toDate is not null && !DateOnly.TryParse(toDate, out var td))
                {
                    return Results.BadRequest("Invalid toDate format.");
                }
                else if (toDate is not null)
                {
                    parsedTo = DateOnly.Parse(toDate);
                }

                var result = await sender.Send(
                    new GetAvailabilitySlotsQuery(tourId, tourGuideId, parsedFrom, parsedTo),
                    ct);
                return result.ToApiResult();
            })
            .WithName("GetAvailabilitySlots")
            .WithSummary("List availability slots — filter by tourId or guideId.")
            .WithTags("Booking | Availability")
            .AllowAnonymous();

        // POST /api/v1/booking/slots — guide creates a slot
        group.MapPost("/", async (
                CreateAvailabilitySlotRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(request.ToCommand(), ct);
                return result.ToApiResult();
            })
            .WithName("CreateAvailabilitySlot")
            .WithSummary("Guide creates an availability slot for one of their tour offerings.")
            .WithTags("Booking | Availability")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Create))
            .RequireAuthorization();

        // PUT /api/v1/booking/slots/{id} — update slot details
        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateAvailabilitySlotRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(request.ToCommand(id), ct);
                return result.ToApiResult();
            })
            .WithName("UpdateAvailabilitySlot")
            .WithSummary("Guide updates an availability slot's date, time, capacity, or price override.")
            .WithTags("Booking | Availability")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Update))
            .RequireAuthorization();

        // DELETE /api/v1/booking/slots/{id} — deactivate a slot
        group.MapDelete("/{id:guid}", async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new DeactivateAvailabilitySlotCommand(id), ct);
                return result.ToApiResult();
            })
            .WithName("DeactivateAvailabilitySlot")
            .WithSummary("Guide deactivates an availability slot.")
            .WithTags("Booking | Availability")
            .WithMetadata(new MustHavePermissionAttribute(BookingFeatures.AvailabilitySlot, AppAction.Delete))
            .RequireAuthorization();
    }
}

/// <summary>Request body for updating an availability slot.</summary>
public sealed record UpdateAvailabilitySlotRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    decimal? PriceOverride,
    string? PriceOverrideCurrency)
{
    public UpdateAvailabilitySlotCommand ToCommand(Guid slotId)
        => new(slotId, Date, StartTime, EndTime, MaxCapacity, PriceOverride, PriceOverrideCurrency);
}

/// <summary>Request body for creating an availability slot.</summary>
public sealed record CreateAvailabilitySlotRequest(
    Guid TourId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxCapacity,
    decimal? PriceOverride,
    string? PriceOverrideCurrency)
{
    public CreateAvailabilitySlotCommand ToCommand()
        => new(TourId, Date, StartTime, EndTime, MaxCapacity, PriceOverride, PriceOverrideCurrency);
}
