namespace Security.Contracts.Abstractions;

/// <summary>
/// Seed data for <see cref="IUserRegistrationService.RegisterExternalAsync"/>.
/// <para>
/// All fields come from the OAuth provider's claims — the caller is responsible
/// for confirming that the provider itself attested email verification
/// (e.g. Google's <c>email_verified=true</c>, or Meta returning an email at all).
/// </para>
/// </summary>
public sealed record ExternalUserRegistrationRequest(
    string Email,
    string FirstName,
    string LastName);
