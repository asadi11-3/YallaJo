using ContentTours.Application.Commands.TourGuides.ClearAvatar;
using ContentTours.Application.Commands.TourGuides.UploadAvatar;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Behaviour tests for the managed tour-guide avatar handlers (TG-AVATAR-B):
/// <see cref="UploadGuideAvatarCommandHandler"/> and <see cref="ClearGuideAvatarCommandHandler"/>.
/// Verifies owner-scoped resolution, AvatarUrl assignment/clear, cache invalidation,
/// and best-effort cleanup of the previous LOCAL avatar file (external URLs are never deleted,
/// and a failed delete must not fail the request after a successful save).
/// </summary>
public sealed class TourGuideAvatarManagedHandlerTests
{
    private static readonly Guid OwnerUserId = Guid.NewGuid();
    private const string NewLocalUrl = "/uploads/guides/avatars/new.png";
    private const string OldLocalUrl = "/uploads/guides/avatars/old.png";
    private const string ExternalUrl = "https://cdn.example.com/old-avatar.png";

    private static TourGuide BuildGuide(string? seedAvatarUrl = null)
    {
        var guide = TourGuide.Register(
            userId: OwnerUserId,
            displayName: "Test Guide",
            slug: "test-guide",
            bio: "Experienced local guide.",
            yearsOfExperience: 3,
            hasFirstAid: true,
            moTALicenseNumber: null).Value;

        if (seedAvatarUrl is not null)
        {
            guide.UpdateAvatar(seedAvatarUrl).IsSuccess.Should().BeTrue();
        }

        return guide;
    }

    private static (
        ITourGuideRepository GuideRepo,
        IContentToursUnitOfWork Uow,
        HybridCache Cache,
        ICurrentUser CurrentUser,
        IFileStorageService Storage) BuildDeps(TourGuide? guide, bool authenticated = true)
    {
        var guideRepo = Substitute.For<ITourGuideRepository>();
        guideRepo
            .GetByUserIdAsync(OwnerUserId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
            .Returns(guide);

        var uow = Substitute.For<IContentToursUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var cache = Substitute.For<HybridCache>();

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(authenticated);
        currentUser.UserId.Returns(authenticated ? OwnerUserId : (Guid?)null);

        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        return (guideRepo, uow, cache, currentUser, storage);
    }

    private static UploadGuideAvatarCommandHandler BuildUploadHandler(
        ITourGuideRepository guideRepo, IContentToursUnitOfWork uow, HybridCache cache,
        ICurrentUser currentUser, IFileStorageService storage) =>
        new(guideRepo, uow, cache, currentUser, storage,
            NullLogger<UploadGuideAvatarCommandHandler>.Instance);

    private static ClearGuideAvatarCommandHandler BuildClearHandler(
        ITourGuideRepository guideRepo, IContentToursUnitOfWork uow, HybridCache cache,
        ICurrentUser currentUser, IFileStorageService storage) =>
        new(guideRepo, uow, cache, currentUser, storage,
            NullLogger<ClearGuideAvatarCommandHandler>.Instance);

    // ---------------------------------------------------------------------
    // Upload
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Upload_SetsAvatarUrl_Persists_AndInvalidatesCache()
    {
        var guide = BuildGuide();
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide);
        var handler = BuildUploadHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new UploadGuideAvatarCommand(NewLocalUrl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guide.AvatarUrl.Should().Be(NewLocalUrl);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(2).RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_WhenProfileMissing_ReturnsNotFound()
    {
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide: null);
        var handler = BuildUploadHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new UploadGuideAvatarCommand(NewLocalUrl), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide: null, authenticated: false);
        var handler = BuildUploadHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new UploadGuideAvatarCommand(NewLocalUrl), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_WhenReplacingOldLocalAvatar_DeletesOldLocalFile()
    {
        var guide = BuildGuide(seedAvatarUrl: OldLocalUrl);
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide);
        var handler = BuildUploadHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new UploadGuideAvatarCommand(NewLocalUrl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guide.AvatarUrl.Should().Be(NewLocalUrl);
        await storage.Received(1).DeleteAsync(OldLocalUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_WhenOldAvatarIsExternalUrl_DoesNotDelete()
    {
        var guide = BuildGuide(seedAvatarUrl: ExternalUrl);
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide);
        var handler = BuildUploadHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new UploadGuideAvatarCommand(NewLocalUrl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_WhenOldFileDeleteThrows_StillSucceeds()
    {
        var guide = BuildGuide(seedAvatarUrl: OldLocalUrl);
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide);
        storage.DeleteAsync(OldLocalUrl, Arg.Any<CancellationToken>())
            .Throws(new IOException("disk error"));
        var handler = BuildUploadHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new UploadGuideAvatarCommand(NewLocalUrl), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guide.AvatarUrl.Should().Be(NewLocalUrl);
    }

    // ---------------------------------------------------------------------
    // Clear
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Clear_SetsAvatarUrlNull_Persists_AndDeletesOldLocalFile()
    {
        var guide = BuildGuide(seedAvatarUrl: OldLocalUrl);
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide);
        var handler = BuildClearHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new ClearGuideAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guide.AvatarUrl.Should().BeNull();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await cache.Received(2).RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await storage.Received(1).DeleteAsync(OldLocalUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clear_WhenOldAvatarIsExternalUrl_DoesNotDelete()
    {
        var guide = BuildGuide(seedAvatarUrl: ExternalUrl);
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide);
        var handler = BuildClearHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new ClearGuideAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        guide.AvatarUrl.Should().BeNull();
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clear_WhenProfileMissing_ReturnsNotFound()
    {
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide: null);
        var handler = BuildClearHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new ClearGuideAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clear_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var (guideRepo, uow, cache, currentUser, storage) = BuildDeps(guide: null, authenticated: false);
        var handler = BuildClearHandler(guideRepo, uow, cache, currentUser, storage);

        var result = await handler.Handle(new ClearGuideAvatarCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
