using Auth.Application.Recaptcha;
using FluentValidation;

namespace Auth.Application.Commands.ResendOtp;

/// <summary>
/// Phase 2C-5 — password-reset resend is no longer routed through
/// <c>/resend-otp</c>. The <c>ForgotPassword</c> command owns the
/// password-reset lifecycle end-to-end (token aggregate, outbox event,
/// SMTP dispatcher, retention policy), and duplicating that surface
/// via a generic-OTP endpoint created a legacy <c>Otp(PasswordReset)</c>
/// write path that bypassed the <see cref="Auth.Domain.Entities.PasswordResetToken"/>
/// aggregate entirely. This validator now rejects
/// <c>Purpose = "PasswordReset"</c> with a clear error message so
/// clients migrate to <c>POST /forgot-password</c>.
/// <para>
/// <c>/resend-otp</c> remains the canonical resend channel for
/// short-lived OTPs that do NOT have their own aggregate — today that's
/// <c>EmailVerification</c>; tomorrow it might include MFA-style codes
/// and other sensitive-action verifications.
/// </para>
/// </summary>
public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    /// <summary>
    /// Purposes that are still legal for <c>/resend-otp</c>. Any purpose
    /// with its own dedicated token aggregate (activation tokens,
    /// password-reset tokens) is removed from this list and routed
    /// through its own command.
    /// </summary>
    private static readonly string[] ValidPurposes = ["EmailVerification"];

    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .Must(p => ValidPurposes.Contains(p))
            .WithMessage(
                "Unsupported OTP purpose. Password reset resend must be requested via /forgot-password; /resend-otp is reserved for short-lived codes such as EmailVerification.");

        RuleFor(x => x.RecaptchaToken).MustBeValidRecaptchaToken();
    }
}
