using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-1 — covers the state machine and attempt-budget invariants of
/// the <see cref="ActivationToken"/> aggregate. These are pure domain
/// tests; no persistence, no handler plumbing.
/// </summary>
public sealed class ActivationTokenAggregateTests
{
    private static ActivationToken NewToken() =>
        ActivationToken.Issue(
            userId:          Guid.NewGuid(),
            tokenHash:       "hash",
            deliveryAddress: "invitee@example.com",
            expiryMinutes:   60);

    [Fact]
    public void Issue_ShouldStartInIssuedPendingState_WithNoAttemptsOrTerminalMarkers()
    {
        var token = NewToken();

        token.State.Should().Be(ActivationTokenState.Issued);
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Pending);
        token.RevokedReason.Should().Be(ActivationTokenRevokedReason.None);
        token.AttemptCount.Should().Be(0);
        token.ConsumedAt.Should().BeNull();
        token.RevokedAt.Should().BeNull();
        token.LastSentAt.Should().BeNull();
        token.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void MarkDelivered_FromIssued_ShouldTransition_AndStampLastSentAt()
    {
        var token = NewToken();

        var before = DateTime.UtcNow.AddSeconds(-1);
        token.MarkDelivered();
        var after = DateTime.UtcNow.AddSeconds(1);

        token.State.Should().Be(ActivationTokenState.Delivered);
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Sent);
        token.LastSentAt.Should().NotBeNull();
        token.LastSentAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkDelivered_FromConsumed_ShouldThrow()
    {
        var token = NewToken();
        token.MarkDelivered();
        token.Consume();

        FluentActions.Invoking(() => token.MarkDelivered())
            .Should().Throw<InvalidActivationTokenTransitionException>()
            .Which.From.Should().Be(ActivationTokenState.Consumed);
    }

    [Fact]
    public void Consume_FromDelivered_ShouldTransition_AndStampConsumedAt()
    {
        var token = NewToken();
        token.MarkDelivered();

        token.Consume();

        token.State.Should().Be(ActivationTokenState.Consumed);
        token.ConsumedAt.Should().NotBeNull();
        token.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Consume_FromIssued_ShouldBeAllowed_ForRaceSafety()
    {
        // A fast-enough activation could in principle arrive before the
        // post-send MarkDelivered write has committed. Consuming from
        // Issued is explicitly legal so the user never sees a spurious
        // "invalid invite" because of infrastructure timing.
        var token = NewToken();

        token.Consume();

        token.State.Should().Be(ActivationTokenState.Consumed);
    }

    [Fact]
    public void Consume_FromRevoked_ShouldThrow()
    {
        var token = NewToken();
        token.Supersede();

        FluentActions.Invoking(() => token.Consume())
            .Should().Throw<InvalidActivationTokenTransitionException>();
    }

    [Fact]
    public void Supersede_ShouldRevokeWithReasonSuperseded()
    {
        var token = NewToken();
        token.MarkDelivered();

        token.Supersede();

        token.State.Should().Be(ActivationTokenState.Revoked);
        token.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Supersede_OnTerminalToken_ShouldBeIdempotent()
    {
        var token = NewToken();
        token.Supersede();

        // Calling again should be a no-op, not throw.
        token.Supersede();

        token.State.Should().Be(ActivationTokenState.Revoked);
        token.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
    }

    [Fact]
    public void RevokeByAdmin_ShouldRevokeWithReasonAdminRevoked()
    {
        var token = NewToken();
        token.MarkDelivered();

        token.RevokeByAdmin();

        token.State.Should().Be(ActivationTokenState.Revoked);
        token.RevokedReason.Should().Be(ActivationTokenRevokedReason.AdminRevoked);
    }

    [Fact]
    public void RevokeOnEmailFailure_ShouldRevoke_AndFlipDeliveryStatusToFailed()
    {
        var token = NewToken();

        token.RevokeOnEmailFailure();

        token.State.Should().Be(ActivationTokenState.Revoked);
        token.RevokedReason.Should().Be(ActivationTokenRevokedReason.EmailFailed);
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Failed);
    }

    [Fact]
    public void MarkDeliveryFailed_FromIssued_ShouldKeepState_ButFlipDeliveryStatus_AndStampLastSentAt()
    {
        var token = NewToken();

        var before = DateTime.UtcNow.AddSeconds(-1);
        token.MarkDeliveryFailed();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Key invariant: the token REMAINS redeemable so the outbox can
        // retry the same valid token.
        token.State.Should().Be(ActivationTokenState.Issued);
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Failed);
        token.LastSentAt.Should().NotBeNull();
        token.LastSentAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        token.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void MarkDeliveryFailed_FromDelivered_ShouldPreserveDelivered_AndFlipDeliveryStatus()
    {
        // Re-attempt of an already-delivered token whose second send leg
        // failed (an admin-driven resend scenario in a future phase).
        var token = NewToken();
        token.MarkDelivered();

        token.MarkDeliveryFailed();

        token.State.Should().Be(ActivationTokenState.Delivered,
            "the token was already delivered once; a later retry-failure must not regress the state");
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Failed);
    }

    [Fact]
    public void MarkDeliveryFailed_OnConsumedToken_ShouldBeNoOp()
    {
        var token = NewToken();
        token.MarkDelivered();
        token.Consume();

        token.MarkDeliveryFailed();

        token.State.Should().Be(ActivationTokenState.Consumed);
        token.DeliveryStatus.Should().Be(ActivationTokenDeliveryStatus.Sent,
            "a late retry after the user already consumed the token must not corrupt the audit trail");
    }

    [Fact]
    public void MarkDeliveryFailed_OnRevokedToken_ShouldBeNoOp()
    {
        var token = NewToken();
        token.Supersede();

        token.MarkDeliveryFailed();

        token.State.Should().Be(ActivationTokenState.Revoked);
        token.RevokedReason.Should().Be(ActivationTokenRevokedReason.Superseded);
    }

    [Fact]
    public void IsExpired_ShouldReflectExpiresAt()
    {
        var token = NewToken();

        token.IsExpired(DateTime.UtcNow.AddMinutes(-1)).Should().BeFalse(
            "60-minute window — not yet expired one minute into the future past issue");

        token.IsExpired(DateTime.UtcNow.AddMinutes(120)).Should().BeTrue(
            "two hours past issue is well beyond the 60-minute window");
    }

    [Fact]
    public void IncrementAttempt_ShouldCount_AndExhaustAfterFiveAttempts()
    {
        var token = NewToken();

        for (var i = 0; i < 5; i++)
        {
            token.IsExhausted.Should().BeFalse();
            token.IncrementAttempt();
        }

        token.AttemptCount.Should().Be(5);
        token.IsExhausted.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "hash", "user@example.com", 60)]
    [InlineData(null, "hash", "user@example.com", 60)]
    public void Issue_ShouldRejectEmptyTokenHash(string? tokenHash, string ignored, string address, int minutes)
    {
        _ = ignored;
        FluentActions.Invoking(() => ActivationToken.Issue(
                Guid.NewGuid(), tokenHash!, address, minutes))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_ShouldRejectEmptyUserId()
    {
        FluentActions.Invoking(() => ActivationToken.Issue(
                Guid.Empty, "hash", "a@b.com", 60))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_ShouldRejectNonPositiveExpiry()
    {
        FluentActions.Invoking(() => ActivationToken.Issue(
                Guid.NewGuid(), "hash", "a@b.com", 0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
