using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;
using SpecializationEntity = ContentCore.Domain.Entities.Specialization;

namespace ContentCore.Application.Commands.Specialization.CreateSpecialization;

public sealed class CreateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateSpecializationCommandHandler> logger)
    : ICommandHandler<CreateSpecializationCommand, CreateSpecializationResult>
{
    public async Task<Result<CreateSpecializationResult>> Handle(
        CreateSpecializationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var specialization = SpecializationEntity.Create(
                request.Name,
                request.Description,
                request.Icon,
                request.SourceLanguageCode);

            await specializationRepository.AddAsync(specialization, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateSpecializationResult>.Conflict(
                    new Error(
                        "Specialization.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("specializations", cancellationToken);

            logger.LogInformation(
                "Specialization created: {SpecializationId} (Name={Name})",
                specialization.Id, specialization.Name);

            return Result<CreateSpecializationResult>.Created(
                new CreateSpecializationResult(specialization.Id, specialization.Name));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateSpecializationResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
