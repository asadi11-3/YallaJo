using YallaJo.Web.Areas.Guide.Models.Discounts;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class DiscountsApiClient
{
    private const string DiscountsBase = "/api/v1/booking/guide-discounts";

    private readonly IApiClient _api;

    public DiscountsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<GuideDiscountResponse>>> GetMyDiscountsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<GuideDiscountResponse>>($"{DiscountsBase}/mine", ct);

    public Task<ApiResult> CreateAsync(CreateGuideDiscountRequest request, CancellationToken ct = default)
        => _api.PostAsync(DiscountsBase, request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateGuideDiscountRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{DiscountsBase}/{id}", request, ct);

    public Task<ApiResult> DeactivateAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{DiscountsBase}/{id}", ct);
}
