using ContentPlaces.Application.Caching;
using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.Business.GetMyBusinesses;

public sealed class GetMyBusinessesQueryHandler(
    IBusinessRepository businessRepository,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<GetMyBusinessesQueryHandler> logger)
    : IQueryHandler<GetMyBusinessesQuery, IReadOnlyList<BusinessSummaryDto>>
{
    public async Task<Result<IReadOnlyList<BusinessSummaryDto>>> Handle(
        GetMyBusinessesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var cacheKey = ContentPlacesCacheKeys.MyBusinesses(userId, request.Page, request.PageSize);
        var tags = new[] { ContentPlacesCacheKeys.MyBusinessesTag(userId), ContentPlacesCacheKeys.TagBusinesses };

        var businesses = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var results = await businessRepository.GetByOwnerIdAsync(userId, request.Page, request.PageSize, ct);
                return results.Select(b => new BusinessSummaryDto(
                    b.Id,
                    b.Name,
                    b.Slug,
                    b.BusinessType.ToString(),
                    b.Status.ToString(),
                    (double)b.Location.Latitude,
                    (double)b.Location.Longitude,
                    b.City,
                    b.Country,
                    b.AverageRating,
                    b.ReviewCount,
                    b.IsVerified,
                    b.IsFeatured,
                    null))
                    .ToList();
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            tags: tags,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} businesses for owner {UserId}",
            businesses.Count, userId);

        return Result<IReadOnlyList<BusinessSummaryDto>>.Success(businesses);
    }
}
