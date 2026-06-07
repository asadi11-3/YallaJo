using FluentAssertions;
using YallaJo.Web.Areas.Accounts.Models.Support;

namespace Web.Tests.Unit;

/// <summary>
/// FE-1C — verifies <see cref="SupportMapper"/> projections: list rows, ticket thread,
/// the "internal staff notes are hidden from the owner" invariant, the CanReply/CanClose
/// affordance gating, and the RowVersion round-trip used by the close action.
/// </summary>
public sealed class SupportMapperTests
{
    private static readonly Guid OwnerId = Guid.Parse("0a0a0a0a-0a0a-0a0a-0a0a-0a0a0a0a0a0a");
    private static readonly Guid StaffId = Guid.Parse("0b0b0b0b-0b0b-0b0b-0b0b-0b0b0b0b0b0b");

    private static SupportTicketItemResponse Ticket(
        string status = "Open",
        string rowVersion = "AAAAAAAAB9E=",
        IReadOnlyList<TicketMessageResponse>? messages = null) => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        CreatedByUserId = OwnerId,
        Category = "BookingIssue",
        Subject = "My tour was cancelled",
        Priority = "Normal",
        Status = status,
        CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
        RowVersion = rowVersion,
        Messages = messages,
    };

    // ── List mapping ───────────────────────────────────────────────────────────

    [Fact]
    public void ToListVm_MapsRows_PreservesCursor_AndNoLoadError()
    {
        var page = new SupportTicketPageResponse
        {
            Items = new[] { Ticket(status: "Open"), Ticket(status: "Closed") },
            NextCursor = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

        var vm = SupportMapper.ToListVm(page);

        vm.LoadError.Should().BeNull();
        vm.Items.Should().HaveCount(2);
        vm.HasItems.Should().BeTrue();
        vm.NextCursor.Should().Be(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        vm.HasNextPage.Should().BeTrue();
        vm.Items[0].IsClosed.Should().BeFalse();
        vm.Items[1].IsClosed.Should().BeTrue();
        vm.Items[0].Subject.Should().Be("My tour was cancelled");
        vm.Items[0].Category.Should().Be("BookingIssue");
    }

    [Fact]
    public void DegradedListVm_IsEmpty_WithLoadError()
    {
        var vm = SupportMapper.DegradedListVm("boom");

        vm.HasItems.Should().BeFalse();
        vm.Items.Should().BeEmpty();
        vm.HasNextPage.Should().BeFalse();
        vm.LoadError.Should().Be("boom");
    }

    // ── Detail / thread mapping ────────────────────────────────────────────────

    [Fact]
    public void ToDetailVm_MapsHeader_AndRowVersionRoundTrip()
    {
        var vm = SupportMapper.ToDetailVm(Ticket(rowVersion: "Zm9vYmFy"));

        vm.Id.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        vm.Subject.Should().Be("My tour was cancelled");
        vm.Category.Should().Be("BookingIssue");
        vm.Status.Should().Be("Open");
        // RowVersion is carried verbatim so the close POST can echo it back.
        vm.RowVersion.Should().Be("Zm9vYmFy");
    }

    [Fact]
    public void ToDetailVm_HidesInternalStaffNotes_FromOwner()
    {
        var messages = new[]
        {
            new TicketMessageResponse { AuthorUserId = OwnerId, Body = "Please help", IsInternal = false, CreatedAt = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc) },
            new TicketMessageResponse { AuthorUserId = StaffId, Body = "INTERNAL: refund risk", IsInternal = true, CreatedAt = new DateTime(2026, 1, 1, 11, 30, 0, DateTimeKind.Utc) },
            new TicketMessageResponse { AuthorUserId = StaffId, Body = "We're looking into it", IsInternal = false, CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) },
        };

        var vm = SupportMapper.ToDetailVm(Ticket(messages: messages));

        vm.Messages.Should().HaveCount(2, "the internal staff note must never be shown to the owner");
        vm.Messages.Should().NotContain(m => m.Body.Contains("INTERNAL"));
    }

    [Fact]
    public void ToDetailVm_OrdersMessagesChronologically_AndFlagsStaffVsOwner()
    {
        var messages = new[]
        {
            new TicketMessageResponse { AuthorUserId = StaffId, Body = "second (staff)", IsInternal = false, CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
            new TicketMessageResponse { AuthorUserId = OwnerId, Body = "first (owner)", IsInternal = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
        };

        var vm = SupportMapper.ToDetailVm(Ticket(messages: messages));

        vm.Messages[0].Body.Should().Be("first (owner)");
        vm.Messages[0].IsFromStaff.Should().BeFalse();
        vm.Messages[1].Body.Should().Be("second (staff)");
        vm.Messages[1].IsFromStaff.Should().BeTrue();
    }

    [Fact]
    public void ToDetailVm_NullMessages_YieldsEmptyThread()
    {
        var vm = SupportMapper.ToDetailVm(Ticket(messages: null));

        vm.Messages.Should().BeEmpty();
    }

    // ── CanReply / CanClose affordance gating ──────────────────────────────────

    [Theory]
    [InlineData("Open", true)]
    [InlineData("Assigned", true)]
    [InlineData("InProgress", true)]
    [InlineData("AwaitingUser", true)]
    [InlineData("Resolved", false)]
    [InlineData("Closed", false)]
    public void ToDetailVm_CanReplyAndCanClose_TrackTerminalStatus(string status, bool expected)
    {
        var vm = SupportMapper.ToDetailVm(Ticket(status: status));

        vm.CanReply.Should().Be(expected);
        vm.CanClose.Should().Be(expected);
    }

    [Fact]
    public void ToRowVm_IsClosed_OnlyForClosedStatus_NotResolved()
    {
        SupportMapper.ToRowVm(Ticket(status: "Closed")).IsClosed.Should().BeTrue();
        SupportMapper.ToRowVm(Ticket(status: "Resolved")).IsClosed.Should().BeFalse();
        SupportMapper.ToRowVm(Ticket(status: "Open")).IsClosed.Should().BeFalse();
    }
}
