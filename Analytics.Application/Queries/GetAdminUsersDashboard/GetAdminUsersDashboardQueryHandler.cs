using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetAdminUsersDashboard;

public sealed class GetAdminUsersDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminUsersDashboardQueryHandler> logger) : IQueryHandler<GetAdminUsersDashboardQuery, AdminUsersDashboardDto>
{
    public async Task<Result<AdminUsersDashboardDto>> Handle(GetAdminUsersDashboardQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read admin users dashboard");
        return Result.Success(await reader.GetAdminUsersAsync(request.From, request.To, ct));
    }
}
