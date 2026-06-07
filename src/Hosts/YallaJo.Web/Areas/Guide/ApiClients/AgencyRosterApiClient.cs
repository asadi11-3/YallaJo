using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Guide.Models.AgencyRoster;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

/// <summary>
/// Agency-OWNER roster management against the Accounts agency endpoints
/// (<c>/api/v1/agency/*</c>). Distinct from the guide-side self-service
/// <see cref="AgencyApiClient"/> (<c>/api/v1/guides/...</c>) — do not confuse these,
/// and never route this through the ContentTours guide-application endpoints.
/// The backend scopes every call to the caller's own agency (currentUser.UserId).
/// </summary>
public sealed class AgencyRosterApiClient
{
    private const string Base = "/api/v1/agency";

    private readonly IApiClient _api;

    public AgencyRosterApiClient(IApiClient api) => _api = api;

    // GET /api/v1/agency/guides — current affiliated guides
    public Task<ApiResult<List<AgencyGuideResponse>>> GetGuidesAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<AgencyGuideResponse>>($"{Base}/guides", ct);

    // GET /api/v1/agency/applications — guide applications to my agency
    public Task<ApiResult<List<AgencyApplicationResponse>>> GetApplicationsAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<AgencyApplicationResponse>>($"{Base}/applications", ct);

    // GET /api/v1/agency/invitations/sent — invitations my agency sent
    public Task<ApiResult<List<AgencySentInvitationResponse>>> GetSentInvitationsAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<AgencySentInvitationResponse>>($"{Base}/invitations/sent", ct);

    // GET /api/v1/agency/guides/available?page&pageSize — independent guides not affiliated
    public Task<ApiResult<List<AvailableGuideResponse>>> GetAvailableGuidesAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = QueryHelpers.AddQueryString($"{Base}/guides/available", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        });
        return _api.GetAsync<List<AvailableGuideResponse>>(url, ct);
    }

    // POST /api/v1/agency/guides/invite — invite an independent guide
    public Task<ApiResult> InviteGuideAsync(InviteGuideApiRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{Base}/guides/invite", req, ct);

    // POST /api/v1/agency/applications/{id}/approve
    public Task<ApiResult> ApproveApplicationAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"{Base}/applications/{id}/approve", null, ct);

    // POST /api/v1/agency/applications/{id}/reject
    public Task<ApiResult> RejectApplicationAsync(Guid id, RejectAgencyApplicationApiRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{Base}/applications/{id}/reject", req, ct);

    // DELETE /api/v1/agency/guides/{guideUserId} — body REQUIRED (reason)
    public Task<ApiResult> RemoveGuideAsync(Guid guideUserId, RemoveAgencyGuideApiRequest req, CancellationToken ct = default) =>
        _api.DeleteAsync($"{Base}/guides/{guideUserId}", req, ct);
}
