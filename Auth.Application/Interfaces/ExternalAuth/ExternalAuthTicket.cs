namespace Auth.Application.Interfaces.ExternalAuth;

public sealed record ExternalAuthTicket(
    Guid TicketId,
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerifiedByProvider,
    DateTime IssuedAt,
    DateTime ExpiresAt,
    string? FirstName = null,
    string? LastName = null);
