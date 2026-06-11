using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Guide.Models.Agency;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class AgencyApiClient
{
    private const string GuidesBase = "/api/v1/guides";
    private const string AgencyBase = "/api/v1/agency";

    private readonly IApiClient _api;

    public AgencyApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<AgencyInvitationResponse>>> GetMyInvitationsAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<AgencyInvitationResponse>>($"{GuidesBase}/me/invitations", ct);

    /// <summary>
    /// GET /api/v1/agency?page&amp;pageSize — public agency directory, used to feed the
    /// F10 agency picker on the apply form (UserId is the agencyUserId the apply route expects).
    /// </summary>
    public Task<ApiResult<GetAgenciesResponse>> GetAgenciesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString(AgencyBase, new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return _api.GetAsync<GetAgenciesResponse>(url, ct);
    }

    public Task<ApiResult> ApplyToAgencyAsync(Guid agencyUserId, ApplyToAgencyRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{GuidesBase}/agencies/{agencyUserId}/apply", req, ct);

    public Task<ApiResult> AcceptInvitationAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"{GuidesBase}/invitations/{id}/accept", null, ct);

    public Task<ApiResult> DeclineInvitationAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"{GuidesBase}/invitations/{id}/decline", null, ct);

    public Task<ApiResult> LeaveAgencyAsync(CancellationToken ct = default) =>
        _api.DeleteAsync($"{GuidesBase}/me/agency", ct);
}
