using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.UnlinkBlogFromTour;

public sealed class UnlinkBlogFromTourCommandHandler(
    IBlogRepository blogRepository,
    IBlogTourRepository blogTourRepository,
    IBlogAuthorHierarchyGuard authorHierarchyGuard,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UnlinkBlogFromTourCommandHandler> logger)
    : ICommandHandler<UnlinkBlogFromTourCommand>
{
    public async Task<Result> Handle(
        UnlinkBlogFromTourCommand request,
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
                    "UnlinkBlogFromTour rejected: stale RowVersion for blog {BlogId}.", blog.Id);
                return Result.Failure(
                    new Error(
                        "Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }
            var removed = await blogTourRepository
                .RemoveAsync(blog.Id, request.TourId, cancellationToken)
                .ConfigureAwait(false);

            if (!removed)
            {
                return Result.Failure(
                    new Error(
                        "Blog.TourLinkNotFound",
                        $"Blog '{blog.Id}' is not linked to tour '{request.TourId}'."),
                    Outcome.NotFound);
            }

            blog.RegisterTourUnlinked(request.TourId, DateTime.UtcNow);

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

            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogToursTag(blog.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogSlugTag(blog.Slug), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "UnlinkBlogFromTour: blog {BlogId} unlinked from tour {TourId}.",
                blog.Id, request.TourId);

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
