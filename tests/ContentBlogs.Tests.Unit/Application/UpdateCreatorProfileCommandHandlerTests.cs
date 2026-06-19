using ContentBlogs.Application.Commands.Creator.UpdateProfile;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// CA-3 behaviour for the creator profile update handler: display name / bio / slug
/// can be updated, but the AvatarUrl is intentionally NOT accepted from profile-update
/// input. The existing avatar is always preserved server-side regardless of any value
/// a client might attempt to inject, because the avatar can only change through the
/// managed upload/clear endpoints.
/// </summary>
public sealed class UpdateCreatorProfileCommandHandlerTests
{
    private readonly ICreatorProfileRepository _profileRepo = Substitute.For<ICreatorProfileRepository>();
    private readonly IContentBlogsUnitOfWork _uow = Substitute.For<IContentBlogsUnitOfWork>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();

    public UpdateCreatorProfileCommandHandlerTests()
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
            displayName: "Original Name",
            bio: "Original bio",
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

    private UpdateCreatorProfileCommandHandler Sut() => new(
        _profileRepo, _uow, _cache, _currentUser,
        NullLogger<UpdateCreatorProfileCommandHandler>.Instance);

    [Fact]
    public async Task Update_DisplayNameAndBio_Succeeds_AndSaves()
    {
        var profile = NewProfile("/uploads/creators/avatars/current.png");
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UpdateCreatorProfileCommand("New Name", "New bio", Slug: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.DisplayName.Should().Be("New Name");
        profile.Bio.Should().Be("New bio");
        _profileRepo.Received(1).Update(profile);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_MissingProfile_ReturnsNotFound()
    {
        SetupProfile(null);

        var result = await Sut().Handle(
            new UpdateCreatorProfileCommand("New Name", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_PreservesExistingLocalAvatarUrl()
    {
        const string existing = "/uploads/creators/avatars/current.png";
        var profile = NewProfile(existing);
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UpdateCreatorProfileCommand("New Name", "New bio", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.AvatarUrl.Should().Be(existing, "the avatar must be preserved on a profile update");
    }

    [Fact]
    public async Task Update_PreservesExistingExternalAvatarUrl()
    {
        const string existing = "https://cdn.example.com/avatars/current.png";
        var profile = NewProfile(existing);
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UpdateCreatorProfileCommand("New Name", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.AvatarUrl.Should().Be(existing, "previously stored external avatars must continue to display");
    }

    [Fact]
    public async Task Update_DoesNotClearAvatar_WhenDisplayNameChanges()
    {
        const string existing = "/uploads/creators/avatars/current.png";
        var profile = NewProfile(existing);
        SetupProfile(profile);

        var result = await Sut().Handle(
            new UpdateCreatorProfileCommand("New Name", null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.AvatarUrl.Should().NotBeNull();
        profile.AvatarUrl.Should().Be(existing);
    }

    [Fact]
    public void Command_HasNoAvatarUrl_Member()
    {
        // CA-3 contract: profile-update input cannot carry an avatar value at all,
        // so no client can overwrite/clear the managed avatar via this path.
        typeof(UpdateCreatorProfileCommand)
            .GetProperty("AvatarUrl")
            .Should().BeNull("avatar changes are restricted to the managed upload/clear endpoints");
    }
}
