using YallaJo.Web.Areas.Guide.Models.Availability;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class AvailabilityApiClient
{
    private const string BlocksBase = "/api/v1/guides/me/availability-blocks";

    private readonly IApiClient _api;

    public AvailabilityApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<GuideAvailabilityBlockResponse>>> GetBlocksAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<GuideAvailabilityBlockResponse>>(BlocksBase, ct);

    public Task<ApiResult> AddBlockAsync(CreateGuideAvailabilityBlockRequest request, CancellationToken ct = default) =>
        _api.PostAsync(BlocksBase, request, ct);

    public Task<ApiResult> DeleteBlockAsync(Guid blockId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{BlocksBase}/{blockId}", ct);
}
