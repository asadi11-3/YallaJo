namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Web-side mirror of the API's <c>ExternalAuth</c> options. The Web BFF uses
/// these values to MINT signed tickets; the API uses a matching binding to
/// VERIFY them. <see cref="SigningKey"/> MUST be identical on both sides.
/// </summary>
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
