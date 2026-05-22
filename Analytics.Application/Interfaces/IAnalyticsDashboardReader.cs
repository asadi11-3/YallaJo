namespace Analytics.Application.Interfaces;

public interface IAnalyticsDashboardReader
{
    Task<AdminDashboardOverviewDto> GetAdminOverviewAsync(CancellationToken ct);
    Task<AdminRevenueDashboardDto> GetAdminRevenueAsync(DateTime? from, DateTime? to, CancellationToken ct);
    Task<AdminBookingsDashboardDto> GetAdminBookingsAsync(DateTime? from, DateTime? to, CancellationToken ct);
    Task<AdminUsersDashboardDto> GetAdminUsersAsync(DateTime? from, DateTime? to, CancellationToken ct);
    Task<ProviderDashboardDto> GetProviderDashboardAsync(Guid providerId, CancellationToken ct);
    Task<ProviderAnalyticsDto> GetProviderAnalyticsAsync(Guid providerId, DateTime? from, DateTime? to, CancellationToken ct);
    Task<CursorPageDto<ProviderTourListItemDto>> GetProviderToursAsync(Guid providerId, long? afterId, int pageSize, CancellationToken ct);
}
