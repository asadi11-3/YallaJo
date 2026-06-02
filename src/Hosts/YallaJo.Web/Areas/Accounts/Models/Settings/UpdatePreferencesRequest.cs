namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Outbound body for PUT /api/v1/notifications/preferences.
/// The API binds Type/Channel enum values from their string names.
/// </summary>
public sealed record UpdatePreferencesRequest(IReadOnlyList<PreferenceUpdateItem> Updates);

public sealed record PreferenceUpdateItem(string Type, string Channel, bool IsEnabled);
