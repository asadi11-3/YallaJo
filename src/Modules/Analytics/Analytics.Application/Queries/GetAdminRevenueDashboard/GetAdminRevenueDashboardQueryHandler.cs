using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetAdminRevenueDashboard;

public sealed class GetAdminRevenueDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminRevenueDashboardQueryHandler> logger) : IQueryHandler<GetAdminRevenueDashboardQuery, AdminRevenueDashboardDto>
{
    public async Task<Result<AdminRevenueDashboardDto>> Handle(GetAdminRevenueDashboardQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read admin revenue dashboard");
        return Result.Success(await reader.GetAdminRevenueAsync(request.From, request.To, ct));
    }
}
