using FluentAssertions;
using Security.Domain.Entities;
using Security.Domain.Events;
using YallaJo.Tests.Shared;

namespace Security.Tests.Unit;

/// <summary>
/// Phase 2A — covers the explicit lifecycle state machine on the
/// <see cref="User"/> aggregate. The shadow boolean <c>IsActive</c> is
/// verified to track <c>LifecycleState == Active</c> at every transition
/// so existing read-side queries keep working without modification.
/// </summary>
public sealed class UserLifecycleStateTests
{
    private static User NewProvisionedUser()
    {
        // User.Register seeds Provisioned + IsActive=false. We use Register
        // (not Create) so the primary email exists for the verify-email tests.
        return User.Register("u@example.com", "Joe", "Doe");
    }

    [Fact]
    public void Register_ShouldStartInProvisionedState_WithIsActiveFalse()
    {
        var user = NewProvisionedUser();

        user.LifecycleState.Should().Be(AccountLifecycleState.Provisioned);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void MarkPendingActivation_ShouldTransition_AndRaiseEvent()
    {
        var user = NewProvisionedUser();
        user.ClearDomainEvents();

        user.MarkPendingActivation();

        user.LifecycleState.Should().Be(AccountLifecycleState.PendingActivation);
        user.IsActive.Should().BeFalse("PendingActivation is not Active — login must remain blocked");

        var evt = DomainEventAssertions.ShouldContainDomainEvent<AccountLifecycleTransitionedEvent>(user);
        evt.From.Should().Be(AccountLifecycleState.Provisioned);
        evt.To.Should().Be(AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void Activate_FromPendingActivation_ShouldTransitionAndFlipIsActive()
    {
        var user = NewProvisionedUser();
        user.MarkPendingActivation();
        user.ClearDomainEvents();

        user.Activate();

        user.LifecycleState.Should().Be(AccountLifecycleState.Active);
        user.IsActive.Should().BeTrue();

        var evt = DomainEventAssertions.ShouldContainDomainEvent<AccountLifecycleTransitionedEvent>(user);
        evt.From.Should().Be(AccountLifecycleState.PendingActivation);
        evt.To.Should().Be(AccountLifecycleState.Active);
    }

    [Fact]
    public void Activate_OnAlreadyActive_ShouldBeIdempotent_AndNotRaiseEvent()
    {
        var user = NewProvisionedUser();
        user.MarkPendingActivation();
        user.Activate();
        user.ClearDomainEvents();

        user.Activate();

        user.LifecycleState.Should().Be(AccountLifecycleState.Active);
        user.DomainEvents.OfType<AccountLifecycleTransitionedEvent>().Should().BeEmpty(
            "self-transitions are no-ops and must not generate audit noise");
    }

    [Fact]
    public void Suspend_ThenReactivate_ShouldRoundTripViaSuspendedState()
    {
        var user = NewProvisionedUser();
        user.MarkPendingActivation();
        user.Activate();
        user.ClearDomainEvents();

        user.Suspend();
        user.LifecycleState.Should().Be(AccountLifecycleState.Suspended);
        user.IsActive.Should().BeFalse();

        user.Reactivate();
        user.LifecycleState.Should().Be(AccountLifecycleState.Active);
        user.IsActive.Should().BeTrue();

        var transitions = user.DomainEvents.OfType<AccountLifecycleTransitionedEvent>().ToList();
        transitions.Should().HaveCount(2);
        transitions[0].To.Should().Be(AccountLifecycleState.Suspended);
        transitions[1].To.Should().Be(AccountLifecycleState.Active);
    }

    [Fact]
    public void Deactivate_LegacyVerb_ShouldDelegateToSuspend()
    {
        var user = NewProvisionedUser();
        user.MarkPendingActivation();
        user.Activate();

        user.Deactivate();

        user.LifecycleState.Should().Be(AccountLifecycleState.Suspended,
            "the legacy Deactivate verb must remain wire-compatible — it now means Suspend");
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void MarkPendingPasswordReset_ShouldTransition_AndBlockLogin()
    {
        var user = NewProvisionedUser();
        user.MarkPendingActivation();
        user.Activate();
        user.ClearDomainEvents();

        user.MarkPendingPasswordReset();

        user.LifecycleState.Should().Be(AccountLifecycleState.PendingPasswordReset);
        user.IsActive.Should().BeFalse("login must be blocked while admin reset is in flight");
    }

    [Fact]
    public void Archive_ShouldBeTerminal_AndAnyFurtherTransitionShouldThrow()
    {
        var user = NewProvisionedUser();
        user.Archive();

        user.LifecycleState.Should().Be(AccountLifecycleState.Archived);
        user.IsActive.Should().BeFalse();

        FluentActions.Invoking(() => user.Activate())
            .Should().Throw<InvalidLifecycleTransitionException>()
            .Which.From.Should().Be(AccountLifecycleState.Archived);

        FluentActions.Invoking(() => user.MarkPendingActivation())
            .Should().Throw<InvalidLifecycleTransitionException>();

        FluentActions.Invoking(() => user.Suspend())
            .Should().Throw<InvalidLifecycleTransitionException>();
    }

    [Fact]
    public void Suspend_FromProvisioned_ShouldThrow_BecauseAccountWasNeverActive()
    {
        var user = NewProvisionedUser();

        FluentActions.Invoking(() => user.Suspend())
            .Should().Throw<InvalidLifecycleTransitionException>()
            .Where(ex => ex.From == AccountLifecycleState.Provisioned
                      && ex.To   == AccountLifecycleState.Suspended);
    }

    [Fact]
    public void VerifyEmail_ShouldRouteThroughLifecycleTransition_AndRaiseTransitionedEvent()
    {
        // Backward-compat invariant: the existing self-registration /
        // external-login path still expects VerifyEmail to flip the user
        // to Active in one shot. Phase 2A preserves this by routing the
        // implicit Activation through TransitionTo, which means a
        // lifecycle event now fires alongside the EmailVerifiedEvent.
        var user = NewProvisionedUser();
        user.SetInitialPasswordHash("h");
        user.ClearDomainEvents();

        var primary = user.GetPrimaryEmail()!;
        user.VerifyEmail(primary.Id);

        user.LifecycleState.Should().Be(AccountLifecycleState.Active);
        user.IsActive.Should().BeTrue();

        DomainEventAssertions.ShouldContainDomainEvent<AccountLifecycleTransitionedEvent>(user)
            .To.Should().Be(AccountLifecycleState.Active);
        DomainEventAssertions.ShouldContainDomainEvent<EmailVerifiedEvent>(user);
    }

    [Theory]
    [InlineData(AccountLifecycleState.Active,             true)]
    [InlineData(AccountLifecycleState.Provisioned,        false)]
    [InlineData(AccountLifecycleState.PendingActivation,  false)]
    [InlineData(AccountLifecycleState.Suspended,          false)]
    [InlineData(AccountLifecycleState.PendingPasswordReset, false)]
    [InlineData(AccountLifecycleState.Archived,           false)]
    public void IsActive_ShouldTrackLifecycleStateExactly(
        AccountLifecycleState targetState,
        bool expectedIsActive)
    {
        // Walk a deterministic path to each target state and assert IsActive.
        var user = NewProvisionedUser();
        switch (targetState)
        {
            case AccountLifecycleState.Provisioned:
                break;
            case AccountLifecycleState.PendingActivation:
                user.MarkPendingActivation();
                break;
            case AccountLifecycleState.Active:
                user.MarkPendingActivation();
                user.Activate();
                break;
            case AccountLifecycleState.Suspended:
                user.MarkPendingActivation();
                user.Activate();
                user.Suspend();
                break;
            case AccountLifecycleState.PendingPasswordReset:
                user.MarkPendingActivation();
                user.Activate();
                user.MarkPendingPasswordReset();
                break;
            case AccountLifecycleState.Archived:
                user.Archive();
                break;
        }

        user.LifecycleState.Should().Be(targetState);
        user.IsActive.Should().Be(expectedIsActive);
    }
}
