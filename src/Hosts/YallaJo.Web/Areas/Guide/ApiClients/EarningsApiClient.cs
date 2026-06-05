using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Models.Earnings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class EarningsApiClient
{
    private const string EarningsBase = "/api/v1/guides/me/earnings";

    private readonly IApiClient _api;

    public EarningsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<GuideEarningsSummaryResponse>> GetSummaryAsync(CancellationToken ct = default) =>
        _api.GetAsync<GuideEarningsSummaryResponse>($"{EarningsBase}/summary", ct);

    public Task<ApiResult<List<GuideEarningByTourResponse>>> GetByTourAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<GuideEarningByTourResponse>>($"{EarningsBase}/by-tour", ct);

    public Task<ApiResult<PaginatedResponse<GuideEarningHistoryItemResponse>>> GetHistoryAsync(
        int page,
        int pageSize,
        CancellationToken ct = default) =>
        _api.GetAsync<PaginatedResponse<GuideEarningHistoryItemResponse>>(
            $"{EarningsBase}/history?page={page}&pageSize={pageSize}", ct);
}
