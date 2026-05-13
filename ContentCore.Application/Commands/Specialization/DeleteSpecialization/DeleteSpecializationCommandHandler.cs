using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Specialization.DeleteSpecialization;

public sealed class DeleteSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteSpecializationCommandHandler> logger)
    : ICommandHandler<DeleteSpecializationCommand>
{
    public async Task<Result> Handle(DeleteSpecializationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var specialization = await specializationRepository.GetByIdAsync(
                request.Id, cancellationToken, asNoTracking: false);

            if (specialization is null)
            {
                return Result.Failure(
                   new Error("Specialization.NotFound", $"Specialization '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            // Specialization is AuditableEntity — soft-delete preserves history.
            // Hard Remove() is NOT used (unlike Tag which is simpler reference data).
            specialization.SoftDelete();

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Specialization.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentCoreCacheKeys.SpecializationsTag, cancellationToken);
            await cache.RemoveByTagAsync(ContentCoreCacheKeys.SpecializationTag(specialization.Id), cancellationToken);

            logger.LogInformation("Specialization {SpecializationId} soft-deleted.", specialization.Id);

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
