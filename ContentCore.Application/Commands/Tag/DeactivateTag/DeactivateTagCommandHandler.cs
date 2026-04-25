using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Tag.DeactivateTag;

public sealed class DeactivateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeactivateTagCommandHandler> logger)
    : ICommandHandler<DeactivateTagCommand>
{
    public async Task<Result> Handle(DeactivateTagCommand request, CancellationToken ct)
    {
        try
        {
            var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (tag is null)
                return Result.Failure(
                    new Error("Tag.NotFound", $"Tag '{request.Id}' was not found."),
                    Outcome.NotFound);

            // Gotcha #14: guard state before raising — idempotent no-op if already inactive.
            if (!tag.IsActive)
                return Result.Success();

            tag.Deactivate();

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Tag.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync("tags", ct);

            logger.LogInformation("Tag {TagId} deactivated.", tag.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
