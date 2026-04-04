using ContentPlaces.Application.Queries.Place.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Place.GetPlaceById;

public sealed class GetPlaceByIdQueryHandler(
    IPlaceRepository placeRepository,
    ILogger<GetPlaceByIdQueryHandler> logger)
    : IQueryHandler<GetPlaceByIdQuery, PlaceDetailDto>
{
    public async Task<Result<PlaceDetailDto>> Handle(
        GetPlaceByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var place = await placeRepository.GetAsync(
                filter:      p => p.Id == request.PlaceId,
                include:     q => q.Include(p => p.PlaceTranslations),
                asNoTracking: true,
                ct:          cancellationToken);

            if (place is null)
            {
                return Result<PlaceDetailDto>.Failure(
                   new Error("Place.NotFound", $"Place '{request.PlaceId}' was not found."),
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
