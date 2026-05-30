using Microsoft.Extensions.Options;

namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Default <see cref="IExternalProviderAvailability"/> implementation. Reads
/// each provider-specific options class and exposes a single flag per
/// provider plus a combined list — so views can avoid duplicating the
/// "configured?" check.
/// </summary>
public sealed class ExternalProviderAvailability : IExternalProviderAvailability
{
    public ExternalProviderAvailability(
        IOptions<GoogleProviderOptions> google,
        IOptions<FacebookProviderOptions> facebook)
    {
        IsGoogleAvailable = google.Value.IsConfigured;
        IsFacebookAvailable = facebook.Value.IsConfigured;

        var list = new List<string>(2);
        if (IsGoogleAvailable) list.Add(ExternalProviderConstants.Google);
        if (IsFacebookAvailable) list.Add(ExternalProviderConstants.Facebook);
        AvailableProviders = list;
    }

    public bool IsGoogleAvailable { get; }

    public bool IsFacebookAvailable { get; }

    public IReadOnlyList<string> AvailableProviders { get; }
}
