namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

public sealed class ExternalProvidersVm
{
    public string? Message { get; set; }
    public bool IsGoogleAvailable { get; set; }
    public bool IsFacebookAvailable { get; set; }
}
