using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentTours.Contracts.Tours;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.LinkBlogTours;

public sealed class LinkBlogToursCommandHandler(
    IBlogRepository blogRepository,
    IBlogTourRepository blogTourRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    ITourExistenceService tourExistenceService,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<LinkBlogToursCommandHandler> logger)
    : ICommandHandler<LinkBlogToursCommand>
{
    private const int MaxLinksPerBlog = 10;

    public async Task<Result> Handle(
        LinkBlogToursCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var blog = await blogRepository
                .GetByIdAsync(request.BlogId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (blog is null)
            {
                return Result.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            var hierarchy = await authorHierarchyGuard
                .EnsureCanManageBlogOwnedByAsync(blog.AuthorId, cancellationToken)
                .ConfigureAwait(false);
            if (!hierarchy.IsSuccess)
                return hierarchy;

            if (!RowVersionUtil.Equal(blog.RowVersion, request.RowVersion))
            {
                logger.LogWarning(
                    "LinkBlogTours rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            foreach (var item in request.Tours)
            {
                var status = await tourExistenceService
                    .GetStatusAsync(item.TourId, cancellationToken)
                    .ConfigureAwait(false);
                switch (status)
                {
                    case TourExistenceStatus.Deleted:
                        return Result.Failure(
                            new Error(
                                "Blog.TourDeleted",
                                $"Tour '{item.TourId}' has been deleted."),
                            Outcome.UnprocessableEntity);

                    case TourExistenceStatus.Active:
                        continue;

                    case TourExistenceStatus.NotFound:
                    default:
                        return Result.Failure(
                            new Error(
                                "Blog.TourNotFound",
                                $"Tour '{item.TourId}' does not exist."),
                            Outcome.UnprocessableEntity);
                }
            }

            var existing = await blogTourRepository
                .GetLinkedTourIdsAsync(blog.Id, cancellationToken)
                .ConfigureAwait(false);

            var newTours = request.Tours
                .Where(t => !existing.Contains(t.TourId))
                .Select(t => (t.TourId, t.SortOrder))
                .ToList();

            if (existing.Count + newTours.Count > MaxLinksPerBlog)
            {
                logger.LogWarning(
                    "LinkBlogTours rejected: blog {BlogId} would exceed the per-blog max " +
                    "of {Max} tour links (existing={Existing}, adding={Adding}).",
                    blog.Id, MaxLinksPerBlog, existing.Count, newTours.Count);
                return Result.Failure(
                    new Error(
                        "Blog.MaxToursExceeded",
                        $"A blog cannot be linked to more than {MaxLinksPerBlog} tours."),
                    Outcome.Conflict);
            }

            var utcNow = DateTime.UtcNow;
            var addedIds = blog.LinkTours(newTours, utcNow);

            if (addedIds.Count > 0)
            {
                var newLinks = blog.BlogTours
                    .Where(bt => addedIds.Contains(bt.TourId))
                    .ToList();
                await blogTourRepository.AddRangeAsync(newLinks, cancellationToken)
                    .ConfigureAwait(false);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            if (addedIds.Count > 0)
            {
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken)
                    .ConfigureAwait(false);
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogToursTag(blog.Id), cancellationToken)
                    .ConfigureAwait(false);

                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "LinkBlogTours: blog {BlogId} linked to {Added} new tour(s) (skipped {Skipped} duplicates).",
                blog.Id, addedIds.Count, request.Tours.Count - addedIds.Count);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
