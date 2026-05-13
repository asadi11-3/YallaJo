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
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

// ─────────────────────────────────────────────────────────────────────────────
// Shared builder for UploadAttachmentCommandHandler in tests
// ─────────────────────────────────────────────────────────────────────────────
file static class UploadAttachmentHandlerBuilder
{
    internal static UploadAttachmentCommandHandler Build(
        IFileStorageService? fileStorageService = null,
        IAttachmentRepository? attachmentRepository = null,
        IContentCoreUnitOfWork? unitOfWork = null,
        ICurrentUser? currentUser = null,
        IEntityOwnershipResolver? ownershipResolver = null,
        IMediaProcessingQueue? mediaProcessingQueue = null)
    {
        return new UploadAttachmentCommandHandler(
            fileStorageService ?? Substitute.For<IFileStorageService>(),
            attachmentRepository ?? Substitute.For<IAttachmentRepository>(),
            unitOfWork ?? OwnershipAuthFixture.NoOpUnitOfWork(),
            currentUser ?? OwnershipAuthFixture.NonAdminUser(Guid.NewGuid()),
            ownershipResolver ?? OwnershipAuthFixture.ResolverReturning(
                OwnershipAuthFixture.ValidOwner(Guid.NewGuid())),
            mediaProcessingQueue ?? Substitute.For<IMediaProcessingQueue>(),
            cache: OwnershipAuthFixture.NoOpCache(),
            Substitute.For<ILogger<UploadAttachmentCommandHandler>>());
    }

    /// <summary>Valid JPEG-header stream (real magic bytes). Stream is owned by the caller.</summary>
    internal static MemoryStream JpegStream() =>
        new([0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x43]);

    internal static UploadAttachmentCommand JpegCommand(Guid entityId, Guid uploadedByUserId) =>
        new(JpegStream(),
            FileName: "photo.jpg",
            ContentType: "image/jpeg",
            FileSize: 1024,
            EntityType: EntityType.Tour,
            EntityId: entityId,
            Type: AttachmentType.Image,
            UploadedByUserId: uploadedByUserId);
}

public sealed class UploadAttachmentCommandHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnInvalidWhenMimeTypeDoesNotMatchBinarySignature()
    {
        var userId = Guid.NewGuid();

        var fileStorageService = Substitute.For<IFileStorageService>();
        var attachmentRepository = Substitute.For<IAttachmentRepository>();
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var ownershipResolver = Substitute.For<IEntityOwnershipResolver>();
        var mediaProcessingQueue = Substitute.For<IMediaProcessingQueue>();
        var logger = Substitute.For<ILogger<UploadAttachmentCommandHandler>>();

        currentUser.UserId.Returns(userId);
        currentUser.IsInRole("Admin").Returns(false);

        // Owner-of-target: ownership probe should succeed so the test can reach
        // the file-validation paths it asserts on.
        ownershipResolver
            .ResolveAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(
                IsSupported: true,
                Exists: true,
                IsDeleted: false,
                OwnerUserId: userId));

        var handler = new UploadAttachmentCommandHandler(
            fileStorageService,
            attachmentRepository,
            unitOfWork,
            currentUser,
            ownershipResolver,
            mediaProcessingQueue,
            cache: null!,
            logger);

        await using var stream = new MemoryStream("%PDF-1.7"u8.ToArray());
        var command = new UploadAttachmentCommand(
            stream,
            FileName: "document.pdf",
            ContentType: "image/jpeg",
            FileSize: stream.Length,
            EntityType: EntityType.Place,
            EntityId: Guid.NewGuid(),
            Type: AttachmentType.Document,
            UploadedByUserId: userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(x => x.Code == "Attachment.ContentTypeMismatch");

        await fileStorageService
            .DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task HandleShouldReturnInvalidWhenFileExtensionDoesNotMatchBinarySignature()
    {
        var userId = Guid.NewGuid();

        var fileStorageService = Substitute.For<IFileStorageService>();
        var attachmentRepository = Substitute.For<IAttachmentRepository>();
        var unitOfWork = Substitute.For<IContentCoreUnitOfWork>();
        var currentUser = Substitute.For<ICurrentUser>();
        var ownershipResolver = Substitute.For<IEntityOwnershipResolver>();
        var mediaProcessingQueue = Substitute.For<IMediaProcessingQueue>();
        var logger = Substitute.For<ILogger<UploadAttachmentCommandHandler>>();

        currentUser.UserId.Returns(userId);
        currentUser.IsInRole("Admin").Returns(false);

        // Owner-of-target: ownership probe should succeed so the test can reach
        // the file-validation paths it asserts on.
        ownershipResolver
            .ResolveAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EntityOwnershipResolution(
                IsSupported: true,
                Exists: true,
                IsDeleted: false,
                OwnerUserId: userId));

        var handler = new UploadAttachmentCommandHandler(
            fileStorageService,
            attachmentRepository,
            unitOfWork,
            currentUser,
            ownershipResolver,
            mediaProcessingQueue,
            cache: null!,
            logger);

        var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xDB };
        await using var stream = new MemoryStream(jpegHeader);

        var command = new UploadAttachmentCommand(
            stream,
            FileName: "photo.png",
            ContentType: "image/jpeg",
            FileSize: stream.Length,
            EntityType: EntityType.Place,
            EntityId: Guid.NewGuid(),
            Type: AttachmentType.Image,
            UploadedByUserId: userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(x => x.Code == "Attachment.ExtensionMismatch");

        await fileStorageService
            .DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
    }
}

