using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.AdminListApplications;

public sealed class AdminListCreatorApplicationsQueryHandler(
    ICreatorApplicationRepository applicationRepository,
    ILogger<AdminListCreatorApplicationsQueryHandler> logger)
    : IQueryHandler<AdminListCreatorApplicationsQuery, PaginatedResult<CreatorApplicationSummaryDto>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PaginatedResult<CreatorApplicationSummaryDto>>> Handle(
        AdminListCreatorApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var search = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : request.Search.Trim().ToLowerInvariant();

            var paged = await applicationRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    selector: app => new CreatorApplicationSummaryDto(
                        app.Id,
                        app.ApplicantUserId,
                        app.Status,
                        app.Source,
                        app.ReapplicationCount,
                        app.CreatedAt),
                    filter: app =>
                        (request.Status == null || app.Status == request.Status.Value)
                        && (search == null
                            || (app.Bio != null && app.Bio.Contains(search))),
                    orderBy: q => q.OrderByDescending(a => a.CreatedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "AdminListCreatorApplications: page={Page} size={Size} total={Total}",
                page, pageSize, paged.TotalCount);

            return Result<PaginatedResult<CreatorApplicationSummaryDto>>.Success(paged);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<PaginatedResult<CreatorApplicationSummaryDto>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
