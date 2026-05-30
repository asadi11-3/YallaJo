using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetAdminBookingsDashboard;

public sealed class GetAdminBookingsDashboardQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetAdminBookingsDashboardQueryHandler> logger) : IQueryHandler<GetAdminBookingsDashboardQuery, AdminBookingsDashboardDto>
{
    public async Task<Result<AdminBookingsDashboardDto>> Handle(GetAdminBookingsDashboardQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read admin bookings dashboard");
        return Result.Success(await reader.GetAdminBookingsAsync(request.From, request.To, ct));
    }
}
