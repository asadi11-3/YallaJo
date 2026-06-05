using YallaJo.Web.Areas.Guide.Models.JoinRequests;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>
/// Talks to the Booking module join-request endpoints under
/// <c>/api/v1/booking/join-requests</c>.
/// </summary>
public sealed class JoinRequestsApiClient
{
    private const string JoinRequestsBase = "/api/v1/booking/join-requests";

    private readonly IApiClient _api;

    public JoinRequestsApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// GET /api/v1/booking/join-requests — list join requests for the current guide's tours.
    /// </summary>
    public Task<ApiResult<List<JoinRequestResponse>>> GetJoinRequestsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<JoinRequestResponse>>(JoinRequestsBase, ct);

    /// <summary>
    /// POST /api/v1/booking/join-requests/{id}/approve
    /// </summary>
    public Task<ApiResult> ApproveAsync(Guid id, ApproveJoinRequestRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{JoinRequestsBase}/{id}/approve", request, ct);

    /// <summary>
    /// POST /api/v1/booking/join-requests/{id}/reject
    /// </summary>
    public Task<ApiResult> RejectAsync(Guid id, RejectJoinRequestRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{JoinRequestsBase}/{id}/reject", request, ct);
}
