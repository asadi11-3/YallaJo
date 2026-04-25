namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

public sealed class FacebookProviderOptions
{
    public const string SectionName = "ExternalProviders:Facebook";
    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppSecret);
}
