using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.GuideApplications;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class GuideApplicationsApiClient
{
    private const string Base = "/api/v1/tours";

    private readonly IApiClient _api;

    public GuideApplicationsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<GuideApplicationPageResponse>> GetApplicationsAsync(
        Guid tourId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query["status"] = status;
        }

        var url = QueryHelpers.AddQueryString($"{Base}/{tourId:D}/applications", query);
        return _api.GetAsync<GuideApplicationPageResponse>(url, ct);
    }

    public Task<ApiResult> ApproveAsync(Guid tourId, Guid applicationId, CancellationToken ct) =>
        _api.PostAsync($"{Base}/{tourId:D}/applications/{applicationId:D}/approve", null, ct);

    public Task<ApiResult> RejectAsync(Guid tourId, Guid applicationId, string reason, CancellationToken ct) =>
        _api.PostAsync($"{Base}/{tourId:D}/applications/{applicationId:D}/reject", new { reason }, ct);
}
