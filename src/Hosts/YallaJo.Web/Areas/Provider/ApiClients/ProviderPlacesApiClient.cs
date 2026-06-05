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
}
