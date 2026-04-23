namespace Auth.Application.ExternalAuth;

public interface IExternalAuthNonceStore
{
    Task<bool> TryConsumeAsync(Guid ticketId, DateTime expiresAt, CancellationToken ct = default);
}
