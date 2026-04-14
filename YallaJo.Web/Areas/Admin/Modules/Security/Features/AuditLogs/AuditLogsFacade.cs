using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

public sealed class AuditLogsFacade
{
    private readonly AuditLogsApiClient _api;
    public AuditLogsFacade(AuditLogsApiClient api) => _api = api;

    public async Task<ApiResult<AuditLogListVm>> GetLogsAsync(
        int page, int pageSize, Guid? userId, CancellationToken ct = default)
    {
        var result = await _api.GetLogsAsync(page, pageSize, userId, ct);

        if (result.IsSuccess)
        {
            var d  = result.Data!;
            var vm = new AuditLogListVm
            {
                Logs         = d.Items.Select(AuditLogsMapper.ToRowVm).ToList(),
                Page         = d.PageNumber,
                TotalCount   = d.TotalCount,
                HasPrevious  = d.HasPreviousPage,
                HasNext      = d.HasNextPage,
                FilterUserId = userId,
            };
            return ApiResult<AuditLogListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<AuditLogListVm>.ForceSignOut();
        return ApiResult<AuditLogListVm>.CreateFailure(result.Error ?? "Could not load audit logs.");
    }
}
