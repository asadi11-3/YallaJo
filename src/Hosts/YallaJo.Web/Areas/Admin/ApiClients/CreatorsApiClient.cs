using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Creators;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class CreatorsApiClient
{
    private const string Base = "/api/v1/blogs/admin/creators";
    private readonly IApiClient _api;

    public CreatorsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<CreatorApplicationPageResponse>> GetApplicationsAsync(
        string? status, int page, int pageSize, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (!string.IsNullOrWhiteSpace(status))
        {
            query["status"] = status;
        }

        var url = QueryHelpers.AddQueryString($"{Base}/applications", query);
        return _api.GetAsync<CreatorApplicationPageResponse>(url, ct);
    }

    // GET /api/v1/blogs/admin/creators/applications/status-counts — counted queue tabs (§5.6)
    public Task<ApiResult<CreatorApplicationStatusCountsResponse>> GetStatusCountsAsync(CancellationToken ct)
        => _api.GetAsync<CreatorApplicationStatusCountsResponse>($"{Base}/applications/status-counts", ct);

    public Task<ApiResult<CreatorApplicationDetailResponse>> GetApplicationAsync(Guid id, CancellationToken ct)
        => _api.GetAsync<CreatorApplicationDetailResponse>($"{Base}/applications/{id:D}", ct);

    public Task<ApiResult> ApproveAsync(Guid id, string displayName, string? avatarUrl, CancellationToken ct)
        => _api.PostAsync($"{Base}/applications/{id:D}/approve", new { displayName, avatarUrl }, ct);

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct)
        => _api.PostAsync($"{Base}/applications/{id:D}/reject", new { reason }, ct);

    public Task<ApiResult> RequestMoreInfoAsync(Guid id, string adminNote, CancellationToken ct)
        => _api.PostAsync($"{Base}/applications/{id:D}/request-more-info", new { adminNote }, ct);

    public Task<ApiResult> SuspendAsync(Guid profileId, string reason, CancellationToken ct)
        => _api.PostAsync($"{Base}/profiles/{profileId:D}/suspend", new { reason }, ct);

    public Task<ApiResult> ReinstateAsync(Guid profileId, CancellationToken ct)
        => _api.PostAsync($"{Base}/profiles/{profileId:D}/reinstate", null, ct);

    // ── §8.6: tier management, edit, delete, invitations ───────────────────────

    // POST /profiles/{profileId}/promote — body { targetTier }
    public Task<ApiResult> PromoteAsync(Guid profileId, string targetTier, CancellationToken ct)
        => _api.PostAsync($"{Base}/profiles/{profileId:D}/promote", new { targetTier }, ct);

    // POST /profiles/{profileId}/demote — body { targetTier, reason }
    public Task<ApiResult> DemoteAsync(Guid profileId, string targetTier, string reason, CancellationToken ct)
        => _api.PostAsync($"{Base}/profiles/{profileId:D}/demote", new { targetTier, reason }, ct);

    // PUT /profiles/{id} — body { displayName, bio, avatarUrl, slug }
    public Task<ApiResult> EditAsync(Guid id, object body, CancellationToken ct)
        => _api.PutAsync($"{Base}/profiles/{id:D}", body, ct);

    // DELETE /profiles/{id} — body { reason }
    public Task<ApiResult> DeleteAsync(Guid id, string reason, CancellationToken ct)
        => _api.DeleteAsync($"{Base}/profiles/{id:D}", new { reason }, ct);

    // POST /invitations — body { kind, email, invitedUserId, personalMessage }
    public Task<ApiResult> SendInvitationAsync(object body, CancellationToken ct)
        => _api.PostAsync($"{Base}/invitations", body, ct);
}
