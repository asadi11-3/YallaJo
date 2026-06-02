using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Areas.Accounts.Models.Settings;
using YallaJo.Web.Areas.Accounts.Models.Settings;

namespace YallaJo.Web.Areas.Accounts.Models.Settings;

/// <summary>
/// Static, allocation-only conversions between Settings DTOs and ViewModels.
/// All toggle-matrix policy (which types/channels are surfaced) lives on the
/// facade; this mapper only projects already-resolved data.
/// </summary>
public static class SettingsMapper
{
    public static string Key(string type, string channel) => $"{type}|{channel}";

    /// <summary>
    /// Projects the curated (type, label) list into rows, overlaying the set of
    /// disabled "{Type}|{Channel}" keys derived from the stored override rows.
    /// </summary>
    public static List<NotificationRowVm> ToRows(
        IReadOnlyList<(string Type, string Label)> surfacedTypes,
        ISet<string> disabledKeys) =>
        surfacedTypes
            .Select(t => new NotificationRowVm
            {
                Type = t.Type,
                Label = t.Label,
                Email = !disabledKeys.Contains(Key(t.Type, "Email")),
                Push = !disabledKeys.Contains(Key(t.Type, "Push")),
                InApp = !disabledKeys.Contains(Key(t.Type, "InApp")),
            })
            .ToList();

    /// <summary>
    /// Builds the full update payload: every surfaced cell is sent so that
    /// unchecking a previously-on cell is honoured by the API.
    /// </summary>
    public static UpdatePreferencesRequest ToUpdateRequest(
        IReadOnlyList<(string Type, string Label)> surfacedTypes,
        IReadOnlyList<string> surfacedChannels,
        ISet<string> enabledKeys)
    {
        var updates = new List<PreferenceUpdateItem>();
        foreach (var (type, _) in surfacedTypes)
            foreach (var channel in surfacedChannels)
                updates.Add(new PreferenceUpdateItem(type, channel, enabledKeys.Contains(Key(type, channel))));

        return new UpdatePreferencesRequest(updates);
    }

    public static HashSet<string> ToDisabledKeys(IEnumerable<NotificationPreferenceResponse> stored)
    {
        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in stored)
            if (!row.IsEnabled)
                disabled.Add(Key(row.Type, row.Channel));
        return disabled;
    }

    public static MarketingConsentVm ToVm(MarketingConsentResponse? r) => new()
    {
        EmailDigest = r?.EmailDigest ?? false,
        PushNotifications = r?.PushNotifications ?? false,
        ReEngagementCampaigns = r?.ReEngagementCampaigns ?? false,
    };

    public static MarketingConsentRequest ToRequest(bool emailDigest, bool push, bool reEngagement) =>
        new(emailDigest, push, reEngagement);
}
