using ContentCore.Contracts.Attachments;
using ContentTours.Application.Queries.Tour.Common;
using ContentTours.Domain.Enums;
using ContentTours.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Queries.Tour.GetTourImages;

public sealed class GetTourImagesQueryHandler(
    ITourRepository tourRepository,
    IPublicEntityImageReader imageReader,
    ILogger<GetTourImagesQueryHandler> logger)
    : IQueryHandler<GetTourImagesQuery, IReadOnlyList<TourImageDto>>
{
    private const string TourEntityType = "Tour";

    public async Task<Result<IReadOnlyList<TourImageDto>>> Handle(
        GetTourImagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tour = await tourRepository
                .GetByIdAsync(request.TourId, cancellationToken, asNoTracking: true)
                .ConfigureAwait(false);

            // Non-existent OR non-approved → 404 (no status/existence leakage).
            if (tour is null || tour.Status != TourStatus.Approved)
            {
                return Result<IReadOnlyList<TourImageDto>>.Failure(
                    new Error("Tour.NotFound", $"Tour '{request.TourId}' was not found."),
                    Outcome.NotFound);
            }

            var images = await imageReader
                .GetEntityImagesAsync(TourEntityType, tour.Id, cancellationToken)
                .ConfigureAwait(false);

            var dtos = images
                .Select(i => new TourImageDto(i.Url, i.ThumbnailUrl, i.SortOrder, i.IsPrimary))
                .ToList();

            logger.LogDebug(
                "GetTourImages: {Count} image(s) for approved tour {TourId}",
                dtos.Count, tour.Id);

            return Result<IReadOnlyList<TourImageDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TourImageDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
