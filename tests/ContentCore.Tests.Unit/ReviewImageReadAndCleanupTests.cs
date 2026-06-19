using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Infrastructure.Persistence;
using ContentCore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Storage;

namespace ContentCore.Tests.Unit;

/// <summary>
/// REVIEWS-IMG-B: covers the new batched public-image read
/// (<see cref="PublicEntityImageReader.GetEntityImagesBatchAsync"/>) and the
/// best-effort lifecycle cleanup (<see cref="EntityAttachmentCleanupService"/>)
/// that powers public review images (EntityType=Review). The InMemory provider is
/// used because the production <c>ContentCoreDbContext</c> declares a SQL Server
/// <c>nvarchar(max)</c> outbox column that SQLite rejects.
/// </summary>
public sealed class ReviewImageReadAndCleanupTests : IDisposable
{
    private readonly ContentCoreDbContext _ctx;

    public ReviewImageReadAndCleanupTests()
    {
        var options = new DbContextOptionsBuilder<ContentCoreDbContext>()
            .UseInMemoryDatabase(databaseName: $"review-images-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _ctx = new ContentCoreDbContext(options);
    }

    public void Dispose() => _ctx.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Attachment> SeedImageAsync(
        Guid reviewId, string url, int sortOrder, bool isPrimary, string? thumbnailUrl = null)
    {
        var attachment = Attachment.Create(
            EntityType.Review, reviewId, AttachmentType.Image, url, Guid.NewGuid(), sortOrder: sortOrder);
        if (thumbnailUrl is not null)
        {
            attachment.SetThumbnailUrl(thumbnailUrl);
        }

        _ctx.Set<Attachment>().Add(attachment);
        _ctx.Set<EntityImage>().Add(
            EntityImage.Create(EntityType.Review, reviewId, attachment.Id, ImageSize.Original, sortOrder, isPrimary));
        await _ctx.SaveChangesAsync();
        return attachment;
    }

    // ── Batched read ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Batch_read_returns_images_grouped_by_review_primary_first()
    {
        var reviewA = Guid.NewGuid();
        var reviewB = Guid.NewGuid();
        await SeedImageAsync(reviewA, "/uploads/reviews/a-secondary.jpg", sortOrder: 1, isPrimary: false);
        await SeedImageAsync(reviewA, "/uploads/reviews/a-primary.jpg", sortOrder: 0, isPrimary: true);
        await SeedImageAsync(reviewB, "/uploads/reviews/b.jpg", sortOrder: 0, isPrimary: true);

        var reader = new PublicEntityImageReader(_ctx);

        var result = await reader.GetEntityImagesBatchAsync(
            "Review", new[] { reviewA, reviewB }, CancellationToken.None);

        result.Should().ContainKey(reviewA);
        result.Should().ContainKey(reviewB);
        // reviewA: primary first (IsPrimary desc).
        result[reviewA].Select(i => i.Url).Should()
            .Equal("/uploads/reviews/a-primary.jpg", "/uploads/reviews/a-secondary.jpg");
        result[reviewB].Should().ContainSingle().Which.Url.Should().Be("/uploads/reviews/b.jpg");
    }

    [Fact]
    public async Task Batch_read_omits_reviews_with_no_images()
    {
        var reviewWith = Guid.NewGuid();
        var reviewWithout = Guid.NewGuid();
        await SeedImageAsync(reviewWith, "/uploads/reviews/x.jpg", 0, isPrimary: true);

        var reader = new PublicEntityImageReader(_ctx);

        var result = await reader.GetEntityImagesBatchAsync(
            "Review", new[] { reviewWith, reviewWithout }, CancellationToken.None);

        result.Should().ContainKey(reviewWith);
        result.Should().NotContainKey(reviewWithout);
    }

    [Fact]
    public async Task Batch_read_returns_empty_for_invalid_entity_type()
    {
        var reader = new PublicEntityImageReader(_ctx);

        var result = await reader.GetEntityImagesBatchAsync(
            "NotARealType", new[] { Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Batch_read_returns_empty_for_empty_id_set()
    {
        var reader = new PublicEntityImageReader(_ctx);

        var result = await reader.GetEntityImagesBatchAsync(
            "Review", Array.Empty<Guid>(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cleanup_removes_attachment_and_entity_image_rows_and_deletes_files()
    {
        var reviewId = Guid.NewGuid();
        await SeedImageAsync(reviewId, "/uploads/reviews/one.jpg", 0, isPrimary: true, thumbnailUrl: "/uploads/reviews/one-thumb.jpg");
        await SeedImageAsync(reviewId, "/uploads/reviews/two.jpg", 1, isPrimary: false);

        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var service = new EntityAttachmentCleanupService(
            _ctx, storage, NullLogger<EntityAttachmentCleanupService>.Instance);

        var removed = await service.DeleteEntityAttachmentsAsync("Review", reviewId, CancellationToken.None);

        removed.Should().Be(2);
        _ctx.Set<Attachment>().Count(a => a.EntityType == EntityType.Review && a.EntityId == reviewId)
            .Should().Be(0);
        _ctx.Set<EntityImage>().Count(i => i.EntityType == EntityType.Review && i.EntityId == reviewId)
            .Should().Be(0);
        // Physical files (incl. the thumbnail) are deleted best-effort.
        await storage.Received().DeleteAsync("/uploads/reviews/one.jpg", Arg.Any<CancellationToken>());
        await storage.Received().DeleteAsync("/uploads/reviews/one-thumb.jpg", Arg.Any<CancellationToken>());
        await storage.Received().DeleteAsync("/uploads/reviews/two.jpg", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cleanup_returns_zero_when_no_attachments_exist()
    {
        var storage = Substitute.For<IFileStorageService>();
        var service = new EntityAttachmentCleanupService(
            _ctx, storage, NullLogger<EntityAttachmentCleanupService>.Instance);

        var removed = await service.DeleteEntityAttachmentsAsync("Review", Guid.NewGuid(), CancellationToken.None);

        removed.Should().Be(0);
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cleanup_removes_rows_even_when_physical_file_delete_fails()
    {
        var reviewId = Guid.NewGuid();
        await SeedImageAsync(reviewId, "/uploads/reviews/boom.jpg", 0, isPrimary: true);

        var storage = Substitute.For<IFileStorageService>();
        storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new IOException("disk error"));

        var service = new EntityAttachmentCleanupService(
            _ctx, storage, NullLogger<EntityAttachmentCleanupService>.Instance);

        var removed = await service.DeleteEntityAttachmentsAsync("Review", reviewId, CancellationToken.None);

        // Best-effort: DB rows are still removed and the count returned despite the
        // file-delete exception being swallowed.
        removed.Should().Be(1);
        _ctx.Set<Attachment>().Count(a => a.EntityType == EntityType.Review && a.EntityId == reviewId)
            .Should().Be(0);
    }

    [Fact]
    public async Task Cleanup_returns_zero_for_invalid_entity_type()
    {
        var storage = Substitute.For<IFileStorageService>();
        var service = new EntityAttachmentCleanupService(
            _ctx, storage, NullLogger<EntityAttachmentCleanupService>.Instance);

        var removed = await service.DeleteEntityAttachmentsAsync("NotARealType", Guid.NewGuid(), CancellationToken.None);

        removed.Should().Be(0);
    }
}
