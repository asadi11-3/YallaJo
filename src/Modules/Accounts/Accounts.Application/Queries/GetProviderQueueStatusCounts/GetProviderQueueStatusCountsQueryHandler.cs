using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetProviderQueueStatusCounts;

public sealed class GetProviderQueueStatusCountsQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ILogger<GetProviderQueueStatusCountsQueryHandler> logger)
    : IQueryHandler<GetProviderQueueStatusCountsQuery, ProviderQueueStatusCountsDto>
{
    public async Task<Result<ProviderQueueStatusCountsDto>> Handle(
        GetProviderQueueStatusCountsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var counts = await providerApplicationRepository
                .GetStatusCountsAsync(cancellationToken)
                .ConfigureAwait(false);

            var dto = new ProviderQueueStatusCountsDto(
                Pending: counts.GetValueOrDefault(ProviderApplicationStatus.Pending),
                AwaitingDocuments: counts.GetValueOrDefault(ProviderApplicationStatus.MoreDocsNeeded),
                Approved: counts.GetValueOrDefault(ProviderApplicationStatus.Approved),
                Suspended: counts.GetValueOrDefault(ProviderApplicationStatus.Suspended),
                Rejected: counts.GetValueOrDefault(ProviderApplicationStatus.Rejected));

            logger.LogDebug("Provider queue status counts fetched");

            return Result<ProviderQueueStatusCountsDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<ProviderQueueStatusCountsDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
