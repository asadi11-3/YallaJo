using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Phase 3A — covers
/// <c>IUserRegistrationService.MarkPendingPasswordResetAsync</c>:
///   • transitions Active → PendingPasswordReset and saves,
///   • idempotent from PendingPasswordReset (no save, no cache churn),
///   • rejects Provisioned / PendingActivation / Suspended / Archived
///     with Conflict,
///   • NotFound when user is missing.
/// </summary>
public sealed class UserRegistrationServiceMarkPendingPasswordResetTests
{
    private readonly IUserRepository     _userRepo    = Substitute.For<IUserRepository>();
    private readonly IRoleRepository     _roleRepo    = Substitute.For<IRoleRepository>();
    private readonly ISecurityUnitOfWork _uow         = Substitute.For<ISecurityUnitOfWork>();
    private readonly IPasswordHasher     _hasher      = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser        _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache         _cache       = Substitute.For<HybridCache>();

    private IUserRegistrationService CreateSut()
    {
        var assembly = typeof(Security.Application.DependencyInjection).Assembly;
        var type = assembly.GetType(
            "Security.Application.Services.UserRegistrationService",
            throwOnError: true)!;
        return (IUserRegistrationService)Activator.CreateInstance(
            type, _userRepo, _roleRepo, _uow, _hasher, _currentUser, _cache)!;
    }

    private static User BuildUserIn(AccountLifecycleState state)
    {
        var user = User.Register("user@example.com", "Jo", "Doe");
        user.ClearDomainEvents();

        switch (state)
        {
            case AccountLifecycleState.Provisioned:
                break; // default
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

        user.ClearDomainEvents();
        return user;
    }

    // ── Success paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkPendingPasswordResetAsync_ShouldTransition_FromActive_ToPendingPasswordReset()
    {
        var user = BuildUserIn(AccountLifecycleState.Active);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingPasswordResetAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.LifecycleState.Should().Be(AccountLifecycleState.PendingPasswordReset);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkPendingPasswordResetAsync_ShouldBeIdempotent_FromPendingPasswordReset_AndNotSave()
    {
        var user = BuildUserIn(AccountLifecycleState.PendingPasswordReset);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingPasswordResetAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.LifecycleState.Should().Be(AccountLifecycleState.PendingPasswordReset);

        // Fast-path: no UoW churn when already in target state.
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Refusal paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkPendingPasswordResetAsync_ShouldReturnNotFound_WhenUserMissing()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((User?)null);

        var result = await CreateSut().MarkPendingPasswordResetAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Theory]
    [InlineData(AccountLifecycleState.Provisioned)]
    [InlineData(AccountLifecycleState.PendingActivation)]
    [InlineData(AccountLifecycleState.Suspended)]
    [InlineData(AccountLifecycleState.Archived)]
    public async Task MarkPendingPasswordResetAsync_ShouldRejectIneligibleStates_WithConflict(
        AccountLifecycleState state)
    {
        var user = BuildUserIn(state);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(user);

        var result = await CreateSut().MarkPendingPasswordResetAsync(user.Id, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);

        // Lifecycle must not have moved.
        user.LifecycleState.Should().Be(state);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
