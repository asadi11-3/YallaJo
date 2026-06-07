using YallaJo.Web.Areas.Accounts.Models.JoinRequests;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class JoinRequestsApiClient
{
    private readonly IApiClient _api;

    public JoinRequestsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<JoinRequestResponse>>> GetMyJoinRequestsAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<JoinRequestResponse>>("/api/v1/booking/join-requests?myRequestsOnly=true", ct);

    public Task<ApiResult<Guid>> SubmitAsync(SubmitJoinRequestApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync<Guid>("/api/v1/booking/join-requests", request, ct);
}
