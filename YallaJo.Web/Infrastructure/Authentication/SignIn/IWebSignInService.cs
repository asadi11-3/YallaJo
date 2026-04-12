namespace YallaJo.Web.Infrastructure.Authentication.SignIn;

/// <summary>
/// Manages the web app's authenticated cookie session.
/// Called after a successful login or verify-email to establish the session,
/// and on logout to tear it down.
/// </summary>
public interface IWebSignInService
{
    /// <summary>
    /// Creates an encrypted cookie session containing the user's identity
    /// and the access + refresh tokens needed for backend API calls.
    /// </summary>
    Task SignInAsync(
        Guid userId,
        string accessToken,
        string refreshToken,
        DateTime refreshTokenExpiresAt);

    /// <summary>
    /// Clears the authentication cookie and ends the web session.
    /// Should be called even when the backend logout API call fails.
    /// </summary>
    Task SignOutAsync();
}
