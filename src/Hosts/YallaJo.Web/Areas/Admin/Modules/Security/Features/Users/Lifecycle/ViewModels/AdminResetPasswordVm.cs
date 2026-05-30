using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.ViewModels;

/// <summary>
/// Phase 5B — view-model for the admin password-reset form post.
/// Reason is optional and bounded so the audit row stays within the
/// backend's 500-char column. Phase 5C will add a modal-based reason
/// field; Phase 5B uses a plain inline form input.
/// </summary>
public sealed class AdminResetPasswordVm
{
    [StringLength(500, ErrorMessage = "Reason must be 500 characters or fewer.")]
    public string? Reason { get; set; }
}
