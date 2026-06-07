using YallaJo.Web.Areas.Admin.Models.AuditLogs;
using YallaJo.Web.Areas.Admin.Models.AuditLogs;
using YallaJo.Web.Infrastructure.Api.Contracts;

using YallaJo.Web.Areas.Admin.ApiClients;
namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Phase 5A — orchestrates the audit-timeline read flow. Forwards every
/// optional Phase 4 filter (<paramref name="actorUserId"/>,
/// <paramref name="action"/>, <paramref name="from"/>, <paramref name="to"/>)
/// to the API client and reflects them back into
/// <see cref="AuditLogListVm"/> so the view can pre-populate the filter
/// bar and preserve them across page navigation.
/// </summary>
public sealed class AuditLogsFacade
{
    private readonly AuditLogsApiClient _api;
    public AuditLogsFacade(AuditLogsApiClient api) => _api = api;

    public async Task<ApiResult<AuditLogListVm>> GetLogsAsync(
        int page,
        int pageSize,
        Guid? userId,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await _api.GetLogsAsync(page, pageSize, userId, actorUserId, action, from, to, ct);

        if (result.IsSuccess)
        {
            var d  = result.Data!;
            var vm = new AuditLogListVm
            {
                Logs              = d.Items.Select(AuditLogsMapper.ToRowVm).ToList(),
                Page              = d.PageNumber,
                TotalCount        = d.TotalCount,
                HasPrevious       = d.HasPreviousPage,
                HasNext           = d.HasNextPage,
                FilterUserId      = userId,
                FilterActorUserId = actorUserId,
                FilterAction      = action,
                FilterFrom        = from,
                FilterTo          = to,
            };
            return ApiResult<AuditLogListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<AuditLogListVm>.ForceSignOut();
        return ApiResult<AuditLogListVm>.CreateFailure(result.Error ?? "Could not load audit logs.");
    }

    // §8.13 — redact a single audit-log entry.
    public async Task<ApiResult> RedactAsync(long id, string? reason, CancellationToken ct = default)
    {
        if (id <= 0)
        {
            return ApiResult.Fail(400, "A valid audit-log entry is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult.Fail(400, "A redaction reason is required.");
        }

        var result = await _api.RedactAsync(id, reason.Trim(), ct);
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "Audit-log entry not found.");
        if (result.IsValidationError && result.ValidationErrors is not null) return ApiResult.Invalid(result.ValidationErrors);
        return result.IsSuccess
            ? ApiResult.Ok()
            : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not redact the audit-log entry.");
    }

    // §8.13 — export audit logs for a date range (returns CSV content).
    public async Task<ApiResult<string>> ExportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to < from)
        {
            return ApiResult<string>.CreateFailure("The end date must be on or after the start date.");
        }

        var result = await _api.ExportAsync(from, to, ct);
        if (result.IsSuccess) return ApiResult<string>.CreateSuccess(result.Data ?? string.Empty);
        if (result.IsUnauthorized) return ApiResult<string>.ForceSignOut();
        return ApiResult<string>.CreateFailure(result.Error ?? "Could not export the audit logs.");
    }
}
