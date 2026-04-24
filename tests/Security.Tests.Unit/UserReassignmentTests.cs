using FluentAssertions;
using Security.Domain.Entities;
using Security.Domain.Events;
using YallaJo.Tests.Shared;

namespace Security.Tests.Unit;

/// <summary>
/// Phase 3C — covers the domain-level reassignment behavior on
/// <see cref="User"/> and <see cref="Email"/>. Verifies:
///   • Active | Suspended | PendingPasswordReset -> PendingActivation is permitted only via ReassignToPendingActivation.
///   • Provisioned / PendingActivation / Archived are rejected.
///   • Primary email is retargeted to the new address (normalized).
///   • Email verification state is reset.
///   • Password hash is replaced with the supplied placeholder.
///   • PasswordResetEvent + AccountLifecycleTransitionedEvent fire.
///   • Email domain verbs (ChangeAddress, ResetVerification) behave correctly in isolation.
/// </summary>
public sealed class UserReassignmentTests
{
    private const string Placeholder = "REASSIGNED:abcdef";

    private static User ActiveUser(string email = "old@example.com")
    {
        var user = User.Register(email, "Joe", "Doe");
        user.SetInitialPasswordHash("old-hash");
        var primary = user.GetPrimaryEmail()!;
        user.VerifyEmail(primary.Id);
        return user;
    }

    [Fact]
    public void ReassignToPendingActivation_FromActive_ShouldRetargetEmail_AndResetLifecycle()
    {
        var user = ActiveUser();
        user.ClearDomainEvents();

        user.ReassignToPendingActivation("  NEW@Example.COM ", Placeholder);

        user.LifecycleState.Should().Be(AccountLifecycleState.PendingActivation);
        user.IsActive.Should().BeFalse();
        user.PasswordHash.Should().Be(Placeholder);

        var primary = user.GetPrimaryEmail()!;
        primary.Address.Should().Be("new@example.com", "ChangeAddress must normalize trim + lowercase");
        primary.IsVerified.Should().BeFalse();
        primary.VerifiedAt.Should().BeNull();

        DomainEventAssertions.ShouldContainDomainEvent<PasswordResetEvent>(user)
            .UserId.Should().Be(user.Id);
        var transition = DomainEventAssertions.ShouldContainDomainEvent<AccountLifecycleTransitionedEvent>(user);
        transition.From.Should().Be(AccountLifecycleState.Active);
        transition.To.Should().Be(AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void ReassignToPendingActivation_FromSuspended_ShouldSucceed()
    {
        var user = ActiveUser();
        user.Suspend();
        user.ClearDomainEvents();

        user.ReassignToPendingActivation("new@example.com", Placeholder);

        user.LifecycleState.Should().Be(AccountLifecycleState.PendingActivation);
        var transition = DomainEventAssertions.ShouldContainDomainEvent<AccountLifecycleTransitionedEvent>(user);
        transition.From.Should().Be(AccountLifecycleState.Suspended);
        transition.To.Should().Be(AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void ReassignToPendingActivation_FromPendingPasswordReset_ShouldSucceed()
    {
        var user = ActiveUser();
        user.MarkPendingPasswordReset();
        user.ClearDomainEvents();

        user.ReassignToPendingActivation("new@example.com", Placeholder);

        user.LifecycleState.Should().Be(AccountLifecycleState.PendingActivation);
        var transition = DomainEventAssertions.ShouldContainDomainEvent<AccountLifecycleTransitionedEvent>(user);
        transition.From.Should().Be(AccountLifecycleState.PendingPasswordReset);
        transition.To.Should().Be(AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void ReassignToPendingActivation_FromProvisioned_ShouldThrow()
    {
        var user = User.Register("old@example.com", "Joe", "Doe");

        FluentActions.Invoking(() =>
                user.ReassignToPendingActivation("new@example.com", Placeholder))
            .Should().Throw<InvalidLifecycleTransitionException>()
            .Where(ex => ex.From == AccountLifecycleState.Provisioned
                      && ex.To == AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void ReassignToPendingActivation_FromPendingActivation_ShouldThrow()
    {
        var user = User.Register("old@example.com", "Joe", "Doe");
        user.MarkPendingActivation();

        FluentActions.Invoking(() =>
                user.ReassignToPendingActivation("new@example.com", Placeholder))
            .Should().Throw<InvalidLifecycleTransitionException>()
            .Where(ex => ex.From == AccountLifecycleState.PendingActivation
                      && ex.To == AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void ReassignToPendingActivation_FromArchived_ShouldThrow()
    {
        var user = ActiveUser();
        user.Archive();

        FluentActions.Invoking(() =>
                user.ReassignToPendingActivation("new@example.com", Placeholder))
            .Should().Throw<InvalidLifecycleTransitionException>()
            .Where(ex => ex.From == AccountLifecycleState.Archived
                      && ex.To == AccountLifecycleState.PendingActivation);
    }

    [Fact]
    public void ReassignToPendingActivation_WithBlankEmail_ShouldThrow()
    {
        var user = ActiveUser();

        FluentActions.Invoking(() => user.ReassignToPendingActivation("   ", Placeholder))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ReassignToPendingActivation_WithBlankPassword_ShouldThrow()
    {
        var user = ActiveUser();

        FluentActions.Invoking(() => user.ReassignToPendingActivation("new@example.com", "  "))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Email_ChangeAddress_ShouldNormalize_AndResetVerification()
    {
        var email = Email.Create(Guid.NewGuid(), "old@example.com", isPrimary: true);
        email.MarkVerified();
        email.IsVerified.Should().BeTrue();

        email.ChangeAddress("  NEW@Example.COM ");

        email.Address.Should().Be("new@example.com");
        email.IsVerified.Should().BeFalse();
        email.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public void Email_ResetVerification_ShouldOnlyClearVerificationState()
    {
        var email = Email.Create(Guid.NewGuid(), "user@example.com", isPrimary: true);
        email.MarkVerified();

        email.ResetVerification();

        email.Address.Should().Be("user@example.com");
        email.IsVerified.Should().BeFalse();
        email.VerifiedAt.Should().BeNull();
    }

    [Fact]
    public void Email_ChangeAddress_WithBlank_ShouldThrow()
    {
        var email = Email.Create(Guid.NewGuid(), "user@example.com", isPrimary: true);

        FluentActions.Invoking(() => email.ChangeAddress("   "))
            .Should().Throw<ArgumentException>();
    }
}
