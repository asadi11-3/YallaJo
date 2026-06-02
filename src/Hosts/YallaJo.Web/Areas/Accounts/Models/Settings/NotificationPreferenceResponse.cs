namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Inbound notification preference row from GET /api/v1/notifications/preferences.
/// Type and Channel are enum string names (e.g. "BookingConfirmed", "Email").
/// </summary>
public sealed class NotificationPreferenceResponse
{
    public string Type { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
