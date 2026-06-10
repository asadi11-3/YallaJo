namespace Auth.Application.Queries.ListLinkedProviders;

public sealed record LinkedProviderDto(
    Guid ProviderId,
    string Provider,
    string? ProviderEmail,
    DateTime LinkedAt);
