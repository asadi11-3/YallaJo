using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Moderation;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class ModerationApiClient
{
    private const string Base = "/api/v1/social/moderation";
    private readonly IApiClient _api;

    public ModerationApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ModerationLogPageResponse>> GetLogsAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (afterCursor is { } cursor && cursor != Guid.Empty)
        {
            query["afterCursor"] = cursor.ToString("D");
        }

        var url = QueryHelpers.AddQueryString($"{Base}/logs", query);
        return _api.GetAsync<ModerationLogPageResponse>(url, ct);
    }

    public Task<ApiResult> WarnAsync(WarnUserApiRequest request, CancellationToken ct)
        => _api.PostAsync($"{Base}/warn", request, ct);

    public Task<ApiResult> BanAsync(BanUserApiRequest request, CancellationToken ct)
        => _api.PostAsync($"{Base}/ban", request, ct);

    public Task<ApiResult> UnbanAsync(Guid userId, CancellationToken ct)
        => _api.DeleteAsync($"{Base}/ban/{userId:D}", ct);
}
