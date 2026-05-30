using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;

public sealed class ListBusinessAmenitiesQueryHandler(
    IBusinessAmenityRepository amenityRepository,
    ILogger<ListBusinessAmenitiesQueryHandler> logger)
    : IQueryHandler<ListBusinessAmenitiesQuery, IReadOnlyList<BusinessAmenityDto>>
{
    public async Task<Result<IReadOnlyList<BusinessAmenityDto>>> Handle(
        ListBusinessAmenitiesQuery request,
        CancellationToken cancellationToken)
    {
        var amenities = await amenityRepository.SelectAsync(
            selector: x => BusinessAmenityDto.From(x),
            filter: x => x.BusinessId == request.BusinessId,
            orderBy: q => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name),
            ct: cancellationToken);

        logger.LogInformation("Fetched amenities for business {BusinessId}", request.BusinessId);

        return Result<IReadOnlyList<BusinessAmenityDto>>.Success(amenities);
    }
}
