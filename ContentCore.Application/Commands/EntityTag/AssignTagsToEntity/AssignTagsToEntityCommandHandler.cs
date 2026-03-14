using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.EntityTag.AssignTagsToEntity;

public sealed class AssignTagsToEntityCommandHandler(
    IEntityTagRepository entityTagRepository,
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork)
    : ICommandHandler<AssignTagsToEntityCommand>
{
    public async Task<Result> Handle(AssignTagsToEntityCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
            return Result.Invalid(new Error("InvalidEntityType", "Invalid entity type."));

        foreach (var tagId in request.TagIds)
        {
            var tag = await tagRepository.GetByIdAsync(tagId, ct);
            if (tag is null)
                return Result.NotFound($"Tag '{tagId}' not found.");

            var exists = await entityTagRepository.ExistsAsync(entityType, request.EntityId, tagId, ct);
            if (exists)
                continue;

            var entityTag = ContentCore.Domain.Entities.EntityTag.Create(entityType, request.EntityId, tagId);
            entityTagRepository.Add(entityTag);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
