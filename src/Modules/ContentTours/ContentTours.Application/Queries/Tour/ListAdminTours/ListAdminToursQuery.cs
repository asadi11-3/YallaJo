using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;

namespace ContentTours.Application.Queries.Tour.ListAdminTours;

public sealed record ListAdminToursQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? Sort = null)
    : IQuery<PaginatedResult<AdminTourSummaryDto>>;
