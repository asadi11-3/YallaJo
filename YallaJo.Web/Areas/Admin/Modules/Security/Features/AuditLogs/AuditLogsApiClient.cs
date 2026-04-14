using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs;

public sealed class AuditLogsApiClient
{
    private readonly ApiClient _api;
    public AuditLogsApiClient(ApiClient api) => _api = api;

    public Task<ApiResult<AuditLogListResponse>> GetLogsAsync(
        int page, int pageSize, Guid? userId, CancellationToken ct = default)
    {
        var url = $"/api/v1/security/audit-logs?page={page}&pageSize={pageSize}";
        if (userId.HasValue)
            url += $"&userId={userId.Value}";
        return _api.GetAsync<AuditLogListResponse>(url, ct);
    }
}
