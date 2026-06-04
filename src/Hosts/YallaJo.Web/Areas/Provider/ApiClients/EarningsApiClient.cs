using YallaJo.Web.Areas.Provider.Models.Earnings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class EarningsApiClient
{
    private readonly IApiClient _api;

    public EarningsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<GuideEarningsSummaryResponse>> GetSummaryAsync(CancellationToken ct = default)
        => _api.GetAsync<GuideEarningsSummaryResponse>("/api/v1/finance/guide/summary", ct);

    public Task<ApiResult<List<GuideEarningResponse>>> GetEarningsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<GuideEarningResponse>>("/api/v1/finance/guide", ct);

    public Task<ApiResult<PayoutPageResponse>> GetPayoutsAsync(CancellationToken ct = default)
        => _api.GetAsync<PayoutPageResponse>("/api/v1/payouts/provider?pageSize=20", ct);

    public Task<ApiResult<List<DisputeResponse>>> GetDisputesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<DisputeResponse>>("/api/v1/disputes/my", ct);
}
