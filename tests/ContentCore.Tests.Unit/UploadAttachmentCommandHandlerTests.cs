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
        var mediaProcessingQueue = Substitute.For<IMediaProcessingQueue>();
        var logger = Substitute.For<ILogger<UploadAttachmentCommandHandler>>();

        currentUser.UserId.Returns(userId);
        currentUser.IsInRole("Admin").Returns(false);

        var handler = new UploadAttachmentCommandHandler(
            fileStorageService,
            attachmentRepository,
            unitOfWork,
            currentUser,
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
        var mediaProcessingQueue = Substitute.For<IMediaProcessingQueue>();
        var logger = Substitute.For<ILogger<UploadAttachmentCommandHandler>>();

        currentUser.UserId.Returns(userId);
        currentUser.IsInRole("Admin").Returns(false);

        var handler = new UploadAttachmentCommandHandler(
            fileStorageService,
            attachmentRepository,
            unitOfWork,
            currentUser,
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
