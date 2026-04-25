using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Specialization.DeactivateSpecialization;

public sealed class DeactivateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeactivateSpecializationCommandHandler> logger)
    : ICommandHandler<DeactivateSpecializationCommand>
{
    public async Task<Result> Handle(DeactivateSpecializationCommand request, CancellationToken ct)
    {
        try
        {
            var spec = await specializationRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (spec is null)
                return Result.Failure(
                    new Error("Specialization.NotFound", $"Specialization '{request.Id}' was not found."),
                    Outcome.NotFound);

            if (!spec.IsActive)
                return Result.Success();   // idempotent no-op

            spec.Deactivate();

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Specialization.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync("specializations", ct);
            await cache.RemoveByTagAsync($"specialization:{spec.Id}", ct);

            logger.LogInformation("Specialization {SpecializationId} deactivated.", spec.Id);

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
