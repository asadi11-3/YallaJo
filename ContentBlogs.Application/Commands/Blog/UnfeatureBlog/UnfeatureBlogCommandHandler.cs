using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.UnfeatureBlog;

public sealed class UnfeatureBlogCommandHandler(
    IBlogRepository blogRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UnfeatureBlogCommandHandler> logger)
    : ICommandHandler<UnfeatureBlogCommand>
{
    public async Task<Result> Handle(
        UnfeatureBlogCommand request,
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

            if (!RowVersionUtil.Equal(blog.RowVersion, request.RowVersion))
            {
                return Result.Failure(
                    new Error("Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            var unfeatureResult = blog.Unfeature(DateTime.UtcNow);
            if (!unfeatureResult.IsSuccess)
            {
                return unfeatureResult;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Blog.ConcurrencyConflict",
                        "Blog was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogTag(blog.Id), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.BlogsListTag, cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.FeaturedBlogsTag, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Blog unfeatured: {BlogId} (Slug={Slug})", blog.Id, blog.Slug);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
