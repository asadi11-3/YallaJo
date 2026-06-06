using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// Thin HTTP client for the current user's notification inbox
/// (Messaging module, group base /api/v1/notifications).
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'ApiClient' suffix.
/// </summary>
public sealed class NotificationsApiClient
{
    private const string Base = "/api/v1/notifications";

    private readonly IApiClient _api;

    public NotificationsApiClient(IApiClient api) => _api = api;

    /// <summary>
    /// Paged notification list with optional filters (FE-1B inbox + load-more).
    /// <paramref name="type"/> is the NotificationType enum name (e.g. "BookingConfirmed").
    /// </summary>
    public Task<ApiResult<NotificationPageResponse>> GetListAsync(
        string? type = null,
        bool? isRead = null,
        DateTime? from = null,
        DateTime? to = null,
        Guid? cursor = null,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;
        if (isRead is { } r) query["isRead"] = r ? "true" : "false";
        if (from is { } f) query["from"] = f.ToString("O", CultureInfo.InvariantCulture);
        if (to is { } t) query["to"] = t.ToString("O", CultureInfo.InvariantCulture);
        if (cursor is { } c && c != Guid.Empty) query["cursor"] = c.ToString("D");

        var url = QueryHelpers.AddQueryString($"{Base}/", query);
        return _api.GetAsync<NotificationPageResponse>(url, ct);
    }

    /// <summary>Convenience wrapper used by the notification bell (recent N, no filters).</summary>
    public Task<ApiResult<NotificationPageResponse>> GetRecentAsync(int pageSize = 10, CancellationToken ct = default)
        => GetListAsync(pageSize: pageSize, ct: ct);

    public Task<ApiResult<int>> GetUnreadCountAsync(CancellationToken ct = default)
        => _api.GetAsync<int>($"{Base}/unread-count", ct);

    public Task<ApiResult> MarkReadAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/read", null, ct);

    public Task<ApiResult> MarkAllReadAsync(CancellationToken ct = default)
        => _api.PostAsync($"{Base}/read-all", null, ct);

    // DELETE /api/v1/notifications/{id} — delete a single own notification (FE-1B)
    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{id}", ct);
}
