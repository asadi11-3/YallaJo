using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using Security.Application.Interfaces;
using Security.Contracts.Abstractions;
using Security.Domain.Entities;
using Security.Domain.Errors;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Tests.Unit;

/// <summary>
/// Locks in the standardized "user not found" error code across
/// <see cref="IUserRegistrationService"/> entry points. Previously each
/// site emitted an inline literal (<c>"User.NotFound"</c>) that conflicted
/// with the catalog constant <see cref="UserErrors.NotFound"/>
/// (code <c>"NotFound.User"</c>). All sites must now surface the catalog
/// constant.
/// </summary>
public sealed class UserRegistrationServiceUserErrorsTests
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

    [Fact]
    public async Task MarkPendingActivation_ShouldUseCatalogUserNotFound_WhenUserMissing()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((User?)null);

        var result = await CreateSut().MarkPendingActivationAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task MarkPendingPasswordReset_ShouldUseCatalogUserNotFound_WhenUserMissing()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns((User?)null);

        var result = await CreateSut().MarkPendingPasswordResetAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task CompleteActivation_ShouldUseCatalogUserNotFound_WhenUserMissing()
    {
        _userRepo.GetByIdWithEmailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await CreateSut().CompleteActivationAsync(
            Guid.NewGuid(),
            "user@example.com",
            "StrongPassw0rd!",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(UserErrors.NotFound.Code);
    }
}
