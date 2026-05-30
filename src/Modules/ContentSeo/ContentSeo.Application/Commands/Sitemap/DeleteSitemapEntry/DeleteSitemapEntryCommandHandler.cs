namespace ContentSeo.Application.Commands.Sitemap.DeleteSitemapEntry;

using ContentSeo.Application.Caching;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class DeleteSitemapEntryCommandHandler(
    ISitemapEntryRepository sitemapEntryRepository,
    HybridCache cache,
    ILogger<DeleteSitemapEntryCommandHandler> logger)
    : ICommandHandler<DeleteSitemapEntryCommand>
{
    public async Task<Result> Handle(DeleteSitemapEntryCommand request, CancellationToken ct)
    {
        try
        {
            var deleted = await sitemapEntryRepository.ExecuteDeleteAsync(s => s.Id == request.Id, ct);
            if (deleted == 0)
            {
                return Result.Failure(new Error("SitemapEntry.NotFound", $"Sitemap entry {request.Id} not found."), Outcome.NotFound);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagSitemapRendered, ct);
            logger.LogInformation("Deleted sitemap entry {SitemapEntryId}", request.Id);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error("SitemapEntry.ConcurrencyConflict", "Sitemap entry was modified concurrently."), Outcome.Conflict);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
