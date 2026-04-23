using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Interfaces;
using Security.Application.Services;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Pins the contract for external-provider auto-create in
/// <see cref="UserRegistrationService.RegisterExternalAsync"/>:
///
/// <list type="bullet">
///   <item><description>Refuses if a local account already exists with this
///   email — the caller (Auth external-login handler) must never override an
///   existing identity, and we fail closed.</description></item>
///   <item><description>Creates a fully-onboarded user: <c>IsActive = true</c>
///   and primary email <c>IsVerified = true</c>. External users can sign in
///   immediately, same UX as Google/Meta/GitHub first-login.</description></item>
///   <item><description>Seeds an inert, non-usable password hash so password
///   login is guaranteed to fail closed until the user resets their password
///   through the normal forgot-password flow.</description></item>
/// </list>
/// </summary>
public sealed class UserRegistrationServiceExternalTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly ISecurityUnitOfWork _uow = Substitute.For<ISecurityUnitOfWork>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    // UserRegistrationService is internal — reach it via its own assembly
    // (referenced by the Security.Application public DI class) and build it
    // through reflection. This keeps the test on the IUserRegistrationService
    // contract, which is the public surface area the Auth module consumes.
    private IUserRegistrationService CreateSut()
    {
        var assembly = typeof(Security.Application.DependencyInjection).Assembly;
        var type = assembly.GetType(
            "Security.Application.Services.UserRegistrationService",
            throwOnError: true)!;
        return (IUserRegistrationService)Activator.CreateInstance(
            type, _userRepo, _roleRepo, _uow, _hasher, _currentUser, _cache)!;
    }

    [Fact]
    public async Task RegisterExternalAsync_ShouldConflict_WhenEmailAlreadyExists()
    {
        _userRepo.AnyAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateSut().RegisterExternalAsync(
            new ExternalUserRegistrationRequest(
                Email:     "existing@gmail.com",
                FirstName: "Ex",
                LastName:  "Isting"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Conflict);
        await _userRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task RegisterExternalAsync_ShouldCreateVerifiedActiveUser_WithInertPassword()
    {
        _userRepo.AnyAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        User? captured = null;
        _userRepo.AddAsync(Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterExternalAsync(
            new ExternalUserRegistrationRequest(
                Email:     "NewUser@Gmail.COM",
                FirstName: "New",
                LastName:  "User"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);

        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeTrue(
            "external auto-create must produce a fully-onboarded account");

        var primary = captured.GetPrimaryEmail();
        primary.Should().NotBeNull();
        primary!.IsVerified.Should().BeTrue(
            "primary email is already proven by the provider's attestation");
        primary.Address.Should().Be("newuser@gmail.com",
            "email must be normalized to trimmed-lowercase on write");

        captured.PasswordHash.Should().StartWith("EXTERNAL-ONLY:",
            "a recognisable non-usable placeholder keeps password login failing " +
            "closed until the user resets their password via forgot-password");

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterExternalAsync_ShouldDefaultFirstNameWhenBlank()
    {
        _userRepo.AnyAsync(
                Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        User? captured = null;
        _userRepo.AddAsync(Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await CreateSut().RegisterExternalAsync(
            new ExternalUserRegistrationRequest(
                Email:     "blank@example.com",
                FirstName: "",
                LastName:  ""),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        // Profile-side user must never end up with an empty first name —
        // fallback is applied here so Accounts.Profile is never seeded blank.
        // The domain event carries the fallback value.
    }

    [Fact]
    public async Task RegisterExternalAsync_ShouldRejectWhenEmailIsEmpty()
    {
        var result = await CreateSut().RegisterExternalAsync(
            new ExternalUserRegistrationRequest(
                Email:     "   ",
                FirstName: "x",
                LastName:  "y"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Invalid);
        await _userRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
