namespace YallaJo.Web.Infrastructure.Authentication.ExternalAuth;

/// <summary>
/// Claim names shared with the API verifier. Do not rename without updating
/// <c>Auth.Infrastructure.ExternalAuth.ExternalAuthTicketClaims</c>.
/// </summary>
public static class ExternalAuthTicketClaims
{
    public const string Provider = "provider";
    public const string ProviderUserId = "provider_user_id";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
}
