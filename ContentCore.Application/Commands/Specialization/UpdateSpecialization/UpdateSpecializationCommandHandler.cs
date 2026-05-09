using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization;

public sealed class UpdateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateSpecializationCommandHandler> logger)
    : ICommandHandler<UpdateSpecializationCommand, UpdateSpecializationResult>
{
    public async Task<Result<UpdateSpecializationResult>> Handle(
        UpdateSpecializationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var specialization = await specializationRepository.GetByIdAsync(request.Id, cancellationToken, asNoTracking: false);

            if (specialization is null) {
                return Result<UpdateSpecializationResult>.Failure(
                   new Error("Specialization.NotFound", $"Specialization '{request.Id}' was not found."),
                   Outcome.NotFound);
            }

            specialization.Update(request.Name, request.Description, request.Icon, request.SourceLanguageCode);

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    specialization.Activate();
                else
                    specialization.Deactivate();
            }

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdateSpecializationResult>.Conflict(
                    new Error(
                        "Specialization.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync(ContentCoreCacheKeys.SpecializationsTag, cancellationToken);
            await cache.RemoveByTagAsync(ContentCoreCacheKeys.SpecializationTag(specialization.Id), cancellationToken);

            logger.LogInformation("Specialization updated: {SpecializationId} (Name={Name})", specialization.Id, specialization.Name);

            return Result<UpdateSpecializationResult>.Success(
                new UpdateSpecializationResult(specialization.Id, specialization.Name));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<UpdateSpecializationResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
