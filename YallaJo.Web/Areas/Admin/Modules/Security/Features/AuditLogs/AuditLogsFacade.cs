using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

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
}
