using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Services;
using WebPermission = YallaJo.Web.Infrastructure.Authorization.WebPermission;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1B — verifies <see cref="NotificationsApiClient"/> hits the correct HTTP verb +
/// URL + query string for every notification endpoint. The URLs are the contract
/// between the Web inbox/bell and the Messaging module endpoints.
/// </summary>
public sealed class NotificationsApiClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private static (NotificationsApiClient Sut, CapturingHandler Handler) CreateSut()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new ApiClient(http, NullLogger<ApiClient>.Instance);
        return (new NotificationsApiClient(api), handler);
    }

    [Fact]
    public async Task GetListAsync_WithAllFilters_BuildsExpectedQueryString()
    {
        var (sut, handler) = CreateSut();
        var cursor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        await sut.GetListAsync(
            type: "BookingConfirmed",
            isRead: false,
            from: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            to: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            cursor: cursor,
            pageSize: 20);

        var uri = handler.Calls[0].RequestUri!;
        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        uri.AbsolutePath.Should().Be("/api/v1/notifications/");
        var q = uri.Query;
        q.Should().Contain("pageSize=20");
        q.Should().Contain("type=BookingConfirmed");
        q.Should().Contain("isRead=false");
        q.Should().Contain("from=");
        q.Should().Contain("to=");
        q.Should().Contain("cursor=dddddddd-dddd-dddd-dddd-dddddddddddd");
    }

    [Fact]
    public async Task GetListAsync_NoFilters_OmitsEmptyParams()
    {
        var (sut, handler) = CreateSut();

        await sut.GetListAsync();

        var q = handler.Calls[0].RequestUri!.Query;
        q.Should().Contain("pageSize=20");
        q.Should().NotContain("type=");
        q.Should().NotContain("isRead=");
        q.Should().NotContain("cursor=");
        q.Should().NotContain("from=");
        q.Should().NotContain("to=");
    }

    [Fact]
    public async Task GetRecentAsync_TargetsListRoot_WithPageSizeOnly()
    {
        var (sut, handler) = CreateSut();

        await sut.GetRecentAsync(10);

        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/notifications/");
        handler.Calls[0].RequestUri!.Query.Should().Contain("pageSize=10");
        handler.Calls[0].RequestUri!.Query.Should().NotContain("isRead=");
    }

    [Fact]
    public async Task GetUnreadCountAsync_TargetsUnreadCountRoute()
    {
        var (sut, handler) = CreateSut();

        await sut.GetUnreadCountAsync();

        handler.Calls[0].Method.Should().Be(HttpMethod.Get);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/notifications/unread-count");
    }

    [Fact]
    public async Task MarkReadAsync_PostsToIdReadRoute()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await sut.MarkReadAsync(id);

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/notifications/{id}/read");
    }

    [Fact]
    public async Task MarkAllReadAsync_PostsToReadAllRoute()
    {
        var (sut, handler) = CreateSut();

        await sut.MarkAllReadAsync();

        handler.Calls[0].Method.Should().Be(HttpMethod.Post);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be("/api/v1/notifications/read-all");
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteToIdRoute()
    {
        var (sut, handler) = CreateSut();
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await sut.DeleteAsync(id);

        handler.Calls[0].Method.Should().Be(HttpMethod.Delete);
        handler.Calls[0].RequestUri!.AbsolutePath.Should().Be($"/api/v1/notifications/{id}");
    }

    [Fact]
    public void Notification_Delete_permission_constant_matches_backend_format()
    {
        WebPermission.Notification.Read.Should().Be("Permission.Notification.Read");
        WebPermission.Notification.Update.Should().Be("Permission.Notification.Update");
        WebPermission.Notification.Delete.Should().Be("Permission.Notification.Delete");
    }
}
