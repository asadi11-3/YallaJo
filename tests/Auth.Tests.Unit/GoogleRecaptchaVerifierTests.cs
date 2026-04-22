using System.Net;
using System.Text;
using System.Text.Json;
using Auth.Infrastructure.Recaptcha;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Tests.Unit;

public sealed class GoogleRecaptchaVerifierTests
{
    private static RecaptchaOptions DefaultOpts(double minScore = 0.5) => new()
    {
        SecretKey = "secret-key-please-keep-safe",
        MinimumScore = minScore,
        VerifyEndpoint = "https://example.test/siteverify",
        TimeoutSeconds = 5,
        BypassForTesting = false,
    };

    /// <summary>
    /// Minimal test double for IHttpClientFactory that serves responses minted
    /// by an in-memory handler. Avoids bringing a Moq/NSubstitute-specific
    /// HttpMessageHandler into the test surface.
    /// </summary>
    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _factory;
        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) => _factory = factory;

        public List<HttpRequestMessage> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return _factory(request);
        }
    }

    private static StubHandler JsonResponse(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(req => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        }));

    private static GoogleRecaptchaVerifier Create(
        StubHandler handler, RecaptchaOptions? opts = null) =>
        new(new StubHttpClientFactory(handler),
            Options.Create(opts ?? DefaultOpts()),
            NullLogger<GoogleRecaptchaVerifier>.Instance);

    [Fact]
    public async Task VerifyAsync_ShouldSucceed_WhenGoogleReturnsValidTokenAndMatchingAction()
    {
        var handler = JsonResponse(new
        {
            success = true,
            score = 0.9,
            action = "login",
        });

        var sut = Create(handler);

        var result = await sut.VerifyAsync("token", "login", "1.2.3.4", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ShouldFail_WhenGoogleReturnsSuccessFalse()
    {
        var handler = JsonResponse(new
        {
            success = false,
            score = 0.9,
            action = "login",
            ErrorCodes = new[] { "invalid-input-response" },
        });

        var sut = Create(handler);

        var result = await sut.VerifyAsync("token", "login", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task VerifyAsync_ShouldFail_WhenScoreBelowThreshold()
    {
        var handler = JsonResponse(new
        {
            success = true,
            score = 0.3,
            action = "login",
        });

        var sut = Create(handler, DefaultOpts(minScore: 0.5));

        var result = await sut.VerifyAsync("token", "login", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ShouldFail_WhenActionMismatch()
    {
        // Token was minted for "login" but the endpoint expects "register" — MUST reject.
        var handler = JsonResponse(new
        {
            success = true,
            score = 0.9,
            action = "login",
        });

        var sut = Create(handler);

        var result = await sut.VerifyAsync("token", "register", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ShouldFail_WhenTokenMissing()
    {
        var handler = JsonResponse(new { success = true, score = 1.0, action = "login" });
        var sut = Create(handler);

        var result = await sut.VerifyAsync(string.Empty, "login", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        handler.Calls.Should().BeEmpty("verifier must not call Google when the token is empty");
    }

    [Fact]
    public async Task VerifyAsync_ShouldFail_WhenExpectedActionMissing()
    {
        var handler = JsonResponse(new { success = true });
        var sut = Create(handler);

        var result = await sut.VerifyAsync("token", string.Empty, null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_ShouldFailClosed_WhenGoogleReturnsNon2xx()
    {
        var handler = JsonResponse(new { }, HttpStatusCode.InternalServerError);
        var sut = Create(handler);

        var result = await sut.VerifyAsync("token", "login", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ShouldFailClosed_WhenNetworkExceptionThrown()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("boom"));
        var sut = Create(handler);

        var result = await sut.VerifyAsync("token", "login", null, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task VerifyAsync_ShouldSucceed_WhenBypassForTesting()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("must not call Google"));
        var opts = DefaultOpts();
        opts = new RecaptchaOptions
        {
            SecretKey = opts.SecretKey,
            MinimumScore = opts.MinimumScore,
            VerifyEndpoint = opts.VerifyEndpoint,
            TimeoutSeconds = opts.TimeoutSeconds,
            BypassForTesting = true,
        };
        var sut = Create(handler, opts);

        var result = await sut.VerifyAsync("anything", "login", null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().BeEmpty();
    }
}
