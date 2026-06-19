using ContentCore.Contracts.Attachments;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Social.Application.Queries.GetPublicReviews;
using Social.Domain.Entities;
using Social.Domain.Enums;
using Social.Domain.Repositories;
using Xunit;

namespace Social.Tests.Unit;

/// <summary>
/// REVIEWS-IMG-B: the public reviews query must surface review image URLs
/// (ImageUrls) by batch-loading ContentCore EntityType=Review attachments, and
/// must do so without an N+1 (a single batch call). Image URLs are emitted only
/// for the published reviews returned by the repository's published-gated list.
/// </summary>
public sealed class GetPublicReviewsImageTests
{
    private static Review BuildPublishedReview() =>
        Review.Create(
            userId: Guid.NewGuid(),
            targetType: ReviewTargetType.Tour,
            targetId: Guid.NewGuid(),
            rating: 5m,
            title: "Great",
            content: "An excellent tour with friendly guides and great views.",
            visitDate: null,
            isVerifiedBooking: true,
            profanityDetected: false,
            timeProvider: TimeProvider.System);

    private static IReviewRepository RepoReturning(params Review[] reviews)
    {
        var repo = Substitute.For<IReviewRepository>();
        repo.GetPublicListAsync(
                Arg.Any<ReviewTargetType>(), Arg.Any<Guid>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Review>)reviews.ToList(), reviews.Length));
        return repo;
    }

    [Fact]
    public async Task Public_reviews_include_image_urls_from_batch_reader()
    {
        var reviewA = BuildPublishedReview();
        var reviewB = BuildPublishedReview();
        var repo = RepoReturning(reviewA, reviewB);

        var reader = Substitute.For<IPublicEntityImageReader>();
        reader.GetEntityImagesBatchAsync("Review", Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<EntityImageDto>>
            {
                [reviewA.Id] = new List<EntityImageDto>
                {
                    new("/uploads/reviews/a1.jpg", null, 0, true),
                    new("/uploads/reviews/a2.jpg", null, 1, false),
                },
            });

        var handler = new GetPublicReviewsQueryHandler(
            repo, reader, NullLogger<GetPublicReviewsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetPublicReviewsQuery(ReviewTargetType.Tour, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items;
        items.Single(r => r.Id == reviewA.Id).ImageUrls
            .Should().Equal("/uploads/reviews/a1.jpg", "/uploads/reviews/a2.jpg");
        // Reviews with no attachments expose an empty (non-null) list.
        items.Single(r => r.Id == reviewB.Id).ImageUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task Public_reviews_batch_load_images_once_no_n_plus_one()
    {
        var repo = RepoReturning(BuildPublishedReview(), BuildPublishedReview(), BuildPublishedReview());
        var reader = Substitute.For<IPublicEntityImageReader>();
        reader.GetEntityImagesBatchAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<EntityImageDto>>());

        var handler = new GetPublicReviewsQueryHandler(
            repo, reader, NullLogger<GetPublicReviewsQueryHandler>.Instance);

        await handler.Handle(new GetPublicReviewsQuery(ReviewTargetType.Tour, Guid.NewGuid()), CancellationToken.None);

        await reader.Received(1).GetEntityImagesBatchAsync(
            "Review", Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        // The per-review single-id reader is never used by the public list path.
        await reader.DidNotReceive().GetEntityImagesAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Public_reviews_empty_when_no_reviews()
    {
        var repo = RepoReturning();
        var reader = Substitute.For<IPublicEntityImageReader>();
        reader.GetEntityImagesBatchAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, IReadOnlyList<EntityImageDto>>());

        var handler = new GetPublicReviewsQueryHandler(
            repo, reader, NullLogger<GetPublicReviewsQueryHandler>.Instance);

        var result = await handler.Handle(
            new GetPublicReviewsQuery(ReviewTargetType.Tour, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }
}
