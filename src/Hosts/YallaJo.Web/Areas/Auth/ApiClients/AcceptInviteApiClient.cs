using YallaJo.Web.Areas.Auth.Models.AcceptInvite;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.ApiClients;
public sealed class AcceptInviteApiClient
{
    private readonly IApiClient _api;
    public AcceptInviteApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<AcceptInviteResponse>> AcceptAsync(
        AcceptInviteRequest request, CancellationToken ct = default)
        => _api.PostAsync<AcceptInviteResponse>("/api/v1/auth/invitations/accept", request, ct);
}
