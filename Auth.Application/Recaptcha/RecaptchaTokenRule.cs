using FluentValidation;

namespace Auth.Application.Recaptcha;


public static class RecaptchaTokenRule
{
    private const int MaxTokenLength = 4096;

    public static IRuleBuilderOptions<T, string> MustBeValidRecaptchaToken<T>(
        this IRuleBuilder<T, string> rule)
        where T : IRecaptchaProtectedCommand =>
        rule
            .NotEmpty().WithMessage("Captcha verification failed.")
            .MaximumLength(MaxTokenLength).WithMessage("Captcha verification failed.");
}
