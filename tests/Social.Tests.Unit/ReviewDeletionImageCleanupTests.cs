using ContentCore.Contracts.Attachments;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Social.Application.Commands.DeleteReview;
using Social.Application.Commands.RemoveReview;
using Social.Application.Interfaces;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Xunit;

namespace Social.Tests.Unit;

/// <summary>
/// REVIEWS-IMG-B: deleting (author) or removing (admin) a review must best-effort
/// clean up that review's public image attachments via IEntityAttachmentCleanupService,
/// and a cleanup failure must never fail the delete/remove request.
/// </summary>
public sealed class ReviewDeletionImageCleanupTests
{
    private const string ReviewEntityType = "Review";

    private static Review BuildReview(Guid authorUserId) =>
        Review.Create(
            userId: authorUserId,
            targetType: ReviewTargetType.Tour,
            targetId: Guid.NewGuid(),
            rating: 4m,
            title: "Nice",
            content: "A solid experience overall with helpful staff and clean facilities.",
            visitDate: null,
            isVerifiedBooking: true,
            profanityDetected: false,
            timeProvider: TimeProvider.System);

    private static HybridCache Cache() => Substitute.For<HybridCache>();

    // ── Delete (author) ────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_invokes_attachment_cleanup_for_the_review()
    {
        var author = Guid.NewGuid();
        var review = BuildReview(author);
        var repo = Substitute.For<IReviewRepository>();
        repo.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        var uow = Substitute.For<ISocialUnitOfWork>();
        var cleanup = Substitute.For<IEntityAttachmentCleanupService>();

        var handler = new DeleteReviewCommandHandler(
            repo, uow, Cache(), TimeProvider.System, cleanup,
            NullLogger<DeleteReviewCommandHandler>.Instance);

        var result = await ((IRequestHandler<DeleteReviewCommand, Result>)handler).Handle(
            new DeleteReviewCommand(review.Id, author, IsAdmin: false, review.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cleanup.Received(1).DeleteEntityAttachmentsAsync(ReviewEntityType, review.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_succeeds_even_when_attachment_cleanup_throws()
    {
        var author = Guid.NewGuid();
        var review = BuildReview(author);
        var repo = Substitute.For<IReviewRepository>();
        repo.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        var uow = Substitute.For<ISocialUnitOfWork>();
        var cleanup = Substitute.For<IEntityAttachmentCleanupService>();
        cleanup.DeleteEntityAttachmentsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("cleanup boom"));

        var handler = new DeleteReviewCommandHandler(
            repo, uow, Cache(), TimeProvider.System, cleanup,
            NullLogger<DeleteReviewCommandHandler>.Instance);

        var result = await ((IRequestHandler<DeleteReviewCommand, Result>)handler).Handle(
            new DeleteReviewCommand(review.Id, author, IsAdmin: false, review.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Remove (admin) ───────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_invokes_attachment_cleanup_for_the_review()
    {
        var review = BuildReview(Guid.NewGuid());
        var repo = Substitute.For<IReviewRepository>();
        repo.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        var moderationLog = Substitute.For<IContentModerationLogRepository>();
        var uow = Substitute.For<ISocialUnitOfWork>();
        var cleanup = Substitute.For<IEntityAttachmentCleanupService>();

        var handler = new RemoveReviewCommandHandler(
            repo, moderationLog, uow, Cache(), TimeProvider.System, cleanup,
            NullLogger<RemoveReviewCommandHandler>.Instance);

        var result = await ((IRequestHandler<RemoveReviewCommand, Result>)handler).Handle(
            new RemoveReviewCommand(Guid.NewGuid(), review.Id, review.RowVersion, "spam"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cleanup.Received(1).DeleteEntityAttachmentsAsync(ReviewEntityType, review.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_succeeds_even_when_attachment_cleanup_throws()
    {
        var review = BuildReview(Guid.NewGuid());
        var repo = Substitute.For<IReviewRepository>();
        repo.GetByIdAsync(review.Id, Arg.Any<CancellationToken>()).Returns(review);
        var moderationLog = Substitute.For<IContentModerationLogRepository>();
        var uow = Substitute.For<ISocialUnitOfWork>();
        var cleanup = Substitute.For<IEntityAttachmentCleanupService>();
        cleanup.DeleteEntityAttachmentsAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("cleanup boom"));

        var handler = new RemoveReviewCommandHandler(
            repo, moderationLog, uow, Cache(), TimeProvider.System, cleanup,
            NullLogger<RemoveReviewCommandHandler>.Instance);

        var result = await ((IRequestHandler<RemoveReviewCommand, Result>)handler).Handle(
            new RemoveReviewCommand(Guid.NewGuid(), review.Id, review.RowVersion, "spam"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
