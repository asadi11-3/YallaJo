namespace Auth.Presentation.Endpoints.ExternalProvider.Models;

public sealed record LinkExternalProviderRequest(
    string Provider,
    string ProviderUserId,
    string? ProviderEmail);
