namespace ContentSeo.Application.Commands.Redirect.UpdateRedirect;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class UpdateRedirectCommandHandler(
    IRedirectRepository redirectRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateRedirectCommandHandler> logger)
    : ICommandHandler<UpdateRedirectCommand>
{
    private const int MaxHops = 3;

    public async Task<Result> Handle(UpdateRedirectCommand request, CancellationToken ct)
    {
        try
        {
            var redirect = await redirectRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (redirect is null || redirect.IsDeleted)
            {
                return Result.Failure(new Error("Redirect.NotFound", $"Redirect {request.Id} not found."), Outcome.NotFound);
            }

            var newUrl = string.IsNullOrWhiteSpace(request.NewUrl) ? null : request.NewUrl.Trim();
            if (newUrl is not null && string.Equals(redirect.OldUrl, newUrl, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new Error("Redirect.CircularChain", "Redirect target cannot equal source URL."), Outcome.Conflict);
            }

            if (newUrl is not null && !string.Equals(redirect.NewUrl, newUrl, StringComparison.Ordinal))
            {
                var finalTargetResult = await ResolveFinalTargetAsync(redirect.OldUrl, newUrl, ct);
                if (!finalTargetResult.IsSuccess)
                {
                    return Result.Failure(finalTargetResult.Errors.First(), finalTargetResult.Outcome);
                }

                var existing = await redirectRepository.GetActivePointingToAsync(redirect.OldUrl, ct);
                foreach (var r in existing.Where(r => r.Id != redirect.Id))
                {
                    r.RewriteTo(finalTargetResult.Value, hopsCollapsed: 1);
                }

                redirect.Update(finalTargetResult.Value, request.StatusCode);
            }
            else
            {
                redirect.Update(null, request.StatusCode);
            }

            if (request.IsActive.HasValue && request.IsActive.Value != redirect.IsActive)
            {
                if (request.IsActive.Value)
                {
                    redirect.Activate();
                }
                else
                {
                    redirect.Deactivate();
                }
            }

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("Redirect.ConcurrencyConflict", "Redirect was modified concurrently."), Outcome.Conflict);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Unexpected database error updating redirect {RedirectId}", request.Id);
                return Result.Failure(new Error("Redirect.DatabaseError", "Unexpected database error updating redirect."), Outcome.ServerError);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagRedirectsList, ct);
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagRedirectsLookup, ct);

            logger.LogInformation("Updated redirect {RedirectId}", request.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }

    private async Task<Result<string>> ResolveFinalTargetAsync(string oldUrl, string newUrl, CancellationToken ct)
    {
        var currentTarget = newUrl;
        var hops = 0;

        while (true)
        {
            var next = await redirectRepository.GetActiveByOldUrlAsync(currentTarget, ct);
            if (next is null)
            {
                return Result<string>.Success(currentTarget);
            }

            currentTarget = next.NewUrl;
            hops++;

            if (string.Equals(currentTarget, oldUrl, StringComparison.OrdinalIgnoreCase))
            {
                return Result<string>.Failure(new Error("Redirect.CircularChain", "Updating this redirect would form a cycle."), Outcome.Conflict);
            }

            if (hops > MaxHops)
            {
                return Result<string>.Failure(new Error("Redirect.ChainTooLong", $"Redirect chain exceeds {MaxHops} hops."), Outcome.Conflict);
            }
        }
    }
}
