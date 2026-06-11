namespace YallaJo.Web.Areas.Admin.Models.Lifecycle;

/// <summary>
/// Phase 5C — view-model for the Archive admin lifecycle action. The
/// Archive modal asks the operator to type a localized confirmation
/// token (resx key <c>Admin.Users.Archive.ConfirmToken</c>) before the
/// Submit button is enabled. The authoritative comparison lives in
/// <c>LifecycleController.Archive</c>, which resolves the token through
/// <c>IStringLocalizer</c> so the expected word always matches the UI
/// language — a user who disables JS or bypasses the modal still cannot
/// trigger an irreversible archive without typing the confirmation.
/// </summary>
public sealed class AdminArchiveVm
{
    public string ConfirmText { get; set; } = string.Empty;
}
