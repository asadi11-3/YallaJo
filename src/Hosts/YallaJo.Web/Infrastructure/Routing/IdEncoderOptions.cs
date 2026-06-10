namespace YallaJo.Web.Infrastructure.Routing;

/// <summary>
/// Options bound from the <c>IdEncoder</c> configuration section.
/// <para>
/// <see cref="Secret"/> must be a stable, non-empty value (user-secrets in dev,
/// Key Vault / env var in prod, per SEC6). Changing it invalidates every previously
/// issued encoded ID — so treat it like a long-lived signing key, not a rotating one.
/// </para>
/// </summary>
public sealed class IdEncoderOptions
{
    public const string SectionName = "IdEncoder";

    /// <summary>Secret used to derive the AES key for encoding entity IDs.</summary>
    public string Secret { get; init; } = string.Empty;
}
