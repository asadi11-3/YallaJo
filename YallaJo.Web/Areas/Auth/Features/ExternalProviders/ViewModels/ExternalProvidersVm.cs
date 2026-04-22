namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

/// <summary>
/// Read-only view model for the "Linked Providers" page. Linking is started
/// by POST-ing to the OAuth challenge endpoint — there is no form binding
/// here so that raw provider IDs cannot be smuggled in by a malicious client.
/// </summary>
public sealed class ExternalProvidersVm
{
    public string? Message { get; set; }
    public bool IsGoogleAvailable { get; set; }
    public bool IsFacebookAvailable { get; set; }
}
