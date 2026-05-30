using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetAdminDashboardOverview;

public sealed class GetAdminDashboardOverviewQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminDashboardOverviewQueryHandler> logger) : IQueryHandler<GetAdminDashboardOverviewQuery, AdminDashboardOverviewDto>
{
    public async Task<Result<AdminDashboardOverviewDto>> Handle(GetAdminDashboardOverviewQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read admin dashboard overview");
        return Result.Success(await reader.GetAdminOverviewAsync(ct));
    }
}
