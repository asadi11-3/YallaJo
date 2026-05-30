using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.GetNearbyBusinesses;

public sealed class GetNearbyBusinessesQueryHandler(
    IBusinessRepository businessRepository,
    ILogger<GetNearbyBusinessesQueryHandler> logger)
    : IQueryHandler<GetNearbyBusinessesQuery, IReadOnlyList<NearbyBusinessSummaryDto>>
{
    public async Task<Result<IReadOnlyList<NearbyBusinessSummaryDto>>> Handle(
        GetNearbyBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var rawResults = await businessRepository.GetNearbyAsync(
            request.Lat, request.Lng, request.RadiusKm, request.PageSize, cancellationToken);

        var dtos = rawResults
            .Select(r => new NearbyBusinessSummaryDto(
                r.Id,
                r.Name,
                r.Slug,
                ((BusinessType)r.BusinessType).ToString(),
                r.Latitude,
                r.Longitude,
                r.City,
                r.Country,
                r.AverageRating,
                r.ReviewCount,
                r.IsVerified,
                r.IsFeatured,
                r.DistanceKm))
            .ToList();

        logger.LogInformation(
            "Found {Count} nearby businesses for ({Lat},{Lng}) within {Radius}km",
            dtos.Count, request.Lat, request.Lng, request.RadiusKm);

        return Result<IReadOnlyList<NearbyBusinessSummaryDto>>.Success(dtos);
    }
}
