using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace YallaJo.Authorization.IntegrationTests;

/// <summary>
/// Patch 0A.2 — a <see cref="WebApplicationFactory{TEntryPoint}"/> that boots the
/// real API host with safe, FAKE test secrets injected explicitly.
///
/// <para>
/// Previously these tests used a bare <c>WebApplicationFactory&lt;Program&gt;</c>,
/// which only booted because the developer machine happened to have user-secrets
/// configured. After Patch 0A blanked the appsettings secrets and added
/// <c>StartupSecretGuards</c>, the host would refuse to start on a clean checkout
/// or in CI. This factory supplies the required signing keys via host configuration
/// so the host boots deterministically without relying on ambient secrets.
/// </para>
///
/// <para>
/// The values are throw-away test keys, NOT real secrets. The production guard is
/// left strict and unmodified.
/// </para>
/// </summary>
public sealed class SecretsAwareApiFactory : WebApplicationFactory<Program>
{
    // Long fake keys, each well over the 32-byte minimum enforced by the
    // external-auth options validator. Clearly labelled as test-only.
    private const string FakeJwtKey =
        "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";

    private const string FakeExternalAuthSigningKey =
        "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", FakeJwtKey);
        builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);
    }
}
