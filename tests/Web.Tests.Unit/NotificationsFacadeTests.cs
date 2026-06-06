using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Areas.Accounts.Models.Notifications;
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1B — verifies <see cref="NotificationsFacade"/> inbox composition, safe-degrade,
/// and delete error mapping.
/// </summary>
public sealed class NotificationsFacadeTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _route;
        public List<HttpRequestMessage> Calls { get; } = new();

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> route) => _route = route;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request);
            return Task.FromResult(_route(request));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private static NotificationsFacade CreateFacade(Func<HttpRequestMessage, HttpResponseMessage> route)
    {
        var handler = new StubHandler(route);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var api = new NotificationsApiClient(new ApiClient(http, NullLogger<ApiClient>.Instance));
        return new NotificationsFacade(api, NullLogger<NotificationsFacade>.Instance);
    }

    private const string TwoItemsPage = """
    {
      "items": [
        { "id": "11111111-1111-1111-1111-111111111111", "type": "BookingConfirmed", "title": "Booking confirmed", "body": "Your tour is booked", "isRead": false, "entityType": "Booking", "entityId": "99999999-9999-9999-9999-999999999999", "createdAt": "2026-01-01T10:00:00Z" },
        { "id": "22222222-2222-2222-2222-222222222222", "type": "ReviewPosted", "title": "New review", "body": "", "isRead": true, "createdAt": "2026-01-02T10:00:00Z" }
      ],
      "nextCursor": "33333333-3333-3333-3333-333333333333",
      "totalCount": null
    }
    """;

    [Fact]
    public async Task GetInboxAsync_Success_MapsItemsCursorAndUnread()
    {
        var facade = CreateFacade(r =>
            r.RequestUri!.AbsolutePath.Contains("unread-count")
                ? Json(HttpStatusCode.OK, "3")
                : Json(HttpStatusCode.OK, TwoItemsPage));

        var vm = await facade.GetInboxAsync(new NotificationInboxFilterVm { Status = "all" }, cursor: null);

        vm.LoadError.Should().BeNull();
        vm.Items.Should().HaveCount(2);
        vm.UnreadCount.Should().Be(3);
        vm.NextCursor.Should().Be(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        vm.HasNextPage.Should().BeTrue();
        // Booking entity resolves to a link; Review does not.
        vm.Items[0].LinkUrl.Should().Be("/accounts/bookings/99999999-9999-9999-9999-999999999999");
        vm.Items[1].LinkUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetInboxAsync_StatusUnread_SendsIsReadFalseToApi()
    {
        HttpRequestMessage? listCall = null;
        var handler = new StubHandler(r =>
        {
            if (!r.RequestUri!.AbsolutePath.Contains("unread-count")) listCall = r;
            return r.RequestUri!.AbsolutePath.Contains("unread-count")
                ? Json(HttpStatusCode.OK, "0")
                : Json(HttpStatusCode.OK, """{ "items": [], "nextCursor": null }""");
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        var sut = new NotificationsFacade(
            new NotificationsApiClient(new ApiClient(http, NullLogger<ApiClient>.Instance)),
            NullLogger<NotificationsFacade>.Instance);

        await sut.GetInboxAsync(new NotificationInboxFilterVm { Status = "unread" }, cursor: null);

        listCall!.RequestUri!.Query.Should().Contain("isRead=false");
    }

    [Fact]
    public async Task GetInboxAsync_ApiFailure_DegradesToEmptyWithLoadError()
    {
        var facade = CreateFacade(r =>
            r.RequestUri!.AbsolutePath.Contains("unread-count")
                ? Json(HttpStatusCode.OK, "0")
                : Json(HttpStatusCode.InternalServerError, """{ "title": "boom" }"""));

        var vm = await facade.GetInboxAsync(new NotificationInboxFilterVm(), cursor: null);

        vm.LoadError.Should().NotBeNullOrWhiteSpace();
        vm.Items.Should().BeEmpty();
        vm.HasItems.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await facade.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_Forbidden_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.Forbidden, """{ "title": "Forbidden" }"""));

        var result = await facade.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("permission");
    }

    [Fact]
    public async Task DeleteAsync_NotFound_MapsToFriendlyMessage()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.NotFound, """{ "title": "NotFound" }"""));

        var result = await facade.DeleteAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error!.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task MarkAllReadAsync_Success_ReturnsOk()
    {
        var facade = CreateFacade(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await facade.MarkAllReadAsync();

        result.IsSuccess.Should().BeTrue();
    }
}
