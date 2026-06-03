using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Providers;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Composes the admin provider-queue screens over the Accounts admin API
/// (<c>/api/v1/admin/providers</c>). Normalizes <see cref="ApiResult"/> values and
/// maps API DTOs to view models. There is no admin detail endpoint, so all data
/// comes from the queue summary.
/// </summary>
public sealed class ProvidersFacade
{
    private readonly ProvidersApiClient _api;

    public ProvidersFacade(ProvidersApiClient api) => _api = api;

    public async Task<ApiResult<ProviderQueueVm>> GetQueueAsync(
        string? status, string? type, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _api.GetQueueAsync(status, type, page, pageSize, ct);

        if (result.IsUnauthorized) return ApiResult<ProviderQueueVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ProviderQueueVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load the provider queue.");

        return ApiResult<ProviderQueueVm>.Ok(ProvidersMapper.ToQueueVm(result.Data, status, type));
    }

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.ApproveAsync(id, ct), "Could not approve the application.");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(_api.RejectAsync(id, new RejectProviderRequest(reason), ct), "Could not reject the application.");

    public Task<ApiResult> RequestDocsAsync(
        Guid id, IReadOnlyList<string> missingDocumentTypes, string notes, CancellationToken ct = default)
        => Normalize(
            _api.RequestDocsAsync(id, new RequestMoreDocsRequest(missingDocumentTypes, notes), ct),
            "Could not request documents.");

    public Task<ApiResult> SuspendAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(_api.SuspendAsync(id, new SuspendProviderRequest(reason), ct), "Could not suspend the provider.");

    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.ReinstateAsync(id, ct), "Could not reinstate the provider.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "Provider application not found.");
        if (result.IsConflict) return ApiResult.Fail(409, "This action is not allowed in the application's current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
