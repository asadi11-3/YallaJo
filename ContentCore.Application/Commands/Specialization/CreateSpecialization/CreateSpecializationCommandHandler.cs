using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Specialization.CreateSpecialization;

public sealed class CreateSpecializationCommandHandler(
    ISpecializationRepository specializationRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<CreateSpecializationCommand, CreateSpecializationResult>
{
    public async Task<Result<CreateSpecializationResult>> Handle(
        CreateSpecializationCommand request,
        CancellationToken ct)
    {
        try
        {
            var specialization = Domain.Entities.Specialization.Create(
                request.Name,
                request.Description,
                request.Icon);

            await specializationRepository.AddAsync(specialization, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateSpecializationResult>.Conflict(
                    new Error("Specialization.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("specializations", ct);

            return Result<CreateSpecializationResult>.Created(
                new CreateSpecializationResult(specialization.Id, specialization.Name));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<CreateSpecializationResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
