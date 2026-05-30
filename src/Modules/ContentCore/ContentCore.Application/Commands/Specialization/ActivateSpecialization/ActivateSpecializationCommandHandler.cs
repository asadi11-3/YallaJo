using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Specialization.ActivateSpecialization;

public sealed class ActivateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ActivateSpecializationCommandHandler> logger)
    : ICommandHandler<ActivateSpecializationCommand>
{
    public async Task<Result> Handle(ActivateSpecializationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var spec = await specializationRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);
            if (spec is null)
            {
                return Result.Failure(
                   new Error("Specialization.NotFound", $"Specialization '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            if (spec.IsActive)
                return Result.Success();   // idempotent no-op

            spec.Activate();

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
            await cache.RemoveByTagAsync(ContentCoreCacheKeys.SpecializationTag(spec.Id), cancellationToken);

            logger.LogInformation("Specialization {SpecializationId} activated.", spec.Id);

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
