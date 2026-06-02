using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Lifecycle;

/// <summary>
/// Phase 5C — view-model for the Archive admin lifecycle action. The
/// Archive modal asks the operator to type the literal word
/// <c>ARCHIVE</c> in capitals before the Submit button is enabled. The
/// regex below mirrors that gate on the server so a user who disables
/// JS or bypasses the modal still cannot trigger an irreversible
/// archive without typing the confirmation.
/// </summary>
public sealed class AdminArchiveVm
{
    [Required(ErrorMessage = "Type ARCHIVE in capitals to confirm.")]
    [RegularExpression("^ARCHIVE$",
        ErrorMessage = "Type ARCHIVE in capitals to confirm.")]
    [Display(Name = "Type ARCHIVE to confirm")]
    public string ConfirmText { get; set; } = string.Empty;
}
