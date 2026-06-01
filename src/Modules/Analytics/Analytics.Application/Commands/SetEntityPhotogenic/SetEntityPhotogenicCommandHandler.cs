using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.SetEntityPhotogenic;

public sealed class SetEntityPhotogenicCommandHandler(
    IEntityAttributeSnapshotRepository snapshotRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<SetEntityPhotogenicCommandHandler> logger) : ICommandHandler<SetEntityPhotogenicCommand>
{
    public async Task<Result> Handle(SetEntityPhotogenicCommand request, CancellationToken ct)
    {
        var snapshot = await snapshotRepository.GetByEntityAsync(request.Kind, request.EntityId, ct).ConfigureAwait(false);
        if (snapshot is null)
            return Result.Failure(new Error("EntityAttributeSnapshot.NotFound", "Entity attribute snapshot was not found."), Outcome.NotFound);

        snapshot.SetPhotogenic(request.IsPhotogenic);
        snapshotRepository.Update(snapshot);

        try
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("EntityAttributeSnapshot.ConcurrencyConflict", "Entity attribute snapshot was modified concurrently. Please refresh and try again."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync($"analytics:entity:{request.Kind}:{request.EntityId:N}", ct).ConfigureAwait(false);
        logger.LogInformation("Set analytics photogenic flag for {Kind}/{EntityId} to {IsPhotogenic}", request.Kind, request.EntityId, request.IsPhotogenic);
        return Result.Success();
    }
}
