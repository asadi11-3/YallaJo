using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Models.Discounts;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class DiscountsApiClient
{
    private const string DiscountsBase = "/api/v1/booking/guide-discounts";
    private const string GuidesBase = "/api/v1/guides";

    private readonly IApiClient _api;

    public DiscountsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<GuideDiscountResponse>>> GetMyDiscountsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<GuideDiscountResponse>>($"{DiscountsBase}/mine", ct);

    /// <summary>
    /// GET /api/v1/guides/{guideId}/tours — the guide's own tours, used to feed the
    /// F10 tour picker on the discounts form. Mirrors MyToursApiClient.GetMyToursAsync
    /// and reuses the shared Dashboard response records.
    /// </summary>
    public Task<ApiResult<GuideToursResponse>> GetMyToursAsync(Guid guideId, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<GuideToursResponse>($"{GuidesBase}/{guideId}/tours?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult> CreateAsync(CreateGuideDiscountRequest request, CancellationToken ct = default)
        => _api.PostAsync(DiscountsBase, request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateGuideDiscountRequest request, CancellationToken ct = default)
        => _api.PutAsync($"{DiscountsBase}/{id}", request, ct);

    public Task<ApiResult> DeactivateAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{DiscountsBase}/{id}", ct);
}
