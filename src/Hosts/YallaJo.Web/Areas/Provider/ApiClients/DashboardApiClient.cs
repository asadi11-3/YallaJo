using YallaJo.Web.Areas.Provider.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class DashboardApiClient
{
    private readonly IApiClient _api;

    public DashboardApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ListMyToursResponse>> GetMyToursAsync(int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<ListMyToursResponse>($"/api/v1/tours/provider/my-tours?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<GuideEarningsSummaryResponse>> GetEarningsSummaryAsync(CancellationToken ct = default)
        => _api.GetAsync<GuideEarningsSummaryResponse>("/api/v1/finance/guide/summary", ct);

    public Task<ApiResult<List<JoinRequestResponse>>> GetJoinRequestsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<JoinRequestResponse>>("/api/v1/booking/join-requests?myRequestsOnly=false", ct);
}
