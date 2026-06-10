using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class SettingsFacade
{
    private readonly SettingsApiClient _api;

    public SettingsFacade(SettingsApiClient api) => _api = api;

    /// <summary>
    /// Curated, user-toggleable notification types (critical ones such as OTP,
    /// security alerts and payment receipts are intentionally excluded because the
    /// API forbids disabling them). Order is the display order.
    /// </summary>
    // TODO(backend): replace this hardcoded list with GET /api/v1/notifications/types
    // (Messaging module) so new notification types surface automatically — Accounts plan
    // Phase 2 item 5; keep this array as the graceful fallback when that endpoint fails.
    private static readonly (string Type, string Label)[] SurfacedTypes =
    [
        ("BookingConfirmed", "Booking confirmed"),
        ("BookingCancelled", "Booking cancelled"),
        ("BookingCompleted", "Booking completed"),
        ("BookingReminderUpcoming", "Upcoming trip reminders"),
        ("PayoutScheduled", "Payout scheduled"),
        ("PayoutCompleted", "Payout completed"),
        ("ReviewPosted", "New review on your listing"),
        ("ReviewReplied", "Replies to your reviews"),
        ("ProfileUpdated", "Profile change confirmations"),
    ];

    private static readonly string[] SurfacedChannels = ["Email", "Push", "InApp"];

    public async Task<ApiResult<IReadOnlyList<NotificationRowVm>>> GetNotificationRowsAsync(CancellationToken ct = default)
    {
        var result = await _api.GetPreferencesAsync(ct);
        if (result.IsUnauthorized)
            return ApiResult<IReadOnlyList<NotificationRowVm>>.ForceSignOut();
        if (!result.IsSuccess)
            return ApiResult<IReadOnlyList<NotificationRowVm>>.Fail(result.StatusCode, result.Error ?? "Could not load notification preferences.");

        // Stored overrides: only rows that DISABLE a channel are persisted; absence means default-on.
        var disabled = SettingsMapper.ToDisabledKeys(result.Data ?? []);
        var rows = SettingsMapper.ToRows(SurfacedTypes, disabled);

        return ApiResult<IReadOnlyList<NotificationRowVm>>.Ok(rows);
    }

    /// <summary>
    /// Persists the posted matrix. <paramref name="enabledKeys"/> contains the
    /// "{Type}|{Channel}" keys whose checkbox was checked in the submitted form.
    /// Every surfaced cell is sent so unchecking a previously-on cell is honoured.
    /// </summary>
    public Task<ApiResult> UpdateNotificationsAsync(ISet<string> enabledKeys, CancellationToken ct = default)
        => _api.UpdatePreferencesAsync(
            SettingsMapper.ToUpdateRequest(SurfacedTypes, SurfacedChannels, enabledKeys), ct);

    public async Task<ApiResult<MarketingConsentVm>> GetMarketingAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMarketingConsentAsync(ct);
        if (result.IsUnauthorized)
            return ApiResult<MarketingConsentVm>.ForceSignOut();
        if (!result.IsSuccess)
            return ApiResult<MarketingConsentVm>.Fail(result.StatusCode, result.Error ?? "Could not load marketing preferences.");

        return ApiResult<MarketingConsentVm>.Ok(SettingsMapper.ToVm(result.Data));
    }

    public async Task<ApiResult> UpdateMarketingAsync(bool emailDigest, bool push, bool reEngagement, CancellationToken ct = default)
    {
        var result = await _api.UpdateMarketingConsentAsync(
            SettingsMapper.ToRequest(emailDigest, push, reEngagement), ct);
        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        return result.IsSuccess
            ? ApiResult.Ok()
            : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not update marketing preferences.");
    }
}
