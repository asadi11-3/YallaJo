using ContentPlaces.Application.Queries.Business.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Business.GetMyBusinesses;

public sealed record GetMyBusinessesQuery(int Page = 1, int PageSize = 20)
    : IQuery<IReadOnlyList<BusinessSummaryDto>>;

