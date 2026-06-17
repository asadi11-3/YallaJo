using System.Linq.Expressions;
using ContentCore.Application.Authorization;
using ContentCore.Application.Queries.Attachment.GetEntityAttachments;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Tests.Unit;

/// <summary>
/// Creator Backend Contract Polish (Gap 2): GET entity attachments must surface the
/// primary-image flag (AttachmentDto.IsPrimary), sourced from the EntityImage join, so
/// the Creator Article Images UI can highlight which image is currently primary.
/// </summary>
public sealed class GetEntityAttachmentsPrimaryTests
{
    private static Attachment NewImage(Guid entityId) =>
        Attachment.Create(EntityType.Blog, entityId, AttachmentType.Image,
            $"https://cdn/app/{Guid.NewGuid():N}.jpg", Guid.NewGuid());

    // Patch 1B added an IOwnershipGuard ownership check to the handler. These tests
    // assert the primary-flag projection on the authorized (owner) path, so the
    // guard is stubbed to allow access.
    private static IOwnershipGuard AllowingGuard()
    {
        var guard = Substitute.For<IOwnershipGuard>();
        guard.AuthorizeAsync(
                Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        return guard;
    }

    [Fact]
    public async Task Marks_only_the_primary_attachment_as_primary()
    {
        var entityId = Guid.NewGuid();
        var img1 = NewImage(entityId);
        var img2 = NewImage(entityId);
        var img3 = NewImage(entityId);

        var repo = Substitute.For<IAttachmentRepository>();
        repo.GetAllAsync(
                Arg.Any<Expression<Func<Attachment, bool>>?>(),
                Arg.Any<Func<IQueryable<Attachment>, IQueryable<Attachment>>?>(),
                Arg.Any<Func<IQueryable<Attachment>, IOrderedQueryable<Attachment>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Attachment> { img1, img2, img3 });

        // img2 is the primary image per the EntityImage join.
        repo.GetEntityImagesAsync(EntityType.Blog, entityId, Arg.Any<CancellationToken>())
            .Returns(new List<EntityImage>
            {
                EntityImage.Create(EntityType.Blog, entityId, img1.Id, ImageSize.Original, 0, isPrimary: false),
                EntityImage.Create(EntityType.Blog, entityId, img2.Id, ImageSize.Original, 1, isPrimary: true),
                EntityImage.Create(EntityType.Blog, entityId, img3.Id, ImageSize.Original, 2, isPrimary: false),
            });

        var handler = new GetEntityAttachmentsQueryHandler(
            repo, AllowingGuard(), NullLogger<GetEntityAttachmentsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetEntityAttachmentsQuery(EntityType.Blog, entityId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dtos = result.Value!;
        dtos.Single(d => d.Id == img1.Id).IsPrimary.Should().BeFalse();
        dtos.Single(d => d.Id == img2.Id).IsPrimary.Should().BeTrue();
        dtos.Single(d => d.Id == img3.Id).IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task No_primary_when_no_entity_image_is_primary()
    {
        var entityId = Guid.NewGuid();
        var img1 = NewImage(entityId);

        var repo = Substitute.For<IAttachmentRepository>();
        repo.GetAllAsync(
                Arg.Any<Expression<Func<Attachment, bool>>?>(),
                Arg.Any<Func<IQueryable<Attachment>, IQueryable<Attachment>>?>(),
                Arg.Any<Func<IQueryable<Attachment>, IOrderedQueryable<Attachment>>?>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Attachment> { img1 });

        repo.GetEntityImagesAsync(EntityType.Blog, entityId, Arg.Any<CancellationToken>())
            .Returns(new List<EntityImage>());

        var handler = new GetEntityAttachmentsQueryHandler(
            repo, AllowingGuard(), NullLogger<GetEntityAttachmentsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetEntityAttachmentsQuery(EntityType.Blog, entityId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Single().IsPrimary.Should().BeFalse();
    }
}
