using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.FlaggedReviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class FlaggedReviewsApiClient
{
    private const string Base = "/api/v1/reviews";
    private readonly IApiClient _api;

    public FlaggedReviewsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ReviewPageResponse>> GetFlaggedAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (afterCursor is { } cursor && cursor != Guid.Empty)
        {
            query["afterCursor"] = cursor.ToString("D");
        }

        var url = QueryHelpers.AddQueryString($"{Base}/admin/flagged", query);
        return _api.GetAsync<ReviewPageResponse>(url, ct);
    }

    public Task<ApiResult> ApproveAsync(Guid id, string? rowVersion, string? notes, CancellationToken ct)
        => _api.PostAsync($"{Base}/admin/{id:D}/approve", new { notes, rowVersion }, ct);

    public Task<ApiResult> RemoveAsync(Guid id, string? rowVersion, string? notes, CancellationToken ct)
        => _api.PostAsync($"{Base}/admin/{id:D}/remove", new { notes, rowVersion }, ct);
}
