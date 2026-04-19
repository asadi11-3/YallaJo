using ContentPlaces.Application.Queries.BusinessAmenity.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.BusinessAmenity.ListBusinessAmenities;

public sealed record ListBusinessAmenitiesQuery(
    Guid BusinessId,
    int Page = 1,
    int PageSize = 10)
    : IQuery<IReadOnlyList<BusinessAmenityDto>>;
