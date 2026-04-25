using Auth.Infrastructure.ExternalAuth;
using FluentAssertions;

namespace Auth.Tests.Unit;

public sealed class ExternalAuthOptionsValidatorTests
{
    private readonly ExternalAuthOptionsValidator _sut = new();

    private static ExternalAuthOptions Build(
        string signingKey = "test-signing-key-at-least-32-bytes-long!",
        string issuer = "iss",
        string audience = "aud",
        int ttlSeconds = 120,
        IReadOnlyList<string>? providers = null) => new()
    {
        SigningKey = signingKey,
        Issuer = issuer,
        Audience = audience,
        TicketLifetimeSeconds = ttlSeconds,
        AllowedProviders = providers ?? new[] { "google" },
    };

    [Fact]
    public void Validate_ShouldSucceed_ForFullyConfiguredOptions()
    {
        _sut.Validate(null, Build()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSigningKeyMissing()
    {
        _sut.Validate(null, Build(signingKey: string.Empty)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSigningKeyTooShort()
    {
        _sut.Validate(null, Build(signingKey: "short")).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIssuerMissing()
    {
        _sut.Validate(null, Build(issuer: string.Empty)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenAudienceMissing()
    {
        _sut.Validate(null, Build(audience: string.Empty)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTicketLifetimeNegative()
    {
        _sut.Validate(null, Build(ttlSeconds: 0)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTicketLifetimeTooLarge()
    {
        _sut.Validate(null, Build(ttlSeconds: 10_000)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenNoAllowedProviders()
    {
        _sut.Validate(null, Build(providers: Array.Empty<string>())).Failed.Should().BeTrue();
    }
}
