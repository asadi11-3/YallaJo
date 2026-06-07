using YallaJo.Web.Areas.Admin.Models.Recommendations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class RecommendationsApiClient
{
    private const string Base = "/api/v1/analytics/admin";
    private readonly IApiClient _api;

    public RecommendationsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<IReadOnlyList<BatchResponse>>> GetBatchesAsync(CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<BatchResponse>>($"{Base}/batches/", ct);

    public Task<ApiResult> RefreshBatchesAsync(RefreshBatchRequest req, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/batches/refresh", new
        {
            sourceKind = req.SourceKind,
            sourceId = req.SourceId,
            context = req.Context,
        }, ct);

    public Task<ApiResult> CreateBoostAsync(CreateBoostRequest req, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/boosts/", new
        {
            providerId = req.ProviderId,
            entityKind = req.EntityKind,
            entityId = req.EntityId,
            multiplier = req.Multiplier,
            startsAt = req.StartsAt,
            expiresAt = req.ExpiresAt,
        }, ct);

    public Task<ApiResult> DeactivateBoostAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/boosts/{id:D}", ct);

    public Task<ApiResult> CreatePinAsync(CreatePinRequest req, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/pins/", new
        {
            entityKind = req.EntityKind,
            entityId = req.EntityId,
            position = req.Position,
            context = req.Context,
            badgeText = req.BadgeText,
            expiresAt = req.ExpiresAt,
        }, ct);

    public Task<ApiResult> DeactivatePinAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/pins/{id:D}", ct);
}
