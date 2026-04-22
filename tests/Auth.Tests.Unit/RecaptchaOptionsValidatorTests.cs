using Auth.Infrastructure.Recaptcha;
using FluentAssertions;

namespace Auth.Tests.Unit;

public sealed class RecaptchaOptionsValidatorTests
{
    private readonly RecaptchaOptionsValidator _sut = new();

    private static RecaptchaOptions Build(
        string secret = "a-secret",
        double minScore = 0.5,
        int timeoutSeconds = 5,
        string verifyEndpoint = "https://www.google.com/recaptcha/api/siteverify",
        bool bypass = false) => new()
    {
        SecretKey = secret,
        MinimumScore = minScore,
        TimeoutSeconds = timeoutSeconds,
        VerifyEndpoint = verifyEndpoint,
        BypassForTesting = bypass,
    };

    [Fact]
    public void Validate_ShouldSucceed_ForFullyConfigured()
    {
        _sut.Validate(null, Build()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenBypassEvenIfOtherFieldsInvalid()
    {
        _sut.Validate(null, Build(secret: "", bypass: true)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSecretKeyMissingAndBypassOff()
    {
        _sut.Validate(null, Build(secret: "")).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenMinimumScoreOutOfRange()
    {
        _sut.Validate(null, Build(minScore: -0.1)).Failed.Should().BeTrue();
        _sut.Validate(null, Build(minScore: 1.5)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTimeoutInvalid()
    {
        _sut.Validate(null, Build(timeoutSeconds: 0)).Failed.Should().BeTrue();
        _sut.Validate(null, Build(timeoutSeconds: 60)).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEndpointNotHttps()
    {
        _sut.Validate(null, Build(verifyEndpoint: "http://google.com/siteverify"))
            .Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenEndpointNotAbsolute()
    {
        _sut.Validate(null, Build(verifyEndpoint: "/siteverify")).Failed.Should().BeTrue();
    }
}
