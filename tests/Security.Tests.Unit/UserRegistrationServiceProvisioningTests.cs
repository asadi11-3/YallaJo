using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Events;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Phase 2B — covers the split provisioning contract:
///   • RegisterProvisionedAsync creates a user in <c>Provisioned</c> state
///     and does NOT advance to PendingActivation.
///   • MarkPendingActivationAsync transitions <c>Provisioned → PendingActivation</c>
///     and is idempotent from <c>PendingActivation</c>.
///   • MarkPendingActivationAsync refuses accounts already past activation
///     (Active / Suspended / PendingPasswordReset / Archived).
/// </summary>
public sealed class UserRegistrationServiceProvisioningTests
{
    private readonly IUserRepository       _userRepo    = Substitute.For<IUserRepository>();
    private readonly IRoleRepository       _roleRepo    = Substitute.For<IRoleRepository>();
    private readonly ISecurityUnitOfWork   _uow         = Substitute.For<ISecurityUnitOfWork>();
    private readonly IPasswordHasher       _hasher      = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser          _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache           _cache       = Substitute.For<HybridCache>();

    private IUserRegistrationService CreateSut()
    {
        var assembly = typeof(Security.Application.DependencyInjection).Assembly;
        var type = assembly.GetType(
            "Security.Application.Services.UserRegistrationService",
            throwOnError: true)!;
        return (IUserRegistrationService)Activator.CreateInstance(
            type, _userRepo, _roleRepo, _uow, _hasher, _currentUser, _cache)!;
    }

    private void StubCurrentUserAsInviter()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole(Arg.Any<string>()).Returns(false);
    }

    private void StubAssignableRole(Guid roleId, string name = "Member")
    {
        var role = Role.Create(name);
        // Pin the role id using reflection so the invitable-roles lookup
        // returns an id the caller supplies.
        typeof(Role).GetProperty(nameof(Role.Id))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(role, new object[] { roleId });

        _roleRepo.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);

        _roleRepo.GetAllAsync(
                Arg.Any<Expression<Func<Role, bool>>?>(),
                Arg.Any<Func<IQueryable<Role>, IQueryable<Role>>?>(),
                Arg.Any<Func<IQueryable<Role>, IOrderedQueryable<Role>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Role> { role });

        _userRepo.AnyWithRoleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    // ── RegisterProvisionedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterProvisionedAsync_ShouldCreateUser_InProvisionedState_AndNotAdvance()
    {
        StubCurrentUserAsInviter();
        var roleId = Guid.NewGuid();
        StubAssignableRole(roleId);

        _userRepo.AnyAsync(
                Arg.Any<Expression<Func<User, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        User? captured = null;
        _userRepo.AddAsync(Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterProvisionedAsync(
            new InvitedUserRegistrationRequest(
                FirstName:      "Jo",
                LastName:       "Doe",
                Email:          "Jo.Doe@Example.COM",
                InitialRoleIds: new[] { roleId }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);

        captured.Should().NotBeNull();
        captured!.LifecycleState.Should().Be(
            AccountLifecycleState.Provisioned,
            "RegisterProvisionedAsync must NOT advance to PendingActivation — that is the responsibility of MarkPendingActivationAsync after an activation email is dispatched");
        captured.IsActive.Should().BeFalse();

        // No AccountLifecycleTransitionedEvent should be present — the user
        // was born at Provisioned (no state change), so no transition event
        // was raised.
        captured.DomainEvents
            .OfType<AccountLifecycleTransitionedEvent>()
            .Should().BeEmpty();
    }

    // ── MarkPendingActivationAsync ────────────────────────────────────────────

    [Fact]
    public async Task MarkPendingActivationAsync_ShouldReturnNotFound_WhenUserMissing()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((User?)null);

        var result = await CreateSut().MarkPendingActivationAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task MarkPendingActivationAsync_ShouldTransition_FromProvisioned_ToPendingActivation()
    {
        var user = User.Register("user@example.com", "Jo", "Doe");
        user.ClearDomainEvents();

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingActivationAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.LifecycleState.Should().Be(AccountLifecycleState.PendingActivation);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkPendingActivationAsync_ShouldBeIdempotent_FromPendingActivation_AndNotSave()
    {
        var user = User.Register("user@example.com", "Jo", "Doe");
        user.MarkPendingActivation();
        user.ClearDomainEvents();

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingActivationAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.LifecycleState.Should().Be(AccountLifecycleState.PendingActivation);

        // Idempotent fast-path: no state change, no UoW churn.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkPendingActivationAsync_ShouldRejectActiveAccount_WithConflict()
    {
        // Build an Active user via the legal transition path.
        var user = User.Register("user@example.com", "Jo", "Doe");
        user.MarkPendingActivation();
        user.Activate();

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingActivationAsync(user.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        // Lifecycle must not have moved.
        user.LifecycleState.Should().Be(AccountLifecycleState.Active);
    }

    [Fact]
    public async Task MarkPendingActivationAsync_ShouldRejectArchivedAccount_WithConflict()
    {
        var user = User.Register("user@example.com", "Jo", "Doe");
        user.Archive();

        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingActivationAsync(user.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        user.LifecycleState.Should().Be(AccountLifecycleState.Archived);
    }
}
