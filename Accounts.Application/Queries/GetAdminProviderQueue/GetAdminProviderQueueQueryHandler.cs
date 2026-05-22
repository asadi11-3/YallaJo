using Accounts.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetAdminProviderQueue;

public sealed class GetAdminProviderQueueQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ILogger<GetAdminProviderQueueQueryHandler> logger)
    : IQueryHandler<GetAdminProviderQueueQuery, GetAdminProviderQueueResult>
{
    public async Task<Result<GetAdminProviderQueueResult>> Handle(
        GetAdminProviderQueueQuery request,
        CancellationToken cancellationToken)
    {
        var items = await providerApplicationRepository.GetQueueAsync(
            request.StatusFilter,
            request.TypeFilter,
            request.Page,
            request.PageSize,
            cancellationToken);

        var total = await providerApplicationRepository.GetQueueCountAsync(
            request.StatusFilter,
            request.TypeFilter,
            cancellationToken);

        var summaries = items.Select(a => new ProviderApplicationSummary(
            ApplicationId: a.Id,
            UserId: a.UserId,
            Type: a.Type,
            BusinessName: a.BusinessName,
            ContactEmail: a.ContactEmail,
            Status: a.Status,
            SubmittedAt: a.SubmittedAt,
            ReviewedAt: a.ReviewedAt,
            DocumentCount: a.Documents.Count,
            ReapplicationCount: a.ReapplicationCount)).ToList();

        logger.LogDebug("Admin provider queue fetched: {Count} items, total: {Total}",
            summaries.Count, total);

        return Result<GetAdminProviderQueueResult>.Success(new GetAdminProviderQueueResult(
            Items: summaries,
            TotalCount: total,
            Page: request.Page,
            PageSize: request.PageSize));
    }
}
