using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Lifecycle;

/// <summary>
/// Phase 5B/5C — view-model for the admin reassignment form post.
/// Mirrors the backend FluentValidation rules in
/// <c>AdminReassignAccountCommandValidator</c>: required email,
/// max 320 chars, optional reason ≤500 chars.
/// <para>
/// Phase 5C — added the <see cref="IUnderstand"/> server-side guard so
/// the modal's required confirmation checkbox cannot be circumvented
/// by bypassing the JS gate. Backend validation is unchanged; this
/// extra rule lives on the Web side only.
/// </para>
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

    /// <summary>
    /// Phase 5C — operator must tick the "I understand …" checkbox in
    /// the Reassign modal before the Submit button is enabled. This
    /// rule mirrors that gate on the server so the checkbox is the
    /// authoritative confirmation, not just UI copy.
    /// </summary>
    [Display(Name = "I understand the consequences")]
    [Range(typeof(bool), "true", "true",
        ErrorMessage = "You must acknowledge that the old owner will lose access.")]
    public bool IUnderstand { get; set; }
}
