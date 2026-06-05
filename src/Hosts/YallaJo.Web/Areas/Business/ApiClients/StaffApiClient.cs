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
}
