using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Recaptcha;

/// <summary>
/// Server-side reCAPTCHA v3 verifier.
///
/// <para>Implementations must:</para>
/// <list type="bullet">
///   <item><description>Call Google's <c>siteverify</c> endpoint with the
///   configured secret key and the client-supplied token.</description></item>
///   <item><description>Reject if <c>success != true</c>.</description></item>
///   <item><description>Reject if <c>action != expectedAction</c> — prevents
///   tokens issued for one flow from being replayed against another.</description></item>
///   <item><description>Reject if <c>score &lt; configured minimum</c>.</description></item>
///   <item><description>Never throw for a normal "bad token" outcome — return
///   a failure <see cref="Result"/> so the caller surfaces a generic 400.</description></item>
/// </list>
/// </summary>
public interface IRecaptchaVerifier
{
    Task<Result> VerifyAsync(
        string token,
        string expectedAction,
        string? remoteIp = null,
        CancellationToken ct = default);
}
