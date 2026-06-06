using YallaJo.Web.Areas.Accounts.Models.Notifications;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Features.Notifications;

/// <summary>
/// Composes the notification-bell view model and exposes mark-read mutations.
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'Facade' suffix.
/// </summary>
public sealed class NotificationsFacade
{
    private const int RecentPageSize = 10;
    private const int InboxPageSize = 20;

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

    /// <summary>
    /// Builds the inbox page VM (FE-1B). Safe-degrades like the bell: any API failure
    /// returns an empty inbox with a non-null <see cref="NotificationsInboxVm.LoadError"/>
    /// so the page chrome always renders instead of 500-ing.
    /// </summary>
    public async Task<NotificationsInboxVm> GetInboxAsync(
        NotificationInboxFilterVm filter, Guid? cursor, CancellationToken ct = default)
    {
        var from = ParseDate(filter.FromDate);
        var to = ParseDate(filter.ToDate);

        var listTask = SafeListAsync(filter.Type, filter.ToIsRead(), from, to, cursor, ct);
        var unreadTask = SafeUnreadCountAsync(ct);
        await Task.WhenAll(listTask, unreadTask);

        var (page, error) = listTask.Result;
        var vm = NotificationsMapper.ToInboxVm(page, filter, unreadTask.Result);

        return new NotificationsInboxVm
        {
            Filter = filter,
            Items = vm.Items,
            NextCursor = vm.NextCursor,
            UnreadCount = vm.UnreadCount,
            LoadError = error,
        };
    }

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.DeleteAsync(id, ct);
            if (result.IsSuccess) return ApiResult.Ok();
            if (result.IsUnauthorized) return ApiResult.ForceSignOut();
            if (result.IsForbidden) return ApiResult.Fail(403, "You don't have permission to delete this notification.");
            if (result.IsNotFound) return ApiResult.Fail(404, "Notification not found.");
            return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not delete the notification.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete notification {NotificationId}", id);
            return ApiResult.Fail("Could not delete the notification.");
        }
    }

    private async Task<(NotificationPageResponse Page, string? Error)> SafeListAsync(
        string? type, bool? isRead, DateTime? from, DateTime? to, Guid? cursor, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetListAsync(type, isRead, from, to, cursor, InboxPageSize, ct);
            if (result.IsSuccess && result.Data is not null)
            {
                return (result.Data, null);
            }

            return (new NotificationPageResponse(), result.Error ?? "Could not load notifications.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load notifications inbox");
            return (new NotificationPageResponse(), "Could not load notifications.");
        }
    }

    private static DateTime? ParseDate(string? value)
        => DateTime.TryParse(value, out var d) ? d : null;

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
