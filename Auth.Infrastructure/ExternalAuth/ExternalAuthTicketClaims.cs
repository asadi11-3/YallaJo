namespace Auth.Infrastructure.ExternalAuth;

public static class ExternalAuthTicketClaims
{
    public const string Provider = "provider";
    public const string ProviderUserId = "provider_user_id";

    public const string Email = "email";

    public const string EmailVerified = "email_verified";
    public const string GivenName = "given_name";

    public const string FamilyName = "family_name";
}
