using ContentTours.Application.Queries.TourPackage.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentTours.Application.Queries.TourPackage.ListTourPackages;

public sealed record ListTourPackagesQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null,
    Guid? TourId = null) : IQuery<PaginatedResult<TourPackageDto>>;
