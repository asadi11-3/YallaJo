using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetProviderAnalytics;

public sealed class GetProviderAnalyticsQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetProviderAnalyticsQueryHandler> logger) : IQueryHandler<GetProviderAnalyticsQuery, ProviderAnalyticsDto>
{
    public async Task<Result<ProviderAnalyticsDto>> Handle(GetProviderAnalyticsQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read provider analytics {ProviderId}", request.ProviderId);
        return Result.Success(await reader.GetProviderAnalyticsAsync(request.ProviderId, request.From, request.To, ct));
    }
}