/// <summary>
/// Authorization regression tests for <see cref="UploadAttachmentCommandHandler"/>
/// covering target-entity ownership (CONTENTCORE-STD-P1-003-followup-attachments).
/// File-format tests remain in <see cref="UploadAttachmentCommandHandlerTests"/>.
/// </summary>
public sealed class UploadAttachmentAuthorizationTests
{
    // ── Unauthenticated ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsNull()
    {
        var handler = UploadAttachmentHandlerBuilder.Build(
            currentUser: OwnershipAuthFixture.UnauthenticatedUser());

        await using var stream = UploadAttachmentHandlerBuilder.JpegStream();
        var command = UploadAttachmentHandlerBuilder.JpegCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
    }

    // ── Anti-spoofing guard (UploadedByUserId != self) ─────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUploadedByUserIdIsSpoofed()
    {
        // Non-admin claims to upload on behalf of a different user.
        var callerId = Guid.NewGuid();
        var spoofedId = Guid.NewGuid();
        var resolver = Substitute.For<IEntityOwnershipResolver>();
        var fileStorage = Substitute.For<IFileStorageService>();

        var handler = UploadAttachmentHandlerBuilder.Build(
            fileStorageService: fileStorage,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: resolver);

        await using var stream = UploadAttachmentHandlerBuilder.JpegStream();
        var command = UploadAttachmentHandlerBuilder.JpegCommand(Guid.NewGuid(), spoofedId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        // Anti-spoofing must fire BEFORE resolver
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
        // Storage must NOT be touched
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
    }

    // ── Target not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTargetEntityDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var fileStorage = Substitute.For<IFileStorageService>();
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = UploadAttachmentHandlerBuilder.Build(
            fileStorageService: fileStorage,
            mediaProcessingQueue: queue,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.NotFound()));

        await using var stream = UploadAttachmentHandlerBuilder.JpegStream();
        var command = UploadAttachmentHandlerBuilder.JpegCommand(Guid.NewGuid(), userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
        await queue.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    // ── Target deleted ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenTargetEntityIsDeleted()
    {
        var userId = Guid.NewGuid();
        var fileStorage = Substitute.For<IFileStorageService>();

        var handler = UploadAttachmentHandlerBuilder.Build(
            fileStorageService: fileStorage,
            currentUser: OwnershipAuthFixture.NonAdminUser(userId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(
                OwnershipAuthFixture.Deleted(ownerUserId: userId)));

        await using var stream = UploadAttachmentHandlerBuilder.JpegStream();
        var command = UploadAttachmentHandlerBuilder.JpegCommand(Guid.NewGuid(), userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().ContainSingle(e => e.Code == "Attachment.TargetDeleted");
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
    }

    // ── Non-owner, non-admin ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerIsNotTargetOwnerAndNotAdminTier()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var fileStorage = Substitute.For<IFileStorageService>();
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = UploadAttachmentHandlerBuilder.Build(
            fileStorageService: fileStorage,
            mediaProcessingQueue: queue,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(
                OwnershipAuthFixture.ValidOwner(ownerId)));   // caller ≠ owner

        await using var stream = UploadAttachmentHandlerBuilder.JpegStream();
        var command = UploadAttachmentHandlerBuilder.JpegCommand(Guid.NewGuid(), callerId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        // Storage must never be touched before auth succeeds
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
        await queue.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ShouldAllowOwner_WhenCallerOwnsTargetEntity()
    {
        var callerId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new FileUploadResult("https://cdn/app/photo.jpg", "attachments/photo.jpg", 1024));
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = UploadAttachmentHandlerBuilder.Build(
            fileStorageService: fileStorage,
            mediaProcessingQueue: queue,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(
                OwnershipAuthFixture.ValidOwner(callerId)));

        var command = UploadAttachmentHandlerBuilder.JpegCommand(entityId, callerId);
        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await fileStorage.Received(1)
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await queue.Received(1).EnqueueAsync(Arg.Any<MediaProcessingJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalid_WhenEntityTypeIsUnsupported()
    {
        var callerId = Guid.NewGuid();
        var fileStorage = Substitute.For<IFileStorageService>();
        var queue = Substitute.For<IMediaProcessingQueue>();

        var handler = UploadAttachmentHandlerBuilder.Build(
            fileStorageService: fileStorage,
            mediaProcessingQueue: queue,
            currentUser: OwnershipAuthFixture.NonAdminUser(callerId),
            ownershipResolver: OwnershipAuthFixture.ResolverReturning(OwnershipAuthFixture.Unsupported()));

        var command = UploadAttachmentHandlerBuilder.JpegCommand(Guid.NewGuid(), callerId);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Errors.Should().Contain(x => x.Code == "Attachment.UnsupportedEntityType");
        await fileStorage.DidNotReceiveWithAnyArgs()
            .UploadAsync(default!, default!, default!, default!, default);
        await queue.DidNotReceiveWithAnyArgs().EnqueueAsync(default!, default);
    }

    // ── Admin-tier bypasses resolver ──────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldNotCallResolver_WhenCallerIsAdminTier()
    {
        var userId = Guid.NewGuid();
        var resolver = Substitute.For<IEntityOwnershipResolver>();

        // Provide an invalid file so the handler short-circuits at format validation
        // without needing real storage — we only care that the resolver was skipped.
        var handler = UploadAttachmentHandlerBuilder.Build(
            currentUser: OwnershipAuthFixture.AdminUser(userId),
            ownershipResolver: resolver);

        // Empty stream → Unknown file type → Outcome.Invalid at format check (no storage call)
        await using var emptyStream = new MemoryStream([]);
        var command = new UploadAttachmentCommand(
            emptyStream, "file.jpg", "image/jpeg", 0,
            EntityType.Tour, Guid.NewGuid(), AttachmentType.Image, userId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        await resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
    }
}
