using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Lifecycle.Requests;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5B — verifies <see cref="LifecycleApiClient"/> hits the right
/// HTTP verb + URL + body shape for every admin lifecycle endpoint.
/// The URLs are the contract between the Web and the
/// Phase 3A/3B/3C admin endpoints, so any drift would silently break
/// the buttons on the User Details page.
/// </summary>
public sealed class LifecycleApiClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            Bodies.Add(request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static (LifecycleApiClient Sut, CapturingHandler Handler) CreateSut()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new LifecycleApiClient(api), handler);
    }

    [Fact]
    public async Task SuspendAsync_ShouldCall_PATCH_AdminSuspendEndpoint()
    {
        var (sut, handler) = CreateSut();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await sut.SuspendAsync(userId);

        result.IsSuccess.Should().BeTrue();
        handler.Calls.Should().HaveCount(1);
        handler.Calls[0].Method.Should().Be(HttpMethod.Patch);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/auth/admin/users/11111111-1111-1111-1111-111111111111/suspend");
    }

    [Fact]
    public async Task ReactivateAsync_ShouldCall_PATCH_AdminReactivateEndpoint()
    {
        var (sut, handler) = CreateSut();
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await sut.ReactivateAsync(userId);

        handler.Calls[0].Method.Should().Be(HttpMethod.Patch);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/auth/admin/users/22222222-2222-2222-2222-222222222222/reactivate");
    }

    [Fact]
    public async Task ArchiveAsync_ShouldCall_PATCH_AdminArchiveEndpoint()
    {
        var (sut, handler) = CreateSut();
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await sut.ArchiveAsync(userId);

        handler.Calls[0].Method.Should().Be(HttpMethod.Patch);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/auth/admin/users/33333333-3333-3333-3333-333333333333/archive");
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldCall_POST_WithReasonInBody()
    {
        var (sut, handler) = CreateSut();
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        await sut.ResetPasswordAsync(userId, new AdminResetPasswordRequest { Reason = "left company" });

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/auth/admin/users/44444444-4444-4444-4444-444444444444/reset-password");

        // Body shape — the API expects { reason: "..." } (camelCase per ApiClient's serializer).
        var body = handler.Bodies[0];
        body.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(body!);
        doc.RootElement.GetProperty("reason").GetString().Should().Be("left company");
    }

    [Fact]
    public async Task ResetPasswordAsync_NullReason_ShouldOmitReasonFromBody()
    {
        // ApiClient serializes with WhenWritingNull → null props are dropped.
        var (sut, handler) = CreateSut();

        await sut.ResetPasswordAsync(Guid.NewGuid(), new AdminResetPasswordRequest { Reason = null });

        var body = handler.Bodies[0];
        body.Should().NotBeNull();
        using var doc = JsonDocument.Parse(body!);
        doc.RootElement.TryGetProperty("reason", out _).Should().BeFalse(
            "null Reason must not travel over the wire — keeps the audit row clean");
    }

    [Fact]
    public async Task ReassignAsync_ShouldCall_POST_WithNewEmailAndReason()
    {
        var (sut, handler) = CreateSut();
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        await sut.ReassignAsync(userId, new AdminReassignAccountRequest
        {
            NewEmail = "new@example.test",
            Reason   = "ownership transfer",
        });

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath
            .Should().Be("/api/v1/auth/admin/users/55555555-5555-5555-5555-555555555555/reassign");

        var body = handler.Bodies[0];
        body.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(body!);
        doc.RootElement.GetProperty("newEmail").GetString().Should().Be("new@example.test");
        doc.RootElement.GetProperty("reason").GetString().Should().Be("ownership transfer");
    }
}
