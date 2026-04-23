namespace Auth.Infrastructure.ExternalAuth;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";
    public string SigningKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = "YallaJo.Web";
    public string Audience { get; init; } = "YallaJo.Api";

    public int TicketLifetimeSeconds { get; init; } = 120;
    public IReadOnlyList<string> AllowedProviders { get; init; } =
        new[] { "google", "facebook" };
}
