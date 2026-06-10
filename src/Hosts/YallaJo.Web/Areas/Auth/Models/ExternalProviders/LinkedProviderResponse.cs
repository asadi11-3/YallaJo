namespace YallaJo.Web.Areas.Auth.Models.ExternalProviders;

/// <summary>
/// Item of GET /api/v1/auth/external-providers. Mirrors the API's LinkedProviderDto —
/// the safe projection only (no ProviderUserId, tokens or raw provider payloads).
/// </summary>
public sealed class LinkedProviderResponse
{
    public Guid     ProviderId    { get; init; }
    public string   Provider      { get; init; } = string.Empty;
    public string?  ProviderEmail { get; init; }
    public DateTime LinkedAt      { get; init; }
}
