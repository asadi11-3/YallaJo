using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class PlacesApiClient
{
    private const string Base = "/api/v1/places";

    private readonly IApiClient _api;

    public PlacesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PlaceLookupResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<PlaceLookupResponse>($"{Base}/{id}", ct);
}
