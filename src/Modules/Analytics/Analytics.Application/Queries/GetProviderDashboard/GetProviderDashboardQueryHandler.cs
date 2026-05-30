using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetProviderDashboard;

public sealed class GetProviderDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetProviderDashboardQueryHandler> logger) : IQueryHandler<GetProviderDashboardQuery, ProviderDashboardDto>
{
    public async Task<Result<ProviderDashboardDto>> Handle(GetProviderDashboardQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read provider dashboard {ProviderId}", request.ProviderId);
        return Result.Success(await reader.GetProviderDashboardAsync(request.ProviderId, ct));
    }
}
