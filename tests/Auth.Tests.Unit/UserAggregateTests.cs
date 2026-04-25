using FluentAssertions;
using Security.Domain.Entities;
using Security.Domain.Events;
using YallaJo.Tests.Shared;

namespace Auth.Tests.Unit;

public sealed class UserAggregateTests
{
    [Fact]
    public void Register_ShouldRaiseUserCreatedEvent_WithNormalizedEmail()
    {
        var user = User.Register("  User@Example.COM ", "Joe", "Doe");

        var evt = DomainEventAssertions.ShouldContainDomainEvent<UserCreatedEvent>(user);
        evt.UserId.Should().Be(user.Id);
        evt.Email.Should().Be("user@example.com");
        evt.FirstName.Should().Be("Joe");
        evt.LastName.Should().Be("Doe");
    }

    [Fact]
    public void SetInitialPasswordHash_ShouldNotRaisePasswordChangedEvent()
    {
        var user = User.Register("u@example.com", "Joe", "Doe");

        user.SetInitialPasswordHash("hashed-password");

        user.PasswordHash.Should().Be("hashed-password");
        user.DomainEvents.OfType<PasswordChangedEvent>().Should().BeEmpty(
            "initial registration is covered by UserCreatedEvent; a change event would mislead auditors");
    }

    [Fact]
    public void SetPasswordHash_ShouldStillRaisePasswordChangedEvent_ForRealChanges()
    {
        var user = User.Register("u@example.com", "Joe", "Doe");
        user.SetInitialPasswordHash("initial-hash");
        user.ClearDomainEvents();

        user.SetPasswordHash("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        DomainEventAssertions.ShouldContainDomainEvent<PasswordChangedEvent>(user)
            .UserId.Should().Be(user.Id);
    }

    [Fact]
    public void VerifyEmail_ShouldActivateAccount_AndRaiseEmailVerifiedEvent()
    {
        var user = User.Register("u@example.com", "Joe", "Doe");
        user.SetInitialPasswordHash("h");
        user.ClearDomainEvents();

        var primary = user.GetPrimaryEmail();
        primary.Should().NotBeNull();

        var verified = user.VerifyEmail(primary!.Id);

        verified.Should().NotBeNull();
        verified!.IsVerified.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        DomainEventAssertions.ShouldContainDomainEvent<EmailVerifiedEvent>(user)
            .UserId.Should().Be(user.Id);
    }

    [Fact]
    public void VerifyEmail_CalledTwice_ShouldBeIdempotent()
    {
        var user = User.Register("u@example.com", "Joe", "Doe");
        user.SetInitialPasswordHash("h");
        var primary = user.GetPrimaryEmail()!;

        user.VerifyEmail(primary.Id);
        user.ClearDomainEvents();

        var second = user.VerifyEmail(primary.Id);

        second.Should().NotBeNull();
        second!.IsVerified.Should().BeTrue();
        user.DomainEvents.OfType<EmailVerifiedEvent>().Should().BeEmpty(
            "the event must not be republished when the email is already verified");
    }
}
