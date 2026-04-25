using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Infrastructure.ExternalAuth;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

public sealed class ExternalAuthTicketVerifierTests
{
    private const string Key = "test-signing-key-at-least-32-bytes-long!";
    private const string WrongKey = "other-signing-key-at-least-32-bytes-long!";

    private static ExternalAuthOptions DefaultOptions() => new()
    {
        SigningKey = Key,
        Issuer = "YallaJo.Web",
        Audience = "YallaJo.Api",
        TicketLifetimeSeconds = 120,
        AllowedProviders = new[] { "google", "facebook" },
    };

    private static ExternalAuthTicketVerifier CreateSut(ExternalAuthOptions? opts = null) =>
        new(Options.Create(opts ?? DefaultOptions()));

    private static string BuildToken(
        string key = Key,
        string issuer = "YallaJo.Web",
        string audience = "YallaJo.Api",
        string provider = "google",
        string providerUserId = "g-1",
        string? email = "u@example.com",
        bool emailVerified = true,
        string? jti = null,
        DateTime? notBefore = null,
        DateTime? expires = null,
        string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti ?? Guid.NewGuid().ToString()),
            new(ExternalAuthTicketClaims.Provider, provider),
            new(ExternalAuthTicketClaims.ProviderUserId, providerUserId),
            new(ExternalAuthTicketClaims.EmailVerified, emailVerified ? "true" : "false", ClaimValueTypes.Boolean),
        };

        if (email is not null)
            claims.Add(new Claim(ExternalAuthTicketClaims.Email, email));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow,
            expires: expires ?? DateTime.UtcNow.AddSeconds(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public void Verify_ShouldReturnSuccess_ForValidTicket()
    {
        var sut = CreateSut();
        var token = BuildToken();

        var result = sut.Verify(token);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Provider.Should().Be("google");
        result.Value.ProviderUserId.Should().Be("g-1");
        result.Value.Email.Should().Be("u@example.com");
        result.Value.EmailVerifiedByProvider.Should().BeTrue();
        result.Value.TicketId.Should().NotBeEmpty();
    }

    [Fact]
    public void Verify_ShouldReject_WhenSignatureInvalid()
    {
        var sut = CreateSut();
        var token = BuildToken(key: WrongKey);

        var result = sut.Verify(token);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    [Fact]
    public void Verify_ShouldReject_WhenIssuerWrong()
    {
        var sut = CreateSut();
        var token = BuildToken(issuer: "evil");

        var result = sut.Verify(token);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenAudienceWrong()
    {
        var sut = CreateSut();
        var token = BuildToken(audience: "evil");

        var result = sut.Verify(token);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenExpired()
    {
        var sut = CreateSut();
        var token = BuildToken(
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5));

        var result = sut.Verify(token);

        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("expired", because: "expiry must be surfaced clearly");
    }

    [Fact]
    public void Verify_ShouldReject_WhenProviderNotAllowed()
    {
        var sut = CreateSut();
        var token = BuildToken(provider: "instagram");

        var result = sut.Verify(token);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenJtiMissing()
    {
        // Build token WITHOUT jti by bypassing the helper.
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "YallaJo.Web",
            audience: "YallaJo.Api",
            claims: new[]
            {
                new Claim(ExternalAuthTicketClaims.Provider, "google"),
                new Claim(ExternalAuthTicketClaims.ProviderUserId, "g-1"),
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddSeconds(60),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var sut = CreateSut();
        var result = sut.Verify(jwt);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenProviderUserIdMissing()
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "YallaJo.Web",
            audience: "YallaJo.Api",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ExternalAuthTicketClaims.Provider, "google"),
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddSeconds(60),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var sut = CreateSut();
        var result = sut.Verify(jwt);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenMalformed()
    {
        var sut = CreateSut();
        var result = sut.Verify("not-a-jwt");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenEmpty()
    {
        var sut = CreateSut();
        var result = sut.Verify(string.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReject_WhenLifetimeExceedsConfiguredMax()
    {
        var sut = CreateSut(new ExternalAuthOptions
        {
            SigningKey = Key,
            Issuer = "YallaJo.Web",
            Audience = "YallaJo.Api",
            TicketLifetimeSeconds = 10,
            AllowedProviders = new[] { "google" },
        });

        // Mint a 10-minute ticket when max is 10 seconds.
        var token = BuildToken(
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(10));

        var result = sut.Verify(token);

        result.IsFailure.Should().BeTrue();
        result.Error!.Message.Should().Contain("lifetime", because: "the check must identify the TTL violation");
    }

    [Fact]
    public void Verify_ShouldReject_UnsupportedAlgorithms()
    {
        // The verifier pins HS256 via ValidAlgorithms — tokens signed with a
        // different symmetric algorithm (e.g. HS384) must be rejected to
        // prevent algorithm-confusion attacks.
        var sut = CreateSut();

        // HS384 needs a 48-byte key.
        var largerKey = new string('k', 48);
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(largerKey)),
            SecurityAlgorithms.HmacSha384);

        var token = new JwtSecurityToken(
            issuer: "YallaJo.Web",
            audience: "YallaJo.Api",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ExternalAuthTicketClaims.Provider, "google"),
                new Claim(ExternalAuthTicketClaims.ProviderUserId, "g-1"),
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddSeconds(60),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var result = sut.Verify(jwt);

        result.IsFailure.Should().BeTrue();
    }
}
