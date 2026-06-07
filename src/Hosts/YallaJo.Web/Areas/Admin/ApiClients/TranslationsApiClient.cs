using YallaJo.Web.Areas.Admin.Models.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class TranslationsApiClient
{
    private readonly IApiClient _api;
    public TranslationsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<EntityTranslationItemResponse>>> GetEntityTranslationsAsync(
        string entityType, Guid entityId, CancellationToken ct = default)
        => _api.GetAsync<List<EntityTranslationItemResponse>>(
            $"/api/v1/content-core/translations/{Uri.EscapeDataString(entityType)}/{entityId}", ct);

    public Task<ApiResult<TranslateTextResponse>> TranslateAsync(
        TranslateRequest request, CancellationToken ct = default)
        => _api.PostAsync<TranslateTextResponse>(
            "/api/v1/content-core/translations/translate", request, ct);

    public Task<ApiResult> UpdateAsync(
        Guid id, UpdateTranslationRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/content-core/translations/{id}", request, ct);

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/content-core/translations/{id}/approve", null, ct);

    // §8.9 — POST /translations/backfill/{entityKind} (entityKind: tag | specialization).
    public Task<ApiResult> BackfillAsync(string entityKind, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/content-core/translations/backfill/{Uri.EscapeDataString(entityKind)}", null, ct);

    // §8.9 — POST /translations/approve-batch (mark all auto-translated fields reviewed).
    public Task<ApiResult> ApproveBatchAsync(object request, CancellationToken ct = default)
        => _api.PostAsync("/api/v1/content-core/translations/approve-batch", request, ct);
}
