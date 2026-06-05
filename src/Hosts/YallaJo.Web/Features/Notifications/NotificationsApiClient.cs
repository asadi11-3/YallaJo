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

    public Task<ApiResult<NotificationPageResponse>> GetRecentAsync(int pageSize = 10, CancellationToken ct = default)
        => _api.GetAsync<NotificationPageResponse>($"{Base}/?pageSize={pageSize}", ct);

    public Task<ApiResult<int>> GetUnreadCountAsync(CancellationToken ct = default)
        => _api.GetAsync<int>($"{Base}/unread-count", ct);

    public Task<ApiResult> MarkReadAsync(Guid id, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/{id}/read", null, ct);

    public Task<ApiResult> MarkAllReadAsync(CancellationToken ct = default)
        => _api.PostAsync($"{Base}/read-all", null, ct);
}
