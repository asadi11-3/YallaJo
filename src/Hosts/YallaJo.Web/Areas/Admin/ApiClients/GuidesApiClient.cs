using YallaJo.Web.Areas.Admin.Models.Guides;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class GuidesApiClient
{
    private const string Base = "/api/v1/guides/admin";

    private readonly IApiClient _api;

    public GuidesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<TourGuideProfileResponse>> GetGuideAsync(Guid guideId, CancellationToken ct)
        => _api.GetAsync<TourGuideProfileResponse>($"{Base}/{guideId:D}/", ct);

    public Task<ApiResult> SuspendAsync(Guid guideId, string reason, CancellationToken ct)
        => _api.PostAsync($"{Base}/{guideId:D}/suspend", new { reason }, ct);

    public Task<ApiResult> ReinstateAsync(Guid guideId, CancellationToken ct)
        => _api.PostAsync($"{Base}/{guideId:D}/reinstate", null, ct);

    // §8.14 — PUT /api/v1/guides/admin/{guideId} (update profile)
    public Task<ApiResult> UpdateAsync(Guid guideId, AdminUpdateGuideApiRequest request, CancellationToken ct)
        => _api.PutAsync($"{Base}/{guideId:D}/", request, ct);

    // §8.14 — DELETE /api/v1/guides/admin/{guideId} (deactivate)
    public Task<ApiResult> DeleteAsync(Guid guideId, CancellationToken ct)
        => _api.DeleteAsync($"{Base}/{guideId:D}/", ct);
}
