using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using ContentPlaces.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;

public sealed class ListBusinessAmenitiesQueryHandler(
    IContentPlacesDbContext dbContext,
    ILogger<ListBusinessAmenitiesQueryHandler> logger)
    : IQueryHandler<ListBusinessAmenitiesQuery, IReadOnlyList<BusinessAmenityDto>>
{
    public async Task<Result<IReadOnlyList<BusinessAmenityDto>>> Handle(
        ListBusinessAmenitiesQuery request,
        CancellationToken cancellationToken)
    {
        var amenities = await dbContext.BusinessAmenities
     .AsNoTracking()
     .Where(x => x.BusinessId == request.BusinessId)
     .OrderBy(x => x.SortOrder) // primary order
     .ThenBy(x => x.Name)       // stable order
     .Skip((request.Page - 1) * request.PageSize) // pagination ترقيم الصفحات
     .Take(request.PageSize) // pagination ترقيم الصفحات
     .Select(x => BusinessAmenityDto.From(x))
     .ToListAsync(cancellationToken);

        logger.LogInformation("Fetched amenities for business {BusinessId}", request.BusinessId);

        return Result<IReadOnlyList<BusinessAmenityDto>>.Success(amenities);
    }
}
