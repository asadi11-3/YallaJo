using Auth.Domain.Entities;
using FluentAssertions;

namespace Auth.Tests.Unit;

/// <summary>
/// Phase 2C-2 — covers the state machine and attempt-budget invariants
/// of the <see cref="PasswordResetToken"/> aggregate. Pure domain tests;
/// no persistence, no handler plumbing.
/// </summary>
public sealed class PasswordResetTokenAggregateTests
{
    private static PasswordResetToken NewToken(
        PasswordResetOrigin origin = PasswordResetOrigin.SelfService) =>
        PasswordResetToken.Issue(
            userId:          Guid.NewGuid(),
            tokenHash:       "hash",
            deliveryAddress: "user@example.com",
            expiryMinutes:   10,
            origin:          origin);

    [Fact]
    public void Issue_ShouldStartInIssuedPendingState_WithSelfServiceOriginByDefault()
    {
        var token = NewToken();

        token.State.Should().Be(PasswordResetTokenState.Issued);
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Pending);
        token.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.None);
        token.ResetOrigin.Should().Be(PasswordResetOrigin.SelfService);
        token.AttemptCount.Should().Be(0);
        token.ConsumedAt.Should().BeNull();
        token.RevokedAt.Should().BeNull();
        token.LastSentAt.Should().BeNull();
        token.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Issue_ShouldPreserveExplicitlyProvidedOrigin()
    {
        var token = NewToken(PasswordResetOrigin.AdminInitiated);

        // The aggregate accepts every origin value — it does NOT gate on
        // the enum because the handler layer is responsible for deciding
        // which origin is legal for which flow. In Phase 2C-2 only the
        // self-service handler is wired; the other origins are reserved.
        token.ResetOrigin.Should().Be(PasswordResetOrigin.AdminInitiated);
    }

    [Fact]
    public void MarkDelivered_FromIssued_ShouldTransition_AndStampLastSentAt()
    {
        var token = NewToken();

        var before = DateTime.UtcNow.AddSeconds(-1);
        token.MarkDelivered();
        var after = DateTime.UtcNow.AddSeconds(1);

        token.State.Should().Be(PasswordResetTokenState.Delivered);
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Sent);
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
            .Should().Throw<InvalidPasswordResetTokenTransitionException>()
            .Which.From.Should().Be(PasswordResetTokenState.Consumed);
    }

    [Fact]
    public void Consume_FromDelivered_ShouldTransition_AndStampConsumedAt()
    {
        var token = NewToken();
        token.MarkDelivered();

        token.Consume();

        token.State.Should().Be(PasswordResetTokenState.Consumed);
        token.ConsumedAt.Should().NotBeNull();
        token.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Consume_FromIssued_ShouldBeAllowed_ForRaceSafety()
    {
        // A fast-enough reset could in principle arrive before the
        // post-send MarkDelivered write has committed. Consuming from
        // Issued is explicitly legal so the user never sees a spurious
        // "invalid reset code" because of infrastructure timing.
        var token = NewToken();

        token.Consume();

        token.State.Should().Be(PasswordResetTokenState.Consumed);
    }

    [Fact]
    public void Consume_FromRevoked_ShouldThrow()
    {
        var token = NewToken();
        token.Supersede();

        FluentActions.Invoking(() => token.Consume())
            .Should().Throw<InvalidPasswordResetTokenTransitionException>();
    }

    [Fact]
    public void Supersede_ShouldRevokeWithReasonSuperseded()
    {
        var token = NewToken();
        token.MarkDelivered();

        token.Supersede();

        token.State.Should().Be(PasswordResetTokenState.Revoked);
        token.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
        token.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Supersede_OnTerminalToken_ShouldBeIdempotent()
    {
        var token = NewToken();
        token.Supersede();

        token.Supersede();

        token.State.Should().Be(PasswordResetTokenState.Revoked);
        token.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
    }

    [Fact]
    public void RevokeByAdmin_ShouldRevokeWithReasonAdminRevoked()
    {
        var token = NewToken();
        token.MarkDelivered();

        token.RevokeByAdmin();

        token.State.Should().Be(PasswordResetTokenState.Revoked);
        token.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.AdminRevoked);
    }

    [Fact]
    public void RevokeOnEmailFailure_ShouldRevoke_AndFlipDeliveryStatusToFailed()
    {
        var token = NewToken();

        token.RevokeOnEmailFailure();

        token.State.Should().Be(PasswordResetTokenState.Revoked);
        token.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.EmailFailed);
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Failed);
    }

    [Fact]
    public void MarkDeliveryFailed_FromIssued_ShouldKeepState_ButFlipDeliveryStatus_AndStampLastSentAt()
    {
        var token = NewToken();

        var before = DateTime.UtcNow.AddSeconds(-1);
        token.MarkDeliveryFailed();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Key invariant: the token REMAINS redeemable so the outbox can
        // retry the same valid reset code.
        token.State.Should().Be(PasswordResetTokenState.Issued);
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Failed);
        token.LastSentAt.Should().NotBeNull();
        token.LastSentAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        token.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void MarkDeliveryFailed_OnConsumedToken_ShouldBeNoOp()
    {
        var token = NewToken();
        token.MarkDelivered();
        token.Consume();

        token.MarkDeliveryFailed();

        token.State.Should().Be(PasswordResetTokenState.Consumed);
        token.DeliveryStatus.Should().Be(PasswordResetTokenDeliveryStatus.Sent,
            "a late retry after the user already consumed the token must not corrupt the audit trail");
    }

    [Fact]
    public void MarkDeliveryFailed_OnRevokedToken_ShouldBeNoOp()
    {
        var token = NewToken();
        token.Supersede();

        token.MarkDeliveryFailed();

        token.State.Should().Be(PasswordResetTokenState.Revoked);
        token.RevokedReason.Should().Be(PasswordResetTokenRevokedReason.Superseded);
    }

    [Fact]
    public void IsExpired_ShouldReflectExpiresAt()
    {
        var token = NewToken();

        token.IsExpired(DateTime.UtcNow.AddMinutes(5)).Should().BeFalse(
            "within the 10-minute expiry window");

        token.IsExpired(DateTime.UtcNow.AddMinutes(30)).Should().BeTrue(
            "well past the 10-minute expiry window");
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
    [InlineData("")]
    [InlineData(null)]
    public void Issue_ShouldRejectEmptyTokenHash(string? tokenHash)
    {
        FluentActions.Invoking(() => PasswordResetToken.Issue(
                Guid.NewGuid(), tokenHash!, "user@example.com", 10))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_ShouldRejectEmptyUserId()
    {
        FluentActions.Invoking(() => PasswordResetToken.Issue(
                Guid.Empty, "hash", "a@b.com", 10))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Issue_ShouldRejectNonPositiveExpiry()
    {
        FluentActions.Invoking(() => PasswordResetToken.Issue(
                Guid.NewGuid(), "hash", "a@b.com", 0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
