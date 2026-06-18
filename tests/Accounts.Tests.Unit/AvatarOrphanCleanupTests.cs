using Accounts.Application.Commands.DeleteAvatar;
using Accounts.Application.Commands.UpdateAvatar;
using Accounts.Domain.Entities;
using Accounts.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

namespace Accounts.Tests.Unit;

/// <summary>
/// M2 orphan-cleanup behaviour for the avatar update/delete handlers:
/// the previous LOCAL (/uploads/...) avatar blob is best-effort deleted after a
/// successful save; external URLs are never deleted; cleanup failures are tolerated.
/// </summary>
public sealed class AvatarOrphanCleanupTests
{
    private readonly IProfileRepository _profileRepo = Substitute.For<IProfileRepository>();
    private readonly IAccountsUnitOfWork _uow = Substitute.For<IAccountsUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly Guid _userId = Guid.NewGuid();

    public AvatarOrphanCleanupTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_userId);
    }

    private Profile ProfileWithAvatar(string? avatarUrl)
    {
        var profile = Profile.Create(_userId, "Test", "User");
        profile.SetAvatarUrl(avatarUrl);
        _profileRepo
            .FirstOrDefaultAsync(
                Arg.Any<Expression<Func<Profile, bool>>>(),
                Arg.Any<Func<IQueryable<Profile>, IQueryable<Profile>>?>(),
                Arg.Any<Func<IQueryable<Profile>, IOrderedQueryable<Profile>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(profile);
        return profile;
    }

    private UpdateAvatarCommandHandler UpdateSut() => new(
        _profileRepo, _uow, _currentUser, _cache, _fileStorage,
        NullLogger<UpdateAvatarCommandHandler>.Instance);

    private DeleteAvatarCommandHandler DeleteSut() => new(
        _profileRepo, _uow, _currentUser, _cache, _fileStorage,
        NullLogger<DeleteAvatarCommandHandler>.Instance);

    // ----- UPDATE -----

    [Fact]
    public async Task UpdateAvatar_DeletesOldLocalAvatar_AfterSuccessfulSave()
    {
        ProfileWithAvatar("/uploads/avatars/old.png");

        var result = await UpdateSut().Handle(
            new UpdateAvatarCommand("/uploads/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("/uploads/avatars/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAvatar_DoesNotDeleteExternalOldAvatar()
    {
        ProfileWithAvatar("https://cdn.example.com/avatars/old.png");

        var result = await UpdateSut().Handle(
            new UpdateAvatarCommand("/uploads/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAvatar_DoesNotDeleteWhenOldEqualsNew()
    {
        ProfileWithAvatar("/uploads/avatars/same.png");

        var result = await UpdateSut().Handle(
            new UpdateAvatarCommand("/uploads/avatars/same.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAvatar_DoesNotDeleteWhenNoPriorAvatar()
    {
        ProfileWithAvatar(null);

        var result = await UpdateSut().Handle(
            new UpdateAvatarCommand("/uploads/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAvatar_DeleteFailure_DoesNotFailRequest()
    {
        ProfileWithAvatar("/uploads/avatars/old.png");
        _fileStorage
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("storage offline"));

        var result = await UpdateSut().Handle(
            new UpdateAvatarCommand("/uploads/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("/uploads/avatars/old.png", Arg.Any<CancellationToken>());
    }

    // ----- DELETE -----

    [Fact]
    public async Task DeleteAvatar_DeletesOldLocalAvatar_AfterSuccessfulSave()
    {
        ProfileWithAvatar("/uploads/avatars/old.png");

        var result = await DeleteSut().Handle(new DeleteAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("/uploads/avatars/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAvatar_DoesNotDeleteExternalOldAvatar()
    {
        ProfileWithAvatar("https://cdn.example.com/avatars/old.png");

        var result = await DeleteSut().Handle(new DeleteAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAvatar_DoesNotDeleteWhenNoPriorAvatar()
    {
        ProfileWithAvatar(null);

        var result = await DeleteSut().Handle(new DeleteAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAvatar_DeleteFailure_DoesNotFailRequest()
    {
        ProfileWithAvatar("/uploads/avatars/old.png");
        _fileStorage
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("storage offline"));

        var result = await DeleteSut().Handle(new DeleteAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("/uploads/avatars/old.png", Arg.Any<CancellationToken>());
    }
}
