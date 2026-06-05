using YallaJo.Web.Areas.Guide.Models.Analytics;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class AnalyticsApiClient
{
    private const string AnalyticsBase = "/api/v1/guides/me/analytics";

    private readonly IApiClient _api;

    public AnalyticsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<GuideBookingOverviewResponse>> GetOverviewAsync(CancellationToken ct = default) =>
        _api.GetAsync<GuideBookingOverviewResponse>($"{AnalyticsBase}/overview", ct);

    public Task<ApiResult<List<BookingTrendResponse>>> GetBookingTrendsAsync(
        string granularity,
        int months,
        CancellationToken ct = default) =>
        _api.GetAsync<List<BookingTrendResponse>>(
            $"{AnalyticsBase}/booking-trends?granularity={Uri.EscapeDataString(granularity)}&months={months}",
            ct);

    public Task<ApiResult<List<PopularTourResponse>>> GetPopularToursAsync(int limit, CancellationToken ct = default) =>
        _api.GetAsync<List<PopularTourResponse>>($"{AnalyticsBase}/popular-tours?limit={limit}", ct);

    public Task<ApiResult<List<PeakDayStatResponse>>> GetPeakDaysAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<PeakDayStatResponse>>($"{AnalyticsBase}/peak-days", ct);
}
