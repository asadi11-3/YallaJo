using ContentCore.Contracts.Attachments;
using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.GetPlaceImages;

public sealed class GetPlaceImagesQueryHandler(
    IPlaceRepository placeRepository,
    IPublicEntityImageReader imageReader,
    ILogger<GetPlaceImagesQueryHandler> logger)
    : IQueryHandler<GetPlaceImagesQuery, IReadOnlyList<PlaceImageDto>>
{
    private const string PlaceEntityType = "Place";

    public async Task<Result<IReadOnlyList<PlaceImageDto>>> Handle(
        GetPlaceImagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var place = await placeRepository.GetAsync(
                filter:       p => p.Id == request.PlaceId,
                include:      null,
                asNoTracking: true,
                ct:           cancellationToken);

            if (place is null)
            {
                return Result<IReadOnlyList<PlaceImageDto>>.Failure(
                    new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
                    Outcome.NotFound);
            }

            var images = await imageReader
                .GetEntityImagesAsync(PlaceEntityType, place.Id, cancellationToken)
                .ConfigureAwait(false);

            var dtos = images
                .Select(i => new PlaceImageDto(i.Url, i.ThumbnailUrl, i.SortOrder, i.IsPrimary))
                .ToList();

            logger.LogDebug(
                "GetPlaceImages: {Count} image(s) for place {PlaceId}",
                dtos.Count, place.Id);

            return Result<IReadOnlyList<PlaceImageDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<PlaceImageDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
