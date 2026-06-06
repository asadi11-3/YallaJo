using ContentCore.Application.Authorization;
using ContentCore.Application.Commands.Attachment.UploadAttachment;
using ContentCore.Application.Interfaces;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

/// <summary>
/// CCD-5 security fix verification: proves that uploading an image attachment to a
/// <see cref="EntityType.Blog"/> is ownership-enforced server-side through the REAL
/// <see cref="OwnershipGuard"/> (not a substitute). The guard resolves Blog ownership
/// via <see cref="IEntityOwnershipResolver"/> (→ Blog AuthorId in production), so:
/// the Blog author may upload; any other non-admin caller is Forbidden.
///
/// This same <c>UploadAttachmentCommand</c> path backs BOTH the single-upload endpoint
/// (POST /attachments) and the bulk endpoint (POST /attachments/images), so a single
/// proof at the handler covers both exposed routes.
/// </summary>
public sealed class UploadAttachmentBlogOwnershipTests
{
    private static UploadAttachmentCommandHandler BuildHandler(
        ICurrentUser currentUser,
        IEntityOwnershipResolver resolver,
        IFileStorageService fileStorage,
        IMediaProcessingQueue queue)
    {
        // REAL guard wired to a substituted resolver — exercises the actual
        // admin-tier + ownership decision logic for EntityType.Blog.
        var guard = new OwnershipGuard(currentUser, resolver);

        return new UploadAttachmentCommandHandler(
            fileStorage,
            Substitute.For<IAttachmentRepository>(),
            OwnershipAuthFixture.NoOpUnitOfWork(),
            currentUser,
            guard,
            queue,
            cache: OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<UploadAttachmentCommandHandler>>());
    }

    private static UploadAttachmentCommand BlogImageCommand(Guid blogId, Guid uploadedByUserId) =>
        new(new MemoryStream([0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x43]), // valid JPEG header
            FileName: "photo.jpg",
            ContentType: "image/jpeg",
            FileSize: 1024,
            EntityType: EntityType.Blog,
            EntityId: blogId,
            Type: AttachmentType.Image,
            UploadedByUserId: uploadedByUserId);

    [Fact]
    public async Task BlogOwner_CanUploadImage_ToOwnBlog()
    {
        var authorId = Guid.NewGuid();
        var blogId = Guid.NewGuid();

        // Resolver reports the Blog is owned by the caller (mirrors BlogOwnershipService → AuthorId).
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(authorId));

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<FileUploadResult>.Success(new FileUploadResult("https://cdn/blogs/photo.jpg", "blogs/photo.jpg", 1024)));
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = BuildHandler(OwnershipAuthFixture.NonAdminUser(authorId), resolver, fileStorage, queue);

        var result = await handler.Handle(BlogImageCommand(blogId, authorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("the Blog author may upload images to their own article");
        result.Outcome.Should().Be(Outcome.Created);
        await fileStorage.Received(1)
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonOwner_CannotUploadImage_ToSomeoneElsesBlog()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid(); // different creator who knows the BlogId
        var blogId = Guid.NewGuid();

        // The Blog is owned by someone else.
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(ownerId));

        var fileStorage = Substitute.For<IFileStorageService>();
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = BuildHandler(OwnershipAuthFixture.NonAdminUser(attackerId), resolver, fileStorage, queue);

        var result = await handler.Handle(BlogImageCommand(blogId, attackerId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden,
            "a creator must not upload images to a Blog they do not own");
        // The file must NEVER be stored when ownership fails.
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
        await queue.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task NonOwner_BlogNotFound_IsRejected_NoStorageWrite()
    {
        var attackerId = Guid.NewGuid();
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound());

        var fileStorage = Substitute.For<IFileStorageService>();
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = BuildHandler(OwnershipAuthFixture.NonAdminUser(attackerId), resolver, fileStorage, queue);

        var result = await handler.Handle(BlogImageCommand(Guid.NewGuid(), attackerId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task AdminTier_CanUploadImage_ToAnyBlog()
    {
        var adminId = Guid.NewGuid();
        var blogId = Guid.NewGuid();

        // Even if the resolver would report a different owner, admin-tier bypasses it.
        var resolver = OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.ValidOwner(Guid.NewGuid()));

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<FileUploadResult>.Success(new FileUploadResult("https://cdn/blogs/photo.jpg", "blogs/photo.jpg", 1024)));
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = BuildHandler(OwnershipAuthFixture.AdminUser(adminId), resolver, fileStorage, queue);

        var result = await handler.Handle(BlogImageCommand(blogId, adminId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("Admin/Owner/SuperAdmin retain their existing bypass");
        result.Outcome.Should().Be(Outcome.Created);
    }
}
