using FluentValidation;

namespace Auth.Application.Recaptcha;

/// <summary>
/// FluentValidation rule fragment re-used by every command validator that
/// guards a reCAPTCHA-protected command. Keeping the rule in one place
/// ensures the message, length limit, and required-ness stay consistent.
/// </summary>
public static class RecaptchaTokenRule
{
    private const int MaxTokenLength = 4096;

    public static IRuleBuilderOptions<T, string> MustBeValidRecaptchaToken<T>(
        this IRuleBuilder<T, string> rule) where T : IRecaptchaProtectedCommand =>
        rule
            .NotEmpty().WithMessage("Captcha verification failed.")
            .MaximumLength(MaxTokenLength).WithMessage("Captcha verification failed.");
}
