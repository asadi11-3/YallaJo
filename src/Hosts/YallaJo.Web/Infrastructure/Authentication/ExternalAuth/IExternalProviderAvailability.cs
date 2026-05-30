namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Exposes at runtime which OAuth providers are configured. Views use this so
/// that a provider button is rendered ONLY when the underlying handler has
/// actually been wired up — missing config simply hides the button instead of
/// taking the user to a dead challenge endpoint.
/// </summary>
public interface IExternalProviderAvailability
{
    bool IsGoogleAvailable { get; }

    bool IsFacebookAvailable { get; }

    IReadOnlyList<string> AvailableProviders { get; }
}
