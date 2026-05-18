using ContentBlogs.Application.Caching;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.IncrementBlogViewCount;

public sealed class IncrementBlogViewCountCommandHandler(
    IBlogRepository blogRepository,
    HybridCache cache,
    ILogger<IncrementBlogViewCountCommandHandler> logger)
    : ICommandHandler<IncrementBlogViewCountCommand>
{
    public async Task<Result> Handle(
        IncrementBlogViewCountCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var incremented = await blogRepository
                .IncrementViewCountIfPublishedAsync(request.BlogId, cancellationToken)
                .ConfigureAwait(false);

            if (!incremented)
            {
                logger.LogDebug(
                    "IncrementViewCount: blog {BlogId} not visible (missing, not Published, or deleted).",
                    request.BlogId);
                return Result.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogTag(request.BlogId), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                    ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Blog view count incremented: {BlogId}", request.BlogId);

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
