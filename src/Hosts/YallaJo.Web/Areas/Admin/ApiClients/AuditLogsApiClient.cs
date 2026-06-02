using System.Globalization;
using YallaJo.Web.Areas.Admin.Models.AuditLogs;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Phase 5A — typed wrapper around the API audit-timeline endpoint.
/// Adds the Phase 4 filter surface (<paramref name="actorUserId"/>,
/// <paramref name="action"/>, <paramref name="from"/>, <paramref name="to"/>)
/// while staying backward-compatible: passing only the original
/// <paramref name="page"/>/<paramref name="pageSize"/>/<paramref name="userId"/>
/// triple keeps the same URL shape as before.
/// </summary>
public sealed class AuditLogsApiClient
{
    private readonly IApiClient _api;
    public AuditLogsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<AuditLogListResponse>> GetLogsAsync(
        int page,
        int pageSize,
        Guid? userId,
        Guid? actorUserId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var url = BuildUrl(page, pageSize, userId, actorUserId, action, from, to);
        return _api.GetAsync<AuditLogListResponse>(url, ct);
    }

    /// <summary>
    /// Phase 5A — internal so unit tests can assert query-string
    /// composition without going through <c>ApiClient</c>. The order of
    /// optional params is fixed so the URL is deterministic and easy to
    /// match in tests / logs.
    /// </summary>
    internal static string BuildUrl(
        int page,
        int pageSize,
        Guid? userId,
        Guid? actorUserId,
        string? action,
        DateTime? from,
        DateTime? to)
    {
        var url = $"/api/v1/security/audit-logs?page={page}&pageSize={pageSize}";

        if (userId.HasValue)
            url += $"&userId={userId.Value}";

        if (actorUserId.HasValue)
            url += $"&actorUserId={actorUserId.Value}";

        if (!string.IsNullOrWhiteSpace(action))
            url += $"&action={Uri.EscapeDataString(action.Trim())}";

        if (from.HasValue)
            url += $"&from={Uri.EscapeDataString(from.Value.ToString("o", CultureInfo.InvariantCulture))}";

        if (to.HasValue)
            url += $"&to={Uri.EscapeDataString(to.Value.ToString("o", CultureInfo.InvariantCulture))}";

        return url;
    }
}
