using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.AdminApplicationStatusCounts;

public sealed class AdminCreatorApplicationStatusCountsQueryHandler(
    ICreatorApplicationRepository applicationRepository,
    ILogger<AdminCreatorApplicationStatusCountsQueryHandler> logger)
    : IQueryHandler<AdminCreatorApplicationStatusCountsQuery, CreatorApplicationStatusCountsDto>
{
    public async Task<Result<CreatorApplicationStatusCountsDto>> Handle(
        AdminCreatorApplicationStatusCountsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var counts = await applicationRepository
                .GetStatusCountsAsync(cancellationToken)
                .ConfigureAwait(false);

            var dto = new CreatorApplicationStatusCountsDto(
                Draft: counts.GetValueOrDefault(CreatorApplicationStatus.Draft),
                Pending: counts.GetValueOrDefault(CreatorApplicationStatus.Pending),
                Approved: counts.GetValueOrDefault(CreatorApplicationStatus.Approved),
                Rejected: counts.GetValueOrDefault(CreatorApplicationStatus.Rejected),
                MoreInfoNeeded: counts.GetValueOrDefault(CreatorApplicationStatus.MoreInfoNeeded));

            logger.LogDebug(
                "AdminCreatorApplicationStatusCounts: pending={Pending} moreInfo={MoreInfo}",
                dto.Pending, dto.MoreInfoNeeded);

            return Result<CreatorApplicationStatusCountsDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreatorApplicationStatusCountsDto>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
