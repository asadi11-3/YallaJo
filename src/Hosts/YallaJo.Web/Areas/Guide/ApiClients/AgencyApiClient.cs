using YallaJo.Web.Areas.Guide.Models.Agency;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class AgencyApiClient
{
    private const string GuidesBase = "/api/v1/guides";

    private readonly IApiClient _api;

    public AgencyApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<AgencyInvitationResponse>>> GetMyInvitationsAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<AgencyInvitationResponse>>($"{GuidesBase}/me/invitations", ct);

    public Task<ApiResult> ApplyToAgencyAsync(Guid agencyUserId, ApplyToAgencyRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{GuidesBase}/agencies/{agencyUserId}/apply", req, ct);

    public Task<ApiResult> AcceptInvitationAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"{GuidesBase}/invitations/{id}/accept", null, ct);

    public Task<ApiResult> DeclineInvitationAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"{GuidesBase}/invitations/{id}/decline", null, ct);

    public Task<ApiResult> LeaveAgencyAsync(CancellationToken ct = default) =>
        _api.DeleteAsync($"{GuidesBase}/me/agency", ct);
}
