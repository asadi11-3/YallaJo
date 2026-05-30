using Booking.Application.Interfaces;
using Booking.Application.Queries.GetAvailabilityForTour;
using Booking.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetAvailabilityForTourOnDate;

public sealed class GetAvailabilityForTourOnDateQueryHandler(
    IAvailabilitySlotRepository availabilitySlotRepository,
    IBookingTourSnapshotReader tourSnapshotReader,
    ILogger<GetAvailabilityForTourOnDateQueryHandler> logger)
    : IQueryHandler<GetAvailabilityForTourOnDateQuery, AvailabilityForDateDto>
{
    public async Task<Result<AvailabilityForDateDto>> Handle(
        GetAvailabilityForTourOnDateQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourSnapshotReader.GetByIdAsync(request.TourId, cancellationToken).ConfigureAwait(false);
            if (tour is null || !tour.IsActive)
            {
                return Result.Failure<AvailabilityForDateDto>(
                    new Error("Tour.NotFound", "Tour is unavailable."),
                    Outcome.NotFound);
            }

            var slots = await availabilitySlotRepository
                .GetActiveSlotsForTourOnDateAsync(request.TourId, request.Date, cancellationToken)
                .ConfigureAwait(false);

            var dto = new AvailabilityForDateDto(
                Slots: slots
                    .Select(s => new AvailabilitySlotListItemDto(s.Id, s.StartTime, s.EndTime, s.AvailableCount))
                    .ToList());

            logger.LogInformation(
                "Availability on date for tour {TourId} {Date}: {Count} slot(s)",
                request.TourId,
                request.Date,
                dto.Slots.Count);

            return Result.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<AvailabilityForDateDto>(
                new Error("Request.Cancelled", "The request was cancelled by the client."),
                Outcome.Canceled);
        }
    }
}
