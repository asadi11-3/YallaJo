using ContentCore.Domain.Enums;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;

public sealed class AssignTagsToEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<AssignTagsToEntityCommand>
{
    public async Task<Result> Handle(AssignTagsToEntityCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entityType = Enum.Parse<EntityType>(request.EntityType, true);

            var tags = await tagRepository.GetAllAsync(
                filter: t => request.TagIds.Contains(t.Id), ct: cancellationToken);

            if (tags.Count != request.TagIds.Count)
            {
                var foundIds = tags.Select(t => t.Id).ToHashSet();
                var missingId = request.TagIds.First(id => !foundIds.Contains(id));
                return Result.Failure(
                    new Error("Tag.NotFound", $"Tag '{missingId}' was not found."),
                    Outcome.NotFound);
            }

            var inactiveTag = tags.FirstOrDefault(t => !t.IsActive);
            if (inactiveTag is not null) {
                return Result.Failure(
                    new Error(
                        "Tag.Inactive",
                        $"Tag '{inactiveTag.Id}' is inactive and cannot be assigned."), Outcome.Invalid);
            }

            var existing = await entityTagRepository.GetByEntityAsync(entityType, request.EntityId, cancellationToken);
            var existingIds = existing.Select(x => x.TagId).ToHashSet();

            foreach (var tagId in request.TagIds)
            {
                if (existingIds.Contains(tagId))
                    continue;

                var entityTag = ContentCore.Domain.Entities.EntityTag.Create(entityType, request.EntityId, tagId);
                entityTagRepository.Add(entityTag);
                existingIds.Add(tagId);
            }

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
