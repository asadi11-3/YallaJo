namespace YallaJo.Web.Areas.Business.Shared;

/// <summary>
/// Model for the shared _DeleteConfirmForm partial: a plain POST form whose
/// confirmation is handled by the global form-ux.js data-confirm modal
/// (F8/MOD3/MOD5) with a native-submit no-JS fallback (PE1). Replaces the
/// per-row delete modals previously rendered in loops (D-7).
/// </summary>
public sealed class DeleteConfirmFormVm
{
    /// <summary>Target action name (e.g. "Remove").</summary>
    public string Action { get; set; } = "Remove";

    /// <summary>Target controller name (e.g. "Amenities").</summary>
    public string Controller { get; set; } = "";

    /// <summary>Route values for the form action (e.g. id + amenityId).</summary>
    public Dictionary<string, string> RouteValues { get; set; } = [];

    /// <summary>Localized confirmation body shown in the modal.</summary>
    public string ConfirmMessage { get; set; } = "";

    /// <summary>Localized accessible label for the submit button.</summary>
    public string ButtonLabel { get; set; } = "";
}
