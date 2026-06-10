namespace Auth.Application.Queries.ListLinkedProviders;

/// <summary>
/// Safe projection of a linked external provider for the account-security UI.
/// Deliberately EXCLUDES <c>ProviderUserId</c> (the provider-side subject identifier),
/// tokens, and any raw provider payloads — clients only ever need the link's own id
/// (for unlink), the provider name, the provider-reported e-mail (masked client-side)
/// and when it was linked.
/// </summary>
public sealed record LinkedProviderDto(
    Guid ProviderId,
    string Provider,
    string? ProviderEmail,
    DateTime LinkedAt);
