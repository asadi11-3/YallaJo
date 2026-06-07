using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Services;
using WebPermission = YallaJo.Web.Infrastructure.Authorization.WebPermission;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1C — verifies <see cref="SupportApiClient"/> (Accounts) hits the correct HTTP
/// verb + URL + body for every user-facing support endpoint. The URLs are the contract
/// between the Web Support pages and the Messaging module's /api/v1/support endpoints.
/// </summary>
public sealed class SupportApiClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (SupportApiClient Sut, CapturingHandler Handler) CreateSut()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new SupportApiClient(api), handler);
    }

    [Fact]
    public async Task GetTicketsAsync_WithCursor_BuildsExpectedQueryString()
    {
        var (sut, handler) = CreateSut();
        var cursor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        await sut.GetTicketsAsync(cursor: cursor, pageSize: 20);

        var uri = handler.Calls[0].RequestUri!;
        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        uri.AbsolutePath.Should().Be("/api/v1/support/tickets");
        uri.Query.Should().Contain("pageSize=20");
        uri.Query.Should().Contain("cursor=dddddddd-dddd-dddd-dddd-dddddddddddd");
    }

    [Fact]
    public async Task GetTicketsAsync_NoCursor_OmitsCursor_AndSendsNoStatusOrCategory()
    {
        var (sut, handler) = CreateSut();

        await sut.GetTicketsAsync();

        var q = handler.Calls[0].RequestUri!.Query;
        q.Should().Contain("pageSize=20");
        q.Should().NotContain("cursor=");
        // v1 intentionally sends NO status/category — backend ignores them for non-admins.
        q.Should().NotContain("status=");
        q.Should().NotContain("category=");
    }

    [Fact]
    public async Task GetTicketAsync_TargetsTicketByIdRoute()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await sut.GetTicketAsync(id);

        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/support/tickets/{id:D}");
    }

    [Fact]
    public async Task PostMessageAsync_PostsBodyToMessagesRoute_WithoutIsInternal()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await sut.PostMessageAsync(id, "Any update on this?");

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/support/tickets/{id:D}/messages");

        using var doc = JsonDocument.Parse(handler.Bodies[0]!);
        doc.RootElement.GetProperty("body").GetString().Should().Be("Any update on this?");
        // Users never send IsInternal — the field must not be present in the payload.
        doc.RootElement.TryGetProperty("isInternal", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CloseAsync_PostsRowVersionToCloseRoute()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await sut.CloseAsync(id, "AAAAAAAAB9E=");

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/support/tickets/{id:D}/close");

        using var doc = JsonDocument.Parse(handler.Bodies[0]!);
        doc.RootElement.GetProperty("rowVersion").GetString().Should().Be("AAAAAAAAB9E=");
    }

    [Fact]
    public void SupportTicket_permission_constants_match_backend_format()
    {
        WebPermission.SupportTicket.Read.Should().Be("Permission.SupportTicket.Read");
        WebPermission.SupportTicket.Close.Should().Be("Permission.SupportTicket.Close");
    }
}
