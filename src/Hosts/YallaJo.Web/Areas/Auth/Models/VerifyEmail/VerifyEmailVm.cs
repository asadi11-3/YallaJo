using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace YallaJo.Web.Areas.Auth.Models.VerifyEmail;

public sealed class VerifyEmailVm
{
    /// <summary>
    /// The pending e-mail under verification. Populated SERVER-SIDE from the encrypted
    /// pending-verification cookie (never from the form/query — [BindNever] enforces
    /// that a posted value is ignored), so a caller cannot point the OTP check at an
    /// arbitrary account.
    /// </summary>
    [BindNever]
    public string Email { get; set; } = string.Empty;

    /// <summary>Display-only, e.g. "j***@example.com" — shown on the OTP screen.</summary>
    [BindNever]
    public string MaskedEmail { get; set; } = string.Empty;

    // ErrorMessage values are SharedResource resx KEYS (en + ar) — see Program.cs
    // DataAnnotationLocalizerProvider (CON1).
    [Required(ErrorMessage = "Auth.Validation.OtpRequired")]
    public string OtpCode { get; set; } = string.Empty;

    public string RecaptchaToken { get; set; } = string.Empty;
}
