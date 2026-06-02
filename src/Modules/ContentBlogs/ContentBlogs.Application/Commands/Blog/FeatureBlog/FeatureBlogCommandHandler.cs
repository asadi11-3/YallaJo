using ContentBlogs.Application.Caching;
using YallaJo.SharedKernel.Application.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.FeatureBlog;

public sealed class FeatureBlogCommandHandler(
    IBlogRepository blogRepository,
    ICurrentUser currentUser,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<FeatureBlogCommandHandler> logger)
    : ICommandHandler<FeatureBlogCommand>
{
    public async Task<Result> Handle(
        FeatureBlogCommand request,
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

            var featureResult = blog.Feature(currentUser.UserId!.Value, DateTime.UtcNow, request.FeaturedUntil);
            if (!featureResult.IsSuccess)
            {
                return featureResult;
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

            logger.LogInformation("Blog featured: {BlogId} (Slug={Slug}) by admin {AdminId}, until={Until}",
                blog.Id, blog.Slug, currentUser.UserId!.Value, request.FeaturedUntil);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
