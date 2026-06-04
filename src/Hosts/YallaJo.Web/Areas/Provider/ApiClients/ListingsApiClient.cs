using YallaJo.Web.Areas.Provider.Models.Listings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ListingsApiClient
{
    private readonly IApiClient _api;

    public ListingsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ListMyToursResponse>> GetMyToursAsync(
        int page, int pageSize, string? status, string? sort, CancellationToken ct = default)
    {
        var query = $"/api/v1/tours/provider/my-tours?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";

        if (!string.IsNullOrWhiteSpace(sort))
            query += $"&sort={Uri.EscapeDataString(sort)}";

        return _api.GetAsync<ListMyToursResponse>(query, ct);
    }

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        _api.DeleteAsync($"/api/v1/tours/{id}", ct);
}
