namespace YallaJo.Web.Areas.Auth.Models.ExternalProviders;

public sealed class ExternalProvidersVm
{
    public bool IsGoogleAvailable { get; set; }
    public bool IsFacebookAvailable { get; set; }

    /// <summary>The user's active linked providers (safe projection, B5).</summary>
    public IReadOnlyList<LinkedProviderVm> LinkedProviders { get; set; } = [];

    /// <summary>
    /// Set when the linked-provider list could not be fetched — the page then renders
    /// link buttons only (safe degrade) plus an inline warning instead of crashing.
    /// </summary>
    public bool LinkedListUnavailable { get; set; }

    public bool IsLinked(string provider) =>
        LinkedProviders.Any(p =>
            string.Equals(p.Provider, provider, StringComparison.OrdinalIgnoreCase));
}

/// <summary>One linked provider row for the account-security page.</summary>
public sealed class LinkedProviderVm
{
    public Guid     ProviderId    { get; init; }

    /// <summary>Normalized provider key (e.g. "google").</summary>
    public string   Provider      { get; init; } = string.Empty;

    /// <summary>Provider-reported e-mail, already MASKED for display (may be empty).</summary>
    public string   MaskedEmail   { get; init; } = string.Empty;

    public DateTime LinkedAt      { get; init; }
}
