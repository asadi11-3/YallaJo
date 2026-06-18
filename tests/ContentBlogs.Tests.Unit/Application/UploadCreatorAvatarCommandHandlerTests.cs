using ContentBlogs.Application.Commands.Creator.UploadAvatar;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// CA-3 behaviour for the managed creator-avatar upload handler:
/// resolves the current creator profile, sets the new public URL, saves, and
/// best-effort deletes the previous LOCAL (/uploads/...) avatar blob after a
/// successful save. External URLs are never deleted; cleanup failures are tolerated.
/// </summary>
public sealed class UploadCreatorAvatarCommandHandlerTests
{
    private readonly ICreatorProfileRepository _profileRepo = Substitute.For<ICreatorProfileRepository>();
    private readonly IContentBlogsUnitOfWork _uow = Substitute.For<IContentBlogsUnitOfWork>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly Guid _userId = Guid.NewGuid();

    public UploadCreatorAvatarCommandHandlerTests()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_userId);
    }

    private CreatorProfile NewProfile(string? avatarUrl)
    {
        var result = CreatorProfile.Create(
            userId: _userId,
            applicationId: Guid.NewGuid(),
            slug: "creator-slug",
            displayName: "Creator",
            bio: "Travel writer",
            avatarUrl: null);
        result.IsSuccess.Should().BeTrue();
        var profile = result.Value!;
        if (avatarUrl is not null)
        {
            profile.UpdateAvatar(avatarUrl);
        }
        return profile;
    }

    private void SetupProfile(CreatorProfile? profile) =>
        _profileRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(profile);

    private UploadCreatorAvatarCommandHandler Sut() => new(
        _profileRepo, _uow, _cache, _currentUser, _fileStorage,
        NullLogger<UploadCreatorAvatarCommandHandler>.Instance);

    [Fact]
    public async Task Upload_SetsAvatarUrl_AndSaves()
    {
        var profile = NewProfile(null);
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UploadCreatorAvatarCommand("/uploads/creators/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.AvatarUrl.Should().Be("/uploads/creators/avatars/new.png");
        _profileRepo.Received(1).Update(profile);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_MissingProfile_ReturnsNotFound()
    {
        SetupProfile(null);

        var result = await Sut().Handle(
            new UploadCreatorAvatarCommand("/uploads/creators/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_DeletesOldLocalAvatar_AfterSuccessfulSave()
    {
        var profile = NewProfile("/uploads/creators/avatars/old.png");
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UploadCreatorAvatarCommand("/uploads/creators/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("/uploads/creators/avatars/old.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_DoesNotDeleteExternalOldAvatar()
    {
        var profile = NewProfile("https://cdn.example.com/avatars/old.png");
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UploadCreatorAvatarCommand("/uploads/creators/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_DoesNotDeleteWhenNoPriorAvatar()
    {
        var profile = NewProfile(null);
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UploadCreatorAvatarCommand("/uploads/creators/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_DeleteFailure_DoesNotFailRequest()
    {
        var profile = NewProfile("/uploads/creators/avatars/old.png");
        SetupProfile(profile);
        _fileStorage
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("storage offline"));

        var result = await Sut().Handle(
            new UploadCreatorAvatarCommand("/uploads/creators/avatars/new.png"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _fileStorage.Received(1).DeleteAsync("/uploads/creators/avatars/old.png", Arg.Any<CancellationToken>());
    }
}
