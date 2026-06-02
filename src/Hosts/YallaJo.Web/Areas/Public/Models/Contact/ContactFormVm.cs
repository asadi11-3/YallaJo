using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Public.Models.Contact;

/// <summary>
/// Contact / support message form. Name and Email are cosmetic only: the support API
/// resolves the creator from the signed-in user, so they are not sent to the backend.
/// </summary>
public sealed class ContactFormVm
{
    [StringLength(100)]
    [Display(Name = "Your name")]
    public string? Name { get; set; }

    [EmailAddress]
    [StringLength(320)]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [Display(Name = "Topic")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Please enter a subject.")]
    [StringLength(200, MinimumLength = 3)]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your message.")]
    [StringLength(2000, MinimumLength = 10)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;
}
