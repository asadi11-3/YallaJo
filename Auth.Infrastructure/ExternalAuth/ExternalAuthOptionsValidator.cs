using System.Text;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.ExternalAuth;

internal sealed class ExternalAuthOptionsValidator : IValidateOptions<ExternalAuthOptions>
{
    private const int MinimumKeyByteLength = 32;

    public ValidateOptionsResult Validate(string? name, ExternalAuthOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            errors.Add($"{ExternalAuthOptions.SectionName}:SigningKey is not configured.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumKeyByteLength)
        {
            errors.Add(
                $"{ExternalAuthOptions.SectionName}:SigningKey must be at least {MinimumKeyByteLength} bytes (UTF-8).");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
            errors.Add($"{ExternalAuthOptions.SectionName}:Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            errors.Add($"{ExternalAuthOptions.SectionName}:Audience is required.");

        if (options.TicketLifetimeSeconds <= 0 || options.TicketLifetimeSeconds > 600)
        {
            errors.Add(
                $"{ExternalAuthOptions.SectionName}:TicketLifetimeSeconds must be between 1 and 600.");
        }

        if (options.AllowedProviders is null || options.AllowedProviders.Count == 0)
            errors.Add($"{ExternalAuthOptions.SectionName}:AllowedProviders must list at least one provider.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
