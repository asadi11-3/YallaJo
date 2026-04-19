using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.Requests;
using YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Invitations;

public sealed class InvitationsApiClient
{
    private readonly ApiClient _api;
    public InvitationsApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<InviteUserResponse>> InviteAsync(
        InviteUserRequest request, CancellationToken ct = default)
        => _api.PostAsync<InviteUserResponse>("/api/v1/auth/invitations", request, ct);

    public Task<ApiResult> ResendAsync(
        ResendInviteRequest request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/auth/invitations/resend", request, ct);
}
