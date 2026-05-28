using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Dashboard;

public sealed class GetProviderDashboardOverviewQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ICurrentUser currentUser,
    HybridCache cache,
    ILogger<GetProviderDashboardOverviewQueryHandler> logger)
    : IQueryHandler<GetProviderDashboardOverviewQuery, ProviderDashboardOverviewResult>
{
    public async Task<Result<ProviderDashboardOverviewResult>> Handle(
        GetProviderDashboardOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var result = await cache.GetOrCreateAsync(
            AccountsCacheKeys.MyApplicationStatus(userId),
            async ct =>
            {
                var application = await providerApplicationRepository.GetWithDocumentsByUserIdAsync(userId, ct);
                if (application is null)
                    return null;

                var now = DateTime.UtcNow;
                var totalDocs = application.Documents.Count;
                var expiredDocs = application.Documents.Count(d => d.ExpiresAt.HasValue && d.ExpiresAt.Value < now);
                var expiringIn30 = application.Documents.Count(d =>
                    d.ExpiresAt.HasValue && d.ExpiresAt.Value >= now && d.ExpiresAt.Value <= now.AddDays(30));

                // Count pending actions: docs expired/expiring + pending resubmit window
                var pendingActions = expiredDocs + expiringIn30;
                if (application.Status == ProviderApplicationStatus.MoreDocsNeeded)
                    pendingActions++;

                return new ProviderDashboardOverviewResult(
                    ApplicationId: application.Id,
                    ProviderType: application.Type,
                    Status: application.Status,
                    BusinessName: application.BusinessName,
                    TotalDocuments: totalDocs,
                    ExpiredDocuments: expiredDocs,
                    ExpiringIn30DaysDocuments: expiringIn30,
                    PendingActionsCount: pendingActions,
                    ReviewDeadline: null, // SLA tracking lives on Business entity in ContentPlaces
                    IsApproved: application.Status == ProviderApplicationStatus.Approved);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
            tags: [AccountsCacheKeys.MyApplicationStatusTag(userId)],
            cancellationToken: cancellationToken);

        if (result is null)
        {
            logger.LogWarning("Dashboard overview requested for user {UserId} but no provider application found", userId);
            return Result<ProviderDashboardOverviewResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);
        }

        return Result<ProviderDashboardOverviewResult>.Success(result);
    }
}
