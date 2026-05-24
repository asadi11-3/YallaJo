using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Events;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the TASK 6 additions to <see cref="JoinRequest"/>:
/// the <c>Create</c> factory and the <c>Approve</c> / <c>Reject</c> lifecycle methods.
/// </summary>
public sealed class JoinRequestDomainTests
{
    private static readonly Guid BookingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid UserId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void Create_with_valid_inputs_yields_pending_request_and_raises_event()
    {
        var jr = JoinRequest.Create(BookingId, UserId, participantCount: 2, message: " hi ");

        jr.TourBookingId.Should().Be(BookingId);
        jr.UserId.Should().Be(UserId);
        jr.ParticipantCount.Should().Be(2);
        jr.Message.Should().Be("hi"); // trimmed
        jr.Status.Should().Be(JoinRequestStatus.Pending);
        jr.RespondedAt.Should().BeNull();
        jr.ResponseMessage.Should().BeNull();
        jr.DomainEvents.Should().ContainSingle(e => e is JoinRequestCreatedDomainEvent);
    }

    [Fact]
    public void Create_normalizes_empty_or_whitespace_message_to_null()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, "   ");
        jr.Message.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_empty_booking_id()
    {
        var act = () => JoinRequest.Create(Guid.Empty, UserId, 1, null);
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*TourBookingId*");
    }

    [Fact]
    public void Create_rejects_empty_user_id()
    {
        var act = () => JoinRequest.Create(BookingId, Guid.Empty, 1, null);
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*UserId*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_invalid_participant_count(int count)
    {
        var act = () => JoinRequest.Create(BookingId, UserId, count, null);
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*ParticipantCount*");
    }

    // ===== Approve =====

    [Fact]
    public void Approve_pending_request_sets_status_and_raises_event()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.ClearDomainEvents();

        jr.Approve();

        jr.Status.Should().Be(JoinRequestStatus.Approved);
        jr.RespondedAt.Should().NotBeNull();
        jr.DomainEvents.Should().ContainSingle(e => e is JoinRequestApprovedDomainEvent);
    }

    [Fact]
    public void Approve_after_approve_throws()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.Approve();

        var act = () => jr.Approve();
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*Approved*");
    }

    [Fact]
    public void Approve_after_reject_throws()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.Reject("nope");

        var act = () => jr.Approve();
        act.Should().Throw<BusinessRuleViolationException>();
    }

    // ===== Reject =====

    [Fact]
    public void Reject_pending_request_sets_status_reason_and_raises_event()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.ClearDomainEvents();

        jr.Reject("  sorry, no room  ");

        jr.Status.Should().Be(JoinRequestStatus.Rejected);
        jr.RespondedAt.Should().NotBeNull();
        jr.ResponseMessage.Should().Be("sorry, no room");
        jr.DomainEvents.Should().ContainSingle(e => e is JoinRequestRejectedDomainEvent);
    }

    [Fact]
    public void Reject_with_null_reason_persists_null_response_message()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.Reject(null);

        jr.Status.Should().Be(JoinRequestStatus.Rejected);
        jr.ResponseMessage.Should().BeNull();
    }

    [Fact]
    public void Reject_with_whitespace_reason_normalizes_to_null()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.Reject("   ");

        jr.ResponseMessage.Should().BeNull();
    }

    [Fact]
    public void Reject_after_approve_throws()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.Approve();

        var act = () => jr.Reject("late");
        act.Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Reject_after_reject_throws()
    {
        var jr = JoinRequest.Create(BookingId, UserId, 1, null);
        jr.Reject("nope");

        var act = () => jr.Reject("again");
        act.Should().Throw<BusinessRuleViolationException>();
    }
}
