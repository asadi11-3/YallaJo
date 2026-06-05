using YallaJo.Web.Areas.Business.Models.Hours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

public sealed class HoursApiClient
{
    private const string Base = "/api/v1/places/businesses";
    private readonly IApiClient _api;

    public HoursApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<BusinessHoursItemResponse>>> GetHoursAsync(Guid businessId, CancellationToken ct = default)
        => _api.GetAsync<List<BusinessHoursItemResponse>>($"{Base}/{businessId:D}/hours", ct);

    public Task<ApiResult> SetHoursAsync(Guid businessId, SetHoursApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/{businessId:D}/hours", request, ct);
}
