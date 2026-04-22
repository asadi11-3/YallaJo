namespace Auth.Application.ExternalAuth;

/// <summary>
/// One-time-use store for external-auth ticket IDs (<c>jti</c>).
///
/// <para>
/// Any ticket presented to the API must have its identifier atomically consumed
/// before the credentials it carries are accepted. A second presentation of the
/// same identifier — even during the short TTL window — must be rejected to
/// block replay of a captured ticket.
/// </para>
/// </summary>
public interface IExternalAuthNonceStore
{
    /// <summary>
    /// Atomically register <paramref name="ticketId"/> as consumed until
    /// <paramref name="expiresAt"/>. Returns <c>false</c> if the same identifier
    /// was already consumed (a replay attempt).
    /// </summary>
    Task<bool> TryConsumeAsync(Guid ticketId, DateTime expiresAt, CancellationToken ct = default);
}
