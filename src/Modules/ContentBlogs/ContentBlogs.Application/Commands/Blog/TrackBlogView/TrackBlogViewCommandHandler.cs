using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.TrackBlogView;

public sealed class TrackBlogViewCommandHandler(
    IBlogViewCounter counter,
    IBlogViewerHashService hashService,
    HybridCache cache,
    ILogger<TrackBlogViewCommandHandler> logger)
    : ICommandHandler<TrackBlogViewCommand, BlogViewCountResult>
{
    public async Task<Result<BlogViewCountResult>> Handle(
        TrackBlogViewCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ViewerId))
            {
                return Result<BlogViewCountResult>.Failure(
                    new Error("BlogView.InvalidViewerId", "ViewerId must not be empty."),
                    Outcome.Invalid);
            }

            byte[] viewerHash;
            try
            {
                viewerHash = hashService.Hash(request.ViewerKind, request.ViewerId);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                logger.LogError(ex, "BlogViewerHashService.Hash failed for ViewerKind={ViewerKind}.", request.ViewerKind);
                return Result<BlogViewCountResult>.Failure(
                    new Error("BlogView.UnsupportedViewerKind", $"Unsupported viewer kind '{request.ViewerKind}'."),
                    Outcome.Invalid);
            }

            var outcome = await counter.TryCountAsync(
                    request.BlogId,
                    request.ViewerKind,
                    viewerHash,
                    cancellationToken)
                .ConfigureAwait(false);

            if (outcome.Slug is null)
            {
                logger.LogDebug(
                    "TrackBlogView: blog {BlogId} not visible to anonymous caller.",
                    request.BlogId);
                return Result<BlogViewCountResult>.Failure(
                    new Error("Blog.NotFound", $"Blog '{request.BlogId}' was not found."),
                    Outcome.NotFound);
            }

            if (outcome.Counted)
            {
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogTag(request.BlogId), cancellationToken)
                    .ConfigureAwait(false);
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogSlugTag(outcome.Slug), cancellationToken)
                    .ConfigureAwait(false);
                await cache.RemoveByTagAsync(
                        ContentBlogsCacheKeys.BlogsListTag, cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "Blog view counted: {BlogId} (Kind={Kind})",
                    request.BlogId, request.ViewerKind);
            }
            else
            {
                logger.LogDebug(
                    "Blog view debounced (already counted for this viewer): {BlogId} (Kind={Kind})",
                    request.BlogId, request.ViewerKind);
            }

            return Result<BlogViewCountResult>.Success(outcome);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<BlogViewCountResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
