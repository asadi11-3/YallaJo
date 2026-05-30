namespace ContentSeo.Application.Commands.Sitemap.UpdateSitemapEntry;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class UpdateSitemapEntryCommandHandler(
    ISitemapEntryRepository sitemapEntryRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateSitemapEntryCommandHandler> logger)
    : ICommandHandler<UpdateSitemapEntryCommand>
{
    public async Task<Result> Handle(UpdateSitemapEntryCommand request, CancellationToken ct)
    {
        try
        {
            var entry = await sitemapEntryRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (entry is null || entry.IsDeleted)
            {
                return Result.Failure(new Error("SitemapEntry.NotFound", $"Sitemap entry {request.Id} not found."), Outcome.NotFound);
            }

            entry.UpdateHints(request.Priority, request.ChangeFrequency);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("SitemapEntry.ConcurrencyConflict", "Sitemap entry was modified concurrently."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagSitemapRendered, ct);
            logger.LogInformation("Updated sitemap entry {SitemapEntryId}", request.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
