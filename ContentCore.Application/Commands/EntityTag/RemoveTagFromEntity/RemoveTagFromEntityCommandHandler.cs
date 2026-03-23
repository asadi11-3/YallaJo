using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityTag.RemoveTagFromEntity;

public sealed class RemoveTagFromEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<RemoveTagFromEntityCommand>
{
    public async Task<Result> Handle(RemoveTagFromEntityCommand request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Result.Failure(
                    new Error("Entity.InvalidType", $"Entity type '{request.EntityType}' is not recognized."),
                    Outcome.Invalid);

            var entityTags = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, ct);
            var entityTag = entityTags.FirstOrDefault(et => et.TagId == request.TagId);
            if (entityTag is null)
                return Result.Failure(
                    new Error("EntityTag.NotFound",
                        $"Tag '{request.TagId}' is not assigned to {request.EntityType}/{request.EntityId}."),
                    Outcome.NotFound);

            entityTagRepository.Remove(entityTag);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Entity.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"entity-tags:{request.EntityType}:{request.EntityId}", ct);

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
