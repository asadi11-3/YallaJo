using YallaJo.Web.Areas.Admin.Models.Disputes;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class DisputesApiClient(IApiClient api)
{
    private const string Base = "/api/v1/disputes";
    private readonly IApiClient _api = api;

    public Task<ApiResult<IReadOnlyList<DisputeResponse>>> GetOpenAsync(CancellationToken ct) =>
        _api.GetAsync<IReadOnlyList<DisputeResponse>>($"{Base}/admin/open", ct);

    public Task<ApiResult> ReviewAsync(Guid id, CancellationToken ct) =>
        _api.PostAsync($"{Base}/{id:D}/review", null, ct);

    public Task<ApiResult> ResolveAsync(Guid id, string resolution, string? notes, CancellationToken ct) =>
        _api.PostAsync($"{Base}/{id:D}/resolve", new { resolution, notes }, ct);

    public Task<ApiResult> EscalateAsync(Guid id, string reason, CancellationToken ct) =>
        _api.PostAsync($"{Base}/{id:D}/escalate", new { reason }, ct);
}
