namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

public sealed class ExternalAuthCompleteVm
{
    public string Provider { get; set; } = string.Empty;

    public string Ticket { get; set; } = string.Empty;

    public string Mode { get; set; } = "login";
    public string? ReturnUrl { get; set; }

    public string RecaptchaAction { get; set; } = string.Empty;
}
