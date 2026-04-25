using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Regression tests for the transport-failure path in <see cref="ApiClient"/>.
///
/// <para>
/// When the API is unreachable the Web BFF used to bubble the raw
/// <c>HttpRequestException</c> / <c>SocketException</c> all the way to the
/// controller, producing a stack-trace page for the user (observed on the
/// "Completing sign-in…" OAuth interstitial). The client now translates every
/// transport-layer failure to a neutral <c>503</c> <see cref="Infrastructure.Api.Contracts.ApiResult"/>
/// so facade code can surface a tidy error message.
/// </para>
/// </summary>
public sealed class ApiClientTransportFailureTests
{
    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Func<Exception> _throw;
        public ThrowingHandler(Func<Exception> throwFactory) => _throw = throwFactory;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _throw();
        }
    }

    private static ApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") },
            NullLogger<ApiClient>.Instance);

    [Fact]
    public async Task PostAsync_ShouldReturn503_WhenApiIsUnreachable()
    {
        var sut = CreateClient(new ThrowingHandler(() =>
            // This is exactly the exception type the framework raises when a
            // TCP connection is refused — matches the production stack trace.
            new HttpRequestException(
                "No connection could be made because the target machine actively refused it.",
                new SocketException((int)SocketError.ConnectionRefused))));

        var result = await sut.PostAsync<object>(
            "/api/v1/auth/external-providers/login", new { }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(503);
        result.Error.Should().NotBeNullOrWhiteSpace();
        // Must not leak infrastructure details (hostname, port, socket codes).
        result.Error.Should().NotContain("localhost", "error must not leak API address");
        result.Error.Should().NotContain("57065");
        result.Error.Should().NotContain("socket", because: "the generic message must not leak socket-level details");
    }

    [Fact]
    public async Task PostAsync_NoBody_ShouldReturn503_WhenApiIsUnreachable()
    {
        var sut = CreateClient(new ThrowingHandler(() =>
            new HttpRequestException("refused", new SocketException((int)SocketError.ConnectionRefused))));

        var result = await sut.PostAsync("/api/v1/auth/link", new { }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetAsync_ShouldReturn503_OnHttpClientTimeout()
    {
        // HttpClient.Timeout surfaces as TaskCanceledException without the
        // caller's CancellationToken firing — IsTransportFailure catches it.
        var sut = CreateClient(new ThrowingHandler(() => new TaskCanceledException("timeout")));

        var result = await sut.GetAsync<object>("/api/v1/test", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task PostAsync_ShouldPropagateCancellation_WhenCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateClient(new ThrowingHandler(() =>
            // Simulate HttpClient translating cancellation to TaskCanceledException.
            new TaskCanceledException("caller cancelled", null, cts.Token)));
        cts.Cancel();

        // When the CALLER cancels, we must propagate — NOT swallow as 503.
        // Facades rely on this to stop processing when the user navigates away.
        Func<Task> act = () => sut.PostAsync<object>("/api/v1/test", null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturn503_OnSocketException()
    {
        var sut = CreateClient(new ThrowingHandler(() =>
            new HttpRequestException("refused", new SocketException((int)SocketError.ConnectionRefused))));

        var result = await sut.DeleteAsync("/api/v1/auth/external-providers/abc", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(503);
    }
}
