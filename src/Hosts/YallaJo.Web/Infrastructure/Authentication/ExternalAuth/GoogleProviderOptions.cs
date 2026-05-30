namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

public sealed class GoogleProviderOptions
{
    public const string SectionName = "ExternalProviders:Google";
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
