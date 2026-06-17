using Microsoft.AspNetCore.Hosting;

namespace YallaJo.ContentCore.IntegrationTests;

/// <summary>
/// Patch 0A.2 — supplies safe, FAKE secrets to a test host so that
/// <c>StartupSecretGuards</c> (Patch 0A) can pass without depending on
/// developer-machine user-secrets or environment variables.
///
/// <para>
/// These values are deterministic throw-away test keys. They are NOT real
/// secrets and must never be used outside the test host. The production guard
/// is left strict and unmodified; the test host simply provides the required
/// configuration keys explicitly via <see cref="IWebHostBuilder.UseSetting"/>,
/// which lands in host configuration and overrides the (now-blank) appsettings
/// placeholders.
/// </para>
/// </summary>
internal static class TestHostSecrets
{
    // Long fake keys, each well over the 32-byte minimum enforced by the
    // external-auth options validator. Clearly labelled as test-only.
    internal const string FakeJwtKey =
        "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";

    internal const string FakeExternalAuthSigningKey =
        "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

    /// <summary>
    /// Injects the always-required signing secrets that the API startup guard
    /// validates. Call this from a test factory's <c>ConfigureWebHost</c>.
    /// </summary>
    internal static IWebHostBuilder UseFakeTestSecrets(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Jwt:Key", FakeJwtKey);
        builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

        return builder;
    }
}
