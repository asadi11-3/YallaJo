using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Recaptcha;

internal sealed class RecaptchaOptionsValidator : IValidateOptions<RecaptchaOptions>
{
    public ValidateOptionsResult Validate(string? name, RecaptchaOptions options)
    {
        var errors = new List<string>();

        if (options.BypassForTesting)
            return ValidateOptionsResult.Success;

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            errors.Add($"{RecaptchaOptions.SectionName}:SecretKey is not configured.");

        if (options.MinimumScore < 0.0 || options.MinimumScore > 1.0)
            errors.Add($"{RecaptchaOptions.SectionName}:MinimumScore must be between 0.0 and 1.0.");

        if (options.TimeoutSeconds <= 0 || options.TimeoutSeconds > 30)
            errors.Add($"{RecaptchaOptions.SectionName}:TimeoutSeconds must be between 1 and 30.");

        if (string.IsNullOrWhiteSpace(options.VerifyEndpoint)
            || !Uri.TryCreate(options.VerifyEndpoint, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add(
                $"{RecaptchaOptions.SectionName}:VerifyEndpoint must be an absolute HTTPS URL.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
