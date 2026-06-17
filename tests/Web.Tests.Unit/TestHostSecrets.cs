using Microsoft.AspNetCore.Hosting;

namespace Web.Tests.Unit;

/// <summary>
/// Patch 0A.2 — supplies safe, FAKE secrets to the Web test host so that it boots
/// without depending on developer-machine user-secrets or environment variables.
///
/// <para>
/// The Web host does not run the API's <c>StartupSecretGuards</c>, but its
/// external-auth wiring validates that <c>ExternalAuth:SigningKey</c> is present
/// and at least 32 bytes. After Patch 0A blanked the appsettings secrets, the Web
/// smoke factories only booted because the developer machine had user-secrets set.
/// This helper makes them boot deterministically on a clean checkout / CI.
/// </para>
///
/// <para>
/// These values are throw-away test keys, NOT real secrets. They are injected via
/// <see cref="IWebHostBuilder.UseSetting"/>, which lands in host configuration and
/// overrides the (now-blank) appsettings placeholders.
/// </para>
/// </summary>
internal static class TestHostSecrets
{
    // Long fake keys, each well over the 32-byte minimum enforced by the
    // external-auth options validator. Clearly labelled as test-only.
    internal const string FakeExternalAuthSigningKey =
        "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

    /// <summary>
    /// Injects the secrets required for the Web host to boot. Call this from a test
    /// factory's <c>ConfigureWebHost</c>.
    /// </summary>
    internal static IWebHostBuilder UseFakeTestSecrets(this IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);

        return builder;
    }
}
