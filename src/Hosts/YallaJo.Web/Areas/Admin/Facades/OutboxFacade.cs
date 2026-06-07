using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Outbox;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class OutboxFacade
{
    private const int DefaultLimit = 50;
    private readonly OutboxApiClient _api;

    public OutboxFacade(OutboxApiClient api) => _api = api;

    public async Task<ApiResult<OutboxVm>> GetIndexAsync(OutboxFilterRequest request, CancellationToken ct = default)
    {
        var limit = request.Limit is < 1 or > 200 ? DefaultLimit : request.Limit;
        var result = await _api.GetDeadLettersAsync(request.Module, limit, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<OutboxVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<OutboxVm>.Fail(result.StatusCode, result.Error ?? "Could not load outbox dead-letters.");
        }

        return ApiResult<OutboxVm>.Ok(OutboxMapper.ToVm(result.Data, request.Module));
    }

    public Task<ApiResult> ReplayAsync(string module, Guid id, CancellationToken ct = default)
        => Normalize(_api.ReplayAsync(module, id, ct), "Could not replay the outbox message.");

    // §8.12 — trigger the tour-snapshot backfill (clamps batch size to a sane range).
    public Task<ApiResult> BackfillTourSnapshotsAsync(int? batchSize, CancellationToken ct = default)
    {
        var size = batchSize is < 1 or > 1000 ? 100 : batchSize.Value;
        return Normalize(_api.BackfillTourSnapshotsAsync(size, ct), "Could not start the tour-snapshot backfill.");
    }

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Dead-letter message not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
