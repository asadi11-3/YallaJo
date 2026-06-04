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
}
