using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BusinessHoursEntity = ContentPlaces.Domain.Entities.BusinessHours;
using PlaceDayOfWeek = ContentPlaces.Domain.Enums.DayOfWeek;

namespace ContentPlaces.Application.Commands.BusinessHours.SetBusinessHours;

public sealed class SetBusinessHoursCommandHandler(
    IBusinessRepository businessRepository,
    IContentPlacesUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<SetBusinessHoursCommandHandler> logger)
    : ICommandHandler<SetBusinessHoursCommand>
{
    public async Task<Result> Handle(SetBusinessHoursCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken, asNoTracking: true);
            if (business is null)
            {
                return Result.Failure(
                    new Error("Business.NotFound", $"Business '{request.BusinessId}' was not found."),
                    Outcome.NotFound);
            }

            if (business.OwnerId != request.ActingUserId)
            {
                return Result.Failure(
                    new Error("Business.Forbidden", "Not the owner"),
                    Outcome.Forbidden);
            }

            var newHours = new List<BusinessHoursEntity>(request.Hours.Count);
            foreach (var entry in request.Hours)
            {
                var openTime = entry.IsClosed || string.IsNullOrWhiteSpace(entry.OpenTime)
                    ? TimeOnly.MinValue
                    : TimeOnly.Parse(entry.OpenTime, System.Globalization.CultureInfo.InvariantCulture);

                var closeTime = entry.IsClosed || string.IsNullOrWhiteSpace(entry.CloseTime)
                    ? TimeOnly.MinValue
                    : TimeOnly.Parse(entry.CloseTime, System.Globalization.CultureInfo.InvariantCulture);

                newHours.Add(BusinessHoursEntity.Create(
                    businessId: request.BusinessId,
                    dayOfWeek: (PlaceDayOfWeek)entry.DayOfWeek,
                    openTime: openTime,
                    closeTime: closeTime,
                    isClosed: entry.IsClosed));
            }

            await businessRepository.ReplaceBusinessHoursAsync(request.BusinessId, newHours, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict setting hours for business '{BusinessId}'.", request.BusinessId);
                return Result.Failure(
                    new Error("Business.ConcurrencyConflict", "The record was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            // Evict hours cache + detail cache (detail embeds hours)
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForBusinessHours(request.BusinessId), cancellationToken);
            await cache.RemoveByTagAsync(ContentPlacesCacheKeys.TagForBusiness(request.BusinessId), cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
