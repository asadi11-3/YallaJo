using ContentCore.Domain.Enums;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
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
    public async Task<Result> Handle(RemoveTagFromEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entityType = Enum.Parse<EntityType>(request.EntityType, true);

            var entityTags = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, cancellationToken);
            var entityTag = entityTags.FirstOrDefault(et => et.TagId == request.TagId);
            if (entityTag is null) {
                return Result.Failure(
                      new Error(
                          "EntityTag.NotFound",
                          $"Tag '{request.TagId}' is not assigned to {request.EntityType}/{request.EntityId}."),
                      Outcome.NotFound);
            }

            entityTagRepository.Remove(entityTag);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Entity.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"entity-tags:{request.EntityType}:{request.EntityId}", cancellationToken);

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
