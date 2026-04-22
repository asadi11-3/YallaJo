using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.ExternalAuth;

/// <summary>
/// Validates signed external-provider tickets produced by the Web BFF.
///
/// <para>
/// Implementations MUST verify (in this order):
/// </para>
/// <list type="number">
///   <item><description>HMAC signature using the shared secret.</description></item>
///   <item><description>Issuer / audience / algorithm (defense in depth against
///   <c>alg=none</c> and token-confusion attacks).</description></item>
///   <item><description>Not-before and not-after windows (short TTL).</description></item>
///   <item><description>Provider is in the configured allow-list.</description></item>
///   <item><description>Required claims (<c>provider</c>, <c>provider_user_id</c>,
///   <c>jti</c>) are present and well-formed.</description></item>
/// </list>
///
/// <para>
/// Replay protection (one-time-use of <c>jti</c>) is NOT performed here — the
/// command handler consumes the nonce through <see cref="IExternalAuthNonceStore"/>
/// so that verification stays idempotent for telemetry / logging paths.
/// </para>
/// </summary>
public interface IExternalAuthTicketVerifier
{
    Result<ExternalAuthTicket> Verify(string ticket);
}
