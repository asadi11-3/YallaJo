using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Accounts.Models.Recommendations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class RecommendationsApiClient
{
    private const string AnalyticsBase = "/api/v1/analytics";
    private const string InteractionsBase = "/api/v1/interactions";
    private readonly IApiClient _api;

    public RecommendationsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<RecommendationsResponse>> GetRecommendationsAsync(
        int limit = 20,
        bool halalOnly = false,
        bool showAllPrices = false,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
            ["halalOnly"] = halalOnly ? "true" : "false",
            ["showAllPrices"] = showAllPrices ? "true" : "false",
        };
        var url = QueryHelpers.AddQueryString($"{AnalyticsBase}/recommendations/", query);
        return _api.GetAsync<RecommendationsResponse>(url, ct);
    }

    public Task<ApiResult<UserPreferencesResponse>> GetPreferencesAsync(CancellationToken ct = default) =>
        _api.GetAsync<UserPreferencesResponse>($"{AnalyticsBase}/preferences/", ct);

    public Task<ApiResult<UserPreferencesResponse>> UpdatePreferencesAsync(
        UpdatePreferencesApiRequest request,
        CancellationToken ct = default) =>
        _api.PutAsync<UserPreferencesResponse>($"{AnalyticsBase}/preferences/", request, ct);

    public Task<ApiResult> MarkNotInterestedAsync(MarkNotInterestedApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"{AnalyticsBase}/recommendations/not-interested", request, ct);

    public Task<ApiResult> RecordInteractionAsync(RecordInteractionApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync(InteractionsBase, request, ct);
}
