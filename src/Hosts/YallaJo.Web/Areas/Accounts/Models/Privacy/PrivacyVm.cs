namespace YallaJo.Web.Areas.Accounts.Models.Privacy;

/// <summary>
/// FE-1D — view model for the Privacy &amp; Data page (/accounts/privacy).
/// <para>
/// There is no backend endpoint to read whether an analytics-deletion is currently
/// pending, so the page shows both the "delete my data" and "cancel deletion" actions
/// unconditionally; the actual pending/not-pending state is surfaced via friendly
/// messages from the action responses (400 AlreadyPending / 404 NotFound).
/// </para>
/// </summary>
public sealed class PrivacyVm
{
    /// <summary>The download-export action is always available to a Preference.Read holder.</summary>
    public bool ExportAvailable { get; init; } = true;

    /// <summary>Optional banner surfaced when a page-load read degraded (kept for parity; the page has no read calls today).</summary>
    public string? LoadError { get; init; }
}
