using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderToursApiClient
{
    private const string Base = "/api/v1/tours";

    private readonly IApiClient _api;

    public ProviderToursApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ListMyToursResponse>> GetMyToursAsync(
        int page, int pageSize, string? status, string? sort, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
        };
        if (!string.IsNullOrWhiteSpace(status)) query["status"] = status;
        if (!string.IsNullOrWhiteSpace(sort)) query["sort"] = sort;

        var url = QueryHelpers.AddQueryString($"{Base}/provider/my-tours", query);
        return _api.GetAsync<ListMyToursResponse>(url, ct);
    }

    public Task<ApiResult<TourDetailResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourDetailResponse>($"{Base}/{id}", ct);

    public Task<ApiResult<CreateTourResponse>> CreateAsync(CreateTourApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateTourResponse>(Base, request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateTourApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{id}", request, ct);

    public Task<ApiResult> SubmitAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/submit", new TourRowVersionApiRequest(rowVersion), ct);

    public Task<ApiResult> ArchiveAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/archive", new TourRowVersionApiRequest(rowVersion), ct);
}
