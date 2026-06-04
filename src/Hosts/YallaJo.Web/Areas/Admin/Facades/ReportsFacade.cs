using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Reports;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class ReportsFacade
{
    private const int DefaultPageSize = 20;
    private readonly ReportsApiClient _api;

    public ReportsFacade(ReportsApiClient api) => _api = api;

    public async Task<ApiResult<ReportsVm>> GetIndexAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetAdminReportsAsync(afterCursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<ReportsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<ReportsVm>.Fail(result.StatusCode, result.Error ?? "Could not load reports.");
        }

        return ApiResult<ReportsVm>.Ok(ReportsMapper.ToVm(result.Data));
    }

    public Task<ApiResult> ResolveAsync(Guid id, string action, string? notes, CancellationToken ct)
        => Normalize(_api.ResolveAsync(id, action, notes, ct), "Could not resolve the report.");

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
            return ApiResult.Fail(404, "Report not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the report's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
