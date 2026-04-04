using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.GetPlaceBySlug;

public sealed class GetPlaceBySlugQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<GetPlaceBySlugQueryHandler> logger)
    : IQueryHandler<GetPlaceBySlugQuery, PlaceDetailDto>
{
    public async Task<Result<PlaceDetailDto>> Handle(
        GetPlaceBySlugQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var place = await placeRepository.GetAsync(
                filter:      p => p.Slug == request.Slug,
                include:     q => q.Include(p => p.PlaceTranslations),
                asNoTracking: true,
                ct:          cancellationToken);

            if (place is null)
            {
                return Result<PlaceDetailDto>.Failure(
                  new Error("Place.NotFound", $"Place with slug '{request.Slug}' was not found."),
                  Outcome.NotFound);
            }

            return Result<PlaceDetailDto>.Success(PlaceDetailDto.From(place));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PlaceDetailDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
