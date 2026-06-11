using YallaJo.Web.Areas.Business.Models.Staff;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

public sealed class StaffApiClient
{
    private const string Base = "/api/v1/places/businesses";
    private readonly IApiClient _api;

    public StaffApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<BusinessStaffItemResponse>>> GetStaffAsync(Guid businessId, CancellationToken ct = default) =>
        _api.GetAsync<List<BusinessStaffItemResponse>>($"{Base}/{businessId:D}/staff", ct);

    public Task<ApiResult> AddAsync(Guid businessId, AddBusinessStaffApiRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"{Base}/{businessId:D}/staff", request, ct);

    public Task<ApiResult> RemoveAsync(Guid staffId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{Base}/staff/{staffId:D}", ct);

    /// <summary>User lookup (B1): typeahead via <paramref name="q"/> or batch enrichment via <paramref name="ids"/>.</summary>
    public Task<ApiResult<List<UserLookupItemResponse>>> LookupUsersAsync(
        string? q, IEnumerable<Guid>? ids = null, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query.Add($"q={Uri.EscapeDataString(q.Trim())}");
        }

        var idList = ids?.Distinct().ToList();
        if (idList is { Count: > 0 })
        {
            query.Add($"ids={string.Join(",", idList.Select(i => i.ToString("D")))}");
        }

        query.Add("limit=20");
        return _api.GetAsync<List<UserLookupItemResponse>>($"/api/v1/users/lookup?{string.Join("&", query)}", ct);
    }
}
