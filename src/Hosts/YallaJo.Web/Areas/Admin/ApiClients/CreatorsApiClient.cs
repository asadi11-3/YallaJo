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
}
