using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Mappers;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

public sealed class AuditLogsFacade
{
    private readonly AuditLogsApiClient _api;
    public AuditLogsFacade(AuditLogsApiClient api) => _api = api;

    public async Task<AuditLogsFacadeResult<AuditLogListVm>> GetLogsAsync(
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
            return AuditLogsFacadeResult<AuditLogListVm>.Ok(vm);
        }

        if (result.IsUnauthorized) return AuditLogsFacadeResult<AuditLogListVm>.ForceSignOut();
        return AuditLogsFacadeResult<AuditLogListVm>.Fail(result.Error ?? "Could not load audit logs.");
    }
}

public sealed class AuditLogsFacadeResult<T>
{
    public bool    IsSuccess      { get; private init; }
    public T?      Data           { get; private init; }
    public string? Error          { get; private init; }
    public bool    RequireSignOut { get; private init; }

    public static AuditLogsFacadeResult<T> Ok(T data)    => new() { IsSuccess = true, Data = data };
    public static AuditLogsFacadeResult<T> Fail(string e) => new() { IsSuccess = false, Error = e };
    public static AuditLogsFacadeResult<T> ForceSignOut() => new() { IsSuccess = false, RequireSignOut = true };
}
