using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Models.Notifications;
using YallaJo.Web.Features.Notifications;
using YallaJo.Web.Infrastructure.Authorization;
using AccountsNotificationsController = YallaJo.Web.Areas.Accounts.Controllers.NotificationsController;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1B — mapper / link-resolver correctness and Accounts NotificationsController
/// attribute wiring (authorize + permission gates + anti-forgery on delete).
/// </summary>
public sealed class NotificationsInboxModelTests
{
    // ── NotificationLinkResolver ───────────────────────────────────────────────

    [Fact]
    public void Resolve_BookingEntity_ReturnsAccountsBookingDetailRoute()
    {
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        NotificationLinkResolver.Resolve("Booking", id)
            .Should().Be($"/accounts/bookings/{id}");
    }

    [Fact]
    public void Resolve_TourBookingEntity_ReturnsAccountsBookingDetailRoute()
    {
        var id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        NotificationLinkResolver.Resolve("TourBooking", id)
            .Should().Be($"/accounts/bookings/{id}");
    }

    [Theory]
    [InlineData("Tour")]
    [InlineData("Place")]
    [InlineData("Business")]
    [InlineData("Review")]
    [InlineData("Invoice")]
    [InlineData("Unknown")]
    public void Resolve_UnsupportedEntityType_ReturnsNull(string entityType)
    {
        NotificationLinkResolver.Resolve(entityType, Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void Resolve_NullEntityId_ReturnsNull()
    {
        NotificationLinkResolver.Resolve("Booking", null).Should().BeNull();
    }

    [Fact]
    public void Resolve_EmptyEntityId_ReturnsNull()
    {
        NotificationLinkResolver.Resolve("Booking", Guid.Empty).Should().BeNull();
    }

    [Fact]
    public void Resolve_NullEntityType_ReturnsNull()
    {
        NotificationLinkResolver.Resolve(null, Guid.NewGuid()).Should().BeNull();
    }

    // ── ToIsRead mapping ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("unread", false)]
    [InlineData("read", true)]
    [InlineData("all", null)]
    [InlineData("", null)]
    public void ToIsRead_MapsStatusCorrectly(string status, bool? expected)
    {
        new NotificationInboxFilterVm { Status = status }.ToIsRead().Should().Be(expected);
    }

    // ── Mapper ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ToInboxVm_PreservesCursorAndUnread_AndMapsRows()
    {
        var page = new NotificationPageResponse
        {
            Items = new[]
            {
                new NotificationItemResponse
                {
                    Id = Guid.NewGuid(), Type = "BookingConfirmed", Title = "T", Body = "B",
                    IsRead = false, EntityType = "Booking",
                    EntityId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    CreatedAt = DateTime.UtcNow,
                },
            },
            NextCursor = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

        var vm = NotificationsMapper.ToInboxVm(page, new NotificationInboxFilterVm(), unreadCount: 5);

        vm.UnreadCount.Should().Be(5);
        vm.NextCursor.Should().Be(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        vm.Items.Should().ContainSingle();
        vm.Items[0].IsRead.Should().BeFalse();
        vm.Items[0].LinkUrl.Should().Be("/accounts/bookings/99999999-9999-9999-9999-999999999999");
    }

    // ── Controller attribute wiring ────────────────────────────────────────────

    [Fact]
    public void Controller_IsAuthorized()
    {
        typeof(AccountsNotificationsController)
            .GetCustomAttribute<AuthorizeAttribute>()
            .Should().NotBeNull("the inbox must require an authenticated user");
    }

    [Fact]
    public void IndexAction_RequiresNotificationReadPermission()
    {
        var method = typeof(AccountsNotificationsController).GetMethod("Index")!;
        var attr = method.GetCustomAttribute<RequirePermissionAttribute>();
        attr.Should().NotBeNull();
        attr!.Permission.Should().Be(WebPermission.Notification.Read);
    }

    [Fact]
    public void DeleteAction_RequiresNotificationDeletePermission_AndAntiForgery()
    {
        var method = typeof(AccountsNotificationsController).GetMethod("Delete")!;

        method.GetCustomAttribute<RequirePermissionAttribute>()!
            .Permission.Should().Be(WebPermission.Notification.Delete);
        method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()
            .Should().NotBeNull("delete is a destructive POST and must be anti-forgery protected");
        method.GetCustomAttribute<HttpPostAttribute>()
            .Should().NotBeNull("delete must be POST-only");
    }
}
