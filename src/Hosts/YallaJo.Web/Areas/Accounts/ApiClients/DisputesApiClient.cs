using YallaJo.Web.Areas.Accounts.Models.Disputes;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

/// <summary>
/// §3.8 Disputes — binds the two customer-facing Finance dispute endpoints.
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'ApiClient' suffix.
/// </summary>
public sealed class DisputesApiClient
{
    private const string Base = "/api/v1/disputes";

    private readonly IApiClient _api;

    public DisputesApiClient(IApiClient api) => _api = api;

    // GET /api/v1/disputes/my  → the current user's disputes (owner-scoped by the API).
    public Task<ApiResult<List<DisputeResponse>>> GetMyDisputesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<DisputeResponse>>($"{Base}/my", ct);

    // POST /api/v1/disputes  → open a dispute against a payment (UserId taken from JWT).
    public Task<ApiResult<DisputeResponse>> OpenAsync(OpenDisputeApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<DisputeResponse>(Base, request, ct);
}
