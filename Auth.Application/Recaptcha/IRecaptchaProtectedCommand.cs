namespace Auth.Application.Recaptcha;

/// <summary>
/// Marker / contract for commands that require reCAPTCHA v3 verification.
///
/// <para>
/// A single <see cref="RecaptchaValidationBehavior{TRequest, TResponse}"/>
/// MediatR pipeline step intercepts every command implementing this interface
/// and invokes <see cref="IRecaptchaVerifier"/> BEFORE the handler runs. This
/// centralizes the check — individual handlers cannot forget it, bypass it,
/// or duplicate the logic.
/// </para>
/// </summary>
public interface IRecaptchaProtectedCommand
{
    /// <summary>Client-side reCAPTCHA v3 token (opaque to the server).</summary>
    string RecaptchaToken { get; }

    /// <summary>
    /// Expected action name (see <see cref="RecaptchaActions"/>). The verifier
    /// rejects the request if Google's response does not carry this action.
    /// </summary>
    string RecaptchaAction { get; }
}
