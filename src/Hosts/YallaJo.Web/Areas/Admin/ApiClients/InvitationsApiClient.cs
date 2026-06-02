using YallaJo.Web.Areas.Admin.Models.Invitations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class InvitationsApiClient
{
    private readonly IApiClient _api;
    public InvitationsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<InviteUserResponse>> InviteAsync(
        InviteUserRequest request, CancellationToken ct = default)
        => _api.PostAsync<InviteUserResponse>("/api/v1/auth/invitations", request, ct);

    public Task<ApiResult<IReadOnlyList<InvitableRoleOptionResponse>>> GetInvitableRolesAsync(
        CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<InvitableRoleOptionResponse>>("/api/v1/auth/invitations/roles", ct);

    public Task<ApiResult> ResendAsync(
        ResendInviteRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/invitations/resend", request, ct);
}
