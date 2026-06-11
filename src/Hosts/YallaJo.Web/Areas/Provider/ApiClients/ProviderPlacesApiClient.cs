using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;


public sealed class ProviderPlacesApiClient
{
    private const string Base = "/api/v1/places";

    private const int LookupPageSize = 50;

    private readonly IApiClient _api;

    public ProviderPlacesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PaginatedPlaceLookupResponse>> ListAsync(CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"]     = "1",
            ["pageSize"] = LookupPageSize.ToString(),
        };

        var url = QueryHelpers.AddQueryString(Base, query);
        return _api.GetAsync<PaginatedPlaceLookupResponse>(url, ct);
    }

    /// <summary>[Backend] B2 — lightweight typeahead lookup (GET /api/v1/places/lookup).</summary>
    public Task<ApiResult<List<PlaceLookupSuggestionResponse>>> LookupAsync(
        string? term, int pageSize = 10, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(term)) query["term"] = term;

        var url = QueryHelpers.AddQueryString($"{Base}/lookup", query);
        return _api.GetAsync<List<PlaceLookupSuggestionResponse>>(url, ct);
    }
}

/// <summary>[Backend] B2 mirror of PlaceLookupDto (typeahead suggestion).</summary>
public sealed record PlaceLookupSuggestionResponse(Guid Id, string Name, string? City);
