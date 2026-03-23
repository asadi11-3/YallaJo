using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
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
    public async Task<Result> Handle(AssignTagsToEntityCommand request, CancellationToken ct)
    {
        try
        {
            if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
                return Result.Failure(
                    new Error("Entity.InvalidType", $"Entity type '{request.EntityType}' is not recognized."),
                    Outcome.Invalid);

            foreach (var tagId in request.TagIds)
            {
                var tag = await tagRepository.GetByIdAsync(tagId, ct);
                if (tag is null)
                    return Result.Failure(
                        new Error("Tag.NotFound", $"Tag '{tagId}' was not found."),
                        Outcome.NotFound);

                var exists = await entityTagRepository.ExistsAsync(entityType, request.EntityId, tagId, ct);
                if (exists)
                    continue;

                var entityTag = ContentCore.Domain.Entities.EntityTag.Create(entityType, request.EntityId, tagId);
                entityTagRepository.Add(entityTag);
            }

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
