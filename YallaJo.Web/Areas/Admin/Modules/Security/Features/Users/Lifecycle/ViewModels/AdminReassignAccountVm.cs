using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.ViewModels;

/// <summary>
/// Phase 5B — view-model for the admin reassignment form post.
/// Mirrors the backend FluentValidation rules in
/// <c>AdminReassignAccountCommandValidator</c>: required email,
/// max 320 chars, optional reason ≤500 chars.
/// </summary>
public sealed class AdminReassignAccountVm
{
    [Required(ErrorMessage = "New email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(320, ErrorMessage = "Email must be 320 characters or fewer.")]
    [Display(Name = "New email")]
    public string NewEmail { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Reason must be 500 characters or fewer.")]
    public string? Reason { get; set; }
}
