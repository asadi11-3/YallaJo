using YallaJo.Web.Areas.Auth.Features.AcceptInvite.Requests;
using YallaJo.Web.Areas.Auth.Features.AcceptInvite.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Auth.Features.AcceptInvite;

public sealed class AcceptInviteApiClient
{
    private readonly ApiClient _api;
    public AcceptInviteApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<AcceptInviteResponse>> AcceptAsync(
        AcceptInviteRequest request, CancellationToken ct = default)
        => _api.PostAsync<AcceptInviteResponse>("/api/v1/auth/invitations/accept", request, ct);
}
