using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// Composes the notification-bell view model and exposes mark-read mutations.
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'Facade' suffix.
/// </summary>
public sealed class NotificationsFacade
{
    private const int RecentPageSize = 8;

    private readonly NotificationsApiClient _api;
    private readonly ILogger<NotificationsFacade> _logger;

    public NotificationsFacade(NotificationsApiClient api, ILogger<NotificationsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Builds the bell VM. Never throws / never blocks a page: any failure degrades to
    /// zero unread + empty list so the header always renders.
    /// </summary>
    public async Task<NotificationBellVm> GetBellAsync(CancellationToken ct = default)
    {
        var unreadTask = SafeUnreadCountAsync(ct);
        var recentTask = SafeRecentAsync(ct);
        await Task.WhenAll(unreadTask, recentTask);

        return new NotificationBellVm
        {
            UnreadCount = unreadTask.Result,
            Recent = recentTask.Result,
        };
    }

    public async Task<ApiResult> MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return Normalize(await _api.MarkReadAsync(id, ct), "Could not mark the notification as read.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark notification {NotificationId} as read", id);
            return ApiResult.Fail("Could not mark the notification as read.");
        }
    }

    public async Task<ApiResult> MarkAllReadAsync(CancellationToken ct = default)
    {
        try
        {
            return Normalize(await _api.MarkAllReadAsync(ct), "Could not mark notifications as read.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark all notifications as read");
            return ApiResult.Fail("Could not mark notifications as read.");
        }
    }

    private async Task<int> SafeUnreadCountAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetUnreadCountAsync(ct);
            return result.IsSuccess ? result.Data : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load notification unread count");
            return 0;
        }
    }

    private async Task<IReadOnlyList<NotificationRowVm>> SafeRecentAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetRecentAsync(RecentPageSize, ct);
            if (!result.IsSuccess || result.Data is null)
            {
                return [];
            }

            return result.Data.Items
                .Select(n => new NotificationRowVm(n.Id, n.Title, n.Body, n.IsRead, n.CreatedAt))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load recent notifications");
            return [];
        }
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
